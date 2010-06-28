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

        public void Initialise(IGenericData GenericData, IConfigSource source)
        {
            if (source.Configs["AuroraConnectors"].GetString("ParcelConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;
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
            try
            {
                GD.Delete("landinfo", new string[] { "ParcelID" }, new string[] { args.GlobalID.ToString() });
            }
            catch (Exception)
            {
            }
            List<object> Values = new List<object>();
            Values.Add(args.GlobalID);
            Values.Add(args.LocalID);
            Values.Add(args.UserLocation.X);
            Values.Add(args.UserLocation.Y);
            Values.Add(args.UserLocation.Z);
            Values.Add(args.Name);
            Values.Add(args.Description);
            Values.Add(args.Flags);
            Values.Add(args.Dwell);

            //InfoUUID is the missing 'real' Gridwide ParcelID for the gridwide parcel store
            Values.Add(args.InfoUUID);

            Values.Add(args.SalePrice);
            Values.Add(args.AuctionID);
            Values.Add(args.Area);
            Values.Add(args.Maturity);
            Values.Add(args.OwnerID);
            Values.Add(args.GroupID);
            Values.Add(args.MediaDescription);
            Values.Add(args.MediaSize[0]);
            Values.Add(args.MediaSize[1]);
            Values.Add(args.MediaLoop);
            Values.Add(args.MediaType);
            Values.Add(args.ObscureMedia);
            Values.Add(args.ObscureMusic);
            Values.Add(args.SnapshotID);
            Values.Add(args.MediaAutoScale);
            Values.Add(args.MediaURL);
            Values.Add(args.MusicURL);
            Values.Add(args.Bitmap);
            Values.Add((int)args.Category);
            Values.Add(args.ClaimDate);
            Values.Add(args.ClaimPrice);
            Values.Add((int)args.Status);
            Values.Add(args.LandingType);
            Values.Add(args.PassHours);
            Values.Add(args.PassPrice);
            Values.Add(args.UserLookAt.X);
            Values.Add(args.UserLookAt.Y);
            Values.Add(args.UserLookAt.Z);
            Values.Add(args.AuthBuyerID);
            Values.Add(args.OtherCleanTime);
            Values.Add(args.RegionID);
            Values.Add(args.RegionHandle);
            List<string> Keys = new List<string>();
            Keys.Add("ParcelID");
            Keys.Add("LocalID");
            Keys.Add("LandingX");
            Keys.Add("LandingY");
            Keys.Add("LandingZ");
            Keys.Add("Name");
            Keys.Add("Description");
            Keys.Add("Flags");
            Keys.Add("Dwell");
            Keys.Add("InfoUUID");
            Keys.Add("SalePrice");
            Keys.Add("Auction");
            Keys.Add("Area");
            Keys.Add("Maturity");
            Keys.Add("OwnerID");
            Keys.Add("GroupID");
            Keys.Add("MediaDescription");
            Keys.Add("MediaHeight");
            Keys.Add("MediaWidth");
            Keys.Add("MediaLoop");
            Keys.Add("MediaType");
            Keys.Add("ObscureMedia");
            Keys.Add("ObscureMusic");
            Keys.Add("SnapshotID");
            Keys.Add("MediaAutoScale");
            Keys.Add("MediaURL");
            Keys.Add("MusicURL");
            Keys.Add("Bitmap");
            Keys.Add("Category");
            Keys.Add("ClaimDate");
            Keys.Add("ClaimPrice");
            Keys.Add("Status");
            Keys.Add("LandingType");
            Keys.Add("PassHours");
            Keys.Add("PassPrice");
            Keys.Add("UserLookAtX");
            Keys.Add("UserLookAtY");
            Keys.Add("UserLookAtZ");
            Keys.Add("AuthBuyerID");
            Keys.Add("OtherCleanTime");
            Keys.Add("RegionID");
            Keys.Add("RegionHandle");
            GD.Insert("landinfo", Keys.ToArray(), Values.ToArray());

            SaveParcelAccessList(args);
        }

        public LandData GetLandData(UUID ParcelID)
        {
            List<string> Query = GD.Query("ParcelID", ParcelID, "landinfo", "*");
            LandData LandData = new LandData();

            if (Query.Count == 0)
                return null;

            LandData.GlobalID = UUID.Parse(Query[0]);
            LandData.LocalID = int.Parse(Query[1]);
            LandData.UserLocation = new Vector3(float.Parse(Query[2]), float.Parse(Query[3]), float.Parse(Query[4]));
            LandData.Name = Query[5];
            LandData.Description = Query[6];
            LandData.Flags = uint.Parse(Query[7]);
            LandData.Dwell = int.Parse(Query[8]);
            LandData.InfoUUID = UUID.Parse(Query[9]);
            LandData.SalePrice = int.Parse(Query[10]);
            LandData.AuctionID = uint.Parse(Query[11]);
            LandData.Area = int.Parse(Query[12]);
            LandData.Maturity = int.Parse(Query[13]);
            LandData.OwnerID = UUID.Parse(Query[14]);
            LandData.GroupID = UUID.Parse(Query[15]);
            LandData.MediaDescription = Query[16];
            LandData.MediaSize = new int[]
            {
                int.Parse(Query[17]), int.Parse(Query[18])
            };
            LandData.MediaLoop = byte.Parse(Query[19]);
            LandData.MediaType = Query[20];
            LandData.ObscureMedia = byte.Parse(Query[21]);
            LandData.ObscureMusic = byte.Parse(Query[22]);
            LandData.SnapshotID = UUID.Parse(Query[23]);
            LandData.MediaAutoScale = byte.Parse(Query[24]);
            LandData.MediaURL = Query[25];
            LandData.MusicURL = Query[26];
            LandData.Bitmap = OpenMetaverse.Utils.StringToBytes(Query[27]);
            LandData.Category = (ParcelCategory)int.Parse(Query[28]);
            LandData.ClaimDate = int.Parse(Query[29]);
            LandData.ClaimPrice = int.Parse(Query[30]);
            LandData.Status = (ParcelStatus)int.Parse(Query[31]);
            LandData.LandingType = byte.Parse(Query[32]);
            LandData.PassHours = Single.Parse(Query[33]);
            LandData.PassPrice = int.Parse(Query[34]);
            LandData.UserLookAt = new Vector3(float.Parse(Query[35]), float.Parse(Query[36]), float.Parse(Query[37]));
            LandData.AuthBuyerID = UUID.Parse(Query[38]);
            LandData.OtherCleanTime = int.Parse(Query[39]);
            LandData.RegionID = UUID.Parse(Query[40]);
            LandData.RegionHandle = ulong.Parse(Query[41]);

            BuildParcelAccessList(LandData);

            return LandData;
        }

        private void SaveParcelAccessList(LandData data)
        {
            try
            {
                GD.Delete("parcelAccess", new string[] { "ParcelID" }, new object[] { data.GlobalID });
            }
            catch { }

            foreach (ParcelManager.ParcelAccessEntry entry in data.ParcelAccessList)
            {
                GD.Insert("parcelAccess", new object[]
                {
                    data.GlobalID,
                    entry.AgentID,
                    entry.Flags,
                    entry.Time
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
            IDataReader Query = GD.QueryReader("RegionID", regionID, "landinfo", "*");
            List<LandData> AllLandObjects = new List<LandData>();

            if (Query.FieldCount == 0)
                return AllLandObjects;

            int dataCount = 0;
            LandData LandData = new LandData();
            while (Query.Read())
            {
                for (int i = 0; i < Query.FieldCount; i++)
                {
                    if (dataCount == 0)
                        LandData.GlobalID = UUID.Parse(Query.GetString(i));
                    if (dataCount == 1)
                        LandData.LocalID = int.Parse(Query.GetString(i));
                    if (dataCount == 2)
                        LandData.UserLocation = new Vector3(float.Parse(Query.GetString(i)), float.Parse(Query.GetString(i + 1)), float.Parse(Query.GetString(i + 2)));
                    if (dataCount == 5)
                        LandData.Name = Query.GetString(i);
                    if (dataCount == 6)
                        LandData.Description = Query.GetString(i);
                    if (dataCount == 7)
                        LandData.Flags = uint.Parse(Query.GetString(i));
                    if (dataCount == 8)
                        LandData.Dwell = int.Parse(Query.GetString(i));
                    if (dataCount == 9)
                        LandData.InfoUUID = UUID.Parse(Query.GetString(i));
                    if (dataCount == 10)
                        LandData.SalePrice = int.Parse(Query.GetString(i));
                    if (dataCount == 11)
                        LandData.AuctionID = uint.Parse(Query.GetString(i));
                    if (dataCount == 12)
                        LandData.Area = int.Parse(Query.GetString(i));
                    if (dataCount == 13)
                        LandData.Maturity = int.Parse(Query.GetString(i));
                    if (dataCount == 14)
                        LandData.OwnerID = UUID.Parse(Query.GetString(i));
                    if (dataCount == 15)
                        LandData.GroupID = UUID.Parse(Query.GetString(i));
                    if (dataCount == 16)
                        LandData.MediaDescription = Query.GetString(i);
                    if (dataCount == 17)
                        LandData.MediaSize = new int[]
                {
                    int.Parse(Query.GetString(i)), int.Parse(Query.GetString(i+1))
                };
                    if (dataCount == 19)
                        LandData.MediaLoop = byte.Parse(Query.GetString(i));
                    if (dataCount == 20)
                        LandData.MediaType = Query.GetString(i);
                    if (dataCount == 21)
                        LandData.ObscureMedia = byte.Parse(Query.GetString(i));
                    if (dataCount == 22)
                        LandData.ObscureMusic = byte.Parse(Query.GetString(i));
                    if (dataCount == 23)
                        LandData.SnapshotID = UUID.Parse(Query.GetString(i));
                    if (dataCount == 24)
                        LandData.MediaAutoScale = byte.Parse(Query.GetString(i));
                    if (dataCount == 25)
                        LandData.MediaURL = Query.GetString(i);
                    if (dataCount == 26)
                        LandData.MusicURL = Query.GetString(i);
                    if (dataCount == 27)
                        LandData.Bitmap = (Byte[])Query["Bitmap"];
                    if (dataCount == 28)
                        LandData.Category = (ParcelCategory)int.Parse(Query.GetString(i));
                    if (dataCount == 29)
                        LandData.ClaimDate = int.Parse(Query.GetString(i));
                    if (dataCount == 30)
                        LandData.ClaimPrice = int.Parse(Query.GetString(i));
                    if (dataCount == 31)
                        LandData.Status = (ParcelStatus)int.Parse(Query.GetString(i));
                    if (dataCount == 32)
                        LandData.LandingType = byte.Parse(Query.GetString(i));
                    if (dataCount == 33)
                        LandData.PassHours = Single.Parse(Query.GetString(i));
                    if (dataCount == 34)
                        LandData.PassPrice = int.Parse(Query.GetString(i));
                    if (dataCount == 35)
                        LandData.UserLookAt = new Vector3(float.Parse(Query.GetString(i)), float.Parse(Query.GetString(i + 1)), float.Parse(Query.GetString(i + 2)));
                    if (dataCount == 38)
                        LandData.AuthBuyerID = UUID.Parse(Query.GetString(i));
                    if (dataCount == 39)
                        LandData.OtherCleanTime = int.Parse(Query.GetString(i));
                    if (dataCount == 40)
                        LandData.RegionID = UUID.Parse(Query.GetString(i));
                    if (dataCount == 41)
                        LandData.RegionHandle = ulong.Parse(Query.GetString(i));

                    dataCount++;

                    if (dataCount == 42)
                    {
                        BuildParcelAccessList(LandData);
                        AllLandObjects.Add(LandData);
                        dataCount = 0;
                        LandData = new LandData();
                    }
                }
            }
            Query.Close();
            Query.Dispose();
            return AllLandObjects;
        }

        public void RemoveLandObject(UUID ParcelID)
        {
            GD.Delete("landinfo", new string[] { "ParcelID" }, new object[] { ParcelID });
            GD.Delete("parcelAccess", new string[] { "ParcelID" }, new object[] { ParcelID });
        }
    }
}
