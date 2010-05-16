using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Services.Base;
using OpenSim.Services.Interfaces;
using Aurora.Framework;
using OpenMetaverse;
using OpenSim.Framework;

namespace OpenSim.Server.Handlers.AuroraMap
{
    public class SimMapConnector
    {
        private IGridService GridService;
        private IGridConnector GridConnector;
        private IEstateConnector EstateConnector;

        Dictionary<UUID, SimMap> Sims = new Dictionary<UUID, SimMap>();

        public SimMapConnector(IGridService GS)
        {
            GridService = GS;
            EstateConnector = Aurora.DataManager.DataManager.IEstateConnector;
            GridConnector = Aurora.DataManager.DataManager.IGridConnector;
        }

        public SimMap GetSimMap(UUID regionID, UUID AgentID)
        {
            SimMap map = new SimMap();
            if (Sims.ContainsKey(regionID))
            {
                Sims.TryGetValue(regionID, out map);

                if (map.LastUpdated > Util.UnixTimeSinceEpoch() + (1000 * 6)) // Greater than 6 minutes since the last update
                {
                    //Its hasn't updated in the last 6 minutes, and it is supposed to update every 5, so it's down.
                    map.Access = map.Access & (uint)SimAccess.Down;
                    Sims.Remove(regionID);
                    Sims.Add(regionID, map);
                }
                if (((int)map.GridRegionFlags & (int)GridRegionFlags.Hidden) == 1)
                {
                    EstateSettings ES = EstateConnector.LoadEstateSettings((int)map.EstateID);
                    if (!ES.IsEstateManager(AgentID))
                        return NotFound(map.RegionLocX, map.RegionLocY);
                }   
            }
            else
            {
                Services.Interfaces.GridRegion R = GridService.GetRegionByUUID(UUID.Zero, regionID);
                if (R == null)
                    return NotFound(0,0);
                else
                {
                    map = AddRegion(R);

                    if (((int)map.GridRegionFlags & (int)GridRegionFlags.Hidden) == 1)
                    {
                        EstateSettings ES = EstateConnector.LoadEstateSettings((int)map.EstateID);
                        if (!ES.IsEstateManager(AgentID))
                            return NotFound(map.RegionLocX, map.RegionLocY);
                    }
                }
            }
            return map;
        }

        public void AddAgent(UUID regionID, Vector3 Position)
        {
        }

        public SimMap AddRegion(Services.Interfaces.GridRegion R)
        {
            SimMap map = new SimMap();
            map.GridRegionFlags = GridConnector.GetRegionFlags(R.RegionID);
            //If the cache doesn't have it, theres nothing there.
            map.Access = FindAccessFromFlags(map.GridRegionFlags);
            map.NumberOfAgents = 0;
            map.RegionID = R.RegionID;
            map.RegionLocX = R.RegionLocX;
            map.RegionLocY = R.RegionLocY;
            map.RegionName = R.RegionName;
            //Since this is the first time we've seen it, we have to use this... even though it may be wrong.
            map.SimMapTextureID = R.TerrainImage;
            map.WaterHeight = 0;
            map.EstateID = FindEstateID(R.RegionID);
            map.RegionFlags = 0; //Unknown
            map.LastUpdated = Util.UnixTimeSinceEpoch();

            //Add to the cache.
            if (Sims.ContainsKey(map.RegionID))
                Sims.Remove(map.RegionID);

            Sims.Add(R.RegionID, map);
            return map;
        }

        public void RemoveRegion(UUID regionID)
        {
            if (Sims.ContainsKey(regionID))
                Sims.Remove(regionID);
        }

        #region Helpers

        private uint FindAccessFromFlags(GridRegionFlags flags)
        {
            if (((int)flags & (int)GridRegionFlags.Adult) == 1)
                return (int)SimAccess.Mature;
            else if (((int)flags & (int)GridRegionFlags.Mature) == 1)
                return (int)SimAccess.PG;
            else if (((int)flags & (int)GridRegionFlags.PG) == 1)
                return (int)SimAccess.Min;
            else
                return (int)SimAccess.Min;
        }

        private uint FindEstateID(UUID regionID)
        {
            EstateSettings ES = EstateConnector.LoadEstateSettings(regionID, false);
            return ES.EstateID;
        }

        public SimMap NotFound(int regionX, int regionY)
        {
            SimMap map = new SimMap();
            map.RegionFlags = 0;
            map.NumberOfAgents = 0;
            map.GridRegionFlags = 0;
            map.EstateID = 0;
            map.Access = (int)SimAccess.Down;
            map.RegionID = UUID.Zero;
            map.RegionLocX = regionX;
            map.RegionLocY = regionY;
            map.RegionName = "";
            map.SimMapTextureID = UUID.Zero;
            map.WaterHeight = 0;
            return map;
        }

        private SimMap GetSimMap(UUID regionID)
        {
            SimMap map = new SimMap();
            if (Sims.ContainsKey(regionID))
            {
                Sims.TryGetValue(regionID, out map);

                if (map.LastUpdated > Util.UnixTimeSinceEpoch() + (1000 * 6)) // Greater than 6 minutes since the last update
                {
                    //Its hasn't updated in the last 6 minutes, and it is supposed to update every 5, so it's down.
                    map.Access = (int)SimAccess.Down;
                    Sims.Remove(regionID);
                    Sims.Add(regionID, map);
                }
            }
            else
            {
                Services.Interfaces.GridRegion R = GridService.GetRegionByUUID(UUID.Zero, regionID);
                if (R == null)
                    return NotFound(0,0);
                else
                {
                    map = AddRegion(R);
                }
            }
            return map;
        }

        #endregion

        public void UpdateRegion(UUID regionID)
        {
            SimMap map = GetSimMap(regionID);

            map.LastUpdated = Util.UnixTimeSinceEpoch();
            map.Access = map.Access | (uint)SimAccess.Down;

            if (Sims.ContainsKey(map.RegionID))
                Sims.Remove(map.RegionID);
            Sims.Add(regionID, map);
            
        }
    }
}
