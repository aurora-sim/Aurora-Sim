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
        private ISimMapDataConnector SimMapDataConnector;
        private IEstateConnector EstateConnector;

        Dictionary<UUID, SimMap> Sims = new Dictionary<UUID, SimMap>();

        public SimMapConnector(IGridService GS)
        {
            GridService = GS;
            EstateConnector = Aurora.DataManager.DataManager.IEstateConnector;
            SimMapDataConnector = Aurora.DataManager.DataManager.ISimMapConnector;
        }

        public SimMap GetSimMap(UUID regionID, UUID AgentID)
        {
            SimMap map = new SimMap();
            if (Sims.ContainsKey(regionID))
            {
                Sims.TryGetValue(regionID, out map);

                //We know theres no region there.
                if (map == null)
                    return NotFound(0, 0);

                if (map.LastUpdated > Util.UnixTimeSinceEpoch() + (1000 * 6)) // Greater than 6 minutes since the last update
                {
                    //Its hasn't updated in the last 6 minutes, and it is supposed to update every 5, so it's down.
                    map.Access = map.Access | (uint)SimAccess.Down;
                    Sims.Remove(regionID);
                    Sims.Add(regionID, map);
                }

                if (((int)map.SimFlags & (int)SimMapFlags.Hidden) == 1)
                {
                    EstateSettings ES = EstateConnector.LoadEstateSettings((int)map.EstateID);
                    if (!ES.IsEstateManager(AgentID))
                        return NotFound(map.RegionLocX, map.RegionLocY);
                }   
            }
            else
            {
                map = SimMapDataConnector.GetSimMap(regionID);
                //Add null regions so we don't query the database again.
                Sims.Add(regionID, map);

                //No region there.
                if (map == null)
                    return NotFound(0,0);
            }
            return map;
        }

        public void AddAgent(UUID regionID, Vector3 Position)
        {

        }

        public SimMap TryAddSimMap(Services.Interfaces.GridRegion R, out string result)
        {
            SimMap map = SimMapDataConnector.GetSimMap(R.RegionID);
            if (map == null)
            {
                //This region hasn't been seen before, we need to check it.
                map = SimMapDataConnector.GetSimMap(R.RegionLocX, R.RegionLocY);
                if (map == null)
                {
                    //This location has never been used before and the region was not found elsewhere
                    map = CreateDefaultSimMap(R);
                }
                else
                {
                    if ((map.SimFlags & SimMapFlags.Reservation) == SimMapFlags.Reservation)
                    {
                        result = "Region coordinates are reserved.";
                    }
                    else
                    {
                        //Some other region already has this location.
                        result = "Region coordinates are already in use.";
                    }
                    return null;
                }
            }
            else
            {
                if (map.RegionLocX != R.RegionLocX && map.RegionLocY != R.RegionLocY)
                {
                    if ((map.SimFlags & SimMapFlags.NoMove) == SimMapFlags.NoMove)
                    {
                        result = "You cannot move this region's coordinates.";
                        return null;
                    }
                    //This region is moving. We need to update it.
                    UpdateSimMap(map);
                }
                //This region is already here.
                map.Access = map.Access & ~(uint)SimAccess.Down;
            }

            if ((map.SimFlags & SimMapFlags.LockedOut) == SimMapFlags.LockedOut)
            {
                result = "This region has been blocked from connecting.";
                return null;
            }

            //Successful
            result = "";
            map.LastUpdated = Util.UnixTimeSinceEpoch();

            //Add to the cache.
            if (Sims.ContainsKey(map.RegionID))
                Sims.Remove(map.RegionID);

            Sims.Add(R.RegionID, map);
            return map;
        }

        public void RemoveSimMap(UUID regionID)
        {
            if (Sims.ContainsKey(regionID))
                Sims.Remove(regionID);
        }

        public void UpdateSimMap(UUID regionID)
        {
            SimMap map = GetSimMap(regionID);

            map.LastUpdated = Util.UnixTimeSinceEpoch();
            map.Access = map.Access & ~(uint)SimAccess.Down;

            if (Sims.ContainsKey(regionID))
                Sims.Remove(regionID);
            Sims.Add(regionID, map);
        }

        public void UpdateSimMap(SimMap map)
        {
            map.LastUpdated = Util.UnixTimeSinceEpoch();
            map.Access = map.Access & ~(uint)SimAccess.Down;

            if (Sims.ContainsKey(map.RegionID))
                Sims.Remove(map.RegionID);
            Sims.Add(map.RegionID, map);

            SimMapDataConnector.SetSimMap(map);
        }

        #region Helpers

        private uint FindAccessFromFlags(SimMapFlags flags)
        {
            if (((int)flags & (int)SimMapFlags.Adult) == (int)SimMapFlags.Adult)
                return (int)SimAccess.Mature;
            else if (((int)flags & (int)SimMapFlags.Mature) == (int)SimMapFlags.Mature)
                return (int)SimAccess.PG;
            else if (((int)flags & (int)SimMapFlags.PG) == (int)SimMapFlags.PG)
                return (int)SimAccess.Min;
            else
                return (int)SimAccess.Min;
        }

        private uint FindEstateID(UUID regionID)
        {
            EstateSettings ES = EstateConnector.LoadEstateSettings(regionID, false);
            return ES.EstateID;
        }

        private SimMap CreateDefaultSimMap(OpenSim.Services.Interfaces.GridRegion R)
        {
            SimMap map = new SimMap();
            //If the cache doesn't have it, theres nothing there.
            map.Access = 2; // Defaults to Adult
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
            map.SimFlags = 0;
            return map;
        }

        public SimMap NotFound(int regionX, int regionY)
        {
            SimMap map = new SimMap();
            map.RegionFlags = 0;
            map.NumberOfAgents = 0;
            map.SimFlags = 0;
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

                //We know theres no region there.
                if (map == null)
                    return NotFound(0, 0);

                if (map.LastUpdated > Util.UnixTimeSinceEpoch() + (1000 * 6)) // Greater than 6 minutes since the last update
                {
                    //Its hasn't updated in the last 6 minutes, and it is supposed to update every 5, so it's down.
                    map.Access = map.Access | (uint)SimAccess.Down;
                    Sims.Remove(regionID);
                    Sims.Add(regionID, map);
                }
            }
            else
            {
                map = SimMapDataConnector.GetSimMap(regionID);
                //Add null regions so we don't query the database again.
                Sims.Add(regionID, map);

                //No region there.
                if (map == null)
                    return NotFound(0, 0);
            }
            return map;
        }

        #endregion
    }
}
