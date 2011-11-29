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
using System.Reflection;
using Aurora.DataManager;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Services.Connectors;
using OpenSim.Services.Interfaces;
using log4net;

namespace OpenSim.Services.UserAccountService
{
    public class UserAccountService : IUserAccountService, IService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected IAuthenticationService m_AuthenticationService;
        protected IUserAccountData m_Database;
        protected IGridService m_GridService;
        protected IInventoryService m_InventoryService;
        protected UserAccountCache m_cache = new UserAccountCache();

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("UserAccountHandler", "") != Name)
                return;
            Configure(config, registry);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_GridService = registry.RequestModuleInterface<IGridService>();
            m_AuthenticationService = registry.RequestModuleInterface<IAuthenticationService>();
            m_InventoryService = registry.RequestModuleInterface<IInventoryService>();
            m_Database = DataManager.RequestPlugin<IUserAccountData>();
            if (m_Database == null)
                throw new Exception("Could not find a storage interface in the given module");
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region IUserAccountService

        public UserAccount GetUserAccount(UUID scopeID, string firstName,
                                          string lastName)
        {
//            m_log.DebugFormat(
//                "[USER ACCOUNT SERVICE]: Retrieving account by username for {0} {1}, scope {2}",
//                firstName, lastName, scopeID);

            UserAccount[] d;

            UserAccount account;
            if (m_cache.Get(firstName + " " + lastName, out account))
                return account;

            if (scopeID != UUID.Zero)
            {
                d = m_Database.Get(
                    new[] {"ScopeID", "FirstName", "LastName"},
                    new[] {scopeID.ToString(), firstName, lastName});
                if (d.Length < 1)
                {
                    d = m_Database.Get(
                        new[] {"ScopeID", "FirstName", "LastName"},
                        new[] {UUID.Zero.ToString(), firstName, lastName});
                }
            }
            else
            {
                d = m_Database.Get(
                    new[] {"FirstName", "LastName"},
                    new[] {firstName, lastName});
            }

            if (d.Length < 1)
                return null;

            m_cache.Cache(d[0].PrincipalID, d[0]);
            return d[0];
        }

        public UserAccount GetUserAccount(UUID scopeID, string name)
        {
            UserAccount[] d;

            UserAccount account;
            if (m_cache.Get(name, out account))
                return account;

            if (scopeID != UUID.Zero)
            {
                d = m_Database.Get(
                    new[] {"ScopeID", "Name"},
                    new[] {scopeID.ToString(), name});
            }
            else
            {
                d = m_Database.Get(
                    new[] {"Name"},
                    new[] {name});
            }

            if (d.Length < 1)
            {
                string[] split = name.Split(' ');
                if (split.Length == 2)
                    return GetUserAccount(scopeID, split[0], split[1]);

                return null;
            }

            m_cache.Cache(d[0].PrincipalID, d[0]);
            return d[0];
        }

        public UserAccount GetUserAccount(UUID scopeID, UUID principalID)
        {
            UserAccount[] d;

            UserAccount account;
            if (m_cache.Get(principalID, out account))
                return account;

            if (scopeID != UUID.Zero)
            {
                d = m_Database.Get(
                    new[] {"ScopeID", "PrincipalID"},
                    new[] {scopeID.ToString(), principalID.ToString()});
                if (d.Length < 1)
                {
                    d = m_Database.Get(
                        new[] {"ScopeID", "PrincipalID"},
                        new[] {UUID.Zero.ToString(), principalID.ToString()});
                }
            }
            else
            {
                d = m_Database.Get(
                    new[] {"PrincipalID"},
                    new[] {principalID.ToString()});
            }

            if (d.Length < 1)
            {
                m_cache.Cache(principalID, null);
                return null;
            }

            m_cache.Cache(principalID, d[0]);
            return d[0];
        }

        public bool StoreUserAccount(UserAccount data)
        {
//            m_log.DebugFormat(
//                "[USER ACCOUNT SERVICE]: Storing user account for {0} {1} {2}, scope {3}",
//                data.FirstName, data.LastName, data.PrincipalID, data.ScopeID);

            if (data.UserTitle == null)
                data.UserTitle = "";

            return m_Database.Store(data);
        }

        public List<UserAccount> GetUserAccounts(UUID scopeID, string query)
        {
            UserAccount[] d = m_Database.GetUsers(scopeID, query);

            if (d == null)
                return new List<UserAccount>();

            List<UserAccount> ret = new List<UserAccount>(d);
            return ret;
        }

        #endregion

        #region Console commands

        protected void HandleSetUserLevel(string[] cmdparams)
        {
            string firstName;
            string lastName;
            string rawLevel;
            int level;

            firstName = cmdparams.Length < 4 ? MainConsole.Instance.CmdPrompt("First name") : cmdparams[3];

            lastName = cmdparams.Length < 5 ? MainConsole.Instance.CmdPrompt("Last name") : cmdparams[4];

            UserAccount account = GetUserAccount(UUID.Zero, firstName, lastName);
            if (account == null)
            {
                m_log.Info("No such user");
                return;
            }

            rawLevel = cmdparams.Length < 6 ? MainConsole.Instance.CmdPrompt("User level") : cmdparams[5];

            if (int.TryParse(rawLevel, out level) == false)
            {
                m_log.Info("Invalid user level");
                return;
            }

            account.UserLevel = level;

            bool success = StoreUserAccount(account);
            if (!success)
                m_log.InfoFormat("Unable to set user level for account {0} {1}.", firstName, lastName);
            else
                m_log.InfoFormat("User level set for user {0} {1} to {2}", firstName, lastName, level);
        }

        protected void HandleShowAccount(string[] cmdparams)
        {
            if (cmdparams.Length != 4)
            {
                MainConsole.Instance.Output("Usage: show account <first-name> <last-name>");
                return;
            }

            string firstName = cmdparams[2];
            string lastName = cmdparams[3];

            UserAccount ua = GetUserAccount(UUID.Zero, firstName, lastName);

            if (ua == null)
            {
                m_log.InfoFormat("No user named {0} {1}", firstName, lastName);
                return;
            }

            m_log.InfoFormat("Name:    {0}", ua.Name);
            m_log.InfoFormat("ID:      {0}", ua.PrincipalID);
            m_log.InfoFormat("Title:   {0}", ua.UserTitle);
            m_log.InfoFormat("E-mail:  {0}", ua.Email);
            m_log.InfoFormat("Created: {0}", Utils.UnixTimeToDateTime(ua.Created));
            m_log.InfoFormat("Level:   {0}", ua.UserLevel);
            m_log.InfoFormat("Flags:   {0}", ua.UserFlags);
        }

        /// <summary>
        ///   Handle the create user command from the console.
        /// </summary>
        /// <param name = "cmdparams">string array with parameters: firstname, lastname, password, locationX, locationY, email</param>
        protected void HandleCreateUser(string[] cmdparams)
        {
            string name;
            string password;
            string email;

            name = MainConsole.Instance.CmdPrompt("Name", "Default User");

            password = MainConsole.Instance.PasswdPrompt("Password");

            email = MainConsole.Instance.CmdPrompt("Email", "");

            CreateUser(name, Util.Md5Hash(password), email);
        }

        protected void HandleResetUserPassword(string[] cmdparams)
        {
            string name;
            string newPassword;

            name = MainConsole.Instance.CmdPrompt("Name");

            newPassword = MainConsole.Instance.PasswdPrompt("New password");

            UserAccount account = GetUserAccount(UUID.Zero, name);
            if (account == null)
                m_log.ErrorFormat("[USER ACCOUNT SERVICE]: No such user");

            bool success = false;
            if (m_AuthenticationService != null)
                success = m_AuthenticationService.SetPassword(account.PrincipalID, "UserAccount", newPassword);
            if (!success)
                m_log.ErrorFormat("[USER ACCOUNT SERVICE]: Unable to reset password for account {0}.",
                                  name);
            else
                m_log.InfoFormat("[USER ACCOUNT SERVICE]: Password reset for user {0}", name);
        }

        #endregion

        #region IUserAccountService Members

        public virtual IUserAccountService InnerService
        {
            get { return this; }
        }

        public void CreateUser(string name, string password, string email)
        {
            CreateUser(UUID.Random(), name, password, email);
        }

        /// <summary>
        ///   Create a user
        /// </summary>
        /// <param name = "firstName"></param>
        /// <param name = "lastName"></param>
        /// <param name = "password"></param>
        /// <param name = "email"></param>
        public void CreateUser(UUID userID, string name, string password, string email)
        {
            UserAccount account = GetUserAccount(UUID.Zero, userID);
            UserAccount nameaccount = GetUserAccount(UUID.Zero, name);
            if (null == account && nameaccount == null)
            {
                account = new UserAccount(UUID.Zero, userID, name, email);
                if (StoreUserAccount(account))
                {
                    bool success;
                    if (m_AuthenticationService != null && password != "")
                    {
                        success = m_AuthenticationService.SetPasswordHashed(account.PrincipalID, "UserAccount", password);
                        if (!success)
                            m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to set password for account {0}.",
                                             name);
                    }

                    m_log.InfoFormat("[USER ACCOUNT SERVICE]: Account {0} created successfully", name);
                    //Cache it as well
                    m_cache.Cache(account.PrincipalID, account);
                }
                else
                {
                    m_log.ErrorFormat("[USER ACCOUNT SERVICE]: Account creation failed for account {0}", name);
                }
            }
            else
            {
                m_log.ErrorFormat("[USER ACCOUNT SERVICE]: A user with the name {0} already exists!", name);
            }
        }

        #endregion

        public void Configure(IConfigSource config, IRegistryCore registry)
        {
            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand(
                    "create user",
                    "create user [<first> [<last> [<pass> [<email>]]]]",
                    "Create a new user", HandleCreateUser);
                MainConsole.Instance.Commands.AddCommand("reset user password",
                                                         "reset user password [<first> [<last> [<password>]]]",
                                                         "Reset a user password", HandleResetUserPassword);
                MainConsole.Instance.Commands.AddCommand(
                    "show account",
                    "show account <first> <last>",
                    "Show account details for the given user", HandleShowAccount);
                MainConsole.Instance.Commands.AddCommand(
                    "set user level",
                    "set user level [<first> [<last> [<level>]]]",
                    "Set user level. If >= 200 and 'allow_grid_gods = true' in OpenSim.ini, "
                    + "this account will be treated as god-moded. "
                    + "It will also affect the 'login level' command. ",
                    HandleSetUserLevel);
            }
            registry.RegisterModuleInterface<IUserAccountService>(this);
        }
    }
}