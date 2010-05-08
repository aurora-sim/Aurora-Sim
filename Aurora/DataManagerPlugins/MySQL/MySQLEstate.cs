using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Aurora.Framework;
using OpenSim.Framework;
using OpenMetaverse;
using MySql.Data.MySqlClient;

namespace Aurora.DataManager.MySQL
{
    public class MySQLEstate : MySQLDataLoader, IEstateData
    {
        #region IEstateData Members

        private FieldInfo[] m_Fields;
        private Dictionary<string, FieldInfo> m_FieldMap =
                new Dictionary<string, FieldInfo>();
        
        public OpenSim.Framework.EstateSettings LoadEstateSettings(OpenMetaverse.UUID regionID, bool create)
        {
            string sql = "select EstateID from estate_map where RegionID = '"+regionID.ToString()+"'";
            string EstateID = Query(sql)[0];
            if (EstateID == "" && !create)
            {
                return new EstateSettings();
            }
            else if(EstateID == "" && create)
            {
                if(m_FieldMap.Count == 0)
                {
                    Type t = typeof(EstateSettings);
                    m_Fields = t.GetFields(BindingFlags.NonPublic |
                                   BindingFlags.Instance |
                                   BindingFlags.DeclaredOnly);

                    foreach (FieldInfo f in m_Fields)
                        if (f.Name.Substring(0, 2) == "m_")
                            m_FieldMap[f.Name.Substring(2)] = f;
                }
                EstateSettings es = new EstateSettings();
                List<string> names = new List<string>(FieldList);

                names.Remove("EstateID");

                List<string> QueryResults = Query("select EstateID from estate_map ORDER BY EstateID DESC");
                if (QueryResults == null && QueryResults.Count == 0 || QueryResults[0] == "")
                {
                    EstateID = "100";
                }
                else
                    EstateID = QueryResults[0];
                if (EstateID == "0")
                    EstateID = "100";
                int estateID = Convert.ToInt32(EstateID);
                estateID++;
                EstateID = estateID.ToString();
                MySqlCommand cmd;
                MySqlConnection dbcon = GetLockedConnection();
                cmd = (MySqlCommand)dbcon.CreateCommand();
                cmd.CommandText = "insert into estate_settings (EstateID," + String.Join(",", names.ToArray()) + ") values (" + EstateID + ", :" + String.Join(", :", names.ToArray()) + ")";
                cmd.Parameters.Clear();

                foreach (string name in FieldList)
                {
                    if (m_FieldMap[name].GetValue(es) is bool)
                    {
                        if ((bool)m_FieldMap[name].GetValue(es))
                            cmd.Parameters.Add(":"+name, "1");
                        else
                            cmd.Parameters.Add(":"+name, "0");
                    }
                    else
                    {
                        cmd.Parameters.Add(":"+name, m_FieldMap[name].GetValue(es).ToString());
                    }
                }
                ExecuteCommand(cmd);
                Insert("estate_map", new string[] { regionID.ToString(), EstateID.ToString() });
                
            }
            return LoadEstateSettings(Convert.ToInt32(EstateID));
        }

        private string[] FieldList
        {
            get { return new List<string>(m_FieldMap.Keys).ToArray(); }
        }

        public bool ExecuteCommand(MySqlCommand query)
        {
            IDataReader reader;

            using (reader = query.ExecuteReader())
            {
                reader.Close();
                reader.Dispose();
            }
            return true;
        }

        public bool ExecuteCommand(string query)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;

            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    reader.Close();
                    reader.Dispose();
                    result.Dispose();
                }
            }
            return true;
        }

        public List<string> Query(MySqlCommand query)
        {
            List<string> RetVal = new List<string>();
            IDataReader reader;

            using (reader = query.ExecuteReader())
            {
                try
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            RetVal.Add(reader.GetString(i));
                        }
                    }
                    if (RetVal.Count == 0)
                    {
                        RetVal.Add("");
                        return RetVal;
                    }
                    else
                    {
                        return RetVal;
                    }
                }
                finally
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
            return RetVal;
        }

        public List<string> Query(string sql)
        {
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            List<string> RetVal = new List<string>();
            using (result = Query(sql, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                RetVal.Add(reader.GetString(i));
                            }
                        }
                        if (RetVal.Count == 0)
                        {
                            RetVal.Add("");
                            return RetVal;
                        }
                        else
                        {
                            return RetVal;
                        }
                    }
                    finally
                    {
                        reader.Close();
                        reader.Dispose();
                        result.Dispose();
                    }
                }
            }
        }

        public OpenSim.Framework.EstateSettings LoadEstateSettings(int estateID)
        {
            string sql = "select * from estate_settings where EstateID = " + estateID.ToString();
            List<string> results = Query(sql);
            EstateSettings settings = new EstateSettings();
            settings.AbuseEmail = results[21];
            settings.AbuseEmailToEstateOwner = results[2] == "1";
            settings.AllowDirectTeleport = results[13] == "1";
            settings.AllowVoice = results[9] == "1";
            settings.BillableFactor = Convert.ToInt32(results[19]);
            settings.BlockDwell = results[7] == "1";
            settings.DenyAnonymous = results[3] == "1";
            settings.DenyIdentified = results[8] == "1";
            settings.DenyMinors = results[23] == "1";
            settings.DenyTransacted = results[6] == "1";
            settings.EstateName = results[1];
            settings.EstateOwner = new OpenMetaverse.UUID(results[22]);
            settings.EstateSkipScripts = results[18] == "1";
            settings.FixedSun = results[5] == "1";
            settings.ParentEstateID = Convert.ToUInt32(results[16]);
            settings.PricePerMeter = Convert.ToInt32(results[11]);
            settings.PublicAccess = results[20] == "1";
            settings.RedirectGridX = Convert.ToInt32(results[14]);
            settings.RedirectGridY = Convert.ToInt32(results[15]);
            settings.ResetHomeOnTeleport = results[4] == "1";
            settings.SunPosition = Convert.ToInt32(results[17]);
            settings.TaxFree = results[12] == "1";
            settings.UseGlobalTime = results[10] == "1";
            settings.EstateID = Convert.ToUInt32(results[0]);

            settings.EstateAccess = LoadUUIDList(settings.EstateID, "estate_users");
            LoadBanList(settings);
            settings.EstateGroups = LoadUUIDList(settings.EstateID, "estate_groups");
            settings.EstateManagers = LoadUUIDList(settings.EstateID, "estate_managers");
            settings.OnSave += SaveEstateSettings;
            return settings;
        }

        private void LoadBanList(EstateSettings es)
        {
            es.ClearBans();

            MySqlCommand cmd;
            MySqlConnection dbcon = GetLockedConnection();
            cmd = (MySqlCommand)dbcon.CreateCommand();
                
            cmd.CommandText = "select bannedUUID from estateban where EstateID = :EstateID";
            cmd.Parameters.Add(":EstateID", es.EstateID);

            IDataReader reader = reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                EstateBan eb = new EstateBan();

                UUID uuid = new UUID();
                UUID.TryParse(reader["bannedUUID"].ToString(), out uuid);

                eb.BannedUserID = uuid;
                eb.BannedHostAddress = "0.0.0.0";
                eb.BannedHostIPMask = "0.0.0.0";
                es.AddBan(eb);
            }
            reader.Close();
        }

        UUID[] LoadUUIDList(uint EstateID, string table)
        {
            List<UUID> uuids = new List<UUID>();

            MySqlCommand cmd;
            MySqlConnection dbcon = GetLockedConnection();
            cmd = (MySqlCommand)dbcon.CreateCommand();
                
            cmd.CommandText = "select uuid from " + table + " where EstateID = :EstateID";
            cmd.Parameters.Add(":EstateID", EstateID);

            IDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                // EstateBan eb = new EstateBan();

                UUID uuid = new UUID();
                UUID.TryParse(reader["uuid"].ToString(), out uuid);

                uuids.Add(uuid);
            }
            reader.Close();

            return uuids.ToArray();
        }

        private void SaveBanList(EstateSettings es)
        {
            MySqlCommand cmd;
            MySqlConnection dbcon = GetLockedConnection();
            cmd = (MySqlCommand)dbcon.CreateCommand();
                
            cmd.CommandText = "delete from estateban where EstateID = :EstateID";
            cmd.Parameters.Add(":EstateID", es.EstateID.ToString());

            Query(cmd);
            cmd.Parameters.Clear();

            cmd.CommandText = "insert into estateban (EstateID, bannedUUID, bannedIp, bannedIpHostMask, bannedNameMask) values ( :EstateID, :bannedUUID, '', '', '' )";

            foreach (EstateBan b in es.EstateBans)
            {
                cmd.Parameters.Add(":EstateID", es.EstateID.ToString());
                cmd.Parameters.Add(":bannedUUID", b.BannedUserID.ToString());

                Query(cmd);
                cmd.Parameters.Clear();
            }
        }

        void SaveUUIDList(uint EstateID, string table, UUID[] data)
        {
            MySqlCommand cmd;
            MySqlConnection dbcon = GetLockedConnection();
            cmd = (MySqlCommand)dbcon.CreateCommand();
                
            cmd.CommandText = "delete from " + table + " where EstateID = :EstateID";
            cmd.Parameters.Add(":EstateID", EstateID.ToString());

            Query(cmd);

            cmd.Parameters.Clear();

            cmd.CommandText = "insert into " + table + " (EstateID, uuid) values ( :EstateID, :uuid )";

            foreach (UUID uuid in data)
            {
                cmd.Parameters.Add(":EstateID", EstateID.ToString());
                cmd.Parameters.Add(":uuid", uuid.ToString());

                Query(cmd);
                cmd.Parameters.Clear();
            }
        }

        public bool StoreEstateSettings(OpenSim.Framework.EstateSettings es)
        {
            if (m_FieldMap.Count == 0)
            {
                Type t = typeof(EstateSettings);
                m_Fields = t.GetFields(BindingFlags.NonPublic |
                               BindingFlags.Instance |
                               BindingFlags.DeclaredOnly);

                foreach (FieldInfo f in m_Fields)
                    if (f.Name.Substring(0, 2) == "m_")
                        m_FieldMap[f.Name.Substring(2)] = f;
            }
            List<string> fields = new List<string>(FieldList);
            fields.Remove("EstateID");

            List<string> terms = new List<string>();

            foreach (string f in fields)
                terms.Add(f + " = :" + f);

            string sql = "update estate_settings set " + String.Join(", ", terms.ToArray()) + " where EstateID = :EstateID";

            MySqlCommand cmd;
            MySqlConnection dbcon = GetLockedConnection();
            cmd = (MySqlCommand)dbcon.CreateCommand();

            cmd.CommandText = sql;

            foreach (string name in FieldList)
            {
                if (m_FieldMap[name].GetValue(es) is bool)
                {
                    if ((bool)m_FieldMap[name].GetValue(es))
                        cmd.Parameters.Add(":" + name, "1");
                    else
                        cmd.Parameters.Add(":" + name, "0");
                }
                else
                {
                    cmd.Parameters.Add(":" + name, m_FieldMap[name].GetValue(es).ToString());
                }
            }

            Query(cmd);

            SaveBanList(es);
            SaveUUIDList(es.EstateID, "estate_managers", es.EstateManagers);
            SaveUUIDList(es.EstateID, "estate_users", es.EstateAccess);
            SaveUUIDList(es.EstateID, "estate_groups", es.EstateGroups);
            return true;
        }

        public void SaveEstateSettings(OpenSim.Framework.EstateSettings es)
        {
            if (m_FieldMap.Count == 0)
            {
                Type t = typeof(EstateSettings);
                m_Fields = t.GetFields(BindingFlags.NonPublic |
                               BindingFlags.Instance |
                               BindingFlags.DeclaredOnly);

                foreach (FieldInfo f in m_Fields)
                    if (f.Name.Substring(0, 2) == "m_")
                        m_FieldMap[f.Name.Substring(2)] = f;
            }
            List<string> fields = new List<string>(FieldList);
            fields.Remove("EstateID");

            List<string> terms = new List<string>();

            foreach (string f in fields)
                terms.Add(f + " = :" + f);

            string sql = "update estate_settings set " + String.Join(", ", terms.ToArray()) + " where EstateID = :EstateID";

            MySqlCommand cmd;
            MySqlConnection dbcon = GetLockedConnection();
            cmd = (MySqlCommand)dbcon.CreateCommand();

            cmd.CommandText = sql;

            foreach (string name in FieldList)
            {
                if (m_FieldMap[name].GetValue(es) is bool)
                {
                    if ((bool)m_FieldMap[name].GetValue(es))
                        cmd.Parameters.Add(":" + name, "1");
                    else
                        cmd.Parameters.Add(":" + name, "0");
                }
                else
                {
                    cmd.Parameters.Add(":" + name, m_FieldMap[name].GetValue(es).ToString());
                }
            }

            Query(cmd);

            SaveBanList(es);
            SaveUUIDList(es.EstateID, "estate_managers", es.EstateManagers);
            SaveUUIDList(es.EstateID, "estate_users", es.EstateAccess);
            SaveUUIDList(es.EstateID, "estate_groups", es.EstateGroups);
        }

        public List<int> GetEstates(string search)
        {
            List<int> result = new List<int>();

            string sql = "select EstateID from estate_settings where EstateName = '" + search + "'";

            MySqlCommand cmd;
            MySqlConnection dbcon = GetLockedConnection();
            cmd = (MySqlCommand)dbcon.CreateCommand();

            cmd.CommandText = sql;
            IDataReader r = cmd.ExecuteReader();

            while (r.Read())
            {
                result.Add(Convert.ToInt32(r["EstateID"]));
            }
            r.Close();

            return result;
        }

        public bool LinkRegion(OpenMetaverse.UUID regionID, int estateID)
        {
            MySqlCommand cmd;
            MySqlConnection dbcon = GetLockedConnection();
            cmd = (MySqlCommand)dbcon.CreateCommand();

            cmd.CommandText = "insert into estate_map values (:RegionID, :EstateID)";
            cmd.Parameters.Add(":RegionID", regionID.ToString());
            cmd.Parameters.Add(":EstateID", estateID.ToString());

            if (Query(cmd).Count == 0)
                return false;

            return true;
        }

        public List<OpenMetaverse.UUID> GetRegions(int estateID)
        {
            string sql = "select RegionID from estate_map where EstateID = '" + estateID.ToString() + "'";
            List<string> RegionIDs = Query(sql);
            List<UUID> regions = new List<UUID>();
            foreach(string RegionID in RegionIDs)
                regions.Add(new UUID(RegionID));
            return regions;
        }

        public bool DeleteEstate(int estateID)
        {
            MySqlCommand cmd;
            MySqlConnection dbcon = GetLockedConnection();
            cmd = (MySqlCommand)dbcon.CreateCommand();

            cmd.CommandText = "delete from estateban where EstateID = :EstateID";
            cmd.Parameters.Add(":EstateID", estateID.ToString());

            Query(cmd);

            cmd = (MySqlCommand)dbcon.CreateCommand();

            cmd.CommandText = "delete from estate_groups where EstateID = :EstateID";
            cmd.Parameters.Add(":EstateID", estateID.ToString());

            Query(cmd);

            cmd = (MySqlCommand)dbcon.CreateCommand();

            cmd.CommandText = "delete from estate_managers where EstateID = :EstateID";
            cmd.Parameters.Add(":EstateID", estateID.ToString());

            Query(cmd);

            cmd = (MySqlCommand)dbcon.CreateCommand();

            cmd.CommandText = "delete from estate_map where EstateID = :EstateID";
            cmd.Parameters.Add(":EstateID", estateID.ToString());

            Query(cmd);

            cmd = (MySqlCommand)dbcon.CreateCommand();

            cmd.CommandText = "delete from estate_settings where EstateID = :EstateID";
            cmd.Parameters.Add(":EstateID", estateID.ToString());

            Query(cmd);

            cmd = (MySqlCommand)dbcon.CreateCommand();

            cmd.CommandText = "delete from estate_users where EstateID = :EstateID";
            cmd.Parameters.Add(":EstateID", estateID.ToString());

            Query(cmd);
            return true;
        }

        #endregion
    }
}
