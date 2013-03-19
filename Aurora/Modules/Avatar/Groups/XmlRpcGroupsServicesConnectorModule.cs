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

using Aurora.Framework;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChatSessionMember = Aurora.Framework.ChatSessionMember;

namespace Aurora.Modules.Groups
{
    public class XmlRpcGroupsServicesConnectorModule : INonSharedRegionModule, IGroupsServicesConnector
    {
        public const GroupPowers m_DefaultEveryonePowers = GroupPowers.AllowSetHome |
                                                           GroupPowers.Accountable |
                                                           GroupPowers.JoinChat |
                                                           GroupPowers.AllowVoiceChat |
                                                           GroupPowers.ReceiveNotices |
                                                           GroupPowers.StartProposal |
                                                           GroupPowers.VoteOnProposal;

        private IUserAccountService m_accountService;
        private int m_cacheTimeout = 30;

        private bool m_connectorEnabled;

        private bool m_disableKeepAlive;

        private string m_groupReadKey = string.Empty;
        private string m_groupWriteKey = string.Empty;

        // Used to track which agents are have dropped from a group chat session
        // Should be reset per agent, on logon
        // SessionID, List<AgentID>
        private Dictionary<UUID, List<UUID>> m_groupsAgentsDroppedFromChatSession = new Dictionary<UUID, List<UUID>>();
        private Dictionary<UUID, List<UUID>> m_groupsAgentsInvitedToChatSession = new Dictionary<UUID, List<UUID>>();
        private string m_groupsServerURI = string.Empty;
        private ExpiringCache<string, XmlRpcResponse> m_memoryCache;

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

            Hashtable param = new Hashtable();
            param["GroupID"] = GroupID.ToString();
            param["Name"] = name;
            param["Charter"] = charter;
            param["ShowInList"] = showInList ? 1 : 0;
            param["InsigniaID"] = insigniaID.ToString();
            param["MembershipFee"] = 0;
            param["OpenEnrollment"] = openEnrollment ? 1 : 0;
            param["AllowPublish"] = allowPublish ? 1 : 0;
            param["MaturePublish"] = maturePublish ? 1 : 0;
            param["FounderID"] = founderID.ToString();
            param["EveryonePowers"] = ((ulong) m_DefaultEveryonePowers).ToString();
            param["OwnerRoleID"] = OwnerRoleID.ToString();

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
            param["OwnersPowers"] = ((ulong) OwnerPowers).ToString();


            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.createGroup", param);

            if (respData.Contains("error"))
            {
                // UUID is not nullable

                return UUID.Zero;
            }

            return UUID.Parse((string) respData["GroupID"]);
        }

        public void UpdateGroup(UUID requestingAgentID, UUID groupID, string charter, bool showInList,
                                UUID insigniaID, int membershipFee, bool openEnrollment,
                                bool allowPublish, bool maturePublish)
        {
            Hashtable param = new Hashtable();
            param["GroupID"] = groupID.ToString();
            param["Charter"] = charter;
            param["ShowInList"] = showInList ? 1 : 0;
            param["InsigniaID"] = insigniaID.ToString();
            param["MembershipFee"] = membershipFee;
            param["OpenEnrollment"] = openEnrollment ? 1 : 0;
            param["AllowPublish"] = allowPublish ? 1 : 0;
            param["MaturePublish"] = maturePublish ? 1 : 0;

            XmlRpcCall(requestingAgentID, "groups.updateGroup", param);
        }

        public void AddGroupRole(UUID requestingAgentID, UUID groupID, UUID roleID, string name, string description,
                                 string title, ulong powers)
        {
            Hashtable param = new Hashtable();
            param["GroupID"] = groupID.ToString();
            param["RoleID"] = roleID.ToString();
            param["Name"] = name;
            param["Description"] = description;
            param["Title"] = title;
            param["Powers"] = powers.ToString();

            XmlRpcCall(requestingAgentID, "groups.addRoleToGroup", param);
        }

        public void RemoveGroupRole(UUID requestingAgentID, UUID groupID, UUID roleID)
        {
            Hashtable param = new Hashtable();
            param["GroupID"] = groupID.ToString();
            param["RoleID"] = roleID.ToString();

            XmlRpcCall(requestingAgentID, "groups.removeRoleFromGroup", param);
        }

        public void UpdateGroupRole(UUID requestingAgentID, UUID groupID, UUID roleID, string name, string description,
                                    string title, ulong powers)
        {
            Hashtable param = new Hashtable();
            param["GroupID"] = groupID.ToString();
            param["RoleID"] = roleID.ToString();
            if (name != null)
            {
                param["Name"] = name;
            }
            if (description != null)
            {
                param["Description"] = description;
            }
            if (title != null)
            {
                param["Title"] = title;
            }
            param["Powers"] = powers.ToString();

            XmlRpcCall(requestingAgentID, "groups.updateGroupRole", param);
        }

        public GroupRecord GetGroupRecord(UUID requestingAgentID, UUID GroupID, string GroupName)
        {
            Hashtable param = new Hashtable();
            if (GroupID != UUID.Zero)
            {
                param["GroupID"] = GroupID.ToString();
            }
            if (!string.IsNullOrEmpty(GroupName))
            {
                param["Name"] = GroupName;
            }

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getGroup", param);

            if (respData.Contains("error"))
            {
                return null;
            }

            return GroupProfileHashtableToGroupRecord(respData);
        }

        public string SetAgentActiveGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            Hashtable param = new Hashtable();
            param["AgentID"] = AgentID.ToString();
            param["GroupID"] = GroupID.ToString();

            XmlRpcCall(requestingAgentID, "groups.setAgentActiveGroup", param);
            return GetAgentGroupMembership(requestingAgentID, AgentID, GroupID).GroupTitle;
        }

        public string SetAgentActiveGroupRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            Hashtable param = new Hashtable();
            param["AgentID"] = AgentID.ToString();
            param["GroupID"] = GroupID.ToString();
            param["SelectedRoleID"] = RoleID.ToString();

            XmlRpcCall(requestingAgentID, "groups.setAgentGroupInfo", param);
            return GetAgentGroupMembership(requestingAgentID, AgentID, GroupID).GroupTitle;
        }

        public void SetAgentGroupInfo(UUID requestingAgentID, UUID AgentID, UUID GroupID, bool AcceptNotices,
                                      bool ListInProfile)
        {
            Hashtable param = new Hashtable();
            param["AgentID"] = AgentID.ToString();
            param["GroupID"] = GroupID.ToString();
            param["AcceptNotices"] = AcceptNotices ? "1" : "0";
            param["ListInProfile"] = ListInProfile ? "1" : "0";

            XmlRpcCall(requestingAgentID, "groups.setAgentGroupInfo", param);
        }

        public void AddAgentToGroupInvite(UUID requestingAgentID, UUID inviteID, UUID groupID, UUID roleID, UUID agentID,
                                          string FromAgentName)
        {
            Hashtable param = new Hashtable();
            param["InviteID"] = inviteID.ToString();
            param["AgentID"] = agentID.ToString();
            param["RoleID"] = roleID.ToString();
            param["GroupID"] = groupID.ToString();

            XmlRpcCall(requestingAgentID, "groups.addAgentToGroupInvite", param);
        }

        public GroupInviteInfo GetAgentToGroupInvite(UUID requestingAgentID, UUID inviteID)
        {
            Hashtable param = new Hashtable();
            param["InviteID"] = inviteID.ToString();

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getAgentToGroupInvite", param);

            if (respData.Contains("error"))
            {
                return null;
            }

            GroupInviteInfo inviteInfo = new GroupInviteInfo
                                             {
                                                 InviteID = inviteID,
                                                 GroupID = UUID.Parse((string) respData["GroupID"]),
                                                 RoleID = UUID.Parse((string) respData["RoleID"]),
                                                 AgentID = UUID.Parse((string) respData["AgentID"])
                                             };

            return inviteInfo;
        }

        public void RemoveAgentToGroupInvite(UUID requestingAgentID, UUID inviteID)
        {
            Hashtable param = new Hashtable();
            param["InviteID"] = inviteID.ToString();

            XmlRpcCall(requestingAgentID, "groups.removeAgentToGroupInvite", param);
        }

        public void AddAgentToGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            Hashtable param = new Hashtable();
            param["AgentID"] = AgentID.ToString();
            param["GroupID"] = GroupID.ToString();
            param["RoleID"] = RoleID.ToString();

            XmlRpcCall(requestingAgentID, "groups.addAgentToGroup", param);
        }

        public bool RemoveAgentFromGroup(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            Hashtable param = new Hashtable();
            param["AgentID"] = AgentID.ToString();
            param["GroupID"] = GroupID.ToString();

            XmlRpcCall(requestingAgentID, "groups.removeAgentFromGroup", param);
            return true;
        }

        public void AddAgentToGroupRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            Hashtable param = new Hashtable();
            param["AgentID"] = AgentID.ToString();
            param["GroupID"] = GroupID.ToString();
            param["RoleID"] = RoleID.ToString();

            XmlRpcCall(requestingAgentID, "groups.addAgentToGroupRole", param);
        }

        public void RemoveAgentFromGroupRole(UUID requestingAgentID, UUID AgentID, UUID GroupID, UUID RoleID)
        {
            Hashtable param = new Hashtable();
            param["AgentID"] = AgentID.ToString();
            param["GroupID"] = GroupID.ToString();
            param["RoleID"] = RoleID.ToString();

            XmlRpcCall(requestingAgentID, "groups.removeAgentFromGroupRole", param);
        }

        public List<DirGroupsReplyData> FindGroups(UUID requestingAgentID, string search, uint? start, uint? count,
                                                   uint queryflags)
        {
            Hashtable param = new Hashtable();
            param["Search"] = search;

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.findGroups", param);

            List<DirGroupsReplyData> findings = new List<DirGroupsReplyData>();

            if (!respData.Contains("error"))
            {
                Hashtable results = (Hashtable) respData["results"];
                foreach (Hashtable groupFind in results.Values)
                {
                    DirGroupsReplyData data = new DirGroupsReplyData {groupID = new UUID((string) groupFind["GroupID"])};
                    data.groupName = (string) groupFind["Name"];
                    data.members = int.Parse((string) groupFind["Members"]);
                    // data.searchOrder = order;

                    findings.Add(data);
                }
            }

            return findings;
        }

        public GroupMembershipData GetAgentGroupMembership(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            Hashtable param = new Hashtable();
            param["AgentID"] = AgentID.ToString();
            param["GroupID"] = GroupID.ToString();

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getAgentGroupMembership", param);

            if (respData.Contains("error"))
            {
                return null;
            }

            GroupMembershipData data = HashTableToGroupMembershipData(respData);

            return data;
        }

        public GroupMembershipData GetAgentActiveMembership(UUID requestingAgentID, UUID AgentID)
        {
            Hashtable param = new Hashtable();
            param["AgentID"] = AgentID.ToString();

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getAgentActiveMembership", param);

            if (respData.Contains("error"))
            {
                return null;
            }

            return HashTableToGroupMembershipData(respData);
        }

        public List<GroupMembershipData> GetAgentGroupMemberships(UUID requestingAgentID, UUID AgentID)
        {
            Hashtable param = new Hashtable();
            param["AgentID"] = AgentID.ToString();

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getAgentGroupMemberships", param);

            List<GroupMembershipData> memberships = new List<GroupMembershipData>();

            if (!respData.Contains("error"))
            {
                memberships.AddRange(from object membership in respData.Values
                                     select HashTableToGroupMembershipData((Hashtable) membership));
            }

            return memberships;
        }

        public List<GroupRolesData> GetAgentGroupRoles(UUID requestingAgentID, UUID AgentID, UUID GroupID)
        {
            Hashtable param = new Hashtable();
            param["AgentID"] = AgentID.ToString();
            param["GroupID"] = GroupID.ToString();

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getAgentRoles", param);

            List<GroupRolesData> Roles = new List<GroupRolesData>();

            if (respData.Contains("error"))
            {
                return Roles;
            }

            Roles.AddRange(from Hashtable role in respData.Values
                           select new GroupRolesData
                                      {
                                          RoleID = new UUID((string) role["RoleID"]),
                                          Name = (string) role["Name"],
                                          Description = (string) role["Description"],
                                          Powers = ulong.Parse((string) role["Powers"]),
                                          Title = (string) role["Title"]
                                      });

            return Roles;
        }

        public List<GroupRolesData> GetGroupRoles(UUID requestingAgentID, UUID GroupID)
        {
            Hashtable param = new Hashtable();
            param["GroupID"] = GroupID.ToString();

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getGroupRoles", param);

            List<GroupRolesData> Roles = new List<GroupRolesData>();

            if (respData.Contains("error"))
            {
                return Roles;
            }

            Roles.AddRange(from Hashtable role in respData.Values
                           select new GroupRolesData
                                      {
                                          Description = (string) role["Description"],
                                          Members = int.Parse((string) role["Members"]),
                                          Name = (string) role["Name"],
                                          Powers = ulong.Parse((string) role["Powers"]),
                                          RoleID = new UUID((string) role["RoleID"]),
                                          Title = (string) role["Title"]
                                      });

            return Roles;
        }

        public List<GroupMembersData> GetGroupMembers(UUID requestingAgentID, UUID GroupID)
        {
            Hashtable param = new Hashtable();
            param["GroupID"] = GroupID.ToString();

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getGroupMembers", param);

            List<GroupMembersData> members = new List<GroupMembersData>();

            if (respData.Contains("error"))
            {
                return members;
            }

            members.AddRange(from Hashtable membership in respData.Values
                             select new GroupMembersData
                                        {
                                            AcceptNotices = ((string) membership["AcceptNotices"]) == "1",
                                            AgentID = new UUID((string) membership["AgentID"]),
                                            Contribution = int.Parse((string) membership["Contribution"]),
                                            IsOwner = ((string) membership["IsOwner"]) == "1",
                                            ListInProfile = ((string) membership["ListInProfile"]) == "1",
                                            AgentPowers = ulong.Parse((string) membership["AgentPowers"]),
                                            Title = (string) membership["Title"]
                                        });

            return members;
        }

        public List<GroupRoleMembersData> GetGroupRoleMembers(UUID requestingAgentID, UUID GroupID)
        {
            Hashtable param = new Hashtable();
            param["GroupID"] = GroupID.ToString();

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getGroupRoleMembers", param);

            List<GroupRoleMembersData> members = new List<GroupRoleMembersData>();

            if (!respData.Contains("error"))
            {
                members.AddRange(from Hashtable membership in respData.Values
                                 select new GroupRoleMembersData
                                            {
                                                MemberID = new UUID((string) membership["AgentID"]),
                                                RoleID = new UUID((string) membership["RoleID"])
                                            });
            }
            return members;
        }

        public List<GroupNoticeData> GetGroupNotices(UUID requestingAgentID, UUID GroupID)
        {
            Hashtable param = new Hashtable();
            param["GroupID"] = GroupID.ToString();

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getGroupNotices", param);

            List<GroupNoticeData> values = new List<GroupNoticeData>();

            if (!respData.Contains("error"))
            {
                values.AddRange(from Hashtable value in respData.Values
                                select new GroupNoticeData
                                           {
                                               NoticeID = UUID.Parse((string) value["NoticeID"]),
                                               Timestamp = uint.Parse((string) value["Timestamp"]),
                                               FromName = (string) value["FromName"],
                                               Subject = (string) value["Subject"],
                                               HasAttachment = false,
                                               AssetType = 0
                                           });
            }
            return values;
        }

        public GroupNoticeInfo GetGroupNotice(UUID requestingAgentID, UUID noticeID)
        {
            Hashtable param = new Hashtable();
            param["NoticeID"] = noticeID.ToString();

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getGroupNotice", param);


            if (respData.Contains("error"))
            {
                return null;
            }

            GroupNoticeInfo data = new GroupNoticeInfo
                                       {
                                           GroupID = UUID.Parse((string) respData["GroupID"]),
                                           Message = (string) respData["Message"],
                                           BinaryBucket =
                                               Utils.HexStringToBytes((string) respData["BinaryBucket"], true),
                                           noticeData =
                                               {
                                                   NoticeID = UUID.Parse((string) respData["NoticeID"]),
                                                   Timestamp = uint.Parse((string) respData["Timestamp"]),
                                                   FromName = (string) respData["FromName"],
                                                   Subject = (string) respData["Subject"],
                                                   HasAttachment = false,
                                                   AssetType = 0
                                               }
                                       };

            if (data.Message == null)
            {
                data.Message = string.Empty;
            }

            return data;
        }

        public void AddGroupNotice(UUID requestingAgentID, UUID groupID, UUID noticeID, string fromName, string subject,
                                   string message, UUID ItemID, int AssetType, string ItemName)
        {
            string binBucket = Utils.BytesToHexString(new byte[0], "");

            Hashtable param = new Hashtable();
            param["GroupID"] = groupID.ToString();
            param["NoticeID"] = noticeID.ToString();
            param["FromName"] = fromName;
            param["Subject"] = subject;
            param["Message"] = message;
            param["BinaryBucket"] = binBucket;
            param["TimeStamp"] = ((uint) Util.UnixTimeSinceEpoch()).ToString();

            XmlRpcCall(requestingAgentID, "groups.addGroupNotice", param);
        }

        public List<GroupInviteInfo> GetGroupInvites(UUID requestingAgentID)
        {
            Hashtable param = new Hashtable();
            param["AgentID"] = requestingAgentID.ToString();
            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getGroupInvites", param);
            if (!respData.Contains("error"))
            {
                List<GroupInviteInfo> GroupInvites = new List<GroupInviteInfo>();
                Hashtable results = (Hashtable) respData["results"];
                if (results != null)
                {
                    GroupInvites.AddRange(from Hashtable invite in results.Values
                                          select new GroupInviteInfo
                                                     {
                                                         AgentID = new UUID((string) invite["AgentID"]),
                                                         GroupID = new UUID((string) invite["GroupID"]),
                                                         InviteID = new UUID((string) invite["InviteID"]),
                                                         RoleID = new UUID((string) invite["RoleID"])
                                                     });
                }
                return GroupInvites;
            }
            return new List<GroupInviteInfo>();
        }

        public void AddGroupProposal(UUID agentID, GroupProposalInfo info)
        {
        }

        public bool CreateSession(ChatSession chatSession)
        {
            return false;
        }

        public void AddMemberToGroup(ChatSessionMember chatSessionMember, UUID GroupID)
        {
        }

        public ChatSession GetSession(UUID SessionID)
        {
            return null;
        }

        public ChatSessionMember FindMember(UUID sessionid, UUID Agent)
        {
            return null;
        }

        public void RemoveSession(UUID sessionid)
        {
        }

        #endregion

        #region XmlRpcHashtableMarshalling

        private GroupProfileData GroupProfileHashtableToGroupProfileData(Hashtable groupProfile)
        {
            GroupProfileData group = new GroupProfileData();
            group.GroupID = UUID.Parse((string) groupProfile["GroupID"]);
            group.Name = (string) groupProfile["Name"];

            if (groupProfile["Charter"] != null)
            {
                group.Charter = (string) groupProfile["Charter"];
            }

            group.ShowInList = ((string) groupProfile["ShowInList"]) == "1";
            group.InsigniaID = UUID.Parse((string) groupProfile["InsigniaID"]);
            group.MembershipFee = int.Parse((string) groupProfile["MembershipFee"]);
            group.OpenEnrollment = ((string) groupProfile["OpenEnrollment"]) == "1";
            group.AllowPublish = ((string) groupProfile["AllowPublish"]) == "1";
            group.MaturePublish = ((string) groupProfile["MaturePublish"]) == "1";
            group.FounderID = UUID.Parse((string) groupProfile["FounderID"]);
            group.OwnerRole = UUID.Parse((string) groupProfile["OwnerRoleID"]);

            group.GroupMembershipCount = int.Parse((string) groupProfile["GroupMembershipCount"]);
            group.GroupRolesCount = int.Parse((string) groupProfile["GroupRolesCount"]);

            return group;
        }

        private GroupRecord GroupProfileHashtableToGroupRecord(Hashtable groupProfile)
        {
            GroupRecord group = new GroupRecord();
            group.GroupID = UUID.Parse((string) groupProfile["GroupID"]);
            group.GroupName = groupProfile["Name"].ToString();
            if (groupProfile["Charter"] != null)
            {
                group.Charter = (string) groupProfile["Charter"];
            }
            group.ShowInList = ((string) groupProfile["ShowInList"]) == "1";
            group.GroupPicture = UUID.Parse((string) groupProfile["InsigniaID"]);
            group.MembershipFee = int.Parse((string) groupProfile["MembershipFee"]);
            group.OpenEnrollment = ((string) groupProfile["OpenEnrollment"]) == "1";
            group.AllowPublish = ((string) groupProfile["AllowPublish"]) == "1";
            group.MaturePublish = ((string) groupProfile["MaturePublish"]) == "1";
            group.FounderID = UUID.Parse((string) groupProfile["FounderID"]);
            group.OwnerRoleID = UUID.Parse((string) groupProfile["OwnerRoleID"]);

            return group;
        }

        private static GroupMembershipData HashTableToGroupMembershipData(Hashtable respData)
        {
            GroupMembershipData data = new GroupMembershipData
                                           {
                                               AcceptNotices = ((string) respData["AcceptNotices"] == "1"),
                                               Contribution = int.Parse((string) respData["Contribution"]),
                                               ListInProfile = ((string) respData["ListInProfile"] == "1"),
                                               ActiveRole = new UUID((string) respData["SelectedRoleID"]),
                                               GroupTitle = (string) respData["Title"],
                                               GroupPowers = ulong.Parse((string) respData["GroupPowers"]),
                                               GroupID = new UUID((string) respData["GroupID"])
                                           };


            // Is this group the agent's active group


            UUID ActiveGroup = new UUID((string) respData["ActiveGroupID"]);
            data.Active = data.GroupID.Equals(ActiveGroup);

            data.AllowPublish = ((string) respData["AllowPublish"] == "1");
            if (respData["Charter"] != null)
            {
                data.Charter = (string) respData["Charter"];
            }
            data.FounderID = new UUID((string) respData["FounderID"]);
            data.GroupID = new UUID((string) respData["GroupID"]);
            data.GroupName = (string) respData["GroupName"];
            data.GroupPicture = new UUID((string) respData["InsigniaID"]);
            data.MaturePublish = ((string) respData["MaturePublish"] == "1");
            data.MembershipFee = int.Parse((string) respData["MembershipFee"]);
            data.OpenEnrollment = ((string) respData["OpenEnrollment"] == "1");
            data.ShowInList = ((string) respData["ShowInList"] == "1");

            return data;
        }

        #endregion

        #region INonSharedRegionModule Members

        public string Name
        {
            get { return "XmlRpcGroupsServicesConnector"; }
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

                MainConsole.Instance.InfoFormat("[XMLRPC-GROUPS-CONNECTOR]: Initializing {0}", this.Name);

                m_groupsServerURI = groupsConfig.GetString("GroupsServerURI", string.Empty);
                if (string.IsNullOrEmpty(m_groupsServerURI))
                {
                    MainConsole.Instance.ErrorFormat(
                        "Please specify a valid URL for GroupsServerURI in Aurora.ini, [Groups]");
                    m_connectorEnabled = false;
                    return;
                }

                m_disableKeepAlive = groupsConfig.GetBoolean("XmlRpcDisableKeepAlive", false);

                m_groupReadKey = groupsConfig.GetString("XmlRpcServiceReadKey", string.Empty);
                m_groupWriteKey = groupsConfig.GetString("XmlRpcServiceWriteKey", string.Empty);


                m_cacheTimeout = groupsConfig.GetInt("GroupsCacheTimeout", 30);
                if (m_cacheTimeout == 0)
                {
                    MainConsole.Instance.WarnFormat("[XMLRPC-GROUPS-CONNECTOR]: Groups Cache Disabled.");
                }
                else
                {
                    MainConsole.Instance.InfoFormat("[XMLRPC-GROUPS-CONNECTOR]: Groups Cache Timeout set to {0}.",
                                                    m_cacheTimeout);
                }

                // If we got all the config options we need, lets start'er'up
                m_memoryCache = new ExpiringCache<string, XmlRpcResponse>();
                m_connectorEnabled = true;
            }
        }

        public void Close()
        {
            MainConsole.Instance.InfoFormat("[XMLRPC-GROUPS-CONNECTOR]: Closing {0}", this.Name);
        }

        public void AddRegion(IScene scene)
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

        public void RemoveRegion(IScene scene)
        {
            if (scene.RequestModuleInterface<IGroupsServicesConnector>() == this)
            {
                scene.UnregisterModuleInterface<IGroupsServicesConnector>(this);
            }
        }

        public void RegionLoaded(IScene scene)
        {
            // TODO: May want to consider listenning for Agent Connections so we can pre-cache group info
            // scene.EventManager.OnNewClient += OnNewClient;
        }

        #endregion

        public GroupProfileData GetGroupProfile(UUID requestingAgentID, UUID GroupID)
        {
            GroupProfileData profile = new GroupProfileData();

            GroupRecord groupInfo = GetGroupRecord(requestingAgentID, GroupID, null);
            if (groupInfo != null)
            {
                profile.AllowPublish = groupInfo.AllowPublish;
                profile.Charter = groupInfo.Charter;
                profile.FounderID = groupInfo.FounderID;
                profile.GroupID = GroupID;
                profile.GroupMembershipCount =
                    GetGroupMembers(requestingAgentID, GroupID).Count;
                profile.GroupRolesCount = GetGroupRoles(requestingAgentID, GroupID).Count;
                profile.InsigniaID = groupInfo.GroupPicture;
                profile.MaturePublish = groupInfo.MaturePublish;
                profile.MembershipFee = groupInfo.MembershipFee;
                profile.Money = 0; // TODO: Get this from the currency server?
                profile.Name = groupInfo.GroupName;
                profile.OpenEnrollment = groupInfo.OpenEnrollment;
                profile.OwnerRole = groupInfo.OwnerRoleID;
                profile.ShowInList = groupInfo.ShowInList;
            }

            GroupMembershipData memberInfo = GetAgentGroupMembership(requestingAgentID,
                                                                     requestingAgentID,
                                                                     GroupID);
            if (memberInfo != null)
            {
                profile.MemberTitle = memberInfo.GroupTitle;
                profile.PowersMask = memberInfo.GroupPowers;
            }

            return profile;
        }

        public List<GroupTitlesData> GetGroupTitles(UUID requestingAgentID, UUID GroupID)
        {
            List<GroupRolesData> agentRoles = GetAgentGroupRoles(requestingAgentID,
                                                                 requestingAgentID, GroupID);
            GroupMembershipData agentMembership = GetAgentGroupMembership(
                requestingAgentID, requestingAgentID, GroupID);

            List<GroupTitlesData> titles = new List<GroupTitlesData>();
            foreach (GroupRolesData role in agentRoles)
            {
                GroupTitlesData title = new GroupTitlesData {Name = role.Title, UUID = role.RoleID};
                if (agentMembership != null)
                    title.Selected = agentMembership.ActiveRole == role.RoleID;

                titles.Add(title);
            }

            return titles;
        }

        public GroupProfileData GetMemberGroupProfile(UUID requestingAgentID, UUID GroupID, UUID AgentID)
        {
            Hashtable param = new Hashtable();
            param["GroupID"] = GroupID.ToString();

            Hashtable respData = XmlRpcCall(requestingAgentID, "groups.getGroup", param);

            if (respData.Contains("error"))
            {
                // GroupProfileData is not nullable
                return new GroupProfileData();
            }

            GroupMembershipData MemberInfo = GetAgentGroupMembership(requestingAgentID, AgentID, GroupID);
            GroupProfileData MemberGroupProfile = GroupProfileHashtableToGroupProfileData(respData);

            MemberGroupProfile.MemberTitle = MemberInfo.GroupTitle;
            MemberGroupProfile.PowersMask = MemberInfo.GroupPowers;

            return MemberGroupProfile;
        }

        public List<GroupProposalInfo> GetActiveProposals(UUID agentID, UUID groupID)
        {
            return new List<GroupProposalInfo>();
        }

        public List<GroupProposalInfo> GetInactiveProposals(UUID agentID, UUID groupID)
        {
            return new List<GroupProposalInfo>();
        }

        public void VoteOnActiveProposals(UUID agentID, UUID groupID, UUID proposalID, string vote)
        {
        }

        /// <summary>
        ///     Encapsulate the XmlRpc call to standardize security and error handling.
        /// </summary>
        private Hashtable XmlRpcCall(UUID requestingAgentID, string function, Hashtable param)
        {
            XmlRpcResponse resp = null;
            string CacheKey = null;

            // Only bother with the cache if it isn't disabled.
            if (m_cacheTimeout > 0)
            {
                if (!function.StartsWith("groups.get"))
                {
                    // Any and all updates cause the cache to clear
                    m_memoryCache.Clear();
                }
                else
                {
                    StringBuilder sb = new StringBuilder(requestingAgentID + function);
#if (!ISWIN)
                    foreach (object key in param.Keys)
                    {
                        if (param[key] != null)
                        {
                            sb.AppendFormat(",{0}:{1}", key, param[key]);
                        }
                    }
#else
                    foreach (object key in param.Keys.Cast<object>().Where(key => param[key] != null))
                    {
                        sb.AppendFormat(",{0}:{1}", key, param[key]);
                    }
#endif

                    CacheKey = sb.ToString();
                    m_memoryCache.TryGetValue(CacheKey, out resp);
                }
            }

            if (resp == null)
            {
                param.Add("RequestingAgentID", requestingAgentID.ToString());
                param.Add("RequestingAgentUserService", "");
                param.Add("RequestingSessionID", "");
                param.Add("ReadKey", m_groupReadKey);
                param.Add("WriteKey", m_groupWriteKey);

                IList parameters = new ArrayList();
                parameters.Add(param);

                ConfigurableKeepAliveXmlRpcRequest req;
                req = new ConfigurableKeepAliveXmlRpcRequest(function, parameters, m_disableKeepAlive);

                try
                {
                    resp = req.Send(m_groupsServerURI, 10000);

                    if ((m_cacheTimeout > 0) && (CacheKey != null))
                    {
                        m_memoryCache.AddOrUpdate(CacheKey, resp, TimeSpan.FromSeconds(m_cacheTimeout));
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat(
                        "[XMLRPC-GROUPS-CONNECTOR]: An error has occured while attempting to access the XmlRpcGroups server method {0} at {1}",
                        function, m_groupsServerURI);

                    MainConsole.Instance.ErrorFormat("[XMLRPC-GROUPS-CONNECTOR]: {0}{1}", e.Message, e.StackTrace);

                    foreach (
                        string ResponseLine in
                            req.RequestResponse.Split(new[] {Environment.NewLine}, StringSplitOptions.None))
                    {
                        MainConsole.Instance.ErrorFormat("[XMLRPC-GROUPS-CONNECTOR]: {0} ", ResponseLine);
                    }

                    foreach (string key in param.Keys)
                    {
                        MainConsole.Instance.WarnFormat("[XMLRPC-GROUPS-CONNECTOR]: {0} :: {1}", key, param[key]);
                    }

                    Hashtable respData = new Hashtable {{"error", e.ToString()}};
                    return respData;
                }
            }

            if (resp.Value is Hashtable)
            {
                Hashtable respData = (Hashtable) resp.Value;
                if (respData.Contains("error") && !respData.Contains("succeed"))
                {
                    LogRespDataToConsoleError(respData);
                }

                return respData;
            }

            MainConsole.Instance.ErrorFormat(
                "[XMLRPC-GROUPS-CONNECTOR]: The XmlRpc server returned a {1} instead of a hashtable for {0}", function,
                resp.Value.GetType());

            if (resp.Value is ArrayList)
            {
                ArrayList al = (ArrayList) resp.Value;
                MainConsole.Instance.ErrorFormat("[XMLRPC-GROUPS-CONNECTOR]: Contains {0} elements", al.Count);

                foreach (object o in al)
                {
                    MainConsole.Instance.ErrorFormat("[XMLRPC-GROUPS-CONNECTOR]: {0} :: {1}", o.GetType(), o);
                }
            }
            else
            {
                MainConsole.Instance.ErrorFormat("[XMLRPC-GROUPS-CONNECTOR]: Function returned: {0}", resp.Value);
            }

            Hashtable error = new Hashtable {{"error", "invalid return value"}};
            return error;
        }

        private void LogRespDataToConsoleError(Hashtable respData)
        {
            MainConsole.Instance.Error("[XMLRPC-GROUPS-CONNECTOR]: Error:");

            foreach (string key in respData.Keys)
            {
                MainConsole.Instance.ErrorFormat("[XMLRPC-GROUPS-CONNECTOR]: Key: {0}", key);

                string[] lines = respData[key].ToString().Split(new[] {'\n'});
                foreach (string line in lines)
                {
                    MainConsole.Instance.ErrorFormat("[XMLRPC-GROUPS-CONNECTOR]: {0}", line);
                }
            }
        }
    }
}