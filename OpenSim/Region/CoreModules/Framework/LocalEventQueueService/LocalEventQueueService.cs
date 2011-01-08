/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Messages.Linden;
using OpenMetaverse.Packets;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using BlockingLLSDQueue = OpenSim.Framework.BlockingQueue<OpenMetaverse.StructuredData.OSD>;
using Caps = OpenSim.Framework.Capabilities.Caps;
using OpenSim.Framework.Capabilities;
using OpenSim.Services.Interfaces;

namespace OpenSim.Region.CoreModules.Framework.EventQueue
{
    public struct QueueItem
    {
        public int id;
        public OSDMap body;
    }

    public class EventQueueGetModule : EventQueueModuleBase, IEventQueueService, INonSharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<UUID, int> m_ids = new Dictionary<UUID, int>();

        private Dictionary<UUID, Queue<OSD>> queues = new Dictionary<UUID, Queue<OSD>>();
        private Dictionary<UUID, UUID> m_QueueUUIDAvatarMapping = new Dictionary<UUID, UUID>();
        private Dictionary<UUID, UUID> m_AvatarQueueUUIDMapping = new Dictionary<UUID, UUID>();
        private bool m_enabled = false;
        #region IRegionModule methods

        public virtual void Initialise(IConfigSource config)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueHandler", "") != Name)
                return;
            m_enabled = true;   
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;
            scene.RegisterModuleInterface<IEventQueueService>(this);

            // Register fallback handler
            // TODO: Leaving these open, or closing them when we
            // become a child is incorrect. It messes up TP in a big
            // way. CAPS/EQ need to be active as long as the UDP
            // circuit is there.

            scene.EventManager.OnClientClosed += ClientClosed;
            scene.EventManager.OnRegisterCaps += OnRegisterCaps;
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise()
        {
        }

        public virtual void Close()
        {
        }

        public virtual string Name
        {
            get { return "EventQueueGetModule"; }
        }

        #endregion

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
                    /*
                    m_log.DebugFormat(
                        "[EVENTQUEUE]: Adding new queue for agent {0} in region {1}", 
                        agentId, m_scene.RegionInfo.RegionName);
                    */
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

        #region IEventQueue Members

        public override bool Enqueue(OSD ev, UUID avatarID, ulong regionHandle)
        {
            //m_log.DebugFormat("[EVENTQUEUE]: Enqueuing event for {0} in region {1}", avatarID, m_scene.RegionInfo.RegionName);
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

        public override bool TryEnqueue(OSD ev, UUID avatarID, ulong regionHandle)
        {
            return Enqueue(ev, avatarID, regionHandle);
        }

        #endregion

        private void ClientClosed(UUID AgentID, Scene scene)
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
        }

        public void OnRegisterCaps(UUID agentID, Caps caps)
        {
            // Register an event queue for the client

            //m_log.DebugFormat(
            //    "[EVENTQUEUE]: OnRegisterCaps: agentID {0} caps {1} region {2}", 
            //    agentID, caps, m_scene.RegionInfo.RegionName);

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
            caps.RegisterHandler("EventQueueGet",
                                 new RestHTTPHandler("POST", capsBase + EventQueueGetUUID.ToString() + "/",
                                                       delegate(Hashtable m_dhttpMethod)
                                                       {
                                                           return ProcessQueue(m_dhttpMethod, agentID);
                                                       }));

            // This will persist this beyond the expiry of the caps handlers
            MainServer.Instance.AddPollServiceHTTPHandler(
                capsBase + EventQueueGetUUID.ToString() + "/", EventQueuePoll, new PollServiceEventArgs(null, HasEvents, GetEvents, NoEvents, agentID));

            Random rnd = new Random(Environment.TickCount);
            lock (m_ids)
            {
                if (!m_ids.ContainsKey(agentID))
                    m_ids.Add(agentID, rnd.Next(30000000));
            }
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
                // Send it a fake event to keep the client polling!   It doesn't like 502s like the proxys say!
                array.Add(EventQueueHelper.KeepAliveEvent());
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
                // Send it a fake event to keep the client polling!   It doesn't like 502s like the proxys say!
                array.Add(EventQueueHelper.KeepAliveEvent());
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

        public bool AuthenticateRequest(UUID agentID, UUID password, ulong RegionHandle)
        {
            return true;
        }
    }
}