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
            object[] values = new object[8];
            values[0] = info.UserID;
            values[1] = info.CurrentRegionID;
            values[2] = Util.ToUnixTime(DateTime.Now); //Convert to binary so that it can be converted easily
            values[3] = info.IsOnline ? 1 : 0;
            values[4] = Util.ToUnixTime(info.LastLogin);
            values[5] = Util.ToUnixTime(info.LastLogout);
            values[6] = OSDParser.SerializeJsonString(info.ToOSD());
            GD.Delete(m_realm, new string[1] { "UserID" }, new object[1] { info.UserID });
            return GD.Insert(m_realm, values);
        }

        public UserInfo Get(string userID)
        {
            List<string> query = GD.Query("UserID", userID, m_realm, "*");
            if (query.Count == 0)
                return null;
            UserInfo user = new UserInfo();
            user.UserID = query[0];
            user.CurrentRegionID = UUID.Parse(query[1]);
            user.IsOnline = query[3] == "1" ? true : false;
            user.LastLogin = Util.ToDateTime(int.Parse(query[4]));
            user.LastLogout = Util.ToDateTime(int.Parse(query[5]));
            UserInfo innerUser = new UserInfo();
            try
            {
                innerUser.FromOSD((OSDMap)OSDParser.DeserializeJson(query[6]));
                user.Info = innerUser.Info;
                user.CurrentLookAt = innerUser.CurrentLookAt;
                user.CurrentPosition = innerUser.CurrentPosition;
                user.CurrentRegionID = innerUser.CurrentRegionID;
                user.HomeLookAt = innerUser.HomeLookAt;
                user.HomePosition = innerUser.HomePosition;
                user.HomeRegionID = innerUser.HomeRegionID;
            }
            catch
            { 
                //Eat it!
            }

            //Check LastSeen
            if (m_checkLastSeen && user.IsOnline && (Util.ToDateTime(int.Parse(query[2])).AddHours(1) < DateTime.Now))
            {
                m_log.Warn("[UserInfoService]: Found a user (" + user.UserID + ") that was not seen within the last hour! Logging them out.");
                user.IsOnline = false;
                Set(user);
            }
            return user;
        }

        #endregion
    }
}
