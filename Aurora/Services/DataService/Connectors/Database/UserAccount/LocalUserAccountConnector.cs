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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class LocalUserAccountConnector : IUserAccountData
    {
        private IGenericData GD;
        private const string m_realm = "useraccounts";

        public string Realm { get { return "useraccounts"; } }

        #region IUserAccountData Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AbuseReportsConnector", "LocalConnector") ==
                "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(connectionString, "UserAccounts",
                                     source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IUserAccountData"; }
        }

        public UserAccount[] Get(List<UUID> scopeIDs, string[] fields, string[] values)
        {
            Dictionary<string, object> where = new Dictionary<string, object>(values.Length);

            for (uint i = 0; i < values.Length; ++i)
            {
                where[fields[i]] = values[i];
            }

            List<string> query = GD.Query(new[] { "*" }, m_realm, new QueryFilter
            {
                andFilters = where
            }, null, null, null);

            return ParseQuery(scopeIDs, query).ToArray();
        }

        public bool Store(UserAccount data)
        {
            if (data.UserTitle == null)
                data.UserTitle = "";

            Dictionary<string, object> row = new Dictionary<string, object>(12);
            row["PrincipalID"] = data.PrincipalID;
            row["ScopeID"] = data.ScopeID;
            row["FirstName"] = data.FirstName;
            row["LastName"] = data.LastName;
            row["Email"] = data.Email;
            row["ServiceURLs"] = Util.ConvertToString(data.ServiceURLs);
            row["Created"] = data.Created;
            row["UserLevel"] = data.UserLevel;
            row["UserFlags"] = data.UserFlags;
            row["UserTitle"] = data.UserTitle;
            row["Name"] = data.Name;
            row["OSD"] = OSDParser.SerializeJsonString(data.ToOSD());

            return GD.Replace(m_realm, row);
        }

        public bool DeleteAccount(UUID userID, bool archiveInformation)
        {
            if (archiveInformation)
            {
                return GD.Update(m_realm, new Dictionary<string, object> { { "UserLevel", -2 } }, null,
                    new QueryFilter { andFilters = new Dictionary<string, object> { { "PrincipalID", userID } } }, null, null);
            }
            QueryFilter filter = new QueryFilter();
            filter.andFilters.Add("PrincipalID", userID);
            return GD.Delete(m_realm, filter);
        }

        public UserAccount[] GetUsers(List<UUID> scopeIDs, string query)
        {
            return GetUsers(scopeIDs, query, null, null);
        }

        private static QueryFilter GetUsersFilter(string query)
        {
            QueryFilter filter = new QueryFilter();

            string[] words = query.Split(new[] { ' ' });

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length < 3)
                {
                    if (i != words.Length - 1)
                    {
                        Array.Copy(words, i + 1, words, i, words.Length - i - 1);
                    }
                    Array.Resize(ref words, words.Length - 1);
                }
            }
            if (words.Length > 0)
            {
                filter.orLikeFilters["Name"] = "%" + query + "%";
                filter.orLikeFilters["FirstName"] = "%" + words[0] + "%";
                if (words.Length == 2)
                {
                    filter.orLikeMultiFilters["LastName"] = new List<string>(2) {"%" + words[0], "%" + words[1] + "%"};
                }
                else
                {
                    filter.orLikeFilters["LastName"] = "%" + words[0] + "%";
                }
            }

            return filter;
        }

        public UserAccount[] GetUsers(List<UUID> scopeIDs, string query, uint? start, uint? count)
        {
            QueryFilter filter = GetUsersFilter(query);

            Dictionary<string, bool> sort = new Dictionary<string, bool>(2);
            sort["LastName"] = true;
            sort["FirstName"] = true; // these are in this order so results should be ordered by last name first, then first name

            List<string> retVal = GD.Query(new[]{
                                                   "PrincipalID",
                                                   "ScopeID",
                                                   "FirstName",
                                                   "LastName",
                                                   "Email",
                                                   "ServiceURLs",
                                                   "Created",
                                                   "UserLevel",
                                                   "UserFlags",
                                                   "UserTitle",
                                                   "IFNULL(Name, " + GD.ConCat(new[] {"FirstName", "' '", "LastName"}) + ") as Name",
                                                   "OSD"
                                               }, m_realm, filter, sort, start, count);

            return ParseQuery(scopeIDs, retVal).ToArray();
        }

        public UserAccount[] GetUsers(List<UUID> scopeIDs, int level, int flag)
        {
            QueryFilter filter = new QueryFilter();
            filter.andGreaterThanEqFilters["UserLevel"] = level;
            if (flag != 0)  
                filter.andBitfieldAndFilters["UserFlags"] = (uint)flag;

            Dictionary<string, bool> sort = new Dictionary<string, bool>(2);
            sort["LastName"] = true;
            sort["FirstName"] = true; // these are in this order so results should be ordered by last name first, then first name

            List<string> retVal = GD.Query(new[]{
                                                   "PrincipalID",
                                                   "ScopeID",
                                                   "FirstName",
                                                   "LastName",
                                                   "Email",
                                                   "ServiceURLs",
                                                   "Created",
                                                   "UserLevel",
                                                   "UserFlags",
                                                   "UserTitle",
                                                   "IFNULL(Name, " + GD.ConCat(new[] {"FirstName", "' '", "LastName"}) + ") as Name",
                                                   "OSD"
                                               }, m_realm, filter, sort, null, null);

            return ParseQuery(scopeIDs, retVal).ToArray();
        }

        public uint NumberOfUsers(List<UUID> scopeIDs, string query)
        {
            return uint.Parse(GD.Query(new[] { "COUNT(*)" }, m_realm, GetUsersFilter(query), null, null, null)[0]);
        }

        #endregion

        public void Dispose()
        {
        }

        private List<UserAccount> ParseQuery(List<UUID> scopeIDs, List<string> query)
        {
            List<UserAccount> list = new List<UserAccount>();
            for (int i = 0; i < query.Count; i += 12)
            {
                UserAccount data = new UserAccount { PrincipalID = UUID.Parse(query[i + 0]), ScopeID = UUID.Parse(query[i + 1]) };
                if(query[i + 11] != "")
                    data.FromOSD((OSDMap)OSDParser.DeserializeJson(query[i + 11]));

                //We keep these even though we don't always use them because we might need to create the "Name" from them
                string FirstName = query[i + 2];
                string LastName = query[i + 3];
                data.Email = query[i + 4];

                data.ServiceURLs = new Dictionary<string, object>();
                if ((query[i + 5] != null) && (query[i + 5].Contains("=")))
                {
                    Dictionary<string, string> dict = Util.ConvertToDictionary(query[i + 5]);
                    foreach (var v in dict)
                        data.ServiceURLs[v.Key] = v.Value;
                }
                data.Created = Int32.Parse(query[i + 6]);
                data.UserLevel = Int32.Parse(query[i + 7]);
                data.UserFlags = Int32.Parse(query[i + 8]);
                data.UserTitle = query[i + 9];
                data.Name = query[i + 10];
                if (string.IsNullOrEmpty(data.Name))
                {
                    data.Name = FirstName + " " + LastName;
                    //Save the change!
                    Store(data);
                }
                list.Add(data);
            }

            return AllScopeIDImpl.CheckScopeIDs(scopeIDs, list);
        }
    }
}