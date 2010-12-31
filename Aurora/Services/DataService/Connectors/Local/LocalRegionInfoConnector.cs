using System;
using System.Collections.Generic;
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

        public void Initialize(IGenericData GenericData, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            //Disabled for now until it is fixed
            if (source.Configs["AuroraConnectors"].GetString("RegionInfoConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString);

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
            Values.Add(region.RegionID);
            Values.Add(region.RegionName);
            Values.Add(OSDParser.SerializeJsonString(region.PackRegionInfoData()));
            Values.Add(Disable);
            GD.Replace("simulator", new string[]{"RegionID","RegionName",
                "RegionInfo","Disabled"}, Values.ToArray());
        }

        public RegionInfo[] GetRegionInfos()
        {
            List<RegionInfo> Infos = new List<RegionInfo>();
            List<string> RetVal = GD.Query("Disabled", false, "simulator", "RegionInfo");
            if (RetVal.Count == 0)
                return Infos.ToArray();
            RegionInfo replyData = new RegionInfo();
            for (int i = 0; i < RetVal.Count; i++)
            {
                replyData.UnpackRegionInfoData((OSDMap)OSDParser.DeserializeJson(RetVal[i]));
                if (replyData.ExternalHostName == "DEFAULT")
                {
                    replyData.ExternalHostName = Aurora.Framework.Utilities.GetExternalIp();
                }
                Infos.Add(replyData);
                replyData = new RegionInfo();
            }
            return Infos.ToArray();
        }

        public RegionInfo GetRegionInfo(UUID regionID)
        {
            List<string> RetVal = GD.Query("RegionID", regionID, "simulator", "RegionInfo");
            RegionInfo replyData = new RegionInfo();
            if (RetVal.Count == 0)
                return null;
            replyData.UnpackRegionInfoData((OSDMap)OSDParser.DeserializeJson(RetVal[0]));
            if (replyData.ExternalHostName == "DEFAULT")
            {
                replyData.ExternalHostName = Aurora.Framework.Utilities.GetExternalIp();
            }
            return replyData;
        }

        public RegionInfo GetRegionInfo(string regionName)
        {
            List<string> RetVal = GD.Query("RegionName", regionName, "simulator", "RegionInfo");
            RegionInfo replyData = new RegionInfo();
            if (RetVal.Count == 0)
                return null;
            replyData.UnpackRegionInfoData((OSDMap)OSDParser.DeserializeJson(RetVal[0]));
            if (replyData.ExternalHostName == "DEFAULT")
            {
                replyData.ExternalHostName = Aurora.Framework.Utilities.GetExternalIp();
            }
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
    }
}
