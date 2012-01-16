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

using System.Collections.Generic;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Services.DataService
{
    public class LocalProfileConnector : IProfileConnector
    {
        //We can use a cache because we are the only place that profiles will be served from
        private readonly Dictionary<UUID, IUserProfileInfo> UserProfilesCache = new Dictionary<UUID, IUserProfileInfo>();
        private IGenericData GD;

        #region IProfileConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            GD = GenericData;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase(defaultConnectionString, "Agent",
                                 source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            DataManager.DataManager.RegisterPlugin(Name + "Local", this);

            if (source.Configs["AuroraConnectors"].GetString("ProfileConnector", "LocalConnector") == "LocalConnector")
            {
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IProfileConnector"; }
        }

        /// <summary>
        ///   Get a user's profile
        /// </summary>
        /// <param name = "agentID"></param>
        /// <returns></returns>
        public IUserProfileInfo GetUserProfile(UUID agentID)
        {
            IUserProfileInfo UserProfile = new IUserProfileInfo();
            //Try from the user profile first before getting from the DB
            if (UserProfilesCache.TryGetValue(agentID, out UserProfile))
                return UserProfile;
            else
            {
                Dictionary<string, object> where = new Dictionary<string, object>();
                where["ID"] = agentID;
                where["`Key`"] = "LLProfile";
                List<string> query = null;
                //Grab it from the almost generic interface
                query = GD.Query(new string[] { "Value" }, "userdata", new QueryFilter
                {
                    andFilters = where
                }, null, null, null);

                if (query == null || query.Count == 0)
                    return null;
                //Pull out the OSDmap
                OSDMap profile = (OSDMap) OSDParser.DeserializeLLSDXml(query[0]);

                UserProfile = new IUserProfileInfo();
                UserProfile.FromOSD(profile);

                //Add to the cache
                UserProfilesCache[agentID] = UserProfile;

                return UserProfile;
            }
        }

        /// <summary>
        ///   Update a user's profile (Note: this does not work if the user does not have a profile)
        /// </summary>
        /// <param name = "Profile"></param>
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
            List<string> SetRows = new List<string> {"Value"};
            SetValues.Add(OSDParser.SerializeLLSDXmlString(Profile.ToOSD()));

            List<object> KeyValue = new List<object>();
            List<string> KeyRow = new List<string> {"ID"};
            KeyValue.Add(Profile.PrincipalID.ToString());
            KeyRow.Add("`Key`");
            KeyValue.Add("LLProfile");

            //Update cache
            UserProfilesCache[Profile.PrincipalID] = Profile;

            return GD.Update("userdata", SetValues.ToArray(), SetRows.ToArray(), KeyRow.ToArray(), KeyValue.ToArray());
        }

        /// <summary>
        ///   Create a new profile for a user
        /// </summary>
        /// <param name = "AgentID"></param>
        public void CreateNewProfile(UUID AgentID)
        {
            List<object> values = new List<object> {AgentID.ToString(), "LLProfile"};

            //Create a new basic profile for them
            IUserProfileInfo profile = new IUserProfileInfo {PrincipalID = AgentID};

            values.Add(OSDParser.SerializeLLSDXmlString(profile.ToOSD())); //Value which is a default Profile

            GD.Insert("userdata", values.ToArray());
        }

        public bool AddClassified(Classified classified)
        {
            if (GetUserProfile(classified.CreatorUUID) == null)
                return false;
            //It might be updating, delete the old
            GD.Delete("userclassifieds", new string[1] {"ClassifiedUUID"}, new object[1] {classified.ClassifiedUUID});
            List<object> values = new List<object>
                                      {
                                          classified.Name.MySqlEscape(),
                                          classified.Category,
                                          classified.SimName.MySqlEscape(),
                                          classified.CreatorUUID,
                                          classified.ClassifiedUUID,
                                          OSDParser.SerializeJsonString(classified.ToOSD())
                                      };
            return GD.Insert("userclassifieds", values.ToArray());
        }

        public List<Classified> GetClassifieds(UUID ownerID)
        {
            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where["OwnerUUID"] = ownerID;

            List<string> query = GD.Query(new string[1] { "*" }, "userclassifieds", new QueryFilter
            {
                andFilters = where
            }, null, null, null);

            List<Classified> classifieds = new List<Classified>();
            for (int i = 0; i < query.Count; i += 6)
            {
                Classified classified = new Classified();
                classified.FromOSD((OSDMap) OSDParser.DeserializeJson(query[i + 5]));
                classifieds.Add(classified);
            }
            return classifieds;
        }

        public Classified GetClassified(UUID queryClassifiedID)
        {
            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where["ClassifiedUUID"] = queryClassifiedID;

            List<string> query = GD.Query(new string[1] { "*" }, "userclassifieds", new QueryFilter
            {
                andFilters = where
            }, null, null, null);

            if (query.Count < 6)
            {
                return null;
            }
            Classified classified = new Classified();
            classified.FromOSD((OSDMap) OSDParser.DeserializeJson(query[5]));
            return classified;
        }

        public void RemoveClassified(UUID queryClassifiedID)
        {
            GD.Delete("userclassifieds", new string[1] {"ClassifiedUUID"}, new object[1] {queryClassifiedID});
        }

        public bool AddPick(ProfilePickInfo pick)
        {
            if (GetUserProfile(pick.CreatorUUID) == null)
                return false;

            //It might be updating, delete the old
            GD.Delete("userpicks", new string[1] {"PickUUID"}, new object[1] {pick.PickUUID});
            List<object> values = new List<object>
                                      {
                                          pick.Name.MySqlEscape(),
                                          pick.SimName.MySqlEscape(),
                                          pick.CreatorUUID,
                                          pick.PickUUID,
                                          OSDParser.SerializeJsonString(pick.ToOSD())
                                      };
            return GD.Insert("userpicks", values.ToArray());
        }

        public ProfilePickInfo GetPick(UUID queryPickID)
        {
            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where["PickUUID"] = queryPickID;

            List<string> query = GD.Query(new string[1] { "*" }, "userpicks", new QueryFilter
            {
                andFilters = where
            }, null, null, null);

            if (query.Count < 5)
                return null;
            ProfilePickInfo pick = new ProfilePickInfo();
            pick.FromOSD((OSDMap) OSDParser.DeserializeJson(query[4]));
            return pick;
        }

        public List<ProfilePickInfo> GetPicks(UUID ownerID)
        {
            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where["OwnerUUID"] = ownerID;

            List<string> query = GD.Query(new string[1] { "*" }, "userpicks", new QueryFilter
            {
                andFilters = where
            }, null, null, null);

            List<ProfilePickInfo> picks = new List<ProfilePickInfo>();
            for (int i = 0; i < query.Count; i += 5)
            {
                ProfilePickInfo pick = new ProfilePickInfo();
                pick.FromOSD((OSDMap) OSDParser.DeserializeJson(query[i + 4]));
                picks.Add(pick);
            }
            return picks;
        }

        public void RemovePick(UUID queryPickID)
        {
            GD.Delete("userpicks", new string[1] {"PickUUID"}, new object[1] {queryPickID});
        }

        #endregion

        public void Dispose()
        {
        }
    }
}