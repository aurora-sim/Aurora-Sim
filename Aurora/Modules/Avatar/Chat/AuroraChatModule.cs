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
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using log4net;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Modules
{
    public class AuroraChatModule : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const int DEBUG_CHANNEL = 2147483647;

        private bool m_enabled = true;
        private int m_saydistance = 30;
        private int m_shoutdistance = 100;
        private int m_whisperdistance = 10;
        private bool m_useAuth = true;
        private bool m_blockChat = false;
        private List<UUID> m_authList = new List<UUID>();
        private List<UUID> m_authorizedSpeakers = new List<UUID>();
        private List<Scene> m_scenes = new List<Scene>();

        private IGenericData GenericData = null;
        internal object m_syncy = new object();
        private IMuteListConnector MuteListConnector;
        private bool m_indicategod;
        private string m_godPrefix;

        internal IConfig m_config;

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

            m_useAuth = m_config.GetBoolean("use_Auth", true);
            m_indicategod = m_config.GetBoolean("indicate_god", true);
            m_godPrefix = m_config.GetString("godPrefix", "");
            m_whisperdistance = m_config.GetInt("whisper_distance", m_whisperdistance);
            m_saydistance = m_config.GetInt("say_distance", m_saydistance);
            m_shoutdistance = m_config.GetInt("shout_distance", m_shoutdistance);
        }

        public virtual void AddRegion(Scene scene)
        {
            if (!m_enabled) return;

            lock (m_syncy)
            {
                m_authList.Add(scene.RegionInfo.EstateSettings.EstateOwner);
                if (!m_scenes.Contains(scene))
                {
                    m_scenes.Add(scene);
                    scene.EventManager.OnNewClient += OnNewClient;
                    scene.EventManager.OnChatFromWorld += OnChatFromWorld;
                    scene.EventManager.OnChatBroadcast += OnChatBroadcast;
                }
            }

            //m_log.InfoFormat("[CHAT]: Initialized for {0} w:{1} s:{2} S:{3}", scene.RegionInfo.RegionName,
            //                 m_whisperdistance, m_saydistance, m_shoutdistance);
        }

        public virtual void RegionLoaded(Scene scene)
        {
            GenericData = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
            MuteListConnector = Aurora.DataManager.DataManager.IMuteListConnector;
        }

        public virtual void RemoveRegion(Scene scene)
        {
            if (!m_enabled) return;

            lock (m_syncy)
            {
                if (m_scenes.Contains(scene))
                {
                    scene.EventManager.OnNewClient -= OnNewClient;
                    scene.EventManager.OnChatFromWorld -= OnChatFromWorld;
                    scene.EventManager.OnChatBroadcast -= OnChatBroadcast;
                    m_scenes.Remove(scene);
                }
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
            if (!m_blockChat)
            {
                if (!m_authorizedSpeakers.Contains(client.AgentId))
                    m_authorizedSpeakers.Add(client.AgentId);
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

            ScenePresence SP = m_scenes[0].GetScenePresence(c.SenderUUID);
            //Always allow gods to do what they want
            if(SP.GodLevel != 0 && !!m_authorizedSpeakers.Contains(c.SenderUUID))
                m_authorizedSpeakers.Add(c.SenderUUID);

            if (SP.GodLevel != 0 && !!m_authList.Contains(c.SenderUUID))
                m_authList.Add(c.SenderUUID);
            
            if (!m_authorizedSpeakers.Contains(c.SenderUUID))
                return;

            if (c.Message.StartsWith("Chat."))
            {
                if (!m_useAuth || m_authList.Contains(c.SenderUUID))
                {
                    ScenePresence senderSP;
                    ((Scene)c.Scene).TryGetScenePresence(c.SenderUUID, out senderSP);
                    string[] message = c.Message.Split('.');
                    if (message[1] == "SayDistance")
                    {
                        m_saydistance = Convert.ToInt32(message[2]);
                        TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[1] + " changed.", ChatSourceType.System);
                    }
                    if (message[1] == "WhisperDistance")
                    {
                        m_whisperdistance = Convert.ToInt32(message[2]);
                        TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[1] + " changed.", ChatSourceType.System);
                    }
                    if (message[1] == "ShoutDistance")
                    {
                        m_shoutdistance = Convert.ToInt32(message[2]);
                        TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[1] + " changed.", ChatSourceType.System);
                    }
                    if (message[1] == "AddToAuth")
                    {
                        ScenePresence NewSP;
                        ((Scene)c.Scene).TryGetAvatarByName(message[2], out NewSP);
                        m_authList.Add(NewSP.UUID);
                        TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[2] + " added.", ChatSourceType.System);
                    }
                    if (message[1] == "RemoveFromAuth")
                    {
                        ScenePresence NewSP;
                        ((Scene)c.Scene).TryGetAvatarByName(message[2], out NewSP);
                        m_authList.Remove(NewSP.UUID);
                        TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[2] + " added.", ChatSourceType.System);
                    } 
                    if (message[1] == "BlockChat")
                    {
                        m_blockChat = true;
                        TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, "Chat blocked.", ChatSourceType.System);
                    }
                    if (message[1] == "AllowChat")
                    {
                        m_blockChat = false;
                        TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, "Chat allowed.", ChatSourceType.System);
                    }
                    if (message[1] == "RevokeSpeakingRights")
                    {
                        ScenePresence NewSP;
                        ((Scene)c.Scene).TryGetAvatarByName(message[2], out NewSP);
                        m_authorizedSpeakers.Remove(NewSP.UUID);
                        TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[2] + " - revoked.", ChatSourceType.System);
                    }
                    if (message[1] == "GiveSpeakingRights")
                    {
                        ScenePresence NewSP;
                        ((Scene)c.Scene).TryGetAvatarByName(message[2], out NewSP);
                        m_authorizedSpeakers.Add(NewSP.UUID);
                        TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[2] + " - revoked.", ChatSourceType.System);
                    }
                }
            }
            else
            {
                if (SP.GodLevel != 0 && m_indicategod)
                    c.Message = m_godPrefix + c.Message;

                DeliverChatToAvatars(ChatSourceType.Agent, c);
            }
        }

        public virtual void OnChatFromWorld(Object sender, OSChatMessage c)
        {
            // early return if not on public or debug channel
            if (c.Channel != 0 && c.Channel != DEBUG_CHANNEL) return;

            DeliverChatToAvatars(ChatSourceType.Object, c);
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
                        bool IsMuted = false;
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
                        if(!IsMuted)
                            TrySendChatMessage(presence, fromPos, regionPos, fromID, fromName, c.Type, message, sourceType);
                    }
                );
            }
        }

        static private Vector3 CenterOfRegion = new Vector3(128, 128, 30);

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


        protected virtual void TrySendChatMessage(ScenePresence presence, Vector3 fromPos, Vector3 regionPos,
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
            //Gets sent if a mute is not selected.
            if (MuteID != UUID.Zero)
            {
                MuteListConnector.DeleteMute(MuteID, AgentID);
                MuteListCache.Remove(AgentID);
            }
            OnMuteListRequest(client, 0);
        }
    }
}
