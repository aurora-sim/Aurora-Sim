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
using Caps = OpenSim.Framework.Capabilities.Caps;
using Mono.Addins;

namespace Aurora.Modules
{
    public class AuroraChatModule : ISharedRegionModule, IChatModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const int DEBUG_CHANNEL = 2147483647;

        private bool m_enabled = true;
        private bool m_useMuteListModule = true;
        private int m_saydistance = 30;
        private int m_shoutdistance = 100;
        private int m_whisperdistance = 10;

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

        public List<Scene> Scenes
        {
            get { return m_scenes; }
        }

        private List<Scene> m_scenes = new List<Scene>();

        private IMuteListConnector MuteListConnector;
        internal IConfig m_config;

        private IMessageTransferModule m_TransferModule = null;

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

            m_useMuteListModule = (config.Configs["Messaging"].GetString("MuteListModule", "AuroraChatModule") == "AuroraChatModule");

            FindChatPlugins();
        }

        private void FindChatPlugins()
        {
            AllChatPlugins = Aurora.Framework.AuroraModuleLoader.LoadPlugins<IChatPlugin>("/Aurora/ChatPlugins", new AuroraChatPluginInitialiser(this));
        }

        private class AuroraChatPluginInitialiser : PluginInitialiserBase
        {
            IChatModule chatModule;
            public AuroraChatPluginInitialiser(IChatModule module)
            {
                chatModule = module;
            }
            public override void Initialise(IPlugin plugin)
            {
                IChatPlugin chatPlugin = plugin as IChatPlugin;
                chatPlugin.Initialize(chatModule);
            }
        }

        public virtual void AddRegion(Scene scene)
        {
            if (!m_enabled) return;

            if (!m_scenes.Contains(scene))
            {
                m_scenes.Add(scene);
                scene.EventManager.OnNewClient += OnNewClient;
                scene.EventManager.OnClientClosed += OnClientClosed;
                scene.EventManager.OnChatFromWorld += OnChatFromWorld;
                scene.EventManager.OnChatBroadcast += OnChatBroadcast;
                scene.EventManager.OnRegisterCaps += RegisterCaps;
                scene.EventManager.OnClientConnect += OnClientConnect;
            }
            //m_log.InfoFormat("[CHAT]: Initialized for {0} w:{1} s:{2} S:{3}", scene.RegionInfo.RegionName,
            //                 m_whisperdistance, m_saydistance, m_shoutdistance);
        }

        public virtual void RegionLoaded(Scene scene)
        {
            if(m_useMuteListModule)
                MuteListConnector = Aurora.DataManager.DataManager.RequestPlugin<IMuteListConnector>("IMuteListConnector");

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
                scene.EventManager.OnClientClosed -= OnClientClosed;
                scene.EventManager.OnChatFromWorld -= OnChatFromWorld;
                scene.EventManager.OnChatBroadcast -= OnChatBroadcast;
                scene.EventManager.OnRegisterCaps -= RegisterCaps;
                scene.EventManager.OnClientConnect -= OnClientConnect;
                m_scenes.Remove(scene);
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

        public virtual void OnNewClient(IClientAPI client)
        {
            client.OnChatFromClient += OnChatFromClient;
            client.OnMuteListRequest += OnMuteListRequest;
            client.OnUpdateMuteListEntry += OnMuteListUpdate;
            client.OnRemoveMuteListEntry += OnMuteListRemove;

            foreach (IChatPlugin plugin in AllChatPlugins)
            {
                plugin.OnNewClient(client);
            }
        }

        public virtual void OnClientClosed(UUID clientID, Scene scene)
        {
            foreach (IChatPlugin plugin in AllChatPlugins)
            {
                plugin.OnClosingClient(clientID, scene);
            }
        }

        protected OSChatMessage FixPositionOfChatMessage(OSChatMessage c)
        {
            ScenePresence avatar;
            Scene scene = (Scene)c.Scene;
            if ((avatar = scene.GetScenePresence(c.Sender.AgentId)) != null)
                c.Position = avatar.AbsolutePosition;

            return c;
        }

        public virtual void OnChatFromClient(Object sender, OSChatMessage c)
        {
            c = FixPositionOfChatMessage(c);

            // redistribute to interested subscribers
            Scene scene = (Scene)c.Scene;
            scene.EventManager.TriggerOnChatFromClient(sender, c);

            // early return if not on public or debug channel
            if (c.Channel != 0 && c.Channel != DEBUG_CHANNEL) return;

            // sanity check:
            if (c.Sender == null)
            {
                m_log.ErrorFormat("[CHAT] OnChatFromClient from {0} has empty Sender field!", sender);
                return;
            }

            if (c.Message != "")
            {
                foreach (string pluginMain in ChatPlugins.Keys)
                {
                    if (pluginMain == "all" || c.Message.StartsWith(pluginMain + "."))
                    {
                        IChatPlugin plugin;
                        ChatPlugins.TryGetValue(pluginMain, out plugin);
                        if (!plugin.OnNewChatMessageFromWorld(c, out c))
                            return;
                    }
                }
            }

            DeliverChatToAvatars(ChatSourceType.Agent, c);
        }

        public virtual void OnChatFromWorld(Object sender, OSChatMessage c)
        {
            // early return if not on public or debug channel
            if (c.Channel != 0 && c.Channel != DEBUG_CHANNEL) return;

            bool Sent = false;

            if (c.Type == ChatTypeEnum.Say)
            {
                if (c.Position.X + m_saydistance > 256)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX + 1, c.Scene.RegionInfo.RegionLocY);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X -= Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.Y + m_saydistance > 256)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX, c.Scene.RegionInfo.RegionLocY + 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.Y -= Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X + m_saydistance > 256 && c.Position.Y + m_saydistance > 256)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX + 1, c.Scene.RegionInfo.RegionLocY + 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X -= Constants.RegionSize;
                        Position.Y -= Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X + m_saydistance > 256 && c.Position.Y - m_saydistance < 0)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX + 1, c.Scene.RegionInfo.RegionLocY - 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X -= Constants.RegionSize;
                        Position.Y += Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.Y - m_saydistance < 0)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX, c.Scene.RegionInfo.RegionLocY - 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.Y += Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X - m_saydistance < 0 && c.Position.Y - m_saydistance < 0)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX - 1, c.Scene.RegionInfo.RegionLocY - 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X += Constants.RegionSize;
                        Position.Y += Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X - m_saydistance < 0)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX - 1, c.Scene.RegionInfo.RegionLocY);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X += Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X - m_saydistance < 0 && c.Position.Y + m_saydistance > 256)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX - 1, c.Scene.RegionInfo.RegionLocY + 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X += Constants.RegionSize;
                        Position.Y -= Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
            }
            if (c.Type == ChatTypeEnum.Whisper)
            {
                if (c.Position.X + m_whisperdistance > 256)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX + 1, c.Scene.RegionInfo.RegionLocY);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X -= Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.Y + m_whisperdistance > 256)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX, c.Scene.RegionInfo.RegionLocY + 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.Y -= Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X + m_whisperdistance > 256 && c.Position.Y + m_whisperdistance > 256)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX + 1, c.Scene.RegionInfo.RegionLocY + 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X -= Constants.RegionSize;
                        Position.Y -= Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X + m_whisperdistance > 256 && c.Position.Y - m_whisperdistance < 0)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX + 1, c.Scene.RegionInfo.RegionLocY - 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X -= Constants.RegionSize;
                        Position.Y += Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.Y - m_whisperdistance < 0)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX, c.Scene.RegionInfo.RegionLocY - 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.Y += Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X - m_whisperdistance < 0 && c.Position.Y - m_whisperdistance < 0)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX - 1, c.Scene.RegionInfo.RegionLocY - 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X += Constants.RegionSize;
                        Position.Y += Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X - m_whisperdistance < 0)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX - 1, c.Scene.RegionInfo.RegionLocY);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X += Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X - m_whisperdistance < 0 && c.Position.Y + m_whisperdistance > 256)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX - 1, c.Scene.RegionInfo.RegionLocY + 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X += Constants.RegionSize;
                        Position.Y -= Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
            }
            if (c.Type == ChatTypeEnum.Shout)
            {
                if (c.Position.X + m_shoutdistance > 256)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX + 1, c.Scene.RegionInfo.RegionLocY);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X -= Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.Y + m_shoutdistance > 256)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX, c.Scene.RegionInfo.RegionLocY + 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.Y -= Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X + m_shoutdistance > 256 && c.Position.Y + m_shoutdistance > 256)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX + 1, c.Scene.RegionInfo.RegionLocY + 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X -= Constants.RegionSize;
                        Position.Y -= Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X + m_shoutdistance > 256 && c.Position.Y - m_shoutdistance < 0)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX + 1, c.Scene.RegionInfo.RegionLocY - 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X -= Constants.RegionSize;
                        Position.Y += Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.Y - m_shoutdistance < 0)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX, c.Scene.RegionInfo.RegionLocY - 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.Y += Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X - m_shoutdistance < 0 && c.Position.Y - m_shoutdistance < 0)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX - 1, c.Scene.RegionInfo.RegionLocY - 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X += Constants.RegionSize;
                        Position.Y += Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X - m_shoutdistance < 0)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX - 1, c.Scene.RegionInfo.RegionLocY);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X += Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
                if (c.Position.X - m_shoutdistance < 0 && c.Position.Y + m_shoutdistance > 256)
                {
                    Scene scene = FindScene(c.Scene.RegionInfo.RegionLocX - 1, c.Scene.RegionInfo.RegionLocY + 1);
                    if (scene != null)
                    {
                        OSChatMessage newC = c.Copy();
                        newC.Scene = scene;
                        Vector3 Position = newC.Position;
                        Position.X += Constants.RegionSize;
                        Position.Y -= Constants.RegionSize;
                        newC.Position = Position;
                        DeliverChatToAvatars(ChatSourceType.Object, newC);
                        Sent = true;
                    }
                }
            }
            if(!Sent)
            DeliverChatToAvatars(ChatSourceType.Object, c);
        }

        private Scene FindScene(uint LocX, uint LocY)
        {
            foreach (Scene scene in m_scenes)
            {
                if (scene.RegionInfo.RegionLocX == LocX &&
                    scene.RegionInfo.RegionLocY == LocY)
                {
                    return scene;
                }
            }
            return null;
        }

        protected virtual void DeliverChatToAvatars(ChatSourceType sourceType, OSChatMessage c)
        {
            string fromName = c.From;
            UUID fromID = UUID.Zero;
            string message = c.Message;
            IScene scene = c.Scene;
            Vector3 fromPos = c.Position;
            Vector3 regionPos = new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                            scene.RegionInfo.RegionLocY * Constants.RegionSize, 0);

            if (c.Channel == DEBUG_CHANNEL) c.Type = ChatTypeEnum.DebugChannel;

            switch (sourceType)
            {
                case ChatSourceType.Agent:
                    if (!(scene is Scene))
                    {
                        m_log.WarnFormat("[CHAT]: scene {0} is not a Scene object, cannot obtain scene presence for {1}",
                                         scene.RegionInfo.RegionName, c.Sender.AgentId);
                        return;
                    }
                    ScenePresence avatar = (scene as Scene).GetScenePresence(c.Sender.AgentId);
                    fromPos = avatar.AbsolutePosition;
                    fromName = avatar.Name;
                    fromID = c.Sender.AgentId;
                    //Always send this so it fires on typing start and end
                    avatar.SendScriptEventToAttachments("changed", new object[] { Changed.STATE });

                    break;

                case ChatSourceType.Object:
                    fromID = c.SenderUUID;

                    break;
            }

            // TODO: iterate over message
            if (message.Length >= 1000) // libomv limit
                message = message.Substring(0, 1000);

            // m_log.DebugFormat("[CHAT]: DCTA: fromID {0} fromName {1}, cType {2}, sType {3}", fromID, fromName, c.Type, sourceType);

            foreach (Scene s in m_scenes)
            {
                s.ForEachScenePresence(
                    delegate(ScenePresence presence)
                    {
                        // don't send stuff to child agents
                        if (!presence.IsChildAgent)
                        {
                            bool IsMuted = false;
                            if (message != "" && m_useMuteListModule)
                            {
                                lock (IsMutedCache)
                                {
                                    if (IsMutedCache.ContainsKey(presence.UUID))
                                    {
                                        Dictionary<UUID, bool> cache;
                                        IsMutedCache.TryGetValue(presence.UUID, out cache);
                                        if (cache.ContainsKey(fromID))
                                        {
                                            cache.TryGetValue(fromID, out IsMuted);
                                        }
                                        else
                                        {
                                            IsMuted = MuteListConnector.IsMuted(presence.UUID, fromID);
                                            cache.Add(fromID, IsMuted);
                                            IsMutedCache.Remove(presence.UUID);
                                            IsMutedCache.Add(presence.UUID, cache);
                                        }
                                    }
                                    else
                                    {
                                        Dictionary<UUID, bool> cache = new Dictionary<UUID, bool>();
                                        IsMuted = MuteListConnector.IsMuted(presence.UUID, fromID);
                                        cache.Add(fromID, IsMuted);
                                        IsMutedCache.Add(presence.UUID, cache);
                                    }
                                }
                            }
                            if (!IsMuted)
                                TrySendChatMessage(presence, fromPos, regionPos, fromID, fromName, c.Type, message, sourceType);
                        }
                    }
                );
            }
        }

        static private Vector3 CenterOfRegion = new Vector3(Constants.RegionSize, Constants.RegionSize, 30);

        public virtual void OnChatBroadcast(Object sender, OSChatMessage c)
        {
            // unless the chat to be broadcast is of type Region, we
            // drop it if its channel is neither 0 nor DEBUG_CHANNEL
            if (c.Channel != 0 && c.Channel != DEBUG_CHANNEL && c.Type != ChatTypeEnum.Region) return;

            ChatTypeEnum cType = c.Type;
            if (c.Channel == DEBUG_CHANNEL)
                cType = ChatTypeEnum.DebugChannel;

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

                    client.SendChatMessage(c.Message, (byte)cType, CenterOfRegion, fromName, fromID,
                                           (byte)sourceType, (byte)ChatAudibleLevel.Fully);
                });
        }


        public virtual void TrySendChatMessage(ScenePresence presence, Vector3 fromPos, Vector3 regionPos,
                                                  UUID fromAgentID, string fromName, ChatTypeEnum type,
                                                  string message, ChatSourceType src)
        {
            // don't send stuff to child agents
            if (presence.IsChildAgent) return;

            Vector3 fromRegionPos = fromPos + regionPos;
            Vector3 toRegionPos = presence.AbsolutePosition +
                new Vector3(presence.Scene.RegionInfo.RegionLocX * Constants.RegionSize,
                            presence.Scene.RegionInfo.RegionLocY * Constants.RegionSize, 0);

            int dis = (int)Util.GetDistanceTo(toRegionPos, fromRegionPos);

            if (type == ChatTypeEnum.Whisper && dis > m_whisperdistance ||
                type == ChatTypeEnum.Say && dis > m_saydistance ||
                type == ChatTypeEnum.Shout && dis > m_shoutdistance)
            {
                return;
            }

            // TODO: should change so the message is sent through the avatar rather than direct to the ClientView
            presence.ControllingClient.SendChatMessage(message, (byte)type, fromPos, fromName,
                                                       fromAgentID, (byte)src, (byte)ChatAudibleLevel.Fully);
        }

        private Dictionary<UUID, Dictionary<UUID, bool>> IsMutedCache = new Dictionary<UUID, Dictionary<UUID, bool>>();
        private Dictionary<UUID, MuteList[]> MuteListCache = new Dictionary<UUID, MuteList[]>();
        private void OnMuteListRequest(IClientAPI client, uint crc)
        {
            if (!m_useMuteListModule)
                return;
            //Sends the name of the file being sent by the xfer module DO NOT EDIT!!!
            string filename = "mutes" + client.AgentId.ToString();
            byte[] fileData = new byte[0];
            string invString = "";
            int i = 0;
            MuteList[] List;

            if (!MuteListCache.ContainsKey(client.AgentId))
                List = MuteListConnector.GetMuteList(client.AgentId);
            else
            {
                client.SendUseCachedMuteList();
                return;
            }

            foreach (MuteList mute in List)
            {
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
        
        private void OnMuteListUpdate(IClientAPI client, UUID MuteID, string Name, int Flags, UUID AgentID)
        {
            if (!m_useMuteListModule)
                return;
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
            OnMuteListRequest(client, 0);
        }

        private void OnMuteListRemove(IClientAPI client, UUID MuteID, string Name, UUID AgentID)    
        {
            if (!m_useMuteListModule)
                return;
            //Gets sent if a mute is not selected.
            if (MuteID != UUID.Zero)
            {
                MuteListConnector.DeleteMute(MuteID, AgentID);
                MuteListCache.Remove(AgentID);
            }
            OnMuteListRequest(client, 0);
        }

        private string m_chatSessionRequestPath = "0009/";
        private Dictionary<UUID, ChatSession> ChatSessions = new Dictionary<UUID, ChatSession>();

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
        
        public void RegisterCaps(UUID agentID, Caps caps)
        {
            string capsBase = "/CAPS/" + caps.CapsObjectPath;

            caps.RegisterHandler("ChatSessionRequest",
                                new RestHTTPHandler("POST", capsBase + m_chatSessionRequestPath,
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
            IEventQueue eq = SP.Scene.RequestModuleInterface<IEventQueue>();

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
                eq.ChatterBoxSessionAgentListUpdates(sessionid, new OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock[] { block }, Agent, "ENTER");

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
                List<OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock> Us = new List<OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock>();
                List<OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock> NotUsAgents = new List<OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock>();

                ChatSession session = GetSession(sessionid);
                ChatSessionMember thismember = FindMember(sessionid, Agent);
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
                        if(sessionMember.HasBeenAdded) // Don't add not joined yet agents. They don't watn to be here.
                            NotUsAgents.Add(block);
                    }
                }
                thismember.HasBeenAdded = true;
                foreach (ChatSessionMember member in session.Members)
                {
                    if (member.AvatarKey == thismember.AvatarKey)
                    {
                        eq.ChatterBoxSessionAgentListUpdates(session.SessionID, NotUsAgents.ToArray(), member.AvatarKey, "ENTER");
                    }
                    else
                    {
                        eq.ChatterBoxSessionAgentListUpdates(session.SessionID, Us.ToArray(), member.AvatarKey, "ENTER");
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
                eq.ChatterBoxSessionAgentListUpdates(sessionid, new OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock[] { block }, AgentID, "");
                
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

        void OnClientConnect(IClientCore client)
        {
            IClientIM clientIM;
            if (client.TryGet(out clientIM))
            {
                clientIM.OnInstantMessage += OnInstantMessage;
            }
        }

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

        public void OnInstantMessage(IClientAPI client, GridInstantMessage im)
        {
            byte dialog = im.dialog;
            //We only deal with friend IM sessions here, groups module handles group IM sessions
            if (dialog == (byte)InstantMessageDialog.SessionSend)
                SendChatToSession(client, im);

            if (dialog == (byte)InstantMessageDialog.SessionDrop)
                DropMemberFromSession(client, im);
        }

        private ChatSessionMember FindMember(UUID sessionid, UUID Agent)
        {
            ChatSession session;
            ChatSessions.TryGetValue(sessionid, out session);
            ChatSessionMember thismember = new ChatSessionMember() { AvatarKey = UUID.Zero };
            foreach (ChatSessionMember testmember in session.Members)
            {
                if (testmember.AvatarKey == Agent)
                    thismember = testmember;
            }
            return thismember;
        }

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
            IEventQueue eq = client.Scene.RequestModuleInterface<IEventQueue>();
            foreach (ChatSessionMember sessionMember in session.Members)
            {
                eq.ChatterBoxSessionAgentListUpdates(session.SessionID, new OpenMetaverse.Messages.Linden.ChatterBoxSessionAgentListUpdatesMessage.AgentUpdatesBlock[] { block }, sessionMember.AvatarKey, "LEAVE");
            }
        }

        public void SendChatToSession(IClientAPI client, GridInstantMessage im)
        {
            ChatSession session;
            ChatSessions.TryGetValue(UUID.Parse(im.imSessionID.ToString()), out session);
            IEventQueue eq = client.Scene.RequestModuleInterface<IEventQueue>();
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
                        );
                }
            }
        }

        public void AddMemberToGroup(ChatSessionMember member, UUID SessionID)
        {
            ChatSession session;
            ChatSessions.TryGetValue(SessionID, out session);
            session.Members.Add(member);
        }

        public void CreateSession(ChatSession session)
        {
            ChatSessions.Add(session.SessionID, session);
        }

        public ChatSession GetSession(UUID SessionID)
        {
            ChatSession session;
            ChatSessions.TryGetValue(SessionID, out session);
            return session;
        }

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
