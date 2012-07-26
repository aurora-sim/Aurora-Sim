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
using System.Linq;
using System.Reflection;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Chat
{
    /// <summary>
    ///   This dialog module has support for mute lists
    /// </summary>
    public class AuroraDialogModule : INonSharedRegionModule, IDialogModule
    {
        protected bool m_enabled = true;
        protected IMuteListModule m_muteListModule;
        protected IScene m_scene;

        public bool IsSharedModule
        {
            get { return false; }
        }

        #region IDialogModule Members

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
            IScenePresence sp = m_scene.GetScenePresence(agentID);

            if (sp != null && !sp.IsChildAgent)
                sp.ControllingClient.SendAgentAlertMessage(message, modal);
        }

        public void SendAlertToUser(string firstName, string lastName, string message, bool modal)
        {
            IScenePresence presence = m_scene.SceneGraph.GetScenePresence(firstName, lastName);
            if (presence != null && !presence.IsChildAgent)
                presence.ControllingClient.SendAgentAlertMessage(message, modal);
        }

        public void SendGeneralAlert(string message)
        {
            m_scene.ForEachScenePresence(delegate(IScenePresence presence)
                                             {
                                                 if (!presence.IsChildAgent)
                                                     presence.ControllingClient.SendAlertMessage(message);
                                             });
        }

        public void SendDialogToUser(
            UUID avatarID, string objectName, UUID objectID, UUID ownerID,
            string message, UUID textureID, int ch, string[] buttonlabels)
        {
            UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.AllScopeIDs, ownerID);
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
#if (!ISWIN)
                foreach (MuteList mute in m_muteListModule.GetMutes(avatarID, out cached))
                {
                    if (mute.MuteID == ownerID)
                    {
                        return;
                    }
                }
#else
                if (m_muteListModule.GetMutes(avatarID, out cached).Any(mute => mute.MuteID == ownerID))
                {
                    return;
                }
#endif
            }

            IScenePresence sp = m_scene.GetScenePresence(avatarID);
            if (sp != null && !sp.IsChildAgent)
                sp.ControllingClient.SendDialog(objectName, objectID, ownerID, ownerFirstName, ownerLastName, message,
                                                textureID, ch, buttonlabels);
        }

        public void SendUrlToUser(
            UUID avatarID, string objectName, UUID objectID, UUID ownerID, bool groupOwned, string message, string url)
        {
            IScenePresence sp = m_scene.GetScenePresence(avatarID);

            //If the user is muted, do NOT send them URL boxes
            if (m_muteListModule != null)
            {
                bool cached = false; //Unneeded
#if (!ISWIN)
                foreach (MuteList mute in m_muteListModule.GetMutes(avatarID, out cached))
                {
                    if (mute.MuteID == ownerID)
                    {
                        return;
                    }
                }
#else
                if (m_muteListModule.GetMutes(avatarID, out cached).Any(mute => mute.MuteID == ownerID))
                {
                    return;
                }
#endif
            }

            if (sp != null && !sp.IsChildAgent)
                sp.ControllingClient.SendLoadURL(objectName, objectID, ownerID, groupOwned, message, url);
        }

        public void SendTextBoxToUser(UUID avatarID, string message, int chatChannel, string name, UUID objectID,
                                      UUID ownerID)
        {
            IScenePresence sp = m_scene.GetScenePresence(avatarID);

            if (sp != null && !sp.IsChildAgent)
            {
                UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.AllScopeIDs, ownerID);
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
#if (!ISWIN)
                    foreach (MuteList mute in m_muteListModule.GetMutes(avatarID, out cached))
                    {
                        if (mute.MuteID == ownerID)
                        {
                            return;
                        }
                    }
#else
                    if (m_muteListModule.GetMutes(avatarID, out cached).Any(mute => mute.MuteID == ownerID))
                    {
                        return;
                    }
#endif
                }
                sp.ControllingClient.SendTextBoxRequest(message, chatChannel, name, ownerFirstName, ownerLastName,
                                                        objectID);
            }
        }

        public void SendNotificationToUsersInRegion(
            UUID fromAvatarID, string fromAvatarName, string message)
        {
            m_scene.ForEachScenePresence(delegate(IScenePresence presence)
                                             {
                                                 if (!presence.IsChildAgent)
                                                     presence.ControllingClient.SendBlueBoxMessage(fromAvatarID,
                                                                                                   fromAvatarName,
                                                                                                   message);
                                             });
        }

        #endregion

        #region INonSharedRegionModule Members

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

        public void AddRegion(IScene scene)
        {
            if (!m_enabled)
                return;
            m_scene = scene;
            m_scene.RegisterModuleInterface<IDialogModule>(this);
            m_scene.EventManager.OnPermissionError += SendAlertToUser;

            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand(
                    "alert", "alert [first] [last] [message]", "Send an alert to a user", HandleAlertConsoleCommand);

                MainConsole.Instance.Commands.AddCommand(
                    "alert general", "alert general [message]", "Send an alert to everyone", HandleAlertConsoleCommand);
            }
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            m_muteListModule = m_scene.RequestModuleInterface<IMuteListModule>();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "Dialog Module"; }
        }

        #endregion

        public void PostInitialise()
        {
        }

        /// <summary>
        ///   Handle an alert command from the console.
        /// </summary>
        /// <param name = "module"></param>
        /// <param name = "cmdparams"></param>
        public void HandleAlertConsoleCommand(string[] cmdparams)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene && MainConsole.Instance.ConsoleScene == null)
                return;

            if (cmdparams[1] == "general")
            {
                string message = Util.CombineParams(cmdparams, 2);

                MainConsole.Instance.InfoFormat(
                    "[DIALOG]: Sending general alert in region {0} with message {1}", m_scene.RegionInfo.RegionName,
                    message);
                SendGeneralAlert(message);
            }
            else
            {
                string firstName = cmdparams[1];
                string lastName = cmdparams[2];
                string message = Util.CombineParams(cmdparams, 3);

                MainConsole.Instance.InfoFormat(
                    "[DIALOG]: Sending alert in region {0} to {1} {2} with message {3}",
                    m_scene.RegionInfo.RegionName, firstName, lastName, message);
                SendAlertToUser(firstName, lastName, message, false);
            }
        }
    }
}