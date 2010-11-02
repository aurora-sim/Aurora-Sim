using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using Nini.Config;


namespace Aurora.Framework
{
	public interface IGroupsServiceConnector
	{
		void CreateGroup(UUID groupID, string name, string charter, bool showInList, UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish, UUID founderID,
            ulong EveryonePowers, UUID OwnerRoleID, ulong OwnerPowers);
        void AddGroupNotice(UUID requestingAgentID, UUID groupID, UUID noticeID, string fromName, string subject, string message, UUID ItemID, int AssetType, string ItemName);
        void SetAgentActiveGroup(UUID AgentID, UUID GroupID);
		void SetAgentGroupSelectedRole(UUID AgentID, UUID GroupID, UUID RoleID);
		void AddAgentToGroup(UUID AgentID, UUID GroupID, UUID RoleID);
        void AddRoleToGroup(UUID requestingAgentID, UUID GroupID, UUID RoleID, string Name, string Description, string Title, ulong Powers);
		void UpdateGroup(UUID requestingAgentID, UUID groupID, string charter, int showInList, UUID insigniaID, int membershipFee, int openEnrollment, int allowPublish, int maturePublish);
        void RemoveRoleFromGroup(UUID requestingAgentID, UUID RoleID, UUID GroupID);
        void UpdateRole(UUID requestingAgentID, UUID GroupID, UUID RoleID, string Name, string Desc, string Title, ulong Powers);
        void SetAgentGroupInfo(UUID requestingAgentID, UUID AgentID, UUID GroupID, int AcceptNotices, int ListInProfile);
        void AddAgentGroupInvite(UUID requestingAgentID, UUID inviteID, UUID GroupID, UUID roleID, UUID AgentID, string FromAgentName);
        void RemoveAgentInvite(UUID requestingAgentID, UUID inviteID);
        void AddAgentToRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        void RemoveAgentFromRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID);
        
        GroupRecord GetGroupRecord(UUID requestingAgentID, UUID GroupID, string GroupName);
		GroupProfileData GetMemberGroupProfile(UUID requestingAgentID, UUID GroupID, UUID AgentID);
		GroupMembershipData GetGroupMembershipData(UUID requestingAgentID, UUID GroupID, UUID AgentID);
		bool RemoveAgentFromGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID);
        UUID GetAgentActiveGroup(UUID RequestingAgentID, UUID AgentID);
        GroupInviteInfo GetAgentToGroupInvite(UUID requestingAgentID, UUID inviteID);
        GroupMembersData GetAgentGroupMemberData(UUID requestingAgentID, UUID GroupID, UUID AgentID);
		GroupNoticeInfo GetGroupNotice(UUID requestingAgentID, UUID noticeID);
		
        List<GroupMembershipData> GetAgentGroupMemberships(UUID requestingAgentID, UUID AgentID);
        List<DirGroupsReplyData> FindGroups(UUID requestingAgentID, string search, int StartQuery, uint queryflags);
        List<GroupRolesData> GetAgentGroupRoles(UUID requestingAgentID, UUID AgentID, UUID GroupID);
        List<GroupRolesData> GetGroupRoles(UUID requestingAgentID, UUID GroupID);
        List<GroupMembersData> GetGroupMembers(UUID requestingAgentID, UUID GroupID);
        List<GroupRoleMembersData> GetGroupRoleMembers(UUID requestingAgentID, UUID GroupID);
        List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, UUID GroupID);
        List<GroupInviteInfo> GetGroupInvites(UUID requestingAgentID);
	}
}