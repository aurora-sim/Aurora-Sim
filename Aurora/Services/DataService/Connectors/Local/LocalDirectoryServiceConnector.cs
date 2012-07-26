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

using Nini.Config;

using Aurora.Framework;

using OpenMetaverse;
using OpenMetaverse.StructuredData;
using EventFlags = OpenMetaverse.DirectoryManager.EventFlags;

using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Services.DataService
{
    public class LocalDirectoryServiceConnector : ConnectorBase, IDirectoryServiceConnector
    {
        private IGenericData GD;

        #region IDirectoryServiceConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
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
                DataManager.DataManager.RegisterPlugin(this);
            }
            Init(simBase, Name);
        }

        public string Name
        {
            get { return "IDirectoryServiceConnector"; }
        }

        public void Dispose()
        {
        }

        #region Region

        /// <summary>
        ///   This also updates the parcel, not for just adding a new one
        /// </summary>
        /// <param name = "parcels"></param>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public void AddRegion(List<LandData> parcels)
        {
            object remoteValue = DoRemote(parcels);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (parcels.Count == 0)
                return;

            ClearRegion(parcels[0].RegionID);

            var OrFilters = new Dictionary<string, object>();
            foreach (var parcel in parcels)
            {
                OrFilters.Add("ParcelID", parcel.GlobalID);
                OrFilters.Add("InfoUUID", parcel.InfoUUID);
            }
            GD.Delete("searchparcel", new QueryFilter { orFilters = OrFilters });
            
            List<object[]> insertValues = parcels.Select(args => new List<object>
            {
                                                                         
                    args.RegionID, 
                    args.GlobalID, 
                    args.LocalID, 
                    args.UserLocation.X, 
                    args.UserLocation.Y, 
                    args.UserLocation.Z, 
                    args.Name, 
                    args.Description, 
                    args.Flags, 
                    args.Dwell, 
                    args.InfoUUID, 
                    ((args.Flags & (uint) ParcelFlags.ForSale) == (uint) ParcelFlags.ForSale) ? 1 : 0, 
                    args.SalePrice, 
                    args.AuctionID, 
                    args.Area, 
                    0, 
                    args.Maturity, 
                    args.OwnerID, 
                    args.GroupID, 
                    ((args.Flags & (uint) ParcelFlags.ShowDirectory) == (uint) ParcelFlags.ShowDirectory) ? 1 : 0, 
                    args.SnapshotID, 
                    OSDParser.SerializeLLSDXmlString(args.Bitmap), 
                    (int) args.Category, 
                    args.ScopeID
             }).Select(Values => Values.ToArray()).ToList();

            GD.InsertMultiple("searchparcel", insertValues);
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public void ClearRegion(UUID regionID)
        {
            object remoteValue = DoRemote(regionID);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["RegionID"] = regionID;
            GD.Delete("searchparcel", filter);
        }

        #endregion

        #region Parcels

        private static List<LandData> Query2LandData(List<string> Query)
        {
            List<LandData> Lands = new List<LandData>();

            for (int i = 0; i < Query.Count; i += 24)
            {
                LandData LandData = new LandData
                                        {
                                            RegionID = UUID.Parse(Query[i]),
                                            GlobalID = UUID.Parse(Query[i + 1]),
                                            LocalID = int.Parse(Query[i + 2]),
                                            UserLocation =
                                                new Vector3(float.Parse(Query[i + 3]), float.Parse(Query[i + 4]),
                                                            float.Parse(Query[i + 5])),
                                            Name = Query[i + 6],
                                            Description = Query[i + 7],
                                            Flags = uint.Parse(Query[i + 8]),
                                            Dwell = int.Parse(Query[i + 9]),
                                            InfoUUID = UUID.Parse(Query[i + 10]),
                                            AuctionID = uint.Parse(Query[i + 13]),
                                            Area = int.Parse(Query[i + 14]),
                                            Maturity = int.Parse(Query[i + 16]),
                                            OwnerID = UUID.Parse(Query[i + 17]),
                                            GroupID = UUID.Parse(Query[i + 18]),
                                            SnapshotID = UUID.Parse(Query[i + 20])
                                        };
                try
                {
                    LandData.Bitmap = OSDParser.DeserializeLLSDXml(Query[i + 21]);
                }
                catch
                {
                }
                LandData.Category = (string.IsNullOrEmpty(Query[i + 22])) ? ParcelCategory.None : (ParcelCategory)int.Parse(Query[i + 22]);
                LandData.ScopeID = UUID.Parse(Query[i + 23]);

                Lands.Add(LandData);
            }
            return Lands;
        }

        /// <summary>
        ///   Gets a parcel from the search database by Info UUID (the true cross instance parcel ID)
        /// </summary>
        /// <param name = "InfoUUID"></param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public LandData GetParcelInfo(UUID InfoUUID)
        {
            object remoteValue = DoRemote(InfoUUID);
            if (remoteValue != null || m_doRemoteOnly)
                return (LandData)remoteValue;

            //Split the InfoUUID so that we get the regions, we'll check for positions in a bit
            int RegionX, RegionY;
            uint X, Y;
            ulong RegionHandle;
            Util.ParseFakeParcelID(InfoUUID, out RegionHandle, out X, out Y);

            Util.UlongToInts(RegionHandle, out RegionX, out RegionY);

            GridRegion r = m_registry.RequestModuleInterface<IGridService>().GetRegionByPosition(null, RegionX,
                                                                                                 RegionY);
            if (r == null)
            {
//                m_log.Warn("[DirectoryService]: Could not find region for ParcelID: " + InfoUUID);
                return null;
            }
            //Get info about a specific parcel somewhere in the metaverse
            QueryFilter filter = new QueryFilter();
            filter.andFilters["RegionID"] = r.RegionID;
            List<string> Query = GD.Query(new[] { "*" }, "searchparcel", filter, null, null, null);
            //Cant find it, return
            if (Query.Count == 0)
            {
                return null;
            }

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
                    if (tempConvertMap[X / 64, Y / 64])
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
                if (tempConvertMap[X / 64, Y / 64])
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

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public LandData GetParcelInfo(UUID RegionID, string ParcelName)
        {
            object remoteValue = DoRemote(RegionID, ParcelName);
            if (remoteValue != null || m_doRemoteOnly)
                return (LandData)remoteValue;

            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                GridRegion region = regiondata.Get(RegionID, null);
                if (region != null)
                {
                    UUID parcelInfoID = UUID.Zero;
                    QueryFilter filter = new QueryFilter();
                    filter.andFilters["RegionID"] = RegionID;
                    filter.andFilters["Name"] = ParcelName;

                    List<string> query = GD.Query(new[] { "InfoUUID" }, "searchparcel", filter, null, 0, 1);

                    if (query.Count >= 1 && UUID.TryParse(query[0], out parcelInfoID))
                    {
                        return GetParcelInfo(parcelInfoID);
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///   Gets all parcels owned by the given user
        /// </summary>
        /// <param name = "OwnerID"></param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<ExtendedLandData> GetParcelByOwner(UUID OwnerID)
        {
            //NOTE: this does check for group deeded land as well, so this can check for that as well
            object remoteValue = DoRemote(OwnerID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<ExtendedLandData>)remoteValue;
            QueryFilter filter = new QueryFilter();
            filter.andFilters["OwnerID"] = OwnerID;
            List<string> Query = GD.Query(new[] { "*" }, "searchparcel", filter, null, null, null);

            return (Query.Count == 0) ? new List<ExtendedLandData>() : LandDataToExtendedLandData(Query2LandData(Query));
        }

        public List<ExtendedLandData> LandDataToExtendedLandData(List<LandData> data)
        {
           return (from land in data
                                              let region = m_registry.RequestModuleInterface<IGridService>().GetRegionByUUID(null, land.RegionID)
                                              where region != null
                                              select new ExtendedLandData
                                              {
                                                  LandData = land,
                                                  RegionType = region.RegionType,
                                                  RegionName = region.RegionName,
                                                  GlobalPosX = region.RegionLocX + land.UserLocation.X,
                                                  GlobalPosY = region.RegionLocY + land.UserLocation.Y
                                              }).ToList();
        }

        private static QueryFilter GetParcelsByRegionWhereClause(UUID RegionID, UUID owner, ParcelFlags flags, ParcelCategory category)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["RegionID"] = RegionID;

            if (owner != UUID.Zero)
            {
                filter.andFilters["OwnerID"] = owner;
            }

            if (flags != ParcelFlags.None)
            {
                filter.andBitfieldAndFilters["Flags"] = (uint)flags;
            }

            if (category != ParcelCategory.Any)
            {
                filter.andFilters["Category"] = (int)category;
            }

            return filter;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<LandData> GetParcelsByRegion(uint start, uint count, UUID RegionID, UUID owner, ParcelFlags flags, ParcelCategory category)
        {
            object remoteValue = DoRemote(start, count, RegionID, owner, flags, category);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<LandData>)remoteValue;

            List<LandData> resp = new List<LandData>(0);
            if (count == 0)
            {
                return resp;
            }

            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                GridRegion region = regiondata.Get(RegionID, null);
                if (region != null)
                {
                    QueryFilter filter = GetParcelsByRegionWhereClause(RegionID, owner, flags, category);
                    Dictionary<string, bool> sort = new Dictionary<string, bool>(1);
                    sort["OwnerID"] = false;
                    return Query2LandData(GD.Query(new[] { "*" }, "searchparcel", filter, sort, start, count));
                }
            }
            return resp;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public uint GetNumberOfParcelsByRegion(UUID RegionID, UUID owner, ParcelFlags flags, ParcelCategory category)
        {
            object remoteValue = DoRemote(RegionID, owner, flags, category);
            if (remoteValue != null || m_doRemoteOnly)
                return (uint)remoteValue;

            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                GridRegion region = regiondata.Get(RegionID, null);
                if (region != null)
                {
                    QueryFilter filter = GetParcelsByRegionWhereClause(RegionID, owner, flags, category);
                    return uint.Parse(GD.Query(new[] { "COUNT(ParcelID)" }, "searchparcel", filter, null, null, null)[0]);
                }
            }
            return 0;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<LandData> GetParcelsWithNameByRegion(uint start, uint count, UUID RegionID, string name)
        {
            object remoteValue = DoRemote(start, count, RegionID, name);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<LandData>)remoteValue;

            List<LandData> resp = new List<LandData>(0);
            if (count == 0)
            {
                return resp;
            }

            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                GridRegion region = regiondata.Get(RegionID, null);
                if (region != null)
                {
                    QueryFilter filter = new QueryFilter();
                    filter.andFilters["RegionID"] = RegionID;
                    filter.andFilters["Name"] = name;
                    
                    Dictionary<string, bool> sort = new Dictionary<string, bool>(1);
                    sort["OwnerID"] = false;

                    return Query2LandData(GD.Query(new[] { "*" }, "searchparcel", filter, sort, start, count));
                }
            }

            return resp;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public uint GetNumberOfParcelsWithNameByRegion(UUID RegionID, string name)
        {
            object remoteValue = DoRemote(RegionID, name);
            if (remoteValue != null || m_doRemoteOnly)
                return (uint)remoteValue;

            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                GridRegion region = regiondata.Get(RegionID, null);
                if (region != null)
                {
                    QueryFilter filter = new QueryFilter();
                    filter.andFilters["RegionID"] = RegionID;
                    filter.andFilters["Name"] = name;

                    return uint.Parse(GD.Query(new[] { "COUNT(ParcelID)" }, "searchparcel", filter, null, null, null)[0]);
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
        /// <param name="Flags"> </param>
        /// <param name="scopeID"> </param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<DirPlacesReplyData> FindLand(string queryText, string category, int StartQuery, uint Flags, UUID scopeID)
        {
            object remoteValue = DoRemote(queryText, category, StartQuery, Flags, scopeID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<DirPlacesReplyData>)remoteValue;

            QueryFilter filter = new QueryFilter();
            Dictionary<string, bool> sort = new Dictionary<string, bool>();

            //If they dwell sort flag is there, sort by dwell going down
            if ((Flags & (uint)DirectoryManager.DirFindFlags.DwellSort) == (uint)DirectoryManager.DirFindFlags.DwellSort)
            {
                sort["Dwell"] = false;
            }
            if (scopeID != UUID.Zero)
                filter.andFilters["ScopeID"] = scopeID;

            filter.orLikeFilters["Name"] = "%" + queryText + "%";
            filter.orLikeFilters["Description"] = "%" + queryText + "%";
            filter.andFilters["ShowInSearch"] = 1;
            if (category != "-1")
                filter.andFilters["Category"] = category;
            if ((Flags & (uint)DirectoryManager.DirFindFlags.AreaSort) == (uint)DirectoryManager.DirFindFlags.AreaSort)
                sort["Area"] = false;
            if ((Flags & (uint)DirectoryManager.DirFindFlags.NameSort) == (uint)DirectoryManager.DirFindFlags.NameSort)
                sort["Name"] = false;

            List<string> retVal = GD.Query(new[]{
                "InfoUUID",
                "Name",
                "ForSale",
                "Auction",
                "Dwell",
                "Flags"
            }, "searchparcel", filter, sort, (uint)StartQuery, 50);

            if (retVal.Count == 0)
            {
                return new List<DirPlacesReplyData>();
            }

            List<DirPlacesReplyData> Data = new List<DirPlacesReplyData>();

            for (int i = 0; i < retVal.Count; i += 6)
            {
                //Check to make sure we are sending the requested maturity levels
                if (!((int.Parse(retVal[i + 5]) & (int)ParcelFlags.MaturePublish) == (int)ParcelFlags.MaturePublish && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeMature)) == 0))
                {
                    Data.Add(new DirPlacesReplyData
                    {
                        parcelID = new UUID(retVal[i]),
                        name = retVal[i + 1],
                        forSale = int.Parse(retVal[i + 2]) == 1,
                        auction = retVal[i + 3] == "0", //Auction is stored as a 0 if there is no auction
                        dwell = float.Parse(retVal[i + 4])
                    });
                }
            }

            return Data;
        }

        /// <summary>
        ///   Searches for parcels for sale around the grid
        /// </summary>
        /// <param name = "searchType">2 = Auction only, 8 = For Sale - Mainland, 16 = For Sale - Estate, 4294967295 = All</param>
        /// <param name = "price"></param>
        /// <param name = "area"></param>
        /// <param name = "StartQuery"></param>
        /// <param name="Flags"> </param>
        /// <param name="scopeID"> </param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<DirLandReplyData> FindLandForSale(string searchType, uint price, uint area, int StartQuery, uint Flags, UUID scopeID)
        {
            object remoteValue = DoRemote(searchType, price, area, StartQuery, Flags, scopeID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<DirLandReplyData>)remoteValue;

            QueryFilter filter = new QueryFilter();

            //Only parcels set for sale will be checked
            filter.andFilters["ForSale"] = "1";
            if (scopeID != UUID.Zero)
                filter.andFilters["ScopeID"] = scopeID;

            //They requested a sale price check
            if ((Flags & (uint)DirectoryManager.DirFindFlags.LimitByPrice) == (uint)DirectoryManager.DirFindFlags.LimitByPrice)
            {
                filter.andLessThanEqFilters["SalePrice"] = (int)price;
            }

            //They requested a 
            if ((Flags & (uint)DirectoryManager.DirFindFlags.LimitByArea) == (uint)DirectoryManager.DirFindFlags.LimitByArea)
            {
                filter.andGreaterThanEqFilters["Area"] = (int)area;
            }
            Dictionary<string, bool> sort = new Dictionary<string, bool>();
            if ((Flags & (uint)DirectoryManager.DirFindFlags.AreaSort) == (uint)DirectoryManager.DirFindFlags.AreaSort)
                sort["Area"] = false;
            if ((Flags & (uint)DirectoryManager.DirFindFlags.NameSort) == (uint)DirectoryManager.DirFindFlags.NameSort)
                sort["Name"] = false;

            List<string> retVal = GD.Query(new[]{
                "InfoUUID",
                "Name",
                "Auction",
                "SalePrice",
                "Area",
                "Flags"
            }, "searchparcel", filter, sort, (uint)StartQuery, 50);

            //if there are none, return
            if (retVal.Count == 0)
                return new List<DirLandReplyData>();

            List<DirLandReplyData> Data = new List<DirLandReplyData>();
            for (int i = 0; i < retVal.Count; i += 6)
            {
                DirLandReplyData replyData = new DirLandReplyData
                                                 {
                                                     forSale = true,
                                                     parcelID = new UUID(retVal[i]),
                                                     name = retVal[i + 1],
                                                     auction = (retVal[i + 2] != "0")
                                                 };
                //If its an auction and we didn't request to see auctions, skip to the next and continue
                if ((Flags & (uint)DirectoryManager.SearchTypeFlags.Auction) == (uint)DirectoryManager.SearchTypeFlags.Auction && !replyData.auction)
                {
                    continue;
                }

                replyData.salePrice = Convert.ToInt32(retVal[i + 3]);
                replyData.actualArea = Convert.ToInt32(retVal[i + 4]);

                //Check maturity levels depending on what flags the user has set
                //0 flag is an override so that we can get all lands for sale, regardless of maturity
                if (Flags == 0 || !((int.Parse(retVal[i + 5]) & (int)ParcelFlags.MaturePublish) == (int)ParcelFlags.MaturePublish && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeMature)) == 0))
                {
                    Data.Add(replyData);
                }
            }

            return Data;
        }

        /// <summary>
        ///   Searches for parcels for sale around the grid
        /// </summary>
        /// <param name = "searchType">2 = Auction only, 8 = For Sale - Mainland, 16 = For Sale - Estate, 4294967295 = All</param>
        /// <param name = "price"></param>
        /// <param name = "area"></param>
        /// <param name = "StartQuery"></param>
        /// <param name="Flags"> </param>
        /// <param name="regionID"> </param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<DirLandReplyData> FindLandForSaleInRegion(string searchType, uint price, uint area, int StartQuery, uint Flags, UUID regionID)
        {
            object remoteValue = DoRemote(searchType, price, area, StartQuery, Flags, regionID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<DirLandReplyData>)remoteValue;

            QueryFilter filter = new QueryFilter();

            //Only parcels set for sale will be checked
            filter.andFilters["ForSale"] = "1";
            filter.andFilters["RegionID"] = regionID;

            //They requested a sale price check
            if ((Flags & (uint)DirectoryManager.DirFindFlags.LimitByPrice) == (uint)DirectoryManager.DirFindFlags.LimitByPrice)
            {
                filter.andLessThanEqFilters["SalePrice"] = (int)price;
            }

            //They requested a 
            if ((Flags & (uint)DirectoryManager.DirFindFlags.LimitByArea) == (uint)DirectoryManager.DirFindFlags.LimitByArea)
            {
                filter.andGreaterThanEqFilters["Area"] = (int)area;
            }
            Dictionary<string, bool> sort = new Dictionary<string, bool>();
            if ((Flags & (uint)DirectoryManager.DirFindFlags.AreaSort) == (uint)DirectoryManager.DirFindFlags.AreaSort)
                sort["Area"] = false;
            if ((Flags & (uint)DirectoryManager.DirFindFlags.NameSort) == (uint)DirectoryManager.DirFindFlags.NameSort)
                sort["Name"] = false;
            //if ((queryFlags & (uint)DirectoryManager.DirFindFlags.PerMeterSort) == (uint)DirectoryManager.DirFindFlags.PerMeterSort)
            //    sort["Area"] = (queryFlags & (uint)DirectoryManager.DirFindFlags.SortAsc) == (uint)DirectoryManager.DirFindFlags.SortAsc);
            if ((Flags & (uint)DirectoryManager.DirFindFlags.PricesSort) == (uint)DirectoryManager.DirFindFlags.PricesSort)
                sort["SalePrice"] = (Flags & (uint)DirectoryManager.DirFindFlags.SortAsc) == (uint)DirectoryManager.DirFindFlags.SortAsc;

            List<string> retVal = GD.Query(new[]{
                "InfoUUID",
                "Name",
                "Auction",
                "SalePrice",
                "Area",
                "Flags"
            }, "searchparcel", filter, sort, (uint)StartQuery, 50);

            //if there are none, return
            if (retVal.Count == 0)
                return new List<DirLandReplyData>();

            List<DirLandReplyData> Data = new List<DirLandReplyData>();
            for (int i = 0; i < retVal.Count; i += 6)
            {
                DirLandReplyData replyData = new DirLandReplyData
                                                 {
                                                     forSale = true,
                                                     parcelID = new UUID(retVal[i]),
                                                     name = retVal[i + 1],
                                                     auction = (retVal[i + 2] != "0")
                                                 };
                //If its an auction and we didn't request to see auctions, skip to the next and continue
                if ((Flags & (uint)DirectoryManager.SearchTypeFlags.Auction) == (uint)DirectoryManager.SearchTypeFlags.Auction && !replyData.auction)
                {
                    continue;
                }

                replyData.salePrice = Convert.ToInt32(retVal[i + 3]);
                replyData.actualArea = Convert.ToInt32(retVal[i + 4]);

                //Check maturity levels depending on what flags the user has set
                //0 flag is an override so that we can get all lands for sale, regardless of maturity
                if (Flags == 0 || !((int.Parse(retVal[i + 5]) & (int)ParcelFlags.MaturePublish) == (int)ParcelFlags.MaturePublish && ((Flags & (uint)DirectoryManager.DirFindFlags.IncludeMature)) == 0))
                {
                    Data.Add(replyData);
                }
            }

            return Data;
        }
        
        /// <summary>
        ///   Searches for the most popular places around the grid
        /// </summary>
        /// <param name = "queryFlags"></param>
        /// <param name = "scopeID"></param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<DirPopularReplyData> FindPopularPlaces(uint queryFlags, UUID scopeID)
        {
            object remoteValue = DoRemote(queryFlags, scopeID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<DirPopularReplyData>)remoteValue;

            QueryFilter filter = new QueryFilter();
            Dictionary<string, bool> sort = new Dictionary<string, bool>();

            if ((queryFlags & (uint)DirectoryManager.DirFindFlags.AreaSort) == (uint)DirectoryManager.DirFindFlags.AreaSort)
                sort["Area"] = false;
            else if ((queryFlags & (uint)DirectoryManager.DirFindFlags.NameSort) == (uint)DirectoryManager.DirFindFlags.NameSort)
                sort["Name"] = false;
            //else if ((queryFlags & (uint)DirectoryManager.DirFindFlags.PerMeterSort) == (uint)DirectoryManager.DirFindFlags.PerMeterSort)
            //    sort["Area"] = (queryFlags & (uint)DirectoryManager.DirFindFlags.SortAsc) == (uint)DirectoryManager.DirFindFlags.SortAsc);
            //else if ((queryFlags & (uint)DirectoryManager.DirFindFlags.PricesSort) == (uint)DirectoryManager.DirFindFlags.PricesSort)
            //    sort["SalePrice"] = (queryFlags & (uint)DirectoryManager.DirFindFlags.SortAsc) == (uint)DirectoryManager.DirFindFlags.SortAsc;
            else
                sort["Dwell"] = false;

            if (scopeID != UUID.Zero)
                filter.andFilters["ScopeID"] = scopeID;


            List<string> retVal = GD.Query(new[]{
                "InfoUUID",
                "Name",
                "Dwell",
                "Flags"
            }, "searchparcel", filter, null, 0, 25);

            //if there are none, return
            if (retVal.Count == 0)
                return new List<DirPopularReplyData>();

            List<DirPopularReplyData> Data = new List<DirPopularReplyData>();
            for (int i = 0; i < retVal.Count; i += 4)
            {
                //Check maturity levels depending on what flags the user has set
                //0 flag is an override so that we can get all lands for sale, regardless of maturity
                if (queryFlags == 0 || !((int.Parse(retVal[i + 3]) & (int)ParcelFlags.MaturePublish) == (int)ParcelFlags.MaturePublish && ((queryFlags & (uint)DirectoryManager.DirFindFlags.IncludeMature)) == 0))
                    Data.Add(new DirPopularReplyData
                    {
                        ParcelID = new UUID(retVal[i]),
                        Name = retVal[i + 1],
                        Dwell = int.Parse(retVal[i + 2])
                    });
            }

            return Data;
        }

        private void ConvertBytesToLandBitmap(ref bool[,] tempConvertMap, byte[] Bitmap, int sizeX)
        {
            try
            {
                int x = 0, y = 0, i = 0;
                int avg = (sizeX*sizeX/128);
                for (i = 0; i < avg; i++)
                {
                    byte tempByte = Bitmap[i];
                    int bitNum = 0;
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

        #endregion

        #region Classifieds

        /// <summary>
        ///   Searches for classifieds
        /// </summary>
        /// <param name = "queryText"></param>
        /// <param name = "category"></param>
        /// <param name = "queryFlags"></param>
        /// <param name = "StartQuery"></param>
        /// <param name="scopeID"> </param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<DirClassifiedReplyData> FindClassifieds(string queryText, string category, uint queryFlags, int StartQuery, UUID scopeID)
        {
            object remoteValue = DoRemote(queryText, category, queryFlags, StartQuery, scopeID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<DirClassifiedReplyData>)remoteValue;

            QueryFilter filter = new QueryFilter();

            filter.andLikeFilters["Name"] = "%" + queryText + "%";
            if (int.Parse(category) != (int)DirectoryManager.ClassifiedCategories.Any) //Check the category
                filter.andFilters["Category"] = category;
            if (scopeID != UUID.Zero)
                filter.andFilters["ScopeID"] = scopeID;

            List<string> retVal = GD.Query(new[] { "*" }, "userclassifieds", filter, null, (uint)StartQuery, 50);
            if (retVal.Count == 0)
                return new List<DirClassifiedReplyData>();

            List<DirClassifiedReplyData> Data = new List<DirClassifiedReplyData>();
            for (int i = 0; i < retVal.Count; i += 9)
            {
                //Pull the classified out of OSD
                Classified classified = new Classified();
                classified.FromOSD((OSDMap) OSDParser.DeserializeJson(retVal[i + 5]));

                DirClassifiedReplyData replyData = new DirClassifiedReplyData
                                                       {
                                                           classifiedFlags = classified.ClassifiedFlags,
                                                           classifiedID = classified.ClassifiedUUID,
                                                           creationDate = classified.CreationDate,
                                                           expirationDate = classified.ExpirationDate,
                                                           price = classified.PriceForListing,
                                                           name = classified.Name
                                                       };
                //Check maturity levels
                if ((replyData.classifiedFlags & (uint)DirectoryManager.ClassifiedFlags.Mature) != (uint)DirectoryManager.ClassifiedFlags.Mature)
                {
                    if ((queryFlags & (uint)DirectoryManager.ClassifiedQueryFlags.Mature) == (uint)DirectoryManager.ClassifiedQueryFlags.Mature)
                        Data.Add(replyData);
                }
                else
                    //Its Mature, add all
                    Data.Add(replyData);
            }
            return Data;
        }

        /// <summary>
        ///   Gets all classifieds in the given region
        /// </summary>
        /// <param name = "regionName"></param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<Classified> GetClassifiedsInRegion(string regionName)
        {
            object remoteValue = DoRemote(regionName);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<Classified>)remoteValue;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["SimName"] = regionName;
            List<string> retVal = GD.Query(new[] { "*" }, "userclassifieds", filter, null, null, null);

            if (retVal.Count == 0)
            {
                return new List<Classified>();
            }

            List<Classified> Classifieds = new List<Classified>();
            for (int i = 0; i < retVal.Count; i += 9)
            {
                Classified classified = new Classified();
                //Pull the classified out of OSD
                classified.FromOSD((OSDMap) OSDParser.DeserializeJson(retVal[i + 6]));
                Classifieds.Add(classified);
            }
            return Classifieds;
        }

        /// <summary>
        ///   Get a classified by its UUID
        /// </summary>
        /// <param name = "id"></param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public Classified GetClassifiedByID(UUID id)
        {
            object remoteValue = DoRemote(id);
            if (remoteValue != null || m_doRemoteOnly)
                return (Classified)remoteValue;

            QueryFilter filter = new QueryFilter();
            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where.Add("ClassifiedUUID", id);
            filter.andFilters = where;
            List<string> retVal = GD.Query(new[] { "*" }, "userclassifieds", filter, null, null, null);
            if ((retVal == null) || (retVal.Count == 0)) return null;
            Classified classified = new Classified();
            classified.FromOSD((OSDMap)OSDParser.DeserializeJson(retVal[6]));
            return classified;
        }

        #endregion

        #region Events

        /// <summary>
        ///   Searches for events with the given parameters
        /// </summary>
        /// <param name = "queryText"></param>
        /// <param name = "eventFlags"></param>
        /// <param name = "StartQuery"></param>
        /// <param name="scopeID"> </param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<DirEventsReplyData> FindEvents(string queryText, uint eventFlags, int StartQuery, UUID scopeID)
        {
            object remoteValue = DoRemote(queryText, eventFlags, StartQuery, scopeID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<DirEventsReplyData>)remoteValue;

            List<DirEventsReplyData> Data = new List<DirEventsReplyData>();

            QueryFilter filter = new QueryFilter();

            //|0| means search between some days
            if (queryText.Contains("|0|"))
            {
                string StringDay = queryText.Split('|')[0];
                if (StringDay == "u") //"u" means search for events that are going on today
                {
                    filter.andGreaterThanEqFilters["UNIX_TIMESTAMP(date)"] = Util.ToUnixTime(DateTime.Today);
                }
                else
                {
                    //Pull the day out then and search for that many days in the future/past
                    int Day = int.Parse(StringDay);
                    DateTime SearchedDay = DateTime.Today.AddDays(Day);
                    //We only look at one day at a time
                    DateTime NextDay = SearchedDay.AddDays(1);
                    filter.andGreaterThanEqFilters["UNIX_TIMESTAMP(date)"] = Util.ToUnixTime(SearchedDay);
                    filter.andLessThanEqFilters["UNIX_TIMESTAMP(date)"] = Util.ToUnixTime(NextDay);
                    filter.andLessThanEqFilters["flags"] = (int)eventFlags;
                }
            }
            else
            {
                filter.andLikeFilters["name"] = "%" + queryText + "%";
            }
            if (scopeID != UUID.Zero)
                filter.andFilters["scopeID"] = scopeID;

            List<string> retVal = GD.Query(new[]{
                "EID",
                "creator",
                "date",
                "maturity",
                "flags",
                "name"
            }, "asevents", filter, null, (uint)StartQuery, 50);

            if (retVal.Count > 0)
            {
                for (int i = 0; i < retVal.Count; i += 6)
                {
                    DirEventsReplyData replyData = new DirEventsReplyData
                                                       {
                                                           eventID = Convert.ToUInt32(retVal[i]),
                                                           ownerID = new UUID(retVal[i + 1]),
                                                           name = retVal[i + 5],
                                                       };
                    DateTime date = DateTime.Parse(retVal[i + 2]);
                    replyData.date = date.ToString(new DateTimeFormatInfo());
                    replyData.unixTime = (uint)Util.ToUnixTime(date);
                    replyData.eventFlags = Convert.ToUInt32(retVal[i + 4]);

                    //Check the maturity levels
                    uint maturity = Convert.ToUInt32(retVal[i + 3]);
                    if(
                            (maturity == 0 && (eventFlags & (uint)EventFlags.PG) == (uint)EventFlags.PG) ||
                            (maturity == 1 && (eventFlags & (uint)EventFlags.Mature) == (uint)EventFlags.Mature) ||
                            (maturity == 2 && (eventFlags & (uint)EventFlags.Adult) == (uint)EventFlags.Adult)
                    )
                    {
                        Data.Add(replyData);
                    }
                }
            }

            return Data;
        }

        /// <summary>
        ///   Retrives all events in the given region by their maturity level
        /// </summary>
        /// <param name = "regionName"></param>
        /// <param name = "maturity">Uses DirectoryManager.EventFlags to determine the maturity requested</param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<DirEventsReplyData> FindAllEventsInRegion(string regionName, int maturity)
        {
            object remoteValue = DoRemote(regionName, maturity);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<DirEventsReplyData>)remoteValue;

            List<DirEventsReplyData> Data = new List<DirEventsReplyData>();

            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (regiondata != null)
            {
                List<GridRegion> regions = regiondata.Get(regionName, null, null, null);
                if (regions.Count >= 1)
                {
                    QueryFilter filter = new QueryFilter();
                    filter.andFilters["region"] = regions[0].RegionID.ToString();
                    filter.andFilters["maturity"] = maturity;

                    List<string> retVal = GD.Query(new[]{
                        "EID",
                        "creator",
                        "date",
                        "maturity",
                        "flags",
                        "name"
                    }, "asevents", filter, null, null, null);

                    if (retVal.Count > 0)
                    {
                        for (int i = 0; i < retVal.Count; i += 6)
                        {
                            DirEventsReplyData replyData = new DirEventsReplyData
                                                               {
                                                                   eventID = Convert.ToUInt32(retVal[i]),
                                                                   ownerID = new UUID(retVal[i + 1]),
                                                                   name = retVal[i + 5],
                                                               };
                            DateTime date = DateTime.Parse(retVal[i + 2]);
                            replyData.date = date.ToString(new DateTimeFormatInfo());
                            replyData.unixTime = (uint)Util.ToUnixTime(date);
                            replyData.eventFlags = Convert.ToUInt32(retVal[i + 4]);

                            Data.Add(replyData);
                        }
                    }
                }
            }

            return Data;
        }
        
        private static List<EventData> Query2EventData(List<string> RetVal){
            List<EventData> Events = new List<EventData>();
            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            if (RetVal.Count % 16 != 0 || regiondata == null)
            {
                return Events;
            }

            for (int i = 0; i < RetVal.Count; i += 16)
            {
                EventData data = new EventData();

                GridRegion region = regiondata.Get(UUID.Parse(RetVal[2]), null);
                if (region == null)
                {
                    continue;
                }
                data.simName = region.RegionName;

                data.eventID = Convert.ToUInt32(RetVal[i]);
                data.creator = RetVal[i + 1];

                //Parse the time out for the viewer
                DateTime date = DateTime.Parse(RetVal[i + 4]);
                data.date = date.ToString(new DateTimeFormatInfo());
                data.dateUTC = (uint)Util.ToUnixTime(date);

                data.cover = data.amount = Convert.ToUInt32(RetVal[i + 5]);
                data.maturity = Convert.ToInt32(RetVal[i + 6]);
                data.eventFlags = Convert.ToUInt32(RetVal[i + 7]);
                data.duration = Convert.ToUInt32(RetVal[i + 8]);

                data.globalPos = new Vector3(
                        region.RegionLocX + float.Parse(RetVal[i + 9]),
                        region.RegionLocY + float.Parse(RetVal[i + 10]),
                        region.RegionLocZ + float.Parse(RetVal[i + 11])
                );

                data.name = RetVal[i + 12];
                data.description = RetVal[i + 13];
                data.category = RetVal[i + 14];

                Events.Add(data);
            }

            return Events;
        }

        /// <summary>
        ///   Gets more info about the event by the events unique event ID
        /// </summary>
        /// <param name = "EventID"></param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public EventData GetEventInfo(uint EventID)
        {
            object remoteValue = DoRemote(EventID);
            if (remoteValue != null || m_doRemoteOnly)
                return (EventData)remoteValue;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["EID"] = EventID;
            List<string> RetVal = GD.Query(new[] { "*" }, "asevents", filter, null, null, null);
            return (RetVal.Count == 0) ? null : Query2EventData(RetVal)[0];
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public EventData CreateEvent(UUID creator, UUID regionID, UUID parcelID, DateTime date, uint cover, EventFlags maturity, uint flags, uint duration, Vector3 localPos, string name, string description, string category)
        {
            object remoteValue = DoRemote(creator, regionID, parcelID, date, cover, maturity, flags, duration, localPos, name, description, category);
            if (remoteValue != null || m_doRemoteOnly)
                return (EventData)remoteValue;

            IRegionData regiondata = DataManager.DataManager.RequestPlugin<IRegionData>();
            IParcelServiceConnector parceldata = DataManager.DataManager.RequestPlugin<IParcelServiceConnector>();
            if(regiondata == null || parceldata == null){
                return null;
            }

            GridRegion region = regiondata.Get(regionID, null);
            if(region == null){
                return null;
            }
            if (parcelID != UUID.Zero)
            {
                LandData parcel = parceldata.GetLandData(region.RegionID, parcelID);
                if (parcel == null)
                {
                    return null;
                }
            }


            EventData eventData = new EventData
                                      {
                                          eventID = GetMaxEventID() + 1,
                                          creator = creator.ToString(),
                                          simName = region.RegionName,
                                          date = date.ToString(new DateTimeFormatInfo()),
                                          dateUTC = (uint) Util.ToUnixTime(date),
                                          amount = cover,
                                          cover = cover,
                                          maturity = (int) maturity,
                                          eventFlags = flags | (uint) maturity,
                                          duration = duration,
                                          globalPos = new Vector3(
                                              region.RegionLocX + localPos.X,
                                              region.RegionLocY + localPos.Y,
                                              region.RegionLocZ + localPos.Z
                                              ),
                                          name = name,
                                          description = description,
                                          category = category
                                      };

            Dictionary<string, object> row = new Dictionary<string, object>(15);
            row["EID"] = eventData.eventID;
            row["creator"] = creator.ToString();
            row["region"] = regionID.ToString();
            row["parcel"] = parcelID.ToString();
            row["date"] = date.ToString("s");
            row["cover"] = eventData.cover;
            row["maturity"] = (uint)maturity;
            row["flags"] = flags;
            row["duration"] = duration;
            row["localPosX"] = localPos.X;
            row["localPosY"] = localPos.Y;
            row["localPosZ"] = localPos.Z;
            row["name"] = name;
            row["description"] = description;
            row["category"] = category;

            GD.Insert("asevents", row);

            return eventData;
        }

        public List<EventData> GetEvents(uint start, uint count, Dictionary<string, bool> sort, Dictionary<string, object> filter)
        {
            return (count == 0) ? new List<EventData>(0) : Query2EventData(GD.Query(new[]{ "*" }, "asevents", new QueryFilter{
                andFilters = filter
            }, sort, start, count ));
        }

        public uint GetNumberOfEvents(Dictionary<string, object> filter)
        {
            return uint.Parse(GD.Query(new[]{
                "COUNT(EID)"
            }, "asevents", new QueryFilter{
                andFilters = filter
            }, null, null, null)[0]);
        }

        public uint GetMaxEventID()
        {
            if (GetNumberOfEvents(new Dictionary<string, object>(0)) == 0)
            {
                return 0;
            }
            return uint.Parse(GD.Query(new[] { "MAX(EID)" }, "asevents", null, null, null, null)[0]);
        }

        #endregion

        #endregion
    }
}