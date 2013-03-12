/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using OpenMetaverse;
using ProtoBuf;

namespace Aurora.Framework
{
    /// <summary>
    ///   A dictionary for task inventory.
    /// </summary>
    /// This class is not thread safe.  Callers must synchronize on Dictionary methods or Clone() this object before
    /// iterating over it.
    [Serializable, ProtoContract()]
    public class TaskInventoryDictionary : Dictionary<UUID, TaskInventoryItem>,
                                           ICloneable, IXmlSerializable
    {
        // private static readonly ILog MainConsole.Instance = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly XmlSerializer tiiSerializer = new XmlSerializer(typeof (TaskInventoryItem));

        #region ICloneable Members

        public Object Clone()
        {
            TaskInventoryDictionary clone = new TaskInventoryDictionary();

            lock (this)
            {
                foreach (UUID uuid in Keys)
                {
                    clone.Add(uuid, (TaskInventoryItem) this[uuid].Clone());
                }
            }

            return clone;
        }

        #endregion

        #region IXmlSerializable Members

        public XmlSchema GetSchema()
        {
            return null;
        }

        // see IXmlSerializable
        public void ReadXml(XmlReader reader)
        {
            // MainConsole.Instance.DebugFormat("[TASK INVENTORY]: ReadXml current node before actions, {0}", reader.Name);

            if (!reader.IsEmptyElement)
            {
                reader.Read();
                while (tiiSerializer.CanDeserialize(reader))
                {
                    TaskInventoryItem item = (TaskInventoryItem) tiiSerializer.Deserialize(reader);
                    Add(item.ItemID, item);

                    //MainConsole.Instance.DebugFormat("[TASK INVENTORY]: Instanted prim item {0}, {1} from xml", item.Name, item.ItemID);
                }

                // MainConsole.Instance.DebugFormat("[TASK INVENTORY]: Instantiated {0} prim items in total from xml", Count);
            }
            // else
            // {
            //     MainConsole.Instance.DebugFormat("[TASK INVENTORY]: Skipping empty element {0}", reader.Name);
            // }

            // For some .net implementations, this last read is necessary so that we advance beyond the end tag
            // of the element wrapping this object so that the rest of the serialization can complete normally.
            reader.Read();

            // MainConsole.Instance.DebugFormat("[TASK INVENTORY]: ReadXml current node after actions, {0}", reader.Name);
        }

        // see IXmlSerializable
        public void WriteXml(XmlWriter writer)
        {
            lock (this)
            {
                foreach (TaskInventoryItem item in Values)
                {
                    tiiSerializer.Serialize(writer, item);
                }
            }

            //tiiSerializer.Serialize(writer, Values);
        }

        #endregion

        public List<TaskInventoryItem> Clone2List()
        {
            List<TaskInventoryItem> clone = new List<TaskInventoryItem>();

            lock (this)
            {
#if (!ISWIN)
                foreach (UUID uuid in Keys)
                {
                    clone.Add((TaskInventoryItem)this[uuid].Clone());
                }
#else
                clone.AddRange(Keys.Select(uuid => (TaskInventoryItem) this[uuid].Clone()));
#endif
            }

            return clone;
        }

        [XmlIgnore]
        public List<TaskInventoryItem> Items
        {
            get { return Clone2List(); }
            set { foreach (TaskInventoryItem itm in value) this.Add(itm.ItemID, itm); }
        }

        // see ICloneable
    }
}