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

namespace Aurora.Modules.Avatar.AuroraChat
{
    public class CalcChatPlugin : IChatPlugin
    {
        IChatModule chatModule;
        public void Initialize(IChatModule module)
        {
            chatModule = module;
            module.RegisterChatPlugin("calc", this);
        }

        public bool OnNewChatMessageFromWorld(OSChatMessage c, out OSChatMessage newc)
        {
            Scene scene = (Scene)c.Scene;
            string[] operators = c.Message.Split(' ');
            if (operators[0] == "calc.Add")
            {
                if (operators.Length == 3)
                {
                    float Num1 = float.Parse(operators[1]);
                    float Num2 = float.Parse(operators[2]);
                    float RetVal = Num1 + Num2;
                    BuildAndSendResult(RetVal, scene, c.Position);
                }
            }
            if (operators[0] == "calc.Subtract")
            {
                if (operators.Length == 3)
                {
                    float Num1 = float.Parse(operators[1]);
                    float Num2 = float.Parse(operators[2]);
                    float RetVal = Num1 - Num2;
                    BuildAndSendResult(RetVal, scene, c.Position);
                }
            }
            if (operators[0] == "calc.Multiply")
            {
                if (operators.Length == 3)
                {
                    float Num1 = float.Parse(operators[1]);
                    float Num2 = float.Parse(operators[2]);
                    float RetVal = Num1 * Num2;
                    BuildAndSendResult(RetVal, scene, c.Position);
                }
            }
            if (operators[0] == "calc.Divide")
            {
                if (operators.Length == 3)
                {
                    float Num1 = float.Parse(operators[1]);
                    float Num2 = float.Parse(operators[2]);
                    float RetVal = Num1 / Num2;
                    BuildAndSendResult(RetVal, scene, c.Position);
                }
            }
            newc = c;
            return true;
        }

        private void BuildAndSendResult(float result, Scene scene, Vector3 position)
        {
            OSChatMessage message = new OSChatMessage();
            message.From = "Server";
            message.Message = "Result: " + result;
            message.Channel = 0;
            message.Type = ChatTypeEnum.Region;
            message.Position = position;
            message.Sender = null;
            message.SenderUUID = UUID.Zero;
            message.Scene = scene;
            scene.EventManager.TriggerOnChatBroadcast(null, message);
        }

        public void OnNewClient(IClientAPI client)
        {
        }

        public void OnClosingClient(UUID clientID, Scene scene)
        {
        }

        public string Name
        {
            get { return "CalcChatPlugin"; }
        }

        public void Dispose()
        {
        }
    }

    public class AdminChatPlugin : IChatPlugin
    {
        IChatModule chatModule;
        private bool m_useAuth = true;
        private bool m_blockChat = false;
        private List<UUID> m_authList = new List<UUID>();
        private List<UUID> m_authorizedSpeakers = new List<UUID>();
        private bool m_announceNewAgents;
        private bool m_announceClosedAgents;
        private bool m_useWelcomeMessage;
        private string m_welcomeMessage;
        private bool m_indicategod;
        private string m_godPrefix;
        private Dictionary<UUID, int> RegionAgentCount = new Dictionary<UUID, int>();

        public void Initialize(IChatModule module)
        {
            m_indicategod = module.Config.GetBoolean("indicate_god", true);
            m_godPrefix = module.Config.GetString("godPrefix", "");
            m_useWelcomeMessage = module.Config.GetBoolean("useWelcomeMessage", true);
            m_welcomeMessage = module.Config.GetString("welcomeMessage", "");
            m_announceNewAgents = module.Config.GetBoolean("announceNewAgents", true);
            m_announceClosedAgents = module.Config.GetBoolean("announceClosingAgents", true);
            m_useAuth = module.Config.GetBoolean("use_Auth", true);
            chatModule = module;
            module.RegisterChatPlugin("Chat", this);
            module.RegisterChatPlugin("all", this);
        }

        public bool OnNewChatMessageFromWorld(OSChatMessage c, out OSChatMessage newc)
        {
            Scene scene = (Scene)c.Scene;
            ScenePresence SP = scene.GetScenePresence(c.SenderUUID);

            if (SP.GodLevel != 0 && !!m_authorizedSpeakers.Contains(c.SenderUUID))
                m_authorizedSpeakers.Add(c.SenderUUID);

            if (SP.GodLevel != 0 && !m_authList.Contains(c.SenderUUID))
                m_authList.Add(c.SenderUUID);

            if (!m_authorizedSpeakers.Contains(c.SenderUUID))
            {
                newc = c;
                return false;
            }

            if (c.Message.Contains("Chat."))
            {
                if (!m_useAuth || m_authList.Contains(c.SenderUUID))
                {
                    ScenePresence senderSP;
                    ((Scene)c.Scene).TryGetScenePresence(c.SenderUUID, out senderSP);
                    string[] message = c.Message.Split('.');
                    if (message[1] == "SayDistance")
                    {
                        chatModule.SayDistance = Convert.ToInt32(message[2]);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[1] + " changed.", ChatSourceType.System, -1);
                    }
                    if (message[1] == "WhisperDistance")
                    {
                        chatModule.WhisperDistance = Convert.ToInt32(message[2]);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[1] + " changed.", ChatSourceType.System, -1);
                    }
                    if (message[1] == "ShoutDistance")
                    {
                        chatModule.ShoutDistance = Convert.ToInt32(message[2]);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[1] + " changed.", ChatSourceType.System, -1);
                    }
                    if (message[1] == "AddToAuth")
                    {
                        ScenePresence NewSP;
                        ((Scene)c.Scene).TryGetAvatarByName(message[2], out NewSP);
                        m_authList.Add(NewSP.UUID);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[2] + " added.", ChatSourceType.System, -1);
                    }
                    if (message[1] == "RemoveFromAuth")
                    {
                        ScenePresence NewSP;
                        ((Scene)c.Scene).TryGetAvatarByName(message[2], out NewSP);
                        m_authList.Remove(NewSP.UUID);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[2] + " added.", ChatSourceType.System, -1);
                    }
                    if (message[1] == "BlockChat")
                    {
                        m_blockChat = true;
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, "Chat blocked.", ChatSourceType.System, -1);
                    }
                    if (message[1] == "AllowChat")
                    {
                        m_blockChat = false;
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, "Chat allowed.", ChatSourceType.System, -1);
                    }
                    if (message[1] == "RevokeSpeakingRights")
                    {
                        ScenePresence NewSP;
                        ((Scene)c.Scene).TryGetAvatarByName(message[2], out NewSP);
                        m_authorizedSpeakers.Remove(NewSP.UUID);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[2] + " - revoked.", ChatSourceType.System, -1);
                    }
                    if (message[1] == "GiveSpeakingRights")
                    {
                        ScenePresence NewSP;
                        ((Scene)c.Scene).TryGetAvatarByName(message[2], out NewSP);
                        m_authorizedSpeakers.Add(NewSP.UUID);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(scene.RegionInfo.RegionLocX * Constants.RegionSize,
                                                scene.RegionInfo.RegionLocY * Constants.RegionSize, 0), UUID.Zero, "AuroraChat", ChatTypeEnum.Region, message[2] + " - revoked.", ChatSourceType.System, -1);
                    }
                }
                newc = c;
                return false;
            }

            if (SP.GodLevel != 0 && m_indicategod)
                c.Message = m_godPrefix + c.Message;

            newc = c;
            return true;
        }

        public void OnNewClient(IClientAPI client)
        {
            if (!m_blockChat)
            {
                if (!m_authorizedSpeakers.Contains(client.AgentId))
                    m_authorizedSpeakers.Add(client.AgentId);
            }
            lock (RegionAgentCount)
            {
                int AgentCount = 0;
                if (RegionAgentCount.ContainsKey(client.Scene.RegionInfo.RegionID))
                {
                    RegionAgentCount.TryGetValue(client.Scene.RegionInfo.RegionID, out AgentCount);
                    RegionAgentCount.Remove(client.Scene.RegionInfo.RegionID);
                }
                AgentCount++;
                RegionAgentCount.Add(client.Scene.RegionInfo.RegionID, AgentCount);

                if (m_announceNewAgents)
                {
                    ((Scene)client.Scene).ForEachScenePresence(delegate(ScenePresence SP)
                    {
                        if (SP.UUID != client.AgentId && !SP.IsChildAgent)
                        {
                            SP.ControllingClient.SendChatMessage(client.Name + " has joined the region. Total Agents: " + AgentCount, 1, SP.AbsolutePosition, "System",
                                                               UUID.Zero, (byte)ChatSourceType.System, (byte)ChatAudibleLevel.Fully);
                        }
                    }
                    );
                }
            }

            if (m_useWelcomeMessage)
            {
                ScenePresence SP = ((Scene)client.Scene).GetScenePresence(client.AgentId);
                client.SendChatMessage(m_welcomeMessage, 1, SP.AbsolutePosition, "System",
                                               UUID.Zero, (byte)ChatSourceType.System, (byte)ChatAudibleLevel.Fully);
            }
        }

        public void OnClosingClient(UUID clientID, Scene scene)
        {
            if (m_authorizedSpeakers.Contains(clientID))
                m_authorizedSpeakers.Remove(clientID);

            lock (RegionAgentCount)
            {
                int AgentCount = 0;
                if (RegionAgentCount.ContainsKey(scene.RegionInfo.RegionID))
                {
                    RegionAgentCount.TryGetValue(scene.RegionInfo.RegionID, out AgentCount);
                    RegionAgentCount.Remove(scene.RegionInfo.RegionID);
                }
                AgentCount--;

                if (AgentCount < 0)
                    AgentCount = 0;

                RegionAgentCount.Add(scene.RegionInfo.RegionID, AgentCount);


                if (m_announceClosedAgents)
                {
                    string leavingAvatar = scene.GetUserName(clientID);
                    scene.ForEachScenePresence(delegate(ScenePresence SP)
                    {
                        if (SP.UUID != clientID && !SP.IsChildAgent)
                        {
                            SP.ControllingClient.SendChatMessage(leavingAvatar + " has left the region. Total Agents: " + AgentCount, 1, SP.AbsolutePosition, "System",
                                                               UUID.Zero, (byte)ChatSourceType.System, (byte)ChatAudibleLevel.Fully);
                        }
                    }
                    );
                }
            }
        }

        public string Name
        {
            get { return "AdminChatPlugin"; }
        }

        public void Dispose()
        {
        }
    }
}
