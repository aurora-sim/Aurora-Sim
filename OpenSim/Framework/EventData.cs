/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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

namespace OpenSim.Framework
{
    public enum EventDataFlags
    {
        Mature = 1 << 0,
    }

    public class EventData
    {
        public uint eventID;
        public string creator;
        public string name;
        public string category;
        public string description;
        public string date;
        public uint dateUTC;
        public uint duration;
        public uint cover;
        public uint amount;
        public string simName;
        public Vector3 globalPos;
        public uint eventFlags;
        public int maturity;
        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["eventID"] = eventID;
            KVP["creator"] = creator;
            KVP["name"] = name;
            KVP["category"] = category;
            KVP["description"] = description;
            KVP["date"] = date;
            KVP["dateUTC"] = dateUTC;
            KVP["duration"] = duration;
            KVP["cover"] = cover;
            KVP["amount"] = amount;
            KVP["simName"] = simName;
            KVP["globalPos"] = globalPos.ToRawString();
            KVP["eventFlags"] = eventFlags;
            KVP["maturity"] = maturity;
            return KVP;
        }

        public EventData()
        {
        }

        public EventData(Dictionary<string, object> KVP)
        {
            eventID = uint.Parse(KVP["eventID"].ToString());
            creator = KVP["creator"].ToString();
            name = KVP["name"].ToString();
            category = KVP["category"].ToString();
            description = KVP["description"].ToString();
            date = KVP["date"].ToString();
            dateUTC = uint.Parse(KVP["dateUTC"].ToString());
            duration = uint.Parse(KVP["duration"].ToString());
            cover = uint.Parse(KVP["cover"].ToString());
            amount = uint.Parse(KVP["amount"].ToString());
            simName = KVP["simName"].ToString();
            string[] Pos = KVP["globalPos"].ToString().Split(' ');
            globalPos = new Vector3(float.Parse(Pos[0]), float.Parse(Pos[1]), float.Parse(Pos[2]));
            eventFlags = uint.Parse(KVP["eventFlags"].ToString());
            maturity = int.Parse(KVP["maturity"].ToString());
        }
    }
}
