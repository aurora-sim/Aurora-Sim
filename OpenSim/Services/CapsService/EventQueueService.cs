using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenMetaverse.Messages;
using OpenMetaverse.Messages.Linden;
using OpenMetaverse.Packets;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.CapsService
{
    public class EventQueueMasterService : IService, IEventQueueService
    {
        #region Declares

        protected ICapsService m_service = null;

        #endregion

        #region IService Members

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueHandler", "") != Name)
                return; 
            registry.RegisterModuleInterface<IEventQueueService>(this);
        }

        public virtual void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual void PostStart(IConfigSource config, IRegistryCore registry)
        {
            m_service = registry.RequestModuleInterface<ICapsService>();
        }

        public virtual void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueHandler", "") != Name)
                return;
            registry.RegisterModuleInterface<IEventQueueService>(this);
        }

        #endregion

        #region IEventQueueService Members

        public virtual bool Enqueue(OSD o, UUID agentID, ulong regionHandle)
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

        public virtual bool TryEnqueue(OSD o, UUID agentID, ulong regionHandle)
        {
            return Enqueue(o, agentID, regionHandle);
        }

        public virtual bool AuthenticateRequest(UUID agentID, UUID password, ulong regionHandle)
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

        #region EventQueue Message Enqueue

        public virtual void DisableSimulator(ulong handle, UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.DisableSimulator(handle);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void EnableSimulator(ulong handle, byte[] IPAddress, int Port, UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.EnableSimulator(handle, IPAddress, Port);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void EstablishAgentCommunication(UUID avatarID, ulong regionHandle, byte[] IPAddress, int Port, string CapsUrl, ulong RegionHandle)
        {
            IPEndPoint endPoint = new IPEndPoint(new IPAddress(IPAddress), Port);
            OSD item = EventQueueHelper.EstablishAgentCommunication(avatarID, regionHandle, endPoint.ToString(), CapsUrl);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void TeleportFinishEvent(ulong regionHandle, byte simAccess,
                                        IPEndPoint regionExternalEndPoint,
                                        uint locationID, uint flags,
                                        UUID avatarID, uint teleportFlags, ulong RegionHandle)
        {
            //Blank (for the CapsUrl) as we do not know what the CapsURL is on the sim side, it will be fixed when it reaches the grid server
            OSD item = EventQueueHelper.TeleportFinishEvent(regionHandle, simAccess, regionExternalEndPoint,
                                                            locationID, flags, "", avatarID, teleportFlags);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void CrossRegion(ulong handle, Vector3 pos, Vector3 lookAt,
                                IPEndPoint newRegionExternalEndPoint,
                                UUID avatarID, UUID sessionID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.CrossRegion(handle, pos, lookAt, newRegionExternalEndPoint,
                                                    "", avatarID, sessionID);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void ChatterBoxSessionStartReply(string groupName, UUID groupID, UUID AgentID, ulong RegionHandle)
        {
            OSD Item = EventQueueHelper.ChatterBoxSessionStartReply(groupName, groupID);
            Enqueue(Item, AgentID, RegionHandle);
        }

        public virtual void ChatterboxInvitation(UUID sessionID, string sessionName,
                                         UUID fromAgent, string message, UUID toAgent, string fromName, byte dialog,
                                         uint timeStamp, bool offline, int parentEstateID, Vector3 position,
                                         uint ttl, UUID transactionID, bool fromGroup, byte[] binaryBucket, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ChatterboxInvitation(sessionID, sessionName, fromAgent, message, toAgent, fromName, dialog,
                                                             timeStamp, offline, parentEstateID, position, ttl, transactionID,
                                                             fromGroup, binaryBucket);
            Enqueue(item, toAgent, RegionHandle);
            //m_log.InfoFormat("########### eq ChatterboxInvitation #############\n{0}", item);

        }

        public virtual void ChatterBoxSessionAgentListUpdates(UUID sessionID, UUID fromAgent, UUID toAgent, bool canVoiceChat,
                                                      bool isModerator, bool textMute, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ChatterBoxSessionAgentListUpdates(sessionID, fromAgent, canVoiceChat,
                                                                          isModerator, textMute);
            Enqueue(item, toAgent, RegionHandle);
            //m_log.InfoFormat("########### eq ChatterBoxSessionAgentListUpdates #############\n{0}", item);
        }

        public virtual void ChatterBoxSessionAgentListUpdates(UUID sessionID, ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock[] messages, UUID toAgent, string Transition, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ChatterBoxSessionAgentListUpdates(sessionID, messages, Transition);
            Enqueue(item, toAgent, RegionHandle);
            //m_log.InfoFormat("########### eq ChatterBoxSessionAgentListUpdates #############\n{0}", item);
        }

        public virtual void ParcelProperties(ParcelPropertiesMessage parcelPropertiesPacket, UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ParcelProperties(parcelPropertiesPacket);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void GroupMembership(AgentGroupDataUpdatePacket groupUpdate, UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.GroupMembership(groupUpdate);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void QueryReply(PlacesReplyPacket groupUpdate, UUID avatarID, string[] info, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.PlacesQuery(groupUpdate, info);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void ScriptRunningReply(UUID objectID, UUID itemID, bool running, bool mono,
            UUID avatarID, ulong RegionHandle)
        {
            OSD Item = EventQueueHelper.ScriptRunningReplyEvent(objectID, itemID, running, true);
            Enqueue(Item, avatarID, RegionHandle);
        }

        //
        // Region > CapsService EventQueueMessages ONLY
        // These are NOT sent to the client under ANY circumstances!
        //

        public virtual void EnableChildAgentsReply(UUID avatarID, ulong RegionHandle, int DrawDistance, GridRegion[] neighbors, AgentCircuitData circuit, AgentData data, uint TeleportFlags)
        {
            OSD item = EventQueueHelper.EnableChildAgents(DrawDistance, neighbors, circuit, TeleportFlags, data, null, 0);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual bool TryEnableChildAgents(UUID avatarID, ulong RegionHandle, int DrawDistance, GridRegion region, AgentCircuitData circuit, AgentData data, uint TeleportFlags, byte[] IPAddress, int Port)
        {
            OSD item = EventQueueHelper.EnableChildAgents(DrawDistance, new GridRegion[1] { region }, circuit, TeleportFlags, data, IPAddress, Port);
            return TryEnqueue(item, avatarID, RegionHandle);
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
                    else if (map.ContainsKey("message") && map["message"] == "EnableChildAgents")
                    {
                        //Some notes on this message:
                        // 1) This is a region > CapsService message ONLY, this should never be sent to the client!
                        // 2) This just enables child agents in the regions given, as the region cannot do it,
                        //       as regions do not have the ability to know what Cap Urls other regions have.
                        // 3) We could do more checking here, but we don't really 'have' to at this point.
                        //       If the sim was able to get it past the password checks and everything,
                        //       it should be able to add the neighbors here. We could do the neighbor finding here
                        //       as well, but it's not necessary at this time.
                        OSDMap body = ((OSDMap)map["body"]);

                        //Parse the OSDMap
                        int DrawDistance = body["DrawDistance"].AsInteger();

                        AgentCircuitData circuitData = new AgentCircuitData();
                        circuitData.UnpackAgentCircuitData((OSDMap)body["Circuit"]);

                        OSDArray neighborsArray = (OSDArray)body["Regions"];
                        GridRegion[] neighbors = new GridRegion[neighborsArray.Count];

                        int i = 0;
                        foreach (OSD r in neighborsArray)
                        {
                            GridRegion region = new GridRegion();
                            region.FromOSD((OSDMap)r);
                            neighbors[i] = region;
                            i++;
                        }
                        uint TeleportFlags = body["TeleportFlags"].AsUInteger();

                        AgentData data = null;
                        if (body.ContainsKey("AgentData"))
                        {
                            data = new AgentData();
                            data.Unpack((OSDMap)body["AgentData"]);
                        }

                        byte[] IPAddress = null;
                        if(body.ContainsKey("IPAddress"))
                            IPAddress = body["IPAddress"].AsBinary();
                        int Port = 0;
                        if (body.ContainsKey("Port"))
                            Port = body["Port"].AsInteger();

                        //Now do the creation
                        //Don't send it to the client at all, so return here
                        return EnableChildAgents(DrawDistance, neighbors, circuitData, TeleportFlags, data,
                            IPAddress, Port);
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

        #region EnableChildAgents 

        public bool EnableChildAgents(int DrawDistance, GridRegion[] neighbors,
            AgentCircuitData circuit, uint TeleportFlags, AgentData data, byte[] IPAddress, int Port)
        {
            int count = 0;
            bool informed = true;
            foreach (GridRegion neighbor in neighbors)
            {
                //m_log.WarnFormat("--> Going to send child agent to {0}, new agent {1}", neighbour.RegionName, newAgent);

                if (neighbor.RegionHandle != m_service.RegionHandle)
                {
                    if (IPAddress == null)
                    {
                        //We need to find the IP then
                        IPEndPoint endPoint = neighbor.ExternalEndPoint;
                        IPAddress = endPoint.Address.GetAddressBytes();
                        Port = endPoint.Port;
                    }
                    if (!InformClientOfNeighbor(circuit.Copy(), neighbor, TeleportFlags, data,
                        IPAddress, Port))
                        informed = false;
                }
                count++;
            }
            return informed;
        }

        /// <summary>
        /// Async component for informing client of which neighbors exist
        /// </summary>
        /// <remarks>
        /// This needs to run asynchronously, as a network timeout may block the thread for a long while
        /// </remarks>
        /// <param name="remoteClient"></param>
        /// <param name="a"></param>
        /// <param name="regionHandle"></param>
        /// <param name="endPoint"></param>
        private bool InformClientOfNeighbor(AgentCircuitData circuitData, GridRegion neighbor, 
            uint TeleportFlags, AgentData data, byte[] IPAddress, int Port)
        {
            m_log.Info("[EventQueueService]: Starting to inform client about neighbor " + neighbor.RegionName);

            //Notes on this method
            // 1) the SimulationService.CreateAgent MUST have a fixed CapsUrl for the region, so we have to create (if needed)
            //       a new Caps handler for it.
            // 2) Then we can call the methods (EnableSimulator and EstatablishAgentComm) to tell the client the new Urls
            // 3) This allows us to make the Caps on the grid server without telling any other regions about what the
            //       Urls are.

            string reason = String.Empty;
            ISimulationService SimulationService = m_service.Registry.RequestModuleInterface<ISimulationService>();
            if (SimulationService != null)
            {
                //Make sure that we have a URL for the Caps on the grid server and one for the sim
                string newSeedCap = CapsUtil.GetCapsSeedPath(CapsUtil.GetRandomCapsObjectPath());
                //Leave this blank so that we can check below so that we use the same Url if the client has already been to that region
                string SimSeedCap = "";
                bool newAgent = m_service.ClientCaps.GetCapsService(neighbor.RegionHandle) == null;
                IRegionClientCapsService otherRegionService = m_service.ClientCaps.GetOrCreateCapsService(neighbor.RegionHandle, newSeedCap, SimSeedCap);
               
                //ONLY UPDATE THE SIM SEED HERE
                //DO NOT PASS THE newSeedCap FROM ABOVE AS IT WILL BREAK THIS CODE
                // AS THE CLIENT EXPECTS THE SAME CAPS SEED IF IT HAS BEEN TO THE REGION BEFORE
                // AND FORCE UPDATING IT HERE WILL BREAK IT.
                string CapsBase = CapsUtil.GetRandomCapsObjectPath();
                if (newAgent)
                {
                    //Build the full URL
                    SimSeedCap
                        = "http://"
                      + neighbor.ExternalEndPoint.Address.ToString()
                      + ":"
                      + neighbor.HttpPort
                      + CapsUtil.GetCapsSeedPath(CapsBase);
                    //Add the new Seed for this region
                }
                else
                {
                    
                }

                //Fix the AgentCircuitData with the new CapsUrl
                circuitData.CapsPath = CapsBase;

                bool regionAccepted = SimulationService.CreateAgent(neighbor, circuitData, TeleportFlags, data, out reason);
                if (regionAccepted)
                {
                    //If the region accepted us, we should get a CAPS url back as the reason, if not, its not updated or not an Aurora region, so don't touch it.
                    if (reason != "")
                    {
                        OSDMap responseMap = (OSDMap)OSDParser.DeserializeJson(reason);
                        SimSeedCap = responseMap["CapsUrl"].AsString();
                    }
                    otherRegionService.AddSEEDCap("", SimSeedCap);
                    if (newAgent)
                    {
                        //m_log.DebugFormat("[EventQueueService]: {0} is sending {1} EnableSimulator for neighbor region {2} @ {3} " +
                        //    "and EstablishAgentCommunication with seed cap {4}",
                        //    m_scene.RegionInfo.RegionName, sp.Name, reg.RegionName, reg.RegionHandle, capsPath);

                        //We 'could' call Enqueue directly... but its better to just let it go and do it this way
                        IEventQueueService EQService = m_service.Registry.RequestModuleInterface<IEventQueueService>();

                        EQService.EnableSimulator(neighbor.RegionHandle, IPAddress, Port, m_service.AgentID, m_service.RegionHandle);

                        // ES makes the client send a UseCircuitCode message to the destination, 
                        // which triggers a bunch of things there.
                        // So let's wait
                        Thread.Sleep(300);
                        EQService.EstablishAgentCommunication(m_service.AgentID, neighbor.RegionHandle, IPAddress, Port, otherRegionService.UrlToInform, m_service.RegionHandle);

                        m_log.Info("[EventQueueService]: Completed inform client about neighbor " + neighbor.RegionName);
                    }
                }
                else
                {
                    m_log.Error("[EventQueueService]: Failed to inform client about neighbor " + neighbor.RegionName + ", reason: " + reason);
                    return false;
                }
                return true;
            }
            m_log.Error("[EventQueueService]: Failed to inform client about neighbor " + neighbor.RegionName + ", reason: SimulationService does not exist!");
            return false;
        }

        #endregion
    }
}
