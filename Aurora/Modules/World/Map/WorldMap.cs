/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
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
        //private static readonly UUID STOP_UUID = UUID.Random();

        //private IConfig m_config;
        protected IScene m_scene;
        private byte[] myMapImageJPEG;
        protected bool m_Enabled = false;
        private IConfigSource m_config;
        private ExpiringCache<ulong, List< mapItemReply>> m_mapItemCache = new ExpiringCache<ulong, List<mapItemReply>>();

        private Queue<MapItemRequester> m_itemsToRequest = new Queue<MapItemRequester> ();
        private bool itemRequesterIsRunning = false;
        private static AuroraThreadPool threadpool = null;
        private static AuroraThreadPool blockthreadpool = null;
        private double minutes = 60 * 24;
        private double oneminute = 60000;
        private System.Timers.Timer UpdateMapImage;
        private System.Timers.Timer UpdateOnlineStatus;
        private bool m_generateMapTiles = true;
        private UUID staticMapTileUUID = UUID.Zero;
        private bool m_asyncMapTileCreation = false;
        private int MapViewLength = 8;
        
		#region INonSharedRegionModule Members

        public virtual void Initialise(IConfigSource source)
		{
            if (source.Configs["MapModule"] != null)
            {
                if (source.Configs["MapModule"].GetString(
                        "WorldMapModule", "AuroraWorldMapModule") !=
                        "AuroraWorldMapModule")
                    return;
                m_Enabled = true;
                //m_log.Info("[AuroraWorldMap] Initializing");
                m_config = source;
                m_asyncMapTileCreation = source.Configs["MapModule"].GetBoolean("UseAsyncMapTileCreation", m_asyncMapTileCreation);
                minutes = source.Configs["MapModule"].GetDouble("TimeBeforeMapTileRegeneration", minutes);
                m_generateMapTiles = source.Configs["MapModule"].GetBoolean("GenerateMaptiles", true);
                UUID.TryParse(source.Configs["MapModule"].GetString("MaptileStaticUUID", UUID.Zero.ToString()), out staticMapTileUUID);
                MapViewLength = source.Configs["MapModule"].GetInt("MapViewLength", MapViewLength);
            }
		}

        public virtual void AddRegion (IScene scene)
		{
            if (!m_Enabled)
                return;

            lock (scene)
            {
                m_scene = scene;

                m_scene.RegisterModuleInterface<IWorldMapModule>(this);

                if (MainConsole.Instance != null)
                {
                    MainConsole.Instance.Commands.AddCommand ("update map",
                        "update map",
                        "Updates the image of the world map", HandleUpdateWorldMapConsoleCommand);

                    MainConsole.Instance.Commands.AddCommand (
                        "export-map",
                        "export-map [<path>]",
                        "Save an image of the world map", HandleExportWorldMapConsoleCommand);
                }

                AddHandlers();

                string name = scene.RegionInfo.RegionName;
                name = name.Replace(' ', '_');
                string regionMapTileUUID = m_config.Configs["MapModule"].GetString(name + "MaptileStaticUUID", "");
                if (regionMapTileUUID != "")
                {
                    //It exists, override the default
                    UUID.TryParse(regionMapTileUUID, out staticMapTileUUID);
                }
            }
		}

        public virtual void RemoveRegion (IScene scene)
		{
			if (!m_Enabled)
				return;

			lock (m_scene)
			{
				m_Enabled = false;
				RemoveHandlers();
				m_scene = null;
			}
            if (UpdateMapImage != null)
            {
                UpdateMapImage.Stop();
                UpdateMapImage.Elapsed -= OnTimedCreateNewMapImage;
                UpdateMapImage.Enabled = false;
                UpdateMapImage.Close();
            }

            if (UpdateOnlineStatus != null)
            {
                UpdateOnlineStatus.Stop();
                UpdateOnlineStatus.Elapsed -= OnUpdateRegion;
                UpdateOnlineStatus.Enabled = false;
                UpdateOnlineStatus.Close();
            }
		}

        public virtual void RegionLoaded (IScene scene)
		{
            if (!m_Enabled)
                return;

            AuroraThreadPoolStartInfo info = new AuroraThreadPoolStartInfo();
            info.priority = ThreadPriority.Lowest;
            info.Threads = 1;
            threadpool = new AuroraThreadPool(info);
            blockthreadpool = new AuroraThreadPool(info);

            scene.EventManager.OnStartupComplete += StartupComplete;
        }

        public void StartupComplete(IScene scene, List<string> data)
        {
            //Startup complete, we can generate a tile now
            CreateTerrainTexture();
            //and set up timers.
            SetUpTimers();
        }

        public void SetUpTimers()
        {
            if (m_generateMapTiles)
            {
                UpdateMapImage = new System.Timers.Timer(oneminute * minutes);
                UpdateMapImage.Elapsed += OnTimedCreateNewMapImage;
                UpdateMapImage.Enabled = true;
            }
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
            m_log.Debug("[WORLD MAP]: JPEG Map location: " + m_scene.RegionInfo.ServerURI + "/index.php?method=" + regionimage);

            MainServer.Instance.AddHTTPHandler(regionimage, OnHTTPGetMapImage);

            m_scene.EventManager.OnNewClient += OnNewClient;
            m_scene.EventManager.OnClosingClient += OnClosingClient;
		}

		// this has to be called with a lock on m_scene
		protected virtual void RemoveHandlers()
		{
            m_scene.EventManager.OnNewClient -= OnNewClient;
            m_scene.EventManager.OnClosingClient -= OnClosingClient;

            string regionimage = "regionImage" + m_scene.RegionInfo.RegionID.ToString();
            regionimage = regionimage.Replace("-", "");
            MainServer.Instance.RemoveHTTPHandler("", regionimage);
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

        private void OnClosingClient(IClientAPI client)
        {
            client.OnRequestMapBlocks -= RequestMapBlocks;
            client.OnMapItemRequest -= HandleMapItemRequest;
            client.OnMapNameRequest -= OnMapNameRequest;
        }

		#endregion

        public virtual void HandleMapItemRequest(IClientAPI remoteClient, uint flags,
            uint EstateID, bool godlike, uint itemtype, ulong regionhandle)
        {
            if (remoteClient.Scene.GetScenePresence (remoteClient.AgentId).IsChildAgent)
                return;//No child agent requests

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
                    IEntityCountModule entityCountModule = m_scene.RequestModuleInterface<IEntityCountModule>();
                    if (entityCountModule != null && entityCountModule.RootAgents <= 1)
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
                    m_scene.ForEachScenePresence(delegate(IScenePresence sp)
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
                        m_itemsToRequest.Enqueue(new MapItemRequester()
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
                        remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                    }
                }
            }
        }

        private void GetMapItems()
        {
            itemRequesterIsRunning = true;
            while(true)
            {
                MapItemRequester item = null;
                if(m_itemsToRequest.Count > 0)
                    item = m_itemsToRequest.Dequeue ();
                if (item == null)
                    break; //Nothing in the queue

                List<mapItemReply> mapitems = new List<mapItemReply>();
                if (!m_mapItemCache.TryGetValue(item.regionhandle, out mapitems)) //try again, might have gotten picked up by this already
                {
                    multipleMapItemReply allmapitems = m_scene.GridService.GetMapItems(item.regionhandle, (OpenMetaverse.GridItemType)item.itemtype);

                    if (allmapitems == null)
                        continue;
                    //Send out the update
                    if (allmapitems.items.ContainsKey(item.regionhandle))
                    {
                        mapitems = allmapitems.items[item.regionhandle];

                        //Update the cache
                        foreach (KeyValuePair<ulong, List<mapItemReply>> kvp in allmapitems.items)
                        {
                            m_mapItemCache.AddOrUpdate(kvp.Key, kvp.Value, 3 * 60); //5 mins
                        }
                    }
                }

                if(mapitems != null)
                    item.remoteClient.SendMapItemReply (mapitems.ToArray (), item.itemtype, item.flags);
                Thread.Sleep (5);
            }
            itemRequesterIsRunning = false;
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
            m_blockitemsToRequest.Enqueue (new MapBlockRequester ()
            {
                maxX = maxX,
                maxY = maxY,
                minX = minX,
                minY = minY,
                mapBlocks = (uint)(flag & ~0x10000),
                remoteClient = remoteClient
            });
            if (!blockRequesterIsRunning)
                blockthreadpool.QueueEvent(GetMapBlocks, 3);
        }

        protected virtual void GetAndSendMapBlocks(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flag)
        {
            m_blockitemsToRequest.Enqueue(new MapBlockRequester()
            {
                maxX = maxX,
                maxY = maxY,
                minX = minX,
                minY = minY,
                mapBlocks = 0,//Map
                remoteClient = remoteClient
            });
            if (!blockRequesterIsRunning)
                blockthreadpool.QueueEvent(GetMapBlocks, 3);
        }

        protected virtual void GetAndSendTerrainBlocks(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flag)
        {
            m_blockitemsToRequest.Enqueue (new MapBlockRequester ()
            {
                maxX = maxX,
                maxY = maxY,
                minX = minX,
                minY = minY,
                mapBlocks = 1,//Terrain
                remoteClient = remoteClient
            });
            if (!blockRequesterIsRunning)
                blockthreadpool.QueueEvent(GetMapBlocks, 3);
        }

        private bool blockRequesterIsRunning = false;
        private Queue<MapBlockRequester> m_blockitemsToRequest = new Queue<MapBlockRequester> ();

        private class MapBlockRequester
        {
            public int minX;
            public int minY;
            public int maxX;
            public int maxY;
            public uint mapBlocks;
            public IClientAPI remoteClient;
        }

        private void GetMapBlocks()
        {
            try
            {
                blockRequesterIsRunning = true;
                while(true)
                {
                    MapBlockRequester item = null;
                    if(m_blockitemsToRequest.Count > 0)
                        item = m_blockitemsToRequest.Dequeue();
                    if (item == null)
                        break;
                    List<MapBlockData> mapBlocks = new List<MapBlockData>();

                    List<GridRegion> regions = m_scene.GridService.GetRegionRange(m_scene.RegionInfo.ScopeID,
                            (item.minX - 4) * (int)Constants.RegionSize,
                            (item.maxX + 4) * (int)Constants.RegionSize,
                            (item.minY - 4) * (int)Constants.RegionSize,
                            (item.maxY + 4) * (int)Constants.RegionSize);

                    foreach (GridRegion region in regions)
                    {
                        if ((item.mapBlocks & 0) == 0 || (item.mapBlocks & 0x10000) != 0)
                            mapBlocks.Add(MapBlockFromGridRegion(region));
                        else if ((item.mapBlocks & 1) == 1)
                            mapBlocks.Add(TerrainBlockFromGridRegion(region));
                        else if ((item.mapBlocks & 2) == 2) //V2 viewer, we need to deal with it a bit
                            mapBlocks.AddRange (Map2BlockFromGridRegion (region));
                    }

                    item.remoteClient.SendMapBlock(mapBlocks, item.mapBlocks);
                    Thread.Sleep (5);
                }
            }
            catch (Exception)
            {
            }
            blockRequesterIsRunning = false;
        }
        
        protected MapBlockData MapBlockFromGridRegion(GridRegion r)
        {
            MapBlockData block = new MapBlockData();
            if (r == null)
            {
                block.Access = (byte)SimAccess.Down;
                block.MapImageID = UUID.Zero;
                return block;
            }
            block.Access = r.Access;
            block.MapImageID = r.TerrainImage;
            if ((r.Access & (byte)SimAccess.Down) == (byte)SimAccess.Down)
                block.Name = r.RegionName + " (offline)";
            else
                block.Name = r.RegionName;
            block.X = (ushort)(r.RegionLocX / Constants.RegionSize);
            block.Y = (ushort)(r.RegionLocY / Constants.RegionSize);
            block.SizeX = (ushort)(int)r.RegionSizeX;
            block.SizeY = (ushort)(int)r.RegionSizeY;

            return block;
        }

        protected List<MapBlockData> Map2BlockFromGridRegion (GridRegion r)
        {
            List<MapBlockData> blocks = new List<MapBlockData> ();
            MapBlockData block = new MapBlockData ();
            if (r == null)
            {
                block.Access = (byte)SimAccess.Down;
                block.MapImageID = UUID.Zero;
                blocks.Add (block);
                return blocks;
            }
            block.Access = r.Access;
            block.MapImageID = r.TerrainImage;
            if ((r.Access & (byte)SimAccess.Down) == (byte)SimAccess.Down)
                block.Name = r.RegionName + " (offline)";
            else
                block.Name = r.RegionName;
            block.X = (ushort)(r.RegionLocX / Constants.RegionSize);
            block.Y = (ushort)(r.RegionLocY / Constants.RegionSize);
            block.SizeX = (ushort)r.RegionSizeX;
            block.SizeY = (ushort)r.RegionSizeY;
            blocks.Add(block);
            if (r.RegionSizeX > Constants.RegionSize || r.RegionSizeY > Constants.RegionSize)
            {
                for (int x = 0; x < r.RegionSizeX / Constants.RegionSize; x++)
                {
                    for (int y = 0; y < r.RegionSizeY / Constants.RegionSize; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;
                         block = new MapBlockData ();
                        block.Access = r.Access;
                        block.MapImageID = r.TerrainImage;
                        block.Name = r.RegionName; //Child piece, so ignore it
                        block.X = (ushort)((r.RegionLocX / Constants.RegionSize) + x);
                        block.Y = (ushort)((r.RegionLocY / Constants.RegionSize) + y);
                        block.SizeX = (ushort)r.RegionSizeX;
                        block.SizeY = (ushort)r.RegionSizeY;
                        blocks.Add (block);
                    }
                }
            }
            return blocks;
        }

        private void OnMapNameRequest (IClientAPI remoteClient, string mapName, uint flags)
        {
            if (mapName.Length < 1)
            {
                remoteClient.SendAlertMessage("Use a search string with at least 1 character");
                return;
            }

            bool TryCoordsSearch = false;
            int XCoord = 0;
            int YCoord = 0;

            string[] splitSearch = mapName.Split(',');
            if (splitSearch.Length != 1)
            {
                if (splitSearch[1].StartsWith (" "))
                    splitSearch[1] = splitSearch[1].Remove (0, 1);
                if (int.TryParse(splitSearch[0], out XCoord) && int.TryParse(splitSearch[1], out YCoord))
                    TryCoordsSearch = true;
            }

            List<MapBlockData> blocks = new List<MapBlockData>();

            List<GridRegion> regionInfos = m_scene.GridService.GetRegionsByName(UUID.Zero, mapName, 20);
            if (TryCoordsSearch)
            {
                GridRegion region = m_scene.GridService.GetRegionByPosition(m_scene.RegionInfo.ScopeID, (int)(XCoord * Constants.RegionSize), (int)(YCoord * Constants.RegionSize));
                if (region != null)
                {
                    region.RegionName = mapName + " - " + region.RegionName;
                    regionInfos.Add (region);
                }
            }
            List<GridRegion> allRegions = new List<GridRegion> ();
            foreach (GridRegion region in regionInfos)
            {
                //Add the found in search region first
                if (!allRegions.Contains (region))
                {
                    allRegions.Add (region);
                    blocks.Add (SearchMapBlockFromGridRegion (region));
                }
                //Then send surrounding regions
                List<GridRegion> regions = m_scene.GridService.GetRegionRange (m_scene.RegionInfo.ScopeID,
                    (region.RegionLocX - (4 * (int)Constants.RegionSize)),
                    (region.RegionLocX + (4 * (int)Constants.RegionSize)),
                    (region.RegionLocY - (4 * (int)Constants.RegionSize)),
                    (region.RegionLocY + (4 * (int)Constants.RegionSize)));
                if (regions != null)
                {
                    foreach (GridRegion r in regions)
                    {
                        if (!allRegions.Contains (region))
                        {
                            allRegions.Add (region);
                            blocks.Add (SearchMapBlockFromGridRegion (r));
                        }
                    }
                }
            }

            // final block, closing the search result
            MapBlockData data = new MapBlockData();
            data.Agents = 0;
            data.Access = 255;
            data.MapImageID = UUID.Zero;
            data.Name = mapName;
            data.RegionFlags = 0;
            data.WaterHeight = 0; // not used
            data.X = 0;
            data.Y = 0;
            data.SizeX = 256;
            data.SizeY = 256;
            blocks.Add(data);

            remoteClient.SendMapBlock (blocks, flags);
        }

        protected MapBlockData SearchMapBlockFromGridRegion(GridRegion r)
        {
            MapBlockData block = new MapBlockData ();
            if (r == null)
            {
                block.Access = (byte)SimAccess.Down;
                block.MapImageID = UUID.Zero;
                return block;
            }
            block.Access = r.Access;
            if ((r.Access & (byte)SimAccess.Down) == (byte)SimAccess.Down)
                block.Name = r.RegionName + " (offline)";
            else
                block.Name = r.RegionName;
            block.MapImageID = r.TerrainImage;
            block.Name = r.RegionName;
            block.X = (ushort)(r.RegionLocX / Constants.RegionSize);
            block.Y = (ushort)(r.RegionLocY / Constants.RegionSize);
            block.SizeX = (ushort)(int)r.RegionSizeX;
            block.SizeY = (ushort)(int)r.RegionSizeY;
            return block;
        }

        protected MapBlockData TerrainBlockFromGridRegion(GridRegion r)
        {
            MapBlockData block = new MapBlockData();
            if (r == null)
            {
                block.Access = (byte)SimAccess.Down;
                block.MapImageID = UUID.Zero;
                return block;
            }
            block.Access = r.Access;
            block.MapImageID = r.TerrainMapImage;
            if ((r.Access & (byte)SimAccess.Down) == (byte)SimAccess.Down)
                block.Name = r.RegionName + " (offline)";
            else
                block.Name = r.RegionName;
            block.X = (ushort)(r.RegionLocX / Constants.RegionSize);
            block.Y = (ushort)(r.RegionLocY / Constants.RegionSize);
            block.SizeX = (ushort)(int)r.RegionSizeX;
            block.SizeY = (ushort)(int)r.RegionSizeY;
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
                Image image = (Image)mapTexture;

                try
                {
                    // Taking our jpeg2000 data, decoding it, then saving it to a byte array with regular jpeg data

                    imgstream = new MemoryStream();

                    // non-async because we know we have the asset immediately.
                    AssetBase mapasset = m_scene.AssetService.Get(m_scene.RegionInfo.RegionSettings.TerrainImageID.ToString());
                    if (mapasset != null)
                    {
                        image = m_scene.RequestModuleInterface<IJ2KDecoder> ().DecodeToImage (mapasset.Data);
                        // Decode image to System.Drawing.Image
                        if (image != null)
                        {
                            // Save to bitmap
                            mapTexture = new Bitmap (image);

                            EncoderParameters myEncoderParameters = new EncoderParameters ();
                            myEncoderParameters.Param[0] = new EncoderParameter (Encoder.Quality, 95L);

                            // Save bitmap to stream
                            mapTexture.Save (imgstream, GetEncoderInfo ("image/jpeg"), myEncoderParameters);

                            // Write the stream to a byte array for output
                            jpeg = imgstream.ToArray ();
                            myMapImageJPEG = jpeg;
                        }
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
        public void HandleUpdateWorldMapConsoleCommand(string[] cmdparams)
        {
            if (MainConsole.Instance.ConsoleScene != null && m_scene != MainConsole.Instance.ConsoleScene)
                return;
            CreateTerrainTexture();
        }

        /// <summary>
        /// Export the world map
        /// </summary>
        /// <param name="fileName"></param>
        public void HandleExportWorldMapConsoleCommand(string[] cmdparams)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;

            string exportPath;

            if (cmdparams.Length > 1)
                exportPath = cmdparams[1];
            else
                exportPath = DEFAULT_WORLD_MAP_EXPORT_PATH;

            m_log.InfoFormat(
                "[WORLD MAP]: Exporting world map for {0} to {1}", m_scene.RegionInfo.RegionName, exportPath);

            List<GridRegion> regions = m_scene.GridService.GetRegionRange(m_scene.RegionInfo.ScopeID,
                    (int)(m_scene.RegionInfo.RegionLocX - (9 * (int)Constants.RegionSize)),
                    (int)(m_scene.RegionInfo.RegionLocX + (9 * (int)Constants.RegionSize)),
                    (int)(m_scene.RegionInfo.RegionLocY - (9 * (int)Constants.RegionSize)),
                    (int)(m_scene.RegionInfo.RegionLocY + (9 * (int)Constants.RegionSize)));
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
                Image image;

                if ((image = m_scene.RequestModuleInterface<IJ2KDecoder> ().DecodeToImage(asset.Data)) != null)
                    bitImages.Add(image);
            }

            Bitmap mapTexture = new Bitmap(2560, 2560);
            Graphics g = Graphics.FromImage(mapTexture);
            SolidBrush sea = new SolidBrush(Color.DarkBlue);
            g.FillRectangle(sea, 0, 0, 2560, 2560);

            for (int i = 0; i < regions.Count; i++)
            {
                ushort x = (ushort)((regions[i].RegionLocX - m_scene.RegionInfo.RegionLocX) + 10);
                ushort y = (ushort)((regions[i].RegionLocY - m_scene.RegionInfo.RegionLocY) + 10);
                g.DrawImage(bitImages[i], (x * 128), 2560 - (y * 128), 128, 128); // y origin is top
            }

            mapTexture.Save(exportPath, ImageFormat.Jpeg);

            m_log.InfoFormat(
                "[WORLD MAP]: Successfully exported world map for {0} to {1}",
                m_scene.RegionInfo.RegionName, exportPath);
        }

        private void OnUpdateRegion(object source, ElapsedEventArgs e)
        {
            if (m_scene != null)
            {
                IGridRegisterModule gridRegModule = m_scene.RequestModuleInterface<IGridRegisterModule>();
                if (gridRegModule != null)
                    gridRegModule.UpdateGridRegion(m_scene);
            }
        }

        private void OnTimedCreateNewMapImage(object source, ElapsedEventArgs e)
        {
            CreateTerrainTexture();
        }
        
        private class MapItemRequester
        {
            public ulong regionhandle = 0;
            public uint itemtype = 0;
            public IClientAPI remoteClient = null;
            public uint flags = 0;
        }

        /// <summary>
        /// Create a terrain texture for this scene
        /// </summary>
        public void CreateTerrainTexture()
        {
            if (!m_generateMapTiles)
            {
                //They want a static texture, lock it in.
                m_scene.RegionInfo.RegionSettings.TerrainMapImageID = staticMapTileUUID;
                m_scene.RegionInfo.RegionSettings.TerrainImageID = staticMapTileUUID;
                return;
            }

            // Cannot create a map for a nonexistant heightmap.
            ITerrainChannel heightmap = m_scene.RequestModuleInterface<ITerrainChannel>();
            if (heightmap == null)
                return;

            //m_log.Debug("[MAPTILE]: STORING MAPTILE IMAGE");

            //Delete the old assets and make sure the UUIDs exist
            bool changed = false;
            if (m_scene.RegionInfo.RegionSettings.TerrainImageID == UUID.Zero)
            {
                m_scene.RegionInfo.RegionSettings.TerrainImageID = UUID.Random ();
                changed = true;
            }

            if (m_scene.RegionInfo.RegionSettings.TerrainMapImageID == UUID.Zero)
            {
                m_scene.RegionInfo.RegionSettings.TerrainMapImageID = UUID.Random ();
                changed = true;
            }

            AssetBase Mapasset = new AssetBase(
                m_scene.RegionInfo.RegionSettings.TerrainImageID,
                "terrainImage_" + m_scene.RegionInfo.RegionID.ToString(),
                AssetType.Simstate,
                m_scene.RegionInfo.RegionID);
            Mapasset.Description = m_scene.RegionInfo.RegionName;
            Mapasset.Flags = AssetFlags.Deletable;

            AssetBase Terrainasset = new AssetBase(
                m_scene.RegionInfo.RegionSettings.TerrainMapImageID,
                "terrainMapImage_" + m_scene.RegionInfo.RegionID.ToString(),
                AssetType.Simstate,
                m_scene.RegionInfo.RegionID);
            Terrainasset.Description = m_scene.RegionInfo.RegionName;
            Terrainasset.Flags = AssetFlags.Deletable;

            if(changed)
                m_scene.RegionInfo.RegionSettings.Save();

            if (!m_asyncMapTileCreation)
            {
                CreateMapTileAsync(Mapasset, Terrainasset);
            }
            else
            {
                CreateMapTile d = CreateMapTileAsync;
                d.BeginInvoke(Mapasset, Terrainasset, CreateMapTileAsyncCompleted, d);
            }
            Mapasset = null;
            Terrainasset = null;
        }

        #region Async map tile

        protected void CreateMapTileAsyncCompleted(IAsyncResult iar)
        {
            CreateMapTile icon = (CreateMapTile)iar.AsyncState;
            icon.EndInvoke(iar);
        }

        public delegate void CreateMapTile(AssetBase Mapasset, AssetBase Terrainasset);

        #endregion

        #region Generate map tile

        public void CreateMapTileAsync(AssetBase Mapasset, AssetBase Terrainasset)
        {
            IMapImageGenerator terrain = m_scene.RequestModuleInterface<IMapImageGenerator>();

            if (terrain == null)
                return;

            //Delete the old assets
            if (Terrainasset.ID != UUID.Zero)
                m_scene.AssetService.Delete(Terrainasset.ID);
            if (Mapasset.ID != UUID.Zero)
                m_scene.AssetService.Delete(Mapasset.ID);

            byte[] terraindata, mapdata;
            terrain.CreateMapTile(out terraindata, out mapdata);
            if (terraindata != null)
            {
                Terrainasset.Data = terraindata;
                Terrainasset.ID = m_scene.AssetService.Store(Terrainasset);
            }

            if (mapdata != null)
            {
                Mapasset.Data = mapdata;
                Mapasset.ID = m_scene.AssetService.Store(Mapasset);
            }

            //Update the grid map
            IGridRegisterModule gridRegModule = m_scene.RequestModuleInterface<IGridRegisterModule>();
            if(gridRegModule != null)
                gridRegModule.UpdateGridRegion(m_scene);
        }

        public void RegenerateMaptile(string ID, byte[] data)
        {
            MemoryStream imgstream = new MemoryStream();
            Bitmap mapTexture = new Bitmap(1, 1);
            Image image = (Image)mapTexture;

            try
            {
                // Taking our jpeg2000 data, decoding it, then saving it to a byte array with regular jpeg data

                imgstream = new MemoryStream();

                image = m_scene.RequestModuleInterface<IJ2KDecoder> ().DecodeToImage (data);
                // Decode image to System.Drawing.Image
                if (image != null)
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
                }
            }
        }

        #endregion
    }
}
