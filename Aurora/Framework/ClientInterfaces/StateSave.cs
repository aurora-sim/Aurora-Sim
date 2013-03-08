/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class StateSave : IDataTransferable
    {
        public string AssemblyName;
        public bool Compiled = true;
        public bool Disabled;
        public UUID ItemID;
        public double MinEventDelay;
        public UUID PermsGranter;
        public int PermsMask;
        public OSDMap Plugins;
        public bool Running;
        public string Source;
        public string State;
        public bool TargetOmegaWasSet;
        public UUID UserInventoryID;
        public string Variables;

        public override void FromOSD(OSDMap map)
        {
            State = map["State"].AsString();
            Source = map["Source"].AsString();
            ItemID = map["ItemID"].AsUUID();
            Running = map["Running"].AsBoolean();
            Disabled = map["Disabled"].AsBoolean();
            Variables = map["Variables"].AsString();
            Plugins = (OSDMap) map["Plugins"];
            PermsGranter = map["PermsGranter"].AsUUID();
            PermsMask = map["PermsMask"].AsInteger();
            MinEventDelay = map["MinEventDelay"].AsReal();
            AssemblyName = map["AssemblyName"].AsString();
            UserInventoryID = map["UserInventoryID"].AsUUID();
            TargetOmegaWasSet = map["TargetOmegaWasSet"].AsBoolean();
            if (map.ContainsKey("Compiled"))
                Compiled = map["Compiled"].AsBoolean();
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap
                             {
                                 {"State", State},
                                 {"Source", Source},
                                 {"ItemID", ItemID},
                                 {"Running", Running},
                                 {"Disabled", Disabled},
                                 {"Variables", Variables},
                                 {"Plugins", Plugins},
                                 {"PermsGranter", PermsGranter},
                                 {"PermsMask", PermsMask},
                                 {"MinEventDelay", MinEventDelay},
                                 {"AssemblyName", AssemblyName},
                                 {"UserInventoryID", UserInventoryID},
                                 {"TargetOmegaWasSet", TargetOmegaWasSet},
                                 {"Compiled", Compiled}
                             };
            return map;
        }
    }
}