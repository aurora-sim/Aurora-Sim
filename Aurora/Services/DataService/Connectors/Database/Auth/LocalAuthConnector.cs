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
using Aurora.Framework;
using Aurora.Framework.Services;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Services.DataService
{
    public class LocalAuthConnector : IAuthenticationData
    {
        private IGenericData GD;
        private int m_LastExpire;
        private string m_realm = "auth";
        private string m_tokensrealm = "tokens";

        #region IAuthenticationData Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AuthConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                if (GD != null)
                    GD.ConnectToDatabase(connectionString, "Auth",
                                         source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                Framework.Utilities.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IAuthenticationData"; }
        }

        public AuthData Get(UUID principalID, string authType)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["UUID"] = principalID;
            filter.andFilters["accountType"] = authType;
            List<string> query = GD.Query(new string[1] {"*"}, m_realm, filter, null, null, null);
            AuthData data = null;
            for (int i = 0; i < query.Count; i += 5)
            {
                data = new AuthData
                           {
                               PrincipalID = UUID.Parse(query[i]),
                               PasswordHash = query[i + 1],
                               PasswordSalt = query[i + 2],
                               AccountType = query[i + 3]
                           };
            }
            return data;
        }

        public bool Store(AuthData data)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["UUID"] = data.PrincipalID;
            filter.andFilters["accountType"] = data.AccountType;
            GD.Delete(m_realm, filter);
            Dictionary<string, object> row = new Dictionary<string, object>(4);
            row["UUID"] = data.PrincipalID;
            row["passwordHash"] = data.PasswordHash;
            row["passwordSalt"] = data.PasswordSalt;
            row["accountType"] = data.AccountType;
            return GD.Insert(m_realm, row);
        }

        // I don't think this is used anywhere (don't know who wrote this comment ~ SignpostMarv)
        public bool SetDataItem(UUID principalID, string item, string value)
        {
            Dictionary<string, object> values = new Dictionary<string, object>(1);
            values[item] = value;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["UUID"] = principalID;

            return GD.Update(m_realm, values, null, filter, null, null);
        }

        public bool Delete(UUID principalID, string authType)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["UUID"] = principalID;
            filter.andFilters["accountType"] = authType;
            return GD.Delete(m_realm, filter);
        }

        public bool SetToken(UUID principalID, string token, int lifetime)
        {
            if (Environment.TickCount - m_LastExpire > 30000)
            {
                DoExpire();
            }

            Dictionary<string, object> row = new Dictionary<string, object>(3);
            row["UUID"] = principalID;
            row["token"] = token;
            row["validity"] = Utils.DateTimeToUnixTime(DateTime.Now) + (lifetime*60);

            return GD.Replace(m_tokensrealm, row);
        }

        public bool CheckToken(UUID principalID, string token, int lifetime)
        {
            if (Environment.TickCount - m_LastExpire > 30000)
            {
                DoExpire();
            }

            uint now = Utils.DateTimeToUnixTime(DateTime.Now);
            Dictionary<string, object> values = new Dictionary<string, object>(1);
            values["validity"] = now + (lifetime*60);

            QueryFilter filter = new QueryFilter();
            filter.andFilters["UUID"] = principalID;
            filter.andFilters["token"] = token;
            filter.andLessThanEqFilters["validity"] = (int) now;

            return GD.Update(m_tokensrealm, values, null, filter, null, null);
        }

        #endregion

        public void Dispose()
        {
        }

        private void DoExpire()
        {
            GD.DeleteByTime(m_tokensrealm, "validity");

            m_LastExpire = Environment.TickCount;
        }
    }
}