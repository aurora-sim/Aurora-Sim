using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using OpenSim.Services.Interfaces;
using Aurora.Framework;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public class SimMapConnector
    {
        private ISimMapDataConnector SimMapDataConnector;
        private IEstateConnector EstateConnector;
        private uint LastNull = 0;

        //DoubleDictionary<UUID, ulong, SimMap> Sims = new DoubleDictionary<UUID, ulong, SimMap>();
        Dictionary<UUID, SimMap> SimIDs = new Dictionary<UUID, SimMap>();
        Dictionary<ulong, SimMap> SimHandles = new Dictionary<ulong, SimMap>();
        public SimMapConnector(IGridService GS)
        {
            EstateConnector = Aurora.DataManager.DataManager.IEstateConnector;
            SimMapDataConnector = Aurora.DataManager.DataManager.ISimMapConnector;
        }

        public SimMap GetSimMap(UUID regionID, UUID AgentID)
        {
            SimMap map = new SimMap();
            if (SimIDs.ContainsKey(regionID))
            {
                SimIDs.TryGetValue(regionID, out map);

                //We know theres no region there.
                if (map == null)
                    return null;

                if (map.LastUpdated > Util.UnixTimeSinceEpoch() + (1000 * 6)) // Greater than 6 minutes since the last update
                {
                    //Its hasn't updated in the last 6 minutes, and it is supposed to update every 5, so it's down.
                    map.Access = map.Access | (uint)SimAccess.Down;
                    SimIDs.Remove(regionID);
                    SimIDs.Add(regionID, map);
                    SimHandles.Add(map.RegionHandle, map);
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
                if (map == null)
                {
                    LastNull++;
                    SimIDs.Add(regionID, map);
                }
                else
                {
                    SimIDs.Add(regionID, map);
                    SimHandles.Add(map.RegionHandle, map);
                }

                //No region there.
                if (map == null)
                    return null;
            }
            return map;
        }

        public void AddAgent(UUID regionID, UUID agentID, Vector3 Position)
        {
            SimMap map = GetSimMap(regionID, agentID);
            map.NumberOfAgents += 1;

            Position.X = NormalizePosition(Position.X);
            Position.Y = NormalizePosition(Position.Y);
            Position.Z = NormalizePosition(Position.Z);

            if (map.PositionsOfAgents.ContainsKey(Position))
            {
                uint NumberOfAgents;
                map.PositionsOfAgents.TryGetValue(Position, out NumberOfAgents);
                NumberOfAgents++;
                map.PositionsOfAgents.Remove(Position);
                map.PositionsOfAgents.Add(Position, NumberOfAgents);
            }
            else
                map.PositionsOfAgents.Add(Position, 1);


            if (map.AgentPosition.ContainsKey(agentID))
                map.AgentPosition.Remove(agentID);
            map.AgentPosition.Add(agentID, Position);

            //No need to call UpdateSimMap, that would update the database for no reason
            SimIDs.Remove(regionID);
            SimIDs.Add(map.RegionID, map);
            SimHandles.Remove(map.RegionHandle);
            SimHandles.Add(map.RegionHandle, map);
        }

        private float NormalizePosition(float number)
        {
            string Number = number.ToString();
            string endNumber = Number.Remove(0, Number.Length - 2);
            string firstNumber = Number.Remove(Number.Length - 2);
            float EndNumber = float.Parse(endNumber);
            if (EndNumber < 2.5f || EndNumber > 7.5)
                EndNumber = 0;
            else
                EndNumber = 5;
            return float.Parse(firstNumber + EndNumber.ToString());
        }

        public void RemoveAgent(UUID regionID, UUID agentID)
        {
            SimMap map = GetSimMap(regionID, agentID);

            map.NumberOfAgents -= 1;

            //Remove the agent's location from memory
            Vector3 Position;
            map.AgentPosition.TryGetValue(agentID, out Position);
            map.AgentPosition.Remove(agentID);

            //Remove an agent from the number of agents count at the agents location.
            uint NumberOfAgents = 0;
            map.PositionsOfAgents.TryGetValue(Position, out NumberOfAgents);
            NumberOfAgents--;
            map.PositionsOfAgents.Remove(Position);
            map.PositionsOfAgents.Add(Position, NumberOfAgents);

            //No need to call UpdateSimMap, that would update the database for no reason
            SimIDs.Remove(regionID);
            SimIDs.Add(map.RegionID, map);
            SimHandles.Remove(map.RegionHandle);
            SimHandles.Add(map.RegionHandle, map);
        }

        public List<mapItemReply> GetMapItems(ulong regionHandle, GridItemType gridItemType)
        {
            if (gridItemType == GridItemType.AgentLocations)
            {
                SimMap map = GetSimMap(regionHandle);

                List<mapItemReply> mapItems = new List<mapItemReply>();
                foreach (KeyValuePair<Vector3,uint> position in map.PositionsOfAgents)
                {
                    mapItemReply mapitem = new mapItemReply();
                    mapitem.x = (uint)(map.RegionLocX + position.Key.X);
                    mapitem.y = (uint)(map.RegionLocX + position.Key.Y);
                    mapitem.id = UUID.Zero;
                    mapitem.name = Util.Md5Hash(map.RegionName + Environment.TickCount.ToString());
                    mapitem.Extra = (int)position.Value;
                    mapitem.Extra2 = 0;
                    mapItems.Add(mapitem);
                }

                //This is a remote request, so we don't use one, 
                // as that is to not add the agents
                // own dot, and that will not happen here.
                if (mapItems.Count == 0)
                {
                    mapItemReply mapitem = new mapItemReply();
                    mapitem.x = (uint)(map.RegionLocX + 1);
                    mapitem.y = (uint)(map.RegionLocY + 1);
                    mapitem.id = UUID.Zero;
                    mapitem.name = Util.Md5Hash(map.RegionName + Environment.TickCount.ToString());
                    mapitem.Extra = 0;
                    mapitem.Extra2 = 0;
                    mapItems.Add(mapitem);
                }
                return mapItems;
            }

            //m_log.Error("[SimMapConnector]: Unknown GridItemType " + gridItemType.ToString());
            return new List<mapItemReply>();
        }

        public SimMap TryAddSimMap(OpenSim.Services.Interfaces.GridRegion R, out string result)
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
                    SimMapDataConnector.SetSimMap(map);
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
            if (SimIDs.ContainsKey(map.RegionID))
                SimIDs.Remove(map.RegionID);
            if (SimHandles.ContainsKey(map.RegionHandle))
                SimHandles.Remove(map.RegionHandle);

            UpdateSimMap(map);
            return map;
        }

        public void RemoveSimMap(UUID regionID)
        {
            SimMap map = GetSimMap(regionID);
            if (SimIDs.ContainsKey(regionID))
                SimIDs.Remove(regionID);
            if (SimHandles.ContainsKey(map.RegionHandle))
                SimHandles.Remove(map.RegionHandle);
        }

        public void UpdateSimMap(UUID regionID)
        {
            SimMap map = GetSimMap(regionID);

            map.LastUpdated = Util.UnixTimeSinceEpoch();
            map.Access = map.Access & ~(uint)SimAccess.Down;

            if (SimIDs.ContainsKey(map.RegionID))
                SimIDs.Remove(map.RegionID);
            if (SimHandles.ContainsKey(map.RegionHandle))
                SimHandles.Remove(map.RegionHandle);
            SimIDs.Add(map.RegionID, map);
            SimHandles.Add(map.RegionHandle, map);
        }

        public void UpdateSimMap(SimMap map)
        {
            map.LastUpdated = Util.UnixTimeSinceEpoch();
            map.Access = map.Access & ~(uint)SimAccess.Down;

            if (SimIDs.ContainsKey(map.RegionID))
                SimIDs.Remove(map.RegionID);
            if (SimHandles.ContainsKey(map.RegionHandle))
                SimHandles.Remove(map.RegionHandle);
            SimIDs.Add(map.RegionID, map);
            SimHandles.Add(map.RegionHandle, map);

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
            map.RegionHandle = R.RegionHandle;
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
            if (SimIDs.ContainsKey(regionID))
            {
                SimIDs.TryGetValue(regionID, out map);

                //We know theres no region there.
                if (map == null)
                    return NotFound(0, 0);

                if (map.LastUpdated > Util.UnixTimeSinceEpoch() + (1000 * 6)) // Greater than 6 minutes since the last update
                {
                    //Its hasn't updated in the last 6 minutes, and it is supposed to update every 5, so it's down.
                    map.Access = map.Access | (uint)SimAccess.Down;
                    SimIDs.Remove(regionID);
                    SimIDs.Add(regionID, map);
                    SimHandles.Add(map.RegionHandle, map);
                }
            }
            else
            {
                map = SimMapDataConnector.GetSimMap(regionID);
                //Add null regions so we don't query the database again.
                if (map == null)
                {
                    LastNull++;
                    SimIDs.Add(regionID, map);
                }
                else
                {
                    SimIDs.Add(regionID, map);
                    SimHandles.Add(map.RegionHandle, map);
                }

                //No region there.
                if (map == null)
                    return NotFound(0, 0);
            }
            return map;
        }

        private SimMap GetSimMap(ulong regionHandle)
        {
            SimMap map = new SimMap();
            if (SimHandles.ContainsKey(regionHandle))
            {
                SimHandles.TryGetValue(regionHandle, out map);

                //We know theres no region there.
                if (map == null)
                    return NotFound(0, 0);

                if (map.LastUpdated > Util.UnixTimeSinceEpoch() + (1000 * 6)) // Greater than 6 minutes since the last update
                {
                    //Its hasn't updated in the last 6 minutes, and it is supposed to update every 5, so it's down.
                    map.Access = map.Access | (uint)SimAccess.Down;
                    SimHandles.Remove(regionHandle);
                    SimHandles.Add(regionHandle, map);
                }
            }
            else
            {
                map = SimMapDataConnector.GetSimMap(regionHandle);
                //Add null regions so we don't query the database again.
                if (map == null)
                {
                    LastNull++;
                    return NotFound(0, 0);
                }
                else
                    SimHandles.Add(regionHandle, map);
            }
            return map;
        }

        #endregion
    }
}
