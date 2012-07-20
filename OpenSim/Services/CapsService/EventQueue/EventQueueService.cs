/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Messages.Linden;
using OpenMetaverse.Packets;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.CapsService
{
    public class EventQueueService : ConnectorBase, IService, IEventQueueService
    {
        #region Declares

        protected ICapsService m_service;

        #endregion

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        #region IEventQueueService Members

        public virtual IEventQueueService InnerService
        {
            get { return this; }
        }

        public virtual bool Enqueue(OSD o, UUID agentID, ulong regionHandle)
        {
            return Enqueue(OSDParser.SerializeLLSDXmlString(o), agentID, regionHandle);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual bool Enqueue(string o, UUID agentID, ulong regionHandle)
        {
            object remoteValue = DoRemote(o, agentID, regionHandle);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            //Find the CapsService for the user and enqueue the event
            IRegionClientCapsService service = GetRegionClientCapsService(agentID, regionHandle);
            if (service == null)
                return false;
            RegionClientEventQueueService eventQueueService = service.GetServiceConnectors().
                OfType<RegionClientEventQueueService>().FirstOrDefault();
            if (eventQueueService == null)
                return false;

            OSD ev = OSDParser.DeserializeLLSDXml(o);
            return eventQueueService.Enqueue(ev);
        }

        #endregion

        #region IService Members

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IEventQueueService>(this);
            Init(registry, Name);
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
            m_service = registry.RequestModuleInterface<ICapsService>();
        }

        public virtual void FinishedStartup()
        {
        }

        #endregion

        private IRegionClientCapsService GetRegionClientCapsService(UUID agentID, ulong RegionHandle)
        {
            IClientCapsService clientCaps = m_service.GetClientCapsService(agentID);
            if (clientCaps == null)
                return null;
            IRegionClientCapsService regionCaps = clientCaps.GetCapsService(RegionHandle);
            //If it doesn't exist, it will be null anyway, so we don't need to check anything else
            return regionCaps;
        }

        #region EventQueue Message Enqueue

        public virtual void DisableSimulator(UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.DisableSimulator(RegionHandle);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void EnableSimulator(ulong handle, byte[] IPAddress, int Port, UUID avatarID, int RegionSizeX,
                                            int RegionSizeY, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.EnableSimulator(handle, IPAddress, Port, RegionSizeX, RegionSizeY);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void ObjectPhysicsProperties(ISceneChildEntity[] entities, UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ObjectPhysicsProperties(entities);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void EstablishAgentCommunication(UUID avatarID, ulong regionHandle, byte[] IPAddress, int Port,
                                                        string CapsUrl, int RegionSizeX, int RegionSizeY,
                                                        ulong RegionHandle)
        {
            IPEndPoint endPoint = new IPEndPoint(new IPAddress(IPAddress), Port);
            OSD item = EventQueueHelper.EstablishAgentCommunication(avatarID, regionHandle, endPoint.ToString(), CapsUrl,
                                                                    RegionSizeX, RegionSizeY);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void TeleportFinishEvent(ulong regionHandle, byte simAccess,
                                                IPAddress address, int port, string capsURL,
                                                uint locationID,
                                                UUID avatarID, uint teleportFlags, int RegionSizeX, int RegionSizeY,
                                                ulong RegionHandle)
        {
            //Blank (for the CapsUrl) as we do not know what the CapsURL is on the sim side, it will be fixed when it reaches the grid server
            OSD item = EventQueueHelper.TeleportFinishEvent(regionHandle, simAccess, address, port,
                                                            locationID, capsURL, avatarID, teleportFlags, RegionSizeX,
                                                            RegionSizeY);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void CrossRegion(ulong handle, Vector3 pos, Vector3 lookAt,
                                        IPAddress address, int port, string capsURL,
                                        UUID avatarID, UUID sessionID, int RegionSizeX, int RegionSizeY,
                                        ulong RegionHandle)
        {
            OSD item = EventQueueHelper.CrossRegion(handle, pos, lookAt, address, port,
                                                    capsURL, avatarID, sessionID, RegionSizeX, RegionSizeY);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void ChatterBoxSessionStartReply(string groupName, UUID groupID, UUID AgentID, ulong RegionHandle)
        {
            OSD Item = EventQueueHelper.ChatterBoxSessionStartReply(groupName, groupID);
            Enqueue(Item, AgentID, RegionHandle);
        }

        public virtual void ChatterboxInvitation(UUID sessionID, string sessionName,
                                                 UUID fromAgent, string message, UUID toAgent, string fromName,
                                                 byte dialog,
                                                 uint timeStamp, bool offline, int parentEstateID, Vector3 position,
                                                 uint ttl, UUID transactionID, bool fromGroup, byte[] binaryBucket,
                                                 ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ChatterboxInvitation(sessionID, sessionName, fromAgent, message, toAgent,
                                                             fromName, dialog,
                                                             timeStamp, offline, parentEstateID, position, ttl,
                                                             transactionID,
                                                             fromGroup, binaryBucket);
            Enqueue(item, toAgent, RegionHandle);
            //MainConsole.Instance.InfoFormat("########### eq ChatterboxInvitation #############\n{0}", item);
        }

        public virtual void ChatterBoxSessionAgentListUpdates(UUID sessionID, UUID fromAgent, UUID toAgent,
                                                              bool canVoiceChat,
                                                              bool isModerator, bool textMute, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ChatterBoxSessionAgentListUpdates(sessionID, fromAgent, canVoiceChat,
                                                                          isModerator, textMute);
            Enqueue(item, toAgent, RegionHandle);
            //MainConsole.Instance.InfoFormat("########### eq ChatterBoxSessionAgentListUpdates #############\n{0}", item);
        }

        public virtual void ChatterBoxSessionAgentListUpdates(UUID sessionID,
                                                              ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock
                                                                  [] messages, UUID toAgent, string Transition,
                                                              ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ChatterBoxSessionAgentListUpdates(sessionID, messages, Transition);
            Enqueue(item, toAgent, RegionHandle);
            //MainConsole.Instance.InfoFormat("########### eq ChatterBoxSessionAgentListUpdates #############\n{0}", item);
        }

        public virtual void ParcelProperties(ParcelPropertiesMessage parcelPropertiesPacket, UUID avatarID,
                                             ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ParcelProperties(parcelPropertiesPacket);
            Enqueue(item, avatarID, RegionHandle);
        }

        public void ParcelObjectOwnersReply(ParcelObjectOwnersReplyMessage parcelMessage, UUID AgentID,
                                            ulong RegionHandle)
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

        private readonly Queue<OSD> queue = new Queue<OSD>();
        private string m_capsPath;
        private int m_ids;
        private bool _isValid = false;
        private IRegionClientCapsService m_service;

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
        ///   Add the given event into the client's queue so that it is sent on the next
        /// </summary>
        /// <param name = "ev"></param>
        /// <param name = "avatarID"></param>
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
                MainConsole.Instance.Error("[EVENTQUEUE] Caught exception: " + e);
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
                    element = queue.Dequeue();
                }

                OSDArray array = new OSDArray();
                if (element == null) // didn't have an event in 15s
                {
                    //return NoEvents(requestID, pAgentId);
                    // Send it a fake event to keep the client polling!   It doesn't like 502s like the proxys say!
                    OSDMap keepAliveEvent = new OSDMap(2)
                                                {{"body", new OSDMap()}, {"message", new OSDString("FAKEEVENT")}};
                    element = keepAliveEvent;
                    array.Add(keepAliveEvent);
                    //MainConsole.Instance.DebugFormat("[EVENTQUEUE]: adding fake event for {0} in region {1}", pAgentId, m_scene.RegionInfo.RegionName);
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

                int removeAt = -1;
                //Look for disable Simulator EQMs so that we can disable ourselves safely
                foreach (OSD ev in array)
                {
                    try
                    {
                        if (ev.Type == OSDType.Map)
                        {
                            OSDMap map = (OSDMap) ev;
                            if (map.ContainsKey("message") && map["message"] == "DisableSimulator")
                            {
                                MainConsole.Instance.Debug("[EQService]: Sim Request to Disable Simulator " + m_service.RegionHandle);
                                removeAt = array.IndexOf(ev);
                                //This will be the last bunch of EQMs that go through, so we can safely die now
                                //Except that we can't do this, the client will freak if we do this
                                //m_service.ClientCaps.RemoveCAPS(m_service.RegionHandle);
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                if (removeAt != -1)
                    array.RemoveAt(removeAt);

                events.Add("events", array);

                events.Add("id", new OSDInteger(m_ids));
                m_ids++;
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[EQS]: Exception! " + ex);
            }
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200;
            responsedata["content_type"] = "application/xml";
            responsedata["keepalive"] = false;
            responsedata["reusecontext"] = false;
            responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(events);
            //MainConsole.Instance.DebugFormat("[EVENTQUEUE]: sending response for {0} in region {1}: {2}", pAgentId, m_scene.RegionInfo.RegionName, responsedata["str_response_string"]);
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

        public byte[] NoEvents(OSHttpResponse response)
        {
            response.StatusCode = 502;
            response.ContentType = "text/plain";
            response.StatusDescription = "Upstream error:";
            return System.Text.Encoding.UTF8.GetBytes("Upstream error: ");
        }

        public byte[] ProcessQueue(OSHttpResponse response, UUID agentID)
        {
            // TODO: this has to be redone to not busy-wait (and block the thread),
            // TODO: as soon as we have a non-blocking way to handle HTTP-requests.

            //            if (MainConsole.Instance.IsDebugEnabled)
            //            { 
            //                String debug = "[EVENTQUEUE]: Got request for agent {0} in region {1} from thread {2}: [  ";
            //                foreach (object key in request.Keys)
            //                {
            //                    debug += key.ToString() + "=" + request[key].ToString() + "  ";
            //                }
            //                MainConsole.Instance.DebugFormat(debug + "  ]", agentID, m_scene.RegionInfo.RegionName, System.Threading.Thread.CurrentThread.Name);
            //            }
            //MainConsole.Instance.Warn("Got EQM get at " + m_handler.CapsURL);
            OSD element = null;
            lock (queue)
            {
                if (queue.Count != 0)
                    element = queue.Dequeue();
            }

            if (element == null)
            {
                //MainConsole.Instance.ErrorFormat("[EVENTQUEUE]: Nothing to process in " + m_scene.RegionInfo.RegionName);
                return NoEvents(response);
            }

            OSDArray array = new OSDArray {element};
            lock (queue)
            {
                while (queue.Count > 0)
                {
                    OSD item = queue.Dequeue();
                    if (item != null)
                    {
                        OSDMap map = (OSDMap) item;
                        if (map.ContainsKey("message") && map["message"] == "DisableSimulator")
                        {
                            continue;
                        }
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
                    OSDMap map = (OSDMap) ev;
                    if (map.ContainsKey("message") && map["message"] == "DisableSimulator")
                    {
                        //Disabled above
                        //This will be the last bunch of EQMs that go through, so we can safely die now
                        m_service.ClientCaps.RemoveCAPS(m_service.RegionHandle);
                    }
                }
                catch
                {
                }
            }

            OSDMap events = new OSDMap {{"events", array}, {"id", new OSDInteger(m_ids)}};

            m_ids++;

            response.ContentType = "application/xml";
            return OSDParser.SerializeLLSDXmlBytes(events);
        }

        public Hashtable EventQueuePoll(Hashtable request)
        {
            return new Hashtable();
        }

        public bool Valid()
        {
            return _isValid;
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
            m_service.AddStreamHandler("EventQueueGet", new GenericStreamHandler("POST", m_capsPath,
                                                           delegate(string path, System.IO.Stream request,
                                                               OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                           {
                                                               return ProcessQueue(httpResponse, service.AgentID);
                                                           }));

            // This will persist this beyond the expiry of the caps handlers
            _isValid = true;
            MainServer.Instance.AddPollServiceHTTPHandler(
                m_capsPath, EventQueuePoll,
                new PollServiceEventArgs(null, HasEvents, GetEvents, NoEvents, Valid, service.AgentID));

            Random rnd = new Random(Environment.TickCount);
            m_ids = rnd.Next(30000000);
        }

        public void EnteringRegion()
        {
            DumpEventQueue();
        }

        public void DeregisterCaps()
        {
            _isValid = false;
            m_service.RemoveStreamHandler("EventQueueGet", "POST", m_capsPath);
            MainServer.Instance.RemovePollServiceHTTPHandler("POST", m_capsPath);
        }

        #endregion
    }
}