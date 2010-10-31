using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using Nini.Config;

namespace Aurora.Services.DataService
{
    public class LocalParcelServiceConnector : IParcelServiceConnector, IAuroraDataPlugin
    {
        private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, IConfigSource source, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("ParcelConnector", "LocalConnector") == "LocalConnector")
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
            get { return "IParcelServiceConnector"; }
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// This also updates the parcel, not for just adding a new one
        /// </summary>
        /// <param name="args"></param>
        public void StoreLandObject(LandData args)
        {
            GenericUtils.AddGeneric(args.RegionID, "LandData", args.GlobalID.ToString(), args.ToOSD(), GD);
            SaveParcelAccessList(args);
        }

        public LandData GetLandData(UUID RegionID, UUID ParcelID)
        {
            LandData data = GenericUtils.GetGeneric<LandData>(RegionID, "LandData", ParcelID.ToString(), GD, new LandData());
            BuildParcelAccessList(data);
            return data;
        }

        private void SaveParcelAccessList(LandData data)
        {
            foreach (ParcelManager.ParcelAccessEntry entry in data.ParcelAccessList)
            {
                GD.Replace("parcelAccess", new string[]
                {
                    "ParcelID",
                    "AccessID",
                    "Flags",
                    "Time"
                }
                ,
                new object[]
                {
                    data.GlobalID,
                    entry.AgentID,
                    entry.Flags,
                    entry.Time.Ticks
                });
            }
        }

        private void BuildParcelAccessList(LandData LandData)
        {
            List<string> Query = GD.Query("ParcelID", LandData.GlobalID, "parcelAccess", "AccessID, Flags, Time");
            int i = 0;
            int dataCount = 0;
            ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry();
            foreach (string retVal in Query)
            {
                if (dataCount == 0)
                    entry.AgentID = UUID.Parse(Query[i]);
                if (dataCount == 1)
                    entry.Flags = (AccessList)int.Parse(Query[i]);
                if (dataCount == 2)
                    entry.Time = new DateTime(long.Parse(Query[i]));
                dataCount++;
                i++;
                if (dataCount == 3)
                {
                    LandData.ParcelAccessList.Add(entry);
                    entry = new ParcelManager.ParcelAccessEntry();
                    dataCount = 0;
                }
            }
        }

        public List<LandData> LoadLandObjects(UUID regionID)
        {
            List<LandData> AllLandObjects = GenericUtils.GetGenerics<LandData>(regionID, "LandData", GD, new LandData());
            for (int i = 0; i < AllLandObjects.Count; i++)
            {
                BuildParcelAccessList(AllLandObjects[i]);
            }
            return AllLandObjects;
        }

        public void RemoveLandObject(UUID RegionID, UUID ParcelID)
        {
            GenericUtils.RemoveGeneric(RegionID, "LandData", ParcelID.ToString(), GD);
            GD.Delete("parcelAccess", new string[] { "ParcelID" }, new object[] { ParcelID });
        }
    }
}
