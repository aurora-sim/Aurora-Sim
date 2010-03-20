using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using MySql.Data.MySqlClient;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.DataManager.MySQL
{
    public class MySQLRegion : MySQLDataLoader, IRegionData
    {
        public Dictionary<string, string> GetRegionHidden()
        {

            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            string sqlstatement = "select RegionHandle,regionName from auroraregions where hidden = '1'";
            using (result = Query(sqlstatement, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        Dictionary<string, string> row = getRegionHidden(reader);
                        return row;
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
            }
        }
        public Dictionary<string, string> getRegionHidden(IDataReader reader)
        {
            Dictionary<string, string> retval = new Dictionary<string, string>();
            try
            {
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i = i + 2)
                    {
                        retval.Add(reader.GetValue(i).ToString(), reader.GetValue(i + 1).ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                ex = new Exception();
            }
            return retval;
        }
        public string AbuseReports()
        {
            string query = "SELECT Number FROM reports ORDER BY Number DESC";
            MySqlConnection dbcon = GetLockedConnection();
            IDbCommand result;
            IDataReader reader;
            using (result = Query(query, new Dictionary<string, object>(), dbcon))
            {
                using (reader = result.ExecuteReader())
                {
                    try
                    {
                        if (reader.Read())
                            return reader.GetString(0);
                        else
                            return "";
                    }
                    finally{}
                }
            }
        }
        
    }
}
