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

using OpenMetaverse;
using OpenSim.Framework;
using System;
using System.Collections.Generic;
using OpenSim.Services.Interfaces;
using OpenSim.Data;
using Nini.Config;
using log4net;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;
using Aurora.Framework;
using Aurora.Simulation.Base;

namespace OpenSim.Services.Friends
{
    public class FriendsService : IFriendsService, IService
    {
        protected IFriendsData m_Database = null;

        public string Name
        {
            get { return GetType().Name; }
        }

        public virtual IFriendsService InnerService
        {
            get { return this; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("FriendsHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IFriendsService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_Database = Aurora.DataManager.DataManager.RequestPlugin<IFriendsData> ();
        }

        public void FinishedStartup()
        {
        }

        public FriendInfo[] GetFriends(UUID PrincipalID)
        {
            return m_Database.GetFriends(PrincipalID);
        }

        public bool StoreFriend(UUID PrincipalID, string Friend, int flags)
        {
            return m_Database.Store(PrincipalID, Friend, flags, 0);
        }

        public bool Delete(UUID PrincipalID, string Friend)
        {
            return m_Database.Delete(PrincipalID, Friend);
        }

    }
}
