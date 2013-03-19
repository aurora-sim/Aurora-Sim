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
using System.Collections.Generic;
using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using OpenMetaverse;

namespace Aurora.Modules.Chat
{
    /// <summary>
    ///     This allows you to use a calculator inworld via chat
    ///     Example would be to type:
    ///     calc.Add 1 1
    ///     which would return 2
    ///     or
    ///     calc.Divide 4 2
    ///     which returns 2
    /// </summary>
    public class CalcChatPlugin : IChatPlugin
    {
        #region IChatPlugin Members

        public void Initialize(IChatModule module)
        {
            module.RegisterChatPlugin("calc", this);
        }

        public bool OnNewChatMessageFromWorld(OSChatMessage c, out OSChatMessage newc)
        {
            string[] operators = c.Message.Split(' ');
            if (operators[0] == "calc.Add")
            {
                if (operators.Length == 3)
                {
                    float Num1 = float.Parse(operators[1]);
                    float Num2 = float.Parse(operators[2]);
                    float RetVal = Num1 + Num2;
                    BuildAndSendResult(RetVal, c.Scene, c.Position);
                }
            }
            if (operators[0] == "calc.Subtract")
            {
                if (operators.Length == 3)
                {
                    float Num1 = float.Parse(operators[1]);
                    float Num2 = float.Parse(operators[2]);
                    float RetVal = Num1 - Num2;
                    BuildAndSendResult(RetVal, c.Scene, c.Position);
                }
            }
            if (operators[0] == "calc.Multiply")
            {
                if (operators.Length == 3)
                {
                    float Num1 = float.Parse(operators[1]);
                    float Num2 = float.Parse(operators[2]);
                    float RetVal = Num1*Num2;
                    BuildAndSendResult(RetVal, c.Scene, c.Position);
                }
            }
            if (operators[0] == "calc.Divide")
            {
                if (operators.Length == 3)
                {
                    float Num1 = float.Parse(operators[1]);
                    float Num2 = float.Parse(operators[2]);
                    float RetVal = Num1/Num2;
                    BuildAndSendResult(RetVal, c.Scene, c.Position);
                }
            }
            newc = c;
            //Block the message from going to everyone, only the server needed to hear
            return true;
        }

        public void OnNewClient(IClientAPI client)
        {
        }

        public void OnClosingClient(UUID clientID, IScene scene)
        {
        }

        public string Name
        {
            get { return "CalcChatPlugin"; }
        }

        #endregion

        /// <summary>
        ///     Tell the client what the result is
        /// </summary>
        /// <param name="result"></param>
        /// <param name="scene"></param>
        /// <param name="position"></param>
        private void BuildAndSendResult(float result, IScene scene, Vector3 position)
        {
            OSChatMessage message = new OSChatMessage
                                        {
                                            From = "Server",
                                            Message = "Result: " + result,
                                            Channel = 0,
                                            Type = ChatTypeEnum.Region,
                                            Position = position,
                                            Sender = null,
                                            SenderUUID = UUID.Zero,
                                            Scene = scene
                                        };
            scene.EventManager.TriggerOnChatBroadcast(null, message);
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    ///     Set some default settings for users entering the sim
    /// </summary>
    public class AdminChatPlugin : IChatPlugin
    {
        private readonly List<UUID> m_authList = new List<UUID>();
        private readonly List<UUID> m_authorizedSpeakers = new List<UUID>();
        private IChatModule chatModule;
        private bool m_announceClosedAgents;
        private bool m_announceNewAgents;
        private bool m_blockChat;
        private string m_godPrefix;
        private bool m_indicategod;
        private bool m_useAuth = true;
        private bool m_useWelcomeMessage;
        private string m_welcomeMessage;

        #region IChatPlugin Members

        public void Initialize(IChatModule module)
        {
            //Show gods in chat by adding the godPrefix to their name
            m_indicategod = module.Config.GetBoolean("indicate_god", true);
            m_godPrefix = module.Config.GetString("godPrefix", "");
            //Send incoming users a message
            m_useWelcomeMessage = module.Config.GetBoolean("useWelcomeMessage", true);
            m_welcomeMessage = module.Config.GetString("welcomeMessage", "");
            //Tell all users about an incoming or outgoing agent
            m_announceNewAgents = module.Config.GetBoolean("announceNewAgents", true);
            m_announceClosedAgents = module.Config.GetBoolean("announceClosingAgents", true);
            m_useAuth = module.Config.GetBoolean("use_Auth", true);
            chatModule = module;
            module.RegisterChatPlugin("Chat", this);
            module.RegisterChatPlugin("all", this);
        }

        public bool OnNewChatMessageFromWorld(OSChatMessage c, out OSChatMessage newc)
        {
            IScenePresence SP = c.Scene.GetScenePresence(c.SenderUUID);
            if (SP != null)
            {
                if (!SP.IsChildAgent)
                {
                    //Check that the agent is allows to speak in this reigon
                    if (SP.GodLevel != 0 && !!m_authorizedSpeakers.Contains(c.SenderUUID))
                        m_authorizedSpeakers.Add(c.SenderUUID);

                    if (SP.GodLevel != 0 && !m_authList.Contains(c.SenderUUID))
                        m_authList.Add(c.SenderUUID);

                    if (!m_authorizedSpeakers.Contains(c.SenderUUID))
                    {
                        newc = c;
                        //They can't talk, so block it
                        return false;
                    }
                }
            }

            if (c.Message.Contains("Chat."))
            {
                if (!m_useAuth || m_authList.Contains(c.SenderUUID))
                {
                    IScenePresence senderSP;
                    c.Scene.TryGetScenePresence(c.SenderUUID, out senderSP);
                    string[] message = c.Message.Split('.');
                    if (message[1] == "SayDistance")
                    {
                        chatModule.SayDistance = Convert.ToInt32(message[2]);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(c.Scene.RegionInfo.RegionLocX,
                                                                                        c.Scene.RegionInfo.RegionLocY, 0),
                                                      UUID.Zero, "AuroraChat", ChatTypeEnum.Region,
                                                      message[1] + " changed.", ChatSourceType.System, -1);
                    }
                    if (message[1] == "WhisperDistance")
                    {
                        chatModule.WhisperDistance = Convert.ToInt32(message[2]);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(c.Scene.RegionInfo.RegionLocX,
                                                                                        c.Scene.RegionInfo.RegionLocY, 0),
                                                      UUID.Zero, "AuroraChat", ChatTypeEnum.Region,
                                                      message[1] + " changed.", ChatSourceType.System, -1);
                    }
                    if (message[1] == "ShoutDistance")
                    {
                        chatModule.ShoutDistance = Convert.ToInt32(message[2]);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(c.Scene.RegionInfo.RegionLocX,
                                                                                        c.Scene.RegionInfo.RegionLocY, 0),
                                                      UUID.Zero, "AuroraChat", ChatTypeEnum.Region,
                                                      message[1] + " changed.", ChatSourceType.System, -1);
                    }
                    //Add the user to the list of allowed speakers and 'chat' admins
                    if (message[1] == "AddToAuth")
                    {
                        IScenePresence NewSP;
                        c.Scene.TryGetAvatarByName(message[2], out NewSP);
                        m_authList.Add(NewSP.UUID);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(c.Scene.RegionInfo.RegionLocX,
                                                                                        c.Scene.RegionInfo.RegionLocY, 0),
                                                      UUID.Zero, "AuroraChat", ChatTypeEnum.Region,
                                                      message[2] + " added.", ChatSourceType.System, -1);
                    }
                    if (message[1] == "RemoveFromAuth")
                    {
                        IScenePresence NewSP;
                        c.Scene.TryGetAvatarByName(message[2], out NewSP);
                        m_authList.Remove(NewSP.UUID);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(c.Scene.RegionInfo.RegionLocX,
                                                                                        c.Scene.RegionInfo.RegionLocY, 0),
                                                      UUID.Zero, "AuroraChat", ChatTypeEnum.Region,
                                                      message[2] + " added.", ChatSourceType.System, -1);
                    }
                    //Block chat from those not in the auth list
                    if (message[1] == "BlockChat")
                    {
                        m_blockChat = true;
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(c.Scene.RegionInfo.RegionLocX,
                                                                                        c.Scene.RegionInfo.RegionLocY, 0),
                                                      UUID.Zero, "AuroraChat", ChatTypeEnum.Region, "Chat blocked.",
                                                      ChatSourceType.System, -1);
                    }
                    //Allow chat from all again
                    if (message[1] == "AllowChat")
                    {
                        m_blockChat = false;
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(c.Scene.RegionInfo.RegionLocX,
                                                                                        c.Scene.RegionInfo.RegionLocY, 0),
                                                      UUID.Zero, "AuroraChat", ChatTypeEnum.Region, "Chat allowed.",
                                                      ChatSourceType.System, -1);
                    }
                    //Remove speaking priviledges from an individual
                    if (message[1] == "RevokeSpeakingRights")
                    {
                        IScenePresence NewSP;
                        c.Scene.TryGetAvatarByName(message[2], out NewSP);
                        m_authorizedSpeakers.Remove(NewSP.UUID);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(c.Scene.RegionInfo.RegionLocX,
                                                                                        c.Scene.RegionInfo.RegionLocY, 0),
                                                      UUID.Zero, "AuroraChat", ChatTypeEnum.Region,
                                                      message[2] + " - revoked.", ChatSourceType.System, -1);
                    }
                    //Allow an individual to speak again
                    if (message[1] == "GiveSpeakingRights")
                    {
                        IScenePresence NewSP;
                        c.Scene.TryGetAvatarByName(message[2], out NewSP);
                        m_authorizedSpeakers.Add(NewSP.UUID);
                        chatModule.TrySendChatMessage(senderSP, c.Position, new Vector3(c.Scene.RegionInfo.RegionLocX,
                                                                                        c.Scene.RegionInfo.RegionLocY, 0),
                                                      UUID.Zero, "AuroraChat", ChatTypeEnum.Region,
                                                      message[2] + " - revoked.", ChatSourceType.System, -1);
                    }
                }
                newc = c;
                //Block commands from normal chat
                return false;
            }

            if (SP != null)
            {
                //Add the god prefix
                if (SP.GodLevel != 0 && m_indicategod)
                    c.Message = m_godPrefix + c.Message;
            }

            newc = c;
            return true;
        }

        public void OnNewClient(IClientAPI client)
        {
            IScenePresence SP = client.Scene.GetScenePresence(client.AgentId);
            if (!SP.IsChildAgent)
            {
                //If chat is not blocked for now, add to the not blocked list
                if (!m_blockChat)
                {
                    if (!m_authorizedSpeakers.Contains(client.AgentId))
                        m_authorizedSpeakers.Add(client.AgentId);
                }

                //Tell all the clients about the incoming client if it is enabled
                if (m_announceNewAgents)
                {
                    client.Scene.ForEachScenePresence(delegate(IScenePresence presence)
                                                          {
                                                              if (presence.UUID != client.AgentId &&
                                                                  !presence.IsChildAgent)
                                                              {
                                                                  IEntityCountModule entityCountModule =
                                                                      client.Scene.RequestModuleInterface
                                                                          <IEntityCountModule>();
                                                                  if (entityCountModule != null)
                                                                      presence.ControllingClient.SendChatMessage(
                                                                          client.Name +
                                                                          " has joined the region. Total Agents: " +
                                                                          (entityCountModule.RootAgents + 1), 1,
                                                                          SP.AbsolutePosition, "System",
                                                                          UUID.Zero, (byte) ChatSourceType.System,
                                                                          (byte) ChatAudibleLevel.Fully);
                                                              }
                                                          }
                        );
                }

                //Send the new user a welcome message
                if (m_useWelcomeMessage)
                {
                    if (m_welcomeMessage != "")
                    {
                        client.SendChatMessage(m_welcomeMessage, 1, SP.AbsolutePosition, "System",
                                               UUID.Zero, (byte) ChatSourceType.System, (byte) ChatAudibleLevel.Fully);
                    }
                }
            }
        }

        public void OnClosingClient(UUID clientID, IScene scene)
        {
            IScenePresence client = scene.GetScenePresence(clientID);
            if (client != null && !client.IsChildAgent)
            {
                //Clear out the auth speakers list
                lock (m_authorizedSpeakers)
                {
                    if (m_authorizedSpeakers.Contains(clientID))
                        m_authorizedSpeakers.Remove(clientID);
                }

                IScenePresence presence = scene.GetScenePresence(clientID);
                //Announce the closing agent if enabled
                if (m_announceClosedAgents)
                {
                    scene.ForEachScenePresence(delegate(IScenePresence SP)
                                                   {
                                                       if (SP.UUID != clientID && !SP.IsChildAgent)
                                                       {
                                                           IEntityCountModule entityCountModule =
                                                               scene.RequestModuleInterface<IEntityCountModule>();
                                                           if (entityCountModule != null)
                                                               SP.ControllingClient.SendChatMessage(
                                                                   presence.Name +
                                                                   " has left the region. Total Agents: " +
                                                                   (entityCountModule.RootAgents - 1), 1,
                                                                   SP.AbsolutePosition, "System",
                                                                   UUID.Zero, (byte) ChatSourceType.System,
                                                                   (byte) ChatAudibleLevel.Fully);
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

        #endregion

        public void Dispose()
        {
        }
    }
}