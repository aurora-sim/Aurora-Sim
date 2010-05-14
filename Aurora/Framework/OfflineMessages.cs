using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class OfflineMessage
    {
        public UUID FromUUID;
        public UUID ToUUID;
        public string FromName;
        public string Message;

        public OfflineMessage() { }
        public OfflineMessage(Dictionary<string, object> KVP)
        {
            FromUUID = new UUID(KVP["FromUUID"].ToString());
            ToUUID = new UUID(KVP["ToUUID"].ToString());
            FromName = KVP["FromName"].ToString();
            Message = KVP["Message"].ToString();
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> RetVal = new Dictionary<string, object>();
            RetVal["Message"] = Message;
            RetVal["FromName"] = FromName;
            RetVal["ToUUID"] = ToUUID;
            RetVal["FromUUID"] = FromUUID;
            return RetVal;
        }
    }
}
