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
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.Connectors.SimianGrid
{
    /// <summary>
    ///   Connects user account data (creating new users, looking up existing 
    ///   users) to the SimianGrid backend
    /// </summary>
    public class SimianUserAccountServiceConnector : IUserAccountService, IService
    {
        private const double CACHE_EXPIRATION_SECONDS = 120.0;

        private readonly ExpiringCache<UUID, UserAccount> m_accountCache = new ExpiringCache<UUID, UserAccount>();
        private string m_serverUrl = String.Empty;

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

            CommonInit(config);
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

        public UserAccount GetUserAccount(UUID scopeID, string firstName, string lastName)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetUser"},
                                                      {"Name", firstName + ' ' + lastName}
                                                  };

            return GetUser(requestArgs);
        }

        public UserAccount GetUserAccount(UUID scopeID, string name)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetUser"},
                                                      {"Name", name}
                                                  };

            return GetUser(requestArgs);
        }

        public UserAccount GetUserAccount(UUID scopeID, UUID userID)
        {
            // Cache check
            UserAccount account;
            if (m_accountCache.TryGetValue(userID, out account))
                return account;

            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetUser"},
                                                      {"UserID", userID.ToString()}
                                                  };

            account = GetUser(requestArgs);

            if (account == null)
            {
                // Store null responses too, to avoid repeated lookups for missing accounts
                m_accountCache.AddOrUpdate(userID, null, CACHE_EXPIRATION_SECONDS);
            }

            return account;
        }

        public List<UserAccount> GetUserAccounts(UUID scopeID, string query)
        {
            List<UserAccount> accounts = new List<UserAccount>();

            MainConsole.Instance.DebugFormat("[SIMIAN ACCOUNT CONNECTOR]: Searching for user accounts with name query " + query);

            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetUsers"},
                                                      {"NameQuery", query}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean())
            {
                OSDArray array = response["Users"] as OSDArray;
                if (array != null && array.Count > 0)
                {
#if (!ISWIN)
                    for (int i = 0; i < array.Count; i++)
                    {
                        UserAccount account = ResponseToUserAccount(array[i] as OSDMap);
                        if (account != null)
                            accounts.Add(account);
                    }
#else
                    accounts.AddRange(array.Select(t => ResponseToUserAccount(t as OSDMap)).Where(account => account != null));
#endif
                }
                else
                {
                    MainConsole.Instance.Warn(
                        "[SIMIAN ACCOUNT CONNECTOR]: Account search failed, response data was in an invalid format");
                }
            }
            else
            {
                MainConsole.Instance.Warn("[SIMIAN ACCOUNT CONNECTOR]: Failed to search for account data by name " + query);
            }

            return accounts;
        }

        public bool StoreUserAccount(UserAccount data)
        {
            MainConsole.Instance.InfoFormat("[SIMIAN ACCOUNT CONNECTOR]: Storing user account for " + data.Name);

            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "AddUser"},
                                                      {"UserID", data.PrincipalID.ToString()},
                                                      {"Name", data.Name},
                                                      {"Email", data.Email},
                                                      {"AccessLevel", data.UserLevel.ToString()}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);

            if (response["Success"].AsBoolean())
            {
                MainConsole.Instance.InfoFormat("[SIMIAN ACCOUNT CONNECTOR]: Storing user account data for " + data.Name);

                requestArgs = new NameValueCollection
                                  {
                                      {"RequestMethod", "AddUserData"},
                                      {"UserID", data.PrincipalID.ToString()},
                                      {"CreationDate", data.Created.ToString()},
                                      {"UserFlags", data.UserFlags.ToString()},
                                      {"UserTitle", data.UserTitle}
                                  };

                response = WebUtils.PostToService(m_serverUrl, requestArgs);
                bool success = response["Success"].AsBoolean();

                if (success)
                {
                    // Cache the user account info
                    m_accountCache.AddOrUpdate(data.PrincipalID, data, CACHE_EXPIRATION_SECONDS);
                }
                else
                {
                    MainConsole.Instance.Warn("[SIMIAN ACCOUNT CONNECTOR]: Failed to store user account data for " + data.Name + ": " +
                               response["Message"].AsString());
                }

                return success;
            }
            else
            {
                MainConsole.Instance.Warn("[SIMIAN ACCOUNT CONNECTOR]: Failed to store user account for " + data.Name + ": " +
                           response["Message"].AsString());
            }

            return false;
        }

        public void CreateUser(string name, string password, string email)
        {
        }

        public void CreateUser(UUID userID, string name, string password, string email)
        {
        }

        #endregion

        private void CommonInit(IConfigSource source)
        {
            IConfig gridConfig = source.Configs["UserAccountService"];
            if (gridConfig != null)
            {
                string serviceUrl = gridConfig.GetString("UserAccountServerURI");
                if (!String.IsNullOrEmpty(serviceUrl))
                {
                    if (!serviceUrl.EndsWith("/") && !serviceUrl.EndsWith("="))
                        serviceUrl = serviceUrl + '/';
                    m_serverUrl = serviceUrl;
                }
            }

            if (String.IsNullOrEmpty(m_serverUrl))
                MainConsole.Instance.Info("[SIMIAN ACCOUNT CONNECTOR]: No UserAccountServerURI specified, disabling connector");
        }

        /// <summary>
        ///   Helper method for the various ways of retrieving a user account
        /// </summary>
        /// <param name = "requestArgs">Service query parameters</param>
        /// <returns>A UserAccount object on success, null on failure</returns>
        private UserAccount GetUser(NameValueCollection requestArgs)
        {
            string lookupValue = (requestArgs.Count > 1) ? requestArgs[1] : "(Unknown)";
            MainConsole.Instance.DebugFormat("[SIMIAN ACCOUNT CONNECTOR]: Looking up user account with query: " + lookupValue);

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean())
            {
                OSDMap user = response["User"] as OSDMap;
                if (user != null)
                    return ResponseToUserAccount(user);
                else
                    MainConsole.Instance.Warn(
                        "[SIMIAN ACCOUNT CONNECTOR]: Account search failed, response data was in an invalid format");
            }
            else
            {
                MainConsole.Instance.Warn("[SIMIAN ACCOUNT CONNECTOR]: Failed to lookup user account with query: " + lookupValue);
            }

            return null;
        }

        /// <summary>
        ///   Convert a User object in LLSD format to a UserAccount
        /// </summary>
        /// <param name = "response">LLSD containing user account data</param>
        /// <returns>A UserAccount object on success, null on failure</returns>
        private UserAccount ResponseToUserAccount(OSDMap response)
        {
            if (response == null)
                return null;

            UserAccount account = new UserAccount
                                      {
                                          PrincipalID = response["UserID"].AsUUID(),
                                          Created = response["CreationDate"].AsInteger(),
                                          Email = response["Email"].AsString(),
                                          ServiceURLs = new Dictionary<string, object>(0),
                                          UserFlags = response["UserFlags"].AsInteger(),
                                          UserLevel = response["AccessLevel"].AsInteger(),
                                          UserTitle = response["UserTitle"].AsString(),
                                          Name = response["Name"].AsString()
                                      };

            // Cache the user account info
            m_accountCache.AddOrUpdate(account.PrincipalID, account, CACHE_EXPIRATION_SECONDS);

            return account;
        }

        /// <summary>
        ///   Convert a name with a single space in it to a first and last name
        /// </summary>
        /// <param name = "name">A full name such as "John Doe"</param>
        /// <param name = "firstName">First name</param>
        /// <param name = "lastName">Last name (surname)</param>
        private static void GetFirstLastName(string name, out string firstName, out string lastName)
        {
            if (String.IsNullOrEmpty(name))
            {
                firstName = String.Empty;
                lastName = String.Empty;
            }
            else
            {
                string[] names = name.Split(' ');

                if (names.Length == 2)
                {
                    firstName = names[0];
                    lastName = names[1];
                }
                else
                {
                    firstName = String.Empty;
                    lastName = name;
                }
            }
        }
    }
}