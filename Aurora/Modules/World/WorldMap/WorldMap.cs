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
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules.WorldMap
{
    public class AuroraWorldMapModule : INonSharedRegionModule, IWorldMapModule
	{
        private const string DEFAULT_WORLD_MAP_EXPORT_PATH = "exportmap.jpg";

        protected IScene m_scene;
        protected bool m_Enabled;
        private readonly ExpiringCache<ulong, List< mapItemReply>> m_mapItemCache = new ExpiringCache<ulong, List<mapItemReply>>();

        private readonly ConcurrentQueue<MapItemRequester> m_itemsToRequest = new ConcurrentQueue<MapItemRequester>();
        private bool itemRequesterIsRunning;
        private static AuroraThreadPool threadpool;
        private static AuroraThreadPool blockthreadpool;
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
                    MainConsole.Instance.Commands.AddCommand (
                        "export-map",
                        "export-map [<path>]",
                        "Save an image of the world map", HandleExportWorldMapConsoleCommand);
                }

                AddHandlers();
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
		}

        public virtual void RegionLoaded (IScene scene)
		{
            if (!m_Enabled)
                return;

            AuroraThreadPoolStartInfo info = new AuroraThreadPoolStartInfo
                                                 {priority = ThreadPriority.Lowest, Threads = 1};
            threadpool = new AuroraThreadPool(info);
            blockthreadpool = new AuroraThreadPool(info);
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
            MainConsole.Instance.Debug("[WORLD MAP]: JPEG Map location: " + MainServer.Instance.ServerURI + "/index.php?method=" + regionimage);

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

            uint xstart;
            uint ystart;
            Utils.LongToUInts(m_scene.RegionInfo.RegionHandle, out xstart, out ystart);
            
            List<mapItemReply> mapitems = new List<mapItemReply>();
            int tc = Environment.TickCount;
            if (itemtype == (int)GridItemType.AgentLocations)
            {
                //If its local, just let it do it on its own.
                if (regionhandle == 0 || regionhandle == m_scene.RegionInfo.RegionHandle)
                {
                    //Only one person here, send a zero person response
                    mapItemReply mapitem;
                    IEntityCountModule entityCountModule = m_scene.RequestModuleInterface<IEntityCountModule>();
                    if (entityCountModule != null && entityCountModule.RootAgents <= 1)
                    {
                        mapitem = new mapItemReply
                                      {
                                          x = xstart + 1,
                                          y = ystart + 1,
                                          id = UUID.Zero,
                                          name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString()),
                                          Extra = 0,
                                          Extra2 = 0
                                      };
                        mapitems.Add(mapitem);
                        remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                        return;
                    }
                    m_scene.ForEachScenePresence(delegate(IScenePresence sp)
                    {
                        // Don't send a green dot for yourself
                        if (!sp.IsChildAgent && sp.UUID != remoteClient.AgentId)
                        {
                            mapitem = new mapItemReply
                                          {
                                              x = (uint) (xstart + sp.AbsolutePosition.X),
                                              y = (uint) (ystart + sp.AbsolutePosition.Y),
                                              id = UUID.Zero,
                                              name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString()),
                                              Extra = 1,
                                              Extra2 = 0
                                          };
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
                        m_itemsToRequest.Enqueue(new MapItemRequester
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
                if(!m_itemsToRequest.TryDequeue(out item))
                    break; //Nothing in the queue

                List<mapItemReply> mapitems;
                if (!m_mapItemCache.TryGetValue(item.regionhandle, out mapitems)) //try again, might have gotten picked up by this already
                {
                    multipleMapItemReply allmapitems = m_scene.GridService.GetMapItems(item.remoteClient.AllScopeIDs, 
                        item.regionhandle, (GridItemType)item.itemtype);

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
        /// <param name="remoteClient"></param>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="flag"></param>
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
                    MainConsole.Instance.Warn("[World Map] : Got new flag, " + flag + " RequestMapBlocks()");
            }
		}

        protected virtual void ClickedOnTile(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flag)
        {
            m_blockitemsToRequest.Enqueue (new MapBlockRequester
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
            m_blockitemsToRequest.Enqueue(new MapBlockRequester
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
            m_blockitemsToRequest.Enqueue (new MapBlockRequester
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

        private bool blockRequesterIsRunning;
        private readonly ConcurrentQueue<MapBlockRequester> m_blockitemsToRequest = new ConcurrentQueue<MapBlockRequester>();

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
                    if(!m_blockitemsToRequest.TryDequeue(out item))
                        break;
                    List<MapBlockData> mapBlocks = new List<MapBlockData>();

                    List<GridRegion> regions = m_scene.GridService.GetRegionRange(item.remoteClient.AllScopeIDs,
                            (item.minX - 4) * Constants.RegionSize,
                            (item.maxX + 4) * Constants.RegionSize,
                            (item.minY - 4) * Constants.RegionSize,
                            (item.maxY + 4) * Constants.RegionSize);

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
            block.SizeX = (ushort)r.RegionSizeX;
            block.SizeY = (ushort)r.RegionSizeY;

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
                        block = new MapBlockData
                                    {
                                        Access = r.Access,
                                        MapImageID = r.TerrainImage,
                                        Name = r.RegionName,
                                        X = (ushort) ((r.RegionLocX/Constants.RegionSize) + x),
                                        Y = (ushort) ((r.RegionLocY/Constants.RegionSize) + y),
                                        SizeX = (ushort) r.RegionSizeX,
                                        SizeY = (ushort) r.RegionSizeY
                                    };
                        //Child piece, so ignore it
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

            List<GridRegion> regionInfos = m_scene.GridService.GetRegionsByName(remoteClient.AllScopeIDs, mapName, 0, 20);
            if (TryCoordsSearch)
            {
                GridRegion region = m_scene.GridService.GetRegionByPosition(remoteClient.AllScopeIDs, XCoord * Constants.RegionSize, YCoord * Constants.RegionSize);
                if (region != null)
                {
                    region.RegionName = mapName + " - " + region.RegionName;
                    regionInfos.Add (region);
                }
            }
            List<GridRegion> allRegions = new List<GridRegion> ();
            if (regionInfos != null)
            {
                foreach (GridRegion region in regionInfos)
                {
                    //Add the found in search region first
                    if (!allRegions.Contains(region))
                    {
                        allRegions.Add(region);
                        blocks.Add(SearchMapBlockFromGridRegion(region));
                    }
                    //Then send surrounding regions
                    List<GridRegion> regions = m_scene.GridService.GetRegionRange(remoteClient.AllScopeIDs,
                        (region.RegionLocX - (4 * Constants.RegionSize)),
                        (region.RegionLocX + (4 * Constants.RegionSize)),
                        (region.RegionLocY - (4 * Constants.RegionSize)),
                        (region.RegionLocY + (4 * Constants.RegionSize)));
                    if (regions != null)
                    {
                        foreach (GridRegion r in regions)
                        {
                            if (!allRegions.Contains(region))
                            {
                                allRegions.Add(region);
                                blocks.Add(SearchMapBlockFromGridRegion(r));
                            }
                        }
                    }
                }
            }

            // final block, closing the search result
            MapBlockData data = new MapBlockData
                                    {
                                        Agents = 0,
                                        Access = 255,
                                        MapImageID = UUID.Zero,
                                        Name = mapName,
                                        RegionFlags = 0,
                                        WaterHeight = 0,
                                        X = 0,
                                        Y = 0,
                                        SizeX = 256,
                                        SizeY = 256
                                    };
            // not used
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
            block.SizeX = (ushort)r.RegionSizeX;
            block.SizeY = (ushort)r.RegionSizeY;
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
            block.SizeX = (ushort)r.RegionSizeX;
            block.SizeY = (ushort)r.RegionSizeY;
            return block;
        }

        public Hashtable OnHTTPGetMapImage(Hashtable keysvals)
        {
            Hashtable reply = new Hashtable();
            string regionImage = "regionImage" + m_scene.RegionInfo.RegionID.ToString();
            regionImage = regionImage.Replace("-", "");
            if (keysvals["method"].ToString() != regionImage)
                return reply;
            MainConsole.Instance.Debug("[WORLD MAP]: Sending map image jpeg");
            const int statuscode = 200;
            byte[] jpeg = new byte[0];

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
                    image = m_scene.RequestModuleInterface<IJ2KDecoder>().DecodeToImage(mapasset.Data);
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
                        jpeg = imgstream.ToArray();
                    }
                }
            }
            catch (Exception)
            {
                // Dummy!
                MainConsole.Instance.Warn("[WORLD MAP]: Unable to generate Map image");
            }
            finally
            {
                // Reclaim memory, these are unmanaged resources
                // If we encountered an exception, one or more of these will be null
                mapTexture.Dispose();

                if (image != null)
                    image.Dispose();

                imgstream.Close();
                imgstream.Dispose();
            }

            reply["str_response_string"] = Convert.ToBase64String(jpeg);
            reply["int_response_code"] = statuscode;
            reply["content_type"] = "image/jpeg";

            return reply;
        }

        // From msdn
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();
#if (!ISWIN)
            foreach (ImageCodecInfo t in encoders)
            {
                if (t.MimeType == mimeType) return t;
            }
            return null;
#else
            return encoders.FirstOrDefault(t => t.MimeType == mimeType);
#endif
        }

        /// <summary>
        /// Export the world map
        /// </summary>
        /// <param name="cmdparams"></param>
        public void HandleExportWorldMapConsoleCommand(string[] cmdparams)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
            {
                if (MainConsole.Instance.ConsoleScene == null && !MainConsole.Instance.HasProcessedCurrentCommand)
                    MainConsole.Instance.HasProcessedCurrentCommand = true;
                else
                    return;
            }

            string exportPath = cmdparams.Length > 1 ? cmdparams[1] : DEFAULT_WORLD_MAP_EXPORT_PATH;

            MainConsole.Instance.InfoFormat(
                "[WORLD MAP]: Exporting world map for {0} to {1}", m_scene.RegionInfo.RegionName, exportPath);

            List<GridRegion> regions = m_scene.GridService.GetRegionRange(null,
                    m_scene.RegionInfo.RegionLocX - (9 * Constants.RegionSize),
                    m_scene.RegionInfo.RegionLocX + (9 * Constants.RegionSize),
                    m_scene.RegionInfo.RegionLocY - (9 * Constants.RegionSize),
                    m_scene.RegionInfo.RegionLocY + (9 * Constants.RegionSize));
            List<Image> bitImages = new List<Image>();

#if (!ISWIN)
            List<AssetBase> textures = new List<AssetBase>();
            foreach (GridRegion r in regions)
            {
                AssetBase texAsset = m_scene.AssetService.Get(r.TerrainImage.ToString());
                if (texAsset != null) textures.Add(texAsset);
            }
#else
            List<AssetBase> textures = regions.Select(r => m_scene.AssetService.Get(r.TerrainImage.ToString())).Where(texAsset => texAsset != null).ToList();
#endif

            foreach (AssetBase asset in textures)
            {
                Image image;

                if ((image = m_scene.RequestModuleInterface<IJ2KDecoder> ().DecodeToImage(asset.Data)) != null)
                    bitImages.Add(image);
            }

            const int size = 2560;
            const int offsetSize = size / 10 / 2;
            Bitmap mapTexture = new Bitmap(size, size);
            Graphics g = Graphics.FromImage(mapTexture);
            SolidBrush sea = new SolidBrush(Color.DarkBlue);
            g.FillRectangle(sea, 0, 0, size, size);

            int regionXOffset = (m_scene.RegionInfo.RegionSizeX / 2 - 128) * -1;//Neg because the image is upside down
            const int regionYOffset = 0; // (m_scene.RegionInfo.RegionSizeY / 2 - 128) * -1;

            for (int i = 0; i < regions.Count; i++)
            {
                int regionSizeOffset = regions[i].RegionSizeX / 2 - 128;
                int x = ((regions[i].RegionLocX - m_scene.RegionInfo.RegionLocX) / Constants.RegionSize) + 10;
                int y = ((regions[i].RegionLocY - m_scene.RegionInfo.RegionLocY) / Constants.RegionSize) + 10;
                if(i < bitImages.Count)
                    g.DrawImage(bitImages[i], (x * offsetSize) + regionXOffset, size - (y * offsetSize + regionSizeOffset) + regionYOffset, regions[i].RegionSizeX / 2, regions[i].RegionSizeY / 2); // y origin is top
            }

            mapTexture.Save(exportPath, ImageFormat.Jpeg);

            MainConsole.Instance.InfoFormat(
                "[WORLD MAP]: Successfully exported world map for {0} to {1}",
                m_scene.RegionInfo.RegionName, exportPath);
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
