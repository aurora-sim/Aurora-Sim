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

        public void Initialize(IGenericData GenericData, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("ProfileConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString);

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
            else
            {
                //Check to make sure that something else exists
                List<string> m_ServerURI = simBase.ApplicationRegistry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                if (m_ServerURI.Count == 0) //Blank, not set up
                {
                    OpenSim.Framework.Console.MainConsole.Instance.Output("[AuroraDataService]: Falling back on local connector for " + "ProfileConnector", "None");
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

            IUserProfileInfo UPI = GetUserProfile(Profile.PrincipalID);
            if (UPI != null)
            {
                IDirectoryServiceConnector dirServiceConnector = DataManager.DataManager.RequestPlugin<IDirectoryServiceConnector>();
                if (dirServiceConnector != null)
                {
                    dirServiceConnector.RemoveClassifieds(UPI.Classifieds);
                    dirServiceConnector.AddClassifieds(Profile.Classifieds);
                }
            }

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
    }
}
