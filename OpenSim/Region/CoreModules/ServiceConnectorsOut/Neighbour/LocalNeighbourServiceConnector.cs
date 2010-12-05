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
using Aurora.Simulation.Base;
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
        private bool CloseLocalRegions = true;
        private int RegionViewSize = 1;

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
            IConfig neighborService = source.Configs["NeighborService"];
            if (neighborService != null)
            {
                RegionViewSize = neighborService.GetInt("RegionSightSize", RegionViewSize);
                //This option is the opposite of the config to make it easier on the user
                CloseLocalRegions = !neighborService.GetBoolean("SeeIntoAllLocalRegions", CloseLocalRegions);
            }
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
        }

        public void RegionLoaded(Scene scene)
        {
            if (m_gridService == null)
                m_gridService = scene.GridService;
            if (m_simService == null)
                m_simService = scene.SimulationService;
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

        public List<GridRegion> InformNeighborsThatRegionIsUp(RegionInfo incomingRegion)
        {
            List<GridRegion> m_informedRegions = new List<GridRegion>();
            m_KnownNeighbors[incomingRegion.RegionID] = FindNewNeighbors(incomingRegion);

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
                m_log.Warn("[NeighborsService]: Failed to inform " + RegionsNotInformed + " neighbors locally about a new neighbor."); 
            }

            return m_informedRegions;
        }

        private List<GridRegion> FindNewNeighbors(RegionInfo region)
        {
            List<GridRegion> neighbors = new List<GridRegion>();
            if (RegionViewSize == 1) //Legacy support
            {
                Border[] northBorders = region.NorthBorders.ToArray();
                Border[] southBorders = region.SouthBorders.ToArray();
                Border[] eastBorders = region.EastBorders.ToArray();
                Border[] westBorders = region.WestBorders.ToArray();

                // Legacy one region.  Provided for simplicity while testing the all inclusive method in the else statement.
                if (northBorders.Length <= 1 && southBorders.Length <= 1 && eastBorders.Length <= 1 && westBorders.Length <= 1)
                {
                    neighbors = m_gridService.GetNeighbours(region.ScopeID, region.RegionID);
                }
                else
                {
                    //Check for larger mega-regions
                    Vector2 extent = Vector2.Zero;
                    for (int i = 0; i < eastBorders.Length; i++)
                    {
                        extent.X = (eastBorders[i].BorderLine.Z > extent.X) ? eastBorders[i].BorderLine.Z : extent.X;
                    }
                    for (int i = 0; i < northBorders.Length; i++)
                    {
                        extent.Y = (northBorders[i].BorderLine.Z > extent.Y) ? northBorders[i].BorderLine.Z : extent.Y;
                    }

                    // Loss of fraction on purpose
                    extent.X = ((int)extent.X / (int)Constants.RegionSize) + 1;
                    extent.Y = ((int)extent.Y / (int)Constants.RegionSize) + 1;

                    int startX = (int)(region.RegionLocX - 1) * (int)Constants.RegionSize;
                    int startY = (int)(region.RegionLocY - 1) * (int)Constants.RegionSize;

                    int endX = ((int)region.RegionLocX + (int)extent.X) * (int)Constants.RegionSize;
                    int endY = ((int)region.RegionLocY + (int)extent.Y) * (int)Constants.RegionSize;

                    neighbors = m_gridService.GetRegionRange(region.ScopeID, startX, endX, startY, endY);
                }
            }
            else
            {
                //Get the range of regions defined by RegionViewSize
                neighbors = m_gridService.GetRegionRange(region.ScopeID, (int)(region.RegionLocX - RegionViewSize) * (int)Constants.RegionSize, (int)(region.RegionLocX + RegionViewSize) * (int)Constants.RegionSize, (int)(region.RegionLocY - RegionViewSize) * (int)Constants.RegionSize, (int)(region.RegionLocY + RegionViewSize) * (int)Constants.RegionSize);
            }
            if (!CloseLocalRegions)
            {
                foreach (Scene scene in m_Scenes)
                {
                    GridRegion gregion = m_gridService.GetRegionByUUID(scene.RegionInfo.ScopeID, scene.RegionInfo.RegionID);
                    if (!neighbors.Contains(gregion))
                        neighbors.Add(gregion);
                }
            }
            neighbors.RemoveAll(delegate(GridRegion r) { return r.RegionID == region.RegionID; });
            return neighbors;
        }

        public List<GridRegion> GetNeighbors(RegionInfo region)
        {
            List<GridRegion> neighbors = new List<GridRegion>();
            if (!m_KnownNeighbors.TryGetValue(region.RegionID, out neighbors))
            {
                neighbors = new List<GridRegion>();
            }
            return neighbors;
        }

        public List<GridRegion> InformNeighborsThatRegionIsDown(RegionInfo closingRegion)
        {
            List<GridRegion> m_informedRegions = new List<GridRegion>();
            List<GridRegion> neighbors = m_KnownNeighbors[closingRegion.RegionID];
            m_KnownNeighbors.Remove(closingRegion.RegionID);

            //We need to inform all the regions around us that our region now exists

            foreach (Scene s in m_Scenes)
            {
                //Don't tell ourselves about us
                if (s.RegionInfo.RegionID == closingRegion.RegionID)
                    continue;

                foreach (GridRegion n in neighbors)
                {
                    if (n.RegionID == s.RegionInfo.RegionID)
                    {
                        m_log.DebugFormat("[NeighborConnector]: Neighbor is closing from {0} to {1}.",
                            closingRegion.RegionName, n.RegionName);

                        //Tell this region about the original region
                        s.IncomingClosingNeighbour(closingRegion);
                        //This region knows now, so add it to the list
                        m_informedRegions.Add(n);
                    }
                }
            }
            int RegionsNotInformed = neighbors.Count - m_informedRegions.Count;
            if (RegionsNotInformed != 0 && m_Enabled) //If we arn't enabled, we are being called from the remote service, so we don't spam this
            {
                m_log.Warn("[NeighborsService]: Failed to inform " + RegionsNotInformed + " neighbors locally about a closing neighbor.");
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

        public void CloseAllNeighborAgents(UUID AgentID, UUID currentRegionID)
        {
            List<GridRegion> NeighborsOfCurrentRegion = m_KnownNeighbors[currentRegionID];
            m_log.DebugFormat(
                "[NeighborService]: Closing all child agents for " + AgentID + ". Checking {0} regions.",
                NeighborsOfCurrentRegion.Count);
            SendCloseChildAgent(AgentID, currentRegionID, NeighborsOfCurrentRegion);
        }

        public bool IsOutsideView(uint x, uint newRegionX, uint y, uint newRegionY)
        {
            Scene scene = FindSceneByPosition(x, y);
            //Check whether it is a local region
            if (!CloseLocalRegions && scene != null)
                return false;

            return ((Math.Abs((int)x - (int)newRegionX) > RegionViewSize) || (Math.Abs((int)y - (int)newRegionY) > RegionViewSize));
        }

        public void CloseNeighborAgents(uint newRegionX, uint newRegionY, UUID AgentID, UUID currentRegionID)
        {
            List<GridRegion> NeighborsOfCurrentRegion = m_KnownNeighbors[currentRegionID];
            List<GridRegion> byebyeRegions = new List<GridRegion>();
            m_log.DebugFormat(
                "[NeighborService]: Closing child agents. Checking {0} regions in {1}",
                NeighborsOfCurrentRegion.Count, FindSceneByUUID(currentRegionID).RegionInfo.RegionName);
            
            foreach (GridRegion region in NeighborsOfCurrentRegion)
            {
                uint x, y;
                x = (uint)region.RegionLocX / (uint)Constants.RegionSize;
                y = (uint)region.RegionLocY / (uint)Constants.RegionSize;

                if (IsOutsideView(x, newRegionX, y, newRegionY))
                {
                    byebyeRegions.Add(region);
                }
            }

            if (byebyeRegions.Count > 0)
            {
                m_log.Debug("[NeighborService]: Closing " + byebyeRegions.Count + " child agents");
                SendCloseChildAgent(AgentID, currentRegionID, byebyeRegions);
            }
        }

        protected void SendCloseChildAgent(UUID agentID, UUID regionID, List<GridRegion> regionsToClose)
        {
            //Close all agents that we've been given regions for
            foreach (GridRegion region in regionsToClose)
            {
                m_simService.CloseAgent(region, agentID);
            }
        }

        public bool SendChatMessageToNeighbors(OSChatMessage message, ChatSourceType type, RegionInfo region)
        {
            bool RetVal = false;
            foreach (GridRegion neighbor in m_KnownNeighbors[region.RegionID])
            {
                if (neighbor.RegionID == region.RegionID)
                    continue;
                Scene scene = FindSceneByUUID(region.RegionID);
                Aurora.Framework.IChatModule chatModule = scene.RequestModuleInterface<Aurora.Framework.IChatModule>();
                if (chatModule != null)
                {
                    chatModule.DeliverChatToAvatars(type, message);
                    RetVal = true;
                }
            }
            return RetVal;
        }

        public List<GridRegion> SendChatMessageToNeighbors(OSChatMessage message, ChatSourceType type, RegionInfo region, out bool RetVal)
        {
            RetVal = false;
            List<GridRegion> regionsNotified = new List<GridRegion>();
            foreach (GridRegion neighbor in m_KnownNeighbors[region.RegionID])
            {
                if (neighbor.RegionID == region.RegionID)
                    continue;
                Scene scene = FindSceneByUUID(neighbor.RegionID);
                if (scene != null)
                {
                    Aurora.Framework.IChatModule chatModule = scene.RequestModuleInterface<Aurora.Framework.IChatModule>();
                    if (chatModule != null)
                    {
                        chatModule.DeliverChatToAvatars(type, message);
                        RetVal = true;
                    }
                    regionsNotified.Add(neighbor);
                }
            }
            return regionsNotified;
        }

        protected Scene FindSceneByUUID(UUID regionID)
        {
            foreach (Scene scene in m_Scenes)
            {
                if (scene.RegionInfo.RegionID == regionID)
                {
                    return scene;
                }
            }
            return null;
        }

        private Scene FindSceneByPosition(uint x, uint y)
        {
            foreach (Scene scene in m_Scenes)
            {
                if (scene.RegionInfo.RegionLocX == x &&
                    scene.RegionInfo.RegionLocY == y)
                {
                    return scene;
                }
            }
            return null;
        }

        #endregion
    }
}
