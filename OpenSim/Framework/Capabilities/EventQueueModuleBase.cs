using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using OpenSim.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenMetaverse.Messages;
using OpenMetaverse.Messages.Linden;
using OpenMetaverse.Packets;

namespace OpenSim.Framework.Capabilities
{
    public class EventQueueModuleBase
    {
        public virtual bool Enqueue(OSD item, UUID avatarID, ulong regionHandle)
        {
            return false;
        }

        public void DisableSimulator(ulong handle, UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.DisableSimulator(handle);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void EnableSimulator(ulong handle, IPEndPoint endPoint, UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.EnableSimulator(handle, endPoint);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void EstablishAgentCommunication(UUID avatarID, ulong regionHandle, IPEndPoint endPoint, ulong RegionHandle)
        {
            //Blank (for the CapsUrl) as we do not know what the CapsURL is on the sim side, it will be fixed when it reaches the grid server
            OSD item = EventQueueHelper.EstablishAgentCommunication(avatarID, regionHandle, endPoint.ToString(), "");
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void TeleportFinishEvent(ulong regionHandle, byte simAccess,
                                        IPEndPoint regionExternalEndPoint,
                                        uint locationID, uint flags,
                                        UUID avatarID, uint teleportFlags, ulong RegionHandle)
        {
            //Blank (for the CapsUrl) as we do not know what the CapsURL is on the sim side, it will be fixed when it reaches the grid server
            OSD item = EventQueueHelper.TeleportFinishEvent(regionHandle, simAccess, regionExternalEndPoint,
                                                            locationID, flags, "", avatarID, teleportFlags);
            Enqueue(item, avatarID, RegionHandle);
        }

        public virtual void CrossRegion(ulong handle, Vector3 pos, Vector3 lookAt,
                                IPEndPoint newRegionExternalEndPoint,
                                UUID avatarID, UUID sessionID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.CrossRegion(handle, pos, lookAt, newRegionExternalEndPoint,
                                                    "", avatarID, sessionID);
            Enqueue(item, avatarID, RegionHandle);
        }

        public void ChatterboxInvitation(UUID sessionID, string sessionName,
                                         UUID fromAgent, string message, UUID toAgent, string fromName, byte dialog,
                                         uint timeStamp, bool offline, int parentEstateID, Vector3 position,
                                         uint ttl, UUID transactionID, bool fromGroup, byte[] binaryBucket, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ChatterboxInvitation(sessionID, sessionName, fromAgent, message, toAgent, fromName, dialog,
                                                             timeStamp, offline, parentEstateID, position, ttl, transactionID,
                                                             fromGroup, binaryBucket);
            Enqueue(item, toAgent, RegionHandle);
            //m_log.InfoFormat("########### eq ChatterboxInvitation #############\n{0}", item);

        }

        public void ChatterBoxSessionAgentListUpdates(UUID sessionID, UUID fromAgent, UUID toAgent, bool canVoiceChat,
                                                      bool isModerator, bool textMute, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ChatterBoxSessionAgentListUpdates(sessionID, fromAgent, canVoiceChat,
                                                                          isModerator, textMute);
            Enqueue(item, toAgent, RegionHandle);
            //m_log.InfoFormat("########### eq ChatterBoxSessionAgentListUpdates #############\n{0}", item);
        }

        public void ChatterBoxSessionAgentListUpdates(UUID sessionID, ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock[] messages, UUID toAgent, string Transition, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ChatterBoxSessionAgentListUpdates(sessionID, messages, Transition);
            Enqueue(item, toAgent, RegionHandle);
            //m_log.InfoFormat("########### eq ChatterBoxSessionAgentListUpdates #############\n{0}", item);
        }

        public void ParcelProperties(ParcelPropertiesMessage parcelPropertiesPacket, UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.ParcelProperties(parcelPropertiesPacket);
            Enqueue(item, avatarID, RegionHandle);
        }

        public void GroupMembership(AgentGroupDataUpdatePacket groupUpdate, UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.GroupMembership(groupUpdate);
            Enqueue(item, avatarID, RegionHandle);
        }

        public void QueryReply(PlacesReplyPacket groupUpdate, UUID avatarID, string[] info, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.PlacesQuery(groupUpdate, info);
            Enqueue(item, avatarID, RegionHandle);
        }
    }
}
