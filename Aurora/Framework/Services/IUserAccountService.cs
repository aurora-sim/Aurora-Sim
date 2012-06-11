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
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    public class UserAccount : IDataTransferable, BaseCacheAccount
    {
        public int Created;
        public string Email;
        public string Name { get; set;}
        public OSDMap GenericData = new OSDMap();
        public UUID PrincipalID { get; set; }
        public UUID ScopeID;
        public Dictionary<string, object> ServiceURLs;
        public int UserFlags;
        public int UserLevel;
        public string UserTitle;

        public UserAccount()
        {
        }

        public UserAccount(UUID principalID)
        {
            PrincipalID = principalID;
        }

        public UserAccount(UUID scopeID, string name, string email)
        {
            PrincipalID = UUID.Random();
            ScopeID = scopeID;
            Name = name;
            Email = email;
            ServiceURLs = new Dictionary<string, object>();
            Created = Util.UnixTimeSinceEpoch();
        }

        public UserAccount(UUID scopeID, UUID principalID, string name, string email)
        {
            PrincipalID = principalID;
            ScopeID = scopeID;
            Name = name;
            Email = email;
            ServiceURLs = new Dictionary<string, object>();
            Created = Util.UnixTimeSinceEpoch();
        }

        public string FirstName
        {
            get { return Name.Split(' ')[0]; }
        }

        public string LastName
        {
            get
            {
                string[] split = Name.Split(' ');
                if (split.Length > 1)
                    return Name.Split(' ')[1];
                else return "";
            }
        }

        public override Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["FirstName"] = FirstName;
            result["LastName"] = LastName;
            result["Email"] = Email;
            result["PrincipalID"] = PrincipalID.ToString();
            result["ScopeID"] = ScopeID.ToString();
            result["Created"] = Created.ToString();
            result["UserLevel"] = UserLevel.ToString();
            result["UserFlags"] = UserFlags.ToString();
            result["UserTitle"] = UserTitle;

#if (!ISWIN)
            string str = string.Empty;
            foreach (KeyValuePair<string, object> l in ServiceURLs)
                str = str + (l.Key + "*" + (l.Value ?? "") + ";");
#else
            string str = ServiceURLs.Aggregate(string.Empty, (current, kvp) => current + (kvp.Key + "*" + (kvp.Value ?? "") + ";"));
#endif
            result["ServiceURLs"] = str;

            return result;
        }

        public override void FromKVP(Dictionary<string, object> kvp)
        {
            if (kvp.ContainsKey("FirstName") && kvp.ContainsKey("LastName"))
                Name = kvp["FirstName"] + " " + kvp["LastName"];
            if (kvp.ContainsKey("Name"))
                Name = kvp["Name"].ToString();
            if (kvp.ContainsKey("Email"))
                Email = kvp["Email"].ToString();
            if (kvp.ContainsKey("PrincipalID"))
            {
                UUID id;
                if (UUID.TryParse(kvp["PrincipalID"].ToString(), out id))
                    PrincipalID = id;
            }
            if (kvp.ContainsKey("ScopeID"))
                UUID.TryParse(kvp["ScopeID"].ToString(), out ScopeID);
            if (kvp.ContainsKey("UserLevel"))
                UserLevel = Convert.ToInt32(kvp["UserLevel"].ToString());
            if (kvp.ContainsKey("UserFlags"))
                UserFlags = Convert.ToInt32(kvp["UserFlags"].ToString());
            if (kvp.ContainsKey("UserTitle"))
                UserTitle = kvp["UserTitle"].ToString();

            if (kvp.ContainsKey("Created"))
                Created = Convert.ToInt32(kvp["Created"].ToString());
            if (kvp.ContainsKey("ServiceURLs") && kvp["ServiceURLs"] != null)
            {
                ServiceURLs = new Dictionary<string, object>();
                string str = kvp["ServiceURLs"].ToString();
                if (str != string.Empty)
                {
                    string[] parts = str.Split(new[] { ';' });
#if (!ISWIN)
                    foreach (string s in parts)
                    {
                        string[] parts2 = s.Split(new[] { '*' });
                        if (parts2.Length == 2)
                        {
                            ServiceURLs[parts2[0]] = parts2[1];
                        }
                    }
#else
                    foreach (string[] parts2 in parts.Select(s => s.Split(new[] {'*'})).Where(parts2 => parts2.Length == 2))
                    {
                        ServiceURLs[parts2[0]] = parts2[1];
                    }
#endif
                }
            }
        }

        public override OSDMap ToOSD()
        {
            OSDMap result = new OSDMap();
            result["FirstName"] = FirstName;
            result["LastName"] = LastName;
            result["Email"] = Email;
            result["PrincipalID"] = PrincipalID;
            result["ScopeID"] = ScopeID;
            result["Created"] = Created;
            result["UserLevel"] = UserLevel;
            result["UserFlags"] = UserFlags;
            result["UserTitle"] = UserTitle;

#if (!ISWIN)
            string str = string.Empty;
            foreach (KeyValuePair<string, object> l in ServiceURLs)
                str = str + (l.Key + "*" + (l.Value ?? "") + ";");
#else
            string str = ServiceURLs.Aggregate(string.Empty, (current, kvp) => current + (kvp.Key + "*" + (kvp.Value ?? "") + ";"));
#endif
            result["ServiceURLs"] = str;

            return result;
        }

        public override void FromOSD(OSDMap map)
        {
            if (map.ContainsKey("FirstName") && map.ContainsKey("LastName"))
                Name = map["FirstName"] + " " + map["LastName"];
            if (map.ContainsKey("Name"))
                Name = map["Name"].ToString();
            if (map.ContainsKey("Email"))
                Email = map["Email"].ToString();
            if (map.ContainsKey("PrincipalID"))
                PrincipalID = map["PrincipalID"];
            if (map.ContainsKey("ScopeID"))
                ScopeID = map["ScopeID"];
            if (map.ContainsKey("UserLevel"))
                UserLevel = map["UserLevel"];
            if (map.ContainsKey("UserFlags"))
                UserFlags = map["UserFlags"];
            if (map.ContainsKey("UserTitle"))
                UserTitle = map["UserTitle"];

            if (map.ContainsKey("Created"))
                Created = map["Created"];
            if (map.ContainsKey("ServiceURLs") && map["ServiceURLs"] != null)
            {
                ServiceURLs = new Dictionary<string, object>();
                string str = map["ServiceURLs"].ToString();
                if (str != string.Empty)
                {
                    string[] parts = str.Split(new[] { ';' });
#if (!ISWIN)
                    foreach (string s in parts)
                    {
                        string[] parts2 = s.Split(new[] { '*' });
                        if (parts2.Length == 2)
                        {
                            ServiceURLs[parts2[0]] = parts2[1];
                        }
                    }
#else
                    foreach (string[] parts2 in parts.Select(s => s.Split(new[] {'*'})).Where(parts2 => parts2.Length == 2))
                    {
                        ServiceURLs[parts2[0]] = parts2[1];
                    }
#endif
                }
            }
        }
    }

    public interface IUserAccountService
    {
        IUserAccountService InnerService { get; }

        /// <summary>
        ///   Get a user given by UUID
        /// </summary>
        /// <param name = "scopeID"></param>
        /// <param name = "userID"></param>
        /// <returns></returns>
        UserAccount GetUserAccount(UUID scopeID, UUID userID);

        /// <summary>
        ///   Get a user given by a first and last name
        /// </summary>
        /// <param name = "scopeID"></param>
        /// <param name = "FirstName"></param>
        /// <param name = "LastName"></param>
        /// <returns></returns>
        UserAccount GetUserAccount(UUID scopeID, string FirstName, string LastName);

        /// <summary>
        ///   Get a user given by its full name
        /// </summary>
        /// <param name = "scopeID"></param>
        /// <param name = "Email"></param>
        /// <returns></returns>
        UserAccount GetUserAccount(UUID scopeID, string Name);

        /// <summary>
        ///   Returns the list of avatars that matches both the search criterion and the scope ID passed
        /// </summary>
        /// <param name = "scopeID"></param>
        /// <param name = "query"></param>
        /// <returns></returns>
        List<UserAccount> GetUserAccounts(UUID scopeID, string query);

        /// <summary>
        /// Returns a paginated list of avatars that matches both the search criteriion and the scope ID passed
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="query"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        List<UserAccount> GetUserAccounts(UUID scopeID, string query, uint? start, uint? count);

        /// <summary>
        /// Returns a paginated list of avatars that matches both the search criteriion and the scope ID passed
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="level">greater than or equal to clause is used</param>
        /// <param name="flags">bit mask clause is used</param>
        /// <returns></returns>
        List<UserAccount> GetUserAccounts(UUID scopeID, int level, int flags);

        /// <summary>
        /// Returns the number of avatars that match both the search criterion and the scope ID passed
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        uint NumberOfUserAccounts(UUID scopeID, string query);

        /// <summary>
        ///   Store the data given, wich replaces the stored data, therefore must be complete.
        /// </summary>
        /// <param name = "data"></param>
        /// <returns></returns>
        bool StoreUserAccount(UserAccount data);

        /// <summary>
        /// Cache the given userAccount so that it doesn't have to be queried later
        /// </summary>
        /// <param name="account"></param>
        void CacheAccount(UserAccount account);

        /// <summary>
        ///   Create the user with the given info
        /// </summary>
        /// <param name = "name"></param>
        /// <param name = "md5password">MD5 hashed password</param>
        /// <param name = "email"></param>
        void CreateUser(string name, string md5password, string email);

        /// <summary>
        ///   Create the user with the given info
        /// </summary>
        /// <param name = "name"></param>
        /// <param name = "md5password">MD5 hashed password</param>
        /// <param name = "email"></param>
        void CreateUser(UUID userID, UUID scopeID, string name, string md5password, string email);

        /// <summary>
        /// Delete a user from the database permanently
        /// </summary>
        /// <param name="userID">The user's ID</param>
        /// <param name="password">The user's password</param>
        /// <param name="archiveInformation">Whether or not we should store the account's name and account information so that the user's information inworld does not go null</param>
        /// <param name="wipeFromDatabase">Whether or not we should remove all of the user's data from other locations in the database</param>
        void DeleteUser(UUID userID, string password, bool archiveInformation, bool wipeFromDatabase);
    }

    /// <summary>
    ///   An interface for connecting to the user accounts datastore
    /// </summary>
    public interface IUserAccountData : IAuroraDataPlugin
    {
        string Realm { get; }
        UserAccount[] Get(string[] fields, string[] values);
        bool Store(UserAccount data);
        bool DeleteAccount(UUID userID, bool archiveInformation);
        UserAccount[] GetUsers(UUID scopeID, string query);
        UserAccount[] GetUsers(UUID scopeID, string query, uint? start, uint? count);
        UserAccount[] GetUsers(UUID scopeID, int level, int flags);
        uint NumberOfUsers(UUID scopeID, string query);
    }
}