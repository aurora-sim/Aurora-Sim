using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Services.Connectors;
using OpenSim.Services.UserAccountService;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules 
{
    public class IWCUserAccountConnector : IUserAccountService, IService
    {
        protected UserAccountService m_localService;
        protected UserAccountServicesConnector m_remoteService;
        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public IUserAccountService InnerService
        {
            get { return m_localService; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("UserAccountHandler", "") != Name)
                return;

            m_localService = new UserAccountService();
            m_localService.Configure(config, registry);
            m_remoteService = new UserAccountServicesConnector();
            m_remoteService.Initialize(config, registry);
            registry.RegisterModuleInterface<IUserAccountService>(this);
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

        public UserAccount GetUserAccount(UUID scopeID, UUID userID)
        {
            UserAccount account = m_localService.GetUserAccount(scopeID, userID);
            if (account == null)
                account = m_remoteService.GetUserAccount(scopeID, userID);
            return account;
        }

        public UserAccount GetUserAccount(UUID scopeID, string FirstName, string LastName)
        {
            UserAccount account = m_localService.GetUserAccount(scopeID, FirstName, LastName);
            if (account == null)
                account = m_remoteService.GetUserAccount(scopeID, FirstName, LastName);
            return account;
        }

        public UserAccount GetUserAccount(UUID scopeID, string Name)
        {
            UserAccount account = m_localService.GetUserAccount(scopeID, Name);
            if (account == null)
                account = m_remoteService.GetUserAccount(scopeID, Name);
            return account;
        }

        public List<UserAccount> GetUserAccounts(UUID scopeID, string query)
        {
            List<UserAccount> accounts = m_localService.GetUserAccounts(scopeID, query);
            accounts.AddRange(m_remoteService.GetUserAccounts(scopeID, query));
            return accounts;
        }

        public bool StoreUserAccount(UserAccount data)
        {
            return m_localService.StoreUserAccount(data);
        }

        public void CreateUser(string name, string md5password, string email)
        {
            m_localService.CreateUser(name, md5password, email);
        }

        #endregion
    }
}
