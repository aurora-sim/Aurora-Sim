using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Threading;
using Aurora.DataManager;
using Mono.Data.SqliteClient;
using OpenSim.Data;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.DataManager.SQLite
{
    public class SQLiteProfile : SQLiteLoader, IProfileData
    {
        private Dictionary<UUID, AuroraProfileData> UserProfilesCache = new Dictionary<UUID, AuroraProfileData>();
        private Dictionary<UUID, AuroraProfileData> UserProfileNotesCache = new Dictionary<UUID, AuroraProfileData>();
        public List<string> ReadClassifiedInfoRow(string classifiedID)
        {
            SqliteCommand cmd = new SqliteCommand();
            string query = "select * from classifieds where classifieduuid = '" + classifiedID + "'";
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
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
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);
            return retval;
        }

        public Dictionary<UUID, string> ReadClassifedRow(string creatoruuid)
        {
            SqliteCommand cmd = new SqliteCommand();
            string query = "select classifieduuid, name from classifieds where creatoruuid = '" + creatoruuid + "'";
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
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
            catch (Exception ex){}
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);
            return retval;
        }

        public Dictionary<UUID, string> ReadPickRequestsRow(string creator)
        {
            SqliteCommand cmd = new SqliteCommand();
            string query = "select pickuuid,name from userpicks where creatoruuid = '" + creator + "'";
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            Dictionary<UUID, string> retval = new Dictionary<UUID, string>();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i = i + 2)
                {
                    retval.Add(new UUID(reader.GetValue(i).ToString()), reader.GetValue(i + 1).ToString());
                }
            }
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);
            return retval;
        }

        public Dictionary<UUID, string> ReadPickRow(string creator)
        {
            SqliteCommand cmd = new SqliteCommand();
            string query = "select pickuuid,name from userpicks where creatoruuid = '" + creator + "'";
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            Dictionary<UUID, string> retval = new Dictionary<UUID, string>();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i = i + 2)
                {
                    retval.Add(new UUID(reader.GetValue(i).ToString()), reader.GetValue(i + 1).ToString());
                }
            }
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);
            return retval;
        }

        public List<string> ReadInterestsInfoRow(string agentID)
        {
            SqliteCommand cmd = new SqliteCommand();
            string query = "select * from usersauth where userUUID = '" + agentID + "'";
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            List<string> retval = new List<string>();

            if (reader.Read())
            {
                try
                {
                    retval.Add(Convert.ToString(reader["profileWantToMask"].ToString()));
                    if (retval[0] == " ")
                        retval[0] = "0";
                    retval.Add(Convert.ToString(reader["profileWantToText"].ToString()));
                    retval.Add(Convert.ToString(reader["profileSkillsMask"].ToString()));
                    if (retval[2] == " ")
                        retval[2] = "0";
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
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);
            return retval;
        }

        public List<string> ReadPickInfoRow(string creator, string pickID)
        {
            SqliteCommand cmd = new SqliteCommand();
            string query = "select * from userpicks where creatoruuid = '" + creator + "' AND pickuuid = '" + pickID + "'";
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            List<string> retval = new List<string>();
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
            reader.Close();
            reader.Dispose();
            CloseReaderCommand(cmd);
            return retval;
        }

        public void InvalidateProfileNotes(UUID target)
        {
            UserProfileNotesCache.Remove(target);
        }

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

        public List<string> Query(string query)
        {
            SqliteCommand cmd = new SqliteCommand();
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            List<string> RetVal = new List<string>();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    RetVal.Add(reader.GetString(i));
                }
            }
            if (RetVal.Count == 0)
                RetVal.Add("");

            CloseReaderCommand(cmd);
            return RetVal;
        }

        public AuroraProfileData GetProfileInfo(UUID agentID, UUID target)
        {
            AuroraProfileData UserProfile = new AuroraProfileData();
            if (UserProfilesCache.ContainsKey(agentID))
            {
                UserProfilesCache.TryGetValue(agentID, out UserProfile);
                return UserProfile;
            }
            else
            {
                string profileURL = Query("select profileURL from usersauth where userUUID = '" + agentID.ToString() + "'")[0];
                List<string> Interests = ReadInterestsInfoRow(agentID.ToString());
                string allowpublish = Query("select profileAllowPublish from usersauth where userUUID = '" + agentID.ToString() + "'")[0];
                string maturepublish = Query("select profileMaturePublish from usersauth where userUUID = '" + agentID.ToString() + "'")[0];
                string membershipGroup = Query("select membershipGroup from usersauth where userUUID = '" + agentID.ToString() + "'")[0];
                string godlevel = Query("select userGodLevel from usersauth where userUUID = '" + agentID.ToString() + "'")[0];
                List<string> name = Query("select userlogin from usersauth where userUUID = '" + agentID.ToString() + "'");
                string Password = Query("select userPass from usersauth where userUUID = '" + agentID.ToString() + "'")[0];
                if (godlevel == " ")
                    godlevel = "0";
                if (allowpublish == " ")
                    allowpublish = "0";
                if (maturepublish == " ")
                    maturepublish = "0";
                UserProfile.PasswordHash = Password;
                UserProfile.FirstName = name[0].Split(' ')[0];
                UserProfile.SurName = name[0].Split(' ')[1];
                UserProfile.Identifier = agentID.ToString();
                UserProfile.ProfileUrl = profileURL;
                UserProfile.Interests = Interests;
                UserProfile.MembershipGroup = membershipGroup;
                UserProfile.AllowPublish = allowpublish;
                UserProfile.MaturePublish = maturepublish;
                UserProfile.GodLevel = Convert.ToInt32(godlevel);
                UserProfilesCache.Add(agentID, UserProfile);
                return UserProfile;
            }
        }

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
                    if (Profile.Count == 1)
                        return null;
                    if (Profile[2] == " ")
                        Profile[2] = "0";
                    if (Profile[5] == " ")
                        Profile[5] = "0";
                    if (Profile[4] == " ")
                        Profile[4] = "0";
                    if (Profile[11] == " ")
                        Profile[11] = UUID.Zero.ToString();
                    if (Profile[12] == " ")
                        Profile[12] = UUID.Zero.ToString();
                    if (Profile[15] == " ")
                        Profile[15] = UUID.Zero.ToString();
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
                    UserProfile.FirstLifeImage = new UUID(Profile[12]);
                    UserProfile.Partner = Profile[12];
                    UserProfile.PermaBanned = Convert.ToInt32(Profile[13]);
                    UserProfile.TempBanned = Convert.ToInt32(Profile[14]);
                    UserProfile.Image = new UUID(Profile[15]);
                    UserProfilesCache.Add(agentID, UserProfile);
                    return UserProfile;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }

        public void UpdateUserProfile(AuroraProfileData Profile)
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

        public AuroraProfileData CreateTemperaryAccount(string client, string first, string last)
        {
            AuroraProfileData UserProfile = new AuroraProfileData();
            UserProfile.FirstName = first;
            UserProfile.SurName = last;
            UserProfile.Temperary = true;
            UserProfilesCache.Add(new UUID(client), UserProfile);
            return UserProfile;
        }

    }
}
