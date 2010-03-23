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
        private Dictionary<UUID, byte[]> m_MuteCache = new Dictionary<UUID, byte[]>(); private Dictionary<string, List<string>> MuteCache = new Dictionary<string, List<string>>();
        private List<string> FreezeCache = new List<string>();
        internal object m_syncy = new object();

        internal IConfig m_config;

        #region ISharedRegionModule Members
        public virtual void Initialise(IConfigSource config)
        {
            m_config = config.Configs["AuroraChat"];

            if (null == m_config)
            {
                m_log.Info("[CHAT]: no config found, plugin disabled");
                m_enabled = false;
                return;
            }

            if (!m_config.GetBoolean("enabled", true))
            {
                m_log.Info("[CHAT]: plugin disabled by configuration");
                m_enabled = false;
                return;
            }

            m_useAuth = m_config.GetBoolean("use_Auth", true);
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

            m_log.InfoFormat("[CHAT]: Initialized for {0} w:{1} s:{2} S:{3}", scene.RegionInfo.RegionName,
                             m_whisperdistance, m_saydistance, m_shoutdistance);
        }

        public virtual void RegionLoaded(Scene scene)
        {
            GenericData = Aurora.DataManager.DataManager.GetGenericPlugin();
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
            if(!m_blockChat)
                m_authorizedSpeakers.Add(client.AgentId);
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
            if (!m_authorizedSpeakers.Contains(c.SenderUUID))
                return;
            if (c.Message.StartsWith("Chat."))
            {
                if (!m_useAuth || m_authList.Contains(c.SenderUUID))
                {
                    ScenePresence senderSP;
                    ((Scene)c.Scene).TryGetAvatar(c.SenderUUID, out senderSP);
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
                        List<string> muteListID = null;
                        if (MuteCache.ContainsKey(presence.ControllingClient.AgentId.ToString()))
                        {
                            MuteCache.TryGetValue(presence.ControllingClient.AgentId.ToString(), out muteListID);
                        }
                        else
                        {
                            muteListID = GenericData.Query("userID",presence.ControllingClient.AgentId.ToString(),"mutelists","muteID");
                            MuteCache.Add(presence.ControllingClient.AgentId.ToString(), muteListID);
                        }
                        if (!muteListID.Contains(c.SenderUUID.ToString()))
                        {
                            TrySendChatMessage(presence, fromPos, regionPos, fromID, fromName, c.Type, message, sourceType);
                                    return;
                        }
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
        private void OnMuteListRequest(IClientAPI client, uint crc)
        {
            //Sends the name of the file being sent by the xfer module DO NOT EDIT!!!
            string filename = "mutes" + client.AgentId.ToString();
            byte[] fileData = new byte[0];
            if (m_MuteCache.ContainsKey(client.AgentId))
            {
                m_MuteCache.TryGetValue(client.AgentId, out fileData);
            }
            else
            {
                List<string> muteListName = GenericData.Query("userID", client.AgentId.ToString(), "mutelists", "muteName");
                List<string> muteListType = GenericData.Query("userID", client.AgentId.ToString(), "mutelists", "muteType");
                List<string> muteListID = GenericData.Query("userID", client.AgentId.ToString(), "mutelists", "muteID");
                string invString = "";
                int i = 0;
                while (muteListName.Count - 1 >= i)
                {
                    invString += (muteListType[i] + " " + muteListID[i] + " " + muteListName[i] + " |\n");
                    i++;
                }
                fileData = OpenMetaverse.Utils.StringToBytes(invString);
            }
            IXfer xfer = client.Scene.RequestModuleInterface<IXfer>();
            if (xfer != null)
            {
                xfer.AddNewFile(filename, fileData);
                client.SendMuteListUpdate(filename);
            }
        }
        //TIED TO MYSQL!!!
        private void OnMuteListUpdate(IClientAPI client, UUID MuteID, string Name, int Flags, UUID AgentID)
        {
            MuteCache.Remove(client.AgentId.ToString());
            m_MuteCache.Remove(client.AgentId);
            List<string> values = new List<string>();
            values.Add(AgentID.ToString());
            values.Add(MuteID.ToString());
            values.Add(Name);
            values.Add(Flags.ToString());
            values.Add(new Guid().ToString());
            GenericData.Insert("mutelists", values.ToArray(),"muteType",Flags.ToString());
            OnMuteListRequest(client, 0);
        }
        private void OnMuteListRemove(IClientAPI client, UUID MuteID, string Name, UUID AgentID)
        {
            List<string> keys = new List<string>();
            List<string> values = new List<string>();
            keys.Add("userID");
            keys.Add("muteID");
            values.Add(AgentID.ToString());
            values.Add(MuteID.ToString());
            GenericData.Delete("mutelists", keys.ToArray(), values.ToArray());
            OnMuteListRequest(client, 0);
        }
    }
}
