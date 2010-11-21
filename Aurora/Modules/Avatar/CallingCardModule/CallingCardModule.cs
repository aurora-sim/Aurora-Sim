using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using Aurora.Framework;

namespace Aurora.Modules
{
    public class CallingCardModule : ISharedRegionModule, ICallingCardModule
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected List<Scene> m_scenes = new List<Scene>();
        protected bool m_Enabled = true;
        protected Dictionary<UUID, UUID> m_pendingCallingcardRequests = new Dictionary<UUID, UUID>();

        #endregion

        #region ISharedRegionModule

        public void Initialise(IConfigSource source)
        {
            IConfig ccmModuleConfig = source.Configs["CallingCardModule"];
            if (ccmModuleConfig != null)
                m_Enabled = ccmModuleConfig.GetBoolean("Enabled", true);
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            if (!m_scenes.Contains(scene))
                m_scenes.Add(scene);

            scene.RegisterModuleInterface<ICallingCardModule>(this);
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            if (m_scenes.Contains(scene))
                m_scenes.Remove(scene);
            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;

            scene.UnregisterModuleInterface<ICallingCardModule>(this);
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;
            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public void PostInitialise()
        {
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
        /// This comes from the Friends module when a friend is added or when a user gives another user a calling card
        /// </summary>
        /// <param name="client"></param>
        /// <param name="destID"></param>
        /// <param name="transactionID"></param>
        private void OnOfferCallingCard(IClientAPI client, UUID destID, UUID transactionID)
        {
            m_log.DebugFormat("[AURORA CALLING CARD MODULE]: got offer from {0} for {1}, transaction {2}",
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
        /// Create the calling card inventory item in the user's inventory
        /// </summary>
        /// <param name="client"></param>
        /// <param name="creator"></param>
        /// <param name="folder"></param>
        /// <param name="name"></param>
        public void CreateCallingCard(IClientAPI client, UUID creator, UUID folder, string name)
        {
            m_log.Debug("[AURORA CALLING CARD MODULE]: Creating calling card for " + client.Name);
            InventoryItemBase item = new InventoryItemBase();
            item.AssetID = UUID.Zero;
            item.AssetType = (int)AssetType.CallingCard;
            item.BasePermissions = (uint)(PermissionMask.Copy | PermissionMask.Modify);
            item.CreationDate = Util.UnixTimeSinceEpoch();
            item.CreatorId = creator.ToString();
            item.CurrentPermissions = item.BasePermissions;
            item.Description = "";
            item.EveryOnePermissions = (uint)PermissionMask.None;
            item.Flags = 0;
            item.Folder = folder;
            item.GroupID = UUID.Zero;
            item.GroupOwned = false;
            item.ID = UUID.Random();
            item.InvType = (int)InventoryType.CallingCard;
            item.Name = name;
            item.NextPermissions = item.EveryOnePermissions;
            item.Owner = client.AgentId;
            item.SalePrice = 10;
            item.SaleType = (byte)SaleType.Not;
            ((Scene)client.Scene).AddInventoryItem(client, item);
        }

        /// <summary>
        /// Accept the user's calling card and add the card to their inventory
        /// </summary>
        /// <param name="client"></param>
        /// <param name="transactionID"></param>
        /// <param name="folderID"></param>
        private void OnAcceptCallingCard(IClientAPI client, UUID transactionID, UUID folderID)
        {
            m_log.DebugFormat("[AURORA CALLING CARD MODULE]: User {0} ({1} {2}) accepted tid {3}, folder {4}",
                              client.AgentId,
                              client.FirstName, client.LastName,
                              transactionID, folderID);
            UUID destID;
            lock (m_pendingCallingcardRequests)
            {
                if (!m_pendingCallingcardRequests.TryGetValue(transactionID, out destID))
                {
                    m_log.WarnFormat("[AURORA CALLING CARD MODULE]: Got a AcceptCallingCard from {0} without an offer before.",
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
        /// Remove the potential calling card and notify the other user
        /// </summary>
        /// <param name="client"></param>
        /// <param name="transactionID"></param>
        private void OnDeclineCallingCard(IClientAPI client, UUID transactionID)
        {
            m_log.DebugFormat("[AURORA CALLING CARD MODULE]: User {0} (ID:{1}) declined card, tid {2}",
                              client.Name, client.AgentId, transactionID);
            UUID destID;
            lock (m_pendingCallingcardRequests)
            {
                if (!m_pendingCallingcardRequests.TryGetValue(transactionID, out destID))
                {
                    m_log.WarnFormat("[AURORA CALLING CARD MODULE]: Got a AcceptCallingCard from {0} without an offer before.",
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
        /// Find the client for a ID
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns></returns>
        public IClientAPI LocateClientObject(UUID agentID)
        {
            Scene scene = GetClientScene(agentID);
            if (scene == null)
                return null;

            ScenePresence presence = scene.GetScenePresence(agentID);
            if (presence == null)
                return null;

            return presence.ControllingClient;
        }

        /// <summary>
        /// Find the scene for an agent
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        private Scene GetClientScene(UUID agentId)
        {
            lock (m_scenes)
            {
                foreach (Scene scene in m_scenes)
                {
                    ScenePresence presence = scene.GetScenePresence(agentId);
                    if (presence != null)
                    {
                        if (!presence.IsChildAgent)
                            return scene;
                    }
                }
            }
            return null;
        }

        #endregion
    }
}
