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
        public ObjectMediaURLInfo[] getObjectMediaInfo(string objectID)
        {
            ObjectMediaURLInfo[] infos = new ObjectMediaURLInfo[6];
            ObjectMediaURLInfo info = new ObjectMediaURLInfo();

            List<string> data = Query("objectUUID", objectID, "assetMediaURL", "*");
            if (data.Count == 1)
                return infos;
            infos[1] = info;
            /*int a = 0;
            for (int i = 0; i < data.Count; ++i)
            {
                info.alt_image_enable = data[a + 2];
                info.auto_loop = Convert.ToInt32(data[a + 3]) == 1;
                info.auto_play = Convert.ToInt32(data[a + 4]) == 1;
                info.auto_scale = Convert.ToInt32(data[a + 5]) == 1;
                info.auto_zoom = Convert.ToInt32(data[a + 6]) == 1;
                info.controls = Convert.ToInt32(data[a + 7]);
                info.current_url = data[a + 8];
                info.first_click_interact = Convert.ToInt32(data[a + 9]) == 1;
                info.height_pixels = Convert.ToInt32(data[a + 10]);
                info.home_url = data[a + 11];
                info.perms_control = Convert.ToInt32(data[a + 12]);
                info.perms_interact = Convert.ToInt32(data[a + 13]);
                info.whitelist = data[a + 14];
                info.whitelist_enable = Convert.ToInt32(data[a + 15]) == 1;
                info.width_pixels = Convert.ToInt32(data[a + 16]);
                info.object_media_version = data[a + 17];
                a++;
                if (i == 18)
                    a = 18;
                if (i == 36)
                    a = 36;
                if (i == 54)
                    a = 54;
                if (i == 72)
                    a = 72;
                if (i == 90)
                    a = 90;
            }*/
            return infos;
        }
    }
}
