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

using System.Collections.Generic;
using System.Linq;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Services.DataService
{
    public class LocalRegionInfoConnector : IRegionInfoConnector
    {
        private IGenericData GD = null;
        private IRegistryCore m_registry = null;
        private const string m_regionSettingsRealm = "regionsettings";

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore reg, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("RegionInfoConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;
                m_registry = reg;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase (defaultConnectionString, "Generics", source.Configs["AuroraConnectors"].GetBoolean ("ValidateTables", true));
                GD.ConnectToDatabase (defaultConnectionString, "RegionInfo", source.Configs["AuroraConnectors"].GetBoolean ("ValidateTables", true));
                
                DataManager.DataManager.RegisterPlugin(this);
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
            RegionInfo oldRegion = GetRegionInfo(region.RegionID);
            if (oldRegion != null)
            {
                m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("RegionInfoChanged", new[] 
                {
                    oldRegion,
                    region
                });
            }
            Dictionary<string, object> row = new Dictionary<string, object>(4);
            row["RegionID"] = region.RegionID;
            row["RegionName"] = region.RegionName.MySqlEscape(50);
            row["RegionInfo"] = OSDParser.SerializeJsonString(region.PackRegionInfoData(true));
            row["DisableD"] = region.Disabled ? 1 : 0;
            GD.Replace("simulator", row);
        }

        public void Delete(RegionInfo region)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["RegionID"] = region.RegionID;
            GD.Delete("simulator", filter);
        }

        public RegionInfo[] GetRegionInfos(bool nonDisabledOnly)
        {
            QueryFilter filter = new QueryFilter();
            if (nonDisabledOnly)
            {
                filter.andFilters["Disabled"] = 0;
            }
                
            List<string> RetVal = GD.Query(new[] { "RegionInfo" }, "simulator", filter, null, null, null);

            if (RetVal.Count == 0)
            {
                return new RegionInfo[]{};
            }

            List<RegionInfo> Infos = new List<RegionInfo>();
            foreach (string t in RetVal)
            {
                RegionInfo replyData = new RegionInfo();
                replyData.UnpackRegionInfoData((OSDMap)OSDParser.DeserializeJson(t));
                Infos.Add(replyData);
            }
            //Sort by startup number
            Infos.Sort(RegionInfoStartupSorter);
            return Infos.ToArray();
        }

        private int RegionInfoStartupSorter(RegionInfo A, RegionInfo B)
        {
            return A.NumberStartup.CompareTo(B.NumberStartup);
        }

        public RegionInfo GetRegionInfo (UUID regionID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["RegionID"] = regionID;
            List<string> RetVal = GD.Query(new[] { "RegionInfo" }, "simulator", filter, null, null, null);

            if (RetVal.Count == 0)
            {
                return null;
            }

            RegionInfo replyData = new RegionInfo();
            replyData.UnpackRegionInfoData((OSDMap)OSDParser.DeserializeJson(RetVal[0]));
            return replyData;
        }

        public RegionInfo GetRegionInfo (string regionName)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["RegionName"] = regionName.MySqlEscape(50);
            List<string> RetVal = GD.Query(new[] { "RegionInfo" }, "simulator", filter, null, null, null);

            if (RetVal.Count == 0)
            {
                return null;
            }

            RegionInfo replyData = new RegionInfo();
            replyData.UnpackRegionInfoData((OSDMap)OSDParser.DeserializeJson(RetVal[0]));
            return replyData;
        }

        public Dictionary<float, RegionLightShareData> LoadRegionWindlightSettings(UUID regionUUID)
        {
            Dictionary<float, RegionLightShareData> RetVal = new Dictionary<float, RegionLightShareData>();
            List<RegionLightShareData> RWLDs = GenericUtils.GetGenerics<RegionLightShareData>(regionUUID, "RegionWindLightData", GD);
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

            Dictionary<string, List<string>> query = GD.QueryNames (new[] { "regionUUID" }, new object[] { regionUUID }, m_regionSettingsRealm, "*");
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
                    settings.BlockTerraform = int.Parse (query["block_terraform"][i]) == 1;
                    settings.BlockFly = int.Parse (query["block_fly"][i]) == 1;
                    settings.AllowDamage = int.Parse (query["allow_damage"][i]) == 1;
                    settings.RestrictPushing = int.Parse (query["restrict_pushing"][i]) == 1;
                    settings.AllowLandResell = int.Parse (query["allow_land_resell"][i]) == 1;
                    settings.AllowLandJoinDivide = int.Parse (query["allow_land_join_divide"][i]) == 1;
                    settings.BlockShowInSearch = int.Parse (query["block_show_in_search"][i]) == 1;
                    settings.AgentLimit = int.Parse (query["agent_limit"][i]);
                    settings.ObjectBonus = double.Parse (query["object_bonus"][i]);
                    settings.Maturity = int.Parse (query["maturity"][i]);
                    settings.DisableScripts = int.Parse (query["disable_scripts"][i]) == 1;
                    settings.DisableCollisions = int.Parse (query["disable_collisions"][i]) == 1;
                    settings.DisablePhysics = int.Parse (query["disable_physics"][i]) == 1;
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
                    settings.UseEstateSun = int.Parse (query["use_estate_sun"][i]) == 1;
                    settings.FixedSun = int.Parse (query["fixed_sun"][i]) == 1;
                    settings.SunPosition = double.Parse (query["sun_position"][i]);
                    settings.Covenant = UUID.Parse (query["covenant"][i]);
                    if(query.ContainsKey("Sandbox"))
                        if(query["Sandbox"][i] != null)
                            settings.Sandbox = int.Parse (query["Sandbox"][i]) == 1;
                    settings.SunVector = new Vector3 (float.Parse (query["sunvectorx"][i]),
                        float.Parse (query["sunvectory"][i]),
                        float.Parse (query["sunvectorz"][i]));
                    settings.LoadedCreationID = query["loaded_creation_id"][i];
                    settings.LoadedCreationDateTime = int.Parse (query["loaded_creation_datetime"][i]);
                    settings.TerrainMapImageID = UUID.Parse (query["map_tile_ID"][i]);
                    settings.TerrainImageID = UUID.Parse (query["terrain_tile_ID"][i]);
                    if (query["minimum_age"][i] != null)
                        settings.MinimumAge = int.Parse (query["minimum_age"][i]);
                    if(query["covenantlastupdated"][i] != null)
                        settings.CovenantLastUpdated = int.Parse (query["covenantlastupdated"][i]);
                    if (query.ContainsKey ("generic") && query["generic"].Count > i && query["generic"][i] != null)
                    {
                        OSD o = OSDParser.DeserializeJson (query["generic"][i]);
                        if (o.Type == OSDType.Map)
                            settings.Generic = (OSDMap)o;
                    }
                    if (query["terrainmaplastregenerated"][i] != null)
                        settings.TerrainMapLastRegenerated = Util.ToDateTime(int.Parse(query["terrainmaplastregenerated"][i]));
                }
            }
            settings.OnSave += StoreRegionSettings;
            return settings;
        }

        public void StoreRegionSettings (RegionSettings rs)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["regionUUID"] = rs.RegionUUID;
            //Delete the original
            GD.Delete (m_regionSettingsRealm, filter);
            //Now replace with the new
            GD.Insert (m_regionSettingsRealm, new object[] {
                rs.RegionUUID,
                rs.BlockTerraform ? 1 : 0,
                rs.BlockFly ? 1 : 0,
                rs.AllowDamage ? 1 : 0,
                rs.RestrictPushing ? 1 : 0,
                rs.AllowLandResell ? 1 : 0,
                rs.AllowLandJoinDivide ? 1 : 0,
                rs.BlockShowInSearch ? 1 : 0,
                rs.AgentLimit, rs.ObjectBonus,
                rs.Maturity,
                rs.DisableScripts ? 1 : 0,
                rs.DisableCollisions ? 1 : 0,
                rs.DisablePhysics ? 1 : 0,
                rs.TerrainTexture1,
                rs.TerrainTexture2,
                rs.TerrainTexture3,
                rs.TerrainTexture4,
                rs.Elevation1NW,
                rs.Elevation2NW,
                rs.Elevation1NE,
                rs.Elevation2NE,
                rs.Elevation1SE,
                rs.Elevation2SE,
                rs.Elevation1SW,
                rs.Elevation2SW,
                rs.WaterHeight,
                rs.TerrainRaiseLimit,
                rs.TerrainLowerLimit,
                rs.UseEstateSun ? 1 : 0,
                rs.FixedSun ? 1 : 0,
                rs.SunPosition,
                rs.Covenant,
                rs.Sandbox ? 1 : 0,
                rs.SunVector.X,
                rs.SunVector.Y,
                rs.SunVector.Z,
                (rs.LoadedCreationID ?? ""),
                rs.LoadedCreationDateTime,
                rs.TerrainMapImageID,
                rs.TerrainImageID,
                rs.MinimumAge,
                rs.CovenantLastUpdated,
                OSDParser.SerializeJsonString(rs.Generic),
                Util.ToUnixTime(rs.TerrainMapLastRegenerated)
            });
        }

        #endregion
    }
}
