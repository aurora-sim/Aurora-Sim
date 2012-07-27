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
using Aurora.DataManager;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Services.Connectors;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.UserAccountService
{
    public class UserAccountService : ConnectorBase, IUserAccountService, IService
    {
        #region Declares

        protected IAuthenticationService m_AuthenticationService;
        protected IUserAccountData m_Database;
        protected IGridService m_GridService;
        protected IInventoryService m_InventoryService;
        protected GenericAccountCache<UserAccount> m_cache = new GenericAccountCache<UserAccount>();

        #endregion

        #region IService Members

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("UserAccountHandler", "") != Name)
                return;
            Configure(config, registry);
            Init(registry, Name);
        }

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
                    "Set user level. If the user's level is > 0, "
                    + "this account will be treated as god-moded. "
                    + "It will also affect the 'login level' command. ",
                    HandleSetUserLevel);
                MainConsole.Instance.Commands.AddCommand(
                    "set user profile title",
                    "set user profile title [<first> [<last> [<Title>]]]",
                    "Sets the title (Normally resident) in a user's title to some custom value.",
                    HandleSetTitle);
                MainConsole.Instance.Commands.AddCommand(
                    "set partner",
                    "set partner",
                    "Sets the partner in a user's profile.",
                    HandleSetPartner);
            }
            registry.RegisterModuleInterface<IUserAccountService>(this);
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

        #region IUserAccountService Members

        public virtual IUserAccountService InnerService
        {
            get { return this; }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public UserAccount GetUserAccount(List<UUID> scopeIDs, string firstName, string lastName)
        {
            UserAccount account;
            if (m_cache.Get(firstName + " " + lastName, out account))
                return AllScopeIDImpl.CheckScopeIDs(scopeIDs, account);

            object remoteValue = DoRemote(scopeIDs, firstName, lastName);
            if (remoteValue != null || m_doRemoteOnly)
            {
                UserAccount acc = (UserAccount)remoteValue;
                if (remoteValue != null)
                    m_cache.Cache(acc.PrincipalID, acc);

                return acc;
            }

            UserAccount[] d;

            d = m_Database.Get(scopeIDs,
                    new[] {"FirstName", "LastName"},
                    new[] {firstName, lastName});

            if (d.Length < 1)
                return null;

            CacheAccount(d[0]);
            return d[0];
        }

        public void CacheAccount(UserAccount account)
        {
            if ((account != null) && (account.UserLevel <= -1))
                return;
            m_cache.Cache(account.PrincipalID, account);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public UserAccount GetUserAccount(List<UUID> scopeIDs, string name)
        {
            UserAccount account;
            if (m_cache.Get(name, out account))
                return AllScopeIDImpl.CheckScopeIDs(scopeIDs, account);

            object remoteValue = DoRemote(scopeIDs, name);
            if (remoteValue != null || m_doRemoteOnly)
            {
                UserAccount acc = (UserAccount)remoteValue;
                if (remoteValue != null)
                    m_cache.Cache(acc.PrincipalID, acc);

                return acc;
            }

            UserAccount[] d;

            d = m_Database.Get(scopeIDs,
                    new[] {"Name"},
                    new[] {name});

            if (d.Length < 1)
            {
                string[] split = name.Split(' ');
                if (split.Length == 2)
                    return GetUserAccount(scopeIDs, split[0], split[1]);

                return null;
            }

            CacheAccount(d[0]);
            return d[0];
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low, RenamedMethod="GetUserAccountUUID")]
        public UserAccount GetUserAccount(List<UUID> scopeIDs, UUID principalID)
        {
            UserAccount account;
            if (m_cache.Get(principalID, out account))
                return AllScopeIDImpl.CheckScopeIDs(scopeIDs, account);

            object remoteValue = DoRemote(scopeIDs, principalID);
            if (remoteValue != null || m_doRemoteOnly)
            {
                UserAccount acc = (UserAccount)remoteValue;
                if (remoteValue != null)
                    m_cache.Cache(principalID, acc);

                return acc;
            }

            UserAccount[] d;

            d = m_Database.Get(scopeIDs,
                    new[] {"PrincipalID"},
                    new[] {principalID.ToString()});

            if (d.Length < 1)
            {
                m_cache.Cache(principalID, null);
                return null;
            }

            CacheAccount(d[0]);
            return d[0];
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public bool StoreUserAccount(UserAccount data)
        {
            object remoteValue = DoRemote(data);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? false : (bool)remoteValue;

            if (data.UserTitle == null)
                data.UserTitle = "";
            m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("UpdateUserInformation", data.PrincipalID);
            return m_Database.Store(data);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<UserAccount> GetUserAccounts(List<UUID> scopeIDs, string query)
        {
            object remoteValue = DoRemote(scopeIDs, query);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<UserAccount>)remoteValue;

            UserAccount[] d = m_Database.GetUsers(scopeIDs, query);

            if (d == null)
                return new List<UserAccount>();

            List<UserAccount> ret = new List<UserAccount>(d);
            return ret;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<UserAccount> GetUserAccounts(List<UUID> scopeIDs, string query, uint? start, uint? count)
        {
            object remoteValue = DoRemote(scopeIDs, query);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<UserAccount>)remoteValue;

            UserAccount[] d = m_Database.GetUsers(scopeIDs, query, start, count);

            if (d == null)
                return new List<UserAccount>();

            List<UserAccount> ret = new List<UserAccount>(d);
            return ret;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<UserAccount> GetUserAccounts(List<UUID> scopeIDs, int level, int flags)
        {
            object remoteValue = DoRemote(level, flags);
            if (remoteValue != null || m_doRemoteOnly)
                return (List<UserAccount>)remoteValue;

            UserAccount[] d = m_Database.GetUsers(scopeIDs, level, flags);

            if (d == null)
                return new List<UserAccount>();

            List<UserAccount> ret = new List<UserAccount>(d);
            return ret;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public uint NumberOfUserAccounts(List<UUID> scopeIDs, string query)
        {
            object remoteValue = DoRemote(scopeIDs, query);
            if (remoteValue != null || m_doRemoteOnly)
                return (uint)remoteValue;

            return m_Database.NumberOfUsers(scopeIDs, query);
        }

        public void CreateUser(string name, string password, string email)
        {
            CreateUser(UUID.Random(), UUID.Zero, name, password, email);
        }

        /// <summary>
        ///   Create a user
        /// </summary>
        /// <param name = "firstName"></param>
        /// <param name = "lastName"></param>
        /// <param name = "password"></param>
        /// <param name = "email"></param>
        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public string CreateUser(UUID userID, UUID scopeID, string name, string password, string email)
        {
            object remoteValue = DoRemote(userID, scopeID, name, password, email);
            if (remoteValue != null || m_doRemoteOnly)
                return remoteValue == null ? "" : remoteValue.ToString();

            UserAccount account = GetUserAccount(null, userID);
            UserAccount nameaccount = GetUserAccount(null, name);
            if (null == account && nameaccount == null)
            {
                account = new UserAccount(scopeID, userID, name, email);
                if (StoreUserAccount(account))
                {
                    bool success;
                    if (m_AuthenticationService != null && password != "")
                    {
                        success = m_AuthenticationService.SetPasswordHashed(account.PrincipalID, "UserAccount", password);
                        if (!success)
                        {
                            MainConsole.Instance.WarnFormat("[USER ACCOUNT SERVICE]: Unable to set password for account {0}.",
                                             name);
                            return "Unable to set password";
                        }
                    }

                    MainConsole.Instance.InfoFormat("[USER ACCOUNT SERVICE]: Account {0} created successfully", name);
                    //Cache it as well
                    CacheAccount(account);
                    m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("CreateUserInformation", userID);
                    return "";
                }
                else
                {
                    MainConsole.Instance.ErrorFormat("[USER ACCOUNT SERVICE]: Account creation failed for account {0}", name);
                    return "Unable to save account";
                }
            }
            else
            {
                MainConsole.Instance.ErrorFormat("[USER ACCOUNT SERVICE]: A user with the name {0} already exists!", name);
                return "A user with the same name already exists";
            }
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public void DeleteUser(UUID userID, string password, bool archiveInformation, bool wipeFromDatabase)
        {
            object remoteValue = DoRemote(userID, password, archiveInformation, wipeFromDatabase);
            if (remoteValue != null || m_doRemoteOnly)
                return;

            if (password != "" && m_AuthenticationService.Authenticate(userID, "UserAccount", password, 0) == "")
                return;//Not authed

            if (!m_Database.DeleteAccount(userID, archiveInformation))
            {
                MainConsole.Instance.WarnFormat("Failed to remove the account for {0}, please check that the database is valid after this operation!", userID);
                return;
            }

            if(wipeFromDatabase)
                m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("DeleteUserInformation", userID);
        }

        #endregion

        #region Console commands

        protected void HandleSetPartner(string[] cmdParams)
        {
            string first = MainConsole.Instance.Prompt("First User's name");
            string second = MainConsole.Instance.Prompt("Second User's name");

            IProfileConnector profileConnector = Aurora.DataManager.DataManager.RequestPlugin<IProfileConnector>();
            if (profileConnector != null)
            {
                IUserProfileInfo firstProfile = profileConnector.GetUserProfile(GetUserAccount(null, first).PrincipalID);
                IUserProfileInfo secondProfile = profileConnector.GetUserProfile(GetUserAccount(null, second).PrincipalID);

                firstProfile.Partner = secondProfile.PrincipalID;
                secondProfile.Partner = firstProfile.PrincipalID;

                profileConnector.UpdateUserProfile(firstProfile);
                profileConnector.UpdateUserProfile(secondProfile);

                MainConsole.Instance.Warn("Partner information updated. ");
            }
        }

        protected void HandleSetTitle(string[] cmdparams)
        {
            string firstName;
            string lastName;
            string title;

            firstName = cmdparams.Length < 5 ? MainConsole.Instance.Prompt("First name") : cmdparams[4];

            lastName = cmdparams.Length < 6 ? MainConsole.Instance.Prompt("Last name") : cmdparams[5];

            UserAccount account = GetUserAccount(null, firstName, lastName);
            if (account == null)
            {
                MainConsole.Instance.Info("No such user");
                return;
            }
            title = cmdparams.Length < 7 ? MainConsole.Instance.Prompt("User Title") : Util.CombineParams(cmdparams, 6);
            account.UserTitle = title;
            Aurora.Framework.IProfileConnector profileConnector = Aurora.DataManager.DataManager.RequestPlugin<Aurora.Framework.IProfileConnector>();
            if (profileConnector != null)
            {
                Aurora.Framework.IUserProfileInfo profile = profileConnector.GetUserProfile(account.PrincipalID);
                profile.MembershipGroup = title;
                profile.CustomType = title;
                profileConnector.UpdateUserProfile(profile);
            }
            bool success = StoreUserAccount(account);
            if (!success)
                MainConsole.Instance.InfoFormat("Unable to set user profile title for account {0} {1}.", firstName, lastName);
            else
                MainConsole.Instance.InfoFormat("User profile title set for user {0} {1} to {2}", firstName, lastName, title);
        }

        protected void HandleSetUserLevel(string[] cmdparams)
        {
            string firstName;
            string lastName;
            string rawLevel;
            int level;

            firstName = cmdparams.Length < 4 ? MainConsole.Instance.Prompt("First name") : cmdparams[3];

            lastName = cmdparams.Length < 5 ? MainConsole.Instance.Prompt("Last name") : cmdparams[4];

            UserAccount account = GetUserAccount(null, firstName, lastName);
            if (account == null)
            {
                MainConsole.Instance.Info("No such user");
                return;
            }

            rawLevel = cmdparams.Length < 6 ? MainConsole.Instance.Prompt("User level") : cmdparams[5];

            if (int.TryParse(rawLevel, out level) == false)
            {
                MainConsole.Instance.Info("Invalid user level");
                return;
            }

            account.UserLevel = level;

            bool success = StoreUserAccount(account);
            if (!success)
                MainConsole.Instance.InfoFormat("Unable to set user level for account {0} {1}.", firstName, lastName);
            else
                MainConsole.Instance.InfoFormat("User level set for user {0} {1} to {2}", firstName, lastName, level);
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

            UserAccount ua = GetUserAccount(null, firstName, lastName);

            if (ua == null)
            {
                MainConsole.Instance.InfoFormat("No user named {0} {1}", firstName, lastName);
                return;
            }

            MainConsole.Instance.InfoFormat("Name:    {0}", ua.Name);
            MainConsole.Instance.InfoFormat("ID:      {0}", ua.PrincipalID);
            MainConsole.Instance.InfoFormat("Title:   {0}", ua.UserTitle);
            MainConsole.Instance.InfoFormat("E-mail:  {0}", ua.Email);
            MainConsole.Instance.InfoFormat("Created: {0}", Utils.UnixTimeToDateTime(ua.Created));
            MainConsole.Instance.InfoFormat("Level:   {0}", ua.UserLevel);
            MainConsole.Instance.InfoFormat("Flags:   {0}", ua.UserFlags);
        }

        /// <summary>
        ///   Handle the create user command from the console.
        /// </summary>
        /// <param name = "cmdparams">string array with parameters: firstname, lastname, password, locationX, locationY, email</param>
        protected void HandleCreateUser(string[] cmdparams)
        {
            string name, password, email, uuid, scopeID;

            name = MainConsole.Instance.Prompt("Name", "Default User");

            password = MainConsole.Instance.PasswordPrompt("Password");

            email = MainConsole.Instance.Prompt("Email", "");

            uuid = MainConsole.Instance.Prompt("UUID (Don't change unless you have a reason)", UUID.Random().ToString());

            scopeID = MainConsole.Instance.Prompt("Scope (Don't change unless you know what this is)", UUID.Zero.ToString());

            CreateUser(UUID.Parse(uuid), UUID.Parse(scopeID), name, Util.Md5Hash(password), email);
        }

        protected void HandleResetUserPassword(string[] cmdparams)
        {
            string name;
            string newPassword;

            name = MainConsole.Instance.Prompt("Name");

            newPassword = MainConsole.Instance.PasswordPrompt("New password");

            UserAccount account = GetUserAccount(null, name);
            if (account == null)
                MainConsole.Instance.ErrorFormat("[USER ACCOUNT SERVICE]: No such user");

            bool success = false;
            if (m_AuthenticationService != null)
                success = m_AuthenticationService.SetPassword(account.PrincipalID, "UserAccount", newPassword);
            if (!success)
                MainConsole.Instance.ErrorFormat("[USER ACCOUNT SERVICE]: Unable to reset password for account {0}.",
                                  name);
            else
                MainConsole.Instance.InfoFormat("[USER ACCOUNT SERVICE]: Password reset for user {0}", name);
        }

        #endregion
    }
}