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

        /// <summary>
        /// Processes a fetch inventory request and sends the reply
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        // Request is like:
        //<llsd>
        //   <map><key>folders</key>
        //       <array>
        //            <map>
        //                <key>fetch-folders</key><boolean>1</boolean><key>fetch-items</key><boolean>1</boolean><key>folder-id</key><uuid>8e1e3a30-b9bf-11dc-95ff-0800200c9a66</uuid><key>owner-id</key><uuid>11111111-1111-0000-0000-000100bba000</uuid><key>sort-order</key><integer>1</integer>
        //            </map>
        //       </array>
        //   </map>
        //</llsd>
        //
        // multiple fetch-folder maps are allowed within the larger folders map.
        public Hashtable FetchInventoryRequest(Hashtable mDhttpMethod, UUID agentID)
        {
            m_log.DebugFormat("[AGENT INVENTORY]: Received CAPS fetch inventory request for {0}", agentID);

            Hashtable hash = new Hashtable();
            try
            {
                hash = (Hashtable)LLSD.LLSDDeserialize(OpenMetaverse.Utils.StringToBytes((string)mDhttpMethod["requestbody"]));
            }
            catch (LLSD.LLSDParseException pe)
            {
                m_log.Error("[AGENT INVENTORY]: Fetch error: " + pe.Message);
                //m_log.Error("Request: " + request.ToString());
            }

            ArrayList foldersrequested = (ArrayList)hash["folders"];

            string response = "";

            for (int i = 0; i < foldersrequested.Count; i++)
            {
                string inventoryitemstr = "";
                Hashtable inventoryhash = (Hashtable)foldersrequested[i];

                LLSDFetchInventoryDescendents llsdRequest = new LLSDFetchInventoryDescendents();
                LLSDHelpers.DeserialiseOSDMap(inventoryhash, llsdRequest);
                LLSDInventoryDescendents reply = FetchInventoryReply(llsdRequest, agentID, false);

                inventoryitemstr = LLSDHelpers.SerialiseLLSDReply(reply);
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

            //m_log.DebugFormat("[AGENT INVENTORY]: Replying to CAPS fetch inventory request with following xml");
            //m_log.Debug(Util.GetFormattedXml(response));
            Hashtable cancelresponsedata = new Hashtable();
            cancelresponsedata["int_response_code"] = 200; //501; //410; //404;
            cancelresponsedata["content_type"] = "text/plain";
            cancelresponsedata["keepalive"] = false;
            cancelresponsedata["str_response_string"] = response;
            return cancelresponsedata;
        }

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

            Hashtable hash = new Hashtable();
            try
            {
                hash = (Hashtable)LLSD.LLSDDeserialize(OpenMetaverse.Utils.StringToBytes(request));
            }
            catch (LLSD.LLSDParseException pe)
            {
                m_log.Error("[AGENT INVENTORY]: Fetch error: " + pe.Message);
                m_log.Error("Request: " + request.ToString());
            }

            ArrayList foldersrequested = (ArrayList)hash["folders"];

            string response = "";
            lock (m_fetchLock)
            {
                for (int i = 0; i < foldersrequested.Count; i++)
                {
                    string inventoryitemstr = "";
                    Hashtable inventoryhash = (Hashtable)foldersrequested[i];

                    LLSDFetchInventoryDescendents llsdRequest = new LLSDFetchInventoryDescendents();

                    try
                    {
                        LLSDHelpers.DeserialiseOSDMap(inventoryhash, llsdRequest);
                    }
                    catch (Exception e)
                    {
                        m_log.Debug("[CAPS]: caught exception doing OSD deserialize" + e);
                    }
                    LLSDInventoryDescendents reply = FetchInventoryReply(llsdRequest, AgentID, false);

                    inventoryitemstr = LLSDHelpers.SerialiseLLSDReply(reply);
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

            Hashtable hash = new Hashtable();
            try
            {
                hash = (Hashtable)LLSD.LLSDDeserialize(OpenMetaverse.Utils.StringToBytes(request));
            }
            catch (LLSD.LLSDParseException pe)
            {
                m_log.Error("[AGENT INVENTORY]: Fetch error: " + pe.Message);
                m_log.Error("Request: " + request.ToString());
            }

            ArrayList foldersrequested = (ArrayList)hash["folders"];

            string response = "";
            lock (m_fetchLock)
            {
                for (int i = 0; i < foldersrequested.Count; i++)
                {
                    string inventoryitemstr = "";
                    Hashtable inventoryhash = (Hashtable)foldersrequested[i];

                    LLSDFetchInventoryDescendents llsdRequest = new LLSDFetchInventoryDescendents();

                    try
                    {
                        LLSDHelpers.DeserialiseOSDMap(inventoryhash, llsdRequest);
                    }
                    catch (Exception e)
                    {
                        m_log.Debug("[CAPS]: caught exception doing OSD deserialize" + e);
                    }
                    LLSDInventoryDescendents reply = FetchInventoryReply(llsdRequest, AgentID, true);

                    inventoryitemstr = LLSDHelpers.SerialiseLLSDReply(reply);
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

            Hashtable hash = new Hashtable();
            try
            {
                hash = (Hashtable)LLSD.LLSDDeserialize(OpenMetaverse.Utils.StringToBytes(request));
            }
            catch (LLSD.LLSDParseException pe)
            {
                m_log.Error("[AGENT INVENTORY]: Fetch error: " + pe.Message);
                m_log.Error("Request: " + request.ToString());
            }

            ArrayList foldersrequested = (ArrayList)hash["items"];

            string response = "";
            lock (m_fetchLock)
            {
                OpenMetaverse.StructuredData.OSDMap map = new OpenMetaverse.StructuredData.OSDMap();
                map.Add("agent_id", OSD.FromUUID(AgentID));
                OpenMetaverse.StructuredData.OSDArray items = new OpenMetaverse.StructuredData.OSDArray();
                for (int i = 0; i < foldersrequested.Count; i++)
                {
                    Hashtable inventoryhash = (Hashtable)foldersrequested[i];

                    LLSDFetchInventory llsdRequest = new LLSDFetchInventory();

                    try
                    {
                        LLSDHelpers.DeserialiseOSDMap(inventoryhash, llsdRequest);
                    }
                    catch (Exception e)
                    {
                        m_log.Debug("[CAPS]: caught exception doing OSD deserialize" + e);
                    }
                    InventoryItemBase item = m_handler.InventoryService.GetItem(new InventoryItemBase(llsdRequest.item_id, llsdRequest.owner_id));
                    if (item != null)
                    {
                        LLSDInventoryItem reply = ConvertInventoryItem(item, llsdRequest.owner_id);
                        OpenMetaverse.StructuredData.OSDMap itemMap = new OpenMetaverse.StructuredData.OSDMap();
                        itemMap.Add("agent_id", reply.agent_id);
                        itemMap.Add("asset_id", reply.asset_id);
                        itemMap.Add("created_at", reply.created_at);
                        itemMap.Add("desc", reply.desc);
                        itemMap.Add("flags", reply.flags);
                        itemMap.Add("inv_type", reply.inv_type);
                        itemMap.Add("item_id", reply.item_id);
                        itemMap.Add("name", reply.name);
                        itemMap.Add("parent_id", reply.parent_id);
                        OpenMetaverse.StructuredData.OSDMap permissions = new OpenMetaverse.StructuredData.OSDMap();
                        permissions.Add("base_mask", reply.permissions.base_mask);
                        permissions.Add("creator_id", reply.permissions.creator_id);
                        permissions.Add("everyone_mask", reply.permissions.everyone_mask);
                        permissions.Add("group_id", reply.permissions.group_id);
                        permissions.Add("group_mask", reply.permissions.group_mask);
                        permissions.Add("is_owner_group", reply.permissions.is_owner_group);
                        permissions.Add("last_owner_id", reply.permissions.last_owner_id);
                        permissions.Add("next_owner_mask", reply.permissions.next_owner_mask);
                        permissions.Add("owner_id", reply.permissions.owner_id);
                        permissions.Add("owner_mask", reply.permissions.owner_mask);
                        itemMap.Add("permissions", permissions);
                        OpenMetaverse.StructuredData.OSDMap sales = new OpenMetaverse.StructuredData.OSDMap();
                        itemMap.Add("sale_price", reply.sale_info.sale_price);
                        itemMap.Add("sale_type", reply.sale_info.sale_type);
                        itemMap.Add("sale_info", sales);
                        itemMap.Add("type", reply.type);
                        items.Add(itemMap);
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

            Hashtable hash = new Hashtable();
            try
            {
                hash = (Hashtable)LLSD.LLSDDeserialize(OpenMetaverse.Utils.StringToBytes(request));
            }
            catch (LLSD.LLSDParseException pe)
            {
                m_log.Error("[AGENT INVENTORY]: Fetch error: " + pe.Message);
                m_log.Error("Request: " + request.ToString());
            }

            ArrayList foldersrequested = (ArrayList)hash["items"];

            string response = "";
            lock (m_fetchLock)
            {
                OpenMetaverse.StructuredData.OSDMap map = new OpenMetaverse.StructuredData.OSDMap();
                map.Add("agent_id", OSD.FromUUID(AgentID));
                OpenMetaverse.StructuredData.OSDArray items = new OpenMetaverse.StructuredData.OSDArray();
                for (int i = 0; i < foldersrequested.Count; i++)
                {
                    Hashtable inventoryhash = (Hashtable)foldersrequested[i];

                    LLSDFetchInventory llsdRequest = new LLSDFetchInventory();

                    try
                    {
                        LLSDHelpers.DeserialiseOSDMap(inventoryhash, llsdRequest);
                    }
                    catch (Exception e)
                    {
                        m_log.Debug("[CAPS]: caught exception doing OSD deserialize" + e);
                    }
                    InventoryItemBase item = null;
                    if (m_handler.LibraryService != null && m_handler.LibraryService.LibraryRootFolder != null)
                    {
                        item = m_handler.LibraryService.LibraryRootFolder.FindItem(llsdRequest.item_id);
                    }
                    if (item == null) //Try normal inventory them
                        item = m_handler.InventoryService.GetItem(new InventoryItemBase(llsdRequest.item_id, llsdRequest.owner_id));
                    if (item != null)
                    {
                        LLSDInventoryItem reply = ConvertInventoryItem(item, llsdRequest.owner_id);
                        OpenMetaverse.StructuredData.OSDMap itemMap = new OpenMetaverse.StructuredData.OSDMap();
                        itemMap.Add("agent_id", reply.agent_id);
                        itemMap.Add("asset_id", reply.asset_id);
                        itemMap.Add("created_at", reply.created_at);
                        itemMap.Add("desc", reply.desc);
                        itemMap.Add("flags", reply.flags);
                        itemMap.Add("inv_type", reply.inv_type);
                        itemMap.Add("item_id", reply.item_id);
                        itemMap.Add("name", reply.name);
                        itemMap.Add("parent_id", reply.parent_id);
                        OpenMetaverse.StructuredData.OSDMap permissions = new OpenMetaverse.StructuredData.OSDMap();
                        permissions.Add("base_mask", reply.permissions.base_mask);
                        permissions.Add("creator_id", reply.permissions.creator_id);
                        permissions.Add("everyone_mask", reply.permissions.everyone_mask);
                        permissions.Add("group_id", reply.permissions.group_id);
                        permissions.Add("group_mask", reply.permissions.group_mask);
                        permissions.Add("is_owner_group", reply.permissions.is_owner_group);
                        permissions.Add("last_owner_id", reply.permissions.last_owner_id);
                        permissions.Add("next_owner_mask", reply.permissions.next_owner_mask);
                        permissions.Add("owner_id", reply.permissions.owner_id);
                        permissions.Add("owner_mask", reply.permissions.owner_mask);
                        itemMap.Add("permissions", permissions);
                        OpenMetaverse.StructuredData.OSDMap sales = new OpenMetaverse.StructuredData.OSDMap();
                        itemMap.Add("sale_price", reply.sale_info.sale_price);
                        itemMap.Add("sale_type", reply.sale_info.sale_type);
                        itemMap.Add("sale_info", sales);
                        itemMap.Add("type", reply.type);
                        items.Add(itemMap);
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
        private LLSDInventoryDescendents FetchInventoryReply(LLSDFetchInventoryDescendents invFetch, UUID AgentID, bool Library)
        {
            LLSDInventoryDescendents reply = new LLSDInventoryDescendents();
            LLSDInventoryFolderContents contents = new LLSDInventoryFolderContents();
            contents.agent_id = AgentID;
            contents.owner_id = invFetch.owner_id;
            contents.folder_id = invFetch.folder_id;

            reply.folders.Array.Add(contents);
            InventoryCollection inv = new InventoryCollection();
            inv.Folders = new List<InventoryFolderBase>();
            inv.Items = new List<InventoryItemBase>();
            int version = 0;
            inv = HandleFetchInventoryDescendentsCAPS(AgentID, invFetch.folder_id, invFetch.owner_id, invFetch.fetch_folders, invFetch.fetch_items, invFetch.sort_order, Library, out version);

            if (inv.Folders != null)
            {
                foreach (InventoryFolderBase invFolder in inv.Folders)
                {
                    contents.categories.Array.Add(ConvertInventoryFolder(invFolder));
                }
            }

            if (inv.Items != null)
            {
                foreach (InventoryItemBase invItem in inv.Items)
                {
                    contents.items.Array.Add(ConvertInventoryItem(invItem, AgentID));
                }
            }

            contents.descendents = contents.items.Array.Count + contents.categories.Array.Count;
            contents.version = version;

            return reply;
        }

        /// <summary>
        /// Convert an internal inventory folder object into an LLSD object.
        /// </summary>
        /// <param name="invFolder"></param>
        /// <returns></returns>
        private LLSDInventoryFolder ConvertInventoryFolder(InventoryFolderBase invFolder)
        {
            LLSDInventoryFolder llsdFolder = new LLSDInventoryFolder();
            llsdFolder.folder_id = invFolder.ID;
            llsdFolder.parent_id = invFolder.ParentID;
            llsdFolder.name = invFolder.Name;
            if (invFolder.Type < 0 || invFolder.Type >= TaskInventoryItem.Types.Length)
                llsdFolder.type = "0";
            else
                llsdFolder.type = TaskInventoryItem.Types[invFolder.Type];
            llsdFolder.preferred_type = "0";

            return llsdFolder;
        }

        /// <summary>
        /// Convert an internal inventory item object into an LLSD object.
        /// </summary>
        /// <param name="invItem"></param>
        /// <returns></returns>
        private LLSDInventoryItem ConvertInventoryItem(InventoryItemBase invItem, UUID AgentID)
        {
            LLSDInventoryItem llsdItem = new LLSDInventoryItem();
            llsdItem.agent_id = AgentID;
            llsdItem.asset_id = invItem.AssetID;
            llsdItem.created_at = invItem.CreationDate;
            llsdItem.desc = invItem.Description;
            llsdItem.flags = (int)invItem.Flags;
            llsdItem.item_id = invItem.ID;
            llsdItem.name = invItem.Name;
            llsdItem.parent_id = invItem.Folder;
            try
            {

                // TODO reevaluate after upgrade to libomv >= r2566. Probably should use UtilsConversions.
                llsdItem.type = TaskInventoryItem.Types[invItem.AssetType];
                llsdItem.inv_type = TaskInventoryItem.InvTypes[invItem.InvType];
                //llsdItem.type = Utils.InventoryTypeToString((InventoryType)invItem.AssetType);
                //llsdItem.inv_type = Utils.InventoryTypeToString((InventoryType)invItem.InvType);
            }
            catch (Exception e)
            {
                m_log.Error("[CAPS]: Problem setting asset/inventory type while converting inventory item " + invItem.Name + " to LLSD:", e);
            }
            llsdItem.permissions = new LLSDPermissions();
            llsdItem.permissions.creator_id = invItem.CreatorIdAsUuid;
            llsdItem.permissions.base_mask = (int)invItem.CurrentPermissions;
            llsdItem.permissions.everyone_mask = (int)invItem.EveryOnePermissions;
            llsdItem.permissions.group_id = invItem.GroupID;
            llsdItem.permissions.group_mask = (int)invItem.GroupPermissions;
            llsdItem.permissions.is_owner_group = invItem.GroupOwned;
            llsdItem.permissions.next_owner_mask = (int)invItem.NextPermissions;
            llsdItem.permissions.last_owner_id = invItem.Owner; //Err... can't set this?
            llsdItem.permissions.owner_id = AgentID;
            llsdItem.permissions.owner_mask = (int)invItem.CurrentPermissions;

            llsdItem.sale_info = new LLSDSaleInfo();
            llsdItem.sale_info.sale_price = invItem.SalePrice;
            switch (invItem.SaleType)
            {
                default:
                    llsdItem.sale_info.sale_type = "not";
                    break;
                case 1:
                    llsdItem.sale_info.sale_type = "original";
                    break;
                case 2:
                    llsdItem.sale_info.sale_type = "copy";
                    break;
                case 3:
                    llsdItem.sale_info.sale_type = "contents";
                    break;
            }

            return llsdItem;
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
                return FetchInventoryRequest(httpMethod, agentID);
            };
            handlers.Add(new RestHTTPHandler("POST", m_handler.CreateCAPS("FetchInventoryDescendents"),
                                                      method));

            method = delegate(Hashtable httpMethod)
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
