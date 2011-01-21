using log4net;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Reflection;
using OpenSim.Framework;
using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.Connectors
{
    public class LocalNeighborServiceConnector : IService, INeighborService
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

        public INeighborService InnerService
        {
            get { return this; }
        }

        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            ReadConfig(config);
            IConfig handlers = config.Configs["Handlers"];
            if (handlers.GetString("NeighborHandler", "") == Name)
                registry.RegisterModuleInterface<INeighborService>(this);
        }

        public void ReadConfig(IConfigSource config)
        {
            IConfig neighborService = config.Configs["NeighborService"];
            if (neighborService != null)
            {
                RegionViewSize = neighborService.GetInt("RegionSightSize", RegionViewSize);
                RegionViewSize *= Constants.RegionSize;
                //This option is the opposite of the config to make it easier on the user
                CloseLocalRegions = !neighborService.GetBoolean("SeeIntoAllLocalRegions", CloseLocalRegions);
            }
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostStart(IConfigSource config, IRegistryCore registry)
        {
            m_gridService = registry.RequestModuleInterface<IGridService>();
            m_simService = registry.RequestModuleInterface<ISimulationService>();
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlers = config.Configs["Handlers"];
            if (handlers.GetString("NeighborHandler", "") == Name)
                registry.RegisterModuleInterface<INeighborService>(this);
        }

        #endregion

        #region Region add/remove

        /// <summary>
        /// Can be called from other modules.
        /// </summary>
        /// <param name="scene"></param>
        public void RemoveScene(IScene sscene)
        {
            Scene scene = (Scene)sscene;
            lock (m_Scenes)
            {
                if (m_Scenes.Contains(scene))
                {
                    m_Scenes.Remove(scene);
                }
            }
        }

        /// <summary>
        /// Can be called from other modules.
        /// </summary>
        /// <param name="scene"></param>
        public void Init(IScene sscene)
        {
            Scene scene = (Scene)sscene;
            lock (m_Scenes)
            {
                if (!m_Scenes.Contains(scene))
                {
                    m_Scenes.Add(scene);
                }
            }
        }

        #endregion

        #region INeighborService

        public List<GridRegion> InformRegionsNeighborsThatRegionIsUp(RegionInfo incomingRegion)
        {
            GridRegion incomingGridRegion = new GridRegion(incomingRegion);

            List<GridRegion> m_informedRegions = new List<GridRegion>();
            m_KnownNeighbors[incomingRegion.RegionID] = FindNewNeighbors(incomingGridRegion);

            //We need to inform all the regions around us that our region now exists

            //First run through the scenes that we have here locally, 
            //   as we don't inform remote regions in this module
            foreach (Scene s in m_Scenes)
            {
                //Don't tell ourselves about us
                if (s.RegionInfo.RegionID == incomingRegion.RegionID)
                    continue;

                GridRegion thisSceneInfo = new GridRegion(s.RegionInfo);

                //Make sure we don't already have this region in the neighbors
                if (!m_informedRegions.Contains(thisSceneInfo))
                {
                    //Now check to see whether the incoming region should be a neighbor of this Scene
                    if (!IsOutsideView(s.RegionInfo.RegionLocX, incomingRegion.RegionLocX,
                        s.RegionInfo.RegionLocY, incomingRegion.RegionLocY))
                    {
                        //Fix this regions neighbors now that it has a new one
                        if(m_KnownNeighbors.ContainsKey(s.RegionInfo.RegionID))
                            m_KnownNeighbors[s.RegionInfo.RegionID].Add(incomingGridRegion);

                        m_log.InfoFormat("[NeighborConnector]: HelloNeighbor from {0} to {1}.",
                            incomingRegion.RegionName, s.RegionInfo.RegionName);

                        //Tell this region about the original region
                        IncomingHelloNeighbor(s, incomingGridRegion);
                        //This region knows now, so add it to the list
                        m_informedRegions.Add(thisSceneInfo);
                    }
                }
            }
            int RegionsNotInformed = m_KnownNeighbors[incomingRegion.RegionID].Count - m_informedRegions.Count;
            if (RegionsNotInformed != 0) //If we arn't enabled, we are being called from the remote service, so we don't spam this
            {
                m_log.Warn("[NeighborsService]: Failed to inform " + RegionsNotInformed + " neighbors locally about a new neighbor.");
            }

            return m_informedRegions;
        }

        public List<GridRegion> InformOurRegionsOfNewNeighbor(RegionInfo incomingRegion)
        {
            GridRegion incomingGridRegion = new GridRegion(incomingRegion);

            List<GridRegion> m_informedRegions = new List<GridRegion>();
            List<GridRegion> neighborsOfIncomingRegion = FindNewNeighbors(incomingGridRegion);

            //We need to inform all the regions around us that our region now exists

            //First run through the scenes that we have here locally, 
            //   as we don't inform remote regions in this module
            foreach (Scene s in m_Scenes)
            {
                //Don't tell ourselves about us
                if (s.RegionInfo.RegionID == incomingRegion.RegionID)
                    continue;

                GridRegion thisSceneInfo = new GridRegion(s.RegionInfo);

                if (neighborsOfIncomingRegion.Contains(thisSceneInfo))
                {
                    //Make sure we don't already have this region in the neighbors
                    if (!m_informedRegions.Contains(thisSceneInfo))
                    {
                        //Now check to see whether the incoming region should be a neighbor of this Scene
                        if (!IsOutsideView(s.RegionInfo.RegionLocX, incomingRegion.RegionLocX,
                            s.RegionInfo.RegionLocY, incomingRegion.RegionLocY))
                        {
                            //Fix this regions neighbors now that it has a new one
                            if (m_KnownNeighbors.ContainsKey(s.RegionInfo.RegionID))
                                m_KnownNeighbors[s.RegionInfo.RegionID].Add(incomingGridRegion);

                            m_log.InfoFormat("[NeighborConnector]: HelloNeighbor from {0} to {1}.",
                                incomingRegion.RegionName, s.RegionInfo.RegionName);

                            //Tell this region about the original region
                            IncomingHelloNeighbor(s, incomingGridRegion);
                            //This region knows now, so add it to the list
                            m_informedRegions.Add(thisSceneInfo);
                        }
                    }
                }
            }

            return m_informedRegions;
        }

        #region Inform scenes about incoming/outgoing neighbors

        /// <summary>
        /// The Scene is being informed of the new region 'otherRegion'
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="otherRegion"></param>
        public void IncomingHelloNeighbor(Scene scene, GridRegion otherRegion)
        {
            // Let the grid service module know, so this can be cached
            scene.EventManager.TriggerOnRegionUp(otherRegion);

            //Add this new region to all the clients so that they can see it as well
            scene.ForEachScenePresence(delegate(ScenePresence agent)
            {
                // If agent is a root agent.
                if (!agent.IsChildAgent)
                {
                    //Now add the agent to the reigon that is coming up
                    IEntityTransferModule transferModule = scene.RequestModuleInterface<IEntityTransferModule>();
                    if (transferModule != null)
                        transferModule.EnableChildAgent(agent, otherRegion);
                }
            });
        }

        /// <summary>
        /// The Scene is being informed of the closing region 'closingNeighbor'
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="closingNeighbor"></param>
        private void IncomingClosingNeighbor(Scene scene, GridRegion closingNeighbor)
        {
            scene.EventManager.TriggerOnRegionDown(closingNeighbor);
        }

        #endregion

        /// <summary>
        /// Get all the neighboring regions of the given region
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        private List<GridRegion> FindNewNeighbors(GridRegion region)
        {
            int startX = (int)(region.RegionLocX - RegionViewSize);
            int startY = (int)(region.RegionLocY - RegionViewSize);

            //-1 so that we don't get size (256) + viewsize (256) and get a region two 256 blocks over
            int endX = ((int)region.RegionLocX + RegionViewSize + (int)region.RegionSizeX - 1);
            int endY = ((int)region.RegionLocY + RegionViewSize + (int)region.RegionSizeY - 1);

            List<GridRegion> neighbors = m_gridService.GetRegionRange(region.ScopeID, startX, endX, startY, endY);
            //If we arn't supposed to close local regions, add all of the scene ones if they are not already there
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

        /// <summary>
        /// Get the cached list of neighbors
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public List<GridRegion> GetNeighbors(RegionInfo region)
        {
            List<GridRegion> neighbors = new List<GridRegion>();
            if (!m_KnownNeighbors.TryGetValue(region.RegionID, out neighbors))
            {
                neighbors = new List<GridRegion>();
            }
            return neighbors;
        }

        /// <summary>
        /// Get the cached list of neighbors or a new list
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public List<GridRegion> GetNeighbors(GridRegion region)
        {
            List<GridRegion> neighbors = new List<GridRegion>();
            if (!m_KnownNeighbors.TryGetValue(region.RegionID, out neighbors))
            {
                neighbors = FindNewNeighbors(region);
                m_KnownNeighbors[region.RegionID] = neighbors;
            }
            return neighbors;
        }

        /// <summary>
        /// Tell the neighbors that this region is going down
        /// </summary>
        /// <param name="closingRegion"></param>
        /// <returns></returns>
        public List<GridRegion> InformNeighborsThatRegionIsDown(RegionInfo closingRegion)
        {
            List<GridRegion> m_informedRegions = new List<GridRegion>();

            if (!m_KnownNeighbors.ContainsKey(closingRegion.RegionID))
                return new List<GridRegion>();

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

                        GridRegion closingNeighbor = new GridRegion(closingRegion);
                        //Tell this region about the original region
                        IncomingClosingNeighbor(s, closingNeighbor);
                        //This region knows now, so add it to the list
                        m_informedRegions.Add(n);
                    }
                }
            }
            int RegionsNotInformed = neighbors.Count - m_informedRegions.Count;
            if (RegionsNotInformed > 0) //If we arn't enabled, we are being called from the remote service, so we don't spam this
            {
                m_log.Warn("[NeighborsService]: Failed to inform " + RegionsNotInformed + " neighbors locally about a closing neighbor.");
            }

            return m_informedRegions;
        }

        public void SendChildAgentUpdate(AgentPosition childAgentUpdate, UUID regionID)
        {
            if (!m_KnownNeighbors.ContainsKey(regionID))
                return;

            Util.FireAndForget(delegate(object o)
            {
                //Send the updates to all known neighbors
                foreach (GridRegion region in m_KnownNeighbors[regionID])
                {
                    m_simService.UpdateAgent(region, childAgentUpdate);
                }
            });
        }

        public void CloseAllNeighborAgents(UUID AgentID, UUID currentRegionID)
        {
            if (!m_KnownNeighbors.ContainsKey(currentRegionID))
                return;
            List<GridRegion> NeighborsOfCurrentRegion = m_KnownNeighbors[currentRegionID];
            m_log.DebugFormat(
                "[NeighborService]: Closing all child agents for " + AgentID + ". Checking {0} regions.",
                NeighborsOfCurrentRegion.Count);
            Util.FireAndForget(delegate(object o)
            {
                SendCloseChildAgent(AgentID, currentRegionID, NeighborsOfCurrentRegion);
            });
        }

        /// <summary>
        /// Check if the new position is outside of the range for the old position
        /// </summary>
        /// <param name="x">old X pos (in meters)</param>
        /// <param name="newRegionX">new X pos (in meters)</param>
        /// <param name="y">old Y pos (in meters)</param>
        /// <param name="newRegionY">new Y pos (in meters)</param>
        /// <returns></returns>
        public bool IsOutsideView(int oldRegionX, int newRegionX, int oldRegionY, int newRegionY)
        {
            Scene scene = FindSceneByPosition(newRegionX, newRegionY);
            //Check whether it is a local region
            if (!CloseLocalRegions && scene != null)
                return false;

            return ((Math.Abs(oldRegionX - newRegionX) > RegionViewSize) || (Math.Abs(oldRegionY - newRegionY) > RegionViewSize));
        }

        public void CloseNeighborAgents(int newRegionX, int newRegionY, UUID AgentID, UUID currentRegionID)
        {
            if (!m_KnownNeighbors.ContainsKey(currentRegionID))
                return;
            List<GridRegion> NeighborsOfCurrentRegion = m_KnownNeighbors[currentRegionID];
            List<GridRegion> byebyeRegions = new List<GridRegion>();
            m_log.DebugFormat(
                "[NeighborService]: Closing child agents. Checking {0} regions in {1}",
                NeighborsOfCurrentRegion.Count, FindSceneByUUID(currentRegionID).RegionInfo.RegionName);

            foreach (GridRegion region in NeighborsOfCurrentRegion)
            {
                if (IsOutsideView(region.RegionLocX, newRegionX, region.RegionLocY, newRegionY))
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

            if (!m_KnownNeighbors.ContainsKey(region.RegionID))
                return RetVal;

            foreach (GridRegion neighbor in m_KnownNeighbors[region.RegionID])
            {
                if (neighbor.RegionID == region.RegionID)
                    continue;
                Scene scene = FindSceneByUUID(region.RegionID);
                if (scene != null)
                {
                    IChatModule chatModule = scene.RequestModuleInterface<IChatModule>();
                    if (chatModule != null && !RetVal)
                    {
                        chatModule.DeliverChatToAvatars(type, message);
                        RetVal = true;
                    }
                }
            }
            return RetVal;
        }

        public List<GridRegion> SendChatMessageToNeighbors(OSChatMessage message, ChatSourceType type, RegionInfo region, out bool RetVal)
        {
            RetVal = false;
            List<GridRegion> regionsNotified = new List<GridRegion>();

            if (!m_KnownNeighbors.ContainsKey(region.RegionID))
                return regionsNotified;

            foreach (GridRegion neighbor in m_KnownNeighbors[region.RegionID])
            {
                if (neighbor.RegionID == region.RegionID)
                    continue;
                Scene scene = FindSceneByUUID(neighbor.RegionID);
                if (scene != null)
                {
                    Aurora.Framework.IChatModule chatModule = scene.RequestModuleInterface<Aurora.Framework.IChatModule>();
                    if (chatModule != null && !RetVal)
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

        private Scene FindSceneByPosition(int x, int y)
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