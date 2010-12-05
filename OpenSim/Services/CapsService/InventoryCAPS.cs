using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;
using OpenSim.Framework.Capabilities;
using OSD = OpenMetaverse.StructuredData.OSD;
using OSDArray = OpenMetaverse.StructuredData.OSDArray;
using OSDMap = OpenMetaverse.StructuredData.OSDMap;
using OpenSim.Services.Base;

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
        private object m_fetchLock = new Object();
        private IPrivateCapsService m_handler;
        
        #region Inventory

        public Hashtable FetchInventoryDescendentsRequest(Hashtable mDhttpMethod, UUID AgentID)
        {
            m_log.DebugFormat("[AGENT INVENTORY]: Received CAPS web fetch inventory request for {0}", AgentID);

            // nasty temporary hack here, the linden client falsely
            // identifies the uuid 00000000-0000-0000-0000-000000000000
            // as a string which breaks us
            //
            // correctly mark it as a uuid
            //
            string request = (string)mDhttpMethod["requestbody"];
            request = request.Replace("<string>00000000-0000-0000-0000-000000000000</string>", "<uuid>00000000-0000-0000-0000-000000000000</uuid>");

            // another hack <integer>1</integer> results in a
            // System.ArgumentException: Object type System.Int32 cannot
            // be converted to target type: System.Boolean
            //
            request = request.Replace("<key>fetch_folders</key><integer>0</integer>", "<key>fetch_folders</key><boolean>0</boolean>");
            request = request.Replace("<key>fetch_folders</key><integer>1</integer>", "<key>fetch_folders</key><boolean>1</boolean>");
            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(request));

            OSDArray foldersrequested = (OSDArray)map["folders"];

            string response = "";
            lock (m_fetchLock)
            {
                for (int i = 0; i < foldersrequested.Count; i++)
                {
                    string inventoryitemstr = "";
                    OSDMap requestedFolders = (OSDMap)foldersrequested[i];

                    inventoryitemstr = FetchInventoryReply(requestedFolders, AgentID, false);

                    inventoryitemstr = inventoryitemstr.Replace("<llsd><map><key>folders</key><array>", "");
                    inventoryitemstr = inventoryitemstr.Replace("</array></map></llsd>", "");

                    response += inventoryitemstr;
                }


                if (response.Length == 0)
                {
                    // Ter-guess: If requests fail a lot, the client seems to stop requesting descendants.
                    // Therefore, I'm concluding that the client only has so many threads available to do requests
                    // and when a thread stalls..   is stays stalled.
                    // Therefore we need to return something valid
                    response = "<llsd><map><key>folders</key><array /></map></llsd>";
                }
                else
                {
                    response = "<llsd><map><key>folders</key><array>" + response + "</array></map></llsd>";
                }

                //m_log.DebugFormat("[CAPS]: Replying to CAPS fetch inventory request with following xml");
                //m_log.Debug("[CAPS] "+response);

            }
            Hashtable cancelresponsedata = new Hashtable();
            cancelresponsedata["int_response_code"] = 200; //501; //410; //404;
            cancelresponsedata["content_type"] = "text/plain";
            cancelresponsedata["keepalive"] = false;
            cancelresponsedata["str_response_string"] = response;
            return cancelresponsedata;
        }

        public Hashtable FetchLibraryInventoryDescendentsRequest(Hashtable mDhttpMethod, UUID AgentID)
        {
            m_log.DebugFormat("[AGENT INVENTORY]: Received CAPS web fetch LIBRARY inventory request for {0}", AgentID);

            // nasty temporary hack here, the linden client falsely
            // identifies the uuid 00000000-0000-0000-0000-000000000000
            // as a string which breaks us
            //
            // correctly mark it as a uuid
            //
            string request = (string)mDhttpMethod["requestbody"];
            request = request.Replace("<string>00000000-0000-0000-0000-000000000000</string>", "<uuid>00000000-0000-0000-0000-000000000000</uuid>");

            // another hack <integer>1</integer> results in a
            // System.ArgumentException: Object type System.Int32 cannot
            // be converted to target type: System.Boolean
            //
            request = request.Replace("<key>fetch_folders</key><integer>0</integer>", "<key>fetch_folders</key><boolean>0</boolean>");
            request = request.Replace("<key>fetch_folders</key><integer>1</integer>", "<key>fetch_folders</key><boolean>1</boolean>");

            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(request));

            OSDArray foldersrequested = (OSDArray)map["folders"];

            string response = "";
            lock (m_fetchLock)
            {
                for (int i = 0; i < foldersrequested.Count; i++)
                {
                    string inventoryitemstr = "";
                    OSDMap requestedFolders = (OSDMap)foldersrequested[i];

                    inventoryitemstr = FetchInventoryReply(requestedFolders, AgentID, true);

                    inventoryitemstr = inventoryitemstr.Replace("<llsd><map><key>folders</key><array>", "");
                    inventoryitemstr = inventoryitemstr.Replace("</array></map></llsd>", "");

                    response += inventoryitemstr;
                }


                if (response.Length == 0)
                {
                    // Ter-guess: If requests fail a lot, the client seems to stop requesting descendants.
                    // Therefore, I'm concluding that the client only has so many threads available to do requests
                    // and when a thread stalls..   is stays stalled.
                    // Therefore we need to return something valid
                    response = "<llsd><map><key>folders</key><array /></map></llsd>";
                }
                else
                {
                    response = "<llsd><map><key>folders</key><array>" + response + "</array></map></llsd>";
                }

                //m_log.DebugFormat("[CAPS]: Replying to CAPS fetch inventory request with following xml");
                //m_log.Debug("[CAPS] "+response);

            }
            Hashtable cancelresponsedata = new Hashtable();
            cancelresponsedata["int_response_code"] = 200; //501; //410; //404;
            cancelresponsedata["content_type"] = "text/plain";
            cancelresponsedata["keepalive"] = false;
            cancelresponsedata["str_response_string"] = response;
            return cancelresponsedata;
        }

        public Hashtable FetchInventoryItemsRequest(Hashtable mDhttpMethod, UUID AgentID)
        {
            m_log.DebugFormat("[AGENT INVENTORY]: Received CAPS inventory items request for {0}", AgentID);

            // nasty temporary hack here, the linden client falsely
            // identifies the uuid 00000000-0000-0000-0000-000000000000
            // as a string which breaks us
            //
            // correctly mark it as a uuid
            //
            string request = (string)mDhttpMethod["requestbody"];
            request = request.Replace("<string>00000000-0000-0000-0000-000000000000</string>", "<uuid>00000000-0000-0000-0000-000000000000</uuid>");

            OSDMap requestmap = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(request));

            OSDArray foldersrequested = (OSDArray)requestmap["folders"];

            string response = "";
            lock (m_fetchLock)
            {
                OpenMetaverse.StructuredData.OSDMap map = new OpenMetaverse.StructuredData.OSDMap();
                map.Add("agent_id", OSD.FromUUID(AgentID));
                OpenMetaverse.StructuredData.OSDArray items = new OpenMetaverse.StructuredData.OSDArray();
                for (int i = 0; i < foldersrequested.Count; i++)
                {
                    OSDMap requestedFolders = (OSDMap)foldersrequested[i];
                    UUID owner_id = requestedFolders["owner_id"].AsUUID();
                    UUID item_id = requestedFolders["item_id"].AsUUID();
                    InventoryItemBase item = m_handler.InventoryService.GetItem(new InventoryItemBase(item_id, owner_id));
                    if (item != null)
                    {
                        items.Add(ConvertInventoryItem(item, owner_id));
                    }
                }
                map.Add("items", items);

                response = OSDParser.SerializeLLSDXmlString(map);

                //m_log.DebugFormat("[CAPS]: Replying to CAPS fetch inventory request with following xml");
                //m_log.Debug("[CAPS] "+response);

            }
            Hashtable cancelresponsedata = new Hashtable();
            cancelresponsedata["int_response_code"] = 200; //501; //410; //404;
            cancelresponsedata["content_type"] = "text/plain";
            cancelresponsedata["keepalive"] = false;
            cancelresponsedata["str_response_string"] = response;
            return cancelresponsedata;
        }

        public Hashtable FetchLibInventoryItemsRequest(Hashtable mDhttpMethod, UUID AgentID)
        {
            m_log.DebugFormat("[AGENT INVENTORY]: Received CAPS library inventory items request for {0}", AgentID);

            // nasty temporary hack here, the linden client falsely
            // identifies the uuid 00000000-0000-0000-0000-000000000000
            // as a string which breaks us
            //
            // correctly mark it as a uuid
            //
            string request = (string)mDhttpMethod["requestbody"];
            request = request.Replace("<string>00000000-0000-0000-0000-000000000000</string>", "<uuid>00000000-0000-0000-0000-000000000000</uuid>");

            OSDMap requestmap = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(request));

            OSDArray foldersrequested = (OSDArray)requestmap["folders"];

            string response = "";
            lock (m_fetchLock)
            {
                OpenMetaverse.StructuredData.OSDMap map = new OpenMetaverse.StructuredData.OSDMap();
                map.Add("agent_id", OSD.FromUUID(AgentID));
                OpenMetaverse.StructuredData.OSDArray items = new OpenMetaverse.StructuredData.OSDArray();
                for (int i = 0; i < foldersrequested.Count; i++)
                {
                    OSDMap requestedFolders = (OSDMap)foldersrequested[i];
                    UUID owner_id = requestedFolders["owner_id"].AsUUID();
                    UUID item_id = requestedFolders["item_id"].AsUUID();
                    InventoryItemBase item = null;
                    if (m_handler.LibraryService != null && m_handler.LibraryService.LibraryRootFolder != null)
                    {
                        item = m_handler.LibraryService.LibraryRootFolder.FindItem(item_id);
                    }
                    if (item == null) //Try normal inventory them
                        item = m_handler.InventoryService.GetItem(new InventoryItemBase(item_id, owner_id));
                    if (item != null)
                    {
                        items.Add(ConvertInventoryItem(item, owner_id));
                    }
                }
                map.Add("items", items);

                response = OSDParser.SerializeLLSDXmlString(map);

                //m_log.DebugFormat("[CAPS]: Replying to CAPS fetch inventory request with following xml");
                //m_log.Debug("[CAPS] "+response);

            }
            Hashtable cancelresponsedata = new Hashtable();
            cancelresponsedata["int_response_code"] = 200; //501; //410; //404;
            cancelresponsedata["content_type"] = "text/plain";
            cancelresponsedata["keepalive"] = false;
            cancelresponsedata["str_response_string"] = response;
            return cancelresponsedata;
        }

        /// <summary>
        /// Construct an LLSD reply packet to a CAPS inventory request
        /// </summary>
        /// <param name="invFetch"></param>
        /// <returns></returns>
        private string FetchInventoryReply(OSDMap invFetch, UUID AgentID, bool Library)
        {
            OSDMap contents = new OSDMap();

            UUID agent_id = invFetch["agent_id"].AsUUID();
            UUID owner_id = invFetch["owner_id"].AsUUID();
            UUID folder_id = invFetch["folder_id"].AsUUID();
            bool fetch_folders = invFetch["fetch_folders"].AsBoolean();
            bool fetch_items = invFetch["fetch_items"].AsBoolean();
            int sort_order = invFetch["sort_order"].AsInteger();
            OSDMap array = new OSDMap();
            array["agent_id"] = AgentID;
            array["owner_id"] = owner_id;
            array["folder_id"] = folder_id;
            contents["folders"] = new OSDArray();
            ((OSDArray)contents["folders"]).Add(array);
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
            contents["categories"] = categories;

            OSDArray items = new OSDArray();
            if (inv.Items != null)
            {
                foreach (InventoryItemBase invItem in inv.Items)
                {
                    items.Add(ConvertInventoryItem(invItem, AgentID));
                }
            }
            contents["items"] = items;

            contents["descendents"] = items.Count + categories.Count;
            contents["version"] = version;

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
            m_log.DebugFormat(
                "[INVENTORY CACHE]: Fetching folders ({0}), items ({1}) from {2} for agent {3}",
                fetchFolders, fetchItems, folderID, agentID);

            // FIXME MAYBE: We're not handling sortOrder!

            InventoryFolderImpl fold;

            InventoryCollection contents = new InventoryCollection();
            //if (Library)
            // {
            //version = 0;
            if (m_handler.LibraryService != null && m_handler.LibraryService.LibraryRootFolder != null)
            {
                if ((fold = m_handler.LibraryService.LibraryRootFolder.FindFolder(folderID)) != null)
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
                contents = m_handler.InventoryService.GetFolderContent(agentID, folderID);
            }
            if (fetchItems)
            {
                contents.Items = m_handler.InventoryService.GetFolderItems(agentID, folderID);
            }
            InventoryFolderBase containingFolder = new InventoryFolderBase();
            containingFolder.ID = folderID;
            containingFolder.Owner = agentID;
            containingFolder = m_handler.InventoryService.GetFolder(containingFolder);
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

        #region ICapsServiceConnector Members

        public List<IRequestHandler> RegisterCaps(UUID agentID, IHttpServer server, IPrivateCapsService handler)
        {
            m_handler = handler;
            List<IRequestHandler> handlers = new List<IRequestHandler>();

            GenericHTTPMethod method = delegate(Hashtable httpMethod)
            {
                return FetchInventoryDescendentsRequest(httpMethod, agentID);
            };
            handlers.Add(new RestHTTPHandler("POST", m_handler.CreateCAPS("WebFetchInventoryDescendents"),
                                                      method));

            method = delegate(Hashtable httpMethod)
            {
                return FetchLibraryInventoryDescendentsRequest(httpMethod, agentID);
            };
            handlers.Add(new RestHTTPHandler("POST", m_handler.CreateCAPS("FetchLibDescendents"),
                                                      method));

            method = delegate(Hashtable httpMethod)
            {
                return FetchInventoryItemsRequest(httpMethod, agentID);
            };
            handlers.Add(new RestHTTPHandler("POST", m_handler.CreateCAPS("FetchInventory"),
                                                      method));

            method = delegate(Hashtable httpMethod)
            {
                return FetchLibInventoryItemsRequest(httpMethod, agentID);
            };
            handlers.Add(new RestHTTPHandler("POST", m_handler.CreateCAPS("FetchLib"),
                                                      method));

            return handlers;
        }

        #endregion
    }
}
