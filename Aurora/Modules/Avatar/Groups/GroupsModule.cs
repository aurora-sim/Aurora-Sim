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
using System.IO;
using System.Linq;
using System.Reflection;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Capabilities;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Groups
{
    public class GroupsModule : ISharedRegionModule, IGroupsModule
    {
        ///<summary>
        ///  ; To use this module, you must specify the following in your Aurora.ini
        ///  [GROUPS]
        ///  Enabled = true
        /// 
        ///  Module   = GroupsModule
        ///  NoticesEnabled = true
        ///  DebugEnabled   = true
        /// 
        ///  GroupsServicesConnectorModule = XmlRpcGroupsServicesConnector
        ///  XmlRpcServiceURL      = http://osflotsam.org/xmlrpc.php
        ///  XmlRpcServiceReadKey  = 1234
        ///  XmlRpcServiceWriteKey = 1234
        /// 
        ///  MessagingModule  = GroupsMessagingModule
        ///  MessagingEnabled = true
        /// 
        ///  ; Disables HTTP Keep-Alive for Groups Module HTTP Requests, work around for
        ///  ; a problem discovered on some Windows based region servers.  Only disable
        ///  ; if you see a large number (dozens) of the following Exceptions:
        ///  ; System.Net.WebException: The request was aborted: The request was canceled.
        ///
        ///  XmlRpcDisableKeepAlive = false
        ///</summary>
        private readonly Dictionary<UUID, GroupMembershipData> m_cachedGroupTitles = new Dictionary<UUID, GroupMembershipData>();
        private readonly Dictionary<UUID, List<GroupMembershipData>> m_cachedGroupMemberships = new Dictionary<UUID, List<GroupMembershipData>>();
        private readonly List<IScene> m_sceneList = new List<IScene>();

        // Configuration settings
        private bool m_debugEnabled = true;
        //private Dictionary<UUID, UUID> GroupAttachmentCache = new Dictionary<UUID, UUID> ();
        //private Dictionary<UUID, UUID> GroupSessionIDCache = new Dictionary<UUID, UUID> (); //For offline messages
        private IGroupsServicesConnector m_groupData;
        private bool m_groupNoticesEnabled = true;
        private bool m_groupsEnabled;
        private IGroupsMessagingModule m_groupsMessagingModule;
        private IMessageTransferModule m_msgTransferModule;

        #region IGroupsModule Members

        public event NewGroupNotice OnNewGroupNotice;

        public GroupRecord GetGroupRecord(UUID GroupID)
        {
            return m_groupData.GetGroupRecord(UUID.Zero, GroupID, null);
        }

        public GroupRecord GetGroupRecord(string name)
        {
            return m_groupData.GetGroupRecord(UUID.Zero, UUID.Zero, name);
        }

        public void ActivateGroup(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            string title = m_groupData.SetAgentActiveGroup(GetRequestingAgentID(remoteClient),
                                                           GetRequestingAgentID(remoteClient), groupID);
            m_cachedGroupTitles.Remove(remoteClient.AgentId);
            // Changing active group changes title, active powers, all kinds of things
            // anyone who is in any region that can see this client, should probably be 
            // updated with new group info.  At a minimum, they should get ScenePresence
            // updated with new title.
            UpdateAllClientsWithGroupInfo(GetRequestingAgentID(remoteClient), title);
        }

        /// <summary>
        ///   Get the Role Titles for an Agent, for a specific group
        /// </summary>
        public List<GroupTitlesData> GroupTitlesRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            return m_groupData.GetGroupTitles(GetRequestingAgentID(remoteClient), groupID);
        }

        public List<GroupMembersData> GroupMembersRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);
            List<GroupMembersData> data = m_groupData.GetGroupMembers(GetRequestingAgentID(remoteClient), groupID);

            if (m_debugEnabled)
            {
                foreach (GroupMembersData member in data)
                {
                    MainConsole.Instance.DebugFormat("[GROUPS]: Member({0}) - IsOwner({1})", member.AgentID, member.IsOwner);
                }
            }

            return data;
        }

        public List<GroupRolesData> GroupRoleDataRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            List<GroupRolesData> data = m_groupData.GetGroupRoles(GetRequestingAgentID(remoteClient), groupID);

            return data;
        }

        public List<GroupRoleMembersData> GroupRoleMembersRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            List<GroupRoleMembersData> data = m_groupData.GetGroupRoleMembers(GetRequestingAgentID(remoteClient),
                                                                              groupID);

            if (m_debugEnabled)
            {
                foreach (GroupRoleMembersData member in data)
                {
                    MainConsole.Instance.DebugFormat("[GROUPS]: Member({0}) - Role({1})", member.MemberID, member.RoleID);
                }
            }
            return data;
        }

        public GroupProfileData GroupProfileRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            return m_groupData.GetGroupProfile(GetRequestingAgentID(remoteClient), groupID);
        }

        public GroupMembershipData[] GetMembershipData(UUID agentID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            return m_groupData.GetAgentGroupMemberships(UUID.Zero, agentID).ToArray();
        }

        public GroupMembershipData GetMembershipData(UUID groupID, UUID agentID)
        {
            if (m_debugEnabled)
                MainConsole.Instance.DebugFormat(
                    "[GROUPS]: {0} called with groupID={1}, agentID={2}",
                    MethodBase.GetCurrentMethod().Name, groupID, agentID);

            return AttemptFindGroupMembershipData(UUID.Zero, agentID, groupID);
        }

        public void UpdateGroupInfo(IClientAPI remoteClient, UUID groupID, string charter, bool showInList,
                                    UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish,
                                    bool maturePublish)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            // Note: Permissions checking for modification rights is handled by the Groups Server/Service
            m_groupData.UpdateGroup(GetRequestingAgentID(remoteClient), groupID, charter, showInList, insigniaID,
                                    membershipFee, openEnrollment, allowPublish, maturePublish);
            NullCacheInfos(groupID);
        }

        public void SetGroupAcceptNotices(IClientAPI remoteClient, UUID groupID, bool acceptNotices, bool listInProfile)
        {
            // Note: Permissions checking for modification rights is handled by the Groups Server/Service
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            m_groupData.SetAgentGroupInfo(GetRequestingAgentID(remoteClient), GetRequestingAgentID(remoteClient),
                                          groupID, acceptNotices, listInProfile);
            NullCacheInfos(remoteClient.AgentId, groupID);
        }

        private void NullCacheInfos(UUID groupID)
        {
            foreach (UUID agentID in m_cachedGroupMemberships.Keys)
                NullCacheInfos(agentID, groupID);
        }

        private void NullCacheInfos(UUID agentID, UUID groupID)
        {
            if (!m_cachedGroupMemberships.ContainsKey(agentID))
                return;
            m_cachedGroupMemberships[agentID].RemoveAll((d) => d.GroupID == groupID);
        }

        public UUID CreateGroup(IClientAPI remoteClient, string name, string charter, bool showInList, UUID insigniaID,
                                int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            if (m_groupData.GetGroupRecord(GetRequestingAgentID(remoteClient), UUID.Zero, name) != null)
            {
                remoteClient.SendCreateGroupReply(UUID.Zero, false, "A group with the same name already exists.");
                return UUID.Zero;
            }
            // is there is a money module present ?
            IMoneyModule money = remoteClient.Scene.RequestModuleInterface<IMoneyModule>();
            if (money != null)
            {
                try
                {
                    // do the transaction, that is if the agent has got sufficient funds
                    if (!money.Charge(GetRequestingAgentID(remoteClient), money.GroupCreationCharge, "Group Creation"))
                    {
                        remoteClient.SendCreateGroupReply(UUID.Zero, false,
                                                          "You have got insuficient funds to create a group.");
                        return UUID.Zero;
                    }
                }
                catch
                {
                    remoteClient.SendCreateGroupReply(UUID.Zero, false,
                                                      "A money related exception occured, please contact your grid administrator.");
                    return UUID.Zero;
                }
            }
            UUID groupID = m_groupData.CreateGroup(GetRequestingAgentID(remoteClient), name, charter, showInList,
                                                   insigniaID, membershipFee, openEnrollment, allowPublish,
                                                   maturePublish, GetRequestingAgentID(remoteClient));

            remoteClient.SendCreateGroupReply(groupID, true, "Group created successfullly");
            m_cachedGroupTitles[remoteClient.AgentId] =
                AttemptFindGroupMembershipData(remoteClient.AgentId, remoteClient.AgentId, groupID);
            m_cachedGroupMemberships.Remove(remoteClient.AgentId);
            // Update the founder with new group information.
            SendAgentGroupDataUpdate(remoteClient, GetRequestingAgentID(remoteClient));

            return groupID;
        }

        public GroupNoticeData[] GroupNoticesListRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            return m_groupData.GetGroupNotices(GetRequestingAgentID(remoteClient), groupID).ToArray();
        }

        /// <summary>
        ///   Get the title of the agent's current role.
        /// </summary>
        public string GetGroupTitle(UUID avatarID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);
            
            //Check the cache first
            GroupMembershipData membership = null;
            if (m_cachedGroupTitles.ContainsKey(avatarID))
                membership = m_cachedGroupTitles[avatarID];
            else
                membership = m_groupData.GetAgentActiveMembership(avatarID, avatarID);

            if (membership != null)
            {
                m_cachedGroupTitles[avatarID] = membership;
                return membership.GroupTitle;
            }
            m_cachedGroupTitles[avatarID] = null;
            return string.Empty;
        }

        /// <summary>
        ///   Change the current Active Group Role for Agent
        /// </summary>
        public void GroupTitleUpdate(IClientAPI remoteClient, UUID groupID, UUID titleRoleID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            string title = m_groupData.SetAgentActiveGroupRole(GetRequestingAgentID(remoteClient),
                                                               GetRequestingAgentID(remoteClient), groupID, titleRoleID);
            m_cachedGroupTitles.Remove(remoteClient.AgentId);
            // TODO: Not sure what all is needed here, but if the active group role change is for the group
            // the client currently has set active, then we need to do a scene presence update too

            UpdateAllClientsWithGroupInfo(GetRequestingAgentID(remoteClient), title);
            NullCacheInfos(remoteClient.AgentId, groupID);
        }

        public void UpdateUsersForExternalRoleUpdate(UUID groupID, UUID roleID, ulong regionID)
        {
            lock (m_sceneList)
            {
                foreach (IScene s in from scene in m_sceneList where scene.RegionInfo.RegionHandle == regionID select scene)
                {
                    foreach (IScenePresence sp in s.GetScenePresences())
                    {
                        if (sp.ControllingClient.ActiveGroupId == groupID)
                        {
                            m_cachedGroupTitles.Remove(sp.UUID); //Remove the old title
                            UpdateAllClientsWithGroupInfo(sp.UUID, GetGroupTitle(sp.UUID));
                        }
                        //Remove their permissions too
                        RemoveFromGroupPowersCache(sp.UUID, groupID);
                    }
                }
            }
        }

        public void GroupRoleUpdate(IClientAPI remoteClient, UUID groupID, UUID roleID, string name, string description,
                                    string title, ulong powers, byte updateType)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            // Security Checks are handled in the Groups Service.

            switch ((GroupRoleUpdate) updateType)
            {
                case OpenMetaverse.GroupRoleUpdate.Create:
                    m_groupData.AddGroupRole(GetRequestingAgentID(remoteClient), groupID, UUID.Random(), name,
                                             description, title, powers);
                    break;

                case OpenMetaverse.GroupRoleUpdate.Delete:
                    m_groupData.RemoveGroupRole(GetRequestingAgentID(remoteClient), groupID, roleID);
                    break;

                case OpenMetaverse.GroupRoleUpdate.UpdateAll:
                case OpenMetaverse.GroupRoleUpdate.UpdateData:
                case OpenMetaverse.GroupRoleUpdate.UpdatePowers:
                    if (m_debugEnabled)
                    {
                        GroupPowers gp = (GroupPowers) powers;
                        MainConsole.Instance.DebugFormat("[GROUPS]: Role ({0}) updated with Powers ({1}) ({2})", name,
                                          powers.ToString(), gp.ToString());
                    }
                    m_groupData.UpdateGroupRole(GetRequestingAgentID(remoteClient), groupID, roleID, name, description,
                                                title, powers);
                    break;

                case OpenMetaverse.GroupRoleUpdate.NoUpdate:
                default:
                    // No Op
                    break;
            }

            ISyncMessagePosterService amps = m_sceneList[0].RequestModuleInterface<ISyncMessagePosterService>();
            if (amps != null)
            {
                OSDMap message = new OSDMap();
                message["Method"] = "FixGroupRoleTitles";
                message["GroupID"] = groupID;
                message["RoleID"] = roleID;
                message["AgentID"] = remoteClient.AgentId;
                message["Type"] = updateType;
                amps.Post(message, remoteClient.Scene.RegionInfo.RegionHandle);
            }

            UpdateUsersForExternalRoleUpdate(groupID, roleID, remoteClient.Scene.RegionInfo.RegionHandle);
        }

        public void GroupRoleChanges(IClientAPI remoteClient, UUID groupID, UUID roleID, UUID memberID, uint changes)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            switch (changes)
            {
                case 0:
                    // Add
                    m_groupData.AddAgentToGroupRole(GetRequestingAgentID(remoteClient), memberID, groupID, roleID);

                    break;
                case 1:
                    // Remove
                    m_groupData.RemoveAgentFromGroupRole(GetRequestingAgentID(remoteClient), memberID, groupID, roleID);

                    break;
                default:
                    MainConsole.Instance.ErrorFormat("[GROUPS]: {0} does not understand changes == {1}",
                                      MethodBase.GetCurrentMethod().Name, changes);
                    break;
            }

            // TODO: This update really should send out updates for everyone in the role that just got changed.
            SendAgentGroupDataUpdate(remoteClient, GetRequestingAgentID(remoteClient));
        }

        public void GroupNoticeRequest(IClientAPI remoteClient, UUID groupNoticeID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            GroupNoticeInfo data = m_groupData.GetGroupNotice(GetRequestingAgentID(remoteClient), groupNoticeID);

            if (data != null)
            {
                GridInstantMessage msg = BuildGroupNoticeIM(data, groupNoticeID, remoteClient.AgentId);
                OutgoingInstantMessage(msg, GetRequestingAgentID(remoteClient));
            }
        }

        public GridInstantMessage CreateGroupNoticeIM(UUID agentID, GroupNoticeInfo info, byte dialog)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            GridInstantMessage msg = new GridInstantMessage
                                         {
                                             toAgentID = agentID,
                                             dialog = dialog,
                                             fromGroup = true,
                                             offline = 1,
                                             ParentEstateID = 0,
                                             Position = Vector3.Zero,
                                             RegionID = UUID.Zero,
                                             imSessionID = UUID.Random()
                                         };

            // msg.dialog = (byte)OpenMetaverse.InstantMessageDialog.GroupNotice;
            // Allow this message to be stored for offline use

            msg.fromAgentID = info.GroupID;
            msg.timestamp = info.noticeData.Timestamp;
            msg.fromAgentName = info.noticeData.FromName;
            msg.message = info.noticeData.Subject + "|" + info.Message;
            if (info.noticeData.HasAttachment)
            {
                msg.binaryBucket = CreateBitBucketForGroupAttachment(info.noticeData, info.GroupID);
                //Save the sessionID for the callback by the client (reject or accept)
                //Only save if has attachment
                msg.imSessionID = info.noticeData.ItemID;
                //GroupAttachmentCache[msg.imSessionID] = info.noticeData.ItemID;
            }
            else
            {
                byte[] bucket = new byte[19];
                bucket[0] = 0; //Attachment enabled == false so 0
                bucket[1] = 0; //No attachment, so no asset type
                info.GroupID.ToBytes(bucket, 2);
                bucket[18] = 0; //dunno
                msg.binaryBucket = bucket;
            }

            return msg;
        }

        public void SendAgentGroupDataUpdate(IClientAPI remoteClient)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            // Send agent information about his groups
            SendAgentGroupDataUpdate(remoteClient, GetRequestingAgentID(remoteClient));
        }

        public void JoinGroupRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            GroupRecord record = m_groupData.GetGroupRecord(GetRequestingAgentID(remoteClient), groupID, "");
            if (record != null && record.OpenEnrollment)
            {
                // Should check to see if OpenEnrollment, or if there's an outstanding invitation
                m_groupData.AddAgentToGroup(GetRequestingAgentID(remoteClient), GetRequestingAgentID(remoteClient), groupID,
                                            UUID.Zero);

                m_cachedGroupMemberships.Remove(remoteClient.AgentId);
                remoteClient.SendJoinGroupReply(groupID, true);

                ActivateGroup(remoteClient, groupID);

                // Should this send updates to everyone in the group?
                SendAgentGroupDataUpdate(remoteClient, GetRequestingAgentID(remoteClient));
            }
        }

        public void LeaveGroupRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            if (
                !m_groupData.RemoveAgentFromGroup(GetRequestingAgentID(remoteClient), GetRequestingAgentID(remoteClient),
                                                  groupID))
                return;

            m_cachedGroupMemberships.Remove(remoteClient.AgentId);
            remoteClient.SendLeaveGroupReply(groupID, true);

            remoteClient.SendAgentDropGroup(groupID);

            if (remoteClient.ActiveGroupId == groupID)
                GroupTitleUpdate(remoteClient, UUID.Zero, UUID.Zero);

            SendAgentGroupDataUpdate(remoteClient, GetRequestingAgentID(remoteClient));

            if (m_groupsMessagingModule != null)
            {
                // SL sends out notifcations to the group messaging session that the person has left
                GridInstantMessage im = new GridInstantMessage
                                            {
                                                fromAgentID = groupID,
                                                dialog = (byte) InstantMessageDialog.SessionSend,
                                                binaryBucket = new byte[0],
                                                fromAgentName = "System",
                                                fromGroup = true,
                                                imSessionID = groupID,
                                                message = remoteClient.Name + " has left the group.",
                                                offline = 1,
                                                RegionID = remoteClient.Scene.RegionInfo.RegionID,
                                                timestamp = (uint) Util.UnixTimeSinceEpoch(),
                                                toAgentID = UUID.Zero
                                            };

                m_groupsMessagingModule.EnsureGroupChatIsStarted(groupID);
                m_groupsMessagingModule.SendMessageToGroup(im, groupID);
            }
        }

        public void EjectGroupMemberRequest(IClientAPI remoteClient, UUID groupID, UUID ejecteeID)
        {
            EjectGroupMember(remoteClient, GetRequestingAgentID(remoteClient), groupID, ejecteeID);
        }

        public void EjectGroupMember(IClientAPI remoteClient, UUID agentID, UUID groupID, UUID ejecteeID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);
            if (!m_groupData.RemoveAgentFromGroup(GetRequestingAgentID(remoteClient), ejecteeID, groupID))
                return;

            m_cachedGroupMemberships.Remove(ejecteeID);
            string agentName;
            RegionInfo regionInfo;

            // remoteClient provided or just agentID?
            if (remoteClient != null)
            {
                agentName = remoteClient.Name;
                regionInfo = remoteClient.Scene.RegionInfo;
                remoteClient.SendEjectGroupMemberReply(agentID, groupID, true);
            }
            else
            {
                IClientAPI client = GetActiveClient(agentID);
                if (client != null)
                {
                    agentName = client.Name;
                    regionInfo = client.Scene.RegionInfo;
                    client.SendEjectGroupMemberReply(agentID, groupID, true);
                }

                else
                {

                    regionInfo = m_sceneList[0].RegionInfo;
                    UserAccount acc = m_sceneList[0].UserAccountService.GetUserAccount(regionInfo.AllScopeIDs, agentID);

                    if (acc != null)
                    {

                        agentName = acc.FirstName + " " + acc.LastName;
                    }
                    else
                    {
                        agentName = "Unknown member";
                    }

                }

            }

            GroupRecord groupInfo = m_groupData.GetGroupRecord(GetRequestingAgentID(remoteClient), groupID, null);

            UserAccount account = m_sceneList[0].UserAccountService.GetUserAccount(regionInfo.AllScopeIDs, ejecteeID);

            if ((groupInfo == null) || (account == null))
                return;

            // Send Message to Ejectee
            GridInstantMessage msg = new GridInstantMessage
            {
                imSessionID = UUID.Zero,
                fromAgentID = UUID.Zero,
                toAgentID = ejecteeID,
                timestamp = 0,
                fromAgentName = "System",
                message =
                    string.Format("You have been ejected from '{1}' by {0}.",
                                  agentName,
                                  groupInfo.GroupName),
                dialog = 210,
                fromGroup = false,
                offline = 0,
                ParentEstateID = 0,
                Position = Vector3.Zero,
                RegionID = remoteClient.Scene.RegionInfo.RegionID,
                binaryBucket = new byte[0]
            };

            OutgoingInstantMessage(msg, ejecteeID);

            //Do this here for local agents, otherwise it never gets done
            IClientAPI ejectee = GetActiveClient(ejecteeID);
            if (ejectee != null)
            {
                msg.dialog = (byte)InstantMessageDialog.MessageFromAgent;
                OutgoingInstantMessage(msg, ejecteeID);
                ejectee.SendAgentDropGroup(groupID);
            }


            // Message to ejector
            // Interop, received special 210 code for ejecting a group member
            // this only works within the comms servers domain, and won't work hypergrid

            m_cachedGroupTitles[ejecteeID] = null;
            UpdateAllClientsWithGroupInfo(ejecteeID, "");

            if (m_groupsMessagingModule != null)
            {
                // SL sends out notifcations to the group messaging session that the person has left
                GridInstantMessage im = new GridInstantMessage
                {
                    fromAgentID = groupID,
                    dialog = (byte)InstantMessageDialog.SessionSend,
                    binaryBucket = new byte[0],
                    fromAgentName = "System",
                    fromGroup = true,
                    imSessionID = groupID,
                    message = account.Name + " has been ejected from the group by " + remoteClient.Name + ".",
                    offline = 1,
                    RegionID = remoteClient.Scene.RegionInfo.RegionID,
                    timestamp = (uint)Util.UnixTimeSinceEpoch(),
                    toAgentID = UUID.Zero
                };

                m_groupsMessagingModule.EnsureGroupChatIsStarted(groupID);
                m_groupsMessagingModule.SendMessageToGroup(im, groupID);
            }
        }

        public void InviteGroupRequest(IClientAPI remoteClient, UUID groupID, UUID invitedAgentID, UUID roleID)
        {

            InviteGroup(remoteClient, GetRequestingAgentID(remoteClient), groupID, invitedAgentID, roleID);
        }

        public void InviteGroup(IClientAPI remoteClient, UUID agentID, UUID groupID, UUID invitedAgentID, UUID roleID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            string agentName;
            RegionInfo regionInfo;
            // remoteClient provided or just agentID?
            if (remoteClient != null)
            {
                agentName = remoteClient.Name;
                regionInfo = remoteClient.Scene.RegionInfo;
            }
            else
            {
                IClientAPI client = GetActiveClient(agentID);
                if (client != null)
                {
                    agentName = client.Name;
                    regionInfo = client.Scene.RegionInfo;
                }

                else
                {
                    regionInfo = m_sceneList[0].RegionInfo;
                    UserAccount account = m_sceneList[0].UserAccountService.GetUserAccount(regionInfo.AllScopeIDs, agentID);
                    if (account != null)
                    {
                        agentName = account.FirstName + " " + account.LastName;
                    }

                    else
                    {
                        agentName = "Unknown member";
                    }
                }
            }

            UUID InviteID = UUID.Random();

            m_groupData.AddAgentToGroupInvite(GetRequestingAgentID(remoteClient), InviteID, groupID, roleID,
                                              invitedAgentID, remoteClient.Name);

            // Check to see if the invite went through, if it did not then it's possible
            // the remoteClient did not validate or did not have permission to invite.
            GroupInviteInfo inviteInfo = m_groupData.GetAgentToGroupInvite(invitedAgentID, InviteID);

            if (inviteInfo != null)
            {
                if (m_msgTransferModule != null)
                {
                    UUID inviteUUID = InviteID;

                    GridInstantMessage msg = new GridInstantMessage
                                                 {
                                                     imSessionID = inviteUUID,
                                                     fromAgentID = groupID,
                                                     toAgentID = invitedAgentID,
                                                     timestamp = 0,
                                                     fromAgentName = agentName
                                                 };
                    // msg.fromAgentID = GetRequestingAgentID(remoteClient).Guid;
                    //msg.timestamp = (uint)Util.UnixTimeSinceEpoch();
                    GroupRecord groupInfo = GetGroupRecord(groupID);
                    string MemberShipCost = ". There is no cost to join this group.";
                    if (groupInfo.MembershipFee != 0)
                    {
                        MemberShipCost = ". To join, you must pay " + groupInfo.MembershipFee.ToString() + ".";
                    }
                    msg.message = string.Format("{0} has invited you to join " + groupInfo.GroupName + MemberShipCost,
                                                remoteClient.Name);
                    msg.dialog = (byte)InstantMessageDialog.GroupInvitation;
                    msg.fromGroup = true;
                    msg.offline = 0;
                    msg.ParentEstateID = 0;
                    msg.Position = Vector3.Zero;
                    msg.RegionID = remoteClient.Scene.RegionInfo.RegionID;
                    msg.binaryBucket = new byte[20];

                    OutgoingInstantMessage(msg, invitedAgentID);
                }
            }
        }

        public GridInstantMessage BuildOfflineGroupNotice(GridInstantMessage msg)
        {
            msg.dialog = 211; //We set this so that it isn't taken the wrong way later
            //Unknown what this did...
            //UUID NoticeID = GroupSessionIDCache[msg.imSessionID];
            //GroupSessionIDCache.Remove(msg.imSessionID);
            //msg.imSessionID = NoticeID;
            return msg;
        }

        #endregion

        #region Client/Update Tools

        public void UpdateCachedData(UUID agentID, CachedUserInfo cachedInfo)
        {
            //Update the cache
            m_cachedGroupTitles[agentID] = cachedInfo.ActiveGroup;
            m_cachedGroupMemberships[agentID] = cachedInfo.GroupMemberships;
        }

        /// <summary>
        ///   Try to find an active IClientAPI reference for agentID giving preference to root connections
        /// </summary>
        private IClientAPI GetActiveClient(UUID agentID)
        {
            IClientAPI child = null;

            // Try root avatar first
            foreach (IScene scene in m_sceneList)
            {
                IScenePresence user;
                if (scene.TryGetScenePresence(agentID, out user))
                {
                    if (!user.IsChildAgent)
                    {
                        return user.ControllingClient;
                    }
                    else
                    {
                        child = user.ControllingClient;
                    }
                }
            }

            // If we didn't find a root, then just return whichever child we found, or null if none
            return child;
        }

        /// <summary>
        ///   Send 'remoteClient' the group membership 'data' for agent 'dataForAgentID'.
        /// </summary>
        private void SendGroupMembershipInfoViaCaps(IClientAPI remoteClient, UUID dataForAgentID,
                                                    GroupMembershipData[] data)
        {
            if (m_debugEnabled) MainConsole.Instance.InfoFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            OSDArray AgentData = new OSDArray(1);
            OSDMap AgentDataMap = new OSDMap(1) {{"AgentID", OSD.FromUUID(dataForAgentID)}};
            AgentData.Add(AgentDataMap);


            OSDArray GroupData = new OSDArray(data.Length);
            OSDArray NewGroupData = new OSDArray(data.Length);

            foreach (GroupMembershipData membership in data)
            {
                if (GetRequestingAgentID(remoteClient) != dataForAgentID)
                {
                    if (!membership.ListInProfile)
                    {
                        // If we're sending group info to remoteclient about another agent, 
                        // filter out groups the other agent doesn't want to share.
                        continue;
                    }
                }

                OSDMap GroupDataMap = new OSDMap(6);
                OSDMap NewGroupDataMap = new OSDMap(1);

                GroupDataMap.Add("GroupID", OSD.FromUUID(membership.GroupID));
                GroupDataMap.Add("GroupPowers", OSD.FromULong(membership.GroupPowers));
                GroupDataMap.Add("AcceptNotices", OSD.FromBoolean(membership.AcceptNotices));
                GroupDataMap.Add("GroupInsigniaID", OSD.FromUUID(membership.GroupPicture));
                GroupDataMap.Add("Contribution", OSD.FromInteger(membership.Contribution));
                GroupDataMap.Add("GroupName", OSD.FromString(membership.GroupName));
                NewGroupDataMap.Add("ListInProfile", OSD.FromBoolean(membership.ListInProfile));

                GroupData.Add(GroupDataMap);
                NewGroupData.Add(NewGroupDataMap);
            }

            OSDMap llDataStruct = new OSDMap(3)
                                      {
                                          {"AgentData", AgentData},
                                          {"GroupData", GroupData},
                                          {"NewGroupData", NewGroupData}
                                      };

            if (m_debugEnabled)
                MainConsole.Instance.InfoFormat("[GROUPS]: {0}", OSDParser.SerializeJsonString(llDataStruct));

            IEventQueueService queue = remoteClient.Scene.RequestModuleInterface<IEventQueueService>();

            if (queue != null)
                queue.Enqueue(buildEvent("AgentGroupDataUpdate", llDataStruct), GetRequestingAgentID(remoteClient),
                              remoteClient.Scene.RegionInfo.RegionHandle);
        }

        public OSD buildEvent(string eventName, OSD eventBody)
        {
            OSDMap llsdEvent = new OSDMap(2) {{"body", eventBody}, {"message", new OSDString(eventName)}};

            return llsdEvent;
        }

        private void SendScenePresenceUpdate(UUID AgentID, string Title)
        {
            if (m_debugEnabled)
                MainConsole.Instance.DebugFormat("[GROUPS]: Updating scene title for {0} with title: {1}", AgentID, Title);

            IScenePresence presence = null;

            lock (m_sceneList)
            {
                foreach (IScene scene in m_sceneList)
                {
                    presence = scene.GetScenePresence(AgentID);
                    if (presence != null)
                    {
                        if (!presence.IsChildAgent)
                        {
                            //Force send a full update
                            IScenePresence presence1 = presence;
                            IScene scene1 = scene;
#if (!ISWIN)
                            foreach (IScenePresence sp in scene.GetScenePresences())
                            {
                                if (sp.SceneViewer.Culler.ShowEntityToClient(sp, presence1, scene1))
                                {
                                    sp.ControllingClient.SendAvatarDataImmediate(presence);
                                }
                            }
#else
                            foreach (IScenePresence sp in scene.GetScenePresences().Where(sp => sp.SceneViewer.Culler.ShowEntityToClient(sp, presence1, scene1)))
                            {
                                sp.ControllingClient.SendAvatarDataImmediate(presence);
                            }
#endif
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   Send updates to all clients who might be interested in groups data for dataForClientID
        /// </summary>
        private void UpdateAllClientsWithGroupInfo(UUID dataForAgentID, string activeGroupTitle)
        {
            if (m_debugEnabled) MainConsole.Instance.InfoFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            // TODO: Probably isn't nessesary to update every client in every scene.
            // Need to examine client updates and do only what's nessesary.

            List<GroupMembershipData> membershipData = m_cachedGroupMemberships.ContainsKey(dataForAgentID) ? m_cachedGroupMemberships[dataForAgentID] :
                                                                                            m_groupData.GetAgentGroupMemberships(dataForAgentID,
                                                                                            dataForAgentID);

            lock (m_sceneList)
            {
                foreach (IScene scene in m_sceneList)
                {
                    scene.ForEachClient(delegate(IClientAPI client)
                                            {
                                                if (m_debugEnabled)
                                                    MainConsole.Instance.InfoFormat(
                                                        "[GROUPS]: SendAgentGroupDataUpdate called for {0}", client.Name);

                                                // TODO: All the client update functions need to be reexamined because most do too much and send too much stuff
                                                OnAgentDataUpdateRequest(client, dataForAgentID, UUID.Zero, false);

                                                GroupMembershipData[] membershipArray;
                                                if (client.AgentId != dataForAgentID)
                                                {
#if (!ISWIN)
                                                    Predicate<GroupMembershipData> showInProfile = delegate(GroupMembershipData membership)
                                                    {
                                                        return membership.ListInProfile;
                                                    };
#else
                                                    Predicate<GroupMembershipData> showInProfile =
                                                        membership => membership.ListInProfile;
#endif
                                                    membershipArray = membershipData.FindAll(showInProfile).ToArray();
                                                }
                                                else
                                                    membershipArray = membershipData.ToArray();

                                                SendGroupMembershipInfoViaCaps(client, dataForAgentID, membershipArray);
                                            });
                }
                SendScenePresenceUpdate(dataForAgentID, activeGroupTitle);
            }
        }

        /// <summary>
        ///   Update remoteClient with group information about dataForAgentID
        /// </summary>
        private void SendAgentGroupDataUpdate(IClientAPI remoteClient, UUID dataForAgentID)
        {
            if (m_debugEnabled)
                MainConsole.Instance.InfoFormat("[GROUPS]: SendAgentGroupDataUpdate called for {0}", remoteClient.Name);

            // TODO: All the client update functions need to be reexamined because most do too much and send too much stuff
            OnAgentDataUpdateRequest(remoteClient, dataForAgentID, UUID.Zero);

            GroupMembershipData[] membershipArray = GetProfileListedGroupMemberships(remoteClient, dataForAgentID);
            SendGroupMembershipInfoViaCaps(remoteClient, dataForAgentID, membershipArray);
        }

        /// <summary>
        ///   Update remoteClient with group information about dataForAgentID
        /// </summary>
        private void SendNewAgentGroupDataUpdate(IClientAPI remoteClient)
        {
            if (m_debugEnabled)
                MainConsole.Instance.InfoFormat("[GROUPS]: SendAgentGroupDataUpdate called for {0}", remoteClient.Name);

            // TODO: All the client update functions need to be reexamined because most do too much and send too much stuff
            OnAgentDataUpdateRequest(remoteClient, remoteClient.AgentId, UUID.Zero, false);

            GroupMembershipData[] membershipArray = GetProfileListedGroupMemberships(remoteClient, remoteClient.AgentId);
            SendGroupMembershipInfoViaCaps(remoteClient, remoteClient.AgentId, membershipArray);
        }

        /// <summary>
        ///   Get a list of groups memberships for the agent that are marked "ListInProfile"
        /// </summary>
        /// <param name = "dataForAgentID"></param>
        /// <returns></returns>
        private GroupMembershipData[] GetProfileListedGroupMemberships(IClientAPI requestingClient, UUID dataForAgentID)
        {
            List<GroupMembershipData> membershipData = m_cachedGroupMemberships.ContainsKey(dataForAgentID) ? m_cachedGroupMemberships[dataForAgentID] :
                                                                                            m_groupData.GetAgentGroupMemberships(requestingClient.AgentId,
                                                                                            dataForAgentID);
            GroupMembershipData[] membershipArray;

            if (requestingClient.AgentId != dataForAgentID)
            {
#if (!ISWIN)
                Predicate<GroupMembershipData> showInProfile = delegate(GroupMembershipData membership)
                {
                    return membership.ListInProfile;
                };
#else
                Predicate<GroupMembershipData> showInProfile =
                    membership => membership.ListInProfile;
#endif

                membershipArray = membershipData.FindAll(showInProfile).ToArray();
            }
            else
            {
                membershipArray = membershipData.ToArray();
            }

            if (m_debugEnabled)
            {
                MainConsole.Instance.InfoFormat("[GROUPS]: Get group membership information for {0} requested by {1}", dataForAgentID,
                                 requestingClient.AgentId);
                foreach (GroupMembershipData membership in membershipArray)
                {
                    MainConsole.Instance.InfoFormat("[GROUPS]: {0} :: {1} - {2} - {3}", dataForAgentID, membership.GroupName,
                                     membership.GroupTitle, membership.GroupPowers);
                }
            }

            return membershipArray;
        }

        #endregion

        #region IM Backed Processes

        public void NotifyChange(UUID groupID)
        {
            // Notify all group members of a chnge in group roles and/or
            // permissions
            //
        }

        private void OutgoingInstantMessage(GridInstantMessage msg, UUID msgTo)
        {
            if (m_debugEnabled) MainConsole.Instance.InfoFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            IClientAPI localClient = GetActiveClient(msgTo);
            if (localClient != null)
            {
                if (m_debugEnabled)
                    MainConsole.Instance.InfoFormat("[GROUPS]: MsgTo ({0}) is local, delivering directly", localClient.Name);
                localClient.SendInstantMessage(msg);
            }
            else
            {
                if (m_debugEnabled)
                    MainConsole.Instance.InfoFormat("[GROUPS]: MsgTo ({0}) is not local, delivering via TransferModule", msgTo);
                m_msgTransferModule.SendInstantMessage(msg);
            }
        }

        #endregion

        #region ISharedRegionModule Members

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
                m_groupsEnabled = groupsConfig.GetBoolean("Enabled", false);
                if (!m_groupsEnabled)
                {
                    return;
                }

                if (groupsConfig.GetString("Module", "Default") != Name)
                {
                    m_groupsEnabled = false;

                    return;
                }

                //MainConsole.Instance.InfoFormat("[GROUPS]: Initializing {0}", this.Name);

                m_groupNoticesEnabled = groupsConfig.GetBoolean("NoticesEnabled", true);
                m_debugEnabled = groupsConfig.GetBoolean("DebugEnabled", true);
            }
        }

        public void AddRegion(IScene scene)
        {
            if (m_groupsEnabled)
                scene.RegisterModuleInterface<IGroupsModule>(this);
        }

        public void RegionLoaded(IScene scene)
        {
            if (!m_groupsEnabled)
                return;

            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            if (m_groupData == null)
            {
                m_groupData = scene.RequestModuleInterface<IGroupsServicesConnector>();

                // No Groups Service Connector, then nothing works...
                if (m_groupData == null)
                {
                    m_groupsEnabled = false;
                    MainConsole.Instance.Error("[GROUPS]: Could not get IGroupsServicesConnector");
                    Close();
                    return;
                }

                m_groupsMessagingModule = scene.RequestModuleInterface<IGroupsMessagingModule>();
            }

            if (m_msgTransferModule == null)
            {
                m_msgTransferModule = scene.RequestModuleInterface<IMessageTransferModule>();

                // No message transfer module, no notices, group invites, rejects, ejects, etc
                if (m_msgTransferModule == null)
                {
                    m_groupsEnabled = false;
                    MainConsole.Instance.Error("[GROUPS]: Could not get MessageTransferModule");
                    Close();
                    return;
                }
            }

            lock (m_sceneList)
                m_sceneList.Add(scene);

            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
            scene.EventManager.OnClosingClient += OnClosingClient;
            scene.EventManager.OnIncomingInstantMessage += OnGridInstantMessage;
            scene.EventManager.OnClientLogin += EventManager_OnClientLogin;
            scene.EventManager.OnRegisterCaps += OnRegisterCaps;
            // The InstantMessageModule itself doesn't do this, 
            // so lets see if things explode if we don't do it
            // scene.EventManager.OnClientClosed += OnClientClosed;
        }

        public void RemoveRegion(IScene scene)
        {
            if (!m_groupsEnabled)
                return;

            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            lock (m_sceneList)
                m_sceneList.Remove(scene);

            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
            scene.EventManager.OnIncomingInstantMessage -= OnGridInstantMessage;
            scene.EventManager.OnClientLogin -= EventManager_OnClientLogin;
            scene.EventManager.OnRegisterCaps -= OnRegisterCaps;
        }

        public void Close()
        {
            if (!m_groupsEnabled)
                return;

            if (m_debugEnabled) MainConsole.Instance.Debug("[GROUPS]: Shutting down Groups module.");
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "GroupsModule"; }
        }

        public void PostInitialise()
        {
            // NoOp
        }

        #endregion

        #region EventHandlers

        private void OnNewClient(IClientAPI client)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            client.OnUUIDGroupNameRequest += HandleUUIDGroupNameRequest;
            client.OnAgentDataUpdateRequest += OnAgentDataUpdateRequest;
            client.OnDirFindQuery += OnDirFindQuery;
            client.OnRequestAvatarProperties += OnRequestAvatarProperties;
            client.OnGroupActiveProposalsRequest += GroupActiveProposalsRequest;
            client.OnGroupVoteHistoryRequest += GroupVoteHistoryRequest;
            client.OnGroupProposalBallotRequest += GroupProposalBallotRequest;

            // Used for Notices and Group Invites/Accept/Reject
            client.OnInstantMessage += OnInstantMessage;
        }

        protected void OnMakeRootAgent(IScenePresence sp)
        {
            // Send client their groups information.
            if (sp != null && !sp.IsChildAgent)
                SendNewAgentGroupDataUpdate(sp.ControllingClient);
        }

        private void OnClosingClient(IClientAPI client)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            client.OnUUIDGroupNameRequest -= HandleUUIDGroupNameRequest;
            client.OnAgentDataUpdateRequest -= OnAgentDataUpdateRequest;
            client.OnDirFindQuery -= OnDirFindQuery;
            client.OnRequestAvatarProperties -= OnRequestAvatarProperties;

            // Used for Notices and Group Invites/Accept/Reject
            client.OnInstantMessage -= OnInstantMessage;

            //Remove them from the cache
            m_cachedGroupTitles.Remove(client.AgentId);
        }

        private void GroupProposalBallotRequest(IClientAPI client, UUID agentID, UUID sessionID, UUID groupID,
                                                UUID proposalID, string vote)
        {
            m_groupData.VoteOnActiveProposals(agentID, groupID, proposalID, vote);
        }

        private void GroupVoteHistoryRequest(IClientAPI client, UUID agentID, UUID sessionID, UUID groupID,
                                             UUID transactionID)
        {
            List<GroupProposalInfo> inactiveProposals = m_groupData.GetInactiveProposals(client.AgentId, groupID);
            foreach (GroupProposalInfo proposal in inactiveProposals)
            {
                GroupVoteHistoryItem[] votes = new GroupVoteHistoryItem[1];
                votes[0] = new GroupVoteHistoryItem();
                votes[0].CandidateID = proposal.VoteID;
                votes[0].NumVotes = proposal.NumVotes;
                votes[0].VoteCast = proposal.Result ? "Yes" : "No";
                GroupVoteHistory history = new GroupVoteHistory();
                history.EndDateTime = Util.BuildYMDDateString(proposal.Ending);
                history.Majority = proposal.Majority.ToString();
                history.ProposalText = proposal.Text;
                history.Quorum = proposal.Quorum.ToString();
                history.StartDateTime = Util.BuildYMDDateString(proposal.Created);
                history.VoteID = proposal.VoteID.ToString();
                history.VoteInitiator = proposal.BallotInitiator.ToString();
                history.VoteResult = proposal.Result ? "Success" : "Failure";
                history.VoteType = "Proposal";//Must be set to this, or the viewer won't show it
                client.SendGroupVoteHistory(groupID, transactionID, history, votes);
            }
        }

        private void GroupActiveProposalsRequest(IClientAPI client, UUID agentID, UUID sessionID, UUID groupID,
                                                 UUID transactionID)
        {
            List<GroupProposalInfo> activeProposals = m_groupData.GetActiveProposals(client.AgentId, groupID);
            GroupActiveProposals[] proposals = new GroupActiveProposals[activeProposals.Count];
            int i = 0;
            foreach (GroupProposalInfo proposal in activeProposals)
            {
                proposals[i] = new GroupActiveProposals();
                proposals[i].ProposalText = proposal.Text;
                proposals[i].Majority = proposal.Majority.ToString();
                proposals[i].Quorum = proposal.Quorum.ToString();
                proposals[i].StartDateTime = Util.BuildYMDDateString(proposal.Created);
                proposals[i].TerseDateID = "";
                proposals[i].VoteID = proposal.VoteID.ToString();
                proposals[i].VoteInitiator = proposal.BallotInitiator.ToString();
                proposals[i].VoteAlreadyCast = proposal.VoteCast != "";
                proposals[i].VoteCast = proposal.VoteCast;
                proposals[i++].EndDateTime = Util.BuildYMDDateString(proposal.Ending);
            }
            client.SendGroupActiveProposals(groupID, transactionID, proposals);
        }

        private byte[] GroupProposalBallot(string request, UUID agentID)
        {
            OSDMap map = (OSDMap) OSDParser.DeserializeLLSDXml(request);

            UUID groupID = map["group-id"].AsUUID();
            UUID proposalID = map["proposal-id"].AsUUID();
            string vote = map["vote"].AsString();

            m_groupData.VoteOnActiveProposals(agentID, groupID, proposalID, vote);

            OSDMap resp = new OSDMap();
            resp["voted"] = OSD.FromBoolean(true);
            return OSDParser.SerializeLLSDXmlBytes(resp);
        }

        private byte[] StartGroupProposal(string request, UUID agentID)
        {
            OSDMap map = (OSDMap) OSDParser.DeserializeLLSDXml(request);

            int duration = map["duration"].AsInteger();
            UUID group = map["group-id"].AsUUID();
            double majority = map["majority"].AsReal();
            string text = map["proposal-text"].AsString();
            int quorum = map["quorum"].AsInteger();
            UUID session = map["session-id"].AsUUID();

            GroupProposalInfo info = new GroupProposalInfo
                                         {
                                             GroupID = group,
                                             Majority = (float) majority,
                                             Quorum = quorum,
                                             Session = session,
                                             Text = text,
                                             Duration = duration,
                                             BallotInitiator = agentID,
                                             Created = DateTime.Now,
                                             Ending = DateTime.Now.AddSeconds(duration),
                                             VoteID = UUID.Random()
                                         };

            m_groupData.AddGroupProposal(agentID, info);

            OSDMap resp = new OSDMap();
            resp["voted"] = OSD.FromBoolean(true);
            return OSDParser.SerializeLLSDXmlBytes(resp);
        }

        private void OnRequestAvatarProperties(IClientAPI remoteClient, UUID avatarID)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            //GroupMembershipData[] avatarGroups = m_groupData.GetAgentGroupMemberships(GetRequestingAgentID(remoteClient), avatarID).ToArray();
            GroupMembershipData[] avatarGroups = GetProfileListedGroupMemberships(remoteClient, avatarID);
            remoteClient.SendAvatarGroupsReply(avatarID, avatarGroups);
        }

        private void EventManager_OnClientLogin(IClientAPI client)
        {
            if (client.Scene.GetScenePresence(client.AgentId).IsChildAgent)
                return;

            List<GroupInviteInfo> inviteInfo = m_groupData.GetGroupInvites(client.AgentId);

            if (inviteInfo.Count != 0)
            {
                foreach (GroupInviteInfo Invite in inviteInfo)
                {
                    if (m_msgTransferModule != null)
                    {
                        UUID inviteUUID = Invite.InviteID;

                        GridInstantMessage msg = new GridInstantMessage
                                                     {
                                                         imSessionID = inviteUUID,
                                                         fromAgentID = Invite.GroupID,
                                                         toAgentID = Invite.AgentID,
                                                         timestamp = (uint) Util.UnixTimeSinceEpoch(),
                                                         fromAgentName = Invite.FromAgentName
                                                     };



                        GroupRecord groupInfo = GetGroupRecord(Invite.GroupID);
                        string MemberShipCost = ". There is no cost to join this group.";
                        if (groupInfo.MembershipFee != 0)
                            MemberShipCost = ". To join, you must pay " + groupInfo.MembershipFee.ToString() + ".";

                        msg.message =
                            string.Format("{0} has invited you to join " + groupInfo.GroupName + MemberShipCost,
                                          Invite.FromAgentName);
                        msg.dialog = (byte) InstantMessageDialog.GroupInvitation;
                        msg.fromGroup = true;
                        msg.offline = 0;
                        msg.ParentEstateID = 0;
                        msg.Position = Vector3.Zero;
                        msg.RegionID = UUID.Zero;
                        msg.binaryBucket = new byte[20];

                        OutgoingInstantMessage(msg, Invite.AgentID);
                    }
                }
            }
        }

        /*
         * This becomes very problematic in a shared module.  In a shared module you may have more then one
         * reference to IClientAPI's, one for 0 or 1 root connections, and 0 or more child connections.
         * The OnClientClosed event does not provide anything to indicate which one of those should be closed
         * nor does it provide what scene it was from so that the specific reference can be looked up.
         * The InstantMessageModule.cs does not currently worry about unregistering the handles, 
         * and it should be an issue, since it's the client that references us not the other way around
         * , so as long as we don't keep a reference to the client laying around, the client can still be GC'ed
        private void OnClientClosed(UUID AgentId)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            lock (m_ActiveClients)
            {
                if (m_ActiveClients.ContainsKey(AgentId))
                {
                    IClientAPI client = m_ActiveClients[AgentId];
                    client.OnUUIDGroupNameRequest -= HandleUUIDGroupNameRequest;
                    client.OnAgentDataUpdateRequest -= OnAgentDataUpdateRequest;
                    client.OnDirFindQuery -= OnDirFindQuery;
                    client.OnInstantMessage -= OnInstantMessage;

                    m_ActiveClients.Remove(AgentId);
                }
                else
                {
                    if (m_debugEnabled) MainConsole.Instance.WarnFormat("[GROUPS]: Client closed that wasn't registered here.");
                }

                
            }
        }
        */

        private void OnDirFindQuery(IClientAPI remoteClient, UUID queryID, string queryText, uint queryFlags,
                                    int queryStart)
        {
            if (((DirectoryManager.DirFindFlags) queryFlags & DirectoryManager.DirFindFlags.Groups) ==
                DirectoryManager.DirFindFlags.Groups)
            {
                if (m_debugEnabled)
                    MainConsole.Instance.DebugFormat(
                        "[GROUPS]: {0} called with queryText({1}) queryFlags({2}) queryStart({3})",
                        MethodBase.GetCurrentMethod().Name, queryText, (DirectoryManager.DirFindFlags) queryFlags,
                        queryStart);

                remoteClient.SendDirGroupsReply(queryID,
                                                m_groupData.FindGroups(GetRequestingAgentID(remoteClient), queryText,
                                                                       queryStart, queryFlags).ToArray());
            }
        }

        private void OnAgentDataUpdateRequest(IClientAPI remoteClient, UUID dataForAgentID, UUID sessionID)
        {
            OnAgentDataUpdateRequest(remoteClient, dataForAgentID, sessionID, true);
        }

        private void OnAgentDataUpdateRequest(IClientAPI remoteClient, UUID dataForAgentID, UUID sessionID,
                                              bool sendToAll)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            UUID activeGroupID = UUID.Zero;
            string activeGroupTitle = string.Empty;
            string activeGroupName = string.Empty;
            ulong activeGroupPowers = (ulong) GroupPowers.None;

            GroupMembershipData membership = m_cachedGroupTitles.ContainsKey(dataForAgentID) ? 
                m_cachedGroupTitles[dataForAgentID] : 
                m_groupData.GetAgentActiveMembership(GetRequestingAgentID(remoteClient),
                                                                                   dataForAgentID);
            m_cachedGroupTitles[dataForAgentID] = membership;
            if (membership != null)
            {
                activeGroupID = membership.GroupID;
                activeGroupTitle = membership.GroupTitle;
                activeGroupPowers = membership.GroupPowers;
                activeGroupName = membership.GroupName;
            }

            //Gotta tell the client about their groups
            remoteClient.SendAgentDataUpdate(dataForAgentID, activeGroupID, remoteClient.FirstName,
                                             remoteClient.LastName, activeGroupPowers, activeGroupName,
                                             activeGroupTitle);

            if (sendToAll)
                SendScenePresenceUpdate(dataForAgentID, activeGroupTitle);
        }

        private void HandleUUIDGroupNameRequest(UUID GroupID, IClientAPI remoteClient)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            string GroupName;

            GroupRecord group = m_groupData.GetGroupRecord(GetRequestingAgentID(remoteClient), GroupID, null);
            GroupName = group != null ? group.GroupName : "Unknown";

            remoteClient.SendGroupNameReply(GroupID, GroupName);
        }

        private void OnInstantMessage(IClientAPI remoteClient, GridInstantMessage im)
        {
            if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            // Group invitations
            if ((im.dialog == (byte) InstantMessageDialog.GroupInvitationAccept) ||
                (im.dialog == (byte) InstantMessageDialog.GroupInvitationDecline))
            {
                UUID inviteID = im.imSessionID;
                GroupInviteInfo inviteInfo = m_groupData.GetAgentToGroupInvite(GetRequestingAgentID(remoteClient),
                                                                               inviteID);

                if (inviteInfo == null)
                {
                    if (m_debugEnabled)
                        MainConsole.Instance.WarnFormat("[GROUPS]: Received an Invite IM for an invite that does not exist {0}.",
                                         inviteID);
                    return;
                }

                if (m_debugEnabled)
                    MainConsole.Instance.DebugFormat("[GROUPS]: Invite is for Agent {0} to Group {1}.", inviteInfo.AgentID,
                                      inviteInfo.GroupID);

                UUID fromAgentID = im.fromAgentID;
                if ((inviteInfo != null) && (fromAgentID == inviteInfo.AgentID))
                {
                    // Accept
                    if (im.dialog == (byte) InstantMessageDialog.GroupInvitationAccept)
                    {
                        if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: Received an accept invite notice.");

                        // and the sessionid is the role
                        UserAccount account = m_sceneList[0].UserAccountService.GetUserAccount(remoteClient.AllScopeIDs,
                                                                                               inviteInfo.FromAgentName);
                        if (account != null)
                        {
                            m_groupData.AddAgentToGroup(account.PrincipalID, inviteInfo.AgentID, inviteInfo.GroupID,
                                                        inviteInfo.RoleID);

                            GridInstantMessage msg = new GridInstantMessage
                                                         {
                                                             imSessionID = UUID.Zero,
                                                             fromAgentID = UUID.Zero,
                                                             toAgentID = inviteInfo.AgentID,
                                                             timestamp = (uint) Util.UnixTimeSinceEpoch(),
                                                             fromAgentName = "Groups",
                                                             message =
                                                                 string.Format("You have been added to the group."),
                                                             dialog = (byte) InstantMessageDialog.MessageBox,
                                                             fromGroup = false,
                                                             offline = 0,
                                                             ParentEstateID = 0,
                                                             Position = Vector3.Zero,
                                                             RegionID = UUID.Zero,
                                                             binaryBucket = new byte[0]
                                                         };

                            OutgoingInstantMessage(msg, inviteInfo.AgentID);

                            //WTH??? noone but the invitee needs to know
                            //The other client wants to know too...
                            GroupMembershipData gmd =
                                AttemptFindGroupMembershipData(inviteInfo.AgentID, inviteInfo.AgentID, inviteInfo.GroupID);
                            m_cachedGroupTitles[inviteInfo.AgentID] = gmd;
                            UpdateAllClientsWithGroupInfo(inviteInfo.AgentID, gmd.GroupTitle);
                            SendAgentGroupDataUpdate(remoteClient);
                            // XTODO: If the inviter is still online, they need an agent dataupdate 
                            // and maybe group membership updates for the invitee
                            // Reply: why do they need that? they will get told about the new user when they reopen the groups panel

                            m_groupData.RemoveAgentToGroupInvite(GetRequestingAgentID(remoteClient), inviteID);
                        }
                    }

                    // Reject
                    if (im.dialog == (byte) InstantMessageDialog.GroupInvitationDecline)
                    {
                        if (m_debugEnabled) MainConsole.Instance.DebugFormat("[GROUPS]: Received a reject invite notice.");
                        m_groupData.RemoveAgentToGroupInvite(GetRequestingAgentID(remoteClient), inviteID);
                    }
                }
            }

            // Group notices
            if ((im.dialog == (byte) InstantMessageDialog.GroupNotice))
            {
                if (!m_groupNoticesEnabled)
                    return;

                UUID GroupID = im.toAgentID;
                if (m_groupData.GetGroupRecord(GetRequestingAgentID(remoteClient), GroupID, null) != null)
                {
                    UUID NoticeID = UUID.Random();
                    string Subject = im.message.Substring(0, im.message.IndexOf('|'));
                    string Message = im.message.Substring(Subject.Length + 1);

                    byte[] bucket;
                    UUID ItemID = UUID.Zero;
                    int AssetType = 0;
                    string ItemName = "";

                    if ((im.binaryBucket.Length == 1) && (im.binaryBucket[0] == 0))
                    {
                        bucket = new byte[19];
                        bucket[0] = 0;
                        bucket[1] = 0;
                        GroupID.ToBytes(bucket, 2);
                        bucket[18] = 0;
                    }
                    else
                    {
                        bucket = im.binaryBucket;
                        string binBucket = Utils.BytesToString(im.binaryBucket);
                        binBucket = binBucket.Remove(0, 14).Trim();

                        OSDMap binBucketOSD = (OSDMap) OSDParser.DeserializeLLSDXml(binBucket);
                        if (binBucketOSD.ContainsKey("item_id"))
                        {
                            ItemID = binBucketOSD["item_id"].AsUUID();

                            InventoryItemBase item = m_sceneList[0].InventoryService.GetItem(new InventoryItemBase(ItemID, GetRequestingAgentID(remoteClient)));
                            if (item != null)
                            {
                                AssetType = item.AssetType;
                                ItemName = item.Name;
                            }
                            else
                                ItemID = UUID.Zero;
                        }
                    }

                    m_groupData.AddGroupNotice(GetRequestingAgentID(remoteClient), GroupID, NoticeID, im.fromAgentName,
                                               Subject, Message, ItemID, AssetType, ItemName);
                    if (OnNewGroupNotice != null)
                        OnNewGroupNotice(GroupID, NoticeID);
                    GroupNoticeInfo notice = new GroupNoticeInfo()
                    {
                        BinaryBucket = im.binaryBucket,
                        GroupID = GroupID,
                        Message = Message,
                        noticeData = new GroupNoticeData()
                        {
                            AssetType = (byte)AssetType,
                            FromName = im.fromAgentName,
                            GroupID = GroupID,
                            HasAttachment = ItemID != UUID.Zero,
                            ItemID = ItemID,
                            ItemName = ItemName,
                            NoticeID = NoticeID,
                            Subject = Subject,
                            Timestamp = im.timestamp
                        }
                    };

                    // Send notice out to everyone that wants notices
                    foreach (
                        GroupMembersData member in
                            m_groupData.GetGroupMembers(GetRequestingAgentID(remoteClient), GroupID))
                    {
                        if (m_debugEnabled)
                        {
                            UserAccount targetUser =
                                m_sceneList[0].UserAccountService.GetUserAccount(remoteClient.Scene.RegionInfo.AllScopeIDs,
                                                                                 member.AgentID);
                            if (targetUser != null)
                            {
                                MainConsole.Instance.DebugFormat(
                                    "[GROUPS]: Prepping group notice {0} for agent: {1} who Accepts Notices ({2})",
                                    NoticeID, targetUser.FirstName + " " + targetUser.LastName, member.AcceptNotices);
                            }
                            else
                            {
                                MainConsole.Instance.DebugFormat(
                                    "[GROUPS]: Prepping group notice {0} for agent: {1} who Accepts Notices ({2})",
                                    NoticeID, member.AgentID, member.AcceptNotices);
                            }
                        }

                        if (member.AcceptNotices)
                        {
                            // Build notice IIM
                            GridInstantMessage msg = CreateGroupNoticeIM(GetRequestingAgentID(remoteClient), notice,
                                                                         (byte) InstantMessageDialog.GroupNotice);

                            msg.toAgentID = member.AgentID;
                            OutgoingInstantMessage(msg, member.AgentID);
                        }
                    }
                }
            }
            else if ((im.dialog == (byte) InstantMessageDialog.GroupNoticeInventoryDeclined) ||
                     (im.dialog == (byte) InstantMessageDialog.GroupNoticeInventoryDeclined))
            {
                //GroupAttachmentCache.Remove(im.imSessionID);
            }
            else if ((im.dialog == (byte) InstantMessageDialog.GroupNoticeInventoryAccepted) ||
                     (im.dialog == (byte) InstantMessageDialog.GroupNoticeInventoryAccepted))
            {
                UUID FolderID = new UUID(im.binaryBucket, 0);
                remoteClient.Scene.InventoryService.GiveInventoryItemAsync(remoteClient.AgentId, im.imSessionID,
                    im.imSessionID, FolderID, false,
                    (item) =>
                    {

                        if (item != null)
                            remoteClient.SendBulkUpdateInventory(item);
                    });
                //GroupAttachmentCache.Remove(im.imSessionID);
            }
            else if ((im.dialog == 210))
            {
                // This is sent from the region that the ejectee was ejected from
                // if it's being delivered here, then the ejectee is here
                // so we need to send local updates to the agent.

                UUID ejecteeID = im.toAgentID;

                im.dialog = (byte) InstantMessageDialog.MessageFromAgent;
                OutgoingInstantMessage(im, ejecteeID);

                IClientAPI ejectee = GetActiveClient(ejecteeID);
                if (ejectee != null)
                {
                    UUID groupID = im.imSessionID;
                    ejectee.SendAgentDropGroup(groupID);
                    if (ejectee.ActiveGroupId == groupID)
                        GroupTitleUpdate(ejectee, UUID.Zero, UUID.Zero);
                }
            }

                // Interop, received special 211 code for offline group notice
            else if ((im.dialog == 211))
            {
                im.dialog = (byte) InstantMessageDialog.GroupNotice;

                //In offline group notices, imSessionID is replaced with the NoticeID so that we can rebuild the packet here
                GroupNoticeInfo GND = m_groupData.GetGroupNotice(im.toAgentID, im.imSessionID);

                //We reset the ID so that if this was set before, it won't be misadded or anything to the cache
                im.imSessionID = UUID.Random();

                //Rebuild the binary bucket
                if (GND.noticeData.HasAttachment)
                {
                    im.binaryBucket = CreateBitBucketForGroupAttachment(GND.noticeData, GND.GroupID);
                    //Save the sessionID for the callback by the client (reject or accept)
                    //Only save if has attachment
                    im.imSessionID = GND.noticeData.ItemID;
                    //GroupAttachmentCache[im.imSessionID] = GND.noticeData.ItemID;
                }
                else
                {
                    byte[] bucket = new byte[19];
                    bucket[0] = 0; //Attachment enabled == false so 0
                    bucket[1] = 0; //No attachment, so no asset type
                    GND.GroupID.ToBytes(bucket, 2);
                    bucket[18] = 0; //dunno
                    im.binaryBucket = bucket;
                }

                OutgoingInstantMessage(im, im.toAgentID);

                //You MUST reset this, otherwise the client will get it twice,
                // as it goes through OnGridInstantMessage
                // which will check and then reresent the notice
                im.dialog = 211;
            }
        }

        private GroupMembershipData AttemptFindGroupMembershipData(UUID requestingAgentID, UUID agentID, UUID groupID)
        {
            if (m_cachedGroupMemberships.ContainsKey(agentID))
            {
                foreach (GroupMembershipData data in from d in m_cachedGroupMemberships[agentID] where d.GroupID == groupID select d)
                return data;
            }
            return m_groupData.GetAgentGroupMembership(requestingAgentID, agentID, groupID);
        }

        private void OnGridInstantMessage(GridInstantMessage msg)
        {
            if (m_debugEnabled) MainConsole.Instance.InfoFormat("[GROUPS]: {0} called", MethodBase.GetCurrentMethod().Name);

            // Trigger the above event handler
            OnInstantMessage(null, msg);

            // If a message from a group arrives here, it may need to be forwarded to a local client
            if (msg.fromGroup)
            {
                switch (msg.dialog)
                {
                    case (byte) InstantMessageDialog.GroupInvitation:
                    case (byte) InstantMessageDialog.GroupNotice:
                        UUID toAgentID = msg.toAgentID;
                        IClientAPI localClient = GetActiveClient(toAgentID);
                        if (localClient != null)
                        {
                            localClient.SendInstantMessage(msg);
                        }
                        break;
                }
            }
        }

        #endregion

        protected OSDMap OnRegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["GroupProposalBallot"] = CapsUtil.CreateCAPS("GroupProposalBallot", "");

            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["GroupProposalBallot"],
                                                      delegate(string path, Stream request, OSHttpRequest httpRequest,
                                                            OSHttpResponse httpResponse)
                                                      {
                                                          return GroupProposalBallot(request.ReadUntilEnd(), agentID);
                                                      }));
            retVal["StartGroupProposal"] = CapsUtil.CreateCAPS("StartGroupProposal", "");
            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["StartGroupProposal"],
                                                      delegate(string path, Stream request, OSHttpRequest httpRequest,
                                                            OSHttpResponse httpResponse)
                                                      {
                                                          return StartGroupProposal(request.ReadUntilEnd(), agentID);
                                                      }));
            return retVal;
        }

        private GridInstantMessage BuildGroupNoticeIM(GroupNoticeInfo data, UUID groupNoticeID, UUID AgentID)
        {
            GridInstantMessage msg = new GridInstantMessage
                                         {
                                             fromAgentID = data.GroupID,
                                             toAgentID = AgentID,
                                             timestamp = data.noticeData.Timestamp,
                                             fromAgentName = data.noticeData.FromName,
                                             message = data.noticeData.Subject + "|" + data.Message,
                                             dialog = (byte) InstantMessageDialog.GroupNoticeRequested,
                                             fromGroup = true,
                                             offline = 1,
                                             ParentEstateID = 0,
                                             Position = Vector3.Zero,
                                             RegionID = UUID.Zero,
                                             imSessionID = UUID.Random()
                                         };

            //Allow offline

            if (data.noticeData.HasAttachment)
            {
                msg.binaryBucket = CreateBitBucketForGroupAttachment(data.noticeData, data.GroupID);
                //Save the sessionID for the callback by the client (reject or accept)
                //Only save if has attachment
                msg.imSessionID = data.noticeData.ItemID;
                //GroupAttachmentCache[msg.imSessionID] = data.noticeData.ItemID;
            }
            else
            {
                byte[] bucket = new byte[19];
                bucket[0] = 0; //Attachment enabled == false so 0
                bucket[1] = 0; //No attachment, so no asset type
                data.GroupID.ToBytes(bucket, 2);
                bucket[18] = 0; //dunno
                msg.binaryBucket = bucket;
            }
            return msg;
        }

        private byte[] CreateBitBucketForGroupAttachment(GroupNoticeData groupNoticeData, UUID groupID)
        {
            int i = 20;
            i += groupNoticeData.ItemName.Length;
            byte[] bitbucket = new byte[i];
            groupID.ToBytes(bitbucket, 2);
            byte[] name = Utils.StringToBytes(" " + groupNoticeData.ItemName);
            Array.ConstrainedCopy(name, 0, bitbucket, 18, name.Length);
            //Utils.Int16ToBytes((short)item.AssetType, bitbucket, 0);
            bitbucket[0] = 1; // 0 for no attachment, 1 for attachment
            bitbucket[1] = groupNoticeData.AssetType; // Asset type

            return bitbucket;
        }

        private UUID GetRequestingAgentID(IClientAPI client)
        {
            UUID requestingAgentID = UUID.Zero;
            if (client != null)
            {
                requestingAgentID = client.AgentId;
            }
            return requestingAgentID;
        }

        #region Permissions

        /// <summary>
        ///   This caches the current group powers that the agent has
        ///   TKey 1 - UUID of the agent
        ///   TKey 2 - UUID of the group
        ///   TValue - Powers of the agent in the given group
        /// </summary>
        private readonly Dictionary<UUID, Dictionary<UUID, ulong>> AgentGroupPowersCache =
            new Dictionary<UUID, Dictionary<UUID, ulong>>();

        /// <summary>
        ///   WARNING: This is not the only place permissions are checked! They are checked in each of the connectors as well!
        /// </summary>
        /// <param name = "AgentID"></param>
        /// <param name = "GroupID"></param>
        /// <returns></returns>
        public bool GroupPermissionCheck(UUID AgentID, UUID GroupID, GroupPowers permissions)
        {
            if (GroupID == UUID.Zero)
                return false;

            if (AgentID == UUID.Zero)
                return false;

            ulong ourPowers = 0;

            Dictionary<UUID, ulong> groupsCache;
            lock (AgentGroupPowersCache)
            {
                if (AgentGroupPowersCache.TryGetValue(AgentID, out groupsCache))
                {
                    if (groupsCache.ContainsKey(GroupID))
                    {
                        ourPowers = groupsCache[GroupID];
                        if (ourPowers == 1)
                            return false;
                                //1 means not in the group or not found in the cache, so stop it here so that we don't check every time, and it can't be a permission, as its 0 then 2 in GroupPermissions
                    }
                }
            }
            //Ask the server as we don't know about this user
            if (ourPowers == 0)
            {
                GroupMembershipData GMD = AttemptFindGroupMembershipData(AgentID, AgentID, GroupID);
                if (GMD == null)
                {
                    AddToGroupPowersCache(AgentID, GroupID, 1);
                    return false;
                }
                ourPowers = GMD.GroupPowers;
                //Add to the cache
                AddToGroupPowersCache(AgentID, GroupID, ourPowers);
            }

            //The user is the group, or it would have been weeded out earlier, so check whether we just need to know whether they are in the group
            if (permissions == GroupPowers.None)
                return true;

            if ((((GroupPowers) ourPowers) & permissions) != permissions)
                return false;

            return true;
        }

        private void AddToGroupPowersCache(UUID AgentID, UUID GroupID, ulong powers)
        {
            lock (AgentGroupPowersCache)
            {
                Dictionary<UUID, ulong> Groups = new Dictionary<UUID, ulong>();
                if (!AgentGroupPowersCache.TryGetValue(AgentID, out Groups))
                    Groups = new Dictionary<UUID, ulong>();
                Groups[GroupID] = powers;
                AgentGroupPowersCache[AgentID] = Groups;
            }
        }

        private void RemoveFromGroupPowersCache(UUID AgentID, UUID GroupID)
        {
            lock (AgentGroupPowersCache)
            {
                Dictionary<UUID, ulong> Groups = new Dictionary<UUID, ulong>();
                if (AgentGroupPowersCache.TryGetValue(AgentID, out Groups))
                {
                    Groups.Remove(GroupID);
                    AgentGroupPowersCache[AgentID] = Groups;
                }
            }
        }

        #endregion
    }
}