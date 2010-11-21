using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public class MuteList : IDataTransferable
    {
        /// <summary>
        /// Name of the person muted
        /// </summary>
        public string MuteName;
        /// <summary>
        /// UUID of the person muted
        /// </summary>
        public UUID MuteID;
        /// <summary>
        /// Are they an object, person, group?
        /// </summary>
        public string MuteType;

        public override void FromOSD(OSDMap map)
        {
            MuteName = map["MuteName"].AsString();
            MuteID = map["MuteID"].AsUUID();
            MuteType = map["MuteType"].AsString();
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map.Add("MuteName", MuteName);
            map.Add("MuteID", MuteID);
            map.Add("MuteType", MuteType);
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
            MuteList m = new MuteList();
            m.FromOSD(ToOSD());
            return m;
        }
    }
}
