using System;
using System.Collections.Generic;
using Nini.Config;
using log4net;
using System.Reflection;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Region.CoreModules.ServiceConnectorsOut;
using OpenSim.Region.CoreModules.ServiceConnectorsOut.UserAccounts;

namespace Aurora.Modules.Communications.MultipleGrids
{
    public class MultipleUserAccountServicesConnector : ISharedRegionModule, IUserAccountService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Enabled = false;
        private List<IUserAccountService> AllServices = new List<IUserAccountService>();

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "MultipleUserAccountServicesConnector"; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("UserAccountServices", "");
                if (name == Name)
                {
                    IConfig multipleConfig = source.Configs["MultipleGridsModule"];
                    if (multipleConfig != null)
                    {
                        IConfig UAS = source.Configs["UserAccountService"];
                        if (UAS != null)
                        {
                            string[] Grids = multipleConfig.GetString("UserAccountServerURIs", "").Split(',');
                            //Set it so that it works for them
                            moduleConfig.Set("UserAccountServices", "RemoteUserAccountServicesConnector");
                            foreach (string gridURL in Grids)
                            {
                                //Set their gridURL
                                UAS.Set("UserAccountServerURI", gridURL);
                                //Start it up
                                RemoteUserAccountServicesConnector connector = new RemoteUserAccountServicesConnector();
                                connector.Initialise(source);
                                AllServices.Add(connector);
                                m_log.Info("[USER CONNECTOR]: Multiple grid users enabled for " + gridURL);
                            }
                        }
                    }
                    //Reset the name
                    moduleConfig.Set("UserAccountServices", Name);
                    m_Enabled = true;
                }
            }
        }

        public void PostInitialise()
        {
            if (!m_Enabled)
                return;
        }

        public void Close()
        {
            if (!m_Enabled)
                return;
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            scene.RegisterModuleInterface<IUserAccountService>(this);
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;
        }

        #region IUserAccountService

        public UserAccount GetUserAccount(UUID scopeID, UUID userID)
        {
            UserAccount account = null;
            foreach (IUserAccountService service in AllServices)
            {
                account = service.GetUserAccount(scopeID, userID);
                if (account != null)
                    return account;
            }
            return account;
        }

        public UserAccount GetUserAccount(UUID scopeID, string firstName, string lastName)
        {
            UserAccount account = null;
            foreach (IUserAccountService service in AllServices)
            {
                account = service.GetUserAccount(scopeID, firstName, lastName);
                if (account != null)
                    return account;
            }
            return account;
        }

        public UserAccount GetUserAccount(UUID scopeID, string Email)
        {
            UserAccount account = null;
            foreach (IUserAccountService service in AllServices)
            {
                account = service.GetUserAccount(scopeID, Email);
                if (account != null)
                    return account;
            }
            return account;
        }

        public List<UserAccount> GetUserAccounts(UUID scopeID, string query)
        {
            List<UserAccount> accounts = null;
            foreach (IUserAccountService service in AllServices)
            {
                //Take in all possible
                accounts.AddRange(service.GetUserAccounts(scopeID, query));
            }
            return accounts;
        }

        public bool StoreUserAccount(UserAccount data)
        {
            // This remote connector refuses to serve this method
            return false;
        }

        #endregion
    }
}
