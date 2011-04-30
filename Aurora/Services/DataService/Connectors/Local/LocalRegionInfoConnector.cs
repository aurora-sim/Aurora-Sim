using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Services.DataService
{
    public class LocalRegionInfoConnector : IRegionInfoConnector
    {
        private IGenericData GD = null;
        private string m_regionSettingsRealm = "regionsettings";

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("RegionInfoConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString, "RegionInfo", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IRegionInfoConnector"; }
        }

        public void Dispose()
        {
        }

        public void UpdateRegionInfo(RegionInfo region)
        {
            List<object> Values = new List<object>();
            Values.Add(region.RegionID);
            Values.Add(region.RegionName);
            Values.Add(OSDParser.SerializeJsonString(region.PackRegionInfoData(true)));
            Values.Add(region.Disabled ? 1 : 0);
            GD.Replace("simulator", new string[]{"RegionID","RegionName",
                "RegionInfo","Disabled"}, Values.ToArray());
        }

        public void Delete(RegionInfo region)
        {
            GD.Delete("simulator", new string[] { "RegionID" }, new object[] { region.RegionID });
        }

        public RegionInfo[] GetRegionInfos(bool nonDisabledOnly)
        {
            List<RegionInfo> Infos = new List<RegionInfo>();
            List<string> RetVal = nonDisabledOnly ?
                GD.Query("Disabled", 0, "simulator", "RegionInfo") :
                GD.Query("", "", "simulator", "RegionInfo");
            if (RetVal.Count == 0)
                return Infos.ToArray();
            RegionInfo replyData = new RegionInfo();
            for (int i = 0; i < RetVal.Count; i++)
            {
                replyData.UnpackRegionInfoData((OSDMap)OSDParser.DeserializeJson(RetVal[i]));
                if (replyData.ExternalHostName == "DEFAULT" || replyData.FindExternalAutomatically)
                {
                    replyData.ExternalHostName = Aurora.Framework.Utilities.GetExternalIp();
                }
                else
                    replyData.ExternalHostName = Util.ResolveEndPoint(replyData.ExternalHostName, replyData.InternalEndPoint.Port).Address.ToString();
                Infos.Add(replyData);
                replyData = new RegionInfo();
            }
            //Sort by startup number
            Infos.Sort(RegionInfoStartupSorter);
            return Infos.ToArray();
        }

        private int RegionInfoStartupSorter(RegionInfo A, RegionInfo B)
        {
            return A.NumberStartup.CompareTo(B.NumberStartup);
        }

        public RegionInfo GetRegionInfo(UUID regionID)
        {
            List<string> RetVal = GD.Query("RegionID", regionID, "simulator", "RegionInfo");
            RegionInfo replyData = new RegionInfo();
            if (RetVal.Count == 0)
                return null;
            replyData.UnpackRegionInfoData((OSDMap)OSDParser.DeserializeJson(RetVal[0]));
            if (replyData.ExternalHostName == "DEFAULT" || replyData.FindExternalAutomatically)
            {
                replyData.ExternalHostName = Aurora.Framework.Utilities.GetExternalIp();
            }
            else
                replyData.ExternalHostName = Util.ResolveEndPoint(replyData.ExternalHostName, replyData.InternalEndPoint.Port).Address.ToString();
            return replyData;
        }

        public RegionInfo GetRegionInfo(string regionName)
        {
            List<string> RetVal = GD.Query("RegionName", regionName, "simulator", "RegionInfo");
            RegionInfo replyData = new RegionInfo();
            if (RetVal.Count == 0)
                return null;
            replyData.UnpackRegionInfoData((OSDMap)OSDParser.DeserializeJson(RetVal[0]));
            if (replyData.ExternalHostName == "DEFAULT" || replyData.FindExternalAutomatically)
            {
                replyData.ExternalHostName = Aurora.Framework.Utilities.GetExternalIp();
            }
            else
                replyData.ExternalHostName = Util.ResolveEndPoint(replyData.ExternalHostName, replyData.InternalEndPoint.Port).Address.ToString();
            return replyData;
        }

        public Dictionary<float, RegionLightShareData> LoadRegionWindlightSettings(UUID regionUUID)
        {
            Dictionary<float, RegionLightShareData> RetVal = new Dictionary<float, RegionLightShareData>();
            List<RegionLightShareData> RWLDs = new List<RegionLightShareData>();
            RegionLightShareData RWLD = new RegionLightShareData();
            RWLDs = GenericUtils.GetGenerics<RegionLightShareData>(regionUUID, "RegionWindLightData", GD, RWLD);
            foreach (RegionLightShareData lsd in RWLDs)
            {
                if(!RetVal.ContainsKey(lsd.minEffectiveAltitude))
                    RetVal.Add(lsd.minEffectiveAltitude, lsd);
            }
            return RetVal;
        }

        public void StoreRegionWindlightSettings(UUID RegionID, UUID ID, RegionLightShareData map)
        {
            GenericUtils.AddGeneric(RegionID, "RegionWindLightData", ID.ToString(), map.ToOSD(), GD);
        }

        #region Region Settings

        public RegionSettings LoadRegionSettings (UUID regionUUID)
        {
            RegionSettings settings = new RegionSettings ();

            Dictionary<string, List<string>> query = GD.QueryNames (new string[1] { "regionUUID" }, new object[1] { regionUUID }, m_regionSettingsRealm, "*");
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

        #endregion
    }
}
