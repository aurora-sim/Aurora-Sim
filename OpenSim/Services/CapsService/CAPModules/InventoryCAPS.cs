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
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Capabilities;

using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Services.CapsService
{
    public class InventoryCAPS : ICapsServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string m_newInventory = "0002";
        private IRegionClientCapsService m_service;
        private IInventoryService m_inventoryService;
        private ILibraryService m_libraryService;
        private IAssetService m_assetService;

        #region ICapsServiceConnector Members

        public void RegisterCaps(IRegionClientCapsService service)
        {
            m_service = service;
            m_assetService = service.Registry.RequestModuleInterface<IAssetService>();
            m_inventoryService = service.Registry.RequestModuleInterface<IInventoryService>();
            m_libraryService = service.Registry.RequestModuleInterface<ILibraryService>();

            RestBytesMethod method = delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return HandleWebFetchInventoryDescendents(request, m_service.AgentID);
            };
            service.AddStreamHandler("WebFetchInventoryDescendents",
                new RestBytesStreamHandler("POST", service.CreateCAPS("WebFetchInventoryDescendents", ""),
                                                      method));
            service.AddStreamHandler("FetchInventoryDescendents",
                new RestBytesStreamHandler("POST", service.CreateCAPS("FetchInventoryDescendents", ""),
                                                      method));
            service.AddStreamHandler("FetchInventoryDescendents2",
                new RestBytesStreamHandler("POST", service.CreateCAPS("FetchInventoryDescendents2", ""),
                                                      method));

            method = delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return HandleFetchLibDescendents(request, m_service.AgentID);
            };
            service.AddStreamHandler("FetchLibDescendents",
                new RestBytesStreamHandler("POST", service.CreateCAPS("FetchLibDescendents", ""),
                                                      method));
            service.AddStreamHandler("FetchLibDescendents2",
                new RestBytesStreamHandler("POST", service.CreateCAPS("FetchLibDescendents2", ""),
                                                      method));

            method = delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return HandleFetchInventory(request, m_service.AgentID);
            };
            service.AddStreamHandler("FetchInventory",
                new RestBytesStreamHandler("POST", service.CreateCAPS("FetchInventory", ""),
                                                      method));
            service.AddStreamHandler("FetchInventory2",
                new RestBytesStreamHandler("POST", service.CreateCAPS("FetchInventory2", ""),
                                                      method));

            method = delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return HandleFetchLib(request, m_service.AgentID);
            };
            service.AddStreamHandler("FetchLib",
                new RestBytesStreamHandler("POST", service.CreateCAPS("FetchLib", ""),
                                                      method));
            service.AddStreamHandler("FetchLib2",
                new RestBytesStreamHandler("POST", service.CreateCAPS("FetchLib2", ""),
                                                      method));

            service.AddStreamHandler("NewFileAgentInventory",
                new RestStreamHandler("POST", service.CreateCAPS("NewFileAgentInventory", m_newInventory),
                                                      NewAgentInventoryRequest));
            service.AddStreamHandler("NewFileAgentInventoryVariablePrice",
                new RestStreamHandler("POST", service.CreateCAPS("NewFileAgentInventoryVariablePrice", ""),
                                                      NewAgentInventoryRequestVariablePrice));

            /*method = delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return HandleInventoryItemCreate(request, m_service.AgentID);
            };
            service.AddStreamHandler("InventoryItemCreate",
                new RestBytesStreamHandler("POST", service.CreateCAPS("InventoryItemCreate", ""),
                                                      method));*/
        }

        public void EnteringRegion()
        {
        }

        public void DeregisterCaps()
        {
            m_service.RemoveStreamHandler("WebFetchInventoryDescendents", "POST");
            m_service.RemoveStreamHandler("FetchInventoryDescendents", "POST");
            m_service.RemoveStreamHandler("FetchInventoryDescendents2", "POST");
            m_service.RemoveStreamHandler("FetchLibDescendents", "POST");
            m_service.RemoveStreamHandler("FetchLibDescendents2", "POST");
            m_service.RemoveStreamHandler("FetchInventory", "POST");
            m_service.RemoveStreamHandler("FetchInventory2", "POST");
            m_service.RemoveStreamHandler("FetchLib", "POST");
            m_service.RemoveStreamHandler("FetchLib2", "POST");
            m_service.RemoveStreamHandler("NewFileAgentInventory", "POST");
            m_service.RemoveStreamHandler("NewFileAgentInventoryVariablePrice", "POST");
            m_service.RemoveStreamHandler("InventoryItemCreate", "POST");
        }

        #endregion

        #region Inventory

        public byte[] HandleWebFetchInventoryDescendents(string request, UUID AgentID)
        {
            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(request);
            OSDArray foldersrequested = (OSDArray)map["folders"];
            try
            {
                //m_log.DebugFormat("[InventoryCAPS]: Received WebFetchInventoryDescendents request for {0}", AgentID);

                return Aurora.DataManager.DataManager.RequestPlugin<IInventoryData>().FetchInventoryReply(foldersrequested, AgentID, UUID.Zero);
            }
            catch (Exception ex)
            {
                m_log.Warn("[InventoryCaps]: SERIOUS ISSUE! " + ex.ToString());
            }
            finally
            {
                map = null;
                foldersrequested = null;
            }
            OSDMap rmap = new OSDMap();
            rmap["folders"] = new OSDArray();
            return OSDParser.SerializeLLSDXmlBytes(rmap);
        }

        public byte[] HandleFetchLibDescendents(string request, UUID AgentID)
        {
            try
            {
                //m_log.DebugFormat("[InventoryCAPS]: Received FetchLibDescendents request for {0}", AgentID);

                OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(request));

                OSDArray foldersrequested = (OSDArray)map["folders"];

                return Aurora.DataManager.DataManager.RequestPlugin<IInventoryData>().FetchInventoryReply(foldersrequested, m_libraryService.LibraryOwner, AgentID);
            }
            catch (Exception ex)
            {
                m_log.Warn("[InventoryCaps]: SERIOUS ISSUE! " + ex.ToString());
            }
            OSDMap rmap = new OSDMap();
            rmap["folders"] = new OSDArray();
            return OSDParser.SerializeLLSDXmlBytes(rmap);
        }

        public byte[] HandleFetchInventory(string request, UUID AgentID)
        {
            try
            {
                //m_log.DebugFormat("[InventoryCAPS]: Received FetchInventory request for {0}", AgentID);

                OSDMap requestmap = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(request));

                OSDArray foldersrequested = (OSDArray)requestmap["items"];

                OSDMap map = new OSDMap();
                //We have to send the agent_id in the main map as well as all the items
                map.Add("agent_id", OSD.FromUUID(AgentID));

                OSDArray items = new OSDArray();
                for (int i = 0; i < foldersrequested.Count; i++)
                {
                    OSDMap requestedFolders = (OSDMap)foldersrequested[i];
                    //UUID owner_id = requestedFolders["owner_id"].AsUUID();
                    UUID item_id = requestedFolders["item_id"].AsUUID();
                    OSDArray item = m_inventoryService.GetItem(item_id);
                    if (item != null && item.Count > 0)
                        items.Add(item[0]);
                }
                map.Add("items", items);

                byte[] response = OSDParser.SerializeLLSDXmlBytes(map);
                map.Clear();
                return response;
            }
            catch (Exception ex)
            {
                m_log.Warn("[InventoryCaps]: SERIOUS ISSUE! " + ex.ToString());
            }
            OSDMap rmap = new OSDMap();
            rmap["items"] = new OSDArray();
            return OSDParser.SerializeLLSDXmlBytes(rmap);
        }

        public byte[] HandleFetchLib(string request, UUID AgentID)
        {
            try
            {
                //m_log.DebugFormat("[InventoryCAPS]: Received FetchLib request for {0}", AgentID);

                OSDMap requestmap = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(request));

                OSDArray foldersrequested = (OSDArray)requestmap["items"];

                OSDMap map = new OSDMap();
                map.Add("agent_id", OSD.FromUUID(AgentID));
                OSDArray items = new OSDArray();
                for (int i = 0; i < foldersrequested.Count; i++)
                {
                    OSDMap requestedFolders = (OSDMap)foldersrequested[i];
                    //UUID owner_id = requestedFolders["owner_id"].AsUUID();
                    UUID item_id = requestedFolders["item_id"].AsUUID();
                    OSDArray item = m_inventoryService.GetItem(item_id);
                    if (item != null && item.Count > 0)
                        items.Add(item[0]);
                }
                map.Add("items", items);

                byte[] response = OSDParser.SerializeLLSDXmlBytes(map);
                map.Clear();
                return response;
            }
            catch (Exception ex)
            {
                m_log.Warn("[InventoryCaps]: SERIOUS ISSUE! " + ex.ToString());
            }
            OSDMap rmap = new OSDMap();
            rmap["items"] = new OSDArray();
            return OSDParser.SerializeLLSDXmlBytes(rmap);
        }

        #endregion

        #region Inventory upload

        /// <summary>
        /// This handles the uploading of some inventory types
        /// </summary>
        /// <param name="llsdRequest"></param>
        /// <returns></returns>
        public string NewAgentInventoryRequest(string request, string path, string param,
                                             OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(request);
            string asset_type = map["asset_type"].AsString();
            if (asset_type == "texture" ||
                asset_type == "animation" ||
                asset_type == "sound")
            {
                IMoneyModule mm = m_service.Registry.RequestModuleInterface<IMoneyModule>();

                /*if (mm != null)
                {
                    if (!mm.Charge (client.AgentID, mm.UploadCharge))
                    {
                        map = new OSDMap ();
                        map["uploader"] = "";
                        map["state"] = "error";
                        return OSDParser.SerializeLLSDXmlString (map);
                    }
                }*/
            }
            return OSDParser.SerializeLLSDXmlString(InternalNewAgentInventoryRequest(map, path, param, httpRequest, httpResponse));
        }
        public string NewAgentInventoryRequestVariablePrice(string request, string path, string param,
                                             OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(request);
            string asset_type = map["asset_type"].AsString();
            if (asset_type == "texture" ||
                asset_type == "animation" ||
                asset_type == "sound")
            {
                IMoneyModule mm = m_service.Registry.RequestModuleInterface<IMoneyModule>();

                /*if (mm != null)
                {
                    if (!mm.Charge (client.AgentID, mm.UploadCharge))
                    {
                        map = new OSDMap ();
                        map["uploader"] = "";
                        map["state"] = "error";
                        return OSDParser.SerializeLLSDXmlString (map);
                    }
                }*/
            }
            OSDMap resp = InternalNewAgentInventoryRequest(map, path, param, httpRequest, httpResponse);

            resp["resource_cost"] = 0;
            resp["upload_price"] = 0;//Set me if you want to use variable cost stuff

            return OSDParser.SerializeLLSDXmlString(map);
        }

        private OSDMap InternalNewAgentInventoryRequest(OSDMap map, string path, string param,
                                             OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            string asset_type = map["asset_type"].AsString();
            //m_log.Info("[CAPS]: NewAgentInventoryRequest Request is: " + map.ToString());
            //m_log.Debug("asset upload request via CAPS" + llsdRequest.inventory_type + " , " + llsdRequest.asset_type);

            string assetName = map["name"].AsString();
            string assetDes = map["description"].AsString();
            UUID parentFolder = map["folder_id"].AsUUID();
            string inventory_type = map["inventory_type"].AsString();
            uint everyone_mask = map["everyone_mask"].AsUInteger();
            uint group_mask = map["group_mask"].AsUInteger();
            uint next_owner_mask = map["next_owner_mask"].AsUInteger();

            UUID newAsset = UUID.Random();
            UUID newInvItem = UUID.Random();
            string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");
            string uploadpath = m_service.CreateCAPS("Upload" + uploaderPath, uploaderPath);

            AssetUploader uploader =
                new AssetUploader(assetName, assetDes, newAsset, newInvItem, parentFolder, inventory_type,
                                  asset_type, uploadpath, "Upload" + uploaderPath, m_service, this, everyone_mask,
                                  group_mask, next_owner_mask);
            m_service.AddStreamHandler("Upload" + uploaderPath,
                new BinaryStreamHandler("POST", uploadpath, uploader.uploaderCaps));

            string uploaderURL = m_service.HostUri + uploadpath;
            map = new OSDMap();
            map["uploader"] = uploaderURL;
            map["state"] = "upload";
            return map;
        }

        public string HandleInventoryItemCreate(string request, UUID AgentID)
        {
            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(request);
            string asset_type = map["asset_type"].AsString();
            //m_log.Info("[CAPS]: NewAgentInventoryRequest Request is: " + map.ToString());
            //m_log.Debug("asset upload request via CAPS" + llsdRequest.inventory_type + " , " + llsdRequest.asset_type);

            if (asset_type == "texture" ||
                asset_type == "animation" ||
                asset_type == "sound")
            {
                /* Disabled until we have a money module that can hook up to this
                 * IMoneyModule mm = .RequestModuleInterface<IMoneyModule>();

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
                 */
            }


            string assetName = map["name"].AsString();
            string assetDes = map["description"].AsString();
            UUID parentFolder = map["folder_id"].AsUUID();
            string inventory_type = map["inventory_type"].AsString();
            uint everyone_mask = map["everyone_mask"].AsUInteger();
            uint group_mask = map["group_mask"].AsUInteger();
            uint next_owner_mask = map["next_owner_mask"].AsUInteger();

            UUID newAsset = UUID.Random();
            UUID newInvItem = UUID.Random();
            string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");
            string uploadpath = m_service.CreateCAPS("Upload" + uploaderPath, uploaderPath);

            AssetUploader uploader =
                new AssetUploader(assetName, assetDes, newAsset, newInvItem, parentFolder, inventory_type,
                                  asset_type, uploadpath, "Upload" + uploaderPath, m_service, this, everyone_mask,
                                  group_mask, next_owner_mask);
            m_service.AddStreamHandler("Upload" + uploaderPath,
                new BinaryStreamHandler("POST", uploadpath, uploader.uploaderCaps));

            string uploaderURL = m_service.HostUri + uploadpath;
            map = new OSDMap();
            map["uploader"] = uploaderURL;
            map["state"] = "upload";
            return OSDParser.SerializeLLSDXmlString(map);
        }

        public class AssetUploader
        {
            private string uploaderPath = String.Empty;
            private string uploadMethod = String.Empty;
            private UUID newAssetID;
            private UUID inventoryItemID;
            private UUID parentFolder;
            private IRegionClientCapsService clientCaps;


            private string m_assetName = String.Empty;
            private string m_assetDes = String.Empty;

            private string m_invType = String.Empty;
            private string m_assetType = String.Empty;
            private InventoryCAPS m_invCaps;

            private uint m_everyone_mask = 0;
            private uint m_group_mask = 0;
            private uint m_next_owner_mask = 0;

            public AssetUploader(string assetName, string description, UUID assetID, UUID inventoryItem,
                                 UUID parentFolderID, string invType, string assetType, string path,
                                 string method, IRegionClientCapsService caps, InventoryCAPS invCaps,
                                 uint everyone_mask, uint group_mask, uint next_owner_mask)
            {
                m_assetName = assetName;
                m_assetDes = description;
                newAssetID = assetID;
                inventoryItemID = inventoryItem;
                uploaderPath = path;
                parentFolder = parentFolderID;
                m_assetType = assetType;
                m_invType = invType;
                clientCaps = caps;
                uploadMethod = method;
                m_invCaps = invCaps;
                m_everyone_mask = everyone_mask;
                m_group_mask = group_mask;
                m_next_owner_mask = next_owner_mask;
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
                clientCaps.RemoveStreamHandler(uploadMethod, "POST", uploaderPath);

                newAssetID = m_invCaps.UploadCompleteHandler(m_assetName, m_assetDes, newAssetID, inv, parentFolder,
                    data, m_invType, m_assetType, m_everyone_mask, m_group_mask, m_next_owner_mask);

                string res = String.Empty;
                OSDMap map = new OSDMap();
                map["new_asset"] = newAssetID.ToString();
                map["new_inventory_item"] = inv;
                map["state"] = "complete";
                res = OSDParser.SerializeLLSDXmlString(map);

                return res;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="assetID"></param>
        /// <param name="inventoryItem"></param>
        /// <param name="data"></param>
        public UUID UploadCompleteHandler(string assetName, string assetDescription, UUID assetID,
                                          UUID inventoryItem, UUID parentFolder, byte[] data, string inventoryType,
                                          string assetType, uint everyone_mask, uint group_mask, uint next_owner_mask)
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
            else if (inventoryType == "snapshot")
            {
                inType = 15;
                assType = 0;
            }
            else if (inventoryType == "object")
            {
                inType = (sbyte)InventoryType.Object;
                assType = (sbyte)AssetType.Object;

                List<Vector3> positions = new List<Vector3>();
                List<Quaternion> rotations = new List<Quaternion>();
                OSDMap request = (OSDMap)OSDParser.DeserializeLLSDXml(data);
                OSDArray instance_list = (OSDArray)request["instance_list"];
                OSDArray mesh_list = (OSDArray)request["mesh_list"];
                OSDArray texture_list = (OSDArray)request["texture_list"];
                SceneObjectGroup grp = null;

                List<UUID> textures = new List<UUID>();
                for (int i = 0; i < texture_list.Count; i++)
                {
                    AssetBase textureAsset = new AssetBase(UUID.Random(), assetName, AssetType.Texture, m_service.AgentID);
                    textureAsset.Data = texture_list[i].AsBinary();
                    textureAsset.ID = m_assetService.Store(textureAsset);
                    textures.Add(textureAsset.ID);
                }
                InventoryFolderBase meshFolder = m_inventoryService.GetFolderForType(m_service.AgentID, InventoryType.Mesh, AssetType.Mesh);
                for (int i = 0; i < mesh_list.Count; i++)
                {
                    PrimitiveBaseShape pbs = PrimitiveBaseShape.CreateBox();

                    Primitive.TextureEntry textureEntry = new Primitive.TextureEntry(Primitive.TextureEntry.WHITE_TEXTURE);
                    OSDMap inner_instance_list = (OSDMap)instance_list[i];

                    OSDArray face_list = (OSDArray)inner_instance_list["face_list"];
                    for (uint face = 0; face < face_list.Count; face++)
                    {
                        OSDMap faceMap = (OSDMap)face_list[(int)face];
                        Primitive.TextureEntryFace f = pbs.Textures.CreateFace(face);
                        if (faceMap.ContainsKey("fullbright"))
                            f.Fullbright = faceMap["fullbright"].AsBoolean();
                        if (faceMap.ContainsKey("diffuse_color"))
                            f.RGBA = faceMap["diffuse_color"].AsColor4();

                        int textureNum = faceMap["image"].AsInteger();
                        float imagerot = faceMap["imagerot"].AsInteger();
                        float offsets = (float)faceMap["offsets"].AsReal();
                        float offsett = (float)faceMap["offsett"].AsReal();
                        float scales = (float)faceMap["scales"].AsReal();
                        float scalet = (float)faceMap["scalet"].AsReal();

                        if (imagerot != 0)
                            f.Rotation = imagerot;
                        if (offsets != 0)
                            f.OffsetU = offsets;
                        if (offsett != 0)
                            f.OffsetV = offsett;
                        if (scales != 0)
                            f.RepeatU = scales;
                        if (scalet != 0)
                            f.RepeatV = scalet;
                        if (textures.Count > textureNum)
                            f.TextureID = textures[textureNum];
                        else
                            f.TextureID = Primitive.TextureEntry.WHITE_TEXTURE;
                        textureEntry.FaceTextures[face] = f;
                    }
                    pbs.TextureEntry = textureEntry.GetBytes();

                    AssetBase meshAsset = new AssetBase(UUID.Random(), assetName, AssetType.Mesh, m_service.AgentID);
                    meshAsset.Data = mesh_list[i].AsBinary();
                    meshAsset.ID = m_assetService.Store(meshAsset);

                    if(meshFolder == null)
                    {
                        m_inventoryService.CreateUserInventory(m_service.AgentID, false);
                        meshFolder = m_inventoryService.GetFolderForType(m_service.AgentID, InventoryType.Mesh, AssetType.Mesh);
                    }

                    InventoryItemBase itemBase = new InventoryItemBase(UUID.Random(), m_service.AgentID);
                    itemBase.AssetType = (sbyte)AssetType.Mesh;//Bad... but whatever
                    itemBase.AssetID = meshAsset.ID;
                    itemBase.CreatorId = m_service.AgentID.ToString();
                    itemBase.Folder = meshFolder.ID;
                    itemBase.InvType = (int)InventoryType.Texture;
                    itemBase.Name = "(Mesh) - " + assetName;
                    itemBase.CurrentPermissions = (uint)PermissionMask.All;
                    itemBase.EveryOnePermissions = everyone_mask;
                    itemBase.GroupPermissions = group_mask;
                    itemBase.NextPermissions = next_owner_mask;
                    m_inventoryService.AddItem(itemBase);

                    pbs.SculptEntry = true;
                    pbs.SculptTexture = meshAsset.ID;
                    pbs.SculptType = (byte)SculptType.Mesh;
                    pbs.SculptData = meshAsset.Data;

                    Vector3 position = inner_instance_list["position"].AsVector3();
                    Vector3 scale = inner_instance_list["scale"].AsVector3();
                    Quaternion rotation = inner_instance_list["rotation"].AsQuaternion();

                    int physicsShapeType = inner_instance_list["physics_shape_type"].AsInteger();
                    int material = inner_instance_list["material"].AsInteger();
                    int mesh = inner_instance_list["mesh"].AsInteger();//?

                    UUID owner_id = m_service.AgentID;

                    IScene fakeScene = new Scene();
                    fakeScene.AddModuleInterfaces(m_service.Registry.GetInterfaces());

                    SceneObjectPart prim = new SceneObjectPart(owner_id, pbs, position, Quaternion.Identity,
                            Vector3.Zero, assetName, fakeScene);

                    prim.Scale = scale;
                    prim.AbsolutePosition = position;
                    rotations.Add(rotation);
                    positions.Add(position);
                    prim.UUID = UUID.Random();
                    prim.CreatorID = owner_id;
                    prim.OwnerID = owner_id;
                    prim.GroupID = UUID.Zero;
                    prim.LastOwnerID = prim.OwnerID;
                    prim.CreationDate = Util.UnixTimeSinceEpoch();
                    prim.Name = assetName;
                    prim.Description = "";
                    prim.PhysicsType = (byte)physicsShapeType;

                    prim.BaseMask = (uint)PermissionMask.All;
                    prim.EveryoneMask = everyone_mask;
                    prim.NextOwnerMask = next_owner_mask;
                    prim.GroupMask = group_mask;
                    prim.OwnerMask = (uint)PermissionMask.All;

                    if (grp == null)
                        grp = new SceneObjectGroup(prim, fakeScene);
                    else
                        grp.AddChild(prim, i + 1);
                    grp.RootPart.IsAttachment = false;
                }
                if (grp.ChildrenList.Count > 1)//Fix first link #
                    grp.RootPart.LinkNum++;

                Vector3 rootPos = positions[0];
                grp.SetAbsolutePosition(false, rootPos);
                for (int i = 0; i < positions.Count; i++)
                {
                    Vector3 offset = positions[i] - rootPos;
                    grp.ChildrenList[i].SetOffsetPosition(offset);
                    Vector3 abs = grp.ChildrenList[i].AbsolutePosition;
                    Vector3 currentPos = positions[i];
                }
                //grp.Rotation = rotations[0];
                for (int i = 0; i < rotations.Count; i++)
                {
                    if (i != 0)
                        grp.ChildrenList[i].SetRotationOffset(false, rotations[i], false);
                }
                grp.UpdateGroupRotationR(rotations[0]);
                data = ASCIIEncoding.ASCII.GetBytes(grp.ToXml2());
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
            AssetBase asset = new AssetBase(assetID, assetName, (AssetType)assType, m_service.AgentID) { Data = data };
            asset.ID = m_assetService.Store(asset);
            assetID = asset.ID;

            InventoryItemBase item = new InventoryItemBase();
            item.Owner = m_service.AgentID;
            item.CreatorId = m_service.AgentID.ToString();
            item.ID = inventoryItem;
            item.AssetID = asset.ID;
            item.Description = assetDescription;
            item.Name = assetName;
            item.AssetType = assType;
            item.InvType = inType;
            item.Folder = parentFolder;
            item.CurrentPermissions = (uint)PermissionMask.All;
            item.BasePermissions = (uint)PermissionMask.All;
            item.EveryOnePermissions = everyone_mask;
            item.NextPermissions = next_owner_mask;
            item.GroupPermissions = group_mask;
            item.CreationDate = Util.UnixTimeSinceEpoch();

            m_inventoryService.AddItem(item);

            return assetID;
        }

        #endregion
    }
}
