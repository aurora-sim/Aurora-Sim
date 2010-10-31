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
using System.Timers;

using log4net;
using Nini.Config;

using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;

using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Region.CoreModules.Framework.EventQueue;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

using OpenSim.Services.Interfaces;

using Caps = OpenSim.Framework.Capabilities.Caps;
using DirFindFlags = OpenMetaverse.DirectoryManager.DirFindFlags;



namespace OpenSim.Region.OptionalModules.Avatar.XmlRpcGroups
{
    public class GroupsModule : ISharedRegionModule, IGroupsModule
    {
        /// <summary>
        /// ; To use this module, you must specify the following in your OpenSim.ini
        /// [GROUPS]
        /// Enabled = true
        /// 
        /// Module   = GroupsModule
        /// NoticesEnabled = true
        /// DebugEnabled   = true
        /// 
        /// GroupsServicesConnectorModule = XmlRpcGroupsServicesConnector
        /// XmlRpcServiceURL      = http://osflotsam.org/xmlrpc.php
        /// XmlRpcServiceReadKey  = 1234
        /// XmlRpcServiceWriteKey = 1234
        /// 
        /// MessagingModule  = GroupsMessagingModule
        /// MessagingEnabled = true
        /// 
        /// ; Disables HTTP Keep-Alive for Groups Module HTTP Requests, work around for
        /// ; a problem discovered on some Windows based region servers.  Only disable
        /// ; if you see a large number (dozens) of the following Exceptions:
        /// ; System.Net.WebException: The request was aborted: The request was canceled.
        ///
        /// XmlRpcDisableKeepAlive = false
        /// </summary>

        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private List<Scene> m_sceneList = new List<Scene>();

        private IMessageTransferModule m_msgTransferModule = null;

        private IGroupsMessagingModule m_groupsMessagingModule = null;
        
        private IGroupsServicesConnector m_groupData = null;

        // Configuration settings
        private bool m_groupsEnabled = false;
        private bool m_groupNoticesEnabled = true;
        private bool m_debugEnabled = true;
        private Dictionary<Guid, UUID> GroupAttachmentCache = new Dictionary<Guid, UUID>();
        private Dictionary<Guid, UUID> GroupSessionIDCache = new Dictionary<Guid, UUID>(); //For offline messages
        private bool m_findOnlineStatus = false;

        #region IRegionModuleBase Members

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

                //m_log.InfoFormat("[GROUPS]: Initializing {0}", this.Name);

                m_groupNoticesEnabled   = groupsConfig.GetBoolean("NoticesEnabled", true);
                m_debugEnabled          = groupsConfig.GetBoolean("DebugEnabled", true);
                m_findOnlineStatus      = groupsConfig.GetBoolean("FindUserOnlineStatus", m_findOnlineStatus);

            }
        }

        public void AddRegion(Scene scene)
        {
            if (m_groupsEnabled)
                scene.RegisterModuleInterface<IGroupsModule>(this);
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_groupsEnabled)
                return;

            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            if (m_groupData == null)
            {
                m_groupData = scene.RequestModuleInterface<IGroupsServicesConnector>();

                // No Groups Service Connector, then nothing works...
                if (m_groupData == null)
                {
                    m_groupsEnabled = false;
                    m_log.Error("[GROUPS]: Could not get IGroupsServicesConnector");
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
                    m_log.Error("[GROUPS]: Could not get MessageTransferModule");
                    Close();
                    return;
                }
            }

            lock (m_sceneList)
            {
                m_sceneList.Add(scene);
            }

            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
            scene.EventManager.OnIncomingInstantMessage += OnGridInstantMessage;
            scene.EventManager.OnClientLogin += EventManager_OnClientLogin;
            scene.EventManager.OnRegisterCaps += new EventManager.RegisterCapsEvent(EventManager_OnRegisterCaps);
            // The InstantMessageModule itself doesn't do this, 
            // so lets see if things explode if we don't do it
            // scene.EventManager.OnClientClosed += OnClientClosed;

        }

        void EventManager_OnRegisterCaps(UUID agentID, Caps caps)
        {
            /*string capsBase = "/CAPS/" + UUID.Random();
            caps.RegisterHandler("GroupProposalBallot",
                                new RestHTTPHandler("POST", capsBase + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return GroupProposalBallot(m_dhttpMethod, agentID);
                                                      }));*/
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_groupsEnabled)
                return;

            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            lock (m_sceneList)
            {
                m_sceneList.Remove(scene);
            }
            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
            scene.EventManager.OnIncomingInstantMessage -= OnGridInstantMessage;
            scene.EventManager.OnClientLogin -= EventManager_OnClientLogin;
            scene.EventManager.OnRegisterCaps -= new EventManager.RegisterCapsEvent(EventManager_OnRegisterCaps);
        }

        public void Close()
        {
            if (!m_groupsEnabled)
                return;

            if (m_debugEnabled) m_log.Debug("[GROUPS]: Shutting down Groups module.");
        }

        public Type ReplaceableInterface 
        {
            get { return null; }
        }

        public string Name
        {
            get { return "GroupsModule"; }
        }

        #endregion

        #region ISharedRegionModule Members

        public void PostInitialise()
        {
            // NoOp
        }

        #endregion

        #region EventHandlers
        private void OnNewClient(IClientAPI client)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            client.OnUUIDGroupNameRequest += HandleUUIDGroupNameRequest;
            client.OnAgentDataUpdateRequest += OnAgentDataUpdateRequest;
            client.OnDirFindQuery += OnDirFindQuery;
            client.OnRequestAvatarProperties += OnRequestAvatarProperties;

            // Used for Notices and Group Invites/Accept/Reject
            client.OnInstantMessage += OnInstantMessage;

            ScenePresence SP = m_sceneList[0].GetScenePresence(client.AgentId);
            // Send client thier groups information.
            if(SP != null && !SP.IsChildAgent)
                SendAgentGroupDataUpdate(client, client.AgentId);
        }

        private void OnClosingClient(IClientAPI client)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            client.OnUUIDGroupNameRequest -= HandleUUIDGroupNameRequest;
            client.OnAgentDataUpdateRequest -= OnAgentDataUpdateRequest;
            client.OnDirFindQuery -= OnDirFindQuery;
            client.OnRequestAvatarProperties -= OnRequestAvatarProperties;

            // Used for Notices and Group Invites/Accept/Reject
            client.OnInstantMessage -= OnInstantMessage;
        }

        private void OnRequestAvatarProperties(IClientAPI remoteClient, UUID avatarID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            //GroupMembershipData[] avatarGroups = m_groupData.GetAgentGroupMemberships(GetRequestingAgentID(remoteClient), avatarID).ToArray();
            GroupMembershipData[] avatarGroups = GetProfileListedGroupMemberships(remoteClient, avatarID);
            remoteClient.SendAvatarGroupsReply(avatarID, avatarGroups);
        }

        void EventManager_OnClientLogin(IClientAPI client)
        {
            List<GroupInviteInfo> inviteInfo = m_groupData.GetGroupInvites(client.AgentId);

            if (inviteInfo.Count != 0)
            {
                foreach (GroupInviteInfo Invite in inviteInfo)
                {
                    if (m_msgTransferModule != null)
                    {
                        Guid inviteUUID = Invite.InviteID.Guid;

                        GridInstantMessage msg = new GridInstantMessage();

                        msg.imSessionID = inviteUUID;

                        msg.fromAgentID = Invite.GroupID.Guid;
                        msg.toAgentID = Invite.AgentID.Guid;
                        msg.timestamp = (uint)Util.UnixTimeSinceEpoch();
                        msg.fromAgentName = Invite.FromAgentName;

                        GroupRecord groupInfo = GetGroupRecord(Invite.GroupID);
                        string MemberShipCost = ". There is no cost to join this group.";
                        if (groupInfo.MembershipFee != 0)
                            MemberShipCost = ". To join, you must pay " + groupInfo.MembershipFee.ToString() + ".";

                        msg.message = string.Format("{0} has invited you to join " + groupInfo.GroupName + MemberShipCost, Invite.FromAgentName);
                        msg.dialog = (byte)OpenMetaverse.InstantMessageDialog.GroupInvitation;
                        msg.fromGroup = true;
                        msg.offline = (byte)0;
                        msg.ParentEstateID = 0;
                        msg.Position = Vector3.Zero;
                        msg.RegionID = UUID.Zero.Guid;
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
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

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
                    if (m_debugEnabled) m_log.WarnFormat("[GROUPS]: Client closed that wasn't registered here.");
                }

                
            }
        }
        */

        void OnDirFindQuery(IClientAPI remoteClient, UUID queryID, string queryText, uint queryFlags, int queryStart)
        {
            if (((DirFindFlags)queryFlags & DirFindFlags.Groups) == DirFindFlags.Groups)
            {
                if (m_debugEnabled)
                    m_log.DebugFormat(
                        "[GROUPS]: {0} called with queryText({1}) queryFlags({2}) queryStart({3})",
                        System.Reflection.MethodBase.GetCurrentMethod().Name, queryText, (DirFindFlags)queryFlags, queryStart);

                remoteClient.SendDirGroupsReply(queryID, m_groupData.FindGroups(GetRequestingAgentID(remoteClient), queryText, queryStart, queryFlags).ToArray());
            }
        }

        private void OnAgentDataUpdateRequest(IClientAPI remoteClient, UUID dataForAgentID, UUID sessionID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            UUID activeGroupID = UUID.Zero;
            string activeGroupTitle = string.Empty;
            string activeGroupName = string.Empty;
            ulong activeGroupPowers  = (ulong)GroupPowers.None;

            GroupMembershipData membership = m_groupData.GetAgentActiveMembership(GetRequestingAgentID(remoteClient), dataForAgentID);
            if (membership != null)
            {
                activeGroupID = membership.GroupID;
                activeGroupTitle = membership.GroupTitle;
                activeGroupPowers = membership.GroupPowers;
                activeGroupName = membership.GroupName;
            }

            SendAgentDataUpdate(remoteClient, dataForAgentID, activeGroupID, activeGroupName, activeGroupPowers, activeGroupTitle);

            SendScenePresenceUpdate(dataForAgentID, activeGroupTitle);
        }

        private void HandleUUIDGroupNameRequest(UUID GroupID, IClientAPI remoteClient)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            string GroupName;
            
            GroupRecord group = m_groupData.GetGroupRecord(GetRequestingAgentID(remoteClient), GroupID, null);
            if (group != null)
            {
                GroupName = group.GroupName;
            }
            else
            {
                GroupName = "Unknown";
            }

            remoteClient.SendGroupNameReply(GroupID, GroupName);
        }

        private void OnInstantMessage(IClientAPI remoteClient, GridInstantMessage im)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            // Group invitations
            if ((im.dialog == (byte)InstantMessageDialog.GroupInvitationAccept) || (im.dialog == (byte)InstantMessageDialog.GroupInvitationDecline))
            {
                UUID inviteID = new UUID(im.imSessionID);
                GroupInviteInfo inviteInfo = m_groupData.GetAgentToGroupInvite(GetRequestingAgentID(remoteClient), inviteID);

                if (inviteInfo == null)
                {
                    if (m_debugEnabled) m_log.WarnFormat("[GROUPS]: Received an Invite IM for an invite that does not exist {0}.", inviteID);
                    return;
                }

                if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: Invite is for Agent {0} to Group {1}.", inviteInfo.AgentID, inviteInfo.GroupID);

                UUID fromAgentID = new UUID(im.fromAgentID);
                if ((inviteInfo != null) && (fromAgentID == inviteInfo.AgentID))
                {
                    // Accept
                    if (im.dialog == (byte)InstantMessageDialog.GroupInvitationAccept)
                    {
                        if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: Received an accept invite notice.");

                        // and the sessionid is the role
                        m_groupData.AddAgentToGroup(GetRequestingAgentID(remoteClient), inviteInfo.AgentID, inviteInfo.GroupID, inviteInfo.RoleID);

                        GridInstantMessage msg = new GridInstantMessage();
                        msg.imSessionID = UUID.Zero.Guid;
                        msg.fromAgentID = UUID.Zero.Guid;
                        msg.toAgentID = inviteInfo.AgentID.Guid;
                        msg.timestamp = (uint)Util.UnixTimeSinceEpoch();
                        msg.fromAgentName = "Groups";
                        msg.message = string.Format("You have been added to the group.");
                        msg.dialog = (byte)OpenMetaverse.InstantMessageDialog.MessageBox;
                        msg.fromGroup = false;
                        msg.offline = (byte)0;
                        msg.ParentEstateID = 0;
                        msg.Position = Vector3.Zero;
                        msg.RegionID = UUID.Zero.Guid;
                        msg.binaryBucket = new byte[0];

                        OutgoingInstantMessage(msg, inviteInfo.AgentID);

                        //WTH??? noone but the invitee needs to know
                        //UpdateAllClientsWithGroupInfo(inviteInfo.AgentID);
                        SendAgentGroupDataUpdate(remoteClient);
                        // XTODO: If the inviter is still online, they need an agent dataupdate 
                        // and maybe group membership updates for the invitee
                        // Reply: why do they need that? they will get told about the new user when they reopen the groups panel

                        m_groupData.RemoveAgentToGroupInvite(GetRequestingAgentID(remoteClient), inviteID);
                    }

                    // Reject
                    if (im.dialog == (byte)InstantMessageDialog.GroupInvitationDecline)
                    {
                        if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: Received a reject invite notice.");
                        m_groupData.RemoveAgentToGroupInvite(GetRequestingAgentID(remoteClient), inviteID);
                    }
                }
            }

            // Group notices
            if ((im.dialog == (byte)InstantMessageDialog.GroupNotice))
            {
                if (!m_groupNoticesEnabled)
                    return;

                UUID GroupID = new UUID(im.toAgentID);
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
                        string binBucket = OpenMetaverse.Utils.BytesToString(im.binaryBucket);
                        binBucket = binBucket.Remove(0, 14).Trim();

                        OSDMap binBucketOSD = (OSDMap)OSDParser.DeserializeLLSDXml(binBucket);
                        if (binBucketOSD.ContainsKey("item_id"))
                        {
                            ItemID = binBucketOSD["item_id"].AsUUID();

                            InventoryItemBase item = new InventoryItemBase(ItemID, GetRequestingAgentID(remoteClient));
                            item = m_sceneList[0].InventoryService.GetItem(item);
                            if (item != null)
                            {
                                AssetType = item.AssetType;
                                ItemName = item.Name;
                            }
                            else
                                ItemID = UUID.Zero;
                        }
                    }

                    m_groupData.AddGroupNotice(GetRequestingAgentID(remoteClient), GroupID, NoticeID, im.fromAgentName, Subject, Message, ItemID, AssetType, ItemName);
                    if (OnNewGroupNotice != null)
                        OnNewGroupNotice(GroupID, NoticeID);

                    // Send notice out to everyone that wants notices
                    foreach (GroupMembersData member in m_groupData.GetGroupMembers(GetRequestingAgentID(remoteClient), GroupID))
                    {
                        if (m_debugEnabled)
                        {
                            UserAccount targetUser = m_sceneList[0].UserAccountService.GetUserAccount(remoteClient.Scene.RegionInfo.ScopeID, member.AgentID);
                            if (targetUser != null)
                            {
                                m_log.DebugFormat("[GROUPS]: Prepping group notice {0} for agent: {1} who Accepts Notices ({2})", NoticeID, targetUser.FirstName + " " + targetUser.LastName, member.AcceptNotices);
                            }
                            else
                            {
                                m_log.DebugFormat("[GROUPS]: Prepping group notice {0} for agent: {1} who Accepts Notices ({2})", NoticeID, member.AgentID, member.AcceptNotices);
                            }
                        }

                        if (member.AcceptNotices)
                        {
                            // Build notice IIM
                            GridInstantMessage msg = CreateGroupNoticeIM(GetRequestingAgentID(remoteClient), NoticeID, (byte)OpenMetaverse.InstantMessageDialog.GroupNotice);

                            msg.toAgentID = member.AgentID.Guid;
                            OutgoingInstantMessage(msg, member.AgentID);
                        }
                    }
                }
            }
            if ((im.dialog == (byte)InstantMessageDialog.GroupNoticeInventoryDeclined) || (im.dialog == (byte)InstantMessageDialog.GroupNoticeInventoryDeclined))
            {
                GroupAttachmentCache.Remove(im.imSessionID);
            }
            if ((im.dialog == (byte)InstantMessageDialog.GroupNoticeInventoryAccepted) || (im.dialog == (byte)InstantMessageDialog.GroupNoticeInventoryAccepted))
            {
                UUID FolderID = new UUID(im.binaryBucket, 0);
                InventoryItemBase item = new InventoryItemBase(GroupAttachmentCache[im.imSessionID]);
                item = ((Scene)remoteClient.Scene).InventoryService.GetItem(item);

                item = ((Scene)remoteClient.Scene).GiveInventoryItem(remoteClient.AgentId, item.Owner, GroupAttachmentCache[im.imSessionID], FolderID);

                if (item != null)
                    remoteClient.SendBulkUpdateInventory(item);
                GroupAttachmentCache.Remove(im.imSessionID);
            }
            
            if ((im.dialog == 210))
            {
                // This is sent from the region that the ejectee was ejected from
                // if it's being delivered here, then the ejectee is here
                // so we need to send local updates to the agent.

                UUID ejecteeID = new UUID(im.toAgentID);

                im.dialog = (byte)InstantMessageDialog.MessageFromAgent;
                OutgoingInstantMessage(im, ejecteeID);

                IClientAPI ejectee = GetActiveClient(ejecteeID);
                if (ejectee != null)
                {
                    UUID groupID = new UUID(im.imSessionID);
                    ejectee.SendAgentDropGroup(groupID);
                }
            }

            // Interop, received special 211 code for offline group notice
            if ((im.dialog == 211))
            {
                im.dialog = (byte)InstantMessageDialog.GroupNotice;

                //In offline group notices, imSessionID is replaced with the NoticeID so that we can rebuild the packet here
                GroupNoticeInfo GND = m_groupData.GetGroupNotice(new UUID(im.toAgentID), new UUID(im.imSessionID));

                //We reset the ID so that if this was set before, it won't be misadded or anything to the cache
                im.imSessionID = UUID.Random().Guid; 

                //Rebuild the binary bucket
                if (GND.noticeData.HasAttachment)
                {
                    im.binaryBucket = CreateBitBucketForGroupAttachment(GND.noticeData, GND.GroupID);
                    //Save the sessionID for the callback by the client (reject or accept)
                    //Only save if has attachment
                    GroupAttachmentCache[im.imSessionID] = GND.noticeData.ItemID;
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

                OutgoingInstantMessage(im, new UUID(im.toAgentID));

                //You MUST reset this, otherwise the client will get it twice,
                // as it goes through OnGridInstantMessage
                // which will check and then reresent the notice
                im.dialog = 211;
            }
        }

        private void OnGridInstantMessage(GridInstantMessage msg)
        {
            if (m_debugEnabled) m_log.InfoFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            // Trigger the above event handler
            OnInstantMessage(null, msg);

            // If a message from a group arrives here, it may need to be forwarded to a local client
            if (msg.fromGroup == true)
            {
                switch (msg.dialog)
                {
                    case (byte)InstantMessageDialog.GroupInvitation:
                    case (byte)InstantMessageDialog.GroupNotice:
                        UUID toAgentID = new UUID(msg.toAgentID);
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
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            m_groupData.SetAgentActiveGroup(GetRequestingAgentID(remoteClient), GetRequestingAgentID(remoteClient), groupID);

            // Changing active group changes title, active powers, all kinds of things
            // anyone who is in any region that can see this client, should probably be 
            // updated with new group info.  At a minimum, they should get ScenePresence
            // updated with new title.
            UpdateAllClientsWithGroupInfo(GetRequestingAgentID(remoteClient));
        }

        /// <summary>
        /// Get the Role Titles for an Agent, for a specific group
        /// </summary>
        public List<GroupTitlesData> GroupTitlesRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);


            List<GroupRolesData> agentRoles = m_groupData.GetAgentGroupRoles(GetRequestingAgentID(remoteClient), GetRequestingAgentID(remoteClient), groupID);
            GroupMembershipData agentMembership = m_groupData.GetAgentGroupMembership(GetRequestingAgentID(remoteClient), GetRequestingAgentID(remoteClient), groupID);

            List<GroupTitlesData> titles = new List<GroupTitlesData>();
            foreach (GroupRolesData role in agentRoles)
            {
                GroupTitlesData title = new GroupTitlesData();
                title.Name = role.Name;
                if (agentMembership != null)
                {
                    title.Selected = agentMembership.ActiveRole == role.RoleID;
                }
                title.UUID = role.RoleID;

                titles.Add(title);
            }

            return titles;
        }

        public List<GroupMembersData> GroupMembersRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);
            List<GroupMembersData> data = m_groupData.GetGroupMembers(GetRequestingAgentID(remoteClient), groupID);

            if (m_findOnlineStatus)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    GridUserInfo info = m_sceneList[0].GridUserService.GetGridUserInfo(data[i].AgentID.ToString());
                    if (info != null && !info.Online)
                        data[i].OnlineStatus = info.Logout.ToShortDateString().ToString();
                    else if (info == null)
                        data[i].OnlineStatus = "Unknown";
                }
            }

            if (m_debugEnabled)
            {
                foreach (GroupMembersData member in data)
                {
                    m_log.DebugFormat("[GROUPS]: Member({0}) - IsOwner({1})", member.AgentID, member.IsOwner);
                }
            }

            return data;

        }

        public List<GroupRolesData> GroupRoleDataRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            List<GroupRolesData> data = m_groupData.GetGroupRoles(GetRequestingAgentID(remoteClient), groupID);

            return data;
        }

        public List<GroupRoleMembersData> GroupRoleMembersRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            List<GroupRoleMembersData> data = m_groupData.GetGroupRoleMembers(GetRequestingAgentID(remoteClient), groupID);

            if (m_debugEnabled)
            {
                foreach (GroupRoleMembersData member in data)
                {
                    m_log.DebugFormat("[GROUPS]: Member({0}) - Role({1})", member.MemberID, member.RoleID);
                }
            }
            return data;
        }

        public GroupProfileData GroupProfileRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            GroupProfileData profile = new GroupProfileData();


            GroupRecord groupInfo = m_groupData.GetGroupRecord(GetRequestingAgentID(remoteClient), groupID, null);
            if (groupInfo != null)
            {
                profile.AllowPublish = groupInfo.AllowPublish;
                profile.Charter = groupInfo.Charter;
                profile.FounderID = groupInfo.FounderID;
                profile.GroupID = groupID;
                profile.GroupMembershipCount = m_groupData.GetGroupMembers(GetRequestingAgentID(remoteClient), groupID).Count;
                profile.GroupRolesCount = m_groupData.GetGroupRoles(GetRequestingAgentID(remoteClient), groupID).Count;
                profile.InsigniaID = groupInfo.GroupPicture;
                profile.MaturePublish = groupInfo.MaturePublish;
                profile.MembershipFee = groupInfo.MembershipFee;
                profile.Money = 0; // TODO: Get this from the currency server?
                profile.Name = groupInfo.GroupName;
                profile.OpenEnrollment = groupInfo.OpenEnrollment;
                profile.OwnerRole = groupInfo.OwnerRoleID;
                profile.ShowInList = groupInfo.ShowInList;
            }

            GroupMembershipData memberInfo = m_groupData.GetAgentGroupMembership(GetRequestingAgentID(remoteClient), GetRequestingAgentID(remoteClient), groupID);
            if (memberInfo != null)
            {
                profile.MemberTitle = memberInfo.GroupTitle;
                profile.PowersMask = memberInfo.GroupPowers;
            }

            return profile;
        }

        public GroupMembershipData[] GetMembershipData(UUID agentID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            return m_groupData.GetAgentGroupMemberships(UUID.Zero, agentID).ToArray();
        }

        public GroupMembershipData GetMembershipData(UUID groupID, UUID agentID)
        {
            if (m_debugEnabled) 
                m_log.DebugFormat(
                    "[GROUPS]: {0} called with groupID={1}, agentID={2}",
                    System.Reflection.MethodBase.GetCurrentMethod().Name, groupID, agentID);

            return m_groupData.GetAgentGroupMembership(UUID.Zero, agentID, groupID);
        }

        public void UpdateGroupInfo(IClientAPI remoteClient, UUID groupID, string charter, bool showInList, UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            // Note: Permissions checking for modification rights is handled by the Groups Server/Service
            m_groupData.UpdateGroup(GetRequestingAgentID(remoteClient), groupID, charter, showInList, insigniaID, membershipFee, openEnrollment, allowPublish, maturePublish);
        }

        public void SetGroupAcceptNotices(IClientAPI remoteClient, UUID groupID, bool acceptNotices, bool listInProfile)
        {
            // Note: Permissions checking for modification rights is handled by the Groups Server/Service
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            m_groupData.SetAgentGroupInfo(GetRequestingAgentID(remoteClient), GetRequestingAgentID(remoteClient), groupID, acceptNotices, listInProfile);
        }

        public UUID CreateGroup(IClientAPI remoteClient, string name, string charter, bool showInList, UUID insigniaID, int membershipFee, bool openEnrollment, bool allowPublish, bool maturePublish)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            if (m_groupData.GetGroupRecord(GetRequestingAgentID(remoteClient), UUID.Zero, name) != null)
            {
                remoteClient.SendCreateGroupReply(UUID.Zero, false, "A group with the same name already exists.");
                return UUID.Zero;
            }
            // is there is a money module present ?
            IMoneyModule money = remoteClient.Scene.RequestModuleInterface<IMoneyModule>();
            if (money != null)
            {
                // do the transaction, that is if the agent has got sufficient funds
                if (!money.AmountCovered(remoteClient, money.GroupCreationCharge)) {
                    remoteClient.SendCreateGroupReply(UUID.Zero, false, "You have got issuficient funds to create a group.");
                    return UUID.Zero;
                }
                money.ApplyCharge(GetRequestingAgentID(remoteClient), money.GroupCreationCharge, "Group Creation");
            }
            UUID groupID = m_groupData.CreateGroup(GetRequestingAgentID(remoteClient), name, charter, showInList, insigniaID, membershipFee, openEnrollment, allowPublish, maturePublish, GetRequestingAgentID(remoteClient));

            remoteClient.SendCreateGroupReply(groupID, true, "Group created successfullly");

            // Update the founder with new group information.
            SendAgentGroupDataUpdate(remoteClient, GetRequestingAgentID(remoteClient));

            return groupID;
        }

        public GroupNoticeData[] GroupNoticesListRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            return m_groupData.GetGroupNotices(GetRequestingAgentID(remoteClient), groupID).ToArray();
        }

        /// <summary>
        /// Get the title of the agent's current role.
        /// </summary>
        public string GetGroupTitle(UUID avatarID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            GroupMembershipData membership = m_groupData.GetAgentActiveMembership(UUID.Zero, avatarID);
            if (membership != null)
            {
                return membership.GroupTitle;
            } 
            return string.Empty;
        }

        /// <summary>
        /// Change the current Active Group Role for Agent
        /// </summary>
        public void GroupTitleUpdate(IClientAPI remoteClient, UUID groupID, UUID titleRoleID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            m_groupData.SetAgentActiveGroupRole(GetRequestingAgentID(remoteClient), GetRequestingAgentID(remoteClient), groupID, titleRoleID);

            // TODO: Not sure what all is needed here, but if the active group role change is for the group
            // the client currently has set active, then we need to do a scene presence update too
                
            UpdateAllClientsWithGroupInfo(GetRequestingAgentID(remoteClient));
        }


        public void GroupRoleUpdate(IClientAPI remoteClient, UUID groupID, UUID roleID, string name, string description, string title, ulong powers, byte updateType)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            // Security Checks are handled in the Groups Service.

            switch ((OpenMetaverse.GroupRoleUpdate)updateType)
            {
                case OpenMetaverse.GroupRoleUpdate.Create:
                    m_groupData.AddGroupRole(GetRequestingAgentID(remoteClient), groupID, UUID.Random(), name, description, title, powers);
                    break;

                case OpenMetaverse.GroupRoleUpdate.Delete:
                    m_groupData.RemoveGroupRole(GetRequestingAgentID(remoteClient), groupID, roleID);
                    break;

                case OpenMetaverse.GroupRoleUpdate.UpdateAll:
                case OpenMetaverse.GroupRoleUpdate.UpdateData:
                case OpenMetaverse.GroupRoleUpdate.UpdatePowers:
                    if (m_debugEnabled)
                    {
                        GroupPowers gp = (GroupPowers)powers;
                        m_log.DebugFormat("[GROUPS]: Role ({0}) updated with Powers ({1}) ({2})", name, powers.ToString(), gp.ToString());
                    }
                    m_groupData.UpdateGroupRole(GetRequestingAgentID(remoteClient), groupID, roleID, name, description, title, powers);
                    break;

                case OpenMetaverse.GroupRoleUpdate.NoUpdate:
                default:
                    // No Op
                    break;

            }

            // TODO: This update really should send out updates for everyone in the role that just got changed.
            foreach (ScenePresence SP in ((Scene)remoteClient.Scene).ScenePresences)
            {
                if (SP.ControllingClient.ActiveGroupId == groupID)
                    SendAgentGroupDataUpdate(SP.ControllingClient, GetRequestingAgentID(SP.ControllingClient));
            }
        }

        public void GroupRoleChanges(IClientAPI remoteClient, UUID groupID, UUID roleID, UUID memberID, uint changes)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);
            
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
                    m_log.ErrorFormat("[GROUPS]: {0} does not understand changes == {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, changes);
                    break;
            }

            // TODO: This update really should send out updates for everyone in the role that just got changed.
            SendAgentGroupDataUpdate(remoteClient, GetRequestingAgentID(remoteClient));
        }

        public void GroupNoticeRequest(IClientAPI remoteClient, UUID groupNoticeID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            GroupNoticeInfo data = m_groupData.GetGroupNotice(GetRequestingAgentID(remoteClient), groupNoticeID);

            if (data != null)
            {
                GridInstantMessage msg = BuildGroupNoticeIM(data, groupNoticeID, remoteClient.AgentId);
                OutgoingInstantMessage(msg, GetRequestingAgentID(remoteClient));
            }

        }

        private GridInstantMessage BuildGroupNoticeIM(GroupNoticeInfo data, UUID groupNoticeID, UUID AgentID)
        {
            GroupRecord groupInfo = m_groupData.GetGroupRecord(AgentID, data.GroupID, null);

            GridInstantMessage msg = new GridInstantMessage();

            msg.fromAgentID = data.GroupID.Guid;
            msg.toAgentID = AgentID.Guid;
            msg.timestamp = data.noticeData.Timestamp;
            msg.fromAgentName = data.noticeData.FromName;
            msg.message = data.noticeData.Subject + "|" + data.Message;
            msg.dialog = (byte)OpenMetaverse.InstantMessageDialog.GroupNoticeRequested;
            msg.fromGroup = true;
            msg.offline = (byte)1; //Allow offline
            msg.ParentEstateID = 0;
            msg.Position = Vector3.Zero;
            msg.RegionID = UUID.Zero.Guid;
            msg.imSessionID = UUID.Random().Guid;

            if (data.noticeData.HasAttachment)
            {
                msg.binaryBucket = CreateBitBucketForGroupAttachment(data.noticeData, data.GroupID);
                //Save the sessionID for the callback by the client (reject or accept)
                //Only save if has attachment
                GroupAttachmentCache[msg.imSessionID] = data.noticeData.ItemID;
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
            bitbucket[1] = (byte)groupNoticeData.AssetType; // Asset type
            
            return bitbucket;
        }

        public GridInstantMessage CreateGroupNoticeIM(UUID agentID, UUID groupNoticeID, byte dialog)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            GridInstantMessage msg = new GridInstantMessage();

            msg.toAgentID = agentID.Guid;
            msg.dialog = dialog;
            // msg.dialog = (byte)OpenMetaverse.InstantMessageDialog.GroupNotice;
            msg.fromGroup = true;
            msg.offline = (byte)1; // Allow this message to be stored for offline use
            msg.ParentEstateID = 0;
            msg.Position = Vector3.Zero;
            msg.RegionID = UUID.Zero.Guid;
            msg.imSessionID = UUID.Random().Guid;

            GroupNoticeInfo info = m_groupData.GetGroupNotice(agentID, groupNoticeID);
            if (info != null)
            {
                msg.fromAgentID = info.GroupID.Guid;
                msg.timestamp = info.noticeData.Timestamp;
                msg.fromAgentName = info.noticeData.FromName;
                msg.message = info.noticeData.Subject + "|" + info.Message;
                if (info.noticeData.HasAttachment)
                {
                    msg.binaryBucket = CreateBitBucketForGroupAttachment(info.noticeData, info.GroupID);
                    //Save the sessionID for the callback by the client (reject or accept)
                    //Only save if has attachment
                    GroupAttachmentCache[msg.imSessionID] = info.noticeData.ItemID;
                }
                else
                {
                    byte[] bucket = new byte[19];
                    bucket[0] = 0; //dunno
                    bucket[1] = 0; //dunno
                    info.GroupID.ToBytes(bucket, 2);
                    bucket[18] = 0; //dunno
                    msg.binaryBucket = bucket;
                }

                GroupSessionIDCache[msg.imSessionID] = info.noticeData.NoticeID;

            }
            else
            {
                if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: Group Notice {0} not found, composing empty message.", groupNoticeID);
                msg.fromAgentID = UUID.Zero.Guid;
                msg.timestamp = (uint)Util.UnixTimeSinceEpoch(); ;
                msg.fromAgentName = string.Empty;
                msg.message = string.Empty;
                msg.binaryBucket = new byte[0];
            }

            return msg;
        }

        public void SendAgentGroupDataUpdate(IClientAPI remoteClient)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            // Send agent information about his groups
            SendAgentGroupDataUpdate(remoteClient, GetRequestingAgentID(remoteClient));
        }

        public void JoinGroupRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            // Should check to see if OpenEnrollment, or if there's an outstanding invitation
            m_groupData.AddAgentToGroup(GetRequestingAgentID(remoteClient), GetRequestingAgentID(remoteClient), groupID, UUID.Zero);

            remoteClient.SendJoinGroupReply(groupID, true);

            // Should this send updates to everyone in the group?
            SendAgentGroupDataUpdate(remoteClient, GetRequestingAgentID(remoteClient));
        }

        public void LeaveGroupRequest(IClientAPI remoteClient, UUID groupID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            if (!m_groupData.RemoveAgentFromGroup(GetRequestingAgentID(remoteClient), GetRequestingAgentID(remoteClient), groupID))
                return;

            remoteClient.SendLeaveGroupReply(groupID, true);

            remoteClient.SendAgentDropGroup(groupID);

            SendAgentGroupDataUpdate(remoteClient, GetRequestingAgentID(remoteClient));

            if (m_groupsMessagingModule != null)
            {
                // SL sends out notifcations to the group messaging session that the person has left
                GridInstantMessage im = new GridInstantMessage();
                im.fromAgentID = groupID.Guid;
                im.dialog = (byte)InstantMessageDialog.SessionSend;
                im.binaryBucket = new byte[0];
                im.fromAgentName = "System";
                im.fromGroup = true;
                im.imSessionID = groupID.Guid;
                im.message = remoteClient.Name + " has left the group.";
                im.offline = 1;
                im.RegionID = remoteClient.Scene.RegionInfo.RegionID.Guid;
                im.timestamp = (uint)Util.UnixTimeSinceEpoch();
                im.toAgentID = Guid.Empty;

                m_groupsMessagingModule.SendMessageToGroup(im, groupID);
            }
        }

        public void EjectGroupMemberRequest(IClientAPI remoteClient, UUID groupID, UUID ejecteeID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            if (!m_groupData.RemoveAgentFromGroup(GetRequestingAgentID(remoteClient), ejecteeID, groupID))
                return;

            remoteClient.SendEjectGroupMemberReply(GetRequestingAgentID(remoteClient), groupID, true);

            GroupRecord groupInfo = m_groupData.GetGroupRecord(GetRequestingAgentID(remoteClient), groupID, null);

            UserAccount account = m_sceneList[0].UserAccountService.GetUserAccount(remoteClient.Scene.RegionInfo.ScopeID, ejecteeID);
            
            if ((groupInfo == null) || (account == null))
            {
                return;
            }

            // Send Message to Ejectee
            GridInstantMessage msg = new GridInstantMessage();
            
            msg.imSessionID = UUID.Zero.Guid;
            msg.fromAgentID = GetRequestingAgentID(remoteClient).Guid;
            msg.toAgentID = ejecteeID.Guid;
            msg.timestamp = 0;
            msg.fromAgentName = remoteClient.Name;
            msg.message = string.Format("You have been ejected from '{1}' by {0}.", remoteClient.Name, groupInfo.GroupName);
            msg.dialog = (byte)210;
            msg.fromGroup = false;
            msg.offline = (byte)0;
            msg.ParentEstateID = 0;
            msg.Position = Vector3.Zero;
            msg.RegionID = remoteClient.Scene.RegionInfo.RegionID.Guid;
            msg.binaryBucket = new byte[0];
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

            msg = new GridInstantMessage();
            msg.imSessionID = UUID.Zero.Guid;
            msg.fromAgentID = GetRequestingAgentID(remoteClient).Guid;
            msg.toAgentID = GetRequestingAgentID(remoteClient).Guid;
            msg.timestamp = 0;
            msg.fromAgentName = remoteClient.Name;
            if (account != null)
            {
                msg.message = string.Format("{2} has been ejected from '{1}' by {0}.", remoteClient.Name, groupInfo.GroupName, account.FirstName + " " + account.LastName);
            }
            else
            {
                msg.message = string.Format("{2} has been ejected from '{1}' by {0}.", remoteClient.Name, groupInfo.GroupName, "Unknown member");
            }
            msg.dialog = (byte)OpenMetaverse.InstantMessageDialog.MessageFromAgent;
            msg.fromGroup = false;
            msg.offline = (byte)0;
            msg.ParentEstateID = 0;
            msg.Position = Vector3.Zero;
            msg.RegionID = remoteClient.Scene.RegionInfo.RegionID.Guid;
            msg.binaryBucket = new byte[0];
            OutgoingInstantMessage(msg, GetRequestingAgentID(remoteClient));

            UpdateAllClientsWithGroupInfo(ejecteeID);

            if (m_groupsMessagingModule != null)
            {
                // SL sends out notifcations to the group messaging session that the person has left
                GridInstantMessage im = new GridInstantMessage();
                im.fromAgentID = groupID.Guid;
                im.dialog = (byte)InstantMessageDialog.SessionSend;
                im.binaryBucket = new byte[0];
                im.fromAgentName = "System";
                im.fromGroup = true;
                im.imSessionID = groupID.Guid;
                im.message = remoteClient.Name + " has left the group.";
                im.offline = 1;
                im.RegionID = remoteClient.Scene.RegionInfo.RegionID.Guid;
                im.timestamp = (uint)Util.UnixTimeSinceEpoch();
                im.toAgentID = Guid.Empty;

                m_groupsMessagingModule.SendMessageToGroup(im, groupID);
            }
        }

        public void InviteGroupRequest(IClientAPI remoteClient, UUID groupID, UUID invitedAgentID, UUID roleID)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            UUID InviteID = UUID.Random();

            m_groupData.AddAgentToGroupInvite(GetRequestingAgentID(remoteClient), InviteID, groupID, roleID, invitedAgentID, remoteClient.Name);

            // Check to see if the invite went through, if it did not then it's possible
            // the remoteClient did not validate or did not have permission to invite.
            GroupInviteInfo inviteInfo = m_groupData.GetAgentToGroupInvite(invitedAgentID, InviteID);

            if (inviteInfo != null)
            {
                if (m_msgTransferModule != null)
                {
                    Guid inviteUUID = InviteID.Guid;

                    GridInstantMessage msg = new GridInstantMessage();

                    msg.imSessionID = inviteUUID;

                    // msg.fromAgentID = GetRequestingAgentID(remoteClient).Guid;
                    msg.fromAgentID = groupID.Guid;
                    msg.toAgentID = invitedAgentID.Guid;
                    //msg.timestamp = (uint)Util.UnixTimeSinceEpoch();
                    msg.timestamp = 0;
                    msg.fromAgentName = remoteClient.Name;
                    GroupRecord groupInfo = GetGroupRecord(groupID);
                    string MemberShipCost = ". There is no cost to join this group.";
                    if(groupInfo.MembershipFee != 0)
                    {
                    	MemberShipCost = ". To join, you must pay " +groupInfo.MembershipFee.ToString()+".";
                    }
                    msg.message = string.Format("{0} has invited you to join " + groupInfo.GroupName + MemberShipCost, remoteClient.Name);
                    msg.dialog = (byte)OpenMetaverse.InstantMessageDialog.GroupInvitation;
                    msg.fromGroup = true;
                    msg.offline = (byte)0;
                    msg.ParentEstateID = 0;
                    msg.Position = Vector3.Zero;
                    msg.RegionID = remoteClient.Scene.RegionInfo.RegionID.Guid;
                    msg.binaryBucket = new byte[20];

                    OutgoingInstantMessage(msg, invitedAgentID);
                }
            }
        }

        #endregion

        #region Client/Update Tools

        /// <summary>
        /// Try to find an active IClientAPI reference for agentID giving preference to root connections
        /// </summary>
        private IClientAPI GetActiveClient(UUID agentID)
        {
            IClientAPI child = null;

            // Try root avatar first
            foreach (Scene scene in m_sceneList)
            {
                if (scene.Entities.ContainsKey(agentID) &&
                        scene.Entities[agentID] is ScenePresence)
                {
                    ScenePresence user = (ScenePresence)scene.Entities[agentID];
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
        /// Send 'remoteClient' the group membership 'data' for agent 'dataForAgentID'.
        /// </summary>
        private void SendGroupMembershipInfoViaCaps(IClientAPI remoteClient, UUID dataForAgentID, GroupMembershipData[] data)
        {
            if (m_debugEnabled) m_log.InfoFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            OSDArray AgentData = new OSDArray(1);
            OSDMap AgentDataMap = new OSDMap(1);
            AgentDataMap.Add("AgentID", OSD.FromUUID(dataForAgentID));
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

            OSDMap llDataStruct = new OSDMap(3);
            llDataStruct.Add("AgentData", AgentData);
            llDataStruct.Add("GroupData", GroupData);
            llDataStruct.Add("NewGroupData", NewGroupData);

            if (m_debugEnabled)
            {
                m_log.InfoFormat("[GROUPS]: {0}", OSDParser.SerializeJsonString(llDataStruct));
            }

            IEventQueue queue = remoteClient.Scene.RequestModuleInterface<IEventQueue>();

            if (queue != null)
            {
                queue.Enqueue(EventQueueHelper.buildEvent("AgentGroupDataUpdate", llDataStruct), GetRequestingAgentID(remoteClient));
            }
            
        }

        private void SendScenePresenceUpdate(UUID AgentID, string Title)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: Updating scene title for {0} with title: {1}", AgentID, Title);

            ScenePresence presence = null;

            foreach (Scene scene in m_sceneList)
            {
                presence = scene.GetScenePresence(AgentID);
                if (presence != null)
                {
                    presence.Grouptitle = Title;

                    presence.SendFullUpdateToAllClients();
                }
            }
        }

        /// <summary>
        /// Send updates to all clients who might be interested in groups data for dataForClientID
        /// </summary>
        private void UpdateAllClientsWithGroupInfo(UUID dataForClientID)
        {
            if (m_debugEnabled) m_log.InfoFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            // TODO: Probably isn't nessesary to update every client in every scene.
            // Need to examine client updates and do only what's nessesary.

            lock (m_sceneList)
            {
                foreach (Scene scene in m_sceneList)
                {
                    scene.ForEachClient(delegate(IClientAPI client) { SendAgentGroupDataUpdate(client, dataForClientID); });
                }
            }
        }

        /// <summary>
        /// Update remoteClient with group information about dataForAgentID
        /// </summary>
        private void SendAgentGroupDataUpdate(IClientAPI remoteClient, UUID dataForAgentID)
        {
            m_log.InfoFormat("[GROUPS]: {0} called for {1}", System.Reflection.MethodBase.GetCurrentMethod().Name, remoteClient.Name);

            // TODO: All the client update functions need to be reexamined because most do too much and send too much stuff

            OnAgentDataUpdateRequest(remoteClient, dataForAgentID, UUID.Zero);


            // Need to send a group membership update to the client
            // UDP version doesn't seem to behave nicely.  But we're going to send it out here
            // with an empty group membership to hopefully remove groups being displayed due
            // to the core Groups Stub
            remoteClient.SendGroupMembership(new GroupMembershipData[0]);

            GroupMembershipData[] membershipArray = GetProfileListedGroupMemberships(remoteClient, dataForAgentID);
            SendGroupMembershipInfoViaCaps(remoteClient, dataForAgentID, membershipArray);
            remoteClient.SendAvatarGroupsReply(dataForAgentID, membershipArray);

        }

        /// <summary>
        /// Get a list of groups memberships for the agent that are marked "ListInProfile"
        /// </summary>
        /// <param name="dataForAgentID"></param>
        /// <returns></returns>
        private GroupMembershipData[] GetProfileListedGroupMemberships(IClientAPI requestingClient, UUID dataForAgentID)
        {
            List<GroupMembershipData> membershipData = m_groupData.GetAgentGroupMemberships(requestingClient.AgentId, dataForAgentID);
            GroupMembershipData[] membershipArray;

            if (requestingClient.AgentId != dataForAgentID)
            {
                Predicate<GroupMembershipData> showInProfile = delegate(GroupMembershipData membership)
                {
                    return membership.ListInProfile;
                };

                membershipArray = membershipData.FindAll(showInProfile).ToArray();
            }
            else
            {
                membershipArray = membershipData.ToArray();
            }

            if (m_debugEnabled)
            {
                m_log.InfoFormat("[GROUPS]: Get group membership information for {0} requested by {1}", dataForAgentID, requestingClient.AgentId);
                foreach (GroupMembershipData membership in membershipArray)
                {
                    m_log.InfoFormat("[GROUPS]: {0} :: {1} - {2} - {3}", dataForAgentID, membership.GroupName, membership.GroupTitle, membership.GroupPowers);
                }
            }

            return membershipArray;
        }

        private void SendAgentDataUpdate(IClientAPI remoteClient, UUID dataForAgentID, UUID activeGroupID, string activeGroupName, ulong activeGroupPowers, string activeGroupTitle)
        {
            if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            // TODO: All the client update functions need to be reexamined because most do too much and send too much stuff
            
            remoteClient.SendAgentDataUpdate(dataForAgentID, activeGroupID, remoteClient.FirstName,
                    remoteClient.LastName, activeGroupPowers, activeGroupName,
                    activeGroupTitle);
        }

        #endregion

        #region IM Backed Processes

        private void OutgoingInstantMessage(GridInstantMessage msg, UUID msgTo)
        {
            if (m_debugEnabled) m_log.InfoFormat("[GROUPS]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            IClientAPI localClient = GetActiveClient(msgTo);
            if (localClient != null)
            {
                if (m_debugEnabled) m_log.InfoFormat("[GROUPS]: MsgTo ({0}) is local, delivering directly", localClient.Name);
                localClient.SendInstantMessage(msg);
            }
            else
            {
                if (m_debugEnabled) m_log.InfoFormat("[GROUPS]: MsgTo ({0}) is not local, delivering via TransferModule", msgTo);
                m_msgTransferModule.SendInstantMessage(msg, delegate(bool success) { if (m_debugEnabled) m_log.DebugFormat("[GROUPS]: Message Sent: {0}", success?"Succeeded":"Failed"); });
            }
        }

        public void NotifyChange(UUID groupID)
        {
            // Notify all group members of a chnge in group roles and/or
            // permissions
            //
        }

        #endregion

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
        /// WARNING: This is not the only place permissions are checked! They are checked in each of the connectors as well!
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="GroupID"></param>
        /// <returns></returns>
        public bool GroupPermissionCheck(UUID AgentID, UUID GroupID, GroupPowers permissions)
        {
            if (GroupID == UUID.Zero)
                return false;

            if (AgentID == UUID.Zero)
                return false;

            GroupMembershipData GMD = m_groupData.GetAgentGroupMembership(AgentID, GroupID, AgentID);
            if (GMD == null)
                return false;
            else if (permissions == GroupPowers.None)
                return true;

            if ((((GroupPowers)GMD.GroupPowers) & permissions) != permissions)
                return false;

            return true;
        }

        #endregion

        public GridInstantMessage BuildOfflineGroupNotice(GridInstantMessage msg)
        {
            msg.dialog = 211; //We set this so that it isn't taken the wrong way later
            Guid NoticeID = GroupSessionIDCache[msg.imSessionID].Guid;
            GroupSessionIDCache.Remove(msg.imSessionID);
            msg.imSessionID = NoticeID;
            return msg;
        }
    }
}
