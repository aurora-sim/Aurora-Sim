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

using System.Collections.Generic;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;

namespace Aurora.Services.DataService
{
    public class LocalFriendsConnector : IFriendsData
    {
        private IGenericData GD;
        private string m_realm = "friends";

        #region IFriendsData Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("FriendsConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(connectionString, "Friends",
                                     source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IFriendsData"; }
        }

        public bool Store(UUID PrincipalID, string Friend, int Flags, int Offered)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["PrincipalID"] = PrincipalID;
            filter.andFilters["Friend"] = Friend;
            GD.Delete(m_realm, filter);
            Dictionary<string, object> row = new Dictionary<string, object>(4);
            row["PrincipalID"] = PrincipalID;
            row["Friend"] = Friend.MySqlEscape();
            row["Flags"] = Flags;
            row["Offered"] = Offered;
            return GD.Insert(m_realm, row);
        }

        public bool Delete(UUID ownerID, string friend)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["PrincipalID"] = ownerID;
            filter.andFilters["Friend"] = friend.MySqlEscape();
            return GD.Delete(m_realm, filter);
        }

        public FriendInfo[] GetFriends(UUID principalID)
        {
            List<FriendInfo> infos = new List<FriendInfo>();

            QueryTables tables = new QueryTables();
            tables.AddTable(m_realm, "my");
            tables.AddTable(m_realm, "his", JoinType.Inner, new[,] { { "my.Friend", "his.PrincipalID" }, { "my.PrincipalID", "his.Friend" } });
            QueryFilter filter = new QueryFilter();
            filter.andFilters["my.PrincipalID"] = principalID;
            List<string> query = GD.Query(new string[]{
                "my.Friend",
                "my.Flags",
                "his.Flags"
            }, tables, filter, null, null, null);

            //These are used to get the other flags below

            for (int i = 0; i < query.Count; i += 3)
            {
                FriendInfo info = new FriendInfo{
                    PrincipalID = principalID,
                    Friend = query[i],
                    MyFlags = int.Parse(query[i + 1]),
                    TheirFlags = int.Parse(query[i + 2])
                };
                infos.Add(info);
            }
            return infos.ToArray();
        }

        #endregion

        public void Dispose()
        {
        }
    }
}