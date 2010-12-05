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
            string gService = m_CAPSServerConfig.GetString("GridService", String.Empty);
            string presenceService = m_CAPSServerConfig.GetString("PresenceService", String.Empty);
            string Password = m_CAPSServerConfig.GetString("Password", String.Empty);
            string HostName = m_CAPSServerConfig.GetString("HostName", String.Empty);
            IInventoryService m_InventoryService = ServerUtils.LoadPlugin<IInventoryService>(invService, args);
            ILibraryService m_LibraryService = ServerUtils.LoadPlugin<ILibraryService>(libService, args);
            IGridUserService m_GridUserService = ServerUtils.LoadPlugin<IGridUserService>(guService, args);
            IPresenceService m_PresenceService = ServerUtils.LoadPlugin<IPresenceService>(presenceService, args);
            IGridService m_GridService = ServerUtils.LoadPlugin<IGridService>(gService, args);
            CapsModules = Aurora.Framework.AuroraModuleLoader.PickupModules<ICapsServiceConnector>();
            //This handler allows sims to post CAPS for their sims on the CAPS server.
            server.AddStreamHandler(new CAPSPublicHandler(server, Password, m_InventoryService, m_LibraryService, m_GridUserService, m_PresenceService, m_GridService, HostName));
        }
    }

    /// <summary>
    /// This handles the seed requests from the client and forwards the request onto the the simulator
    /// </summary>
    public class CAPSPrivateSeedHandler : IPrivateCapsService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IGridUserService m_GridUserService;
        private IPresenceService m_PresenceService;
        private IInventoryService m_InventoryService;
        private ILibraryService m_LibraryService;
        private IGridService m_GridService;
        public IGridUserService GridUserService
        {
            get { return m_GridUserService; }
        }
        public IPresenceService PresenceService
        {
            get { return m_PresenceService; }
        }
        public IInventoryService InventoryService
        {
            get { return m_InventoryService; }
        }
        public ILibraryService LibraryService
        {
            get { return m_LibraryService; }
        }
        public IGridService GridService
        {
            get { return m_GridService; }
        }
        private IHttpServer m_server;
        public IHttpServer HttpServer
        {
            get { return m_server; }
            set { m_server = value; }
        }
        private string m_SimToInform;
        public string SimToInform
        {
            get { return m_SimToInform; }
            set { m_SimToInform = value; }
        }
        private UUID m_AgentID;
        public UUID AgentID
        {
            get { return m_AgentID; }
        }
        //X cap name to path
        public OSDMap registeredCAPS = new OSDMap();
        //Paths to X cap
        public OSDMap registeredCAPSPath = new OSDMap();
        private CAPSEQMHandler EQMHandler = new CAPSEQMHandler();
        private string m_HostName;
        public string HostName
        {
            get { return m_HostName; }
            set { m_HostName = value; }
        }
        private OSDMap postToSendToSim = new OSDMap();

        public OSDMap PostToSendToSim
        {
            get { return postToSendToSim; }
            set { postToSendToSim = value; }
        }

        private bool m_runTheEQM;
        private ulong m_regionHandle = 0;
        public ulong RegionHandle
        {
            get { return m_regionHandle; }
        }
        private ICAPSPublicHandler m_publicHandler;
        public ICAPSPublicHandler PublicHandler
        {
            get { return m_publicHandler; }
        }
        private string m_capsURL;
        public string CapsURL
        {
            get { return m_capsURL; }
        }

        public CAPSPrivateSeedHandler(IHttpServer server, IInventoryService inventoryService, ILibraryService libraryService, IGridUserService guService, IGridService gService, IPresenceService presenceService, string URL, UUID agentID, string HostName, bool runTheEQM, ulong regionHandle, ICAPSPublicHandler handler, string capsURL)
        {
            m_server = server;
            m_InventoryService = inventoryService;
            m_LibraryService = libraryService;
            m_GridUserService = guService;
            m_GridService = gService;
            m_PresenceService = presenceService;
            SimToInform = URL;
            m_AgentID = agentID;
            m_HostName = HostName;
            m_runTheEQM = runTheEQM;
            m_regionHandle = regionHandle;
            m_publicHandler = handler;
            m_capsURL = capsURL;
        }

        public void Initialise()
        {
            if (m_server != null)
                AddServerCAPS();
        }

        public List<IRequestHandler> GetServerCAPS()
        {
            List<IRequestHandler> handlers = new List<IRequestHandler>();

            if (m_runTheEQM)
            {
                // The EventQueue module is now handled by the CapsService (if we arn't disabling it) as it needs to be completely protected
                //  This means we deal with all teleports and keeping track of the passwords for the agents
                IRequestHandler handle = EQMHandler.RegisterCap(m_AgentID, m_server, this);
                if(handle != null)
                    handlers.Add(handle);
            }

            foreach (ICapsServiceConnector conn in AuroraCAPSHandler.CapsModules)
            {
                handlers.AddRange(conn.RegisterCaps(m_AgentID, m_server, this));
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
                m_log.Debug("[CAPSService]: Seed request was added for region " + SimToInform + " at " + CapsURL);
                if (reply != "")
                {
                    OSDMap hash = (OSDMap)OSDParser.DeserializeLLSDXml(OpenMetaverse.Utils.StringToBytes(reply));
                    foreach (string key in hash.Keys)
                    {
                        if (!registeredCAPS.ContainsKey(key))
                            registeredCAPS[key] = hash[key].AsString();
                        //else
                        //    m_log.WarnFormat("[CAPSService]: Simulator tried to override grid CAPS setting! @ {0}", SimToInform);
                    }
                }
                //m_log.Warn("[CAPS]: EQM Request for " + registeredCAPS["EventQueueGet"].AsString());
            }
            catch
            {
            }
            return OSDParser.SerializeLLSDXmlString(registeredCAPS);
        }

        public string CreateCAPS(string method)
        {
            string caps = "/CAPS/" + method + "/" + UUID.Random() + "/";
            AddCAPS(method, caps);
            return caps;
        }

        public string CreateCAPS(string method, string appendedPath)
        {
            string caps = "/CAPS/" + method + "/" + UUID.Random() + appendedPath + "/";
            AddCAPS(method, caps);
            return caps;
        }

        public void AddCAPS(string method, string caps)
        {
            registeredCAPS[method] = m_HostName + caps;
            registeredCAPSPath[m_HostName + caps] = method;
        }

        public string GetCAPS(string method)
        {
            if (registeredCAPS.ContainsKey(method))
                return registeredCAPS[method].ToString();
            return "";
        }
    }

    #region Public handler

    /// <summary>
    /// This handles requests from the user server about clients that need a CAPS seed URL.
    /// </summary>
    public class CAPSPublicHandler : BaseStreamHandler, ICAPSPublicHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IHttpServer m_server;
        private string CAPSPass;
        private IGridUserService m_GridUserService;
        private IGridService m_GridService;
        private IPresenceService m_PresenceService;
        private IInventoryService m_inventory;
        private ILibraryService m_library;
        private string m_hostName;

        public CAPSPublicHandler(IHttpServer server, string pass, IInventoryService inventory, ILibraryService library, IGridUserService guService, IPresenceService presenceService, IGridService gService, string hostName) :
            base("POST", "/CAPS/REGISTER")
        {
            m_server = server;
            CAPSPass = pass;
            m_inventory = inventory;
            m_library = library;
            m_GridService = gService;
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
                ulong regionHandle = ulong.Parse((string)m_dhttpMethod["REGIONHANDLE"]);

                m_CapsServices.Remove(AgentID);
                CreateCAPS(AgentID, simCAPS, CAPS, regionHandle);

                Dictionary<string, object> result = new Dictionary<string, object>();
                result.Add("result", "true");
                string xmlString = ServerUtils.BuildXmlResponse(result);
                UTF8Encoding encoding = new UTF8Encoding();
                return encoding.GetBytes(xmlString);
            }
        }

        private void CreateCAPS(UUID AgentID, string SimCAPS, string CAPS, ulong regionHandle)
        {
            //This makes the new SEED url on the CAPS server
            AddCapsService(new CAPSPrivateSeedHandler(m_server, m_inventory, m_library, m_GridUserService, m_GridService, m_PresenceService, SimCAPS, AgentID, m_hostName, true, regionHandle, this, CAPS), CAPS, AgentID);
        }

        Dictionary<UUID, Dictionary<ulong, IPrivateCapsService>> m_CapsServices = new Dictionary<UUID, Dictionary<ulong, IPrivateCapsService>>();

        public IPrivateCapsService GetCapsService(ulong regionID, UUID agentID)
        {
            if (m_CapsServices.ContainsKey(agentID) && m_CapsServices[agentID].ContainsKey(regionID))
                return m_CapsServices[agentID][regionID];
            return null;
        }

        public void AddCapsService(IPrivateCapsService handler, string CAPS, UUID agentID)
        {
            if (!m_CapsServices.ContainsKey(agentID))
                m_CapsServices.Add(agentID, new Dictionary<ulong,IPrivateCapsService>());
            if (!m_CapsServices[agentID].ContainsKey(handler.RegionHandle))
            {
                m_CapsServices[agentID][handler.RegionHandle] = handler;
                handler.Initialise();
                m_server.AddStreamHandler(new RestStreamHandler("POST", CAPS, handler.CapsRequest));
            }
            else
            {
            }
        }
    }

    #endregion

    #region EQM

    public class CAPSEQMHandler// : ICapsServiceModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<UUID, int> m_ids = new Dictionary<UUID, int>();

        private Dictionary<UUID, Queue<OSD>> queues = new Dictionary<UUID, Queue<OSD>>();
        private Dictionary<UUID, UUID> m_AvatarQueueUUIDMapping = new Dictionary<UUID, UUID>();
        private Dictionary<UUID, UUID> m_AvatarPasswordMap = new Dictionary<UUID, UUID>();
        private IHttpServer m_server;
        private IPrivateCapsService m_handler;

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
                if (ev.Type == OSDType.Map)
                {
                    OSDMap map = (OSDMap)ev;
                    if (map.ContainsKey("message") && map["message"] == "DisableSimulator")
                    {
                    }
                    if (map.ContainsKey("message") && map["message"] == "EstablishAgentCommunication")
                    {
                        string SeedCap = ((OSDMap)map["body"])["seed-capability"].AsString();
                        ulong regionHandle = ((OSDMap)map["body"])["region-handle"].AsULong();
                        
                        uint x, y;
                        Utils.LongToUInts(regionHandle, out x, out y);
                        OpenSim.Services.Interfaces.GridRegion region = m_handler.GridService.GetRegionByPosition(UUID.Zero, (int)x, (int)y);

                        //Create a new private seed handler by default, but let the public handler deal with whether it actually needs created
                        IPrivateCapsService handler = new CAPSPrivateSeedHandler(m_server, m_handler.InventoryService, m_handler.LibraryService, m_handler.GridUserService, m_handler.GridService, m_handler.PresenceService,
                            SeedCap, avatarID, m_handler.HostName, true, regionHandle, m_handler.PublicHandler, CapsUtil.GetCapsSeedPath(CapsUtil.GetRandomCapsObjectPath()));

                        handler.PublicHandler.AddCapsService(handler, handler.CapsURL, handler.AgentID);
                        handler = m_handler.PublicHandler.GetCapsService(regionHandle, handler.AgentID);
                        handler.SimToInform = SeedCap;
                        
                        //Get the seed cap from the CapsService for that region
                        SeedCap = handler.HostName + handler.CapsURL;

                        ((OSDMap)map["body"])["seed-capability"] = SeedCap;
                    }
                    else if (map.ContainsKey("message") && map["message"] == "CrossedRegion")
                    {
                        OSDMap infoMap = ((OSDMap)((OSDArray)((OSDMap)map["body"])["RegionData"])[0]);
                        string SeedCap = infoMap["SeedCapability"].AsString();
                        ulong regionHandle = infoMap["RegionHandle"].AsULong();
                        uint x, y;
                        Utils.LongToUInts(regionHandle, out x, out y);
                        OpenSim.Services.Interfaces.GridRegion region = m_handler.GridService.GetRegionByPosition(UUID.Zero, (int)x, (int)y);

                        //Create a new private seed handler by default, but let the public handler deal with whether it actually needs created
                        IPrivateCapsService handler = new CAPSPrivateSeedHandler(m_server, m_handler.InventoryService, m_handler.LibraryService, m_handler.GridUserService, m_handler.GridService, m_handler.PresenceService,
                            SeedCap, avatarID, m_handler.HostName, true, regionHandle, m_handler.PublicHandler, CapsUtil.GetCapsSeedPath(CapsUtil.GetRandomCapsObjectPath()));

                        handler.PublicHandler.AddCapsService(handler, handler.CapsURL, handler.AgentID);
                        handler = m_handler.PublicHandler.GetCapsService(regionHandle, handler.AgentID);
                        handler.SimToInform = SeedCap;
                        //Get the seed cap from the CapsService for that region
                        SeedCap = handler.HostName + handler.CapsURL;

                        //Now tell the client about it correctly
                        ((OSDMap)((OSDArray)((OSDMap)map["body"])["RegionData"])[0])["SeedCapability"] = SeedCap;
                    }
                    else if (map.ContainsKey("message") && map["message"] == "TeleportFinish")
                    {
                        OSDMap infoMap = ((OSDMap)((OSDArray)((OSDMap)map["body"])["Info"])[0]);
                        string SeedCap = infoMap["SeedCapability"].AsString();
                        ulong regionHandle = infoMap["RegionHandle"].AsULong();

                        //Create a new private seed handler by default, but let the public handler deal with whether it actually needs created
                        uint x, y;
                        Utils.LongToUInts(regionHandle, out x, out y);
                        OpenSim.Services.Interfaces.GridRegion region = m_handler.GridService.GetRegionByPosition(UUID.Zero, (int)x, (int)y);
                        
                        //Create a new private seed handler by default, but let the public handler deal with whether it actually needs created
                        IPrivateCapsService handler = new CAPSPrivateSeedHandler(m_server, m_handler.InventoryService, m_handler.LibraryService, m_handler.GridUserService, m_handler.GridService, m_handler.PresenceService,
                            SeedCap, avatarID, m_handler.HostName, true, regionHandle, m_handler.PublicHandler, CapsUtil.GetCapsSeedPath(CapsUtil.GetRandomCapsObjectPath()));

                        handler.PublicHandler.AddCapsService(handler, handler.CapsURL, handler.AgentID);
                        handler = m_handler.PublicHandler.GetCapsService(regionHandle, handler.AgentID);
                        handler.SimToInform = SeedCap;
                        
                        //Get the seed cap from the CapsService for that region
                        SeedCap = handler.HostName + handler.CapsURL;
                        
                        //Now tell the client about it correctly
                        ((OSDMap)((OSDArray)((OSDMap)map["body"])["Info"])[0])["SeedCapability"] = SeedCap;
                    }
                }
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

        public IRequestHandler RegisterCap(UUID agentID, IHttpServer server, IPrivateCapsService handler)
        {
            m_server = server;
            m_handler = handler;
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

            lock (m_AvatarQueueUUIDMapping)
            {
                if (!m_AvatarQueueUUIDMapping.ContainsKey(agentID))
                    m_AvatarQueueUUIDMapping.Add(agentID, EventQueueGetUUID);
            }

            string caps = capsBase + EventQueueGetUUID.ToString() + "/";


            // Register this as a caps handler
            IRequestHandler rhandler = new RestHTTPHandler("POST", caps,
                                                           delegate(Hashtable m_dhttpMethod)
                                                           {
                                                               return ProcessQueue(m_dhttpMethod, agentID);
                                                           });
            handler.AddCAPS("EventQueueGet", caps);

            //This handler allows sims to post EQM messages for their sims on the CAPS server.
            server.AddStreamHandler(new EQMEventPoster(this));

            // This will persist this beyond the expiry of the caps handlers
            MainServer.Instance.AddPollServiceHTTPHandler(
                caps, EventQueuePoll, new PollServiceEventArgs(null, HasEvents, GetEvents, NoEvents, agentID));

            Random rnd = new Random(Environment.TickCount);
            lock (m_ids)
            {
                if (!m_ids.ContainsKey(agentID))
                    m_ids.Add(agentID, rnd.Next(30000000));
            }

            UUID Password = UUID.Random();

            if (!m_AvatarPasswordMap.ContainsKey(agentID))
                m_AvatarPasswordMap.Add(agentID, Password);
            m_AvatarPasswordMap[agentID] = Password;
            handler.PostToSendToSim["EventQueuePass"] = OSD.FromUUID(Password);
            return rhandler;
        }

        public bool AuthenticateRequest(UUID agentID, UUID Password)
        {
            //if (m_AvatarPasswordMap.ContainsKey(agentID) && m_AvatarPasswordMap[agentID] == Password)
                return true;
            //return false;
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
            //m_log.Warn("Got EQM get at " + m_handler.CapsURL);
            Queue<OSD> queue = TryGetQueue(agentID);
            OSD element = null;
            if (queue.Count != 0)
                element = queue.Dequeue(); // 15s timeout

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
            array.Add(element);
            while (queue.Count > 0)
            {
                OSD item = queue.Dequeue();
                if (item != null)
                {
                    array.Add(item);
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

            private CAPSEQMHandler m_handler;

            public EQMEventPoster(CAPSEQMHandler handler) :
                base("POST", "/CAPS/EQMPOSTER" + handler.m_handler.RegionHandle)
            {
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
                    m_handler.Enqueue(OSDParser.DeserializeLLSDXml(llsd), agentID);
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
