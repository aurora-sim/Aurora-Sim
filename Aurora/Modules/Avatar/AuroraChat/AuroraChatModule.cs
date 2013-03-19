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
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aurora.Modules.Chat
{
    public class AuroraChatModule : INonSharedRegionModule, IChatModule, IMuteListModule
    {
        private const int DEBUG_CHANNEL = 2147483647;
        private const int DEFAULT_CHANNEL = 0;
        private readonly Dictionary<UUID, ChatSession> ChatSessions = new Dictionary<UUID, ChatSession>();
        private readonly Dictionary<UUID, MuteList[]> MuteListCache = new Dictionary<UUID, MuteList[]>();
        private IScene m_Scene;

        private IMuteListConnector MuteListConnector;
        private IInstantMessagingService m_imService;
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
        ///     Send the message from the prim to the avatars in the regions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="c"></param>
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
        ///     Say this message directly to a single person
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <param name="channel"></param>
        /// <param name="fromPos"></param>
        /// <param name="fromName"></param>
        /// <param name="fromAgentID"></param>
        /// <param name="fromAgent"></param>
        /// <param name="toAgentID"></param>
        /// <param name="scene"></param>
        public void SimChatBroadcast(string message, ChatTypeEnum type, int channel, Vector3 fromPos, string fromName,
                                     UUID fromAgentID, bool fromAgent, UUID toAgentID, IScene scene)
        {
            SimChat(message, type, channel, fromPos, fromName, fromAgentID, fromAgent, true, -1, toAgentID, scene);
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

            foreach (IScenePresence presence in from presence in m_Scene.GetScenePresences()
                                                where !presence.IsChildAgent
                                                let fromRegionPos = fromPos + regionPos
                                                let toRegionPos = presence.AbsolutePosition +
                                                                  new Vector3(presence.Scene.RegionInfo.RegionLocX,
                                                                              presence.Scene.RegionInfo.RegionLocY, 0)
                                                let dis = (int) Util.GetDistanceTo(toRegionPos, fromRegionPos)
                                                where
                                                    (c.Type != ChatTypeEnum.Whisper || dis <= m_whisperdistance) &&
                                                    (c.Type != ChatTypeEnum.Say || dis <= m_saydistance) &&
                                                    (c.Type != ChatTypeEnum.Shout || dis <= m_shoutdistance) &&
                                                    (c.Type != ChatTypeEnum.Custom || dis <= c.Range)
                                                where
                                                    sourceType != ChatSourceType.Agent || avatar == null ||
                                                    avatar.CurrentParcel == null ||
                                                    (avatar.CurrentParcelUUID == presence.CurrentParcelUUID ||
                                                     (!avatar.CurrentParcel.LandData.Private &&
                                                      !presence.CurrentParcel.LandData.Private))
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
        ///     Get all the mutes from the database
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
            lock (MuteListCache)
            {
                if (!MuteListCache.TryGetValue(AgentID, out List))
                {
                    List = MuteListConnector.GetMuteList(AgentID).ToArray();
                    MuteListCache.Add(AgentID, List);
                }
                else
                    Cached = true;
            }

            return List;
        }

        private void UpdateCachedInfo(UUID agentID, CachedUserInfo info)
        {
            lock (MuteListCache)
                MuteListCache[agentID] = info.MuteList.ToArray();
        }

        /// <summary>
        ///     Update the mute in the database
        /// </summary>
        /// <param name="MuteID"></param>
        /// <param name="Name"></param>
        /// <param name="Flags"></param>
        /// <param name="AgentID"></param>
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
            lock (MuteListCache)
                MuteListCache.Remove(AgentID);
        }

        /// <summary>
        ///     Remove the given mute from the user's mute list in the database
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
                lock (MuteListCache)
                    MuteListCache.Remove(AgentID);
            }
        }

        #endregion

        #region INonSharedRegionModule Members

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

            m_Scene = scene;
            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
            scene.EventManager.OnCachedUserInfo += UpdateCachedInfo;

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
                MuteListConnector = Framework.Utilities.DataManager.RequestPlugin<IMuteListConnector>();

            m_imService = scene.RequestModuleInterface<IInstantMessagingService>();
        }

        public virtual void RemoveRegion(IScene scene)
        {
            if (!m_enabled)
                return;

            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
            scene.EventManager.OnCachedUserInfo -= UpdateCachedInfo;

            m_Scene = null;
            scene.UnregisterModuleInterface<IMuteListModule>(this);
            scene.UnregisterModuleInterface<IChatModule>(this);
        }

        public virtual void Close()
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
        ///     Set the correct position for the chat message
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        protected OSChatMessage FixPositionOfChatMessage(OSChatMessage c)
        {
            IScenePresence avatar;
            if ((avatar = c.Scene.GetScenePresence(c.Sender.AgentId)) != null)
                c.Position = avatar.AbsolutePosition;

            return c;
        }

        /// <summary>
        ///     New chat message from the client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="c"></param>
        protected virtual void OnChatFromClient(IClientAPI sender, OSChatMessage c)
        {
            c = FixPositionOfChatMessage(c);

            // redistribute to interested subscribers
            if (c.Message != "")
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
                foreach (
                    string pluginMain in
                        ChatPlugins.Keys.Where(
                            pluginMain => pluginMain == "all" || c.Message.StartsWith(pluginMain + ".")))
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
                        foreach (MuteList m in mutes)
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
        ///     Get all the mutes the client has set
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
            /*if (cached)
            {
                client.SendUseCachedMuteList();
                return;
            }*/

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
        ///     Update the mute (from the client)
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
        ///     Remove the mute (from the client)
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
        ///     Find the presence from all the known sims
        /// </summary>
        /// <param name="avID"></param>
        /// <returns></returns>
        public IScenePresence findScenePresence(UUID avID)
        {
            return m_Scene.GetScenePresence(avID);
        }

        /// <summary>
        ///     If its a message we deal with, pull it from the client here
        /// </summary>
        /// <param name="client"></param>
        /// <param name="im"></param>
        private void OnInstantMessage(IClientAPI client, GridInstantMessage im)
        {
            byte dialog = im.dialog;
            switch (dialog)
            {
                case (byte) InstantMessageDialog.SessionGroupStart:
                    m_imService.CreateGroupChat(client.AgentId, im);
                    break;
                case (byte) InstantMessageDialog.SessionSend:
                    m_imService.SendChatToSession(client.AgentId, im);
                    break;
                case (byte) InstantMessageDialog.SessionDrop:
                    m_imService.DropMemberFromSession(client.AgentId, im);
                    break;
            }
        }
    }
}