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

namespace OpenSim.Framework
{
    public interface IGroupsServicesConnector
    {
        UUID CreateGroup(UUID RequestingAgentID, string name, string charter, bool showInList, UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish, UUID founderID);
        void UpdateGroup(UUID RequestingAgentID, UUID groupID, string charter, bool showInList, UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish);
        GroupRecord GetGroupRecord(UUID RequestingAgentID, UUID GroupID, string GroupName);
        List<DirGroupsReplyData> FindGroups(UUID RequestingAgentID, string search, int queryStart, uint queryFlags);
        List<GroupMembersData> GetGroupMembers(UUID RequestingAgentID, UUID GroupID);

        void AddGroupRole(UUID RequestingAgentID, UUID groupID, UUID roleID, string name, string description, string title, ulong powers);
        void UpdateGroupRole(UUID RequestingAgentID, UUID groupID, UUID roleID, string name, string description, string title, ulong powers);
        void RemoveGroupRole(UUID RequestingAgentID, UUID groupID, UUID roleID);
        List<GroupRolesData> GetGroupRoles(UUID RequestingAgentID, UUID GroupID);
        List<GroupRoleMembersData> GetGroupRoleMembers(UUID RequestingAgentID, UUID GroupID);

        void AddAgentToGroup(UUID RequestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        bool RemoveAgentFromGroup(UUID RequestingAgentID, UUID AgentID, UUID GroupID);

        void AddAgentToGroupInvite(UUID RequestingAgentID, UUID inviteID, UUID groupID, UUID roleID, UUID agentIDFromAgentName, string FromAgentName);
        GroupInviteInfo GetAgentToGroupInvite(UUID RequestingAgentID, UUID inviteID);
        void RemoveAgentToGroupInvite(UUID RequestingAgentID, UUID inviteID);

        void AddAgentToGroupRole(UUID RequestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        void RemoveAgentFromGroupRole(UUID RequestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        List<GroupRolesData> GetAgentGroupRoles(UUID RequestingAgentID, UUID AgentID, UUID GroupID);

        void SetAgentActiveGroup(UUID RequestingAgentID, UUID AgentID, UUID GroupID);
        GroupMembershipData GetAgentActiveMembership(UUID RequestingAgentID, UUID AgentID);

        void SetAgentActiveGroupRole(UUID RequestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        void SetAgentGroupInfo(UUID RequestingAgentID, UUID AgentID, UUID GroupID, bool AcceptNotices, bool ListInProfile);

        GroupMembershipData GetAgentGroupMembership(UUID RequestingAgentID, UUID AgentID, UUID GroupID);
        List<GroupMembershipData> GetAgentGroupMemberships(UUID RequestingAgentID, UUID AgentID);

        void AddGroupNotice(UUID RequestingAgentID, UUID groupID, UUID noticeID, string fromName, string subject, string message, UUID ItemID, int AssetType, string ItemName);
        GroupNoticeInfo GetGroupNotice(UUID RequestingAgentID, UUID noticeID);
        List<GroupNoticeData> GetGroupNotices(UUID RequestingAgentID, UUID GroupID);

        void ResetAgentGroupChatSessions(UUID agentID);
        bool hasAgentBeenInvitedToGroupChatSession(UUID agentID, UUID groupID);
        bool hasAgentDroppedGroupChatSession(UUID agentID, UUID groupID);
        void AgentDroppedFromGroupChatSession(UUID agentID, UUID groupID);
        void AgentInvitedToGroupChatSession(UUID agentID, UUID groupID);
        List<GroupInviteInfo> GetGroupInvites(UUID requestingAgentID);
        void AddGroupProposal(UUID agentID, GroupProposalInfo info);
    }

    public class GroupInviteInfo
    {
        public UUID GroupID  = UUID.Zero;
        public UUID RoleID   = UUID.Zero;
        public UUID AgentID  = UUID.Zero;
        public UUID InviteID = UUID.Zero;
        public string FromAgentName = "";

        public GroupInviteInfo()
        {
        }

        public GroupInviteInfo(Dictionary<string, object> values)
        {
            GroupID = UUID.Parse(values["GroupID"].ToString());
            RoleID = UUID.Parse(values["RoleID"].ToString());
            AgentID = UUID.Parse(values["AgentID"].ToString());
            InviteID = UUID.Parse(values["InviteID"].ToString());
            FromAgentName = values["FromAgentName"].ToString();
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["GroupID"] = GroupID;
            values["RoleID"] = RoleID;
            values["AgentID"] = AgentID;
            values["InviteID"] = InviteID;
            values["FromAgentName"] = FromAgentName;
            return values;
        }
    }

    public class GroupNoticeInfo
    {
        public GroupNoticeData noticeData = new GroupNoticeData();
        public UUID GroupID = UUID.Zero;
        public string Message = string.Empty;
        public byte[] BinaryBucket = new byte[0];

        public GroupNoticeInfo()
        {
        }

        public GroupNoticeInfo(Dictionary<string, object> values)
        {
            noticeData = new GroupNoticeData(values["noticeData"] as Dictionary<string, object>);
            GroupID = UUID.Parse(values["GroupID"].ToString());
            Message = values["Message"].ToString();
            BinaryBucket = Utils.HexStringToBytes(values["BinaryBucket"].ToString(), true);
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["noticeData"] = noticeData.ToKeyValuePairs();
            values["GroupID"] = GroupID;
            values["Message"] = Message;
            values["BinaryBucket"] = Utils.BytesToHexString(BinaryBucket, "BinaryBucket");
            return values;
        }
    }

    public class GroupProposalInfo : Aurora.Framework.IDataTransferable
    {
        public UUID GroupID = UUID.Zero;
        public int Duration = 0;
        public float Majority = 0;
        public string Text = string.Empty;
        public int Quorum = 0;
        public UUID Session = UUID.Zero;

        public override void FromOSD(OpenMetaverse.StructuredData.OSDMap map)
        {
            GroupID = map["GroupID"].AsUUID();
            Duration = map["Duration"].AsInteger();
            Majority = (float)map["Majority"].AsReal();
            Text = map["Text"].AsString();
            Quorum = map["Quorum"].AsInteger();
            Session = map["Session"].AsUUID();
        }

        public override OpenMetaverse.StructuredData.OSDMap ToOSD()
        {
            OpenMetaverse.StructuredData.OSDMap map = new OpenMetaverse.StructuredData.OSDMap();
            map["GroupID"] = GroupID;
            map["Duration"] = Duration;
            map["Majority"] = Majority;
            map["Text"] = Text;
            map["Quorum"] = Quorum;
            map["Session"] = Session;
            return map;
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            return Util.OSDToDictionary(ToOSD());
        }

        public override Aurora.Framework.IDataTransferable Duplicate()
        {
            GroupProposalInfo p = new GroupProposalInfo();
            p.FromOSD(ToOSD());
            return p;
        }
    }
}
