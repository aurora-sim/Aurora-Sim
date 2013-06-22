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
using System.Collections.Generic;
using GridRegion = Aurora.Framework.Services.GridRegion;

namespace Aurora.Services
{
    public class LocalSimulationServiceConnector : IService
    {
        #region Declares

        private IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_registry.RequestModuleInterface<ISyncMessageRecievedService>().OnMessageReceived += LocalSimulationServiceConnector_OnMessageReceived;
        }

        OSDMap LocalSimulationServiceConnector_OnMessageReceived(OSDMap message)
        {
            if (!message.ContainsKey("Method"))
                return null;
            switch (message["Method"].AsString())
            {
                case "CreateAgentRequest":
                    CreateAgentRequest createAgentRequest = new CreateAgentRequest();
                    createAgentRequest.FromOSD(message);
                    CreateAgentResponse createAgentResponse = new CreateAgentResponse();
                    createAgentResponse.Success = CreateAgent(createAgentRequest.Destination, createAgentRequest.CircuitData, createAgentRequest.TeleportFlags, out createAgentResponse);
                    return createAgentResponse.ToOSD();
                case "UpdateAgentPositionRequest":
                    UpdateAgentPositionRequest updateAgentPositionRequest = new UpdateAgentPositionRequest();
                    updateAgentPositionRequest.FromOSD(message);
                    return new OSDMap() { new KeyValuePair<string, OSD>("Success", UpdateAgent(updateAgentPositionRequest.Destination, updateAgentPositionRequest.Update)) };
                case "UpdateAgentDataRequest":
                    UpdateAgentDataRequest updateAgentDataRequest = new UpdateAgentDataRequest();
                    updateAgentDataRequest.FromOSD(message);
                    return new OSDMap() { new KeyValuePair<string, OSD>("Success", UpdateAgent(updateAgentDataRequest.Destination, updateAgentDataRequest.Update)) };
                case "FailedToMoveAgentIntoNewRegionRequest":
                    FailedToMoveAgentIntoNewRegionRequest failedToMoveAgentIntoNewRegionRequest = new FailedToMoveAgentIntoNewRegionRequest();
                    failedToMoveAgentIntoNewRegionRequest.FromOSD(message);
                    FailedToMoveAgentIntoNewRegion(failedToMoveAgentIntoNewRegionRequest.AgentID, failedToMoveAgentIntoNewRegionRequest.RegionID);
                    break;
                case "CloseAgentRequest":
                    CloseAgentRequest closeAgentRequest = new CloseAgentRequest();
                    closeAgentRequest.FromOSD(message);
                    CloseAgent(closeAgentRequest.Destination, closeAgentRequest.AgentID);
                    break;
                case "MakeChildAgentRequest":
                    MakeChildAgentRequest makeChildAgentRequest = new MakeChildAgentRequest();
                    makeChildAgentRequest.FromOSD(message);
                    MakeChildAgent(makeChildAgentRequest.AgentID, makeChildAgentRequest.Destination, makeChildAgentRequest.IsCrossing);
                    break;
                case "FailedToTeleportAgentRequest":
                    FailedToTeleportAgentRequest failedToTeleportAgentRequest = new FailedToTeleportAgentRequest();
                    failedToTeleportAgentRequest.FromOSD(message);
                    FailedToTeleportAgent(failedToTeleportAgentRequest.Destination, failedToTeleportAgentRequest.FailedRegionID,
                        failedToTeleportAgentRequest.AgentID, failedToTeleportAgentRequest.Reason, failedToTeleportAgentRequest.IsCrossing);
                    break;
                case "RetrieveAgentRequest":
                    RetrieveAgentRequest retrieveAgentRequest = new RetrieveAgentRequest();
                    retrieveAgentRequest.FromOSD(message);
                    RetrieveAgentResponse retrieveAgentResponse = new RetrieveAgentResponse();
                    retrieveAgentResponse.Success = RetrieveAgent(retrieveAgentRequest.Destination, retrieveAgentRequest.AgentID, retrieveAgentRequest.AgentIsLeaving,
                        out retrieveAgentResponse.AgentData, out retrieveAgentResponse.CircuitData);
                    return retrieveAgentResponse.ToOSD();
                case "CreateObjectRequest":
                    CreateObjectRequest createObjectRequest = new CreateObjectRequest();
                    createObjectRequest.FromOSD(message);
                    createObjectRequest.Scene = GetScene(createObjectRequest.Destination.RegionID);
                    createObjectRequest.DeserializeObject();
                    return new OSDMap() { new KeyValuePair<string, OSD>("Success", CreateObject(createObjectRequest.Destination, createObjectRequest.Object)) };
            }
            return null;
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region ISimulation

        private IScene GetScene(UUID regionID)
        {
            ISceneManager man = m_registry.RequestModuleInterface<ISceneManager>();
            if (man != null)
                return man.Scenes.Find((s) => s.RegionInfo.RegionID == regionID);
            return null;
        }

        /**
         * Agent-related communications
         */

        public bool CreateAgent(GridRegion destination, AgentCircuitData aCircuit, uint teleportFlags, out CreateAgentResponse response)
        {
            response = new CreateAgentResponse();
            IScene Scene = destination == null ? null : GetScene(destination.RegionID);
            if (destination == null || Scene == null)
            {
                response.Reason = "Given destination was null";
                response.Success = false;
                return false;
            }

            if (Scene.RegionInfo.RegionID != destination.RegionID)
            {
                response.Reason = "Did not find region " + destination.RegionName;;
                response.Success = false;
                return false;
            }
            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                return transferModule.NewUserConnection(Scene, aCircuit, teleportFlags, out response);

            response.Reason = "Did not find region " + destination.RegionName;
            response.Success = false;
            return false;
        }

        public bool UpdateAgent(GridRegion destination, AgentData agentData)
        {
            IScene Scene = destination == null ? null : GetScene(destination.RegionID);
            if (destination == null || Scene == null || agentData == null)
                return false;

            bool retVal = false;
            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                retVal = transferModule.IncomingChildAgentDataUpdate(Scene, agentData);

            //            MainConsole.Instance.DebugFormat("[LOCAL COMMS]: Did not find region {0} for ChildAgentUpdate", regionHandle);
            return retVal;
        }

        public bool UpdateAgent(GridRegion destination, AgentPosition agentData)
        {
            IScene Scene = destination == null ? null : GetScene(destination.RegionID);
            if (Scene == null || destination == null)
                return false;

            //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to send ChildAgentUpdate");
            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                return transferModule.IncomingChildAgentDataUpdate(Scene, agentData);

            return false;
        }

        public bool FailedToMoveAgentIntoNewRegion(UUID AgentID, UUID RegionID)
        {
            IScene Scene = GetScene(RegionID);
            if (Scene == null)
                return false;

            IScenePresence sp = Scene.GetScenePresence(AgentID);
            if (sp != null)
                sp.AgentFailedToLeave();
            return true;
        }

        public bool MakeChildAgent(UUID AgentID, GridRegion destination, bool isCrossing)
        {
            IScene Scene = destination == null ? null : GetScene(destination.RegionID);
            if (Scene == null)
                return false;

            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule == null) return false;
            transferModule.MakeChildAgent(Scene.GetScenePresence(AgentID), destination, isCrossing);
            return true;
        }

        public bool FailedToTeleportAgent(GridRegion destination, UUID failedRegionID, UUID agentID, string reason,
                                          bool isCrossing)
        {
            IScene Scene = destination == null ? null : GetScene(destination.RegionID);
            if (Scene == null)
                return false;

            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule == null) return false;
            transferModule.FailedToTeleportAgent(destination, agentID, reason, isCrossing);
            return true;
        }

        public bool RetrieveAgent(GridRegion destination, UUID agentID, bool agentIsLeaving, out AgentData agentData,
                                  out AgentCircuitData circuitData)
        {
            agentData = null;
            circuitData = null;

            IScene Scene = destination == null ? null : GetScene(destination.RegionID);
            if (Scene == null || destination == null)
                return false;

            //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to send ChildAgentUpdate");
            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                return transferModule.IncomingRetrieveRootAgent(Scene, agentID, agentIsLeaving, out agentData,
                                                                out circuitData);
            return false;
            //MainConsole.Instance.Debug("[LOCAL COMMS]: region not found for ChildAgentUpdate");
        }

        public bool CloseAgent(GridRegion destination, UUID agentID)
        {
            IScene Scene = destination == null ? null : GetScene(destination.RegionID);
            if (Scene == null || destination == null)
                return false;

            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
                return transferModule.IncomingCloseAgent(Scene, agentID);
            return false;
        }

        /**
         * Object-related communications
         */

        public bool CreateObject(GridRegion destination, ISceneEntity sog)
        {
            IScene Scene = destination == null ? null : GetScene(destination.RegionID);
            if (Scene == null || destination == null)
                return false;

            //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to SendCreateObject");
            IEntityTransferModule AgentTransferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
            if (AgentTransferModule != null)
                return AgentTransferModule.IncomingCreateObject(Scene.RegionInfo.RegionID, sog);

            return false;
        }

        #endregion
    }
}