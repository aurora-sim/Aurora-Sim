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

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Nini.Config;
using log4net;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Simulation.Base;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services
{
    public class AgentInfoService : IService, IAgentInfoService
    {
        #region Declares

        protected IAgentInfoConnector m_agentInfoConnector;
        protected IRegistryCore m_registry;
        protected List<string> m_lockedUsers = new List<string>();

        #endregion

        #region IService Members

        public virtual void Initialize (IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AgentInfoHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IAgentInfoService>(this);
        }

        public virtual void Start (IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual void FinishedStartup ()
        {
            m_agentInfoConnector = Aurora.DataManager.DataManager.RequestPlugin<IAgentInfoConnector>();
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        #endregion

        #region IAgentInfoService Members

        public IAgentInfoService InnerService
        {
            get { return this; }
        }

        public virtual UserInfo GetUserInfo(string userID)
        {
            return GetUserInfo (userID, true);
        }

        private UserInfo GetUserInfo (string userID, bool checkForOfflineStatus)
        {
            bool changed = false;
            UserInfo info = m_agentInfoConnector.Get (userID, checkForOfflineStatus, out changed);
            if (changed)
                if (!m_lockedUsers.Contains (userID))
                    m_registry.RequestModuleInterface<ISimulationBase> ().EventManager.FireGenericEventHandler ("UserStatusChange", new object[3] { userID, false, UUID.Zero });
            return info;
        }

        public virtual UserInfo[] GetUserInfos (string[] userIDs)
        {
            UserInfo[] infos = new UserInfo[userIDs.Length];
            for (int i = 0; i < userIDs.Length; i++)
            {
                infos[i] = GetUserInfo(userIDs[i]);
            }
            return infos;
        }

        public virtual string[] GetAgentsLocations (string requestor, string[] userIDs)
        {
            string[] infos = new string[userIDs.Length];
            for (int i = 0; i < userIDs.Length; i++)
            {
                UserInfo user = GetUserInfo(userIDs[i]);
                if (user != null && user.IsOnline)
                    infos[i] = m_registry.RequestModuleInterface<IGridService>().GetRegionByUUID(UUID.Zero, user.CurrentRegionID).ServerURI;
                else
                    infos[i] = "NotOnline";
            }
            return infos;
        }

        public virtual bool SetHomePosition (string userID, UUID homeID, Vector3 homePosition, Vector3 homeLookAt)
        {
            m_agentInfoConnector.SetHomePosition(userID, homeID, homePosition, homeLookAt);
            return true;
        }

        public virtual void SetLastPosition (string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            m_agentInfoConnector.SetLastPosition(userID, regionID, lastPosition, lastLookAt);
        }

        public virtual void LockLoggedInStatus (string userID, bool locked)
        {
            if(locked && !m_lockedUsers.Contains(userID))
                m_lockedUsers.Add(userID);
            else
                m_lockedUsers.Remove(userID);
        }

        public virtual void SetLoggedIn (string userID, bool loggingIn, bool fireLoggedInEvent, UUID enteringRegion)
        {
            UserInfo userInfo = GetUserInfo (userID, false);//We are changing the status, so don't look
            if (userInfo == null)
            {
                Save (new UserInfo ()
                {
                    IsOnline = loggingIn,
                    UserID = userID,
                    CurrentLookAt = Vector3.Zero,
                    CurrentPosition = Vector3.Zero,
                    CurrentRegionID = enteringRegion,
                    HomeLookAt = Vector3.Zero,
                    HomePosition = Vector3.Zero,
                    HomeRegionID = UUID.Zero,
                    Info = new OpenMetaverse.StructuredData.OSDMap (),
                    LastLogin = DateTime.Now,
                    LastLogout = DateTime.Now
                });
            }
            if (m_lockedUsers.Contains (userID))
                return; //User is locked, leave them alone
            if(loggingIn)
                if(enteringRegion == UUID.Zero)
                    m_agentInfoConnector.Update (userID, new string[3] { "IsOnline", "LastLogin", "LastSeen" }, new object[3] { loggingIn ? 1 : 0, Util.ToUnixTime (DateTime.Now), Util.ToUnixTime (DateTime.Now) });
                else
                    m_agentInfoConnector.Update (userID, new string[4] { "IsOnline", "LastLogin", "CurrentRegionID", "LastSeen" }, new object[4] { loggingIn ? 1 : 0, Util.ToUnixTime (DateTime.Now), enteringRegion, Util.ToUnixTime (DateTime.Now) });
            else
                m_agentInfoConnector.Update (userID, new string[3] { "IsOnline", "LastLogout", "LastSeen" }, new object[3] { loggingIn ? 1 : 0, Util.ToUnixTime (DateTime.Now), Util.ToUnixTime (DateTime.Now) });

            if (fireLoggedInEvent)
            {
                //Trigger an event so listeners know
                m_registry.RequestModuleInterface<ISimulationBase> ().EventManager.FireGenericEventHandler ("UserStatusChange", new object[3] { userID, loggingIn, enteringRegion });
            }
        }

        public virtual void Save (UserInfo userInfo)
        {
            m_agentInfoConnector.Set(userInfo);
        }

        #endregion
    }
}
