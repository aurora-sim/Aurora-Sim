using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.DatabaseInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Messages.Linden;
using OpenMetaverse.StructuredData;
using System.Collections.Generic;
using System.Linq;
using ChatSessionMember = Aurora.Framework.DatabaseInterfaces.ChatSessionMember;

namespace Aurora.Services
{
    public class InstantMessagingService : ConnectorBase, IService, IInstantMessagingService
    {
        #region Declares

        private IEventQueueService m_eventQueueService;
        private IGroupsServiceConnector m_groupData;
        private readonly Dictionary<UUID, ChatSession> ChatSessions = new Dictionary<UUID, ChatSession>();

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            registry.RegisterModuleInterface<IInstantMessagingService>(this);

            Init(registry, "InstantMessagingService", "", "/im/", "InstantMessageServerURI");
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
            m_eventQueueService = m_registry.RequestModuleInterface<IEventQueueService>();
            ISyncMessageRecievedService syncRecievedService =
                m_registry.RequestModuleInterface<ISyncMessageRecievedService>();
            if (syncRecievedService != null)
                syncRecievedService.OnMessageReceived += syncRecievedService_OnMessageReceived;
            m_groupData = Framework.Utilities.DataManager.RequestPlugin<IGroupsServiceConnector>();
            m_registry.RequestModuleInterface<ISimulationBase>().EventManager.RegisterEventHandler("UserStatusChange",
                                                                                                   OnGenericEvent);
        }

        #endregion

        #region Region-side message sending

        private OSDMap syncRecievedService_OnMessageReceived(OSDMap message)
        {
            string method = message["Method"];
            if (method == "SendInstantMessages")
            {
                List<GridInstantMessage> messages =
                    ((OSDArray) message["Messages"]).ConvertAll<GridInstantMessage>((o) =>
                                                                                        {
                                                                                            GridInstantMessage im =
                                                                                                new GridInstantMessage();
                                                                                            im.FromOSD((OSDMap) o);
                                                                                            return im;
                                                                                        });
                ISceneManager manager = m_registry.RequestModuleInterface<ISceneManager>();
                if (manager != null)
                {
                    IMessageTransferModule messageTransfer =
                        manager.Scene.RequestModuleInterface<IMessageTransferModule>();
                    if (messageTransfer != null)
                    {
                        foreach (GridInstantMessage im in messages)
                            messageTransfer.SendInstantMessage(im);
                    }
                }
            }
            return null;
        }

        #endregion

        #region User Status Change

        protected object OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "UserStatusChange")
            {
                //A user has logged in or out... we need to update friends lists across the grid

                object[] info = (object[]) parameters;
                UUID us = UUID.Parse(info[0].ToString());
                bool isOnline = bool.Parse(info[1].ToString());

                if (!isOnline)
                {
                    //If they are going offline, actually remove from from all group chats so that the next time they log in, they will be readded
                    foreach (GroupMembershipData gmd in m_groupData.GetAgentGroupMemberships(us, us))
                    {
                        ChatSessionMember member = FindMember(gmd.GroupID, us);
                        if (member != null)
                        {
                            member.HasBeenAdded = false;
                            member.RequestedRemoval = false;
                        }
                    }
                }
            }

            return null;
        }

        #endregion

        #region IInstantMessagingService Members

        public string ChatSessionRequest(IRegionClientCapsService caps, OSDMap req)
        {
            string method = req["method"].AsString();

            UUID sessionid = UUID.Parse(req["session-id"].AsString());

            switch (method)
            {
                case "start conference":
                    {
                        if (SessionExists(sessionid))
                            return ""; //No duplicate sessions
                        //Create the session.
                        CreateSession(new ChatSession
                                          {
                                              Members = new List<ChatSessionMember>(),
                                              SessionID = sessionid,
                                              Name = caps.ClientCaps.AccountInfo.Name + " Conference"
                                          });

                        OSDArray parameters = (OSDArray) req["params"];
                        //Add other invited members.
                        foreach (OSD param in parameters)
                        {
                            AddDefaultPermsMemberToSession(param.AsUUID(), sessionid);
                        }

                        //Add us to the session!
                        AddMemberToGroup(new ChatSessionMember
                                             {
                                                 AvatarKey = caps.AgentID,
                                                 CanVoiceChat = true,
                                                 IsModerator = true,
                                                 MuteText = false,
                                                 MuteVoice = false,
                                                 HasBeenAdded = true,
                                                 RequestedRemoval = false
                                             }, sessionid);


                        //Inform us about our room
                        ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock block =
                            new ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock
                                {
                                    AgentID = caps.AgentID,
                                    CanVoiceChat = true,
                                    IsModerator = true,
                                    MuteText = false,
                                    MuteVoice = false,
                                    Transition = "ENTER"
                                };
                        m_eventQueueService.ChatterBoxSessionAgentListUpdates(sessionid, new[] {block}, caps.AgentID,
                                                                              "ENTER",
                                                                              caps.RegionID);

                        ChatterBoxSessionStartReplyMessage cs = new ChatterBoxSessionStartReplyMessage
                                                                    {
                                                                        VoiceEnabled = true,
                                                                        TempSessionID = sessionid,
                                                                        Type = 1,
                                                                        Success = true,
                                                                        SessionID = sessionid,
                                                                        SessionName =
                                                                            caps.ClientCaps.AccountInfo.Name +
                                                                            " Conference",
                                                                        ModeratedVoice = true
                                                                    };
                        return OSDParser.SerializeLLSDXmlString(cs.Serialize());
                    }
                case "accept invitation":
                    {
                        //They would like added to the group conversation
                        List<ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock> Us =
                            new List<ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock>();
                        List<ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock> NotUsAgents =
                            new List<ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock>();

                        ChatSession session = GetSession(sessionid);
                        if (session != null)
                        {
                            ChatSessionMember thismember = FindMember(sessionid, caps.AgentID);
                            //Tell all the other members about the incoming member
                            foreach (ChatSessionMember sessionMember in session.Members)
                            {
                                ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock block =
                                    new ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock
                                        {
                                            AgentID = sessionMember.AvatarKey,
                                            CanVoiceChat = sessionMember.CanVoiceChat,
                                            IsModerator = sessionMember.IsModerator,
                                            MuteText = sessionMember.MuteText,
                                            MuteVoice = sessionMember.MuteVoice,
                                            Transition = "ENTER"
                                        };
                                if (sessionMember.AvatarKey == thismember.AvatarKey)
                                {
                                    Us.Add(block);
                                    NotUsAgents.Add(block);
                                }
                                else
                                {
                                    if (sessionMember.HasBeenAdded)
                                        // Don't add not joined yet agents. They don't want to be here.
                                        NotUsAgents.Add(block);
                                }
                            }
                            thismember.HasBeenAdded = true;
                            foreach (ChatSessionMember member in session.Members)
                            {
                                if (member.HasBeenAdded) //Only send to those in the group
                                {
                                    UUID regionID = FindRegionID(member.AvatarKey);
                                    if (regionID != UUID.Zero)
                                    {
                                        m_eventQueueService.ChatterBoxSessionAgentListUpdates(session.SessionID,
                                                                                              member.AvatarKey ==
                                                                                              thismember.AvatarKey
                                                                                                  ? NotUsAgents.ToArray()
                                                                                                  : Us.ToArray(),
                                                                                              member.AvatarKey, "ENTER",
                                                                                              regionID);
                                    }
                                }
                            }
                            return "Accepted";
                        }
                        else
                            return ""; //no session exists?
                    }
                case "mute update":
                    {
                        //Check if the user is a moderator
                        if (!CheckModeratorPermission(caps.AgentID, sessionid))
                            return "";

                        OSDMap parameters = (OSDMap) req["params"];
                        UUID AgentID = parameters["agent_id"].AsUUID();
                        OSDMap muteInfoMap = (OSDMap) parameters["mute_info"];

                        ChatSessionMember thismember = FindMember(sessionid, AgentID);
                        if (muteInfoMap.ContainsKey("text"))
                            thismember.MuteText = muteInfoMap["text"].AsBoolean();
                        if (muteInfoMap.ContainsKey("voice"))
                            thismember.MuteVoice = muteInfoMap["voice"].AsBoolean();

                        ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock block =
                            new ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock
                                {
                                    AgentID = thismember.AvatarKey,
                                    CanVoiceChat = thismember.CanVoiceChat,
                                    IsModerator = thismember.IsModerator,
                                    MuteText = thismember.MuteText,
                                    MuteVoice = thismember.MuteVoice,
                                    Transition = "ENTER"
                                };

                        ChatSession session = GetSession(sessionid);
                        // Send an update to all users so that they show the correct permissions
                        foreach (ChatSessionMember member in session.Members)
                        {
                            if (member.HasBeenAdded) //Only send to those in the group
                            {
                                UUID regionID = FindRegionID(member.AvatarKey);
                                if (regionID != UUID.Zero)
                                {
                                    m_eventQueueService.ChatterBoxSessionAgentListUpdates(sessionid, new[] {block},
                                                                                          member.AvatarKey, "",
                                                                                          regionID);
                                }
                            }
                        }

                        return "Accepted";
                    }
                case "call":
                    {
                        //Implement voice chat for conferences...

                        IVoiceService voiceService = m_registry.RequestModuleInterface<IVoiceService>();
                        if (voiceService == null)
                            return "";

                        OSDMap resp = voiceService.GroupConferenceCallRequest(caps, sessionid);
                        return OSDParser.SerializeLLSDXmlString(resp);
                    }
                default:
                    MainConsole.Instance.Warn("ChatSessionRequest : " + method);
                    return "";
            }
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public void EnsureSessionIsStarted(UUID groupID)
        {
            if (m_doRemoteOnly)
            {
                DoRemoteCallPost(true, "InstantMessageServerURI", groupID);
                return;
            }

            if (!SessionExists(groupID))
            {
                GroupRecord groupInfo = m_groupData.GetGroupRecord(UUID.Zero, groupID, null);

                CreateSession(new ChatSession
                                  {
                                      Members = new List<ChatSessionMember>(),
                                      SessionID = groupID,
                                      Name = groupInfo.GroupName
                                  });

                foreach (
                    GroupMembersData gmd in
                        m_groupData.GetGroupMembers(UUID.Zero, groupID)
                                   .Where(
                                       gmd =>
                                       (gmd.AgentPowers & (ulong) GroupPowers.JoinChat) == (ulong) GroupPowers.JoinChat)
                    )
                {
                    AddMemberToGroup(new ChatSessionMember
                                         {
                                             AvatarKey = gmd.AgentID,
                                             CanVoiceChat = false,
                                             IsModerator =
                                                 GroupPermissionCheck(gmd.AgentID, groupID, GroupPowers.ModerateChat),
                                             MuteText = false,
                                             MuteVoice = false,
                                             HasBeenAdded = false
                                         }, groupID);
                }
            }
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public void CreateGroupChat(UUID AgentID, GridInstantMessage im)
        {
            if (m_doRemoteOnly)
            {
                DoRemoteCallPost(true, "InstantMessageServerURI", AgentID, im);
                return;
            }

            UUID GroupID = im.SessionID;

            GroupRecord groupInfo = m_groupData.GetGroupRecord(AgentID, GroupID, null);

            if (groupInfo != null)
            {
                if (!GroupPermissionCheck(AgentID, GroupID, GroupPowers.JoinChat))
                    return; //They have to be able to join to create a group chat
                //Create the session.
                if (!SessionExists(GroupID))
                {
                    CreateSession(new ChatSession
                                      {
                                          Members = new List<ChatSessionMember>(),
                                          SessionID = GroupID,
                                          Name = groupInfo.GroupName
                                      });
                    AddMemberToGroup(new ChatSessionMember
                                         {
                                             AvatarKey = AgentID,
                                             CanVoiceChat = false,
                                             IsModerator =
                                                 GroupPermissionCheck(AgentID, GroupID, GroupPowers.ModerateChat),
                                             MuteText = false,
                                             MuteVoice = false,
                                             HasBeenAdded = true
                                         }, GroupID);

                    foreach (
                        GroupMembersData gmd in
                            m_groupData.GetGroupMembers(AgentID, GroupID)
                                       .Where(gmd => gmd.AgentID != AgentID)
                                       .Where(
                                           gmd =>
                                           (gmd.AgentPowers & (ulong) GroupPowers.JoinChat) ==
                                           (ulong) GroupPowers.JoinChat))
                    {
                        AddMemberToGroup(new ChatSessionMember
                                             {
                                                 AvatarKey = gmd.AgentID,
                                                 CanVoiceChat = false,
                                                 IsModerator =
                                                     GroupPermissionCheck(gmd.AgentID, GroupID, GroupPowers.ModerateChat),
                                                 MuteText = false,
                                                 MuteVoice = false,
                                                 HasBeenAdded = false
                                             }, GroupID);
                    }
                    //Tell us that it was made successfully
                    m_eventQueueService.ChatterBoxSessionStartReply(groupInfo.GroupName, GroupID,
                                                                    AgentID, FindRegionID(AgentID));
                }
                else
                {
                    ChatSession thisSession = GetSession(GroupID);
                    //A session already exists
                    //Add us
                    AddMemberToGroup(new ChatSessionMember
                                         {
                                             AvatarKey = AgentID,
                                             CanVoiceChat = false,
                                             IsModerator =
                                                 GroupPermissionCheck(AgentID, GroupID, GroupPowers.ModerateChat),
                                             MuteText = false,
                                             MuteVoice = false,
                                             HasBeenAdded = true
                                         }, GroupID);

                    //Tell us that we entered successfully
                    m_eventQueueService.ChatterBoxSessionStartReply(groupInfo.GroupName, GroupID,
                                                                    AgentID, FindRegionID(AgentID));
                    List<ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock> Us =
                        new List<ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock>();
                    List<ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock> NotUsAgents =
                        new List<ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock>();

                    foreach (ChatSessionMember sessionMember in thisSession.Members)
                    {
                        ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock block =
                            new ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock
                                {
                                    AgentID = sessionMember.AvatarKey,
                                    CanVoiceChat = sessionMember.CanVoiceChat,
                                    IsModerator = sessionMember.IsModerator,
                                    MuteText = sessionMember.MuteText,
                                    MuteVoice = sessionMember.MuteVoice,
                                    Transition = "ENTER"
                                };
                        if (AgentID == sessionMember.AvatarKey)
                            Us.Add(block);
                        if (sessionMember.HasBeenAdded)
                            // Don't add not joined yet agents. They don't want to be here.
                            NotUsAgents.Add(block);
                    }
                    foreach (ChatSessionMember member in thisSession.Members)
                    {
                        if (member.HasBeenAdded) //Only send to those in the group
                        {
                            UUID regionID = FindRegionID(member.AvatarKey);
                            if (regionID != UUID.Zero)
                            {
                                if (member.AvatarKey == AgentID)
                                {
                                    //Tell 'us' about all the other agents in the group
                                    m_eventQueueService.ChatterBoxSessionAgentListUpdates(GroupID, NotUsAgents.ToArray(),
                                                                                          member.AvatarKey,
                                                                                          "ENTER", regionID);
                                }
                                else
                                {
                                    //Tell 'other' agents about the new agent ('us')
                                    m_eventQueueService.ChatterBoxSessionAgentListUpdates(GroupID, Us.ToArray(),
                                                                                          member.AvatarKey,
                                                                                          "ENTER", regionID);
                                }
                            }
                        }
                    }
                }

                ChatSessionMember agentMember = FindMember(GroupID, AgentID);

                //Tell us that we entered
                ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock ourblock =
                    new ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock
                        {
                            AgentID = AgentID,
                            CanVoiceChat = agentMember.CanVoiceChat,
                            IsModerator = agentMember.IsModerator,
                            MuteText = agentMember.MuteText,
                            MuteVoice = agentMember.MuteVoice,
                            Transition = "ENTER"
                        };
                m_eventQueueService.ChatterBoxSessionAgentListUpdates(GroupID, new[] {ourblock}, AgentID, "ENTER",
                                                                      FindRegionID(AgentID));
            }
        }

        /// <summary>
        ///     Remove the member from this session
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="im"></param>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public void DropMemberFromSession(UUID agentID, GridInstantMessage im)
        {
            if (m_doRemoteOnly)
            {
                DoRemoteCallPost(true, "InstantMessageServerURI", agentID, im);
                return;
            }

            ChatSession session;
            ChatSessions.TryGetValue(im.SessionID, out session);
            if (session == null)
                return;
            ChatSessionMember member = null;
            foreach (
                ChatSessionMember testmember in
                    session.Members.Where(testmember => testmember.AvatarKey == im.FromAgentID))
                member = testmember;

            if (member == null)
                return;

            member.HasBeenAdded = false;
            member.RequestedRemoval = true;

            if (session.Members.Count(mem => mem.HasBeenAdded) == 0) //If a member hasn't been added, kill this anyway
            {
                ChatSessions.Remove(session.SessionID);
                return;
            }

            ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock block =
                new ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock
                    {
                        AgentID = member.AvatarKey,
                        CanVoiceChat = member.CanVoiceChat,
                        IsModerator = member.IsModerator,
                        MuteText = member.MuteText,
                        MuteVoice = member.MuteVoice,
                        Transition = "LEAVE"
                    };
            foreach (ChatSessionMember sessionMember in session.Members)
            {
                if (sessionMember.HasBeenAdded) //Only send to those in the group
                {
                    UUID regionID = FindRegionID(sessionMember.AvatarKey);
                    if (regionID != UUID.Zero)
                    {
                        m_eventQueueService.ChatterBoxSessionAgentListUpdates(session.SessionID, new[] {block},
                                                                              sessionMember.AvatarKey, "LEAVE",
                                                                              regionID);
                    }
                }
            }
        }

        /// <summary>
        ///     Send chat to all the members of this friend conference
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="im"></param>
        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public void SendChatToSession(UUID agentID, GridInstantMessage im)
        {
            if (m_doRemoteOnly)
            {
                DoRemoteCallPost(true, "InstantMessageServerURI", agentID, im);
                return;
            }

            Util.FireAndForget((o) =>
                                   {
                                       ChatSession session;
                                       ChatSessions.TryGetValue(im.SessionID, out session);
                                       if (session == null)
                                           return;

                                       if (agentID != UUID.Zero) //Not system
                                       {
                                           ChatSessionMember sender = FindMember(im.SessionID, agentID);
                                           if (sender.MuteText)
                                               return; //They have been admin muted, don't allow them to send anything
                                       }

                                       Dictionary<string, List<GridInstantMessage>> messagesToSend =
                                           new Dictionary<string, List<GridInstantMessage>>();
                                       foreach (ChatSessionMember member in session.Members)
                                       {
                                           if (member.HasBeenAdded)
                                           {
                                               im.ToAgentID = member.AvatarKey;
                                               im.BinaryBucket = Utils.StringToBytes(session.Name);
                                               im.RegionID = UUID.Zero;
                                               im.ParentEstateID = 0;
                                               im.Offline = 0;
                                               GridInstantMessage message = new GridInstantMessage();
                                               message.FromOSD(im.ToOSD());
                                               //im.timestamp = 0;
                                               string uri = FindRegionURI(member.AvatarKey);
                                               if (uri != "") //Check if they are online
                                               {
                                                   //Bulk send all of the instant messages to the same region, so that we don't send them one-by-one over and over
                                                   if (messagesToSend.ContainsKey(uri))
                                                       messagesToSend[uri].Add(message);
                                                   else
                                                       messagesToSend.Add(uri, new List<GridInstantMessage>() {message});
                                               }
                                           }
                                           else if (!member.RequestedRemoval)
                                               //If they're requested to leave, don't recontact them
                                           {
                                               UUID regionID = FindRegionID(member.AvatarKey);
                                               if (regionID != UUID.Zero)
                                               {
                                                   im.ToAgentID = member.AvatarKey;
                                                   m_eventQueueService.ChatterboxInvitation(
                                                       session.SessionID
                                                       , session.Name
                                                       , im.FromAgentID
                                                       , im.Message
                                                       , im.ToAgentID
                                                       , im.FromAgentName
                                                       , im.Dialog
                                                       , im.Timestamp
                                                       , im.Offline == 1
                                                       , (int) im.ParentEstateID
                                                       , im.Position
                                                       , 1
                                                       , im.SessionID
                                                       , false
                                                       , Utils.StringToBytes(session.Name)
                                                       , regionID
                                                       );
                                               }
                                           }
                                       }
                                       foreach (KeyValuePair<string, List<GridInstantMessage>> kvp in messagesToSend)
                                       {
                                           SendInstantMessages(kvp.Key, kvp.Value);
                                       }
                                   });
        }

        #endregion

        #region Session Caching

        private bool SessionExists(UUID GroupID)
        {
            return ChatSessions.ContainsKey(GroupID);
        }

        private bool GroupPermissionCheck(UUID AgentID, UUID GroupID, GroupPowers groupPowers)
        {
            GroupMembershipData GMD = m_groupData.GetGroupMembershipData(AgentID, GroupID, AgentID);
            if (GMD == null) return false;
            return (GMD.GroupPowers & (ulong) groupPowers) == (ulong) groupPowers;
        }

        private void SendInstantMessages(string uri, List<GridInstantMessage> ims)
        {
            ISyncMessagePosterService syncMessagePoster = m_registry.RequestModuleInterface<ISyncMessagePosterService>();
            if (syncMessagePoster != null)
            {
                OSDMap map = new OSDMap();
                map["Method"] = "SendInstantMessages";
                map["Messages"] = ims.ToOSDArray();
                syncMessagePoster.Post(uri, map);
            }
        }

        private UUID FindRegionID(UUID agentID)
        {
            IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
            UserInfo user = agentInfoService.GetUserInfo(agentID.ToString());
            return (user != null && user.IsOnline) ? user.CurrentRegionID : UUID.Zero;
        }

        private string FindRegionURI(UUID agentID)
        {
            IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
            UserInfo user = agentInfoService.GetUserInfo(agentID.ToString());
            return (user != null && user.IsOnline) ? user.CurrentRegionURI : "";
        }

        /// <summary>
        ///     Find the member from X sessionID
        /// </summary>
        /// <param name="sessionid"></param>
        /// <param name="Agent"></param>
        /// <returns></returns>
        private ChatSessionMember FindMember(UUID sessionid, UUID Agent)
        {
            ChatSession session;
            ChatSessions.TryGetValue(sessionid, out session);
            if (session == null)
                return null;
            ChatSessionMember thismember = new ChatSessionMember {AvatarKey = UUID.Zero};
            foreach (ChatSessionMember testmember in session.Members.Where(testmember => testmember.AvatarKey == Agent))
            {
                thismember = testmember;
            }
            return thismember;
        }

        /// <summary>
        ///     Check whether the user has moderator permissions
        /// </summary>
        /// <param name="Agent"></param>
        /// <param name="sessionid"></param>
        /// <returns></returns>
        private bool CheckModeratorPermission(UUID Agent, UUID sessionid)
        {
            ChatSession session;
            ChatSessions.TryGetValue(sessionid, out session);
            if (session == null)
                return false;
            ChatSessionMember thismember = new ChatSessionMember {AvatarKey = UUID.Zero};
            foreach (ChatSessionMember testmember in session.Members.Where(testmember => testmember.AvatarKey == Agent))
            {
                thismember = testmember;
            }
            if (thismember == null)
                return false;
            return thismember.IsModerator;
        }

        /// <summary>
        ///     Add this member to the friend conference
        /// </summary>
        /// <param name="member"></param>
        /// <param name="SessionID"></param>
        private void AddMemberToGroup(ChatSessionMember member, UUID SessionID)
        {
            ChatSession session;
            ChatSessions.TryGetValue(SessionID, out session);
            ChatSessionMember oldMember;
            if ((oldMember = session.Members.Find(mem => mem.AvatarKey == member.AvatarKey)) != null)
            {
                oldMember.HasBeenAdded = true;
                oldMember.RequestedRemoval = false;
            }
            else
                session.Members.Add(member);
        }

        /// <summary>
        ///     Create a new friend conference session
        /// </summary>
        /// <param name="session"></param>
        private void CreateSession(ChatSession session)
        {
            ChatSessions.Add(session.SessionID, session);
        }

        /// <summary>
        ///     Get a session by a user's sessionID
        /// </summary>
        /// <param name="SessionID"></param>
        /// <returns></returns>
        private ChatSession GetSession(UUID SessionID)
        {
            ChatSession session;
            ChatSessions.TryGetValue(SessionID, out session);
            return session;
        }

        /// <summary>
        ///     Add the agent to the in-memory session lists and give them the default permissions
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="SessionID"></param>
        private void AddDefaultPermsMemberToSession(UUID AgentID, UUID SessionID)
        {
            ChatSession session;
            ChatSessions.TryGetValue(SessionID, out session);
            ChatSessionMember member = new ChatSessionMember
                                           {
                                               AvatarKey = AgentID,
                                               CanVoiceChat = true,
                                               IsModerator = false,
                                               MuteText = false,
                                               MuteVoice = false,
                                               HasBeenAdded = false
                                           };
            session.Members.Add(member);
        }

        #endregion
    }
}