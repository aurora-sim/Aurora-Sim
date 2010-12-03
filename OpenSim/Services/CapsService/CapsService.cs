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
    public class AuroraCAPSHandler : ServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public IHttpServer m_server = null;
        public static List<ICapsServiceConnector> CapsModules = new List<ICapsServiceConnector>();

        public AuroraCAPSHandler(IConfigSource config, IHttpServer server, string configName) :
            base(config, server, configName)
        {
            m_log.Debug("[AuroraCAPSService]: Starting...");
            IConfig m_CAPSServerConfig = config.Configs["CAPSService"];
            if (m_CAPSServerConfig == null)
                throw new Exception(String.Format("No section CAPSService in config file"));

            m_server = server;
            Object[] args = new Object[] { config };
            string invService = m_CAPSServerConfig.GetString("InventoryService", String.Empty);
            string libService = m_CAPSServerConfig.GetString("LibraryService", String.Empty);
            string guService = m_CAPSServerConfig.GetString("GridUserService", String.Empty);
            string presenceService = m_CAPSServerConfig.GetString("PresenceService", String.Empty);
            string Password = m_CAPSServerConfig.GetString("Password", String.Empty);
            string HostName = m_CAPSServerConfig.GetString("HostName", String.Empty);
            IInventoryService m_InventoryService = ServerUtils.LoadPlugin<IInventoryService>(invService, args);
            ILibraryService m_LibraryService = ServerUtils.LoadPlugin<ILibraryService>(libService, args);
            IGridUserService m_GridUserService = ServerUtils.LoadPlugin<IGridUserService>(guService, args);
            IPresenceService m_PresenceService = ServerUtils.LoadPlugin<IPresenceService>(presenceService, args);
            CapsModules = Aurora.Framework.AuroraModuleLoader.PickupModules<ICapsServiceConnector>();
            //This handler allows sims to post CAPS for their sims on the CAPS server.
            server.AddStreamHandler(new CAPSPublicHandler(server, Password, m_InventoryService, m_LibraryService, m_GridUserService, m_PresenceService, HostName));
        }
    }

    /// <summary>
    /// This handles the seed requests from the client and forwards the request onto the the simulator
    /// </summary>
    public class CAPSPrivateSeedHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IGridUserService m_GridUserService;
        private IPresenceService m_PresenceService;
        private IInventoryService m_InventoryService;
        private ILibraryService m_LibraryService;
        private IHttpServer m_server;
        private object m_fetchLock = new Object();
        private string SimToInform;
        private UUID m_AgentID;
        //X cap name to path
        public Hashtable registeredCAPS = new Hashtable();
        //Paths to X cap
        public Hashtable registeredCAPSPath = new Hashtable();
        private CAPSEQMHandler EQMHandler = new CAPSEQMHandler();
        private string m_HostName;
        public OSDMap postToSendToSim = new OSDMap();

        public CAPSPrivateSeedHandler(IHttpServer server, IInventoryService inventoryService, ILibraryService libraryService, IGridUserService guService, IPresenceService presenceService, string URL, UUID agentID, string HostName)
        {
            m_server = server;
            m_InventoryService = inventoryService;
            m_LibraryService = libraryService;
            m_GridUserService = guService;
            m_PresenceService = presenceService;
            SimToInform = URL;
            m_AgentID = agentID;
            m_HostName = HostName;

            if(m_server != null)
                AddServerCAPS();
        }

        public List<IRequestHandler> GetServerCAPS()
        {
            List<IRequestHandler> handlers = new List<IRequestHandler>();

            handlers.Add(EQMHandler.RegisterCap(m_AgentID, this));

            GenericHTTPMethod method = delegate(Hashtable httpMethod)
            {
                return ProcessUpdateAgentLanguage(httpMethod, m_AgentID);
            };
            handlers.Add(new RestHTTPHandler("POST", CreateCAPS("UpdateAgentLanguage"),
                                                      method));
            method = delegate(Hashtable httpMethod)
            {
                return ProcessUpdateAgentInfo(httpMethod, m_AgentID);
            };
            handlers.Add(new RestHTTPHandler("POST", CreateCAPS("UpdateAgentInformation"),
                                                      method));

            method = delegate(Hashtable httpMethod)
            {
                return FetchInventoryRequest(httpMethod, m_AgentID);
            };
            handlers.Add(new RestHTTPHandler("POST", CreateCAPS("FetchInventoryDescendents"),
                                                      method));

            method = delegate(Hashtable httpMethod)
            {
                return HomeLocation(httpMethod, m_AgentID);
            };
            handlers.Add(new RestHTTPHandler("POST", CreateCAPS("HomeLocation"),
                                                      method));

            method = delegate(Hashtable httpMethod)
            {
                return FetchInventoryDescendentsRequest(httpMethod, m_AgentID);
            };
            handlers.Add(new RestHTTPHandler("POST", CreateCAPS("WebFetchInventoryDescendents"),
                                                      method));

            method = delegate(Hashtable httpMethod)
            {
                return FetchLibraryInventoryDescendentsRequest(httpMethod, m_AgentID);
            };
            handlers.Add(new RestHTTPHandler("POST", CreateCAPS("FetchLibDescendents"),
                                                      method));

            method = delegate(Hashtable httpMethod)
            {
                return FetchInventoryItemsRequest(httpMethod, m_AgentID);
            };
            handlers.Add(new RestHTTPHandler("POST", CreateCAPS("FetchInventory"),
                                                      method));

            method = delegate(Hashtable httpMethod)
            {
                return FetchLibInventoryItemsRequest(httpMethod, m_AgentID);
            };
            handlers.Add(new RestHTTPHandler("POST", CreateCAPS("FetchLib"),
                                                      method));

            foreach (ICapsServiceConnector conn in AuroraCAPSHandler.CapsModules)
            {
                handlers.AddRange(conn.RegisterCaps(m_AgentID));
            }

            return handlers;
        }

        private void AddServerCAPS()
        {
            List<IRequestHandler> handlers = GetServerCAPS();
            foreach (IRequestHandler handle in handlers)
            {
                m_server.AddStreamHandler(handle);
            }
        }

        public string CapsRequest(string request, string path, string param,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        SimToInform,
                        OSDParser.SerializeLLSDXmlString(postToSendToSim));
                if (reply != "")
                {
                    Hashtable hash = (Hashtable)LLSD.LLSDDeserialize(OpenMetaverse.Utils.StringToBytes(reply));
                    foreach (string key in hash.Keys)
                    {
                        if (!registeredCAPS.ContainsKey(key))
                            registeredCAPS[key] = hash[key];
                        //else
                        //    m_log.WarnFormat("[CAPSService]: Simulator tried to override grid CAPS setting! @ {0}", SimToInform);
                    }
                }
            }
            catch
            {
            }
            return LLSDHelpers.SerialiseLLSDReply(registeredCAPS);
        }

        private string CreateCAPS(string method)
        {
            string caps = "/CAPS/" + method + "/" + UUID.Random() + "/";
            registeredCAPS[method] = m_HostName + caps;
            registeredCAPSPath[m_HostName + caps] = method;
            return caps;
        }

        #region Other CAPS

        private Hashtable HomeLocation(Hashtable mDhttpMethod, UUID agentID)
        {
            OpenMetaverse.StructuredData.OSDMap rm = (OpenMetaverse.StructuredData.OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
            OpenMetaverse.StructuredData.OSDMap HomeLocation = rm["HomeLocation"] as OpenMetaverse.StructuredData.OSDMap;
            OpenMetaverse.StructuredData.OSDMap pos = HomeLocation["LocationPos"] as OpenMetaverse.StructuredData.OSDMap;
            Vector3 position = new Vector3((float)pos["X"].AsReal(),
                (float)pos["Y"].AsReal(),
                (float)pos["Z"].AsReal());
            OpenMetaverse.StructuredData.OSDMap lookat = HomeLocation["LocationLookAt"] as OpenMetaverse.StructuredData.OSDMap;
            Vector3 lookAt = new Vector3((float)lookat["X"].AsReal(),
                (float)lookat["Y"].AsReal(),
                (float)lookat["Z"].AsReal());
            int locationID = HomeLocation["LocationId"].AsInteger();

            PresenceInfo presence = m_PresenceService.GetAgents(new string[]{agentID.ToString()})[0];
            bool r = m_GridUserService.SetHome(agentID.ToString(), presence.RegionID, position, lookAt);

            rm.Add("success", OSD.FromBoolean(r));

            //Send back data
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(rm);
            return responsedata;
        }

        private Hashtable ProcessUpdateAgentLanguage(Hashtable m_dhttpMethod, UUID agentID)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";

            OpenMetaverse.StructuredData.OSD r = OpenMetaverse.StructuredData.OSDParser.DeserializeLLSDXml((string)m_dhttpMethod["requestbody"]);
            OpenMetaverse.StructuredData.OSDMap rm = (OpenMetaverse.StructuredData.OSDMap)r;
            IAgentConnector AgentFrontend = DataManager.RequestPlugin<IAgentConnector>();
            if (AgentFrontend != null)
            {
                IAgentInfo IAI = AgentFrontend.GetAgent(agentID);
                if (IAI == null)
                    return responsedata;
                IAI.Language = rm["language"].AsString();
                IAI.LanguageIsPublic = int.Parse(rm["language_is_public"].AsString()) == 1;
                AgentFrontend.UpdateAgent(IAI);
            }
            return responsedata;
        }

        private Hashtable ProcessUpdateAgentInfo(Hashtable mDhttpMethod, UUID agentID)
        {
            OpenMetaverse.StructuredData.OSD r = OpenMetaverse.StructuredData.OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
            OpenMetaverse.StructuredData.OSDMap rm = (OpenMetaverse.StructuredData.OSDMap)r;
            OpenMetaverse.StructuredData.OSDMap access = (OpenMetaverse.StructuredData.OSDMap)rm["access_prefs"];
            string Level = access["max"].AsString();
            int maxLevel = 0;
            if (Level == "PG")
                maxLevel = 0;
            if (Level == "M")
                maxLevel = 1;
            if (Level == "A")
                maxLevel = 2;
            IAgentConnector data = DataManager.RequestPlugin<IAgentConnector>();
            if (data != null)
            {
                IAgentInfo agent = data.GetAgent(agentID);
                agent.MaturityRating = maxLevel;
                data.UpdateAgent(agent);
            }
            Hashtable cancelresponsedata = new Hashtable();
            cancelresponsedata["int_response_code"] = 200; //501; //410; //404;
            cancelresponsedata["content_type"] = "text/plain";
            cancelresponsedata["keepalive"] = false;
            cancelresponsedata["str_response_string"] = "";
            return cancelresponsedata;
        }

        #endregion

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
                    InventoryItemBase item = m_InventoryService.GetItem(new InventoryItemBase(llsdRequest.item_id, llsdRequest.owner_id));
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
                    if (m_LibraryService != null && m_LibraryService.LibraryRootFolder != null)
                    {
                        item = m_LibraryService.LibraryRootFolder.FindItem(llsdRequest.item_id);
                    }
                    if(item == null) //Try normal inventory them
                        item = m_InventoryService.GetItem(new InventoryItemBase(llsdRequest.item_id, llsdRequest.owner_id));
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
                if (m_LibraryService != null && m_LibraryService.LibraryRootFolder != null)
                {
                    if ((fold = m_LibraryService.LibraryRootFolder.FindFolder(folderID)) != null)
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
                contents = m_InventoryService.GetFolderContent(agentID, folderID);
            }
            if (fetchItems)
            {
                contents.Items = m_InventoryService.GetFolderItems(agentID, folderID);
            }
                InventoryFolderBase containingFolder = new InventoryFolderBase();
                containingFolder.ID = folderID;
                containingFolder.Owner = agentID;
                containingFolder = m_InventoryService.GetFolder(containingFolder);
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
    }

    #region Public handler

    /// <summary>
    /// This handles requests from the user server about clients that need a CAPS seed URL.
    /// </summary>
    public class CAPSPublicHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IHttpServer m_server;
        private string CAPSPass;
        private IGridUserService m_GridUserService;
        private IPresenceService m_PresenceService;
        private IInventoryService m_inventory;
        private ILibraryService m_library;
        private string m_hostName;

        public CAPSPublicHandler(IHttpServer server, string pass, IInventoryService inventory, ILibraryService library, IGridUserService guService, IPresenceService presenceService, string hostName) :
            base("POST", "/CAPS/REGISTER")
        {
            m_server = server;
            CAPSPass = pass;
            m_inventory = inventory;
            m_library = library;
            m_GridUserService = guService;
            m_PresenceService = presenceService;
            m_hostName = hostName;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            //m_log.DebugFormat("[XXX]: query String: {0}", body);
            string method = string.Empty;
            try
            {
                Dictionary<string, object> request = new Dictionary<string, object>();
                request = ServerUtils.ParseQueryString(body);
                if (request.Count == 1)
                    request = ServerUtils.ParseXmlResponse(body);
                object value = null;
                request.TryGetValue("<?xml version", out value);
                if (value != null)
                    request = ServerUtils.ParseXmlResponse(body);

                return ProcessAddCAP(request);
            }
            catch (Exception)
            {
            }

            return null;

        }

        private byte[] ProcessAddCAP(Dictionary<string, object> m_dhttpMethod)
        {
            //This is called by the user server
            if ((string)m_dhttpMethod["PASS"] != CAPSPass)
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                result.Add("result", "false");
                string xmlString = ServerUtils.BuildXmlResponse(result);
                UTF8Encoding encoding = new UTF8Encoding();
                return encoding.GetBytes(xmlString);
            }
            else
            {
                string CAPS = (string)m_dhttpMethod["CAPSSEEDPATH"];
                object SimCaps = m_dhttpMethod["SIMCAPS"];
                string simCAPS = SimCaps.ToString();
                UUID AgentID = UUID.Parse((string)m_dhttpMethod["AGENTID"]);

                //The client calls this to find out all the CAPS
                CreateCAPS(AgentID, simCAPS, CAPS);

                Dictionary<string, object> result = new Dictionary<string, object>();
                result.Add("result", "true");
                string xmlString = ServerUtils.BuildXmlResponse(result);
                UTF8Encoding encoding = new UTF8Encoding();
                return encoding.GetBytes(xmlString);
            }
        }

        private void CreateCAPS(UUID AgentID, string SimCAPS, string CAPS)
        {
            //This makes the new SEED url on the CAPS server
            m_server.AddStreamHandler(new RestStreamHandler("POST", CAPS, new CAPSPrivateSeedHandler(m_server, m_inventory, m_library, m_GridUserService, m_PresenceService, SimCAPS, AgentID, m_hostName).CapsRequest));
        }
    }

    #endregion

    #region EQM

    public class CAPSEQMHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<UUID, int> m_ids = new Dictionary<UUID, int>();

        private Dictionary<UUID, Queue<OSD>> queues = new Dictionary<UUID, Queue<OSD>>();
        private Dictionary<UUID, UUID> m_QueueUUIDAvatarMapping = new Dictionary<UUID, UUID>();
        private Dictionary<UUID, UUID> m_AvatarQueueUUIDMapping = new Dictionary<UUID, UUID>();
        private Dictionary<UUID, UUID> m_AvatarPasswordMap = new Dictionary<UUID, UUID>();

        /// <summary>
        ///  Always returns a valid queue
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        private Queue<OSD> TryGetQueue(UUID agentId)
        {
            lock (queues)
            {
                if (!queues.ContainsKey(agentId))
                {
                    queues[agentId] = new Queue<OSD>();
                }

                return queues[agentId];
            }
        }

        /// <summary>
        /// May return a null queue
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        private Queue<OSD> GetQueue(UUID agentId)
        {
            lock (queues)
            {
                if (queues.ContainsKey(agentId))
                {
                    return queues[agentId];
                }
                else
                    return null;
            }
        }

        public bool Enqueue(OSD ev, UUID avatarID)
        {
            try
            {
                Queue<OSD> queue = GetQueue(avatarID);
                if (queue != null)
                    queue.Enqueue(ev);
            }
            catch (NullReferenceException e)
            {
                m_log.Error("[EVENTQUEUE] Caught exception: " + e);
                return false;
            }

            return true;
        }

        /*private void ClientClosed(UUID AgentID, Scene scene)
        {
            //m_log.DebugFormat("[EVENTQUEUE]: Closed client {0} in region {1}", AgentID, m_scene.RegionInfo.RegionName);

            //Errr... shouldn't we just close the client?
            int count = 0;
            while (queues.ContainsKey(AgentID) && queues[AgentID].Count > 0 && count++ < 2)
            {
                Thread.Sleep(100);
            }

            lock (queues)
            {
                queues.Remove(AgentID);
            }
            List<UUID> removeitems = new List<UUID>();
            lock (m_AvatarQueueUUIDMapping)
            {
                foreach (UUID ky in m_AvatarQueueUUIDMapping.Keys)
                {
                    if (ky == AgentID)
                    {
                        removeitems.Add(ky);
                    }
                }

                foreach (UUID ky in removeitems)
                {
                    m_AvatarQueueUUIDMapping.Remove(ky);
                    MainServer.Instance.RemovePollServiceHTTPHandler("", "/CAPS/EQG/" + ky.ToString() + "/");
                }

            }
            UUID searchval = UUID.Zero;

            removeitems.Clear();

            lock (m_QueueUUIDAvatarMapping)
            {
                foreach (UUID ky in m_QueueUUIDAvatarMapping.Keys)
                {
                    searchval = m_QueueUUIDAvatarMapping[ky];

                    if (searchval == AgentID)
                    {
                        removeitems.Add(ky);
                    }
                }

                foreach (UUID ky in removeitems)
                    m_QueueUUIDAvatarMapping.Remove(ky);
            }
        }*/

        public IRequestHandler RegisterCap(UUID agentID, CAPSPrivateSeedHandler handler)
        {
            // Register an event queue for the client

            // Let's instantiate a Queue for this agent right now
            TryGetQueue(agentID);

            string capsBase = "/CAPS/EQG/";
            UUID EventQueueGetUUID = UUID.Zero;

            lock (m_AvatarQueueUUIDMapping)
            {
                // Reuse open queues.  The client does!
                if (m_AvatarQueueUUIDMapping.ContainsKey(agentID))
                {
                    //m_log.DebugFormat("[EVENTQUEUE]: Found Existing UUID!");
                    EventQueueGetUUID = m_AvatarQueueUUIDMapping[agentID];
                }
                else
                {
                    EventQueueGetUUID = UUID.Random();
                    //m_log.DebugFormat("[EVENTQUEUE]: Using random UUID!");
                }
            }

            lock (m_QueueUUIDAvatarMapping)
            {
                if (!m_QueueUUIDAvatarMapping.ContainsKey(EventQueueGetUUID))
                    m_QueueUUIDAvatarMapping.Add(EventQueueGetUUID, agentID);
            }

            lock (m_AvatarQueueUUIDMapping)
            {
                if (!m_AvatarQueueUUIDMapping.ContainsKey(agentID))
                    m_AvatarQueueUUIDMapping.Add(agentID, EventQueueGetUUID);
            }

            // Register this as a caps handler
            IRequestHandler rhandler =
                                 new RestHTTPHandler("POST", capsBase + EventQueueGetUUID.ToString() + "/",
                                                       delegate(Hashtable m_dhttpMethod)
                                                       {
                                                           return ProcessQueue(m_dhttpMethod, agentID);
                                                       });

            // This will persist this beyond the expiry of the caps handlers
            MainServer.Instance.AddPollServiceHTTPHandler(
                capsBase + EventQueueGetUUID.ToString() + "/", EventQueuePoll, new PollServiceEventArgs(null, HasEvents, GetEvents, NoEvents, agentID));

            Random rnd = new Random(Environment.TickCount);
            lock (m_ids)
            {
                if (!m_ids.ContainsKey(agentID))
                    m_ids.Add(agentID, rnd.Next(30000000));
            }

            UUID Password = UUID.Random();

            m_AvatarPasswordMap.Add(agentID, Password);
            handler.postToSendToSim.Add("EventQueuePass", OSD.FromUUID(Password));
            return rhandler;
        }

        public bool AuthenticateRequest(UUID agentID, UUID Password)
        {
            if (m_AvatarPasswordMap.ContainsKey(agentID) && m_AvatarPasswordMap[agentID] == Password)
                return true;
            return false;
        }

        public bool HasEvents(UUID requestID, UUID agentID)
        {
            // Don't use this, because of race conditions at agent closing time
            //Queue<OSD> queue = TryGetQueue(agentID);

            Queue<OSD> queue = GetQueue(agentID);
            if (queue != null)
                lock (queue)
                {
                    if (queue.Count > 0)
                        return true;
                    else
                        return false;
                }
            return false;
        }

        public Hashtable GetEvents(UUID requestID, UUID pAgentId, string request)
        {
            Queue<OSD> queue = TryGetQueue(pAgentId);
            OSD element;
            lock (queue)
            {
                if (queue.Count == 0)
                    return NoEvents(requestID, pAgentId);
                element = queue.Dequeue(); // 15s timeout
            }



            int thisID = 0;
            lock (m_ids)
                thisID = m_ids[pAgentId];

            OSDArray array = new OSDArray();
            if (element == null) // didn't have an event in 15s
            {
                OSDMap keepAliveEvent = new OSDMap(2);
                keepAliveEvent.Add("body", new OSDMap());
                keepAliveEvent.Add("message", new OSDString("FAKEEVENT"));

                // Send it a fake event to keep the client polling!   It doesn't like 502s like the proxys say!
                array.Add(keepAliveEvent);
                //m_log.DebugFormat("[EVENTQUEUE]: adding fake event for {0} in region {1}", pAgentId, m_scene.RegionInfo.RegionName);
            }
            else
            {
                array.Add(element);
                lock (queue)
                {
                    while (queue.Count > 0)
                    {
                        array.Add(queue.Dequeue());
                        thisID++;
                    }
                }
            }

            OSDMap events = new OSDMap();
            events.Add("events", array);

            events.Add("id", new OSDInteger(thisID));
            lock (m_ids)
            {
                m_ids[pAgentId] = thisID + 1;
            }
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200;
            responsedata["content_type"] = "application/xml";
            responsedata["keepalive"] = false;
            responsedata["reusecontext"] = false;
            responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(events);
            //m_log.DebugFormat("[EVENTQUEUE]: sending response for {0} in region {1}: {2}", pAgentId, m_scene.RegionInfo.RegionName, responsedata["str_response_string"]);
            return responsedata;
        }

        public Hashtable NoEvents(UUID requestID, UUID agentID)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 502;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["reusecontext"] = false;
            responsedata["str_response_string"] = "Upstream error: ";
            responsedata["error_status_text"] = "Upstream error:";
            responsedata["http_protocol_version"] = "HTTP/1.0";
            return responsedata;
        }

        public Hashtable ProcessQueue(Hashtable request, UUID agentID)
        {
            // TODO: this has to be redone to not busy-wait (and block the thread),
            // TODO: as soon as we have a non-blocking way to handle HTTP-requests.

            //            if (m_log.IsDebugEnabled)
            //            { 
            //                String debug = "[EVENTQUEUE]: Got request for agent {0} in region {1} from thread {2}: [  ";
            //                foreach (object key in request.Keys)
            //                {
            //                    debug += key.ToString() + "=" + request[key].ToString() + "  ";
            //                }
            //                m_log.DebugFormat(debug + "  ]", agentID, m_scene.RegionInfo.RegionName, System.Threading.Thread.CurrentThread.Name);
            //            }

            Queue<OSD> queue = TryGetQueue(agentID);
            OSD element = queue.Dequeue(); // 15s timeout

            Hashtable responsedata = new Hashtable();

            int thisID = 0;
            lock (m_ids)
                thisID = m_ids[agentID];

            if (element == null)
            {
                //m_log.ErrorFormat("[EVENTQUEUE]: Nothing to process in " + m_scene.RegionInfo.RegionName);
                if (thisID == -1) // close-request
                {
                    m_log.ErrorFormat("[EVENTQUEUE]: 404 for " + agentID);
                    responsedata["int_response_code"] = 404; //501; //410; //404;
                    responsedata["content_type"] = "text/plain";
                    responsedata["keepalive"] = false;
                    responsedata["str_response_string"] = "Closed EQG";
                    return responsedata;
                }
                responsedata["int_response_code"] = 502;
                responsedata["content_type"] = "text/plain";
                responsedata["keepalive"] = false;
                responsedata["str_response_string"] = "Upstream error: ";
                responsedata["error_status_text"] = "Upstream error:";
                responsedata["http_protocol_version"] = "HTTP/1.0";
                return responsedata;
            }

            OSDArray array = new OSDArray();
            if (element == null) // didn't have an event in 15s
            {
                OSDMap keepAliveEvent = new OSDMap(2);
                keepAliveEvent.Add("body", new OSDMap());
                keepAliveEvent.Add("message", new OSDString("FAKEEVENT"));

                // Send it a fake event to keep the client polling!   It doesn't like 502s like the proxys say!
                array.Add(keepAliveEvent);
                //m_log.DebugFormat("[EVENTQUEUE]: adding fake event for {0} in region {1}", agentID, m_scene.RegionInfo.RegionName);
            }
            else
            {
                array.Add(element);
                while (queue.Count > 0)
                {
                    array.Add(queue.Dequeue());
                    thisID++;
                }
            }

            OSDMap events = new OSDMap();
            events.Add("events", array);

            events.Add("id", new OSDInteger(thisID));
            lock (m_ids)
            {
                m_ids[agentID] = thisID + 1;
            }

            responsedata["int_response_code"] = 200;
            responsedata["content_type"] = "application/xml";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(events);
            //m_log.DebugFormat("[EVENTQUEUE]: sending response for {0} in region {1}: {2}", agentID, m_scene.RegionInfo.RegionName, responsedata["str_response_string"]);

            return responsedata;
        }

        public Hashtable EventQueuePoll(Hashtable request)
        {
            return new Hashtable();
        }

        #region EQM event poster

        public class EQMEventPoster : BaseStreamHandler
        {
            private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            private IHttpServer m_server;
            private CAPSEQMHandler m_handler;

            public EQMEventPoster(IHttpServer server, CAPSEQMHandler handler) :
                base("POST", "/CAPS/EQMPOSTER")
            {
                m_server = server;
                m_handler = handler;
            }

            public override byte[] Handle(string path, Stream requestData,
                    OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                StreamReader sr = new StreamReader(requestData);
                string body = sr.ReadToEnd();
                sr.Close();
                body = body.Trim();

                //m_log.DebugFormat("[XXX]: query String: {0}", body);
                string method = string.Empty;
                try
                {
                    Dictionary<string, object> request = new Dictionary<string, object>();
                    request = ServerUtils.ParseQueryString(body);
                    if (request.Count == 1)
                        request = ServerUtils.ParseXmlResponse(body);
                    object value = null;
                    request.TryGetValue("<?xml version", out value);
                    if (value != null)
                        request = ServerUtils.ParseXmlResponse(body);

                    return ProcessAddCAP(request);
                }
                catch (Exception)
                {
                }

                return null;

            }

            private byte[] ProcessAddCAP(Dictionary<string, object> m_dhttpMethod)
            {
                UUID agentID = UUID.Parse((string)m_dhttpMethod["AGENTID"]);
                UUID password = UUID.Parse((string)m_dhttpMethod["PASS"]);
                string llsd = (string)m_dhttpMethod["LLSD"];
                //This is called by the user server
                if (!m_handler.AuthenticateRequest(agentID,password))
                {
                    Dictionary<string, object> result = new Dictionary<string, object>();
                    result.Add("result", "false");
                    string xmlString = ServerUtils.BuildXmlResponse(result);
                    UTF8Encoding encoding = new UTF8Encoding();
                    return encoding.GetBytes(xmlString);
                }
                else
                {
                    m_handler.Enqueue(OSDParser.DeserializeJson(llsd), agentID);
                    Dictionary<string, object> result = new Dictionary<string, object>();
                    result.Add("result", "true");
                    string xmlString = ServerUtils.BuildXmlResponse(result);
                    UTF8Encoding encoding = new UTF8Encoding();
                    return encoding.GetBytes(xmlString);
                }
            }
        }

        #endregion
    }

    #endregion
}
