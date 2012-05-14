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
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IEntityTransferModule
    {
        /// <summary>
        ///   Teleports the given agent to the given region at the given position/rotation
        /// </summary>
        /// <param name = "agent">The agent to teleport</param>
        /// <param name = "regionHandle">The region handle of the region you are teleporting to</param>
        /// <param name = "position">The position in the new region you are teleporting into</param>
        /// <param name = "lookAt">The rotation you will have once you enter the region</param>
        /// <param name = "teleportFlags">The flags (TeleportFlags class) that are being sent along with this teleport</param>
        void Teleport(IScenePresence agent, ulong regionHandle, Vector3 position,
                      Vector3 lookAt, uint teleportFlags);

        /// <summary>
        ///   Teleports the given agent to their home, and if it is not available, a welcome region
        /// </summary>
        /// <param name = "id">The UUID of the client to teleport home</param>
        /// <param name = "client">The client to teleport hom</param>
        bool TeleportHome(UUID id, IClientAPI client);

        // <summary>
        /// Crosses the given agent to the given neighboring region.
        /// </summary>
        /// <param name = "agent">The agent to cross</param>
        /// <param name = "isFlying">Whether the agent is currently flying</param>
        /// <param name = "neighborRegion">The neighboring region to cross the agent into</param>
        void Cross(IScenePresence agent, bool isFlying, GridRegion neighborRegion);

        /// <summary>
        ///   Crosses the given object to the given neighboring region.
        /// </summary>
        /// <param name = "sog">The agent to cross</param>
        /// <param name = "position">The position to put the object in the neighboring region</param>
        /// <param name = "neighborRegion">The neighboring region to cross the agent into</param>
        /// <returns>
        ///   True if the object was added to the neighbor region, false if not
        /// </returns>
        bool CrossGroupToNewRegion(SceneObjectGroup sog, Vector3 position, GridRegion neighborRegion);

        /// <summary>
        ///   Cancel the given teleport for the given agent in the current region (the region the agent started in)
        /// </summary>
        /// <param name = "AgentID">The agent whose teleport will be canceled</param>
        /// <param name = "RegionHandle">The region that the agent first asked to be teleported from</param>
        void CancelTeleport(UUID AgentID, ulong RegionHandle);

        /// <summary>
        ///   Teleports the given client to the given region at position/rotation
        /// </summary>
        /// <param name = "client">The agent to cross</param>
        /// <param name = "regionHandle">The region handle of the region the client is teleporting to</param>
        /// <param name = "position">The position in the new region you are teleporting into</param>
        /// <param name = "lookAt">The rotation you will have once you enter the region</param>
        /// <param name = "teleportFlags">The flags (TeleportFlags class) that are being sent along with this teleport</param>
        void RequestTeleportLocation(IClientAPI client, ulong regionHandle, Vector3 position, Vector3 lookAt,
                                     uint teleportFlags);

        /// <summary>
        ///   Teleports the given client to the given region at position/rotation
        /// </summary>
        /// <param name = "client">The agent to cross</param>
        /// <param name = "reg">The region the agent is teleporting to</param>
        /// <param name = "position">The position in the new region you are teleporting into</param>
        /// <param name = "lookAt">The rotation you will have once you enter the region</param>
        /// <param name = "teleportFlags">The flags (TeleportFlags class) that are being sent along with this teleport</param>
        void RequestTeleportLocation(IClientAPI client, GridRegion reg, Vector3 position, Vector3 lookAt,
                                     uint teleportFlags);

        /// <summary>
        ///   Teleports the given client to the given region at position/rotation
        /// </summary>
        /// <param name = "client">The agent to cross</param>
        /// <param name = "RegionName">The name of the region the client is teleporting to</param>
        /// <param name = "position">The position in the new region you are teleporting into</param>
        /// <param name = "lookAt">The rotation you will have once you enter the region</param>
        /// <param name = "teleportFlags">The flags (TeleportFlags class) that are being sent along with this teleport</param>
        void RequestTeleportLocation(IClientAPI iClientAPI, string RegionName, Vector3 pos, Vector3 lookat,
                                     uint teleportFlags);

        /// <summary>
        ///   A new object (attachment) has come in from the SimulationHandlers, add it to the scene if we are able to
        /// </summary>
        /// <param name = "regionID">The UUID of the region this object will be added to</param>
        /// <param name = "userID">The user who will have this object attached to them</param>
        /// <param name = "itemID">The itemID to attach to the user</param>
        /// <returns>
        ///   True if the object was added, false if not
        /// </returns>
        bool IncomingCreateObject(UUID regionID, UUID userID, UUID itemID);

        /// <summary>
        ///   A new object has come in from the SimulationHandlers, add it to the scene if we are able to
        /// </summary>
        /// <param name = "regionID">The UUID of the region this object will be added to</param>
        /// <param name = "sog">The object to add</param>
        /// <returns>
        ///   True if the object was added, false if not
        /// </returns>
        bool IncomingCreateObject(UUID regionID, ISceneObject sog);

        /// <summary>
        ///   A new user wants to enter the given region, return whether they are allowed to enter the region or not
        /// </summary>
        /// <param name = "scene">The region to update the agent</param>
        /// <param name = "agent">The agent information</param>
        /// <param name = "teleportFlags">The flags on the agent's teleport</param>
        /// <param name = "UDPPort">The port to tell the client to connect to</param>
        /// <param name = "reason">The reason the agent cannot enter the region</param>
        /// <returns>
        ///   True if the user can enter, false if not
        /// </returns>
        bool NewUserConnection(IScene scene, AgentCircuitData agent, uint teleportFlags, out int UDPPort,
                               out string reason);

        /// <summary>
        ///   New data has come in about one of our child agents, update them with the new information
        /// </summary>
        /// <param name = "scene">The region to update the agent</param>
        /// <param name = "cAgentData">The agent information to update</param>
        /// <returns>
        ///   True if the user was updated, false if not
        /// </returns>
        bool IncomingChildAgentDataUpdate(IScene scene, AgentData cAgentData);

        /// <summary>
        ///   New data has come in about one of our child agents, update them with the new information
        /// </summary>
        /// <param name = "scene">The region to update the agent</param>
        /// <param name = "cAgentData">The agent information to update</param>
        /// <returns>
        ///   True if the user was updated, false if not
        /// </returns>
        bool IncomingChildAgentDataUpdate(IScene scene, AgentPosition cAgentData);

        /// <summary>
        ///   Get information about the given client from the given region
        /// </summary>
        /// <param name = "scene">The region to get the agent information from</param>
        /// <param name = "id">The id of the agent</param>
        /// <param name = "agentIsLeaving">Whether the agent will be leaving the sim or not</param>
        /// <param name = "agent">The information about the agent</param>
        /// <param name = "circuitData">The information about the agent</param>
        /// <returns>
        ///   True if the user was found, false if not
        /// </returns>
        bool IncomingRetrieveRootAgent(IScene scene, UUID id, bool agentIsLeaving, out AgentData agent,
                                       out AgentCircuitData circuitData);

        /// <summary>
        ///   Close the given agent in the given scene
        /// </summary>
        /// <param name = "scene">The region to close the agent in</param>
        /// <param name = "agentID">The agent to close</param>
        /// <returns>
        ///   True if the user was closed, false if not
        /// </returns>
        bool IncomingCloseAgent(IScene scene, UUID agentID);

        /// <summary>
        ///   Turn a former root child into a child agent (for when the agent leaves)
        /// </summary>
        /// <param name = "sp"></param>
        /// <param name = "finalDestination"></param>
        /// <param name = "markAgentAsLeaving"></param>
        void MakeChildAgent(IScenePresence sp, GridRegion finalDestination, bool markAgentAsLeaving);
    }
}