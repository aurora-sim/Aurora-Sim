using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class IDataTransferable
    {
        /// <summary>
        /// Serialize the module to OSD
        /// </summary>
        /// <returns></returns>
        public virtual OSDMap ToOSD() { return null; }

        /// <summary>
        /// Deserialize the module from OSD
        /// </summary>
        /// <param name="map"></param>
        public virtual void FromOSD(OSDMap map) { }

        /// <summary>
        /// Serialize the module to a Dictionary
        /// </summary>
        /// <param name="KVP"></param>
        public virtual void FromKVP(Dictionary<string, object> KVP) { }

        /// <summary>
        /// Deserialize this module from a Dictionary
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<string, object> ToKeyValuePairs() { return null; }

        /// <summary>
        /// Duplicate this module
        /// </summary>
        /// <returns></returns>
        public virtual IDataTransferable Duplicate() { return null; }
    }
}
