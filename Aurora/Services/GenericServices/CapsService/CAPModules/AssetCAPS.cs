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
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Assets;
using Aurora.Framework.Services.ClassHelpers.Inventory;
using Aurora.Framework.Utilities;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Imaging;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Encoder = System.Drawing.Imaging.Encoder;

namespace Aurora.Services
{
    public class AssetCAPS : ICapsServiceConnector
    {
        private const string m_uploadBakedTexturePath = "0010";
        protected IAssetService m_assetService;
        protected IRegionClientCapsService m_service;
        public const string DefaultFormat = "x-j2c";
        // TODO: Change this to a config option
        protected string REDIRECT_URL = null;

        public void RegisterCaps(IRegionClientCapsService service)
        {
            m_service = service;
            m_assetService = service.Registry.RequestModuleInterface<IAssetService>();

            service.AddStreamHandler("GetTexture",
                                     new GenericStreamHandler("GET", service.CreateCAPS("GetTexture", ""),
                                                              ProcessGetTexture));
            service.AddStreamHandler("UploadBakedTexture",
                                     new GenericStreamHandler("POST",
                                                              service.CreateCAPS("UploadBakedTexture",
                                                                                 m_uploadBakedTexturePath),
                                                              UploadBakedTexture));
            service.AddStreamHandler("GetMesh",
                                     new GenericStreamHandler("GET", service.CreateCAPS("GetMesh", ""),
                                                              ProcessGetMesh));
            service.AddStreamHandler("UpdateAvatarAppearance",
                                     new GenericStreamHandler("POST", service.CreateCAPS("UpdateAvatarAppearance", ""),
                                                              UpdateAvatarAppearance));
        }

        public void EnteringRegion()
        {
        }

        public void DeregisterCaps()
        {
            m_service.RemoveStreamHandler("GetTexture", "GET");
            m_service.RemoveStreamHandler("UploadBakedTexture", "POST");
            m_service.RemoveStreamHandler("GetMesh", "GET");
            m_service.RemoveStreamHandler("UpdateAvatarAppearance", "POST");
        }

        #region Get Texture

        private byte[] ProcessGetTexture(string path, Stream request, OSHttpRequest httpRequest,
                                         OSHttpResponse httpResponse)
        {
            //MainConsole.Instance.DebugFormat("[GETTEXTURE]: called in {0}", m_scene.RegionInfo.RegionName);

            // Try to parse the texture ID from the request URL
            NameValueCollection query = HttpUtility.ParseQueryString(httpRequest.Url.Query);
            string textureStr = query.GetOne("texture_id");
            string format = query.GetOne("format");

            if (m_assetService == null)
            {
                httpResponse.StatusCode = (int) System.Net.HttpStatusCode.NotFound;
                return MainServer.BlankResponse;
            }

            UUID textureID;
            if (!String.IsNullOrEmpty(textureStr) && UUID.TryParse(textureStr, out textureID))
            {
                string[] formats;
                if (!string.IsNullOrEmpty(format))
                    formats = new[] {format.ToLower()};
                else
                {
                    formats = WebUtils.GetPreferredImageTypes(httpRequest.Headers.Get("Accept"));
                    if (formats.Length == 0)
                        formats = new[] {DefaultFormat}; // default
                }
                // OK, we have an array with preferred formats, possibly with only one entry
                byte[] response;
                foreach (string f in formats)
                {
                    if (FetchTexture(httpRequest, httpResponse, textureID, f, out response))
                        return response;
                }
            }
            else
            {
                MainConsole.Instance.Warn("[GETTEXTURE]: Failed to parse a texture_id from GetTexture request: " +
                                          httpRequest.Url);
            }

            httpResponse.StatusCode = (int) System.Net.HttpStatusCode.NotFound;
            return MainServer.BlankResponse;
        }

        /// <summary>
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <param name="httpResponse"></param>
        /// <param name="textureID"></param>
        /// <param name="format"></param>
        /// <param name="response"></param>
        /// <returns>False for "caller try another codec"; true otherwise</returns>
        private bool FetchTexture(OSHttpRequest httpRequest, OSHttpResponse httpResponse, UUID textureID, string format,
                                  out byte[] response)
        {
            //MainConsole.Instance.DebugFormat("[GETTEXTURE]: {0} with requested format {1}", textureID, format);
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
                    if (texture.Type != (sbyte) AssetType.Texture && texture.Type != (sbyte) AssetType.Unknown &&
                        texture.Type != (sbyte) AssetType.Simstate)
                    {
                        httpResponse.StatusCode = (int) System.Net.HttpStatusCode.NotFound;
                        response = MainServer.BlankResponse;
                        return true;
                    }
                    WriteTextureData(httpRequest, httpResponse, texture, format);
                }
                else
                {
                    string textureUrl = REDIRECT_URL + textureID.ToString();
                    MainConsole.Instance.Debug("[GETTEXTURE]: Redirecting texture request to " + textureUrl);
                    httpResponse.RedirectLocation = textureUrl;
                    response = MainServer.BlankResponse;
                    return true;
                }
            }
            else // no redirect
            {
                // try the cache
                texture = m_assetService.GetCached(fullID);

                if (texture == null)
                {
                    //MainConsole.Instance.DebugFormat("[GETTEXTURE]: texture was not in the cache");

                    // Fetch locally or remotely. Misses return a 404
                    texture = m_assetService.Get(textureID.ToString());

                    if (texture != null)
                    {
                        if (texture.Type != (sbyte) AssetType.Texture && texture.Type != (sbyte) AssetType.Unknown &&
                            texture.Type != (sbyte) AssetType.Simstate)
                        {
                            httpResponse.StatusCode = (int) System.Net.HttpStatusCode.NotFound;
                            response = MainServer.BlankResponse;
                            return true;
                        }
                        if (format == DefaultFormat)
                        {
                            response = WriteTextureData(httpRequest, httpResponse, texture, format);
                            texture = null;
                            return true;
                        }
                        AssetBase newTexture = new AssetBase(texture.ID + "-" + format, texture.Name, AssetType.Texture,
                                                             texture.CreatorID)
                                                   {Data = ConvertTextureData(texture, format)};
                        if (newTexture.Data.Length == 0)
                        {
                            response = MainServer.BlankResponse;
                            return false; // !!! Caller try another codec, please!
                        }

                        newTexture.Flags = AssetFlags.Collectable | AssetFlags.Temporary;
                        newTexture.ID = m_assetService.Store(newTexture);
                        response = WriteTextureData(httpRequest, httpResponse, newTexture, format);
                        newTexture = null;
                        return true;
                    }
                }
                else // it was on the cache
                {
                    if (texture.Type != (sbyte) AssetType.Texture && texture.Type != (sbyte) AssetType.Unknown &&
                        texture.Type != (sbyte) AssetType.Simstate)
                    {
                        httpResponse.StatusCode = (int) System.Net.HttpStatusCode.NotFound;
                        response = MainServer.BlankResponse;
                        return true;
                    }
                    //MainConsole.Instance.DebugFormat("[GETTEXTURE]: texture was in the cache");
                    response = WriteTextureData(httpRequest, httpResponse, texture, format);
                    texture = null;
                    return true;
                }
            }

            // not found
            MainConsole.Instance.Warn("[GETTEXTURE]: Texture " + textureID + " not found");
            httpResponse.StatusCode = (int) System.Net.HttpStatusCode.NotFound;
            response = MainServer.BlankResponse;
            return true;
        }

        private byte[] WriteTextureData(OSHttpRequest request, OSHttpResponse response, AssetBase texture, string format)
        {
            m_service.Registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler(
                "AssetRequested", new object[] {m_service.Registry, texture, m_service.AgentID});

            string range = request.Headers.GetOne("Range");
            //MainConsole.Instance.DebugFormat("[GETTEXTURE]: Range {0}", range);
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
                        response.StatusCode = (int) System.Net.HttpStatusCode.RequestedRangeNotSatisfiable;
                        return MainServer.BlankResponse;
                    }
                    else
                    {
                        end = Utils.Clamp(end, 0, texture.Data.Length - 1);
                        start = Utils.Clamp(start, 0, end);
                        int len = end - start + 1;

                        //MainConsole.Instance.Debug("Serving " + start + " to " + end + " of " + texture.Data.Length + " bytes for texture " + texture.ID);

                        if (len < texture.Data.Length)
                            response.StatusCode = (int) System.Net.HttpStatusCode.PartialContent;
                        else
                            response.StatusCode = (int) System.Net.HttpStatusCode.OK;

                        response.ContentType = texture.TypeString;
                        response.AddHeader("Content-Range",
                                           String.Format("bytes {0}-{1}/{2}", start, end, texture.Data.Length));
                        byte[] array = new byte[len];
                        Array.Copy(texture.Data, start, array, 0, len);
                        return array;
                    }
                }
                else
                {
                    MainConsole.Instance.Warn("[GETTEXTURE]: Malformed Range header: " + range);
                    response.StatusCode = (int) System.Net.HttpStatusCode.BadRequest;
                    return MainServer.BlankResponse;
                }
            }
            else // JP2's or other formats
            {
                // Full content request
                response.StatusCode = (int) System.Net.HttpStatusCode.OK;
                response.ContentType = texture.TypeString;
                if (format == DefaultFormat)
                    response.ContentType = texture.TypeString;
                else
                    response.ContentType = "image/" + format;
                return texture.Data;
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
            MainConsole.Instance.DebugFormat("[GETTEXTURE]: Converting texture {0} to {1}", texture.ID, format);
            byte[] data = new byte[0];

            MemoryStream imgstream = new MemoryStream();
            Bitmap mTexture = new Bitmap(1, 1);
            ManagedImage managedImage;
            Image image = mTexture;

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
                        MainConsole.Instance.WarnFormat("[GETTEXTURE]: No such codec {0}", format);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.WarnFormat("[GETTEXTURE]: Unable to convert texture {0} to {1}: {2}", texture.ID,
                                                format, e.Message);
            }
            finally
            {
                // Reclaim memory, these are unmanaged resources
                // If we encountered an exception, one or more of these will be null
                mTexture.Dispose();
                mTexture = null;
                managedImage = null;

                if (image != null)
                    image.Dispose();
                image = null;

                imgstream.Close();
                imgstream = null;
            }

            return data;
        }

        // From msdn
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();
            return encoders.FirstOrDefault(t => t.MimeType == mimeType);
        }

        #endregion

        #region Baked Textures

        public byte[] UploadBakedTexture(string path, Stream request, OSHttpRequest httpRequest,
                                         OSHttpResponse httpResponse)
        {
            try
            {
                //MainConsole.Instance.Debug("[CAPS]: UploadBakedTexture Request in region: " +
                //        m_regionName);

                string uploaderPath = UUID.Random().ToString();
                string uploadpath = m_service.CreateCAPS("Upload" + uploaderPath, uploaderPath);
                BakedTextureUploader uploader =
                    new BakedTextureUploader(uploadpath, "Upload" + uploaderPath,
                                             m_service);
                uploader.OnUpLoad += BakedTextureUploaded;

                m_service.AddStreamHandler(uploadpath,
                                           new GenericStreamHandler("POST", uploadpath,
                                                                    uploader.uploaderCaps));

                string uploaderURL = m_service.HostUri + uploadpath;
                OSDMap map = new OSDMap();
                map["uploader"] = uploaderURL;
                map["state"] = "upload";
                return OSDParser.SerializeLLSDXmlBytes(map);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[CAPS]: " + e);
            }

            return null;
        }

        public delegate void UploadedBakedTexture(byte[] data, out UUID newAssetID);

        public class BakedTextureUploader
        {
            public event UploadedBakedTexture OnUpLoad;
            private UploadedBakedTexture handlerUpLoad = null;

            private readonly string uploaderPath = String.Empty;
            private readonly string uploadMethod = "";
            private readonly IRegionClientCapsService clientCaps;

            public BakedTextureUploader(string path, string method, IRegionClientCapsService caps)
            {
                uploaderPath = path;
                uploadMethod = method;
                clientCaps = caps;
            }

            /// <summary>
            /// </summary>
            /// <param name="path"></param>
            /// <param name="request"></param>
            /// <param name="httpRequest"></param>
            /// <param name="httpResponse"></param>
            /// <returns></returns>
            public byte[] uploaderCaps(string path, Stream request,
                                       OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                handlerUpLoad = OnUpLoad;
                UUID newAssetID;
                handlerUpLoad(HttpServerHandlerHelpers.ReadFully(request), out newAssetID);

                OSDMap map = new OSDMap();
                map["new_asset"] = newAssetID.ToString();
                map["item_id"] = UUID.Zero;
                map["state"] = "complete";
                clientCaps.RemoveStreamHandler(uploadMethod, "POST", uploaderPath);

                return OSDParser.SerializeLLSDXmlBytes(map);
            }
        }

        public void BakedTextureUploaded(byte[] data, out UUID newAssetID)
        {
            //MainConsole.Instance.InfoFormat("[AssetCAPS]: Received baked texture {0}", assetID.ToString());
            AssetBase asset = new AssetBase(UUID.Random(), "Baked Texture", AssetType.Texture, m_service.AgentID)
                                  {Data = data, Flags = AssetFlags.Deletable | AssetFlags.Temporary};
            newAssetID = asset.ID = m_assetService.Store(asset);
            MainConsole.Instance.DebugFormat("[AssetCAPS]: Baked texture new id {0}", newAssetID.ToString());
        }

        public byte[] ProcessGetMesh(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            httpResponse.ContentType = "text/plain";

            string meshStr = string.Empty;


            if (httpRequest.QueryString["mesh_id"] != null)
                meshStr = httpRequest.QueryString["mesh_id"];


            UUID meshID = UUID.Zero;
            if (!String.IsNullOrEmpty(meshStr) && UUID.TryParse(meshStr, out meshID))
            {
                if (m_assetService == null)
                    return Encoding.UTF8.GetBytes("The asset service is unavailable.  So is your mesh.");

                // Only try to fetch locally cached textures. Misses are redirected
                AssetBase mesh = m_assetService.GetCached(meshID.ToString());
                if (mesh != null)
                {
                    if (mesh.Type == (SByte) AssetType.Mesh)
                    {
                        httpResponse.StatusCode = 200;
                        httpResponse.ContentType = "application/vnd.ll.mesh";
                        return mesh.Data;
                    }
                        // Optionally add additional mesh types here
                    else
                    {
                        httpResponse.StatusCode = 404; //501; //410; //404;
                        httpResponse.ContentType = "text/plain";
                        return Encoding.UTF8.GetBytes("Unfortunately, this asset isn't a mesh.");
                    }
                }
                else
                {
                    mesh = m_assetService.GetMesh(meshID.ToString());
                    if (mesh != null)
                    {
                        if (mesh.Type == (SByte) AssetType.Mesh)
                        {
                            httpResponse.StatusCode = 200;
                            httpResponse.ContentType = "application/vnd.ll.mesh";
                            return mesh.Data;
                        }
                            // Optionally add additional mesh types here
                        else
                        {
                            httpResponse.StatusCode = 404; //501; //410; //404;
                            httpResponse.ContentType = "text/plain";
                            return Encoding.UTF8.GetBytes("Unfortunately, this asset isn't a mesh.");
                        }
                    }

                    else
                    {
                        httpResponse.StatusCode = 404; //501; //410; //404;
                        httpResponse.ContentType = "text/plain";
                        return Encoding.UTF8.GetBytes("Your Mesh wasn't found.  Sorry!");
                    }
                }
            }

            httpResponse.StatusCode = 404;
            return Encoding.UTF8.GetBytes("Failed to find mesh");
        }

        #endregion

        #region Server Side Baked Textures

        private OpenMetaverse.AppearanceManager.TextureData[] Textures = new OpenMetaverse.AppearanceManager.TextureData[(int)AvatarTextureIndex.NumberOfEntries];
        public byte[] UpdateAvatarAppearance(string path, Stream request, OSHttpRequest httpRequest,
                                             OSHttpResponse httpResponse)
        {
            try
            {
                OSDMap rm = (OSDMap) OSDParser.DeserializeLLSDXml(request);
                int cof_version = rm["cof_version"].AsInteger();

                bool success = false;
                string error = "";
                IAvatarService avatarService = m_service.Registry.RequestModuleInterface<IAvatarService>();
                IInventoryService inventoryService = m_service.Registry.RequestModuleInterface<IInventoryService>();
                ISyncMessagePosterService syncMessage = m_service.Registry.RequestModuleInterface<ISyncMessagePosterService>();
                AvatarAppearance appearance = avatarService.GetAppearance(m_service.AgentID);
                List<BakeType> pendingBakes = new List<BakeType>();
                List<InventoryItemBase> items = inventoryService.GetFolderItems(m_service.AgentID, inventoryService.GetFolderForType(m_service.AgentID, InventoryType.Unknown, AssetType.CurrentOutfitFolder).ID);
                foreach (InventoryItemBase itm in items)
                {
                    MainConsole.Instance.Warn("[SSB]: Baking " + itm.Name);
                }
                for (int i = 0; i < Textures.Length; i++)
                {
                    Textures[i] = new AppearanceManager.TextureData();
                }
                foreach (InventoryItemBase itm in items)
                {
                    if (itm.AssetType == (int)AssetType.Link)
                    {
                        UUID assetID = inventoryService.GetItemAssetID(m_service.AgentID, itm.AssetID);
                        OpenMetaverse.AppearanceManager.WearableData wearable = new OpenMetaverse.AppearanceManager.WearableData();
                        AssetBase asset = m_assetService.Get(assetID.ToString());
                        if (asset != null && asset.TypeAsset != AssetType.Object)
                        {
                            wearable.Asset = new AssetClothing(assetID, asset.Data);
                            if (wearable.Asset.Decode())
                            {
                                wearable.AssetID = assetID;
                                wearable.AssetType = wearable.Asset.AssetType;
                                wearable.WearableType = wearable.Asset.WearableType;
                                wearable.ItemID = itm.AssetID;
                                DecodeWearableParams(wearable);
                            }
                        }
                    }
                }
                for (int i = 0; i < Textures.Length; i++)
                {
                    if (Textures[i].TextureID == UUID.Zero)
                        continue;
                    AssetBase asset = m_assetService.Get(Textures[i].TextureID.ToString());
                    if (asset != null)
                    {
                        Textures[i].Texture = new AssetTexture(Textures[i].TextureID, asset.Data);
                        Textures[i].Texture.Decode();
                    }
                }

                for (int bakedIndex = 0; bakedIndex < AppearanceManager.BAKED_TEXTURE_COUNT; bakedIndex++)
                {
                    AvatarTextureIndex textureIndex = AppearanceManager.BakeTypeToAgentTextureIndex((BakeType)bakedIndex);

                    if (Textures[(int)textureIndex].TextureID == UUID.Zero)
                    {
                        // If this is the skirt layer and we're not wearing a skirt then skip it
                        if (bakedIndex == (int)BakeType.Skirt && appearance.Wearables[(int)WearableType.Skirt].Count == 0)
                            continue;

                        pendingBakes.Add((BakeType)bakedIndex);
                    }
                }

                int start = Environment.TickCount;
                List<UUID> newBakeIDs = new List<UUID>();
                foreach (BakeType bakeType in pendingBakes)
                {
                    List<AvatarTextureIndex> textureIndices = OpenMetaverse.AppearanceManager.BakeTypeToTextures(bakeType);
                    Baker oven = new Baker(bakeType);

                    for (int i = 0; i < textureIndices.Count; i++)
                    {
                        int textureIndex = (int)textureIndices[i];
                        OpenMetaverse.AppearanceManager.TextureData texture = Textures[(int)textureIndex];
                        texture.TextureIndex = (AvatarTextureIndex)textureIndex;

                        oven.AddTexture(texture);
                    }

                    oven.Bake();
                    byte[] assetData = oven.BakedTexture.AssetData;
                    AssetBase newBakedAsset = new AssetBase(UUID.Random());
                    newBakedAsset.Data = assetData;
                    newBakedAsset.TypeAsset = AssetType.Texture;
                    newBakedAsset.Name = "SSB Texture";
                    newBakedAsset.Flags = AssetFlags.Deletable | AssetFlags.Collectable | AssetFlags.Rewritable | AssetFlags.Temporary;
                    if (appearance.Texture.FaceTextures[(int)AppearanceManager.BakeTypeToAgentTextureIndex(bakeType)].TextureID != UUID.Zero)
                        m_assetService.Delete(appearance.Texture.FaceTextures[(int)AppearanceManager.BakeTypeToAgentTextureIndex(bakeType)].TextureID);
                    UUID assetID = m_assetService.Store(newBakedAsset);
                    newBakeIDs.Add(assetID);
                    MainConsole.Instance.WarnFormat("[SSB]: Baked {0}", assetID);
                    int place = (int)AppearanceManager.BakeTypeToAgentTextureIndex(bakeType);
                    appearance.Texture.FaceTextures[place].TextureID = assetID;
                }

                MainConsole.Instance.ErrorFormat("[SSB]: Baking took {0} ms", (Environment.TickCount - start));

                appearance.Serial = cof_version+1;
                avatarService.SetAppearance(m_service.AgentID, appearance);
                OSDMap uaamap = new OSDMap();
                uaamap["Method"] = "UpdateAvatarAppearance";
                uaamap["AgentID"] = m_service.AgentID;
                uaamap["Appearance"] = appearance.ToOSD();
                syncMessage.Post(m_service.Region.ServerURI, uaamap);
                success = true;

                OSDMap map = new OSDMap();
                map["success"] = success;
                map["error"] = error;
                map["agent_id"] = m_service.AgentID;
                map["avatar_scale"] = appearance.AvatarHeight;
                map["textures"] = newBakeIDs.ToOSDArray();
                OSDArray visualParams = new OSDArray();
                foreach(byte b in appearance.VisualParams)
                    visualParams.Add((int)b);
                map["visual_params"] = visualParams;
                return OSDParser.SerializeLLSDXmlBytes(map);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[CAPS]: " + e);
            }

            return null;
        }

        /// <summary>
        /// Populates textures and visual params from a decoded asset
        /// </summary>
        /// <param name="wearable">Wearable to decode</param>
        private void DecodeWearableParams(OpenMetaverse.AppearanceManager.WearableData wearable)
        {
            Dictionary<VisualAlphaParam, float> alphaMasks = new Dictionary<VisualAlphaParam, float>();
            List<ColorParamInfo> colorParams = new List<ColorParamInfo>();

            // Populate collection of alpha masks from visual params
            // also add color tinting information
            foreach (KeyValuePair<int, float> kvp in wearable.Asset.Params)
            {
                if (!VisualParams.Params.ContainsKey(kvp.Key)) continue;

                VisualParam p = VisualParams.Params[kvp.Key];

                ColorParamInfo colorInfo = new ColorParamInfo();
                colorInfo.WearableType = wearable.WearableType;
                colorInfo.VisualParam = p;
                colorInfo.Value = kvp.Value;

                // Color params
                if (p.ColorParams.HasValue)
                {
                    colorInfo.VisualColorParam = p.ColorParams.Value;

                    // If this is not skin, just add params directly
                    if (wearable.WearableType != WearableType.Skin)
                    {
                        colorParams.Add(colorInfo);
                    }
                    else
                    {
                        // For skin we skip makeup params for now and use only the 3
                        // that are used to determine base skin tone
                        // Param 108 - Rainbow Color
                        // Param 110 - Red Skin (Ruddiness)
                        // Param 111 - Pigment
                        if (kvp.Key == 108 || kvp.Key == 110 || kvp.Key == 111)
                        {
                            colorParams.Add(colorInfo);
                        }
                    }
                }

                // Add alpha mask
                if (p.AlphaParams.HasValue && p.AlphaParams.Value.TGAFile != string.Empty && !p.IsBumpAttribute && !alphaMasks.ContainsKey(p.AlphaParams.Value))
                {
                    alphaMasks.Add(p.AlphaParams.Value, kvp.Value);
                }

                // Alhpa masks can also be specified in sub "driver" params
                if (p.Drivers != null)
                {
                    for (int i = 0; i < p.Drivers.Length; i++)
                    {
                        if (VisualParams.Params.ContainsKey(p.Drivers[i]))
                        {
                            VisualParam driver = VisualParams.Params[p.Drivers[i]];
                            if (driver.AlphaParams.HasValue && driver.AlphaParams.Value.TGAFile != string.Empty && !driver.IsBumpAttribute && !alphaMasks.ContainsKey(driver.AlphaParams.Value))
                            {
                                alphaMasks.Add(driver.AlphaParams.Value, kvp.Value);
                            }
                        }
                    }
                }
            }

            Color4 wearableColor = Color4.White; // Never actually used
            if (colorParams.Count > 0)
            {
                wearableColor = GetColorFromParams(colorParams);
                Logger.DebugLog("Setting tint " + wearableColor + " for " + wearable.WearableType);
            }

            // Loop through all of the texture IDs in this decoded asset and put them in our cache of worn textures
            foreach (KeyValuePair<AvatarTextureIndex, UUID> entry in wearable.Asset.Textures)
            {
                int i = (int)entry.Key;

                // Update information about color and alpha masks for this texture
                Textures[i].AlphaMasks = alphaMasks;
                Textures[i].Color = wearableColor;

                // If this texture changed, update the TextureID and clear out the old cached texture asset
                if (Textures[i].TextureID != entry.Value)
                {
                    // Treat DEFAULT_AVATAR_TEXTURE as null
                    if (entry.Value != AppearanceManager.DEFAULT_AVATAR_TEXTURE)
                        Textures[i].TextureID = entry.Value;
                    else
                        Textures[i].TextureID = UUID.Zero;

                    Textures[i].Texture = null;
                }
            }
        }

        /// <summary>
        /// Calculates base color/tint for a specific wearable
        /// based on its params
        /// </summary>
        /// <param name="param">All the color info gathered from wearable's VisualParams
        /// passed as list of ColorParamInfo tuples</param>
        /// <returns>Base color/tint for the wearable</returns>
        private Color4 GetColorFromParams(List<ColorParamInfo> param)
        {
            // Start off with a blank slate, black, fully transparent
            Color4 res = new Color4(0, 0, 0, 0);

            // Apply color modification from each color parameter
            foreach (ColorParamInfo p in param)
            {
                int n = p.VisualColorParam.Colors.Length;

                Color4 paramColor = new Color4(0, 0, 0, 0);

                if (n == 1)
                {
                    // We got only one color in this param, use it for application
                    // to the final color
                    paramColor = p.VisualColorParam.Colors[0];
                }
                else if (n > 1)
                {
                    // We have an array of colors in this parameter
                    // First, we need to find out, based on param value
                    // between which two elements of the array our value lands

                    // Size of the step using which we iterate from Min to Max
                    float step = (p.VisualParam.MaxValue - p.VisualParam.MinValue) / ((float)n - 1);

                    // Our color should land inbetween colors in the array with index a and b
                    int indexa = 0;
                    int indexb = 0;

                    int i = 0;

                    for (float a = p.VisualParam.MinValue; a <= p.VisualParam.MaxValue; a += step)
                    {
                        if (a <= p.Value)
                        {
                            indexa = i;
                        }
                        else
                        {
                            break;
                        }

                        i++;
                    }

                    // Sanity check that we don't go outside bounds of the array
                    if (indexa > n - 1)
                        indexa = n - 1;

                    indexb = (indexa == n - 1) ? indexa : indexa + 1;

                    // How far is our value from Index A on the 
                    // line from Index A to Index B
                    float distance = p.Value - (float)indexa * step;

                    // We are at Index A (allowing for some floating point math fuzz),
                    // use the color on that index
                    if (distance < 0.00001f || indexa == indexb)
                    {
                        paramColor = p.VisualColorParam.Colors[indexa];
                    }
                    else
                    {
                        // Not so simple as being precisely on the index eh? No problem.
                        // We take the two colors that our param value places us between
                        // and then find the value for each ARGB element that is
                        // somewhere on the line between color1 and color2 at some
                        // distance from the first color
                        Color4 c1 = paramColor = p.VisualColorParam.Colors[indexa];
                        Color4 c2 = paramColor = p.VisualColorParam.Colors[indexb];

                        // Distance is some fraction of the step, use that fraction
                        // to find the value in the range from color1 to color2
                        paramColor = Color4.Lerp(c1, c2, distance / step);
                    }

                    // Please leave this fragment even if its commented out
                    // might prove useful should ($deity forbid) there be bugs in this code
                    //string carray = "";
                    //foreach (Color c in p.VisualColorParam.Colors)
                    //{
                    //    carray += c.ToString() + " - ";
                    //}
                    //Logger.DebugLog("Calculating color for " + p.WearableType + " from " + p.VisualParam.Name + ", value is " + p.Value + " in range " + p.VisualParam.MinValue + " - " + p.VisualParam.MaxValue + " step " + step + " with " + n + " elements " + carray + " A: " + indexa + " B: " + indexb + " at distance " + distance);
                }

                // Now that we have calculated color from the scale of colors
                // that visual params provided, lets apply it to the result
                switch (p.VisualColorParam.Operation)
                {
                    case VisualColorOperation.Add:
                        res += paramColor;
                        break;
                    case VisualColorOperation.Multiply:
                        res *= paramColor;
                        break;
                    case VisualColorOperation.Blend:
                        res = Color4.Lerp(res, paramColor, p.Value);
                        break;
                }
            }

            return res;
        }

        /// <summary>
        /// Data collected from visual params for each wearable
        /// needed for the calculation of the color
        /// </summary>
        private struct ColorParamInfo
        {
            public VisualParam VisualParam;
            public VisualColorParam VisualColorParam;
            public float Value;
            public WearableType WearableType;
        }

        #endregion
    }
}