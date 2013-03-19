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
using Aurora.Framework.Modules;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class multipleMapItemReply : IDataTransferable
    {
        public Dictionary<ulong, List<mapItemReply>> items = new Dictionary<ulong, List<mapItemReply>>();

        public multipleMapItemReply()
        {
        }

        public override OSDMap ToOSD()
        {
            OSDMap result = new OSDMap();
            foreach (KeyValuePair<ulong, List<mapItemReply>> kvp in items)
            {
                OSDArray array = new OSDArray();
                foreach (mapItemReply item in kvp.Value)
                {
                    array.Add(item.ToOSD());
                }
                result[kvp.Key.ToString()] = array;
            }
            return result;
        }

        public override void FromOSD(OSDMap map)
        {
            foreach (KeyValuePair<string, OSD> kvp in map)
            {
                ulong regionHandle = ulong.Parse(kvp.Key);
                OSDArray array = (OSDArray) kvp.Value;
                List<mapItemReply> replies = new List<mapItemReply>();
                foreach (OSD o in array)
                {
                    mapItemReply r = new mapItemReply();
                    r.FromOSD((OSDMap) o);
                    replies.Add(r);
                }
                items[regionHandle] = replies;
            }
        }
    }

    public class mapItemReply : IDataTransferable
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

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["X"] = (int) x;
            map["Y"] = (int) y;
            map["ID"] = id;
            map["Extra"] = Extra;
            map["Extra2"] = Extra2;
            map["Name"] = name;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            x = (uint) (int) map["X"];
            y = (uint) (int) map["Y"];
            id = map["ID"];
            Extra = map["Extra"];
            Extra2 = map["Extra2"];
            name = map["Name"];
        }
    }
}