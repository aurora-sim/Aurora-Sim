using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;

namespace OpenSim.Services.CapsService
{
    /// <summary>
    /// This is loaded ONCE by the service loader and runs events depending on where the user is in the grid
    /// </summary>
    public class EventQueueMasterService : EventQueueModuleBase, IService, IEventQueueService
    {
        protected ICapsService m_service = null;

        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueHandler", "") != Name)
                return; 
            registry.RegisterInterface<IEventQueueService>(this);
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_service = registry.Get<ICapsService>();
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
        }

        #endregion

        #region IEventQueueService Members

        public override bool Enqueue(OSD o, UUID avatarID, ulong regionHandle)
        {
            //Find the CapsService for the user and enqueue the event
            PrivateCapsService service = (PrivateCapsService)m_service.GetCapsService(regionHandle, avatarID);
            return service.EventQueueService.Enqueue(o, avatarID);
        }

        public bool AuthenticateRequest(UUID agentID, UUID password, ulong RegionHandle)
        {
            //Find the CapsService for the user and check their authentication
            PrivateCapsService service = (PrivateCapsService)m_service.GetCapsService(RegionHandle, agentID);
            return service.EventQueueService.AuthenticateRequest(agentID, password);
        }

        #endregion
    }
    
    /// <summary>
    /// This is created for EVERY client in EVERY region they have a CAPS seed in, unlike the one MasterService above
    /// </summary>
    public class EventQueueService : IInternalEventQueueService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<UUID, int> m_ids = new Dictionary<UUID, int>();

        private Dictionary<UUID, Queue<OSD>> queues = new Dictionary<UUID, Queue<OSD>>();
        private Dictionary<UUID, UUID> m_AvatarQueueUUIDMapping = new Dictionary<UUID, UUID>();
        private Dictionary<UUID, UUID> m_AvatarPasswordMap = new Dictionary<UUID, UUID>();
        private IHttpServer m_server;
        protected Dictionary<UUID, IPrivateCapsService> m_handlers = new Dictionary<UUID, IPrivateCapsService>();

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

        /// <summary>
        /// Add the given event into the client's queue so that it is sent on the next 
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="avatarID"></param>
        /// <returns></returns>
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
                        //m_handlers[avatarID].PublicHandler.RemoveCAPS(avatarID)
                    }
                    if (map.ContainsKey("message") && map["message"] == "EstablishAgentCommunication")
                    {
                        string SeedCap = ((OSDMap)map["body"])["seed-capability"].AsString();
                        ulong regionHandle = ((OSDMap)map["body"])["region-handle"].AsULong();

                        SeedCap = CreateNewCAPSClient(SeedCap, regionHandle, avatarID);

                        ((OSDMap)map["body"])["seed-capability"] = SeedCap;
                    }
                    else if (map.ContainsKey("message") && map["message"] == "CrossedRegion")
                    {
                        OSDMap infoMap = ((OSDMap)((OSDArray)((OSDMap)map["body"])["RegionData"])[0]);
                        string SeedCap = infoMap["SeedCapability"].AsString();
                        ulong regionHandle = infoMap["RegionHandle"].AsULong();

                        SeedCap = CreateNewCAPSClient(SeedCap, regionHandle, avatarID);

                        //Now tell the client about it correctly
                        ((OSDMap)((OSDArray)((OSDMap)map["body"])["RegionData"])[0])["SeedCapability"] = SeedCap;
                    }
                    else if (map.ContainsKey("message") && map["message"] == "TeleportFinish")
                    {
                        OSDMap infoMap = ((OSDMap)((OSDArray)((OSDMap)map["body"])["Info"])[0]);
                        string SeedCap = infoMap["SeedCapability"].AsString();
                        ulong regionHandle = infoMap["RegionHandle"].AsULong();

                        SeedCap = CreateNewCAPSClient(SeedCap, regionHandle, avatarID);

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

        public string CreateNewCAPSClient(string SeedCap, ulong regionHandle, UUID avatarID)
        {
            //Create a new private seed handler by default, but let the public handler deal with whether it actually needs created
            string newSeedCap = CapsUtil.GetCapsSeedPath(CapsUtil.GetRandomCapsObjectPath());
            IPrivateCapsService handler = new PrivateCapsService(m_server, m_handlers[avatarID].InventoryService, m_handlers[avatarID].LibraryService, m_handlers[avatarID].GridUserService, m_handlers[avatarID].GridService, m_handlers[avatarID].PresenceService, m_handlers[avatarID].AssetService,
                SeedCap, avatarID, regionHandle, m_handlers[avatarID].PublicHandler, m_handlers[avatarID].PublicHandler.HostURI + newSeedCap, newSeedCap);

            handler.PublicHandler.AddCapsService(handler);
            //Now make sure we have the right one, as there could have already been a CAP in that region
            handler = m_handlers[avatarID].PublicHandler.GetCapsService(regionHandle, handler.AgentID);
            //Now send the client the Seed created by the CapsService
            return handler.CapsURL;
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
            m_handlers[agentID] = handler;
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
            //Nothing to process... don't confuse the client
            if (array.Count == 0)
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
    }
}
