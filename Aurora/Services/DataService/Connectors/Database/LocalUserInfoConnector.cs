using System;
using System.Collections.Generic;
using System.Reflection;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using Nini.Config;
using log4net;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace Aurora.Services.DataService
{
    public class LocalUserInfoConnector : IAgentInfoConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
		private IGenericData GD = null;
        private string m_realm = "userinfo";
        protected bool m_allowDuplicatePresences = true;
        protected bool m_checkLastSeen = true;

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
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
                GD.ConnectToDatabase(connectionString, "UserInfo", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IAgentInfoConnector"; }
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

        public UserInfo Get(string userID)
        {
            List<string> query = GD.Query("UserID", userID, m_realm, "*");
            UserInfo user = new UserInfo();
            user.UserID = query[0];
            user.CurrentRegionID = UUID.Parse(query[1]);
            //users[i / 8].SessionID = UUID.Parse(query[2]);
            //Check LastSeen
            if (m_checkLastSeen && (new DateTime(long.Parse(query[3])).AddHours(1) < DateTime.Now))
            {
                m_log.Warn("[UserInfoService]: Found a user (" + user.UserID + ") that was not seen within the last hour! Logging them out.");
                return null;
            }
            user.IsOnline = query[4] == "1" ? true : false;
            user.LastLogin = new DateTime(long.Parse(query[5]));
            user.LastLogout = new DateTime(long.Parse(query[6]));
            user.Info = (OSDMap)OSDParser.DeserializeJson(query[7]);
            return user;
        }

        #endregion
    }
}
