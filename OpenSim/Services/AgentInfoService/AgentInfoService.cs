/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using OpenSim.Framework.Console;
using OpenSim.Data;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Simulation.Base;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.PresenceService
{
    public class AgentInfoService : IService, IAgentInfoService
    {
        #region Declares

        protected IGenericsConnector m_genericsConnector;
        protected IRegistryCore m_registry;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterModuleInterface<IAgentInfoService>(this);
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_genericsConnector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region IAgentInfoService Members

        public UserInfo GetUserInfo(string userID)
        {
            return m_genericsConnector.GetGeneric<UserInfo>(UUID.Parse(userID), "UserInfo", userID, new UserInfo());
        }

        public UserInfo[] GetUserInfos(string[] userIDs)
        {
            UserInfo[] infos = new UserInfo[userIDs.Length];
            for (int i = 0; i < userIDs.Length; i++)
            {
                infos[i] = GetUserInfo(userIDs[i]);
            }
            return infos;
        }

        public string[] GetAgentsLocations(string[] userIDs)
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

        public bool SetHomePosition(string userID, UUID homeID, Vector3 homePosition, Vector3 homeLookAt)
        {
            UserInfo userInfo = GetUserInfo(userID);
            if (userInfo != null)
            {
                userInfo.HomeRegionID = homeID;
                userInfo.HomePosition = homePosition;
                userInfo.HomeLookAt = homeLookAt;
                Save(userInfo);
                return true;
            }
            return false;
        }

        public void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            UserInfo userInfo = GetUserInfo(userID);
            if (userInfo != null)
            {
                userInfo.CurrentRegionID = regionID;
                userInfo.CurrentPosition = lastPosition;
                userInfo.CurrentLookAt = lastLookAt;
                Save(userInfo);
            }
        }

        public void SetLoggedIn(string userID, bool loggingIn)
        {
            UserInfo userInfo = GetUserInfo(userID);
            if (userInfo == null)
            {
                userInfo = new UserInfo();
                userInfo.UserID = userID;
            }
            userInfo.IsOnline = loggingIn;
            if (loggingIn)
                userInfo.LastLogin = DateTime.Now;
            else
                userInfo.LastLogout = DateTime.Now;
            Save(userInfo);

            //Trigger an event so listeners know
            m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("UserStatusChange", userInfo);
        }

        public void Save(UserInfo userInfo)
        {
            m_genericsConnector.AddGeneric(UUID.Parse(userInfo.UserID), "UserInfo", userInfo.UserID, userInfo.ToOSD());
        }

        #endregion
    }
}
