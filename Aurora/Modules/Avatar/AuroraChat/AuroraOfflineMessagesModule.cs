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
using System.Linq;
using System.Reflection;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Services.Interfaces;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.Modules.Chat
{
    public class AuroraOfflineMessageModule : ISharedRegionModule
    {
        private bool enabled = true;
        private readonly List<IScene> m_SceneList = new List<IScene> ();
        IMessageTransferModule m_TransferModule = null;
        private bool m_ForwardOfflineGroupMessages = true;
        private IOfflineMessagesConnector OfflineMessagesConnector;
        private bool m_SendOfflineMessagesToEmail = false;

        public void Initialise(IConfigSource config)
        {
            IConfig cnf = config.Configs["Messaging"];
            if (cnf == null)
            {
                enabled = false;
                return;
            }
            if (cnf.GetString("OfflineMessageModule", "AuroraOfflineMessageModule") !=
                "AuroraOfflineMessageModule")
            {
                enabled = false;
                return;
            }

            m_ForwardOfflineGroupMessages = cnf.GetBoolean ("ForwardOfflineGroupMessages", m_ForwardOfflineGroupMessages);
            m_SendOfflineMessagesToEmail = cnf.GetBoolean ("SendOfflineMessagesToEmail", m_SendOfflineMessagesToEmail);
        }

        public void AddRegion (IScene scene)
        {
            if (!enabled)
                return;

            lock (m_SceneList)
            {
                m_SceneList.Add(scene);

                scene.EventManager.OnNewClient += OnNewClient;
                scene.EventManager.OnClosingClient += OnClosingClient;
            }
        }

        public void RegionLoaded (IScene scene)
        {
            if (!enabled)
                return;

            if (m_TransferModule == null)
            {
                OfflineMessagesConnector = DataManager.DataManager.RequestPlugin<IOfflineMessagesConnector>();
                m_TransferModule = scene.RequestModuleInterface<IMessageTransferModule>();
                if (m_TransferModule == null || OfflineMessagesConnector == null)
                {
                    scene.EventManager.OnNewClient -= OnNewClient;
                    scene.EventManager.OnClosingClient -= OnClosingClient;

                    enabled = false;
                    m_SceneList.Clear();

                    MainConsole.Instance.Error("[OFFLINE MESSAGING] No message transfer module or OfflineMessagesConnector is enabled. Diabling offline messages");
                    return;
                }
                m_TransferModule.OnUndeliveredMessage += UndeliveredMessage;
            }
        }

        public void RemoveRegion (IScene scene)
        {
            if (!enabled)
                return;

            lock (m_SceneList)
                m_SceneList.Remove(scene);

            if (m_TransferModule != null)
            {
                scene.EventManager.OnNewClient -= OnNewClient;
                scene.EventManager.OnClosingClient -= OnClosingClient;
                m_TransferModule.OnUndeliveredMessage -= UndeliveredMessage;
            }
        }

        public void PostInitialise()
        {
            if (!enabled)
                return;

            //MainConsole.Instance.Debug("[OFFLINE MESSAGING] Offline messages enabled");
        }

        public string Name
        {
            get { return "AuroraOfflineMessageModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Close()
        {
        }

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
            if (OfflineMessagesConnector == null)
                return;
            MainConsole.Instance.DebugFormat("[OFFLINE MESSAGING] Retrieving stored messages for {0}", client.AgentId);

            List<GridInstantMessage> msglist = OfflineMessagesConnector.GetOfflineMessages(client.AgentId);
            msglist.Sort(delegate(GridInstantMessage a, GridInstantMessage b)
            {
                return a.timestamp.CompareTo(b.timestamp);
            });
            foreach (GridInstantMessage IM in msglist)
            {
                // Send through scene event manager so all modules get a chance
                // to look at this message before it gets delivered.
                //
                // Needed for proper state management for stored group
                // invitations
                //
                IM.offline = 1;
                IScene s = FindScene(client.AgentId);
                if (s != null)
                    s.EventManager.TriggerIncomingInstantMessage(IM);
            }
        }

        private void UndeliveredMessage(GridInstantMessage im, string reason)
        {
            if (OfflineMessagesConnector == null || im == null)
                return;
            IClientAPI client = FindClient(im.fromAgentID);
            if ((client == null) && (im.dialog != 32))
                return;
            if (!OfflineMessagesConnector.AddOfflineMessage (im))
            {
                if ((!im.fromGroup) && (reason != "User does not exist.") && (client != null))
                    client.SendInstantMessage(new GridInstantMessage(
                            null, im.toAgentID,
                            "System", im.fromAgentID,
                            (byte)InstantMessageDialog.MessageFromAgent,
                            "User has too many IMs already, please try again later.",
                            false, Vector3.Zero));
                else if (client == null)
                    return;
            }
            else if ((im.offline != 0)
                && (!im.fromGroup || (im.fromGroup && m_ForwardOfflineGroupMessages)))
            {
                if (im.dialog == 32) //Group notice
                {
                    IGroupsModule module = m_SceneList[0].RequestModuleInterface<IGroupsModule>();
                    if (module != null)
                        im = module.BuildOfflineGroupNotice(im);
                    return;
                }
                if (client == null) return;
                IEmailModule emailModule = m_SceneList[0].RequestModuleInterface<IEmailModule> ();
                if (emailModule != null && m_SendOfflineMessagesToEmail)
                {
                    IUserProfileInfo profile = DataManager.DataManager.RequestPlugin<IProfileConnector> ().GetUserProfile (im.toAgentID);
                    if (profile != null && profile.IMViaEmail)
                    {
                        UserAccount account = m_SceneList[0].UserAccountService.GetUserAccount(null, im.toAgentID.ToString());
                        if (account != null && !string.IsNullOrEmpty(account.Email))
                        {
                            emailModule.SendEmail (UUID.Zero, account.Email, string.Format ("Offline Message from {0}", im.fromAgentName),
                                string.Format ("Time: {0}\n", Util.ToDateTime (im.timestamp).ToShortDateString ()) +
                                string.Format ("From: {0}\n", im.fromAgentName) +
                                string.Format("Message: {0}\n", im.message), m_SceneList[0]);
                        }
                    }
                }

                if(im.dialog == (byte)InstantMessageDialog.MessageFromAgent && !im.fromGroup)
                {
                    client.SendInstantMessage(new GridInstantMessage(
                            null, im.toAgentID,
                            "System", im.fromAgentID,
                            (byte)InstantMessageDialog.MessageFromAgent,
                            "Message saved, reason: " + reason,
                            false, new Vector3()));
                }

                if (im.dialog == (byte)InstantMessageDialog.InventoryOffered)
                    client.SendAlertMessage("User is not online. Inventory has been saved");
            }
            else if (im.offline == 0)
            {
                if (client == null) return;
                if(im.dialog == (byte)InstantMessageDialog.MessageFromAgent && !im.fromGroup)
                {
                    client.SendInstantMessage(new GridInstantMessage(
                            null, im.toAgentID,
                            "System", im.fromAgentID,
                            (byte)InstantMessageDialog.MessageFromAgent,
                            "Message saved, reason: " + reason,
                            false, new Vector3()));
                }

                if (im.dialog == (byte)InstantMessageDialog.InventoryOffered)
                    client.SendAlertMessage("User not able to be found. Inventory has been saved");
            }
        }
    }
}

