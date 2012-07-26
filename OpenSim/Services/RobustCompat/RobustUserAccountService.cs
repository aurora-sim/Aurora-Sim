using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using OpenSim.Services.Interfaces;
using Nini.Config;
using Aurora.Simulation.Base;
using Aurora.Framework.Servers.HttpServer;
using OpenMetaverse;

namespace OpenSim.Services.Robust
{
    public class UserAccountServicesConnector : IUserAccountService, IService
    {
        private IRegistryCore m_registry;

        public virtual IUserAccountService InnerService
        {
            get { return this; }
        }

        public virtual UserAccount GetUserAccount(List<UUID> scopeIDs, string firstName, string lastName)
        {
            UserAccount account;

            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = 0.ToString();
            sendData["VERSIONMAX"] = 0.ToString();
            sendData["METHOD"] = "getaccount";

            sendData["ScopeID"] = GetScopeID(scopeIDs);
            sendData["FirstName"] = firstName.ToString();
            sendData["LastName"] = lastName.ToString();

            account = SendAndGetReply(UUID.Zero, sendData);
            return account;
        }

        public virtual UserAccount GetUserAccount(List<UUID> scopeIDs, string name)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = 0.ToString();
            sendData["VERSIONMAX"] = 0.ToString();
            sendData["METHOD"] = "getaccount";

            sendData["ScopeID"] = GetScopeID(scopeIDs);
            sendData["Name"] = name;

            //Leave these for compatibility with OpenSim!!!
            string[] names = name.Split(' ');
            sendData["FirstName"] = names[0];
            if(names.Length >= 2)
            {
                //Join all the names together
                string lastName= string.Join(" ", names, 1, names.Length - 1);
                sendData["LastName"] = lastName.ToString();
            }
            else
                sendData["LastName"] = "";//No last name then

            return SendAndGetReply(UUID.Zero, sendData);
        }

        public virtual UserAccount GetUserAccount(List<UUID> scopeIDs, UUID userID)
        {
            UserAccount account;

            //MainConsole.Instance.DebugFormat("[ACCOUNTS CONNECTOR]: GetUserAccount {0}", userID);
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = 0.ToString();
            sendData["VERSIONMAX"] = 0.ToString();
            sendData["METHOD"] = "getaccount";

            sendData["ScopeID"] = GetScopeID(scopeIDs);
            sendData["UserID"] = userID.ToString();

            account = SendAndGetReply(userID, sendData);
            return account;
        }
        public List<UserAccount> GetUserAccounts(List<UUID> scopeIDs, string query, uint? start, uint? count)
        {
            return GetUserAccounts(scopeIDs, query);
        }

        public List<UserAccount> GetUserAccounts(List<UUID> scopeIDs, int level, int flags)
        {
            return new List<UserAccount>();
        }

        private string GetScopeID(List<UUID> scopeIDs)
        {
            if (scopeIDs == null || scopeIDs.Count == 0)
                return UUID.Zero.ToString();
            return scopeIDs[0].ToString();
        }

        public List<UserAccount> GetUserAccounts(List<UUID> scopeIDs, string query)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = 0.ToString();
            sendData["VERSIONMAX"] = 0.ToString();
            sendData["METHOD"] = "getaccounts";

            sendData["ScopeID"] = GetScopeID(scopeIDs);
            sendData["query"] = query;

            string reply = string.Empty;
            string reqString = WebUtils.BuildQueryString(sendData);
            // MainConsole.Instance.DebugFormat("[ACCOUNTS CONNECTOR]: queryString = {0}", reqString);
            List<UserAccount> accounts = new List<UserAccount>();

            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("UserAccountServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI,
                        reqString);
                    if (reply == null || (reply != null && reply == string.Empty))
                        continue;

                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        if (replyData.ContainsKey("result") && replyData.ContainsKey("result").ToString() == "null")
                            continue;

                        Dictionary<string, object>.ValueCollection accountList = replyData.Values;
                        //MainConsole.Instance.DebugFormat("[ACCOUNTS CONNECTOR]: GetAgents returned {0} elements", pinfosList.Count);
                        foreach (object acc in accountList)
                        {
                            if (acc is Dictionary<string, object>)
                            {
                                UserAccount pinfo = new UserAccount();
                                pinfo.FromKVP((Dictionary<string, object>)acc);
                                pinfo.GenericData["GridURL"] = m_ServerURI.Remove(m_ServerURI.LastIndexOf('/'));
                                accounts.Add(pinfo);
                            }
                            else
                                MainConsole.Instance.DebugFormat("[ACCOUNT CONNECTOR]: GetUserAccounts received invalid response type {0}",
                                    acc.GetType());
                        }
                    }
                    else
                        MainConsole.Instance.DebugFormat("[ACCOUNTS CONNECTOR]: GetUserAccounts received null response");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.InfoFormat("[ACCOUNT CONNECTOR]: Exception when contacting accounts server: {0}", e.Message);
            }

            return accounts;
        }

        public virtual void CreateUser (string name, string password, string email)
        {
        }

        public virtual void CreateUser (UUID userID, string name, string password, string email)
        {
        }

        public virtual bool StoreUserAccount(UserAccount data)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = 0.ToString();
            sendData["VERSIONMAX"] = 0.ToString();
            sendData["METHOD"] = "setaccount";

            Dictionary<string, object> structData = data.ToKVP();

            foreach (KeyValuePair<string, object> kvp in structData)
            {
                if (kvp.Value == null)
                {
                    sendData[kvp.Key] = "";
                }
                else
                    sendData[kvp.Key] = kvp.Value.ToString();
            }

            return SendAndGetBoolReply(data.PrincipalID, sendData);
        }

        private UserAccount SendAndGetReply(UUID avatarID, Dictionary<string, object> sendData)
        {
            string reply = string.Empty;
            string reqString = WebUtils.BuildQueryString(sendData);
            // MainConsole.Instance.DebugFormat("[ACCOUNTS CONNECTOR]: queryString = {0}", reqString);
            UserAccount account = null;
            List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(avatarID.ToString(), "UserAccountServerURI", true);
            foreach (string m_ServerURI in m_ServerURIs)
            {
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest ("POST",
                        m_ServerURI,
                        reqString);
                    if (reply == string.Empty)
                        continue;

                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse (reply);

                    if ((replyData != null) && replyData.ContainsKey ("result") && (replyData["result"] != null))
                    {
                        if (replyData["result"] is Dictionary<string, object>)
                        {
                            account = new UserAccount ();
                            account.FromKVP((Dictionary<string, object>)replyData["result"]);
                            account.GenericData["GridURL"] = m_ServerURI.Remove (m_ServerURI.LastIndexOf ('/'));
                            return account;
                        }
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.InfoFormat ("[ACCOUNT CONNECTOR]: Exception when contacting user account server: {0}", e.Message);
                }
            }

            return account;

        }

        private bool SendAndGetBoolReply(UUID avatarID, Dictionary<string, object> sendData)
        {
            string reqString = WebUtils.BuildQueryString(sendData);
            // MainConsole.Instance.DebugFormat("[ACCOUNTS CONNECTOR]: queryString = {0}", reqString);
            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(avatarID.ToString(), "UserAccountServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                            m_ServerURI + "/accounts",
                            reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData.ContainsKey("result"))
                        {
                            if (replyData["result"].ToString().ToLower() == "success")
                                return true;
                        }
                        else
                            MainConsole.Instance.DebugFormat("[ACCOUNTS CONNECTOR]: Set or Create UserAccount reply data does not contain result field");

                    }
                    else
                        MainConsole.Instance.DebugFormat("[ACCOUNTS CONNECTOR]: Set or Create UserAccount received empty reply");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[ACCOUNTS CONNECTOR]: Exception when contacting user account server: {0}", e.Message);
            }

            return false;
        }

        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("UserAccountHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IUserAccountService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion

        public uint NumberOfUserAccounts(List<UUID> scopeIDs, string query)
        {
            return 0;
        }

        public void CacheAccount(UserAccount account)
        {
        }

        public string CreateUser(UUID userID, UUID scopeID, string name, string md5password, string email)
        {
            return "";
        }

        public void DeleteUser(UUID userID, string password, bool archiveInformation, bool wipeFromDatabase)
        {
        }
    }
}
