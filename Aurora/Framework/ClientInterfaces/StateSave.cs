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
using System;
using ProtoBuf;

namespace Aurora.Framework
{
    [ProtoContract]
    public class StateSave
    {
        [ProtoMember(1)]
        public string AssemblyName;
        [ProtoMember(2)]
        public bool Compiled = true;
        [ProtoMember(3)]
        public bool Disabled;
        [ProtoMember(4)]
        public UUID ItemID;
        [ProtoMember(5)]
        public double MinEventDelay;
        [ProtoMember(6)]
        public UUID PermsGranter;
        [ProtoMember(7)]
        public int PermsMask;
        [ProtoMember(8)]
        public string Plugins;
        [ProtoMember(9)]
        public bool Running;
        [ProtoMember(10)]
        public string Source;
        [ProtoMember(11)]
        public string State;
        [ProtoMember(12)]
        public bool TargetOmegaWasSet;
        [ProtoMember(13)]
        public UUID UserInventoryID;
        [ProtoMember(14)]
        public string Variables;
    }
}