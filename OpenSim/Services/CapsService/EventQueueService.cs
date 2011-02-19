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

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
            m_service = registry.RequestModuleInterface<ICapsService>();
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

        public virtual void EnableSimulator(ulong handle, byte[] IPAddress, int Port, UUID avatarID, int RegionSizeX, int RegionSizeY, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.EnableSimulator(handle, IPAddress, Port, RegionSizeX, RegionSizeY);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void EstablishAgentCommunication(UUID avatarID, ulong regionHandle, byte[] IPAddress, int Port, string CapsUrl, int RegionSizeX, int RegionSizeY, ulong RegionHandle)
        {
            IPEndPoint endPoint = new IPEndPoint(new IPAddress(IPAddress), Port);
            OSD item = EventQueueHelper.EstablishAgentCommunication(avatarID, regionHandle, endPoint.ToString(), CapsUrl, RegionSizeX, RegionSizeY);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void TeleportFinishEvent(ulong regionHandle, byte simAccess,
                                        IPEndPoint regionExternalEndPoint, string capsURL,
                                        uint locationID,
                                        UUID avatarID, uint teleportFlags, int RegionSizeX, int RegionSizeY, ulong RegionHandle)
        {
            //Blank (for the CapsUrl) as we do not know what the CapsURL is on the sim side, it will be fixed when it reaches the grid server
            OSD item = EventQueueHelper.TeleportFinishEvent(regionHandle, simAccess, regionExternalEndPoint,
                                                            locationID, capsURL, avatarID, teleportFlags, RegionSizeX, RegionSizeY);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void CrossRegion(ulong handle, Vector3 pos, Vector3 lookAt,
                                IPEndPoint newRegionExternalEndPoint, string capsURL,
                                UUID avatarID, UUID sessionID, int RegionSizeX, int RegionSizeY, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.CrossRegion(handle, pos, lookAt, newRegionExternalEndPoint,
                                                    capsURL, avatarID, sessionID, RegionSizeX, RegionSizeY);
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

        public void ParcelObjectOwnersReply(ParcelObjectOwnersReplyMessage parcelMessage, UUID AgentID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ParcelObjectOwnersReply(parcelMessage);
            Enqueue(item, AgentID, RegionHandle);
        }

        public void LandStatReply(LandStatReplyMessage message, UUID AgentID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.LandStatReply(message);
            Enqueue(item, AgentID, RegionHandle);
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
                                m_log.Warn("[EQService]: Disabling Simulator " + m_service.RegionHandle);
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
    }
}
