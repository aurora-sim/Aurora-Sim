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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using log4net;
using Nini.Config;

using OpenMetaverse;
using OpenMetaverse.StructuredData;

using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
using Aurora.Framework;
using Aurora.DataManager;

namespace OpenSim.Region.OptionalModules.Avatar.XmlRpcGroups
{
    public class AuroraDataGroupsServicesConnectorModule : ISharedRegionModule, IGroupsServicesConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public const GroupPowers m_DefaultEveryonePowers = GroupPowers.AllowSetHome | 
            GroupPowers.Accountable | 
            GroupPowers.JoinChat | 
            GroupPowers.AllowVoiceChat | 
            GroupPowers.ReceiveNotices | 
            GroupPowers.StartProposal | 
            GroupPowers.VoteOnProposal;

        private bool m_connectorEnabled = false;

        private string m_groupsServerURI = string.Empty;

        private string m_groupReadKey  = string.Empty;
        private string m_groupWriteKey = string.Empty;

        private IUserAccountService m_accountService = null;

        // Used to track which agents are have dropped from a group chat session
        // Should be reset per agent, on logon
        // TODO: move this to Flotsam XmlRpc Service
        // SessionID, List<AgentID>
        private Dictionary<UUID, List<UUID>> m_groupsAgentsDroppedFromChatSession = new Dictionary<UUID, List<UUID>>();
        private Dictionary<UUID, List<UUID>> m_groupsAgentsInvitedToChatSession = new Dictionary<UUID, List<UUID>>();

        private IGroupsServiceConnector GroupsConnector;

        #region IRegionModuleBase Members

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

                //m_log.InfoFormat("[AURORA-GROUPS-CONNECTOR]: Initializing {0}", this.Name);

                m_connectorEnabled = true;
            }
        }

        public void Close()
        {
            m_log.InfoFormat("[AURORA-GROUPS-CONNECTOR]: Closing {0}", this.Name);
        }

        public void AddRegion(OpenSim.Region.Framework.Scenes.Scene scene)
        {
            if (m_connectorEnabled)
            {
                if (m_accountService == null)
                {
                    m_accountService = scene.UserAccountService;
                }
                scene.RegisterModuleInterface<IGroupsServicesConnector>(this);
            }
        }

        public void RemoveRegion(OpenSim.Region.Framework.Scenes.Scene scene)
        {
            if (scene.RequestModuleInterface<IGroupsServicesConnector>() == this)
            {
                scene.UnregisterModuleInterface<IGroupsServicesConnector>(this);
            }
        }

        public void RegionLoaded(OpenSim.Region.Framework.Scenes.Scene scene)
        {
            GroupsConnector = Aurora.DataManager.DataManager.RequestPlugin<IGroupsServiceConnector>();
            if (GroupsConnector == null)
            {
                m_log.Warn("[AURORA-GROUPS-CONNECTOR]: GroupsConnector is null");
                m_connectorEnabled = false;
            }
        }

        #endregion

        #region ISharedRegionModule Members

        public void PostInitialise()
        {
            // NoOp
        }

        #endregion

        #region IGroupsServicesConnector Members

        /// <summary>
        /// Create a Group, including Everyone and Owners Role, place FounderID in both groups, select Owner as selected role, and newly created group as agent's active role.
        /// </summary>
        public UUID CreateGroup(UUID requestingAgentID, string name, string charter, bool showInList, UUID insigniaID, 
                                int membershipFee, bool openEnrollment, bool allowPublish, 
                                bool maturePublish, UUID founderID)
        {
            UUID GroupID = UUID.Random();
            UUID OwnerRoleID = UUID.Random();

            // Would this be cleaner as (GroupPowers)ulong.MaxValue;
            GroupPowers OwnerPowers = GroupPowers.Accountable
                                    | GroupPowers.AllowEditLand
                                    | GroupPowers.AllowFly
                                    | GroupPowers.AllowLandmark
                                    | GroupPowers.AllowRez
                                    | GroupPowers.AllowSetHome
                                    | GroupPowers.AllowVoiceChat
                                    | GroupPowers.AssignMember
                                    | GroupPowers.AssignMemberLimited
                                    | GroupPowers.ChangeActions
                                    | GroupPowers.ChangeIdentity
                                    | GroupPowers.ChangeMedia
                                    | GroupPowers.ChangeOptions
                                    | GroupPowers.CreateRole
                                    | GroupPowers.DeedObject
                                    | GroupPowers.DeleteRole
                                    | GroupPowers.Eject
                                    | GroupPowers.FindPlaces
                                    | GroupPowers.Invite
                                    | GroupPowers.JoinChat
                                    | GroupPowers.LandChangeIdentity
                                    | GroupPowers.LandDeed
                                    | GroupPowers.LandDivideJoin
                                    | GroupPowers.LandEdit
                                    | GroupPowers.LandEjectAndFreeze
                                    | GroupPowers.LandGardening
                                    | GroupPowers.LandManageAllowed
                                    | GroupPowers.LandManageBanned
                                    | GroupPowers.LandManagePasses
                                    | GroupPowers.LandOptions
                                    | GroupPowers.LandRelease
                                    | GroupPowers.LandSetSale
                                    | GroupPowers.ModerateChat
                                    | GroupPowers.ObjectManipulate
                                    | GroupPowers.ObjectSetForSale
                                    | GroupPowers.ReceiveNotices
                                    | GroupPowers.RemoveMember
                                    | GroupPowers.ReturnGroupOwned
                                    | GroupPowers.ReturnGroupSet
                                    | GroupPowers.ReturnNonGroup
                                    | GroupPowers.RoleProperties
                                    | GroupPowers.SendNotices
                                    | GroupPowers.SetLandingPoint
                                    | GroupPowers.StartProposal
                                    | GroupPowers.VoteOnProposal;
            
            GroupsConnector.CreateGroup(GroupID, name, charter, showInList,
                insigniaID, 0, openEnrollment, allowPublish, maturePublish, founderID,
                ((ulong)m_DefaultEveryonePowers), OwnerRoleID, ((ulong)OwnerPowers));

            return GroupID;
        }

        public void UpdateGroup(UUID requestingAgentID, UUID groupID, string charter, bool showInList, 
                                UUID insigniaID, int membershipFee, bool openEnrollment, 
                                bool allowPublish, bool maturePublish)
        {
            GroupsConnector.UpdateGroup(requestingAgentID, groupID, charter,
                showInList == true ? 1 : 0, insigniaID, membershipFee,
                openEnrollment == true ? 1 : 0, allowPublish == true ? 1 : 0,
                maturePublish == true ? 1 : 0);
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
            Hashtable param = new Hashtable();
            if (GroupID != UUID.Zero)
            {
                param["GroupID"] = GroupID.ToString();
            }
            if ((GroupName != null) && (GroupName != string.Empty))
            {
                param["Name"] = GroupName.ToString();
            }

            return GroupsConnector.GetGroupRecord(requestingAgentID, GroupID, GroupName);
        }

        public GroupProfileData GetMemberGroupProfile(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            GroupMembershipData MemberInfo = GroupsConnector.GetGroupMembershipData(requestingAgentID, GroupID, AgentID);
            GroupProfileData MemberGroupProfile = GroupsConnector.GetMemberGroupProfile(requestingAgentID, GroupID, AgentID);

            MemberGroupProfile.MemberTitle = MemberInfo.GroupTitle;
            MemberGroupProfile.PowersMask = MemberInfo.GroupPowers;

            return MemberGroupProfile;
        }

        public void SetAgentActiveGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            GroupsConnector.SetAgentActiveGroup(AgentID, GroupID);
        }

        public void SetAgentActiveGroupRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            GroupsConnector.SetAgentGroupSelectedRole(AgentID, GroupID, RoleID);
        }

        public void SetAgentGroupInfo(UUID requestingAgentID, UUID AgentID, UUID GroupID, bool AcceptNotices, bool ListInProfile)
        {
            GroupsConnector.SetAgentGroupInfo(requestingAgentID, AgentID, GroupID, AcceptNotices ? 1 : 0, ListInProfile ? 1 : 0);
        }

        public void AddAgentToGroupInvite(UUID requestingAgentID, UUID inviteID, UUID groupID, UUID roleID, UUID agentID, string FromAgentName)
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
            GroupsConnector.AddAgentToGroup(AgentID, GroupID, RoleID);
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

        public List<DirGroupsReplyData> FindGroups(UUID requestingAgentID, string search, int queryStart, uint queryflags)
        {
            //TODO: Fix this.. should be in the search module
            return GroupsConnector.FindGroups(requestingAgentID, search, queryStart, queryflags);
        }

        public GroupMembershipData GetAgentGroupMembership(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            return GroupsConnector.GetGroupMembershipData(requestingAgentID, GroupID, AgentID);
        }

        public GroupMembershipData GetAgentActiveMembership(UUID requestingAgentID, UUID AgentID)
        {
            return GroupsConnector.GetGroupMembershipData(requestingAgentID, GroupsConnector.GetAgentActiveGroup(requestingAgentID, AgentID), AgentID);
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
            return GroupsConnector.GetGroupNotices(requestingAgentID, GroupID);
        }

        public GroupNoticeInfo GetGroupNotice(UUID requestingAgentID, UUID noticeID)
        {
            return GroupsConnector.GetGroupNotice(requestingAgentID, noticeID);
        }

        public void AddGroupNotice(UUID requestingAgentID, UUID groupID, UUID noticeID, string fromName, string subject, string message, UUID ItemID, int AssetType, string ItemName)
        {
            GroupsConnector.AddGroupNotice(requestingAgentID, groupID, noticeID, fromName, subject, message, ItemID, AssetType, ItemName);
        }

        public void ResetAgentGroupChatSessions(UUID agentID)
        {
            foreach (List<UUID> agentList in m_groupsAgentsDroppedFromChatSession.Values)
            {
                agentList.Remove(agentID);
            }
        }

        public bool hasAgentBeenInvitedToGroupChatSession(UUID agentID, UUID groupID)
        {
            // If we're  tracking this group, and we can find them in the tracking, then they've been invited
            return m_groupsAgentsInvitedToChatSession.ContainsKey(groupID)
                && m_groupsAgentsInvitedToChatSession[groupID].Contains(agentID);
        }

        public bool hasAgentDroppedGroupChatSession(UUID agentID, UUID groupID)
        {
            // If we're tracking drops for this group, 
            // and we find them, well... then they've dropped
            return m_groupsAgentsDroppedFromChatSession.ContainsKey(groupID)
                && m_groupsAgentsDroppedFromChatSession[groupID].Contains(agentID);
        }

        public void AgentDroppedFromGroupChatSession(UUID agentID, UUID groupID)
        {
            if (m_groupsAgentsDroppedFromChatSession.ContainsKey(groupID))
            {
                // If not in dropped list, add
                if (!m_groupsAgentsDroppedFromChatSession[groupID].Contains(agentID))
                {
                    m_groupsAgentsDroppedFromChatSession[groupID].Add(agentID);
                }
            }
        }

        public void AgentInvitedToGroupChatSession(UUID agentID, UUID groupID)
        {
            // Add Session Status if it doesn't exist for this session
            CreateGroupChatSessionTracking(groupID);

            // If nessesary, remove from dropped list
            if (m_groupsAgentsDroppedFromChatSession[groupID].Contains(agentID))
            {
                m_groupsAgentsDroppedFromChatSession[groupID].Remove(agentID);
            }
        }

        private void CreateGroupChatSessionTracking(UUID groupID)
        {
            if (!m_groupsAgentsDroppedFromChatSession.ContainsKey(groupID))
            {
                m_groupsAgentsDroppedFromChatSession.Add(groupID, new List<UUID>());
                m_groupsAgentsInvitedToChatSession.Add(groupID, new List<UUID>());
            }

        }

        #endregion
        
        /// <summary>
        /// Group Request Tokens are an attempt to allow the groups service to authenticate 
        /// requests.  
        /// TODO: This broke after the big grid refactor, either find a better way, or discard this
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private void GetClientGroupRequestID(UUID AgentID, out string UserServiceURL, out UUID SessionID)
        {
            UserServiceURL = "";
            SessionID = UUID.Zero;
        }

        public List<GroupInviteInfo> GetGroupInvites(UUID requestingAgentID)
        {
            return GroupsConnector.GetGroupInvites(requestingAgentID);
        }
    }
}