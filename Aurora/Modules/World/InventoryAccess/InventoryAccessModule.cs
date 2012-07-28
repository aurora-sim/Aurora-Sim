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
using System.Xml;

using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using OpenSim.Services.Interfaces;
using OpenMetaverse.StructuredData;
using OpenMetaverse;
using Nini.Config;

namespace Aurora.Modules.InventoryAccess
{
    public class BasicInventoryAccessModule : INonSharedRegionModule, IInventoryAccessModule
    {
        protected bool m_Enabled = false;
        protected IScene m_scene;
        protected ILLClientInventory m_LLCLientInventoryModule;

        #region INonSharedRegionModule

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public virtual string Name
        {
            get { return "BasicInventoryAccessModule"; }
        }

        public virtual void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("InventoryAccessModule", "");
                if (name == Name)
                {
                    m_Enabled = true;
                    //MainConsole.Instance.InfoFormat("[INVENTORY ACCESS MODULE]: {0} enabled.", Name);
                }
            }
        }

        public virtual void PostInitialise()
        {
        }

        public virtual void AddRegion (IScene scene)
        {
            if (!m_Enabled)
                return;

            m_scene = scene;

            scene.RegisterModuleInterface<IInventoryAccessModule>(this);
            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
        }

        public virtual void OnNewClient(IClientAPI client)
        {
            client.OnRezObject += ClientRezObject;
        }

        public virtual void OnClosingClient(IClientAPI client)
        {
            client.OnRezObject -= ClientRezObject;
        }

        public virtual void Close()
        {
        }

        public virtual void RemoveRegion (IScene scene)
        {
            if (!m_Enabled)
                return;
            m_scene = null;
        }

        public virtual void RegionLoaded (IScene scene)
        {
            m_LLCLientInventoryModule = scene.RequestModuleInterface<ILLClientInventory>();
        }

        #endregion

        #region Inventory Access

        #region Client methods

        /// <summary>
        /// The only difference between this and the other RezObject method is the return value...
        /// The client needs this method
        /// </summary>
        private void ClientRezObject(IClientAPI remoteClient, UUID itemID, Vector3 RayEnd, Vector3 RayStart,
                                    UUID RayTargetID, byte BypassRayCast, bool RayEndIsIntersection,
                                    bool RezSelected, bool RemoveItem, UUID fromTaskID)
        {
            RezObject(remoteClient, itemID, RayEnd, RayStart, RayTargetID, BypassRayCast,
                RayEndIsIntersection, RezSelected, RemoveItem, fromTaskID);
        }

        #endregion

        #region CAPS update

        /// <summary>
        /// Capability originating call to update the asset of an item in an agent's inventory
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="itemID"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual string CapsUpdateInventoryItemAsset(IClientAPI remoteClient, UUID itemID, byte[] data)
        {
            InventoryItemBase item = new InventoryItemBase(itemID, remoteClient.AgentId);
            item = m_scene.InventoryService.GetItem(item);

            if (item != null)
            {
                if ((InventoryType)item.InvType == InventoryType.Notecard)
                {
                    if (!m_scene.Permissions.CanEditNotecard(itemID, UUID.Zero, remoteClient.AgentId))
                    {
                        remoteClient.SendAlertMessage("Insufficient permissions to edit notecard");
                        return FailedPermissionsNotecardCAPSUpdate(UUID.Zero, itemID);
                    }

                    UUID newID;
                    if ((newID = m_scene.AssetService.UpdateContent(item.AssetID, data)) != UUID.Zero)
                        item.AssetID = newID;
                    else
                        remoteClient.SendAlertMessage("Failed to update notecard asset");

                    m_scene.InventoryService.UpdateItem(item);

                    return SuccessNotecardCAPSUpdate(item.AssetID, itemID);
                }
                if ((InventoryType)item.InvType == InventoryType.Gesture)
                {
                    if (!m_scene.Permissions.CanEditNotecard(itemID, UUID.Zero, remoteClient.AgentId))
                    {
                        remoteClient.SendAlertMessage("Insufficient permissions to edit gesture");
                        return FailedPermissionsNotecardCAPSUpdate(UUID.Zero, itemID);
                    }

                    UUID newID;
                    if ((newID = m_scene.AssetService.UpdateContent(item.AssetID, data)) != UUID.Zero)
                        item.AssetID = newID;
                    else
                        remoteClient.SendAlertMessage("Failed to update gesture asset");

                    m_scene.InventoryService.UpdateItem(item);

                    return SuccessNotecardCAPSUpdate(item.AssetID, itemID);
                }
                if ((InventoryType)item.InvType == InventoryType.LSL)
                {
                    if (!m_scene.Permissions.CanEditScript(itemID, UUID.Zero, remoteClient.AgentId))
                        return FailedPermissionsScriptCAPSUpdate(UUID.Zero, itemID);

                    UUID newID;
                    if ((newID = m_scene.AssetService.UpdateContent(item.AssetID, data)) != UUID.Zero)
                        item.AssetID = newID;
                    else
                        remoteClient.SendAlertMessage("Failed to update script asset");

                    m_scene.InventoryService.UpdateItem(item);

                    IScriptModule ScriptEngine = m_scene.RequestModuleInterface<IScriptModule>();
                    if (ScriptEngine != null)
                    {
                        string Errors = ScriptEngine.TestCompileScript(item.AssetID, itemID);
                        if (Errors != "")
                            return FailedCompileScriptCAPSUpdate(item.AssetID, itemID, Errors);
                    }

                    return SuccessScriptCAPSUpdate(item.AssetID, itemID);
                }
                return "";
            }
            MainConsole.Instance.ErrorFormat(
                "[AGENT INVENTORY]: Could not find item {0} for caps inventory update",
                itemID);

            return "";
        }

        private string FailedCompileScriptCAPSUpdate(UUID assetID, UUID inv, string error)
        {
            OSDMap map = new OSDMap();
            map["new_asset"] = assetID.ToString();
            map["new_inventory_item"] = inv;
            map["state"] = "complete";
            map["compiled"] = false;
            map["errors"] = new OSDArray();
            ((OSDArray)map["errors"]).Add(error);
            return OSDParser.SerializeLLSDXmlString(map);
        }

        private string FailedPermissionsScriptCAPSUpdate(UUID assetID, UUID inv)
        {
            OSDMap map = new OSDMap();
            map["new_asset"] = assetID.ToString();
            map["new_inventory_item"] = inv;
            map["state"] = "complete";
            map["compiled"] = false;
            map["errors"] = new OSDArray();
            ((OSDArray)map["errors"]).Add("Insufficient permissions to edit script");
            return OSDParser.SerializeLLSDXmlString(map);
        }

        private string SuccessScriptCAPSUpdate(UUID assetID, UUID inv)
        {
            OSDMap map = new OSDMap();
            map["new_asset"] = assetID.ToString();
            map["new_inventory_item"] = inv;
            map["state"] = "complete";
            map["compiled"] = true;
            map["errors"] = new OSDArray();
            return OSDParser.SerializeLLSDXmlString(map);
        }

        private string FailedPermissionsNotecardCAPSUpdate(UUID assetID, UUID inv)
        {
            OSDMap map = new OSDMap();
            map["new_asset"] = assetID.ToString();
            map["new_inventory_item"] = inv;
            map["state"] = "complete";
            return OSDParser.SerializeLLSDXmlString(map);
        }

        private string SuccessNotecardCAPSUpdate(UUID assetID, UUID inv)
        {
            OSDMap map = new OSDMap();
            map["new_asset"] = assetID.ToString();
            map["new_inventory_item"] = inv;
            map["state"] = "complete";
            return OSDParser.SerializeLLSDXmlString(map);
        }

        #endregion

        /// <summary>
        /// Delete a scene object from a scene and place in the given avatar's inventory.
        /// Returns the UUID of the newly created asset.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="folderID"></param>
        /// <param name="objectGroups"></param>
        /// <param name="agentId"></param>
        /// <param name="itemID"></param>
        public virtual UUID DeleteToInventory(DeRezAction action, UUID folderID,
                List<ISceneEntity> objectGroups, UUID agentId, out UUID itemID)
        {
            itemID = UUID.Zero;
            if (objectGroups.Count == 0)
                return UUID.Zero;

            UUID assetID = UUID.Zero;
            Vector3 GroupMiddle = Vector3.Zero;
            string AssetXML = "<groups>";

            if (objectGroups.Count == 1)
            {
                m_scene.AuroraEventManager.FireGenericEventHandler("DeleteToInventory", objectGroups[0]);
                AssetXML = ((ISceneObject)objectGroups[0]).ToXml2();
            }
            else
            {
                foreach (ISceneEntity objectGroup in objectGroups)
                {
                    Vector3 inventoryStoredPosition = new Vector3
                                (((objectGroup.AbsolutePosition.X > m_scene.RegionInfo.RegionSizeX)
                                      ? m_scene.RegionInfo.RegionSizeX - 1
                                      : objectGroup.AbsolutePosition.X)
                                 ,
                                 (objectGroup.AbsolutePosition.Y > m_scene.RegionInfo.RegionSizeY)
                                     ? m_scene.RegionInfo.RegionSizeY - 1
                                     : objectGroup.AbsolutePosition.Y,
                                 objectGroup.AbsolutePosition.Z);
                    GroupMiddle += inventoryStoredPosition;
                    Vector3 originalPosition = objectGroup.AbsolutePosition;

                    objectGroup.AbsolutePosition = inventoryStoredPosition;

                    m_scene.AuroraEventManager.FireGenericEventHandler ("DeleteToInventory", objectGroup);
                    AssetXML += ((ISceneObject)objectGroup).ToXml2();

                    objectGroup.AbsolutePosition = originalPosition;
                }
                GroupMiddle.X /= objectGroups.Count;
                GroupMiddle.Y /= objectGroups.Count;
                GroupMiddle.Z /= objectGroups.Count;
                AssetXML += "<middle>";
                AssetXML += "<mid>" + GroupMiddle.ToRawString() + "</mid>";
                AssetXML += "</middle>";
                AssetXML += "</groups>";
            }
            // Get the user info of the item destination
            //
            IScenePresence SP = m_scene.GetScenePresence (agentId);
            UUID userID = UUID.Zero;

            if (action == DeRezAction.Take || action == DeRezAction.AcquireToUserInventory ||
                action == DeRezAction.SaveToExistingUserInventoryItem)
            {
                // Take or take copy require a taker
                // Saving changes requires a local user
                //
                if (SP == null || SP.ControllingClient == null)
                    return UUID.Zero;

                userID = agentId;
            }
            else
            {
                // All returns / deletes go to the object owner
                //

                userID = objectGroups[0].OwnerID;
            }

            if (userID == UUID.Zero) // Can't proceed
            {
                return UUID.Zero;
            }

            // If we're returning someone's item, it goes back to the
            // owner's Lost And Found folder.
            // Delete is treated like return in this case
            // Deleting your own items makes them go to trash
            //

            InventoryFolderBase folder = null;
            InventoryItemBase item = null;
                    
            if (DeRezAction.SaveToExistingUserInventoryItem == action)
            {
                item = new InventoryItemBase(objectGroups[0].RootChild.FromUserInventoryItemID, userID);
                item = m_scene.InventoryService.GetItem(item);

                //item = userInfo.RootFolder.FindItem(
                //        objectGroup.RootPart.FromUserInventoryItemID);

                if (null == item)
                {
                    MainConsole.Instance.DebugFormat(
                        "[AGENT INVENTORY]: Object {0} {1} scheduled for save to inventory has already been deleted.",
                        objectGroups[0].Name, objectGroups[0].UUID);
                    return UUID.Zero;
                }
            }
            else
            {
                // Folder magic
                //
                if (action == DeRezAction.Delete)
                {
                    // Deleting someone else's item
                    //

                    if (SP == null || SP.ControllingClient == null ||
                        objectGroups[0].OwnerID != agentId)
                    {

                        folder = m_scene.InventoryService.GetFolderForType(userID, InventoryType.Unknown, AssetType.LostAndFoundFolder);
                    }
                    else
                    {
                        folder = m_scene.InventoryService.GetFolderForType (userID, InventoryType.Unknown, AssetType.TrashFolder);
                    }
                }
                else if (action == DeRezAction.Return)
                {

                    // Dump to lost + found unconditionally
                    //
                    folder = m_scene.InventoryService.GetFolderForType (userID, InventoryType.Unknown, AssetType.LostAndFoundFolder);
                }

                if (folderID == UUID.Zero && folder == null)
                {
                    if (action == DeRezAction.Delete)
                    {
                        // Deletes go to trash by default
                        //
                        folder = m_scene.InventoryService.GetFolderForType (userID, InventoryType.Unknown, AssetType.TrashFolder);
                    }
                    else
                    {
                        if (SP == null || SP.ControllingClient == null ||
                            objectGroups[0].OwnerID != agentId)
                        {

                            folder = m_scene.InventoryService.GetFolderForType (userID, InventoryType.Unknown, AssetType.LostAndFoundFolder);
                        }
                        else
                        {
                            folder = m_scene.InventoryService.GetFolderForType(userID, InventoryType.Unknown, AssetType.TrashFolder);
                        }
                    }
                }

                // Override and put into where it came from, if it came
                // from anywhere in inventory
                //
                if (action == DeRezAction.Attachment || action == DeRezAction.Take || action == DeRezAction.AcquireToUserInventory)
                {
                    if (objectGroups[0].RootChild.FromUserInventoryItemID != UUID.Zero)
                    {
                        InventoryFolderBase f = new InventoryFolderBase(objectGroups[0].RootChild.FromUserInventoryItemID, userID);
                        folder = m_scene.InventoryService.GetFolder(f);
                    }
                    else
                    {
                        folder = m_scene.InventoryService.GetFolderForType (userID, InventoryType.Object, AssetType.Object);
                    }
                }

                if (folder == null) // None of the above
                {
                    folder = new InventoryFolderBase(folderID);
                }

                item = new InventoryItemBase
                           {
                               CreatorId = objectGroups[0].RootChild.CreatorID.ToString(),
                               ID = UUID.Random(),
                               InvType = (int) InventoryType.Object,
                               Folder = folder.ID,
                               Owner = userID
                           };
            }

            AssetBase asset = CreateAsset(
                objectGroups[0].Name,
                objectGroups[0].RootChild.Description,
                (sbyte)AssetType.Object,
                Utils.StringToBytes(AssetXML),
                objectGroups[0].OwnerID.ToString());
            asset.ID = m_scene.AssetService.Store(asset);
            assetID = asset.ID;
            item.AssetID = assetID;
            if (DeRezAction.SaveToExistingUserInventoryItem != action)
            {
                item.Description = asset.Description;
                item.Name = asset.Name;
                item.AssetType = asset.Type;
            }
            AssetXML = null;
            asset = null;

            if (DeRezAction.SaveToExistingUserInventoryItem == action)
            {
                m_scene.InventoryService.UpdateItem(item);
            }
            else
            {
                if (SP != null && SP.ControllingClient != null && (SP.ControllingClient.AgentId != objectGroups[0].OwnerID) && m_scene.Permissions.PropagatePermissions())
                {
                    foreach (SceneObjectGroup group in objectGroups)
                    {
                        uint perms = group.GetEffectivePermissions();
                        uint nextPerms = (perms & 7) << 13;
                        if ((nextPerms & (uint)PermissionMask.Copy) == 0)
                            perms &= ~(uint)PermissionMask.Copy;
                        if ((nextPerms & (uint)PermissionMask.Transfer) == 0)
                            perms &= ~(uint)PermissionMask.Transfer;
                        if ((nextPerms & (uint)PermissionMask.Modify) == 0)
                            perms &= ~(uint)PermissionMask.Modify;

                        // Make sure all bits but the ones we want are clear
                        // on take.
                        // This will be applied to the current perms, so
                        // it will do what we want.
                        group.RootPart.NextOwnerMask &=
                                ((uint)PermissionMask.Copy |
                                 (uint)PermissionMask.Transfer |
                                 (uint)PermissionMask.Modify);
                        group.RootPart.NextOwnerMask |=
                                (uint)PermissionMask.Move;

                        item.BasePermissions = perms & group.RootPart.NextOwnerMask;
                        item.CurrentPermissions = item.BasePermissions;
                        item.NextPermissions = group.RootPart.NextOwnerMask;
                        item.EveryOnePermissions = group.RootPart.EveryoneMask & group.RootPart.NextOwnerMask;
                        item.GroupPermissions = group.RootPart.GroupMask & group.RootPart.NextOwnerMask;

                        // Magic number badness. Maybe this deserves an enum.
                        // bit 4 (16) is the "Slam" bit, it means treat as passed
                        // and apply next owner perms on rez
                        item.CurrentPermissions |= 16; // Slam!

                        item.SalePrice = group.RootChild.SalePrice;
                        item.SaleType = group.RootChild.ObjectSaleType;
                    }
                }
                else
                {
                    foreach (SceneObjectGroup group in objectGroups)
                    {
                        item.BasePermissions = group.GetEffectivePermissions();
                        item.CurrentPermissions = group.GetEffectivePermissions();
                        item.NextPermissions = group.RootPart.NextOwnerMask;
                        item.EveryOnePermissions = group.RootPart.EveryoneMask;
                        item.GroupPermissions = group.RootPart.GroupMask;

                        item.SalePrice = group.RootChild.SalePrice;
                        item.SaleType = group.RootChild.ObjectSaleType;

                        item.CurrentPermissions &=
                                ((uint)PermissionMask.Copy |
                                 (uint)PermissionMask.Transfer |
                                 (uint)PermissionMask.Modify |
                                 (uint)PermissionMask.Move |
                                 7); // Preserve folded permissions

                    }
                }

                if(objectGroups.Count != 1)
                    item.Flags |= (uint)InventoryItemFlags.ObjectHasMultipleItems;
                item.CreationDate = Util.UnixTimeSinceEpoch();

                m_LLCLientInventoryModule.AddInventoryItem(item);

                if (SP != null && SP.ControllingClient != null && item.Owner == SP.ControllingClient.AgentId)
                {
                    SP.ControllingClient.SendInventoryItemCreateUpdate(item, 0);
                }
                else
                {
                    IScenePresence notifyUser = m_scene.GetScenePresence (item.Owner);
                    if (notifyUser != null)
                    {
                        notifyUser.ControllingClient.SendInventoryItemCreateUpdate(item, 0);
                    }
                }
            }
            itemID = item.ID;
            return assetID;
        }

        public virtual SceneObjectGroup CreateObjectFromInventory(IClientAPI remoteClient, UUID itemID, out InventoryItemBase item)
        {
            XmlDocument doc;
            item = m_scene.InventoryService.GetItem(new InventoryItemBase(itemID, remoteClient.AgentId));
            UUID itemId = UUID.Zero;

            // If we have permission to copy then link the rezzed object back to the user inventory
            // item that it came from.  This allows us to enable 'save object to inventory'
            if (!m_scene.Permissions.BypassPermissions())
            {
                if ((item.CurrentPermissions & (uint)PermissionMask.Copy) == (uint)PermissionMask.Copy)
                {
                    itemId = item.ID;
                }
            }
            else
            {
                // Brave new fullperm world
                //
                itemId = item.ID;
            }
            return CreateObjectFromInventory(remoteClient, itemId, item.AssetID, out doc);
        }

        public virtual SceneObjectGroup CreateObjectFromInventory(IClientAPI remoteClient, UUID itemID, UUID assetID)
        {
            XmlDocument doc;
            return CreateObjectFromInventory(remoteClient, itemID, assetID, out doc);
        }

        protected virtual SceneObjectGroup CreateObjectFromInventory(InventoryItemBase item, IClientAPI remoteClient, UUID itemID, out XmlDocument doc)
        {
            UUID itemId = UUID.Zero;

            // If we have permission to copy then link the rezzed object back to the user inventory
            // item that it came from.  This allows us to enable 'save object to inventory'
            if (!m_scene.Permissions.BypassPermissions())
            {
                if ((item.CurrentPermissions & (uint)PermissionMask.Copy) == (uint)PermissionMask.Copy)
                {
                    itemId = item.ID;
                }
            }
            else
            {
                // Brave new fullperm world
                //
                itemId = item.ID;
            }
            return CreateObjectFromInventory(remoteClient, itemId, item.AssetID, out doc);
        }

        protected virtual SceneObjectGroup CreateObjectFromInventory(IClientAPI remoteClient, UUID itemID, UUID assetID, out XmlDocument doc)
        {
            AssetBase rezAsset = m_scene.AssetService.Get(assetID.ToString());

            if (rezAsset != null)
            {
                string xmlData = Utils.BytesToString(rezAsset.Data);
                doc = new XmlDocument();
                try
                {
                    doc.LoadXml(xmlData);
                }
                catch
                {
                    return null;
                }

                if (doc.FirstChild.OuterXml.StartsWith("<groups>") ||
                    (doc.FirstChild.NextSibling != null &&
                    doc.FirstChild.NextSibling.OuterXml.StartsWith("<groups>")))
                {
                    //We don't do multiple objects here
                    return null;
                }
                string xml = "";
                if (doc.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
                {
                    if (doc.FirstChild.NextSibling != null) xml = doc.FirstChild.NextSibling.OuterXml;
                }
                else
                    xml = doc.FirstChild.OuterXml;
                SceneObjectGroup group
                            = SceneObjectSerializer.FromOriginalXmlFormat(itemID, xml, m_scene);
                if (group == null)
                    return null;

                group.IsDeleted = false;
                group.m_isLoaded = true;
                foreach (SceneObjectPart part in group.ChildrenList)
                {
                    part.IsLoading = false;
                }
                return group;
            }
            doc = null;
            return null;
        }


        /// <summary>
        /// Rez an object into the scene from the user's inventory
        /// </summary>
        /// FIXME: It would be really nice if inventory access modules didn't also actually do the work of rezzing
        /// things to the scene.  The caller should be doing that, I think.
        /// <param name="remoteClient"></param>
        /// <param name="itemID"></param>
        /// <param name="RayEnd"></param>
        /// <param name="RayStart"></param>
        /// <param name="RayTargetID"></param>
        /// <param name="BypassRayCast"></param>
        /// <param name="RayEndIsIntersection"></param>
        /// <param name="RezSelected"></param>
        /// <param name="RemoveItem"></param>
        /// <param name="fromTaskID"></param>
        /// <returns>The SceneObjectGroup rezzed or null if rez was unsuccessful.</returns>
        public virtual SceneObjectGroup RezObject (IClientAPI remoteClient, UUID itemID, Vector3 RayEnd, Vector3 RayStart,
                                    UUID RayTargetID, byte BypassRayCast, bool RayEndIsIntersection,
                                    bool RezSelected, bool RemoveItem, UUID fromTaskID)
        {
            // Work out position details
            byte bRayEndIsIntersection = (byte)0;

            bRayEndIsIntersection = (byte) (RayEndIsIntersection ? 1 : 0);

            XmlDocument doc;
            InventoryItemBase item = new InventoryItemBase (itemID, remoteClient.AgentId);
            item = m_scene.InventoryService.GetItem (item);
            SceneObjectGroup group = CreateObjectFromInventory (item, remoteClient, itemID, out doc);

            Vector3 pos = m_scene.SceneGraph.GetNewRezLocation (
                      RayStart, RayEnd, RayTargetID, Quaternion.Identity,
                      BypassRayCast, bRayEndIsIntersection, true, new Vector3(0.5f, 0.5f, 0.5f), false);

            if (doc == null)
            {
                //No asset, check task inventory
                IEntity e;
                m_scene.SceneGraph.TryGetEntity (fromTaskID, out e);
                if (e != null && e is SceneObjectGroup)
                {
                    SceneObjectGroup grp = (SceneObjectGroup)e;
                    TaskInventoryItem taskItem = grp.RootPart.Inventory.GetInventoryItem (itemID);
                    item = new InventoryItemBase
                               {
                                   ID = UUID.Random(),
                                   CreatorId = taskItem.CreatorID.ToString(),
                                   Owner = remoteClient.AgentId,
                                   AssetID = taskItem.AssetID,
                                   Description = taskItem.Description,
                                   Name = taskItem.Name,
                                   AssetType = taskItem.Type,
                                   InvType = taskItem.InvType,
                                   Flags = taskItem.Flags,
                                   SalePrice = taskItem.SalePrice,
                                   SaleType = taskItem.SaleType
                               };


                    if (m_scene.Permissions.PropagatePermissions ())
                    {
                        item.BasePermissions = taskItem.BasePermissions & (taskItem.NextPermissions | (uint)PermissionMask.Move);
                        if (taskItem.InvType == (int)InventoryType.Object)
                            item.CurrentPermissions = item.BasePermissions & (((taskItem.CurrentPermissions & 7) << 13) | (taskItem.CurrentPermissions & (uint)PermissionMask.Move));
                        else
                            item.CurrentPermissions = item.BasePermissions & taskItem.CurrentPermissions;

                        item.CurrentPermissions |= 16; // Slam
                        item.NextPermissions = taskItem.NextPermissions;
                        item.EveryOnePermissions = taskItem.EveryonePermissions & (taskItem.NextPermissions | (uint)PermissionMask.Move);
                        item.GroupPermissions = taskItem.GroupPermissions & taskItem.NextPermissions;
                    }
                    else
                    {
                        item.BasePermissions = taskItem.BasePermissions;
                        item.CurrentPermissions = taskItem.CurrentPermissions;
                        item.NextPermissions = taskItem.NextPermissions;
                        item.EveryOnePermissions = taskItem.EveryonePermissions;
                        item.GroupPermissions = taskItem.GroupPermissions;
                    }
                    group = CreateObjectFromInventory (item, remoteClient, itemID, out doc);
                }
            }
            if (group == null && doc != null && doc.FirstChild != null && (doc.FirstChild.OuterXml.StartsWith("<groups>") ||
                (doc.FirstChild.NextSibling != null &&
                doc.FirstChild.NextSibling.OuterXml.StartsWith ("<groups>"))))
            {
                    XmlNodeList nodes;
                    if (doc.FirstChild.OuterXml.StartsWith("<groups>")) nodes = doc.FirstChild.ChildNodes;
                    else if (doc.FirstChild.NextSibling != null) nodes = doc.FirstChild.NextSibling.ChildNodes;
                    else
                    {
                        remoteClient.SendAlertMessage("Failed to find the item you requested.");
                        return null;
                    }
                    List<SceneObjectGroup> Groups = RezMultipleObjectsFromInventory (nodes, itemID, remoteClient, pos, RezSelected, item, RayTargetID, BypassRayCast, RayEndIsIntersection, RayEnd, RayStart, bRayEndIsIntersection);
                    if (Groups.Count != 0)
                        return Groups[0];
                    remoteClient.SendAlertMessage ("Failed to rez the item you requested.");
                    return null;
            }
            if(group == null)
            {
                remoteClient.SendAlertMessage("Failed to find the item you requested.");
                return null;
            }

            string reason;
            if (!m_scene.Permissions.CanRezObject (
                    group.ChildrenList.Count, remoteClient.AgentId, pos, out reason))
            {
                // The client operates in no fail mode. It will
                // have already removed the item from the folder
                // if it's no copy.
                // Put it back if it's not an attachment
                //
                if ((item.CurrentPermissions & (uint)PermissionMask.Copy) == 0)
                    remoteClient.SendBulkUpdateInventory (item);
                remoteClient.SendAlertMessage ("You do not have permission to rez objects here.");
                return null;
            }

            if (RezSelected)
                group.RootPart.AddFlag (PrimFlags.CreateSelected);
            // If we're rezzing an attachment then don't ask AddNewSceneObject() to update the client since
            // we'll be doing that later on.  Scheduling more than one full update during the attachment
            // process causes some clients to fail to display the attachment properly.
            m_scene.SceneGraph.AddPrimToScene (group);

            //  MainConsole.Instance.InfoFormat("ray end point for inventory rezz is {0} {1} {2} ", RayEnd.X, RayEnd.Y, RayEnd.Z);
            //  Set it's position in world.
            const float offsetHeight = 0;
            //The OOBsize is only half the size, x2
            Vector3 newSize = (group.OOBsize * 2) * Quaternion.Inverse (group.GroupRotation);
            pos = m_scene.SceneGraph.GetNewRezLocation (
                RayStart, RayEnd, RayTargetID, Quaternion.Identity,
                BypassRayCast, bRayEndIsIntersection, true, newSize, false);
            pos.Z += offsetHeight;
            group.AbsolutePosition = pos;
            //   MainConsole.Instance.InfoFormat("rezx point for inventory rezz is {0} {1} {2}  and offsetheight was {3}", pos.X, pos.Y, pos.Z, offsetHeight);

            ISceneChildEntity rootPart = group.GetChildPart(group.UUID);
            if (rootPart == null)
            {
                MainConsole.Instance.Error ("[AGENT INVENTORY]: Error rezzing ItemID: " + itemID + " object has no rootpart.");
                return null;
            }

            // Since renaming the item in the inventory does not affect the name stored
            // in the serialization, transfer the correct name from the inventory to the
            // object itself before we rez.
            rootPart.Name = item.Name;
            rootPart.Description = item.Description;

            List<SceneObjectPart> partList = new List<SceneObjectPart> (group.ChildrenList);

            group.SetGroup (remoteClient.ActiveGroupId, remoteClient.AgentId, false);
            item.Owner = remoteClient.AgentId;
            if (rootPart.OwnerID != item.Owner)
            {
                //Need to kill the for sale here
                rootPart.ObjectSaleType = 0;
                rootPart.SalePrice = 10;

                if (m_scene.Permissions.PropagatePermissions ())
                {
                    if ((item.CurrentPermissions & 8) != 0)
                    {
                        foreach (SceneObjectPart part in partList)
                        {
                            part.EveryoneMask = item.EveryOnePermissions;
                            part.NextOwnerMask = item.NextPermissions;
                            part.GroupMask = 0; // DO NOT propagate here
                        }
                    }

                    group.ApplyNextOwnerPermissions ();
                }
            }

            foreach (SceneObjectPart part in partList)
            {
                if (part.OwnerID != item.Owner)
                {
                    part.LastOwnerID = part.OwnerID;
                    part.OwnerID = item.Owner;
                    part.Inventory.ChangeInventoryOwner (item.Owner);
                }
                else if ((item.CurrentPermissions & 8) != 0) // Slam!
                {
                    part.EveryoneMask = item.EveryOnePermissions;
                    part.NextOwnerMask = item.NextPermissions;

                    part.GroupMask = 0; // DO NOT propagate here
                }
            }

            rootPart.TrimPermissions ();

            if (group.RootPart.Shape.PCode == (byte)PCode.Prim)
            {
                group.ClearPartAttachmentData ();
            }

            // Fire on_rez
            group.CreateScriptInstances(0, true, StateSource.NewRez, UUID.Zero, false);

            group.ScheduleGroupUpdate (PrimUpdateFlags.ForcedFullUpdate);
            if (!m_scene.Permissions.BypassPermissions ())
            {
                if ((item.CurrentPermissions & (uint)PermissionMask.Copy) == 0)
                {
                    List<UUID> uuids = new List<UUID> {item.ID};
                    m_scene.InventoryService.DeleteItems (item.Owner, uuids);
                }
            }
            return group;
        }

        private List<SceneObjectGroup> RezMultipleObjectsFromInventory(XmlNodeList nodes, UUID itemId, IClientAPI remoteClient, Vector3 pos, bool RezSelected,
            InventoryItemBase item, UUID RayTargetID, byte BypassRayCast, bool RayEndIsIntersection, Vector3 RayEnd, Vector3 RayStart, byte bRayEndIsIntersection)
        {
            Vector3 OldMiddlePos = Vector3.Zero;
            List<SceneObjectGroup> NewGroup = new List<SceneObjectGroup>();

            foreach (XmlNode aPrimNode in nodes)
            {
                if (aPrimNode.OuterXml.StartsWith("<middle>"))
                {
                    string Position = aPrimNode.OuterXml.Remove(0, 13);
                    Position = Position.Remove(Position.Length - 16, 16);
                    string[] XYZ = Position.Split(' ');
                    OldMiddlePos = new Vector3(float.Parse(XYZ[0]), float.Parse(XYZ[1]), float.Parse(XYZ[2]));
                    continue;
                }
                SceneObjectGroup group
                       = SceneObjectSerializer.FromOriginalXmlFormat(aPrimNode.OuterXml, m_scene);
                if (group == null)
                    return null;

                group.IsDeleted = false;
                group.m_isLoaded = true;
                foreach (SceneObjectPart part in group.ChildrenList)
                {
                    part.IsLoading = false;
                }
                NewGroup.Add(group);

                string reason;
                if (!m_scene.Permissions.CanRezObject(
                    group.ChildrenList.Count, remoteClient.AgentId, pos, out reason))
                {
                    // The client operates in no fail mode. It will
                    // have already removed the item from the folder
                    // if it's no copy.
                    // Put it back if it's not an attachment
                    //
                    if (((item.CurrentPermissions & (uint)PermissionMask.Copy) == 0))
                        remoteClient.SendBulkUpdateInventory(item);
                    return null;
                }

                if (RezSelected)
                    group.RootPart.AddFlag(PrimFlags.CreateSelected);
                // If we're rezzing an attachment then don't ask AddNewSceneObject() to update the client since
                // we'll be doing that later on.  Scheduling more than one full update during the attachment
                // process causes some clients to fail to display the attachment properly.
                m_scene.SceneGraph.AddPrimToScene(group);

                //  MainConsole.Instance.InfoFormat("ray end point for inventory rezz is {0} {1} {2} ", RayEnd.X, RayEnd.Y, RayEnd.Z);
                // if attachment we set it's asset id so object updates can reflect that
                // if not, we set it's position in world.
                float offsetHeight = 0;
                pos = m_scene.SceneGraph.GetNewRezLocation(
                    RayStart, RayEnd, RayTargetID, Quaternion.Identity,
                    BypassRayCast, bRayEndIsIntersection, true, group.GetAxisAlignedBoundingBox(out offsetHeight), false);
                pos.Z += offsetHeight;
                //group.AbsolutePosition = pos;
                //   MainConsole.Instance.InfoFormat("rezx point for inventory rezz is {0} {1} {2}  and offsetheight was {3}", pos.X, pos.Y, pos.Z, offsetHeight);

                SceneObjectPart rootPart = (SceneObjectPart)group.GetChildPart (group.UUID);

                // Since renaming the item in the inventory does not affect the name stored
                // in the serialization, transfer the correct name from the inventory to the
                // object itself before we rez.
                rootPart.Name = item.Name;
                rootPart.Description = item.Description;

                List<SceneObjectPart> partList = new List<SceneObjectPart>(group.ChildrenList);

                group.SetGroup(remoteClient.ActiveGroupId, remoteClient.AgentId, false);
                item.Owner = remoteClient.AgentId;
                if (rootPart.OwnerID != item.Owner)
                {
                    //Need to kill the for sale here
                    rootPart.ObjectSaleType = 0;
                    rootPart.SalePrice = 10;

                    if (m_scene.Permissions.PropagatePermissions())
                    {
                        if ((item.CurrentPermissions & 8) != 0)
                        {
                            foreach (SceneObjectPart part in partList)
                            {
                                part.EveryoneMask = item.EveryOnePermissions;
                                part.NextOwnerMask = item.NextPermissions;
                                part.GroupMask = 0; // DO NOT propagate here
                            }
                        }

                        group.ApplyNextOwnerPermissions();
                    }
                }

                foreach (SceneObjectPart part in partList)
                {
                    if (part.OwnerID != item.Owner)
                    {
                        part.LastOwnerID = part.OwnerID;
                        part.OwnerID = item.Owner;
                        part.Inventory.ChangeInventoryOwner(item.Owner);
                    }
                    else if ((item.CurrentPermissions & 8) != 0) // Slam!
                    {
                        part.EveryoneMask = item.EveryOnePermissions;
                        part.NextOwnerMask = item.NextPermissions;

                        part.GroupMask = 0; // DO NOT propagate here
                    }
                }

                rootPart.TrimPermissions();

                if (group.RootPart.Shape.PCode == (byte)PCode.Prim)
                {
                    group.ClearPartAttachmentData();
                }

                // Fire on_rez
                group.CreateScriptInstances(0, true, StateSource.NewRez, UUID.Zero, false);

                if (!m_scene.Permissions.BypassPermissions())
                {
                    if ((item.CurrentPermissions & (uint)PermissionMask.Copy) == 0)
                    {
                        // If this is done on attachments, no
                        // copy ones will be lost, so avoid it
                        //
                        List<UUID> uuids = new List<UUID> {item.ID};
                        m_scene.InventoryService.DeleteItems(item.Owner, uuids);
                    }
                }
            }
            foreach (SceneObjectGroup group in NewGroup)
            {
                if (OldMiddlePos != Vector3.Zero)
                {
                    Vector3 NewPosOffset = Vector3.Zero;
                    NewPosOffset.X = group.AbsolutePosition.X - OldMiddlePos.X;
                    NewPosOffset.Y = group.AbsolutePosition.Y - OldMiddlePos.Y;
                    NewPosOffset.Z = group.AbsolutePosition.Z - OldMiddlePos.Z;
                    group.AbsolutePosition = pos + NewPosOffset;
                }
                group.ScheduleGroupUpdate (PrimUpdateFlags.ForcedFullUpdate);
            }
            return NewGroup;
        }

        public virtual bool GetAgentInventoryItem(IClientAPI remoteClient, UUID itemID, UUID requestID)
        {
            InventoryItemBase assetRequestItem = GetItem(remoteClient.AgentId, itemID);
            if (assetRequestItem == null)
            {
                return false;
            }

            // At this point, we need to apply perms
            // only to notecards and scripts. All
            // other asset types are always available
            //
            if (assetRequestItem.AssetType == (int)AssetType.LSLText)
            {
                if (!m_scene.Permissions.CanViewScript(itemID, UUID.Zero, remoteClient.AgentId))
                {
                    remoteClient.SendAgentAlertMessage("Insufficient permissions to view script", false);
                    return false;
                }
            }
            else if (assetRequestItem.AssetType == (int)AssetType.Notecard)
            {
                if (!m_scene.Permissions.CanViewNotecard(itemID, UUID.Zero, remoteClient.AgentId))
                {
                    remoteClient.SendAgentAlertMessage("Insufficient permissions to view notecard", false);
                    return false;
                }
            }

            if (assetRequestItem.AssetID != requestID)
            {
                MainConsole.Instance.WarnFormat(
                    "[CLIENT]: {0} requested asset {1} from item {2} but this does not match item's asset {3}",
                    Name, requestID, itemID, assetRequestItem.AssetID);
                return false;
            }

            return true;
        }


        public virtual bool IsForeignUser(UUID userID, out string assetServerURL)
        {
            assetServerURL = string.Empty;
            return false;
        }

        #endregion

        #region Misc

        /// <summary>
        /// Create a new asset data structure.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="assetType"></param>
        /// <param name="data"></param>
        /// <param name="creatorID"></param>
        /// <returns></returns>
        private AssetBase CreateAsset(string name, string description, sbyte assetType, byte[] data, string creatorID)
        {
            AssetBase asset = new AssetBase(UUID.Random(), name, (AssetType) assetType, UUID.Parse(creatorID))
                                  {Description = description, Data = data ?? new byte[1]};

            return asset;
        }

        protected virtual InventoryItemBase GetItem(UUID agentID, UUID itemID)
        {
            IInventoryService invService = m_scene.RequestModuleInterface<IInventoryService> ();
            InventoryItemBase assetRequestItem = new InventoryItemBase (itemID, agentID);
            assetRequestItem = invService.GetItem (assetRequestItem);

            if (assetRequestItem != null &&
                assetRequestItem.CreatorData != null && 
                assetRequestItem.CreatorData != string.Empty)
            {
                IUserFinder userManagement = m_scene.RequestModuleInterface<IUserFinder>();
                if (userManagement != null)
                    userManagement.AddUser (assetRequestItem.CreatorIdAsUuid, assetRequestItem.CreatorData);
            }
            return assetRequestItem;
        }

        #endregion
    }
}
