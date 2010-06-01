using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class MuteList
    {
        public string MuteName;
        public UUID MuteID;
        public string MuteType;

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["MuteName"] = MuteName;
            KVP["MuteID"] = MuteID;
            KVP["MuteType"] = MuteType;
            return KVP;
        }

        public MuteList() { }

        public MuteList(Dictionary<string, object> KVP)
        {
            MuteName = KVP["MuteName"].ToString();
            MuteID = UUID.Parse(KVP["MuteID"].ToString());
            MuteType = KVP["MuteType"].ToString();
        }
    }
}
