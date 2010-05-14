using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class Telehub
    {
        public string RegionID = UUID.Zero.ToString();
        public float RegionLocX = 0;
        public float RegionLocY = 0;
        public float TelehubX = 0;
        public float TelehubY = 0;
        public float TelehubZ = 0;

        public Telehub() { }

        public Telehub(Dictionary<string, object> KVP)
        {
            RegionID = KVP["RegionID"].ToString();
            RegionLocX = float.Parse(KVP["RegionLocX"].ToString());
            RegionLocY = float.Parse(KVP["RegionLocY"].ToString());
            TelehubX = float.Parse(KVP["TelehubX"].ToString());
            TelehubY = float.Parse(KVP["TelehubY"].ToString());
            TelehubZ = float.Parse(KVP["TelehubZ"].ToString());
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> Telehub = new Dictionary<string, object>();
            Telehub["RegionID"] = RegionID;
            Telehub["RegionLocX"] = RegionLocX;
            Telehub["RegionLocY"] = RegionLocY;
            Telehub["TelehubX"] = TelehubX;
            Telehub["TelehubY"] = TelehubY;
            Telehub["TelehubZ"] = TelehubZ;
            return Telehub;
        }
    }
}
