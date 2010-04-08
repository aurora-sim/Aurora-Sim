using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using MySql.Data.MySqlClient;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.DataManager.MySQL
{
    public class MySQLProfile : MySQLDataLoader, IProfileData
    {
        public Dictionary<UUID, string> ReadPickRow(string creator)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            string query = "select pickuuid,name from userpicks where creatoruuid = '" + creator + "'";
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {   
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        Dictionary<UUID, string> row = readPickRow(reader);
                        return row;
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }

        }
        public Dictionary<UUID, string> readPickRow(IDataReader reader)
        {
            Dictionary<UUID, string> retval = new Dictionary<UUID, string>();


            try
            {
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i = i + 2)
                    {
                        retval.Add(new UUID(reader.GetValue(i).ToString()), reader.GetValue(i + 1).ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                ex = new Exception();
            }
            return retval;
        }
        public List<string> ReadPickInfoRow(string avatar_id, string pick_id)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;

            string query = "select * from userpicks where creatoruuid = '" + avatar_id + "' AND pickuuid = '" + pick_id + "'";
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        List<string> row = readPickInfoRow(reader);
                        return row;
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }
        }
        public List<string> readPickInfoRow(IDataReader reader)
        {
            List<string> retval = new List<string>();
            //select * from userpicks where creatoruuid = '" + avatar_id + "' AND pickuuid = '" + pick_id + "'
            if (reader.Read())
            {
                try
                {
                    retval.Add(Convert.ToString(reader["pickuuid"].ToString()));
                    retval.Add(Convert.ToString(reader["creatoruuid"].ToString()));
                    retval.Add(Convert.ToString(reader["toppick"].ToString()));
                    retval.Add(Convert.ToString(reader["parceluuid"].ToString()));
                    retval.Add(Convert.ToString(reader["name"].ToString()));
                    retval.Add(Convert.ToString(reader["description"].ToString()));
                    retval.Add(Convert.ToString(reader["snapshotuuid"].ToString()));
                    retval.Add(Convert.ToString(reader["user"].ToString()));
                    retval.Add(Convert.ToString(reader["originalname"].ToString()));
                    retval.Add(Convert.ToString(reader["simname"].ToString()));
                    retval.Add(Convert.ToString(reader["posglobal"].ToString()));
                    retval.Add(Convert.ToString(reader["sortorder"].ToString()));
                    retval.Add(Convert.ToString(reader["enabled"].ToString()));

                }
                catch (Exception ex)
                {
                    ex = new Exception();
                }
            }
            else
            {
                List<string> retvaln = new List<string>();
                retvaln.Add("");
                retvaln.Add("");
                retvaln.Add("");
                retvaln.Add("");
                retvaln.Add("");
                retvaln.Add("");
                retvaln.Add("");
                retvaln.Add("");
                retvaln.Add("");
                retvaln.Add("");
                retvaln.Add("");
                retvaln.Add("");
                retvaln.Add("");
                return retvaln;
            }
            return retval;
        }
        public List<string> ReadInterestsInfoRow(string agentID)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;

            string query = "select * from usersauth where userUUID = '" + agentID + "'";
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        List<string> row = readInterestsInfoRow(reader);
                        return row;
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }
        }
        public List<string> readInterestsInfoRow(IDataReader reader)
        {
            List<string> retval = new List<string>();

            if (reader.Read())
            {
                try
                {
                    retval.Add(Convert.ToString(reader["profileWantToMask"].ToString()));
                    retval.Add(Convert.ToString(reader["profileWantToText"].ToString()));
                    retval.Add(Convert.ToString(reader["profileSkillsMask"].ToString()));
                    retval.Add(Convert.ToString(reader["profileSkillsText"].ToString()));
                    retval.Add(Convert.ToString(reader["profileLanguages"].ToString()));
                }
                catch (Exception ex)
                {
                    ex = new Exception();
                }
            }
            else
            {
                return null;
            }
            return retval;
        }
        public Dictionary<UUID, string> ReadClassifedRow(string creatoruuid)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;

            
            string query = "select classifieduuid, name from classifieds where creatoruuid = '" + creatoruuid + "'";
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        Dictionary<UUID, string> row = readClassifedRow(reader);
                        return row;
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }
        }
        public Dictionary<UUID, string> readClassifedRow(IDataReader reader)
        {
            Dictionary<UUID, string> retval = new Dictionary<UUID, string>();
            try
            {
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i = i + 2)
                    {
                        retval.Add(new UUID(reader.GetValue(i).ToString()), reader.GetValue(i + 1).ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                ex = new Exception();
            }
            return retval;
        }
        public List<string> ReadClassifiedInfoRow(string classifieduuid)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;

            string query = "select * from classifieds where classifieduuid = '" + classifieduuid + "'";
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        List<string> row = ReadClassifiedInfoRow(reader);
                        return row;
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }
        }
        public List<string> ReadClassifiedInfoRow(IDataReader reader)
        {
            List<string> retval = new List<string>();

            if (reader.Read())
            {
                try
                {
                    retval.Add(Convert.ToString(reader["creatoruuid"].ToString()));
                    retval.Add(Convert.ToString(reader["creationdate"].ToString()));
                    retval.Add(Convert.ToString(reader["expirationdate"].ToString()));
                    retval.Add(Convert.ToString(reader["category"].ToString()));
                    retval.Add(Convert.ToString(reader["name"].ToString()));
                    retval.Add(Convert.ToString(reader["description"].ToString()));
                    retval.Add(Convert.ToString(reader["parceluuid"].ToString()));
                    retval.Add(Convert.ToString(reader["parentestate"].ToString()));
                    retval.Add(Convert.ToString(reader["snapshotuuid"].ToString()));
                    retval.Add(Convert.ToString(reader["simname"].ToString()));
                    retval.Add(Convert.ToString(reader["posglobal"].ToString()));
                    retval.Add(Convert.ToString(reader["parcelname"].ToString()));
                    retval.Add(Convert.ToString(reader["classifiedflags"].ToString()));
                    retval.Add(Convert.ToString(reader["priceforlisting"].ToString()));

                }
                catch (Exception ex)
                {
                    ex = new Exception();
                }
            }
            else
            {
                return null;
            }
            return retval;
        }
        
        public List<string> Query(string query)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            List<string> RetVal = new List<string>();
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                RetVal.Add(reader.GetString(i));
                            }
                        }
                        if (RetVal.Count == 0)
                        {
                            RetVal.Add("");
                            return RetVal;
                        }
                        else
                        {
                            return RetVal;
                        }
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }
        }
        
        private Dictionary<UUID, AuroraProfileData> UserProfilesCache = new Dictionary<UUID, AuroraProfileData>();
        private Dictionary<UUID, AuroraProfileData> UserProfileNotesCache = new Dictionary<UUID, AuroraProfileData>();
        public AuroraProfileData GetProfileInfo(UUID agentID)
        {
            AuroraProfileData UserProfile = new AuroraProfileData();
            if (UserProfilesCache.ContainsKey(agentID))
            {
                UserProfilesCache.TryGetValue(agentID, out UserProfile);
                return UserProfile;
            }
            else
            {
                try
                {
                    List<string> Interests = ReadInterestsInfoRow(agentID.ToString());
                    List<string> Profile = Query("select userLogin,userPass,userGodLevel,membershipGroup,profileMaturePublish,profileAllowPublish,profileURL,AboutText,CustomType,Email,FirstLifeAboutText,FirstLifeImage,Partner,PermaBanned,TempBanned,Image from usersauth where userUUID = '" + agentID.ToString() + "'");

                    UserProfile.FirstName = Profile[0].Split(' ')[0];
                    UserProfile.SurName = Profile[0].Split(' ')[1];
                    UserProfile.PasswordHash = Profile[1];
                    UserProfile.Identifier = agentID.ToString();
                    UserProfile.ProfileURL = Profile[6];
                    UserProfile.Interests = Interests;
                    UserProfile.MembershipGroup = Profile[3];
                    UserProfile.AllowPublish = Profile[5];
                    UserProfile.MaturePublish = Profile[4];
                    UserProfile.GodLevel = Convert.ToInt32(Profile[2]);
                    UserProfile.AboutText = Profile[7];
                    UserProfile.CustomType = Profile[8];
                    UserProfile.Email = Profile[9];
                    UserProfile.FirstLifeAboutText = Profile[10];
                    UserProfile.FirstLifeImage = new UUID(Profile[11]);
                    UserProfile.Partner = new UUID(Profile[12]);
                    UserProfile.PermaBanned = Convert.ToInt32(Profile[13]);
                    UserProfile.TempBanned = Convert.ToInt32(Profile[14]);
                    UserProfile.Image = new UUID(Profile[15]);
                    UserProfilesCache.Add(agentID, UserProfile);

                    return UserProfile;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }




        public void InvalidateProfileNotes(UUID target)
        {
            UserProfileNotesCache.Remove(target);
        }

        //Deletion coming up soon!
        public AuroraProfileData GetProfileNotes(UUID agentID, UUID target)
        {
            AuroraProfileData UserProfile = new AuroraProfileData();
            if (UserProfileNotesCache.ContainsKey(target))
            {
                UserProfileNotesCache.TryGetValue(target, out UserProfile);
                return UserProfile;
            }
            else
            {
                string notes = Query("select notes from usernotes where useruuid = '" + agentID.ToString() + "' AND targetuuid = '" + target + "'")[0];
                if (notes == "")
                {
                    List<string> values = new List<string>();
                    values.Add(agentID.ToString());
                    values.Add(target.ToString());
                    values.Add("Insert your notes here.");
                    values.Add(System.Guid.NewGuid().ToString());
                    Insert("usernotes", values.ToArray());
                    notes = Query("select notes from usernotes where useruuid = '" + agentID.ToString() + "' AND targetuuid = '" + target + "'")[0];
                }
                Dictionary<UUID, string> Notes = new Dictionary<UUID, string>();
                Notes.Add(target, notes);

                UserProfile.Identifier = agentID.ToString();
                UserProfile.Notes = Notes; 

                UserProfileNotesCache.Add(target, UserProfile);
                return UserProfile;
            }
        }


        public void UpdateUserProfile(AuroraProfileData Profile)
        {
            List<string> SetValues = new List<string>();
            List<string> SetRows = new List<string>();
            SetRows.Add("AboutText");
            SetRows.Add("AllowPublish");
            SetRows.Add("FirstLifeAboutText");
            SetRows.Add("FirstLifeImage");
            SetRows.Add("Image");
            SetRows.Add("ProfileURL");
            SetValues.Add(Profile.AboutText);
            SetValues.Add(Profile.AllowPublish);
            SetValues.Add(Profile.FirstLifeAboutText);
            SetValues.Add(Profile.FirstLifeImage.ToString());
            SetValues.Add(Profile.Image.ToString());
            SetValues.Add(Profile.ProfileURL);
            List<string> KeyValue = new List<string>();
            List<string> KeyRow = new List<string>();
            KeyRow.Add("userUUID");
            KeyValue.Add(Profile.Identifier);
            Update("usersauth", SetValues.ToArray(), SetRows.ToArray(), KeyRow.ToArray(), KeyValue.ToArray());
        }

        public AuroraProfileData CreateTemperaryAccount(string client, string first, string last)
        {
            AuroraProfileData UserProfile = new AuroraProfileData();
            UserProfile.FirstName = first;
            UserProfile.SurName = last;
            UserProfile.Temperary = true;
            UserProfilesCache.Add(new UUID(client), UserProfile);
            return UserProfile;
        }

        public void FullUpdateUserProfile(AuroraProfileData Profile)
        {
            List<string> SetValues = new List<string>();
            List<string> SetRows = new List<string>();
            SetRows.Add("AboutText");
            SetRows.Add("profileAllowPublish");
            SetRows.Add("FirstLifeAboutText");
            SetRows.Add("FirstLifeImage");
            SetRows.Add("Image");
            SetRows.Add("ProfileURL");
            SetRows.Add("TempBanned");
            SetRows.Add("profileWantToMask");
            SetRows.Add("profileWantToText");
            SetRows.Add("profileSkillsMask");
            SetRows.Add("profileSkillsText");
            SetRows.Add("profileLanguages");
            SetValues.Add(Profile.AboutText);
            SetValues.Add(Profile.AllowPublish);
            SetValues.Add(Profile.FirstLifeAboutText);
            SetValues.Add(Profile.FirstLifeImage.ToString());
            SetValues.Add(Profile.Image.ToString());
            SetValues.Add(Profile.ProfileURL);
            SetValues.Add(Profile.TempBanned.ToString());
            SetValues.Add(Profile.Interests[0]);
            SetValues.Add(Profile.Interests[1]);
            SetValues.Add(Profile.Interests[2]);
            SetValues.Add(Profile.Interests[3]);
            SetValues.Add(Profile.Interests[4]);
            List<string> KeyValue = new List<string>();
            List<string> KeyRow = new List<string>();
            KeyRow.Add("userUUID");
            KeyValue.Add(Profile.Identifier);
            Update("usersauth", SetValues.ToArray(), SetRows.ToArray(), KeyRow.ToArray(), KeyValue.ToArray());
        }
    	public DirPlacesReplyData[] PlacesQuery(string queryText, string category, string table, string wantedValue, int StartQuery)
        {
        	List<DirPlacesReplyData> Data = new List<DirPlacesReplyData>();
            string query = String.Format("select {0} from {1} where ",
                                      wantedValue, table);
            query += "PCategory = '"+category+"' and Pdesc LIKE '%" + queryText + "%' OR PName LIKE '%" + queryText + "%'";
       
            query += " LIMIT "+StartQuery.ToString()+",50";
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        while (reader.Read())
                        {
                            int DataCount = 0;
                            DirPlacesReplyData replyData = new DirPlacesReplyData();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (DataCount == 0)
                                    replyData.parcelID = new UUID(reader.GetString(i));
                                if (DataCount == 1)
                                    replyData.name = reader.GetString(i);
                                if (DataCount == 2)
                                    replyData.forSale = Convert.ToBoolean(reader.GetString(i));
                                if (DataCount == 3)
                                    replyData.auction = Convert.ToBoolean(reader.GetString(i));
                                if (DataCount == 4)
                                    replyData.dwell = (float)Convert.ToUInt32(reader.GetString(i));
                                DataCount++;
                                if (DataCount == 5)
                                {
                                    DataCount = 0;
                                    Data.Add(replyData);
                                    replyData = new DirPlacesReplyData();
                                }
                            }
                        }
                        return Data.ToArray();
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }
        }
		public DirLandReplyData[] LandForSaleQuery(string searchType, string price, string area, string table, string wantedValue, int StartQuery)
        {
        	List<DirLandReplyData> Data = new List<DirLandReplyData>();
            string query = String.Format("select {0} from {1} where ",
                                      wantedValue, table);
            //TODO: Check this searchType ref!
            //if(searchType != "0")
            //	query += "PType = '"+searchType+"' and PPrice <= '" + price + "' and area >= '" + area + "'";
            //else
            	query += "PSalePrice <= '" + price + "' and PArea >= '" + area + "'";
            
            query += " LIMIT "+StartQuery.ToString()+",50";
            MySqlConnection dbcon = GetLockedConnection();
			IDbCommand result;
			IDataReader reader;
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
			{
                using (reader = result.ExecuteReader())
                {
                    try
                    {
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
                        return Data.ToArray();
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }
        }
		public DirEventsReplyData[] EventQuery(string queryText, string flags, string table, string wantedValue, int StartQuery)
		{
			MySqlConnection dbcon = GetLockedConnection();
			IDbCommand result;
			IDataReader reader;
			List<DirEventsReplyData> Data = new List<DirEventsReplyData>();
            string query = String.Format("select {0} from {1} where ",
                                      wantedValue, table);
            query += "EName LIKE '%" + queryText + "%' and EFlags <= '" + flags + "'";
            query += " LIMIT "+StartQuery.ToString()+",50";
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
			{
				using (reader = result.ExecuteReader())
				{
					try
					{
						while (reader.Read())
						{
							int DataCount = 0;
                            DirEventsReplyData replyData = new DirEventsReplyData();
							for (int i = 0; i < reader.FieldCount; i++)
							{
                                if (DataCount == 0)
                                    replyData.ownerID = new UUID(reader.GetString(i));
                                if (DataCount == 1)
                                    replyData.name = reader.GetString(i);
                                if (DataCount == 2)
                                    replyData.eventID = Convert.ToUInt32(reader.GetString(i));
                                if(DataCount == 3)
            					{
            						replyData.date = new DateTime(Convert.ToUInt32(reader.GetString(i))).ToString(new System.Globalization.DateTimeFormatInfo());
            						replyData.unixTime = Convert.ToUInt32(reader.GetString(i));
            					}
                                if (DataCount == 4)
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
						return Data.ToArray();
					}
					finally
					{
						reader.Close();
						reader.Dispose();
						result.Dispose();
					}
				}
			}
		}
		
		public DirEventsReplyData[] GetAllEventsNearXY(string table, int X, int Y)
		{
			MySqlConnection dbcon = GetLockedConnection();
			IDbCommand result;
			IDataReader reader;
			List<DirEventsReplyData> Data = new List<DirEventsReplyData>();
            string query = String.Format("select EOwnerID,EName,EID,EDate,EFlags from {0}",
                                      table);
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
			{
				using (reader = result.ExecuteReader())
				{
					try
					{
						while (reader.Read())
						{
							int DataCount = 0;
                            DirEventsReplyData replyData = new DirEventsReplyData();
							for (int i = 0; i < reader.FieldCount; i++)
							{
                                if (DataCount == 0)
                                    replyData.ownerID = new UUID(reader.GetString(i));
                                if (DataCount == 1)
                                    replyData.name = reader.GetString(i);
                                if (DataCount == 2)
                                    replyData.eventID = Convert.ToUInt32(reader.GetString(i));
                                if(DataCount == 3)
            					{
            						replyData.date = new DateTime(Convert.ToUInt32(reader.GetString(i))).ToString(new System.Globalization.DateTimeFormatInfo());
            						replyData.unixTime = Convert.ToUInt32(reader.GetString(i));
            					}
                                if (DataCount == 4)
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
						return Data.ToArray();
					}
					finally
					{
						reader.Close();
						reader.Dispose();
						result.Dispose();
					}
				}
			}
		}
		
		
		public DirClassifiedReplyData[] ClassifiedsQuery(string queryText, string category, string queryFlags, int StartQuery)
		{
			MySqlConnection dbcon = GetLockedConnection();
			IDbCommand result;
			IDataReader reader;
            List<DirClassifiedReplyData> Data = new List<DirClassifiedReplyData>();
			string query = "select classifieduuid, name, creationdate, expirationdate, priceforlisting from classifieds where name LIKE '%" + queryText + "%' and category = '"+category+"'";
			query += " LIMIT "+StartQuery.ToString()+",50";
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
			{
				using (reader = result.ExecuteReader())
				{
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
						return Data.ToArray();
					}
					finally
					{
						reader.Close();
						reader.Dispose();
						result.Dispose();
					}
				}
			}
		}

        public EventData GetEventInfo(string EventID)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            EventData data = new EventData();
            string query = "select EID, ECreator, EName, ECategory, EDesc, EDate, EDateUTC, EDuration, ECover, EAmount, ESimName, EGlobalPos, EEventFlags from classifieds where EID = '" + EventID + "'";
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
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
                                if (i == 12)
                                    data.eventFlags = Convert.ToUInt32(reader.GetString(i));
                            }
                        }
                        return data;
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }
        }
    }
}
