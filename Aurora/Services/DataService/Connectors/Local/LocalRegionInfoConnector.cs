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
    public class LocalRegionInfoConnector : IRegionInfoConnector, IAuroraDataPlugin
    {
        private IGenericData GD = null;

        public void Initialise(IGenericData GenericData, IConfigSource source, string defaultConnectionString)
        {
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
                Values.Add(region.ObjectCapacity);
                Values.Add(region.AccessLevel);
                Values.Add(Disable ? 1 : 0);
                Values.Add(region.AllowScriptCrossing ? 1 : 0);
                Values.Add(region.TrustBinariesFromForeignSims ? 1 : 0);
                Values.Add(region.SeeIntoThisSimFromNeighbor ? 1 : 0);
                Values.Add(region.AllowPhysicalPrims ? 1 : 0);
                GD.Update("simulator", Values.ToArray(), new string[]{"RegionName","RegionLocX",
                "RegionLocY","InternalIP","Port","ExternalIP","RegionType","MaxPrims","AccessLevel","Disabled"},
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
                Values.Add(region.ObjectCapacity);
                Values.Add(0);
                Values.Add(0);
                Values.Add(0);
                Values.Add(region.AccessLevel);
                Values.Add(Disable ? 1 : 0);
                Values.Add(region.AllowScriptCrossing ? 1 : 0);
                Values.Add(region.TrustBinariesFromForeignSims ? 1 : 0);
                Values.Add(region.SeeIntoThisSimFromNeighbor ? 1 : 0);
                Values.Add(region.AllowPhysicalPrims ? 1 : 0);
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
                    replyData.RegionName = RetVal[i];
                if (DataCount == 2)
                    replyData.RegionLocX = uint.Parse(RetVal[i]);
                if (DataCount == 3)
                    replyData.RegionLocY = uint.Parse(RetVal[i]);
                if (DataCount == 6)
                    replyData.ExternalHostName = RetVal[i];
                if (DataCount == 7)
                    replyData.RegionType = RetVal[i];
                if (DataCount == 11)
                    replyData.ObjectCapacity = Convert.ToInt32(RetVal[i]);
                if (DataCount == 15)
                    replyData.AccessLevel = Convert.ToByte(RetVal[i]);
                if (DataCount == 16)
                    replyData.Disabled = int.Parse(RetVal[i]) == 1;
                if (DataCount == 17)
                    replyData.AllowScriptCrossing = int.Parse(RetVal[i]) == 1;
                if (DataCount == 18)
                    replyData.TrustBinariesFromForeignSims = int.Parse(RetVal[i]) == 1;
                if (DataCount == 19)
                    replyData.SeeIntoThisSimFromNeighbor = int.Parse(RetVal[i]) == 1;
                if (DataCount == 20)
                    replyData.AllowPhysicalPrims = int.Parse(RetVal[i]) == 1;
                DataCount++;

                if (DataCount == 21)
                {
                    replyData.SetEndPoint(RetVal[(i - (DataCount - 1)) + 4], int.Parse(RetVal[(i - (DataCount - 1)) + 5]));
                    if (replyData.ExternalHostName == "DEFAULT")
                    {
                        replyData.ExternalHostName = Aurora.Framework.Utilities.GetExternalIp();
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
                if (i == 11)
                    replyData.ObjectCapacity = Convert.ToInt32(RetVal[i]);
                if (i == 15)
                    replyData.AccessLevel = Convert.ToByte(RetVal[i]);
                if (i == 16)
                    replyData.Disabled = int.Parse(RetVal[i]) == 1;
                if (i == 17)
                    replyData.AllowScriptCrossing = int.Parse(RetVal[i]) == 1;
                if (i == 18)
                    replyData.TrustBinariesFromForeignSims = int.Parse(RetVal[i]) == 1;
                if (i == 19)
                    replyData.SeeIntoThisSimFromNeighbor = int.Parse(RetVal[i]) == 1;
                if (i == 20)
                {
                    replyData.AllowPhysicalPrims = int.Parse(RetVal[i]) == 1;
                    replyData.SetEndPoint(RetVal[4], int.Parse(RetVal[5]));
                    if (replyData.ExternalHostName == "DEFAULT")
                    {
                        replyData.ExternalHostName = Aurora.Framework.Utilities.GetExternalIp();
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
                if (i == 11)
                    replyData.ObjectCapacity = Convert.ToInt32(RetVal[i]);
                if (i == 15)
                    replyData.AccessLevel = Convert.ToByte(RetVal[i]);
                if (i == 16)
                    replyData.Disabled = int.Parse(RetVal[i]) == 1;
                if (i == 17)
                    replyData.AllowScriptCrossing = int.Parse(RetVal[i]) == 1;
                if (i == 18)
                    replyData.TrustBinariesFromForeignSims = int.Parse(RetVal[i]) == 1;
                if (i == 19)
                    replyData.SeeIntoThisSimFromNeighbor = int.Parse(RetVal[i]) == 1;
                if (i == 20)
                {
                    replyData.AllowPhysicalPrims = int.Parse(RetVal[i]) == 1;
                    replyData.SetEndPoint(RetVal[4], int.Parse(RetVal[5]));
                    if (replyData.ExternalHostName == "DEFAULT")
                    {
                        replyData.ExternalHostName = Aurora.Framework.Utilities.GetExternalIp();
                    }
                    replyData.HttpPort = uint.Parse(RetVal[5]);
                }
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
