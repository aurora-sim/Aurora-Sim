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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Services.Connectors;
using OpenSim.Services;
using OpenSim.Services.Friends;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;

namespace Aurora.Modules 
{
    public class IWCFriendsConnector : IFriendsService, IService
    {
        protected FriendsService m_localService;
        protected FriendsServicesConnector m_remoteService;
        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public IFriendsService InnerService
        {
            get { return m_localService; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString ("FriendsHandler", "") != Name)
                return;

            m_localService = new FriendsService ();
            m_localService.Initialize(config, registry);
            m_remoteService = new FriendsServicesConnector ();
            m_remoteService.Initialize(config, registry);
            registry.RegisterModuleInterface<IFriendsService> (this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_localService != null)
                m_localService.Start(config, registry);
        }

        public void FinishedStartup()
        {
            if (m_localService != null)
                m_localService.FinishedStartup();
        }

        #endregion

        #region IFriendsService Members

        public FriendInfo[] GetFriends (UUID PrincipalID)
        {
            FriendInfo[] friends = m_localService.GetFriends (PrincipalID);
            if (friends == null || friends.Length == 0)
                friends = m_remoteService.GetFriends (PrincipalID);
            return friends;
        }

        public bool StoreFriend (UUID PrincipalID, string Friend, int flags)
        {
            bool success = m_localService.StoreFriend (PrincipalID, Friend, flags);
            if (!success)
                success = m_remoteService.StoreFriend (PrincipalID, Friend, flags);
            return success;
        }

        public bool Delete (UUID PrincipalID, string Friend)
        {
            bool success = m_localService.Delete (PrincipalID, Friend);
            if (!success)
                success = m_remoteService.Delete (PrincipalID, Friend);
            return success;
        }

        #endregion
    }
}
