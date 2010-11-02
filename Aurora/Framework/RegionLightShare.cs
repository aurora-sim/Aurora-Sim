using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public class RegionLightShareData : IDataTransferable, ICloneable
    {
        public UUID regionID = UUID.Zero;
        public UUID UUID = UUID.Random();
        public Vector4 waterColor = new Vector4(4.0f, 38.0f, 64.0f, 0.0f);
        public float waterFogDensityExponent = 4.0f;
        public float underwaterFogModifier = 0.25f;
        public Vector3 reflectionWaveletScale = new Vector3(2.0f, 2.0f, 2.0f);
        public float fresnelScale = 0.40f;
        public float fresnelOffset = 0.50f;
        public float refractScaleAbove = 0.03f;
        public float refractScaleBelow = 0.20f;
        public float blurMultiplier = 0.040f;
        public Vector2 bigWaveDirection = new Vector2(1.05f, -0.42f);
        public Vector2 littleWaveDirection = new Vector2(1.11f, -1.16f);
        public UUID normalMapTexture = new UUID("822ded49-9a6c-f61c-cb89-6df54f42cdf4");
        public Vector4 horizon = new Vector4(0.25f, 0.25f, 0.32f, 0.32f);
        public float hazeHorizon = 0.19f;
        public Vector4 blueDensity = new Vector4(0.12f, 0.22f, 0.38f, 0.38f);
        public float hazeDensity = 0.70f;
        public float densityMultiplier = 0.18f;
        public float distanceMultiplier = 0.8f;
        public UInt16 maxAltitude = 1605;
        public Vector4 sunMoonColor = new Vector4(0.24f, 0.26f, 0.30f, 0.30f);
        public float sunMoonPosition = 0.317f;
        public Vector4 ambient = new Vector4(0.35f, 0.35f, 0.35f, 0.35f);
        public float eastAngle = 0.0f;
        public float sunGlowFocus = 0.10f;
        public float sunGlowSize = 1.75f;
        public float sceneGamma = 1.0f;
        public float starBrightness = 0.0f;
        public Vector4 cloudColor = new Vector4(0.41f, 0.41f, 0.41f, 0.41f);
        public Vector3 cloudXYDensity = new Vector3(1.00f, 0.53f, 1.00f);
        public float cloudCoverage = 0.27f;
        public float cloudScale = 0.42f;
        public Vector3 cloudDetailXYDensity = new Vector3(1.00f, 0.53f, 0.12f);
        public float cloudScrollX = 0.20f;
        public bool cloudScrollXLock = false;
        public float cloudScrollY = 0.01f;
        public bool cloudScrollYLock = false;
        public bool drawClassicClouds = true;
        public float classicCloudRange = 48;
        public float classicCloudHeight = 192;
        public float fade = 1; //Default to having 1 so that it isn't instant
        public float minEffectiveAltitude = 0;
        public float maxEffectiveAltitude = 0;
        public bool overrideParcels = false;
        //Notes:
        // 0 - Region wide
        // 1 - Parcel based
        // 2 - Area based
        public int type = 0;

        public object Clone()
        {
            return this.MemberwiseClone();      // call clone method
        }

        public override void FromOSD(OSDMap map)
        {
            this.ambient = new Vector4((float)map["ambientX"].AsReal(),
                (float)map["ambientY"].AsReal(),
                (float)map["ambientZ"].AsReal(),
                (float)map["ambientW"].AsReal());

            this.bigWaveDirection = new Vector2((float)map["bigWaveDirectionX"].AsReal(),
                (float)map["bigWaveDirectionY"].AsReal());
            this.blueDensity = new Vector4((float)map["blueDensityX"].AsReal(),
                (float)map["blueDensityY"].AsReal(),
                (float)map["blueDensityZ"].AsReal(),
                (float)map["blueDensityW"].AsReal());
            this.blurMultiplier = (float)map["blurMultiplier"].AsReal();
            this.cloudColor = new Vector4((float)map["cloudColorX"].AsReal(),
                (float)map["cloudColorY"].AsReal(),
                (float)map["cloudColorZ"].AsReal(),
                (float)map["cloudColorW"].AsReal());
            this.cloudCoverage = (float)map["cloudCoverage"].AsReal();
            this.cloudDetailXYDensity = new Vector3((float)map["cloudDetailXYDensityX"].AsReal(),
                (float)map["cloudDetailXYDensityY"].AsReal(),
                (float)map["cloudDetailXYDensityZ"].AsReal());
            this.cloudScale = (float)map["cloudScale"].AsReal();
            this.cloudScrollX = (float)map["cloudScrollX"].AsReal();
            this.cloudScrollXLock = map["cloudScrollXLock"].AsBoolean();
            this.cloudScrollY = (float)map["cloudScrollY"].AsReal();
            this.cloudScrollYLock = map["cloudScrollYLock"].AsBoolean();

            this.cloudXYDensity = new Vector3((float)map["cloudXYDensityX"].AsReal(),
                (float)map["cloudXYDensityY"].AsReal(),
                (float)map["cloudXYDensityZ"].AsReal());
            this.densityMultiplier = (float)map["densityMultiplier"].AsReal();
            this.distanceMultiplier = (float)map["distanceMultiplier"].AsReal();

            this.drawClassicClouds = map["drawClassicClouds"].AsBoolean();
            this.classicCloudHeight = (float)map["classicCloudHeight"].AsReal();
            this.classicCloudRange = (float)map["classicCloudRange"].AsReal();

            this.eastAngle = (float)map["eastAngle"].AsReal();
            this.fresnelOffset = (float)map["fresnelOffset"].AsReal();
            this.fresnelScale = (float)map["fresnelScale"].AsReal();
            this.hazeDensity = (float)map["hazeDensity"].AsReal();
            this.hazeHorizon = (float)map["hazeHorizon"].AsReal();
            this.horizon = new Vector4((float)map["horizonX"].AsReal(),
                (float)map["horizonY"].AsReal(),
                (float)map["horizonZ"].AsReal(),
                (float)map["horizonW"].AsReal());
            this.littleWaveDirection = new Vector2((float)map["littleWaveDirectionX"].AsReal(),
                (float)map["littleWaveDirectionY"].AsReal());
            this.maxAltitude = (ushort)map["maxAltitude"].AsReal();
            this.normalMapTexture = map["normalMapTexture"].AsUUID();
            this.reflectionWaveletScale = new Vector3((float)map["reflectionWaveletScaleX"].AsReal(),
                (float)map["reflectionWaveletScaleY"].AsReal(),
                (float)map["reflectionWaveletScaleZ"].AsReal());
            this.refractScaleAbove = (float)map["refractScaleAbove"].AsReal();
            this.refractScaleBelow = (float)map["refractScaleBelow"].AsReal();
            this.sceneGamma = (float)map["sceneGamma"].AsReal();
            this.starBrightness = (float)map["starBrightness"].AsReal();
            this.sunGlowFocus = (float)map["sunGlowFocus"].AsReal();
            this.sunGlowSize = (float)map["sunGlowSize"].AsReal();
            this.sunMoonColor = new Vector4((float)map["sunMoonColorX"].AsReal(),
                (float)map["sunMoonColorY"].AsReal(),
                (float)map["sunMoonColorZ"].AsReal(),
                (float)map["sunMoonColorW"].AsReal());
            this.sunMoonPosition = (float)map["sunMoonPosition"].AsReal();
            this.underwaterFogModifier = (float)map["underwaterFogModifier"].AsReal();
            this.waterColor = new Vector4((float)map["waterColorX"].AsReal(),
                (float)map["waterColorY"].AsReal(),
                (float)map["waterColorZ"].AsReal(),
                (float)map["waterColorW"].AsReal());
            this.waterFogDensityExponent = (float)map["waterFogDensityExponent"].AsReal();
            this.fade = (float)map["fade"].AsReal();
            if (map.ContainsKey("overrideParcels"))
                this.overrideParcels = map["overrideParcels"].AsBoolean();
            if(map.ContainsKey("maxEffectiveAltitude"))
                this.maxEffectiveAltitude = (float)map["maxEffectiveAltitude"].AsReal();
            if (map.ContainsKey("minEffectiveAltitude"))
                this.minEffectiveAltitude = (float)map["minEffectiveAltitude"].AsReal();
            this.type = map["type"].AsInteger();

            this.regionID = map["regionID"].AsUUID();
            this.UUID = map["UUID"].AsUUID();
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();

            map.Add("waterColorX", OSD.FromReal(this.waterColor.X));
            map.Add("waterColorY", OSD.FromReal(this.waterColor.Y));
            map.Add("waterColorZ", OSD.FromReal(this.waterColor.Z));
            map.Add("waterColorW", OSD.FromReal(this.waterColor.W));

            map.Add("waterFogDensityExponent", OSD.FromReal(this.waterFogDensityExponent));
            map.Add("underwaterFogModifier", OSD.FromReal(this.underwaterFogModifier));
            map.Add("reflectionWaveletScaleX", OSD.FromReal(this.reflectionWaveletScale.X));
            map.Add("reflectionWaveletScaleY", OSD.FromReal(this.reflectionWaveletScale.Y));
            map.Add("reflectionWaveletScaleZ", OSD.FromReal(this.reflectionWaveletScale.Z));

            map.Add("fresnelScale", OSD.FromReal(this.fresnelScale));
            map.Add("fresnelOffset", OSD.FromReal(this.fresnelOffset));
            map.Add("refractScaleAbove", OSD.FromReal(this.refractScaleAbove));
            map.Add("refractScaleBelow", OSD.FromReal(this.refractScaleBelow));
            map.Add("blurMultiplier", OSD.FromReal(this.blurMultiplier));
            map.Add("bigWaveDirectionX", OSD.FromReal(this.bigWaveDirection.X));
            map.Add("bigWaveDirectionY", OSD.FromReal(this.bigWaveDirection.Y));
            map.Add("littleWaveDirectionX", OSD.FromReal(this.littleWaveDirection.X));
            map.Add("littleWaveDirectionY", OSD.FromReal(this.littleWaveDirection.Y));
            map.Add("normalMapTexture", OSD.FromUUID(this.normalMapTexture));


            map.Add("sunMoonColorX", OSD.FromReal(this.sunMoonColor.X));
            map.Add("sunMoonColorY", OSD.FromReal(this.sunMoonColor.Y));
            map.Add("sunMoonColorZ", OSD.FromReal(this.sunMoonColor.Z));
            map.Add("sunMoonColorW", OSD.FromReal(this.sunMoonColor.W));

            map.Add("ambientX", OSD.FromReal(this.ambient.X));
            map.Add("ambientY", OSD.FromReal(this.ambient.Y));
            map.Add("ambientZ", OSD.FromReal(this.ambient.Z));
            map.Add("ambientW", OSD.FromReal(this.ambient.W));

            map.Add("horizonX", OSD.FromReal(this.horizon.X));
            map.Add("horizonY", OSD.FromReal(this.horizon.Y));
            map.Add("horizonZ", OSD.FromReal(this.horizon.Z));
            map.Add("horizonW", OSD.FromReal(this.horizon.W));

            map.Add("blueDensityX", OSD.FromReal(this.blueDensity.X));
            map.Add("blueDensityY", OSD.FromReal(this.blueDensity.Y));
            map.Add("blueDensityZ", OSD.FromReal(this.blueDensity.Z));

            map.Add("hazeHorizon", OSD.FromReal(this.hazeHorizon));

            map.Add("hazeDensity", OSD.FromReal(this.hazeDensity));
            map.Add("cloudCoverage", OSD.FromReal(this.cloudCoverage));

            map.Add("densityMultiplier", OSD.FromReal(this.densityMultiplier));
            map.Add("distanceMultiplier", OSD.FromReal(this.distanceMultiplier));
            map.Add("maxAltitude", OSD.FromReal(this.maxAltitude));

            map.Add("cloudColorX", OSD.FromReal(this.cloudColor.X));
            map.Add("cloudColorY", OSD.FromReal(this.cloudColor.Y));
            map.Add("cloudColorZ", OSD.FromReal(this.cloudColor.Z));
            map.Add("cloudColorW", OSD.FromReal(this.cloudColor.W));

            map.Add("cloudXYDensityX", OSD.FromReal(this.cloudXYDensity.X));
            map.Add("cloudXYDensityY", OSD.FromReal(this.cloudXYDensity.Y));
            map.Add("cloudXYDensityZ", OSD.FromReal(this.cloudXYDensity.Z));

            map.Add("cloudDetailXYDensityX", OSD.FromReal(this.cloudDetailXYDensity.X));
            map.Add("cloudDetailXYDensityY", OSD.FromReal(this.cloudDetailXYDensity.Y));
            map.Add("cloudDetailXYDensityZ", OSD.FromReal(this.cloudDetailXYDensity.Z));

            map.Add("starBrightness", OSD.FromReal(this.starBrightness));
            map.Add("eastAngle", OSD.FromReal(this.eastAngle));
            map.Add("sunMoonPosition", OSD.FromReal(this.sunMoonPosition));

            map.Add("sunGlowFocus", OSD.FromReal(this.sunGlowFocus));
            map.Add("sunGlowSize", OSD.FromReal(this.sunGlowSize));
            map.Add("cloudScale", OSD.FromReal(this.cloudScale));
            map.Add("sceneGamma", OSD.FromReal(this.sceneGamma));
            map.Add("cloudScrollX", OSD.FromReal(this.cloudScrollX));
            map.Add("cloudScrollY", OSD.FromReal(this.cloudScrollY));
            map.Add("cloudScrollXLock", OSD.FromBoolean(this.cloudScrollXLock));
            map.Add("cloudScrollYLock", OSD.FromBoolean(this.cloudScrollYLock));
            map.Add("drawClassicClouds", OSD.FromBoolean(this.drawClassicClouds));
            map.Add("classicCloudHeight", OSD.FromReal(this.classicCloudHeight));
            map.Add("classicCloudRange", OSD.FromReal(this.classicCloudRange));

            map.Add("fade", OSD.FromReal(this.fade));
            map.Add("type", OSD.FromReal(this.type));
            map.Add("overrideParcels", OSD.FromBoolean(this.overrideParcels));
            map.Add("maxEffectiveAltitude", OSD.FromReal(this.maxEffectiveAltitude));
            map.Add("minEffectiveAltitude", OSD.FromReal(this.minEffectiveAltitude));

            map.Add("regionID", OSD.FromUUID(this.regionID));
            if (this.UUID == UUID.Zero)
                this.UUID = UUID.Random();
            map.Add("UUID", OSD.FromUUID(this.UUID));
            return map;
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            return Util.OSDToDictionary(ToOSD());
        }

        public override IDataTransferable Duplicate()
        {
            RegionLightShareData m = new RegionLightShareData();
            m.FromOSD(ToOSD());
            return m;
        }
    }
}
