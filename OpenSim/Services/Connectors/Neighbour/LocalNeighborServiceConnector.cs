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
                        //Fix this regions neighbors now that it has a new one
                        m_KnownNeighbors[s.RegionInfo.RegionID] = FindNewNeighbors(s.RegionInfo);

                        m_log.InfoFormat("[NeighborConnector]: HelloNeighbor from {0} to {1}.",
                            incomingRegion.RegionName, n.RegionName);

                        //Tell this region about the original region
                        s.IncomingHelloNeighbor(incomingRegion);
                        //Tell the original region about this new region
                        incomingRegion.TriggerRegionUp(n);
                        //This region knows now, so add it to the list
                        m_informedRegions.Add(n);
                    }
                }
            }
            int RegionsNotInformed = m_KnownNeighbors[incomingRegion.RegionID].Count - m_informedRegions.Count;
            if (RegionsNotInformed > 0) //If we arn't enabled, we are being called from the remote service, so we don't spam this
            {
                m_log.Warn("[NeighborsService]: Failed to inform " + RegionsNotInformed + " neighbors locally about a new neighbor.");
            }

            return m_informedRegions;
        }

        /// <summary>
        /// Get all the neighboring regions of the given region
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
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
                Border[] northBorders = region.NorthBorders.ToArray();
                Border[] southBorders = region.SouthBorders.ToArray();
                Border[] eastBorders = region.EastBorders.ToArray();
                Border[] westBorders = region.WestBorders.ToArray();

                // Legacy one region.  Provided for simplicity while testing the all inclusive method in the else statement.
                if (northBorders.Length > 1 && southBorders.Length > 1 && eastBorders.Length > 1 && westBorders.Length > 1)
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

                    List<GridRegion> Regions = m_gridService.GetRegionRange(region.ScopeID, startX, endX, startY, endY);
                    foreach (GridRegion gregion in Regions)
                    {
                        if (!neighbors.Contains(gregion))
                            neighbors.Add(gregion);
                    }
                }
            }
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

                        //Tell this region about the original region
                        s.IncomingClosingNeighbor(closingRegion);
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
            if (!m_KnownNeighbors.ContainsKey(currentRegionID))
                return;
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