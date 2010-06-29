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
        public float TelehubLocX = 0;
        public float TelehubLocY = 0;
        public float TelehubLocZ = 0;

        public float TelehubRotX = 0;
        public float TelehubRotY = 0;
        public float TelehubRotZ = 0;

        public Telehub() { }

        public Telehub(Dictionary<string, object> KVP)
        {
            RegionID = KVP["RegionID"].ToString();
            RegionLocX = float.Parse(KVP["RegionLocX"].ToString());
            RegionLocY = float.Parse(KVP["RegionLocY"].ToString());
            TelehubRotX = float.Parse(KVP["TelehubRotX"].ToString());
            TelehubRotY = float.Parse(KVP["TelehubRotY"].ToString());
            TelehubRotZ = float.Parse(KVP["TelehubRotZ"].ToString());
            TelehubLocX = float.Parse(KVP["TelehubLocX"].ToString());
            TelehubLocY = float.Parse(KVP["TelehubLocY"].ToString());
            TelehubLocZ = float.Parse(KVP["TelehubLocZ"].ToString());
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> Telehub = new Dictionary<string, object>();
            Telehub["RegionID"] = RegionID;
            Telehub["RegionLocX"] = RegionLocX;
            Telehub["RegionLocY"] = RegionLocY;
            Telehub["TelehubRotX"] = TelehubRotX;
            Telehub["TelehubRotY"] = TelehubRotY;
            Telehub["TelehubRotZ"] = TelehubRotZ;
            Telehub["TelehubLocX"] = TelehubLocX;
            Telehub["TelehubLocY"] = TelehubLocY;
            Telehub["TelehubLocZ"] = TelehubLocZ;
            return Telehub;
        }
    }
}
