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
using System.IO;
using System.Reflection;
using log4net;
using OpenMetaverse;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Framework.Capabilities
{
    public delegate void UpLoadedAsset(
        string assetName, string description, UUID assetID, UUID inventoryItem, UUID parentFolder,
        byte[] data, string inventoryType, string assetType);

    public delegate string UpdateItem(UUID itemID, byte[] data);

    public delegate void UpdateTaskScript(UUID itemID, UUID primID, bool isScriptRunning, byte[] data, ref ArrayList errors);

    public delegate void NewInventoryItem(UUID userID, InventoryItemBase item);

    public delegate void NewAsset(AssetBase asset);

    public delegate void UploadedBakedTexture(UUID assetID, byte[] data);

    public delegate string ItemUpdatedCallback(UUID userID, UUID itemID, byte[] data);

    public delegate ArrayList TaskScriptUpdatedCallback(UUID userID, UUID itemID, UUID primID,
                                                   bool isScriptRunning, byte[] data);

    public delegate InventoryCollection FetchInventoryDescendentsCAPS(UUID agentID, UUID folderID, UUID ownerID,
                                                                          bool fetchFolders, bool fetchItems, int sortOrder, out int version);
    /// <summary>
    /// XXX Probably not a particularly nice way of allow us to get the scene presence from the scene (chiefly so that
    /// we can popup a message on the user's client if the inventory service has permanently failed).  But I didn't want
    /// to just pass the whole Scene into CAPS.
    /// </summary>
    public delegate IClientAPI GetClientDelegate(UUID agentID);

    public class Caps
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string m_httpListenerHostName;
        private uint m_httpListenPort;

        /// <summary>
        /// This is the uuid portion of every CAPS path.  It is used to make capability urls private to the requester.
        /// </summary>
        private string m_capsObjectPath;
        public string CapsObjectPath { get { return m_capsObjectPath; } }

        private CapsHandlers m_capsHandlers;

        private static readonly string m_requestPath = "0000/";
        // private static readonly string m_mapLayerPath = "0001/";
        private static readonly string m_newInventory = "0002/";
        //private static readonly string m_requestTexture = "0003/";
        private static readonly string m_notecardUpdatePath = "0004/";
        private static readonly string m_notecardTaskUpdatePath = "0005/";
        //private static readonly string m_fetchInventoryPath = "0006/";


        // The following entries are in a module, however, they are also here so that we don't re-assign
        // the path to another cap by mistake.
        // private static readonly string m_parcelVoiceInfoRequestPath = "0007/"; // This is in a module.
        // private static readonly string m_provisionVoiceAccountRequestPath = "0008/";// This is in a module.

        // private static readonly string m_remoteParcelRequestPath = "0009/";// This is in the LandManagementModule.

        private static readonly string m_uploadBakedTexturePath = "0010/";
        //private string eventQueue = "0100/";
        private IScene m_Scene;
        private IHttpServer m_httpListener;
        private UUID m_agentID;
        private IAssetService m_assetCache;
        private int m_eventQueueCount = 1;
        private Queue<string> m_capsEventQueue = new Queue<string>();
        private string m_regionName;
        private object m_fetchLock = new Object();

        public bool SSLCaps
        {
            get { return m_httpListener.UseSSL; }
        }
        public string SSLCommonName
        {
            get { return m_httpListener.SSLCommonName; }
        }
        public CapsHandlers CapsHandlers
        {
            get { return m_capsHandlers; }
        }

        // These are callbacks which will be setup by the scene so that we can update scene data when we
        // receive capability calls
        public NewInventoryItem AddNewInventoryItem = null;
        public NewAsset AddNewAsset = null;
        public ItemUpdatedCallback ItemUpdatedCall = null;
        public TaskScriptUpdatedCallback TaskScriptUpdatedCall = null;
        public FetchInventoryDescendentsCAPS CAPSFetchInventoryDescendents = null;
        public GetClientDelegate GetClient = null;

        public Caps(IScene scene, IAssetService assetCache, IHttpServer httpServer, string httpListen, uint httpPort, string capsPath,
                    UUID agent, string regionName)
        {
            m_Scene = scene;
            m_assetCache = assetCache;
            m_capsObjectPath = capsPath;
            m_httpListener = httpServer;
            m_httpListenerHostName = httpListen;

            m_httpListenPort = httpPort;

            if (httpServer.UseSSL)
            {
                m_httpListenPort = httpServer.SSLPort;
                httpListen = httpServer.SSLCommonName;
                httpPort = httpServer.SSLPort;
            }

            m_agentID = agent;
            m_capsHandlers = new CapsHandlers(httpServer, httpListen, httpPort, httpServer.UseSSL);
            m_regionName = regionName;
        }

        /// <summary>
        /// Register all CAPS http service handlers
        /// </summary>
        public void RegisterHandlers()
        {
            DeregisterHandlers();

            string capsBase = "/CAPS/" + m_capsObjectPath;

            RegisterRegionServiceHandlers(capsBase);
            RegisterInventoryServiceHandlers(capsBase);

        }

        public void RegisterRegionServiceHandlers(string capsBase)
        {
            try
            {
                // the root of all evil
                m_capsHandlers["SEED"] = new RestStreamHandler("POST", capsBase + m_requestPath, CapsRequest);

                m_capsHandlers["UploadBakedTexture"] =
                    new RestStreamHandler("POST", capsBase + m_uploadBakedTexturePath, UploadBakedTexture);

                m_capsHandlers["UpdateScriptTaskInventory"] =
                    new RestStreamHandler("POST", capsBase + m_notecardTaskUpdatePath, ScriptTaskInventory);
                m_capsHandlers["UpdateScriptTask"] = m_capsHandlers["UpdateScriptTaskInventory"];

            }
            catch (Exception e)
            {
                m_log.Error("[CAPS]: " + e.ToString());
            }
        }

        public void RegisterInventoryServiceHandlers(string capsBase)
        {
            try
            {
                // I don't think this one works...
                m_capsHandlers["NewFileAgentInventory"] =
                    new LLSDStreamhandler<LLSDAssetUploadRequest, LLSDAssetUploadResponse>("POST",
                                                                                           capsBase + m_newInventory,
                                                                                           NewAgentInventoryRequest);
                m_capsHandlers["UpdateNotecardAgentInventory"] =
                    new RestStreamHandler("POST", capsBase + m_notecardUpdatePath, NoteCardAgentInventory);
                
                m_capsHandlers["UpdateScriptAgentInventory"] = m_capsHandlers["UpdateNotecardAgentInventory"];
                m_capsHandlers["UpdateScriptAgent"] = m_capsHandlers["UpdateScriptAgentInventory"];
                
                //
                //START: MOVED TO THE CAPS SERVICE
                //

                // As of RC 1.22.9 of the Linden client this is
                // supported

                //m_capsHandlers["WebFetchInventoryDescendents"] =
                //    new RestStreamHandler("POST", capsBase + m_fetchInventoryPath, FetchInventoryDescendentsRequest);

                // justincc: I've disabled the CAPS service for now to fix problems with selecting textures, and
                // subsequent inventory breakage, in the edit object pane (such as mantis 1085).  This requires
                // enhancements (probably filling out the folder part of the LLSD reply) to our CAPS service,
                // but when I went on the Linden grid, the
                // simulators I visited (version 1.21) were, surprisingly, no longer supplying this capability.  Instead,
                // the 1.19.1.4 client appeared to be happily flowing inventory data over UDP
                //
                // This is very probably just a temporary measure - once the CAPS service appears again on the Linden grid
                // we will be
                // able to get the data we need to implement the necessary part of the protocol to fix the issue above.

                //Reenabled
                //m_capsHandlers["FetchInventoryDescendents"] =
                //       new RestStreamHandler("POST", capsBase + m_fetchInventoryPath, FetchInventoryRequest);

                 //m_capsHandlers["RequestTextureDownload"] = new RestStreamHandler("POST",
                 //                                                                 capsBase + m_requestTexture,
                 //                                                                 RequestTexture);


                //
                //END: MOVED TO THE CAPS SERVICE
                //
            }
            catch (Exception e)
            {
                m_log.Error("[CAPS]: " + e.ToString());
            }
        }

        /// <summary>
        /// Register a handler.  This allows modules to register handlers.
        /// </summary>
        /// <param name="capName"></param>
        /// <param name="handler"></param>
        public void RegisterHandler(string capName, IRequestHandler handler)
        {
            m_capsHandlers[capName] = handler;
        }

        /// <summary>
        /// Remove all CAPS service handlers.
        ///
        /// </summary>
        /// <param name="httpListener"></param>
        /// <param name="path"></param>
        /// <param name="restMethod"></param>
        public void DeregisterHandlers()
        {
            if (m_capsHandlers != null)
            {
                foreach (string capsName in m_capsHandlers.Caps)
                {
                    m_capsHandlers.Remove(capsName);
                }
            }
        }

        /// <summary>
        /// Construct a client response detailing all the capabilities this server can provide.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="httpRequest">HTTP request header object</param>
        /// <param name="httpResponse">HTTP response header object</param>
        /// <returns></returns>
        public string CapsRequest(string request, string path, string param,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            //m_log.Debug("[CAPS]: Seed Caps Request in region: " + m_regionName);

            if (!m_Scene.CheckClient(m_agentID, httpRequest.RemoteIPEndPoint))
            {
                m_log.DebugFormat("[CAPS]: Unauthorized CAPS client");
                return string.Empty;
            }

            string result = LLSDHelpers.SerialiseLLSDReply(m_capsHandlers.CapsDetails);

            //m_log.DebugFormat("[CAPS] CapsRequest {0}", result);

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="mapReq"></param>
        /// <returns></returns>
        public LLSDMapLayerResponse GetMapLayer(LLSDMapRequest mapReq)
        {
            m_log.Debug("[CAPS]: MapLayer Request in region: " + m_regionName);
            LLSDMapLayerResponse mapResponse = new LLSDMapLayerResponse();
            mapResponse.LayerData.Array.Add(GetOSDMapLayerResponse());
            return mapResponse;
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public string RequestTexture(string request, string path, string param)
        {
            m_log.Debug("texture request " + request);
            // Needs implementing (added to remove compiler warning)
            return String.Empty;
        }

        #region EventQueue (Currently not enabled)

        /// <summary>
        ///
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public string ProcessEventQueue(string request, string path, string param)
        {
            string res = String.Empty;

            if (m_capsEventQueue.Count > 0)
            {
                lock (m_capsEventQueue)
                {
                    string item = m_capsEventQueue.Dequeue();
                    res = item;
                }
            }
            else
            {
                res = CreateEmptyEventResponse();
            }
            return res;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="caps"></param>
        /// <param name="ipAddressPort"></param>
        /// <returns></returns>
        public string CreateEstablishAgentComms(string caps, string ipAddressPort)
        {
            LLSDCapEvent eventItem = new LLSDCapEvent();
            eventItem.id = m_eventQueueCount;
            //should be creating a EstablishAgentComms item, but there isn't a class for it yet
            eventItem.events.Array.Add(new LLSDEmpty());
            string res = LLSDHelpers.SerialiseLLSDReply(eventItem);
            m_eventQueueCount++;

            m_capsEventQueue.Enqueue(res);
            return res;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string CreateEmptyEventResponse()
        {
            LLSDCapEvent eventItem = new LLSDCapEvent();
            eventItem.id = m_eventQueueCount;
            eventItem.events.Array.Add(new LLSDEmpty());
            string res = LLSDHelpers.SerialiseLLSDReply(eventItem);
            m_eventQueueCount++;
            return res;
        }

        #endregion

        /// <summary>
        /// Called by the script task update handler.  Provides a URL to which the client can upload a new asset.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="httpRequest">HTTP request header object</param>
        /// <param name="httpResponse">HTTP response header object</param>
        /// <returns></returns>
        public string ScriptTaskInventory(string request, string path, string param,
                                          OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            try
            {
                m_log.Debug("[CAPS]: ScriptTaskInventory Request in region: " + m_regionName);
                //m_log.DebugFormat("[CAPS]: request: {0}, path: {1}, param: {2}", request, path, param);

                Hashtable hash = (Hashtable) LLSD.LLSDDeserialize(Utils.StringToBytes(request));
                LLSDTaskScriptUpdate llsdUpdateRequest = new LLSDTaskScriptUpdate();
                LLSDHelpers.DeserialiseOSDMap(hash, llsdUpdateRequest);

                string capsBase = "/CAPS/" + m_capsObjectPath;
                string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");

                TaskInventoryScriptUpdater uploader =
                    new TaskInventoryScriptUpdater(
                        llsdUpdateRequest.item_id,
                        llsdUpdateRequest.task_id,
                        llsdUpdateRequest.is_script_running,
                        capsBase + uploaderPath,
                        m_httpListener);
                uploader.OnUpLoad += TaskScriptUpdated;

                m_httpListener.AddStreamHandler(
                    new BinaryStreamHandler("POST", capsBase + uploaderPath, uploader.uploaderCaps));

                string protocol = "http://";

                if (m_httpListener.UseSSL)
                    protocol = "https://";

                string uploaderURL = protocol + m_httpListenerHostName + ":" + m_httpListenPort.ToString() + capsBase +
                                     uploaderPath;

                LLSDAssetUploadResponse uploadResponse = new LLSDAssetUploadResponse();
                uploadResponse.uploader = uploaderURL;
                uploadResponse.state = "upload";

//                m_log.InfoFormat("[CAPS]: " +
//                                 "ScriptTaskInventory response: {0}",
//                                 LLSDHelpers.SerialiseLLSDReply(uploadResponse)));

                return LLSDHelpers.SerialiseLLSDReply(uploadResponse);
            }
            catch (Exception e)
            {
                m_log.Error("[CAPS]: " + e.ToString());
            }

            return null;
        }

        /// <summary>
        /// Called by the notecard update handler.  Provides a URL to which the client can upload a new asset.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public string NoteCardAgentInventory(string request, string path, string param,
                                             OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            //m_log.Debug("[CAPS]: NoteCardAgentInventory Request in region: " + m_regionName + "\n" + request);
            //m_log.Debug("[CAPS]: NoteCardAgentInventory Request is: " + request);
            
            //OpenMetaverse.StructuredData.OSDMap hash = (OpenMetaverse.StructuredData.OSDMap)OpenMetaverse.StructuredData.LLSDParser.DeserializeBinary(Utils.StringToBytes(request));
            Hashtable hash = (Hashtable) LLSD.LLSDDeserialize(Utils.StringToBytes(request));
            LLSDItemUpdate llsdRequest = new LLSDItemUpdate();
            LLSDHelpers.DeserialiseOSDMap(hash, llsdRequest);

            string capsBase = "/CAPS/" + m_capsObjectPath;
            string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");

            ItemUpdater uploader =
                new ItemUpdater(llsdRequest.item_id, capsBase + uploaderPath, m_httpListener);
            uploader.OnUpLoad += ItemUpdated;

            m_httpListener.AddStreamHandler(
                new BinaryStreamHandler("POST", capsBase + uploaderPath, uploader.uploaderCaps));

            string protocol = "http://";

            if (m_httpListener.UseSSL)
                protocol = "https://";

            string uploaderURL = protocol + m_httpListenerHostName + ":" + m_httpListenPort.ToString() + capsBase +
                                 uploaderPath;

            LLSDAssetUploadResponse uploadResponse = new LLSDAssetUploadResponse();
            uploadResponse.uploader = uploaderURL;
            uploadResponse.state = "upload";

//            m_log.InfoFormat("[CAPS]: " +
//                             "NoteCardAgentInventory response: {0}",
//                             LLSDHelpers.SerialiseLLSDReply(uploadResponse)));

            return LLSDHelpers.SerialiseLLSDReply(uploadResponse);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="llsdRequest"></param>
        /// <returns></returns>
        public LLSDAssetUploadResponse NewAgentInventoryRequest(LLSDAssetUploadRequest llsdRequest)
        {
            //m_log.Debug("[CAPS]: NewAgentInventoryRequest Request is: " + llsdRequest.ToString());
            //m_log.Debug("asset upload request via CAPS" + llsdRequest.inventory_type + " , " + llsdRequest.asset_type);

            if (llsdRequest.asset_type == "texture" ||
                llsdRequest.asset_type == "animation" ||
                llsdRequest.asset_type == "sound")
            {
                IClientAPI client = null;
                IScene scene = null;
                if (GetClient != null)
                {
                    client = GetClient(m_agentID);
                    scene = client.Scene;

                    IMoneyModule mm = scene.RequestModuleInterface<IMoneyModule>();

                    if (mm != null)
                    {
                        if (!mm.UploadCovered(client, mm.UploadCharge))
                        {
                            if (client != null)
                                client.SendAgentAlertMessage("Unable to upload asset. Insufficient funds.", false);

                            LLSDAssetUploadResponse errorResponse = new LLSDAssetUploadResponse();
                            errorResponse.uploader = "";
                            errorResponse.state = "error";
                            return errorResponse;
                        }
                    }
                }
            }


            string assetName = llsdRequest.name;
            string assetDes = llsdRequest.description;
            string capsBase = "/CAPS/" + m_capsObjectPath;
            UUID newAsset = UUID.Random();
            UUID newInvItem = UUID.Random();
            UUID parentFolder = llsdRequest.folder_id;
            string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");

            AssetUploader uploader =
                new AssetUploader(assetName, assetDes, newAsset, newInvItem, parentFolder, llsdRequest.inventory_type,
                                  llsdRequest.asset_type, capsBase + uploaderPath, m_httpListener);
            m_httpListener.AddStreamHandler(
                new BinaryStreamHandler("POST", capsBase + uploaderPath, uploader.uploaderCaps));

            string protocol = "http://";

            if (m_httpListener.UseSSL)
                protocol = "https://";

            string uploaderURL = protocol + m_httpListenerHostName + ":" + m_httpListenPort.ToString() + capsBase +
                                 uploaderPath;

            LLSDAssetUploadResponse uploadResponse = new LLSDAssetUploadResponse();
            uploadResponse.uploader = uploaderURL;
            uploadResponse.state = "upload";
            uploader.OnUpLoad += UploadCompleteHandler;
            return uploadResponse;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="assetID"></param>
        /// <param name="inventoryItem"></param>
        /// <param name="data"></param>
        public void UploadCompleteHandler(string assetName, string assetDescription, UUID assetID,
                                          UUID inventoryItem, UUID parentFolder, byte[] data, string inventoryType,
                                          string assetType)
        {
            sbyte assType = 0;
            sbyte inType = 0;

            if (inventoryType == "sound")
            {
                inType = 1;
                assType = 1;
            }
            else if (inventoryType == "animation")
            {
                inType = 19;
                assType = 20;
            }
            else if (inventoryType == "wearable")
            {
                inType = 18;
                switch (assetType)
                {
                    case "bodypart":
                        assType = 13;
                        break;
                    case "clothing":
                        assType = 5;
                        break;
                }
            }

            AssetBase asset;
            asset = new AssetBase(assetID, assetName, assType, m_agentID.ToString());
            asset.Data = data;
            if (AddNewAsset != null)
                AddNewAsset(asset);
            else if (m_assetCache != null)
                m_assetCache.Store(asset);

            InventoryItemBase item = new InventoryItemBase();
            item.Owner = m_agentID;
            item.CreatorId = m_agentID.ToString();
            item.ID = inventoryItem;
            item.AssetID = asset.FullID;
            item.Description = assetDescription;
            item.Name = assetName;
            item.AssetType = assType;
            item.InvType = inType;
            item.Folder = parentFolder;
            item.CurrentPermissions = 2147483647;
            item.BasePermissions = 2147483647;
            item.EveryOnePermissions = 0;
            item.NextPermissions = 2147483647;
            item.CreationDate = Util.UnixTimeSinceEpoch();

            if (AddNewInventoryItem != null)
            {
                AddNewInventoryItem(m_agentID, item);
            }
        }

        /// <summary>
        /// Called when new asset data for an agent inventory item update has been uploaded.
        /// </summary>
        /// <param name="itemID">Item to update</param>
        /// <param name="data">New asset data</param>
        /// <returns></returns>
        public string ItemUpdated(UUID itemID, byte[] data)
        {
            if (ItemUpdatedCall != null)
            {
                return ItemUpdatedCall(m_agentID, itemID, data);
            }

            return "";
        }

        public string UploadBakedTexture(string request, string path,
                string param, OSHttpRequest httpRequest,
                OSHttpResponse httpResponse)
        {
            try
            {
                //m_log.Debug("[CAPS]: UploadBakedTexture Request in region: " +
                //        m_regionName);

                string capsBase = "/CAPS/" + m_capsObjectPath;
                string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");

                BakedTextureUploader uploader =
                    new BakedTextureUploader( capsBase + uploaderPath,
                        m_httpListener);
                uploader.OnUpLoad += BakedTextureUploaded;

                m_httpListener.AddStreamHandler(
                        new BinaryStreamHandler("POST", capsBase + uploaderPath,
                        uploader.uploaderCaps));

                string protocol = "http://";

                if (m_httpListener.UseSSL)
                    protocol = "https://";

                string uploaderURL = protocol + m_httpListenerHostName + ":" +
                        m_httpListenPort.ToString() + capsBase + uploaderPath;

                LLSDAssetUploadResponse uploadResponse =
                        new LLSDAssetUploadResponse();
                uploadResponse.uploader = uploaderURL;
                uploadResponse.state = "upload";

                return LLSDHelpers.SerialiseLLSDReply(uploadResponse);
            }
            catch (Exception e)
            {
                m_log.Error("[CAPS]: " + e.ToString());
            }

            return null;
        }

        public void BakedTextureUploaded(UUID assetID, byte[] data)
        {
            m_log.DebugFormat("[CAPS]: Received baked texture {0}", assetID.ToString());
            IJ2KDecoder j2kDecoder = GetClient(m_agentID).Scene.RequestModuleInterface<IJ2KDecoder>();
            if (j2kDecoder != null)
            {
                if (!j2kDecoder.Decode(assetID, data))
                {
                    //Uhoh, bad upload, rerequest them from the client
                    m_log.DebugFormat("[CAPS]: Received corrupted baked texture {0}", assetID.ToString());
                    GetClient(m_agentID).SendRebakeAvatarTextures(assetID);
                    return;
                }
            }
            AssetBase asset;
            asset = new AssetBase(assetID, "Baked Texture", (sbyte)AssetType.Texture, m_agentID.ToString());
            asset.Data = data;
            asset.Temporary = true;
            asset.Local = true;
            m_assetCache.Store(asset);
        }


        /// <summary>
        /// Called when new asset data for an agent inventory item update has been uploaded.
        /// </summary>
        /// <param name="itemID">Item to update</param>
        /// <param name="primID">Prim containing item to update</param>
        /// <param name="isScriptRunning">Signals whether the script to update is currently running</param>
        /// <param name="data">New asset data</param>
        public void TaskScriptUpdated(UUID itemID, UUID primID, bool isScriptRunning, byte[] data, ref ArrayList errors)
        {
            if (TaskScriptUpdatedCall != null)
            {
                ArrayList e = TaskScriptUpdatedCall(m_agentID, itemID, primID, isScriptRunning, data);
                foreach (Object item in e)
                    errors.Add(item);
            }
        }

        public class BakedTextureUploader
        {
            public event UploadedBakedTexture OnUpLoad;
            private UploadedBakedTexture handlerUpLoad = null;

            private string uploaderPath = String.Empty;
            private UUID newAssetID;
            private IHttpServer httpListener;

            public BakedTextureUploader(string path, IHttpServer httpServer)
            {
                newAssetID = UUID.Random();
                uploaderPath = path;
                httpListener = httpServer;
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
                LLSDAssetUploadComplete uploadComplete = new LLSDAssetUploadComplete();
                uploadComplete.new_asset = newAssetID.ToString();
                uploadComplete.new_inventory_item = UUID.Zero;
                uploadComplete.state = "complete";

                res = LLSDHelpers.SerialiseLLSDReply(uploadComplete);

                httpListener.RemoveStreamHandler("POST", uploaderPath);

                handlerUpLoad = OnUpLoad;
                if (handlerUpLoad != null)
                {
                    handlerUpLoad(newAssetID, data);
                }

                return res;
            }
        }


        public class AssetUploader
        {
            public event UpLoadedAsset OnUpLoad;
            private UpLoadedAsset handlerUpLoad = null;

            private string uploaderPath = String.Empty;
            private UUID newAssetID;
            private UUID inventoryItemID;
            private UUID parentFolder;
            private IHttpServer httpListener;
            private string m_assetName = String.Empty;
            private string m_assetDes = String.Empty;

            private string m_invType = String.Empty;
            private string m_assetType = String.Empty;

            public AssetUploader(string assetName, string description, UUID assetID, UUID inventoryItem,
                                 UUID parentFolderID, string invType, string assetType, string path,
                                 IHttpServer httpServer)
            {
                m_assetName = assetName;
                m_assetDes = description;
                newAssetID = assetID;
                inventoryItemID = inventoryItem;
                uploaderPath = path;
                httpListener = httpServer;
                parentFolder = parentFolderID;
                m_assetType = assetType;
                m_invType = invType;
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
                UUID inv = inventoryItemID;
                string res = String.Empty;
                LLSDAssetUploadComplete uploadComplete = new LLSDAssetUploadComplete();
                uploadComplete.new_asset = newAssetID.ToString();
                uploadComplete.new_inventory_item = inv;
                uploadComplete.state = "complete";

                res = LLSDHelpers.SerialiseLLSDReply(uploadComplete);

                httpListener.RemoveStreamHandler("POST", uploaderPath);

                handlerUpLoad = OnUpLoad;
                if (handlerUpLoad != null)
                {
                    handlerUpLoad(m_assetName, m_assetDes, newAssetID, inv, parentFolder, data, m_invType, m_assetType);
                }

                return res;
            }
            ///Left this in and commented in case there are unforseen issues
            //private void SaveAssetToFile(string filename, byte[] data)
            //{
            //    FileStream fs = File.Create(filename);
            //    BinaryWriter bw = new BinaryWriter(fs);
            //    bw.Write(data);
            //    bw.Close();
            //    fs.Close();
            //}
            private static void SaveAssetToFile(string filename, byte[] data)
            {
                string assetPath = "UserAssets";
                if (!Directory.Exists(assetPath))
                {
                    Directory.CreateDirectory(assetPath);
                }
                FileStream fs = File.Create(Path.Combine(assetPath, Util.safeFileName(filename)));
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(data);
                bw.Close();
                fs.Close();
            }
        }

        /// <summary>
        /// This class is a callback invoked when a client sends asset data to
        /// an agent inventory notecard update url
        /// </summary>
        public class ItemUpdater
        {
            public event UpdateItem OnUpLoad;

            private UpdateItem handlerUpdateItem = null;

            private string uploaderPath = String.Empty;
            private UUID inventoryItemID;
            private IHttpServer httpListener;

            public ItemUpdater(UUID inventoryItem, string path, IHttpServer httpServer)
            {
                inventoryItemID = inventoryItem;
                uploaderPath = path;
                httpListener = httpServer;
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
                UUID inv = inventoryItemID;
                string res = String.Empty;
                handlerUpdateItem = OnUpLoad;
                if (handlerUpdateItem != null)
                {
                    res = handlerUpdateItem(inv, data);
                }

                httpListener.RemoveStreamHandler("POST", uploaderPath);

                return res;
            }
            ///Left this in and commented in case there are unforseen issues
            //private void SaveAssetToFile(string filename, byte[] data)
            //{
            //    FileStream fs = File.Create(filename);
            //    BinaryWriter bw = new BinaryWriter(fs);
            //    bw.Write(data);
            //    bw.Close();
            //    fs.Close();
            //}
            private static void SaveAssetToFile(string filename, byte[] data)
            {
                string assetPath = "UserAssets";
                if (!Directory.Exists(assetPath))
                {
                    Directory.CreateDirectory(assetPath);
                }
                FileStream fs = File.Create(Path.Combine(assetPath, filename));
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(data);
                bw.Close();
                fs.Close();
            }
        }

        /// <summary>
        /// This class is a callback invoked when a client sends asset data to
        /// a task inventory script update url
        /// </summary>
        public class TaskInventoryScriptUpdater
        {
            public event UpdateTaskScript OnUpLoad;

            private UpdateTaskScript handlerUpdateTaskScript = null;

            private string uploaderPath = String.Empty;
            private UUID inventoryItemID;
            private UUID primID;
            private bool isScriptRunning;
            private IHttpServer httpListener;

            public TaskInventoryScriptUpdater(UUID inventoryItemID, UUID primID, int isScriptRunning,
                                              string path, IHttpServer httpServer)
            {

                this.inventoryItemID = inventoryItemID;
                this.primID = primID;

                // This comes in over the packet as an integer, but actually appears to be treated as a bool
                this.isScriptRunning = (0 == isScriptRunning ? false : true);

                uploaderPath = path;
                httpListener = httpServer;
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
                try
                {
//                    m_log.InfoFormat("[CAPS]: " +
//                                     "TaskInventoryScriptUpdater received data: {0}, path: {1}, param: {2}",
//                                     data, path, param));

                    string res = String.Empty;
                    LLSDTaskScriptUploadComplete uploadComplete = new LLSDTaskScriptUploadComplete();

                    ArrayList errors = new ArrayList();
                    handlerUpdateTaskScript = OnUpLoad;
                    if (handlerUpdateTaskScript != null)
                    {
                        handlerUpdateTaskScript(inventoryItemID, primID, isScriptRunning, data, ref errors);
                    }

                    uploadComplete.new_asset = inventoryItemID;
                    uploadComplete.compiled = errors.Count > 0 ? false : true;
                    uploadComplete.state = "complete";
                    uploadComplete.errors = new OSDArray();
                    uploadComplete.errors.Array = errors;

                    res = LLSDHelpers.SerialiseLLSDReply(uploadComplete);

                    httpListener.RemoveStreamHandler("POST", uploaderPath);

//                    m_log.InfoFormat("[CAPS]: TaskInventoryScriptUpdater.uploaderCaps res: {0}", res);

                    return res;
                }
                catch (Exception e)
                {
                    m_log.Error("[CAPS]: " + e.ToString());
                }

                // XXX Maybe this should be some meaningful error packet
                return null;
            }
            ///Left this in and commented in case there are unforseen issues
            //private void SaveAssetToFile(string filename, byte[] data)
            //{
            //    FileStream fs = File.Create(filename);
            //    BinaryWriter bw = new BinaryWriter(fs);
            //    bw.Write(data);
            //    bw.Close();
            //    fs.Close();
            //}
            private static void SaveAssetToFile(string filename, byte[] data)
            {
                string assetPath = "UserAssets";
                if (!Directory.Exists(assetPath))
                {
                    Directory.CreateDirectory(assetPath);
                }
                FileStream fs = File.Create(Path.Combine(assetPath, filename));
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(data);
                bw.Close();
                fs.Close();
            }
        }
    }
}
