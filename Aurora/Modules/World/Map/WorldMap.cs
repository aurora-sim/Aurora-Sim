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

        //private IConfig m_config;
        protected Scene m_scene;
        private byte[] myMapImageJPEG;
        protected bool m_Enabled = false;
        private List<UUID> m_rootAgents = new List<UUID>();
        private IConfigSource m_config;
        private ExpiringCache<ulong, MapBlockData> m_mapRegionCache = new ExpiringCache<ulong, MapBlockData>();
        private ExpiringCache<ulong, List< mapItemReply>> m_mapItemCache = new ExpiringCache<ulong, List<mapItemReply>>();
        private ExpiringCache<string, List<MapBlockData>> m_terrainCache = new ExpiringCache<string, List<MapBlockData>>();

        private List<MapItemRequester> m_itemsToRequest = new List<MapItemRequester>();
        private bool itemRequesterIsRunning = false;
        private AuroraThreadPool threadpool = null;
        private AuroraThreadPool blockthreadpool = null;
        private double minutes = 30;
        private double oneminute = 60000;
        private System.Timers.Timer UpdateMapImage;
        private System.Timers.Timer UpdateOnlineStatus;
        
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
            UpdateMapImage.Stop();
            UpdateMapImage.Elapsed -= OnTimedCreateNewMapImage;
            UpdateMapImage.Enabled = false;
            UpdateMapImage.Close();

            UpdateOnlineStatus.Stop();
            UpdateOnlineStatus.Elapsed -= OnUpdateRegion;
            UpdateOnlineStatus.Enabled = false;
            UpdateOnlineStatus.Close();
		}

		public virtual void RegionLoaded (Scene scene)
		{
            if (!m_Enabled)
                return;

            AuroraThreadPoolStartInfo info = new AuroraThreadPoolStartInfo();
            info.priority = ThreadPriority.Lowest;
            info.Threads = 1;
            threadpool = new AuroraThreadPool(info);
            blockthreadpool = new AuroraThreadPool(info);

            new MapActivityDetector(scene);
            SetUpTimers();
        }

        public void SetUpTimers()
        {
            UpdateMapImage = new System.Timers.Timer(oneminute * minutes);
            UpdateMapImage.Elapsed += OnTimedCreateNewMapImage;
            UpdateMapImage.Enabled = true;
            UpdateOnlineStatus = new System.Timers.Timer(oneminute * 20);
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
            m_log.Info("[WORLD MAP]: JPEG Map location: http://" + m_scene.RegionInfo.ExternalEndPoint.Address.ToString() + ":" + m_scene.RegionInfo.HttpPort.ToString() + "/index.php?method=" + regionimage);

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

                List<GridRegion> regions = m_scene.GridService.GetRegionRange(m_scene.RegionInfo.ScopeID,
                        (int)(m_scene.RegionInfo.RegionLocX - 8) * (int)Constants.RegionSize,
                        (int)(m_scene.RegionInfo.RegionLocX + 8) * (int)Constants.RegionSize,
                        (int)(m_scene.RegionInfo.RegionLocY - 8) * (int)Constants.RegionSize,
                        (int)(m_scene.RegionInfo.RegionLocY + 8) * (int)Constants.RegionSize);
                foreach (GridRegion r in regions)
                {
                    mapBlocks.Add(MapBlockFromGridRegion(r));
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
            mapLayer.Bottom = 0;
            mapLayer.Left = 0;
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
            client.OnMapNameRequest += OnMapNameRequest;
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
                    List<mapItemReply> reply;
                    if (!m_mapItemCache.TryGetValue(regionhandle, out reply))
                    {
                        m_itemsToRequest.Add(new MapItemRequester()
                        {
                            flags = flags,
                            itemtype = itemtype,
                            regionhandle = regionhandle,
                            remoteClient = remoteClient
                        });

                        if(!itemRequesterIsRunning)
                            threadpool.QueueEvent(GetMapItems, 3);
                    }
                    else
                    {
                        remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, 0);
                    }
                }
            }
        }

        private bool GetMapItems()
        {
            itemRequesterIsRunning = true;
            for (int i = 0; i < m_itemsToRequest.Count; i++)
            {
                MapItemRequester item = m_itemsToRequest[i];
                if (item == null)
                    continue;
                List<mapItemReply> mapitems = new List<mapItemReply>();
                if (!m_mapItemCache.TryGetValue(item.regionhandle, out mapitems)) //try again, might have gotten picked up by this already
                {
                    multipleMapItemReply allmapitems = m_scene.GridService.GetMapItems(item.regionhandle, (OpenMetaverse.GridItemType)item.itemtype);

                    //Send out the update
                    if (allmapitems.items.ContainsKey(item.regionhandle))
                    {
                        mapitems = allmapitems.items[item.regionhandle];
                        item.remoteClient.SendMapItemReply(mapitems.ToArray(), item.itemtype, 0);

                        //Update the cache
                        foreach (KeyValuePair<ulong, List<mapItemReply>> kvp in allmapitems.items)
                        {
                            m_mapItemCache.AddOrUpdate(kvp.Key, kvp.Value, DateTime.Now.AddHours(0.5));
                        }
                    }
                }
            }
            itemRequesterIsRunning = false;
            return true;
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
            else if (flag == 0) //Terrain and objects
            {
                // normal mapblock request. Use the provided values
                GetAndSendMapBlocks(remoteClient, minX, minY, maxX, maxY, flag);
            }
            else if ((flag & 1) == 1) //Terrain only
            {
                // normal terrain only request. Use the provided values
                GetAndSendTerrainBlocks(remoteClient, minX, minY, maxX, maxY, flag);
            }
            else
            {
                if (flag != 2) //Land sales
                    m_log.Warn("[World Map] : Got new flag, " + flag + " RequestMapBlocks()");
            }
		}

        protected virtual void ClickedOnTile(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flag)
        {
            m_blockitemsToRequest.Add(new MapBlockRequester()
            {
                maxX = maxX,
                maxY = maxY,
                minX = minX,
                minY = minY,
                remoteClient = remoteClient
            });
            if (!blockRequesterIsRunning)
                blockthreadpool.QueueEvent(GetMapBlocks, 3);
        }

        protected virtual void GetAndSendMapBlocks(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flag)
        {
            m_blockitemsToRequest.Add(new MapBlockRequester()
            {
                maxX = maxX,
                maxY = maxY,
                minX = minX,
                minY = minY,
                remoteClient = remoteClient
            });
            if (!blockRequesterIsRunning)
                blockthreadpool.QueueEvent(GetMapBlocks, 3);
        }

        protected virtual void GetAndSendTerrainBlocks(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flag)
        {
            List<MapBlockData> mapBlocks = new List<MapBlockData>();
            string CacheKey = minX + " " + maxX + " " + minY + " " + maxY;
            if (!m_terrainCache.TryGetValue(CacheKey, out mapBlocks))
            {
                mapBlocks = new List<MapBlockData>();
                List<GridRegion> regions = m_scene.GridService.GetRegionRange(m_scene.RegionInfo.ScopeID,
                    (minX - 4) * (int)Constants.RegionSize,
                    (maxX + 4) * (int)Constants.RegionSize,
                    (minY - 4) * (int)Constants.RegionSize,
                    (maxY + 4) * (int)Constants.RegionSize);

                foreach (GridRegion region in regions)
                {
                    mapBlocks.Add(TerrainBlockFromGridRegion(region));
                }
            }
            m_terrainCache.AddOrUpdate(CacheKey, mapBlocks, DateTime.Now.AddHours(0.5));

            remoteClient.SendMapBlock(mapBlocks, 1);
        }

        private bool blockRequesterIsRunning = false;
        private List<MapBlockRequester> m_blockitemsToRequest = new List<MapBlockRequester>();
        //private MapBlockData[,] mapBlockCache = new MapBlockData[10000, 10000];

        private class MapBlockRequester
        {
            public int minX;
            public int minY;
            public int maxX;
            public int maxY;
            public IClientAPI remoteClient;
        }

        private bool GetMapBlocks()
        {
            try
            {
                blockRequesterIsRunning = true;
                for (int i = 0; i < m_blockitemsToRequest.Count; i++)
                {
                    MapBlockRequester item = m_blockitemsToRequest[i];
                    if (item == null)
                        continue;
                    List<MapBlockData> mapBlocks = new List<MapBlockData>();
                    //bool foundAll = true;

                    /*for (int X = item.minX; i < item.maxX; i++)
                    {
                        for (int Y = item.minY; i < item.maxY; i++)
                        {
                            if (mapBlockCache[X, Y] != null)
                            {
                                mapBlocks.Add(mapBlockCache[X, Y]);
                            }
                            else foundAll = false;
                        }
                    }
                    if (!foundAll)
                    {*/
                        List<GridRegion> regions = m_scene.GridService.GetRegionRange(m_scene.RegionInfo.ScopeID,
                                (item.minX - 4) * (int)Constants.RegionSize,
                                (item.maxX + 4) * (int)Constants.RegionSize,
                                (item.minY - 4) * (int)Constants.RegionSize,
                                (item.maxY + 4) * (int)Constants.RegionSize);

                        foreach (GridRegion region in regions)
                        {
                            //mapBlockCache[region.RegionLocX / 256, region.RegionLocY / 256] = MapBlockFromGridRegion(region);
                            mapBlocks.Add(MapBlockFromGridRegion(region));
                        }
                    /*}
                    else
                    {
                    }*/

                    item.remoteClient.SendMapBlock(mapBlocks, 0);
                }
                m_blockitemsToRequest.Clear();
            }
            catch (Exception)
            {
            }
            blockRequesterIsRunning = false;
            //mapBlockCache = new MapBlockData[10000, 10000];
            return true;
        }
        
        protected MapBlockData MapBlockFromGridRegion(GridRegion r)
        {
            MapBlockData block = new MapBlockData();
            if (r == null)
            {
                block.Access = (byte)SimAccess.Down;
                block.MapImageId = UUID.Zero;
                return block;
            }
            block.Access = r.Access;
            block.MapImageId = r.TerrainImage;
            block.Name = r.RegionName;
            block.X = (ushort)(r.RegionLocX / Constants.RegionSize);
            block.Y = (ushort)(r.RegionLocY / Constants.RegionSize);
            return block;
        }

        private void OnMapNameRequest(IClientAPI remoteClient, string mapName)
        {
            if (mapName.Length < 1)
            {
                remoteClient.SendAlertMessage("Use a search string with at least 1 character");
                return;
            }

            List<MapBlockData> blocks = new List<MapBlockData>();

            List<GridRegion> regionInfos = m_scene.GridService.GetRegionsByName(UUID.Zero, mapName, 20);
            foreach (GridRegion region in regionInfos)
            {
                blocks.Add(SearchMapBlockFromGridRegion(region));
            }

            // final block, closing the search result
            MapBlockData data = new MapBlockData();
            data.Agents = 0;
            data.Access = 255;
            data.MapImageId = UUID.Zero;
            data.Name = mapName;
            data.RegionFlags = 0;
            data.WaterHeight = 0; // not used
            data.X = 0;
            data.Y = 0;
            blocks.Add(data);

            remoteClient.SendMapBlock(blocks, 2);
        }

        protected MapBlockData SearchMapBlockFromGridRegion(GridRegion r)
        {
            MapBlockData block = new MapBlockData();
            block.Access = r.Access;
            if ((r.Access & (byte)SimAccess.Down) == (byte)SimAccess.Down)
                block.Name = r.RegionName + " (offline)";
            else
                block.Name = r.RegionName;
            block.MapImageId = r.TerrainImage;
            block.Name = r.RegionName;
            block.X = (ushort)(r.RegionLocX / Constants.RegionSize);
            block.Y = (ushort)(r.RegionLocY / Constants.RegionSize);
            return block;
        }

        protected MapBlockData TerrainBlockFromGridRegion(GridRegion r)
        {
            MapBlockData block = new MapBlockData();
            if (r == null)
            {
                block.Access = (byte)SimAccess.Down;
                block.MapImageId = UUID.Zero;
                return block;
            }
            block.Access = r.Access;
            block.MapImageId = r.TerrainMapImage;
            block.Name = r.RegionName;
            block.X = (ushort)(r.RegionLocX / Constants.RegionSize);
            block.Y = (ushort)(r.RegionLocY / Constants.RegionSize);
            return block;
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
                AssetBase texAsset = m_scene.AssetService.Get(r.TerrainImage.ToString());

                if (texAsset != null)
                    textures.Add(texAsset);
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
            MemoryStream imgstream = new MemoryStream();
            Bitmap mapTexture = new Bitmap(1, 1);
            ManagedImage managedImage;
            Image image = (Image)mapTexture;

            try
            {
                // Taking our jpeg2000 data, decoding it, then saving it to a byte array with regular jpeg data

                imgstream = new MemoryStream();

                // Decode image to System.Drawing.Image
                if (OpenJPEG.DecodeToImage(data, out managedImage, out image))
                {
                    // Save to bitmap
                    mapTexture = new Bitmap(image);

                    EncoderParameters myEncoderParameters = new EncoderParameters();
                    myEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 95L);

                    // Save bitmap to stream
                    mapTexture.Save(imgstream, GetEncoderInfo("image/jpeg"), myEncoderParameters);

                    // Write the stream to a byte array for output
                    myMapImageJPEG = imgstream.ToArray();
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
            if (m_scene != null)
                m_scene.UpdateGridRegion();
        }

        private void OnTimedCreateNewMapImage(object source, ElapsedEventArgs e)
        {
            m_scene.CreateTerrainTexture();
        }
        
        private class MapItemRequester
        {
            public ulong regionhandle = 0;
            public uint itemtype = 0;
            public IClientAPI remoteClient = null;
            public uint flags = 0;
        }
	}
}
