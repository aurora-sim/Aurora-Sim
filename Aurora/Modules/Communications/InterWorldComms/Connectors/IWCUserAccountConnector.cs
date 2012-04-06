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
using System.Linq;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Services.Connectors;
using OpenSim.Services.Interfaces;
using OpenSim.Services.UserAccountService;

namespace Aurora.Modules
{
    public class IWCUserAccountConnector : ConnectorBase, IUserAccountService, IService
    {
        protected UserAccountService m_localService;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("UserAccountHandler", "") != Name)
                return;

            m_localService = new UserAccountService();
            m_localService.Configure(config, registry);
            Init(registry, Name);
            registry.RegisterModuleInterface<IUserAccountService>(this);
            m_registry = registry;
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

        #region IUserAccountService Members

        public IUserAccountService InnerService
        {
            get
            {
                //If we are getting URls for an IWC connection, we don't want to be calling other things, as they are calling us about only our info
                //If we arn't, its ar region we are serving, so give it everything we know
                if (m_registry.RequestModuleInterface<InterWorldCommunications>().IsGettingUrlsForIWCConnection)
                    return m_localService;
                else
                    return this;
            }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low, RenamedMethod = "GetUserAccountUUID")]
        public UserAccount GetUserAccount(UUID scopeID, UUID principalID)
        {
            UserAccount account = m_localService.GetUserAccount(scopeID, principalID);
            if (account == null)
                account = FixRemoteAccount((UserAccount)DoRemoteForced(scopeID, principalID));
            return account;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public UserAccount GetUserAccount(UUID scopeID, string FirstName, string LastName)
        {
            UserAccount account = m_localService.GetUserAccount(scopeID, FirstName, LastName);
            if (account == null)
                account = FixRemoteAccount((UserAccount)DoRemoteForced(scopeID, FirstName, LastName));
            return account;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public UserAccount GetUserAccount(UUID scopeID, string name)
        {
            UserAccount account = m_localService.GetUserAccount(scopeID, name);
            if (account == null)
                account = FixRemoteAccount((UserAccount)DoRemoteForced(scopeID, name));
            return account;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<UserAccount> GetUserAccounts(UUID scopeID, string query)
        {
            return GetUserAccounts(scopeID, query, null, null);
        }

        public List<UserAccount> GetUserAccounts(UUID scopeID, string query, uint? start, uint? count)
        {
            List<UserAccount> accounts = m_localService.GetUserAccounts(scopeID, query);
            accounts.AddRange(FixRemoteAccounts((List<UserAccount>)DoRemoteForced(scopeID, query, start, count)));
            return accounts;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public uint NumberOfUserAccounts(UUID scopeID, string query)
        {
            return m_localService.NumberOfUserAccounts(scopeID, query);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public bool StoreUserAccount(UserAccount data)
        {
            return m_localService.StoreUserAccount(data);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public void CreateUser(string name, string md5password, string email)
        {
            m_localService.CreateUser(name, md5password, email);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public void CreateUser(UUID userID, UUID scopeID, string name, string md5password, string email)
        {
            m_localService.CreateUser(userID, scopeID, name, md5password, email);
        }

        public void CacheAccount(UserAccount account)
        {
            m_localService.CacheAccount(account);
        }

        #endregion

        private IEnumerable<UserAccount> FixRemoteAccounts (List<UserAccount> list)
        {
            List<UserAccount> accounts = new List<UserAccount> ();
            foreach (UserAccount account in list)
            {
                accounts.Add (FixRemoteAccount (account));
            }
            return accounts;
        }

        private UserAccount FixRemoteAccount(UserAccount userAccount)
        {
            if (userAccount == null)
                return userAccount;
            if (userAccount.Name.Contains("@"))
                return userAccount; //If it already has this added, don't mess with it
            userAccount.Name = userAccount.FirstName + " " + userAccount.LastName + "@" +
                               userAccount.GenericData["GridURL"];
            return userAccount;
        }


        public void DeleteUser(UUID userID, string password, bool archiveInformation, bool wipeFromDatabase)
        {
        }
    }
}