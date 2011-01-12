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

            RestMethod method = delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return HandleWebFetchInventoryDescendents(request, m_service.AgentID);
            };
            service.AddStreamHandler("WebFetchInventoryDescendents",
                new RestStreamHandler("POST", service.CreateCAPS("WebFetchInventoryDescendents", ""),
                                                      method));

            method = delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return HandleFetchLibDescendents(request, m_service.AgentID);
            };
            service.AddStreamHandler("FetchLibDescendents",
                new RestStreamHandler("POST", service.CreateCAPS("FetchLibDescendents", ""),
                                                      method));

            method = delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return HandleFetchInventory(request, m_service.AgentID);
            };
            service.AddStreamHandler("FetchInventory",
                new RestStreamHandler("POST", service.CreateCAPS("FetchInventory", ""),
                                                      method));

            method = delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return HandleFetchLib(request, m_service.AgentID);
            };
            service.AddStreamHandler("FetchLib",
                new RestStreamHandler("POST", service.CreateCAPS("FetchLib", ""),
                                                      method));

            service.AddStreamHandler("NewFileAgentInventory",
                new RestStreamHandler("POST", service.CreateCAPS("NewFileAgentInventory", m_newInventory),
                                                      NewAgentInventoryRequest));
        }

        public void EnteringRegion()
        {
        }

        public void DeregisterCaps()
        {
        }

        #endregion
        
        #region Inventory

        public string HandleWebFetchInventoryDescendents(string request, UUID AgentID)
        {
            m_log.DebugFormat("[InventoryCAPS]: Received WebFetchInventoryDescendents request for {0}", AgentID);

            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(request));

            OSDArray foldersrequested = (OSDArray)map["folders"];

            string response = FetchInventoryReply(foldersrequested, AgentID, false);
            return response;
        }

        public string HandleFetchLibDescendents(string request, UUID AgentID)
        {
            m_log.DebugFormat("[InventoryCAPS]: Received FetchLibDescendents request for {0}", AgentID);

            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(request));

            OSDArray foldersrequested = (OSDArray)map["folders"];

            string response = FetchInventoryReply(foldersrequested, AgentID, true);
            return response;
        }

        public string HandleFetchInventory(string request, UUID AgentID)
        {
            m_log.DebugFormat("[InventoryCAPS]: Received FetchInventory request for {0}", AgentID);

            OSDMap requestmap = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(request));

            OSDArray foldersrequested = (OSDArray)requestmap["items"];

            string response = "";
            OSDMap map = new OSDMap();
            //We have to send the agent_id in the main map as well as all the items
            map.Add("agent_id", OSD.FromUUID(AgentID));

            OSDArray items = new OSDArray();
            for (int i = 0; i < foldersrequested.Count; i++)
            {
                OSDMap requestedFolders = (OSDMap)foldersrequested[i];
                UUID owner_id = requestedFolders["owner_id"].AsUUID();
                UUID item_id = requestedFolders["item_id"].AsUUID();
                InventoryItemBase item = m_inventoryService.GetItem(new InventoryItemBase(item_id, owner_id));
                if (item != null)
                {
                    items.Add(ConvertInventoryItem(item, owner_id));
                }
            }
            map.Add("items", items);

            response = OSDParser.SerializeLLSDXmlString(map);
            return response;
        }

        public string HandleFetchLib(string request, UUID AgentID)
        {
            m_log.DebugFormat("[InventoryCAPS]: Received FetchLib request for {0}", AgentID);

            OSDMap requestmap = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(request));

            OSDArray foldersrequested = (OSDArray)requestmap["items"];

            string response = "";
            OSDMap map = new OSDMap();
            map.Add("agent_id", OSD.FromUUID(AgentID));
            OSDArray items = new OSDArray();
            for (int i = 0; i < foldersrequested.Count; i++)
            {
                OSDMap requestedFolders = (OSDMap)foldersrequested[i];
                UUID owner_id = requestedFolders["owner_id"].AsUUID();
                UUID item_id = requestedFolders["item_id"].AsUUID();
                InventoryItemBase item = null;
                if (m_libraryService != null && m_libraryService.LibraryRootFolder != null)
                {
                    item = m_libraryService.LibraryRootFolder.FindItem(item_id);
                }
                if (item == null) //Try normal inventory them
                    item = m_inventoryService.GetItem(new InventoryItemBase(item_id, owner_id));
                if (item != null)
                {
                    items.Add(ConvertInventoryItem(item, owner_id));
                }
            }
            map.Add("items", items);

            response = OSDParser.SerializeLLSDXmlString(map);
            return response;
        }

        /// <summary>
        /// Construct an LLSD reply packet to a CAPS inventory request
        /// </summary>
        /// <param name="invFetch"></param>
        /// <returns></returns>
        private string FetchInventoryReply(OSDArray fetchRequest, UUID AgentID, bool Library)
        {
            OSDMap contents = new OSDMap();
            OSDArray folders = new OSDArray();

            foreach (OSD m in fetchRequest)
            {
                OSDMap invFetch = (OSDMap)m;
                OSDMap internalContents = new OSDMap();

                UUID agent_id = invFetch["agent_id"].AsUUID();
                UUID owner_id = invFetch["owner_id"].AsUUID();
                UUID folder_id = invFetch["folder_id"].AsUUID();
                bool fetch_folders = invFetch["fetch_folders"].AsBoolean();
                bool fetch_items = invFetch["fetch_items"].AsBoolean();
                int sort_order = invFetch["sort_order"].AsInteger();

                //Set the normal stuff
                internalContents["agent_id"] = AgentID;
                internalContents["owner_id"] = owner_id;
                internalContents["folder_id"] = folder_id;


                InventoryCollection inv = new InventoryCollection();
                inv.Folders = new List<InventoryFolderBase>();
                inv.Items = new List<InventoryItemBase>();
                int version = 0;
                inv = HandleFetchInventoryDescendentsCAPS(AgentID, folder_id, owner_id, fetch_folders, fetch_items, sort_order, Library, out version);

                OSDArray categories = new OSDArray();
                if (inv.Folders != null)
                {
                    foreach (InventoryFolderBase invFolder in inv.Folders)
                    {
                        categories.Add(ConvertInventoryFolder(invFolder));
                    }
                }
                internalContents["categories"] = categories;

                OSDArray items = new OSDArray();
                if (inv.Items != null)
                {
                    foreach (InventoryItemBase invItem in inv.Items)
                    {
                        items.Add(ConvertInventoryItem(invItem, AgentID));
                    }
                }
                internalContents["items"] = items;

                internalContents["descendents"] = items.Count + categories.Count;
                internalContents["version"] = version;

                //Now add it to the folder array
                folders.Add(internalContents);
            }

            contents["folders"] = folders;
            return OSDParser.SerializeLLSDXmlString(contents);
        }

        /// <summary>
        /// Convert an internal inventory folder object into an LLSD object.
        /// </summary>
        /// <param name="invFolder"></param>
        /// <returns></returns>
        private OSDMap ConvertInventoryFolder(InventoryFolderBase invFolder)
        {
            OSDMap folder = new OSDMap();
            folder["folder_id"] = invFolder.ID;
            folder["parent_id"] = invFolder.ParentID;
            folder["name"] = invFolder.Name;
            if (invFolder.Type < 0 || invFolder.Type >= TaskInventoryItem.Types.Length)
                folder["type"] = "0";
            else
                folder["type"] = TaskInventoryItem.Types[invFolder.Type];
            folder["preferred_type"] = "0";

            return folder;
        }

        /// <summary>
        /// Convert an internal inventory item object into an LLSD object.
        /// </summary>
        /// <param name="invItem"></param>
        /// <returns></returns>
        private OSDMap ConvertInventoryItem(InventoryItemBase invItem, UUID AgentID)
        {
            OSDMap item = new OSDMap();
            item["agent_id"] = AgentID;
            item["asset_id"] = invItem.AssetID;
            item["created_at"] = invItem.CreationDate;
            item["desc"] = invItem.Description;
            item["flags"] = (int)invItem.Flags;
            item["item_id"] = invItem.ID;
            item["name"] = invItem.Name;
            item["parent_id"] = invItem.Folder;
            try
            {

                // TODO reevaluate after upgrade to libomv >= r2566. Probably should use UtilsConversions.
                item["type"] = TaskInventoryItem.Types[invItem.AssetType];
                item["inv_type"] = TaskInventoryItem.InvTypes[invItem.InvType];
                //llsdItem.type = Utils.InventoryTypeToString((InventoryType)invItem.AssetType);
                //llsdItem.inv_type = Utils.InventoryTypeToString((InventoryType)invItem.InvType);
            }
            catch (Exception e)
            {
                m_log.Error("[CAPS]: Problem setting asset/inventory type while converting inventory item " + invItem.Name + " to LLSD:", e);
            }
            OSDMap permissions = new OSDMap();
            permissions["creator_id"] = invItem.CreatorIdAsUuid;
            permissions["base_mask"] = (int)invItem.CurrentPermissions;
            permissions["everyone_mask"] = (int)invItem.EveryOnePermissions;
            permissions["group_id"] = invItem.GroupID;
            permissions["group_mask"] = (int)invItem.GroupPermissions;
            permissions["is_owner_group"] = invItem.GroupOwned;
            permissions["next_owner_mask"] = (int)invItem.NextPermissions;
            permissions["last_owner_id"] = invItem.Owner; //Err... can't set this?
            permissions["owner_id"] = AgentID;
            permissions["owner_mask"] = (int)invItem.CurrentPermissions;
            item["permissions"] = permissions;

            OSDMap sale_info = new OSDMap();
            sale_info["sale_price"] = invItem.SalePrice;
            switch (invItem.SaleType)
            {
                default:
                    sale_info["sale_type"] = "not";
                    break;
                case 1:
                    sale_info["sale_type"] = "original";
                    break;
                case 2:
                    sale_info["sale_type"] = "copy";
                    break;
                case 3:
                    sale_info["sale_type"] = "contents";
                    break;
            }
            item["sale_info"] = sale_info;

            return item;
        }

        public InventoryCollection HandleFetchInventoryDescendentsCAPS(UUID agentID, UUID folderID, UUID ownerID,
                                                   bool fetchFolders, bool fetchItems, int sortOrder, bool Library, out int version)
        {
            // FIXME MAYBE: We're not handling sortOrder!

            InventoryFolderImpl fold;

            InventoryCollection contents = new InventoryCollection();
            //if (Library)
            // {
            //version = 0;
            if (m_libraryService != null && m_libraryService.LibraryRootFolder != null)
            {
                if ((fold = m_libraryService.LibraryRootFolder.FindFolder(folderID)) != null)
                {
                    version = 0;
                    InventoryCollection ret = new InventoryCollection();
                    ret.Folders = new List<InventoryFolderBase>();
                    ret.Items = fold.RequestListOfItems();

                    return ret;
                }
            }
            //return contents;
            //}

            //if (folderID != UUID.Zero)
            //{
            if (fetchFolders)
            {
                contents = m_inventoryService.GetFolderContent(agentID, folderID);
            }
            if (fetchItems)
            {
                contents.Items = m_inventoryService.GetFolderItems(agentID, folderID);
            }
            InventoryFolderBase containingFolder = new InventoryFolderBase();
            containingFolder.ID = folderID;
            containingFolder.Owner = agentID;
            containingFolder = m_inventoryService.GetFolder(containingFolder);
            if (containingFolder != null)
                version = containingFolder.Version;
            else
                version = 1;
            //}
            //else
            //{
            //    // Lost itemsm don't really need a version
            //    version = 1;
            //}

            return contents;

        }
        #endregion

        #region Inventory upload

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
            m_log.Info("[CAPS]: NewAgentInventoryRequest Request is: " + map.ToString());
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

            UUID newAsset = UUID.Random();
            UUID newInvItem = UUID.Random();
            string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");
            string uploadpath = m_service.CreateCAPS("Upload" + uploaderPath, uploaderPath);
                
            AssetUploader uploader =
                new AssetUploader(assetName, assetDes, newAsset, newInvItem, parentFolder, inventory_type,
                                  asset_type, uploadpath, "Upload" + uploaderPath, m_service, this);
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

            public AssetUploader(string assetName, string description, UUID assetID, UUID inventoryItem,
                                 UUID parentFolderID, string invType, string assetType, string path,
                                 string method, IRegionClientCapsService caps, InventoryCAPS invCaps)
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

                clientCaps.RemoveStreamHandler(uploadMethod, "POST", uploaderPath);

                m_invCaps.UploadCompleteHandler(m_assetName, m_assetDes, newAssetID, inv, parentFolder, data, m_invType, m_assetType);
                
                return res;
            }
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
            else if (inventoryType == "snapshot")
            {
                inType = 15;
                assType = 0;
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
            AssetBase asset = new AssetBase(assetID, assetName, assType, m_service.AgentID.ToString());
            asset.Data = data;
            m_assetService.Store(asset);

            InventoryItemBase item = new InventoryItemBase();
            item.Owner = m_service.AgentID;
            item.CreatorId = m_service.AgentID.ToString();
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

            m_inventoryService.AddItem(item);
        }

        #endregion
    }
}
