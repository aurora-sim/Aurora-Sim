using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using Nini.Config;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class LocalProfileConnector : IProfileConnector
	{
        //We can use a cache because we are the only place that profiles will be served from
		private Dictionary<UUID, IUserProfileInfo> UserProfilesCache = new Dictionary<UUID, IUserProfileInfo>();
        private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            GD = GenericData;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase(defaultConnectionString, "Agent", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            DataManager.DataManager.RegisterPlugin(Name+"Local", this);

            if (source.Configs["AuroraConnectors"].GetString("ProfileConnector", "LocalConnector") == "LocalConnector")
            {
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IProfileConnector"; }
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Get a user's profile
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns></returns>
		public IUserProfileInfo GetUserProfile(UUID agentID)
		{
			IUserProfileInfo UserProfile = new IUserProfileInfo();
            //Try from the user profile first before getting from the DB
            if (UserProfilesCache.TryGetValue(agentID, out UserProfile)) 
				return UserProfile;
            else 
            {
                UserProfile = new IUserProfileInfo();
                List<string> query = null;
                //Grab it from the almost generic interface
                query = GD.Query(new string[]{"ID", "`Key`"}, new object[]{agentID, "LLProfile"}, "userdata", "Value");
                
                if (query == null || query.Count == 0)
					return null;
                //Pull out the OSDmap
                OSDMap profile = (OSDMap)OSDParser.DeserializeLLSDXml(query[0]);
                UserProfile.FromOSD(profile);

				//Add to the cache
			    UserProfilesCache[agentID] = UserProfile;

				return UserProfile;
			}
		}

        /// <summary>
        /// Update a user's profile (Note: this does not work if the user does not have a profile)
        /// </summary>
        /// <param name="Profile"></param>
        /// <returns></returns>
        public bool UpdateUserProfile(IUserProfileInfo Profile)
        {
            IUserProfileInfo previousProfile = GetUserProfile(Profile.PrincipalID);
            //Make sure the previous one exists
            if (previousProfile == null)
                return false;
            //Now fix values that the sim cannot change
            Profile.Partner = previousProfile.Partner;
            Profile.CustomType = previousProfile.CustomType;
            Profile.MembershipGroup = previousProfile.MembershipGroup;
            Profile.Created = previousProfile.Created;

            List<object> SetValues = new List<object>();
            List<string> SetRows = new List<string>();
            SetRows.Add("Value");
            SetValues.Add(OSDParser.SerializeLLSDXmlString(Profile.ToOSD()));

            List<object> KeyValue = new List<object>();
			List<string> KeyRow = new List<string>();
			KeyRow.Add("ID");
            KeyValue.Add(Profile.PrincipalID.ToString());
            KeyRow.Add("`Key`");
            KeyValue.Add("LLProfile");

            //Update cache
            UserProfilesCache[Profile.PrincipalID] = Profile;

            return GD.Update("userdata", SetValues.ToArray(), SetRows.ToArray(), KeyRow.ToArray(), KeyValue.ToArray());
		}

        /// <summary>
        /// Create a new profile for a user
        /// </summary>
        /// <param name="AgentID"></param>
        public void CreateNewProfile(UUID AgentID)
		{
			List<object> values = new List<object>();
            values.Add(AgentID.ToString()); //ID
            values.Add("LLProfile"); //Key

            //Create a new basic profile for them
            IUserProfileInfo profile = new IUserProfileInfo();
            profile.PrincipalID = AgentID;

            values.Add(OSDParser.SerializeLLSDXmlString(profile.ToOSD())); //Value which is a default Profile
			
            GD.Insert("userdata", values.ToArray());
		}

        public void AddClassified (Classified classified)
        {
            //It might be updating, delete the old
            GD.Delete ("userclassifieds", new string[1] { "ClassifiedUUID" }, new object[1] { classified.ClassifiedUUID });
            List<object> values = new List<object>();
            values.Add(classified.Name);
            values.Add(classified.Category);
            values.Add(classified.SimName);
            values.Add(classified.CreatorUUID);
            values.Add(classified.ClassifiedUUID);
            values.Add(OSDParser.SerializeJsonString(classified.ToOSD()));
            GD.Insert("userclassifieds", values.ToArray());
        }

        public List<Classified> GetClassifieds (UUID ownerID)
        {
            List<Classified> classifieds = new List<Classified> ();
            List<string> query = GD.Query (new string[1] { "OwnerUUID" }, new object[1] { ownerID }, "userclassifieds", "*");
            for (int i = 0; i < query.Count; i+=6)
            {
                Classified classified = new Classified ();
                classified.FromOSD ((OSDMap)OSDParser.DeserializeJson (query[i+5]));
                classifieds.Add (classified);
            }
            return classifieds;
        }

        public Classified GetClassified (UUID queryClassifiedID)
        {
            List<string> query = GD.Query (new string[1] { "ClassifiedUUID" }, new object[1] { queryClassifiedID }, "userclassifieds", "*");
            if (query.Count < 6)
                return null;
            Classified classified = new Classified ();
            classified.FromOSD ((OSDMap)OSDParser.DeserializeJson (query[5]));
            return classified;
        }

        public void RemoveClassified (UUID queryClassifiedID)
        {
            GD.Delete ("userclassifieds", new string[1] { "ClassifiedUUID" }, new object[1] { queryClassifiedID });
        }

        public void AddPick (ProfilePickInfo pick)
        {
            //It might be updating, delete the old
            GD.Delete ("userpicks", new string[1] { "PickUUID" }, new object[1] { pick.PickUUID });
            List<object> values = new List<object> ();
            values.Add (pick.Name);
            values.Add (pick.SimName);
            values.Add (pick.CreatorUUID);
            values.Add (pick.PickUUID);
            values.Add (OSDParser.SerializeJsonString (pick.ToOSD ()));
            GD.Insert ("userpicks", values.ToArray ());
        }

        public ProfilePickInfo GetPick (UUID queryPickID)
        {
            List<string> query = GD.Query (new string[1] { "PickUUID" }, new object[1] { queryPickID }, "userpicks", "*");
            if (query.Count < 5)
                return null;
            ProfilePickInfo pick = new ProfilePickInfo ();
            pick.FromOSD ((OSDMap)OSDParser.DeserializeJson (query[4]));
            return pick;
        }

        public List<ProfilePickInfo> GetPicks (UUID ownerID)
        {
            List<ProfilePickInfo> picks = new List<ProfilePickInfo> ();
            List<string> query = GD.Query (new string[1] { "OwnerUUID" }, new object[1] { ownerID }, "userpicks", "*");
            for (int i = 0; i < query.Count; i+=5)
            {
                ProfilePickInfo pick = new ProfilePickInfo ();
                pick.FromOSD ((OSDMap)OSDParser.DeserializeJson (query[i+4]));
                picks.Add (pick);
            }
            return picks;
        }

        public void RemovePick (UUID queryPickID)
        {
            GD.Delete ("userpicks", new string[1] { "PickUUID" }, new object[1] { queryPickID });
        }
    }
}
