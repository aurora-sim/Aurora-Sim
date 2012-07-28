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
    public class LocalProfileConnector : ConnectorBase, IProfileConnector
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
            Init(simBase, Name);
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
        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public IUserProfileInfo GetUserProfile(UUID agentID)
        {
            object remoteValue = DoRemote(agentID);
            if (remoteValue != null || m_doRemoteOnly)
                return (IUserProfileInfo)remoteValue;

            IUserProfileInfo UserProfile = new IUserProfileInfo();
            //Try from the user profile first before getting from the DB
            if (UserProfilesCache.TryGetValue(agentID, out UserProfile))
                return UserProfile;

            var connector = GetWhetherUserIsForeign(agentID);
            if (connector != null)
                return connector.GetUserProfile(agentID);

            QueryFilter filter = new QueryFilter();
            filter.andFilters["ID"] = agentID;
            filter.andFilters["`Key`"] = "LLProfile";
            List<string> query = null;
            //Grab it from the almost generic interface
            query = GD.Query(new[] { "Value" }, "userdata", filter, null, null, null);

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

        private IRemoteProfileConnector GetWhetherUserIsForeign(UUID agentID)
        {
            OpenSim.Services.Interfaces.IUserFinder userFinder = m_registry.RequestModuleInterface<OpenSim.Services.Interfaces.IUserFinder>();
            if (userFinder != null && !userFinder.IsLocalGridUser(agentID))
            {
                string url = userFinder.GetUserServerURL(agentID, "ProfileServerURI");

                IRemoteProfileConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IRemoteProfileConnector>();
                if (connector != null)
                    connector.Init(url, m_registry);
                return connector;
            }
            return null;
        }

        /// <summary>
        ///   Update a user's profile (Note: this does not work if the user does not have a profile)
        /// </summary>
        /// <param name = "Profile"></param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool UpdateUserProfile(IUserProfileInfo Profile)
        {
            object remoteValue = DoRemote(Profile);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue != null && (bool)remoteValue;

            IUserProfileInfo previousProfile = GetUserProfile(Profile.PrincipalID);
            //Make sure the previous one exists
            if (previousProfile == null)
                return false;
            //Now fix values that the sim cannot change
            Profile.Partner = previousProfile.Partner;
            Profile.CustomType = previousProfile.CustomType;
            Profile.MembershipGroup = previousProfile.MembershipGroup;
            Profile.Created = previousProfile.Created;

            Dictionary<string, object> values = new Dictionary<string, object>(1);
            values["Value"] = OSDParser.SerializeLLSDXmlString(Profile.ToOSD());

            QueryFilter filter = new QueryFilter();
            filter.andFilters["ID"] = Profile.PrincipalID.ToString();
            filter.andFilters["`Key`"] = "LLProfile";

            //Update cache
            UserProfilesCache[Profile.PrincipalID] = Profile;

            return GD.Update("userdata", values, null, filter, null, null);
        }

        /// <summary>
        ///   Create a new profile for a user
        /// </summary>
        /// <param name = "AgentID"></param>
        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public void CreateNewProfile(UUID AgentID)
        {
            object remoteValue = DoRemote(AgentID);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            List<object> values = new List<object> {AgentID.ToString(), "LLProfile"};

            //Create a new basic profile for them
            IUserProfileInfo profile = new IUserProfileInfo {PrincipalID = AgentID};

            values.Add(OSDParser.SerializeLLSDXmlString(profile.ToOSD())); //Value which is a default Profile

            GD.Insert("userdata", values.ToArray());
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool AddClassified(Classified classified)
        {
            object remoteValue = DoRemote(classified);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue != null && (bool)remoteValue;

            if (GetUserProfile(classified.CreatorUUID) == null)
                return false;
            string keywords = classified.Description;
            if (keywords.Length > 512)
                keywords = keywords.Substring(keywords.Length - 512, 512);
            //It might be updating, delete the old
            QueryFilter filter = new QueryFilter();
            filter.andFilters["ClassifiedUUID"] = classified.ClassifiedUUID;
            GD.Delete("userclassifieds", filter);
            List<object> values = new List<object>{
                classified.Name,
                classified.Category,
                classified.SimName,
                classified.CreatorUUID,
                classified.ClassifiedUUID,
                OSDParser.SerializeJsonString(classified.ToOSD()),
                classified.ScopeID,
                classified.PriceForListing,
                keywords
            };
            return GD.Insert("userclassifieds", values.ToArray());
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<Classified> GetClassifieds(UUID ownerID)
        {
            object remoteValue = DoRemote(ownerID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<Classified>)remoteValue;

            var connector = GetWhetherUserIsForeign(ownerID);
            if (connector != null)
                return connector.GetClassifieds(ownerID);

            QueryFilter filter = new QueryFilter();
            filter.andFilters["OwnerUUID"] = ownerID;

            List<string> query = GD.Query(new[] { "*" }, "userclassifieds", filter, null, null, null);

            List<Classified> classifieds = new List<Classified>();
            for (int i = 0; i < query.Count; i += 9)
            {
                Classified classified = new Classified();
                classified.FromOSD((OSDMap) OSDParser.DeserializeJson(query[i + 5]));
                classifieds.Add(classified);
            }
            return classifieds;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public Classified GetClassified(UUID queryClassifiedID)
        {
            object remoteValue = DoRemote(queryClassifiedID);
            if (remoteValue != null || m_doRemoteOnly)
                return (Classified)remoteValue;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["ClassifiedUUID"] = queryClassifiedID;

            List<string> query = GD.Query(new[] { "*" }, "userclassifieds", filter, null, null, null);

            if (query.Count < 6)
            {
                return null;
            }
            Classified classified = new Classified();
            classified.FromOSD((OSDMap) OSDParser.DeserializeJson(query[5]));
            return classified;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void RemoveClassified(UUID queryClassifiedID)
        {
            object remoteValue = DoRemote(queryClassifiedID);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["ClassifiedUUID"] = queryClassifiedID;
            GD.Delete("userclassifieds", filter);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool AddPick(ProfilePickInfo pick)
        {
            object remoteValue = DoRemote(pick);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue != null && (bool)remoteValue;

            if (GetUserProfile(pick.CreatorUUID) == null)
                return false;

            //It might be updating, delete the old
            QueryFilter filter = new QueryFilter();
            filter.andFilters["PickUUID"] = pick.PickUUID;
            GD.Delete("userpicks", filter);
            List<object> values = new List<object>
                                      {
                                          pick.Name,
                                          pick.SimName,
                                          pick.CreatorUUID,
                                          pick.PickUUID,
                                          OSDParser.SerializeJsonString(pick.ToOSD())
                                      };
            return GD.Insert("userpicks", values.ToArray());
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public ProfilePickInfo GetPick(UUID queryPickID)
        {
            object remoteValue = DoRemote(queryPickID);
            if (remoteValue != null || m_doRemoteOnly)
                return (ProfilePickInfo)remoteValue;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["PickUUID"] = queryPickID;

            List<string> query = GD.Query(new[] { "*" }, "userpicks", filter, null, null, null);

            if (query.Count < 5)
                return null;
            ProfilePickInfo pick = new ProfilePickInfo();
            pick.FromOSD((OSDMap) OSDParser.DeserializeJson(query[4]));
            return pick;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<ProfilePickInfo> GetPicks(UUID ownerID)
        {
            object remoteValue = DoRemote(ownerID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<ProfilePickInfo>)remoteValue;

            var connector = GetWhetherUserIsForeign(ownerID);
            if (connector != null)
                return connector.GetPicks(ownerID);
            
            QueryFilter filter = new QueryFilter();
            filter.andFilters["OwnerUUID"] = ownerID;

            List<string> query = GD.Query(new[] { "*" }, "userpicks", filter, null, null, null);

            List<ProfilePickInfo> picks = new List<ProfilePickInfo>();
            for (int i = 0; i < query.Count; i += 5)
            {
                ProfilePickInfo pick = new ProfilePickInfo();
                pick.FromOSD((OSDMap) OSDParser.DeserializeJson(query[i + 4]));
                picks.Add(pick);
            }
            return picks;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void RemovePick(UUID queryPickID)
        {
            object remoteValue = DoRemote(queryPickID);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["PickUUID"] = queryPickID;
            GD.Delete("userpicks", filter);
        }

        #endregion

        public void Dispose()
        {
        }
    }
}