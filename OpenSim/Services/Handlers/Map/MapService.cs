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
using System.Linq;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.Handlers.Map
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

        public void Initialize (IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig mapConfig = config.Configs["MapService"];
            if (mapConfig != null)
            {
                m_enabled = mapConfig.GetBoolean ("Enabled", m_enabled);
                m_port = mapConfig.GetUInt ("Port", m_port);
                m_cacheEnabled = mapConfig.GetBoolean ("CacheEnabled", m_cacheEnabled);
                m_cacheExpires = mapConfig.GetFloat ("CacheExpires", m_cacheExpires);
            }
            if(!m_enabled)
                return;

            if (m_cacheEnabled)
                CreateCacheDirectories ();

            m_server = registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(m_port);
            m_server.AddHTTPHandler("/MapService/", MapRequest);
            m_server.AddHTTPHandler(new GenericStreamHandler("GET", "/MapAPI/", MapAPIRequest));

            registry.RegisterModuleInterface<IMapService>(this);
        }

        private void CreateCacheDirectories ()
        {
            if (!Directory.Exists ("assetcache"))
                Directory.CreateDirectory ("assetcache");
            if(!Directory.Exists("assetcache/mapzoomlevels"))
                Directory.CreateDirectory ("assetcache/mapzoomlevels");
        }

        public void Start (IConfigSource config, IRegistryCore registry)
        {
            m_assetService = m_registry.RequestModuleInterface<IAssetService>();
            m_gridService = m_registry.RequestModuleInterface<IGridService>();
        }

        public void FinishedStartup ()
        {
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
                    grid_x * Constants.RegionSize, grid_y * Constants.RegionSize);
                if (region == null)
                {
                    List<GridRegion> regions = m_gridService.GetRegionRange(null,
                        (grid_x * Constants.RegionSize) - (m_gridService.GetMaxRegionSize()),
                        (grid_x * Constants.RegionSize),
                        (grid_y * Constants.RegionSize) - (m_gridService.GetMaxRegionSize()),
                        (grid_y * Constants.RegionSize));
                    bool found = false;
                    foreach (var r in regions)
                    {
                        if (r.PointIsInRegion(grid_x * Constants.RegionSize, grid_y * Constants.RegionSize))
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

        public Hashtable MapRequest (Hashtable request)
        {
            Hashtable reply = new Hashtable ();
            string uri = request["uri"].ToString ();
            //Remove the /MapService/
            uri = uri.Remove (0, 12);
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
                        (1000 * Constants.RegionSize) - (8 * Constants.RegionSize),
                        (1000 * Constants.RegionSize) + (8 * Constants.RegionSize),
                        (1000 * Constants.RegionSize) - (8 * Constants.RegionSize),
                        (1000 * Constants.RegionSize) + (8 * Constants.RegionSize));
                    foreach (var region in regions)
                    {
                        resp += "<Contents><Key>map-1-" + region.RegionLocX/256 + "-" + region.RegionLocY/256 + "-objects.jpg</Key>" +
                            "<LastModified>2012-07-09T21:26:32.000Z</LastModified></Contents>";
                    }
                    resp += "</ListBucketResult>";
                    reply["str_response_string"] = resp;
                    reply["int_response_code"] = 200;
                    reply["content_type"] = "application/xml";
                    return reply;
                }
                return null;
            }
            string[] splitUri = uri.Split ('-');
            byte[] jpeg = FindCachedImage(uri);
            if (jpeg.Length != 0)
            {
                reply["str_response_string"] = Convert.ToBase64String (jpeg);
                reply["int_response_code"] = 200;
                reply["content_type"] = "image/jpeg";

                return reply;
            }
            try
            {
                int mapLayer = int.Parse (uri.Substring (4, 1));
                int mapView = (int)Math.Pow(2, (mapLayer - 1));
                int regionX = int.Parse (splitUri[2]);
                int regionY = int.Parse (splitUri[3]);

                List<GridRegion> regions = m_gridService.GetRegionRange(null,
                        (regionX * Constants.RegionSize) - (mapView * Constants.RegionSize),
                        (regionX * Constants.RegionSize) + (mapView * Constants.RegionSize),
                        (regionY * Constants.RegionSize) - (mapView * Constants.RegionSize),
                        (regionY * Constants.RegionSize) + (mapView * Constants.RegionSize));
                List<AssetBase> textures = new List<AssetBase> ();
                List<Image> bitImages = new List<Image> ();
                List<GridRegion> badRegions = new List<GridRegion> ();
                foreach (GridRegion r in regions)
                {
                    AssetBase texAsset = m_assetService.Get(r.TerrainMapImage.ToString());

                    if (texAsset != null)
                    {
                        textures.Add(texAsset);
                        Image image;
                        ManagedImage mImage;
                        if ((OpenJPEG.DecodeToImage(texAsset.Data, out mImage, out image)) && image != null)
                            bitImages.Add(image);
                        else
                            badRegions.Add(r);
                    }
                    else
                        badRegions.Add(r);
                }
                foreach (GridRegion r in badRegions)
                    regions.Remove (r);

                const int SizeOfImage = 256;

                Bitmap mapTexture = new Bitmap (SizeOfImage, SizeOfImage);
                Graphics g = Graphics.FromImage (mapTexture);
                SolidBrush sea = new SolidBrush (Color.FromArgb (29, 71, 95));
                g.FillRectangle (sea, 0, 0, SizeOfImage, SizeOfImage);

                for (int i = 0; i < regions.Count; i++)
                {
                    //Find the offsets first
                    float x = (regions[i].RegionLocX - (regionX * (float)Constants.RegionSize)) / Constants.RegionSize;
                    float y = (regions[i].RegionLocY - (regionY * (float)Constants.RegionSize)) / Constants.RegionSize;
                    y += (regions[i].RegionSizeX - Constants.RegionSize) / Constants.RegionSize;
                    float xx = (float)(x * (SizeOfImage / mapView));
                    float yy = SizeOfImage - (y * (SizeOfImage / mapView) + (SizeOfImage / (mapView)));
                    g.DrawImage (bitImages[i], xx, yy,
                        (int)(SizeOfImage / (float)mapView * ((float)regions[i].RegionSizeX / Constants.RegionSize)), (int)(SizeOfImage / (float)mapView * (regions[i].RegionSizeY / (float)Constants.RegionSize))); // y origin is top
                }

                EncoderParameters myEncoderParameters = new EncoderParameters ();
                myEncoderParameters.Param[0] = new EncoderParameter (Encoder.Quality, 95L);

                MemoryStream imgstream = new MemoryStream ();
                // Save bitmap to stream
                mapTexture.Save (imgstream, GetEncoderInfo ("image/jpeg"), myEncoderParameters);

                // Write the stream to a byte array for output
                jpeg = imgstream.ToArray ();
                SaveCachedImage (uri, jpeg);
            }
            catch
            {
            }
            if (jpeg.Length == 0 && splitUri.Length > 1 && splitUri[1].Length > 1)
            {
                MemoryStream imgstream = new MemoryStream();
                GridRegion region = m_registry.RequestModuleInterface<IGridService>().GetRegionByName(null,
                                                                                                      splitUri[1].Remove
                                                                                                          (4));
                if (region == null)
                    return null;
                // non-async because we know we have the asset immediately.
                AssetBase mapasset =
                    m_registry.RequestModuleInterface<IAssetService>().Get(region.TerrainMapImage.ToString());
                Image image;
                ManagedImage mImage;
                if (!(OpenJPEG.DecodeToImage(mapasset.Data, out mImage, out image)) || image == null)
                    return null;
                // Decode image to System.Drawing.Image

                // Save to bitmap
                Bitmap mapTexture = new Bitmap(image);

                EncoderParameters myEncoderParameters = new EncoderParameters();
                myEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 95L);

                // Save bitmap to stream
                mapTexture.Save(imgstream, GetEncoderInfo("image/jpeg"), myEncoderParameters);

                // Write the stream to a byte array for output
                jpeg = imgstream.ToArray();
                SaveCachedImage(uri, jpeg);
            }
            reply["str_response_string"] = Convert.ToBase64String (jpeg);
            reply["int_response_code"] = 200;
            reply["content_type"] = "image/jpeg";

            return reply;
        }

        // From msdn
        private static ImageCodecInfo GetEncoderInfo (String mimeType)
        {
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders ();
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

        private byte[] FindCachedImage (string name)
        {
            if (!m_cacheEnabled)
                return new byte[0];

            string fullPath = Path.Combine ("assetcache", Path.Combine ("mapzoomlevels", name));
            if (File.Exists (fullPath))
            {
                //Make sure the time is ok
                if(DateTime.Now < File.GetLastWriteTime (fullPath).AddHours(m_cacheExpires))
                    return File.ReadAllBytes (fullPath);
            }
            return new byte[0];
        }

        private void SaveCachedImage (string name, byte[] data)
        {
            if (!m_cacheEnabled)
                return;

            string fullPath = Path.Combine ("assetcache", Path.Combine ("mapzoomlevels", name));
            File.WriteAllBytes (fullPath, data);
        }
    }
}
