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
using System.IO;
using System.Reflection;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.Modules.Agent.AssetTransaction
{
    public class AssetXferUploader
    {
        private readonly AgentAssetTransactions m_userTransactions;

        private UUID InventFolder = UUID.Zero;
        private UUID TransactionID = UUID.Zero;
        public ulong XferID;
        private sbyte invType;
        private AssetBase m_asset;
        private bool m_createItem;
        private uint m_createItemCallback;
        private string m_description = String.Empty;
        private bool m_finished;
        public bool Finished { get { return m_finished; } }
        private string m_name = String.Empty;
        private bool m_storeLocal;
        private uint nextPerm;
        private sbyte type;
        private byte wearableType;
        public NoParam FinishedEvent = null;

        public AssetXferUploader(AgentAssetTransactions transactions)
        {
            m_userTransactions = transactions;
        }

        /// <summary>
        ///   Process transfer data received from the client.
        /// </summary>
        /// <param name = "xferID"></param>
        /// <param name = "packetID"></param>
        /// <param name = "data"></param>
        /// <returns>True if the transfer is complete, false otherwise or if the xferID was not valid</returns>
        public bool HandleXferPacket(IClientAPI remoteClient, ulong xferID, uint packetID, byte[] data)
        {
            if (XferID == xferID)
            {
                if (m_asset.Data.Length > 1)
                {
                    byte[] destinationArray = new byte[m_asset.Data.Length + data.Length];
                    Array.Copy(m_asset.Data, 0, destinationArray, 0, m_asset.Data.Length);
                    Array.Copy(data, 0, destinationArray, m_asset.Data.Length, data.Length);
                    m_asset.Data = destinationArray;
                }
                else
                {
                    byte[] buffer2 = new byte[data.Length - 4];
                    Array.Copy(data, 4, buffer2, 0, data.Length - 4);
                    m_asset.Data = buffer2;
                }

                remoteClient.SendConfirmXfer(xferID, packetID);

                if ((packetID & 0x80000000) != 0)
                {
                    SendCompleteMessage(remoteClient);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   Initialise asset transfer from the client
        /// </summary>
        /// <param name = "xferID"></param>
        /// <param name = "packetID"></param>
        /// <param name = "data"></param>
        /// <returns>True if the transfer is complete, false otherwise</returns>
        public bool Initialise(IClientAPI remoteClient, UUID assetID, UUID transaction, sbyte type, byte[] data,
                               bool storeLocal, bool tempFile)
        {
            m_asset = new AssetBase(assetID, "blank", (AssetType) type, remoteClient.AgentId)
                          {Data = data, Description = "empty"};
            if (storeLocal) m_asset.Flags |= AssetFlags.Local;
            if (tempFile) m_asset.Flags |= AssetFlags.Temporary;

            TransactionID = transaction;
            m_storeLocal = storeLocal;

            if (m_asset.Data.Length > 2)
            {
                SendCompleteMessage(remoteClient);
                return true;
            }
            else
            {
                RequestStartXfer(remoteClient);
            }

            return false;
        }

        protected void RequestStartXfer(IClientAPI remoteClient)
        {
            XferID = Util.GetNextXferID();
            remoteClient.SendXferRequest(XferID, short.Parse(m_asset.Type.ToString()), m_asset.ID, 0, new byte[0]);
        }

        protected void SendCompleteMessage(IClientAPI remoteClient)
        {
            m_finished = true;
            if (FinishedEvent != null)
                FinishedEvent();

            if (m_createItem)
            {
                DoCreateItem(m_createItemCallback, remoteClient);
            }
            else if (m_storeLocal)
            {
                m_asset.ID = m_userTransactions.Manager.MyScene.AssetService.Store(m_asset);
            }
            remoteClient.SendAssetUploadCompleteMessage((sbyte) m_asset.Type, true, m_asset.ID);

            IMonitorModule monitorModule = m_userTransactions.Manager.MyScene.RequestModuleInterface<IMonitorModule>();
            if (monitorModule != null)
            {
                INetworkMonitor networkMonitor =
                    (INetworkMonitor)
                    monitorModule.GetMonitor(m_userTransactions.Manager.MyScene.RegionInfo.RegionID.ToString(),
                                             MonitorModuleHelper.NetworkMonitor);
                networkMonitor.AddPendingUploads(-1);
            }

            MainConsole.Instance.DebugFormat(
                "[ASSET TRANSACTIONS]: Uploaded asset {0} for transaction {1}", m_asset.ID, TransactionID);
        }

        public void RequestCreateInventoryItem(IClientAPI remoteClient, UUID transactionID, UUID folderID,
                                               uint callbackID, string description, string name, sbyte invType,
                                               sbyte type, byte wearableType, uint nextOwnerMask)
        {
            if (TransactionID == transactionID)
            {
                InventFolder = folderID;
                m_name = name;
                m_description = description;
                this.type = type;
                this.invType = invType;
                this.wearableType = wearableType;
                nextPerm = nextOwnerMask;
                m_asset.Name = name;
                m_asset.Description = description;
                m_asset.Type = type;

                if (m_finished)
                {
                    DoCreateItem(callbackID, remoteClient);
                }
                else
                {
                    m_createItem = true; //set flag so the inventory item is created when upload is complete
                    m_createItemCallback = callbackID;
                }
            }
        }

        private void DoCreateItem(uint callbackID, IClientAPI remoteClient)
        {
            m_asset.ID = m_userTransactions.Manager.MyScene.AssetService.Store(m_asset);

            IMonitorModule monitorModule = m_userTransactions.Manager.MyScene.RequestModuleInterface<IMonitorModule>();
            if (monitorModule != null)
            {
                INetworkMonitor networkMonitor =
                    (INetworkMonitor)
                    monitorModule.GetMonitor(m_userTransactions.Manager.MyScene.RegionInfo.RegionID.ToString(),
                                             MonitorModuleHelper.NetworkMonitor);
                networkMonitor.AddPendingUploads(-1);
            }

            InventoryItemBase item = new InventoryItemBase
                                         {
                                             Owner = remoteClient.AgentId,
                                             CreatorId = remoteClient.AgentId.ToString(),
                                             ID = UUID.Random(),
                                             AssetID = m_asset.ID,
                                             Description = m_description,
                                             Name = m_name,
                                             AssetType = type,
                                             InvType = invType,
                                             Folder = InventFolder,
                                             BasePermissions = 0x7fffffff,
                                             CurrentPermissions = 0x7fffffff,
                                             GroupPermissions = 0,
                                             EveryOnePermissions = 0,
                                             NextPermissions = nextPerm,
                                             Flags = wearableType,
                                             CreationDate = Util.UnixTimeSinceEpoch()
                                         };

            ILLClientInventory inventoryModule =
                m_userTransactions.Manager.MyScene.RequestModuleInterface<ILLClientInventory>();
            if (inventoryModule != null && inventoryModule.AddInventoryItem(item))
                remoteClient.SendInventoryItemCreateUpdate(item, callbackID);
            else
                remoteClient.SendAlertMessage("Unable to create inventory item");
        }

        /// <summary>
        ///   Get the asset data uploaded in this transfer.
        /// </summary>
        /// <returns>null if the asset has not finished uploading</returns>
        public AssetBase GetAssetData()
        {
            if (m_finished)
            {
                return m_asset;
            }

            return null;
        }
    }
}