using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.Services.DataService
{
    public class LocalParcelServiceConnector : IParcelServiceConnector
    {
        private IGenericData GD = null;
        public LocalParcelServiceConnector(IGenericData GenericData)
        {
            GD = GenericData;
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

    public class LocalDirectoryServiceConnector : IDirectoryServiceConnector
    {
        private IGenericData GD = null;
        public LocalDirectoryServiceConnector(IGenericData GenericData)
        {
            GD = GenericData;
        }

        /// <summary>
        /// This also updates the parcel, not for just adding a new one
        /// </summary>
        /// <param name="args"></param>
        /// <param name="regionID"></param>
        /// <param name="forSale"></param>
        /// <param name="EstateID"></param>
        /// <param name="showInSearch"></param>
        public void AddLandObject(LandData args, UUID regionID, bool forSale, uint EstateID, bool showInSearch, UUID InfoUUID)
        {
            try
            {
                GD.Delete("searchparcel", new string[] { "ParcelID" }, new string[] { args.GlobalID.ToString() });
            }
            catch (Exception)
            {
            }
            List<object> Values = new List<object>();
            Values.Add(regionID);
            Values.Add(args.GlobalID);
            Values.Add(args.LocalID);
            Values.Add(args.UserLocation.X);
            Values.Add(args.UserLocation.Y);
            Values.Add(args.UserLocation.Z);
            Values.Add(args.Name);
            Values.Add(args.Description);
            Values.Add(args.Flags);
            Values.Add(args.Dwell);
            //InfoUUID is the missing 'real' Gridwide ParcelID
            Values.Add(InfoUUID);
            Values.Add(forSale);
            Values.Add(args.SalePrice);
            Values.Add(args.AuctionID);
            Values.Add(args.Area);
            Values.Add(EstateID);
            Values.Add(args.Maturity);
            Values.Add(args.OwnerID);
            Values.Add(args.GroupID);
            Values.Add(showInSearch);
            Values.Add(args.SnapshotID);
            GD.Insert("searchparcel", Values.ToArray());
        }

        public AuroraLandData GetParcelInfo(UUID InfoUUID)
        {
            List<string> Query = GD.Query("InfoUUID", InfoUUID, "searchparcel", "*");
            AuroraLandData LandData = new AuroraLandData();
            if (Query.Count == 0)
                return null;
            LandData.RegionID = UUID.Parse(Query[0]);
            LandData.ParcelID = UUID.Parse(Query[1]);
            LandData.LocalID = int.Parse(Query[2]);
            LandData.LandingX = float.Parse(Query[3]);
            LandData.LandingY = float.Parse(Query[4]);
            LandData.LandingZ = float.Parse(Query[5]);
            LandData.Name = Query[6];
            LandData.Description = Query[7];
            LandData.Flags = uint.Parse(Query[8]);
            LandData.Dwell = int.Parse(Query[9]);
            LandData.InfoUUID = UUID.Parse(Query[10]);
            LandData.ForSale = bool.Parse(Query[11]);
            LandData.SalePrice = float.Parse(Query[12]);
            LandData.AuctionID = uint.Parse(Query[13]);
            LandData.Area = int.Parse(Query[14]);
            LandData.EstateID = uint.Parse(Query[15]);
            LandData.Maturity = int.Parse(Query[16]);
            LandData.OwnerID = UUID.Parse(Query[17]);
            LandData.GroupID = UUID.Parse(Query[18]);
            LandData.ShowInSearch = bool.Parse(Query[19]);
            LandData.SnapshotID = UUID.Parse(Query[20]);
            return LandData;
        }

        public AuroraLandData[] GetParcelByOwner(UUID OwnerID)
        {
            List<AuroraLandData> Lands = new List<AuroraLandData>();
            List<string> Query = GD.Query("OwnerID", OwnerID, "searchparcel", "*");
            if (Query.Count == 0)
                return Lands.ToArray();
            int i = 0;
            int DataCount = 0;
            AuroraLandData LandData = new AuroraLandData();
            foreach (string RetVal in Query)
            {
                if (DataCount == 0)
                    LandData.RegionID = UUID.Parse(Query[i]);
                if (DataCount == 1)
                    LandData.ParcelID = UUID.Parse(Query[i]);
                if (DataCount == 2)
                    LandData.LocalID = int.Parse(Query[i]);
                if (DataCount == 3)
                    LandData.LandingX = float.Parse(Query[i]);
                if (DataCount == 4)
                    LandData.LandingY = float.Parse(Query[i]);
                if (DataCount == 5)
                    LandData.LandingZ = float.Parse(Query[i]);
                if (DataCount == 6)
                    LandData.Name = Query[i];
                if (DataCount == 7)
                    LandData.Description = Query[i];
                if (DataCount == 8)
                    LandData.Flags = uint.Parse(Query[i]);
                if (DataCount == 9)
                    LandData.Dwell = int.Parse(Query[i]);
                if (DataCount == 10)
                    LandData.InfoUUID = UUID.Parse(Query[i]);
                if (DataCount == 11)
                    LandData.ForSale = bool.Parse(Query[i]);
                if (DataCount == 12)
                    LandData.SalePrice = float.Parse(Query[i]);
                if (DataCount == 13)
                    LandData.AuctionID = uint.Parse(Query[i]);
                if (DataCount == 14)
                    LandData.Area = int.Parse(Query[i]);
                if (DataCount == 15)
                    LandData.EstateID = uint.Parse(Query[i]);
                if (DataCount == 16)
                    LandData.Maturity = int.Parse(Query[i]);
                if (DataCount == 17)
                    LandData.OwnerID = UUID.Parse(Query[i]);
                if (DataCount == 18)
                    LandData.GroupID = UUID.Parse(Query[i]);
                if (DataCount == 19)
                    LandData.ShowInSearch = bool.Parse(Query[i]);
                if (DataCount == 20)
                    LandData.SnapshotID = UUID.Parse(Query[i]);

                DataCount++;
                i++;
                if (DataCount == 21)
                {
                    Lands.Add(LandData);
                    LandData = new AuroraLandData();
                    DataCount = 0;
                }
            }
            return Lands.ToArray();
        }


        public DirPlacesReplyData[] FindLand(string queryText, string category, int StartQuery)
        {
            List<DirPlacesReplyData> Data = new List<DirPlacesReplyData>();
            //Don't support category yet. //string whereClause = " PCategory = '" + category + "' and Description LIKE '%" + queryText + "%' OR Name LIKE '%" + queryText + "%' LIMIT " + StartQuery.ToString() + ",50 ";
            string whereClause = " Description LIKE '%" + queryText + "%' OR Name LIKE '%" + queryText + "%' and ShowInSearch = 'True' LIMIT " + StartQuery.ToString() + ",50 ";
            List<string> retVal = GD.Query(whereClause, "searchparcel", "InfoUUID,Name,ForSale,Auction,Dwell");
            if (retVal.Count == 0)
                return Data.ToArray();

            int DataCount = 0;
            DirPlacesReplyData replyData = new DirPlacesReplyData();

            for (int i = 0; i < retVal.Count; i++)
            {
                if (DataCount == 0)
                    replyData.parcelID = new UUID(retVal[i]);
                if (DataCount == 1)
                    replyData.name = retVal[i];
                if (DataCount == 2)
                    replyData.forSale = Convert.ToBoolean(retVal[i]);
                if (DataCount == 3)
                    replyData.auction = retVal[i] == "0";
                if (DataCount == 4)
                    replyData.dwell = float.Parse(retVal[i]);
                DataCount++;
                if (DataCount == 5)
                {
                    DataCount = 0;
                    Data.Add(replyData);
                    replyData = new DirPlacesReplyData();
                }
            }

            return Data.ToArray();
        }

        public DirLandReplyData[] FindLandForSale(string searchType, string price, string area, int StartQuery)
        {
            //searchType
            // 2 - Auction only
            // 8 - For Sale - Mainland
            // 16 - For Sale - Estate
            // 4294967295 - All
            List<DirLandReplyData> Data = new List<DirLandReplyData>();
            string whereClause = " SalePrice <= '" + price + "' and Area >= '" + area + "' LIMIT " + StartQuery.ToString() + ",50 ";
            List<string> retVal = GD.Query(whereClause, "searchparcel", "InfoUUID,Name,Auction,SalePrice,Area,Flags");

            if (retVal.Count == 0)
                return Data.ToArray();

            int DataCount = 0;
            DirLandReplyData replyData = new DirLandReplyData();
            replyData.forSale = true;
            bool AddToList = true;
            for (int i = 0; i < retVal.Count; i++)
            {
                if (DataCount == 0)
                    replyData.parcelID = new UUID(retVal[i]);
                if (DataCount == 1)
                    replyData.name = retVal[i];
                if (DataCount == 2)
                    replyData.auction = (retVal[i] != "0");
                if (DataCount == 3)
                    replyData.salePrice = Convert.ToInt32(retVal[i]);
                if (DataCount == 4)
                    replyData.actualArea = Convert.ToInt32(retVal[i]);
                if (DataCount == 5)
                {
                    if ((Convert.ToInt32(retVal[i]) & (int)OpenMetaverse.ParcelFlags.ForSale) == 0)
                        AddToList = false;
                }
                DataCount++;
                if (DataCount == 6)
                {
                    DataCount = 0;
                    if (AddToList)
                        Data.Add(replyData);
                    AddToList = true;
                    replyData = new DirLandReplyData();
                    replyData.forSale = true;
                }
            }

            return Data.ToArray();
        }

        public DirEventsReplyData[] FindEvents(string queryText, string flags, int StartQuery)
        {
            List<DirEventsReplyData> Data = new List<DirEventsReplyData>();
            string whereClause = "";
            if (queryText.Contains("|0|"))
            {
                string StringDay = queryText.Split('|')[0];
                if (StringDay == "u")
                {
                    whereClause = " EDate >= '" + Util.ToUnixTime(DateTime.Today) + "' and EFlags <= '" + flags + "' LIMIT " + StartQuery.ToString() + ",50 ";
                }
                else
                {
                    int Day = int.Parse(StringDay);
                    DateTime SearchedDay = DateTime.Today.AddDays(Day);
                    DateTime NextDay = SearchedDay.AddDays(1);
                    whereClause = " EDate >= '" + Util.ToUnixTime(SearchedDay) + "' and EDate <= '" + Util.ToUnixTime(NextDay) + "' and EFlags <= '" + flags + "' LIMIT " + StartQuery.ToString() + ",50 ";
                }
            }
            else
            {
                whereClause = " EName LIKE '%" + queryText + "%' and EFlags <= '" + flags + "' LIMIT " + StartQuery.ToString() + ",50 ";
            }

            List<string> retVal = GD.Query(whereClause, "events", "EOwnerID,EName,EID,EDate,EFlags");
            if (retVal.Count == 0)
                return Data.ToArray();

            int DataCount = 0;
            DirEventsReplyData replyData = new DirEventsReplyData();
            for (int i = 0; i < retVal.Count; i++)
            {
                if (DataCount == 0)
                    replyData.ownerID = new UUID(retVal[i]);
                if (DataCount == 1)
                    replyData.name = retVal[i];
                if (DataCount == 2)
                    replyData.eventID = Convert.ToUInt32(retVal[i]);
                if (DataCount == 3)
                {
                    DateTime date = Util.ToDateTime(ulong.Parse(retVal[i]));
                    replyData.date = date.ToString(new System.Globalization.DateTimeFormatInfo());
                    replyData.unixTime = (uint)Util.ToUnixTime(date);
                }
                if (DataCount == 4)
                    replyData.eventFlags = Convert.ToUInt32(retVal[i]);
                DataCount++;
                if (DataCount == 5)
                {
                    DataCount = 0;
                    Data.Add(replyData);
                    replyData = new DirEventsReplyData();
                }
            }

            return Data.ToArray();
        }

        public DirEventsReplyData[] FindAllEventsInRegion(string regionName)
        {
            List<DirEventsReplyData> Data = new List<DirEventsReplyData>();
            List<string> retVal = GD.Query("ESimName", regionName, "events", "EOwnerID,EName,EID,EDate,EFlags");
            if (retVal.Count == 0)
                return Data.ToArray();
            int DataCount = 0;
            DirEventsReplyData replyData = new DirEventsReplyData();
            for (int i = 0; i < retVal.Count; i++)
            {
                if (DataCount == 0)
                    replyData.ownerID = new UUID(retVal[i]);
                if (DataCount == 1)
                    replyData.name = retVal[i];
                if (DataCount == 2)
                    replyData.eventID = Convert.ToUInt32(retVal[i]);
                if (DataCount == 3)
                {
                    DateTime date = Util.ToDateTime(ulong.Parse(retVal[i]));
                    replyData.date = date.ToString(new System.Globalization.DateTimeFormatInfo());
                    replyData.unixTime = (uint)Util.ToUnixTime(date);
                }
                if (DataCount == 4)
                    replyData.eventFlags = Convert.ToUInt32(retVal[i]);
                DataCount++;
                if (DataCount == 5)
                {
                    DataCount = 0;
                    Data.Add(replyData);
                    replyData = new DirEventsReplyData();
                }
            }

            return Data.ToArray();
        }

        public DirClassifiedReplyData[] FindClassifieds(string queryText, string category, string queryFlags, int StartQuery)
        {
            List<DirClassifiedReplyData> Data = new List<DirClassifiedReplyData>();

            string whereClause = "";
            if (category == "0")
            {
                whereClause = " name LIKE '%" + queryText + "%' LIMIT " + StartQuery.ToString() + ",50 ";
            }
            else
            {
                whereClause = " name LIKE '%" + queryText + "%' and category = '" + category + "' LIMIT " + StartQuery.ToString() + ",50 ";
            }
            List<string> retVal = GD.Query(whereClause, "profileclassifieds", "classifiedflags, classifieduuid, creationdate, expirationdate, priceforlisting, name");
            if (retVal.Count == 0)
                return Data.ToArray();

            int DataCount = 0;
            DirClassifiedReplyData replyData = new DirClassifiedReplyData();
            for (int i = 0; i < retVal.Count; i++)
            {
                if (DataCount == 0)
                    replyData.classifiedFlags = Convert.ToByte(retVal[i]);
                if (DataCount == 1)
                    replyData.classifiedID = new UUID(retVal[i]);
                if (DataCount == 2)
                    replyData.creationDate = Convert.ToUInt32(retVal[i]);
                if (DataCount == 3)
                    replyData.expirationDate = Convert.ToUInt32(retVal[i]);
                if (DataCount == 4)
                    replyData.price = Convert.ToInt32(retVal[i]);
                if (DataCount == 5)
                    replyData.name = retVal[i];
                DataCount++;
                if (DataCount == 6)
                {
                    DataCount = 0;
                    Data.Add(replyData);
                    replyData = new DirClassifiedReplyData();
                }
            }
            return Data.ToArray();
        }

        public EventData GetEventInfo(string EventID)
        {
            EventData data = new EventData();
            List<string> RetVal = GD.Query("EID", EventID, "events", "EID, ECreatorID, EName, ECategory, EDesc, EDate, EDuration, ECoverCharge, ECoverAmount, ESimName, EGlobalPos, EFlags, EMature");
            if (RetVal.Count == 0)
                return null;
            for (int i = 0; i < RetVal.Count; i++)
            {
                if (i == 0)
                    data.eventID = Convert.ToUInt32(RetVal[i]);
                if (i == 1)
                    data.creator = RetVal[i];
                if (i == 2)
                    data.name = RetVal[i];
                if (i == 3)
                    data.category = RetVal[i];
                if (i == 4)
                    data.description = RetVal[i];
                if (i == 5)
                {
                    DateTime date = Util.ToDateTime(ulong.Parse(RetVal[i]));
                    data.date = date.ToString(new System.Globalization.DateTimeFormatInfo());
                    data.dateUTC = (uint)Util.ToUnixTime(date);
                }
                if (i == 6)
                    data.duration = Convert.ToUInt32(RetVal[i]);
                if (i == 7)
                    data.cover = Convert.ToUInt32(RetVal[i]);
                if (i == 8)
                    data.amount = Convert.ToUInt32(RetVal[i]);
                if (i == 9)
                    data.simName = RetVal[i];
                if (i == 10)
                    Vector3.TryParse(RetVal[i], out data.globalPos);
                if (i == 11)
                    data.eventFlags = Convert.ToUInt32(RetVal[i]);
                if (i == 12)
                    data.maturity = Convert.ToInt32(RetVal[i]);
            }
            return data;
        }

        public Classified[] GetClassifiedsInRegion(string regionName)
        {
            List<Classified> Classifieds = new List<Classified>();
            List<string> retVal = GD.Query("simname", regionName, "profileclassifieds", "*");
            if (retVal.Count == 0)
                return Classifieds.ToArray();
            int a = 0;
            Classified classified = new Classified();
            for (int i = 0; i < retVal.Count; i++)
            {
                if (a == 0)
                    classified.ClassifiedUUID = retVal[i];
                if (a == 1)
                    classified.CreatorUUID = retVal[i];
                if (a == 2)
                    classified.CreationDate = retVal[i];
                if (a == 3)
                    classified.ExpirationDate = retVal[i];
                if (a == 4)
                    classified.Category = retVal[i];
                if (a == 5)
                    classified.Name = retVal[i];
                if (a == 6)
                    classified.Description = retVal[i];
                if (a == 7)
                    classified.ParcelUUID = retVal[i];
                if (a == 8)
                    classified.ParentEstate = retVal[i];
                if (a == 9)
                    classified.SnapshotUUID = retVal[i];
                if (a == 10)
                    classified.SimName = retVal[i];
                if (a == 11)
                    classified.PosGlobal = retVal[i];
                if (a == 12)
                    classified.ParcelName = retVal[i];
                if (a == 13)
                    classified.ClassifiedFlags = retVal[i];
                if (a == 14)
                    classified.PriceForListing = retVal[i];
                a++;
                if (a == 15)
                {
                    a = 0;
                    Classifieds.Add(classified);
                    classified = new Classified();
                }
            }
            return Classifieds.ToArray();
        }
    }
}
