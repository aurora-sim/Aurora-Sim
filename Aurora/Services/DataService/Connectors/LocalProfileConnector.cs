using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;

namespace Aurora.Services.DataService
{
	public class LocalProfileConnector : IProfileConnector
	{
		private Dictionary<UUID, IUserProfileInfo> UserProfilesCache = new Dictionary<UUID, IUserProfileInfo>();
		private IGenericData GD = null;
		public LocalProfileConnector()
		{
			GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
		}

		public Classified FindClassified(string classifiedID)
		{
			List<string> retval = GD.Query("classifieduuid", classifiedID, "profileclassifieds", "*");
            if (retval.Count == 0)
                return null;
            Classified classified = new Classified();
			try {
				classified.ClassifiedUUID = retval[0];
				classified.CreatorUUID = retval[1];
				classified.CreationDate = retval[2];
				classified.ExpirationDate = retval[3];
				classified.Category = retval[4];
				classified.Name = retval[5];
				classified.Description = retval[6];
				classified.ParcelUUID = retval[7];
				classified.ParentEstate = retval[8];
				classified.SnapshotUUID = retval[9];
				classified.SimName = retval[10];
				classified.PosGlobal = retval[11];
				classified.ParcelName = retval[12];
				classified.ClassifiedFlags = retval[13];
				classified.PriceForListing = retval[14];
			} catch {
			}
			return classified;
		}

		private Classified[] ReadClassifedRow(string creatoruuid)
		{
			List<Classified> Classifieds = new List<Classified>();
			List<string> query = GD.Query("creatoruuid", creatoruuid, "profileclassifieds", "classifieduuid");
            if (query.Count == 0)
                return Classifieds.ToArray();
			try {
				for (int i = 0; i < query.Count; i++) 
                {
                    if (query[i] == "")
                        continue;
					Classifieds.Add(FindClassified(query[i]));
				}
			} catch {
			}
			return Classifieds.ToArray();
		}

		private ProfilePickInfo[] ReadPickRequestsRow(string creator)
		{
            List<ProfilePickInfo> Picks = new List<ProfilePickInfo>();
            List<string> query = GD.Query("creatoruuid", creator, "profilepicks", "pickuuid");
            if (query.Count == 0)
                return Picks.ToArray();
            try 
            {
				for (int i = 0; i < query.Count; i++)
                {
                    if (query[i] == "")
                        continue;
                    Picks.Add(FindPick(query[i]));
				}
			} 
            catch
            {
			}

			return Picks.ToArray();
		}

		public ProfilePickInfo FindPick(string pickID)
		{
			ProfilePickInfo pick = new ProfilePickInfo();
			List<string> retval = GD.Query("pickuuid", pickID, "profilepicks", "*");
            if (retval.Count == 0)
                return null;
            try 
            {
				pick.pickuuid = retval[0];
				pick.creatoruuid = retval[1];
				pick.toppick = retval[2];
				pick.parceluuid = retval[3];
				pick.name = retval[4];
				pick.description = retval[5];
				pick.snapshotuuid = retval[6];
				pick.user = retval[7];
				pick.originalname = retval[8];
				pick.simname = retval[9];
				pick.posglobal = retval[10];
				pick.sortorder = retval[11];
				pick.enabled = retval[12];
			} catch {
			}
			return pick;
		}

        private ProfileInterests ReadInterestsInfoRow(string agentID)
		{
			ProfileInterests interests = new ProfileInterests();
			List<string> results = GD.Query("PrincipalID", agentID, "profilegeneral", "WantToMask,WantToText,CanDoMask,CanDoText,Languages");
            if (results.Count == 0)
                return null;
			try {
				interests.WantToMask = results[0];
				interests.WantToText = results[1];
				interests.CanDoMask = results[2];
				interests.CanDoText = results[3];
				interests.Languages = results[4];
				if (interests.WantToMask == " ")
					interests.WantToMask = "0";
				if (interests.CanDoMask == " ")
					interests.CanDoMask = "0";
			} catch {
			}
			return interests;
		}

		public void UpdateUserNotes(UUID agentID, UUID targetAgentID, string notes, IUserProfileInfo UPI)
		{
			if (UPI.Notes.Count == 0)
				GD.Insert("profilenotes", new object[] {
					agentID,
					targetAgentID,
					notes,
					UUID.Random().ToString()
				});
			else
				GD.Update("profilenotes", new object[] { notes }, new string[] { "notes" }, new string[] {
					"userid",
					"targetuuid"
				}, new object[] {
					agentID,
					targetAgentID
				});
			RemoveFromCache(agentID);
			UserProfilesCache.Add(agentID, UPI);

		}

		public IUserProfileInfo GetUserProfile(UUID agentID)
		{
			IUserProfileInfo UserProfile = new IUserProfileInfo();
			if (UserProfilesCache.ContainsKey(agentID)) {
				UserProfilesCache.TryGetValue(agentID, out UserProfile);
				return UserProfile;
			} else {
				List<string> userauthReturns = GD.Query("PrincipalID", agentID.ToString(), "profilegeneral", "AllowPublish,MaturePublish,Partner,WebURL,AboutText,FirstLifeAboutText,Image,FirstLifeImage,CustomType,Visible,IMViaEmail,MembershipGroup,AArchiveName,IsNewUser,Created");
				UserProfile.Classifieds = ReadClassifedRow(agentID.ToString());
				UserProfile.Interests = ReadInterestsInfoRow(agentID.ToString());
				UserProfile.Picks = ReadPickRequestsRow(agentID.ToString());

				#region Notes
				List<string> notesReturns = GD.Query("userid", agentID.ToString(), "profilenotes", "targetuuid,notes");
				Dictionary<string, string> Notes = new Dictionary<string, string>();
				if (notesReturns.Count != 0) {
					for (int i = 0; i < notesReturns.Count; i = i + 2) 
                    {
                        if (notesReturns[i] == "" || Notes.ContainsKey(notesReturns[i]))
                            continue;
						Notes.Add(notesReturns[i], notesReturns[i + 1]);
					}
				}
				UserProfile.Notes = Notes;
				#endregion

				if (userauthReturns.Count == 0)
					return null;

				UserProfile.PrincipalID = agentID;
				UserProfile.AllowPublish = bool.Parse(userauthReturns[0]);
				UserProfile.MaturePublish = bool.Parse(userauthReturns[1]);
				UserProfile.Partner = new UUID(userauthReturns[2]);
				UserProfile.WebURL = userauthReturns[3];
				UserProfile.AboutText = userauthReturns[4];
				UserProfile.FirstLifeAboutText = userauthReturns[5];
				UserProfile.Image = new UUID(userauthReturns[6]);
				UserProfile.FirstLifeImage = new UUID(userauthReturns[7]);
				UserProfile.CustomType = userauthReturns[8];
				UserProfile.Visible = Convert.ToBoolean(userauthReturns[9]);
				UserProfile.IMViaEmail = Convert.ToBoolean(userauthReturns[10]);
				UserProfile.MembershipGroup = userauthReturns[11];
				UserProfile.AArchiveName = userauthReturns[12];
				UserProfile.IsNewUser = Convert.ToBoolean(userauthReturns[13]);
				UserProfile.Created = Convert.ToInt32(userauthReturns[14]);
                if (UserProfilesCache.ContainsKey(agentID))
                    UserProfilesCache.Remove(agentID);
					UserProfilesCache.Add(agentID, UserProfile);
				return UserProfile;
			}
		}

        public void UpdateUserNotes(UUID uUID, UUID queryTargetID, string notes)
        {

        }

        public bool UpdateUserProfile(IUserProfileInfo Profile)
		{
			List<object> SetValues = new List<object>();
			List<string> SetRows = new List<string>();
			SetRows.Add("AllowPublish");
			SetRows.Add("MaturePublish");
			SetRows.Add("Partner");
			SetRows.Add("WebURL");
			SetRows.Add("AboutText");
			SetRows.Add("FirstLifeAboutText");
			SetRows.Add("Image");
			SetRows.Add("FirstLifeImage");
			SetRows.Add("CustomType");
			SetRows.Add("Visible");
			SetRows.Add("IMViaEmail");
			SetRows.Add("MembershipGroup");
			SetRows.Add("AArchiveName");
			SetRows.Add("IsNewUser");
			SetRows.Add("Created");
			//Interests
			SetRows.Add("WantToMask");
			SetRows.Add("WantToText");
			SetRows.Add("CanDoMask");
			SetRows.Add("CanDoText");
			SetRows.Add("Languages");
			SetValues.Add(Profile.AllowPublish);
			SetValues.Add(Profile.MaturePublish);
			SetValues.Add(Profile.Partner);
			SetValues.Add(Profile.WebURL);
			SetValues.Add(Profile.AboutText);
			SetValues.Add(Profile.FirstLifeAboutText);
			SetValues.Add(Profile.Image);
			SetValues.Add(Profile.FirstLifeImage);
			SetValues.Add(Profile.CustomType);
			SetValues.Add(Profile.Visible);
			SetValues.Add(Profile.IMViaEmail);
			SetValues.Add(Profile.MembershipGroup);
			SetValues.Add(Profile.AArchiveName);
			SetValues.Add(Profile.IsNewUser);
			SetValues.Add(Profile.Created);
			//Interests
			SetValues.Add(Profile.Interests.WantToMask);
			SetValues.Add(Profile.Interests.WantToText);
			SetValues.Add(Profile.Interests.CanDoMask);
			SetValues.Add(Profile.Interests.CanDoText);
			SetValues.Add(Profile.Interests.Languages);
			List<object> KeyValue = new List<object>();
			List<string> KeyRow = new List<string>();
			KeyRow.Add("PrincipalID");
			KeyValue.Add(Profile.PrincipalID.ToString());
			RemoveFromCache(Profile.PrincipalID);
			return GD.Update("profilegeneral", SetValues.ToArray(), SetRows.ToArray(), KeyRow.ToArray(), KeyValue.ToArray());
		}

        public void UpdateUserInterests(IUserProfileInfo Profile)
        {
            List<object> SetValues = new List<object>();
            List<string> SetRows = new List<string>();
            //Interests
            SetRows.Add("WantToMask");
            SetRows.Add("WantToText");
            SetRows.Add("CanDoMask");
            SetRows.Add("CanDoText");
            SetRows.Add("Languages");
            //Interests
            SetValues.Add(Profile.Interests.WantToMask);
            SetValues.Add(Profile.Interests.WantToText);
            SetValues.Add(Profile.Interests.CanDoMask);
            SetValues.Add(Profile.Interests.CanDoText);
            SetValues.Add(Profile.Interests.Languages);
            List<object> KeyValue = new List<object>();
            List<string> KeyRow = new List<string>();
            KeyRow.Add("PrincipalID");
            KeyValue.Add(Profile.PrincipalID.ToString());
            RemoveFromCache(Profile.PrincipalID);
            GD.Update("profilegeneral", SetValues.ToArray(), SetRows.ToArray(), KeyRow.ToArray(), KeyValue.ToArray());
        }

		public void CreateNewProfile(UUID UUID)
		{
			List<object> values = new List<object>();
			values.Add(UUID.ToString());
			values.Add(true);
			values.Add(true);
			values.Add(UUID.Zero);
			values.Add(" ");
			values.Add(" ");
			values.Add(" ");
			values.Add(UUID.Zero);
			values.Add(UUID.Zero);
			values.Add(" ");
			values.Add("0");
			values.Add(" ");
			values.Add("0");
			values.Add(" ");
			values.Add(" ");
			values.Add(true);
			values.Add(false);
			values.Add(" ");
			values.Add(" ");
			values.Add(true);
			values.Add(Util.UnixTimeSinceEpoch().ToString());
			var GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
			GD.Insert("profilegeneral", values.ToArray());
		}

		public void RemoveFromCache(UUID ID)
		{
			UserProfilesCache.Remove(ID);
		}

        public void AddClassified(Classified classified)
        {
            List<string> values = new List<string>();
            values.Add(classified.ClassifiedUUID);
            values.Add(classified.CreatorUUID);
            values.Add(classified.CreationDate);
            values.Add(classified.ExpirationDate);
            values.Add(classified.Category);
            values.Add(classified.Name);
            values.Add(classified.Description);
            values.Add(classified.ParcelUUID);
            values.Add(classified.ParentEstate);
            values.Add(classified.SnapshotUUID);
            values.Add(classified.SimName);
            values.Add(classified.PosGlobal);
            values.Add(classified.ParcelName);
            values.Add(classified.ClassifiedFlags);
            values.Add(classified.PriceForListing);
            GD.Insert("classifieds", values.ToArray());
            RemoveFromCache(new UUID(classified.CreatorUUID));
        }

        public void DeleteClassified(UUID ID, UUID agentID)
        {
            List<string> keys = new List<string>();
            List<object> values = new List<object>();
            keys.Add("classifieduuid");
            values.Add(ID);
            GD.Delete("classifieds", keys.ToArray(), values.ToArray());
            RemoveFromCache(agentID);
        }

        public void AddPick(ProfilePickInfo pick)
        {
            List<string> values = new List<string>();
            values.Add(pick.pickuuid);
            values.Add(pick.creatoruuid);
            values.Add(pick.toppick);
            values.Add(pick.parceluuid);
            values.Add(pick.name);
            values.Add(pick.description);
            values.Add(pick.snapshotuuid);
            values.Add(pick.user);
            values.Add(pick.originalname);
            values.Add(pick.simname);
            values.Add(pick.posglobal);
            values.Add(pick.sortorder);
            values.Add(pick.enabled);

            GD.Insert("userpicks", values.ToArray());
            RemoveFromCache(new UUID(pick.creatoruuid));
        }

        public void UpdatePick(ProfilePickInfo pick)
        {
            List<string> keys = new List<string>();
            List<string> values = new List<string>();
            keys.Add("parceluuid");
            keys.Add("name");
            keys.Add("snapshotuuid");
            keys.Add("description");
            keys.Add("simname");
            keys.Add("posglobal");
            keys.Add("sortorder");
            keys.Add("enabled");
            values.Add(pick.parceluuid);
            values.Add(pick.name);
            values.Add(pick.snapshotuuid);
            values.Add(pick.description);
            values.Add(pick.simname);
            values.Add(pick.posglobal);
            values.Add(pick.sortorder);
            values.Add(pick.enabled);
            GD.Update("userpicks", values.ToArray(), keys.ToArray(), new string[] { "pickuuid" }, new object[] { pick.pickuuid });
            RemoveFromCache(new UUID(pick.creatoruuid));
        }

        public void DeletePick(UUID ID, UUID agentID)
        {
            List<string> keys = new List<string>();
            List<object> values = new List<object>();
            keys.Add("pickuuid");
            values.Add(ID);
            GD.Delete("userpicks", keys.ToArray(), values.ToArray());
            RemoveFromCache(agentID);
        }
    }
}
