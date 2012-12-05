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
using System.Linq;
using System.Reflection;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;

namespace OpenSim.Services.Robust
{
    public class RobustOfflineMessageModule : ISharedRegionModule
    {
        private readonly List<IScene> m_SceneList = new List<IScene>();
        private bool enabled = true;
        private bool m_ForwardOfflineGroupMessages = true;
        private string m_RestURL = String.Empty;

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
            IConfig cnf = config.Configs["Messaging"];
            if (cnf == null)
            {
                enabled = false;
                return;
            }
            if (cnf != null && cnf.GetString("OfflineMessageModule", "None") !=
                "RobustOfflineMessageModule")
            {
                enabled = false;
                return;
            }

            m_RestURL = cnf.GetString("OfflineMessageURL", "");
            if (m_RestURL == "")
            {
                MainConsole.Instance.Error("[OFFLINE MESSAGING] Module was enabled, but no URL is given, disabling");
                enabled = false;
                return;
            }

            m_ForwardOfflineGroupMessages = cnf.GetBoolean("ForwardOfflineGroupMessages", m_ForwardOfflineGroupMessages);
        }

        public void AddRegion(IScene scene)
        {
            if (!enabled)
                return;

            lock (m_SceneList)
                m_SceneList.Add(scene);

            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
        }

        public void RegionLoaded(IScene scene)
        {
            if (!enabled)
                return;

            if (scene.RequestModuleInterface<IMessageTransferModule>() == null)
            {
                scene.EventManager.OnNewClient -= OnNewClient;

                enabled = false;
                m_SceneList.Clear();

                MainConsole.Instance.Error("[OFFLINE MESSAGING] No message transfer module is enabled. Diabling offline messages");
            }
            else
                scene.RequestModuleInterface<IMessageTransferModule>().OnUndeliveredMessage += UndeliveredMessage;
        }

        public void RemoveRegion(IScene scene)
        {
            if (!enabled)
                return;

            lock (m_SceneList)
                m_SceneList.Remove(scene);

            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public void PostInitialise()
        {
            if (!enabled)
                return;

            MainConsole.Instance.Debug("[OFFLINE MESSAGING] Offline messages enabled");
        }

        public string Name
        {
            get { return "OfflineMessageModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Close()
        {
        }

        #endregion

        private IScene FindScene(UUID agentID)
        {
            return (from s in m_SceneList let presence = s.GetScenePresence(agentID) where presence != null && !presence.IsChildAgent select s).FirstOrDefault();
        }

        private IClientAPI FindClient(UUID agentID)
        {
            return (from s in m_SceneList
                    select s.GetScenePresence(agentID)
                    into presence where presence != null && !presence.IsChildAgent select presence.ControllingClient).
                FirstOrDefault();
        }

        private void OnNewClient(IClientAPI client)
        {
            client.OnRetrieveInstantMessages += RetrieveInstantMessages;
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnRetrieveInstantMessages -= RetrieveInstantMessages;
        }

        private void RetrieveInstantMessages(IClientAPI client)
        {
            if (m_RestURL != "")
            {
                MainConsole.Instance.DebugFormat("[OFFLINE MESSAGING] Retrieving stored messages for {0}", client.AgentId);

                List<GridInstantMessage> msglist = SynchronousRestObjectRequester.MakeRequest
                    <UUID, List<GridInstantMessage>>(
                        "POST", m_RestURL + "/RetrieveMessages/", client.AgentId);

                foreach (GridInstantMessage im in msglist)
                {
                    if (im.dialog == (byte)InstantMessageDialog.InventoryOffered)
                        // send it directly or else the item will be given twice
                        client.SendInstantMessage(im);
                    else
                    {
                        // Send through scene event manager so all modules get a chance
                        // to look at this message before it gets delivered.
                        //
                        // Needed for proper state management for stored group
                        // invitations
                        //
                        IScene s = FindScene(client.AgentId);
                        if (s != null)
                            s.EventManager.TriggerIncomingInstantMessage(im);
                    }
                }
            }
        }

        private void UndeliveredMessage(GridInstantMessage im, string reason)
        {
            if ((im.offline != 0)
                && (!im.fromGroup || (im.fromGroup && m_ForwardOfflineGroupMessages)))
            {
                bool success = SynchronousRestObjectRequester.MakeRequest<GridInstantMessage, bool>(
                    "POST", m_RestURL + "/SaveMessage/", im);

                if (im.dialog == (byte) InstantMessageDialog.MessageFromAgent)
                {
                    IClientAPI client = FindClient(im.fromAgentID);
                    if (client == null)
                        return;

                    client.SendInstantMessage(new GridInstantMessage(
                                                  null, im.toAgentID,
                                                  "System", im.fromAgentID,
                                                  (byte) InstantMessageDialog.MessageFromAgent,
                                                  "User is not logged in. " +
                                                  (success ? "Message saved." : "Message not saved"),
                                                  false, new Vector3()));
                }
            }
        }
    }
}