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
using System.Globalization;
using System.Linq;
using System.Reflection;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using log4net;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Services.DataService
{
    public class LocalDirectoryServiceConnector : IDirectoryServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IGenericData GD;
        private IRegistryCore m_registry;

        #region IDirectoryServiceConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            GD = GenericData;
            m_registry = simBase;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase(defaultConnectionString, "Directory",
                                 source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            DataManager.DataManager.RegisterPlugin(Name + "Local", this);

            if (source.Configs["AuroraConnectors"].GetString("DirectoryServiceConnector", "LocalConnector") ==
                "LocalConnector")
            {
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IDirectoryServiceConnector"; }
        }

        /// <summary>
        ///   This also updates the parcel, not for just adding a new one
        /// </summary>
        /// <param name = "args"></param>
        /// <param name = "regionID"></param>
        /// <param name = "forSale"></param>
        /// <param name = "EstateID"></param>
        /// <param name = "showInSearch"></param>
        public void AddRegion(List<LandData> parcels)
        {
            if (parcels.Count == 0)
                return;

            ClearRegion(parcels[0].RegionID);
            List<object[]> insertValues = new List<object[]>();
#if (!ISWIN)
            foreach (LandData args in parcels)
            {
                List<object> Values = new List<object>
                                          {
                                              args.RegionID,
                                              args.GlobalID,
                                              args.LocalID,
                                              args.UserLocation.X,
                                              args.UserLocation.Y,
                                              args.UserLocation.Z,
                                              args.Name.MySqlEscape(50),
                                              args.Description.MySqlEscape(255),
                                              args.Flags,
                                              args.Dwell,
                                              args.InfoUUID,
                                              ((args.Flags & (uint) ParcelFlags.ForSale) == (uint) ParcelFlags.ForSale)
                                                  ? 1
                                                  : 0,
                                              args.SalePrice,
                                              args.AuctionID,
                                              args.Area,
                                              0,
                                              args.Maturity,
                                              args.OwnerID,
                                              args.GroupID,
                                              ((args.Flags & (uint) ParcelFlags.ShowDirectory) ==
                                               (uint) ParcelFlags.ShowDirectory)
                                                  ? 1
                                                  : 0,
                                              args.SnapshotID,
                                              OSDParser.SerializeLLSDXmlString(args.Bitmap)
                                          };
                //InfoUUID is the missing 'real' Gridwide ParcelID

                insertValues.Add(Values.ToArray());
            }
#else
            foreach (List<object> Values in parcels.Select(args => new List<object>
                                                               {
                                                                   args.RegionID,
                                                                   args.GlobalID,
                                                                   args.LocalID,
                                                                   args.UserLocation.X,
                                                                   args.UserLocation.Y,
                                                                   args.UserLocation.Z,
                                                                   args.Name.MySqlEscape(50),
                                                                   args.Description.MySqlEscape(255),
                                                                   args.Flags,
                                                                   args.Dwell,
                                                                   args.InfoUUID,
                                                                   ((args.Flags & (uint) ParcelFlags.ForSale) == (uint) ParcelFlags.ForSale)
                                                                       ? 1
                                                                       : 0,
                                                                   args.SalePrice,
                                                                   args.AuctionID,
                                                                   args.Area,
                                                                   0,
                                                                   args.Maturity,
                                                                   args.OwnerID,
                                                                   args.GroupID,
                                                                   ((args.Flags & (uint) ParcelFlags.ShowDirectory) ==
                                                                    (uint) ParcelFlags.ShowDirectory)
                                                                       ? 1
                                                                       : 0,
                                                                   args.SnapshotID,
                                                                   OSDParser.SerializeLLSDXmlString(args.Bitmap)
                                                               }))
            {
                //InfoUUID is the missing 'real' Gridwide ParcelID

                insertValues.Add(Values.ToArray());
            }
#endif
            GD.InsertMultiple("searchparcel", insertValues);
        }

        public void ClearRegion(UUID regionID)
        {
            GD.Delete("searchparcel", new string[1] {"RegionID"}, new object[1] {regionID});
        }

        private static List<LandData> Query2LandData(List<string> Query)
        {
            List<LandData> Lands = new List<LandData>();

            LandData LandData;

            for (int i = 0; i < Query.Count; i += 22)
            {
                LandData = new LandData();
                LandData.RegionID = UUID.Parse(Query[i]);
                LandData.GlobalID = UUID.Parse(Query[i + 1]);
                LandData.LocalID = int.Parse(Query[i + 2]);
                LandData.UserLocation = new Vector3(float.Parse(Query[i + 3]), float.Parse(Query[i + 4]),
                                                    float.Parse(Query[i + 5]));
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
                try
                {
                    LandData.Bitmap = OSDParser.DeserializeLLSDXml(Query[i + 21]);
                }
                catch
                {
                }

                Lands.Add(LandData);
            }
            return Lands;
        }

        /// <summary>
        ///   Gets a parcel from the search database by Info UUID (the true cross instance parcel ID)
        /// </summary>
        /// <param name = "ParcelID"></param>
        /// <returns></returns>
        public LandData GetParcelInfo(UUID InfoUUID)
        {
            //Split the InfoUUID so that we get the regions, we'll check for positions in a bit
            int RegionX, RegionY;
            uint X, Y;
            ulong RegionHandle;
            Util.ParseFakeParcelID(InfoUUID, out RegionHandle, out X, out Y);

            Util.UlongToInts(RegionHandle, out RegionX, out RegionY);

            GridRegion r = m_registry.RequestModuleInterface<IGridService>().GetRegionByPosition(UUID.Zero, RegionX,
                                                                                                 RegionY);
            if (r == null)
            {
                m_log.Warn("[DirectoryService]: Could not find region for ParcelID: " + InfoUUID);
                return null;
            }
            //Get info about a specific parcel somewhere in the metaverse
            List<string> Query = GD.Query("RegionID", r.RegionID, "searchparcel", "*");
            //Cant find it, return
            if (Query.Count == 0)
                return null;

            List<LandData> Lands = Query2LandData(Query);
            LandData LandData = null;

            bool[,] tempConvertMap = new bool[r.RegionSizeX/4,r.RegionSizeX/4];
            tempConvertMap.Initialize();
#if (!ISWIN)
            foreach (LandData land in Lands)
            {
                if (land.Bitmap != null)
                {
                    ConvertBytesToLandBitmap(ref tempConvertMap, land.Bitmap, r.RegionSizeX);
                    if (tempConvertMap[X/64, Y/64])
                    {
                        LandData = land;
                        break;
                    }
                }
            }
#else
            foreach (LandData land in Lands.Where(land => land.Bitmap != null))
            {
                ConvertBytesToLandBitmap(ref tempConvertMap, land.Bitmap, r.RegionSizeX);
                if (tempConvertMap[X/64, Y/64])
                {
                    LandData = land;
                    break;
                }
            }
#endif
            if (LandData == null && Lands.Count != 0)
                LandData = Lands[0];
            return LandData;
        }

        /// <summary>
        ///   Gets all parcels owned by the given user
        /// </summary>
        /// <param name = "OwnerID"></param>
        /// <returns></returns>
        public LandData[] GetParcelByOwner(UUID OwnerID)
        {
            List<LandData> Lands = new List<LandData>();
            //NOTE: this does check for group deeded land as well, so this can check for that as well
            List<string> Query = GD.Query("OwnerID", OwnerID, "searchparcel", "*");
            //Return if no values
            if (Query.Count == 0)
                return Lands.ToArray();

            return Query2LandData(Query).ToArray();
        }

        private static string GetParcelsByRegionWhereClause(UUID RegionID, UUID scopeID, UUID owner, ParcelFlags flags, ParcelCategory category)
        {
            string whereClause = string.Format("RegionID = '{0}'", RegionID);

            if (owner != UUID.Zero)
            {
                whereClause += string.Format(" AND OwnerID = '{0}'", owner);
            }

            if (flags != ParcelFlags.None)
            {
                whereClause += string.Format(" AND Flags & {0}", flags);
            }

            if (category != ParcelCategory.Any)
            {
//                whereClause += string.Format(" AND Category = {0,D}", category);
            }
            return whereClause;
        }
        
        public List<LandData> GetParcelsByRegion(uint start, uint count, UUID RegionID, UUID scopeID, UUID owner, ParcelFlags flags, ParcelCategory category)
        {
            List<LandData> resp = new List<LandData>(0);
            if (count == 0)
            {
                return resp;
            }

            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                GridRegion region = regiondata.Get(RegionID, scopeID);
                if (region != null)
                {
                    string whereClause = GetParcelsByRegionWhereClause(RegionID, scopeID, owner, flags, category);
                    whereClause += " ORDER BY OwnerID DESC, Name DESC";
                    whereClause += string.Format(" LIMIT {0}, {1}", start, count);
                    return Query2LandData(GD.Query(whereClause, "searchparcel", "*"));
                }
            }
            return resp;
        }

        public uint GetNumberOfParcelsByRegion(UUID RegionID, UUID scopeID, UUID owner, ParcelFlags flags, ParcelCategory category)
        {
            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                GridRegion region = regiondata.Get(RegionID, scopeID);
                if (region != null)
                {
                    return uint.Parse(GD.Query(GetParcelsByRegionWhereClause(RegionID, scopeID, owner, flags, category), "searchparcel", "COUNT(ParcelID)")[0]);
                }
            }
            return 0;
        }

        /// <summary>
        ///   Searches for parcels around the grid
        /// </summary>
        /// <param name = "queryText"></param>
        /// <param name = "category"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        public DirPlacesReplyData[] FindLand(string queryText, string category, int StartQuery, uint Flags)
        {
            List<DirPlacesReplyData> Data = new List<DirPlacesReplyData>();

            string categoryString = "";
            string dwell = "";

            if (category != "-1") //Check for category
                categoryString = " PCategory = '" + category + "' &&";

            //If they dwell sort flag is there, sort by dwell going down
            if ((Flags & (uint) DirectoryManager.DirFindFlags.DwellSort) ==
                (uint) DirectoryManager.DirFindFlags.DwellSort)
                dwell = " ORDER BY Dwell DESC";

            string whereClause = categoryString + " Description LIKE '%" + queryText + "%' OR Name LIKE '%" + queryText +
                                 "%' and ShowInSearch = '1'" + dwell + " LIMIT " + StartQuery.ToString() + ",50 ";
            List<string> retVal = GD.Query(whereClause, "searchparcel", "InfoUUID,Name,ForSale,Auction,Dwell,Flags");
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
                if ((int.Parse(retVal[i + 5]) & (int) ParcelFlags.MaturePublish) == (int) ParcelFlags.MaturePublish &&
                    ((Flags & (uint) DirectoryManager.DirFindFlags.IncludeMature)) == 0)
                {
                }
                else
                    Data.Add(replyData);
                replyData = new DirPlacesReplyData();
            }

            return Data.ToArray();
        }

        /// <summary>
        ///   Searches for parcels for sale around the grid
        /// </summary>
        /// <param name = "searchType"></param>
        /// <param name = "price"></param>
        /// <param name = "area"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        public DirLandReplyData[] FindLandForSale(string searchType, string price, string area, int StartQuery,
                                                  uint Flags)
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
            if ((Flags & (uint) DirectoryManager.DirFindFlags.LimitByPrice) ==
                (uint) DirectoryManager.DirFindFlags.LimitByPrice)
                pricestring = " SalePrice <= '" + price + "'";

            //They requested a 
            if ((Flags & (uint) DirectoryManager.DirFindFlags.LimitByArea) ==
                (uint) DirectoryManager.DirFindFlags.LimitByArea)
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

            string whereClause = pricestring + areastring + forsalestring + " LIMIT " + StartQuery.ToString() + ",50 " +
                                 dwell;
            List<string> retVal = GD.Query(whereClause, "searchparcel", "InfoUUID,Name,Auction,SalePrice,Area,Flags");

            //if there are none, return
            if (retVal.Count == 0)
                return Data.ToArray();

            DirLandReplyData replyData;
            for (int i = 0; i < retVal.Count; i += 6)
            {
                replyData = new DirLandReplyData
                                {
                                    forSale = true,
                                    parcelID = new UUID(retVal[i]),
                                    name = retVal[i + 1],
                                    auction = (retVal[i + 2] != "0")
                                };
                //If its an auction and we didn't request to see auctions, skip to the next and continue
                if ((Flags & (uint) DirectoryManager.SearchTypeFlags.Auction) ==
                    (uint) DirectoryManager.SearchTypeFlags.Auction && !replyData.auction)
                    continue;

                replyData.salePrice = Convert.ToInt32(retVal[i + 3]);
                replyData.actualArea = Convert.ToInt32(retVal[i + 4]);
                //0 flag is an override so that we can get all lands for sale, regardless of maturity
                if (Flags == 0)
                    Data.Add(replyData);
                //Check maturity levels depending on what flags the user has set
                if ((int.Parse(retVal[i + 5]) & (int) ParcelFlags.MaturePublish) == (int) ParcelFlags.MaturePublish &&
                    ((Flags & (uint) DirectoryManager.DirFindFlags.IncludeMature)) == 0)
                {
                }
                else
                    Data.Add(replyData);
            }

            return Data.ToArray();
        }

        /// <summary>
        ///   Searches for events with the given parameters
        /// </summary>
        /// <param name = "queryText"></param>
        /// <param name = "flags"></param>
        /// <param name = "StartQuery"></param>
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
                    whereClause = " EDate >= '" + Util.ToUnixTime(DateTime.Today) + "' LIMIT " + StartQuery.ToString() +
                                  ",50 ";
                }
                else
                {
                    //Pull the day out then and search for that many days in the future/past
                    int Day = int.Parse(StringDay);
                    DateTime SearchedDay = DateTime.Today.AddDays(Day);
                    //We only look at one day at a time
                    DateTime NextDay = SearchedDay.AddDays(1);
                    whereClause = " EDate >= '" + Util.ToUnixTime(SearchedDay) + "' and EDate <= '" +
                                  Util.ToUnixTime(NextDay) + "' and EFlags <= '" + flags + "' LIMIT " +
                                  StartQuery.ToString() + ",50 ";
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
                replyData = new DirEventsReplyData
                                {
                                    ownerID = new UUID(retVal[i]),
                                    name = retVal[i + 1],
                                    eventID = Convert.ToUInt32(retVal[i + 2])
                                };
                DateTime date = Util.ToDateTime(ulong.Parse(retVal[i + 3]));
                replyData.date = date.ToString(new DateTimeFormatInfo());
                replyData.unixTime = (uint) Util.ToUnixTime(date);
                replyData.eventFlags = Convert.ToUInt32(retVal[i + 4]);

                //Check the maturity levels
                if (Convert.ToInt32(retVal[i + 5]) == 2 &&
                    (eventFlags & (uint) DirectoryManager.EventFlags.Adult) == (uint) DirectoryManager.EventFlags.Adult)
                    Data.Add(replyData);
                else if (Convert.ToInt32(retVal[i + 5]) == 1 &&
                         (eventFlags & (uint) DirectoryManager.EventFlags.Mature) ==
                         (uint) DirectoryManager.EventFlags.Mature)
                    Data.Add(replyData);
                else if (Convert.ToInt32(retVal[i + 5]) == 0 &&
                         (eventFlags & (uint) DirectoryManager.EventFlags.PG) ==
                         (uint) DirectoryManager.EventFlags.PG)
                    Data.Add(replyData);
            }

            return Data.ToArray();
        }

        /// <summary>
        ///   Retrives all events in the given region by their maturity level
        /// </summary>
        /// <param name = "regionName"></param>
        /// <param name = "maturity">Uses DirectoryManager.EventFlags to determine the maturity requested</param>
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
                if (int.Parse(retVal[i + 5]) != maturity)
                    continue;

                replyData.ownerID = new UUID(retVal[i]);
                replyData.name = retVal[i + 1];
                replyData.eventID = Convert.ToUInt32(retVal[i + 2]);

                //Parse the date for the viewer
                DateTime date = Util.ToDateTime(ulong.Parse(retVal[i + 3]));
                replyData.date = date.ToString(new DateTimeFormatInfo());
                replyData.unixTime = (uint) Util.ToUnixTime(date);

                replyData.eventFlags = Convert.ToUInt32(retVal[i + 4]);
                Data.Add(replyData);
                replyData = new DirEventsReplyData();
            }

            return Data.ToArray();
        }

        /// <summary>
        ///   Searches for classifieds
        /// </summary>
        /// <param name = "queryText"></param>
        /// <param name = "category"></param>
        /// <param name = "queryFlags"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        public DirClassifiedReplyData[] FindClassifieds(string queryText, string category, string queryFlags,
                                                        int StartQuery)
        {
            List<DirClassifiedReplyData> Data = new List<DirClassifiedReplyData>();

            string whereClause = "";
            string classifiedClause = "";
            uint cqf = uint.Parse(queryFlags);

            if (int.Parse(category) != (int) DirectoryManager.ClassifiedCategories.Any) //Check the category
                classifiedClause = " and Category = '" + category + "'";

            whereClause = " Name LIKE '%" + queryText + "%'" + classifiedClause + " LIMIT " + StartQuery.ToString() +
                          ",50 ";
            List<string> retVal = GD.Query(whereClause, "userclassifieds", "*");
            if (retVal.Count == 0)
                return Data.ToArray();

            DirClassifiedReplyData replyData = null;
            for (int i = 0; i < retVal.Count; i += 6)
            {
                //Pull the classified out of OSD
                Classified classified = new Classified();
                classified.FromOSD((OSDMap) OSDParser.DeserializeJson(retVal[i + 5]));

                replyData = new DirClassifiedReplyData
                                {
                                    classifiedFlags = classified.ClassifiedFlags,
                                    classifiedID = classified.ClassifiedUUID,
                                    creationDate = classified.CreationDate,
                                    expirationDate = classified.ExpirationDate,
                                    price = classified.PriceForListing,
                                    name = classified.Name
                                };
                //Check maturity levels
                if ((replyData.classifiedFlags & (uint) DirectoryManager.ClassifiedFlags.Mature) ==
                    (uint) DirectoryManager.ClassifiedFlags.Mature)
                {
                    if ((cqf & (uint) DirectoryManager.ClassifiedQueryFlags.Mature) ==
                        (uint) DirectoryManager.ClassifiedQueryFlags.Mature)
                        Data.Add(replyData);
                }
                else //Its PG, add all
                    Data.Add(replyData);
            }
            return Data.ToArray();
        }

        /// <summary>
        ///   Gets more info about the event by the events unique event ID
        /// </summary>
        /// <param name = "EventID"></param>
        /// <returns></returns>
        public EventData GetEventInfo(string EventID)
        {
            EventData data = new EventData();
            List<string> RetVal = GD.Query("EID", EventID, "events",
                                           "EID, ECreatorID, EName, ECategory, EDesc, EDate, EDuration, ECoverCharge, ECoverAmount, ESimName, EGlobalPos, EFlags, EMature");
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
                data.date = date.ToString(new DateTimeFormatInfo());
                data.dateUTC = (uint) Util.ToUnixTime(date);

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
        ///   Gets all classifieds in the given region
        /// </summary>
        /// <param name = "regionName"></param>
        /// <returns></returns>
        public Classified[] GetClassifiedsInRegion(string regionName)
        {
            List<Classified> Classifieds = new List<Classified>();
            List<string> retVal = GD.Query("SimName", regionName, "userclassifieds", "*");

            if (retVal.Count == 0)
                return Classifieds.ToArray();

            Classified classified = new Classified();
            for (int i = 0; i < retVal.Count; i += 6)
            {
                //Pull the classified out of OSD
                classified.FromOSD((OSDMap) OSDParser.DeserializeJson(retVal[i + 5]));
                Classifieds.Add(classified);
                classified = new Classified();
            }
            return Classifieds.ToArray();
        }

        #endregion

        public void Dispose()
        {
        }

        private void ConvertBytesToLandBitmap(ref bool[,] tempConvertMap, byte[] Bitmap, int sizeX)
        {
            try
            {
                byte tempByte = 0;
                int x = 0, y = 0, i = 0, bitNum = 0;
                int avg = (sizeX*sizeX/128);
                for (i = 0; i < avg; i++)
                {
                    tempByte = Bitmap[i];
                    for (bitNum = 0; bitNum < 8; bitNum++)
                    {
                        bool bit = Convert.ToBoolean(Convert.ToByte(tempByte >> bitNum) & 1);
                        tempConvertMap[x, y] = bit;
                        x++;
                        if (x > (sizeX/4) - 1)
                        {
                            x = 0;
                            y++;
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}