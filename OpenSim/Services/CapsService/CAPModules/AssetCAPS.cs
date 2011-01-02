using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using System.Web;
using log4net;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Capabilities;

using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;
using OpenMetaverse.StructuredData;
using OpenMetaverse.Imaging;

namespace OpenSim.Services.CapsService
{
    public class AssetCAPS : ICapsServiceConnector
    {
        #region Stream Handler

        public delegate byte[] StreamHandlerCallback(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse);

        public class StreamHandler : BaseStreamHandler
        {
            StreamHandlerCallback m_callback;

            public StreamHandler(string httpMethod, string path, StreamHandlerCallback callback)
                : base(httpMethod, path)
            {
                m_callback = callback;
            }

            public override byte[] Handle(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return m_callback(path, request, httpRequest, httpResponse);
            }
        }

        #endregion Stream Handler

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string m_uploadBakedTexturePath = "0010";
        protected IAssetService m_assetService;
        protected IRegionClientCapsService m_service;
        public const string DefaultFormat = "x-j2c";
        // TODO: Change this to a config option
        protected string REDIRECT_URL = null;
        protected UUID m_agentID;

        public void RegisterCaps(UUID agentID, IRegionClientCapsService service)
        {
            m_assetService = service.Registry.RequestModuleInterface<IAssetService>();
            m_agentID = agentID;

            service.AddStreamHandler("GetTexture", 
                new StreamHandler("GET", service.CreateCAPS("GetTexture", ""),
                                                        ProcessGetTexture));
            service.AddStreamHandler("UploadBakedTexture", 
                new RestStreamHandler("POST", service.CreateCAPS("UploadBakedTexture", m_uploadBakedTexturePath),
                                                        UploadBakedTexture));
        }

        #region Get Texture

        private byte[] ProcessGetTexture(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            //m_log.DebugFormat("[GETTEXTURE]: called in {0}", m_scene.RegionInfo.RegionName);

            // Try to parse the texture ID from the request URL
            NameValueCollection query = HttpUtility.ParseQueryString(httpRequest.Url.Query);
            string textureStr = query.GetOne("texture_id");
            string format = query.GetOne("format");

            if (m_assetService == null)
            {
                m_log.Error("[GETTEXTURE]: Cannot fetch texture " + textureStr + " without an asset service");
                httpResponse.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
                return null;
            }

            UUID textureID;
            if (!String.IsNullOrEmpty(textureStr) && UUID.TryParse(textureStr, out textureID))
            {
                string[] formats;
                if (format != null && format != string.Empty)
                {
                    formats = new string[1] { format.ToLower() };
                }
                else
                {
                    formats = WebUtils.GetPreferredImageTypes(httpRequest.Headers.Get("Accept"));
                    if (formats.Length == 0)
                        formats = new string[1] { DefaultFormat }; // default
                }
                // OK, we have an array with preferred formats, possibly with only one entry
                httpResponse.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
                foreach (string f in formats)
                {
                    if (FetchTexture(httpRequest, httpResponse, textureID, f))
                        break;
                }
            }
            else
            {
                m_log.Warn("[GETTEXTURE]: Failed to parse a texture_id from GetTexture request: " + httpRequest.Url);
            }

            httpResponse.Send();
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <param name="httpResponse"></param>
        /// <param name="textureID"></param>
        /// <param name="format"></param>
        /// <returns>False for "caller try another codec"; true otherwise</returns>
        private bool FetchTexture(OSHttpRequest httpRequest, OSHttpResponse httpResponse, UUID textureID, string format)
        {
            m_log.DebugFormat("[GETTEXTURE]: {0} with requested format {1}", textureID, format);
            AssetBase texture;

            string fullID = textureID.ToString();
            if (format != DefaultFormat)
                fullID = fullID + "-" + format;

            if (!String.IsNullOrEmpty(REDIRECT_URL))
            {
                // Only try to fetch locally cached textures. Misses are redirected
                texture = m_assetService.GetCached(fullID);

                if (texture != null)
                {
                    if (texture.Type != (sbyte)AssetType.Texture)
                    {
                        httpResponse.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
                        return true;
                    }
                    WriteTextureData(httpRequest, httpResponse, texture, format);
                }
                else
                {
                    string textureUrl = REDIRECT_URL + textureID.ToString();
                    m_log.Debug("[GETTEXTURE]: Redirecting texture request to " + textureUrl);
                    httpResponse.RedirectLocation = textureUrl;
                    return true;
                }
            }
            else // no redirect
            {
                // try the cache
                texture = m_assetService.GetCached(fullID);

                if (texture == null)
                {
                    //m_log.DebugFormat("[GETTEXTURE]: texture was not in the cache");

                    // Fetch locally or remotely. Misses return a 404
                    texture = m_assetService.Get(textureID.ToString());

                    if (texture != null)
                    {
                        if (texture.Type != (sbyte)AssetType.Texture)
                        {
                            httpResponse.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
                            return true;
                        }
                        if (format == DefaultFormat)
                        {
                            WriteTextureData(httpRequest, httpResponse, texture, format);
                            return true;
                        }
                        else
                        {
                            AssetBase newTexture = new AssetBase(texture.ID + "-" + format, texture.Name, (sbyte)AssetType.Texture, texture.Metadata.CreatorID);
                            newTexture.Data = ConvertTextureData(texture, format);
                            if (newTexture.Data.Length == 0)
                                return false; // !!! Caller try another codec, please!

                            newTexture.Flags = AssetFlags.Collectable;
                            newTexture.Temporary = true;
                            m_assetService.Store(newTexture);
                            WriteTextureData(httpRequest, httpResponse, newTexture, format);
                            return true;
                        }
                    }
                }
                else // it was on the cache
                {
                    //m_log.DebugFormat("[GETTEXTURE]: texture was in the cache");
                    WriteTextureData(httpRequest, httpResponse, texture, format);
                    return true;
                }

            }

            // not found
            m_log.Warn("[GETTEXTURE]: Texture " + textureID + " not found");
            httpResponse.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
            return true;

        }

        private void WriteTextureData(OSHttpRequest request, OSHttpResponse response, AssetBase texture, string format)
        {
            string range = request.Headers.GetOne("Range");
            //m_log.DebugFormat("[GETTEXTURE]: Range {0}", range);
            if (!String.IsNullOrEmpty(range)) // JP2's only
            {
                // Range request
                int start, end;
                if (TryParseRange(range, out start, out end))
                {
                    // Before clamping start make sure we can satisfy it in order to avoid
                    // sending back the last byte instead of an error status
                    if (start >= texture.Data.Length)
                    {
                        response.StatusCode = (int)System.Net.HttpStatusCode.RequestedRangeNotSatisfiable;
                        return;
                    }

                    end = Utils.Clamp(end, 0, texture.Data.Length - 1);
                    start = Utils.Clamp(start, 0, end);
                    int len = end - start + 1;

                    //m_log.Debug("Serving " + start + " to " + end + " of " + texture.Data.Length + " bytes for texture " + texture.ID);

                    if (len < texture.Data.Length)
                        response.StatusCode = (int)System.Net.HttpStatusCode.PartialContent;
                    else
                        response.StatusCode = (int)System.Net.HttpStatusCode.OK;

                    response.ContentLength = len;
                    response.ContentType = texture.Metadata.ContentType;
                    response.AddHeader("Content-Range", String.Format("bytes {0}-{1}/{2}", start, end, texture.Data.Length));

                    response.Body.Write(texture.Data, start, len);
                }
                else
                {
                    m_log.Warn("[GETTEXTURE]: Malformed Range header: " + range);
                    response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                }
            }
            else // JP2's or other formats
            {
                // Full content request
                response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                response.ContentLength = texture.Data.Length;
                response.ContentType = texture.Metadata.ContentType;
                if (format == DefaultFormat)
                    response.ContentType = texture.Metadata.ContentType;
                else
                    response.ContentType = "image/" + format;
                response.Body.Write(texture.Data, 0, texture.Data.Length);
            }
        }

        private bool TryParseRange(string header, out int start, out int end)
        {
            if (header.StartsWith("bytes="))
            {
                string[] rangeValues = header.Substring(6).Split('-');
                if (rangeValues.Length == 2)
                {
                    if (Int32.TryParse(rangeValues[0], out start) && Int32.TryParse(rangeValues[1], out end))
                        return true;
                }
            }

            start = end = 0;
            return false;
        }

        private byte[] ConvertTextureData(AssetBase texture, string format)
        {
            m_log.DebugFormat("[GETTEXTURE]: Converting texture {0} to {1}", texture.ID, format);
            byte[] data = new byte[0];

            MemoryStream imgstream = new MemoryStream();
            Bitmap mTexture = new Bitmap(1, 1);
            ManagedImage managedImage;
            Image image = (Image)mTexture;

            try
            {
                // Taking our jpeg2000 data, decoding it, then saving it to a byte array with regular data

                imgstream = new MemoryStream();

                // Decode image to System.Drawing.Image
                if (OpenJPEG.DecodeToImage(texture.Data, out managedImage, out image))
                {
                    // Save to bitmap
                    mTexture = new Bitmap(image);

                    EncoderParameters myEncoderParameters = new EncoderParameters();
                    myEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 95L);

                    // Save bitmap to stream
                    ImageCodecInfo codec = GetEncoderInfo("image/" + format);
                    if (codec != null)
                    {
                        mTexture.Save(imgstream, codec, myEncoderParameters);
                        // Write the stream to a byte array for output
                        data = imgstream.ToArray();
                    }
                    else
                        m_log.WarnFormat("[GETTEXTURE]: No such codec {0}", format);

                }
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[GETTEXTURE]: Unable to convert texture {0} to {1}: {2}", texture.ID, format, e.Message);
            }
            finally
            {
                // Reclaim memory, these are unmanaged resources
                // If we encountered an exception, one or more of these will be null
                if (mTexture != null)
                    mTexture.Dispose();

                if (image != null)
                    image.Dispose();

                if (imgstream != null)
                {
                    imgstream.Close();
                    imgstream.Dispose();
                }
            }

            return data;
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

        #endregion

        #region Baked Textures

        public string UploadBakedTexture(string request, string path,
                string param, OSHttpRequest httpRequest,
                OSHttpResponse httpResponse)
        {
            try
            {
                //m_log.Debug("[CAPS]: UploadBakedTexture Request in region: " +
                //        m_regionName);

                string uploaderPath = Util.RandomClass.Next(1000, 8000).ToString("0000");
                string uploadpath = m_service.CreateCAPS("Upload" + uploaderPath, uploaderPath);
                BakedTextureUploader uploader =
                    new BakedTextureUploader(uploadpath, "Upload" + uploaderPath,
                        m_service);
                uploader.OnUpLoad += BakedTextureUploaded;

                m_service.AddStreamHandler(uploadpath,
                        new BinaryStreamHandler("POST", uploadpath,
                        uploader.uploaderCaps));

                string uploaderURL = m_service.HostUri + uploadpath;
                OSDMap map = new OSDMap();
                map["uploader"] = uploaderURL;
                map["state"] = "upload";
                return OSDParser.SerializeLLSDXmlString(map);
            }
            catch (Exception e)
            {
                m_log.Error("[CAPS]: " + e.ToString());
            }

            return null;
        }

        public delegate void UploadedBakedTexture(UUID assetID, byte[] data);
        public class BakedTextureUploader
        {
            public event UploadedBakedTexture OnUpLoad;
            private UploadedBakedTexture handlerUpLoad = null;

            private string uploaderPath = String.Empty;
            private string uploadMethod = "";
            private UUID newAssetID;
            private IRegionClientCapsService clientCaps;

            public BakedTextureUploader(string path, string method, IRegionClientCapsService caps)
            {
                newAssetID = UUID.Random();
                uploaderPath = path;
                uploadMethod = method;
                clientCaps = caps;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="data"></param>
            /// <param name="path"></param>
            /// <param name="param"></param>
            /// <returns></returns>
            public string uploaderCaps(byte[] data, string path, string param)
            {
                string res = String.Empty;
                OSDMap map = new OSDMap();
                map["new_asset"] = newAssetID.ToString();
                map["item_id"] = UUID.Zero;
                map["state"] = "complete";
                res = OSDParser.SerializeLLSDXmlString(map);
                clientCaps.RemoveStreamHandler(uploadMethod, "POST", uploaderPath);

                handlerUpLoad = OnUpLoad;
                if (handlerUpLoad != null)
                {
                    handlerUpLoad(newAssetID, data);
                }

                return res;
            }
        }

        public void BakedTextureUploaded(UUID assetID, byte[] data)
        {
            m_log.InfoFormat("[AssetCAPS]: Received baked texture {0}", assetID.ToString());
            AssetBase asset;
            asset = new AssetBase(assetID, "Baked Texture", (sbyte)AssetType.Texture, m_agentID.ToString());
            asset.Data = data;
            asset.Temporary = true;
            asset.Flags = AssetFlags.Deletable;
            asset.Local = false;

            m_assetService.Store(asset);
        }

        #endregion
    }
}
