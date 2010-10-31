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
    public class LocalDirectoryServiceConnector : IDirectoryServiceConnector, IAuroraDataPlugin
    {
        private IGenericData GD = null;
        private int minTimeBeforeNextParcelUpdate = 60;
        private Dictionary<UUID, int> timeBeforeNextUpdate = new Dictionary<UUID, int>();

        public void Initialize(IGenericData GenericData, IConfigSource source, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("DirectoryServiceConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                {
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);
                    minTimeBeforeNextParcelUpdate = source.Configs[Name].GetInt("MinUpdateTimeForParcels", minTimeBeforeNextParcelUpdate);
                }
                GD.ConnectToDatabase(defaultConnectionString);

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
            else
            {
                //Check to make sure that something else exists
                string m_ServerURI = source.Configs["AuroraData"].GetString("RemoteServerURI", "");
                if (m_ServerURI == "") //Blank, not set up
                {
                    OpenSim.Framework.Console.MainConsole.Instance.Output("[AuroraDataService]: Falling back on local connector for " + "DirectoryConnector", "None");
                    
                    GD = GenericData;

                    if (source.Configs[Name] != null)
                        defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                    GD.ConnectToDatabase(defaultConnectionString);

                    DataManager.DataManager.RegisterPlugin(Name, this);
                }
            }
        }

        public string Name
        {
            get { return "IDirectoryServiceConnector"; }
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// This also updates the parcel, not for just adding a new one
        /// </summary>
        /// <param name="args"></param>
        /// <param name="regionID"></param>
        /// <param name="forSale"></param>
        /// <param name="EstateID"></param>
        /// <param name="showInSearch"></param>
        public void AddLandObject(LandData args)
        {
            if (timeBeforeNextUpdate.ContainsKey(args.InfoUUID) &&
                Util.UnixTimeSinceEpoch() < timeBeforeNextUpdate[args.InfoUUID])
                return; //Too soon to update

            //Update the time
            timeBeforeNextUpdate[args.InfoUUID] = Util.UnixTimeSinceEpoch() + (60 * minTimeBeforeNextParcelUpdate);
            
            List<object> Values = new List<object>();
            Values.Add(args.RegionID);
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
            Values.Add(args.InfoUUID);
            Values.Add(((args.Flags & (uint)ParcelFlags.ForSale) == (uint)ParcelFlags.ForSale) ? 1 : 0);
            Values.Add(args.SalePrice);
            Values.Add(args.AuctionID);
            Values.Add(args.Area);
            Values.Add(0);
            Values.Add(args.Maturity);
            Values.Add(args.OwnerID);
            Values.Add(args.GroupID);
            Values.Add(((args.Flags & (uint)ParcelFlags.ShowDirectory) == (uint)ParcelFlags.ShowDirectory) ? 1 : 0);
            Values.Add(args.SnapshotID);

            List<string> Keys = new List<string>();
            Keys.Add("RegionID");
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
            Keys.Add("ForSale");
            Keys.Add("SalePrice");
            Keys.Add("Auction");
            Keys.Add("Area");
            Keys.Add("EstateID");
            Keys.Add("Maturity");
            Keys.Add("OwnerID");
            Keys.Add("GroupID");
            Keys.Add("ShowInSearch");
            Keys.Add("SnapshotID");

            GD.Replace("searchparcel", Keys.ToArray(), Values.ToArray());
        }

        public LandData GetParcelInfo(UUID InfoUUID)
        {
            List<string> Query = GD.Query("InfoUUID", InfoUUID, "searchparcel", "*");
            LandData LandData = new LandData();
            if (Query.Count == 0)
                return null;
            LandData.RegionID = UUID.Parse(Query[0]);
            LandData.GlobalID = UUID.Parse(Query[1]);
            LandData.LocalID = int.Parse(Query[2]);
            LandData.UserLocation = Vector3.Parse(Query[3]);
            LandData.Name = Query[6];
            LandData.Description = Query[7];
            LandData.Flags = uint.Parse(Query[8]);
            LandData.Dwell = int.Parse(Query[9]);
            LandData.InfoUUID = UUID.Parse(Query[10]);
            LandData.AuctionID = uint.Parse(Query[13]);
            LandData.Area = int.Parse(Query[14]);
            LandData.Maturity = int.Parse(Query[16]);
            LandData.OwnerID = UUID.Parse(Query[17]);
            LandData.GroupID = UUID.Parse(Query[18]);
            LandData.SnapshotID = UUID.Parse(Query[20]);
            return LandData;
        }

        public LandData[] GetParcelByOwner(UUID OwnerID)
        {
            List<LandData> Lands = new List<LandData>();
            List<string> Query = GD.Query("OwnerID", OwnerID, "searchparcel", "*");
            if (Query.Count == 0)
                return Lands.ToArray();
            int i = 0;
            int DataCount = 0;
            LandData LandData = new LandData();
            foreach (string RetVal in Query)
            {
                if (DataCount == 0)
                    LandData.RegionID = UUID.Parse(Query[i]);
                if (DataCount == 1)
                    LandData.GlobalID = UUID.Parse(Query[i]);
                if (DataCount == 2)
                    LandData.LocalID = int.Parse(Query[i]);
                if (DataCount == 3)
                    LandData.UserLocation = Vector3.Parse(Query[i]);
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
                if (DataCount == 13)
                    LandData.AuctionID = uint.Parse(Query[i]);
                if (DataCount == 14)
                    LandData.Area = int.Parse(Query[i]);
                if (DataCount == 16)
                    LandData.Maturity = int.Parse(Query[i]);
                if (DataCount == 17)
                    LandData.OwnerID = UUID.Parse(Query[i]);
                if (DataCount == 18)
                    LandData.GroupID = UUID.Parse(Query[i]);
                if (DataCount == 20)
                    LandData.SnapshotID = UUID.Parse(Query[i]);

                DataCount++;
                i++;
                if (DataCount == 21)
                {
                    Lands.Add(LandData);
                    LandData = new LandData();
                    DataCount = 0;
                }
            }
            return Lands.ToArray();
        }

        public DirPlacesReplyData[] FindLand(string queryText, string category, int StartQuery, uint Flags)
        {
            List<DirPlacesReplyData> Data = new List<DirPlacesReplyData>();

            string categoryString = "";
            string dwell = "";

            if (category != "-1")
                categoryString = " PCategory = '" + category + "' &&";

            if ((Flags & (uint)DirectoryManager.DirFindFlags.DwellSort) == (uint)DirectoryManager.DirFindFlags.DwellSort)
                dwell = " ORDER BY Dwell DESC";

            string whereClause = categoryString + " Description LIKE '%" + queryText + "%' OR Name LIKE '%" + queryText + "%' and ShowInSearch = 'True'"  + dwell + " LIMIT " + StartQuery.ToString() + ",50 ";
            List<string> retVal = GD.Query(whereClause, "searchparcel", "InfoUUID,Name,ForSale,Auction,Dwell,Maturity");
            if (retVal.Count == 0)
                return Data.ToArray();

            DirPlacesReplyData replyData = new DirPlacesReplyData();

            for (int i = 0; i < retVal.Count; i += 6)
            {
                replyData.parcelID = new UUID(retVal[i]);
                replyData.name = retVal[i + 1];
                replyData.forSale = int.Parse(retVal[i + 2]) == 1;
                replyData.auction = retVal[i + 3] == "0";
                replyData.dwell = float.Parse(retVal[i + 4]);
                if (int.Parse(retVal[i + 5]) == 0 && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludePG)) == (uint)DirectoryManager.DirFindFlags.IncludePG)
                    Data.Add(replyData);
                else if (int.Parse(retVal[i + 5]) == 1 && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeMature)) == (uint)DirectoryManager.DirFindFlags.IncludeMature)
                    Data.Add(replyData);
                else if (int.Parse(retVal[i + 5]) == 2 && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeAdult)) == (uint)DirectoryManager.DirFindFlags.IncludeAdult)
                    Data.Add(replyData);
                replyData = new DirPlacesReplyData();
            }

            return Data.ToArray();
        }

        public DirLandReplyData[] FindLandForSale(string searchType, string price, string area, int StartQuery, uint Flags)
        {
            //searchType
            // 2 - Auction only
            // 8 - For Sale - Mainland
            // 16 - For Sale - Estate
            // 4294967295 - All
            List<DirLandReplyData> Data = new List<DirLandReplyData>();

            string dwell = "";
            string pricestring = "";
            string areastring = "";
            string forsalestring = "";

            if ((Flags & (uint)DirectoryManager.DirFindFlags.LimitByPrice) == (uint)DirectoryManager.DirFindFlags.LimitByPrice)
                pricestring = " SalePrice <= '" + price + "'";

            if ((Flags & (uint)DirectoryManager.DirFindFlags.LimitByArea) == (uint)DirectoryManager.DirFindFlags.LimitByArea)
            {
                areastring = pricestring == "" ? "" : " and";
                areastring += " Area >= '" + area + "'";
            }

            if (areastring != "" || pricestring != "")
                forsalestring = " and";

            forsalestring += " ForSale = 'True'";

            string whereClause = pricestring + areastring + forsalestring + " LIMIT " + StartQuery.ToString() + ",50 " + dwell;
            List<string> retVal = GD.Query(whereClause, "searchparcel", "InfoUUID,Name,Auction,SalePrice,Area");

            if (retVal.Count == 0)
                return Data.ToArray();

            DirLandReplyData replyData;
            for (int i = 0; i < retVal.Count; i += 6)
            {
                replyData = new DirLandReplyData();
                replyData.forSale = true;
                replyData.parcelID = new UUID(retVal[i]);
                replyData.name = retVal[i + 1];
                replyData.auction = (retVal[i + 2] != "0");
                if ((Flags & (uint)DirectoryManager.SearchTypeFlags.Auction) == (uint)DirectoryManager.SearchTypeFlags.Auction && !replyData.auction)
                    continue;
                replyData.salePrice = Convert.ToInt32(retVal[i + 3]);
                replyData.actualArea = Convert.ToInt32(retVal[i + 4]);
                if (int.Parse(retVal[i + 5]) == 0 && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludePG)) == (uint)DirectoryManager.DirFindFlags.IncludePG)
                    Data.Add(replyData);
                else if (int.Parse(retVal[i + 5]) == 1 && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeMature)) == (uint)DirectoryManager.DirFindFlags.IncludeMature)
                    Data.Add(replyData);
                else if (int.Parse(retVal[i + 5]) == 2 && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeAdult)) == (uint)DirectoryManager.DirFindFlags.IncludeAdult)
                    Data.Add(replyData);
            }

            return Data.ToArray();
        }

        public DirEventsReplyData[] FindEvents(string queryText, string flags, int StartQuery)
        {
            List<DirEventsReplyData> Data = new List<DirEventsReplyData>();
            string whereClause = "";
            uint eventFlags = uint.Parse(flags);
            if (queryText.Contains("|0|"))
            {
                string StringDay = queryText.Split('|')[0];
                if (StringDay == "u")
                {
                    whereClause = " EDate >= '" + Util.ToUnixTime(DateTime.Today) + "' LIMIT " + StartQuery.ToString() + ",50 ";
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
                whereClause = " EName LIKE '%" + queryText + "%' LIMIT " + StartQuery.ToString() + ",50 ";
            }
            List<string> retVal = GD.Query(whereClause, "events", "EOwnerID,EName,EID,EDate,EFlags,EMature");
            if (retVal.Count == 0)
                return Data.ToArray();

            DirEventsReplyData replyData;
            for (int i = 0; i < retVal.Count; i += 6)
            {
                replyData = new DirEventsReplyData();
                replyData.ownerID = new UUID(retVal[i]);
                replyData.name = retVal[i + 1];
                replyData.eventID = Convert.ToUInt32(retVal[i + 2]);
                DateTime date = Util.ToDateTime(ulong.Parse(retVal[i + 3]));
                replyData.date = date.ToString(new System.Globalization.DateTimeFormatInfo());
                replyData.unixTime = (uint)Util.ToUnixTime(date);
                replyData.eventFlags = Convert.ToUInt32(retVal[i + 4]);

                if (Convert.ToInt32(retVal[i + 5]) == 2 && (eventFlags & (uint)DirectoryManager.EventFlags.Adult) == (uint)DirectoryManager.EventFlags.Adult)
                    Data.Add(replyData);
                else if (Convert.ToInt32(retVal[i + 5]) == 1 && (eventFlags & (uint)DirectoryManager.EventFlags.Mature) == (uint)DirectoryManager.EventFlags.Mature)
                    Data.Add(replyData);
                else if (Convert.ToInt32(retVal[i + 5]) == 0 && (eventFlags & (uint)DirectoryManager.EventFlags.PG) == (uint)DirectoryManager.EventFlags.PG)
                    Data.Add(replyData);
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
            //TODO: BROKEN!
            List<DirClassifiedReplyData> Data = new List<DirClassifiedReplyData>();

            string whereClause = "";
            string classifiedClause = "";
            uint cqf = uint.Parse(queryFlags);

            if (category != "0")
                classifiedClause = " and category = '" + category + "'";

            whereClause = " name LIKE '%" + queryText + "%'" + classifiedClause + " LIMIT " + StartQuery.ToString() + ",50 ";
            List<string> retVal = GD.Query(whereClause, "profileclassifieds", "classifiedflags, classifieduuid, creationdate, expirationdate, priceforlisting, name");
            if (retVal.Count == 0)
                return Data.ToArray();

            DirClassifiedReplyData replyData = null;
            for (int i = 0; i < retVal.Count; i += 6)
            {
                replyData = new DirClassifiedReplyData();
                replyData.classifiedFlags = Convert.ToByte(retVal[i]);
                replyData.classifiedID = new UUID(retVal[i + 1]);
                replyData.creationDate = Convert.ToUInt32(retVal[i + 2]);
                replyData.expirationDate = Convert.ToUInt32(retVal[i + 3]);
                replyData.price = Convert.ToInt32(retVal[i + 4]);
                replyData.name = retVal[i + 5];
                if ((replyData.classifiedFlags & (uint)DirectoryManager.ClassifiedFlags.Mature) == (uint)DirectoryManager.ClassifiedFlags.Mature)
                {
                    if ((cqf & (uint)DirectoryManager.ClassifiedQueryFlags.Mature) == (uint)DirectoryManager.ClassifiedQueryFlags.Mature)
                        Data.Add(replyData);
                }
                else
                    Data.Add(replyData);
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
            //TODO: BROKEN!
            List<Classified> Classifieds = new List<Classified>();
            List<string> retVal = GD.Query("simname", regionName, "profileclassifieds", "*");
            if (retVal.Count == 0)
                return Classifieds.ToArray();
            int a = 0;
            Classified classified = new Classified();
            for (int i = 0; i < retVal.Count; i++)
            {
                if (a == 0)
                    classified.ClassifiedUUID = UUID.Parse(retVal[i]);
                if (a == 1)
                    classified.CreatorUUID = UUID.Parse(retVal[i]);
                if (a == 2)
                    classified.CreationDate = uint.Parse(retVal[i]);
                if (a == 3)
                    classified.ExpirationDate = uint.Parse(retVal[i]);
                if (a == 4)
                    classified.Category = uint.Parse(retVal[i]);
                if (a == 5)
                    classified.Name = retVal[i];
                if (a == 6)
                    classified.Description = retVal[i];
                if (a == 7)
                    classified.ParcelUUID = UUID.Parse(retVal[i]);
                if (a == 8)
                    classified.ParentEstate = uint.Parse(retVal[i]);
                if (a == 9)
                    classified.SnapshotUUID = UUID.Parse(retVal[i]);
                if (a == 10)
                    classified.SimName = retVal[i];
                if (a == 11)
                    classified.GlobalPos = Vector3.Parse(retVal[i]);
                if (a == 12)
                    classified.ParcelName = retVal[i];
                if (a == 13)
                    classified.ClassifiedFlags = byte.Parse(retVal[i]);
                if (a == 14)
                    classified.PriceForListing = int.Parse(retVal[i]);
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
