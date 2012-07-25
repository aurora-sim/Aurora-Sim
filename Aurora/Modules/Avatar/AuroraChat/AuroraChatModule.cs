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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Messages.Linden;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Capabilities;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
using ChatSessionMember = Aurora.Framework.ChatSessionMember;

namespace Aurora.Modules.Chat
{
    public class AuroraChatModule : ISharedRegionModule, IChatModule, IMuteListModule
    {
        private const int DEBUG_CHANNEL = 2147483647;
        private const int DEFAULT_CHANNEL = 0;
        private readonly Dictionary<UUID, ChatSession> ChatSessions = new Dictionary<UUID, ChatSession>();
        private readonly Dictionary<UUID, MuteList[]> MuteListCache = new Dictionary<UUID, MuteList[]>();
        private readonly List<IScene> m_scenes = new List<IScene>();

        private IMuteListConnector MuteListConnector;
        private IMessageTransferModule m_TransferModule;
        internal IConfig m_config;

        private bool m_enabled = true;
        private float m_maxChatDistance = 100;
        private int m_saydistance = 30;
        private int m_shoutdistance = 256;
        private bool m_useMuteListModule = true;
        private int m_whisperdistance = 10;

        public float MaxChatDistance
        {
            get { return m_maxChatDistance; }
            set { m_maxChatDistance = value; }
        }

        public List<IScene> Scenes
        {
            get { return m_scenes; }
        }

        #region IChatModule Members

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

        public IConfig Config
        {
            get { return m_config; }
        }

        /// <summary>
        ///   Send the message from the prim to the avatars in the regions
        /// </summary>
        /// <param name = "sender"></param>
        /// <param name = "c"></param>
        public virtual void OnChatFromWorld(Object sender, OSChatMessage c)
        {
            // early return if not on public or debug channel
            if (c.Channel != DEFAULT_CHANNEL && c.Channel != DEBUG_CHANNEL) return;

            if (c.Range > m_maxChatDistance) //Check for max distance
                c.Range = m_maxChatDistance;

            DeliverChatToAvatars(ChatSourceType.Object, c);
        }

        public void SimChat(string message, ChatTypeEnum type, int channel, Vector3 fromPos, string fromName,
                            UUID fromID, bool fromAgent, bool broadcast, float range, UUID ToAgentID, IScene scene)
        {
            OSChatMessage args = new OSChatMessage
                                     {
                                         Message = message,
                                         Channel = channel,
                                         Type = type,
                                         Position = fromPos,
                                         Range = range,
                                         SenderUUID = fromID,
                                         Scene = scene,
                                         ToAgentID = ToAgentID
                                     };


            if (fromAgent)
            {
                IScenePresence user = scene.GetScenePresence(fromID);
                if (user != null)
                    args.Sender = user.ControllingClient;
            }
            else
            {
                args.SenderObject = scene.GetSceneObjectPart(fromID);
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
                            UUID fromID, bool fromAgent, IScene scene)
        {
            SimChat(message, type, channel, fromPos, fromName, fromID, fromAgent, false, -1, UUID.Zero, scene);
        }

        /// <summary>
        ///   Say this message directly to a single person
        /// </summary>
        /// <param name = "message"></param>
        /// <param name = "type"></param>
        /// <param name = "fromPos"></param>
        /// <param name = "fromName"></param>
        /// <param name = "fromAgentID"></param>
        public void SimChatBroadcast(string message, ChatTypeEnum type, int channel, Vector3 fromPos, string fromName,
                                     UUID fromID, bool fromAgent, UUID ToAgentID, IScene scene)
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
            Vector3 regionPos = scene != null
                                    ? new Vector3(scene.RegionInfo.RegionLocX,
                                                  scene.RegionInfo.RegionLocY, 0)
                                    : Vector3.Zero;

            if (c.Channel == DEBUG_CHANNEL) c.Type = ChatTypeEnum.DebugChannel;

            IScenePresence avatar = (scene != null && c.Sender != null)
                                        ? scene.GetScenePresence(c.Sender.AgentId)
                                        : null;
            switch (sourceType)
            {
                case ChatSourceType.Agent:
                    if (scene != null)
                    {
                        if (avatar != null && message == "")
                        {
                            fromPos = avatar.AbsolutePosition;
                            fromName = avatar.Name;
                            fromID = c.Sender.AgentId;
                            //Always send this so it fires on typing start and end
                            IAttachmentsModule attMod = scene.RequestModuleInterface<IAttachmentsModule>();
                            if (attMod != null)
                                attMod.SendScriptEventToAttachments(avatar.UUID, "changed", new object[] {Changed.STATE});
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

            // MainConsole.Instance.DebugFormat("[CHAT]: DCTA: fromID {0} fromName {1}, cType {2}, sType {3}", fromID, fromName, c.Type, sourceType);

            foreach (IScenePresence presence in from s in m_scenes
                                                select s.GetScenePresences() into ScenePresences
                                                from presence in ScenePresences
                                                where !presence.IsChildAgent
                                                let fromRegionPos = fromPos + regionPos
                                                let toRegionPos = presence.AbsolutePosition +
                                                                  new Vector3(presence.Scene.RegionInfo.RegionLocX,
                                                                              presence.Scene.RegionInfo.RegionLocY, 0)
                                                let dis = (int)Util.GetDistanceTo(toRegionPos, fromRegionPos)
                                                where (c.Type != ChatTypeEnum.Whisper || dis <= m_whisperdistance) && (c.Type != ChatTypeEnum.Say || dis <= m_saydistance) && (c.Type != ChatTypeEnum.Shout || dis <= m_shoutdistance) && (c.Type != ChatTypeEnum.Custom || dis <= c.Range)
                                                where sourceType != ChatSourceType.Agent || avatar == null || avatar.CurrentParcel == null || (avatar.CurrentParcelUUID == presence.CurrentParcelUUID || (!avatar.CurrentParcel.LandData.Private && !presence.CurrentParcel.LandData.Private))
                                                select presence)
            {
                //If one of them is in a private parcel, and the other isn't in the same parcel, don't send the chat message
                TrySendChatMessage(presence, fromPos, regionPos, fromID, fromName, c.Type, message, sourceType,
                                   c.Range);
            }
        }

        public virtual void TrySendChatMessage(IScenePresence presence, Vector3 fromPos, Vector3 regionPos,
                                               UUID fromAgentID, string fromName, ChatTypeEnum type,
                                               string message, ChatSourceType src, float Range)
        {
            if (type == ChatTypeEnum.Custom)
            {
                Vector3 fromRegionPos = fromPos + regionPos;
                Vector3 toRegionPos = presence.AbsolutePosition +
                                      new Vector3(presence.Scene.RegionInfo.RegionLocX,
                                                  presence.Scene.RegionInfo.RegionLocY, 0);

                int dis = (int) Util.GetDistanceTo(toRegionPos, fromRegionPos);
                //Set the best fitting setting for custom
                if (dis < m_whisperdistance)
                    type = ChatTypeEnum.Whisper;
                else if (dis > m_saydistance)
                    type = ChatTypeEnum.Shout;
                else if (dis > m_whisperdistance && dis < m_saydistance)
                    type = ChatTypeEnum.Say;
            }

            // TODO: should change so the message is sent through the avatar rather than direct to the ClientView
            presence.ControllingClient.SendChatMessage(message, (byte) type, fromPos, fromName,
                                                       fromAgentID, (byte) src, (byte) ChatAudibleLevel.Fully);
        }

        #endregion

        #region IChatModule

        public List<IChatPlugin> AllChatPlugins = new List<IChatPlugin>();
        public Dictionary<string, IChatPlugin> ChatPlugins = new Dictionary<string, IChatPlugin>();

        public void RegisterChatPlugin(string main, IChatPlugin plugin)
        {
            if (!ChatPlugins.ContainsKey(main))
                ChatPlugins.Add(main, plugin);
        }

        #endregion

        #region IMuteListModule Members

        /// <summary>
        ///   Get all the mutes from the database
        /// </summary>
        /// <param name = "AgentID"></param>
        /// <param name = "Cached"></param>
        /// <returns></returns>
        public MuteList[] GetMutes(UUID AgentID, out bool Cached)
        {
            Cached = false;
            MuteList[] List = new MuteList[0];
            if (MuteListConnector == null)
                return List;
            if (!MuteListCache.TryGetValue(AgentID, out List))
                List = MuteListConnector.GetMuteList(AgentID).ToArray();
            else
                Cached = true;

            return List;
        }

        /// <summary>
        ///   Update the mute in the database
        /// </summary>
        /// <param name = "MuteID"></param>
        /// <param name = "Name"></param>
        /// <param name = "Flags"></param>
        /// <param name = "AgentID"></param>
        public void UpdateMuteList(UUID MuteID, string Name, int Flags, UUID AgentID)
        {
            if (MuteID == UUID.Zero)
                return;
            MuteList Mute = new MuteList
                                {
                                    MuteID = MuteID,
                                    MuteName = Name,
                                    MuteType = Flags.ToString()
                                };
            MuteListConnector.UpdateMute(Mute, AgentID);
            MuteListCache.Remove(AgentID);
        }

        /// <summary>
        ///   Remove the given mute from the user's mute list in the database
        /// </summary>
        /// <param name = "MuteID"></param>
        /// <param name = "Name"></param>
        /// <param name = "AgentID"></param>
        public void RemoveMute(UUID MuteID, string Name, UUID AgentID)
        {
            //Gets sent if a mute is not selected.
            if (MuteID != UUID.Zero)
            {
                MuteListConnector.DeleteMute(MuteID, AgentID);
                MuteListCache.Remove(AgentID);
            }
        }

        #endregion

        #region ISharedRegionModule Members

        public virtual void Initialise(IConfigSource config)
        {
            m_config = config.Configs["AuroraChat"];

            if (null == m_config)
            {
                MainConsole.Instance.Info("[AURORACHAT]: no config found, plugin disabled");
                m_enabled = false;
                return;
            }

            if (!m_config.GetBoolean("enabled", true))
            {
                MainConsole.Instance.Info("[AURORACHAT]: plugin disabled by configuration");
                m_enabled = false;
                return;
            }

            m_whisperdistance = m_config.GetInt("whisper_distance", m_whisperdistance);
            m_saydistance = m_config.GetInt("say_distance", m_saydistance);
            m_shoutdistance = m_config.GetInt("shout_distance", m_shoutdistance);
            m_maxChatDistance = m_config.GetFloat("max_chat_distance", m_maxChatDistance);

            m_useMuteListModule = (config.Configs["Messaging"].GetString("MuteListModule", "AuroraChatModule") ==
                                   "AuroraChatModule");
        }

        public virtual void AddRegion(IScene scene)
        {
            if (!m_enabled)
                return;

            m_scenes.Add(scene);
            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
            scene.EventManager.OnRegisterCaps += RegisterCaps;
            scene.EventManager.OnIncomingInstantMessage += OnGridInstantMessage;
            scene.EventManager.OnChatSessionRequest += OnChatSessionRequest;

            scene.RegisterModuleInterface<IMuteListModule>(this);
            scene.RegisterModuleInterface<IChatModule>(this);
            FindChatPlugins();
            //MainConsole.Instance.InfoFormat("[CHAT]: Initialized for {0} w:{1} s:{2} S:{3}", scene.RegionInfo.RegionName,
            //                 m_whisperdistance, m_saydistance, m_shoutdistance);
        }

        public virtual void RegionLoaded(IScene scene)
        {
            if (!m_enabled) return;

            if (m_useMuteListModule)
                MuteListConnector = DataManager.DataManager.RequestPlugin<IMuteListConnector>();

            if (m_TransferModule == null)
            {
                m_TransferModule =
                    scene.RequestModuleInterface<IMessageTransferModule>();

                if (m_TransferModule == null)
                {
                    MainConsole.Instance.Error("[CONFERANCE MESSAGE]: No message transfer module, IM will not work!");

                    m_scenes.Clear();
                    m_enabled = false;
                }
            }
        }

        public virtual void RemoveRegion(IScene scene)
        {
            if (!m_enabled)
                return;

            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
            scene.EventManager.OnRegisterCaps -= RegisterCaps;
            scene.EventManager.OnIncomingInstantMessage -= OnGridInstantMessage;
            scene.EventManager.OnChatSessionRequest -= OnChatSessionRequest;

            m_scenes.Remove(scene);
            scene.UnregisterModuleInterface<IMuteListModule>(this);
            scene.UnregisterModuleInterface<IChatModule>(this);
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

        private void FindChatPlugins()
        {
            AllChatPlugins = AuroraModuleLoader.PickupModules<IChatPlugin>();
            foreach (IChatPlugin plugin in AllChatPlugins)
            {
                plugin.Initialize(this);
            }
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnChatFromClient -= OnChatFromClient;
            client.OnMuteListRequest -= OnMuteListRequest;
            client.OnUpdateMuteListEntry -= OnMuteListUpdate;
            client.OnRemoveMuteListEntry -= OnMuteListRemove;
            client.OnInstantMessage -= OnInstantMessage;
            //Tell all client plugins that the user left
            foreach (IChatPlugin plugin in AllChatPlugins)
            {
                plugin.OnClosingClient(client.AgentId, client.Scene);
            }
        }

        public virtual void OnNewClient(IClientAPI client)
        {
            client.OnChatFromClient += OnChatFromClient;
            client.OnMuteListRequest += OnMuteListRequest;
            client.OnUpdateMuteListEntry += OnMuteListUpdate;
            client.OnRemoveMuteListEntry += OnMuteListRemove;
            client.OnInstantMessage += OnInstantMessage;

            //Tell all the chat plugins about the new user
            foreach (IChatPlugin plugin in AllChatPlugins)
            {
                plugin.OnNewClient(client);
            }
        }

        /// <summary>
        ///   Set the correct position for the chat message
        /// </summary>
        /// <param name = "c"></param>
        /// <returns></returns>
        protected OSChatMessage FixPositionOfChatMessage(OSChatMessage c)
        {
            IScenePresence avatar;
            if ((avatar = c.Scene.GetScenePresence(c.Sender.AgentId)) != null)
                c.Position = avatar.AbsolutePosition;

            return c;
        }

        /// <summary>
        ///   New chat message from the client
        /// </summary>
        /// <param name = "sender"></param>
        /// <param name = "c"></param>
        protected virtual void OnChatFromClient(IClientAPI sender, OSChatMessage c)
        {
            c = FixPositionOfChatMessage(c);

            // redistribute to interested subscribers
            if(c.Message != "")
                c.Scene.EventManager.TriggerOnChatFromClient(sender, c);

            // early return if not on public or debug channel
            if (c.Channel != DEFAULT_CHANNEL && c.Channel != DEBUG_CHANNEL) return;

            // sanity check:
            if (c.Sender == null)
            {
                MainConsole.Instance.ErrorFormat("[CHAT] OnChatFromClient from {0} has empty Sender field!", sender);
                return;
            }

            //If the message is not blank, tell the plugins about it
            if (c.Message != "")
            {
#if (!ISWIN)
                foreach (string pluginMain in ChatPlugins.Keys)
                {
                    if (pluginMain == "all" || c.Message.StartsWith(pluginMain + "."))
                    {
                        IChatPlugin plugin;
                        ChatPlugins.TryGetValue(pluginMain, out plugin);
                        //If it returns false, stop the message from being sent
                        if (!plugin.OnNewChatMessageFromWorld(c, out c))
                            return;
                    }
                }
#else
                foreach (string pluginMain in ChatPlugins.Keys.Where(pluginMain => pluginMain == "all" || c.Message.StartsWith(pluginMain + ".")))
                {
                    IChatPlugin plugin;
                    ChatPlugins.TryGetValue(pluginMain, out plugin);
                    //If it returns false, stop the message from being sent
                    if (!plugin.OnNewChatMessageFromWorld(c, out c))
                        return;
                }
#endif

            }
            string Name2 = "";
            if (sender is IClientAPI)
            {
                Name2 = (sender).Name;
            }
            c.From = Name2;

            DeliverChatToAvatars(ChatSourceType.Agent, c);
        }

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

            if (cType == ChatTypeEnum.SayTo)
                //Change to something client can understand as SayTo doesn't exist except on the server
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
                IScenePresence avatar = c.Scene.GetScenePresence(c.Sender.AgentId);
                fromID = c.Sender.AgentId;
                fromName = avatar.Name;
                sourceType = ChatSourceType.Agent;
            }

            // MainConsole.Instance.DebugFormat("[CHAT] Broadcast: fromID {0} fromName {1}, cType {2}, sType {3}", fromID, fromName, cType, sourceType);

            c.Scene.ForEachScenePresence(
                delegate(IScenePresence presence)
                    {
                        // ignore chat from child agents
                        if (presence.IsChildAgent) return;

                        IClientAPI client = presence.ControllingClient;

                        // don't forward SayOwner chat from objects to
                        // non-owner agents
                        if ((c.Type == ChatTypeEnum.Owner) &&
                            (null != c.SenderObject) &&
                            (c.SenderObject.OwnerID != client.AgentId))
                            return;

                        // don't forward SayTo chat from objects to
                        // non-targeted agents
                        if ((c.Type == ChatTypeEnum.SayTo) &&
                            (c.ToAgentID != client.AgentId))
                            return;
                        bool cached = false;
                        MuteList[] mutes = GetMutes(client.AgentId, out cached);
                        foreach(MuteList m in mutes)
                            if (m.MuteID == c.SenderUUID ||
                                (c.SenderObject != null && m.MuteID == c.SenderObject.ParentEntity.UUID))
                                return;
                        client.SendChatMessage(c.Message, (byte) cType,
                                               new Vector3(client.Scene.RegionInfo.RegionSizeX*0.5f,
                                                           client.Scene.RegionInfo.RegionSizeY*0.5f, 30), fromName,
                                               fromID,
                                               (byte) sourceType, (byte) ChatAudibleLevel.Fully);
                    });
        }


        /// <summary>
        ///   Get all the mutes the client has set
        /// </summary>
        /// <param name = "client"></param>
        /// <param name = "crc"></param>
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

            if (invString != "")
                invString = invString.Remove(invString.Length - 3, 3);

            fileData = Utils.StringToBytes(invString);
            IXfer xfer = client.Scene.RequestModuleInterface<IXfer>();
            if (xfer != null)
            {
                xfer.AddNewFile(filename, fileData);
                client.SendMuteListUpdate(filename);
            }
        }

        /// <summary>
        ///   Update the mute (from the client)
        /// </summary>
        /// <param name = "client"></param>
        /// <param name = "MuteID"></param>
        /// <param name = "Name"></param>
        /// <param name = "Flags"></param>
        /// <param name = "AgentID"></param>
        private void OnMuteListUpdate(IClientAPI client, UUID MuteID, string Name, int Flags, UUID AgentID)
        {
            if (!m_useMuteListModule)
                return;
            UpdateMuteList(MuteID, Name, Flags, client.AgentId);
            OnMuteListRequest(client, 0);
        }

        /// <summary>
        ///   Remove the mute (from the client)
        /// </summary>
        /// <param name = "client"></param>
        /// <param name = "MuteID"></param>
        /// <param name = "Name"></param>
        /// <param name = "AgentID"></param>
        private void OnMuteListRemove(IClientAPI client, UUID MuteID, string Name, UUID AgentID)
        {
            if (!m_useMuteListModule)
                return;
            RemoveMute(MuteID, Name, client.AgentId);
            OnMuteListRequest(client, 0);
        }

        /// <summary>
        ///   Set up the CAPS for friend conferencing
        /// </summary>
        /// <param name = "agentID"></param>
        /// <param name = "caps"></param>
        public OSDMap RegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["ChatSessionRequest"] = CapsUtil.CreateCAPS("ChatSessionRequest", "");

            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["ChatSessionRequest"],
                                                      delegate(string path, Stream request,
                                                        OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                      {
                                                          return ProcessChatSessionRequest(request, agentID);
                                                      }));
            return retVal;
        }

        private byte[] ProcessChatSessionRequest(Stream request, UUID Agent)
        {
            OSDMap rm = (OSDMap) OSDParser.DeserializeLLSDXml(request);

            return Encoding.UTF8.GetBytes(findScene(Agent).EventManager.TriggerChatSessionRequest(Agent, rm));
        }

        private string OnChatSessionRequest(UUID Agent, OSDMap rm)
        {
            string method = rm["method"].AsString();

            UUID sessionid = UUID.Parse(rm["session-id"].AsString());

            IScenePresence SP = findScenePresence(Agent);
            IEventQueueService eq = SP.Scene.RequestModuleInterface<IEventQueueService>();

            if (method == "start conference")
            {
                //Create the session.
                CreateSession(new ChatSession
                                  {
                                      Members = new List<ChatSessionMember>(),
                                      SessionID = sessionid,
                                      Name = SP.Name + " Conference"
                                  });

                OSDArray parameters = (OSDArray) rm["params"];
                //Add other invited members.
                foreach (OSD param in parameters)
                {
                    AddDefaultPermsMemberToSession(param.AsUUID(), sessionid);
                }

                //Add us to the session!
                AddMemberToGroup(new ChatSessionMember
                                     {
                                         AvatarKey = Agent,
                                         CanVoiceChat = true,
                                         IsModerator = true,
                                         MuteText = false,
                                         MuteVoice = false,
                                         HasBeenAdded = true
                                     }, sessionid);


                //Inform us about our room
                ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock block =
                    new ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock
                        {
                            AgentID = Agent,
                            CanVoiceChat = true,
                            IsModerator = true,
                            MuteText = false,
                            MuteVoice = false,
                            Transition = "ENTER"
                        };
                eq.ChatterBoxSessionAgentListUpdates(sessionid, new[] {block}, Agent, "ENTER",
                                                     findScene(Agent).RegionInfo.RegionHandle);

                ChatterBoxSessionStartReplyMessage cs = new ChatterBoxSessionStartReplyMessage
                                                            {
                                                                VoiceEnabled = true,
                                                                TempSessionID = UUID.Random(),
                                                                Type = 1,
                                                                Success = true,
                                                                SessionID = sessionid,
                                                                SessionName = SP.Name + " Conference",
                                                                ModeratedVoice = true
                                                            };

                return cs.Serialize().ToString();
            }
            else if (method == "accept invitation")
            {
                //They would like added to the group conversation
                List<ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock> Us =
                    new List<ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock>();
                List<ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock> NotUsAgents =
                    new List<ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock>();

                ChatSession session = GetSession(sessionid);
                if (session != null)
                {
                    ChatSessionMember thismember = FindMember(sessionid, Agent);
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
                            Us.Add(block);
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
                        eq.ChatterBoxSessionAgentListUpdates(session.SessionID,
                                                             member.AvatarKey == thismember.AvatarKey
                                                                 ? NotUsAgents.ToArray()
                                                                 : Us.ToArray(),
                                                             member.AvatarKey, "ENTER",
                                                             findScene(Agent).RegionInfo.RegionHandle);
                    }
                    return "Accepted";
                }
                else
                    return ""; //not this type of session
            }
            else if (method == "mute update")
            {
                //Check if the user is a moderator
                if (!CheckModeratorPermission(Agent, sessionid))
                {
                    return "";
                }

                OSDMap parameters = (OSDMap) rm["params"];
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

                // Send an update to the affected user
                eq.ChatterBoxSessionAgentListUpdates(sessionid, new[] {block}, AgentID, "",
                                                     findScene(Agent).RegionInfo.RegionHandle);

                return "Accepted";
            }
            else
            {
                MainConsole.Instance.Warn("ChatSessionRequest : " + method);
                return "";
            }
        }

        private IScene findScene(UUID agentID)
        {
            return (from scene in m_scenes let SP = scene.GetScenePresence(agentID) where SP != null && !SP.IsChildAgent select scene).FirstOrDefault();
        }

        /// <summary>
        ///   Find the presence from all the known sims
        /// </summary>
        /// <param name = "avID"></param>
        /// <returns></returns>
        public IScenePresence findScenePresence(UUID avID)
        {
#if (!ISWIN)
            foreach (IScene s in m_scenes)
            {
                IScenePresence SP = s.GetScenePresence(avID);
                if (SP != null)
                {
                    return SP;
                }
            }
            return null;
#else
            return m_scenes.Select(s => s.GetScenePresence(avID)).FirstOrDefault(SP => SP != null);
#endif
        }

        private void OnGridInstantMessage(GridInstantMessage msg)
        {
            OnInstantMessage(findScenePresence(msg.toAgentID).ControllingClient, msg);
        }

        /// <summary>
        ///   If its a message we deal with, pull it from the client here
        /// </summary>
        /// <param name = "client"></param>
        /// <param name = "im"></param>
        public void OnInstantMessage(IClientAPI client, GridInstantMessage im)
        {
            byte dialog = im.dialog;
            //We only deal with friend IM sessions here, groups module handles group IM sessions
            if (dialog == (byte) InstantMessageDialog.SessionSend)
                SendChatToSession(client, im);

            if (dialog == (byte) InstantMessageDialog.SessionDrop)
                DropMemberFromSession(client, im);
        }

        /// <summary>
        ///   Find the member from X sessionID
        /// </summary>
        /// <param name = "sessionid"></param>
        /// <param name = "Agent"></param>
        /// <returns></returns>
        private ChatSessionMember FindMember(UUID sessionid, UUID Agent)
        {
            ChatSession session;
            ChatSessions.TryGetValue(sessionid, out session);
            if (session == null)
                return null;
            ChatSessionMember thismember = new ChatSessionMember {AvatarKey = UUID.Zero};
#if (!ISWIN)
            foreach (ChatSessionMember testmember in session.Members)
            {
                if (testmember.AvatarKey == Agent)
                {
                    thismember = testmember;
                }
            }
#else
            foreach (ChatSessionMember testmember in session.Members.Where(testmember => testmember.AvatarKey == Agent))
            {
                thismember = testmember;
            }
#endif
            return thismember;
        }

        /// <summary>
        ///   Check whether the user has moderator permissions
        /// </summary>
        /// <param name = "Agent"></param>
        /// <param name = "sessionid"></param>
        /// <returns></returns>
        public bool CheckModeratorPermission(UUID Agent, UUID sessionid)
        {
            ChatSession session;
            ChatSessions.TryGetValue(sessionid, out session);
            if (session == null)
                return false;
            ChatSessionMember thismember = new ChatSessionMember {AvatarKey = UUID.Zero};
#if (!ISWIN)
            foreach (ChatSessionMember testmember in session.Members)
            {
                if (testmember.AvatarKey == Agent)
                {
                    thismember = testmember;
                }
            }
#else
             foreach (ChatSessionMember testmember in session.Members.Where(testmember => testmember.AvatarKey == Agent))
            {
                thismember = testmember;
            }
#endif
            if (thismember == null)
                return false;
            return thismember.IsModerator;
        }

        /// <summary>
        ///   Remove the member from this session
        /// </summary>
        /// <param name = "client"></param>
        /// <param name = "im"></param>
        public void DropMemberFromSession(IClientAPI client, GridInstantMessage im)
        {
            ChatSession session;
            ChatSessions.TryGetValue(im.imSessionID, out session);
            if (session == null)
                return;
            ChatSessionMember member = new ChatSessionMember {AvatarKey = UUID.Zero};
#if (!ISWIN)
            foreach (ChatSessionMember testmember in session.Members)
            {
                if (testmember.AvatarKey == im.fromAgentID)
                {
                    member = testmember;
                }
            }
#else
            foreach (ChatSessionMember testmember in session.Members.Where(testmember => testmember.AvatarKey == im.fromAgentID))
            {
                member = testmember;
            }
#endif

            if (member.AvatarKey != UUID.Zero)
                session.Members.Remove(member);

            if (session.Members.Count == 0)
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
            IEventQueueService eq = client.Scene.RequestModuleInterface<IEventQueueService>();
            foreach (ChatSessionMember sessionMember in session.Members)
            {
                eq.ChatterBoxSessionAgentListUpdates(session.SessionID, new[] {block}, sessionMember.AvatarKey, "LEAVE",
                                                     findScene(sessionMember.AvatarKey).RegionInfo.RegionHandle);
            }
        }

        /// <summary>
        ///   Send chat to all the members of this friend conference
        /// </summary>
        /// <param name = "client"></param>
        /// <param name = "im"></param>
        public void SendChatToSession(IClientAPI client, GridInstantMessage im)
        {
            ChatSession session;
            ChatSessions.TryGetValue(im.imSessionID, out session);
            if (session == null)
                return;
            IEventQueueService eq = client.Scene.RequestModuleInterface<IEventQueueService>();
            foreach (ChatSessionMember member in session.Members)
            {
                if (member.HasBeenAdded)
                {
                    im.toAgentID = member.AvatarKey;
                    im.binaryBucket = Utils.StringToBytes(session.Name);
                    im.RegionID = UUID.Zero;
                    im.ParentEstateID = 0;
                    //im.timestamp = 0;
                    m_TransferModule.SendInstantMessage(im);
                }
                else
                {
                    im.toAgentID = member.AvatarKey;
                    eq.ChatterboxInvitation(
                        session.SessionID
                        , session.Name
                        , im.fromAgentID
                        , im.message
                        , im.toAgentID
                        , im.fromAgentName
                        , im.dialog
                        , im.timestamp
                        , im.offline == 1
                        , (int) im.ParentEstateID
                        , im.Position
                        , 1
                        , im.imSessionID
                        , false
                        , Utils.StringToBytes(session.Name)
                        , findScene(member.AvatarKey).RegionInfo.RegionHandle
                        );
                }
            }
        }

        /// <summary>
        ///   Add this member to the friend conference
        /// </summary>
        /// <param name = "member"></param>
        /// <param name = "SessionID"></param>
        public void AddMemberToGroup(ChatSessionMember member, UUID SessionID)
        {
            ChatSession session;
            ChatSessions.TryGetValue(SessionID, out session);
            session.Members.Add(member);
        }

        /// <summary>
        ///   Create a new friend conference session
        /// </summary>
        /// <param name = "session"></param>
        public void CreateSession(ChatSession session)
        {
            ChatSessions.Add(session.SessionID, session);
        }

        /// <summary>
        ///   Get a session by a user's sessionID
        /// </summary>
        /// <param name = "SessionID"></param>
        /// <returns></returns>
        public ChatSession GetSession(UUID SessionID)
        {
            ChatSession session;
            ChatSessions.TryGetValue(SessionID, out session);
            return session;
        }

        /// <summary>
        ///   Add the agent to the in-memory session lists and give them the default permissions
        /// </summary>
        /// <param name = "AgentID"></param>
        /// <param name = "SessionID"></param>
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
    }
}