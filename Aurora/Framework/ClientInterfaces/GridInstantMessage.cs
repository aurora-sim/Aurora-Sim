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
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    [Serializable]
    public class GridInstantMessage : IDataTransferable
    {
        public uint ParentEstateID;
        public Vector3 Position;
        public UUID RegionID;
        public byte[] binaryBucket;
        public byte dialog;
        public UUID fromAgentID;
        public string fromAgentName;
        public bool fromGroup;
        public UUID imSessionID;
        public string message;
        public byte offline;
        public uint timestamp;
        public UUID toAgentID;

        public GridInstantMessage()
        {
            binaryBucket = new byte[0];
            timestamp = (uint) Util.UnixTimeSinceEpoch();
        }

        public GridInstantMessage(IScene scene, UUID _fromAgentID,
                                  string _fromAgentName, UUID _toAgentID,
                                  byte _dialog, bool _fromGroup, string _message,
                                  UUID _imSessionID, bool _offline, Vector3 _position,
                                  byte[] _binaryBucket)
        {
            fromAgentID = _fromAgentID;
            fromAgentName = _fromAgentName;
            toAgentID = _toAgentID;
            dialog = _dialog;
            fromGroup = _fromGroup;
            message = _message;
            imSessionID = _imSessionID;
            offline = _offline ? (Byte) 1 : (Byte) 0;
            Position = _position;
            binaryBucket = _binaryBucket;

            if (scene != null)
            {
                ParentEstateID = scene.RegionInfo.EstateSettings.ParentEstateID;
                RegionID = scene.RegionInfo.RegionSettings.RegionUUID;
            }
            timestamp = (uint) Util.UnixTimeSinceEpoch();
        }

        public GridInstantMessage(IScene scene, UUID _fromAgentID,
                                  string _fromAgentName, UUID _toAgentID, byte _dialog,
                                  string _message, bool _offline,
                                  Vector3 _position) : this(scene, _fromAgentID, _fromAgentName,
                                                            _toAgentID, _dialog, false, _message,
                                                            _fromAgentID ^ _toAgentID, _offline, _position, new byte[0])
        {
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap
                             {
                                 {"fromAgentID", OSD.FromUUID(fromAgentID)},
                                 {"fromAgentName", OSD.FromString(fromAgentName)},
                                 {"toAgentID", OSD.FromUUID(toAgentID)},
                                 {"dialog", OSD.FromInteger(dialog)},
                                 {"fromGroup", OSD.FromBoolean(fromGroup)},
                                 {"message", OSD.FromString(message)},
                                 {"imSessionID", OSD.FromUUID(imSessionID)},
                                 {"offline", OSD.FromInteger(offline)},
                                 {"Position", OSD.FromVector3(Position)},
                                 {"binaryBucket", OSD.FromBinary(binaryBucket)},
                                 {"ParentEstateID", OSD.FromUInteger(ParentEstateID)},
                                 {"RegionID", OSD.FromUUID(RegionID)},
                                 {"timestamp", OSD.FromUInteger(timestamp)}
                             };
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            fromAgentID = map["fromAgentID"].AsUUID();
            fromAgentName = map["fromAgentName"].AsString();
            toAgentID = map["toAgentID"].AsUUID();
            dialog = (byte) map["dialog"].AsInteger();
            fromGroup = map["fromGroup"].AsBoolean();
            message = map["message"].ToString();
            offline = (byte) map["offline"].AsInteger();
            Position = map["Position"].AsVector3();
            binaryBucket = map["binaryBucket"].AsBinary();
            ParentEstateID = map["ParentEstateID"].AsUInteger();
            RegionID = map["RegionID"].AsUUID();
            imSessionID = map["imSessionID"].AsUUID();
            timestamp = map["timestamp"].AsUInteger();
        }
    }
}