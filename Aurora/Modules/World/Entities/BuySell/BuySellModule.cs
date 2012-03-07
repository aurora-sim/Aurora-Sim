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
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;

namespace Aurora.Modules.Entities.BuySell
{
    public class BuySellModule : IBuySellModule, INonSharedRegionModule
    {
//        private static readonly ILog MainConsole.Instance = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected IDialogModule m_dialogModule;
        protected IScene m_scene;

        #region IBuySellModule Members

        public bool BuyObject(IClientAPI remoteClient, UUID categoryID, uint localID, byte saleType, int salePrice)
        {
            ISceneChildEntity part = m_scene.GetSceneObjectPart(localID);

            if (part == null)
                return false;

            if (part.ParentEntity == null)
                return false;

            ISceneEntity group = part.ParentEntity;
            ILLClientInventory inventoryModule = m_scene.RequestModuleInterface<ILLClientInventory>();

            switch (saleType)
            {
                case 1: // Sell as original (in-place sale)
                    uint effectivePerms = group.GetEffectivePermissions();

                    if ((effectivePerms & (uint) PermissionMask.Transfer) == 0)
                    {
                        if (m_dialogModule != null)
                            m_dialogModule.SendAlertToUser(remoteClient, "This item doesn't appear to be for sale");
                        return false;
                    }

                    group.SetOwnerId(remoteClient.AgentId);
                    group.SetRootPartOwner(part, remoteClient.AgentId, remoteClient.ActiveGroupId);

                    if (m_scene.Permissions.PropagatePermissions())
                    {
                        foreach (ISceneChildEntity child in group.ChildrenEntities())
                        {
                            child.Inventory.ChangeInventoryOwner(remoteClient.AgentId);
                            child.TriggerScriptChangedEvent(Changed.OWNER);
                            child.ApplyNextOwnerPermissions();
                        }
                    }

                    part.ObjectSaleType = 0;
                    part.SalePrice = 10;

                    group.HasGroupChanged = true;
                    part.GetProperties(remoteClient);
                    part.TriggerScriptChangedEvent(Changed.OWNER);
                    group.ResumeScripts();
                    part.ScheduleUpdate(PrimUpdateFlags.ForcedFullUpdate);

                    break;

                case 2: // Sell a copy
                    Vector3 inventoryStoredPosition = new Vector3
                        (((group.AbsolutePosition.X > m_scene.RegionInfo.RegionSizeX)
                              ? m_scene.RegionInfo.RegionSizeX - 1
                              : group.AbsolutePosition.X)
                         ,
                         (group.AbsolutePosition.X > m_scene.RegionInfo.RegionSizeY)
                             ? m_scene.RegionInfo.RegionSizeY - 1
                             : group.AbsolutePosition.X,
                         group.AbsolutePosition.Z);

                    Vector3 originalPosition = group.AbsolutePosition;

                    group.AbsolutePosition = inventoryStoredPosition;

                    string sceneObjectXml = SceneObjectSerializer.ToOriginalXmlFormat((SceneObjectGroup) group);
                    group.AbsolutePosition = originalPosition;

                    uint perms = group.GetEffectivePermissions();

                    if ((perms & (uint) PermissionMask.Transfer) == 0)
                    {
                        if (m_dialogModule != null)
                            m_dialogModule.SendAlertToUser(remoteClient, "This item doesn't appear to be for sale");
                        return false;
                    }

                    AssetBase asset = new AssetBase(UUID.Random(), part.Name,
                                                    AssetType.Object, group.OwnerID)
                                          {Description = part.Description, Data = Utils.StringToBytes(sceneObjectXml)};
                    asset.ID = m_scene.AssetService.Store(asset);

                    InventoryItemBase item = new InventoryItemBase
                                                 {
                                                     CreatorId = part.CreatorID.ToString(),
                                                     CreatorData = part.CreatorData,
                                                     ID = UUID.Random(),
                                                     Owner = remoteClient.AgentId,
                                                     AssetID = asset.ID,
                                                     Description = asset.Description,
                                                     Name = asset.Name,
                                                     AssetType = asset.Type,
                                                     InvType = (int) InventoryType.Object,
                                                     Folder = categoryID
                                                 };


                    uint nextPerms = (perms & 7) << 13;
                    if ((nextPerms & (uint) PermissionMask.Copy) == 0)
                        perms &= ~(uint) PermissionMask.Copy;
                    if ((nextPerms & (uint) PermissionMask.Transfer) == 0)
                        perms &= ~(uint) PermissionMask.Transfer;
                    if ((nextPerms & (uint) PermissionMask.Modify) == 0)
                        perms &= ~(uint) PermissionMask.Modify;

                    item.BasePermissions = perms & part.NextOwnerMask;
                    item.CurrentPermissions = perms & part.NextOwnerMask;
                    item.NextPermissions = part.NextOwnerMask;
                    item.EveryOnePermissions = part.EveryoneMask &
                                               part.NextOwnerMask;
                    item.GroupPermissions = part.GroupMask &
                                            part.NextOwnerMask;
                    item.CurrentPermissions |= 16; // Slam!
                    item.CreationDate = Util.UnixTimeSinceEpoch();

                    if (inventoryModule != null)
                    {
                        if (inventoryModule.AddInventoryItem(item))
                        {
                            remoteClient.SendInventoryItemCreateUpdate(item, 0);
                        }
                        else
                        {
                            if (m_dialogModule != null)
                                m_dialogModule.SendAlertToUser(remoteClient,
                                                               "Cannot buy now. Your inventory is unavailable");
                            return false;
                        }
                    }
                    break;

                case 3: // Sell contents
                    List<UUID> invList = part.Inventory.GetInventoryList();

#if (!ISWIN)
                    bool okToSell = true;
                    foreach (UUID invId in invList)
                    {
                        TaskInventoryItem item1 = part.Inventory.GetInventoryItem(invId);
                        if ((item1.CurrentPermissions & (uint) PermissionMask.Transfer) == 0)
                        {
                            okToSell = false;
                            break;
                        }
                    }
#else
                    bool okToSell = invList.Select(invID => part.Inventory.GetInventoryItem(invID)).All(item1 => (item1.CurrentPermissions & (uint) PermissionMask.Transfer) != 0);
#endif

                    if (!okToSell)
                    {
                        if (m_dialogModule != null)
                            m_dialogModule.SendAlertToUser(
                                remoteClient, "This item's inventory doesn't appear to be for sale");
                        return false;
                    }

                    if (invList.Count > 0)
                    {
                        if (inventoryModule != null)
                            inventoryModule.MoveTaskInventoryItemsToUserInventory(remoteClient.AgentId, part.Name, part,
                                                                                  invList);
                    }
                    break;
            }

            return true;
        }

        #endregion

        #region INonSharedRegionModule Members

        public string Name
        {
            get { return "Object BuySell Module"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(IScene scene)
        {
            m_scene = scene;
            m_scene.RegisterModuleInterface<IBuySellModule>(this);
            m_scene.EventManager.OnNewClient += SubscribeToClientEvents;
            m_scene.EventManager.OnClosingClient += UnsubscribeFromClientEvents;
        }

        public void RemoveRegion(IScene scene)
        {
            m_scene.EventManager.OnNewClient -= SubscribeToClientEvents;
            m_scene.EventManager.OnClosingClient -= UnsubscribeFromClientEvents;
        }

        public void RegionLoaded(IScene scene)
        {
            m_dialogModule = scene.RequestModuleInterface<IDialogModule>();
        }

        public void Close()
        {
            RemoveRegion(m_scene);
        }

        #endregion

        public void SubscribeToClientEvents(IClientAPI client)
        {
            client.OnObjectSaleInfo += ObjectSaleInfo;
            client.OnObjectBuy += ObjectBuy;
            client.OnRequestPayPrice += ObjectRequestPayPrice;
        }

        public void UnsubscribeFromClientEvents(IClientAPI client)
        {
            client.OnObjectSaleInfo -= ObjectSaleInfo;
            client.OnObjectBuy -= ObjectBuy;
            client.OnRequestPayPrice -= ObjectRequestPayPrice;
        }

        protected void ObjectRequestPayPrice(IClientAPI client, UUID objectID)
        {
            ISceneChildEntity task = client.Scene.GetSceneObjectPart(objectID);
            if (task == null)
                return;

            client.SendPayPrice(objectID, task.ParentEntity.RootChild.PayPrice);
        }

        protected void ObjectSaleInfo(
            IClientAPI client, UUID sessionID, uint localID, byte saleType, int salePrice)
        {
            ISceneChildEntity part = m_scene.GetSceneObjectPart(localID);
            if (part == null || part.ParentEntity == null)
                return;

            if (part.ParentEntity.IsDeleted)
                return;

            if (part.OwnerID != client.AgentId && (!m_scene.Permissions.IsGod(client.AgentId)))
                return;

            part = part.ParentEntity.RootChild;

            part.ObjectSaleType = saleType;
            part.SalePrice = salePrice;

            part.ParentEntity.HasGroupChanged = true;

            part.GetProperties(client);
        }

        protected void ObjectBuy(IClientAPI remoteClient,
                                 UUID sessionID, UUID groupID, UUID categoryID,
                                 uint localID, byte saleType, int salePrice)
        {
            // We're actually validating that the client is sending the data
            // that it should.   In theory, the client should already know what to send here because it'll see it when it
            // gets the object data.   If the data sent by the client doesn't match the object, the viewer probably has an 
            // old idea of what the object properties are.   Viewer developer Hazim informed us that the base module 
            // didn't check the client sent data against the object do any.   Since the base modules are the 
            // 'crowning glory' examples of good practice..

            ISceneChildEntity part = remoteClient.Scene.GetSceneObjectPart(localID);
            if (part == null)
            {
                remoteClient.SendAgentAlertMessage("Unable to buy now. The object was not found.", false);
                return;
            }

            // Validate that the client sent the price that the object is being sold for 
            if (part.SalePrice != salePrice)
            {
                remoteClient.SendAgentAlertMessage(
                    "Cannot buy at this price. Buy Failed. If you continue to get this relog.", false);
                return;
            }

            // Validate that the client sent the proper sale type the object has set 
            if (part.ObjectSaleType != saleType)
            {
                remoteClient.SendAgentAlertMessage(
                    "Cannot buy this way. Buy Failed. If you continue to get this relog.", false);
                return;
            }

            IMoneyModule moneyMod = remoteClient.Scene.RequestModuleInterface<IMoneyModule>();
            if (moneyMod != null)
            {
                if (
                    !moneyMod.Transfer(part.OwnerID, remoteClient.AgentId, part.ParentUUID, UUID.Zero, part.SalePrice,
                                       "Object Purchase", TransactionType.ObjectBuy))
                {
                    remoteClient.SendAgentAlertMessage("You do not have enough money to buy this object.", false);
                    return;
                }
            }

            BuyObject(remoteClient, categoryID, localID, saleType, salePrice);
        }
    }
}