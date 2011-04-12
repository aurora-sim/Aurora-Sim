using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using Nini.Config;
using log4net;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class LocalSimulationConnector //: IAgentInfoConnector
    {
        //private static readonly ILog m_log =
        //        LogManager.GetLogger(
        //        MethodBase.GetCurrentMethod().DeclaringType);
		private IGenericData GD = null;
        private string m_regionSettingsRealm = "regionsettings";
        private string m_terrainRealm = "terrain";

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if(source.Configs["AuroraConnectors"].GetString("UserInfoConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                {
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);
                }
                GD.ConnectToDatabase(connectionString, "Simulation", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                //DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "ISimulationConnector"; }
        }

        public void Dispose()
        {
        }

        public RegionSettings LoadRegionSettings (UUID regionUUID)
        {
            RegionSettings settings = new RegionSettings ();

            Dictionary<string, List<string>> query = GD.QueryNames (new string[1]{"regionUUID"}, new object[1]{regionUUID}, m_regionSettingsRealm, "*");
            if (query.Count == 0)
            {
                settings.RegionUUID = regionUUID;
                StoreRegionSettings (settings);
            }
            else
            {
                for (int i = 0; i < query.ElementAt (0).Value.Count; i++)
                {
                    settings.RegionUUID = UUID.Parse (query["regionUUID"][i]);
                    settings.BlockTerraform = bool.Parse (query["block_terraform"][i]);
                    settings.BlockFly = bool.Parse (query["block_fly"][i]);
                    settings.AllowDamage = bool.Parse (query["allow_damage"][i]);
                    settings.RestrictPushing = bool.Parse (query["restrict_pushing"][i]);
                    settings.AllowLandResell = bool.Parse (query["allow_land_resell"][i]);
                    settings.AllowLandJoinDivide = bool.Parse (query["allow_land_join_divide"][i]);
                    settings.BlockShowInSearch = bool.Parse (query["block_show_in_search"][i]);
                    settings.AgentLimit = int.Parse (query["agent_limit"][i]);
                    settings.ObjectBonus = double.Parse (query["object_bonus"][i]);
                    settings.Maturity = int.Parse (query["maturity"][i]);
                    settings.DisableScripts = bool.Parse (query["disable_scripts"][i]);
                    settings.DisableCollisions = bool.Parse (query["disable_collisions"][i]);
                    settings.DisablePhysics = bool.Parse (query["disable_physics"][i]);
                    settings.TerrainTexture1 = UUID.Parse (query["terrain_texture_1"][i]);
                    settings.TerrainTexture2 = UUID.Parse (query["terrain_texture_2"][i]);
                    settings.TerrainTexture3 = UUID.Parse (query["terrain_texture_3"][i]);
                    settings.TerrainTexture4 = UUID.Parse (query["terrain_texture_4"][i]);
                    settings.Elevation1NW = double.Parse (query["elevation_1_nw"][i]);
                    settings.Elevation2NW = double.Parse (query["elevation_2_nw"][i]);
                    settings.Elevation1NE = double.Parse (query["elevation_1_ne"][i]);
                    settings.Elevation2NE = double.Parse (query["elevation_2_ne"][i]);
                    settings.Elevation1SE = double.Parse (query["elevation_1_se"][i]);
                    settings.Elevation2SE = double.Parse (query["elevation_2_se"][i]);
                    settings.Elevation1SW = double.Parse (query["elevation_1_sw"][i]);
                    settings.Elevation2SW = double.Parse (query["elevation_2_sw"][i]);
                    settings.WaterHeight = double.Parse (query["water_height"][i]);
                    settings.TerrainRaiseLimit = double.Parse (query["terrain_raise_limit"][i]);
                    settings.TerrainLowerLimit = double.Parse (query["terrain_lower_limit"][i]);
                    settings.UseEstateSun = bool.Parse (query["use_estate_sun"][i]);
                    settings.FixedSun = bool.Parse (query["fixed_sun"][i]);
                    settings.SunPosition = double.Parse (query["sun_position"][i]);
                    settings.Covenant = UUID.Parse (query["covenant"][i]);
                    settings.Sandbox = bool.Parse (query["Sandbox"][i]);
                    settings.SunVector = new Vector3 (float.Parse (query["sunvectorx"][i]),
                        float.Parse (query["sunvectory"][i]),
                        float.Parse (query["sunvectorz"][i]));
                    settings.LoadedCreationID = query["loaded_creation_id"][i];
                    settings.LoadedCreationDateTime = int.Parse (query["loaded_creation_datetime"][i]);
                    settings.TerrainMapImageID = UUID.Parse (query["map_tile_ID"][i]);
                    settings.TerrainImageID = UUID.Parse (query["terrain_tile_ID"][i]);
                    settings.MinimumAge = int.Parse (query["minimum_age"][i]);
                    settings.CovenantLastUpdated = int.Parse (query["covenantlastupdated"][i]);
                    OSD o = OSDParser.DeserializeJson (query["generic"][i]);
                    if (o.Type == OSDType.Map)
                        settings.Generic = (OSDMap)o;
                }
            }
            settings.OnSave += StoreRegionSettings;
            return settings;
        }

        public void StoreRegionSettings (RegionSettings rs)
        {
            //Delete the original
            GD.Delete (m_regionSettingsRealm, new string[1] { "regionUUID" }, new object[1] { rs.RegionUUID });
            //Now replace with the new
            GD.Insert (m_regionSettingsRealm, new object[] { rs.RegionUUID, rs.BlockTerraform, rs.BlockFly, rs.AllowDamage,
                rs.RestrictPushing, rs.AllowLandResell, rs.AllowLandJoinDivide, rs.BlockShowInSearch, rs.AgentLimit, rs.ObjectBonus,
                rs.Maturity, rs.DisableScripts, rs.DisableCollisions, rs.DisablePhysics, rs.TerrainTexture1,
                rs.TerrainTexture2, rs.TerrainTexture3, rs.TerrainTexture4, rs.Elevation1NW, rs.Elevation2NW,
                rs.Elevation1NE, rs.Elevation2NE, rs.Elevation1SE, rs.Elevation2SE, rs.Elevation1SW, rs.Elevation2SW,
                rs.WaterHeight, rs.TerrainRaiseLimit, rs.TerrainLowerLimit, rs.UseEstateSun, rs.FixedSun, rs.SunPosition,
                rs.Covenant, rs.Sandbox, rs.SunVector.X, rs.SunVector.Y, rs.SunVector.Z, rs.LoadedCreationID, rs.LoadedCreationDateTime,
                rs.TerrainMapImageID, rs.TerrainImageID, rs.MinimumAge, rs.CovenantLastUpdated, OSDParser.SerializeJsonString(rs.Generic)});
        }

        public void StoreTerrain (short[] ter, UUID regionID, bool Revert)
        {
            //Remove the old terrain
            GD.Delete (m_terrainRealm, new string[1] { "RegionUUID" }, new object[1] { regionID });

            byte[] heightmap = new byte[ter.Length * sizeof (short)];
            int ii = 0;
            for (int i = 0; i < ter.Length; i++)
            {
                Utils.Int16ToBytes (ter[i], heightmap, ii);
                ii += 2;
            }
            GD.Insert (m_terrainRealm, new object[5] { regionID, heightmap, Revert ? 2 : 1, 0, 0 });
        }

        public void StoreWater (short[] water, UUID regionID, bool Revert)
        {
            //Remove the old terrain
            GD.Delete (m_terrainRealm, new string[1] { "RegionUUID" }, new object[1] { regionID });

            byte[] heightmap = new byte[water.Length * sizeof (short)];
            int ii = 0;
            for (int i = 0; i < water.Length; i++)
            {
                Utils.Int16ToBytes (water[i], heightmap, ii);
                ii += 2;
            }
            GD.Insert (m_terrainRealm, new object[5] { regionID, heightmap, Revert ? 4 : 3, 0, 0 });
        }

        public short[] LoadTerrain (UUID regionID, bool Revert, int RegionSizeX, int RegionSizeY)
        {
            using (IDataReader reader = GD.QueryData (string.Format ("where RegionUUID = {0} and Revert = {1} order by Revision desc limit 1", regionID.ToString (), Revert.ToString ()), m_terrainRealm, "Heightfield,X,Y"))
            {
                while (reader.Read ())
                {
                    if (reader["X"].ToString () == "-1")
                    {
                        byte[] heightmap = (byte[])reader["Heightfield"];
                        short[] map = new short[RegionSizeX * RegionSizeX];
                        double[,] terrain = null;
                        terrain = new double[RegionSizeX, RegionSizeY];
                        terrain.Initialize ();

                        using (MemoryStream str = new MemoryStream (heightmap))
                        {
                            using (BinaryReader br = new BinaryReader (str))
                            {
                                for (int x = 0; x < RegionSizeX; x++)
                                {
                                    for (int y = 0; y < RegionSizeY; y++)
                                    {
                                        terrain[x, y] = br.ReadDouble ();
                                    }
                                }
                            }
                        }
                        for (int x = 0; x < RegionSizeX; x++)
                        {
                            for (int y = 0; y < RegionSizeY; y++)
                            {
                                map[y * RegionSizeX + x] = (short)(terrain[x, y] * Constants.TerrainCompression);
                            }
                        }
                        this.StoreTerrain (map, regionID, Revert);
                        return map;
                    }
                    else
                    {
                        byte[] heightmap = (byte[])reader["Heightfield"];
                        short[] map = new short[RegionSizeX * RegionSizeX];
                        int ii = 0;
                        for (int i = 0; i < heightmap.Length; i += sizeof (short))
                        {
                            map[ii] = Utils.BytesToInt16 (heightmap, i);
                            ii++;
                        }
                        heightmap = null;
                        return map;
                    }
                }
            }
            return null;
        }

        public short[] LoadWater(UUID regionID, bool Revert, int RegionSizeX, int RegionSizeY)
        {
            int r = Revert ? 3 : 2; //Use numbers so that we can coexist with terrain
            using (IDataReader reader = GD.QueryData (string.Format ("where RegionUUID = {0} and Revert = {1} order by Revision desc limit 1", regionID.ToString (), r.ToString ()), m_terrainRealm, "Heightfield,X,Y"))
            {
                while (reader.Read ())
                {
                    if (reader["X"].ToString () == "-1")
                    {
                        byte[] heightmap = (byte[])reader["Heightfield"];
                        short[] map = new short[RegionSizeX * RegionSizeX];
                        double[,] terrain = null;
                        terrain = new double[RegionSizeX, RegionSizeY];
                        terrain.Initialize ();

                        using (MemoryStream str = new MemoryStream (heightmap))
                        {
                            using (BinaryReader br = new BinaryReader (str))
                            {
                                for (int x = 0; x < RegionSizeX; x++)
                                {
                                    for (int y = 0; y < RegionSizeY; y++)
                                    {
                                        terrain[x, y] = br.ReadDouble ();
                                    }
                                }
                            }
                        }
                        for (int x = 0; x < RegionSizeX; x++)
                        {
                            for (int y = 0; y < RegionSizeY; y++)
                            {
                                map[y * RegionSizeX + x] = (short)(terrain[x, y] * Constants.TerrainCompression);
                            }
                        }
                        this.StoreTerrain (map, regionID, Revert);
                        return map;
                    }
                    else
                    {
                        byte[] heightmap = (byte[])reader["Heightfield"];
                        short[] map = new short[RegionSizeX * RegionSizeX];
                        int ii = 0;
                        for (int i = 0; i < heightmap.Length; i += sizeof (short))
                        {
                            map[ii] = Utils.BytesToInt16 (heightmap, i);
                            ii++;
                        }
                        heightmap = null;
                        return map;
                    }
                }
            }
            return null;
        }
    }
}
