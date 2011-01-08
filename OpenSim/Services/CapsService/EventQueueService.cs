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
    public class EventQueueMasterService : EventQueueModuleBase, IService, IEventQueueService
    {
        #region Declares

        protected ICapsService m_service = null;

        #endregion

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
            registry.RegisterModuleInterface<IEventQueueService>(this);
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostStart(IConfigSource config, IRegistryCore registry)
        {
            m_service = registry.RequestModuleInterface<ICapsService>();
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueHandler", "") != Name)
                return;
            registry.RegisterModuleInterface<IEventQueueService>(this);
        }

        #endregion

        #region IEventQueueService Members

        public override bool Enqueue(OSD o, UUID agentID, ulong regionHandle)
        {
            //Find the CapsService for the user and enqueue the event
            IRegionClientCapsService service = GetRegionClientCapsService(agentID, regionHandle);
            if (service == null)
                return false;
            RegionClientEventQueueService eventQueueService = FindEventQueueConnector(service);
            if (eventQueueService == null)
                return false;

            return eventQueueService.Enqueue(o);
        }

        public bool AuthenticateRequest(UUID agentID, UUID password, ulong regionHandle)
        {
            //Find the CapsService for the user and check their authentication
            IRegionClientCapsService service = GetRegionClientCapsService(agentID, regionHandle);
            if (service == null)
                return false;
            RegionClientEventQueueService eventQueueService = FindEventQueueConnector(service);
            if (eventQueueService == null)
                return false;

            return eventQueueService.AuthenticateRequest(password);
        }

        private IRegionClientCapsService GetRegionClientCapsService(UUID agentID, ulong RegionHandle)
        {
            IClientCapsService clientCaps = m_service.GetClientCapsService(agentID);
            if (clientCaps == null)
                return null;
            IRegionClientCapsService regionCaps = clientCaps.GetCapsService(RegionHandle);
            //If it doesn't exist, it will be null anyway, so we don't need to check anything else
            return regionCaps;
        }

        private RegionClientEventQueueService FindEventQueueConnector(IRegionClientCapsService service)
        {
            foreach (ICapsServiceConnector connector in service.GetServiceConnectors())
            {
                if (connector is RegionClientEventQueueService)
                {
                    return (RegionClientEventQueueService)connector;
                }
            }
            return null;
        }

        #endregion
    }

    public class RegionClientEventQueueService : ICapsServiceConnector
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private int m_ids = 0;

        private Queue<OSD> queue = new Queue<OSD>();
        private UUID m_AvatarPassword = UUID.Zero;
        private IRegionClientCapsService m_service;
        private string m_capsPath;

        #endregion

        #region IInternalEventQueueService members

        #region Enqueue a message/Create/Remove handlers

        /// <summary>
        /// Add the given event into the client's queue so that it is sent on the next 
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="avatarID"></param>
        /// <returns></returns>
        public bool Enqueue(OSD ev)
        {
            try
            {
                //Check the messages to pull out ones that are creating or destroying CAPS in this or other regions
                if (ev.Type == OSDType.Map)
                {
                    OSDMap map = (OSDMap)ev;
                    if (map.ContainsKey("message") && map["message"] == "DisableSimulator")
                    {
                        m_service.ClientCaps.RemoveCAPS(m_service.RegionHandle);
                    }
                    else if (map.ContainsKey("message") && map["message"] == "EstablishAgentCommunication")
                    {
                        string SimSeedCap = ((OSDMap)map["body"])["seed-capability"].AsString();
                        ulong regionHandle = ((OSDMap)map["body"])["region-handle"].AsULong();

                        string newSeedCap = CapsUtil.GetCapsSeedPath(CapsUtil.GetRandomCapsObjectPath());
                        IRegionClientCapsService otherRegionService = m_service.ClientCaps.GetOrCreateCapsService(regionHandle, newSeedCap, SimSeedCap);
                        //ONLY UPDATE THE SIM SEED HERE
                        //DO NOT PASS THE newSeedCap FROM ABOVE AS IT WILL BREAK THIS CODE
                        // AS THE CLIENT EXPECTS THE SAME CAPS SEED IF IT HAS BEEN TO THE REGION BEFORE
                        // AND FORCE UPDATING IT HERE WILL BREAK IT.
                        otherRegionService.AddSEEDCap("", SimSeedCap);
                        
                        ((OSDMap)map["body"])["seed-capability"] = otherRegionService.CapsUrl;
                    }
                    else if (map.ContainsKey("message") && map["message"] == "CrossedRegion")
                    {
                        OSDMap infoMap = ((OSDMap)((OSDArray)((OSDMap)map["body"])["RegionData"])[0]);
                        string SimSeedCap = infoMap["SeedCapability"].AsString();
                        ulong regionHandle = infoMap["RegionHandle"].AsULong();

                        string newSeedCap = CapsUtil.GetCapsSeedPath(CapsUtil.GetRandomCapsObjectPath());
                        IRegionClientCapsService otherRegionService = m_service.ClientCaps.GetOrCreateCapsService(regionHandle, newSeedCap, SimSeedCap);
                        //ONLY UPDATE THE SIM SEED HERE
                        //DO NOT PASS THE newSeedCap FROM ABOVE AS IT WILL BREAK THIS CODE
                        // AS THE CLIENT EXPECTS THE SAME CAPS SEED IF IT HAS BEEN TO THE REGION BEFORE
                        // AND FORCE UPDATING IT HERE WILL BREAK IT.
                        otherRegionService.AddSEEDCap("", SimSeedCap);

                        //Now tell the client about it correctly
                        ((OSDMap)((OSDArray)((OSDMap)map["body"])["RegionData"])[0])["SeedCapability"] = otherRegionService.CapsUrl;
                    }
                    else if (map.ContainsKey("message") && map["message"] == "TeleportFinish")
                    {
                        OSDMap infoMap = ((OSDMap)((OSDArray)((OSDMap)map["body"])["Info"])[0]);
                        string SimSeedCap = infoMap["SeedCapability"].AsString();
                        ulong regionHandle = infoMap["RegionHandle"].AsULong();

                        string newSeedCap = CapsUtil.GetCapsSeedPath(CapsUtil.GetRandomCapsObjectPath());
                        IRegionClientCapsService otherRegionService = m_service.ClientCaps.GetOrCreateCapsService(regionHandle, newSeedCap, SimSeedCap);
                        //ONLY UPDATE THE SIM SEED HERE
                        //DO NOT PASS THE newSeedCap FROM ABOVE AS IT WILL BREAK THIS CODE
                        // AS THE CLIENT EXPECTS THE SAME CAPS SEED IF IT HAS BEEN TO THE REGION BEFORE
                        // AND FORCE UPDATING IT HERE WILL BREAK IT.
                        otherRegionService.AddSEEDCap("", SimSeedCap);

                        //Now tell the client about it correctly
                        ((OSDMap)((OSDArray)((OSDMap)map["body"])["Info"])[0])["SeedCapability"] = otherRegionService.CapsUrl;
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

        #endregion

        #region Register and authenticate requests

        /// <summary>
        /// Check to make sure that the password we sent to the region is the same as the one here
        /// </summary>
        /// <param name="Password"></param>
        /// <returns></returns>
        public bool AuthenticateRequest(UUID Password)
        {
            if (m_AvatarPassword == Password)
                return true;
            return false;
        }

        #endregion

        #region Process Get/Has events

        public bool HasEvents(UUID requestID, UUID agentID)
        {
            // Don't use this, because of race conditions at agent closing time
            //Queue<OSD> queue = TryGetQueue(agentID);

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
            OSD element;
            lock (queue)
            {
                if (queue.Count == 0)
                    return NoEvents(requestID, pAgentId);
                element = queue.Dequeue(); // 15s timeout
            }

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
                        m_ids++;
                    }
                }
            }

            OSDMap events = new OSDMap();
            events.Add("events", array);

            events.Add("id", new OSDInteger(m_ids));
            m_ids += 1;
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
            OSD element = null;
            if (queue.Count != 0)
                element = queue.Dequeue(); // 15s timeout

            Hashtable responsedata = new Hashtable();

            if (element == null)
            {
                //m_log.ErrorFormat("[EVENTQUEUE]: Nothing to process in " + m_scene.RegionInfo.RegionName);
                if (m_ids == -1) // close-request
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
                    m_ids++;
                }
            }
            //Nothing to process... don't confuse the client
            if (array.Count == 0)
            {
                //m_log.ErrorFormat("[EVENTQUEUE]: Nothing to process in " + m_scene.RegionInfo.RegionName);
                if (m_ids == -1) // close-request
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

            events.Add("id", new OSDInteger(m_ids));
            m_ids += 1;

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

        #endregion

        #endregion

        #region ICapsServiceConnector Members

        public void RegisterCaps(IRegionClientCapsService service)
        {
            m_service = service;
            
            string capsBase = "/CAPS/EQG/";
            m_capsPath = capsBase + UUID.Random() + "/";


            // Register this as a caps handler
            IRequestHandler rhandler = new RestHTTPHandler("POST", m_capsPath,
                                                           delegate(Hashtable m_dhttpMethod)
                                                           {
                                                               return ProcessQueue(m_dhttpMethod, service.AgentID);
                                                           });
            m_service.AddStreamHandler("EventQueueGet", rhandler);

            // This will persist this beyond the expiry of the caps handlers
            MainServer.Instance.AddPollServiceHTTPHandler(
                m_capsPath, EventQueuePoll, new PollServiceEventArgs(null, HasEvents, GetEvents, NoEvents, service.AgentID));

            Random rnd = new Random(Environment.TickCount);
            m_ids = rnd.Next(30000000);

            m_AvatarPassword = UUID.Random();
            service.InfoToSendToUrl["EventQueuePass"] = OSD.FromUUID(m_AvatarPassword);
        }

        public void DeregisterCaps()
        {
            m_service.RemoveStreamHandler("EventQueueGet", "POST", m_capsPath);
            MainServer.Instance.RemovePollServiceHTTPHandler("", m_capsPath);
        }

        #endregion
    }
}
