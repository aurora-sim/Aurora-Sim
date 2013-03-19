/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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

using Aurora.Framework;
using Aurora.Framework.Services;
using Nini.Config;
using OpenMetaverse;
using System;
using System.Collections.Generic;

namespace Aurora.Modules.Groups
{
    public class AuroraDataGroupsServicesConnectorModule : INonSharedRegionModule, IGroupsServicesConnector
    {
        private readonly Dictionary<UUID, ChatSession> ChatSessions = new Dictionary<UUID, ChatSession>();
        private IGroupsServiceConnector GroupsConnector;
        private IUserAccountService m_accountService;

        private bool m_connectorEnabled;

        #region IGroupsServicesConnector Members

        /// <summary>
        ///     Create a Group, including Everyone and Owners Role, place FounderID in both groups, select Owner as selected role, and newly created group as agent's active role.
        /// </summary>
        public UUID CreateGroup(UUID requestingAgentID, string name, string charter, bool showInList, UUID insigniaID,
                                int membershipFee, bool openEnrollment, bool allowPublish,
                                bool maturePublish, UUID founderID)
        {
            UUID GroupID = UUID.Random();
            UUID OwnerRoleID = UUID.Random();

            GroupsConnector.CreateGroup(GroupID, name, charter, showInList,
                                        insigniaID, 0, openEnrollment, allowPublish, maturePublish, founderID,
                                        OwnerRoleID);

            return GroupID;
        }

        public void UpdateGroup(UUID requestingAgentID, UUID groupID, string charter, bool showInList,
                                UUID insigniaID, int membershipFee, bool openEnrollment,
                                bool allowPublish, bool maturePublish)
        {
            GroupsConnector.UpdateGroup(requestingAgentID, groupID, charter,
                                        showInList ? 1 : 0, insigniaID, membershipFee,
                                        openEnrollment ? 1 : 0, allowPublish ? 1 : 0,
                                        maturePublish ? 1 : 0);
        }

        public void AddGroupRole(UUID requestingAgentID, UUID groupID, UUID roleID, string name, string description,
                                 string title, ulong powers)
        {
            GroupsConnector.AddRoleToGroup(requestingAgentID, groupID, roleID, name, description, title, powers);
        }

        public void RemoveGroupRole(UUID requestingAgentID, UUID groupID, UUID roleID)
        {
            GroupsConnector.RemoveRoleFromGroup(requestingAgentID, roleID, groupID);
        }

        public void UpdateGroupRole(UUID requestingAgentID, UUID groupID, UUID roleID, string name, string description,
                                    string title, ulong powers)
        {
            GroupsConnector.UpdateRole(requestingAgentID, groupID, roleID, name, description, title, powers);
        }

        public GroupRecord GetGroupRecord(UUID requestingAgentID, UUID GroupID, string GroupName)
        {
            return GroupsConnector.GetGroupRecord(requestingAgentID, GroupID, GroupName);
        }

        public string SetAgentActiveGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            return GroupsConnector.SetAgentActiveGroup(AgentID, GroupID);
        }

        public string SetAgentActiveGroupRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            return GroupsConnector.SetAgentGroupSelectedRole(AgentID, GroupID, RoleID);
        }

        public void SetAgentGroupInfo(UUID requestingAgentID, UUID AgentID, UUID GroupID, bool AcceptNotices,
                                      bool ListInProfile)
        {
            GroupsConnector.SetAgentGroupInfo(requestingAgentID, AgentID, GroupID, AcceptNotices ? 1 : 0,
                                              ListInProfile ? 1 : 0);
        }

        public void AddAgentToGroupInvite(UUID requestingAgentID, UUID inviteID, UUID groupID, UUID roleID, UUID agentID,
                                          string FromAgentName)
        {
            GroupsConnector.AddAgentGroupInvite(requestingAgentID, inviteID, groupID, roleID, agentID, FromAgentName);
        }

        public GroupInviteInfo GetAgentToGroupInvite(UUID requestingAgentID, UUID inviteID)
        {
            return GroupsConnector.GetAgentToGroupInvite(requestingAgentID, inviteID);
        }

        public void RemoveAgentToGroupInvite(UUID requestingAgentID, UUID inviteID)
        {
            GroupsConnector.RemoveAgentInvite(requestingAgentID, inviteID);
        }

        public void AddAgentToGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            GroupsConnector.AddAgentToGroup(requestingAgentID, AgentID, GroupID, RoleID);
        }

        public bool RemoveAgentFromGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            return GroupsConnector.RemoveAgentFromGroup(requestingAgentID, AgentID, GroupID);
        }

        public void AddAgentToGroupRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            GroupsConnector.AddAgentToRole(requestingAgentID, AgentID, GroupID, RoleID);
        }

        public void RemoveAgentFromGroupRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            GroupsConnector.RemoveAgentFromRole(requestingAgentID, AgentID, GroupID, RoleID);
        }

        public List<DirGroupsReplyData> FindGroups(UUID requestingAgentID, string search, uint? start, uint? count,
                                                   uint queryflags)
        {
            //TODO: Fix this.. should be in the search module
            return GroupsConnector.FindGroups(requestingAgentID, search, start, count, queryflags);
        }

        public GroupProfileData GetGroupProfile(UUID requestingAgentID, UUID GroupID)
        {
            return GroupsConnector.GetGroupProfile(requestingAgentID, GroupID);
        }

        public GroupMembershipData GetAgentGroupMembership(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            return GroupsConnector.GetGroupMembershipData(requestingAgentID, GroupID, AgentID);
        }

        public GroupMembershipData GetAgentActiveMembership(UUID requestingAgentID, UUID AgentID)
        {
            return GroupsConnector.GetGroupMembershipData(requestingAgentID,
                                                          UUID.Zero,
                                                          AgentID);
        }

        public List<GroupMembershipData> GetAgentGroupMemberships(UUID requestingAgentID, UUID AgentID)
        {
            return GroupsConnector.GetAgentGroupMemberships(requestingAgentID, AgentID);
        }

        public List<GroupRolesData> GetAgentGroupRoles(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            return GroupsConnector.GetAgentGroupRoles(requestingAgentID, AgentID, GroupID);
        }

        public List<GroupRolesData> GetGroupRoles(UUID requestingAgentID, UUID GroupID)
        {
            return GroupsConnector.GetGroupRoles(requestingAgentID, GroupID);
        }

        public List<GroupMembersData> GetGroupMembers(UUID requestingAgentID, UUID GroupID)
        {
            return GroupsConnector.GetGroupMembers(requestingAgentID, GroupID);
        }

        public List<GroupRoleMembersData> GetGroupRoleMembers(UUID requestingAgentID, UUID GroupID)
        {
            return GroupsConnector.GetGroupRoleMembers(requestingAgentID, GroupID);
        }

        public List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, UUID GroupID)
        {
            return GetGroupNotices(requestingAgentID, 0, 0, GroupID);
        }

        public List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, uint start, uint count, UUID GroupID)
        {
            return GroupsConnector.GetGroupNotices(requestingAgentID, start, count, GroupID);
        }

        public List<GroupTitlesData> GetGroupTitles(UUID requestingAgentID, UUID GroupID)
        {
            return GroupsConnector.GetGroupTitles(requestingAgentID, GroupID);
        }

        public uint GetNumberOfGroupNotices(UUID requestingAgentID, UUID GroupID)
        {
            return GroupsConnector.GetNumberOfGroupNotices(requestingAgentID, GroupID);
        }

        public uint GetNumberOfGroupNotices(UUID requestingAgentID, List<UUID> GroupIDs)
        {
            return GroupsConnector.GetNumberOfGroupNotices(requestingAgentID, GroupIDs);
        }

        public GroupNoticeData GetGroupNoticeData(UUID requestingAgentID, UUID noticeID)
        {
            return GroupsConnector.GetGroupNoticeData(requestingAgentID, noticeID);
        }

        public GroupNoticeInfo GetGroupNotice(UUID requestingAgentID, UUID noticeID)
        {
            return GroupsConnector.GetGroupNotice(requestingAgentID, noticeID);
        }

        public void AddGroupNotice(UUID requestingAgentID, UUID groupID, UUID noticeID, string fromName, string subject,
                                   string message, UUID ItemID, int AssetType, string ItemName)
        {
            GroupsConnector.AddGroupNotice(requestingAgentID, groupID, noticeID, fromName, subject, message, ItemID,
                                           AssetType, ItemName);
        }

        public void AddGroupProposal(UUID agentID, GroupProposalInfo info)
        {
            GroupsConnector.AddGroupProposal(agentID, info);
        }

        public void VoteOnActiveProposals(UUID agentID, UUID groupID, UUID proposalID, string vote)
        {
            GroupsConnector.VoteOnActiveProposals(agentID, groupID, proposalID, vote);
        }

        public List<GroupInviteInfo> GetGroupInvites(UUID requestingAgentID)
        {
            return GroupsConnector.GetGroupInvites(requestingAgentID);
        }

        public List<GroupProposalInfo> GetActiveProposals(UUID agentID, UUID groupID)
        {
            return GroupsConnector.GetActiveProposals(agentID, groupID);
        }

        public List<GroupProposalInfo> GetInactiveProposals(UUID agentID, UUID groupID)
        {
            return GroupsConnector.GetInactiveProposals(agentID, groupID);
        }

        #endregion

        #region INonSharedRegionModule Members

        public string Name
        {
            get { return "AuroraDataGroupsServicesConnectorModule"; }
        }

        // this module is not intended to be replaced, but there should only be 1 of them.
        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource config)
        {
            IConfig groupsConfig = config.Configs["Groups"];

            if (groupsConfig == null)
            {
                // Do not run this module by default.
                return;
            }
            else
            {
                // if groups aren't enabled, we're not needed.
                // if we're not specified as the connector to use, then we're not wanted
                if ((groupsConfig.GetBoolean("Enabled", false) == false)
                    || (groupsConfig.GetString("ServicesConnectorModule", "Default") != Name))
                {
                    m_connectorEnabled = false;
                    return;
                }

                //MainConsole.Instance.InfoFormat("[AURORA-GROUPS-CONNECTOR]: Initializing {0}", this.Name);

                m_connectorEnabled = true;
            }
        }

        public void Close()
        {
            MainConsole.Instance.InfoFormat("[AURORA-GROUPS-CONNECTOR]: Closing {0}", this.Name);
        }

        public void AddRegion(IScene scene)
        {
            GroupsConnector = Framework.Utilities.DataManager.RequestPlugin<IGroupsServiceConnector>();
            if (GroupsConnector == null)
            {
                MainConsole.Instance.Warn("[AURORA-GROUPS-CONNECTOR]: GroupsConnector is null");
                m_connectorEnabled = false;
            }
            if (m_connectorEnabled)
            {
                if (m_accountService == null)
                {
                    m_accountService = scene.UserAccountService;
                }
                scene.RegisterModuleInterface<IGroupsServicesConnector>(this);
            }
        }

        public void RemoveRegion(IScene scene)
        {
            if (scene.RequestModuleInterface<IGroupsServicesConnector>() == this)
            {
                scene.UnregisterModuleInterface<IGroupsServicesConnector>(this);
            }
        }

        public void RegionLoaded(IScene scene)
        {
        }

        #endregion

        public GroupProfileData GetMemberGroupProfile(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            GroupMembershipData MemberInfo = GroupsConnector.GetGroupMembershipData(requestingAgentID, GroupID, AgentID);
            GroupProfileData MemberGroupProfile = GroupsConnector.GetMemberGroupProfile(requestingAgentID, GroupID,
                                                                                        AgentID);

            MemberGroupProfile.MemberTitle = MemberInfo.GroupTitle;
            MemberGroupProfile.PowersMask = MemberInfo.GroupPowers;

            return MemberGroupProfile;
        }
    }
}