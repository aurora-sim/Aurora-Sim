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

using Aurora.Framework;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Services.ClassHelpers.Inventory;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using System;
using System.Collections.Generic;

namespace Aurora.Modules.CallingCards
{
    public class CallingCardModule : INonSharedRegionModule, ICallingCardModule
    {
        #region Declares

        protected bool m_Enabled = true;
        protected Dictionary<UUID, UUID> m_pendingCallingcardRequests = new Dictionary<UUID, UUID>();
        protected IScene m_Scene;

        #endregion

        #region INonSharedRegionModule

        public void Initialise(IConfigSource source)
        {
            IConfig ccmModuleConfig = source.Configs["CallingCardModule"];
            if (ccmModuleConfig != null)
                m_Enabled = ccmModuleConfig.GetBoolean("Enabled", true);
        }

        public void AddRegion(IScene scene)
        {
            if (!m_Enabled)
                return;

            m_Scene = scene;

            scene.RegisterModuleInterface<ICallingCardModule>(this);
        }

        public void RemoveRegion(IScene scene)
        {
            if (!m_Enabled)
                return;

            m_Scene = null;

            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;

            scene.UnregisterModuleInterface<ICallingCardModule>(this);
        }

        public void RegionLoaded(IScene scene)
        {
            if (!m_Enabled)
                return;
            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public void Close()
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "AuroraCallingCardModule"; }
        }

        #endregion

        #region Client

        private void OnNewClient(IClientAPI client)
        {
            // ... calling card handling...
            client.OnOfferCallingCard += OnOfferCallingCard;
            client.OnAcceptCallingCard += OnAcceptCallingCard;
            client.OnDeclineCallingCard += OnDeclineCallingCard;
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnOfferCallingCard -= OnOfferCallingCard;
            client.OnAcceptCallingCard -= OnAcceptCallingCard;
            client.OnDeclineCallingCard -= OnDeclineCallingCard;
        }

        #endregion

        #region ICallingCardModule interface

        /// <summary>
        ///     Create the calling card inventory item in the user's inventory
        /// </summary>
        /// <param name="client"></param>
        /// <param name="creator"></param>
        /// <param name="folder"></param>
        /// <param name="name"></param>
        public void CreateCallingCard(IClientAPI client, UUID creator, UUID folder, string name)
        {
            MainConsole.Instance.Debug("[AURORA CALLING CARD MODULE]: Creating calling card for " + client.Name);
            InventoryItemBase item = new InventoryItemBase
                                         {
                                             AssetID = UUID.Zero,
                                             AssetType = (int) AssetType.CallingCard,
                                             BasePermissions = (uint) (PermissionMask.Copy | PermissionMask.Modify),
                                             CurrentPermissions = (uint) (PermissionMask.Copy | PermissionMask.Modify),
                                             NextPermissions = (uint) PermissionMask.None,
                                             CreationDate = Util.UnixTimeSinceEpoch(),
                                             CreatorId = creator.ToString(),
                                             Description = "",
                                             EveryOnePermissions = (uint) PermissionMask.None,
                                             Flags = 0,
                                             Folder = folder,
                                             GroupID = UUID.Zero,
                                             GroupOwned = false,
                                             ID = UUID.Random(),
                                             InvType = (int) InventoryType.CallingCard,
                                             Name = name,
                                             Owner = client.AgentId,
                                             SalePrice = 10,
                                             SaleType = (byte) SaleType.Not
                                         };


            ILLClientInventory inventory = client.Scene.RequestModuleInterface<ILLClientInventory>();
            if (inventory != null)
                inventory.AddInventoryItemAsync(client, item);
        }

        /// <summary>
        ///     This comes from the Friends module when a friend is added or when a user gives another user a calling card
        /// </summary>
        /// <param name="client"></param>
        /// <param name="destID"></param>
        /// <param name="transactionID"></param>
        private void OnOfferCallingCard(IClientAPI client, UUID destID, UUID transactionID)
        {
            MainConsole.Instance.DebugFormat(
                "[AURORA CALLING CARD MODULE]: got offer from {0} for {1}, transaction {2}",
                client.AgentId, destID, transactionID);

            IClientAPI friendClient = LocateClientObject(destID);
            if (friendClient == null)
            {
                client.SendAlertMessage("The person you have offered a card to can't be found anymore.");
                return;
            }

            lock (m_pendingCallingcardRequests)
            {
                m_pendingCallingcardRequests[transactionID] = client.AgentId;
            }
            // inform the destination agent about the offer
            friendClient.SendOfferCallingCard(client.AgentId, transactionID);
        }

        /// <summary>
        ///     Accept the user's calling card and add the card to their inventory
        /// </summary>
        /// <param name="client"></param>
        /// <param name="transactionID"></param>
        /// <param name="folderID"></param>
        private void OnAcceptCallingCard(IClientAPI client, UUID transactionID, UUID folderID)
        {
            MainConsole.Instance.DebugFormat(
                "[AURORA CALLING CARD MODULE]: User {0} ({1} {2}) accepted tid {3}, folder {4}",
                client.AgentId,
                client.FirstName, client.LastName,
                transactionID, folderID);
            UUID destID;
            lock (m_pendingCallingcardRequests)
            {
                if (!m_pendingCallingcardRequests.TryGetValue(transactionID, out destID))
                {
                    MainConsole.Instance.WarnFormat(
                        "[AURORA CALLING CARD MODULE]: Got a AcceptCallingCard from {0} without an offer before.",
                        client.Name);
                    return;
                }
                // else found pending calling card request with that transaction.
                m_pendingCallingcardRequests.Remove(transactionID);
            }


            IClientAPI friendClient = LocateClientObject(destID);
            // inform sender of the card that destination accepted the offer
            if (friendClient != null)
                friendClient.SendAcceptCallingCard(transactionID);

            // put a calling card into the inventory of receiver
            CreateCallingCard(client, destID, folderID, friendClient.Name);
        }

        /// <summary>
        ///     Remove the potential calling card and notify the other user
        /// </summary>
        /// <param name="client"></param>
        /// <param name="transactionID"></param>
        private void OnDeclineCallingCard(IClientAPI client, UUID transactionID)
        {
            MainConsole.Instance.DebugFormat("[AURORA CALLING CARD MODULE]: User {0} (ID:{1}) declined card, tid {2}",
                                             client.Name, client.AgentId, transactionID);
            UUID destID;
            lock (m_pendingCallingcardRequests)
            {
                if (!m_pendingCallingcardRequests.TryGetValue(transactionID, out destID))
                {
                    MainConsole.Instance.WarnFormat(
                        "[AURORA CALLING CARD MODULE]: Got a AcceptCallingCard from {0} without an offer before.",
                        client.Name);
                    return;
                }
                // else found pending calling card request with that transaction.
                m_pendingCallingcardRequests.Remove(transactionID);
            }

            IClientAPI friendClient = LocateClientObject(destID);
            // inform sender of the card that destination declined the offer
            if (friendClient != null)
                friendClient.SendDeclineCallingCard(transactionID);
        }

        #endregion

        #region Helpers

        /// <summary>
        ///     Find the client for a ID
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns></returns>
        public IClientAPI LocateClientObject(UUID agentID)
        {
            IScenePresence presence = m_Scene.GetScenePresence(agentID);
            if (presence == null)
                return null;

            return presence.ControllingClient;
        }

        #endregion
    }
}