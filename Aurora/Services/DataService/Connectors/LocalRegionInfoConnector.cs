using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Services.DataService
{
    public class LocalRegionInfoConnector : IRegionInfoConnector, IAuroraDataPlugin
	{
        private IGenericData GD = null;

        public void Initialise(IGenericData GenericData, IConfigSource source)
        {
            if (source.Configs["AuroraConnectors"].GetString("RegionInfoConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;
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

        public void UpdateRegionInfo(RegionInfo region, bool Disable)
        {
            List<object> Values = new List<object>();
            if (GetRegionInfo(region.RegionID) != null)
            {
                Values.Add(region.RegionName);
                Values.Add(region.RegionLocX);
                Values.Add(region.RegionLocY);
                Values.Add(region.InternalEndPoint.Address);
                Values.Add(region.InternalEndPoint.Port);
                if (region.FindExternalAutomatically)
                {
                    Values.Add("DEFAULT");
                }
                else
                {
                    Values.Add(region.ExternalHostName);
                }
                Values.Add(region.RegionType);
                Values.Add(region.NonphysPrimMax);
                Values.Add(region.PhysPrimMax);
                Values.Add(region.ClampPrimSize);
                Values.Add(region.ObjectCapacity);
                Values.Add(region.AccessLevel);
                Values.Add(Disable);
                GD.Update("simulator", Values.ToArray(), new string[]{"RegionName","RegionLocX",
                "RegionLocY","InternalIP","Port","ExternalIP","RegionType","NonphysicalPrimMax",
                "PhysicalPrimMax","ClampPrimSize","MaxPrims","AccessLevel","Disabled"},
                    new string[] { "RegionID" }, new object[] { region.RegionID });
            }
            else
            {
                Values.Add(region.RegionID);
                Values.Add(region.RegionName);
                Values.Add(region.RegionLocX);
                Values.Add(region.RegionLocY);
                Values.Add(region.InternalEndPoint.Address);
                Values.Add(region.InternalEndPoint.Port);
                if (region.FindExternalAutomatically)
                {
                    Values.Add("DEFAULT");
                }
                else
                {
                    Values.Add(region.ExternalHostName);
                }
                Values.Add(region.RegionType);
                Values.Add(region.NonphysPrimMax);
                Values.Add(region.PhysPrimMax);
                Values.Add(region.ClampPrimSize);
                Values.Add(region.ObjectCapacity);
                Values.Add(0);
                Values.Add(false);
                Values.Add(false);
                Values.Add(region.AccessLevel);
                Values.Add(Disable);
                GD.Insert("simulator", Values.ToArray());
            }
        }

		public RegionInfo[] GetRegionInfos()
		{
            List<RegionInfo> Infos = new List<RegionInfo>();
            List<string> RetVal = GD.Query("Disabled", false, "simulator", "*");
            if (RetVal.Count == 0)
                return Infos.ToArray();
            int DataCount = 0;
            RegionInfo replyData = new RegionInfo();
            for (int i = 0; i < RetVal.Count; i++)
            {
                if (DataCount == 0)
                    replyData.RegionID = new UUID(RetVal[i]);
                if (DataCount == 1)
                    replyData.RegionName =RetVal[i];
                if (DataCount == 2)
                    replyData.RegionLocX = uint.Parse(RetVal[i]);
                if (DataCount == 3)
                    replyData.RegionLocY = uint.Parse(RetVal[i]);
                if (DataCount == 6)
                    replyData.ExternalHostName = RetVal[i];
                if (DataCount == 7)
                    replyData.RegionType = RetVal[i];
                if (DataCount == 8)
                    replyData.NonphysPrimMax = Convert.ToInt32(RetVal[i]);
                if (DataCount == 9)
                    replyData.PhysPrimMax = Convert.ToInt32(RetVal[i]);
                if (DataCount == 10)
                    replyData.ClampPrimSize = Convert.ToBoolean(RetVal[i]);
                if (DataCount == 11)
                    replyData.ObjectCapacity = Convert.ToInt32(RetVal[i]);
                if (DataCount == 15)
                    replyData.AccessLevel = Convert.ToByte(RetVal[i]);
                if (DataCount == 16)
                    replyData.Disabled = Convert.ToBoolean(RetVal[i]);
                DataCount++;
                
                if (DataCount == 17)
                {
                    replyData.SetEndPoint(RetVal[(i - (DataCount - 1)) + 4], int.Parse(RetVal[(i - (DataCount - 1)) + 5]));
                    if (replyData.ExternalHostName == "DEFAULT")
                    {
                        replyData.ExternalHostName = Aurora.Framework.Utils.GetExternalIp();
                    }
                    replyData.HttpPort = uint.Parse(RetVal[(i - (DataCount - 1)) + 5]);
                    DataCount = 0;
                    Infos.Add(replyData);
                    replyData = new RegionInfo();
                }
            }
            return Infos.ToArray();
		}

        public RegionInfo GetRegionInfo(UUID regionID)
        {
            List<string> RetVal = GD.Query("RegionID", regionID, "simulator", "*");
            RegionInfo replyData = new RegionInfo();
            if (RetVal.Count == 0)
                return null;
            for (int i = 0; i < RetVal.Count; i++)
            {
                if (i == 0)
                    replyData.RegionID = new UUID(RetVal[i]);
                if (i == 1)
                    replyData.RegionName = RetVal[i];
                if (i == 2)
                    replyData.RegionLocX = uint.Parse(RetVal[i]);
                if (i == 3)
                    replyData.RegionLocY = uint.Parse(RetVal[i]);
                if (i == 6)
                    replyData.ExternalHostName = RetVal[i];
                if (i == 7)
                    replyData.RegionType = RetVal[i];
                if (i == 8)
                    replyData.NonphysPrimMax = Convert.ToInt32(RetVal[i]);
                if (i == 9)
                    replyData.PhysPrimMax = Convert.ToInt32(RetVal[i]);
                if (i == 10)
                    replyData.ClampPrimSize = Convert.ToBoolean(RetVal[i]);
                if (i == 11)
                    replyData.ObjectCapacity = Convert.ToInt32(RetVal[i]);
                if (i == 15)
                    replyData.AccessLevel = Convert.ToByte(RetVal[i]);
                if (i == 16)
                {
                    replyData.Disabled = Convert.ToBoolean(RetVal[i]);
                    replyData.SetEndPoint(RetVal[4], int.Parse(RetVal[5]));
                    if (replyData.ExternalHostName == "DEFAULT")
                    {
                        replyData.ExternalHostName = Aurora.Framework.Utils.GetExternalIp();
                    }
                    replyData.HttpPort = uint.Parse(RetVal[5]);
                }
            }
            return replyData;
        }

        public RegionInfo GetRegionInfo(string regionName)
        {
            List<string> RetVal = GD.Query("RegionName", regionName, "simulator", "*");
            RegionInfo replyData = new RegionInfo();
            if (RetVal.Count == 0)
                return null;
            int i = 0;
            for (i = 0; i < RetVal.Count; i++)
            {
                if (i == 0)
                    replyData.RegionID = new UUID(RetVal[i]);
                if (i == 1)
                    replyData.RegionName = RetVal[i];
                if (i == 2)
                    replyData.RegionLocX = uint.Parse(RetVal[i]);
                if (i == 3)
                    replyData.RegionLocY = uint.Parse(RetVal[i]);
                if (i == 6)
                    replyData.ExternalHostName = RetVal[i];
                if (i == 7)
                    replyData.RegionType = RetVal[i];
                if (i == 8)
                    replyData.NonphysPrimMax = Convert.ToInt32(RetVal[i]);
                if (i == 9)
                    replyData.PhysPrimMax = Convert.ToInt32(RetVal[i]);
                if (i == 10)
                    replyData.ClampPrimSize = Convert.ToBoolean(RetVal[i]);
                if (i == 11)
                    replyData.ObjectCapacity = Convert.ToInt32(RetVal[i]);
                if (i == 15)
                    replyData.AccessLevel = Convert.ToByte(RetVal[i]);
                if (i == 16)
                {
                    replyData.Disabled = Convert.ToBoolean(RetVal[i]);
                    replyData.SetEndPoint(RetVal[4], int.Parse(RetVal[5]));
                    if (replyData.ExternalHostName == "DEFAULT")
                    {
                        replyData.ExternalHostName = Aurora.Framework.Utils.GetExternalIp();
                    }
                    replyData.HttpPort = uint.Parse(RetVal[5]);
                }
            }
            return replyData;
        }

        public RegionLightShareData LoadRegionWindlightSettings(UUID regionUUID)
        {
            RegionLightShareData nWP = new RegionLightShareData();
            nWP.OnSave += StoreRegionWindlightSettings;

            List<string> results = GD.Query("region_id", regionUUID, "regionwindlight", "*");
            nWP.regionID = regionUUID;

            if (results.Count == 0)
            {
                StoreRegionWindlightSettings(nWP);
                return nWP;
            }

            UUID.TryParse(results[0].ToString(), out nWP.regionID);
            nWP.waterColor.X = Convert.ToSingle(results[1]);
            nWP.waterColor.Y = Convert.ToSingle(results[2]);
            nWP.waterColor.Z = Convert.ToSingle(results[3]);
            nWP.waterFogDensityExponent = Convert.ToSingle(results[4]);
            nWP.underwaterFogModifier = Convert.ToSingle(results[5]);
            nWP.reflectionWaveletScale.X = Convert.ToSingle(results[6]);
            nWP.reflectionWaveletScale.Y = Convert.ToSingle(results[7]);
            nWP.reflectionWaveletScale.Z = Convert.ToSingle(results[8]);
            nWP.fresnelScale = Convert.ToSingle(results[9]);
            nWP.fresnelOffset = Convert.ToSingle(results[10]);
            nWP.refractScaleAbove = Convert.ToSingle(results[11]);
            nWP.refractScaleBelow = Convert.ToSingle(results[12]);
            nWP.blurMultiplier = Convert.ToSingle(results[13]);
            nWP.bigWaveDirection.X = Convert.ToSingle(results[14]);
            nWP.bigWaveDirection.Y = Convert.ToSingle(results[15]);
            nWP.littleWaveDirection.X = Convert.ToSingle(results[16]);
            nWP.littleWaveDirection.Y = Convert.ToSingle(results[17]);
            UUID.TryParse(results[18].ToString(), out nWP.normalMapTexture);
            nWP.horizon.X = Convert.ToSingle(results[19]);
            nWP.horizon.Y = Convert.ToSingle(results[20]);
            nWP.horizon.Z = Convert.ToSingle(results[21]);
            nWP.horizon.W = Convert.ToSingle(results[22]);
            nWP.hazeHorizon = Convert.ToSingle(results[23]);
            nWP.blueDensity.X = Convert.ToSingle(results[24]);
            nWP.blueDensity.Y = Convert.ToSingle(results[25]);
            nWP.blueDensity.Z = Convert.ToSingle(results[26]);
            nWP.blueDensity.W = Convert.ToSingle(results[27]);
            nWP.hazeDensity = Convert.ToSingle(results[28]);
            nWP.densityMultiplier = Convert.ToSingle(results[29]);
            nWP.distanceMultiplier = Convert.ToSingle(results[30]);
            nWP.maxAltitude = Convert.ToUInt16(results[31]);
            nWP.sunMoonColor.X = Convert.ToSingle(results[32]);
            nWP.sunMoonColor.Y = Convert.ToSingle(results[33]);
            nWP.sunMoonColor.Z = Convert.ToSingle(results[34]);
            nWP.sunMoonColor.W = Convert.ToSingle(results[35]);
            nWP.sunMoonPosition = Convert.ToSingle(results[36]);
            nWP.ambient.X = Convert.ToSingle(results[37]);
            nWP.ambient.Y = Convert.ToSingle(results[38]);
            nWP.ambient.Z = Convert.ToSingle(results[39]);
            nWP.ambient.W = Convert.ToSingle(results[40]);
            nWP.eastAngle = Convert.ToSingle(results[41]);
            nWP.sunGlowFocus = Convert.ToSingle(results[42]);
            nWP.sunGlowSize = Convert.ToSingle(results[43]);
            nWP.sceneGamma = Convert.ToSingle(results[44]);
            nWP.starBrightness = Convert.ToSingle(results[45]);
            nWP.cloudColor.X = Convert.ToSingle(results[46]);
            nWP.cloudColor.Y = Convert.ToSingle(results[47]);
            nWP.cloudColor.Z = Convert.ToSingle(results[48]);
            nWP.cloudColor.W = Convert.ToSingle(results[49]);
            nWP.cloudXYDensity.X = Convert.ToSingle(results[50]);
            nWP.cloudXYDensity.Y = Convert.ToSingle(results[51]);
            nWP.cloudXYDensity.Z = Convert.ToSingle(results[52]);
            nWP.cloudCoverage = Convert.ToSingle(results[53]);
            nWP.cloudScale = Convert.ToSingle(results[54]);
            nWP.cloudDetailXYDensity.X = Convert.ToSingle(results[55]);
            nWP.cloudDetailXYDensity.Y = Convert.ToSingle(results[56]);
            nWP.cloudDetailXYDensity.Z = Convert.ToSingle(results[57]);
            nWP.cloudScrollX = Convert.ToSingle(results[58]);
            nWP.cloudScrollXLock = Convert.ToBoolean(results[59]);
            nWP.cloudScrollY = Convert.ToSingle(results[60]);
            nWP.cloudScrollYLock = Convert.ToBoolean(results[61]);
            nWP.drawClassicClouds = Convert.ToBoolean(results[62]);

            return nWP;
        }

        public void StoreRegionWindlightSettings(RegionLightShareData wl)
        {
            try
            {
                GD.Delete("regionwindlight", new string[] { "region_id" }, new object[] { wl.regionID });
            }
            catch (Exception) { }

            List<string> Keys = new List<string>();
            Keys.Add("region_id");
            Keys.Add("water_color_r");
            Keys.Add("water_color_g");
            Keys.Add("water_color_b");
            Keys.Add("water_fog_density_exponent");
            Keys.Add("underwater_fog_modifier");
            Keys.Add("reflection_wavelet_scale_1");
            Keys.Add("reflection_wavelet_scale_2");
            Keys.Add("reflection_wavelet_scale_3");
            Keys.Add("fresnel_scale");
            Keys.Add("fresnel_offset");
            Keys.Add("refract_scale_above");
            Keys.Add("refract_scale_below");
            Keys.Add("blur_multiplier");
            Keys.Add("big_wave_direction_x");
            Keys.Add("big_wave_direction_y");
            Keys.Add("little_wave_direction_x");
            Keys.Add("little_wave_direction_y");
            Keys.Add("normal_map_texture");
            Keys.Add("horizon_r");
            Keys.Add("horizon_g");
            Keys.Add("horizon_b");
            Keys.Add("horizon_i");
            Keys.Add("haze_horizon");
            Keys.Add("blue_density_r");
            Keys.Add("blue_density_g");
            Keys.Add("blue_density_b");
            Keys.Add("blue_density_i");
            Keys.Add("haze_density");
            Keys.Add("density_multiplier");
            Keys.Add("distance_multiplier");
            Keys.Add("max_altitude");
            Keys.Add("sun_moon_color_r");
            Keys.Add("sun_moon_color_g");
            Keys.Add("sun_moon_color_b");
            Keys.Add("sun_moon_color_i");
            Keys.Add("sun_moon_position");
            Keys.Add("ambient_r");
            Keys.Add("ambient_g");
            Keys.Add("ambient_b");
            Keys.Add("ambient_i");
            Keys.Add("east_angle");
            Keys.Add("sun_glow_focus");
            Keys.Add("sun_glow_size");
            Keys.Add("scene_gamma");
            Keys.Add("star_brightness");
            Keys.Add("cloud_color_r");
            Keys.Add("cloud_color_g");
            Keys.Add("cloud_color_b");
            Keys.Add("cloud_color_i");
            Keys.Add("cloud_x");
            Keys.Add("cloud_y");
            Keys.Add("cloud_density");
            Keys.Add("cloud_coverage");
            Keys.Add("cloud_scale");
            Keys.Add("cloud_detail_x");
            Keys.Add("cloud_detail_y");
            Keys.Add("cloud_detail_density");
            Keys.Add("cloud_scroll_x");
            Keys.Add("cloud_scroll_x_lock");
            Keys.Add("cloud_scroll_y");
            Keys.Add("cloud_scroll_y_lock");
            Keys.Add("draw_classic_clouds");

            List<object> Values = new List<object>();
            Values.Add(wl.regionID);
            Values.Add(wl.waterColor.X);
            Values.Add(wl.waterColor.Y);
            Values.Add(wl.waterColor.Z);
            Values.Add(wl.waterFogDensityExponent);
            Values.Add(wl.underwaterFogModifier);
            Values.Add(wl.reflectionWaveletScale.X);
            Values.Add(wl.reflectionWaveletScale.Y);
            Values.Add(wl.reflectionWaveletScale.Z);
            Values.Add(wl.fresnelScale);
            Values.Add(wl.fresnelOffset);
            Values.Add(wl.refractScaleAbove);
            Values.Add(wl.refractScaleBelow);
            Values.Add(wl.bigWaveDirection.X);
            Values.Add(wl.bigWaveDirection.Y);
            Values.Add(wl.littleWaveDirection.X);
            Values.Add(wl.littleWaveDirection.Y);
            Values.Add(wl.normalMapTexture);
            Values.Add(wl.horizon.X);
            Values.Add(wl.horizon.Y);
            Values.Add(wl.horizon.Z);
            Values.Add(wl.horizon.W);
            Values.Add(wl.hazeHorizon);
            Values.Add(wl.blueDensity.X);
            Values.Add(wl.blueDensity.Y);
            Values.Add(wl.blueDensity.Z);
            Values.Add(wl.blueDensity.W);
            Values.Add(wl.densityMultiplier);
            Values.Add(wl.distanceMultiplier);
            Values.Add(wl.maxAltitude);
            Values.Add(wl.sunMoonColor.X);
            Values.Add(wl.sunMoonColor.Y);
            Values.Add(wl.sunMoonColor.Z);
            Values.Add(wl.sunMoonColor.W);
            Values.Add(wl.sunMoonPosition);
            Values.Add(wl.ambient.X);
            Values.Add(wl.ambient.Y);
            Values.Add(wl.ambient.Z);
            Values.Add(wl.ambient.W);
            Values.Add(wl.eastAngle);
            Values.Add(wl.sunGlowFocus);
            Values.Add(wl.sunGlowSize);
            Values.Add(wl.sceneGamma);
            Values.Add(wl.starBrightness);
            Values.Add(wl.cloudColor.X);
            Values.Add(wl.cloudColor.Y);
            Values.Add(wl.cloudColor.Z);
            Values.Add(wl.cloudColor.W);
            Values.Add(wl.cloudXYDensity.X);
            Values.Add(wl.cloudXYDensity.Y);
            Values.Add(wl.cloudXYDensity.Z);
            Values.Add(wl.cloudCoverage);
            Values.Add(wl.cloudScale);
            Values.Add(wl.cloudDetailXYDensity.X);
            Values.Add(wl.cloudDetailXYDensity.Y);
            Values.Add(wl.cloudDetailXYDensity.Z);
            Values.Add(wl.cloudScrollX);
            Values.Add(wl.cloudScrollXLock);
            Values.Add(wl.cloudScrollY);
            Values.Add(wl.cloudScrollYLock);
            Values.Add(wl.drawClassicClouds);

            GD.Insert("regionwindlight", Keys.ToArray(), Values.ToArray());
        }
    }
}
