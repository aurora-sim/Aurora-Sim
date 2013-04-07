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
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework.ClientInterfaces
{
    public class WindlightDayCycle
    {
        public UUID RegionID;
        public DayCycle Cycle;
        public WaterData Water;

        public class DayCycle
        {
            public bool IsStaticDayCycle = false;
            public Dictionary<string, SkyData> DataSettings = new Dictionary<string, SkyData>();

            public void FromOSD(OSDArray osd)
            {
                OSDArray array = osd as OSDArray;
                OSDArray settingsArray = array[1] as OSDArray;
                OSDMap windlightSettingsArray = array[2] as OSDMap;
                foreach (OSD setting in settingsArray)
                {
                    OSDArray innerSetting = setting as OSDArray;
                    string key = innerSetting[0];
                    string name = innerSetting[1];

                    OSDMap settings = windlightSettingsArray[name] as OSDMap;

                    SkyData skySettings = new SkyData();
                    skySettings.FromOSD(name, settings);
                    DataSettings[key] = skySettings;
                }

                if (DataSettings.Count == 1 && DataSettings.ContainsKey("-1"))
                    IsStaticDayCycle = true;
            }

            public OSDArray ToOSD(ref OSDArray array)
            {
                OSDMap settings = new OSDMap();
                OSDArray cycle = new OSDArray();
                foreach (KeyValuePair<string, SkyData> kvp in DataSettings)
                {
                    cycle.Add(new OSDArray {kvp.Key, kvp.Value.preset_name});
                    settings[kvp.Value.preset_name] = kvp.Value.ToOSD();
                }

                array[1] = cycle;
                array[2] = settings;

                return array;
            }
        }

        public class SkyData
        {
            public Vector4 ambient;
            public Vector4 blue_density;
            public Vector4 blue_horizon;
            public Vector4 cloud_color;
            public Vector4 cloud_pos_density1;
            public Vector4 cloud_pos_density2;
            public Vector4 cloud_scale;
            public Vector2 cloud_scroll_rate;
            public Vector4 cloud_shadow;
            public Vector4 density_multiplier;
            public Vector4 distance_multiplier;
            public Vector2 enable_cloud_scroll;
            public Vector4 gamma;
            public Vector4 glow;
            public Vector4 haze_density;
            public Vector4 haze_horizon;
            public Vector4 lightnorm;
            public Vector4 max_y;
            public int preset_num;
            public float star_brightness;
            public Vector4 sunlight_color;
            public string preset_name;

            public void FromOSD(string preset_name, OSD osd)
            {
                OSDMap map = osd as OSDMap;

                ambient = map["ambient"];
                blue_density = map["blue_density"];
                blue_horizon = map["blue_horizon"];
                cloud_color = map["cloud_color"];
                cloud_pos_density1 = map["cloud_pos_density1"];
                cloud_pos_density2 = map["cloud_pos_density2"];
                cloud_scale = map["cloud_scale"];
                cloud_scroll_rate = map["cloud_scroll_rate"];
                cloud_shadow = map["cloud_shadow"];
                density_multiplier = map["density_multiplier"];
                distance_multiplier = map["distance_multiplier"];
                enable_cloud_scroll = map["enable_cloud_scroll"];
                gamma = map["gamma"];
                glow = map["glow"];
                haze_density = map["haze_density"];
                haze_horizon = map["haze_horizon"];
                lightnorm = map["lightnorm"];
                max_y = map["max_y"];
                preset_num = map["preset_num"];
                star_brightness = map["star_brightness"];
                sunlight_color = map["sunlight_color"];
                this.preset_name = preset_name;
            }

            public OSD ToOSD()
            {
                OSDMap map = new OSDMap();

                map["ambient"] = ambient;
                map["blue_density"] = blue_density;
                map["blue_horizon"] = blue_horizon;
                map["cloud_color"] = cloud_color;
                map["cloud_pos_density1"] = cloud_pos_density1;
                map["cloud_pos_density2"] = cloud_pos_density2;
                map["cloud_scale"] = cloud_scale;
                map["cloud_scroll_rate"] = cloud_scroll_rate;
                map["cloud_shadow"] = cloud_shadow;
                map["density_multiplier"] = density_multiplier;
                map["distance_multiplier"] = distance_multiplier;
                map["enable_cloud_scroll"] = new OSDArray {enable_cloud_scroll.X == 1, enable_cloud_scroll.Y == 1};
                map["gamma"] = gamma;
                map["glow"] = glow;
                map["haze_density"] = haze_density;
                map["haze_horizon"] = haze_horizon;
                map["lightnorm"] = lightnorm;
                map["max_y"] = max_y;
                map["preset_num"] = preset_num;
                map["star_brightness"] = star_brightness;
                map["sunlight_color"] = sunlight_color;

                return map;
            }
        }

        public class WaterData
        {
            public float blurMultiplier;
            public float fresnelOffset;
            public float fresnelScale;
            public Vector3 normScale;
            public UUID normalMap;
            public float scaleAbove;
            public float scaleBelow;
            public float underWaterFogMod;
            public Vector4 waterFogColor;
            public float waterFogDensity;
            public Vector2 wave1Dir;
            public Vector2 wave2Dir;

            public void FromOSD(OSD osd)
            {
                OSDMap map = osd as OSDMap;
                blurMultiplier = map["blurMultiplier"];
                fresnelOffset = map["fresnelOffset"];
                fresnelScale = map["fresnelScale"];
                normScale = map["normScale"];
                normalMap = map["normalMap"];
                scaleAbove = map["scaleAbove"];
                scaleBelow = map["scaleBelow"];
                underWaterFogMod = map["underWaterFogMod"];
                waterFogColor = map["waterFogColor"];
                waterFogDensity = map["waterFogDensity"];
                wave1Dir = map["wave1Dir"];
                wave2Dir = map["wave2Dir"];
            }

            public void ToOSD(ref OSDArray array)
            {
                OSDMap map = new OSDMap();

                map["blurMultiplier"] = blurMultiplier;
                map["fresnelOffset"] = fresnelOffset;
                map["fresnelScale"] = fresnelScale;
                map["normScale"] = normScale;
                map["normalMap"] = normalMap;
                map["scaleAbove"] = scaleAbove;
                map["scaleBelow"] = scaleBelow;
                map["underWaterFogMod"] = underWaterFogMod;
                map["waterFogColor"] = waterFogColor;
                map["waterFogDensity"] = waterFogDensity;
                map["wave1Dir"] = wave1Dir;
                map["wave2Dir"] = wave2Dir;

                array[3] = map;
            }
        }

        public void FromOSD(OSD osd)
        {
            OSDArray array = osd as OSDArray;

            RegionID = (array[0] as OSDMap)["regionID"];
            Cycle = new DayCycle();
            Cycle.FromOSD(array);

            Water = new WaterData();
            Water.FromOSD(array[3]);
        }

        public OSD ToOSD()
        {
            OSDArray array = new OSDArray(4) {null, null, null, null};
            array[0] = new OSDMap {{"regionID", RegionID}};
            Cycle.ToOSD(ref array);
            Water.ToOSD(ref array);

            return array;
        }
    }
}