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
        public string MuteName;
        public UUID MuteID;
        public string MuteType;

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["MuteName"] = MuteName;
            KVP["MuteID"] = MuteID;
            KVP["MuteType"] = MuteType;
            return KVP;
        }

        public override void FromOSD(OSDMap map)
        {
            FromKVP(Util.OSDToDictionary(map));
        }

        public override OSDMap ToOSD()
        {
            return Util.DictionaryToOSD(ToKeyValuePairs());
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            MuteName = KVP["MuteName"].ToString();
            MuteID = UUID.Parse(KVP["MuteID"].ToString());
            MuteType = KVP["MuteType"].ToString();
        }

        public override IDataTransferable Duplicate()
        {
            MuteList m = new MuteList();
            m.FromOSD(ToOSD());
            return m;
        }
    }
}
