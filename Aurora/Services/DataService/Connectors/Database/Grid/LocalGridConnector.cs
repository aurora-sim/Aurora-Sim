/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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
using System.Linq;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace Aurora.Services.DataService
{
    public class LocalGridConnector : IRegionData
    {
        private IGenericData GD;
        private string m_realm = "gridregions";

        #region IRegionData Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("GridConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                string connectionString = (source.Configs[Name] != null) ? connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString) : defaultConnectionString;

                GD.ConnectToDatabase(connectionString, "GridRegions", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(this);

                MainConsole.Instance.Commands.AddCommand("fix missing region owner", "fix missing region owner", "Attempts to fix missing region owners in the database.", delegate(string[] cmd)
                {
                    FixMissingRegionOwners();
                });
            }
        }

        public string Name
        {
            get { return "IRegionData"; }
        }

        private void FixMissingRegionOwners()
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["OwnerUUID"] = UUID.Zero;

            List<GridRegion> borked = ParseQuery(null, GD.Query(new string[1] { "*" }, m_realm, filter, null, null, null));

            if(borked.Count < 1){
                MainConsole.Instance.Debug("[LocalGridConnector] No regions found with missing owners.");
            }
            IEstateConnector estatePlugin = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector>();

            if(estatePlugin == null){
                MainConsole.Instance.Error("[LocalGridConnector] " + borked.Count + " regions found with missing owners, but could not get IEstateConnector plugin.");
                return;
            }else{
                MainConsole.Instance.Error("[LocalGridConnector] " + borked.Count + " regions found with missing owners, attempting fix.");
            }

            Dictionary<int, List<GridRegion>> borkedByEstate = new Dictionary<int, List<GridRegion>>();
            foreach (GridRegion region in borked)
            {
                int estateID = estatePlugin.GetEstateID(region.RegionID);
                if (!borkedByEstate.ContainsKey(estateID))
                {
                    borkedByEstate[estateID] = new List<GridRegion>();
                }
                borkedByEstate[estateID].Add(region);
            }

            Dictionary<int, UUID> estateOwnerIDs = new Dictionary<int, UUID>();
            uint estateFail = 0;
            foreach (int estateID in borkedByEstate.Keys)
            {
                EstateSettings es = estatePlugin.GetEstateSettings(estateID);
                if (es == null)
                {
                    MainConsole.Instance.Error("[LocalGridConnector] Cannot fix missing owner for regions in Estate " + estateID + ", could not get estate settings.");
                }else if (es.EstateOwner == UUID.Zero)
                {
                    MainConsole.Instance.Error("[LocalGridConnector] Cannot fix missing owner for regions in Estate " + estateID + ", Estate Owner is also missing.");
                }
                if (es == null || es.EstateOwner == UUID.Zero)
                {
                    ++estateFail;
                    continue;
                }
                estateOwnerIDs[estateID] = es.EstateOwner;
            }

            if (estateFail > 0)
            {
                if (estateFail == borkedByEstate.Count)
                {
                    MainConsole.Instance.Error("[LocalGridConnector] " + borked.Count + " regions found with missing owners, could not locate any estate settings from IEstateConnector plugin.");
                    return;
                }
                else
                {
                    MainConsole.Instance.Error("[LocalGridConnector] " + borked.Count + " regions found with missing owners, could not locate estate settings for " + estateFail + " estates.");
                }
            }

            uint storeSuccess = 0;
            uint storeFail = 0;
            int borkedCount = borked.Count;
            foreach (KeyValuePair<int, UUID> kvp in estateOwnerIDs)
            {
                List<GridRegion> regions = borkedByEstate[kvp.Key];
                foreach (GridRegion region in regions)
                {
                    region.EstateOwner = kvp.Value;
                    if (!Store(region))
                    {
                        MainConsole.Instance.Error("[LocalGridConnector] Failed to fix missing region for " + region.RegionName + " (" + region.RegionID + ")");
                        ++storeFail;
                    }else{
                        ++storeSuccess;
                        borked.Remove(region);
                    }
                }
            }

            if (storeFail > 0)
            {
                MainConsole.Instance.Error("[LocalGridConnector] " + borkedCount + " regions found with missing owners, fix failed on " + storeFail + " regions, fix attempted on " + storeSuccess + " regions.");
            }
            else if (storeSuccess != borked.Count)
            {
                MainConsole.Instance.Error("[LocalGridConnector] " + borkedCount + " regions found with missing owners, fix attempted on " + storeSuccess + " regions.");
            }
            else
            {
                MainConsole.Instance.Info("[LocalGridConnector] All regions found with missing owners should have their owners restored.");
            }
            if(borked.Count > 0){
                List<string> blurbs = new List<string>(borked.Count);
                foreach (GridRegion region in borked)
                {
                    blurbs.Add(region.RegionName + " (" + region.RegionID + ")");
                }
                MainConsole.Instance.Info("[LocalGridConnector] Failed to fix missing region owners for regions " + string.Join(", ", blurbs.ToArray()));
            }
        }

        public uint GetCount(string regionName, List<UUID> scopeIDs)
        {
            QueryFilter filter = new QueryFilter();
            filter.andLikeFilters["RegionName"] = regionName;

            return uint.Parse(GD.Query(new[] { "COUNT(*)" }, m_realm, filter, null, null, null)[0]);
        }

        public List<GridRegion> Get(string regionName, List<UUID> scopeIDs, uint? start, uint? count)
        {
            QueryFilter filter = new QueryFilter();
            filter.andLikeFilters["RegionName"] = regionName;

            List<string> query = GD.Query(new string[1] { "*" }, m_realm, filter, null, start, count);

            return (query.Count == 0) ? null : ParseQuery(scopeIDs, query);
        }

        public List<GridRegion> Get(RegionFlags flags)
        {
            QueryFilter filter = new QueryFilter();
            filter.andBitfieldAndFilters["Flags"] = (uint)flags;
            return ParseQuery(null, GD.Query(new string[1] { "*" }, m_realm, filter, null, null, null));
        }

        public GridRegion GetZero(int posX, int posY, List<UUID> scopeIDs)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["LocX"] = posX;
            filter.andFilters["LocY"] = posY;

            List<string> query = GD.Query(new string[1] { "*" }, m_realm, filter, null, null, null);

            return (query.Count == 0) ? null : ParseQuery(scopeIDs, query)[0];
        }

        public List<GridRegion> Get(int posX, int posY, List<UUID> scopeIDs)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["LocX"] = posX;
            filter.andFilters["LocY"] = posY;

            Dictionary<string, bool> sort = new Dictionary<string, bool>(1);
            sort["LocZ"] = true;

            return ParseQuery(scopeIDs, GD.Query(new string[1] { "*" }, m_realm, filter, sort, null, null));
        }

        public GridRegion Get(UUID regionID, List<UUID> scopeIDs)
        {
            List<string> query;
            Dictionary<string, object> where = new Dictionary<string, object>();

            where["RegionUUID"] = regionID;

            query = GD.Query(new string[1] { "*" }, m_realm, new QueryFilter
            {
                andFilters = where
            }, null, null, null);

            return (query.Count == 0) ? null : ParseQuery(scopeIDs, query)[0];
        }

        public List<GridRegion> Get(int startX, int startY, int endX, int endY, List<UUID> scopeIDs)
        {
            int foo;
            if (startX > endX)
            {
                foo = endX;
                endX = startX;
                startX = foo;
            }
            if (startY > endY)
            {
                foo = endY;
                endY = startY;
                startY = foo;
            }
            QueryFilter filter = new QueryFilter();
            filter.andGreaterThanEqFilters["LocX"] = startX;
            filter.andLessThanEqFilters["LocX"] = endX;
            filter.andGreaterThanEqFilters["LocY"] = startY;
            filter.andLessThanEqFilters["LocY"] = endY;

            return ParseQuery(scopeIDs, GD.Query(new string[1] { "*" }, m_realm, filter, null, null, null));
        }

        public List<GridRegion> Get(RegionFlags flags, Dictionary<string, bool> sort)
        {
            return Get(flags, 0, null, null, sort);
        }

        public List<GridRegion> Get(uint start, uint count, uint estateID, RegionFlags flags, Dictionary<string, bool> sort)
        {
            List<GridRegion> resp = new List<GridRegion>();
            IEstateConnector estates = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector>();

            if (count == 0 || estates == null)
            {
                return resp;
            }

            EstateSettings es = estates.GetEstateSettings((int)estateID);

            QueryFilter filter = new QueryFilter();
            filter.andBitfieldAndFilters["Flags"] = (uint)flags;

            while (resp.Count < count)
            {
                uint limit = count - (uint)resp.Count;
                List<GridRegion> query = ParseQuery(null, GD.Query(new string[] { "*" }, m_realm, filter, sort, start, count));

                if (query.Count == 0)
                {
                    break;
                }

                query.ForEach(delegate(GridRegion region)
                {
                    if (region.EstateOwner == es.EstateOwner && estates.GetEstateID(region.RegionID) == es.EstateID)
                    {
                        resp.Add(region);
                    }
                });

                start += limit;
            }

            return resp;
        }

        public List<GridRegion> Get(RegionFlags includeFlags, RegionFlags excludeFlags, uint? start, uint? count, Dictionary<string, bool> sort)
        {
            QueryFilter filter = new QueryFilter();
            if (includeFlags > 0)
            {
                filter.andBitfieldAndFilters["Flags"] = (uint)includeFlags;
            }
            if (excludeFlags > 0)
            {
                filter.andBitfieldNandFilters["Flags"] = (uint)excludeFlags;
            }

            return ParseQuery(null, GD.Query(new string[1] { "*" }, m_realm, filter, sort, start, count));
        }

        public uint Count(RegionFlags includeFlags, RegionFlags excludeFlags)
        {
            QueryFilter filter = new QueryFilter();
            if (includeFlags > 0)
            {
                filter.andBitfieldAndFilters["Flags"] = (uint)includeFlags;
            }
            if (excludeFlags > 0)
            {
                filter.andBitfieldNandFilters["Flags"] = (uint)excludeFlags;
            }

            return uint.Parse(GD.Query(new string[1] { "COUNT(*)" }, m_realm, filter, null, null, null)[0]);
        }

        public List<GridRegion> GetNeighbours(UUID regionID, List<UUID> scopeIDs, uint squareRangeFromCenterInMeters)
        {
            List<GridRegion> regions = new List<GridRegion>(0);
            GridRegion region = Get(regionID, scopeIDs);

            if (region != null)
            {
                int centerX = region.RegionLocX + (region.RegionSizeX / 2); // calculate center of region
                int centerY = region.RegionLocY + (region.RegionSizeY / 2); // calculate center of region

                regions = Get(scopeIDs, region.RegionID, centerX, centerY, squareRangeFromCenterInMeters);
            }

            return regions;
        }

        public List<GridRegion> Get(List<UUID> scopeIDs, UUID excludeRegion, float centerX, float centerY, uint squareRangeFromCenterInMeters)
        {
            QueryFilter filter = new QueryFilter();

            if (excludeRegion != UUID.Zero)
            {
                filter.andNotFilters["RegionUUID"] = excludeRegion;
            }
            filter.andGreaterThanEqFilters["(LocX + SizeX)"] = centerX - squareRangeFromCenterInMeters;
            filter.andGreaterThanEqFilters["(LocY + SizeY)"] = centerY - squareRangeFromCenterInMeters;
            filter.andLessThanEqFilters["(LocX - SizeX)"] = centerX + squareRangeFromCenterInMeters;
            filter.andLessThanEqFilters["(LocY - SizeY)"] = centerY + squareRangeFromCenterInMeters;

            Dictionary<string, bool> sort = new Dictionary<string, bool>(3);
            sort["LocZ"] = true;
            sort["LocX"] = true;
            sort["LocY"] = true;

            return ParseQuery(scopeIDs, GD.Query(new string[] { "*" }, m_realm, filter, sort, null, null));
        }

        public uint Count(uint estateID, RegionFlags flags)
        {
            IEstateConnector estates = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector>();

            if (estates == null)
            {
                return 0;
            }

            EstateSettings es = estates.GetEstateSettings((int)estateID);

            QueryFilter filter = new QueryFilter();
            filter.andBitfieldAndFilters["Flags"] = (uint)flags;

            List<GridRegion> query = ParseQuery(null, GD.Query(new string[] { "*" }, m_realm, filter, null, null, null));

            uint count = 0;
            query.ForEach(delegate(GridRegion region)
            {
                if (region.EstateOwner == es.EstateOwner && estates.GetEstateID(region.RegionID) == es.EstateID)
                {
                    ++count;
                }
            });

            return count;
        }

        public bool Store(GridRegion region)
        {
            if (region.EstateOwner == UUID.Zero)
            {
                IEstateConnector EstateConnector = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector>();
                EstateSettings ES = null;
                if (EstateConnector != null)
                {
                    ES = EstateConnector.GetEstateSettings(region.RegionID);
                    if (ES != null)
                        region.EstateOwner = ES.EstateOwner;
                }
                if (region.EstateOwner == UUID.Zero && ES != null && ES.EstateID != 0)
                {
                    MainConsole.Instance.Error("[LocalGridConnector] Attempt to store region with owner of UUID.Zero detected:" + (new System.Diagnostics.StackTrace()).GetFrame(1).ToString());
                }
            }

            Dictionary<string, object> row = new Dictionary<string, object>(14);
            row["ScopeID"] = region.ScopeID;
            row["RegionUUID"] = region.RegionID;
            row["RegionName"] = region.RegionName;
            row["LocX"] = region.RegionLocX;
            row["LocY"] = region.RegionLocY;
            row["LocZ"] = region.RegionLocZ;
            row["OwnerUUID"] = region.EstateOwner;
            row["Access"] = region.Access;
            row["SizeX"] = region.RegionSizeX;
            row["SizeY"] = region.RegionSizeY;
            row["SizeZ"] = region.RegionSizeZ;
            row["Flags"] = region.Flags;
            row["SessionID"] = region.SessionID;
            row["Info"] = OSDParser.SerializeJsonString(region.ToOSD());

            return GD.Replace(m_realm, row);
        }

        public bool Delete(UUID regionID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["RegionUUID"] = regionID;
            return GD.Delete(m_realm, filter);
        }

        public bool DeleteAll(string[] criteriaKey, object[] criteriaValue)
        {
            QueryFilter filter = new QueryFilter();
            int i = 0;
            foreach (object value in criteriaValue)
            {
                filter.andFilters[criteriaKey[i++]] = value;
            }
            return GD.Delete(m_realm, filter);
        }

        public List<GridRegion> GetDefaultRegions(List<UUID> scopeIDs)
        {
            return Get((int) RegionFlags.DefaultRegion, scopeIDs);
        }

        public List<GridRegion> GetFallbackRegions(List<UUID> scopeIDs, int x, int y)
        {
            List<GridRegion> regions = Get((int) RegionFlags.FallbackRegion, scopeIDs);
            RegionDataDistanceCompare distanceComparer = new RegionDataDistanceCompare(x, y);
            regions.Sort(distanceComparer);
            return regions;
        }

        public List<GridRegion> GetSafeRegions(List<UUID> scopeIDs, int x, int y)
        {
            List<GridRegion> Regions = Get((int) RegionFlags.Safe, scopeIDs);
            Regions.AddRange(Get((int) RegionFlags.RegionOnline, scopeIDs));

            RegionDataDistanceCompare distanceComparer = new RegionDataDistanceCompare(x, y);
            Regions.Sort(distanceComparer);
            return Regions;
        }

        #endregion

        public void Dispose()
        {
        }

        private List<GridRegion> Get(int regionFlags, List<UUID> scopeIDs)
        {
            QueryFilter filter = new QueryFilter();
            filter.andBitfieldAndFilters["Flags"] = (uint)regionFlags;

            return ParseQuery(scopeIDs, GD.Query(new string[1] { "*" }, m_realm, filter, null, null, null));
        }

        protected List<GridRegion> ParseQuery(List<UUID> scopeIDs, List<string> query)
        {
            List<GridRegion> regionData = new List<GridRegion>();

            if ((query.Count % 14) == 0)
            {
                for (int i = 0; i < query.Count; i += 14)
                {
                    GridRegion data = new GridRegion();
                    OSDMap map = (OSDMap)OSDParser.DeserializeJson(query[i + 13]);
                    map["owner_uuid"] = (!map.ContainsKey("owner_uuid") || map["owner_uuid"].AsUUID() == UUID.Zero) ? OSD.FromUUID(UUID.Parse(query[i + 6])) : map["owner_uuid"];
                    map["EstateOwner"] = (!map.ContainsKey("EstateOwner") || map["EstateOwner"].AsUUID() == UUID.Zero) ? OSD.FromUUID(UUID.Parse(query[i + 6])) : map["EstateOwner"];
                    data.FromOSD(map);

                    //Check whether it should be down
                    if (data.LastSeen > (Util.UnixTimeSinceEpoch() + (1000 * 6)))
                        data.Access |= (int)SimAccess.Down;

                    if (!regionData.Contains(data))
                        regionData.Add(data);
                }
            }

            return AllScopeIDImpl.CheckScopeIDs(scopeIDs, regionData);
        }

        #region Nested type: RegionDataDistanceCompare

        public class RegionDataDistanceCompare : IComparer<GridRegion>
        {
            private readonly Vector2 m_origin;

            public RegionDataDistanceCompare(int x, int y)
            {
                m_origin = new Vector2(x, y);
            }

            #region IComparer<GridRegion> Members

            public int Compare(GridRegion regionA, GridRegion regionB)
            {
                Vector2 vectorA = new Vector2(regionA.RegionLocX, regionA.RegionLocY);
                Vector2 vectorB = new Vector2(regionB.RegionLocX, regionB.RegionLocY);
                return Math.Sign(VectorDistance(m_origin, vectorA) - VectorDistance(m_origin, vectorB));
            }

            #endregion

            private float VectorDistance(Vector2 x, Vector2 y)
            {
                return (x - y).Length();
            }
        }

        #endregion
    }
}