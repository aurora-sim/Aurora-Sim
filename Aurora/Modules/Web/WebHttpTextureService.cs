using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Services;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Aurora.Modules.Web
{
    public class WebHttpTextureService : IService, IWebHttpTextureService
    {
        protected IRegistryCore _registry;
        protected string _gridNick;
        protected IHttpServer _server;

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            _registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
            _server = _registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(0);
            if (_server != null)
            {
                _server.AddHTTPHandler(new GenericStreamHandler("GET", "GridTexture", OnHTTPGetTextureImage));
                _registry.RegisterModuleInterface<IWebHttpTextureService>(this);
            }
            IGridInfo gridInfo = _registry.RequestModuleInterface<IGridInfo>();
            _gridNick = gridInfo != null
                            ? gridInfo.GridName
                            : "No Grid Name Available, please set this";
        }

        public string GetTextureURL(UUID textureID)
        {
            return _server.ServerURI + "/index.php?method=GridTexture&uuid=" + textureID.ToString();
        }

        public byte[] OnHTTPGetTextureImage(string path, Stream request, OSHttpRequest httpRequest,
                                            OSHttpResponse httpResponse)
        {
            byte[] jpeg = new byte[0];
            IAssetService m_AssetService = _registry.RequestModuleInterface<IAssetService>();

            using (MemoryStream imgstream = new MemoryStream())
            {
                // Taking our jpeg2000 data, decoding it, then saving it to a byte array with regular jpeg data

                // non-async because we know we have the asset immediately.
                byte[] mapasset = m_AssetService.GetData(httpRequest.QueryString["uuid"]);

                if (mapasset != null)
                {
                    // Decode image to System.Drawing.Image
                    Image image = null;
                    ManagedImage managedImage;
                    if (OpenJPEG.DecodeToImage(mapasset, out managedImage, out image))
                    {
                        // Save to bitmap
                        using (Bitmap texture = ResizeBitmap(image, 256, 256))
                        {
                            EncoderParameters myEncoderParameters = new EncoderParameters();
                            myEncoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,
                                                                                75L);

                            // Save bitmap to stream
                            texture.Save(imgstream, GetEncoderInfo("image/jpeg"), myEncoderParameters);

                            // Write the stream to a byte array for output
                            jpeg = imgstream.ToArray();
                        }
                        image.Dispose();
                    }
                }
            }

            httpResponse.ContentType = "image/jpeg";

            return jpeg;
        }

        private Bitmap ResizeBitmap(Image b, int nWidth, int nHeight)
        {
            Bitmap newsize = new Bitmap(nWidth, nHeight);
            Graphics temp = Graphics.FromImage(newsize);
            temp.DrawImage(b, 0, 0, nWidth, nHeight);
            temp.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            temp.DrawString(_gridNick, new Font("Arial", 8, FontStyle.Regular),
                            new SolidBrush(Color.FromArgb(90, 255, 255, 50)), new Point(2, nHeight - 13));

            return newsize;
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
    }
}