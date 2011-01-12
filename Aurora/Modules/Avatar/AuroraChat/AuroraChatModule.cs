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
using System.Reflection;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Messages;
using OpenSim.Framework;
using OpenSim.Framework.Client;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using log4net;
using Aurora.DataManager;
using Aurora.Framework;
using System.Collections;
using OpenMetaverse.StructuredData;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules
{
    public class AuroraChatModule : ISharedRegionModule, IChatModule, IMuteListModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const int DEBUG_CHANNEL = 2147483647;
        private const int DEFAULT_CHANNEL = 0;

        private bool m_enabled = true;
        private bool m_useMuteListModule = true;
        private int m_saydistance = 30;
        private int m_shoutdistance = 100;
        private int m_whisperdistance = 10;
        private float m_maxChatDistance = 100;

        public int SayDistance
        {
            get { return m_saydistance; }
            set { m_saydistance = value; }
        }

        public int ShoutDistance
        {
            get { return m_shoutdistance; }
            set { m_shoutdistance = value; }
        }

        public int WhisperDistance
        {
            get { return m_whisperdistance; }
            set { m_whisperdistance = value; }
        }

        public float MaxChatDistance
        {
            get { return m_maxChatDistance; }
            set { m_maxChatDistance = value; }
        }

        public List<Scene> Scenes
        {
            get { return m_scenes; }
        }

        private List<Scene> m_scenes = new List<Scene>();

        private IMuteListConnector MuteListConnector;
        internal IConfig m_config;

        private IMessageTransferModule m_TransferModule = null;

        private Dictionary<UUID, MuteList[]> MuteListCache = new Dictionary<UUID, MuteList[]>();
        
        public IConfig Config
        {
            get { return m_config; }
        }

        #region IChatModule

        public Dictionary<string, IChatPlugin> ChatPlugins = new Dictionary<string, IChatPlugin>();
        public List<IChatPlugin> AllChatPlugins = new List<IChatPlugin>();
        public void RegisterChatPlugin(string main, IChatPlugin plugin)
        {
            if (!ChatPlugins.ContainsKey(main))
                ChatPlugins.Add(main, plugin);
        }
        
        #endregion

        #region ISharedRegionModule Members

        public virtual void Initialise(IConfigSource config)
        {
            m_config = config.Configs["AuroraChat"];

            if (null == m_config)
            {
                m_log.Info("[AURORACHAT]: no config found, plugin disabled");
                m_enabled = false;
                return;
            }

            if (!m_config.GetBoolean("enabled", true))
            {
                m_log.Info("[AURORACHAT]: plugin disabled by configuration");
                m_enabled = false;
                return;
            }

            m_whisperdistance = m_config.GetInt("whisper_distance", m_whisperdistance);
            m_saydistance = m_config.GetInt("say_distance", m_saydistance);
            m_shoutdistance = m_config.GetInt("shout_distance", m_shoutdistance);
            m_maxChatDistance = m_config.GetFloat("max_chat_distance", m_maxChatDistance);

            m_useMuteListModule = (config.Configs["Messaging"].GetString("MuteListModule", "AuroraChatModule") == "AuroraChatModule");
        }

        private void FindChatPlugins()
        {
            AllChatPlugins = AuroraModuleLoader.PickupModules<IChatPlugin>();
            foreach (IChatPlugin plugin in AllChatPlugins)
            {
                plugin.Initialize(this);
            }
        }

        public virtual void AddRegion(Scene scene)
        {
            if (!m_enabled) return;

            if (!m_scenes.Contains(scene))
            {
                m_scenes.Add(scene);
                scene.EventManager.OnNewClient += OnNewClient;
                scene.EventManager.OnClosingClient += OnClosingClient;
                scene.EventManager.OnRegisterCaps += RegisterCaps;
                scene.EventManager.OnClientConnect += OnClientConnect;
                scene.EventManager.OnIncomingInstantMessage += OnGridInstantMessage;

                scene.RegisterModuleInterface<IMuteListModule>(this);
                scene.RegisterModuleInterface<IChatModule>(this);
                FindChatPlugins();
            }
            //m_log.InfoFormat("[CHAT]: Initialized for {0} w:{1} s:{2} S:{3}", scene.RegionInfo.RegionName,
            //                 m_whisperdistance, m_saydistance, m_shoutdistance);
        }

        public virtual void RegionLoaded(Scene scene)
        {
            if (!m_enabled) return;

            if (m_useMuteListModule)
                MuteListConnector = Aurora.DataManager.DataManager.RequestPlugin<IMuteListConnector>();

            if (m_TransferModule == null)
            {
                m_TransferModule =
                    scene.RequestModuleInterface<IMessageTransferModule>();

                if (m_TransferModule == null)
                {
                    m_log.Error("[CONFERANCE MESSAGE]: No message transfer module, IM will not work!");
                    scene.EventManager.OnClientConnect -= OnClientConnect;

                    m_scenes.Clear();
                    m_enabled = false;
                }
            }
        }

        public virtual void RemoveRegion(Scene scene)
        {
            if (!m_enabled) return;

            if (m_scenes.Contains(scene))
            {
                scene.EventManager.OnNewClient -= OnNewClient;
                scene.EventManager.OnClosingClient -= OnClosingClient;
                scene.EventManager.OnClientConnect -= OnClientConnect;
                scene.EventManager.OnRegisterCaps -= RegisterCaps;
                scene.EventManager.OnIncomingInstantMessage -= OnGridInstantMessage;

                m_scenes.Remove(scene);
                scene.UnregisterModuleInterface<IMuteListModule>(this);
                scene.UnregisterModuleInterface<IChatModule>(this);
            }
        }

        public virtual void Close()
        {
        }

        public virtual void PostInitialise()
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public virtual string Name
        {
            get { return "AuroraChatModule"; }
        }

        #endregion

        private void OnClosingClient(IClientAPI client)
        {
            client.OnChatFromClient -= OnChatFromClient;
            client.OnMuteListRequest -= OnMuteListRequest;
            client.OnUpdateMuteListEntry -= OnMuteListUpdate;
            client.OnRemoveMuteListEntry -= OnMuteListRemove;
            //Tell all client plugins that the user left
            foreach (IChatPlugin plugin in AllChatPlugins)
            {
                plugin.OnClosingClient(client.AgentId, ((Scene)client.Scene));
            }
        }

        public virtual void OnNewClient(IClientAPI client)
        {
            client.OnChatFromClient += OnChatFromClient;
            client.OnMuteListRequest += OnMuteListRequest;
            client.OnUpdateMuteListEntry += OnMuteListUpdate;
            client.OnRemoveMuteListEntry += OnMuteListRemove;

            //Tell all the chat plugins about the new user
            foreach (IChatPlugin plugin in AllChatPlugins)
            {
                plugin.OnNewClient(client);
            }
        }

        /// <summary>
        /// Set the correct position for the chat message
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        protected OSChatMessage FixPositionOfChatMessage(OSChatMessage c)
        {
            ScenePresence avatar;
            Scene scene = (Scene)c.Scene;
            if ((avatar = scene.GetScenePresence(c.Sender.AgentId)) != null)
                c.Position = avatar.AbsolutePosition;

            return c;
        }

        /// <summary>
        /// New chat message from the client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="c"></param>
        protected virtual void OnChatFromClient(Object sender, OSChatMessage c)
        {
            c = FixPositionOfChatMessage(c);

            // redistribute to interested subscribers
            Scene scene = (Scene)c.Scene;
            scene.EventManager.TriggerOnChatFromClient(sender, c);

            // early return if not on public or debug channel
            if (c.Channel != DEFAULT_CHANNEL && c.Channel != DEBUG_CHANNEL) return;

            // sanity check:
            if (c.Sender == null)
            {
                m_log.ErrorFormat("[CHAT] OnChatFromClient from {0} has empty Sender field!", sender);
                return;
            }

            //If the message is not blank, tell the plugins about it
            if (c.Message != "")
            {
                foreach (string pluginMain in ChatPlugins.Keys)
                {
                    //if their plugin level is all or the message starts with the message, send the message to the plugin
                    if (pluginMain == "all" || c.Message.StartsWith(pluginMain + "."))
                    {
                        IChatPlugin plugin;
                        ChatPlugins.TryGetValue(pluginMain, out plugin);
                        //If it returns false, stop the message from being sent
                        if (!plugin.OnNewChatMessageFromWorld(c, out c))
                            return;
                    }
                }
            }
            bool Sent = false;
            OpenSim.Services.Interfaces.INeighborService neighborService = c.Scene.RequestModuleInterface<OpenSim.Services.Interfaces.INeighborService>();
            if (neighborService != null)
            {
                string Name = "";
                if(sender is IClientAPI)
                {
                    Name = ((IClientAPI)sender).Name;
                }
                c.From = Name;
                Sent = neighborService.SendChatMessageToNeighbors(c, ChatSourceType.Agent, c.Scene.RegionInfo);
            }
            if (!Sent)
                DeliverChatToAvatars(ChatSourceType.Agent, c);
        }

        /// <summary>
        /// Send the message from the prim to the avatars in the regions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="c"></param>
        public virtual void OnChatFromWorld(Object sender, OSChatMessage c)
        {
            // early return if not on public or debug channel
            if (c.Channel != DEFAULT_CHANNEL && c.Channel != DEBUG_CHANNEL) return;

            bool Sent = false;

            if(c.Range > m_maxChatDistance) //Check for max distance
                c.Range = m_maxChatDistance;

            OpenSim.Services.Interfaces.INeighborService neighborService = c.Scene.RequestModuleInterface<OpenSim.Services.Interfaces.INeighborService>();
            if (neighborService != null && c.Message != "")
            {
                Sent = neighborService.SendChatMessageToNeighbors(c, ChatSourceType.Object, c.Scene.RegionInfo);
            }

            if (!Sent)
                DeliverChatToAvatars(ChatSourceType.Object, c);
        }

        public void SimChat(string message, ChatTypeEnum type, int channel, Vector3 fromPos, string fromName,
                               UUID fromID, bool fromAgent, bool broadcast, float range, UUID ToAgentID, Scene scene)
        {
            OSChatMessage args = new OSChatMessage();

            args.Message = message;
            args.Channel = channel;
            args.Type = type;
            args.Position = fromPos;
            args.Range = range;
            args.SenderUUID = fromID;
            args.Scene = scene;
            args.ToAgentID = ToAgentID;

            if (fromAgent)
            {
                ScenePresence user = scene.GetScenePresence(fromID);
                if (user != null)
                    args.Sender = user.ControllingClient;
            }
            else
            {
                SceneObjectPart obj = scene.GetSceneObjectPart(fromID);
                args.SenderObject = obj;
            }

            args.From = fromName;
            //args.

            if (broadcast)
            {
                OnChatBroadcast(scene, args);
                scene.EventManager.TriggerOnChatBroadcast(scene, args);
            }
            else
            {
                OnChatFromWorld(scene, args);
                scene.EventManager.TriggerOnChatFromWorld(scene, args);
            }
        }

        public void SimChat(string message, ChatTypeEnum type, int channel, Vector3 fromPos, string fromName,
                            UUID fromID, bool fromAgent, Scene scene)
        {
            SimChat(message, type, channel, fromPos, fromName, fromID, fromAgent, false, -1, UUID.Zero, scene);
        }

        /// <summary>
        /// Say this message directly to a single person
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <param name="fromPos"></param>
        /// <param name="fromName"></param>
        /// <param name="fromAgentID"></param>
        public void SimChatBroadcast(string message, ChatTypeEnum type, int channel, Vector3 fromPos, string fromName,
                                     UUID fromID, bool fromAgent, UUID ToAgentID, Scene scene)
        {
            SimChat(message, type, channel, fromPos, fromName, fromID, fromAgent, true, -1, ToAgentID, scene);
        }

        public virtual void DeliverChatToAvatars(ChatSourceType sourceType, OSChatMessage c)
        {
            string fromName = c.From;
            UUID fromID = UUID.Zero;
            string message = c.Message;
            IScene scene = c.Scene;
            Vector3 fromPos = c.Position;
            Vector3 regionPos = scene != null ? new Vector3(scene.RegionInfo.RegionLocX,
                                            scene.RegionInfo.RegionLocY, 0) : Vector3.Zero;

            if (c.Channel == DEBUG_CHANNEL) c.Type = ChatTypeEnum.DebugChannel;

            switch (sourceType)
            {
                case ChatSourceType.Agent:
                    if (scene != null)
                    {
                        if (!(scene is Scene))
                        {
                            m_log.WarnFormat("[CHAT]: scene {0} is not a Scene object, cannot obtain scene presence for {1}",
                                             scene.RegionInfo.RegionName, c.Sender.AgentId);
                            return;
                        }
                        ScenePresence avatar = (scene as Scene).GetScenePresence(c.Sender.AgentId);
                        if (avatar != null && message == "")
                        {
                            fromPos = avatar.AbsolutePosition;
                            fromName = avatar.Name;
                            fromID = c.Sender.AgentId;
                            //Always send this so it fires on typing start and end
                            avatar.SendScriptEventToAttachments("changed", new object[] { Changed.STATE });
                        }
                        else
                            fromID = c.SenderUUID;
                    }
                    else
                        fromID = c.SenderUUID;
                    break;

                case ChatSourceType.Object:
                    fromID = c.SenderUUID;

                    break;
            }

            if (message.Length >= 1000) // libomv limit
                message = message.Substring(0, 1000);

            // m_log.DebugFormat("[CHAT]: DCTA: fromID {0} fromName {1}, cType {2}, sType {3}", fromID, fromName, c.Type, sourceType);

            foreach (Scene s in m_scenes)
            {
                List<ScenePresence> ScenePresences = s.ScenePresences;
                foreach (ScenePresence presence in ScenePresences)
                {
                    // don't send stuff to child agents
                    if (!presence.IsChildAgent)
                    {
                        //Block this out early so we don't look through the mutes if the message shouldn't even be sent
                        Vector3 fromRegionPos = fromPos + regionPos;
                        Vector3 toRegionPos = presence.AbsolutePosition +
                            new Vector3(presence.Scene.RegionInfo.RegionLocX,
                                        presence.Scene.RegionInfo.RegionLocY, 0);

                        int dis = (int)Util.GetDistanceTo(toRegionPos, fromRegionPos);

                        //Check for max range
                        if (c.Type == ChatTypeEnum.Whisper && dis > m_whisperdistance ||
                            c.Type == ChatTypeEnum.Say && dis > m_saydistance ||
                            c.Type == ChatTypeEnum.Shout && dis > m_shoutdistance ||
                            c.Type == ChatTypeEnum.Custom && dis > c.Range)
                        {
                            continue;
                        }
                        TrySendChatMessage(presence, fromPos, regionPos, fromID, fromName, c.Type, message, sourceType, c.Range);
                    }
                }
            }
        }

        static private Vector3 CenterOfRegion = new Vector3(Constants.RegionSize, Constants.RegionSize, 30);

        protected virtual void OnChatBroadcast(Object sender, OSChatMessage c)
        {
            // unless the chat to be broadcast is of type Region, we
            // drop it if its channel is neither 0 nor DEBUG_CHANNEL
            if (c.Channel != DEFAULT_CHANNEL && c.Channel != DEBUG_CHANNEL && c.Type != ChatTypeEnum.Region) return;

            ChatTypeEnum cType = c.Type;
            if (c.Channel == DEBUG_CHANNEL)
                cType = ChatTypeEnum.DebugChannel;

            if (c.Range > m_maxChatDistance)
                c.Range = m_maxChatDistance;

            if (cType == ChatTypeEnum.SayTo) //Change to something client can understand as SayTo doesn't exist except on the server
                cType = ChatTypeEnum.Owner;

            if (cType == ChatTypeEnum.Region)
                cType = ChatTypeEnum.Say;

            if (c.Message.Length > 1100)
                c.Message = c.Message.Substring(0, 1000);

            // broadcast chat works by redistributing every incoming chat
            // message to each avatar in the scene.
            string fromName = c.From;

            UUID fromID = UUID.Zero;
            ChatSourceType sourceType = ChatSourceType.Object;
            if (null != c.Sender)
            {
                ScenePresence avatar = (c.Scene as Scene).GetScenePresence(c.Sender.AgentId);
                fromID = c.Sender.AgentId;
                fromName = avatar.Name;
                sourceType = ChatSourceType.Agent;
            }

            // m_log.DebugFormat("[CHAT] Broadcast: fromID {0} fromName {1}, cType {2}, sType {3}", fromID, fromName, cType, sourceType);

            ((Scene)c.Scene).ForEachScenePresence(
                delegate(ScenePresence presence)
                {
                    // ignore chat from child agents
                    if (presence.IsChildAgent) return;

                    IClientAPI client = presence.ControllingClient;

                    // don't forward SayOwner chat from objects to
                    // non-owner agents
                    if ((c.Type == ChatTypeEnum.Owner) &&
                        (null != c.SenderObject) &&
                        (((SceneObjectPart)c.SenderObject).OwnerID != client.AgentId))
                        return;

                    // don't forward SayTo chat from objects to
                    // non-targeted agents
                    if ((c.Type == ChatTypeEnum.SayTo) &&
                        (c.ToAgentID != client.AgentId))
                        return;

                    client.SendChatMessage(c.Message, (byte)cType, CenterOfRegion, fromName, fromID,
                                           (byte)sourceType, (byte)ChatAudibleLevel.Fully);
                });
        }


        public virtual void TrySendChatMessage(ScenePresence presence, Vector3 fromPos, Vector3 regionPos,
                                                  UUID fromAgentID, string fromName, ChatTypeEnum type,
                                                  string message, ChatSourceType src, float Range)
        {
            if (type == ChatTypeEnum.Custom)
            {
                Vector3 fromRegionPos = fromPos + regionPos;
                Vector3 toRegionPos = presence.AbsolutePosition +
                    new Vector3(presence.Scene.RegionInfo.RegionLocX,
                                presence.Scene.RegionInfo.RegionLocY, 0);

                int dis = (int)Util.GetDistanceTo(toRegionPos, fromRegionPos);
                //Set the best fitting setting for custom
                if (dis < m_whisperdistance)
                    type = ChatTypeEnum.Whisper;
                else if (dis > m_saydistance)
                    type = ChatTypeEnum.Shout;
                else if (dis > m_whisperdistance && dis < m_saydistance)
                    type = ChatTypeEnum.Say;
            }

            // TODO: should change so the message is sent through the avatar rather than direct to the ClientView
            presence.ControllingClient.SendChatMessage(message, (byte)type, fromPos, fromName,
                                                       fromAgentID, (byte)src, (byte)ChatAudibleLevel.Fully);
        }

        /// <summary>
        /// Get all the mutes the client has set
        /// </summary>
        /// <param name="client"></param>
        /// <param name="crc"></param>
        private void OnMuteListRequest(IClientAPI client, uint crc)
        {
            if (!m_useMuteListModule)
                return;
            //Sends the name of the file being sent by the xfer module DO NOT EDIT!!!
            string filename = "mutes" + client.AgentId.ToString();
            byte[] fileData = new byte[0];
            string invString = "";
            int i = 0;
            bool cached = false;
            MuteList[] List = GetMutes(client.AgentId, out cached);
            if (List == null)
                return;
            if (cached)
                client.SendUseCachedMuteList();

            Dictionary<UUID, bool> cache = new Dictionary<UUID, bool>();
            foreach (MuteList mute in List)
            {
                cache[mute.MuteID] = true;
                invString += (mute.MuteType + " " + mute.MuteID + " " + mute.MuteName + " |\n");
                i++;
            }
            
            if(invString != "")
                invString = invString.Remove(invString.Length - 3, 3);
            
            fileData = OpenMetaverse.Utils.StringToBytes(invString);
            IXfer xfer = client.Scene.RequestModuleInterface<IXfer>();
            if (xfer != null)
            {
                xfer.AddNewFile(filename, fileData);
                client.SendMuteListUpdate(filename);
            }
        }

        /// <summary>
        /// Get all the mutes from the database
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="Cached"></param>
        /// <returns></returns>
        public MuteList[] GetMutes(UUID AgentID, out bool Cached)
        {
            Cached = false;
            MuteList[] List = new MuteList[0];
            if (MuteListConnector == null)
                return List;
            if (!MuteListCache.TryGetValue(AgentID, out List))
                List = MuteListConnector.GetMuteList(AgentID);
            else
            {
                Cached = true;
            }
            return List;
        }

        /// <summary>
        /// Update the mute (from the client)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="MuteID"></param>
        /// <param name="Name"></param>
        /// <param name="Flags"></param>
        /// <param name="AgentID"></param>
        private void OnMuteListUpdate(IClientAPI client, UUID MuteID, string Name, int Flags, UUID AgentID)
        {
            if (!m_useMuteListModule)
                return;
            UpdateMuteList(MuteID, Name, Flags, client.AgentId);
            OnMuteListRequest(client, 0);
        }

        /// <summary>
        /// Update the mute in the database
        /// </summary>
        /// <param name="MuteID"></param>
        /// <param name="Name"></param>
        /// <param name="Flags"></param>
        /// <param name="AgentID"></param>
        public void UpdateMuteList(UUID MuteID, string Name, int Flags, UUID AgentID)
        {
            if (MuteID == UUID.Zero)
                return;
            MuteList Mute = new MuteList()
            {
                MuteID = MuteID,
                MuteName = Name,
                MuteType = Flags.ToString()
            };
            MuteListConnector.UpdateMute(Mute, AgentID);
            MuteListCache.Remove(AgentID);
        }

        /// <summary>
        /// Remove the mute (from the client)
        /// </summary>
        /// <param name="client"></param>
        /// <param name="MuteID"></param>
        /// <param name="Name"></param>
        /// <param name="AgentID"></param>
        private void OnMuteListRemove(IClientAPI client, UUID MuteID, string Name, UUID AgentID)    
        {
            if (!m_useMuteListModule)
                return;
            RemoveMute(MuteID, Name, client.AgentId);
            OnMuteListRequest(client, 0);
        }

        /// <summary>
        /// Remove the given mute from the user's mute list in the database
        /// </summary>
        /// <param name="MuteID"></param>
        /// <param name="Name"></param>
        /// <param name="AgentID"></param>
        public void RemoveMute(UUID MuteID, string Name, UUID AgentID)
        {
            //Gets sent if a mute is not selected.
            if (MuteID != UUID.Zero)
            {
                MuteListConnector.DeleteMute(MuteID, AgentID);
                MuteListCache.Remove(AgentID);
            }
        }

        private Dictionary<UUID, ChatSession> ChatSessions = new Dictionary<UUID, ChatSession>();

        /// <summary>
        /// Internal class for chat sessions 
        /// </summary>
        public class ChatSession
        {
            public UUID SessionID;
            public List<ChatSessionMember> Members;
            public string Name;
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
            public bool HasBeenAdded;
        }
        
        /// <summary>
        /// Set up the CAPS for friend conferencing
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="caps"></param>
        public void RegisterCaps(UUID agentID, IRegionClientCapsService caps)
        {
            string capsBase = "/CAPS/" + UUID.Random() + "/";

            caps.AddStreamHandler("ChatSessionRequest",
                                new RestHTTPHandler("POST", capsBase,
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return ProcessChatSessionRequest(m_dhttpMethod, agentID);
                                                      }));
        }

        private Hashtable ProcessChatSessionRequest(Hashtable mDhttpMethod, UUID Agent)
        {
            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
            string method = rm["method"].AsString();

            UUID sessionid = UUID.Parse(rm["session-id"].AsString());

            ScenePresence SP = findScenePresence(Agent);
            IEventQueueService eq = SP.Scene.RequestModuleInterface<IEventQueueService>();

            if (method == "start conference")
            {
                //Create the session.
                CreateSession(new ChatSession()
                {
                    Members = new List<ChatSessionMember>(),
                    SessionID = sessionid,
                    Name = SP.Name + " Conference"
                });

                OSDArray parameters = (OSDArray)rm["params"];
                //Add other invited members.
                foreach (OSD param in parameters)
                {
                    AddDefaultPermsMemberToSession(param.AsUUID(), sessionid);
                }

                //Add us to the session!
                AddMemberToGroup(new ChatSessionMember()
                {
                    AvatarKey = Agent,
                    CanVoiceChat = true,
                    IsModerator = true,
                    MuteText = false,
                    MuteVoice = false,
                    HasBeenAdded = true
                }, sessionid);


                //Inform us about our room
                OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock block = new OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock();
                block.AgentID = Agent;
                block.CanVoiceChat = true;
                block.IsModerator = true;
                block.MuteText = false;
                block.MuteVoice = false;
                block.Transition = "ENTER";
                eq.ChatterBoxSessionAgentListUpdates(sessionid, new OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock[] { block }, Agent, "ENTER", findScene(Agent).RegionInfo.RegionHandle);

                OpenMetaverse.Messages.Linden.ChatterBoxSessionStartReplyMessage cs = new OpenMetaverse.Messages.Linden.ChatterBoxSessionStartReplyMessage();
                cs.VoiceEnabled = true;
                cs.TempSessionID = UUID.Random();
                cs.Type = 1;
                cs.Success = true;
                cs.SessionID = sessionid;
                cs.SessionName = SP.Name + " Conference";
                cs.ModeratedVoice = true;

                Hashtable responsedata = new Hashtable();
                responsedata["int_response_code"] = 200; //501; //410; //404;
                responsedata["content_type"] = "text/plain";
                responsedata["keepalive"] = false;
                OSDMap map = cs.Serialize();
                responsedata["str_response_string"] = map.ToString();
                return responsedata;
            }
            else if (method == "accept invitation")
            {
                //They would like added to the group conversation
                List<OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock> Us = new List<OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock>();
                List<OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock> NotUsAgents = new List<OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock>();

                ChatSession session = GetSession(sessionid);
                if (session != null)
                {
                    ChatSessionMember thismember = FindMember(sessionid, Agent);
                    //Tell all the other members about the incoming member
                    foreach (ChatSessionMember sessionMember in session.Members)
                    {
                        OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock block = new OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock();
                        block.AgentID = sessionMember.AvatarKey;
                        block.CanVoiceChat = sessionMember.CanVoiceChat;
                        block.IsModerator = sessionMember.IsModerator;
                        block.MuteText = sessionMember.MuteText;
                        block.MuteVoice = sessionMember.MuteVoice;
                        block.Transition = "ENTER"; 
                        if (sessionMember.AvatarKey == thismember.AvatarKey)
                            Us.Add(block);
                        else
                        {
                            if (sessionMember.HasBeenAdded) // Don't add not joined yet agents. They don't watn to be here.
                                NotUsAgents.Add(block);
                        }
                    }
                    thismember.HasBeenAdded = true;
                    foreach (ChatSessionMember member in session.Members)
                    {
                        if (member.AvatarKey == thismember.AvatarKey)
                        {
                            //Tell 'us' about all the other agents in the group
                            eq.ChatterBoxSessionAgentListUpdates(session.SessionID, NotUsAgents.ToArray(), member.AvatarKey, "ENTER", findScene(Agent).RegionInfo.RegionHandle);
                        }
                        else
                        {
                            //Tell 'other' agents about the new agent ('us')
                            eq.ChatterBoxSessionAgentListUpdates(session.SessionID, Us.ToArray(), member.AvatarKey, "ENTER", findScene(Agent).RegionInfo.RegionHandle);
                        }
                    }
                }
                Hashtable responsedata = new Hashtable();
                responsedata["int_response_code"] = 200; //501; //410; //404;
                responsedata["content_type"] = "text/plain";
                responsedata["keepalive"] = false;
                responsedata["str_response_string"] = "Accepted";
                return responsedata;
            }
            else if (method == "mute update")
            {
                //Check if the user is a moderator
                Hashtable responsedata = new Hashtable();
                if (!CheckModeratorPermission(Agent, sessionid))
                {
                    responsedata["int_response_code"] = 200; //501; //410; //404;
                    responsedata["content_type"] = "text/plain";
                    responsedata["keepalive"] = false;
                    responsedata["str_response_string"] = "Accepted";
                    return responsedata;
                }

                OSDMap parameters = (OSDMap)rm["params"];
                UUID AgentID = parameters["agent_id"].AsUUID();
                OSDMap muteInfoMap = (OSDMap)parameters["mute_info"];

                ChatSessionMember thismember = FindMember(sessionid, Agent);
                if (muteInfoMap.ContainsKey("text"))
                    thismember.MuteText = muteInfoMap["text"].AsBoolean();
                if (muteInfoMap.ContainsKey("voice"))
                    thismember.MuteVoice = muteInfoMap["voice"].AsBoolean();

                OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock block = new OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock();
                block.AgentID = thismember.AvatarKey;
                block.CanVoiceChat = thismember.CanVoiceChat;
                block.IsModerator = thismember.IsModerator;
                block.MuteText = thismember.MuteText;
                block.MuteVoice = thismember.MuteVoice;
                block.Transition = "ENTER";

                // Send an update to the affected user
                eq.ChatterBoxSessionAgentListUpdates(sessionid, new OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock[] { block }, AgentID, "", findScene(Agent).RegionInfo.RegionHandle);
                
                responsedata["int_response_code"] = 200; //501; //410; //404;
                responsedata["content_type"] = "text/plain";
                responsedata["keepalive"] = false;
                responsedata["str_response_string"] = "Accepted";
                return responsedata;
            }
            else
            {
                m_log.Warn("ChatSessionRequest : " + method);
                Hashtable responsedata = new Hashtable();
                responsedata["int_response_code"] = 200; //501; //410; //404;
                responsedata["content_type"] = "text/plain";
                responsedata["keepalive"] = false;
                responsedata["str_response_string"] = "Accepted";
                return responsedata;
            }
        }

        private Scene findScene(UUID agentID)
        {
            foreach (Scene scene in m_scenes)
            {
                ScenePresence SP = scene.GetScenePresence(agentID);
                if (SP != null && !SP.IsChildAgent)
                    return scene;
            }
            return null;
        }

        /// <summary>
        /// Hook up the IMs from the client
        /// </summary>
        /// <param name="client"></param>
        void OnClientConnect(IClientCore client)
        {
            IClientIM clientIM;
            if (client.TryGet(out clientIM))
            {
                clientIM.OnInstantMessage += OnInstantMessage;
            }
        }

        /// <summary>
        /// Find the presence from all the known sims
        /// </summary>
        /// <param name="avID"></param>
        /// <returns></returns>
        public ScenePresence findScenePresence(UUID avID)
        {
            foreach (Scene s in m_scenes)
            {
                ScenePresence SP = s.GetScenePresence(avID);
                if (SP != null)
                {
                    return SP;
                }
            }
            return null;
        }

        private void OnGridInstantMessage(GridInstantMessage msg)
        {
            OnInstantMessage(findScenePresence(new UUID(msg.toAgentID)).ControllingClient, msg);
        }

        /// <summary>
        /// If its a message we deal with, pull it from the client here
        /// </summary>
        /// <param name="client"></param>
        /// <param name="im"></param>
        public void OnInstantMessage(IClientAPI client, GridInstantMessage im)
        {
            byte dialog = im.dialog;
            //We only deal with friend IM sessions here, groups module handles group IM sessions
            if (dialog == (byte)InstantMessageDialog.SessionSend)
                SendChatToSession(client, im);

            if (dialog == (byte)InstantMessageDialog.SessionDrop)
                DropMemberFromSession(client, im);
        }

        /// <summary>
        /// Find the member from X sessionID 
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
            ChatSessionMember thismember = new ChatSessionMember() { AvatarKey = UUID.Zero };
            foreach (ChatSessionMember testmember in session.Members)
            {
                if (testmember.AvatarKey == Agent)
                    thismember = testmember;
            }
            return thismember;
        }

        /// <summary>
        /// Check whether the user has moderator permissions
        /// </summary>
        /// <param name="Agent"></param>
        /// <param name="sessionid"></param>
        /// <returns></returns>
        public bool CheckModeratorPermission(UUID Agent, UUID sessionid)
        {
            ChatSession session;
            ChatSessions.TryGetValue(sessionid, out session);
            ChatSessionMember thismember = new ChatSessionMember() { AvatarKey = UUID.Zero };
            foreach (ChatSessionMember testmember in session.Members)
            {
                if (testmember.AvatarKey == Agent)
                    thismember = testmember;
            }
            if (thismember == null)
                return false;
            return thismember.IsModerator;
        }

        /// <summary>
        /// Remove the member from this session
        /// </summary>
        /// <param name="client"></param>
        /// <param name="im"></param>
        public void DropMemberFromSession(IClientAPI client, GridInstantMessage im)
        {
            ChatSession session;
            ChatSessions.TryGetValue(UUID.Parse(im.imSessionID.ToString()), out session);
            if (session == null)
                return;
            ChatSessionMember member = new ChatSessionMember() { AvatarKey = UUID.Zero };
            foreach (ChatSessionMember testmember in session.Members)
            {
                if (member.AvatarKey == UUID.Parse(im.fromAgentID.ToString()))
                    member = testmember;
            }

            if (member.AvatarKey != UUID.Zero)
                session.Members.Remove(member);

            if (session.Members.Count == 0)
            {
                ChatSessions.Remove(session.SessionID);
                return;
            }

            OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock block = new OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock();
            block.AgentID = member.AvatarKey;
            block.CanVoiceChat = member.CanVoiceChat;
            block.IsModerator = member.IsModerator;
            block.MuteText = member.MuteText;
            block.MuteVoice = member.MuteVoice;
            block.Transition = "LEAVE";
            IEventQueueService eq = client.Scene.RequestModuleInterface<IEventQueueService>();
            foreach (ChatSessionMember sessionMember in session.Members)
            {
                eq.ChatterBoxSessionAgentListUpdates(session.SessionID, new OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock[] { block }, sessionMember.AvatarKey, "LEAVE", findScene(sessionMember.AvatarKey).RegionInfo.RegionHandle);
            }
        }

        /// <summary>
        /// Send chat to all the members of this friend conference
        /// </summary>
        /// <param name="client"></param>
        /// <param name="im"></param>
        public void SendChatToSession(IClientAPI client, GridInstantMessage im)
        {
            ChatSession session;
            ChatSessions.TryGetValue(UUID.Parse(im.imSessionID.ToString()), out session);
            if (session == null)
                return;
            IEventQueueService eq = client.Scene.RequestModuleInterface<IEventQueueService>();
            foreach (ChatSessionMember member in session.Members)
            {
                if (member.HasBeenAdded)
                {
                    im.toAgentID = member.AvatarKey.Guid;
                    im.binaryBucket = OpenMetaverse.Utils.StringToBytes(session.Name);
                    im.RegionID = Guid.Empty;
                    im.ParentEstateID = 0;
                    im.timestamp = 0;
                    m_TransferModule.SendInstantMessage(im);
                }
                else
                {
                    im.toAgentID = member.AvatarKey.Guid;
                    eq.ChatterboxInvitation(
                        session.SessionID
                        , session.Name
                        , new UUID(im.fromAgentID)
                        , im.message
                        , new UUID(im.toAgentID)
                        , im.fromAgentName
                        , im.dialog
                        , im.timestamp
                        , im.offline == 1
                        , (int)im.ParentEstateID
                        , im.Position
                        , 1
                        , new UUID(im.imSessionID)
                        , false
                        , OpenMetaverse.Utils.StringToBytes(session.Name)
                        , findScene(member.AvatarKey).RegionInfo.RegionHandle
                        );
                }
            }
        }

        /// <summary>
        /// Add this member to the friend conference
        /// </summary>
        /// <param name="member"></param>
        /// <param name="SessionID"></param>
        public void AddMemberToGroup(ChatSessionMember member, UUID SessionID)
        {
            ChatSession session;
            ChatSessions.TryGetValue(SessionID, out session);
            session.Members.Add(member);
        }

        /// <summary>
        /// Create a new friend conference session
        /// </summary>
        /// <param name="session"></param>
        public void CreateSession(ChatSession session)
        {
            ChatSessions.Add(session.SessionID, session);
        }

        /// <summary>
        /// Get a session by a user's sessionID
        /// </summary>
        /// <param name="SessionID"></param>
        /// <returns></returns>
        public ChatSession GetSession(UUID SessionID)
        {
            ChatSession session;
            ChatSessions.TryGetValue(SessionID, out session);
            return session;
        }

        /// <summary>
        /// Add the agent to the in-memory session lists and give them the default permissions
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="SessionID"></param>
        private void AddDefaultPermsMemberToSession(UUID AgentID, UUID SessionID)
        {
            ChatSession session;
            ChatSessions.TryGetValue(SessionID, out session);
            ChatSessionMember member = new ChatSessionMember()
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
    }
}
