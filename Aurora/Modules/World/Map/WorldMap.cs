/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Caps=OpenSim.Framework.Capabilities.Caps;
using OSDArray=OpenMetaverse.StructuredData.OSDArray;
using OSDMap=OpenMetaverse.StructuredData.OSDMap;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Services.Interfaces;
using System.Timers;

namespace Aurora.Modules
{
    public class AuroraWorldMapModule : INonSharedRegionModule, IWorldMapModule
	{
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string DEFAULT_WORLD_MAP_EXPORT_PATH = "exportmap.jpg";
        private static readonly UUID STOP_UUID = UUID.Random();
        private static readonly string m_mapLayerPath = "0001/";

        private OpenSim.Framework.BlockingQueue<MapRequestState> requests = new OpenSim.Framework.BlockingQueue<MapRequestState>();

        //private IConfig m_config;
        protected Scene m_scene;
        private List<MapBlockData> cachedMapBlocks = new List<MapBlockData>();
        private int cachedTime = 0;
        private byte[] myMapImageJPEG;
        protected volatile bool m_Enabled = false;
        private Dictionary<UUID, MapRequestState> m_openRequests = new Dictionary<UUID, MapRequestState>();
        private Dictionary<string, int> m_blacklistedurls = new Dictionary<string, int>();
        private Dictionary<ulong, int> m_blacklistedregions = new Dictionary<ulong, int>();
        private Dictionary<ulong, string> m_cachedRegionMapItemsAddress = new Dictionary<ulong, string>();
        private List<UUID> m_rootAgents = new List<UUID>();
        private volatile bool threadrunning = false;
        private IConfigSource m_config;
        //private int CacheRegionsDistance = 256;
        private double minutes = 30;
        private double oneminute = 60000;
        private System.Timers.Timer UpdateMapImage;
        private System.Timers.Timer UpdateOnlineStatus;
        private string SimMapServerURI;
        
		#region INonSharedRegionModule Members
        public virtual void Initialise(IConfigSource source)
		{
            if (source.Configs["MapModule"] != null)
            {
                if (source.Configs["MapModule"].GetString(
                        "WorldMapModule", "AuroraWorldMapModule") !=
                        "AuroraWorldMapModule")
                {
                    m_Enabled = false;
                    return;
                }
            }
            //m_log.Info("[AuroraWorldMap] Initializing");
            m_config = source;
			m_Enabled = true;
		}

		public virtual void AddRegion (Scene scene)
		{
            if (!m_Enabled)
                return;

            lock (scene)
            {
                m_scene = scene;

                m_scene.RegisterModuleInterface<IWorldMapModule>(this);

                m_scene.AddCommand(
                    this, "update map",
                    "update map",
                    "Updates the image of the world map", HandleUpdateWorldMapConsoleCommand);

                m_scene.AddCommand(
                    this, "export-map",
                    "export-map [<path>]",
                    "Save an image of the world map", HandleExportWorldMapConsoleCommand);

                AddHandlers();
            }
		}

		public virtual void RemoveRegion (Scene scene)
		{
			if (!m_Enabled)
				return;

			lock (m_scene)
			{
				m_Enabled = false;
				RemoveHandlers();
				m_scene = null;
			}
		}

		public virtual void RegionLoaded (Scene scene)
		{
            new MapActivityDetector(scene.SimMapConnector);
            SetUpTimers();
        }

        public void SetUpTimers()
        {
            UpdateMapImage = new System.Timers.Timer(oneminute * minutes);
            UpdateMapImage.Elapsed += OnTimedCreateNewMapImage;
            UpdateMapImage.Enabled = true;
            UpdateOnlineStatus = new System.Timers.Timer(oneminute * 5);
            UpdateOnlineStatus.Elapsed += OnUpdateRegion;
            UpdateOnlineStatus.Enabled = true;
        }

		public virtual void Close()
		{
		}

		public Type ReplaceableInterface
		{
			get { return null; }
		}

		public virtual string Name
		{
            get { return "AuroraWorldMapModule"; }
		}

		#endregion
		// this has to be called with a lock on m_scene
		protected virtual void AddHandlers()
		{
            string regionimage = "regionImage" + m_scene.RegionInfo.RegionID.ToString();
            regionimage = regionimage.Replace("-", "");
            //m_log.Info("[WORLD MAP]: JPEG Map location: http://" + m_scene.RegionInfo.ExternalEndPoint.Address.ToString() + ":" + m_scene.RegionInfo.HttpPort.ToString() + "/index.php?method=" + regionimage);

            MainServer.Instance.AddHTTPHandler(regionimage, OnHTTPGetMapImage);
            
            m_scene.EventManager.OnRegisterCaps += OnRegisterCaps;
            m_scene.EventManager.OnNewClient += OnNewClient;
            m_scene.EventManager.OnClientClosed += ClientLoggedOut;
            m_scene.EventManager.OnMakeChildAgent += MakeChildAgent;
            m_scene.EventManager.OnMakeRootAgent += MakeRootAgent;
		}

		// this has to be called with a lock on m_scene
		protected virtual void RemoveHandlers()
		{
            m_scene.EventManager.OnMakeRootAgent -= MakeRootAgent;
            m_scene.EventManager.OnMakeChildAgent -= MakeChildAgent;
            m_scene.EventManager.OnClientClosed -= ClientLoggedOut;
            m_scene.EventManager.OnNewClient -= OnNewClient;
            m_scene.EventManager.OnRegisterCaps -= OnRegisterCaps;

            string regionimage = "regionImage" + m_scene.RegionInfo.RegionID.ToString();
            regionimage = regionimage.Replace("-", "");
            MainServer.Instance.RemoveHTTPHandler("", regionimage);
		}

		public void OnRegisterCaps(UUID agentID, Caps caps)
		{
			//m_log.DebugFormat("[WORLD MAP]: OnRegisterCaps: agentID {0} caps {1}", agentID, caps);
			string capsBase = "/CAPS/" + caps.CapsObjectPath;
			caps.RegisterHandler("MapLayer",
			                     new RestStreamHandler("POST", capsBase + m_mapLayerPath,
			                                           delegate(string request, string path, string param,
			                                                    OSHttpRequest httpRequest, OSHttpResponse httpResponse)
			                                           {
			                                           	return MapLayerRequest(request, path, param,
			                                           	                       agentID, caps);
			                                           }));
		}

		/// <summary>
		/// Callback for a map layer request
		/// </summary>
		/// <param name="request"></param>
		/// <param name="path"></param>
		/// <param name="param"></param>
		/// <param name="agentID"></param>
		/// <param name="caps"></param>
		/// <returns></returns>
		public string MapLayerRequest(string request, string path, string param,
		                              UUID agentID, Caps caps)
		{
            ScenePresence avatarPresence = null;

            m_scene.TryGetScenePresence(agentID, out avatarPresence);

            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, agentID);
            if (avatarPresence != null)
            {
                List<MapBlockData> mapBlocks = new List<MapBlockData>(); ;

                List<SimMap> Sims = m_scene.SimMapConnector.GetSimMapRange(
                    (uint)(m_scene.RegionInfo.RegionLocX - 8) * Constants.RegionSize,
                    (uint)(m_scene.RegionInfo.RegionLocY - 8) * Constants.RegionSize,
                    (uint)(m_scene.RegionInfo.RegionLocX + 8) * Constants.RegionSize,
                    (uint)(m_scene.RegionInfo.RegionLocY + 8) * Constants.RegionSize,
                    agentID);
                foreach (SimMap map in Sims)
                {
                    mapBlocks.Add(map.ToMapBlockData());
                }
                avatarPresence.ControllingClient.SendMapBlock(mapBlocks, 0);
            }
            LLSDMapLayerResponse mapResponse = new LLSDMapLayerResponse();
            mapResponse.LayerData.Array.Add(GetOSDMapLayerResponse());
            return mapResponse.ToString();
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		protected static OSDMapLayer GetOSDMapLayerResponse()
		{
            OSDMapLayer mapLayer = new OSDMapLayer();
            mapLayer.Right = 5000;
            mapLayer.Top = 5000;
            mapLayer.ImageID = new UUID("00000000-0000-1111-9999-000000000006");

            return mapLayer;
		}
		#region EventHandlers

		/// <summary>
		/// Registered for event
		/// </summary>
		/// <param name="client"></param>
		private void OnNewClient(IClientAPI client)
		{
			client.OnRequestMapBlocks += RequestMapBlocks;
			client.OnMapItemRequest += HandleMapItemRequest;
		}

		/// <summary>
		/// Client logged out, check to see if there are any more root agents in the simulator
		/// If not, stop the mapItemRequest Thread
		/// Event handler
		/// </summary>
		/// <param name="AgentId">AgentID that logged out</param>
		private void ClientLoggedOut(UUID AgentId, Scene scene)
		{
			lock (m_rootAgents)
            {
                m_rootAgents.Remove(AgentId);
            }
		}
		#endregion

        public virtual void HandleMapItemRequest(IClientAPI remoteClient, uint flags,
            uint EstateID, bool godlike, uint itemtype, ulong regionhandle)
        {
            lock (m_rootAgents)
            {
                if (!m_rootAgents.Contains(remoteClient.AgentId))
                    return;
            }

            uint xstart = 0;
            uint ystart = 0;
            OpenMetaverse.Utils.LongToUInts(m_scene.RegionInfo.RegionHandle, out xstart, out ystart);
            
            List<mapItemReply> mapitems = new List<mapItemReply>();
            int tc = Environment.TickCount;
            if (itemtype == (int)OpenMetaverse.GridItemType.AgentLocations)
            {
                //If its local, just let it do it on its own.
                if (regionhandle == 0 || regionhandle == m_scene.RegionInfo.RegionHandle)
                {
                    //Only one person here, send a zero person response
                    mapItemReply mapitem = new mapItemReply();
                    if (m_scene.GetRootAgentCount() <= 1)
                    {
                        mapitem = new mapItemReply();
                        mapitem.x = (uint)(xstart + 1);
                        mapitem.y = (uint)(ystart + 1);
                        mapitem.id = UUID.Zero;
                        mapitem.name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString());
                        mapitem.Extra = 0;
                        mapitem.Extra2 = 0;
                        mapitems.Add(mapitem);
                        remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                        return;
                    }
                    m_scene.ForEachScenePresence(delegate(ScenePresence sp)
                    {
                        // Don't send a green dot for yourself
                        if (!sp.IsChildAgent && sp.UUID != remoteClient.AgentId)
                        {
                            mapitem = new mapItemReply();
                            mapitem.x = (uint)(xstart + sp.AbsolutePosition.X);
                            mapitem.y = (uint)(ystart + sp.AbsolutePosition.Y);
                            mapitem.id = UUID.Zero;
                            mapitem.name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString());
                            mapitem.Extra = 1;
                            mapitem.Extra2 = 0;
                            mapitems.Add(mapitem);
                        }
                    });
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                }
                else
                {
                    mapitems = m_scene.SimMapConnector.GetMapItems(regionhandle, (OpenMetaverse.GridItemType)itemtype);
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                    // GridRegion R = m_scene.GridService.GetRegionByPosition(UUID.Zero, (int)xstart, (int)ystart);
                    // if (((int)GridConnector.GetRegionFlags(R.RegionID) & (int)SimMapFlags.Hidden) == (int)SimMapFlags.Hidden)
                    // {
                    //     if (!m_scene.Permissions.CanIssueEstateCommand(remoteClient.AgentId, false))
                    //     {
                    //         return;
                    //     }
                    // }
                    // Remote Map Item Request

                    // ensures that the blockingqueue doesn't get borked if the GetAgents() timing changes.
                    // Note that we only start up a remote mapItem Request thread if there's users who could
                    // be making requests
                    // if (!threadrunning)
                    // {
                    //     //m_log.Warn("[WORLD MAP]: Starting new remote request thread manually.  This means that AvatarEnteringParcel never fired!  This needs to be fixed!  Don't Mantis this, as the developers can see it in this message");
                    //     StartThread(new object());
                    // }

                    // RequestMapItems("", remoteClient.AgentId, flags, EstateID, godlike, itemtype, regionhandle);
                }
            }
        }

		/// <summary>
		/// Requests map blocks in area of minX, maxX, minY, MaxY in world cordinates
		/// </summary>
		/// <param name="minX"></param>
		/// <param name="minY"></param>
		/// <param name="maxX"></param>
		/// <param name="maxY"></param>
		public virtual void RequestMapBlocks(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flag)
		{
            if ((flag & 0x10000) != 0)  // user clicked on the map a tile that isn't visible
            {
                ClickedOnTile(remoteClient, minX, minY, maxX, maxY, flag);
            }
            else
            {
                // normal mapblock request. Use the provided values
                GetAndSendBlocks(remoteClient, minX, minY, maxX, maxY, flag);
            }
		}

        protected virtual void ClickedOnTile(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flag)
        {
            List<MapBlockData> response = new List<MapBlockData>();

            // this should return one mapblock at most. 
            List<SimMap> Sims = m_scene.SimMapConnector.GetSimMapRange(
                (uint)(minX) * (int)Constants.RegionSize,
                (uint)(minY) * (int)Constants.RegionSize,
                (uint)(maxX) * (int)Constants.RegionSize,
                (uint)(maxY) * (int)Constants.RegionSize,
                remoteClient.AgentId);

            foreach (SimMap map in Sims)
            {
                response.Add(map.ToMapBlockData());
            }
            remoteClient.SendMapBlock(response, 0);
        }

		protected virtual void GetAndSendBlocks(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flag)
		{
            List<MapBlockData> mapBlocks = new List<MapBlockData>();
            List<SimMap> Sims = m_scene.SimMapConnector.GetSimMapRange(
                (uint)(minX - 4) * (int)Constants.RegionSize,
                (uint)(minY - 4) * (int)Constants.RegionSize,
                (uint)(maxX + 4) * (int)Constants.RegionSize,
                (uint)(maxY + 4) * (int)Constants.RegionSize,
                remoteClient.AgentId);

            foreach (SimMap map in Sims)
            {
                mapBlocks.Add(map.ToMapBlockData());
            }
            remoteClient.SendMapBlock(mapBlocks, flag);
		}

		protected void MapBlockFromGridRegion(MapBlockData block, GridRegion r)
		{
			block.Access = r.Access;
			block.MapImageId = r.TerrainImage;
			block.Name = r.RegionName;
			block.X = (ushort)(r.RegionLocX / Constants.RegionSize);
			block.Y = (ushort)(r.RegionLocY / Constants.RegionSize);
		}

        public Hashtable OnHTTPGetMapImage(Hashtable keysvals)
        {
            Hashtable reply = new Hashtable();
            string regionImage = "regionImage" + m_scene.RegionInfo.RegionID.ToString();
            regionImage = regionImage.Replace("-", "");
            if (keysvals["method"].ToString() != regionImage)
                return reply;
            m_log.Debug("[WORLD MAP]: Sending map image jpeg");
            int statuscode = 200;
            byte[] jpeg = new byte[0];

            if (myMapImageJPEG == null ||myMapImageJPEG.Length == 0)
            {
                MemoryStream imgstream = new MemoryStream();
                Bitmap mapTexture = new Bitmap(1, 1);
                ManagedImage managedImage;
                Image image = (Image)mapTexture;

                try
                {
                    // Taking our jpeg2000 data, decoding it, then saving it to a byte array with regular jpeg data

                    imgstream = new MemoryStream();

                    // non-async because we know we have the asset immediately.
                    AssetBase mapasset = m_scene.AssetService.Get(m_scene.RegionInfo.RegionSettings.TerrainImageID.ToString());

                    // Decode image to System.Drawing.Image
                    if (OpenJPEG.DecodeToImage(mapasset.Data, out managedImage, out image))
                    {
                        // Save to bitmap
                        mapTexture = new Bitmap(image);

                        EncoderParameters myEncoderParameters = new EncoderParameters();
                        myEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 95L);

                        // Save bitmap to stream
                        mapTexture.Save(imgstream, GetEncoderInfo("image/jpeg"), myEncoderParameters);

                        // Write the stream to a byte array for output
                        jpeg = imgstream.ToArray();
                        myMapImageJPEG = jpeg;
                    }
                }
                catch (Exception)
                {
                    // Dummy!
                    m_log.Warn("[WORLD MAP]: Unable to generate Map image");
                }
                finally
                {
                    // Reclaim memory, these are unmanaged resources
                    // If we encountered an exception, one or more of these will be null
                    if (mapTexture != null)
                        mapTexture.Dispose();

                    if (image != null)
                        image.Dispose();

                    if (imgstream != null)
                    {
                        imgstream.Close();
                        imgstream.Dispose();
                    }
                }
            }
            else
            {
                // Use cached version so we don't have to loose our mind
                jpeg = myMapImageJPEG;
            }

            reply["str_response_string"] = Convert.ToBase64String(jpeg);
            reply["int_response_code"] = statuscode;
            reply["content_type"] = "image/jpeg";

            return reply;
        }

        // From msdn
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (int j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        /// <summary>
        /// Update the world map
        /// </summary>
        /// <param name="fileName"></param>
        public void HandleUpdateWorldMapConsoleCommand(string module, string[] cmdparams)
        {
            m_scene.CreateTerrainTexture();
        }

        /// <summary>
        /// Export the world map
        /// </summary>
        /// <param name="fileName"></param>
        public void HandleExportWorldMapConsoleCommand(string module, string[] cmdparams)
        {
            if (m_scene.ConsoleScene() == null)
            {
                // FIXME: If console region is root then this will be printed by every module.  Currently, there is no
                // way to prevent this, short of making the entire module shared (which is complete overkill).
                // One possibility is to return a bool to signal whether the module has completely handled the command
                m_log.InfoFormat("[WORLD MAP]: Please change to a specific region in order to export its world map");
                return;
            }

            if (m_scene.ConsoleScene() != m_scene)
                return;

            string exportPath;

            if (cmdparams.Length > 1)
                exportPath = cmdparams[1];
            else
                exportPath = DEFAULT_WORLD_MAP_EXPORT_PATH;

            m_log.InfoFormat(
                "[WORLD MAP]: Exporting world map for {0} to {1}", m_scene.RegionInfo.RegionName, exportPath);

            List<MapBlockData> mapBlocks = new List<MapBlockData>();
            List<GridRegion> regions = m_scene.GridService.GetRegionRange(m_scene.RegionInfo.ScopeID,
                    (int)(m_scene.RegionInfo.RegionLocX - 9) * (int)Constants.RegionSize,
                    (int)(m_scene.RegionInfo.RegionLocX + 9) * (int)Constants.RegionSize,
                    (int)(m_scene.RegionInfo.RegionLocY - 9) * (int)Constants.RegionSize,
                    (int)(m_scene.RegionInfo.RegionLocY + 9) * (int)Constants.RegionSize);
            List<AssetBase> textures = new List<AssetBase>();
            List<Image> bitImages = new List<Image>();

            foreach (GridRegion r in regions)
            {
                MapBlockData mapBlock = new MapBlockData();
                MapBlockFromGridRegion(mapBlock, r);
                AssetBase texAsset = m_scene.AssetService.Get(mapBlock.MapImageId.ToString());

                if (texAsset != null)
                {
                    textures.Add(texAsset);
                }
            }

            foreach (AssetBase asset in textures)
            {
                ManagedImage managedImage;
                Image image;

                if (OpenJPEG.DecodeToImage(asset.Data, out managedImage, out image))
                    bitImages.Add(image);
            }

            Bitmap mapTexture = new Bitmap(2560, 2560);
            Graphics g = Graphics.FromImage(mapTexture);
            SolidBrush sea = new SolidBrush(Color.DarkBlue);
            g.FillRectangle(sea, 0, 0, 2560, 2560);

            for (int i = 0; i < mapBlocks.Count; i++)
            {
                ushort x = (ushort)((mapBlocks[i].X - m_scene.RegionInfo.RegionLocX) + 10);
                ushort y = (ushort)((mapBlocks[i].Y - m_scene.RegionInfo.RegionLocY) + 10);
                g.DrawImage(bitImages[i], (x * 128), 2560 - (y * 128), 128, 128); // y origin is top
            }

            mapTexture.Save(exportPath, ImageFormat.Jpeg);

            m_log.InfoFormat(
                "[WORLD MAP]: Successfully exported world map for {0} to {1}",
                m_scene.RegionInfo.RegionName, exportPath);
        }

        public void RegenerateMaptile(string ID, byte[] data)
        {
            myMapImageJPEG = data;
            List<SimMap> map = m_scene.SimMapConnector.GetSimMap(m_scene.RegionInfo.RegionID, m_scene.RegionInfo.EstateSettings.EstateOwner);
            //This will be null if the region has never joined the grid before.
            if(map != null && map.Count != 0 && map[0] != null)
            {
                SimMap sim = map[0];
                sim.SimMapTextureID = new UUID(ID);
                m_scene.SimMapConnector.UpdateSimMap(sim);
            }
        }

        private void MakeRootAgent(ScenePresence avatar)
		{

			lock (m_rootAgents)
			{
				if (!m_rootAgents.Contains(avatar.UUID))
				{
					m_rootAgents.Add(avatar.UUID);
				}
			}
		}

		private void MakeChildAgent(ScenePresence avatar)
		{
			lock (m_rootAgents)
            {
                m_rootAgents.Remove(avatar.UUID);
            }
        }

        private void OnUpdateRegion(object source, ElapsedEventArgs e)
        {
            m_scene.SimMapConnector.UpdateSimMapOnlineStatus(m_scene.RegionInfo.RegionID);
        }

        private void OnTimedCreateNewMapImage(object source, ElapsedEventArgs e)
        {
            m_scene.CreateTerrainTexture();
        }
	}

	public struct MapRequestState
	{
		public UUID agentID;
		public uint flags;
		public uint EstateID;
		public bool godlike;
		public uint itemtype;
		public ulong regionhandle;
	}
}
