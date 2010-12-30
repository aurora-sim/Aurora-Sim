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
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;

using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using Aurora.Framework;

namespace Aurora.Modules
{
    /// <summary>
    /// This dialog module has support for mute lists
    /// </summary>
    public class AuroraDialogModule : INonSharedRegionModule, IDialogModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected Scene m_scene;
        protected bool m_enabled = true;
        protected IMuteListModule m_muteListModule = null;
            
        public void Initialise(IConfigSource source)
        {
            IConfig m_config = source.Configs["Dialog"];

            if (null == m_config)
            {
                m_enabled = false;
                return;
            }

            if (m_config.GetString("DialogModule", "DialogModule") != "AuroraDialogModule")
            {
                m_enabled = false;
            }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;
            m_scene = scene;
            m_scene.RegisterModuleInterface<IDialogModule>(this);
            m_scene.EventManager.OnPermissionError += SendAlertToUser;

            MainConsole.Instance.Commands.AddCommand(this.Name, false, 
                "alert", "alert <first> <last> <message>", "Send an alert to a user", HandleAlertConsoleCommand);

            MainConsole.Instance.Commands.AddCommand(this.Name, false,
                 "alert general", "alert general <message>", "Send an alert to everyone", HandleAlertConsoleCommand);
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
            m_muteListModule = m_scene.RequestModuleInterface<IMuteListModule>();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise() { }
        public void Close() { }
        public string Name { get { return "Dialog Module"; } }
        public bool IsSharedModule { get { return false; } }

        public void SendAlertToUser(IClientAPI client, string message)
        {
            SendAlertToUser(client, message, false);
        }

        public void SendAlertToUser(IClientAPI client, string message, bool modal)
        {
            client.SendAgentAlertMessage(message, modal);
        }

        public void SendAlertToUser(UUID agentID, string message)
        {
            SendAlertToUser(agentID, message, false);
        }

        public void SendAlertToUser(UUID agentID, string message, bool modal)
        {
            ScenePresence sp = m_scene.GetScenePresence(agentID);

            if (sp != null && !sp.IsChildAgent)
                sp.ControllingClient.SendAgentAlertMessage(message, modal);
        }

        public void SendAlertToUser(string firstName, string lastName, string message, bool modal)
        {
            ScenePresence presence = m_scene.GetScenePresence(firstName, lastName);
            if (presence != null && !presence.IsChildAgent)
                presence.ControllingClient.SendAgentAlertMessage(message, modal);
        }

        public void SendGeneralAlert(string message)
        {
            m_scene.ForEachScenePresence(delegate(ScenePresence presence)
            {
                if (!presence.IsChildAgent)
                    presence.ControllingClient.SendAlertMessage(message);
            });
        }

        public void SendDialogToUser(
            UUID avatarID, string objectName, UUID objectID, UUID ownerID,
            string message, UUID textureID, int ch, string[] buttonlabels)
        {
            UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.ScopeID, ownerID);
            string ownerFirstName, ownerLastName;
            if (account != null)
            {
                ownerFirstName = account.FirstName;
                ownerLastName = account.LastName;
            }
            else
            {
                ownerFirstName = "(unknown";
                ownerLastName = "user)";
            }

            //If the user is muted, we do NOT send them dialog boxes
            if (m_muteListModule != null)
            {
                bool cached = false; //Unneeded
                foreach (MuteList mute in m_muteListModule.GetMutes(avatarID, out cached))
                {
                    if (mute.MuteID == ownerID)
                        return;
                }
            }

            ScenePresence sp = m_scene.GetScenePresence(avatarID);
            if (sp != null && !sp.IsChildAgent)
                sp.ControllingClient.SendDialog(objectName, objectID, ownerFirstName, ownerLastName, message, textureID, ch, buttonlabels);
        }

        public void SendUrlToUser(
            UUID avatarID, string objectName, UUID objectID, UUID ownerID, bool groupOwned, string message, string url)
        {
            ScenePresence sp = m_scene.GetScenePresence(avatarID);

            //If the user is muted, do NOT send them URL boxes
            if (m_muteListModule != null)
            {
                bool cached = false; //Unneeded
                foreach (MuteList mute in m_muteListModule.GetMutes(avatarID, out cached))
                {
                    if (mute.MuteID == ownerID)
                        return;
                }
            }

            if (sp != null && !sp.IsChildAgent)
                sp.ControllingClient.SendLoadURL(objectName, objectID, ownerID, groupOwned, message, url);
        }

        public void SendTextBoxToUser(UUID avatarID, string message, int chatChannel, string name, UUID objectID, UUID ownerID)
        {
            ScenePresence sp = m_scene.GetScenePresence(avatarID);

            if (sp != null && !sp.IsChildAgent)
            {
                UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.ScopeID, ownerID);
                string ownerFirstName, ownerLastName;
                if (account != null)
                {
                    ownerFirstName = account.FirstName;
                    ownerLastName = account.LastName;
                }
                else
                {
                    if (name != "")
                    {
                        ownerFirstName = name;
                        ownerLastName = "";
                    }
                    else
                    {
                        ownerFirstName = "(unknown";
                        ownerLastName = "user)";
                    }
                }

                //If the user is muted, do not send the text box
                if (m_muteListModule != null)
                {
                    bool cached = false; //Unneeded
                    foreach (MuteList mute in m_muteListModule.GetMutes(avatarID, out cached))
                    {
                        if (mute.MuteID == ownerID)
                            return;
                    }
                }
                sp.ControllingClient.SendTextBoxRequest(message, chatChannel, name, ownerFirstName, ownerLastName, objectID);
            }
        }

        public void SendNotificationToUsersInRegion(
            UUID fromAvatarID, string fromAvatarName, string message)
        {
            m_scene.ForEachScenePresence(delegate(ScenePresence presence)
            {
                if (!presence.IsChildAgent)
                    presence.ControllingClient.SendBlueBoxMessage(fromAvatarID, fromAvatarName, message);
            });
        }

        /// <summary>
        /// Handle an alert command from the console.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmdparams"></param>
        public void HandleAlertConsoleCommand(string module, string[] cmdparams)
        {
            if (m_scene.ConsoleScene() != null && m_scene.ConsoleScene() != m_scene)
                return;

            if (cmdparams[1] == "general")
            {
                string message = CombineParams(cmdparams, 2);

                m_log.InfoFormat(
                    "[DIALOG]: Sending general alert in region {0} with message {1}", m_scene.RegionInfo.RegionName, message);
                SendGeneralAlert(message);
            }
            else
            {
                string firstName = cmdparams[1];
                string lastName = cmdparams[2];
                string message = CombineParams(cmdparams, 3);

                m_log.InfoFormat(
                    "[DIALOG]: Sending alert in region {0} to {1} {2} with message {3}",
                    m_scene.RegionInfo.RegionName, firstName, lastName, message);
                SendAlertToUser(firstName, lastName, message, false);
            }
        }

        private string CombineParams(string[] commandParams, int pos)
        {
            string result = string.Empty;
            for (int i = pos; i < commandParams.Length; i++)
            {
                result += commandParams[i] + " ";
            }

            return result;
        }
    }
}
