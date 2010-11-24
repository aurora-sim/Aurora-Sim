using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public class StateSave : IDataTransferable
    {
        public string State;
        public UUID ItemID;
        public string Source;
        public bool Running;
        public bool Disabled;
        public object Variables;
        public object Plugins;
        public string Permissions;
        public double MinEventDelay;
        public string AssemblyName;
        public UUID UserInventoryID;

        public override void FromOSD(OSDMap map)
        {
            State = map["State"].AsString();
            ItemID = map["ItemID"].AsUUID();
            Source = map["Source"].AsString();
            Running = map["Running"].AsBoolean();
            Disabled = map["Disabled"].AsBoolean();
            Variables = map["Variables"].AsString();
            Plugins = map["Plugins"].AsString();
            Permissions = map["Permissions"].AsString();
            MinEventDelay = map["MinEventDelay"].AsReal();
            AssemblyName = map["AssemblyName"].AsString();
            UserInventoryID = map["UserInventoryID"].AsUUID();
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map.Add("State", State);
            map.Add("ItemID", ItemID);
            map.Add("Source", Source);
            map.Add("Running", Running);
            map.Add("Disabled", Disabled);
            map.Add("Variables", Variables.ToString());
            map.Add("Plugins", Plugins.ToString());
            map.Add("Permissions", Permissions);
            map.Add("MinEventDelay", MinEventDelay);
            map.Add("AssemblyName", AssemblyName);
            map.Add("UserInventoryID", UserInventoryID);
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
            StateSave m = new StateSave();
            m.FromOSD(ToOSD());
            return m;
        }
    }
}
