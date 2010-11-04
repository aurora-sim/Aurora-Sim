using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using log4net;
using Mono.Data.Sqlite;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Data.SQLite
{
    public class SQLitePresence : SQLiteGenericTableHandler<PresenceData>,
            IPresenceData
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SQLitePresence(string connectionString, string realm) :
                base(connectionString, realm, "Presence")
        {
        }

        public PresenceData Get(UUID sessionID)
        {
            PresenceData[] ret = Get("SessionID",
                    sessionID.ToString());

            if (ret.Length == 0)
                return null;

            return ret[0];
        }

        public void LogoutRegionAgents(UUID regionID)
        {
            SqliteCommand cmd = new SqliteCommand();

            cmd.CommandText = String.Format("delete from {0} where 'RegionID'=:RegionID", m_Realm);

            cmd.Parameters.AddWithValue(":RegionID", regionID.ToString());

            ExecuteNonQuery(cmd);
        }

        public bool ReportAgent(UUID sessionID, UUID regionID)
        {
            PresenceData[] pd = Get("SessionID", sessionID.ToString());
            if (pd.Length == 0)
                return false;

            SqliteCommand cmd = new SqliteCommand();

            cmd.CommandText = String.Format("update {0} set RegionID=:RegionID, LastSeen=:LastSeen where 'SessionID'=:SessionID", m_Realm);

            cmd.Parameters.AddWithValue(":SessionID", sessionID.ToString());
            cmd.Parameters.AddWithValue(":LastSeen", Util.UnixTimeSinceEpoch());
            cmd.Parameters.AddWithValue(":RegionID", regionID.ToString());

            if (ExecuteNonQuery(cmd) == 0)
                return false;

            return true;
        }
    }
}
