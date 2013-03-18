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

using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class EventData : IDataTransferable
    {
        public uint amount;
        public string category;
        public uint cover;
        public string creator;
        public string date;
        public uint dateUTC;
        public string description;
        public uint duration;
        public uint eventFlags;
        public uint eventID;
        public Vector3 globalPos;
        public Vector3 regionPos;
        public int maturity;
        public string name;
        public string simName;

        public EventData()
        {
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["eventID"] = eventID;
            map["creator"] = creator;
            map["name"] = name;
            map["category"] = category;
            map["description"] = description;
            map["date"] = date;
            map["dateUTC"] = dateUTC;
            map["duration"] = duration;
            map["cover"] = cover;
            map["amount"] = amount;
            map["simName"] = simName;
            map["globalPos"] = globalPos;
            map["regionPos"] = regionPos;
            map["eventFlags"] = eventFlags;
            map["maturity"] = maturity;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            eventID = map["eventID"];
            creator = map["creator"];
            name = map["name"];
            category = map["category"];
            description = map["description"];
            date = map["date"];
            dateUTC = map["dateUTC"];
            duration = map["duration"];
            cover = map["cover"];
            amount = map["amount"];
            simName = map["simName"];
            globalPos = map["globalPos"];
            regionPos = map["regionPos"];
            eventFlags = map["eventFlags"];
            maturity = map["maturity"];
        }
    }
}