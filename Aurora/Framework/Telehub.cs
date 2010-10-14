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

        public List<Vector3> SpawnPos = new List<Vector3>();
        public string Name = "";
        public string ObjectUUID = "";

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
            SpawnPos = BuildToList(KVP["Spawns"].ToString());
            Name = KVP["Name"].ToString();
            ObjectUUID = KVP["ObjectUUID"].ToString();
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
            Telehub["Spawns"] = BuildFromList(SpawnPos);
            Telehub["ObjectUUID"] = ObjectUUID;
            Telehub["Name"] = Name;
            return Telehub;
        }

        public string BuildFromList(List<Vector3> SpawnPos)
        {
            string retVal = "";
            foreach (Vector3 Pos in SpawnPos)
            {
                retVal += Pos.ToString() + "\n";
            }
            return retVal;
        }

        public List<Vector3> BuildToList(string SpawnPos)
        {
            if (SpawnPos == "" || SpawnPos == " ")
                return new List<Vector3>();
            List<Vector3> retVal = new List<Vector3>();
            foreach (string Pos in SpawnPos.Split('\n'))
            {
                if (Pos == "")
                    continue;
                retVal.Add(Vector3.Parse(Pos));
            }
            return retVal;
        }
    }
}
