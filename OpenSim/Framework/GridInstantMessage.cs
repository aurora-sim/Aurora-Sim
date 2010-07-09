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
using OpenMetaverse;

namespace OpenSim.Framework
{
    [Serializable]
    public class GridInstantMessage
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
        public GridInstantMessage(System.Collections.Generic.Dictionary<string, object> RetVal)
        {
            fromAgentID = UUID.Parse(RetVal["fromAgentID"].ToString()).Guid;
            fromAgentName = RetVal["fromAgentName"].ToString();
            toAgentID = UUID.Parse(RetVal["toAgentID"].ToString()).Guid;
            dialog = byte.Parse(RetVal["dialog"].ToString());
            fromGroup = bool.Parse(RetVal["fromGroup"].ToString());
            message = RetVal["message"].ToString();
            offline = byte.Parse(RetVal["offline"].ToString());
            Position = Vector3.Parse(RetVal["Position"].ToString());
            binaryBucket = Utils.StringToBytes(RetVal["binaryBucket"].ToString());
            ParentEstateID = uint.Parse(RetVal["ParentEstateID"].ToString());
            RegionID = UUID.Parse(RetVal["RegionID"].ToString()).Guid;
            timestamp = uint.Parse(RetVal["timestamp"].ToString());
        }

        public System.Collections.Generic.Dictionary<string, object> ToKeyValuePairs()
        {
            System.Collections.Generic.Dictionary<string, object> RetVal = new System.Collections.Generic.Dictionary<string, object>();
            RetVal.Add("fromAgentID", fromAgentID);
            RetVal.Add("fromAgentName", fromAgentName);
            RetVal.Add("toAgentID", toAgentID);
            RetVal.Add("dialog", dialog);
            RetVal.Add("fromGroup", fromGroup);
            RetVal.Add("message", message);
            RetVal.Add("imSessionID", imSessionID);
            RetVal.Add("offline", offline);
            RetVal.Add("Position", Position);
            RetVal.Add("binaryBucket", binaryBucket);
            RetVal.Add("ParentEstateID", ParentEstateID);
            RetVal.Add("RegionID", RegionID);
            RetVal.Add("timestamp", timestamp);
            return RetVal;
        }
    }
}
