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

using System;
using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;

namespace OpenSim.Framework
{
    [Serializable]
    public class GridInstantMessage : IDataTransferable
    {
        public Guid fromAgentID;
        public string fromAgentName;
        public Guid toAgentID;
        public byte dialog;
        public bool fromGroup;
        public string message;
        public Guid imSessionID;
        public byte offline;
        public Vector3 Position;
        public byte[] binaryBucket;


        public uint ParentEstateID;
        public Guid RegionID;
        public uint timestamp;

        public GridInstantMessage()
        {
            binaryBucket = new byte[0];
        }

        public GridInstantMessage(IScene scene, UUID _fromAgentID,
                string _fromAgentName, UUID _toAgentID,
                byte _dialog, bool _fromGroup, string _message,
                UUID _imSessionID, bool _offline, Vector3 _position,
                byte[] _binaryBucket)
        {
            fromAgentID = _fromAgentID.Guid;
            fromAgentName = _fromAgentName;
            toAgentID = _toAgentID.Guid;
            dialog = _dialog;
            fromGroup = _fromGroup;
            message = _message;
            imSessionID = _imSessionID.Guid;
            if (_offline)
                offline = 1;
            else
                offline = 0;
            Position = _position;
            binaryBucket = _binaryBucket;

            if (scene != null)
            {
                ParentEstateID = scene.RegionInfo.EstateSettings.ParentEstateID;
                RegionID = scene.RegionInfo.RegionSettings.RegionUUID.Guid;
            }
            timestamp = (uint)Util.UnixTimeSinceEpoch();
        }

        public GridInstantMessage(IScene scene, UUID _fromAgentID,
                string _fromAgentName, UUID _toAgentID, byte _dialog,
                string _message, bool _offline,
                Vector3 _position) : this(scene, _fromAgentID, _fromAgentName,
                _toAgentID, _dialog, false, _message,
                _fromAgentID ^ _toAgentID, _offline, _position, new byte[0])
        {
        }

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            return Util.OSDToDictionary(ToOSD());
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map.Add("fromAgentID", OSD.FromUUID(new UUID(fromAgentID)));
            map.Add("fromAgentName", OSD.FromString(fromAgentName));
            map.Add("toAgentID", OSD.FromUUID(new UUID(toAgentID)));
            map.Add("dialog", OSD.FromInteger(dialog));
            map.Add("fromGroup", OSD.FromBoolean(fromGroup));
            map.Add("message", OSD.FromString(message));
            map.Add("imSessionID", OSD.FromUUID(new UUID(imSessionID)));
            map.Add("offline", OSD.FromInteger(offline));
            map.Add("Position", OSD.FromVector3(Position));
            map.Add("binaryBucket", OSD.FromBinary(binaryBucket));
            map.Add("ParentEstateID", OSD.FromUInteger(ParentEstateID));
            map.Add("RegionID", OSD.FromUUID(new UUID(RegionID)));
            map.Add("timestamp", OSD.FromUInteger(timestamp));
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            fromAgentID = map["fromAgentID"].AsUUID().Guid;
            fromAgentName = map["fromAgentName"].AsString();
            toAgentID = map["toAgentID"].AsUUID().Guid;
            dialog = (byte)map["dialog"].AsInteger();
            fromGroup = map["fromGroup"].AsBoolean();
            message = map["message"].ToString();
            offline = (byte)map["offline"].AsInteger();
            Position = map["Position"].AsVector3();
            binaryBucket = map["binaryBucket"].AsBinary();
            ParentEstateID = map["ParentEstateID"].AsUInteger();
            RegionID = map["RegionID"].AsUUID().Guid;
            imSessionID = map["imSessionID"].AsUUID().Guid;
            timestamp = map["timestamp"].AsUInteger();
        }

        public override void FromKVP(Dictionary<string, object> RetVal)
        {
            FromOSD(Util.DictionaryToOSD(RetVal));
        }

        public override IDataTransferable Duplicate()
        {
            GridInstantMessage m = new GridInstantMessage();
            m.FromOSD(ToOSD());
            return m;
        }
    }
}
