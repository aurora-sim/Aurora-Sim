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

using Aurora.Framework;
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using System.Collections.Generic;
using FriendInfo = Aurora.Framework.Services.FriendInfo;

namespace Aurora.Services
{
    public class FriendsService : ConnectorBase, IFriendsService, IService
    {
        #region Declares

        protected IFriendsData m_Database;

        #endregion

        #region IService Members

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("FriendsHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IFriendsService>(this);
            m_registry = registry;
            Init(registry, Name);
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
            m_Database = Framework.Utilities.DataManager.RequestPlugin<IFriendsData>();
        }

        public virtual void FinishedStartup()
        {
        }

        #endregion

        #region IFriendsService Members

        public virtual IFriendsService InnerService
        {
            get { return this; }
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual List<FriendInfo> GetFriends(UUID PrincipalID)
        {
            object remoteValue = DoRemote(PrincipalID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<FriendInfo>) remoteValue;

            return new List<FriendInfo>(m_Database.GetFriends(PrincipalID));
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual List<FriendInfo> GetFriendsRequest(UUID PrincipalID)
        {
            object remoteValue = DoRemote(PrincipalID);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<FriendInfo>) remoteValue;

            return new List<FriendInfo>(m_Database.GetFriendsRequest(PrincipalID));
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual bool StoreFriend(UUID PrincipalID, string friend, int flags)
        {
            object remoteValue = DoRemote(PrincipalID, friend, flags);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool) remoteValue;

            return m_Database.Store(PrincipalID, friend, flags, 0);
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public virtual bool Delete(UUID PrincipalID, string friend)
        {
            object remoteValue = DoRemote(PrincipalID, friend);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool) remoteValue;

            return m_Database.Delete(PrincipalID, friend);
        }

        #endregion
    }
}