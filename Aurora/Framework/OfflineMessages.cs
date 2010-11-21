using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class OfflineMessage
    {
        public UUID ToUUID;
        public string Message;

        public OfflineMessage() { }
        public OfflineMessage(Dictionary<string, object> KVP)
        {
            ToUUID = new UUID(KVP["ToUUID"].ToString());
            Message = KVP["Message"].ToString();
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> RetVal = new Dictionary<string, object>();
            RetVal["Message"] = Message;
            RetVal["ToUUID"] = ToUUID;
            return RetVal;
        }
    }
}
