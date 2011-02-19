using System;
using System.Collections.Generic;
using System.Reflection;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using Nini.Config;
using log4net;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace Aurora.Services.DataService
{
    public interface IUserInfoConnector : IAuroraDataPlugin
    {
        bool Set(UserInfo info);
        UserInfo[] Get(string userID);
    }
    public class LocalUserInfoConnector : IUserInfoConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
		private IGenericData GD = null;
        private string m_realm = "userinfo";
        protected bool m_allowDuplicatePresences = true;
        protected bool m_checkLastSeen = true;

        public void Initialize(IGenericData GenericData, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if(source.Configs["AuroraConnectors"].GetString("UserInfoConnector", "LocalConnector") == "LocalConnector")
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
                GD.ConnectToDatabase(connectionString);

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IUserInfoData"; }
        }

        public void Dispose()
        {
        }

        #region IUserInfoConnector Members

        public bool Set(UserInfo info)
        {
            string[] keys = new string[8];
            keys[0] = "UserID";
            keys[1] = "RegionID";
            keys[2] = "SessionID";
            keys[3] = "LastSeen";
            keys[4] = "IsOnline";
            keys[5] = "LastLogin";
            keys[6] = "LastLogout";
            keys[7] = "Info";
            object[] values = new object[8];
            values[0] = info.UserID;
            values[1] = info.CurrentRegionID;
            values[2] = UUID.Zero;
            values[3] =  DateTime.Now.ToBinary(); //Convert to binary so that it can be converted easily
            values[4] = info.IsOnline ? 1 : 0;
            values[5] = info.LastLogin.ToBinary();
            values[6] = info.LastLogout.ToBinary();
            values[7] = OSDParser.SerializeJsonString(info.Info);
            return GD.Replace(m_realm, keys, values);
        }

        public UserInfo[] Get(string userID)
        {
            List<string> query = GD.Query("UserID", userID, m_realm, "*");
            UserInfo[] users = new UserInfo[query.Count / 8];
            for (int i = 0; i < query.Count; i += 8)
            {
                //Build each user now
                users[i / 8] = new UserInfo();
                users[i / 8].UserID = query[i];
                users[i / 8].CurrentRegionID = UUID.Parse(query[i+1]);
                //users[i / 8].SessionID = UUID.Parse(query[i+2]);
                //Check LastSeen
                if (m_checkLastSeen && (new DateTime(long.Parse(query[i + 3])).AddHours(1) < DateTime.Now))
                {
                    m_log.Warn("[UserInfoService]: Found a user (" + users[i / 8].UserID + ") that was not seen within the last hour! Logging them out.");
                    return null;
                }
                users[i / 8].IsOnline = query[i+4] == "1" ? true : false;
                users[i / 8].LastLogin = new DateTime(long.Parse(query[i+5]));
                users[i / 8].LastLogout = new DateTime(long.Parse(query[i+6]));
                users[i / 8].Info = (OSDMap)OSDParser.DeserializeJson(query[i+7]);
            }
            return users;
        }

        #endregion
    }
}
