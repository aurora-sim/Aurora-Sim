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
    public class LocalParcelServiceConnector : IParcelServiceConnector
    {
        private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
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
            //Parcel access is saved seperately
            SaveParcelAccessList(args);
        }

        /// <summary>
        /// Get a specific region's parcel info
        /// </summary>
        /// <param name="RegionID"></param>
        /// <param name="ParcelID"></param>
        /// <returns></returns>
        public LandData GetLandData(UUID RegionID, UUID ParcelID)
        {
            LandData data = GenericUtils.GetGeneric<LandData>(RegionID, "LandData", ParcelID.ToString(), GD, new LandData());
            //Stored seperately, so rebuild it
            BuildParcelAccessList(data);
            return data;
        }

        /// <summary>
        /// Save this parcel's access list 
        /// </summary>
        /// <param name="data"></param>
        private void SaveParcelAccessList(LandData data)
        {
            //Clear out all old parcel bans and access list entries
            GD.Delete("parcelaccess", new string[] { "ParcelID" }, new object[] { data.GlobalID });
            foreach (ParcelManager.ParcelAccessEntry entry in data.ParcelAccessList)
            {
                //Replace all the old ones
                GD.Replace("parcelaccess", new string[]
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

        /// <summary>
        /// Rebuild the access list from the database
        /// </summary>
        /// <param name="LandData"></param>
        private void BuildParcelAccessList(LandData LandData)
        {
            List<string> Query = GD.Query("ParcelID", LandData.GlobalID, "parcelaccess", "AccessID, Flags, Time");
            ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry();
            for(int i = 0; i < Query.Count; i += 3)
            {
                entry.AgentID = UUID.Parse(Query[i]);
                entry.Flags = (AccessList)int.Parse(Query[i+1]);
                entry.Time = new DateTime(long.Parse(Query[i+2]));
                LandData.ParcelAccessList.Add(entry);
                entry = new ParcelManager.ParcelAccessEntry();
            }
        }

        /// <summary>
        /// Load all parcels in the region
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        public List<LandData> LoadLandObjects(UUID regionID)
        {
            //Load all from the database
            List<LandData> AllLandObjects = GenericUtils.GetGenerics<LandData>(regionID, "LandData", GD, new LandData());
            for (int i = 0; i < AllLandObjects.Count; i++)
            {
                BuildParcelAccessList(AllLandObjects[i]);
            }
            return AllLandObjects;
        }

        /// <summary>
        /// Delete a parcel from the database
        /// </summary>
        /// <param name="RegionID"></param>
        /// <param name="ParcelID"></param>
        public void RemoveLandObject(UUID RegionID, UUID ParcelID)
        {
            //Remove both the generic and the parcel access list
            GenericUtils.RemoveGeneric(RegionID, "LandData", ParcelID.ToString(), GD);
            GD.Delete("parcelaccess", new string[] { "ParcelID" }, new object[] { ParcelID });
        }
    }
}
