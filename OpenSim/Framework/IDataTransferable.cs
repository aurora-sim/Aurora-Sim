using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class IDataTransferable
    {
        public virtual OSDMap ToOSD() { return null; }
        public virtual void FromOSD(OSDMap map) { }

        public virtual void FromKVP(Dictionary<string, object> KVP) { }
        public virtual Dictionary<string, object> ToKeyValuePairs() { return null; }

        public virtual IDataTransferable Duplicate() { return null; }
    }
}
