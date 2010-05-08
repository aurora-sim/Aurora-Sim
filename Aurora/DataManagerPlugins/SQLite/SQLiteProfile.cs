using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Threading;
using Aurora.DataManager;
using Mono.Data.SqliteClient;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.DataManager.SQLite
{
    public class SQLiteProfile : SQLiteLoader, IProfileData
    {
        #region Search

        public DirPlacesReplyData[] PlacesQuery(string queryText, string category, string table, string wantedValue, int StartQuery)
        {
        	var cmd = new SqliteCommand();
            List<DirPlacesReplyData> Data = new List<DirPlacesReplyData>();
            string query = String.Format("select {0} from {1} where ",
                                      wantedValue, table);
            query += "PCategory = '"+category+"' and Pdesc LIKE '%" + queryText + "%' OR PName LIKE '%" + queryText + "%' LIMIT "+StartQuery.ToString()+",50 ";
            
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            
            while (reader.Read())
            {
            	int DataCount = 0;
            	DirPlacesReplyData replyData = new DirPlacesReplyData();
            	for (int i = 0; i < reader.FieldCount; i++)
                {
            		if(DataCount == 0)
            			replyData.parcelID = new UUID(reader.GetString(i));
            		if(DataCount == 1)
            			replyData.name = reader.GetString(i);
            		if(DataCount == 2)
            			replyData.forSale = Convert.ToBoolean(reader.GetString(i));
            		if(DataCount == 3)
            			replyData.auction = Convert.ToBoolean(reader.GetString(i));
            		if(DataCount == 4)
            			replyData.dwell = (float)Convert.ToUInt32(reader.GetString(i));
                    DataCount++;
                    if(DataCount == 5)
                    {
                    	DataCount = 0;
                    	Data.Add(replyData);
                    	replyData = new DirPlacesReplyData();
                    }
                }
            }
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);

            return Data.ToArray();
        }
		public DirLandReplyData[] LandForSaleQuery(string searchType, string price, string area, string table, string wantedValue, int StartQuery)
        {
        	var cmd = new SqliteCommand();
            List<DirLandReplyData> Data = new List<DirLandReplyData>();
            string query = String.Format("select {0} from {1} where ",
                                      wantedValue, table);
            query += "PSalePrice <= '" + price + "' and PArea >= '" + area + "'";
            query += " LIMIT "+StartQuery.ToString()+",50";
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            
            while (reader.Read())
            {
            	int DataCount = 0;
            	DirLandReplyData replyData = new DirLandReplyData();
				replyData.forSale = true;
            	for (int i = 0; i < reader.FieldCount; i++)
                {
            		if(DataCount == 0)
            			replyData.parcelID = new UUID(reader.GetString(i));
            		if(DataCount == 1)
            			replyData.name = reader.GetString(i);
            		if(DataCount == 2)
            			replyData.auction = Convert.ToBoolean(reader.GetString(i));
            		if(DataCount == 3)
            			replyData.salePrice = Convert.ToInt32(reader.GetString(i));
            		if(DataCount == 4)
            			replyData.actualArea = Convert.ToInt32(reader.GetString(i));
                    DataCount++;
                    if(DataCount == 5)
                    {
                    	DataCount = 0;
                    	Data.Add(replyData);
                    	replyData = new DirLandReplyData();
                    	replyData.forSale = true;
                    }
                }
            }
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);

            return Data.ToArray();
        }
		public DirEventsReplyData[] EventQuery(string queryText, string flags, string table, string wantedValue, int StartQuery)
		{
			var cmd = new SqliteCommand();
            List<DirEventsReplyData> Data = new List<DirEventsReplyData>();
            string query = String.Format("select {0} from {1} where ",
                                      wantedValue, table);
            query += "EName LIKE '%" + queryText + "%' and EFlags <= '" + flags + "'";
            query += " LIMIT "+StartQuery.ToString()+",50";
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            
            while (reader.Read())
            {
            	int DataCount = 0;
            	DirEventsReplyData replyData = new DirEventsReplyData();
            	for (int i = 0; i < reader.FieldCount; i++)
                {
            		if(DataCount == 0)
            			replyData.ownerID = new UUID(reader.GetString(i));
            		if(DataCount == 1)
            			replyData.name = reader.GetString(i);
            		if(DataCount == 2)
            			replyData.eventID = Convert.ToUInt32(reader.GetString(i));
            		if(DataCount == 3)
            		{
            			replyData.date = new DateTime(Convert.ToUInt32(reader.GetString(i))).ToString(new System.Globalization.DateTimeFormatInfo());
            			replyData.unixTime = Convert.ToUInt32(reader.GetString(i));
            		}
            		if(DataCount == 4)
            			replyData.eventFlags = Convert.ToUInt32(reader.GetString(i));
                    DataCount++;
                    if(DataCount == 5)
                    {
                    	DataCount = 0;
                    	Data.Add(replyData);
                    	replyData = new DirEventsReplyData();
                    }
                }
            }
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);

            return Data.ToArray();
		}
		
		public DirEventsReplyData[] GetAllEventsNearXY(string table, int X, int Y)
		{
			var cmd = new SqliteCommand();
            List<DirEventsReplyData> Data = new List<DirEventsReplyData>();
            string query = String.Format("select EOwnerID,EName,EID,EDate,EFlags from {0}",
                                      table);
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            
            while (reader.Read())
            {
            	int DataCount = 0;
            	DirEventsReplyData replyData = new DirEventsReplyData();
            	for (int i = 0; i < reader.FieldCount; i++)
                {
            		if(DataCount == 0)
            			replyData.ownerID = new UUID(reader.GetString(i));
            		if(DataCount == 1)
            			replyData.name = reader.GetString(i);
            		if(DataCount == 2)
            			replyData.eventID = Convert.ToUInt32(reader.GetString(i));
            		if(DataCount == 3)
            		{
            			replyData.date = new DateTime(Convert.ToUInt32(reader.GetString(i))).ToString(new System.Globalization.DateTimeFormatInfo());
            			replyData.unixTime = Convert.ToUInt32(reader.GetString(i));
            		}
            		if(DataCount == 4)
            			replyData.eventFlags = Convert.ToUInt32(reader.GetString(i));
                    DataCount++;
                    if(DataCount == 5)
                    {
                    	DataCount = 0;
                    	Data.Add(replyData);
                    	replyData = new DirEventsReplyData();
                    }
                }
            }
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);

            return Data.ToArray();
		}
		
    	public DirClassifiedReplyData[] ClassifiedsQuery(string queryText, string category, string queryFlags, int StartQuery)
		{
			SqliteCommand cmd = new SqliteCommand();
            List<DirClassifiedReplyData> Data = new List<DirClassifiedReplyData>();
            string query = "select classifieduuid, name, creationdate, expirationdate, priceforlisting from profileclassifieds where name LIKE '%" + queryText + "%' and category = '" + category + "'";
            query += " LIMIT "+StartQuery.ToString()+",50";
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            try
            {
            	while (reader.Read())
            	{
            		int DataCount = 0;
            		DirClassifiedReplyData replyData = new DirClassifiedReplyData();
            		for (int i = 0; i < reader.FieldCount; i++)
            		{
            			if(DataCount == 0)
            				replyData.classifiedFlags = Convert.ToByte(reader.GetString(i));
            			if(DataCount == 1)
            				replyData.classifiedID = new UUID(reader.GetString(i));
            			if(DataCount == 2)
            				replyData.creationDate = Convert.ToUInt32(reader.GetString(i));
            			if(DataCount == 3)
            				replyData.expirationDate = Convert.ToUInt32(reader.GetString(i));
            			if(DataCount == 4)
            				replyData.price = Convert.ToInt32(reader.GetString(i));
            			DataCount++;
            			if(DataCount == 5)
            			{
            				DataCount = 0;
            				Data.Add(replyData);
            				replyData = new DirClassifiedReplyData();
            			}
            		}
            	}
            }
            catch (Exception){}
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);
            return Data.ToArray();
		}

        public EventData GetEventInfo(string EventID)
        {
            SqliteCommand cmd = new SqliteCommand();
            EventData data = new EventData();
            string query = "select EID, ECreator, EName, ECategory, EDesc, EDate, EDateUTC, EDuration, ECover, EAmount, ESimName, EGlobalPos, EEventFlags from profileclassifieds where EID = '" + EventID + "'";
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            try
            {
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (i == 0)
                            data.eventID = Convert.ToUInt32(reader.GetString(i));
                        if (i == 1)
                            data.creator = reader.GetString(i);
                        if (i == 2) 
                            data.name = reader.GetString(i);
                        if (i == 3) 
                            data.category = reader.GetString(i);
                        if (i == 4) 
                            data.description = reader.GetString(i);
                        if (i == 5) 
                            data.date = reader.GetString(i);
                        if (i == 6) 
                            data.dateUTC = Convert.ToUInt32(reader.GetString(i));
                        if (i == 7) 
                            data.duration = Convert.ToUInt32(reader.GetString(i));
                        if (i == 8) 
                            data.cover = Convert.ToUInt32(reader.GetString(i));
                        if (i == 9) 
                            data.amount = Convert.ToUInt32(reader.GetString(i));
                        if (i == 10) 
                            data.simName = reader.GetString(i);
                        if (i == 11) 
                            Vector3.TryParse(reader.GetString(i), out data.globalPos);
                        if (i ==12) 
                            data.eventFlags = Convert.ToUInt32(reader.GetString(i));
                    }
                }
            }
            catch (Exception) { }
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);
            return data;
        }
        public EventData[] GetEvents()
        {
            SqliteCommand cmd = new SqliteCommand();
            string query = "select EID, ECreator, EName, ECategory, EDesc, EDate, EDateUTC, EDuration, ECover, EAmount, ESimName, EGlobalPos, EEventFlags from profileclassifieds";
            cmd.CommandText = query;
            List<EventData> datas = new List<EventData>();
            IDataReader reader = GetReader(cmd);
            try
            {
                while (reader.Read())
                {
                    int a = 0;
                    EventData data = new EventData();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (a == 0)
                            data.eventID = Convert.ToUInt32(reader.GetString(i));
                        if (a == 1)
                            data.creator = reader.GetString(i);
                        if (a == 2)
                            data.name = reader.GetString(i);
                        if (a == 3)
                            data.category = reader.GetString(i);
                        if (a == 4)
                            data.description = reader.GetString(i);
                        if (a == 5)
                            data.date = reader.GetString(i);
                        if (a == 6)
                            data.dateUTC = Convert.ToUInt32(reader.GetString(i));
                        if (a == 7)
                            data.duration = Convert.ToUInt32(reader.GetString(i));
                        if (a == 8)
                            data.cover = Convert.ToUInt32(reader.GetString(i));
                        if (a == 9)
                            data.amount = Convert.ToUInt32(reader.GetString(i));
                        if (a == 10)
                            data.simName = reader.GetString(i);
                        if (a == 11)
                            Vector3.TryParse(reader.GetString(i), out data.globalPos);
                        if (a == 12)
                            data.eventFlags = Convert.ToUInt32(reader.GetString(i));
                        a++;
                        if (a == 13)
                        {
                            a = 0;
                            datas.Add(data);
                            data = new EventData();
                        }
                    }
                }
            }
            catch (Exception) { }
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);
            return datas.ToArray();
        }

        public Classified[] GetClassifieds()
        {
            List<Classified> Classifieds = new List<Classified>();
            SqliteCommand cmd = new SqliteCommand();
            string query = "select * from profileclassifieds";
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            Classified classified = new Classified();
            while (reader.Read())
            {
                int a = 0;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (a == 0)
                        classified.ClassifiedUUID = reader.GetString(i);
                    if (a == 1)
                        classified.CreatorUUID = reader.GetString(i);
                    if (a == 2)
                        classified.CreationDate = reader.GetString(i);
                    if (a == 3)
                        classified.ExpirationDate = reader.GetString(i);
                    if (a == 4)
                        classified.Category = reader.GetString(i);
                    if (a == 5)
                        classified.Name = reader.GetString(i);
                    if (a == 6)
                        classified.Description = reader.GetString(i);
                    if (a == 7)
                        classified.ParcelUUID = reader.GetString(i);
                    if (a == 8)
                        classified.ParentEstate = reader.GetString(i);
                    if (a == 9)
                        classified.SnapshotUUID = reader.GetString(i);
                    if (a == 10)
                        classified.SimName = reader.GetString(i);
                    if (a == 11)
                        classified.PosGlobal = reader.GetString(i);
                    if (a == 12)
                        classified.ParcelName = reader.GetString(i);
                    if (a == 13)
                        classified.ClassifiedFlags = reader.GetString(i);
                    if (a == 14)
                        classified.PriceForListing = reader.GetString(i);
                    a++;
                    if (a == 15)
                    {
                        a = 0;
                        Classifieds.Add(classified);
                        classified = new Classified();
                    }
                }
            }
            return Classifieds.ToArray();
        }
        #endregion
    }
}
