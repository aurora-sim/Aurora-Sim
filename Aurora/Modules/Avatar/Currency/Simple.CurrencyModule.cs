using Aurora.Framework;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Servers;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;

namespace Simple.Currency
{
    public class SimpleCurrencyModule : IMoneyModule, IService
    {
        #region Declares

        private SimpleCurrencyConfig Config
        {
            get { return m_connector.GetConfig(); }
        }
        private IScene m_scene;
        private SimpleCurrencyConnector m_connector;
        private IRegistryCore m_registry;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            if (config.Configs["Currency"] == null ||
                config.Configs["Currency"].GetString("Module", "") != "SimpleCurrency")
                return;

            m_registry = registry;
            m_connector = DataManager.RequestPlugin<ISimpleCurrencyConnector>() as SimpleCurrencyConnector;

            registry.RegisterModuleInterface<IMoneyModule>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_registry == null)
                return;
            ISyncMessageRecievedService syncRecievedService =
                registry.RequestModuleInterface<ISyncMessageRecievedService>();
            if (syncRecievedService != null)
                syncRecievedService.OnMessageReceived += syncRecievedService_OnMessageReceived;
        }

        public void FinishedStartup()
        {
            if (m_registry == null)
                return;
            ISceneManager manager = m_registry.RequestModuleInterface<ISceneManager>();
            if (manager != null)
            {
                manager.OnAddedScene += (scene) =>
                {
                                                m_scene = scene;
                                                scene.EventManager.OnNewClient += OnNewClient;
                                                scene.EventManager.OnClosingClient += OnClosingClient;
                                                scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
                                                scene.EventManager.OnValidateBuyLand += EventManager_OnValidateBuyLand;
                                                scene.RegisterModuleInterface<IMoneyModule>(this);
                                            };
                manager.OnCloseScene += (scene) =>
                                            {
                                                m_scene.EventManager.OnNewClient -= OnNewClient;
                                                m_scene.EventManager.OnClosingClient -= OnClosingClient;
                                                m_scene.EventManager.OnMakeRootAgent -= OnMakeRootAgent;
                                                scene.EventManager.OnValidateBuyLand -= EventManager_OnValidateBuyLand;
                                                m_scene.RegisterModuleInterface<IMoneyModule>(this);
                                                m_scene = null;
                                            };
            }

            if (!m_connector.DoRemoteCalls)
            {
                if ((m_connector.GetConfig().GiveStipends) && (m_connector.GetConfig().Stipend > 0))
                    new GiveStipends(m_connector.GetConfig(), m_registry, m_connector);
            }
        }

        private bool EventManager_OnValidateBuyLand(EventManager.LandBuyArgs e)
        {
            IParcelManagementModule parcelMangaement = m_scene.RequestModuleInterface<IParcelManagementModule>();
            if (parcelMangaement == null)
                return false;
            ILandObject lob = parcelMangaement.GetLandObject(e.parcelLocalID);

            if (lob != null)
            {
                UUID AuthorizedID = lob.LandData.AuthBuyerID;
                int saleprice = lob.LandData.SalePrice;
                UUID pOwnerID = lob.LandData.OwnerID;

                bool landforsale = ((lob.LandData.Flags &
                                     (uint)
                                     (ParcelFlags.ForSale | ParcelFlags.ForSaleObjects |
                                      ParcelFlags.SellParcelObjects)) != 0);
                if ((AuthorizedID == UUID.Zero || AuthorizedID == e.agentId) && e.parcelPrice >= saleprice &&
                    landforsale)
                {
                    if (m_connector.UserCurrencyTransfer(lob.LandData.OwnerID, e.agentId, UUID.Zero, UUID.Zero,
                                                         (uint) saleprice, "Land Buy", TransactionType.LandSale,
                                                         UUID.Zero))
                    {
                        e.parcelOwnerID = pOwnerID;
                        e.landValidated = true;
                        return true;
                    }
                    else
                    {
                        e.landValidated = false;
                    }
                }
            }
            return false;
        }

        #endregion

        #region IMoneyModule Members

        public int UploadCharge
        {
            get { return Config.PriceUpload; }
        }

        public int GroupCreationCharge
        {
            get { return Config.PriceGroupCreate; }
        }

        public int ClientPort
        {
            get 
            {
                int port = Config.ClientPort;
                if (port == 0) return (int)MainServer.Instance.Port;
                return port;
            }
        }

        public bool ObjectGiveMoney(UUID objectID, UUID fromID, UUID toID, int amount)
        {
            return m_connector.UserCurrencyTransfer(toID, fromID, UUID.Zero, objectID, (uint) amount, "Object payment",
                                                    TransactionType.ObjectPays, UUID.Zero);
        }

        public int Balance(UUID agentID)
        {
            return (int) m_connector.GetUserCurrency(agentID).Amount;
        }

        public bool Charge(IClientAPI client, int amount)
        {
            return m_connector.UserCurrencyTransfer(UUID.Zero, client.AgentId, UUID.Zero, UUID.Zero, (uint) amount,
                                                    "Charge", TransactionType.SystemGenerated, UUID.Zero);
        }

        public bool Charge(UUID agentID, int amount, string text)
        {
            return m_connector.UserCurrencyTransfer(UUID.Zero, agentID, UUID.Zero, UUID.Zero, (uint) amount, text,
                                                    TransactionType.SystemGenerated, UUID.Zero);
        }

        public event ObjectPaid OnObjectPaid;

        public void FireObjectPaid(UUID objectID, UUID agentID, int amount)
        {
            if (OnObjectPaid != null)
                OnObjectPaid(objectID, agentID, amount);
        }

        public bool Transfer(UUID toID, UUID fromID, int amount, string description)
        {
            return m_connector.UserCurrencyTransfer(toID, fromID, UUID.Zero, UUID.Zero, (uint) amount, description,
                                                    TransactionType.Gift, UUID.Zero);
        }

        public bool Transfer(UUID toID, UUID fromID, int amount, string description, TransactionType type)
        {
            return m_connector.UserCurrencyTransfer(toID, fromID, UUID.Zero, UUID.Zero, (uint) amount, description, type,
                                                    UUID.Zero);
        }

        public bool Transfer(UUID toID, UUID fromID, UUID toObjectID, UUID fromObjectID, int amount, string description,
                             TransactionType type)
        {
            bool result = m_connector.UserCurrencyTransfer(toID, fromID, toObjectID, fromObjectID, (uint)amount, description, type, UUID.Zero);
            if (toObjectID != UUID.Zero)
            {
                ISceneManager manager = m_registry.RequestModuleInterface<ISceneManager>();
                if(manager != null && manager.Scene != null)
                {
                    ISceneChildEntity ent = manager.Scene.GetSceneObjectPart(toObjectID);
                    if(ent != null)
                        FireObjectPaid(toObjectID, fromID, amount);
                }
            }
            return result;
        }

        public List<GroupAccountHistory> GetTransactions(UUID groupID, UUID agentID, int currentInterval,
                                                         int intervalDays)
        {
            return new List<GroupAccountHistory>();
        }

        public GroupBalance GetGroupBalance(UUID groupID)
        {
            return m_connector.GetGroupBalance(groupID);
        }

        #endregion

        #region Client Members

        private void OnNewClient(IClientAPI client)
        {
            client.OnEconomyDataRequest += EconomyDataRequestHandler;
            client.OnMoneyBalanceRequest += SendMoneyBalance;
            client.OnMoneyTransferRequest += ProcessMoneyTransferRequest;
            client.OnParcelBuyPass += ClientOnParcelBuyPass;
        }

        private void OnMakeRootAgent(IScenePresence presence)
        {
            presence.ControllingClient.SendMoneyBalance(UUID.Zero, true, new byte[0],
                                                        (int) m_connector.GetUserCurrency(presence.UUID).Amount);
        }

        protected void OnClosingClient(IClientAPI client)
        {
            client.OnEconomyDataRequest -= EconomyDataRequestHandler;
            client.OnMoneyBalanceRequest -= SendMoneyBalance;
            client.OnMoneyTransferRequest -= ProcessMoneyTransferRequest;
            client.OnParcelBuyPass -= ClientOnParcelBuyPass;
        }

        private void ProcessMoneyTransferRequest(UUID fromID, UUID toID, int amount, int type, string description)
        {
            if (toID != UUID.Zero)
            {
                ISceneManager manager = m_registry.RequestModuleInterface<ISceneManager>();
                if (manager != null && manager.Scene != null)
                {
                    ISceneChildEntity ent = manager.Scene.GetSceneObjectPart(toID);
                    if (ent != null)
                    {
                        bool success = m_connector.UserCurrencyTransfer(ent.OwnerID, fromID, UUID.Zero, UUID.Zero, (uint)amount, description,
                                             (TransactionType)type, UUID.Random());
                        if (success)
                            FireObjectPaid(toID, fromID, amount);
                    }
                    else
                    {
                        m_connector.UserCurrencyTransfer(toID, fromID, UUID.Zero, UUID.Zero, (uint)amount, description,
                                                 (TransactionType)type, UUID.Random());
                    }
                }
            }
        }

        private bool ValidateLandBuy(EventManager.LandBuyArgs e)
        {
            return m_connector.UserCurrencyTransfer(e.parcelOwnerID, e.agentId, UUID.Zero, UUID.Zero,
                                                    (uint) e.parcelPrice, "Land Purchase", TransactionType.LandSale,
                                                    UUID.Random());
        }

        private void EconomyDataRequestHandler(IClientAPI remoteClient)
        {
            if (Config == null)
            {
                remoteClient.SendEconomyData(0, remoteClient.Scene.RegionInfo.ObjectCapacity,
                                             remoteClient.Scene.RegionInfo.ObjectCapacity,
                                             0, 0,
                                             0, 0,
                                             0, 0,
                                             0,
                                             0, 0,
                                             0, 0,
                                             0,
                                             0, 0);
            }
            else
                remoteClient.SendEconomyData(0, remoteClient.Scene.RegionInfo.ObjectCapacity,
                                             remoteClient.Scene.RegionInfo.ObjectCapacity,
                                             0, Config.PriceGroupCreate,
                                             0, 0,
                                             0, 0,
                                             0,
                                             0, 0,
                                             0, 0,
                                             Config.PriceUpload,
                                             0, 0);
        }

        private void SendMoneyBalance(IClientAPI client, UUID agentId, UUID sessionId, UUID transactionId)
        {
            if (client.AgentId == agentId && client.SessionId == sessionId)
                client.SendMoneyBalance(transactionId, true, new byte[0],
                                        (int) m_connector.GetUserCurrency(client.AgentId).Amount);
            else
                client.SendAlertMessage("Unable to send your money balance to you!");
        }

        /// <summary>
        ///     The client wants to buy a pass for a parcel
        /// </summary>
        /// <param name="client"></param>
        /// <param name="fromID"></param>
        /// <param name="parcelLocalId"></param>
        private void ClientOnParcelBuyPass(IClientAPI client, UUID fromID, int parcelLocalId)
        {
            IScenePresence agentSp = m_scene.GetScenePresence(client.AgentId);
            IParcelManagementModule parcelManagement = agentSp.Scene.RequestModuleInterface<IParcelManagementModule>();
            ILandObject landParcel = null;
            List<ILandObject> land = parcelManagement.AllParcels();
            foreach (ILandObject landObject in land)
            {
                if (landObject.LandData.LocalID == parcelLocalId)
                {
                    landParcel = landObject;
                }
            }
            if (landParcel != null)
            {
                bool giveResult = m_connector.UserCurrencyTransfer(landParcel.LandData.OwnerID, fromID, UUID.Zero,
                                                                   UUID.Zero,
                                                                   (uint) landParcel.LandData.PassPrice, "Parcel Pass",
                                                                   TransactionType.LandPassSale, UUID.Random());
                if (giveResult)
                {
                    ParcelManager.ParcelAccessEntry entry
                        = new ParcelManager.ParcelAccessEntry
                              {
                                  AgentID = fromID,
                                  Flags = AccessList.Access,
                                  Time = DateTime.Now.AddHours(landParcel.LandData.PassHours)
                              };
                    landParcel.LandData.ParcelAccessList.Add(entry);
                    agentSp.ControllingClient.SendAgentAlertMessage("You have been added to the parcel access list.",
                                                                    false);
                }
            }
            else
            {
                agentSp.ControllingClient.SendAgentAlertMessage("Unable to find parcel.", false);
            }
        }

        #endregion

        #region Service Members

        private OSDMap syncRecievedService_OnMessageReceived(OSDMap message)
        {
            string method = message["Method"];
            if (method == "UpdateMoneyBalance")
            {
                UUID agentID = message["AgentID"];
                int Amount = message["Amount"];
                string Message = message["Message"];
                UUID TransactionID = message["TransactionID"];
                IDialogModule dialogModule = m_scene.RequestModuleInterface<IDialogModule>();
                if (dialogModule != null && !string.IsNullOrEmpty(Message))
                {
                    IScenePresence sp = m_scene.GetScenePresence(agentID);
                    if (sp != null)
                    {
                        sp.ControllingClient.SendMoneyBalance(TransactionID, true, Utils.StringToBytes(Message), Amount);
                        dialogModule.SendAlertToUser(agentID, Message);
                    }
                }
            }
            else if (method == "GetLandData")
            {
                UUID agentID = message["AgentID"];
                IParcelManagementModule parcelManagement = m_scene.RequestModuleInterface<IParcelManagementModule>();
                if (parcelManagement != null)
                {
                    IScenePresence sp = m_scene.GetScenePresence(agentID);
                    if (sp != null)
                    {
                        ILandObject lo = sp.CurrentParcel;
                        if ((lo.LandData.Flags & (uint) ParcelFlags.ForSale) == (uint) ParcelFlags.ForSale)
                        {
                            if (lo.LandData.AuthBuyerID != UUID.Zero && lo.LandData.AuthBuyerID != agentID)
                                return new OSDMap() {new KeyValuePair<string, OSD>("Success", false)};
                            OSDMap map = lo.LandData.ToOSD();
                            map["Success"] = true;
                            return map;
                        }
                    }
                }
                return new OSDMap() {new KeyValuePair<string, OSD>("Success", false)};
            }
            return null;
        }

        /// <summary>
        ///     All message for money actually go through this function. Which also update the balance
        /// </summary>
        /// <param name="toId"></param>
        /// <param name="message"></param>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        public bool SendGridMessage(UUID toId, string message, UUID transactionId)
        {
            IDialogModule dialogModule = m_scene.RequestModuleInterface<IDialogModule>();
            if (dialogModule != null)
            {
                IScenePresence icapiTo = m_scene.GetScenePresence(toId);
                if (icapiTo != null)
                {
                    icapiTo.ControllingClient.SendMoneyBalance(transactionId, true, Utils.StringToBytes(message),
                                                               (int) m_connector.GetUserCurrency(icapiTo.UUID).Amount);
                    dialogModule.SendAlertToUser(toId, message);
                }

                return true;
            }
            return false;
        }

        #endregion
    }
}