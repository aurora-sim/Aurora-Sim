using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.Services.DataService
{
	public class LocalDirectoryServiceConnector : IDirectoryServiceConnector
	{
		private IGenericData GD = null;
		public LocalDirectoryServiceConnector()
		{
			GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
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
				GD.Delete("landinfo", new string[] { "ParcelID" }, new string[] { args.GlobalID.ToString() });
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
			Values.Add(args.MediaDesc);
			Values.Add(args.MediaSize[0]);
			Values.Add(args.MediaSize[1]);
			Values.Add(args.MediaLoop);
			Values.Add(args.MediaType);
			Values.Add(args.ObscureMedia.ToString());
			Values.Add(args.ObscureMusic.ToString());
			Values.Add(showInSearch);
			GD.Insert("landinfo", Values.ToArray());
		}

		public AuroraLandData GetLandData(UUID ParcelID)
		{
            List<string> Query = GD.Query("ParcelID", ParcelID, "landinfo", "*");
            AuroraLandData LandData = new AuroraLandData();
            if (Query.Count == 0)
                return null;
            LandData.ParcelID = UUID.Parse(Query[0]);
            LandData.LocalID = int.Parse(Query[1]);
            LandData.LandingX = float.Parse(Query[2]);
            LandData.LandingY = float.Parse(Query[3]);
            LandData.LandingZ = float.Parse(Query[4]);
            LandData.Name = Query[5];
            LandData.Description = Query[6];
            LandData.Flags = uint.Parse(Query[7]);
            LandData.Dwell = int.Parse(Query[8]);
            LandData.InfoUUID = UUID.Parse(Query[9]);
            LandData.ForSale = bool.Parse(Query[10]);
            LandData.SalePrice = float.Parse(Query[11]);
            LandData.AuctionID = uint.Parse(Query[12]);
            LandData.Area = int.Parse(Query[13]);
            LandData.EstateID = uint.Parse(Query[14]);
            LandData.Maturity = int.Parse(Query[15]);
            LandData.OwnerID = UUID.Parse(Query[16]);
            LandData.GroupID = UUID.Parse(Query[17]);
            LandData.MediaDescription = Query[18];
            LandData.MediaSize = new int[]
            {
                int.Parse(Query[19]), int.Parse(Query[20])
            };
            LandData.MediaLoop = byte.Parse(Query[21]);
            LandData.MediaType = Query[22];
            LandData.ObscureMedia = bool.Parse(Query[23]);
            LandData.ObscureMusic = bool.Parse(Query[24]);
            LandData.ShowInSearch = bool.Parse(Query[25]);
            return LandData;
		}

		public LandData GetLandObject(LandData LD)
		{
			List<string> Query = GD.Query("ParcelID", LD.GlobalID.ToString(), "landinfo", "MediaDescription,MediaHeight,MediaWidth,MediaLoop,MediaType,ObscureMedia,ObscureMusic");

            if (Query.Count == 0)
            {
				return null;
			}
			LD.MediaDesc = Query[0];
			LD.MediaLoop = Convert.ToByte(Query[3]);
			LD.MediaType = Query[4];
			LD.MediaSize = new int[]
            {
				Convert.ToInt32(Query[1]),
				Convert.ToInt32(Query[2])
			};
			LD.ObscureMedia = Convert.ToByte(Query[5]);
			LD.ObscureMusic = Convert.ToByte(Query[6]);
			return LD;
		}

		public DirPlacesReplyData[] FindLand(string queryText, string category, int StartQuery)
		{
            List<DirPlacesReplyData> Data = new List<DirPlacesReplyData>();
            //Don't support category yet. //string whereClause = " PCategory = '" + category + "' and Description LIKE '%" + queryText + "%' OR Name LIKE '%" + queryText + "%' LIMIT " + StartQuery.ToString() + ",50 ";
            string whereClause = " Description LIKE '%" + queryText + "%' OR Name LIKE '%" + queryText + "%' LIMIT " + StartQuery.ToString() + ",50 ";
            List<string> retVal = GD.Query(whereClause, "landinfo", "InfoUUID,Name,ForSale,Auction,Dwell");
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
			List<DirLandReplyData> Data = new List<DirLandReplyData>();
            string whereClause = " SalePrice <= '" + price + "' and Area >= '" + area + "' LIMIT " + StartQuery.ToString() + ",50 ";
            List<string> retVal = GD.Query(whereClause, "landinfo", "InfoUUID,Name,Auction,SalePrice,Area");
            
            if (retVal.Count == 0)
                return Data.ToArray();

			int DataCount = 0;
			DirLandReplyData replyData = new DirLandReplyData();
			replyData.forSale = true;
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
				DataCount++;
				if (DataCount == 5)
                {
					DataCount = 0;
					Data.Add(replyData);
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
			for (int i = 0; i < retVal.Count; i++) {
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
				if (DataCount == 5) {
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
			for (int i = 0; i < retVal.Count; i++) {
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
				if (DataCount == 5) {
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

			string whereClause = " name LIKE '%" + queryText + "%' and category = '" + category + "' LIMIT " + StartQuery.ToString() + ",50 ";
			List<string> retVal = GD.Query(whereClause, "profileclassifieds", "classifieduuid, name, creationdate, expirationdate, priceforlisting");
            if (retVal.Count == 0)
                return Data.ToArray();
			
			int DataCount = 0;
			DirClassifiedReplyData replyData = new DirClassifiedReplyData();
			for (int i = 0; i < retVal.Count; i++) {
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
				DataCount++;
				if (DataCount == 5) {
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
			for (int i = 0; i < RetVal.Count; i++) {
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
			for (int i = 0; i < retVal.Count; i++) {
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
				if (a == 15) {
					a = 0;
					Classifieds.Add(classified);
					classified = new Classified();
				}
			}
			return Classifieds.ToArray();
		}
	}
}
