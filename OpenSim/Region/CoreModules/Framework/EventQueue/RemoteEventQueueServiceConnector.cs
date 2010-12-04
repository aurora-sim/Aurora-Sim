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

namespace OpenSim.Region.CoreModules.Framework.EventQueue
{
    public class RemoteEventQueueServiceConnector : IEventQueue, INonSharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private bool m_enabled = false;
        private Dictionary<UUID, UUID> m_AvatarPasswordMap = new Dictionary<UUID, UUID>();
        private string m_serverURL = "";
        private Scene m_scene;

        #region IRegionModule methods

        public virtual void Initialise(IConfigSource config)
        {
            IConfig modulesConfig = config.Configs["Modules"];
            if (modulesConfig != null)
            {
                if (modulesConfig.GetString("EventQueueService", Name) == Name)
                {
                    IConfig serviceConfig = config.Configs["EventQueueService"];
                    if (serviceConfig != null)
                    {
                        m_serverURL = serviceConfig.GetString("EventQueueServiceURI") + "/CAPS/EQMPOSTER";
                    }
                    m_enabled = true;
                }
            }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;
            m_scene = scene;
            scene.RegisterModuleInterface<IEventQueue>(this);
        }

        void FindAndPopulateEQMPassword(UUID agentID)
        {
            ICapabilitiesModule module = m_scene.RequestModuleInterface<ICapabilitiesModule>();
            if (module != null)
            {
                Caps caps = module.GetCapsHandlerForUser(agentID);
                if (caps != null)
                {
                    if (caps.RequestMap.ContainsKey("EventQueuePass"))
                    {
                        UUID Password = caps.RequestMap["EventQueuePass"].AsUUID();
                        m_AvatarPasswordMap[agentID] = Password;
                    }
                }
            }
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
            get { return "RemoteEventQueueServiceConnector"; }
        }

        #endregion

        #region IEventQueue Members

        public bool Enqueue(OSD ev, UUID avatarID)
        {
            //m_log.DebugFormat("[EVENTQUEUE]: Enqueuing event for {0} in region {1}", avatarID, m_scene.RegionInfo.RegionName);
            try
            {
                FindAndPopulateEQMPassword(avatarID);

                if (!m_AvatarPasswordMap.ContainsKey(avatarID))
                    return false;

                Dictionary<string, object> request = new Dictionary<string,object>();
                request.Add("AGENTID", avatarID.ToString());
                request.Add("PASS", m_AvatarPasswordMap[avatarID].ToString());
                request.Add("LLSD", OSDParser.SerializeLLSDXmlString(ev));
                AsynchronousRestObjectRequester.MakeRequest("POST", m_serverURL, OpenSim.Server.Base.ServerUtils.BuildQueryString(request));
            } 
            catch(Exception e)
            {
                m_log.Error("[EVENTQUEUE] Caught exception: " + e);
                return false;
            }
            
            return true;
        }

        #endregion

        public void DisableSimulator(ulong handle, UUID avatarID)
        {
            OSD item = EventQueueHelper.DisableSimulator(handle);
            Enqueue(item, avatarID);
        }

        public virtual void EnableSimulator(ulong handle, IPEndPoint endPoint, UUID avatarID)
        {
            OSD item = EventQueueHelper.EnableSimulator(handle, endPoint);
            Enqueue(item, avatarID);
        }

        public virtual void EstablishAgentCommunication(UUID avatarID, IPEndPoint endPoint, string capsPath) 
        {
            OSD item = EventQueueHelper.EstablishAgentCommunication(avatarID, endPoint.ToString(), capsPath);
            Enqueue(item, avatarID);
        }

        public virtual void TeleportFinishEvent(ulong regionHandle, byte simAccess, 
                                        IPEndPoint regionExternalEndPoint,
                                        uint locationID, uint flags, string capsURL,
                                        UUID avatarID, uint teleportFlags)
        {
            OSD item = EventQueueHelper.TeleportFinishEvent(regionHandle, simAccess, regionExternalEndPoint,
                                                            locationID, flags, capsURL, avatarID, teleportFlags);
            Enqueue(item, avatarID);
        }

        public virtual void CrossRegion(ulong handle, Vector3 pos, Vector3 lookAt,
                                IPEndPoint newRegionExternalEndPoint,
                                string capsURL, UUID avatarID, UUID sessionID)
        {
            OSD item = EventQueueHelper.CrossRegion(handle, pos, lookAt, newRegionExternalEndPoint,
                                                    capsURL, avatarID, sessionID);
            Enqueue(item, avatarID);
        }

        public void ChatterboxInvitation(UUID sessionID, string sessionName,
                                         UUID fromAgent, string message, UUID toAgent, string fromName, byte dialog,
                                         uint timeStamp, bool offline, int parentEstateID, Vector3 position,
                                         uint ttl, UUID transactionID, bool fromGroup, byte[] binaryBucket)
        {
            OSD item = EventQueueHelper.ChatterboxInvitation(sessionID, sessionName, fromAgent, message, toAgent, fromName, dialog, 
                                                             timeStamp, offline, parentEstateID, position, ttl, transactionID, 
                                                             fromGroup, binaryBucket);
            Enqueue(item, toAgent);
            //m_log.InfoFormat("########### eq ChatterboxInvitation #############\n{0}", item);

        }

        public void ChatterBoxSessionAgentListUpdates(UUID sessionID, UUID fromAgent, UUID toAgent, bool canVoiceChat, 
                                                      bool isModerator, bool textMute)
        {
            OSD item = EventQueueHelper.ChatterBoxSessionAgentListUpdates(sessionID, fromAgent, canVoiceChat,
                                                                          isModerator, textMute);
            Enqueue(item, toAgent);
            //m_log.InfoFormat("########### eq ChatterBoxSessionAgentListUpdates #############\n{0}", item);
        }

        public void ChatterBoxSessionAgentListUpdates(UUID sessionID, OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock[] messages, UUID toAgent, string Transition)
        {
            OSD item = EventQueueHelper.ChatterBoxSessionAgentListUpdates(sessionID, messages, Transition);
            Enqueue(item, toAgent);
            //m_log.InfoFormat("########### eq ChatterBoxSessionAgentListUpdates #############\n{0}", item);
        }

        public void ParcelProperties(ParcelPropertiesMessage parcelPropertiesPacket, UUID avatarID)
        {
            OSD item = EventQueueHelper.ParcelProperties(parcelPropertiesPacket);
            Enqueue(item, avatarID);
        }

        public void GroupMembership(AgentGroupDataUpdatePacket groupUpdate, UUID avatarID)
        {
            OSD item = EventQueueHelper.GroupMembership(groupUpdate);
            Enqueue(item, avatarID);
        }

        public void QueryReply(PlacesReplyPacket groupUpdate, UUID avatarID, string[] info)
        {
            OSD item = EventQueueHelper.PlacesQuery(groupUpdate, info);
            Enqueue(item, avatarID);
        }
    }
}
