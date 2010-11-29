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

using log4net;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Reflection;
using OpenSim.Framework;
using OpenSim.Server.Base;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Region.CoreModules.ServiceConnectorsOut.Neighbour
{
    public class LocalNeighbourServicesConnector :
            ISharedRegionModule, INeighbourService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private List<Scene> m_Scenes = new List<Scene>();
        private IGridService m_gridService = null;
        private ISimulationService m_simService = null;
        private Dictionary<UUID, List<GridRegion>> m_KnownNeighbors = new Dictionary<UUID, List<GridRegion>>();

        public Dictionary<UUID, List<GridRegion>> Neighbors
        {
            get { return m_KnownNeighbors; }
        }

        private bool m_Enabled = false;

        #region ISharedRegionModule

        public Type ReplaceableInterface 
        {
            get { return null; }
        }

        public string Name
        {
            get { return "LocalNeighbourServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("NeighbourServices", this.Name);
                if (name == Name)
                {
                    // m_Enabled rules whether this module registers as INeighbourService or not
                    m_Enabled = true;
                    //m_log.Info("[NEIGHBOUR CONNECTOR]: Local neighbour connector enabled");
                }
            }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            //Keep this here so that we register the region no matter what as the remote service needs this
            m_Scenes.Add(scene);

            if (!m_Enabled)
                return;

            scene.RegisterModuleInterface<INeighbourService>(this);
            if (m_gridService == null)
                m_gridService = scene.GridService;
            if (m_simService == null)
                m_simService = scene.SimulationService;
        }

        public void RegionLoaded(Scene scene)
        {
            //m_log.Info("[NEIGHBOUR CONNECTOR]: Local neighbour connector enabled for region " + scene.RegionInfo.RegionName);
        }

        public void RemoveRegion(Scene scene)
        {
            // Always remove as the remote service uses this
            if (m_Scenes.Contains(scene))
                m_Scenes.Remove(scene);
        }

        #endregion ISharedRegionModule

        public void SetServices(IGridService gridService, ISimulationService simService)
        {
            if (m_gridService == null)
                m_gridService = gridService;
            if (m_simService == null)
                m_simService = simService;
        }

        #region INeighbourService

        public List<GridRegion> InformNeighborsThatRegionisUp(RegionInfo incomingRegion)
        {
            List<GridRegion> m_informedRegions = new List<GridRegion>();
            m_KnownNeighbors[incomingRegion.RegionID] = m_gridService.GetNeighbours(incomingRegion.ScopeID, incomingRegion.RegionID);

            //We need to inform all the regions around us that our region now exists
            
            foreach (Scene s in m_Scenes)
            {
                //Don't tell ourselves about us
                if (s.RegionInfo.RegionID == incomingRegion.RegionID)
                    continue;

                foreach (GridRegion n in m_KnownNeighbors[incomingRegion.RegionID])
                {
                    if (n.RegionID == s.RegionInfo.RegionID)
                    {
                        m_log.DebugFormat("[NeighborConnector]: HelloNeighbour from {0} to {1}.",
                            incomingRegion.RegionName, n.RegionName);

                        //Tell this region about the original region
                        s.IncomingHelloNeighbour(incomingRegion);
                        //Tell the original region about this new region
                        incomingRegion.TriggerRegionUp(n);
                        //This region knows now, so add it to the list
                        m_informedRegions.Add(n);
                    }
                }
            }
            int RegionsNotInformed = m_KnownNeighbors[incomingRegion.RegionID].Count - m_informedRegions.Count;
            if (RegionsNotInformed != 0 && m_Enabled) //If we arn't enabled, we are being called from the remote service, so we don't spam this
            {
                m_log.Warn("[NeighborsService]: Failed to inform " + RegionsNotInformed + " neighbors locally."); 
            }

            return m_informedRegions;
        }

        public void SendChildAgentUpdate(AgentPosition childAgentUpdate, UUID regionID)
        {
            //Send the updates to all known neighbors
            foreach (GridRegion region in m_KnownNeighbors[regionID])
            {
                m_simService.UpdateAgent(region, childAgentUpdate);
            }
        }

        public void SendCloseChildAgent(UUID agentID, UUID regionID, List<ulong> regionsToClose)
        {
            foreach (GridRegion region in m_KnownNeighbors[regionID])
            {
                //If it is one of the ones that needs closing, close it
                if(regionsToClose.Contains(region.RegionHandle))
                    m_simService.CloseAgent(region, agentID);
            }
        }

        #endregion
    }
}
