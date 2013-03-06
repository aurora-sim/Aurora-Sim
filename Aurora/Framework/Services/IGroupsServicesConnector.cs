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
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public interface IGroupsServicesConnector
    {
        UUID CreateGroup(UUID RequestingAgentID, string name, string charter, bool showInList, UUID insigniaID,
                         int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish, UUID founderID);

        void UpdateGroup(UUID RequestingAgentID, UUID groupID, string charter, bool showInList, UUID insigniaID,
                         int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish);

        GroupRecord GetGroupRecord(UUID RequestingAgentID, UUID GroupID, string GroupName);
        GroupProfileData GetGroupProfile(UUID RequestingAgentID, UUID GroupID);
        List<DirGroupsReplyData> FindGroups(UUID RequestingAgentID, string search, uint? start, uint? count, uint queryFlags);
        List<GroupMembersData> GetGroupMembers(UUID RequestingAgentID, UUID GroupID);

        void AddGroupRole(UUID RequestingAgentID, UUID groupID, UUID roleID, string name, string description,
                          string title, ulong powers);

        void UpdateGroupRole(UUID RequestingAgentID, UUID groupID, UUID roleID, string name, string description,
                             string title, ulong powers);

        void RemoveGroupRole(UUID RequestingAgentID, UUID groupID, UUID roleID);
        List<GroupRolesData> GetGroupRoles(UUID RequestingAgentID, UUID GroupID);
        List<GroupRoleMembersData> GetGroupRoleMembers(UUID RequestingAgentID, UUID GroupID);

        void AddAgentToGroup(UUID RequestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        bool RemoveAgentFromGroup(UUID RequestingAgentID, UUID AgentID, UUID GroupID);

        void AddAgentToGroupInvite(UUID RequestingAgentID, UUID inviteID, UUID groupID, UUID roleID,
                                   UUID agentIDFromAgentName, string FromAgentName);

        GroupInviteInfo GetAgentToGroupInvite(UUID RequestingAgentID, UUID inviteID);
        void RemoveAgentToGroupInvite(UUID RequestingAgentID, UUID inviteID);

        void AddAgentToGroupRole(UUID RequestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        void RemoveAgentFromGroupRole(UUID RequestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        List<GroupRolesData> GetAgentGroupRoles(UUID RequestingAgentID, UUID AgentID, UUID GroupID);

        string SetAgentActiveGroup(UUID RequestingAgentID, UUID AgentID, UUID GroupID);
        GroupMembershipData GetAgentActiveMembership(UUID RequestingAgentID, UUID AgentID);

        string SetAgentActiveGroupRole(UUID RequestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);

        void SetAgentGroupInfo(UUID RequestingAgentID, UUID AgentID, UUID GroupID, bool AcceptNotices,
                               bool ListInProfile);

        GroupMembershipData GetAgentGroupMembership(UUID RequestingAgentID, UUID AgentID, UUID GroupID);
        List<GroupMembershipData> GetAgentGroupMemberships(UUID RequestingAgentID, UUID AgentID);

        void AddGroupNotice(UUID RequestingAgentID, UUID groupID, UUID noticeID, string fromName, string subject,
                            string message, UUID ItemID, int AssetType, string ItemName);

        GroupNoticeInfo GetGroupNotice(UUID RequestingAgentID, UUID noticeID);
        List<GroupNoticeData> GetGroupNotices(UUID RequestingAgentID, UUID GroupID);

        List<GroupInviteInfo> GetGroupInvites(UUID requestingAgentID);
        void AddGroupProposal(UUID agentID, GroupProposalInfo info);

        List<GroupTitlesData> GetGroupTitles(UUID agentID, UUID groupID);

        List<GroupProposalInfo> GetActiveProposals(UUID agentID, UUID groupID);
        List<GroupProposalInfo> GetInactiveProposals(UUID agentID, UUID groupID);

        void VoteOnActiveProposals(UUID agentID, UUID groupID, UUID proposalID, string vote);
    }

    /// <summary>
    ///   Internal class for chat sessions
    /// </summary>
    public class ChatSession
    {
        public List<ChatSessionMember> Members;
        public string Name;
        public UUID SessionID;
    }

    //Pulled from OpenMetaverse
    // Summary:
    //     Struct representing a member of a group chat session and their settings
    public class ChatSessionMember
    {
        // Summary:
        //     The OpenMetaverse.UUID of the Avatar
        public UUID AvatarKey;
        //
        // Summary:
        //     True if user has voice chat enabled
        public bool CanVoiceChat;
        /// <summary>
        /// Whether the user has accepted being added to the group chat
        /// </summary>
        public bool HasBeenAdded;
        /// <summary>
        /// Whether the user has asked to be removed from the chat
        /// </summary>
        public bool RequestedRemoval;
        //
        // Summary:
        //     True of Avatar has moderator abilities
        public bool IsModerator;
        //
        // Summary:
        //     True if a moderator has muted this avatars chat
        public bool MuteText;
        //
        // Summary:
        //     True if a moderator has muted this avatars voice
        public bool MuteVoice;
        //
        // Summary:
        //     True if they have been requested to join the session
    }

    public class GroupInviteInfo : IDataTransferable
    {
        public UUID AgentID = UUID.Zero;
        public string FromAgentName = "";
        public UUID GroupID = UUID.Zero;
        public UUID InviteID = UUID.Zero;
        public UUID RoleID = UUID.Zero;

        public GroupInviteInfo()
        {
        }

        public GroupInviteInfo(Dictionary<string, object> values)
        {
            FromKVP(values);
        }

        public override Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["GroupID"] = GroupID;
            values["RoleID"] = RoleID;
            values["AgentID"] = AgentID;
            values["InviteID"] = InviteID;
            values["FromAgentName"] = FromAgentName;
            return values;
        }

        public override void FromKVP(Dictionary<string, object> values)
        {
            GroupID = UUID.Parse(values["GroupID"].ToString());
            RoleID = UUID.Parse(values["RoleID"].ToString());
            AgentID = UUID.Parse(values["AgentID"].ToString());
            InviteID = UUID.Parse(values["InviteID"].ToString());
            FromAgentName = values["FromAgentName"].ToString();
        }

        public override OSDMap ToOSD()
        {
            OSDMap values = new OSDMap();
            values["GroupID"] = GroupID;
            values["RoleID"] = RoleID;
            values["AgentID"] = AgentID;
            values["InviteID"] = InviteID;
            values["FromAgentName"] = FromAgentName;
            return values;
        }

        public override void FromOSD(OSDMap values)
        {
            GroupID = values["GroupID"];
            RoleID = values["RoleID"];
            AgentID = values["AgentID"];
            InviteID = values["InviteID"];
            FromAgentName = values["FromAgentName"];
        }
    }

    public class GroupNoticeInfo : IDataTransferable
    {
        public byte[] BinaryBucket = new byte[0];
        public UUID GroupID = UUID.Zero;
        public string Message = string.Empty;
        public GroupNoticeData noticeData = new GroupNoticeData();

        public GroupNoticeInfo()
        {
        }

        public GroupNoticeInfo(Dictionary<string, object> values)
        {
            FromKVP(values);
        }

        public override void FromKVP(Dictionary<string, object> values)
        {
            noticeData = new GroupNoticeData(values["noticeData"] as Dictionary<string, object>);
            GroupID = UUID.Parse(values["GroupID"].ToString());
            Message = values["Message"].ToString();
            BinaryBucket = Utils.HexStringToBytes(values["BinaryBucket"].ToString(), true);
        }

        public override Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["noticeData"] = noticeData.ToKVP();
            values["GroupID"] = GroupID;
            values["Message"] = Message;
            values["BinaryBucket"] = Utils.BytesToHexString(BinaryBucket, "BinaryBucket");
            return values;
        }

        public override OSDMap ToOSD()
        {
            OSDMap values = new OSDMap();
            values["noticeData"] = noticeData.ToOSD();
            values["GroupID"] = GroupID;
            values["Message"] = Message;
            values["BinaryBucket"] = BinaryBucket;
            return values;
        }

        public override void FromOSD(OSDMap values)
        {
            noticeData = new GroupNoticeData();
            noticeData.FromOSD((OSDMap)values["noticeData"]);
            GroupID = values["GroupID"];
            Message = values["Message"];
            BinaryBucket = values["BinaryBucket"];
        }
    }

    public class GroupProposalInfo : IDataTransferable
    {
        public int Duration;
        public UUID GroupID = UUID.Zero;
        public float Majority;
        public int Quorum;
        public UUID Session = UUID.Zero;
        public string Text = string.Empty;
        public UUID BallotInitiator = UUID.Zero;
        public DateTime Created = DateTime.Now;
        public DateTime Ending = DateTime.Now;
        public UUID VoteID = UUID.Random();
        /// <summary>
        /// Only set when a user is calling to find out proposal info, it is what said user voted
        /// </summary>
        public string VoteCast = "";

        /// <summary>
        /// The result of the proposal (success or failure)
        /// </summary>
        public bool Result = false;
        /// <summary>
        /// The number of votes cast (so far if the proposal is still open)
        /// </summary>
        public int NumVotes = 0;

        /// <summary>
        /// If this is false, the result of the proposal has not been calculated and should be when it is retrieved next
        /// </summary>
        public bool HasCalculatedResult = false;

        public override void FromOSD(OSDMap map)
        {
            GroupID = map["GroupID"].AsUUID();
            Duration = map["Duration"].AsInteger();
            Majority = (float) map["Majority"].AsReal();
            Text = map["Text"].AsString();
            Quorum = map["Quorum"].AsInteger();
            Session = map["Session"].AsUUID();
            BallotInitiator = map["BallotInitiator"];
            Created = map["Created"];
            Ending = map["Ending"];
            VoteID = map["VoteID"];
            VoteCast = map["VoteCast"];
            Result = map["Result"];
            NumVotes = map["NumVotes"];
            HasCalculatedResult = map["HasCalculatedResult"];
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["GroupID"] = GroupID;
            map["Duration"] = Duration;
            map["Majority"] = Majority;
            map["Text"] = Text;
            map["Quorum"] = Quorum;
            map["Session"] = Session;
            map["BallotInitiator"] = BallotInitiator;
            map["Created"] = Created;
            map["Ending"] = Ending;
            map["VoteID"] = VoteID;
            map["VoteCast"] = VoteCast;
            map["Result"] = Result;
            map["NumVotes"] = NumVotes;
            map["HasCalculatedResult"] = HasCalculatedResult;
            return map;
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }

        public override Dictionary<string, object> ToKVP()
        {
            return Util.OSDToDictionary(ToOSD());
        }
    }
}