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

using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Aurora.Simulation.Base;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Connectors.Simulation
{
    public class LocalSimulationServiceConnector : IService, ISimulationService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<IScene> m_sceneList = new List<IScene>();

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlers = config.Configs["Handlers"];
            if (handlers.GetString("SimulationHandler", "") == Name)
                registry.RegisterModuleInterface<ISimulationService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            SceneManager man = registry.RequestModuleInterface<SceneManager>() ;
            if (man != null)
            {
                man.OnAddedScene += Init;
                man.OnCloseScene += RemoveScene;
            }
        }

        public void FinishedStartup()
        {
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        #endregion

        #region ISimulation

        public IScene GetScene(ulong regionhandle)
        {
            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionHandle == regionhandle)
                    return s;
            }
            // ? weird. should not happen
            return null;
        }

        public ISimulationService GetInnerService()
        {
            return this;
        }

        /// <summary>
        /// Can be called from other modules.
        /// </summary>
        /// <param name="scene"></param>
        public void RemoveScene(IScene scene)
        {
            lock (m_sceneList)
            {
                m_sceneList.Remove(scene);
            }
        }

        /// <summary>
        /// Can be called from other modules.
        /// </summary>
        /// <param name="scene"></param>
        public void Init(IScene scene)
        {
            lock (m_sceneList)
            {
                m_sceneList.Add (scene);
            }
        }

        /**
         * Agent-related communications
         */

        public bool CreateAgent(GridRegion destination, ref AgentCircuitData aCircuit, uint teleportFlags, AgentData data, out int requestedUDPPort, out string reason)
        {
            requestedUDPPort = 0;
            if(destination.ExternalEndPoint != null)    
                requestedUDPPort = destination.ExternalEndPoint.Port;
            if (destination == null)
            {
                reason = "Given destination was null";
                m_log.DebugFormat("[LOCAL SIMULATION CONNECTOR]: CreateAgent was given a null destination");
                return false;
            }

            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionID == destination.RegionID)
                {
                    //m_log.DebugFormat("[LOCAL SIMULATION CONNECTOR]: Found region {0} to send SendCreateChildAgent", destination.RegionName);
                    if (data != null)
                        UpdateAgent(destination, data);
                    IEntityTransferModule transferModule = s.RequestModuleInterface<IEntityTransferModule> ();
                    if (transferModule != null)
                        return transferModule.NewUserConnection (s, aCircuit, teleportFlags, out requestedUDPPort, out reason);
                }
            }

            m_log.DebugFormat("[LOCAL SIMULATION CONNECTOR]: Did not find region {0} for CreateAgent", destination.RegionName);
            OSDMap map = new OSDMap();
            map["Reason"] = "Did not find region " + destination.RegionName;
            map["Success"] = false;
            reason = OSDParser.SerializeJsonString(map);
            return false;
        }

        public bool UpdateAgent(GridRegion destination, AgentData cAgentData)
        {
            if (destination == null)
                return false;

            bool retVal = false;
            foreach (IScene s in m_sceneList)
            {
                IEntityTransferModule transferModule = s.RequestModuleInterface<IEntityTransferModule> ();
                if(transferModule != null)
                {
                    if(destination.RegionID == s.RegionInfo.RegionID)
                        retVal = transferModule.IncomingChildAgentDataUpdate(s, cAgentData);
                }
            }

            //            m_log.DebugFormat("[LOCAL COMMS]: Did not find region {0} for ChildAgentUpdate", regionHandle);
            return retVal;
        }

        public bool UpdateAgent(GridRegion destination, AgentPosition cAgentData)
        {
            if (destination == null)
                return false;

            bool retVal = false;
            foreach (IScene s in m_sceneList)
            {
                //m_log.Debug("[LOCAL COMMS]: Found region to send ChildAgentUpdate");
                IEntityTransferModule transferModule = s.RequestModuleInterface<IEntityTransferModule> ();
                if (transferModule != null)
                    if (retVal)
                        transferModule.IncomingChildAgentDataUpdate (s, cAgentData);
                    else
                        retVal = transferModule.IncomingChildAgentDataUpdate (s, cAgentData);
            }
            //m_log.Debug("[LOCAL COMMS]: region not found for ChildAgentUpdate");
            return retVal;
        }

        public bool RetrieveAgent(GridRegion destination, UUID id, out IAgentData agent)
        {
            agent = null;

            if (destination == null)
                return false;

            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionID == destination.RegionID)
                {
                    //m_log.Debug("[LOCAL COMMS]: Found region to send ChildAgentUpdate");
                    IEntityTransferModule transferModule = s.RequestModuleInterface<IEntityTransferModule> ();
                    if (transferModule != null)
                        return transferModule.IncomingRetrieveRootAgent (s, id, out agent);
                }
            }
            //m_log.Debug("[LOCAL COMMS]: region not found for ChildAgentUpdate");
            return false;
        }

        public bool CloseAgent(GridRegion destination, UUID id)
        {
            if (destination == null)
                return false;

            foreach (IScene s in m_sceneList)
            {
                if (s.RegionInfo.RegionID == destination.RegionID)
                {
                    //m_log.Debug("[LOCAL COMMS]: Found region to SendCloseAgent");
                    IEntityTransferModule transferModule = s.RequestModuleInterface<IEntityTransferModule> ();
                    if (transferModule != null)
                        return transferModule.IncomingCloseAgent (s, id);
                }
            }
            m_log.Debug("[LOCAL SIMULATION COMMS]: region not found in CloseAgent");
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
                if (s.RegionInfo.RegionHandle == destination.RegionHandle)
                {
                    //m_log.Debug("[LOCAL COMMS]: Found region to SendCreateObject");
                    IEntityTransferModule AgentTransferModule = s.RequestModuleInterface<IEntityTransferModule>();
                    if (AgentTransferModule != null)
                    {
                        return AgentTransferModule.IncomingCreateObject(s.RegionInfo.RegionID, sog);
                    }
                }
            }
            m_log.Warn("[LOCAL SIMULATION COMMS]: region not found in CreateObject");
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
            m_log.Warn("[LOCAL SIMULATION COMMS]: region not found in CreateObject");
            return false;
        }

        #endregion /* ISimulationService */

        #region Misc

        public bool IsLocalRegion(ulong regionhandle)
        {
            foreach (IScene s in m_sceneList)
                if (s.RegionInfo.RegionHandle == regionhandle)
                    return true;
            return false;
        }

        public bool IsLocalRegion(UUID id)
        {
            foreach (IScene s in m_sceneList)
                if (s.RegionInfo.RegionID == id)
                    return true;
            return false;
        }

        #endregion
    }
}
