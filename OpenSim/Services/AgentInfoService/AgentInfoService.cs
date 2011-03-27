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
using OpenSim.Data;
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

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AgentInfoHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IAgentInfoService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
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

        public UserInfo GetUserInfo(string userID)
        {
            return m_agentInfoConnector.Get(userID);
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
            m_agentInfoConnector.SetHomePosition(userID, homeID, homePosition, homeLookAt);
            return true;
        }

        public void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            m_agentInfoConnector.SetLastPosition(userID, regionID, lastPosition, lastLookAt);
        }

        public void SetLoggedIn(string userID, bool loggingIn, bool fireLoggedInEvent)
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

            if (fireLoggedInEvent)
            {
                //Trigger an event so listeners know
                m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("UserStatusChange", userInfo);
            }
        }

        public void Save(UserInfo userInfo)
        {
            m_agentInfoConnector.Set(userInfo);
        }

        #endregion
    }
}
