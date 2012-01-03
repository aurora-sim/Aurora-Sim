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
using System.Linq;
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.Connectors
{
    public class UserAccountServicesConnector : IUserAccountService, IService
    {
        private readonly UserAccountCache m_cache = new UserAccountCache();
        private IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IService Members

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

        #region IUserAccountService Members

        public virtual IUserAccountService InnerService
        {
            get { return this; }
        }

        public virtual UserAccount GetUserAccount(UUID scopeID, string firstName, string lastName)
        {
            UserAccount account;
            if (m_cache.Get(firstName + " " + lastName, out account))
                return account;

            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getaccount";

            sendData["ScopeID"] = scopeID;
            sendData["FirstName"] = firstName;
            sendData["LastName"] = lastName;

            account = SendAndGetReply(UUID.Zero, sendData);
            if (account != null)
                m_cache.Cache(account.PrincipalID, account);
            return account;
        }

        public virtual UserAccount GetUserAccount(UUID scopeID, string name)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getaccount";

            sendData["ScopeID"] = scopeID;
            sendData["Name"] = name;

            //Leave these for compatibility with OpenSim!!!
            string[] names = name.Split(' ');
            sendData["FirstName"] = names[0];
            if (names.Length >= 2)
            {
                //Join all the names together
                string lastName = string.Join(" ", names, 1, names.Length - 1);
                sendData["LastName"] = lastName;
            }
            else
                sendData["LastName"] = ""; //No last name then

            return SendAndGetReply(UUID.Zero, sendData);
        }

        public virtual UserAccount GetUserAccount(UUID scopeID, UUID userID)
        {
            UserAccount account;
            if (m_cache.Get(userID, out account))
                return account;

            //MainConsole.Instance.DebugFormat("[ACCOUNTS CONNECTOR]: GetUserAccount {0}", userID);
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getaccount";

            sendData["ScopeID"] = scopeID;
            sendData["UserID"] = userID.ToString();

            account = SendAndGetReply(userID, sendData);
            m_cache.Cache(userID, account);
            return account;
        }

        public List<UserAccount> GetUserAccounts(UUID scopeID, string query)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getaccounts";

            sendData["ScopeID"] = scopeID.ToString();
            sendData["query"] = query;

            string reply = string.Empty;
            string reqString = WebUtils.BuildQueryString(sendData);
            // MainConsole.Instance.DebugFormat("[ACCOUNTS CONNECTOR]: queryString = {0}", reqString);
            List<UserAccount> accounts = new List<UserAccount>();

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("UserAccountServerURI");
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
                                m_cache.Cache(pinfo.PrincipalID, pinfo);
                                pinfo.GenericData["GridURL"] = m_ServerURI.Remove(m_ServerURI.LastIndexOf('/'));
                                accounts.Add(pinfo);
                            }
                            else
                                MainConsole.Instance.DebugFormat(
                                    "[ACCOUNT CONNECTOR]: GetUserAccounts received invalid response type {0}",
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

        public virtual void CreateUser(string name, string password, string email)
        {
        }

        public virtual void CreateUser(UUID userID, string name, string password, string email)
        {
        }

        public virtual bool StoreUserAccount(UserAccount data)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
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

        #endregion

        private UserAccount SendAndGetReply(UUID avatarID, Dictionary<string, object> sendData)
        {
            string reply = string.Empty;
            string reqString = WebUtils.BuildQueryString(sendData);
            // MainConsole.Instance.DebugFormat("[ACCOUNTS CONNECTOR]: queryString = {0}", reqString);
            UserAccount account = null;
            List<string> m_ServerURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(avatarID.ToString(),
                                                                                       "UserAccountServerURI", true);
            foreach (string m_ServerURI in m_ServerURIs)
            {
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      reqString);
                    if (reply == string.Empty)
                        continue;

                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if ((replyData != null) && replyData.ContainsKey("result") && (replyData["result"] != null))
                    {
                        if (replyData["result"] is Dictionary<string, object>)
                        {
                            account = new UserAccount();
                            account.FromKVP((Dictionary<string, object>)replyData["result"]);
                            account.GenericData["GridURL"] = m_ServerURI.Remove(m_ServerURI.LastIndexOf('/'));
                            return account;
                        }
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.InfoFormat("[ACCOUNT CONNECTOR]: Exception when contacting user account server: {0}",
                                     e.Message);
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
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(avatarID.ToString(),
                                                                                           "UserAccountServerURI");
                foreach (string mServerUri in m_ServerURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri + "/accounts", reqString);
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
    }
}