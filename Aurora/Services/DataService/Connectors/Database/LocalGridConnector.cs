using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using Nini.Config;
using OpenSim.Data;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace Aurora.Services.DataService
{
    public class LocalGridConnector : IRegionData
	{
		private IGenericData GD = null;
        private string m_realm = "gridregions";

        public void Initialize(IGenericData GenericData, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if(source.Configs["AuroraConnectors"].GetString("AbuseReportsConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(connectionString);

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IRegionData"; }
        }

        public void Dispose()
        {
        }

        public List<GridRegion> Get(string regionName, UUID scopeID)
        {
            List<string> query;
            if (scopeID != UUID.Zero)
                query = GD.Query(new string[2] { "RegionName", "ScopeID" }, new object[2]{regionName, scopeID}, m_realm, "*");
            else
                query = GD.Query("RegionName", regionName, m_realm, "*");

            if (query.Count == 0)
                return null;

            return ParseQuery(query);
        }

        public GridRegion Get(int posX, int posY, UUID scopeID)
        {
            List<string> query;
            if (scopeID != UUID.Zero)
                query = GD.Query(new string[3] { "LocX", "LocY", "ScopeID" },
                    new object[3] { posX, posY, scopeID }, m_realm, "*");
            else
                query = GD.Query(new string[2] { "LocX", "LocY" }, 
                    new object[2] { posX, posY }, m_realm, "*");

            if (query.Count == 0)
                return null;

            return ParseQuery(query)[0];
        }

        public GridRegion Get(UUID regionID, UUID scopeID)
        {
            List<string> query;
            if (scopeID != UUID.Zero)
                query = GD.Query(new string[2] { "RegionUUID", "ScopeID" }, new object[2] { regionID, scopeID }, m_realm, "*");
            else
                query = GD.Query("RegionUUID", regionID, m_realm, "*");

            if (query.Count == 0)
                return null;

            return ParseQuery(query)[0];
        }

        private List<GridRegion> Get(int regionFlags, UUID scopeID)
        {
            List<string> query;
            if (scopeID != UUID.Zero)
                query = GD.Query("(Flags & " + regionFlags.ToString() + ") <> 0 and ScopeID = '" + scopeID + "'", m_realm, "*");
            else
                query = GD.Query("(Flags & " + regionFlags.ToString() + ") <> 0", m_realm, "*");

            if (query.Count == 0)
                return new List<GridRegion>();

            return ParseQuery(query);
        }

        public List<GridRegion> Get(int startX, int startY, int endX, int endY, UUID scopeID)
        {
            List<string> query;
            if (scopeID != UUID.Zero)
                query = GD.Query("LocX between '" + startX + "' and '" + endX +
                    "' and LocY between '" + startY + "' and '" + endY + "'",
                    m_realm, "*");
            else
                query = GD.Query("LocX between '" + startX + "' and '" + endX +
                    "' and LocY between '" + startY + "' and '" + endY +
                    "' and ScopeID = '" + scopeID + "'", m_realm, "*");

            if (query.Count == 0)
                return new List<GridRegion>();

            return ParseQuery(query);
        }

        protected List<GridRegion> ParseQuery(List<string> query)
        {
            List<GridRegion> regionData = new List<GridRegion>();

            for (int i = 0; i < query.Count; i += 14)
            {
                GridRegion data = new GridRegion();
                OSDMap map = (OSDMap)OSDParser.DeserializeJson(query[13]);
                data.FromOSD(map);

                //Check whether it should be down
                if (data.LastSeen > Util.UnixTimeSinceEpoch() + (1000 * 6))
                    data.Access |= (int)SimAccess.Down;

                regionData.Add(data);
            }

            return regionData;
        }

        public bool Store(GridRegion region)
        {
            List<string> keys = new List<string>();
            List<object> values = new List<object>();

            keys.Add("ScopeID");
            keys.Add("RegionUUID");
            keys.Add("RegionName");
            keys.Add("LocX");
            keys.Add("LocY");
            keys.Add("LocZ");
            keys.Add("OwnerUUID");
            keys.Add("Access");
            keys.Add("SizeX");
            keys.Add("SizeY");
            keys.Add("SizeZ");
            keys.Add("Flags");
            keys.Add("SessionID");
            keys.Add("Info");

            values.Add(region.ScopeID);
            values.Add(region.RegionID);
            values.Add(region.RegionName);
            values.Add(region.RegionLocX);
            values.Add(region.RegionLocY);
            values.Add(region.RegionLocZ);
            values.Add(region.EstateOwner);
            values.Add(region.Access);
            values.Add(region.RegionSizeX);
            values.Add(region.RegionSizeY);
            values.Add(region.RegionSizeZ);
            values.Add(region.Flags); //Flags
            values.Add(region.SessionID);
            values.Add(OSDParser.SerializeJsonString(region.ToOSD()));

            return GD.Replace(m_realm, keys.ToArray(), values.ToArray());
        }

        public bool Delete(UUID regionID)
        {
            return GD.Delete(m_realm, new string[1] { "RegionUUID" }, new object[1] { regionID });
        }

        public List<GridRegion> GetDefaultRegions(UUID scopeID)
        {
            return Get((int)RegionFlags.DefaultRegion, scopeID);
        }

        public List<GridRegion> GetFallbackRegions(UUID scopeID, int x, int y)
        {
            List<GridRegion> regions = Get((int)RegionFlags.FallbackRegion, scopeID);
            RegionDataDistanceCompare distanceComparer = new RegionDataDistanceCompare(x, y);
            regions.Sort(distanceComparer);
            return regions;
        }

        public List<GridRegion> GetHyperlinks(UUID scopeID)
        {
            return Get((int)RegionFlags.Hyperlink, scopeID);
        }

        public List<GridRegion> GetSafeRegions(UUID scopeID, int x, int y)
        {
            List<GridRegion> Regions = Get((int)RegionFlags.Safe, scopeID);

            RegionDataDistanceCompare distanceComparer = new RegionDataDistanceCompare(x, y);
            Regions.Sort(distanceComparer);
            return Regions;
        }

        public class RegionDataDistanceCompare : IComparer<GridRegion>
        {
            private Vector2 m_origin;

            public RegionDataDistanceCompare(int x, int y)
            {
                m_origin = new Vector2(x, y);
            }

            public int Compare(GridRegion regionA, GridRegion regionB)
            {
                Vector2 vectorA = new Vector2(regionA.RegionLocX, regionA.RegionLocY);
                Vector2 vectorB = new Vector2(regionB.RegionLocX, regionB.RegionLocY);
                return Math.Sign(VectorDistance(m_origin, vectorA) - VectorDistance(m_origin, vectorB));
            }

            private float VectorDistance(Vector2 x, Vector2 y)
            {
                return (x - y).Length();
            }
        }
    }
}
