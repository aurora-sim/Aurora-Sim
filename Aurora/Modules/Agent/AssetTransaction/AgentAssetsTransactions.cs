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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Agent.AssetTransaction
{
    /// <summary>
    ///   Manage asset transactions for a single agent.
    /// </summary>
    public class AgentAssetTransactions
    {
        // Fields
        public AssetTransactionModule Manager;
        public UUID UserID;
        public Dictionary<UUID, AssetXferUploader> XferUploaders = new Dictionary<UUID, AssetXferUploader>();

        // Methods
        public AgentAssetTransactions(UUID agentID, AssetTransactionModule manager)
        {
            UserID = agentID;
            Manager = manager;
        }

        public AssetXferUploader RequestXferUploader(UUID transactionID)
        {
            if (!XferUploaders.ContainsKey(transactionID))
            {
                AssetXferUploader uploader = new AssetXferUploader(this);

                lock (XferUploaders)
                {
                    XferUploaders.Add(transactionID, uploader);
                }

                return uploader;
            }
            return null;
        }

        public void HandleXfer(IClientAPI remoteClient, ulong xferID, uint packetID, byte[] data)
        {
            lock (XferUploaders)
            {
#if (!ISWIN)
                foreach (AssetXferUploader uploader in XferUploaders.Values)
                {
                    if (uploader.XferID == xferID)
                    {
                        uploader.HandleXferPacket(remoteClient, xferID, packetID, data);
                        break;
                    }
                }
#else
                foreach (AssetXferUploader uploader in XferUploaders.Values.Where(uploader => uploader.XferID == xferID))
                {
                    uploader.HandleXferPacket(remoteClient, xferID, packetID, data);
                    break;
                }
#endif
            }
        }

        public void RequestCreateInventoryItem(IClientAPI remoteClient, UUID transactionID, UUID folderID,
                                               uint callbackID, string description, string name, sbyte invType,
                                               sbyte type, byte wearableType, uint nextOwnerMask)
        {
            if (XferUploaders.ContainsKey(transactionID))
            {
                XferUploaders[transactionID].RequestCreateInventoryItem(remoteClient, transactionID, folderID,
                                                                        callbackID, description, name, invType, type,
                                                                        wearableType, nextOwnerMask);
            }
        }


        /// <summary>
        ///   Get an uploaded asset.  If the data is successfully retrieved, the transaction will be removed.
        /// </summary>
        /// <param name = "transactionID"></param>
        /// <returns>The asset if the upload has completed, null if it has not.</returns>
        public AssetBase GetTransactionAsset(UUID transactionID)
        {
            if (XferUploaders.ContainsKey(transactionID))
            {
                AssetXferUploader uploader = XferUploaders[transactionID];
                AssetBase asset = uploader.GetAssetData();

                lock (XferUploaders)
                {
                    XferUploaders.Remove(transactionID);
                }

                return asset;
            }

            return null;
        }

        //private void CreateItemFromUpload(AssetBase asset, IClientAPI ourClient, UUID inventoryFolderID, uint nextPerms, uint wearableType)
        //{
        //    Manager.MyScene.CommsManager.AssetCache.AddAsset(asset);
        //    CachedUserInfo userInfo = Manager.MyScene.CommsManager.UserProfileCacheService.GetUserDetails(
        //            ourClient.AgentId);

        //    if (userInfo != null)
        //    {
        //        InventoryItemBase item = new InventoryItemBase();
        //        item.Owner = ourClient.AgentId;
        //        item.Creator = ourClient.AgentId;
        //        item.ID = UUID.Random();
        //        item.AssetID = asset.FullID;
        //        item.Description = asset.Description;
        //        item.Name = asset.Name;
        //        item.AssetType = asset.Type;
        //        item.InvType = asset.Type;
        //        item.Folder = inventoryFolderID;
        //        item.BasePermissions = 0x7fffffff;
        //        item.CurrentPermissions = 0x7fffffff;
        //        item.EveryOnePermissions = 0;
        //        item.NextPermissions = nextPerms;
        //        item.Flags = wearableType;
        //        item.CreationDate = Util.UnixTimeSinceEpoch();

        //        userInfo.AddItem(item);
        //        ourClient.SendInventoryItemCreateUpdate(item);
        //    }
        //    else
        //    {
        //        MainConsole.Instance.ErrorFormat(
        //            "[ASSET TRANSACTIONS]: Could not find user {0} for inventory item creation",
        //            ourClient.AgentId);
        //    }
        //}

        public void RequestUpdateTaskInventoryItem(
            IClientAPI remoteClient, ISceneChildEntity part, UUID transactionID, TaskInventoryItem item)
        {
            if (XferUploaders.ContainsKey(transactionID))
            {
                AssetBase asset = XferUploaders[transactionID].GetAssetData();
                if (asset != null)
                {
                    MainConsole.Instance.DebugFormat(
                        "[ASSET TRANSACTIONS]: Updating task item {0} in {1} with asset in transaction {2}",
                        item.Name, part.Name, transactionID);

                    asset.Name = item.Name;
                    asset.Description = item.Description;
                    asset.Type = (sbyte) item.Type;
                    item.AssetID = asset.ID;

                    IMonitorModule monitorModule = Manager.MyScene.RequestModuleInterface<IMonitorModule>();
                    if (monitorModule != null)
                    {
                        INetworkMonitor networkMonitor =
                            (INetworkMonitor)
                            monitorModule.GetMonitor(Manager.MyScene.RegionInfo.RegionID.ToString(),
                                                     MonitorModuleHelper.NetworkMonitor);
                        networkMonitor.AddPendingUploads(-1);
                    }

                    asset.ID = Manager.MyScene.AssetService.Store(asset);
                    item.AssetID = asset.ID;

                    if (part.Inventory.UpdateInventoryItem(item))
                    {
                        if ((InventoryType) item.InvType == InventoryType.Notecard)
                            remoteClient.SendAgentAlertMessage("Notecard saved", false);
                        else if ((InventoryType) item.InvType == InventoryType.LSL)
                            remoteClient.SendAgentAlertMessage("Script saved", false);
                        else
                            remoteClient.SendAgentAlertMessage("Item saved", false);

                        part.GetProperties(remoteClient);
                    }
                }
            }
        }


        public void RequestUpdateInventoryItem(IClientAPI remoteClient, UUID transactionID,
                                               InventoryItemBase item)
        {
            if (XferUploaders.ContainsKey(transactionID))
            {
                UUID assetID = UUID.Combine(transactionID, remoteClient.SecureSessionId);

                AssetXferUploader uploader = XferUploaders[transactionID];
                if (!uploader.Finished)
                {
                    uploader.FinishedEvent = () =>
                        UpdateInventoryItemWithAsset(item, assetID, transactionID);
                    return;
                }

                UpdateInventoryItemWithAsset(item, assetID, transactionID);
            }
        }

        private void UpdateInventoryItemWithAsset(InventoryItemBase item, UUID assetID, UUID transactionID)
        {
            AssetXferUploader uploader = XferUploaders[transactionID];
            AssetBase asset = uploader.GetAssetData();
            if (asset != null && asset.ID == assetID)
            {
                // Assets never get updated, new ones get created
                asset.ID = UUID.Random();
                asset.Name = item.Name;
                asset.Description = item.Description;
                asset.Type = (sbyte)item.AssetType;
                item.AssetID = asset.ID;

                asset.ID = Manager.MyScene.AssetService.Store(asset);
                item.AssetID = asset.ID;
                XferUploaders.Remove(transactionID);
            }
            else
                return;

            IMonitorModule monitorModule = Manager.MyScene.RequestModuleInterface<IMonitorModule>();
            if (monitorModule != null)
            {
                INetworkMonitor networkMonitor =
                    (INetworkMonitor)
                    monitorModule.GetMonitor(Manager.MyScene.RegionInfo.RegionID.ToString(),
                                             MonitorModuleHelper.NetworkMonitor);
                networkMonitor.AddPendingUploads(-1);
            }

            IInventoryService invService = Manager.MyScene.InventoryService;
            invService.UpdateItem(item);
        }
    }
}