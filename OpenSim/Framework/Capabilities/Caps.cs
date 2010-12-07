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
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenMetaverse.StructuredData;

namespace OpenSim.Framework.Capabilities
{
    public delegate void UpLoadedAsset(
        string assetName, string description, UUID assetID, UUID inventoryItem, UUID parentFolder,
        byte[] data, string inventoryType, string assetType);

    public delegate void UploadedBakedTexture(UUID assetID, byte[] data);

    public delegate string UpdateItem(UUID itemID, byte[] data);

    public delegate void UpdateTaskScript(UUID itemID, UUID primID, bool isScriptRunning, byte[] data, ref ArrayList errors);

    public delegate void NewInventoryItem(UUID userID, InventoryItemBase item);

    public delegate void NewAsset(AssetBase asset);

    public delegate string ItemUpdatedCallback(UUID userID, UUID itemID, byte[] data);

    public delegate ArrayList TaskScriptUpdatedCallback(UUID userID, UUID itemID, UUID primID,
                                                    bool isScriptRunning, byte[] data);

    public delegate InventoryCollection FetchInventoryDescendentsCAPS(UUID agentID, UUID folderID, UUID ownerID,
                                                    bool fetchFolders, bool fetchItems, int sortOrder, out int version);
    
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
        private string m_regionName;
        private bool m_persistBakedTextures = true;

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
        public OSDMap RequestMap = new OSDMap();
        
        public Caps(IScene scene, IAssetService assetCache, IHttpServer httpServer, string httpListen, uint httpPort, string capsPath,
                    UUID agent, string regionName)
        {
            m_Scene = scene;
            m_assetCache = assetCache;
            m_capsObjectPath = capsPath;
            m_httpListener = httpServer;
            m_httpListenerHostName = httpListen;

            m_httpListenPort = httpPort;

            IConfigSource config = m_Scene.Config;
            if (config != null)
            {
                IConfig sconfig = config.Configs["Startup"];
                if (sconfig != null)
                    m_persistBakedTextures = sconfig.GetBoolean("PersistBakedTextures", m_persistBakedTextures);
            }

            if (httpServer != null && httpServer.UseSSL)
            {
                m_httpListenPort = httpServer.SSLPort;
                httpListen = httpServer.SSLCommonName;
                httpPort = httpServer.SSLPort;
            }

            m_agentID = agent;
            m_capsHandlers = new CapsHandlers(httpServer, httpListen, httpPort, (httpServer == null) ? false : httpServer.UseSSL);
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
                m_capsHandlers["UploadBakedTexture"] =
                    new RestStreamHandler("POST", capsBase + m_uploadBakedTexturePath, UploadBakedTexture);

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
                    new RestStreamHandler("POST", capsBase + m_newInventory, NewAgentInventoryRequest);
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
            try
            {
                if (request != "")
                {
                    OSD osdRequest = OSDParser.DeserializeLLSDXml(request);
                    if (osdRequest is OSDMap)
                        RequestMap = (OSDMap)osdRequest;
                }
            }
            catch
            {
            }
            string result = OSDParser.SerializeLLSDXmlString(m_capsHandlers.CapsDetails);

            //m_log.DebugFormat("[CAPS] CapsRequest {0}", result);

            return result;
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
                OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(Utils.StringToBytes(request));
                UUID item_id = map["item_id"].AsUUID();
                UUID task_id = map["task_id"].AsUUID();
                int is_script_running = map["is_script_running"].AsInteger();
                string capsBase = "/CAPS/" + m_capsObjectPath;
                string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");

                TaskInventoryScriptUpdater uploader =
                    new TaskInventoryScriptUpdater(
                        item_id,
                        task_id,
                        is_script_running,
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

                map = new OSDMap();
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
                    new BakedTextureUploader(capsBase + uploaderPath,
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
            
            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(Utils.StringToBytes(request));
            
            string capsBase = "/CAPS/" + m_capsObjectPath;
            string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");

            ItemUpdater uploader =
                new ItemUpdater(map["item_id"].AsUUID(), capsBase + uploaderPath, m_httpListener);
            uploader.OnUpLoad += ItemUpdated;

            m_httpListener.AddStreamHandler(
                new BinaryStreamHandler("POST", capsBase + uploaderPath, uploader.uploaderCaps));

            string protocol = "http://";

            if (m_httpListener.UseSSL)
                protocol = "https://";

            string uploaderURL = protocol + m_httpListenerHostName + ":" + m_httpListenPort.ToString() + capsBase +
                                 uploaderPath;

            map = new OSDMap();
            map["uploader"] = uploaderURL;
            map["state"] = "upload";
            return OSDParser.SerializeLLSDXmlString(map);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="llsdRequest"></param>
        /// <returns></returns>
        public string NewAgentInventoryRequest(string request, string path, string param,
                                             OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(request);
            string asset_type = map["asset_type"].AsString();
            //m_log.Debug("[CAPS]: NewAgentInventoryRequest Request is: " + llsdRequest.ToString());
            //m_log.Debug("asset upload request via CAPS" + llsdRequest.inventory_type + " , " + llsdRequest.asset_type);

            if (asset_type == "texture" ||
                asset_type == "animation" ||
                asset_type == "sound")
            {
                IClientAPI client = null;
                IScenePresence sp = null;
                if(m_Scene.TryGetScenePresence(m_agentID, out sp))
                {
                    client = sp.ControllingClient;
                    IScene scene = client.Scene;

                    IMoneyModule mm = scene.RequestModuleInterface<IMoneyModule>();

                    if (mm != null)
                    {
                        if (!mm.UploadCovered(client, mm.UploadCharge))
                        {
                            if (client != null)
                                client.SendAgentAlertMessage("Unable to upload asset. Insufficient funds.", false);
                            
                            map = new OSDMap();
                            map["uploader"] = "";
                            map["state"] = "error";
                            return OSDParser.SerializeLLSDXmlString(map);
                        }
                        else
                            mm.ApplyUploadCharge(client.AgentId, mm.UploadCharge, "Upload asset.");
                    }
                }
            }


            string assetName = map["name"].AsString();
            string assetDes = map["description"].AsString();
            UUID parentFolder = map["folder_id"].AsUUID();
            string inventory_type = map["inventory_type"].AsString();
            
            string capsBase = "/CAPS/" + m_capsObjectPath;
            UUID newAsset = UUID.Random();
            UUID newInvItem = UUID.Random();
            string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");

            AssetUploader uploader =
                new AssetUploader(assetName, assetDes, newAsset, newInvItem, parentFolder, inventory_type,
                                  asset_type, capsBase + uploaderPath, m_httpListener);
            m_httpListener.AddStreamHandler(
                new BinaryStreamHandler("POST", capsBase + uploaderPath, uploader.uploaderCaps));

            string protocol = "http://";

            if (m_httpListener.UseSSL)
                protocol = "https://";

            string uploaderURL = protocol + m_httpListenerHostName + ":" + m_httpListenPort.ToString() + capsBase +
                                 uploaderPath;
            uploader.OnUpLoad += UploadCompleteHandler;
            map = new OSDMap();
            map["uploader"] = uploaderURL;
            map["state"] = "upload";
            return OSDParser.SerializeLLSDXmlString(map);
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
            item.CurrentPermissions = (uint)PermissionMask.All;
            item.BasePermissions = (uint)PermissionMask.All;
            item.EveryOnePermissions = 0;
            item.NextPermissions = (uint)(PermissionMask.Move | PermissionMask.Modify | PermissionMask.Transfer);
            item.CreationDate = Util.UnixTimeSinceEpoch();

            if (AddNewInventoryItem != null)
            {
                AddNewInventoryItem(m_agentID, item);
            }
        }

        public void BakedTextureUploaded(UUID assetID, byte[] data)
        {
            m_log.InfoFormat("[CAPS]: Received baked texture {0}", assetID.ToString());
            IScenePresence sp = null;
            if(m_Scene.TryGetScenePresence(m_agentID, out sp))
            {
                IJ2KDecoder j2kDecoder = sp.ControllingClient.Scene.RequestModuleInterface<IJ2KDecoder>();
                if (j2kDecoder != null)
                {
                    if (!j2kDecoder.Decode(assetID, data))
                    {
                        //Uhoh, bad upload, rerequest them from the client
                        m_log.DebugFormat("[CAPS]: Received corrupted baked texture {0}", assetID.ToString());
                        sp.ControllingClient.SendRebakeAvatarTextures(assetID);
                        return;
                    }
                }
            }
            AssetBase asset;
            asset = new AssetBase(assetID, "Baked Texture", (sbyte)AssetType.Texture, m_agentID.ToString());
            asset.Data = data;
            asset.Temporary = false;
            asset.Flags = AssetFlags.Deletable;
            asset.Local = !m_persistBakedTextures; // Local assets aren't persisted, non-local are
            m_assetCache.Store(asset);
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
                OSDMap map = new OSDMap();
                map["new_asset"] = newAssetID.ToString();
                map["new_inventory_item"] = inv;
                map["state"] = "complete";
                res = OSDParser.SerializeLLSDXmlString(map);
                
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
                    res = handlerUpdateItem(inv, data).ToString();
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
                    
                    ArrayList errors = new ArrayList();
                    handlerUpdateTaskScript = OnUpLoad;
                    if (handlerUpdateTaskScript != null)
                    {
                        handlerUpdateTaskScript(inventoryItemID, primID, isScriptRunning, data, ref errors);
                    }

                    OSDMap map = new OSDMap();
                    map["new_asset"] = inventoryItemID;
                    map["compiled"] = errors.Count > 0 ? false : true;
                    map["state"] = "complete";
                    OSDArray array = new OSDArray();
                    foreach (object o in errors)
                    {
                        array.Add(OSD.FromObject(o));
                    }
                    map["errors"] = array;
                    res = OSDParser.SerializeLLSDXmlString(map);

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
                OSDMap map = new OSDMap();
                map["new_asset"] = newAssetID.ToString();
                map["new_inventory_item"] = UUID.Zero;
                map["state"] = "complete";
                res = OSDParser.SerializeLLSDXmlString(map);
                httpListener.RemoveStreamHandler("POST", uploaderPath);

                handlerUpLoad = OnUpLoad;
                if (handlerUpLoad != null)
                {
                    handlerUpLoad(newAssetID, data);
                }

                return res;
            }
        }
    }
}
