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

using System.Net;
using OpenMetaverse;
using OpenMetaverse.Messages.Linden;
using OpenMetaverse.Packets;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public interface IEventQueueService
    {
        /// <summary>
        ///   The local service (if possible)
        /// </summary>
        IEventQueueService InnerService { get; }

        /// <summary>
        ///   This adds a EventQueueMessage to the user's CAPS handler at the given region handle
        /// </summary>
        /// <param name = "o"></param>
        /// <param name = "avatarID"></param>
        /// <param name = "regionID"></param>
        /// <returns>Whether it was added successfully</returns>
        bool Enqueue(OSD o, UUID avatarID, UUID regionID);

        // These are required to decouple Scenes from EventQueueHelper

        /// <summary>
        ///   Disables the simulator in the client
        /// </summary>
        /// <param name = "avatarID"></param>
        /// <param name = "RegionHandle"></param>
        /// <param name = "regionID"></param>
        void DisableSimulator(UUID avatarID, ulong RegionHandle, UUID regionID);

        void EnableSimulator(ulong handle, byte[] IPAddress, int Port, UUID avatarID, int RegionSizeX, int RegionSizeY,
                             UUID regionID);

        void EstablishAgentCommunication(UUID avatarID, ulong regionHandle, byte[] IPAddress, int Port, string CapsUrl,
                                         int RegionSizeX, int RegionSizeY, UUID regionID);

        void TeleportFinishEvent(ulong regionHandle, byte simAccess,
                                 IPAddress address, int port, string capsURL,
                                 uint locationID, UUID agentID, uint teleportFlags, int RegionSizeX, int RegionSizeY,
                                 UUID regionID);

        void CrossRegion(ulong handle, Vector3 pos, Vector3 lookAt,
                         IPAddress address, int port, string capsURL,
                         UUID avatarID, UUID sessionID, int RegionSizeX, int RegionSizeY, UUID regionID);

        void ChatterBoxSessionStartReply(string groupName, UUID groupID, UUID AgentID, UUID regionID);

        void ChatterboxInvitation(UUID sessionID, string sessionName,
                                  UUID fromAgent, string message, UUID toAgent, string fromName, byte dialog,
                                  uint timeStamp, bool offline, int parentEstateID, Vector3 position,
                                  uint ttl, UUID transactionID, bool fromGroup, byte[] binaryBucket, UUID regionID);

        void ChatterBoxSessionAgentListUpdates(UUID sessionID, UUID fromAgent, UUID toAgent, bool canVoiceChat,
                                               bool isModerator, bool textMute, UUID regionID);

        void ParcelProperties(ParcelPropertiesMessage parcelPropertiesMessage, UUID avatarID, UUID regionID);
        void ParcelObjectOwnersReply(ParcelObjectOwnersReplyMessage parcelMessage, UUID avatarID, UUID regionID);
        void LandStatReply(LandStatReplyMessage message, UUID avatarID, UUID regionID);

        void ChatterBoxSessionAgentListUpdates(UUID sessionID,
                                               ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock[] message,
                                               UUID toAgent, string Transition, UUID regionID);

        void GroupMembership(AgentGroupDataUpdatePacket groupUpdate, UUID avatarID, UUID regionID);
        void QueryReply(PlacesReplyPacket placesReply, UUID avatarID, string[] RegionTypes, UUID regionID);

        void ScriptRunningReply(UUID objectID, UUID itemID, bool running, bool mono,
                                UUID avatarID, UUID regionID);

        void ObjectPhysicsProperties(ISceneChildEntity[] entities, UUID avatarID, UUID regionID);
    }
}