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
using OpenMetaverse.StructuredData;

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
            //Check whether this region is just spamming add to search and stop them if they are
            if (timeBeforeNextUpdate.ContainsKey(args.InfoUUID) &&
                Util.UnixTimeSinceEpoch() < timeBeforeNextUpdate[args.InfoUUID])
                return; //Too soon to update

            //Update the time with now + the time to wait for the next update
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

        /// <summary>
        /// Gets a parcel from the search database by Info UUID (the true cross instance parcel ID)
        /// </summary>
        /// <param name="ParcelID"></param>
        /// <returns></returns>
        public LandData GetParcelInfo(UUID InfoUUID)
        {
            //Get info about a specific parcel somewhere in the metaverse
            List<string> Query = GD.Query("InfoUUID", InfoUUID, "searchparcel", "*");
            //Cant find it, return
            if (Query.Count == 0)
                return null;

            //Parse and return
            LandData LandData = new LandData();
            LandData.RegionID = UUID.Parse(Query[0]);
            LandData.GlobalID = UUID.Parse(Query[1]);
            LandData.LocalID = int.Parse(Query[2]);
            LandData.UserLocation = new Vector3(int.Parse(Query[3]), int.Parse(Query[4]), int.Parse(Query[5]));
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

        /// <summary>
        /// Gets all parcels owned by the given user
        /// </summary>
        /// <param name="OwnerID"></param>
        /// <returns></returns>
        public LandData[] GetParcelByOwner(UUID OwnerID)
        {
            List<LandData> Lands = new List<LandData>();
            //NOTE: this does check for group deeded land as well, so this can check for that as well
            List<string> Query = GD.Query("OwnerID", OwnerID, "searchparcel", "*");
            //Return if no values
            if (Query.Count == 0)
                return Lands.ToArray();
            
            LandData LandData = new LandData();
            //Add all the parcels belonging to the owner to the list
            for (int i = 0; i < Query.Count; i += 21)
            {
                LandData.RegionID = UUID.Parse(Query[i]);
                LandData.GlobalID = UUID.Parse(Query[i + 1]);
                LandData.LocalID = int.Parse(Query[i + 2]);
                LandData.UserLocation = new Vector3(int.Parse(Query[i + 3]), int.Parse(Query[i + 4]), int.Parse(Query[i + 5]));
                LandData.Name = Query[i + 6];
                LandData.Description = Query[i + 7];
                LandData.Flags = uint.Parse(Query[i + 8]);
                LandData.Dwell = int.Parse(Query[i + 9]);
                LandData.InfoUUID = UUID.Parse(Query[i + 10]);
                LandData.AuctionID = uint.Parse(Query[i + 13]);
                LandData.Area = int.Parse(Query[i + 14]);
                LandData.Maturity = int.Parse(Query[i + 16]);
                LandData.OwnerID = UUID.Parse(Query[i + 17]);
                LandData.GroupID = UUID.Parse(Query[i + 18]);
                LandData.SnapshotID = UUID.Parse(Query[i + 20]);

                Lands.Add(LandData);
                LandData = new LandData();
            }
            return Lands.ToArray();
        }

        /// <summary>
        /// Searches for parcels around the grid
        /// </summary>
        /// <param name="queryText"></param>
        /// <param name="category"></param>
        /// <param name="StartQuery"></param>
        /// <returns></returns>
        public DirPlacesReplyData[] FindLand(string queryText, string category, int StartQuery, uint Flags)
        {
            List<DirPlacesReplyData> Data = new List<DirPlacesReplyData>();

            string categoryString = "";
            string dwell = "";

            if (category != "-1") //Check for category
                categoryString = " PCategory = '" + category + "' &&";

            //If they dwell sort flag is there, sort by dwell going down
            if ((Flags & (uint)DirectoryManager.DirFindFlags.DwellSort) == (uint)DirectoryManager.DirFindFlags.DwellSort)
                dwell = " ORDER BY Dwell DESC";

            string whereClause = categoryString + " Description LIKE '%" + queryText + "%' OR Name LIKE '%" + queryText + "%' and ShowInSearch = '1'"  + dwell + " LIMIT " + StartQuery.ToString() + ",50 ";
            List<string> retVal = GD.Query(whereClause, "searchparcel", "InfoUUID,Name,ForSale,Auction,Dwell,Maturity");
            if (retVal.Count == 0)
                return Data.ToArray();

            DirPlacesReplyData replyData = new DirPlacesReplyData();

            for (int i = 0; i < retVal.Count; i += 6)
            {
                replyData.parcelID = new UUID(retVal[i]);
                replyData.name = retVal[i + 1];
                replyData.forSale = int.Parse(retVal[i + 2]) == 1;
                replyData.auction = retVal[i + 3] == "0"; //Auction is stored as a 0 if there is no auction
                replyData.dwell = float.Parse(retVal[i + 4]);

                //Check to make sure we are sending the requested maturity levels
                if (int.Parse(retVal[i + 5]) <= 0 && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludePG)) == (uint)DirectoryManager.DirFindFlags.IncludePG)
                    Data.Add(replyData);
                else if (int.Parse(retVal[i + 5]) <= 1 && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeMature)) == (uint)DirectoryManager.DirFindFlags.IncludeMature)
                    Data.Add(replyData);
                else if (int.Parse(retVal[i + 5]) <= 2 && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeAdult)) == (uint)DirectoryManager.DirFindFlags.IncludeAdult)
                    Data.Add(replyData);
                replyData = new DirPlacesReplyData();
            }

            return Data.ToArray();
        }

        /// <summary>
        /// Searches for parcels for sale around the grid
        /// </summary>
        /// <param name="searchType"></param>
        /// <param name="price"></param>
        /// <param name="area"></param>
        /// <param name="StartQuery"></param>
        /// <returns></returns>
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

            //They requested a sale price check
            if ((Flags & (uint)DirectoryManager.DirFindFlags.LimitByPrice) == (uint)DirectoryManager.DirFindFlags.LimitByPrice)
                pricestring = " SalePrice <= '" + price + "'";

            //They requested a 
            if ((Flags & (uint)DirectoryManager.DirFindFlags.LimitByArea) == (uint)DirectoryManager.DirFindFlags.LimitByArea)
            {
                //Check to make sure we add the 'and' into the SQL statement
                // If the price string exists, we need the and inbetween these two statements
                areastring = pricestring == "" ? "" : " and";
                //Add the area command
                areastring += " Area >= '" + area + "'";
            }
            //If either exists, we need the 'and' between these and the for sale statement
            if (areastring != "" || pricestring != "")
                forsalestring = " and";

            //Only parcels set for sale will be checked
            forsalestring += " ForSale = '1'";

            string whereClause = pricestring + areastring + forsalestring + " LIMIT " + StartQuery.ToString() + ",50 " + dwell;
            List<string> retVal = GD.Query(whereClause, "searchparcel", "InfoUUID,Name,Auction,SalePrice,Area,Maturity");

            //if there are none, return
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
                //If its an auction and we didn't request to see auctions, skip to the next and continue
                if ((Flags & (uint)DirectoryManager.SearchTypeFlags.Auction) == (uint)DirectoryManager.SearchTypeFlags.Auction && !replyData.auction)
                    continue;

                replyData.salePrice = Convert.ToInt32(retVal[i + 3]);
                replyData.actualArea = Convert.ToInt32(retVal[i + 4]);
                //0 flag is an override so that we can get all lands for sale, regardless of maturity
                if (Flags == 0)
                    Data.Add(replyData);
                //Check maturity levels depending on what flags the user has set
                if (int.Parse(retVal[i + 5]) == 0 && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludePG)) == (uint)DirectoryManager.DirFindFlags.IncludePG)
                    Data.Add(replyData);
                else if (int.Parse(retVal[i + 5]) == 1 && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeMature)) == (uint)DirectoryManager.DirFindFlags.IncludeMature)
                    Data.Add(replyData);
                else if (int.Parse(retVal[i + 5]) == 2 && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeAdult)) == (uint)DirectoryManager.DirFindFlags.IncludeAdult)
                    Data.Add(replyData);
            }

            return Data.ToArray();
        }

        /// <summary>
        /// Searches for events with the given parameters
        /// </summary>
        /// <param name="queryText"></param>
        /// <param name="flags"></param>
        /// <param name="StartQuery"></param>
        /// <returns></returns>
        public DirEventsReplyData[] FindEvents(string queryText, string flags, int StartQuery)
        {
            List<DirEventsReplyData> Data = new List<DirEventsReplyData>();
            string whereClause = "";
            uint eventFlags = uint.Parse(flags);
            //|0| means search between some days
            if (queryText.Contains("|0|"))
            {
                string StringDay = queryText.Split('|')[0];
                if (StringDay == "u") //"u" means search for events that are going on today
                {
                    whereClause = " EDate >= '" + Util.ToUnixTime(DateTime.Today) + "' LIMIT " + StartQuery.ToString() + ",50 ";
                }
                else
                {
                    //Pull the day out then and search for that many days in the future/past
                    int Day = int.Parse(StringDay);
                    DateTime SearchedDay = DateTime.Today.AddDays(Day);
                    //We only look at one day at a time
                    DateTime NextDay = SearchedDay.AddDays(1);
                    whereClause = " EDate >= '" + Util.ToUnixTime(SearchedDay) + "' and EDate <= '" + Util.ToUnixTime(NextDay) + "' and EFlags <= '" + flags + "' LIMIT " + StartQuery.ToString() + ",50 ";
                }
            }
            else
            {
                //Else, search for the search term
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

                //Check the maturity levels
                if (Convert.ToInt32(retVal[i + 5]) == 2 && (eventFlags & (uint)DirectoryManager.EventFlags.Adult) == (uint)DirectoryManager.EventFlags.Adult)
                    Data.Add(replyData);
                else if (Convert.ToInt32(retVal[i + 5]) == 1 && (eventFlags & (uint)DirectoryManager.EventFlags.Mature) == (uint)DirectoryManager.EventFlags.Mature)
                    Data.Add(replyData);
                else if (Convert.ToInt32(retVal[i + 5]) == 0 && (eventFlags & (uint)DirectoryManager.EventFlags.PG) == (uint)DirectoryManager.EventFlags.PG)
                    Data.Add(replyData);
            }

            return Data.ToArray();
        }

        /// <summary>
        /// Retrives all events in the given region by their maturity level
        /// </summary>
        /// <param name="regionName"></param>
        /// <param name="maturity">Uses DirectoryManager.EventFlags to determine the maturity requested</param>
        /// <returns></returns>
        public DirEventsReplyData[] FindAllEventsInRegion(string regionName, int maturity)
        {
            List<DirEventsReplyData> Data = new List<DirEventsReplyData>();
            
            List<string> retVal = GD.Query("ESimName", regionName, "events", "EOwnerID,EName,EID,EDate,EFlags,EMature");
            
            if (retVal.Count == 0)
                return Data.ToArray();
            
            DirEventsReplyData replyData = new DirEventsReplyData();
            for (int i = 0; i < retVal.Count; i += 6)
            {
                //Check maturity level
                if(int.Parse(retVal[i+5]) != maturity)
                    continue;

                replyData.ownerID = new UUID(retVal[i]);
                replyData.name = retVal[i + 1];
                replyData.eventID = Convert.ToUInt32(retVal[i + 2]);

                //Parse the date for the viewer
                DateTime date = Util.ToDateTime(ulong.Parse(retVal[i + 3]));
                replyData.date = date.ToString(new System.Globalization.DateTimeFormatInfo());
                replyData.unixTime = (uint)Util.ToUnixTime(date);

                replyData.eventFlags = Convert.ToUInt32(retVal[i + 4]);
                Data.Add(replyData);
                replyData = new DirEventsReplyData();
            }

            return Data.ToArray();
        }

        /// <summary>
        /// Searches for classifieds
        /// </summary>
        /// <param name="queryText"></param>
        /// <param name="category"></param>
        /// <param name="queryFlags"></param>
        /// <param name="StartQuery"></param>
        /// <returns></returns>
        public DirClassifiedReplyData[] FindClassifieds(string queryText, string category, string queryFlags, int StartQuery)
        {
            List<DirClassifiedReplyData> Data = new List<DirClassifiedReplyData>();

            string whereClause = "";
            string classifiedClause = "";
            uint cqf = uint.Parse(queryFlags);

            if (int.Parse(category) != (int)DirectoryManager.ClassifiedCategories.Any) //Check the category
                classifiedClause = " and Category = '" + category + "'";

            whereClause = " Name LIKE '%" + queryText + "%'" + classifiedClause + " LIMIT " + StartQuery.ToString() + ",50 ";
            List<string> retVal = GD.Query(whereClause, "profileclassifieds", "*");
            if (retVal.Count == 0)
                return Data.ToArray();

            DirClassifiedReplyData replyData = null;
            for (int i = 0; i < retVal.Count; i += 5)
            {
                //Pull the classified out of OSD
                Classified classified = new Classified();
                classified.FromOSD((OSDMap)OSDParser.DeserializeJson(retVal[i + 4]));

                replyData = new DirClassifiedReplyData();
                replyData.classifiedFlags = classified.ClassifiedFlags;
                replyData.classifiedID = classified.ClassifiedUUID;
                replyData.creationDate = classified.CreationDate;
                replyData.expirationDate = classified.ExpirationDate;
                replyData.price = classified.PriceForListing;
                replyData.name = classified.Name;
                //Check maturity levels
                if ((replyData.classifiedFlags & (uint)DirectoryManager.ClassifiedFlags.Mature) == (uint)DirectoryManager.ClassifiedFlags.Mature)
                {
                    if ((cqf & (uint)DirectoryManager.ClassifiedQueryFlags.Mature) == (uint)DirectoryManager.ClassifiedQueryFlags.Mature)
                        Data.Add(replyData);
                }
                else //Its PG, add all
                    Data.Add(replyData);
            }
            return Data.ToArray();
        }

        /// <summary>
        /// Gets more info about the event by the events unique event ID
        /// </summary>
        /// <param name="EventID"></param>
        /// <returns></returns>
        public EventData GetEventInfo(string EventID)
        {
            EventData data = new EventData();
            List<string> RetVal = GD.Query("EID", EventID, "events", "EID, ECreatorID, EName, ECategory, EDesc, EDate, EDuration, ECoverCharge, ECoverAmount, ESimName, EGlobalPos, EFlags, EMature");
            if (RetVal.Count == 0)
                return null;
            for (int i = 0; i < RetVal.Count; i += 12)
            {
                data.eventID = Convert.ToUInt32(RetVal[i]);
                data.creator = RetVal[i + 1];
                data.name = RetVal[i + 2];
                data.category = RetVal[i + 3];
                data.description = RetVal[i + 4];
                //Parse the time out for the viewer
                DateTime date = Util.ToDateTime(ulong.Parse(RetVal[i + 5]));
                data.date = date.ToString(new System.Globalization.DateTimeFormatInfo());
                data.dateUTC = (uint)Util.ToUnixTime(date);

                data.duration = Convert.ToUInt32(RetVal[i + 6]);
                data.cover = Convert.ToUInt32(RetVal[i + 7]);
                data.amount = Convert.ToUInt32(RetVal[i + 8]);
                data.simName = RetVal[i + 9];
                Vector3.TryParse(RetVal[i + 10], out data.globalPos);
                data.eventFlags = Convert.ToUInt32(RetVal[i + 11]);
                data.maturity = Convert.ToInt32(RetVal[i + 12]);
            }
            return data;
        }

        /// <summary>
        /// Gets all classifieds in the given region
        /// </summary>
        /// <param name="regionName"></param>
        /// <returns></returns>
        public Classified[] GetClassifiedsInRegion(string regionName)
        {
            List<Classified> Classifieds = new List<Classified>();
            List<string> retVal = GD.Query("SimName", regionName, "profileclassifieds", "*");
            
            if (retVal.Count == 0)
                return Classifieds.ToArray();
            
            Classified classified = new Classified();
            for (int i = 0; i < retVal.Count; i += 5)
            {
                //Pull the classified out of OSD
                classified.FromOSD((OSDMap)OSDParser.DeserializeJson(retVal[i + 4]));
                Classifieds.Add(classified);
                classified = new Classified();
            }
            return Classifieds.ToArray();
        }

        /// <summary>
        /// Add classifieds to the search database
        /// LOCAL Only, called by the profile service
        /// </summary>
        /// <param name="dictionary">objects of the dictionary are OSDMaps made from Classified</param>
        public void AddClassifieds(Dictionary<string, object> dictionary)
        {
            //Add a dictionary of classifieds
            foreach (object o in dictionary.Values)
            {
                //Pull out the OSD map and make it into a classified
                OSDMap map = (OSDMap)o;
                Classified c = new Classified();
                c.FromOSD(map);
                List<object> Values = new List<object>();
                Values.Add(c.Name);
                Values.Add(c.Category);
                Values.Add(c.SimName);
                Values.Add(c.ClassifiedUUID);
                Values.Add(OSDParser.SerializeJsonString(map));
                GD.Insert("profileclassifieds", Values.ToArray());
            }
        }

        /// <summary>
        /// Remove classifieds from the search database
        /// LOCAL Only, called by the profile service
        /// </summary>
        /// <param name="dictionary">objects of the dictionary are OSDMaps made from Classified</param>
        public void RemoveClassifieds(Dictionary<string, object> dictionary)
        {
            //Remove all the UUIDs in the given dictionary from search
            foreach (object o in dictionary.Values)
            {
                //Pull out the OSDMaps
                OSDMap map = (OSDMap)o;
                Classified c = new Classified();
                c.FromOSD(map);
                List<string> Keys = new List<string>();
                Keys.Add("ClassifiedUUID");
                List<object> Values = new List<object>();
                Values.Add(c.ClassifiedUUID);
                GD.Delete("profileclassifieds", Keys.ToArray(), Values.ToArray());
            }
        }
    }
}
