using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class Telehub : IDataTransferable
    {
        /// <summary>
        /// Region UUID
        /// </summary>
        public UUID RegionID = UUID.Zero;

        /// <summary>
        /// Global region coordinates (in meters)
        /// </summary>
        public float RegionLocX = 0;
        public float RegionLocY = 0;
        /// <summary>
        /// Position of the telehub in the region
        /// </summary>
        public float TelehubLocX = 0;
        public float TelehubLocY = 0;
        public float TelehubLocZ = 0;

        /// <summary>
        /// Rotation of the av
        /// </summary>
        public float TelehubRotX = 0;
        public float TelehubRotY = 0;
        public float TelehubRotZ = 0;

        /// <summary>
        /// Positions users will spawn at in order of creation
        /// </summary>
        public List<Vector3> SpawnPos = new List<Vector3>();

        /// <summary>
        /// Name of the teleHUB object
        /// </summary>
        public string Name = "";

        /// <summary>
        /// UUID of the teleHUB object
        /// </summary>
        public UUID ObjectUUID = UUID.Zero;

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

        public override void FromOSD(OSDMap map)
        {
            RegionID = map["RegionID"].AsUUID();
            RegionLocX = (float)map["RegionLocX"].AsReal();
            RegionLocY = (float)map["RegionLocY"].AsReal();
            TelehubRotX = (float)map["TelehubRotX"].AsReal();
            TelehubRotY = (float)map["TelehubRotY"].AsReal();
            TelehubRotZ = (float)map["TelehubRotZ"].AsReal();
            TelehubLocX = (float)map["TelehubLocX"].AsReal();
            TelehubLocY = (float)map["TelehubLocY"].AsReal();
            TelehubLocZ = (float)map["TelehubLocZ"].AsReal();
            SpawnPos = BuildToList(map["Spawns"].AsString());
            Name = map["Name"].AsString();
            ObjectUUID = map["ObjectUUID"].AsUUID();
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map.Add("RegionID", OSD.FromUUID(RegionID));
            map.Add("RegionLocX", OSD.FromReal(RegionLocX));
            map.Add("RegionLocY", OSD.FromReal(RegionLocY));
            map.Add("TelehubRotX", OSD.FromReal(TelehubRotX));
            map.Add("TelehubRotY", OSD.FromReal(TelehubRotY));
            map.Add("TelehubRotZ", OSD.FromReal(TelehubRotZ));
            map.Add("TelehubLocX", OSD.FromReal(TelehubLocX));
            map.Add("TelehubLocY", OSD.FromReal(TelehubLocY));
            map.Add("TelehubLocZ", OSD.FromReal(TelehubLocZ));
            map.Add("Spawns", OSD.FromString(BuildFromList(SpawnPos)));
            map.Add("ObjectUUID", OSD.FromUUID(ObjectUUID));
            map.Add("Name", OSD.FromString(Name));
            return map;
        }

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            return Util.OSDToDictionary(ToOSD());
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }

        public override IDataTransferable Duplicate()
        {
            Telehub t = new Telehub();
            t.FromOSD(ToOSD());
            return t;
        }
    }
}
