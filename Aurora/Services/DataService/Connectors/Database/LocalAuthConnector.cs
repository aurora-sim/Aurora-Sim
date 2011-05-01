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
    public class LocalAuthConnector : IAuthenticationData
    {
        private IGenericData GD = null;
        private string m_realm = "auth";
        private string m_tokensrealm = "tokens";
        private int m_LastExpire = 0;

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AuthConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(connectionString, "Auth", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IAuthenticationData"; }
        }

        public void Dispose()
        {
        }

        #region IAuthenticationData Members

        public AuthData Get(UUID principalID, string authType)
        {
            List<string> query = GD.Query (new string[2] { "UUID", "accountType" }, new object[2] { principalID.ToString (), authType }, m_realm, "*");
            AuthData data = null;
            for (int i = 0; i < query.Count; i += 5)
            {
                data = new AuthData();
                data.PrincipalID = UUID.Parse(query[i]);
                data.PasswordHash = query[i + 1];
                data.PasswordSalt = query[i + 2];
                data.AccountType = query[i + 3];
            }
            return data;
        }

        public bool Store(AuthData data)
        {
            GD.Delete (m_realm, new string[1] {"UUID"}, new object[1] { data.PrincipalID });
            return GD.Insert (m_realm, new string[] { "UUID", "passwordHash", "passwordSalt",
                "accountType" }, new object[] { data.PrincipalID, 
                    data.PasswordHash, data.PasswordSalt, data.AccountType });
        }

        public bool SetDataItem(UUID principalID, string item, string value)
        {
            return GD.Update(m_realm, new object[1] { value }, new string[1] { item },
                new string[1] { "UUID" }, new object[1] { principalID });
        }

        public bool Delete (UUID principalID, string authType)
        {
            return GD.Delete (m_realm, new string[2] { "UUID", "accountType" }, new object[2] { principalID, authType });
        }

        public bool SetToken(UUID principalID, string token, int lifetime)
        {
            if (System.Environment.TickCount - m_LastExpire > 30000)
                DoExpire();
            return GD.DirectReplace(m_tokensrealm, new string[] { "UUID", "token", "validity" }, new object[3] { "'" + principalID + "'", "'" + token + "'", GD.FormatDateTimeString(lifetime) });
        }

        public bool CheckToken(UUID principalID, string token, int lifetime)
        {
            if (System.Environment.TickCount - m_LastExpire > 30000)
                DoExpire();
            return GD.Update(m_tokensrealm, new object[] { GD.FormatDateTimeString(lifetime) }, new string[] { "validity" },
                new string[] { "UUID", "token", "validity" }, new object[3] { principalID, token, GD.FormatDateTimeString(0) });
        }

        private void DoExpire()
        {
            GD.DeleteByTime(m_tokensrealm, "validity");

            m_LastExpire = System.Environment.TickCount;
        }

        #endregion
    }
}
