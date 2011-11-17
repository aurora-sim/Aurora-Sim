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
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using Nini.Config;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace Aurora.Services.DataService
{
    public class LocalAvatarConnector : IAvatarData
    {
        private IGenericData GD = null;
        private string m_realm = "avatars";
        //private string m_cacherealm = "avatarscache";

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AvatarConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(connectionString, "Avatars", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IAvatarData"; }
        }

        public void Dispose()
        {
        }

        #region IAvatarData Members

        public AvatarData Get (string field, string val)
        {
            return InternalGet (m_realm, field, val);
        }

        private AvatarData InternalGet (string realm, string field, string val)
        {
            List<string> data = GD.Query (field, val, realm, "Name, Value");
            AvatarData retVal = new AvatarData ();
            retVal.AvatarType = 1;
            retVal.Data = new Dictionary<string, string> ();
            for (int i = 0; i < data.Count; i += 2)
            {
                retVal.Data[data[i]] = data[i + 1];
            }
            return retVal;
        }

        public bool Store (UUID PrincipalID, AvatarData data)
        {
            GD.Delete (m_realm, new string[1] { "PrincipalID" }, new object[1] { PrincipalID });
            for (int i = 0; i < data.Data.Count; i++)
            {
                if (data.Data.ElementAt(i).Key == "Textures")
                    GD.Insert(m_realm, new object[3] { PrincipalID, data.Data.ElementAt(i).Key.MySqlEscape(), data.Data.ElementAt(i).Value });
                else
                    GD.Insert(m_realm, new object[3] { PrincipalID, data.Data.ElementAt(i).Key.MySqlEscape(), data.Data.ElementAt(i).Value.MySqlEscape() });
            }
            return true;
        }

        public bool Delete (string field, string val)
        {
            return GD.Delete (m_realm, new string[1] { field }, new object[1] { val });
        }

        #endregion
    }
}
