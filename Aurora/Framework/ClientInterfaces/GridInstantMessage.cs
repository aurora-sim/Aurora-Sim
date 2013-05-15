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
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Utilities;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;

namespace Aurora.Framework.ClientInterfaces
{
    [ProtoContract(UseProtoMembersOnly=true)]
    public class GridInstantMessage : IDataTransferable
    {
        [ProtoMember(1)]
        public uint ParentEstateID;
        [ProtoMember(2)]
        public Vector3 Position;
        [ProtoMember(3)]
        public UUID RegionID;
        [ProtoMember(4)]
        public byte[] BinaryBucket;
        [ProtoMember(5)]
        public byte Dialog;
        [ProtoMember(6)]
        public UUID FromAgentID;
        [ProtoMember(7)]
        public string FromAgentName;
        [ProtoMember(8)]
        public bool FromGroup;
        [ProtoMember(9)]
        public UUID SessionID;
        [ProtoMember(10)]
        public string Message;
        [ProtoMember(11)]
        public byte Offline;
        [ProtoMember(12)]
        public uint Timestamp;
        [ProtoMember(13)]
        public UUID ToAgentID;

        public GridInstantMessage()
        {
            BinaryBucket = new byte[0];
            Timestamp = (uint) Util.UnixTimeSinceEpoch();
            SessionID = FromAgentID ^ ToAgentID;
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap
                             {
                                 {"fromAgentID", OSD.FromUUID(FromAgentID)},
                                 {"fromAgentName", OSD.FromString(FromAgentName)},
                                 {"toAgentID", OSD.FromUUID(ToAgentID)},
                                 {"dialog", OSD.FromInteger(Dialog)},
                                 {"fromGroup", OSD.FromBoolean(FromGroup)},
                                 {"message", OSD.FromString(Message)},
                                 {"imSessionID", OSD.FromUUID(SessionID)},
                                 {"offline", OSD.FromInteger(Offline)},
                                 {"Position", OSD.FromVector3(Position)},
                                 {"binaryBucket", OSD.FromBinary(BinaryBucket)},
                                 {"ParentEstateID", OSD.FromUInteger(ParentEstateID)},
                                 {"RegionID", OSD.FromUUID(RegionID)},
                                 {"timestamp", OSD.FromUInteger(Timestamp)}
                             };
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            FromAgentID = map["fromAgentID"].AsUUID();
            FromAgentName = map["fromAgentName"].AsString();
            ToAgentID = map["toAgentID"].AsUUID();
            Dialog = (byte) map["dialog"].AsInteger();
            FromGroup = map["fromGroup"].AsBoolean();
            Message = map["message"].ToString();
            Offline = (byte) map["offline"].AsInteger();
            Position = map["Position"].AsVector3();
            BinaryBucket = map["binaryBucket"].AsBinary();
            ParentEstateID = map["ParentEstateID"].AsUInteger();
            RegionID = map["RegionID"].AsUUID();
            SessionID = map["imSessionID"].AsUUID();
            Timestamp = map["timestamp"].AsUInteger();
        }
    }
}