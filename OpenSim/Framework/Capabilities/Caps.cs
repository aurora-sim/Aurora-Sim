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

    public delegate string UpdateItem(UUID itemID, byte[] data);

    public delegate void UpdateTaskScript(UUID itemID, UUID primID, bool isScriptRunning, byte[] data, ref ArrayList errors);

    public delegate string ItemUpdatedCallback(UUID userID, UUID itemID, byte[] data);

    public delegate ArrayList TaskScriptUpdatedCallback(UUID userID, UUID itemID, UUID primID,
                                                    bool isScriptRunning, byte[] data);

    public class Caps
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private CapsHandlers m_capsHandlers;

        private static readonly string m_seedRequestPath = "0000/";
        private IScene m_Scene;
        public ulong RegionHandle
        {
            get { return m_Scene.RegionInfo.RegionHandle; }
        }
        private IHttpServer m_httpListener;
        private UUID m_agentID;
        private String m_httpBase;

        // These are callbacks which will be setup by the scene so that we can update scene data when we
        // receive capability calls
        public ItemUpdatedCallback ItemUpdatedCall = null;
        public TaskScriptUpdatedCallback TaskScriptUpdatedCall = null;
        public OSDMap RequestMap = new OSDMap();
        
        public Caps(IScene scene, IHttpServer httpServer, UUID agent)
        {
            m_Scene = scene;
            m_httpListener = httpServer;

            string Protocol = "http://";
            if (httpServer.UseSSL)
                Protocol = "https://";
            m_httpBase = Protocol + scene.RegionInfo.ExternalHostName + ":" + httpServer.Port;

            m_agentID = agent;
            m_capsHandlers = new CapsHandlers(httpServer, m_httpBase);
        }

        /// <summary>
        /// Register all CAPS http service handlers
        /// </summary>
        public void RegisterHandlers(string capsObjectPath)
        {
            DeregisterHandlers();

            RegisterRegionServiceHandlers(capsObjectPath);
        }

        public void RegisterRegionServiceHandlers(string capsObjectPath)
        {
            try
            {
                string capsBase = "/CAPS/" + capsObjectPath;
                // the root of all evil
                RegisterHandler("SEED", 
                    new RestStreamHandler("POST", capsBase + m_seedRequestPath, CapsRequest));

                capsBase = "/CAPS/" + UUID.Random() + "/";
                //Region Server bound
                IRequestHandler handler = new RestStreamHandler("POST", capsBase, ScriptTaskInventory);
                RegisterHandler("UpdateScriptTaskInventory",
                    handler);
                RegisterHandler("UpdateScriptTask",
                    handler);

                capsBase = "/CAPS/" + UUID.Random() + "/";
                //Unless the script engine goes, region server bound
                handler = new RestStreamHandler("POST", capsBase, NoteCardAgentInventory);
                RegisterHandler("UpdateNotecardAgentInventory",
                    handler);
                RegisterHandler("UpdateScriptAgentInventory",
                    handler);
                RegisterHandler("UpdateScriptAgent",
                    handler);
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
            foreach (string capsName in m_capsHandlers.Caps)
            {
                m_capsHandlers.Remove(capsName);
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
                m_log.Warn("[CAPS]: Unauthorized CAPS client");
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
                m_log.Debug("[CAPS]: ScriptTaskInventory Request in region: " + m_Scene.RegionInfo.RegionName);
                //m_log.DebugFormat("[CAPS]: request: {0}, path: {1}, param: {2}", request, path, param);
                OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(Utils.StringToBytes(request));
                UUID item_id = map["item_id"].AsUUID();
                UUID task_id = map["task_id"].AsUUID();
                int is_script_running = map["is_script_running"].AsInteger();
                string capsBase = "/CAPS/" + UUID.Random();
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

                string uploaderURL = m_httpBase + capsBase +
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
            
            string capsBase = "/CAPS/" + UUID.Random();
            string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");

            ItemUpdater uploader =
                new ItemUpdater(map["item_id"].AsUUID(), capsBase + uploaderPath, m_httpListener);
            uploader.OnUpLoad += ItemUpdated;

            m_httpListener.AddStreamHandler(
                new BinaryStreamHandler("POST", capsBase + uploaderPath, uploader.uploaderCaps));

            string uploaderURL = m_httpBase + capsBase +
                                 uploaderPath;

            map = new OSDMap();
            map["uploader"] = uploaderURL;
            map["state"] = "upload";
            return OSDParser.SerializeLLSDXmlString(map);
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
    }
}
