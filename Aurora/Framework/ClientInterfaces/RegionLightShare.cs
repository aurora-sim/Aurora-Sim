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
using OpenMetaverse;
using OpenMetaverse.StructuredData;
namespace Aurora.Framework
{
    public class RegionLightShareData : IDataTransferable, ICloneable
    {
        public UUID UUID = UUID.Random();
        public Vector4 ambient = new Vector4(0.35f, 0.35f, 0.35f, 0.35f);
        public Vector2 bigWaveDirection = new Vector2(1.05f, -0.42f);
        public Vector4 blueDensity = new Vector4(0.12f, 0.22f, 0.38f, 0.38f);
        public float blurMultiplier = 0.040f;
        public float classicCloudHeight = 192;
        public float classicCloudRange = 48;
        public Vector4 cloudColor = new Vector4(0.41f, 0.41f, 0.41f, 0.41f);
        public float cloudCoverage = 0.27f;
        public Vector3 cloudDetailXYDensity = new Vector3(1.00f, 0.53f, 0.12f);
        public float cloudScale = 0.42f;
        public float cloudScrollX = 0.20f;
        public bool cloudScrollXLock;
        public float cloudScrollY = 0.01f;
        public bool cloudScrollYLock;
        public Vector3 cloudXYDensity = new Vector3(1.00f, 0.53f, 1.00f);
        public float densityMultiplier = 0.18f;
        public float distanceMultiplier = 0.8f;
        public bool drawClassicClouds = true;
        public float eastAngle;
        public float fade = 1; //Default to having 1 so that it isn't instant
        public float fresnelOffset = 0.50f;
        public float fresnelScale = 0.40f;
        public float hazeDensity = 0.70f;
        public float hazeHorizon = 0.19f;
        public Vector4 horizon = new Vector4(0.25f, 0.25f, 0.32f, 0.32f);
        public Vector2 littleWaveDirection = new Vector2(1.11f, -1.16f);
        public UInt16 maxAltitude = 1605;
        public float maxEffectiveAltitude;
        public float minEffectiveAltitude;
        public UUID normalMapTexture = new UUID("822ded49-9a6c-f61c-cb89-6df54f42cdf4");
        public bool overrideParcels;
        public Vector3 reflectionWaveletScale = new Vector3(2.0f, 2.0f, 2.0f);
        public float refractScaleAbove = 0.03f;
        public float refractScaleBelow = 0.20f;
        public UUID regionID = UUID.Zero;
        public float sceneGamma = 1.0f;
        public float starBrightness;
        public float sunGlowFocus = 0.10f;
        public float sunGlowSize = 1.75f;
        public Vector4 sunMoonColor = new Vector4(0.24f, 0.26f, 0.30f, 0.30f);
        public float sunMoonPosition = 0.317f;
        //Notes:
        // 0 - Region wide
        // 1 - Parcel based
        // 2 - Area based
        public int type;
        public float underwaterFogModifier = 0.25f;
        public Vector4 waterColor = new Vector4(4.0f, 38.0f, 64.0f, 0.0f);
        public float waterFogDensityExponent = 4.0f;

        #region ICloneable Members

        public object Clone()
        {
            return this.MemberwiseClone(); // call clone method
        }

        #endregion

        public override void FromOSD(OSDMap map)
        {
            this.ambient = new Vector4((float) map["ambientX"].AsReal(),
                                       (float) map["ambientY"].AsReal(),
                                       (float) map["ambientZ"].AsReal(),
                                       (float) map["ambientW"].AsReal());

            this.bigWaveDirection = new Vector2((float) map["bigWaveDirectionX"].AsReal(),
                                                (float) map["bigWaveDirectionY"].AsReal());
            this.blueDensity = new Vector4((float) map["blueDensityX"].AsReal(),
                                           (float) map["blueDensityY"].AsReal(),
                                           (float) map["blueDensityZ"].AsReal(),
                                           (float) map["blueDensityW"].AsReal());
            this.blurMultiplier = (float) map["blurMultiplier"].AsReal();
            this.cloudColor = new Vector4((float) map["cloudColorX"].AsReal(),
                                          (float) map["cloudColorY"].AsReal(),
                                          (float) map["cloudColorZ"].AsReal(),
                                          (float) map["cloudColorW"].AsReal());
            this.cloudCoverage = (float) map["cloudCoverage"].AsReal();
            this.cloudDetailXYDensity = new Vector3((float) map["cloudDetailXYDensityX"].AsReal(),
                                                    (float) map["cloudDetailXYDensityY"].AsReal(),
                                                    (float) map["cloudDetailXYDensityZ"].AsReal());
            this.cloudScale = (float) map["cloudScale"].AsReal();
            this.cloudScrollX = (float) map["cloudScrollX"].AsReal();
            this.cloudScrollXLock = map["cloudScrollXLock"].AsBoolean();
            this.cloudScrollY = (float) map["cloudScrollY"].AsReal();
            this.cloudScrollYLock = map["cloudScrollYLock"].AsBoolean();

            this.cloudXYDensity = new Vector3((float) map["cloudXYDensityX"].AsReal(),
                                              (float) map["cloudXYDensityY"].AsReal(),
                                              (float) map["cloudXYDensityZ"].AsReal());
            this.densityMultiplier = (float) map["densityMultiplier"].AsReal();
            this.distanceMultiplier = (float) map["distanceMultiplier"].AsReal();

            this.drawClassicClouds = map["drawClassicClouds"].AsBoolean();
            this.classicCloudHeight = (float) map["classicCloudHeight"].AsReal();
            this.classicCloudRange = (float) map["classicCloudRange"].AsReal();

            this.eastAngle = (float) map["eastAngle"].AsReal();
            this.fresnelOffset = (float) map["fresnelOffset"].AsReal();
            this.fresnelScale = (float) map["fresnelScale"].AsReal();
            this.hazeDensity = (float) map["hazeDensity"].AsReal();
            this.hazeHorizon = (float) map["hazeHorizon"].AsReal();
            this.horizon = new Vector4((float) map["horizonX"].AsReal(),
                                       (float) map["horizonY"].AsReal(),
                                       (float) map["horizonZ"].AsReal(),
                                       (float) map["horizonW"].AsReal());
            this.littleWaveDirection = new Vector2((float) map["littleWaveDirectionX"].AsReal(),
                                                   (float) map["littleWaveDirectionY"].AsReal());
            this.maxAltitude = (ushort) map["maxAltitude"].AsReal();
            this.normalMapTexture = map["normalMapTexture"].AsUUID();
            this.reflectionWaveletScale = new Vector3((float) map["reflectionWaveletScaleX"].AsReal(),
                                                      (float) map["reflectionWaveletScaleY"].AsReal(),
                                                      (float) map["reflectionWaveletScaleZ"].AsReal());
            this.refractScaleAbove = (float) map["refractScaleAbove"].AsReal();
            this.refractScaleBelow = (float) map["refractScaleBelow"].AsReal();
            this.sceneGamma = (float) map["sceneGamma"].AsReal();
            this.starBrightness = (float) map["starBrightness"].AsReal();
            this.sunGlowFocus = (float) map["sunGlowFocus"].AsReal();
            this.sunGlowSize = (float) map["sunGlowSize"].AsReal();
            this.sunMoonColor = new Vector4((float) map["sunMoonColorX"].AsReal(),
                                            (float) map["sunMoonColorY"].AsReal(),
                                            (float) map["sunMoonColorZ"].AsReal(),
                                            (float) map["sunMoonColorW"].AsReal());
            this.sunMoonPosition = (float) map["sunMoonPosition"].AsReal();
            this.underwaterFogModifier = (float) map["underwaterFogModifier"].AsReal();
            this.waterColor = new Vector4((float) map["waterColorX"].AsReal(),
                                          (float) map["waterColorY"].AsReal(),
                                          (float) map["waterColorZ"].AsReal(),
                                          (float) map["waterColorW"].AsReal());
            this.waterFogDensityExponent = (float) map["waterFogDensityExponent"].AsReal();
            this.fade = (float) map["fade"].AsReal();
            if (map.ContainsKey("overrideParcels"))
                this.overrideParcels = map["overrideParcels"].AsBoolean();
            if (map.ContainsKey("maxEffectiveAltitude"))
                this.maxEffectiveAltitude = (float) map["maxEffectiveAltitude"].AsReal();
            if (map.ContainsKey("minEffectiveAltitude"))
                this.minEffectiveAltitude = (float) map["minEffectiveAltitude"].AsReal();
            this.type = map["type"].AsInteger();

            this.regionID = map["regionID"].AsUUID();
            this.UUID = map["UUID"].AsUUID();
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap
                             {
                                 {"waterColorX", OSD.FromReal(this.waterColor.X)},
                                 {"waterColorY", OSD.FromReal(this.waterColor.Y)},
                                 {"waterColorZ", OSD.FromReal(this.waterColor.Z)},
                                 {"waterColorW", OSD.FromReal(this.waterColor.W)},
                                 {"waterFogDensityExponent", OSD.FromReal(this.waterFogDensityExponent)},
                                 {"underwaterFogModifier", OSD.FromReal(this.underwaterFogModifier)},
                                 {"reflectionWaveletScaleX", OSD.FromReal(this.reflectionWaveletScale.X)},
                                 {"reflectionWaveletScaleY", OSD.FromReal(this.reflectionWaveletScale.Y)},
                                 {"reflectionWaveletScaleZ", OSD.FromReal(this.reflectionWaveletScale.Z)},
                                 {"fresnelScale", OSD.FromReal(this.fresnelScale)},
                                 {"fresnelOffset", OSD.FromReal(this.fresnelOffset)},
                                 {"refractScaleAbove", OSD.FromReal(this.refractScaleAbove)},
                                 {"refractScaleBelow", OSD.FromReal(this.refractScaleBelow)},
                                 {"blurMultiplier", OSD.FromReal(this.blurMultiplier)},
                                 {"bigWaveDirectionX", OSD.FromReal(this.bigWaveDirection.X)},
                                 {"bigWaveDirectionY", OSD.FromReal(this.bigWaveDirection.Y)},
                                 {"littleWaveDirectionX", OSD.FromReal(this.littleWaveDirection.X)},
                                 {"littleWaveDirectionY", OSD.FromReal(this.littleWaveDirection.Y)},
                                 {"normalMapTexture", OSD.FromUUID(this.normalMapTexture)},
                                 {"sunMoonColorX", OSD.FromReal(this.sunMoonColor.X)},
                                 {"sunMoonColorY", OSD.FromReal(this.sunMoonColor.Y)},
                                 {"sunMoonColorZ", OSD.FromReal(this.sunMoonColor.Z)},
                                 {"sunMoonColorW", OSD.FromReal(this.sunMoonColor.W)},
                                 {"ambientX", OSD.FromReal(this.ambient.X)},
                                 {"ambientY", OSD.FromReal(this.ambient.Y)},
                                 {"ambientZ", OSD.FromReal(this.ambient.Z)},
                                 {"ambientW", OSD.FromReal(this.ambient.W)},
                                 {"horizonX", OSD.FromReal(this.horizon.X)},
                                 {"horizonY", OSD.FromReal(this.horizon.Y)},
                                 {"horizonZ", OSD.FromReal(this.horizon.Z)},
                                 {"horizonW", OSD.FromReal(this.horizon.W)},
                                 {"blueDensityX", OSD.FromReal(this.blueDensity.X)},
                                 {"blueDensityY", OSD.FromReal(this.blueDensity.Y)},
                                 {"blueDensityZ", OSD.FromReal(this.blueDensity.Z)},
                                 {"hazeHorizon", OSD.FromReal(this.hazeHorizon)},
                                 {"hazeDensity", OSD.FromReal(this.hazeDensity)},
                                 {"cloudCoverage", OSD.FromReal(this.cloudCoverage)},
                                 {"densityMultiplier", OSD.FromReal(this.densityMultiplier)},
                                 {"distanceMultiplier", OSD.FromReal(this.distanceMultiplier)},
                                 {"maxAltitude", OSD.FromReal(this.maxAltitude)},
                                 {"cloudColorX", OSD.FromReal(this.cloudColor.X)},
                                 {"cloudColorY", OSD.FromReal(this.cloudColor.Y)},
                                 {"cloudColorZ", OSD.FromReal(this.cloudColor.Z)},
                                 {"cloudColorW", OSD.FromReal(this.cloudColor.W)},
                                 {"cloudXYDensityX", OSD.FromReal(this.cloudXYDensity.X)},
                                 {"cloudXYDensityY", OSD.FromReal(this.cloudXYDensity.Y)},
                                 {"cloudXYDensityZ", OSD.FromReal(this.cloudXYDensity.Z)},
                                 {"cloudDetailXYDensityX", OSD.FromReal(this.cloudDetailXYDensity.X)},
                                 {"cloudDetailXYDensityY", OSD.FromReal(this.cloudDetailXYDensity.Y)},
                                 {"cloudDetailXYDensityZ", OSD.FromReal(this.cloudDetailXYDensity.Z)},
                                 {"starBrightness", OSD.FromReal(this.starBrightness)},
                                 {"eastAngle", OSD.FromReal(this.eastAngle)},
                                 {"sunMoonPosition", OSD.FromReal(this.sunMoonPosition)},
                                 {"sunGlowFocus", OSD.FromReal(this.sunGlowFocus)},
                                 {"sunGlowSize", OSD.FromReal(this.sunGlowSize)},
                                 {"cloudScale", OSD.FromReal(this.cloudScale)},
                                 {"sceneGamma", OSD.FromReal(this.sceneGamma)},
                                 {"cloudScrollX", OSD.FromReal(this.cloudScrollX)},
                                 {"cloudScrollY", OSD.FromReal(this.cloudScrollY)},
                                 {"cloudScrollXLock", OSD.FromBoolean(this.cloudScrollXLock)},
                                 {"cloudScrollYLock", OSD.FromBoolean(this.cloudScrollYLock)},
                                 {"drawClassicClouds", OSD.FromBoolean(this.drawClassicClouds)},
                                 {"classicCloudHeight", OSD.FromReal(this.classicCloudHeight)},
                                 {"classicCloudRange", OSD.FromReal(this.classicCloudRange)},
                                 {"fade", OSD.FromReal(this.fade)},
                                 {"type", OSD.FromReal(this.type)},
                                 {"overrideParcels", OSD.FromBoolean(this.overrideParcels)},
                                 {"maxEffectiveAltitude", OSD.FromReal(this.maxEffectiveAltitude)},
                                 {"minEffectiveAltitude", OSD.FromReal(this.minEffectiveAltitude)},
                                 {"regionID", OSD.FromUUID(this.regionID)}
                             };


            if (this.UUID == UUID.Zero)
                this.UUID = UUID.Random();
            map.Add("UUID", OSD.FromUUID(this.UUID));
            return map;
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }

        public override Dictionary<string, object> ToKVP()
        {
            return Util.OSDToDictionary(ToOSD());
        }
    }
}