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
using System.Net;
using OpenSim.Framework;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenMetaverse.StructuredData;
using OpenMetaverse.Messages.Linden;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Framework
{
    public class SyncMessageHelper
    {
        public static OSDMap EnableChildAgents(UUID AgentID, int DrawDistance, AgentCircuitData circuit, ulong RequestingRegion)
        {
            OSDMap llsdBody = new OSDMap();

            llsdBody.Add("DrawDistance", DrawDistance);
            llsdBody.Add("Circuit", circuit.PackAgentCircuitData());

            return buildEvent("EnableChildAgents", llsdBody, AgentID, RequestingRegion);
        }

        public static OSDMap ArrivedAtDestination(UUID AgentID, int DrawDistance, AgentCircuitData circuit, ulong requestingRegion)
        {
            OSDMap llsdBody = new OSDMap();

            llsdBody.Add("AgentID", AgentID);

            llsdBody.Add("DrawDistance", DrawDistance);
            llsdBody.Add("Circuit", circuit.PackAgentCircuitData());

            return buildEvent("ArrivedAtDestination", llsdBody, AgentID, requestingRegion);
        }

        public static OSDMap CrossAgent(GridRegion crossingRegion, Vector3 pos,
            Vector3 velocity, AgentCircuitData circuit, AgentData cAgent, ulong RequestingRegion)
        {
            OSDMap llsdBody = new OSDMap();

            llsdBody.Add("Pos", pos);
            llsdBody.Add("Vel", velocity);
            llsdBody.Add("Region", crossingRegion.ToOSD());
            llsdBody.Add("Circuit", circuit.PackAgentCircuitData());
            llsdBody.Add("AgentData", cAgent.Pack());
            return buildEvent("CrossAgent", llsdBody, circuit.AgentID, RequestingRegion);
        }

        public static OSDMap TeleportAgent(int DrawDistance, AgentCircuitData circuit,
            AgentData data, uint TeleportFlags,
            GridRegion destination, ulong requestingRegion)
        {
            OSDMap llsdBody = new OSDMap();

            llsdBody.Add("DrawDistance", DrawDistance);
            llsdBody.Add("Circuit", circuit.PackAgentCircuitData());
            llsdBody.Add("TeleportFlags", TeleportFlags);
            llsdBody.Add("AgentData", data.Pack());
            llsdBody.Add("Region", destination.ToOSD());
            return buildEvent("TeleportAgent", llsdBody, circuit.AgentID, requestingRegion);
        }

        public static OSDMap SendChildAgentUpdate(AgentPosition agentpos, ulong requestingRegion)
        {
            OSDMap llsdBody = new OSDMap();

            llsdBody.Add("AgentPos", agentpos.Pack());
            return buildEvent("SendChildAgentUpdate", llsdBody, agentpos.AgentID, requestingRegion);
        }

        public static OSDMap CancelTeleport(UUID AgentID, ulong requestingRegion)
        {
            OSDMap llsdBody = new OSDMap();

            llsdBody.Add("AgentID", AgentID);
            llsdBody.Add("RequestingRegion", requestingRegion);
            return buildEvent("CancelTeleport", llsdBody, AgentID, requestingRegion);
        }

        public static OSDMap LogoutRegionAgents(ulong requestingRegion)
        {
            OSDMap llsdBody = new OSDMap();
            return buildEvent("LogoutRegionAgents", llsdBody, UUID.Zero, requestingRegion);
        }

        public static OSDMap DisableSimulator(UUID AgentID, ulong requestingRegion)
        {
            OSDMap llsdBody = new OSDMap();
            return buildEvent("DisableSimulator", llsdBody, AgentID, requestingRegion);
        }

        public static OSDMap buildEvent(string eventName, OSD eventBody, UUID AgentID, ulong requestingRegion)
        {
            OSDMap llsdEvent = new OSDMap(2);
            llsdEvent.Add("Message", eventBody);
            llsdEvent.Add("Method", new OSDString(eventName));
            llsdEvent.Add("AgentID", AgentID);
            llsdEvent.Add("RequestingRegion", requestingRegion);

            return llsdEvent;
        }
    }
}
