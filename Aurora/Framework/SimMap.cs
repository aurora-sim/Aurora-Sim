using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public class SimMap
    {
        public UUID RegionID;
        public ulong RegionHandle;
        public uint EstateID;
        public uint NumberOfAgents;
        public int RegionLocX;
        public int RegionLocY;
        public UUID SimMapTextureID;
        public string RegionName;
        public uint RegionFlags;
        public uint WaterHeight;
        public uint Access;
        public SimMapFlags SimFlags;

        //These things should not be sent to the region
        public int LastUpdated;
        public Dictionary<Vector3, uint> PositionsOfAgents = new Dictionary<Vector3, uint>();
        public Dictionary<UUID, Vector3> AgentPosition = new Dictionary<UUID, Vector3>();

        public SimMap() { }
        public SimMap(Dictionary<string, object> KVP)
        {
            RegionID = UUID.Parse(KVP["RegionID"].ToString());
            RegionHandle = ulong.Parse(KVP["RegionHandle"].ToString());
            EstateID = uint.Parse(KVP["EstateID"].ToString());
            NumberOfAgents = uint.Parse(KVP["NumberOfAgents"].ToString());
            RegionLocX = int.Parse(KVP["RegionLocX"].ToString());
            RegionLocY = int.Parse(KVP["RegionLocY"].ToString());
            SimMapTextureID = UUID.Parse(KVP["SimMapTextureID"].ToString());
            RegionName = KVP["RegionName"].ToString();
            RegionFlags = uint.Parse(KVP["RegionFlags"].ToString());
            WaterHeight = uint.Parse(KVP["WaterHeight"].ToString());
            Access = uint.Parse(KVP["Access"].ToString());
            SimFlags = (SimMapFlags)int.Parse(KVP["GridRegionFlags"].ToString());
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["RegionID"] = RegionID;
            KVP["RegionHandle"] = RegionHandle;
            KVP["EstateID"] = EstateID;
            KVP["NumberOfAgents"] = NumberOfAgents;
            KVP["RegionLocX"] = RegionLocX;
            KVP["RegionLocY"] = RegionLocY;
            KVP["SimMapTextureID"] = SimMapTextureID;
            KVP["RegionName"] = RegionName;
            KVP["RegionFlags"] = RegionFlags;
            KVP["WaterHeight"] = WaterHeight;
            KVP["Access"] = Access;
            KVP["GridRegionFlags"] = SimFlags;
            return KVP;
        }

        public MapBlockData ToMapBlockData()
        {
            MapBlockData data = new MapBlockData();
            data.Access = Convert.ToByte(Access);
            data.Agents = Convert.ToByte(NumberOfAgents);
            data.MapImageId = SimMapTextureID;
            data.Name = RegionName;
            data.RegionFlags = RegionFlags;
            data.WaterHeight = Convert.ToByte(WaterHeight);
            data.X = (ushort)(RegionLocX / Constants.RegionSize);
            data.Y = (ushort)(RegionLocY / Constants.RegionSize);
            return data;
        }
    }
}
