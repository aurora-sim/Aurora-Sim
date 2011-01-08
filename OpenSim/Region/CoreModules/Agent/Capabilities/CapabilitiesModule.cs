/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyrightD
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
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Caps=OpenSim.Framework.Capabilities.Caps;

namespace OpenSim.Region.CoreModules.Agent.Capabilities
{
    public class CapabilitiesModule : INonSharedRegionModule, ICapabilitiesModule
    { 
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected Scene m_scene;
        
        /// <summary>
        /// Each agent has its own capabilities handler.
        /// </summary>
        protected Dictionary<UUID, Caps> m_capsHandlers = new Dictionary<UUID, Caps>();
        
        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;
            m_scene.RegisterModuleInterface<ICapabilitiesModule>(this);
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
            m_scene.UnregisterModuleInterface<ICapabilitiesModule>(this);
        }
        
        public void PostInitialise() {}

        public void Close() {}

        public string Name 
        { 
            get { return "Capabilities Module"; } 
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void AddCapsHandler(AgentCircuitData agent)
        {
            if (!m_capsHandlers.ContainsKey(agent.AgentID))
            {
                Caps caps
                    = new Caps(m_scene,
                        MainServer.Instance, agent.AgentID);

                caps.RegisterHandlers(agent.CapsPath);

                m_scene.EventManager.TriggerOnRegisterCaps(agent.AgentID, caps);

                m_capsHandlers[agent.AgentID] = caps;
            }
        }

        public void RemoveCapsHandler(UUID agentId)
        {
            lock (m_capsHandlers)
            {
                if (m_capsHandlers.ContainsKey(agentId))
                {
                    m_capsHandlers[agentId].DeregisterHandlers();
                    m_scene.EventManager.TriggerOnDeregisterCaps(agentId, m_capsHandlers[agentId]);
                    m_capsHandlers.Remove(agentId);
                }
                else
                {
                    m_log.WarnFormat(
                        "[CapsModule]: Received request to remove CAPS handler for root agent {0} in {1}, but no such CAPS handler found!",
                        agentId, m_scene.RegionInfo.RegionName);
                }
            }
        }
        
        public Caps GetCapsHandlerForUser(UUID agentId)
        {
            lock (m_capsHandlers)
            {
                if (m_capsHandlers.ContainsKey(agentId))
                {
                    return m_capsHandlers[agentId];
                }
            }
            
            return null;
        }
    }
}
