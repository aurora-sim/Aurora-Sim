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
using System.Linq;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class LocalAvatarConnector : IAvatarData
    {
        private IGenericData GD;
        private string m_realm = "avatars";
        //private string m_cacherealm = "avatarscache";
        private PreAddedDictionary<UUID, object> m_locks = new PreAddedDictionary<UUID, object>(() => new object());

        #region IAvatarData Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AvatarConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(connectionString, "Avatars",
                                     source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IAvatarData"; }
        }

        public AvatarData Get(string field, string val)
        {
            return InternalGet(m_realm, field, val);
        }

        public bool Store(UUID PrincipalID, AvatarData data)
        {
            lock (m_locks[PrincipalID])
            {
                QueryFilter filter = new QueryFilter();
                filter.andFilters["PrincipalID"] = PrincipalID;
                GD.Delete(m_realm, filter);
                List<object[]> insertList = new List<object[]>();
                foreach (KeyValuePair<string, string> kvp in data.Data)
                {
                    insertList.Add(new object[3]{
                        PrincipalID,
                        kvp.Key,
                        kvp.Value
                    });
                }
                GD.InsertMultiple(m_realm, insertList);
            }
            return true;
        }

        public bool Delete(string field, string val)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters[field] = val;
            return GD.Delete(m_realm, filter);
        }

        #endregion

        public void Dispose()
        {
        }

        private AvatarData InternalGet(string realm, string field, string val)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters[field] = val;
            List<string> data = GD.Query(new string[]{
                "Name",
                "`Value`"
            }, realm, filter, null, null, null);
            AvatarData retVal = new AvatarData {
                AvatarType = 1,
                Data = new Dictionary<string, string>()
            };
            for (int i = 0; i < data.Count; i += 2)
            {
                retVal.Data[data[i]] = data[i + 1];
            }
            return retVal;
        }
    }
}