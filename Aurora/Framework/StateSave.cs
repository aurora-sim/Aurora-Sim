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
        public string Source;
        public UUID ItemID;
        public bool Running;
        public bool Disabled;
        public string Variables;
        public OSDMap Plugins;
        public string Permissions;
        public double MinEventDelay;
        public string AssemblyName;
        public UUID UserInventoryID;

        public override void FromOSD (OSDMap map)
        {
            State = map["State"].AsString ();
            Source = map["Source"].AsString ();
            ItemID = map["ItemID"].AsUUID ();
            Running = map["Running"].AsBoolean ();
            Disabled = map["Disabled"].AsBoolean ();
            Variables = map["Variables"].AsString ();
            Plugins = (OSDMap)map["Plugins"];
            Permissions = map["Permissions"].AsString ();
            MinEventDelay = map["MinEventDelay"].AsReal ();
            AssemblyName = map["AssemblyName"].AsString ();
            UserInventoryID = map["UserInventoryID"].AsUUID ();
        }

        public override OSDMap ToOSD ()
        {
            OSDMap map = new OSDMap ();
            map.Add ("State", State);
            map.Add ("Source", Source);
            map.Add ("ItemID", ItemID);
            map.Add ("Running", Running);
            map.Add ("Disabled", Disabled);
            map.Add ("Variables", Variables);
            map.Add ("Plugins", Plugins);
            map.Add ("Permissions", Permissions);
            map.Add ("MinEventDelay", MinEventDelay);
            map.Add ("AssemblyName", AssemblyName);
            map.Add ("UserInventoryID", UserInventoryID);
            return map;
        }

        public override Dictionary<string, object> ToKeyValuePairs ()
        {
            return Util.OSDToDictionary (ToOSD ());
        }

        public override void FromKVP (Dictionary<string, object> KVP)
        {
            FromOSD (Util.DictionaryToOSD (KVP));
        }

        public override IDataTransferable Duplicate ()
        {
            StateSave m = new StateSave ();
            m.FromOSD (ToOSD ());
            return m;
        }
    }
}
