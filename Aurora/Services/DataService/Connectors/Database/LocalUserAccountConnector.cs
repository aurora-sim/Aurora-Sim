using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using Nini.Config;
using OpenSim.Data;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class LocalUserAccountConnector : IUserAccountData
	{
		private IGenericData GD = null;
        private string m_realm = "useraccounts";

        public void Initialize(IGenericData GenericData, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if(source.Configs["AuroraConnectors"].GetString("AbuseReportsConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(connectionString);

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IUserAccountData"; }
        }

        public void Dispose()
        {
        }

        public UserAccountData[] Get(string[] fields, string[] values)
        {
            List<string> query = GD.Query(fields, values, m_realm, "*");
            List<UserAccountData> list = new List<UserAccountData>();

            ParseQuery(query, ref list);

            return list.ToArray();
        }

        private void ParseQuery(List<string> query, ref List<UserAccountData> list)
        {
            for (int i = 0; i < query.Count; i += 10)
            {
                UserAccountData data = new UserAccountData();
                data.PrincipalID = UUID.Parse(query[i + 0]);
                data.ScopeID = UUID.Parse(query[i + 1]);
                data.FirstName = query[i + 2];
                data.LastName = query[i + 3];
                data.Data = new Dictionary<string, string>();
                data.Data["Email"] = query[i + 4];
                data.Data["ServiceURLs"] = query[i + 5];
                data.Data["Created"] = query[i + 6];
                data.Data["UserLevel"] = query[i + 7];
                data.Data["UserFlags"] = query[i + 8];
                data.Data["UserTitle"] = query[i + 9];
                list.Add(data);
            }
        }

        public bool Store(UserAccountData data)
        {
            return GD.Replace(m_realm, new string[] { "PrincipalID", "ScopeID", "FirstName",
                "LastName", "Email", "ServiceURLs", "Created", "UserLevel", "UserFlags", "UserTitle"}, new object[]{
                data.PrincipalID, data.ScopeID, data.FirstName, data.LastName, data.Data["Email"],
                data.Data["ServiceURLs"],data.Data["Created"],data.Data["UserLevel"],data.Data["UserFlags"],data.Data["UserTitle"]});
        }

        public bool Delete(string field, string val)
        {
            return true;
        }

        public UserAccountData[] GetUsers(UUID scopeID, string query)
        {
            List<UserAccountData> data = new List<UserAccountData>();

            string[] words = query.Split(new char[] { ' ' });

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length < 3)
                {
                    if (i != words.Length - 1)
                        Array.Copy(words, i + 1, words, i, words.Length - i - 1);
                    Array.Resize(ref words, words.Length - 1);
                }
            }

            if (words.Length == 0)
                return new UserAccountData[0];

            if (words.Length > 2)
                return new UserAccountData[0];

            List<string> retVal;
            if (words.Length == 1)
                retVal = GD.Query("(ScopeID=?ScopeID or ScopeID='00000000-0000-0000-0000-000000000000') and (FirstName like '% " + words[0] + " %' or LastName like '%" + words[0] + "%')", m_realm, "*");
            else
                retVal = GD.Query("(ScopeID=?ScopeID or ScopeID='00000000-0000-0000-0000-000000000000') and (FirstName like '% " + words[0] + "%' or LastName like '%" + words[0] + "%')", m_realm, "*");

            ParseQuery(retVal, ref data);

            return data.ToArray();
        }
    }
}
