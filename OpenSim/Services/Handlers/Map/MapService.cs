using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using Nini.Config;
using log4net;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Aurora.DataManager;
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

        public void Initialize (IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig mapConfig = config.Configs["MapService"];
            if (mapConfig != null)
            {
                m_enabled = mapConfig.GetBoolean ("Enabled", m_enabled);
                m_cacheEnabled = mapConfig.GetBoolean ("CacheEnabled", m_cacheEnabled);
                m_cacheExpires = mapConfig.GetFloat ("CacheExpires", m_cacheExpires);
            }
            if(!m_enabled)
                return;

            if (m_cacheEnabled)
                CreateCacheDirectories ();

            registry.RegisterModuleInterface<IMapService> (this);
            m_server = registry.RequestModuleInterface<ISimulationBase> ().GetHttpServer (m_port);
            m_server.AddHTTPHandler ("/MapService/", MapRequest);
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
        }

        public void FinishedStartup ()
        {
        }

        public string GetURLOfMap ()
        {
            return m_server.HostName + ":" + m_server.Port + "/MapService/";
        }

        public Hashtable MapRequest (Hashtable request)
        {
            Hashtable reply = new Hashtable ();
            string uri = request["uri"].ToString ();
            //Remove the /MapService/
            uri = uri.Remove (0, 12);
            if (!uri.StartsWith ("map"))
                return null;
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
                int regionX = int.Parse (splitUri[2]);
                int regionY = int.Parse (splitUri[3]);

                List<GridRegion> regions = m_registry.RequestModuleInterface<IGridService> ().GetRegionRange (UUID.Zero,
                        (int)((regionX * (int)Constants.RegionSize) - (mapLayer * (int)Constants.RegionSize)),
                        (int)((regionX * (int)Constants.RegionSize) + (mapLayer * (int)Constants.RegionSize)),
                        (int)((regionY * (int)Constants.RegionSize) - (mapLayer * (int)Constants.RegionSize)),
                        (int)((regionY * (int)Constants.RegionSize) + (mapLayer * (int)Constants.RegionSize)));
                List<AssetBase> textures = new List<AssetBase> ();
                List<Image> bitImages = new List<Image> ();

                foreach (GridRegion r in regions)
                {
                    AssetBase texAsset = m_registry.RequestModuleInterface<IAssetService> ().Get (r.TerrainImage.ToString ());

                    if (texAsset != null)
                        textures.Add (texAsset);
                }

                foreach (AssetBase asset in textures)
                {
                    Image image;
                    ManagedImage mImage;
                    if ((OpenJPEG.DecodeToImage (asset.Data, out mImage, out image)) && image != null)
                        bitImages.Add (image);
                }

                int SizeOfImage = Constants.RegionSize * mapLayer;
                int RealSizeOfImage = 256;
                int XRealSizeOfImage = 256;
                int[] pwt = new int[4] { 256, 512, 1024, 2048 };
                for (int i = 0; i < pwt.Length; i++)
                {
                    if (pwt[i] >= SizeOfImage && i > 0 && pwt[i - 1] < SizeOfImage)
                        XRealSizeOfImage = pwt[i];
                }
                //Force to 512 only... the viewer can't seem to handle larger well
                //RealSizeOfImage = SizeOfImage > pwt[1] ? pwt[1] : RealSizeOfImage;
                SizeOfImage = (int)(RealSizeOfImage / ((float)XRealSizeOfImage / (float)SizeOfImage));
                Bitmap mapTexture = new Bitmap (RealSizeOfImage, RealSizeOfImage);
                Graphics g = Graphics.FromImage (mapTexture);
                SolidBrush sea = new SolidBrush (Color.FromArgb (29, 71, 95));
                g.FillRectangle (sea, 0, 0, RealSizeOfImage, RealSizeOfImage);

                for (int i = 0; i < regions.Count; i++)
                {
                    float x = (float)((regions[i].RegionLocX - (regionX * (float)Constants.RegionSize)) / (float)Constants.RegionSize);
                    float y = (float)((regions[i].RegionLocY - (regionY * (float)Constants.RegionSize)) / (float)Constants.RegionSize);
                    float xx = (float)(x * (SizeOfImage / mapLayer));
                    float yy = RealSizeOfImage - (y * (SizeOfImage / mapLayer) + (SizeOfImage / (mapLayer)));
                    bitImages[i].RotateFlip (RotateFlipType.RotateNoneFlipX);
                    g.DrawImage (bitImages[i], xx, yy,
                        (int)((float)SizeOfImage / (float)mapLayer), (int)((float)SizeOfImage / (float)mapLayer)); // y origin is top
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
            if(jpeg.Length == 0)
            {
                MemoryStream imgstream = new MemoryStream ();
                GridRegion region = m_registry.RequestModuleInterface<IGridService> ().GetRegionByName (UUID.Zero, splitUri[1].Remove(4));
                if (region == null)
                    return null;
                // non-async because we know we have the asset immediately.
                AssetBase mapasset = m_registry.RequestModuleInterface<IAssetService> ().Get (region.TerrainMapImage.ToString ());
                Image image;
                ManagedImage mImage;
                if (!(OpenJPEG.DecodeToImage (mapasset.Data, out mImage, out image)) || image == null)
                    return null;
                // Decode image to System.Drawing.Image
                if (image != null)
                {
                    // Save to bitmap
                    Bitmap mapTexture = new Bitmap (image);

                    EncoderParameters myEncoderParameters = new EncoderParameters ();
                    myEncoderParameters.Param[0] = new EncoderParameter (Encoder.Quality, 95L);

                    // Save bitmap to stream
                    mapTexture.Save (imgstream, GetEncoderInfo ("image/jpeg"), myEncoderParameters);

                    // Write the stream to a byte array for output
                    jpeg = imgstream.ToArray ();
                    SaveCachedImage (uri, jpeg);
                }
            }
            reply["str_response_string"] = Convert.ToBase64String (jpeg);
            reply["int_response_code"] = 200;
            reply["content_type"] = "image/jpeg";

            return reply;
        }

        // From msdn
        private static ImageCodecInfo GetEncoderInfo (String mimeType)
        {
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders ();
            for (int j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
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
