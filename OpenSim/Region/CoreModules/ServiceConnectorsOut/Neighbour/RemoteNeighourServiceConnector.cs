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
using System;
using System.Collections.Generic;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Services.Connectors;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;
using OpenSim.Server.Handlers.Base;
using OpenMetaverse;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Region.CoreModules.ServiceConnectorsOut.Neighbour
{
    public class RemoteNeighbourServicesConnector :
            NeighbourServicesConnector, ISharedRegionModule, INeighbourService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Registered = false;
        private bool m_Enabled = false;
        private IConfigSource m_config = null;
        private LocalNeighbourServicesConnector m_LocalService;
        private string serviceDll = "OpenSim.Server.Handlers.dll:NeighbourServiceInConnector";
        //private List<Scene> m_Scenes = new List<Scene>();

        public Type ReplaceableInterface 
        {
            get { return null; }
        }

        public string Name
        {
            get { return "RemoteNeighbourServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
            m_config = source;
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("NeighbourServices");
                if (name == Name)
                {
                    m_Enabled = true;
                    m_LocalService = new LocalNeighbourServicesConnector();
                    IConfig neighbourConfig = source.Configs["NeighbourService"];
                    if (neighbourConfig == null)
                    {
                        m_log.Error("[NEIGHBOUR CONNECTOR]: NeighbourService missing from OpenSim.ini");
                        return;
                    }
                    serviceDll = neighbourConfig.GetString("RemoteServiceModule", serviceDll);
                    if (serviceDll == String.Empty)
                    {
                        m_log.Error("[NEIGHBOUR CONNECTOR]: No LocalServiceModule named in section NeighbourService");
                        return;
                    }

                    //m_log.Info("[NEIGHBOUR CONNECTOR]: Remote Neighbour connector enabled");
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
            if (!m_Enabled)
                return;

            //Add the local region for this
            m_LocalService.AddRegion(scene);
            //Set the grid service for the local regions
            m_LocalService.SetServices(scene.GridService, scene.SimulationService);
            scene.RegisterModuleInterface<INeighbourService>(this);

            //Add the incoming remote neighbor handlers
            if (!m_Registered)
            {
                m_Registered = true;
                Object[] args = new Object[] { m_config, MainServer.Instance, m_LocalService, scene };
                ServerUtils.LoadPlugin<IServiceConnector>(serviceDll, args);
            }
        }

        public void RemoveRegion(Scene scene)
        {
            if (m_Enabled)
                m_LocalService.RemoveRegion(scene);
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;

            m_GridService = scene.GridService;

            //m_log.InfoFormat("[NEIGHBOUR CONNECTOR]: Enabled remote neighbours for region {0}", scene.RegionInfo.RegionName);

        }

        #region INeighbourService

        public override List<GridRegion> InformNeighborsThatRegionIsUp(RegionInfo incomingRegion)
        {
            List<GridRegion> nowInformedRegions = m_LocalService.InformNeighborsThatRegionIsUp(incomingRegion);
            
            //Get the known regions from the local connector, as it queried the grid service to find them all
            m_KnownNeighbors = m_LocalService.Neighbors;

            int RegionsNotInformed = m_KnownNeighbors[incomingRegion.RegionID].Count - nowInformedRegions.Count;
            
            //We informed all of them locally, so quit early
            if (RegionsNotInformed == 0)
                return nowInformedRegions;

            //Now add the remote ones and tell it which ones have already been informed locally so that it doesn't inform them twice
            nowInformedRegions.AddRange(base.InformNeighborsRegionIsUp(incomingRegion, nowInformedRegions));
            
            //Now check to see if we informed everyone
            RegionsNotInformed = m_KnownNeighbors[incomingRegion.RegionID].Count - nowInformedRegions.Count;
            if (RegionsNotInformed != 0)
            {
                m_log.Warn("[NeighborsService]: Failed to inform " + RegionsNotInformed + " neighbors remotely about a new neighbor.");
            }
            return nowInformedRegions;
        }

        public override List<GridRegion> InformNeighborsThatRegionIsDown(RegionInfo closingRegion)
        {
            List<GridRegion> neighbors = m_KnownNeighbors[closingRegion.RegionID];
            List<GridRegion> nowInformedRegions = m_LocalService.InformNeighborsThatRegionIsDown(closingRegion);

            int RegionsNotInformed = neighbors.Count - nowInformedRegions.Count;

            //We informed all of them locally, so quit early
            if (RegionsNotInformed == 0)
                return nowInformedRegions;

            //Now add the remote ones and tell it which ones have already been informed locally so that it doesn't inform them twice
            nowInformedRegions.AddRange(base.InformNeighborsRegionIsDown(closingRegion, nowInformedRegions, neighbors));

            //Now check to see if we informed everyone
            RegionsNotInformed = neighbors.Count - nowInformedRegions.Count;
            if (RegionsNotInformed != 0)
            {
                m_log.Warn("[NeighborsService]: Failed to inform " + RegionsNotInformed + " neighbors remotely about a closing neighbor.");
            }
            return nowInformedRegions;
        }

        public override void SendChildAgentUpdate(AgentPosition childAgentUpdate, UUID regionID)
        {
            m_LocalService.SendChildAgentUpdate(childAgentUpdate, regionID);
        }

        public override void SendCloseChildAgent(UUID agentID, UUID regionID, List<ulong> regionsToClose)
        {
            m_LocalService.SendCloseChildAgent(agentID, regionID, regionsToClose);
        }

        #endregion
    }
}
