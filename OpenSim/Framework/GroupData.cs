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

using System.Collections.Generic;
using OpenMetaverse;

namespace OpenSim.Framework
{
    public class GroupRecord
    {
        public bool AllowPublish = true;
        public string Charter;
        public UUID FounderID = UUID.Zero;
        public UUID GroupID;
        public string GroupName;
        public UUID GroupPicture = UUID.Zero;
        public bool MaturePublish = true;
        public int MembershipFee;
        public bool OpenEnrollment = true;
        public UUID OwnerRoleID = UUID.Zero;
        public bool ShowInList;

        public GroupRecord()
        {
        }

        public GroupRecord(Dictionary<string, object> values)
        {
            GroupID = UUID.Parse(values["GroupID"].ToString());
            GroupName = values["GroupName"].ToString();
            AllowPublish = bool.Parse(values["AllowPublish"].ToString());
            MaturePublish = bool.Parse(values["MaturePublish"].ToString());
            Charter = values["Charter"].ToString();
            FounderID = UUID.Parse(values["FounderID"].ToString());
            GroupPicture = UUID.Parse(values["GroupPicture"].ToString());
            MembershipFee = int.Parse(values["MembershipFee"].ToString());
            OpenEnrollment = bool.Parse(values["OpenEnrollment"].ToString());
            OwnerRoleID = UUID.Parse(values["OwnerRoleID"].ToString());
            ShowInList = bool.Parse(values["ShowInList"].ToString());
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["GroupID"] = GroupID;
            values["GroupName"] = GroupName;
            values["AllowPublish"] = AllowPublish;
            values["MaturePublish"] = MaturePublish;
            values["Charter"] = Charter;
            values["FounderID"] = FounderID;
            values["GroupPicture"] = GroupPicture;
            values["MembershipFee"] = MembershipFee;
            values["OpenEnrollment"] = OpenEnrollment;
            values["OwnerRoleID"] = OwnerRoleID;
            values["ShowInList"] = ShowInList;
            return values;
        }
    }

    public class GroupMembershipData
    {
        // Group base data
        public bool AcceptNotices = true;
        public bool Active;
        public UUID ActiveRole = UUID.Zero;
        public bool AllowPublish = true;
        public string Charter;
        public int Contribution;
        public UUID FounderID = UUID.Zero;
        public UUID GroupID;
        public string GroupName;
        public UUID GroupPicture = UUID.Zero;
        public ulong GroupPowers;
        public string GroupTitle;
        public bool ListInProfile;
        public bool MaturePublish = true;
        public int MembershipFee;
        public bool OpenEnrollment = true;
        public bool ShowInList = true;

        // Per user data

        public GroupMembershipData()
        {
        }

        public GroupMembershipData(Dictionary<string, object> values)
        {
            GroupID = UUID.Parse(values["GroupID"].ToString());
            GroupName = values["GroupName"].ToString();
            AllowPublish = bool.Parse(values["AllowPublish"].ToString());
            MaturePublish = bool.Parse(values["MaturePublish"].ToString());
            Charter = values["Charter"].ToString();
            FounderID = UUID.Parse(values["FounderID"].ToString());
            GroupPicture = UUID.Parse(values["GroupPicture"].ToString());
            MembershipFee = int.Parse(values["MembershipFee"].ToString());
            OpenEnrollment = bool.Parse(values["OpenEnrollment"].ToString());
            ShowInList = bool.Parse(values["ShowInList"].ToString());
            AcceptNotices = bool.Parse(values["AcceptNotices"].ToString());
            Contribution = int.Parse(values["Contribution"].ToString());
            GroupPowers = ulong.Parse(values["GroupPowers"].ToString());
            Active = bool.Parse(values["Active"].ToString());
            ActiveRole = UUID.Parse(values["ActiveRole"].ToString());
            ListInProfile = bool.Parse(values["ListInProfile"].ToString());
            GroupTitle = values["GroupTitle"].ToString();
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["GroupID"] = GroupID;
            values["GroupName"] = GroupName;
            values["AllowPublish"] = AllowPublish;
            values["MaturePublish"] = MaturePublish;
            values["Charter"] = Charter;
            values["FounderID"] = FounderID;
            values["GroupPicture"] = GroupPicture;
            values["MembershipFee"] = MembershipFee;
            values["OpenEnrollment"] = OpenEnrollment;
            values["ShowInList"] = ShowInList;
            values["AcceptNotices"] = AcceptNotices;
            values["Contribution"] = Contribution;
            values["GroupPowers"] = GroupPowers;
            values["Active"] = Active;
            values["ActiveRole"] = ActiveRole;
            values["ListInProfile"] = ListInProfile;
            values["GroupTitle"] = GroupTitle;
            return values;
        }
    }

    public class GroupTitlesData
    {
        public string Name;
        public bool Selected;
        public UUID UUID;

        public GroupTitlesData()
        {
        }

        public GroupTitlesData(Dictionary<string, object> values)
        {
            UUID = UUID.Parse(values["UUID"].ToString());
            Name = values["Name"].ToString();
            Selected = bool.Parse(values["Selected"].ToString());
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["Name"] = Name;
            values["UUID"] = UUID;
            values["Selected"] = Selected;
            return values;
        }
    }

    public class GroupProfileData
    {
        public bool AllowPublish;
        public string Charter;
        public UUID FounderID;
        public UUID GroupID;
        public int GroupMembershipCount;
        public int GroupRolesCount;
        public UUID InsigniaID;
        public bool MaturePublish;
        public string MemberTitle;
        public int MembershipFee;
        public int Money;
        public string Name;
        public bool OpenEnrollment;
        public UUID OwnerRole;
        public ulong PowersMask;
        public bool ShowInList;

        public GroupProfileData()
        {
        }

        public GroupProfileData(Dictionary<string, object> values)
        {
            GroupID = UUID.Parse(values["GroupID"].ToString());
            Name = values["Name"].ToString();
            Charter = values["Charter"].ToString();
            ShowInList = bool.Parse(values["ShowInList"].ToString());
            MemberTitle = values["MemberTitle"].ToString();
            PowersMask = ulong.Parse(values["PowersMask"].ToString());
            InsigniaID = UUID.Parse(values["InsigniaID"].ToString());
            FounderID = UUID.Parse(values["FounderID"].ToString());
            MembershipFee = int.Parse(values["MembershipFee"].ToString());
            OpenEnrollment = bool.Parse(values["OpenEnrollment"].ToString());
            Money = int.Parse(values["Money"].ToString());
            GroupMembershipCount = int.Parse(values["GroupMembershipCount"].ToString());
            GroupRolesCount = int.Parse(values["GroupRolesCount"].ToString());
            AllowPublish = bool.Parse(values["AllowPublish"].ToString());
            MaturePublish = bool.Parse(values["MaturePublish"].ToString());
            OwnerRole = UUID.Parse(values["OwnerRole"].ToString());
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["GroupID"] = GroupID;
            values["Name"] = Name;
            values["Charter"] = Charter;
            values["ShowInList"] = ShowInList;
            values["MemberTitle"] = MemberTitle;
            values["PowersMask"] = PowersMask;
            values["InsigniaID"] = InsigniaID;
            values["FounderID"] = FounderID;
            values["MembershipFee"] = MembershipFee;
            values["OpenEnrollment"] = OpenEnrollment;
            values["Money"] = Money;
            values["GroupMembershipCount"] = GroupMembershipCount;
            values["GroupRolesCount"] = GroupRolesCount;
            values["AllowPublish"] = AllowPublish;
            values["MaturePublish"] = MaturePublish;
            values["OwnerRole"] = OwnerRole;
            return values;
        }
    }

    public class GroupMembersData
    {
        public bool AcceptNotices;
        public UUID AgentID;
        public ulong AgentPowers;
        public int Contribution;
        public bool IsOwner;
        public bool ListInProfile;
        public string OnlineStatus;
        public string Title;

        public GroupMembersData()
        {
        }

        public GroupMembersData(Dictionary<string, object> values)
        {
            AgentID = UUID.Parse(values["AgentID"].ToString());
            Contribution = int.Parse(values["Contribution"].ToString());
            OnlineStatus = values["OnlineStatus"].ToString();
            Title = values["Title"].ToString();
            AgentPowers = ulong.Parse(values["AgentPowers"].ToString());
            IsOwner = bool.Parse(values["IsOwner"].ToString());
            ListInProfile = bool.Parse(values["ListInProfile"].ToString());
            AcceptNotices = bool.Parse(values["AcceptNotices"].ToString());
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["AgentID"] = AgentID;
            values["Contribution"] = Contribution;
            values["OnlineStatus"] = OnlineStatus;
            values["AgentPowers"] = AgentPowers;
            values["Title"] = Title;
            values["IsOwner"] = IsOwner;
            values["ListInProfile"] = ListInProfile;
            values["AcceptNotices"] = AcceptNotices;
            return values;
        }
    }

    public class GroupRolesData
    {
        public string Description;
        public int Members;
        public string Name;
        public ulong Powers;
        public UUID RoleID;
        public string Title;

        public GroupRolesData()
        {
        }

        public GroupRolesData(Dictionary<string, object> values)
        {
            RoleID = UUID.Parse(values["RoleID"].ToString());
            Name = values["Name"].ToString();
            Title = values["Title"].ToString();
            Description = values["Description"].ToString();
            Powers = ulong.Parse(values["Powers"].ToString());
            Members = int.Parse(values["Members"].ToString());
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["RoleID"] = RoleID;
            values["Name"] = Name;
            values["Title"] = Title;
            values["Description"] = Description;
            values["Powers"] = Powers;
            values["Members"] = Members;
            return values;
        }
    }

    public class GroupRoleMembersData
    {
        public UUID MemberID;
        public UUID RoleID;

        public GroupRoleMembersData()
        {
        }

        public GroupRoleMembersData(Dictionary<string, object> values)
        {
            RoleID = UUID.Parse(values["RoleID"].ToString());
            MemberID = UUID.Parse(values["MemberID"].ToString());
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["RoleID"] = RoleID;
            values["MemberID"] = MemberID;
            return values;
        }
    }

    public class GroupNoticeData
    {
        public UUID GroupID;
        public byte AssetType;
        public string FromName;
        public bool HasAttachment;
        public UUID ItemID;
        public string ItemName;
        public UUID NoticeID;
        public string Subject;
        public uint Timestamp;

        public GroupNoticeData()
        {
        }

        public GroupNoticeData(Dictionary<string, object> values)
        {
            GroupID = UUID.Parse(values["GroupID"].ToString());
            NoticeID = UUID.Parse(values["NoticeID"].ToString());
            Timestamp = uint.Parse(values["Timestamp"].ToString());
            FromName = values["FromName"].ToString();
            Subject = values["Subject"].ToString();
            HasAttachment = bool.Parse(values["HasAttachment"].ToString());
            AssetType = byte.Parse(values["AssetType"].ToString());
            ItemID = UUID.Parse(values["ItemID"].ToString());
            if (values.ContainsKey("ItemName"))
                ItemName = values["ItemName"].ToString();
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["GroupID"] = NoticeID;
            values["NoticeID"] = NoticeID;
            values["Timestamp"] = Timestamp;
            values["FromName"] = FromName;
            values["Subject"] = Subject;
            values["HasAttachment"] = HasAttachment;
            values["AssetType"] = AssetType;
            values["ItemID"] = ItemID;
            values["ItemName"] = ItemName;
            return values;
        }
    }

    public struct GroupVoteHistory
    {
        public string EndDateTime;
        public string Majority;
        public string ProposalText;
        public string Quorum;
        public string StartDateTime;
        public string TerseDateID;
        public string VoteID;
        public string VoteInitiator;
        public string VoteResult;
        public string VoteType;
    }

    public struct GroupActiveProposals
    {
        public string EndDateTime;
        public string Majority;
        public string ProposalText;
        public string Quorum;
        public string StartDateTime;
        public string TerseDateID;
        public string VoteID;
        public string VoteInitiator;
    }

    public struct GroupVoteHistoryItem
    {
        public UUID CandidateID;
        public int NumVotes;
        public string VoteCast;
    }
}