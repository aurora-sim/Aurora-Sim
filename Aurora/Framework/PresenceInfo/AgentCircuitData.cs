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
using System.Collections.Generic;
using System.Net;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Services;
using ProtoBuf;

namespace Aurora.Framework.PresenceInfo
{
    /// <summary>
    ///     Circuit data for an agent.  Connection information shared between
    ///     regions that accept UDP connections from a client
    /// </summary>
    [ProtoContract(UseProtoMembersOnly = true)]
    public class AgentCircuitData : IDataTransferable
    {
        #region Variables

        /// <summary>
        ///     Avatar Unique Agent Identifier
        /// </summary>
        [ProtoMember(1)]
        public UUID AgentID;

        /// <summary>
        ///     The client's IP address, as captured by the login service
        /// </summary>
        [ProtoMember(2)]
        public string IPAddress;

        /// <summary>
        ///     Other unknown info
        /// </summary>
        [ProtoMember(3)]
        public CachedUserInfo CachedUserInfo = null;

        /// <summary>
        ///     IntenalUseOnly - Kept by the server to tell the rest of the server what port to call the region on
        /// </summary>
        [ProtoMember(4)]
        public int RegionUDPPort;

        /// <summary>
        ///     Random Unique GUID for this session.  Client gets this at login and it's
        ///     only supposed to be disclosed over secure channels
        /// </summary>
        [ProtoMember(5)]
        public UUID SecureSessionID;

        /// <summary>
        ///     Non secure Session ID
        /// </summary>
        [ProtoMember(6)]
        public UUID SessionID;

        /// <summary>
        ///     Number given to the client when they log-in that they provide
        ///     as credentials to the UDP server
        /// </summary>
        [ProtoMember(7)]
        public uint CircuitCode;

        /// <summary>
        ///     The real child boolean, OpenSim always sends false, so we read this for Aurora regions
        /// </summary>
        [ProtoMember(8)]
        public bool IsChildAgent;

        /// <summary>
        ///     Position the Agent's Avatar starts in the region
        /// </summary>
        [ProtoMember(9)]
        public Vector3 StartingPosition;

        /// <summary>
        ///     How this agent got here
        /// </summary>
        [ProtoMember(10)]
        public uint TeleportFlags;

        #endregion

        #region IDataTransferable

        /// <summary>
        ///     Serialize the module to OSD
        /// </summary>
        /// <returns></returns>
        public override OSDMap ToOSD()
        {
            OSDMap args = new OSDMap();
            args["AgentID"] = AgentID;
            args["IsChildAgent"] = IsChildAgent;
            args["CircuitCode"] = CircuitCode;
            args["SessionID"] = SessionID;
            args["SecureSessionID"] = SecureSessionID;
            args["StartingPosition"] = StartingPosition;
            args["IPAddress"] = IPAddress;
            if (CachedUserInfo != null)
                args["CachedUserInfo"] = CachedUserInfo.ToOSD();
            args["TeleportFlags"] = OSD.FromUInteger(TeleportFlags);
            args["RegionUDPPort"] = RegionUDPPort;

            return args;
        }

        /// <summary>
        ///     Deserialize the module from OSD
        /// </summary>
        /// <param name="map"></param>
        public override void FromOSD(OSDMap map)
        {
            AgentID = map["AgentID"];
            IsChildAgent = map["IsChildAgent"];
            CircuitCode = map["CircuitCode"];
            SecureSessionID = map["SecureSessionID"];
            SessionID = map["SessionID"];
            IPAddress = map["IPAddress"];
            RegionUDPPort = map["RegionUDPPort"];
            StartingPosition = map["StartingPosition"];
            TeleportFlags = map["TeleportFlags"];
            if (map.ContainsKey("CachedUserInfo"))
            {
                CachedUserInfo = new CachedUserInfo();
                CachedUserInfo.FromOSD((OSDMap)map["CachedUserInfo"]);
            }
        }

        #region oldFunctions

        public virtual AgentCircuitData Copy()
        {
            AgentCircuitData Copy = new AgentCircuitData
            {
                AgentID = AgentID,
                IsChildAgent = IsChildAgent,
                CircuitCode = CircuitCode,
                IPAddress = IPAddress,
                SecureSessionID = SecureSessionID,
                SessionID = SessionID,
                StartingPosition = StartingPosition,
                TeleportFlags = TeleportFlags,
                CachedUserInfo = CachedUserInfo
            };


            return Copy;
        }

        #endregion

        #endregion
    }
}