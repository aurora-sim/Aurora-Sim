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
using Aurora.Framework;

namespace OpenSim.Services.Interfaces
{
    public delegate void GetResponse(OSDMap response);
    /// <summary>
    ///   This Service deals with posting events to a (local or remote) host
    ///   This is used for secure communications between regions and the grid service
    ///   for things such as EventQueueMessages and posting of a grid wide notice
    /// </summary>
    public interface ISyncMessagePosterService
    {
        /// <summary>
        ///   Post a request to all hosts that we have
        /// </summary>
        /// <param name = "request"></param>
        void Post(OSDMap request, ulong RegionHandle);

        /// <summary>
        ///   Post a request to all hosts that we have
        ///   Returns an OSDMap of the response.
        /// </summary>
        /// <param name = "request"></param>
        /// <returns></returns>
        void Get(OSDMap request, UUID userID, ulong RegionHandle, GetResponse response);
    }

    public delegate OSDMap MessageReceived(OSDMap message);

    /// <summary>
    ///   This is used to deal with incoming requests from the ISyncMessagePosterService
    /// </summary>
    public interface IAsyncMessageRecievedService
    {
        /// <summary>
        ///   This is fired when a message from the ISyncMessagePosterService
        ///   has been received either by the IAsyncMessagePosterService for Aurora
        ///   or the ISyncMessagePosterService for Aurora.Server
        ///   
        ///   Notes on this event:
        ///   This is subscribed to by many events and many events will not be dealing with the request.
        ///   If you do not wish to send a response back to the poster, return null, otherwise, return a
        ///   valid OSDMap that will be added to the response.
        /// </summary>
        event MessageReceived OnMessageReceived;

        /// <summary>
        ///   Fire the MessageReceived event
        /// </summary>
        /// <param name = "message"></param>
        /// <returns></returns>
        OSDMap FireMessageReceived(string SessionID, OSDMap message);
    }

    /// <summary>
    ///   This interface is used mainly on Aurora.Server to asyncronously post events to
    ///   regions in the grid. This can be used to send grid wide notices or other events 
    ///   that regions need to know about
    /// </summary>
    public interface IAsyncMessagePostService
    {
        /// <summary>
        ///   Post a request to the given region asyncronously
        ///   This request will be picked up by the region normally within 30 seconds
        /// </summary>
        /// <param name = "RegionHandle"></param>
        /// <param name = "request"></param>
        void Post(ulong RegionHandle, OSDMap request);
    }

    public class LoginAgentArgs : IDataTransferable
    {
        public bool Success = false;
        public AgentCircuitData CircuitData;
        public string SeedCap;
        public string Reason;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();

            map["Success"] = Success;
            map["CircuitData"] = CircuitData.ToOSD();
            map["SeedCap"] = SeedCap;
            map["Reason"] = Reason;

            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Success = map["Success"];
            CircuitData = new AgentCircuitData();
            CircuitData.FromOSD((OSDMap)map["CircuitData"]);
            SeedCap = map["SeedCap"];
            Reason = map["Reason"];
        }
    }

    public interface IAgentProcessing
    {
        /// <summary>
        ///   Log out the current agent
        /// </summary>
        /// <param name = "regionCaps"></param>
        void LogoutAgent(IRegionClientCapsService regionCaps, bool kickRootAgent);

        /// <summary>
        ///   Called by the login service, adds an agent to the region without firing any EQM messages
        /// </summary>
        /// <param name = "region"></param>
        /// <param name = "aCircuit"></param>
        /// <param name = "reason"></param>
        LoginAgentArgs LoginAgent(GridRegion region, AgentCircuitData aCircuit);

        /// <summary>
        ///   Logout all agents in the given region
        /// </summary>
        /// <param name = "requestingRegion"></param>
        void LogOutAllAgentsForRegion(ulong requestingRegion);

        /// <summary>
        ///   Enable any child agents that might need added for the restarted region
        /// </summary>
        /// <param name = "requestingRegion"></param>
        /// <returns></returns>
        bool EnableChildAgentsForRegion(GridRegion requestingRegion);

        /// <summary>
        ///   Enable all child agents that should exist in neighboring regions for the given avatar
        /// </summary>
        /// <param name = "AgentID"></param>
        /// <param name = "requestingRegion"></param>
        /// <param name = "DrawDistance"></param>
        /// <param name = "circuit"></param>
        /// <returns></returns>
        void EnableChildAgents(UUID AgentID, ulong requestingRegion, int DrawDistance, AgentCircuitData circuit);

        /// <summary>
        ///   Teleport the given agent to another sim
        /// </summary>
        /// <param name = "destination"></param>
        /// <param name = "TeleportFlags"></param>
        /// <param name = "DrawDistance"></param>
        /// <param name = "circuit"></param>
        /// <param name = "agentData"></param>
        /// <param name = "AgentID"></param>
        /// <param name = "requestingRegion"></param>
        /// <param name = "reason"></param>
        /// <returns></returns>
        bool TeleportAgent(ref GridRegion destination, uint TeleportFlags, int DrawDistance,
                           AgentCircuitData circuit, AgentData agentData, UUID AgentID, ulong requestingRegion,
                           out string reason);

        /// <summary>
        ///   Cross the given agent to another sim
        /// </summary>
        /// <param name = "crossingRegion"></param>
        /// <param name = "pos"></param>
        /// <param name = "velocity"></param>
        /// <param name = "circuit"></param>
        /// <param name = "cAgent"></param>
        /// <param name = "AgentID"></param>
        /// <param name = "requestingRegion"></param>
        /// <param name = "reason"></param>
        /// <returns></returns>
        bool CrossAgent(GridRegion crossingRegion, Vector3 pos,
                        Vector3 velocity, AgentCircuitData circuit, AgentData cAgent, UUID AgentID,
                        ulong requestingRegion, out string reason);

        /// <summary>
        ///   Send an agent update to all neighbors for the given agent
        ///   This updates the agent's position, throttles, velocity, etc
        /// </summary>
        /// <param name = "agentpos"></param>
        /// <param name = "regionCaps"></param>
        void SendChildAgentUpdate(AgentPosition agentpos, IRegionClientCapsService regionCaps);
    }
}