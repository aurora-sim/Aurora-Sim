/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using System.Reflection;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Chat
{
    public class InstantMessageModule : ISharedRegionModule
    {
        private readonly List<IScene> m_scenes = new List<IScene>();

        private IMessageTransferModule m_TransferModule;

        /// <value>
        ///   Is this module enabled?
        /// </value>
        private bool m_enabled;

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
            if (config.Configs["Messaging"] != null)
            {
                if (config.Configs["Messaging"].GetString(
                    "InstantMessageModule", "InstantMessageModule") !=
                    "InstantMessageModule")
                    return;
            }

            m_enabled = true;
        }

        public void AddRegion(IScene scene)
        {
            if (!m_enabled)
                return;

            lock (m_scenes)
            {
                m_scenes.Add(scene);
                scene.EventManager.OnNewClient += EventManager_OnNewClient;
                scene.EventManager.OnClosingClient += EventManager_OnClosingClient;
                scene.EventManager.OnIncomingInstantMessage += OnGridInstantMessage;
            }
        }

        public void RegionLoaded(IScene scene)
        {
            if (!m_enabled)
                return;

            if (m_TransferModule == null)
            {
                m_TransferModule =
                    scene.RequestModuleInterface<IMessageTransferModule>();

                if (m_TransferModule == null)
                {
                    MainConsole.Instance.Error("[INSTANT MESSAGE]: No message transfer module, IM will not work!");
                    scene.EventManager.OnNewClient -= EventManager_OnNewClient;
                    scene.EventManager.OnClosingClient -= EventManager_OnClosingClient;
                    scene.EventManager.OnIncomingInstantMessage -= OnGridInstantMessage;

                    m_scenes.Clear();
                    m_enabled = false;
                }
            }
        }

        public void RemoveRegion(IScene scene)
        {
            if (!m_enabled)
                return;

            lock (m_scenes)
                m_scenes.Remove(scene);
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "InstantMessageModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        private void EventManager_OnClosingClient(IClientAPI client)
        {
            //client.OnInstantMessage -= OnInstantMessage;
        }

        private void EventManager_OnNewClient(IClientAPI client)
        {
            client.OnInstantMessage += OnInstantMessage;
        }

        public void OnInstantMessage(IClientAPI client, GridInstantMessage im)
        {
            byte dialog = im.dialog;

            if (dialog != (byte) InstantMessageDialog.MessageFromAgent
                && dialog != (byte) InstantMessageDialog.StartTyping
                && dialog != (byte) InstantMessageDialog.StopTyping
                && dialog != (byte) InstantMessageDialog.BusyAutoResponse
                && dialog != (byte) InstantMessageDialog.MessageFromObject)
            {
                return;
            }

            if (m_TransferModule != null)
            {
                if (client == null)
                {
                    UserAccount account = m_scenes[0].UserAccountService.GetUserAccount(m_scenes[0].RegionInfo.AllScopeIDs,
                                                                                        im.fromAgentID);
                    if (account != null)
                        im.fromAgentName = account.Name;
                    else
                        im.fromAgentName = im.fromAgentName + "(No account found for this user)";
                }
                else
                    im.fromAgentName = client.Name;

                m_TransferModule.SendInstantMessage(im);
            }
        }

        ///<summary>
        ///</summary>
        ///<param name = "msg"></param>
        private void OnGridInstantMessage(GridInstantMessage msg)
        {
            byte dialog = msg.dialog;

            if (dialog != (byte) InstantMessageDialog.MessageFromAgent
                && dialog != (byte) InstantMessageDialog.StartTyping
                && dialog != (byte) InstantMessageDialog.StopTyping
                && dialog != (byte) InstantMessageDialog.MessageFromObject)
            {
                return;
            }

            if (m_TransferModule != null)
            {
                UserAccount account = m_scenes[0].UserAccountService.GetUserAccount(m_scenes[0].RegionInfo.AllScopeIDs,
                                                                                    msg.fromAgentID);
                if (account != null)
                    msg.fromAgentName = account.Name;
                else
                    msg.fromAgentName = msg.fromAgentName + "(No account found for this user)";

                foreach (IScene scene in m_scenes)
                {
                    IScenePresence presence = null;
                    if (scene.TryGetScenePresence(msg.toAgentID, out presence))
                    {
                        presence.ControllingClient.SendInstantMessage(msg);
                        return;
                    }
                }
                if (dialog == (uint) InstantMessageDialog.StartTyping ||
                    dialog == (uint) InstantMessageDialog.StopTyping ||
                    dialog == (uint) InstantMessageDialog.MessageFromObject)
                {
                    return;
                }
            }
        }
    }
}