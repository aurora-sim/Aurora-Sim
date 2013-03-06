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

        public virtual bool Enqueue(OSD o, UUID agentID, UUID regionID)
        {
            return Enqueue(OSDParser.SerializeLLSDXmlString(o), agentID, regionID);
        }

        public virtual bool Enqueue(string o, UUID agentID, UUID regionID)
        {
            if (m_doRemoteCalls && m_doRemoteOnly)
            {
                Util.FireAndForget((none) =>
                    {
                        EnqueueInternal(o, agentID, regionID);
                    });
                return true;
            }

            //Find the CapsService for the user and enqueue the event
            IRegionClientCapsService service = GetRegionClientCapsService(agentID, regionID);
            if (service == null)
                return false;
            RegionClientEventQueueService eventQueueService = service.GetServiceConnectors().
                OfType<RegionClientEventQueueService>().FirstOrDefault();
            if (eventQueueService == null)
                return false;

            OSD ev = OSDParser.DeserializeLLSDXml(o);
            return eventQueueService.Enqueue(ev);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public virtual void EnqueueInternal(string o, UUID agentID, UUID regionID)
        {
            if (m_doRemoteCalls && m_doRemoteOnly)
                DoRemote(o, agentID, regionID);
            else
                Enqueue(o, agentID, regionID);
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

        private IRegionClientCapsService GetRegionClientCapsService(UUID agentID, UUID RegionHandle)
        {
            IClientCapsService clientCaps = m_service.GetClientCapsService(agentID);
            if (clientCaps == null)
                return null;
            //If it doesn't exist, it will be null anyway, so we don't need to check anything else
            return clientCaps.GetCapsService(RegionHandle);
        }

        #region EventQueue Message Enqueue

        public virtual void DisableSimulator(UUID avatarID, ulong RegionHandle, UUID regionID)
        {
            OSD item = EventQueueHelper.DisableSimulator(RegionHandle);
            Enqueue(item, avatarID, regionID);
        }

        public virtual void EnableSimulator(ulong handle, byte[] IPAddress, int Port, UUID avatarID, int RegionSizeX,
                                            int RegionSizeY, UUID regionID)
        {
            OSD item = EventQueueHelper.EnableSimulator(handle, IPAddress, Port, RegionSizeX, RegionSizeY);
            Enqueue(item, avatarID, regionID);
        }

        public virtual void ObjectPhysicsProperties(ISceneChildEntity[] entities, UUID avatarID, UUID regionID)
        {
            OSD item = EventQueueHelper.ObjectPhysicsProperties(entities);
            Enqueue(item, avatarID, regionID);
        }

        public virtual void EstablishAgentCommunication(UUID avatarID, ulong regionHandle, byte[] IPAddress, int Port,
                                                        string CapsUrl, int RegionSizeX, int RegionSizeY,
                                                        UUID regionID)
        {
            IPEndPoint endPoint = new IPEndPoint(new IPAddress(IPAddress), Port);
            OSD item = EventQueueHelper.EstablishAgentCommunication(avatarID, regionHandle, endPoint.ToString(), CapsUrl,
                                                                    RegionSizeX, RegionSizeY);
            Enqueue(item, avatarID, regionID);
        }

        public virtual void TeleportFinishEvent(ulong regionHandle, byte simAccess,
                                                IPAddress address, int port, string capsURL,
                                                uint locationID,
                                                UUID avatarID, uint teleportFlags, int RegionSizeX, int RegionSizeY,
                                                UUID regionID)
        {
            //Blank (for the CapsUrl) as we do not know what the CapsURL is on the sim side, it will be fixed when it reaches the grid server
            OSD item = EventQueueHelper.TeleportFinishEvent(regionHandle, simAccess, address, port,
                                                            locationID, capsURL, avatarID, teleportFlags, RegionSizeX,
                                                            RegionSizeY);
            Enqueue(item, avatarID, regionID);
        }

        public virtual void CrossRegion(ulong handle, Vector3 pos, Vector3 lookAt,
                                        IPAddress address, int port, string capsURL,
                                        UUID avatarID, UUID sessionID, int RegionSizeX, int RegionSizeY,
                                        UUID regionID)
        {
            OSD item = EventQueueHelper.CrossRegion(handle, pos, lookAt, address, port,
                                                    capsURL, avatarID, sessionID, RegionSizeX, RegionSizeY);
            Enqueue(item, avatarID, regionID);
        }

        public virtual void ChatterBoxSessionStartReply(string groupName, UUID groupID, UUID AgentID, UUID regionID)
        {
            OSD Item = EventQueueHelper.ChatterBoxSessionStartReply(groupName, groupID);
            Enqueue(Item, AgentID, regionID);
        }

        public virtual void ChatterboxInvitation(UUID sessionID, string sessionName,
                                                 UUID fromAgent, string message, UUID toAgent, string fromName,
                                                 byte dialog,
                                                 uint timeStamp, bool offline, int parentEstateID, Vector3 position,
                                                 uint ttl, UUID transactionID, bool fromGroup, byte[] binaryBucket,
                                                 UUID regionID)
        {
            OSD item = EventQueueHelper.ChatterboxInvitation(sessionID, sessionName, fromAgent, message, toAgent,
                                                             fromName, dialog,
                                                             timeStamp, offline, parentEstateID, position, ttl,
                                                             transactionID,
                                                             fromGroup, binaryBucket);
            Enqueue(item, toAgent, regionID);
            //MainConsole.Instance.InfoFormat("########### eq ChatterboxInvitation #############\n{0}", item);
        }

        public virtual void ChatterBoxSessionAgentListUpdates(UUID sessionID, UUID fromAgent, UUID toAgent,
                                                              bool canVoiceChat,
                                                              bool isModerator, bool textMute, UUID regionID)
        {
            OSD item = EventQueueHelper.ChatterBoxSessionAgentListUpdates(sessionID, fromAgent, canVoiceChat,
                                                                          isModerator, textMute);
            Enqueue(item, toAgent, regionID);
            //MainConsole.Instance.InfoFormat("########### eq ChatterBoxSessionAgentListUpdates #############\n{0}", item);
        }

        public virtual void ChatterBoxSessionAgentListUpdates(UUID sessionID,
                                                              ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock
                                                                  [] messages, UUID toAgent, string Transition,
                                                              UUID regionID)
        {
            OSD item = EventQueueHelper.ChatterBoxSessionAgentListUpdates(sessionID, messages, Transition);
            Enqueue(item, toAgent, regionID);
            //MainConsole.Instance.InfoFormat("########### eq ChatterBoxSessionAgentListUpdates #############\n{0}", item);
        }

        public virtual void ParcelProperties(ParcelPropertiesMessage parcelPropertiesPacket, UUID avatarID,
                                             UUID regionID)
        {
            OSD item = EventQueueHelper.ParcelProperties(parcelPropertiesPacket);
            Enqueue(item, avatarID, regionID);
        }

        public void ParcelObjectOwnersReply(ParcelObjectOwnersReplyMessage parcelMessage, UUID AgentID,
                                            UUID regionID)
        {
            OSD item = EventQueueHelper.ParcelObjectOwnersReply(parcelMessage);
            Enqueue(item, AgentID, regionID);
        }

        public void LandStatReply(LandStatReplyMessage message, UUID AgentID, UUID regionID)
        {
            OSD item = EventQueueHelper.LandStatReply(message);
            Enqueue(item, AgentID, regionID);
        }

        public virtual void GroupMembership(AgentGroupDataUpdatePacket groupUpdate, UUID avatarID, UUID regionID)
        {
            OSD item = EventQueueHelper.GroupMembership(groupUpdate);
            Enqueue(item, avatarID, regionID);
        }

        public virtual void QueryReply(PlacesReplyPacket groupUpdate, UUID avatarID, string[] info, UUID regionID)
        {
            OSD item = EventQueueHelper.PlacesQuery(groupUpdate, info);
            Enqueue(item, avatarID, regionID);
        }

        public virtual void ScriptRunningReply(UUID objectID, UUID itemID, bool running, bool mono,
                                               UUID avatarID, UUID regionID)
        {
            OSD Item = EventQueueHelper.ScriptRunningReplyEvent(objectID, itemID, running, true);
            Enqueue(Item, avatarID, regionID);
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
                OSDArray array = new OSDArray();
                lock (queue)
                {
                    if (queue.Count == 0)
                        return NoEvents(requestID, pAgentId);

                    while (queue.Count > 0)
                    {
                        array.Add(queue.Dequeue());
                        m_ids++;
                    }
                }

                events.Add("events", array);

                events.Add("id", new OSDInteger(m_ids));
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
            responsedata["http_protocol_version"] = "1.1";
            return responsedata;
        }

        public byte[] NoEvents(OSHttpResponse response)
        {
            response.StatusCode = 502;
            response.ContentType = "text/plain";
            response.StatusDescription = "Upstream error:";
            return System.Text.Encoding.UTF8.GetBytes("Upstream error: ");
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
                                                               return new byte[0];
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