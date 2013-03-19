/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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

using Aurora.DataManager;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;

namespace Aurora.Services
{
    public class AgentInfoService : ConnectorBase, IService, IAgentInfoService
    {
        #region Declares

        protected IAgentInfoConnector m_agentInfoConnector;

        #endregion

        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AgentInfoHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IAgentInfoService>(this);
            Init(registry, Name);
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual void FinishedStartup()
        {
            m_agentInfoConnector = Aurora.DataManager.DataManager.RequestPlugin<IAgentInfoConnector>();
        }

        #endregion

        #region IAgentInfoService Members

        public IAgentInfoService InnerService
        {
            get { return this; }
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual UserInfo GetUserInfo(string userID)
        {
            object remoteValue = DoRemote(userID);
            if (remoteValue != null || m_doRemoteOnly)
                return (UserInfo)remoteValue;

            return GetUserInfo(userID, true);
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual List<UserInfo> GetUserInfos(List<string> userIDs)
        {
            object remoteValue = DoRemote(userIDs);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<UserInfo>)remoteValue;

            List<UserInfo> infos = new List<UserInfo>();
            for (int i = 0; i < userIDs.Count; i++)
            {
                var userInfo = GetUserInfo(userIDs[i]);
                if(userInfo != null)
                    infos.Add(userInfo);
            }
            return infos;
        }

        /// <summary>
        /// Gets a list of userinfos that are logged into the given region
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public List<UserInfo> GetUserInfos(UUID regionID)
        {
            object remoteValue = DoRemote(regionID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<UserInfo>)remoteValue;

            return m_agentInfoConnector.GetByCurrentRegion(regionID.ToString());
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual List<string> GetAgentsLocations(string requestor, List<string> userIDs)
        {
            object remoteValue = DoRemote(requestor, userIDs);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<string>)remoteValue;

            string[] infos = new string[userIDs.Count];
            for (int i = 0; i < userIDs.Count; i++)
            {
                UserInfo user = GetUserInfo(userIDs[i]);
                if (user != null && user.IsOnline)
                    infos[i] = user.CurrentRegionURI;
                else if (user == null)
                    infos[i] = "NonExistant";
                else
                    infos[i] = "NotOnline";
            }
            return new List<string>(infos);
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual bool SetHomePosition(string userID, UUID homeID, Vector3 homePosition, Vector3 homeLookAt)
        {
            object remoteValue = DoRemote(userID, homeID, homePosition, homeLookAt);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            m_agentInfoConnector.SetHomePosition(userID, homeID, homePosition, homeLookAt);
            return true;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt, string regionURI)
        {
            object remoteValue = DoRemote(userID, regionID, lastPosition, lastLookAt);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            m_agentInfoConnector.SetLastPosition(userID, regionID, regionURI, lastPosition, lastLookAt);
        }

        public virtual void SetLoggedIn(string userID, bool loggingIn, UUID enteringRegion, string enteringRegionURI)
        {
            UserInfo userInfo = GetUserInfo(userID, false); //We are changing the status, so don't look
            if (userInfo == null)
            {
                Save(new UserInfo
                         {
                             IsOnline = loggingIn,
                             UserID = userID,
                             CurrentLookAt = Vector3.Zero,
                             CurrentPosition = Vector3.Zero,
                             CurrentRegionID = enteringRegion,
                             CurrentRegionURI = enteringRegionURI,
                             HomeLookAt = Vector3.Zero,
                             HomePosition = Vector3.Zero,
                             HomeRegionID = UUID.Zero,
                             Info = new OSDMap(),
                             LastLogin = DateTime.Now.ToUniversalTime(),
                             LastLogout = DateTime.Now.ToUniversalTime(),
                         });
                return;
            }

            Dictionary<string, object> agentUpdateValues = new Dictionary<string, object>();
            agentUpdateValues["IsOnline"] = loggingIn ? 1 : 0;
            if (loggingIn)
            {
                agentUpdateValues["LastLogin"] = Util.ToUnixTime(DateTime.Now.ToUniversalTime());
                if (enteringRegion != UUID.Zero)
                {
                    agentUpdateValues["CurrentRegionID"] = enteringRegion;
                    agentUpdateValues["CurrentRegionURI"] = enteringRegionURI;
                }
            }
            else
            {
                agentUpdateValues["LastLogout"] = Util.ToUnixTime(DateTime.Now.ToUniversalTime());
            }
            agentUpdateValues["LastSeen"] = Util.ToUnixTime(DateTime.Now.ToUniversalTime());
            m_agentInfoConnector.Update(userID, agentUpdateValues);
        }

        public void FireUserStatusChangeEvent(string userID, bool loggingIn, UUID enteringRegion)
        {
            //Trigger an event so listeners know
            m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler(
                "UserStatusChange", new object[] { userID, loggingIn, enteringRegion });
        }

        #endregion

        #region Helpers

        private UserInfo GetUserInfo(string userID, bool checkForOfflineStatus)
        {
            bool changed = false;
            UserInfo info = m_agentInfoConnector.Get(userID, checkForOfflineStatus, out changed);
            if (changed)
                m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler(
                    "UserStatusChange", new object[] {userID, false, UUID.Zero});
            return info;
        }

        public virtual void Save(UserInfo userInfo)
        {
            m_agentInfoConnector.Set(userInfo);
        }

        #endregion
    }
}