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
    }
}
