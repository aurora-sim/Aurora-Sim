using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.Services.DataService.Connectors
{
	public class LocalDirectoryServiceConnector : IDirectoryServiceConnector
	{
		private IGenericData GD = null;
		public LocalDirectoryServiceConnector()
		{
			GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
		}

		public void AddLandObject(LandData args, UUID regionID, bool forSale, uint EstateID, bool showInSearch)
		{
			try {
				GD.Delete("landinfo", new string[] { "ParcelID" }, new string[] { args.GlobalID.ToString() });
			} catch (Exception) {
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
			Values.Add(args.GlobalID);
			Values.Add(forSale);
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
			return null;
		}

		public LandData GetLandObject(LandData LD)
		{
			List<string> Query = GD.Query("ParcelID", LD.GlobalID.ToString(), "landinfo", "MediaDescription,MediaHeight,MediaWidth,MediaLoop,MediaType,ObscureMedia,ObscureMusic");

			if (Query.Count == 0) {
				return null;
			}
			LD.MediaDesc = Query[0];
			LD.MediaLoop = Convert.ToByte(Query[3]);
			LD.MediaType = Query[4];
			LD.MediaSize = new int[] {
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
			string whereClause = " PCategory = '" + category + "' and Pdesc LIKE '%" + queryText + "%' OR PName LIKE '%" + queryText + "%' LIMIT " + StartQuery.ToString() + ",50 ";
			List<string> retVal = GD.Query(whereClause, "landinfo", "PID,PName,PForSale,PAuction,PDwell");

			int DataCount = 0;
			DirPlacesReplyData replyData = new DirPlacesReplyData();
			for (int i = 0; i < retVal.Count; i++) {
				if (DataCount == 0)
					replyData.parcelID = new UUID(retVal[i]);
				if (DataCount == 1)
					replyData.name = retVal[i];
				if (DataCount == 2)
					replyData.forSale = Convert.ToBoolean(retVal[i]);
				if (DataCount == 3)
					replyData.auction = Convert.ToBoolean(retVal[i]);
				if (DataCount == 4)
					replyData.dwell = float.Parse(retVal[i]);
				DataCount++;
				if (DataCount == 5) {
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
			string whereClause = " PSalePrice <= '" + price + "' and PArea >= '" + area + "' LIMIT " + StartQuery.ToString() + ",50 ";
			List<string> retVal = GD.Query(whereClause, "landinfo", "PID,PName,PAuction,PSalePrice,PArea");

			int DataCount = 0;
			DirLandReplyData replyData = new DirLandReplyData();
			replyData.forSale = true;
			for (int i = 0; i < retVal.Count; i++) {
				if (DataCount == 0)
					replyData.parcelID = new UUID(retVal[i]);
				if (DataCount == 1)
					replyData.name = retVal[i];
				if (DataCount == 2)
					replyData.auction = Convert.ToBoolean(retVal[i]);
				if (DataCount == 3)
					replyData.salePrice = Convert.ToInt32(retVal[i]);
				if (DataCount == 4)
					replyData.actualArea = Convert.ToInt32(retVal[i]);
				DataCount++;
				if (DataCount == 5) {
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

			string whereClause = " EName LIKE '%" + queryText + "%' and EFlags <= '" + flags + "' LIMIT " + StartQuery.ToString() + ",50 ";
			List<string> retVal = GD.Query(whereClause, "events", "EOwnerID,EName,EID,EDate,EFlags");

			int DataCount = 0;
			DirEventsReplyData replyData = new DirEventsReplyData();
			for (int i = 0; i < retVal.Count; i++) {
				if (DataCount == 0)
					replyData.ownerID = new UUID(retVal[i]);
				if (DataCount == 1)
					replyData.name = retVal[i];
				if (DataCount == 2)
					replyData.eventID = Convert.ToUInt32(retVal[i]);
				if (DataCount == 3) {
					replyData.date = new DateTime(Convert.ToUInt32(retVal[i])).ToString(new System.Globalization.DateTimeFormatInfo());
					replyData.unixTime = Convert.ToUInt32(retVal[i]);
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

			int DataCount = 0;
			DirEventsReplyData replyData = new DirEventsReplyData();
			for (int i = 0; i < retVal.Count; i++) {
				if (DataCount == 0)
					replyData.ownerID = new UUID(retVal[i]);
				if (DataCount == 1)
					replyData.name = retVal[i];
				if (DataCount == 2)
					replyData.eventID = Convert.ToUInt32(retVal[i]);
				if (DataCount == 3) {
					replyData.date = new DateTime(Convert.ToUInt32(retVal[i])).ToString(new System.Globalization.DateTimeFormatInfo());
					replyData.unixTime = Convert.ToUInt32(retVal[i]);
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
            List<string> RetVal = GD.Query("EID", EventID, "events", "EID, ECreator, EName, ECategory, EDesc, EDate, EDateUTC, EDuration, ECover, EAmount, ESimName, EGlobalPos, EEventFlags, EMature");

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
					data.date = RetVal[i];
				if (i == 6)
					data.dateUTC = Convert.ToUInt32(RetVal[i]);
				if (i == 7)
					data.duration = Convert.ToUInt32(RetVal[i]);
				if (i == 8)
					data.cover = Convert.ToUInt32(RetVal[i]);
				if (i == 9)
					data.amount = Convert.ToUInt32(RetVal[i]);
				if (i == 10)
					data.simName = RetVal[i];
				if (i == 11)
					Vector3.TryParse(RetVal[i], out data.globalPos);
				if (i == 12)
					data.eventFlags = Convert.ToUInt32(RetVal[i]);
                if (i == 13)
                    data.maturity = Convert.ToInt32(RetVal[i]);
			}
			return data;
		}

		public Classified[] GetClassifiedsInRegion(string regionName)
		{
			List<Classified> Classifieds = new List<Classified>();
			List<string> retVal = GD.Query("simname", regionName, "profileclassifieds", "*");

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
