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
using System.Reflection;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class LocalUserInfoConnector : IAgentInfoConnector
    {
        private IGenericData GD;
        protected bool m_allowDuplicatePresences = true;
        protected bool m_checkLastSeen = true;
        private string m_realm = "userinfo";

        #region IAgentInfoConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("UserInfoConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                {
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                    m_allowDuplicatePresences =
                        source.Configs[Name].GetBoolean("AllowDuplicatePresences",
                                                        m_allowDuplicatePresences);
                    m_checkLastSeen =
                        source.Configs[Name].GetBoolean("CheckLastSeen",
                                                        m_checkLastSeen);
                }
                GD.ConnectToDatabase(connectionString, "UserInfo",
                                     source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IAgentInfoConnector"; }
        }

        public bool Set(UserInfo info)
        {
            object[] values = new object[13];
            values[0] = info.UserID;
            values[1] = info.CurrentRegionID;
            values[2] = Util.ToUnixTime(DateTime.Now.ToUniversalTime());
                //Convert to binary so that it can be converted easily
            values[3] = info.IsOnline ? 1 : 0;
            values[4] = Util.ToUnixTime(info.LastLogin);
            values[5] = Util.ToUnixTime(info.LastLogout);
            values[6] = OSDParser.SerializeJsonString(info.Info);
            values[7] = info.CurrentRegionID.ToString();
            values[8] = info.CurrentPosition.ToString();
            values[9] = info.CurrentLookAt.ToString();
            values[10] = info.HomeRegionID.ToString();
            values[11] = info.HomePosition.ToString();
            values[12] = info.HomeLookAt.ToString();

            QueryFilter filter = new QueryFilter();
            filter.andFilters["UserID"] = info.UserID;
            GD.Delete(m_realm, filter);
            return GD.Insert(m_realm, values);
        }

        public void Update(string userID, Dictionary<string, object> values)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["UserID"] = userID;

            GD.Update(m_realm, values, null, filter, null, null);
        }

        public void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            Dictionary<string, object> values = new Dictionary<string, object>(5);
            values["CurrentRegionID"] = regionID;
            values["CurrentPosition"] = lastPosition;
            values["CurrentLookat"] = lastLookAt;
            values["LastSeen"] = Util.ToUnixTime(DateTime.Now.ToUniversalTime());
                //Set the last seen and is online since if the user is moving, they are sending updates
            values["IsOnline"] = 1;

            Update(userID, values);
        }

        public void SetHomePosition(string userID, UUID regionID, Vector3 Position, Vector3 LookAt)
        {
            Dictionary<string, object> values = new Dictionary<string, object>(4);
            values["HomeRegionID"] = regionID;
            values["LastSeen"] = Util.ToUnixTime(DateTime.Now.ToUniversalTime());
            values["HomePosition"] = Position;
            values["HomeLookat"] = LookAt;

            Update(userID, values);
        }

        private static List<UserInfo> ParseQuery(List<string> query)
        {
            List<UserInfo> users = new List<UserInfo>();

            if (query.Count % 13 == 0)
            {
                for (int i = 0; i < query.Count; i += 13)
                {

                    UserInfo user = new UserInfo
                    {
                        UserID = query[i],
                        CurrentRegionID = UUID.Parse(query[i + 1]),
                        IsOnline = query[i + 3] == "1",
                        LastLogin = Util.ToDateTime(int.Parse(query[i + 4])),
                        LastLogout = Util.ToDateTime(int.Parse(query[i + 5])),
                        Info = (OSDMap)OSDParser.DeserializeJson(query[i + 6])
                    };
                    try
                    {
                        user.CurrentRegionID = UUID.Parse(query[i + 7]);
                        if (query[i + 8] != "")
                            user.CurrentPosition = Vector3.Parse(query[i + 8]);
                        if (query[i + 9] != "")
                            user.CurrentLookAt = Vector3.Parse(query[i + 9]);
                        user.HomeRegionID = UUID.Parse(query[i + 10]);
                        if (query[i + 11] != "")
                            user.HomePosition = Vector3.Parse(query[i + 11]);
                        if (query[i + 12] != "")
                            user.HomeLookAt = Vector3.Parse(query[i + 12]);
                    }
                    catch
                    {
                    }

                    users.Add(user);
                }
            }

            return users;
        }

        public UserInfo Get(string userID, bool checkOnlineStatus, out bool onlineStatusChanged)
        {
            onlineStatusChanged = false;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["UserID"] = userID;
            List<string> query = GD.Query(new string[1] { "*" }, m_realm, filter, null, null, null);

            if (query.Count == 0)
            {
                return null;
            }
            UserInfo user = ParseQuery(query)[0];

            //Check LastSeen
            DateTime timeLastSeen = Util.ToDateTime(int.Parse(query[2]));
            DateTime timeNow = DateTime.Now.ToUniversalTime();
            if (checkOnlineStatus && m_checkLastSeen && user.IsOnline && (timeLastSeen.AddHours(1) < timeNow))
            {
                if (user.CurrentRegionID != AgentInfoHelpers.LOGIN_STATUS_LOCKED)
                    //The login status can be locked with this so that it cannot be changed with this method
                {
                    MainConsole.Instance.Warn("[UserInfoService]: Found a user (" + user.UserID +
                               ") that was not seen within the last hour " +
                               "(since " + timeLastSeen.ToLocalTime().ToString() + ", time elapsed " +
                               (timeNow - timeLastSeen).Days + " days, " + (timeNow - timeLastSeen).Hours +
                               " hours)! Logging them out.");
                    user.IsOnline = false;
                    Set(user);
                    onlineStatusChanged = true;
                }
            }
            return user;
        }

        public uint RecentlyOnline(uint secondsAgo, bool stillOnline)
        {
            int now = (int)Utils.DateTimeToUnixTime(DateTime.Now) - (int)secondsAgo;

            QueryFilter filter = new QueryFilter();
            filter.orGreaterThanEqFilters["LastLogin"] = now;
            filter.orGreaterThanEqFilters["LastSeen"] = now;
            if (stillOnline)
            {
//                filter.andGreaterThanFilters["LastLogout"] = now;
                filter.andFilters["IsOnline"] = "1";
            }

            return uint.Parse(GD.Query(new string[1] { "COUNT(UserID)" }, m_realm, filter, null, null, null)[0]);
        }

        public List<UserInfo> RecentlyOnline(uint secondsAgo, bool stillOnline, Dictionary<string, bool> sort, uint start, uint count)
        {
            int now = (int)Utils.DateTimeToUnixTime(DateTime.Now) - (int)secondsAgo;

            QueryFilter filter = new QueryFilter();
            filter.orGreaterThanEqFilters["LastLogin"] = now;
            filter.orGreaterThanEqFilters["LastSeen"] = now;
            if (stillOnline)
            {
//                filter.andGreaterThanFilters["LastLogout"] = now;
                filter.andFilters["IsOnline"] = "1";
            }

            List<string> query = GD.Query(new string[] { "*" }, m_realm, filter, sort, start, count);

            return ParseQuery(query);
        }

        #endregion

        public void Dispose()
        {
        }
    }
}