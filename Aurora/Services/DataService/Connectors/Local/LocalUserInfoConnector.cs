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
            GD.Delete(m_realm, new string[1] {"UserID"}, new object[1] {info.UserID});
            return GD.Insert(m_realm, values);
        }

        public void Update(string userID, string[] keys, object[] values)
        {
            GD.Update(m_realm, values, keys, new string[1] {"UserID"}, new object[1] {userID});
        }

        public void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            string[] keys = new string[5];
            keys[0] = "CurrentRegionID";
            keys[1] = "CurrentPosition";
            keys[2] = "CurrentLookat";
            keys[3] = "LastSeen";
                //Set the last seen and is online since if the user is moving, they are sending updates
            keys[4] = "IsOnline";
            object[] values = new object[5];
            values[0] = regionID;
            values[1] = lastPosition;
            values[2] = lastLookAt;
            values[3] = Util.ToUnixTime(DateTime.Now.ToUniversalTime());
                //Convert to binary so that it can be converted easily
            values[4] = 1;
            GD.Update(m_realm, values, keys, new string[1] {"UserID"}, new object[1] {userID});
        }

        public void SetHomePosition(string userID, UUID regionID, Vector3 Position, Vector3 LookAt)
        {
            string[] keys = new string[4];
            keys[0] = "HomeRegionID";
            keys[1] = "LastSeen";
            keys[2] = "HomePosition";
            keys[3] = "HomeLookat";
            object[] values = new object[4];
            values[0] = regionID;
            values[1] = Util.ToUnixTime(DateTime.Now.ToUniversalTime());
                //Convert to binary so that it can be converted easily
            values[2] = Position;
            values[3] = LookAt;
            GD.Update(m_realm, values, keys, new string[1] {"UserID"}, new object[1] {userID});
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
            UserInfo user = new UserInfo
            {
                UserID = query[0],
                CurrentRegionID = UUID.Parse(query[1]),
                IsOnline = query[3] == "1",
                LastLogin = Util.ToDateTime(int.Parse(query[4])),
                LastLogout = Util.ToDateTime(int.Parse(query[5])),
                Info = (OSDMap)OSDParser.DeserializeJson(query[6])
            };
            try
            {
                user.CurrentRegionID = UUID.Parse(query[7]);
                if (query[8] != "")
                    user.CurrentPosition = Vector3.Parse(query[8]);
                if (query[9] != "")
                    user.CurrentLookAt = Vector3.Parse(query[9]);
                user.HomeRegionID = UUID.Parse(query[10]);
                if (query[11] != "")
                    user.HomePosition = Vector3.Parse(query[11]);
                if (query[12] != "")
                    user.HomeLookAt = Vector3.Parse(query[12]);
            }
            catch
            {
            }

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

        #endregion

        public void Dispose()
        {
        }
    }
}