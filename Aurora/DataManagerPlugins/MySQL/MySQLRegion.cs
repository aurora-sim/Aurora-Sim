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
                        result.Dispose();
                        reader.Dispose();
                        CloseDatabase(dbcon);
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
            string query = "SELECT ReportNumber FROM reports ORDER BY ReportNumber DESC";
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
                    finally
                    {
                        reader.Close();
                        result.Dispose();
                        reader.Dispose();
                        CloseDatabase(dbcon);
                    }
                }
            }
        }
        public ObjectMediaURLInfo getObjectMediaInfo(string objectID, int side)
        {
            ObjectMediaURLInfo info = new ObjectMediaURLInfo();
            List<string> data = Query(new string[] {"objectUUID","side"}, new string[] {objectID,side.ToString()}, "assetMediaURL", "*");
            if (data.Count == 1)
                return null;
            for (int i = 0; i < data.Count; ++i)
            {
                if (i == 2)
                    info.alt_image_enable = data[i];
                if (i == 3)
                    info.auto_loop = Convert.ToInt32(data[i]) == 1;
                if (i == 4)
                    info.auto_play = Convert.ToInt32(data[i]) == 1;
                if (i == 5)
                    info.auto_scale = Convert.ToInt32(data[i]) == 1;
                if (i == 6)
                    info.auto_zoom = Convert.ToInt32(data[i]) == 1;
                if (i == 7)
                    info.controls = Convert.ToInt32(data[i]);
                if (i == 8)
                    info.current_url = data[i];
                if (i == 9)
                    info.first_click_interact = Convert.ToInt32(data[i]) == 1;
                if (i == 10)
                    info.height_pixels = Convert.ToInt32(data[i]);
                if (i == 11)
                    info.home_url = data[i];
                if (i == 12)
                    info.perms_control = Convert.ToInt32(data[i]);
                if (i == 13)
                    info.perms_interact = Convert.ToInt32(data[i]);
                if (i == 14)
                    info.whitelist = data[i];
                if (i == 15)
                    info.whitelist_enable = Convert.ToInt32(data[i]) == 1;
                if (i == 16)
                    info.width_pixels = Convert.ToInt32(data[i]);
                if (i == 17)
                    info.object_media_version = data[i];
            }
            return info;
        }
        
        public bool GetIsRegionMature(string region)
        {
        	string query = "SELECT isMature FROM auroraregions where regionUUID = '"+region+"'";
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
                            return reader.GetBoolean(0);
                        else
                            return true;
                    }
                    finally
                    {
                        reader.Close();
                        result.Dispose();
                        reader.Dispose();
                        CloseDatabase(dbcon);
                    }
                }
            }
        }

        public AbuseReport GetAbuseReport(int formNumber)
        {
            AbuseReport report = new AbuseReport();
            List<string> Reports = Query("ReportNumber", formNumber.ToString(), "abusereports");
            int i = 0;
            foreach (string part in Reports)
            {
                if (i == 0)
                    report.Category = part;
                if (i == 1)
                    report.Reporter = part;
                if (i == 2)
                    report.ObjectName = part;
                if (i == 3)
                    report.ObjectUUID = part;
                if (i == 4)
                    report.Abuser = part;
                if (i == 5)
                    report.Location = part;
                if (i == 6)
                    report.Details = part;
                if (i == 7)
                    report.Position = part;
                if (i == 8)
                    report.Estate = part;
                if (i == 9)
                    report.Summary = part;
                if (i == 10)
                    report.ReportNumber = part;
                if (i == 11)
                    report.AssignedTo = part;
                if (i == 12)
                    report.Active = part;
                if (i == 13)
                    report.Checked = part;
                if (i == 14)
                    report.Notes = part;
                i++;
                if (i == 15)
                    i = 0;
            }
            return report;
        }
        public OfflineMessage[] GetOfflineMessages(string agentID)
        {
            List<OfflineMessage> messages = new List<OfflineMessage>();
            List<string> Messages = Query("ToUUID", agentID, "offlinemessages", "*");
            Delete("offlinemessages", new string[] { "ToUUID" }, new string[] { agentID });
            int i = 0;
            OfflineMessage Message = new OfflineMessage();
            foreach (string part in Messages)
            {
                if (i == 0)
                    Message.FromUUID = part;
                if (i == 1)
                    Message.FromName = part;
                if (i == 2)
                    Message.ToUUID = part;
                if (i == 3)
                    Message.Message = part;
                i++;
                if (i == 4)
                {
                    i = 0;
                    messages.Add(Message);
                    Message = new OfflineMessage();
                }
            }
            return messages.ToArray();
        }

        public bool AddOfflineMessage(string fromUUID, string fromName, string toUUID, string message)
        {
            return Insert("offlinemessages", new string[] { fromUUID, fromName, toUUID, message });
        }
        public void AddLandObject(OpenSim.Framework.LandData ILandData)
        {
            OpenSim.Framework.LandData args = (OpenSim.Framework.LandData)ILandData;
            try
            {
                Delete("auroraland", new string[] { "UUID" }, new string[] { args.GlobalID.ToString() });
            }
            catch (Exception) { }
            List<string> Values = new List<string>();
            Values.Add(args.GlobalID.ToString());
            Values.Add(args.LocalID.ToString());
            Values.Add(args.MediaDesc.ToString());
            Values.Add(args.MediaSize[1].ToString());
            Values.Add(args.MediaLoop.ToString());
            Values.Add(args.MediaType.ToString());
            Values.Add(args.MediaSize[0].ToString());
            Values.Add(args.ObscureMedia.ToString());
            Values.Add(args.ObscureMusic.ToString());
            Insert("auroraland", Values.ToArray());
        }
    }
}
