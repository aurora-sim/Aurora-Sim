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

using Aurora.Framework;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Servers.HttpServer.Interfaces;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using GridRegion = Aurora.Framework.Services.GridRegion;

namespace Aurora.Services
{
    public class MapService : IService, IMapService
    {
        private uint m_port = 8005;
        private IHttpServer m_server;
        private IRegistryCore m_registry;
        private bool m_enabled = false;
        private bool m_cacheEnabled = true;
        private float m_cacheExpires = 24;
        private IAssetService m_assetService;
        private IGridService m_gridService;
        private static Bitmap m_blankRegionTile = null;
        private MapTileIndex m_blankTiles = new MapTileIndex();
        private byte[] m_blankRegionTileData;

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig mapConfig = config.Configs["MapService"];
            if (mapConfig != null)
            {
                m_enabled = mapConfig.GetBoolean("Enabled", m_enabled);
                m_port = mapConfig.GetUInt("Port", m_port);
                m_cacheEnabled = mapConfig.GetBoolean("CacheEnabled", m_cacheEnabled);
                m_cacheExpires = mapConfig.GetFloat("CacheExpires", m_cacheExpires);
            }
            if (!m_enabled)
                return;

            if (m_cacheEnabled)
                CreateCacheDirectories();

            m_server = registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(m_port);
            m_server.AddHTTPHandler(new GenericStreamHandler("GET", "/MapService/", MapRequest));
            m_server.AddHTTPHandler(new GenericStreamHandler("GET", "/MapAPI/", MapAPIRequest));

            registry.RegisterModuleInterface<IMapService>(this);

            m_blankRegionTile = new Bitmap(256, 256);
            m_blankRegionTile.Tag = "StaticBlank";
            using (Graphics g = Graphics.FromImage(m_blankRegionTile))
            {
                SolidBrush sea = new SolidBrush(Color.FromArgb(29, 71, 95));
                g.FillRectangle(sea, 0, 0, 256, 256);
            }
            m_blankRegionTileData = CacheMapTexture(1, 0, 0, m_blankRegionTile, true);
            /*string path = Path.Combine("assetcache", Path.Combine("mapzoomlevels", "blankMap.index"));
            if(File.Exists(path))
            {
                FileStream stream = File.OpenRead(path);
                m_blankTiles = ProtoBuf.Serializer.Deserialize<MapTileIndex>(stream);
                stream.Close();
            }*/
        }

        private void CreateCacheDirectories()
        {
            if (!Directory.Exists("assetcache"))
                Directory.CreateDirectory("assetcache");
            if (!Directory.Exists("assetcache/mapzoomlevels"))
                Directory.CreateDirectory("assetcache/mapzoomlevels");
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (!m_enabled) return;
            m_assetService = m_registry.RequestModuleInterface<IAssetService>();
            m_gridService = m_registry.RequestModuleInterface<IGridService>();
        }

        public void FinishedStartup()
        {
            if (!m_enabled) return;
            IGridServerInfoService serverInfo = m_registry.RequestModuleInterface<IGridServerInfoService>();
            if (serverInfo != null)
            {
                serverInfo.AddURI("MapService", MapServiceURL);
                serverInfo.AddURI("MapAPIService", MapServiceAPIURL);
            }
        }

        public string MapServiceURL
        {
            get { return m_server.ServerURI + "/MapService/"; }
        }

        public string MapServiceAPIURL
        {
            get { return m_server.ServerURI + "/MapAPI/"; }
        }

        public byte[] MapAPIRequest(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            byte[] response = MainServer.BlankResponse;

            string var = httpRequest.Query["var"].ToString();
            if (path == "/MapAPI/get-region-coords-by-name")
            {
                string resp = "var {0} = {\"x\":{1},\"y\":{2}};";
                string sim_name = httpRequest.Query["sim_name"].ToString();
                var region = m_registry.RequestModuleInterface<IGridService>().GetRegionByName(null, sim_name);
                if (region == null)
                    resp = "var " + var + " = {error: true};";
                else
                    resp = "var " + var + " = {\"x\":" + region.RegionLocX + ",\"y\":" + region.RegionLocY + "};";
                response = System.Text.Encoding.UTF8.GetBytes(resp);
                httpResponse.ContentType = "text/javascript";
            }
            else if (path == "/MapAPI/get-region-name-by-coords")
            {
                string resp = "var {0} = \"{1}\";";
                int grid_x = int.Parse(httpRequest.Query["grid_x"].ToString());
                int grid_y = int.Parse(httpRequest.Query["grid_y"].ToString());
                var region = m_registry.RequestModuleInterface<IGridService>().GetRegionByPosition(null,
                                                                                                   grid_x*
                                                                                                   Constants.RegionSize,
                                                                                                   grid_y*
                                                                                                   Constants.RegionSize);
                if (region == null)
                {
                    List<GridRegion> regions = m_gridService.GetRegionRange(null,
                                                                            (grid_x*Constants.RegionSize) -
                                                                            (m_gridService.GetMaxRegionSize()),
                                                                            (grid_x*Constants.RegionSize),
                                                                            (grid_y*Constants.RegionSize) -
                                                                            (m_gridService.GetMaxRegionSize()),
                                                                            (grid_y*Constants.RegionSize));
                    bool found = false;
                    foreach (var r in regions)
                    {
                        if (r.PointIsInRegion(grid_x*Constants.RegionSize, grid_y*Constants.RegionSize))
                        {
                            resp = string.Format(resp, var, r.RegionName);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        resp = "var " + var + " = {error: true};";
                }
                else
                    resp = string.Format(resp, var, region.RegionName);
                response = System.Text.Encoding.UTF8.GetBytes(resp);
                httpResponse.ContentType = "text/javascript";
            }

            return response;
        }

        public byte[] MapRequest(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            //Remove the /MapService/
            string uri = httpRequest.RawUrl.Remove(0, 12);
            if (!uri.StartsWith("map"))
            {
                if (uri == "")
                {
                    string resp = "<ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\">" +
                                  "<Name>map.secondlife.com</Name>" +
                                  "<Prefix/>" +
                                  "<Marker/>" +
                                  "<MaxKeys>1000</MaxKeys>" +
                                  "<IsTruncated>true</IsTruncated>";
                    List<GridRegion> regions = m_gridService.GetRegionRange(null,
                                                                            (1000*Constants.RegionSize) -
                                                                            (8*Constants.RegionSize),
                                                                            (1000*Constants.RegionSize) +
                                                                            (8*Constants.RegionSize),
                                                                            (1000*Constants.RegionSize) -
                                                                            (8*Constants.RegionSize),
                                                                            (1000*Constants.RegionSize) +
                                                                            (8*Constants.RegionSize));
                    foreach (var region in regions)
                    {
                        resp += "<Contents><Key>map-1-" + region.RegionLocX/256 + "-" + region.RegionLocY/256 +
                                "-objects.jpg</Key>" +
                                "<LastModified>2012-07-09T21:26:32.000Z</LastModified></Contents>";
                    }
                    resp += "</ListBucketResult>";
                    httpResponse.ContentType = "application/xml";
                    return System.Text.Encoding.UTF8.GetBytes(resp);
                }
                using (MemoryStream imgstream = new MemoryStream())
                {
                    GridRegion region = m_registry.RequestModuleInterface<IGridService>().GetRegionByName(null,
                                                                                                          uri.Remove
                                                                                                              (4));
                    if (region == null)
                        region = m_registry.RequestModuleInterface<IGridService>().GetRegionByUUID(null, OpenMetaverse.UUID.Parse(uri.Remove(uri.Length - 4)));

                    // non-async because we know we have the asset immediately.
                    byte[] mapasset = m_assetService.GetData(region.TerrainMapImage.ToString());
                    if (mapasset != null)
                    {
                        Image image;
                        ManagedImage mImage;
                        if (!OpenJPEG.DecodeToImage(mapasset, out mImage, out image) || image == null)
                            return null;
                        // Decode image to System.Drawing.Image

                        EncoderParameters myEncoderParameters = new EncoderParameters();
                        myEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 95L);

                        // Save bitmap to stream
                        image.Save(imgstream, GetEncoderInfo("image/jpeg"), myEncoderParameters);

                        image.Dispose();

                        // Write the stream to a byte array for output
                        return imgstream.ToArray();

                    }
                }
                return null;
            }
            string[] splitUri = uri.Split('-');
            byte[] jpeg = FindCachedImage(uri);
            if (jpeg.Length != 0)
            {
                httpResponse.ContentType = "image/jpeg";
                return jpeg;
            }
            try
            {
                int mapLayer = int.Parse(uri.Substring(4, 1));
                int mapView = (int) Math.Pow(2, (mapLayer - 1));
                int regionX = int.Parse(splitUri[2]);
                int regionY = int.Parse(splitUri[3]);
                int distance = (int)Math.Pow(2, mapLayer);
                int maxRegionSize = m_gridService.GetMaxRegionSize();
                if (maxRegionSize == 0) maxRegionSize = 8192;
                List<GridRegion> regions = m_gridService.GetRegionRange(null,
                                                                    ((regionX) * Constants.RegionSize) - maxRegionSize,
                                                                    ((regionX + distance) * Constants.RegionSize) + maxRegionSize,
                                                                    ((regionY) * Constants.RegionSize) - maxRegionSize,
                                                                    ((regionY + distance) * Constants.RegionSize) + maxRegionSize);
                Bitmap mapTexture = BuildMapTile(mapLayer, regionX, regionY, regions);
                jpeg = CacheMapTexture(mapLayer, regionX, regionY, mapTexture);
                DisposeTexture(mapTexture);
            }
            catch
            {
            }
            httpResponse.ContentType = "image/jpeg";
            return jpeg;
        }

        private Bitmap BuildMapTile(int mapView, int regionX, int regionY, List<GridRegion> regions)
        {
            Bitmap mapTexture = FindCachedImage(mapView, regionX, regionY);
            if (mapTexture != null) 
                return mapTexture;
            if (mapView == 1)
                return BuildMapTile(regionX, regionY, regions.ToList());

            const int SizeOfImage = 256;

            List<Bitmap> generatedMapTiles = new List<Bitmap>();
            int offset = (int)(Math.Pow(2, mapView - 1) / 2f);
            generatedMapTiles.Add(BuildMapTile(mapView - 1, regionX, regionY, regions));
            generatedMapTiles.Add(BuildMapTile(mapView - 1, regionX + offset, regionY, regions));
            generatedMapTiles.Add(BuildMapTile(mapView - 1, regionX, regionY + offset, regions));
            generatedMapTiles.Add(BuildMapTile(mapView - 1, regionX + offset, regionY + offset, regions));
            bool isStatic = true;
            for (int i = 0; i < 4; i++)
                if (!IsStaticBlank(generatedMapTiles[i]))
                    isStatic = false;
                else
                    generatedMapTiles[i] = null;
            if (isStatic)
            {
                lock (m_blankTiles.BlankTilesLayers)
                    m_blankTiles.BlankTilesLayers.Add(((long)Util.IntsToUlong(regionX, regionY) << 8) + mapView);
                return m_blankRegionTile;
            }

            mapTexture = new Bitmap(SizeOfImage, SizeOfImage);
            using (Graphics g = Graphics.FromImage(mapTexture))
            {
                SolidBrush sea = new SolidBrush(Color.FromArgb(29, 71, 95));
                g.FillRectangle(sea, 0, 0, SizeOfImage, SizeOfImage);

                if (generatedMapTiles[0] != null)
                {
                    Bitmap texture = ResizeBitmap(generatedMapTiles[0], 128, 128);
                    g.DrawImage(texture, new Point(0, 128));
                    DisposeTexture(texture);
                }
                
                if (generatedMapTiles[1] != null)
                {
                    Bitmap texture = ResizeBitmap(generatedMapTiles[1], 128, 128);
                    g.DrawImage(texture, new Point(128, 128));
                    DisposeTexture(texture);
                }

                if (generatedMapTiles[2] != null)
                {
                    Bitmap texture = ResizeBitmap(generatedMapTiles[2], 128, 128);
                    g.DrawImage(texture, new Point(0, 0));
                    DisposeTexture(texture);
                }

                if (generatedMapTiles[3] != null)
                {
                    Bitmap texture = ResizeBitmap(generatedMapTiles[3], 128, 128);
                    g.DrawImage(texture, new Point(128, 0));
                    DisposeTexture(texture);
                }
            }

            CacheMapTexture(mapView, regionX, regionY, mapTexture);
            return mapTexture;
        }

        private void DisposeTexture(Bitmap bitmap)
        {
            if (!IsStaticBlank(bitmap))
                bitmap.Dispose();
        }

        private bool IsStaticBlank(Bitmap bitmap)
        {
            return bitmap.Tag != null && (bitmap.Tag is string) && ((string)bitmap.Tag) == "StaticBlank";
        }

        private Bitmap ResizeBitmap(Bitmap b, int nWidth, int nHeight)
        {
            Bitmap newsize = new Bitmap(nWidth, nHeight);
            using (Graphics temp = Graphics.FromImage(newsize))
            {
                temp.DrawImage(b, 0, 0, nWidth, nHeight);
                temp.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            }
            DisposeTexture(b);
            return newsize;
        }

        private Bitmap BuildMapTile(int regionX, int regionY, List<GridRegion> regions)
        {
            byte[] jpeg = new byte[0];
            if (regions == null)
            {
                int maxRegionSize = m_gridService.GetMaxRegionSize();
                if (maxRegionSize == 0) maxRegionSize = 8192;
                regions = m_gridService.GetRegionRange(null,
                                                                    (regionX * Constants.RegionSize) - maxRegionSize,
                                                                    (regionX * Constants.RegionSize) + maxRegionSize,
                                                                    (regionY * Constants.RegionSize) - maxRegionSize,
                                                                    (regionY * Constants.RegionSize) + maxRegionSize);
            }

            List<Image> bitImages = new List<Image>();
            List<GridRegion> badRegions = new List<GridRegion>();
            int newregionX = regionX * Constants.RegionSize;
            int newregionY = regionY * Constants.RegionSize;
            Rectangle mapRect = new Rectangle(regionX * Constants.RegionSize, regionY * Constants.RegionSize, Constants.RegionSize, Constants.RegionSize);
            foreach (GridRegion r in regions)
            {
                Rectangle regionRect = new Rectangle(r.RegionLocX, r.RegionLocY, r.RegionSizeX, r.RegionSizeY);
                if (!mapRect.IntersectsWith(regionRect))
                    badRegions.Add(r);
            }
            foreach (GridRegion r in badRegions)
                regions.Remove(r);
            badRegions.Clear();
            IJ2KDecoder decoder = m_registry.RequestModuleInterface<IJ2KDecoder>();
            foreach (GridRegion r in regions)
            {
                byte[] texAsset = m_assetService.GetData(r.TerrainMapImage.ToString());

                if (texAsset != null)
                {
                    Image image = decoder.DecodeToImage(texAsset);
                    if (image != null)
                        bitImages.Add(image);
                    else
                        badRegions.Add(r);
                }
                else
                    badRegions.Add(r);
            }
            foreach (GridRegion r in badRegions)
                regions.Remove(r);

            if (regions.Count == 0)
            {
                lock (m_blankTiles.BlankTiles)
                    m_blankTiles.BlankTiles.Add(Util.IntsToUlong(regionX, regionY));
                return m_blankRegionTile;
            }

            const int SizeOfImage = 256;

            Bitmap mapTexture = new Bitmap(SizeOfImage, SizeOfImage);
            using (Graphics g = Graphics.FromImage(mapTexture))
            {
                SolidBrush sea = new SolidBrush(Color.FromArgb(29, 71, 95));
                g.FillRectangle(sea, 0, 0, SizeOfImage, SizeOfImage);

                for (int i = 0; i < regions.Count; i++)
                {
                    //Find the offsets first
                    float x = (regions[i].RegionLocX - (regionX * (float)Constants.RegionSize)) /
                                Constants.RegionSize;
                    float y = (regions[i].RegionLocY - (regionY * (float)Constants.RegionSize)) /
                                Constants.RegionSize;
                    y += (regions[i].RegionSizeX - Constants.RegionSize) / Constants.RegionSize;
                    float xx = (float)(x * (SizeOfImage));
                    float yy = SizeOfImage - (y * (SizeOfImage) + (SizeOfImage));
                    g.DrawImage(bitImages[i], xx, yy,
                                (int)
                                (SizeOfImage *
                                    ((float)regions[i].RegionSizeX / Constants.RegionSize)),
                                (int)
                                (SizeOfImage *
                                    (regions[i].RegionSizeY / (float)Constants.RegionSize))); // y origin is top
                }
            }

            foreach (var bmp in bitImages)
                bmp.Dispose();

            CacheMapTexture(1, regionX, regionY, mapTexture);
            //mapTexture = ResizeBitmap(mapTexture, 128, 128);
            return mapTexture;
        }

        // From msdn
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();
            return encoders.FirstOrDefault(t => t.MimeType == mimeType);
        }

        private byte[] FindCachedImage(string name)
        {
            if (!m_cacheEnabled)
                return new byte[0];

            string fullPath = Path.Combine("assetcache", Path.Combine("mapzoomlevels", name));
            if (File.Exists(fullPath))
            {
                //Make sure the time is ok
                if (DateTime.Now < File.GetLastWriteTime(fullPath).AddHours(m_cacheExpires))
                    return File.ReadAllBytes(fullPath);
            }
            return new byte[0];
        }

        private Bitmap FindCachedImage(int maplayer, int regionX, int regionY)
        {
            if (!m_cacheEnabled)
                return null;

            if (maplayer == 1)
            {
                lock (m_blankTiles.BlankTiles)
                    if (m_blankTiles.BlankTiles.Contains(Util.IntsToUlong(regionX, regionY)))
                        return m_blankRegionTile;
            }
            else
            {
                lock (m_blankTiles.BlankTilesLayers)
                    if (m_blankTiles.BlankTilesLayers.Contains(((long)Util.IntsToUlong(regionX, regionY) << 8) + maplayer))
                        return m_blankRegionTile;
            }

            string name = string.Format("map-{0}-{1}-{2}-objects.jpg", maplayer, regionX, regionY);
            string fullPath = Path.Combine("assetcache", Path.Combine("mapzoomlevels", name));
            if (File.Exists(fullPath))
            {
                //Make sure the time is ok
                if (DateTime.Now < File.GetLastWriteTime(fullPath).AddHours(m_cacheExpires))
                {
                    using (MemoryStream imgstream = new MemoryStream(File.ReadAllBytes(fullPath)))
                    {
                        return new Bitmap(imgstream);
                    }
                }
            }
            return null;
        }

        private byte[] CacheMapTexture(int maplayer, int regionX, int regionY, Bitmap mapTexture, bool forced = false)
        {
            if (!forced && IsStaticBlank(mapTexture))
                return m_blankRegionTileData;

            byte[] jpeg;
            EncoderParameters myEncoderParameters = new EncoderParameters();
            myEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 95L);

            using (MemoryStream imgstream = new MemoryStream())
            {
                // Save bitmap to stream
                lock(mapTexture)
                    mapTexture.Save(imgstream, GetEncoderInfo("image/jpeg"), myEncoderParameters);

                // Write the stream to a byte array for output
                jpeg = imgstream.ToArray();
            }
            SaveCachedImage(maplayer, regionX, regionY, jpeg);
            return jpeg;
        }

        private void SaveCachedImage(int maplayer, int regionX, int regionY, byte[] data)
        {
            if (!m_cacheEnabled)
                return;

            string name = string.Format("map-{0}-{1}-{2}-objects.jpg", maplayer, regionX, regionY);
            string fullPath = Path.Combine("assetcache", Path.Combine("mapzoomlevels", name));
            File.WriteAllBytes(fullPath, data);
        }
    }

    [ProtoContract()]
    internal class MapTileIndex
    {
        [ProtoMember(1)]
        public HashSet<ulong> BlankTiles = new HashSet<ulong>();
        [ProtoMember(2)]
        public HashSet<long> BlankTilesLayers = new HashSet<long>();
    }
}