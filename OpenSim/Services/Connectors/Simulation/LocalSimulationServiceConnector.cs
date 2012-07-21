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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.Connectors.Simulation
{
    public class LocalSimulationServiceConnector : IService, ISimulationService
    {
        private readonly List<IScene> m_sceneList = new List<IScene>();

        public string Name
        {
            get { return GetType().Name; }
        }

        #region ISimulation

        public IScene GetScene(ulong regionhandle)
        {
#if (!ISWIN)
            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionHandle == regionhandle) return s;
            }
            return null;
#else
            return m_sceneList.FirstOrDefault(s => s.RegionInfo.RegionHandle == regionhandle);
#endif
            // ? weird. should not happen
        }

        public ISimulationService GetInnerService()
        {
            return this;
        }

        /// <summary>
        ///   Can be called from other modules.
        /// </summary>
        /// <param name = "scene"></param>
        public void RemoveScene(IScene scene)
        {
            lock (m_sceneList)
            {
                m_sceneList.Remove(scene);
            }
        }

        /// <summary>
        ///   Can be called from other modules.
        /// </summary>
        /// <param name = "scene"></param>
        public void Init(IScene scene)
        {
            lock (m_sceneList)
            {
                m_sceneList.Add(scene);
            }
        }

        /**
         * Agent-related communications
         */

        public bool CreateAgent(GridRegion destination, AgentCircuitData aCircuit, uint teleportFlags,
                                AgentData data, out int requestedUDPPort, out string reason)
        {
            requestedUDPPort = 0;
            if (destination == null)
            {
                reason = "Given destination was null";
                MainConsole.Instance.DebugFormat("[LOCAL SIMULATION CONNECTOR]: CreateAgent was given a null destination");
                return false;
            }
            if (destination.ExternalEndPoint != null) requestedUDPPort = destination.ExternalEndPoint.Port;

#if (!ISWIN)
            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionID == destination.RegionID)
                {
                    //MainConsole.Instance.DebugFormat("[LOCAL SIMULATION CONNECTOR]: Found region {0} to send SendCreateChildAgent", destination.RegionName);
                    if (data != null)
                        UpdateAgent(destination, data);
                    IEntityTransferModule transferModule = s.RequestModuleInterface<IEntityTransferModule>();
                    if (transferModule != null)
                        return transferModule.NewUserConnection(s, aCircuit, teleportFlags, out requestedUDPPort, out reason);
                }
            }
#else
            foreach (IScene s in m_sceneList.Where(s => s.RegionInfo.RegionID == destination.RegionID))
            {
                //MainConsole.Instance.DebugFormat("[LOCAL SIMULATION CONNECTOR]: Found region {0} to send SendCreateChildAgent", destination.RegionName);
                if (data != null)
                    UpdateAgent(destination, data);
                IEntityTransferModule transferModule = s.RequestModuleInterface<IEntityTransferModule>();
                if (transferModule != null)
                    return transferModule.NewUserConnection(s, aCircuit, teleportFlags, out requestedUDPPort,
                                                            out reason);
            }
#endif

            MainConsole.Instance.DebugFormat("[LOCAL SIMULATION CONNECTOR]: Did not find region {0} for CreateAgent",
                              destination.RegionName);
            OSDMap map = new OSDMap();
            map["Reason"] = "Did not find region " + destination.RegionName;
            map["Success"] = false;
            reason = OSDParser.SerializeJsonString(map);
            return false;
        }

        public bool UpdateAgent(GridRegion destination, AgentData cAgentData)
        {
            if (destination == null || m_sceneList.Count == 0 || cAgentData == null)
                return false;

            bool retVal = false;
            IEntityTransferModule transferModule = m_sceneList[0].RequestModuleInterface<IEntityTransferModule>();
            if (transferModule != null)
            {
#if (!ISWIN)
                foreach (IScene s in m_sceneList)
                {
                    if (destination.RegionID == s.RegionInfo.RegionID)
                    {
                        retVal = transferModule.IncomingChildAgentDataUpdate(s, cAgentData);
                    }
                }
#else
                foreach (IScene s in m_sceneList.Where(s => destination.RegionID == s.RegionInfo.RegionID))
                {
                    retVal = transferModule.IncomingChildAgentDataUpdate(s, cAgentData);
                }
#endif
            }

            //            MainConsole.Instance.DebugFormat("[LOCAL COMMS]: Did not find region {0} for ChildAgentUpdate", regionHandle);
            return retVal;
        }

        public bool UpdateAgent(GridRegion destination, AgentPosition cAgentData)
        {
            if (destination == null)
                return false;

            bool retVal = false;
            foreach (IScene s in m_sceneList)
            {
                //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to send ChildAgentUpdate");
                IEntityTransferModule transferModule = s.RequestModuleInterface<IEntityTransferModule>();
                if (transferModule != null)
                    if (retVal)
                        transferModule.IncomingChildAgentDataUpdate(s, cAgentData);
                    else
                        retVal = transferModule.IncomingChildAgentDataUpdate(s, cAgentData);
            }
            //MainConsole.Instance.Debug("[LOCAL COMMS]: region not found for ChildAgentUpdate");
            return retVal;
        }

        public bool FailedToMoveAgentIntoNewRegion(UUID AgentID, UUID RegionID)
        {
#if (!ISWIN)
            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionID == RegionID)
                {
                    IScenePresence sp = s.GetScenePresence(AgentID);
                    if (sp != null)
                    {
                        sp.AgentFailedToLeave();
                    }
                }
            }
#else
            foreach (IScenePresence sp in m_sceneList.Where(s => s.RegionInfo.RegionID == RegionID).Select(s => s.GetScenePresence(AgentID)).Where(sp => sp != null))
            {
                sp.AgentFailedToLeave();
            }
#endif
            return false;
        }

        public bool MakeChildAgent(UUID AgentID, GridRegion destination)
        {
            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionID != destination.RegionID) continue;
                //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to send ChildAgentUpdate");
                IEntityTransferModule transferModule = s.RequestModuleInterface<IEntityTransferModule>();
                if (transferModule == null) continue;
                transferModule.MakeChildAgent(s.GetScenePresence(AgentID), new GridRegion(s.RegionInfo), true);
                return true;
            }
            return false;
        }

        public bool RetrieveAgent(GridRegion destination, UUID id, bool agentIsLeaving, out AgentData agent,
                                  out AgentCircuitData circuitData)
        {
            agent = null;
            circuitData = null;

            if (destination == null)
                return false;

            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionID == destination.RegionID)
                {
                    //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to send ChildAgentUpdate");
                    IEntityTransferModule transferModule = s.RequestModuleInterface<IEntityTransferModule>();
                    if (transferModule != null)
                        return transferModule.IncomingRetrieveRootAgent(s, id, agentIsLeaving, out agent,
                                                                        out circuitData);
                }
            }
            return false;
            //MainConsole.Instance.Debug("[LOCAL COMMS]: region not found for ChildAgentUpdate");
        }

        public bool CloseAgent(GridRegion destination, UUID id)
        {
            if (destination == null)
                return false;

            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionID == destination.RegionID)
                {
                    //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to SendCloseAgent");
                    IEntityTransferModule transferModule = s.RequestModuleInterface<IEntityTransferModule>();
                    if (transferModule != null)
                        return transferModule.IncomingCloseAgent(s, id);
                }
            }
            MainConsole.Instance.Debug("[LOCAL SIMULATION COMMS]: region not found in CloseAgent");
            return false;
        }

        /**
         * Object-related communications
         */

        public bool CreateObject(GridRegion destination, ISceneObject sog)
        {
            if (destination == null)
                return false;

            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionHandle != destination.RegionHandle) continue;
                //MainConsole.Instance.Debug("[LOCAL COMMS]: Found region to SendCreateObject");
                IEntityTransferModule AgentTransferModule = s.RequestModuleInterface<IEntityTransferModule>();
                if (AgentTransferModule != null)
                {
                    return AgentTransferModule.IncomingCreateObject(s.RegionInfo.RegionID, sog);
                }
            }
            MainConsole.Instance.Warn("[LOCAL SIMULATION COMMS]: region not found in CreateObject");
            return false;
        }

        public bool CreateObject(GridRegion destination, UUID userID, UUID itemID)
        {
            if (destination == null)
                return false;

            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionHandle == destination.RegionHandle)
                {
                    IEntityTransferModule AgentTransferModule = s.RequestModuleInterface<IEntityTransferModule>();
                    if (AgentTransferModule != null)
                    {
                        return AgentTransferModule.IncomingCreateObject(s.RegionInfo.RegionID, userID, itemID);
                    }
                }
            }
            MainConsole.Instance.Warn("[LOCAL SIMULATION COMMS]: region not found in CreateObject");
            return false;
        }

        #endregion /* ISimulationService */

        #region Misc

        public bool IsLocalRegion(ulong regionhandle)
        {
#if (!ISWIN)
            foreach (IScene s in m_sceneList)
                if (s.RegionInfo.RegionHandle == regionhandle)
                    return true;
            return false;
#else
            if (m_sceneList.Any(s => s.RegionInfo.RegionHandle == regionhandle))
            {
                return true;
            }
            return false;
#endif
        }

        public bool IsLocalRegion(UUID id)
        {
#if (!ISWIN)
            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionID == id) return true;
            }
            return false;
#else
            return m_sceneList.Any(s => s.RegionInfo.RegionID == id);
#endif
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlers = config.Configs["Handlers"];
            if (handlers.GetString("SimulationHandler", "") == Name)
                registry.RegisterModuleInterface<ISimulationService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            ISceneManager man = registry.RequestModuleInterface<ISceneManager>();
            if (man != null)
            {
                man.OnAddedScene += Init;
                man.OnCloseScene += RemoveScene;
            }
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}