using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class AbuseReport
    {
        public object Category;
        public string ReporterName;
        public string ObjectName;
        public UUID ObjectUUID;
        public string AbuserName;
        public string AbuseLocation;
        public string AbuseDetails;
        public string ObjectPosition;
        public string RegionName;
        public UUID ScreenshotID;
        public string AbuseSummary;
        public int Number;
        public string AssignedTo;
        public bool Active;
        public bool Checked;
        public string Notes;
        public AbuseReport()
        {
        }
        public AbuseReport(Dictionary<string, object> KVP)
        {
            Category = KVP["Category"];
            ScreenshotID = new UUID(KVP["ScreenshotID"].ToString());
            ReporterName = KVP["ReporterName"].ToString();
            ObjectName = KVP["ObjectName"].ToString();
            ObjectUUID = new UUID(KVP["ObjectUUID"].ToString());
            AbuserName = KVP["AbuserName"].ToString();
            AbuseLocation = KVP["AbuseLocation"].ToString();
            AbuseDetails = KVP["AbuseDetails"].ToString();
            ObjectPosition = KVP["AbusePosition"].ToString();
            RegionName = KVP["RegionName"].ToString();
            AbuseSummary = KVP["AbuseSummary"].ToString();
            Number = int.Parse(KVP["Number"].ToString());
            AssignedTo = KVP["AssignedTo"].ToString();
            Active = bool.Parse(KVP["Active"].ToString());
            Checked = bool.Parse(KVP["Checked"].ToString());
            Notes = KVP["Notes"].ToString();
        }
        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> RetVal = new Dictionary<string, object>();
            RetVal["Category"] = Category;
            RetVal["ReporterName"] = ReporterName;
            RetVal["ObjectName"] = ObjectName;
            RetVal["ObjectUUID"] = ObjectUUID;
            RetVal["AbuserName"] = AbuserName;
            RetVal["AbuseLocation"] = AbuseLocation;
            RetVal["AbuseDetails"] = AbuseDetails;
            RetVal["AbusePosition"] = ObjectPosition;
            RetVal["RegionName"] = RegionName;
            RetVal["AbuseSummary"] = AbuseSummary;
            RetVal["Number"] = Number;
            RetVal["AssignedTo"] = AssignedTo;
            RetVal["Active"] = Active;
            RetVal["Checked"] = Checked;
            RetVal["Notes"] = Notes;
            RetVal["ScreenshotID"] = ScreenshotID;
            return RetVal;
        }
    }
}
