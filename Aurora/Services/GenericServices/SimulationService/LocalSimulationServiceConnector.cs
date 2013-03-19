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
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using GridRegion = Aurora.Framework.Services.GridRegion;

namespace Aurora.Services
{
    public class LocalSimulationServiceConnector : IService, ISimulationService
    {
        private IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region ISimulation

        public IScene Scene
        {
            get
            {
                ISceneManager man = m_registry.RequestModuleInterface<ISceneManager>();
                if (man != null)
                    return man.Scene;
                return null;
            }
        }

        public ISimulationService InnerService
        {
            get { return this; }
        }

        /**
         * Agent-related communications
         */

        public bool CreateAgent(GridRegion destination, AgentCircuitData aCircuit, uint teleportFlags,
                                AgentData data, out int requestedUDPPort, out string reason)
        {
            OSDMap map = new OSDMap();
            requestedUDPPort = 0;
            if (destination == null || Scene == null)
            {
                map["Reason"] = "Given destination was null";
                map["Success"] = false;
                reason = OSDParser.SerializeJsonString(map);
                return false;
            }
            if (destination.ExternalEndPoint != null) requestedUDPPort = destination.ExternalEndPoint.Port;

            //MainConsole.Instance.DebugFormat("[LOCAL SIMULATION CONNECTOR]: Found region {0} to send SendCreateChildAgent", destination.RegionName);

            if (Scene.RegionInfo.RegionID != destination.RegionID)
            {
                map["Reason"] = "Did not find region " + destination.RegionName;
                map["Success"] = false;
                reason = OSDParser.SerializeJsonString(map);
                return false;
            }
            if (data != null)
                UpdateAgent(destination, data);
            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                return transferModule.NewUserConnection(Scene, aCircuit, teleportFlags, out requestedUDPPort,
                                                        out reason);

            MainConsole.Instance.DebugFormat("[LOCAL SIMULATION CONNECTOR]: Did not find region {0} for CreateAgent",
                                             destination.RegionName);
            map["Reason"] = "Did not find region " + destination.RegionName;
            map["Success"] = false;
            reason = OSDParser.SerializeJsonString(map);
            return false;
        }

        public bool UpdateAgent(GridRegion destination, AgentData cAgentData)
        {
            if (destination == null || Scene == null || cAgentData == null)
                return false;

            bool retVal = false;
            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                retVal = transferModule.IncomingChildAgentDataUpdate(Scene, cAgentData);

            //            MainConsole.Instance.DebugFormat("[LOCAL COMMS]: Did not find region {0} for ChildAgentUpdate", regionHandle);
            return retVal;
        }

        public bool UpdateAgent(GridRegion destination, AgentPosition cAgentData)
        {
            if (Scene == null || destination == null)
                return false;

            bool retVal = false;
            //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to send ChildAgentUpdate");
            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                if (retVal)
                    transferModule.IncomingChildAgentDataUpdate(Scene, cAgentData);
                else
                    retVal = transferModule.IncomingChildAgentDataUpdate(Scene, cAgentData);
            //MainConsole.Instance.Debug("[LOCAL COMMS]: region not found for ChildAgentUpdate");
            return retVal;
        }

        public bool FailedToMoveAgentIntoNewRegion(UUID AgentID, UUID RegionID)
        {
            if (Scene == null)
                return false;

            IScenePresence sp = Scene.GetScenePresence(AgentID);
            if (sp != null)
            {
                sp.AgentFailedToLeave();
            }
            return false;
        }

        public bool MakeChildAgent(UUID AgentID, UUID leavingRegion, GridRegion destination, bool isCrossing)
        {
            if (Scene == null)
                return false;

            //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to send ChildAgentUpdate");
            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule == null) return false;
            transferModule.MakeChildAgent(Scene.GetScenePresence(AgentID), destination, isCrossing);
            return true;
        }

        public bool FailedToTeleportAgent(GridRegion destination, UUID failedRegionID, UUID agentID, string reason,
                                          bool isCrossing)
        {
            if (Scene == null)
                return false;

            //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to send FailedToTeleportAgent");
            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule == null) return false;
            transferModule.FailedToTeleportAgent(new GridRegion() {RegionID = failedRegionID},
                                                 agentID, reason, isCrossing);
            return true;
        }

        public bool RetrieveAgent(GridRegion destination, UUID id, bool agentIsLeaving, out AgentData agent,
                                  out AgentCircuitData circuitData)
        {
            agent = null;
            circuitData = null;

            if (Scene == null || destination == null)
                return false;

            //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to send ChildAgentUpdate");
            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                return transferModule.IncomingRetrieveRootAgent(Scene, id, agentIsLeaving, out agent,
                                                                out circuitData);
            return false;
            //MainConsole.Instance.Debug("[LOCAL COMMS]: region not found for ChildAgentUpdate");
        }

        public bool CloseAgent(GridRegion destination, UUID id)
        {
            if (Scene == null || destination == null)
                return false;

            //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to SendCloseAgent");
            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                return transferModule.IncomingCloseAgent(Scene, id);
            MainConsole.Instance.Debug("[LOCAL SIMULATION COMMS]: region not found in CloseAgent");
            return false;
        }

        /**
         * Object-related communications
         */

        public bool CreateObject(GridRegion destination, ISceneEntity sog)
        {
            if (Scene == null || destination == null)
                return false;

            //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to SendCreateObject");
            IEntityTransferModule AgentTransferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (AgentTransferModule != null)
            {
                return AgentTransferModule.IncomingCreateObject(Scene.RegionInfo.RegionID, sog);
            }
            MainConsole.Instance.Warn("[LOCAL SIMULATION COMMS]: region not found in CreateObject");
            return false;
        }

        public bool CreateObject(GridRegion destination, UUID userID, UUID itemID)
        {
            if (Scene == null || destination == null)
                return false;

            IEntityTransferModule AgentTransferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (AgentTransferModule != null)
            {
                return AgentTransferModule.IncomingCreateObject(Scene.RegionInfo.RegionID, userID, itemID);
            }
            return false;
        }

        public bool IsLocalRegion(ulong handle)
        {
            if (Scene == null)
                return false;

            return Scene.RegionInfo.RegionHandle == handle;
        }

        #endregion /* ISimulationService */

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlers = config.Configs["Handlers"];
            if (handlers.GetString("SimulationHandler", "") == Name)
                registry.RegisterModuleInterface<ISimulationService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}