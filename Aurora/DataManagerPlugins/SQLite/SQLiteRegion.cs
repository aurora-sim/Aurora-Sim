using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Threading;
using Aurora.DataManager;
using Mono.Data.SqliteClient;

namespace Aurora.DataManager.SQLite
{
    public class SQLiteRegion: SQLiteLoader, IRegionData
    {

        public Dictionary<string, string> GetRegionHidden()
        {
            SqliteCommand cmd = new SqliteCommand();
            string query = "";
            query = String.Format("select RegionHandle,regionName from auroraregions where hidden = '{0}'", "1");
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            Dictionary<string, string> retval = new Dictionary<string, string>();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i = i + 2)
                {
                     retval.Add(reader.GetValue(i).ToString(), reader.GetValue(i + 1).ToString());
                }
            }
            return retval;
        }

        public string AbuseReports()
        {
            string query = "SELECT Number FROM reports ORDER BY Number DESC";
            SqliteCommand cmd = new SqliteCommand();
            cmd.CommandText = query;
            IDataReader reader = GetReader(cmd);
            if (reader.Read())
            {
                return reader.GetString(0);
            }
            else
            {
                return "";
            }
        }
    }
}
