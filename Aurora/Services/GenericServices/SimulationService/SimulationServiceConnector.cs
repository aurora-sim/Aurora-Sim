/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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

using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Threading;
using GridRegion = Aurora.Framework.Services.GridRegion;

namespace Aurora.Services
{
    public class SimulationServiceConnector : ISimulationService, IService
    {
        /// <summary>
        ///     These are regions that have timed out and we are not sending updates to until the (int) time passes
        /// </summary>
        protected Dictionary<string, int> m_blackListedRegions = new Dictionary<string, int>();

        protected ISyncMessagePosterService m_syncMessagePoster;
        protected IRegistryCore m_registry;

        #region IService Members

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlers = config.Configs["Handlers"];
            if (handlers.GetString("SimulationHandler", "") == "SimulationServiceConnector")
                registry.RegisterModuleInterface<ISimulationService>(this);

            m_registry = registry;
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
            m_syncMessagePoster = registry.RequestModuleInterface<ISyncMessagePosterService>();
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region Methods

        public bool CreateAgent(GridRegion destination, AgentCircuitData aCircuit, uint teleportFlags, out CreateAgentResponse response)
        {
            response = null;
            if (destination == null)
            {
                response = new CreateAgentResponse();
                response.Reason = "Could not connect to destination";
                response.Success = false;
                return false;
            }
            CreateAgentRequest request = new CreateAgentRequest();
            request.CircuitData = aCircuit;
            request.Destination = destination;
            request.TeleportFlags = teleportFlags;

            AutoResetEvent resetEvent = new AutoResetEvent(false);
            OSDMap result = null;
            MainConsole.Instance.DebugFormat("[SimulationServiceConnector]: Sending Create Agent to " + destination.ServerURI);
            m_syncMessagePoster.Get(destination.ServerURI, request.ToOSD(), (osdresp) =>
            {
                result = osdresp;
                resetEvent.Set();
            });
            bool success = resetEvent.WaitOne(10000);
            if (!success || result == null)
            {
                response = new CreateAgentResponse();
                response.Reason = "Could not connect to destination";
                response.Success = false;
                return false;
            }

            response = new CreateAgentResponse();
            response.FromOSD(result);

            if (!response.Success)
                return false;
            return response.Success;
        }

        public bool UpdateAgent(GridRegion destination, AgentData data)
        {
            if (m_blackListedRegions.ContainsKey(destination.ServerURI))
            {
                //Check against time
                if (m_blackListedRegions[destination.ServerURI] > 3 &&
                    Util.EnvironmentTickCountSubtract(m_blackListedRegions[destination.ServerURI]) > 0)
                {
                    MainConsole.Instance.Warn("[SimServiceConnector]: Blacklisted region " + destination.RegionName +
                                              " requested");
                    //Still blacklisted
                    return false;
                }
            }

            UpdateAgentDataRequest request = new UpdateAgentDataRequest();
            request.Update = data;
            request.Destination = destination;

            AutoResetEvent resetEvent = new AutoResetEvent(false);
            OSDMap result = null;
            m_syncMessagePoster.Get(destination.ServerURI, request.ToOSD(), (response) =>
            {
                result = response;
                resetEvent.Set();
            });
            bool success = resetEvent.WaitOne(10000);
            if (!success)
            {
                if (m_blackListedRegions.ContainsKey(destination.ServerURI))
                {
                    if (m_blackListedRegions[destination.ServerURI] == 3)
                    {
                        //add it to the blacklist as the request completely failed 3 times
                        m_blackListedRegions[destination.ServerURI] = Util.EnvironmentTickCount() + 60 * 1000; //60 seconds
                    }
                    else if (m_blackListedRegions[destination.ServerURI] == 0)
                        m_blackListedRegions[destination.ServerURI]++;
                }
                else
                    m_blackListedRegions[destination.ServerURI] = 0;
                return false;
            }

            //Clear out the blacklist if it went through
            m_blackListedRegions.Remove(destination.ServerURI);

            return result["Success"].AsBoolean();
        }

        public bool UpdateAgent(GridRegion destination, AgentPosition data)
        {
            if (m_blackListedRegions.ContainsKey(destination.ServerURI))
            {
                //Check against time
                if (m_blackListedRegions[destination.ServerURI] > 3 &&
                    Util.EnvironmentTickCountSubtract(m_blackListedRegions[destination.ServerURI]) > 0)
                {
                    MainConsole.Instance.Warn("[SimServiceConnector]: Blacklisted region " + destination.RegionName +
                                              " requested");
                    //Still blacklisted
                    return false;
                }
            }

            UpdateAgentPositionRequest request = new UpdateAgentPositionRequest();
            request.Update = data;
            request.Destination = destination;

            AutoResetEvent resetEvent = new AutoResetEvent(false);
            OSDMap result = null;
            m_syncMessagePoster.Get(destination.ServerURI, request.ToOSD(), (response) =>
            {
                result = response;
                resetEvent.Set();
            });
            bool success = resetEvent.WaitOne(10000) && result != null;
            if (!success)
            {
                if (m_blackListedRegions.ContainsKey(destination.ServerURI))
                {
                    if (m_blackListedRegions[destination.ServerURI] == 3)
                    {
                        //add it to the blacklist as the request completely failed 3 times
                        m_blackListedRegions[destination.ServerURI] = Util.EnvironmentTickCount() + 60 * 1000; //60 seconds
                    }
                    else if (m_blackListedRegions[destination.ServerURI] == 0)
                        m_blackListedRegions[destination.ServerURI]++;
                }
                else
                    m_blackListedRegions[destination.ServerURI] = 0;
                return false;
            }

            //Clear out the blacklist if it went through
            m_blackListedRegions.Remove(destination.ServerURI);

            return result["Success"].AsBoolean();
        }

        public bool FailedToMoveAgentIntoNewRegion(UUID AgentID, GridRegion destination)
        {
            FailedToMoveAgentIntoNewRegionRequest request = new FailedToMoveAgentIntoNewRegionRequest();
            request.AgentID = AgentID;
            request.RegionID = destination.RegionID;

            m_syncMessagePoster.Post(destination.ServerURI, request.ToOSD());
            return true;
        }

        public bool MakeChildAgent(UUID AgentID, GridRegion oldRegion, GridRegion destination, bool isCrossing)
        {
            MakeChildAgentRequest request = new MakeChildAgentRequest();
            request.AgentID = AgentID;
            request.Destination = destination;
            request.IsCrossing = isCrossing;

            m_syncMessagePoster.Post(oldRegion.ServerURI, request.ToOSD());
            return true;
        }

        public bool FailedToTeleportAgent(GridRegion destination, UUID failedRegionID, UUID AgentID, string reason,
                                          bool isCrossing)
        {
            FailedToTeleportAgentRequest request = new FailedToTeleportAgentRequest();
            request.AgentID = AgentID;
            request.Destination = destination;
            request.IsCrossing = isCrossing;
            request.FailedRegionID = failedRegionID;
            request.Reason = reason;

            m_syncMessagePoster.Post(destination.ServerURI, request.ToOSD());
            return true;
        }

        public bool RetrieveAgent(GridRegion destination, UUID agentID, bool agentIsLeaving, out AgentData agentData,
                                  out AgentCircuitData circuitData)
        {
            agentData = null;
            circuitData = null;

            RetrieveAgentRequest request = new RetrieveAgentRequest();
            request.AgentID = agentID;
            request.Destination = destination;
            request.AgentIsLeaving = agentIsLeaving;

            AutoResetEvent resetEvent = new AutoResetEvent(false);
            OSDMap result = null;
            m_syncMessagePoster.Get(destination.ServerURI, request.ToOSD(), (osdresp) =>
            {
                result = osdresp;
                resetEvent.Set();
            });
            bool success = resetEvent.WaitOne(10000);
            if (!success) return false;

            RetrieveAgentResponse response = new RetrieveAgentResponse();
            response.FromOSD(result);

            circuitData = response.CircuitData;
            agentData = response.AgentData;
            return response.Success;
        }

        public bool CloseAgent(GridRegion destination, UUID agentID)
        {
            CloseAgentRequest request = new CloseAgentRequest();
            request.AgentID = agentID;
            request.Destination = destination;
            m_syncMessagePoster.Post(destination.ServerURI, request.ToOSD());
            return true;
        }

        public bool CreateObject(GridRegion destination, ISceneEntity sog)
        {
            CreateObjectRequest request = new CreateObjectRequest();
            request.Object = sog;
            request.Destination = destination;
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            OSDMap result = null;
            m_syncMessagePoster.Get(destination.ServerURI, request.ToOSD(), (osdresp) =>
            {
                result = osdresp;
                resetEvent.Set();
            });
            bool success = resetEvent.WaitOne(10000);
            if (!success) return false;
            return result["Success"].AsBoolean();
        }

        #endregion
    }
}