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

using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenMetaverse;

namespace OpenSim.Services.Connectors
{
    public class UserAccountServicesConnector : IUserAccountService, IService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
        private UserAccountCache m_cache = new UserAccountCache();

        private string m_ServerURI = String.Empty;

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
            sendData["FirstName"] = firstName.ToString();
            sendData["LastName"] = lastName.ToString();

            account = SendAndGetReply(sendData);
            if(account != null)
                m_cache.Cache(account.PrincipalID, account);
            return account;
        }

        public virtual UserAccount GetUserAccount(UUID scopeID, string email)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getaccount";

            sendData["ScopeID"] = scopeID;
            sendData["Email"] = email;

            return SendAndGetReply(sendData);
        }

        public virtual UserAccount GetUserAccount(UUID scopeID, UUID userID)
        {
            UserAccount account;
            if (m_cache.Get(userID, out account))
                return account;

            //m_log.DebugFormat("[ACCOUNTS CONNECTOR]: GetUserAccount {0}", userID);
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getaccount";

            sendData["ScopeID"] = scopeID;
            sendData["UserID"] = userID.ToString();

            account = SendAndGetReply(sendData);
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
            // m_log.DebugFormat("[ACCOUNTS CONNECTOR]: queryString = {0}", reqString);
            try
            {
                reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/accounts",
                        reqString);
                if (reply == null || (reply != null && reply == string.Empty))
                {
                    m_log.DebugFormat("[ACCOUNT CONNECTOR]: GetUserAccounts received null or empty reply");
                    return null;
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[ACCOUNT CONNECTOR]: Exception when contacting accounts server: {0}", e.Message);
            }

            List<UserAccount> accounts = new List<UserAccount>();

            Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

            if (replyData != null)
            {
                if (replyData.ContainsKey("result") && replyData.ContainsKey("result").ToString() == "null")
                {
                    return accounts;
                }

                Dictionary<string, object>.ValueCollection accountList = replyData.Values;
                //m_log.DebugFormat("[ACCOUNTS CONNECTOR]: GetAgents returned {0} elements", pinfosList.Count);
                foreach (object acc in accountList)
                {
                    if (acc is Dictionary<string, object>)
                    {
                        UserAccount pinfo = new UserAccount((Dictionary<string, object>)acc);
                        m_cache.Cache(pinfo.PrincipalID, pinfo);
                        accounts.Add(pinfo);
                    }
                    else
                        m_log.DebugFormat("[ACCOUNT CONNECTOR]: GetUserAccounts received invalid response type {0}",
                            acc.GetType());
                }
            }
            else
                m_log.DebugFormat("[ACCOUNTS CONNECTOR]: GetUserAccounts received null response");

            return accounts;
        }

        public virtual bool StoreUserAccount(UserAccount data)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "setaccount";

            Dictionary<string, object> structData = data.ToKeyValuePairs();

            foreach (KeyValuePair<string, object> kvp in structData)
            {
                if (kvp.Value == null)
                {
                    sendData[kvp.Key] = "";
                }
                else
                    sendData[kvp.Key] = kvp.Value.ToString();
            }

            return SendAndGetBoolReply(sendData);
        }

        private UserAccount SendAndGetReply(Dictionary<string, object> sendData)
        {
            string reply = string.Empty;
            string reqString = WebUtils.BuildQueryString(sendData);
            // m_log.DebugFormat("[ACCOUNTS CONNECTOR]: queryString = {0}", reqString);
            try
            {
                reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/accounts",
                        reqString);
                if (reply == null || (reply != null && reply == string.Empty))
                {
                    m_log.DebugFormat("[ACCOUNT CONNECTOR]: GetUserAccount received null or empty reply");
                    return null;
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[ACCOUNT CONNECTOR]: Exception when contacting user account server: {0}", e.Message);
            }

            Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
            UserAccount account = null;

            if ((replyData != null) && replyData.ContainsKey("result") && (replyData["result"] != null))
            {
                if (replyData["result"] is Dictionary<string, object>)
                {
                    account = new UserAccount((Dictionary<string, object>)replyData["result"]);
                }
            }

            return account;

        }

        private bool SendAndGetBoolReply(Dictionary<string, object> sendData)
        {
            string reqString = WebUtils.BuildQueryString(sendData);
            // m_log.DebugFormat("[ACCOUNTS CONNECTOR]: queryString = {0}", reqString);
            try
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
                        else
                            return false;
                    }
                    else
                        m_log.DebugFormat("[ACCOUNTS CONNECTOR]: Set or Create UserAccount reply data does not contain result field");

                }
                else
                    m_log.DebugFormat("[ACCOUNTS CONNECTOR]: Set or Create UserAccount received empty reply");
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[ACCOUNTS CONNECTOR]: Exception when contacting user account server: {0}", e.Message);
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
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("UserAccountHandler", "") != Name)
                return;

            registry.RegisterInterface<IUserAccountService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostStart(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("UserAccountHandler", "") != Name)
                return;

            string serviceURI = registry.Get<IAutoConfigurationService>().FindValueOf("UserAccountServerURI",
                        "UserAccountService");

            if (serviceURI == String.Empty)
            {
                m_log.Error("[ACCOUNT CONNECTOR]: No Server URI named in section UserAccountService");
                throw new Exception("User account connector init error");
            }
            m_ServerURI = serviceURI;
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("UserAccountHandler", "") != Name)
                return;

            registry.RegisterInterface<IUserAccountService>(this);
        }

        #endregion
    }
}
