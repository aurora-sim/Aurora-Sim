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

using System.Collections.Generic;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class multipleMapItemReply
    {
        public Dictionary<ulong, List<mapItemReply>> items = new Dictionary<ulong, List<mapItemReply>>();

        public multipleMapItemReply()
        {
        }

        public multipleMapItemReply(Dictionary<string, object> KVP)
        {
            foreach (KeyValuePair<string, object> kvp in KVP)
            {
                ulong handle = ulong.Parse(kvp.Key.Split('A')[1]);
                mapItemReply item = new mapItemReply(kvp.Value as Dictionary<string, object>);

                if (!items.ContainsKey(handle))
                    items.Add(handle, new List<mapItemReply>());

                items[handle].Add(item);
            }
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (KeyValuePair<ulong, List<mapItemReply>> kvp in items)
            {
                int i = 0;
                foreach (mapItemReply item in kvp.Value)
                {
                    result["A" + kvp.Key + "A" + i.ToString()] = item.ToKeyValuePairs();
                    i++;
                }
            }
            return result;
        }
    }

    public class mapItemReply
    {
        public int Extra;
        public int Extra2;
        public UUID id;
        public string name;
        public uint x;
        public uint y;

        public mapItemReply()
        {
        }

        public mapItemReply(Dictionary<string, object> KVP)
        {
            x = uint.Parse(KVP["X"].ToString());
            y = uint.Parse(KVP["Y"].ToString());
            id = UUID.Parse(KVP["ID"].ToString());
            Extra = int.Parse(KVP["Extra"].ToString());
            Extra2 = int.Parse(KVP["Extra2"].ToString());
            name = KVP["Name"].ToString();
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["X"] = x;
            KVP["Y"] = y;
            KVP["ID"] = id;
            KVP["Extra"] = Extra;
            KVP["Extra2"] = Extra2;
            KVP["Name"] = name;
            return KVP;
        }
    }
}