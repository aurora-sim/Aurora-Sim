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

        public virtual void DisableSimulator(UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.DisableSimulator(RegionHandle);
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
                                        uint locationID,
                                        UUID avatarID, uint teleportFlags, ulong RegionHandle)
        {
            //Blank (for the CapsUrl) as we do not know what the CapsURL is on the sim side, it will be fixed when it reaches the grid server
            OSD item = EventQueueHelper.TeleportFinishEvent(regionHandle, simAccess, regionExternalEndPoint,
                                                            locationID, "", avatarID, teleportFlags);
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

        #region Server ONLY messages

        //
        //
        // Region > CapsService EventQueueMessages ONLY
        // These are NOT sent to the client under ANY circumstances!
        //
        //

        public virtual void EnableChildAgentsReply(UUID avatarID, ulong RegionHandle, int DrawDistance, AgentCircuitData circuit)
        {
            OSD item = EventQueueHelper.EnableChildAgents(DrawDistance, circuit);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual bool CrossAgent(GridRegion crossingRegion, Vector3 pos,
            Vector3 velocity, AgentCircuitData circuit, AgentData cAgent, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.CrossAgent(crossingRegion, pos, velocity, circuit, cAgent);
            return TryEnqueue(item, circuit.AgentID, RegionHandle);
        }

        public virtual bool TeleportAgent(UUID AgentID, int DrawDistance, AgentCircuitData circuit, 
            AgentData data, uint TeleportFlags, 
            GridRegion destination, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.TeleportAgent(DrawDistance, circuit, data, TeleportFlags, destination);
            return TryEnqueue(item, AgentID, RegionHandle);
        }

        public virtual void SendChildAgentUpdate(AgentPosition agentpos, UUID regionID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.SendChildAgentUpdate(agentpos, regionID);
            Enqueue(item, agentpos.AgentID, RegionHandle);
        }

        public virtual void CancelTeleport(UUID AgentID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.CancelTeleport(AgentID);
            Enqueue(item, AgentID, RegionHandle);
        }

        public virtual void ArrivedAtDestination(UUID AgentID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ArrivedAtDestination(AgentID);
            Enqueue(item, AgentID, RegionHandle);
        }

        #endregion

        #endregion
    }

    public class RegionClientEventQueueService : ICapsServiceConnector
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private int m_ids = 0;

        private Queue<OSD> queue = new Queue<OSD>();
        private IRegionClientCapsService m_service;
        private string m_capsPath;

        #endregion

        #region IInternalEventQueueService members

        #region Enqueue a message/Create/Remove handlers

        public void DumpEventQueue()
        {
            lock (queue)
            {
                queue.Clear();
            }
        }

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
                if (ev == null)
                    return false;

                //Check the messages to pull out ones that are creating or destroying CAPS in this or other regions
                if (ev.Type == OSDType.Map)
                {
                    OSDMap map = (OSDMap)ev;
                    if (map.ContainsKey("message") && map["message"] == "DisableSimulator")
                    {
                        //Let this pass through, after the next event queue pass we can remove it
                        //m_service.ClientCaps.RemoveCAPS(m_service.RegionHandle);
                    }
                    else if (map.ContainsKey("message") && map["message"] == "ArrivedAtDestination")
                    {
                        //Recieved a callback
                        m_service.ClientCaps.CallbackHasCome = true;

                        //Don't send it to the client
                        return true;
                    }
                    else if (map.ContainsKey("message") && map["message"] == "CancelTeleport")
                    {
                        //The user has requested to cancel the teleport, stop them.
                        m_service.ClientCaps.RequestToCancelTeleport = true;

                        //Don't send it to the client
                        return true;
                    }
                    else if (map.ContainsKey("message") && map["message"] == "SendChildAgentUpdate")
                    {
                        OSDMap body = ((OSDMap)map["body"]);

                        AgentPosition pos = new AgentPosition();
                        pos.Unpack((OSDMap)body["AgentPos"]);
                        UUID region = body["Region"].AsUUID();

                        SendChildAgentUpdate(pos, region);
                        //Don't send to the client
                        return true;
                    }
                    else if (map.ContainsKey("message") && map["message"] == "TeleportAgent")
                    {
                        OSDMap body = ((OSDMap)map["body"]);

                        GridRegion destination = new GridRegion();
                        destination.FromOSD((OSDMap)body["Region"]);

                        uint TeleportFlags = body["TeleportFlags"].AsUInteger();
                        int DrawDistance = body["DrawDistance"].AsInteger();

                        AgentCircuitData Circuit = new AgentCircuitData();
                        Circuit.UnpackAgentCircuitData((OSDMap)body["Circuit"]);

                        AgentData AgentData = new AgentData();
                        AgentData.Unpack((OSDMap)body["AgentData"]);

                        //Don't send to the client
                        return TeleportAgent(destination, TeleportFlags, DrawDistance, Circuit, AgentData);
                    }
                    else if (map.ContainsKey("message") && map["message"] == "CrossAgent")
                    {
                        //This is a simulator message that tells us to cross the agent
                        OSDMap body = ((OSDMap)map["body"]);

                        Vector3 pos = body["Pos"].AsVector3();
                        Vector3 Vel = body["Vel"].AsVector3();
                        GridRegion Region = new GridRegion();
                        Region.FromOSD((OSDMap)body["Region"]);
                        AgentCircuitData Circuit = new AgentCircuitData();
                        Circuit.UnpackAgentCircuitData((OSDMap)body["Circuit"]);
                        AgentData AgentData = new AgentData();
                        AgentData.Unpack((OSDMap)body["AgentData"]);

                        //Client doesn't get this
                        return CrossAgent(Region, pos, Vel, Circuit, AgentData);
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
                        
                        //Now do the creation
                        //Don't send it to the client at all, so return here
                        return EnableChildAgents(DrawDistance, circuitData);
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
                        otherRegionService.AddSEEDCap("", SimSeedCap, otherRegionService.Password);
                        
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
                        otherRegionService.AddSEEDCap("", SimSeedCap, otherRegionService.Password);

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
                        otherRegionService.AddSEEDCap("", SimSeedCap, otherRegionService.Password);

                        //Now tell the client about it correctly
                        ((OSDMap)((OSDArray)((OSDMap)map["body"])["Info"])[0])["SeedCapability"] = otherRegionService.CapsUrl;
                    }
                }
                lock (queue)
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

        #region Process Get/Has events

        public bool HasEvents(UUID requestID, UUID agentID)
        {
            lock (queue)
            {
                return queue.Count > 0;
            }
        }

        public Hashtable GetEvents(UUID requestID, UUID pAgentId, string request)
        {
            OSDMap events = new OSDMap();
            try
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
                    //return NoEvents(requestID, pAgentId);
                    // Send it a fake event to keep the client polling!   It doesn't like 502s like the proxys say!
                    OSDMap keepAliveEvent = new OSDMap(2);
                    keepAliveEvent.Add("body", new OSDMap());
                    keepAliveEvent.Add("message", new OSDString("FAKEEVENT"));
                    element = keepAliveEvent;
                    array.Add(keepAliveEvent);
                    //m_log.DebugFormat("[EVENTQUEUE]: adding fake event for {0} in region {1}", pAgentId, m_scene.RegionInfo.RegionName);
                }

                array.Add(element);
                lock (queue)
                {
                    while (queue.Count > 0)
                    {
                        array.Add(queue.Dequeue());
                        m_ids++;
                    }
                }

                //Look for disable Simulator EQMs so that we can disable ourselves safely
                foreach (OSD ev in array)
                {
                    try
                    {
                        if (ev.Type == OSDType.Map)
                        {
                            OSDMap map = (OSDMap)ev;
                            if (map.ContainsKey("message") && map["message"] == "DisableSimulator")
                            {
                                //This will be the last bunch of EQMs that go through, so we can safely die now
                                m_service.ClientCaps.RemoveCAPS(m_service.RegionHandle);
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                events.Add("events", array);

                events.Add("id", new OSDInteger(m_ids));
                m_ids++;
            }
            catch
            {
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
            responsedata["keepalive"] = true;
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
            lock (queue)
            {
                if (queue.Count != 0)
                    element = queue.Dequeue(); // 15s timeout
            }

            Hashtable responsedata = new Hashtable();

            if (element == null)
            {
                //m_log.ErrorFormat("[EVENTQUEUE]: Nothing to process in " + m_scene.RegionInfo.RegionName);
                return NoEvents(UUID.Zero, agentID);
            }

            OSDArray array = new OSDArray();
            array.Add(element);
            lock (queue)
            {
                while (queue.Count > 0)
                {
                    OSD item = queue.Dequeue();
                    if (item != null)
                    {
                        array.Add(item);
                        m_ids++;
                    }
                }
            }
            //Look for disable Simulator EQMs so that we can disable ourselves safely
            foreach (OSD ev in array)
            {
                try
                {
                    OSDMap map = (OSDMap)ev;
                    if (map.ContainsKey("message") && map["message"] == "DisableSimulator")
                    {
                        //This will be the last bunch of EQMs that go through, so we can safely die now
                        m_service.ClientCaps.RemoveCAPS(m_service.RegionHandle);
                    }
                }
                catch
                {
                }
            }

            OSDMap events = new OSDMap();
            events.Add("events", array);

            events.Add("id", new OSDInteger(m_ids));
            m_ids++;

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
        }

        public void EnteringRegion()
        {
            DumpEventQueue();
        }

        public void DeregisterCaps()
        {
            m_service.RemoveStreamHandler("EventQueueGet", "POST", m_capsPath);
            MainServer.Instance.RemovePollServiceHTTPHandler("", m_capsPath);
        }

        #endregion

        #region Agent code (teleporting, crossing, disabling/enabling)

        #region EnableChildAgents

        public bool EnableChildAgents(int DrawDistance, AgentCircuitData circuit)
        {
            int count = 0;
            bool informed = true;
            INeighborService neighborService = m_service.Registry.RequestModuleInterface<INeighborService>();
            if (neighborService != null)
            {
                uint x, y;
                Utils.LongToUInts(m_service.RegionHandle, out x, out y);
                GridRegion ourRegion = m_service.Registry.RequestModuleInterface<IGridService>().GetRegionByPosition(UUID.Zero, (int)x, (int)y);
                List<GridRegion> neighbors = neighborService.GetNeighbors(ourRegion, DrawDistance);

                foreach (GridRegion neighbor in neighbors)
                {
                    //m_log.WarnFormat("--> Going to send child agent to {0}, new agent {1}", neighbour.RegionName, newAgent);

                    if (neighbor.RegionHandle != m_service.RegionHandle)
                    {
                        if (!InformClientOfNeighbor(circuit.Copy(), neighbor,
                            (uint)TeleportFlags.Default, null))
                            informed = false;
                    }
                    count++;
                }
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
            uint TeleportFlags, AgentData agentData)
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
                    //Update 21-1-11 (Revolution) This is still very much needed for standalone mode
                    //Build the full URL
                    SimSeedCap
                        = neighbor.ServerURI
                      + CapsUtil.GetCapsSeedPath(CapsBase);
                    //Add the new Seed for this region
                }
                //Fix the AgentCircuitData with the new CapsUrl
                circuitData.CapsPath = CapsBase;
                //Add the password too
                circuitData.OtherInformation["CapsPassword"] = otherRegionService.Password;

                //Note: we have to pull the new grid region info as the one from the region cannot be trusted
                IGridService GridService = m_service.Registry.RequestModuleInterface<IGridService>();
                if (GridService != null)
                {
                    neighbor = GridService.GetRegionByUUID(UUID.Zero, neighbor.RegionID);
                    bool regionAccepted = SimulationService.CreateAgent(neighbor, circuitData,
                        TeleportFlags, agentData, out reason);
                    if (regionAccepted)
                    {
                        //If the region accepted us, we should get a CAPS url back as the reason, if not, its not updated or not an Aurora region, so don't touch it.
                        if (reason != "")
                        {
                            OSDMap responseMap = (OSDMap)OSDParser.DeserializeJson(reason);
                            SimSeedCap = responseMap["CapsUrl"].AsString();
                        }
                        //ONLY UPDATE THE SIM SEED HERE
                        //DO NOT PASS THE newSeedCap FROM ABOVE AS IT WILL BREAK THIS CODE
                        // AS THE CLIENT EXPECTS THE SAME CAPS SEED IF IT HAS BEEN TO THE REGION BEFORE
                        // AND FORCE UPDATING IT HERE WILL BREAK IT.
                        otherRegionService.AddSEEDCap("", SimSeedCap, otherRegionService.Password);
                        if (newAgent)
                        {
                            //We 'could' call Enqueue directly... but its better to just let it go and do it this way
                            IEventQueueService EQService = m_service.Registry.RequestModuleInterface<IEventQueueService>();

                            EQService.EnableSimulator(neighbor.RegionHandle,
                                neighbor.ExternalEndPoint.Address.GetAddressBytes(),
                                neighbor.ExternalEndPoint.Port, m_service.AgentID, m_service.RegionHandle);

                            // ES makes the client send a UseCircuitCode message to the destination, 
                            // which triggers a bunch of things there.
                            // So let's wait
                            Thread.Sleep(300);
                            EQService.EstablishAgentCommunication(m_service.AgentID, neighbor.RegionHandle,
                                neighbor.ExternalEndPoint.Address.GetAddressBytes(),
                                neighbor.ExternalEndPoint.Port, otherRegionService.UrlToInform,
                                m_service.RegionHandle);

                            m_log.Info("[EventQueueService]: Completed inform client about neighbor " + neighbor.RegionName);
                        }
                    }
                    else
                    {
                        m_log.Error("[EventQueueService]: Failed to inform client about neighbor " + neighbor.RegionName + ", reason: " + reason);
                        return false;
                    }
                }
                else
                {
                    m_log.Error("[EventQueueService]: Failed to inform client about neighbor " + neighbor.RegionName + ", reason: The Grid Service did not exist");
                    return false;
                }
                return true;
            }
            m_log.Error("[EventQueueService]: Failed to inform client about neighbor " + neighbor.RegionName + ", reason: SimulationService does not exist!");
            return false;
        }

        #endregion

        #region Crossing

        protected bool CrossAgent(GridRegion crossingRegion, Vector3 pos,
            Vector3 velocity, AgentCircuitData circuit, AgentData cAgent)
        {
            //We arn't going to deal with CallbackURLs
            SetCallbackURL(cAgent, crossingRegion);

            ISimulationService SimulationService = m_service.Registry.RequestModuleInterface<ISimulationService>();
            if (SimulationService != null)
            {
                //Note: we have to pull the new grid region info as the one from the region cannot be trusted
                IGridService GridService = m_service.Registry.RequestModuleInterface<IGridService>();
                if (GridService != null)
                {
                    //Set the user in transit so that we block duplicate tps and reset any cancelations
                    if (!SetUserInTransit())
                        return false;

                    crossingRegion = GridService.GetRegionByUUID(UUID.Zero, crossingRegion.RegionID);
                    if (!SimulationService.UpdateAgent(crossingRegion, cAgent))
                    {
                        m_log.Warn("[EventQueue]: Failed to cross agent " + m_service.AgentID + " because region did not accept it. Resetting.");
                        return false;
                    }

                    IEventQueueService EQService = m_service.Registry.RequestModuleInterface<IEventQueueService>();

                    //Tell the client about the transfer
                    EQService.CrossRegion(crossingRegion.RegionHandle, pos, velocity, crossingRegion.ExternalEndPoint,
                                       m_service.AgentID, circuit.SessionID, m_service.RegionHandle);

                    bool result = WaitForCallback();
                    if (!result)
                        m_log.Warn("[EntityTransferModule]: Callback never came in crossing agent " + circuit.AgentID + ". Resetting.");
                    else
                    {
                        // Next, let's close the child agent connections that are too far away.
                        INeighborService service = m_service.Registry.RequestModuleInterface<INeighborService>();
                        if (service != null)
                        {
                            uint x, y;
                            Utils.LongToUInts(m_service.RegionHandle, out x, out y);
                            GridRegion ourRegion = m_service.Registry.RequestModuleInterface<IGridService>().GetRegionByPosition(UUID.Zero, (int)x, (int)y);
                            service.GetNeighbors(ourRegion);
                            service.CloseNeighborAgents(crossingRegion.RegionLocX, crossingRegion.RegionLocY, m_service.AgentID, ourRegion.RegionID);
                        }
                    }

                    //All done
                    ResetFromTransit();
                    return result;
                }
            }
            return false;
        }

        #endregion

        #region Teleporting

        protected bool TeleportAgent(GridRegion destination, uint TeleportFlags, int DrawDistance, 
            AgentCircuitData circuit, AgentData agentData)
        {
            //Set the callback URL
            SetCallbackURL(agentData, destination);

            bool result = false;

            ISimulationService SimulationService = m_service.Registry.RequestModuleInterface<ISimulationService>();
            if (SimulationService != null)
            {
                //Set the user in transit so that we block duplicate tps and reset any cancelations
                if (!SetUserInTransit())
                    return false;

                //Inform the client of the neighbor if needed
                if (!InformClientOfNeighbor(circuit, destination, TeleportFlags,
                    agentData))
                    return false;

                uint x, y;
                Utils.LongToUInts(m_service.RegionHandle, out x, out y);
                GridRegion ourRegion = m_service.Registry.RequestModuleInterface<IGridService>().GetRegionByPosition(UUID.Zero, (int)x, (int)y);

                IEventQueueService EQService = m_service.Registry.RequestModuleInterface<IEventQueueService>();

                EQService.TeleportFinishEvent(destination.RegionHandle, destination.Access, destination.ExternalEndPoint,
                                           4, m_service.AgentID, TeleportFlags, m_service.RegionHandle);

                // TeleportFinish makes the client send CompleteMovementIntoRegion (at the destination), which
                // trigers a whole shebang of things there, including MakeRoot. So let's wait for confirmation
                // that the client contacted the destination before we send the attachments and close things here.

                INeighborService service = m_service.Registry.RequestModuleInterface<INeighborService>();
                if (service != null)
                {
                    bool callWasCanceled = false;
                    result = WaitForCallback(out callWasCanceled);
                    if (!result)
                    {
                        //It says it failed, lets call the sim and check
                        IAgentData data = null;
                        if (!SimulationService.RetrieveAgent(destination, m_service.AgentID, out data))
                        {
                            if (!callWasCanceled)
                            {
                                m_log.Warn("[EntityTransferModule]: Callback never came for teleporting agent " +
                                    m_service.AgentID + ". Resetting.");
                            }
                            //Close the agent at the place we just created if it isn't a neighbor
                            if (service.IsOutsideView(ourRegion.RegionLocX, destination.RegionLocX,
                                ourRegion.RegionLocY, destination.RegionLocY))
                                SimulationService.CloseAgent(destination, m_service.AgentID);
                        }
                        else
                        {
                            //Ok... the agent exists... so lets assume that it worked?
                            service.GetNeighbors(ourRegion);
                            service.CloseNeighborAgents(destination.RegionLocX, destination.RegionLocY, m_service.AgentID, ourRegion.RegionID);
                            //Make sure to set the result correctly as well
                            result = true;
                        }
                    }
                    else
                    {
                        // Next, let's close the child agent connections that are too far away.
                        service.GetNeighbors(ourRegion);
                        service.CloseNeighborAgents(destination.RegionLocX, destination.RegionLocY, m_service.AgentID, ourRegion.RegionID);
                    }
                }

                //All done
                ResetFromTransit();
            }

            return result;
        }

        protected void ResetFromTransit()
        {
            m_service.ClientCaps.InTeleport = false;
            m_service.ClientCaps.RequestToCancelTeleport = false;
            m_service.ClientCaps.CallbackHasCome = false;
        }

        protected bool SetUserInTransit()
        {
            if (m_service.ClientCaps.InTeleport)
            {
                m_log.Warn("[EventQueueService]: Got a request to teleport during another teleport for agent " + m_service.AgentID + "!");
                return false; //What??? Stop here and don't go forward
            }

            m_service.ClientCaps.InTeleport = true;
            m_service.ClientCaps.RequestToCancelTeleport = false;
            m_service.ClientCaps.CallbackHasCome = false;
            return true;
        }

        #region Callbacks

        protected void SetCallbackURL(AgentData agent, GridRegion region)
        {
            string path = "/agent/" + agent.AgentID.ToString() + "/" + region.RegionID.ToString() + "/release/";
            agent.CallbackURI = m_service.HostUri + path;
            m_service.ClientCaps.Server.AddHTTPHandler("/agent/", CallbackHandler);
        }

        protected bool WaitForCallback(out bool callWasCanceled)
        {
            int count = 100;
            while (!m_service.ClientCaps.CallbackHasCome && count > 0)
            {
                //m_log.Debug("  >>> Waiting... " + count);
                if (m_service.ClientCaps.RequestToCancelTeleport)
                {
                    //If the call was canceled, we need to break here 
                    //   now and tell the code that called us about it
                    callWasCanceled = true;
                    return true;
                }
                Thread.Sleep(100);
                count--;
            }
            //If we made it through the whole loop, we havn't been canceled,
            //    as we either have timed out or made it, so no checks are needed
            callWasCanceled = false;
            return m_service.ClientCaps.CallbackHasCome;
        }

        protected bool WaitForCallback()
        {
            int count = 100;
            while (!m_service.ClientCaps.CallbackHasCome && count > 0)
            {
                //m_log.Debug("  >>> Waiting... " + count);
                Thread.Sleep(100);
                count--;
            }
            return m_service.ClientCaps.CallbackHasCome;
        }

        public Hashtable CallbackHandler(Hashtable request)
        {
            //m_log.Debug("[CONNECTION DEBUGGING]: AgentHandler Called");

            //m_log.Debug("---------------------------");
            //m_log.Debug(" >> uri=" + request["uri"]);
            //m_log.Debug(" >> content-type=" + request["content-type"]);
            //m_log.Debug(" >> http-method=" + request["http-method"]);
            //m_log.Debug("---------------------------\n");

            Hashtable responsedata = new Hashtable();
            responsedata["content_type"] = "text/html";
            responsedata["keepalive"] = false;
            //Remove it so that it doesn't stay around
            m_service.ClientCaps.Server.RemoveHTTPHandler("POST", "/agent/");

            UUID agentID;
            UUID regionID;
            string action;
            string uri = ((string)request["uri"]);
            if (!WebUtils.GetParams(uri, out agentID, out regionID, out action))
            {
                m_log.InfoFormat("[AGENT HANDLER]: Invalid parameters for agent message {0}", request["uri"]);
                responsedata["int_response_code"] = 404;
                responsedata["str_response_string"] = "false";

                return responsedata;
            }


            responsedata["int_response_code"] = HttpStatusCode.OK;
            OSDMap map = new OSDMap();
            map["Agent"] = agentID;
            responsedata["str_response_string"] = OSDParser.SerializeJsonString(map);

            m_service.ClientCaps.CallbackHasCome = true;

            m_log.Debug("[AGENT HANDLER]: Agent Released/Deleted.");
            return responsedata;
        }

        #endregion

        #endregion

        #region Agent Update

        protected void SendChildAgentUpdate(AgentPosition agentpos, UUID regionID)
        {
            //We need to send this update out to all the child agents this region has
            INeighborService service = m_service.Registry.RequestModuleInterface<INeighborService>();
            if (service != null)
            {
                uint x, y;
                Utils.LongToUInts(m_service.RegionHandle, out x, out y);
                GridRegion ourRegion = m_service.Registry.RequestModuleInterface<IGridService>().GetRegionByPosition(UUID.Zero, (int)x, (int)y);
                service.GetNeighbors(ourRegion);
                service.SendChildAgentUpdate(agentpos, regionID);
            }
        }

        #endregion

        #endregion
    }
}
