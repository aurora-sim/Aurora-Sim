/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenSim.Framework;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;

namespace OpenSim.Region.CoreModules.Avatar.Attachments
{
    public class AttachmentsModule : IAttachmentsModule, INonSharedRegionModule
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected Scene m_scene = null;

        public string Name { get { return "Attachments Module"; } }
        public Type ReplaceableInterface { get { return null; } }
        public IAvatarFactory AvatarFactory = null;

        #endregion

        #region INonSharedRegionModule Methods

        public void Initialise(IConfigSource source) { }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;
            m_scene.RegisterModuleInterface<IAttachmentsModule>(this);
            m_scene.EventManager.OnNewClient += SubscribeToClientEvents;
            m_scene.EventManager.OnClosingClient += UnsubscribeFromClientEvents;
            m_scene.EventManager.OnMakeRootAgent += MakeRootAgent;
            m_scene.EventManager.OnMakeChildAgent += MakeChildAgent;
        }

        public void RemoveRegion(Scene scene)
        {
            m_scene.UnregisterModuleInterface<IAttachmentsModule>(this);
            m_scene.EventManager.OnNewClient -= SubscribeToClientEvents;
            m_scene.EventManager.OnClosingClient -= UnsubscribeFromClientEvents;
            m_scene.EventManager.OnMakeRootAgent -= MakeRootAgent;
            m_scene.EventManager.OnMakeChildAgent -= MakeChildAgent;
        }

        public void RegionLoaded(Scene scene) 
        {
            AvatarFactory = scene.RequestModuleInterface<IAvatarFactory>();
        }

        public void Close()
        {
            RemoveRegion(m_scene);
        }

        #endregion

        #region Client Methods

        protected void MakeRootAgent(ScenePresence presence)
        {
            Util.FireAndForget(delegate(object o) { RezAttachments(presence); });
        }

        protected void MakeChildAgent(ScenePresence presence)
        {
            foreach (AvatarAttachment att in presence.Appearance.GetAttachments())
            {
                //Don't fire events as we just want to remove them
                DetachSingleAttachmentToInv(att.ItemID, presence.ControllingClient, false);
            }
        }

        protected void SubscribeToClientEvents(IClientAPI client)
        {
            client.OnRezSingleAttachmentFromInv += ClientRezSingleAttachmentFromInventory;
            client.OnObjectAttach += AttachObject;
            client.OnObjectDetach += DetachObject;
            client.OnDetachAttachmentIntoInv += ShowDetachInUserInventory;
        }

        protected void UnsubscribeFromClientEvents(IClientAPI client)
        {
            client.OnRezSingleAttachmentFromInv -= ClientRezSingleAttachmentFromInventory;
            client.OnObjectAttach -= AttachObject;
            client.OnObjectDetach -= DetachObject;
            client.OnDetachAttachmentIntoInv -= ShowDetachInUserInventory;
        }

        protected void RezAttachments(ScenePresence presence)
        {
            if (null == presence.Appearance)
            {
                m_log.WarnFormat("[ATTACHMENT]: Appearance has not been initialized for agent {0}", presence.UUID);
                return;
            }
            
            //Create the avatar attachments plugin for the av
            AvatarAttachments attachmentsPlugin = new AvatarAttachments(presence);

            List<AvatarAttachment> attachments = presence.Appearance.GetAttachments();
            foreach (AvatarAttachment attach in attachments)
            {
                int p = attach.AttachPoint;
                UUID itemID = attach.ItemID;

                try
                {
                    RezSingleAttachmentFromInventory(presence.ControllingClient, itemID, p);
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("[ATTACHMENT]: Unable to rez attachment: {0}{1}", e.Message, e.StackTrace);
                }
            }
        }

        protected UUID ClientRezSingleAttachmentFromInventory(
            IClientAPI remoteClient, UUID itemID, int AttachmentPt)
        {
            SceneObjectGroup att = RezSingleAttachmentFromInventory(remoteClient, itemID, AttachmentPt);

            if (null == att)
                return UUID.Zero;
            return att.UUID;
        }

        protected void AttachObject(IClientAPI remoteClient, uint objectLocalID, int AttachmentPt, bool silent)
        {
            m_log.Debug("[ATTACHMENTS MODULE]: Invoking AttachObject");

            try
            {
                // If we can't take it, we can't attach it!
                SceneObjectPart part = m_scene.GetSceneObjectPart(objectLocalID);
                if (part == null)
                    return;

                // Calls attach with a Zero position
                AttachObjectFromInworldObject(objectLocalID, remoteClient, part.ParentGroup, AttachmentPt);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[ATTACHMENTS MODULE]: exception upon Attach Object {0}", e);
            }
        }

        #endregion

        #region Public Methods

        public bool AttachObjectFromInworldObject(uint localID, IClientAPI remoteClient,
            SceneObjectGroup group, int AttachmentPt)
        {
            if (m_scene.Permissions.CanTakeObject(group.UUID, remoteClient.AgentId))
                FindAttachmentPoint(remoteClient, localID, group, AttachmentPt);
            else
            {
                remoteClient.SendAgentAlertMessage(
                    "You don't have sufficient permissions to attach this object", false);

                return false;
            }

            return true;
        }

        public SceneObjectGroup RezSingleAttachmentFromInventory(
            IClientAPI remoteClient, UUID itemID, int AttachmentPt)
        {
            m_log.DebugFormat(
                "[ATTACHMENTS MODULE]: Rezzing attachment to point {0} from item {1} for {2}",
                (AttachmentPoint)AttachmentPt, itemID, remoteClient.Name);
            IInventoryAccessModule invAccess = m_scene.RequestModuleInterface<IInventoryAccessModule>();
            if (invAccess != null)
            {
                SceneObjectGroup objatt = invAccess.CreateObjectFromInventory(remoteClient,
                    itemID);

                if (objatt != null)
                {
                    #region Set up object for attachment status

                    InventoryItemBase item = new InventoryItemBase(itemID, remoteClient.AgentId);
                    item = m_scene.InventoryService.GetItem(item);

                    objatt.RootPart.Flags |= PrimFlags.Phantom;
                    objatt.RootPart.IsAttachment = true;
                    objatt.SetFromItemID(itemID);

                    // Since renaming the item in the inventory does not affect the name stored
                    // in the serialization, transfer the correct name from the inventory to the
                    // object itself before we rez.
                    objatt.RootPart.Name = item.Name;
                    objatt.RootPart.Description = item.Description;

                    List<SceneObjectPart> partList = new List<SceneObjectPart>(objatt.ChildrenList);

                    objatt.SetGroup(remoteClient.ActiveGroupId, remoteClient);
                    if (objatt.RootPart.OwnerID != item.Owner)
                    {
                        //Need to kill the for sale here
                        objatt.RootPart.ObjectSaleType = 0;
                        objatt.RootPart.SalePrice = 10;

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

                            objatt.ApplyNextOwnerPermissions();
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
                    }
                    objatt.RootPart.TrimPermissions();
                    objatt.RootPart.IsAttachment = true;
                    objatt.IsDeleted = false;

                    //NOTE: we MUST do this manually, otherwise it will never be added!
                    //We also have to reset the IDs!
                    foreach (SceneObjectPart part in partList)
                    {
                        part.ResetEntityIDs();
                    }
                    m_scene.Entities.Add(objatt);

                    #endregion

                    //                m_log.DebugFormat(
                    //                    "[ATTACHMENTS MODULE]: Retrieved single object {0} for attachment to {1} on point {2}", 
                    //                    objatt.Name, remoteClient.Name, AttachmentPt);

                    FindAttachmentPoint(remoteClient, objatt.LocalId, objatt, AttachmentPt);

                    // Fire after attach, so we don't get messy perms dialogs
                    // 4 == AttachedRez
                    objatt.CreateScriptInstances(0, true, 4, UUID.Zero);
                    objatt.ResumeScripts();
                }
                else
                {
                    m_log.WarnFormat(
                        "[ATTACHMENTS MODULE]: Could not retrieve item {0} for attaching to avatar {1} at point {2}",
                        itemID, remoteClient.Name, AttachmentPt);
                }

                return objatt;
            }

            return null;
        }

        #endregion

        protected void FindAttachmentPoint(IClientAPI remoteClient, uint localID, SceneObjectGroup group,
            int AttachmentPt)
        {
            Vector3 attachPos = group.AbsolutePosition;
            // TODO: this short circuits multiple attachments functionality  in  LL viewer 2.1+ and should
            // be removed when that functionality is implemented in opensim
            AttachmentPt &= 0x7f;

            // If the attachment point isn't the same as the one previously used
            // set it's offset position = 0 so that it appears on the attachment point
            // and not in a weird location somewhere unknown.
            if (AttachmentPt != 0 && AttachmentPt != (int)group.GetAttachmentPoint())
            {
                attachPos = Vector3.Zero;
            }

            // AttachmentPt 0 means the client chose to 'wear' the attachment.
            /*if (AttachmentPt == 0)
            {
                // Check object for stored attachment point
                AttachmentPt = (int)group.GetAttachmentPoint();
                attachPos = group.GetAttachmentPos();
            }*/

            if (AttachmentPt == 0)
            {
                // Check object for older stored attachment point
                AttachmentPt = group.RootPart.Shape.State;
                //attachPos = group.AbsolutePosition;
            }

            // if we still didn't find a suitable attachment point.......
            if (AttachmentPt == 0)
            {
                // Stick it on right hand with Zero Offset from the attachment point.
                AttachmentPt = (int)AttachmentPoint.RightHand;
                attachPos = Vector3.Zero;
            }

            group.SetAttachmentPoint((byte)AttachmentPt);
            group.AbsolutePosition = attachPos;

            // Remove any previous attachments
            ScenePresence sp = m_scene.GetScenePresence(remoteClient.AgentId);
            UUID itemID = UUID.Zero;
            if (sp != null)
            {
                SceneObjectGroup[] attachments = GetAttachmentsForAvatar(remoteClient.AgentId);
                foreach (SceneObjectGroup grp in attachments)
                {
                    if (grp.GetAttachmentPoint() == (byte)AttachmentPt)
                    {
                        itemID = grp.GetFromItemID();
                        break;
                    }
                }
                if (itemID != UUID.Zero)
                    DetachSingleAttachmentToInv(itemID, remoteClient, true);
            }
            itemID = group.GetFromItemID();

            if (itemID == UUID.Zero)
            {
                //Delete the object inworld to inventory

                List<SceneObjectGroup> groups = new List<SceneObjectGroup>(1) { group };
                
                IInventoryAccessModule inventoryAccess = m_scene.RequestModuleInterface<IInventoryAccessModule>();
                if (inventoryAccess != null)
                    inventoryAccess.DeleteToInventory(DeRezAction.AcquireToUserInventory, UUID.Zero,
                        groups, remoteClient.AgentId, out itemID);
            }
            ShowAttachInUserInventory(remoteClient, AttachmentPt, itemID, group);
            AttachToAgent(sp, group, AttachmentPt, attachPos);

            if (AvatarFactory != null)
                AvatarFactory.QueueAppearanceSave(remoteClient.AgentId);

            m_scene.EventManager.TriggerOnAttach(localID, group.GetFromItemID(), remoteClient.AgentId);
        }

        /// <summary>
        /// Update the user inventory to reflect an attachment
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="AttachmentPt"></param>
        /// <param name="itemID"></param>
        /// <param name="att"></param>
        protected void ShowAttachInUserInventory(
            IClientAPI remoteClient, int AttachmentPt, UUID itemID, SceneObjectGroup att)
        {
            //            m_log.DebugFormat(
            //                "[USER INVENTORY]: Updating attachment {0} for {1} at {2} using item ID {3}", 
            //                att.Name, remoteClient.Name, AttachmentPt, itemID);

            if (UUID.Zero == itemID)
            {
                m_log.Error("[ATTACHMENTS MODULE]: Unable to save attachment. Error inventory item ID.");
                return;
            }

            if (0 == AttachmentPt)
            {
                m_log.Error("[ATTACHMENTS MODULE]: Unable to save attachment. Error attachment point.");
                return;
            }

            if (null == att.RootPart)
            {
                m_log.Error("[ATTACHMENTS MODULE]: Unable to save attachment for a prim without the rootpart!");
                return;
            }

            ScenePresence presence;
            if (m_scene.TryGetScenePresence(remoteClient.AgentId, out presence))
            {
                // XXYY!!
                InventoryItemBase item = new InventoryItemBase(itemID, remoteClient.AgentId);
                item = m_scene.InventoryService.GetItem(item);
                att.SetFromItemID(itemID);
                if(presence.Appearance.SetAttachment((int)AttachmentPt, itemID, item.AssetID))
                    AvatarFactory.QueueAppearanceSave(remoteClient.AgentId);
            }
        }

        #region Detach

        public void DetachObject(uint objectLocalID, IClientAPI remoteClient)
        {
            SceneObjectGroup group = m_scene.GetGroupByPrim(objectLocalID);
            if (group != null)
            {
                //group.DetachToGround();
                ShowDetachInUserInventory(group.GetFromItemID(), remoteClient);
            }
        }

        public void ShowDetachInUserInventory(UUID itemID, IClientAPI remoteClient)
        {
            ScenePresence presence;
            if (m_scene.TryGetScenePresence(remoteClient.AgentId, out presence))
            {
                presence.Appearance.DetachAttachment(itemID);

                m_log.Debug("[ATTACHMENTS MODULE]: Detaching from UserID: " + remoteClient.AgentId + ", ItemID: " + itemID);
                if (AvatarFactory != null)
                    AvatarFactory.QueueAppearanceSave(remoteClient.AgentId);
            }

            DetachSingleAttachmentToInv(itemID, remoteClient, true);
        }

        public void DetachSingleAttachmentToGround(UUID itemID, IClientAPI remoteClient)
        {
            SceneObjectPart part = m_scene.GetSceneObjectPart(itemID);
            if (part == null || part.ParentGroup == null)
                return;

            if (part.ParentGroup.RootPart.AttachedAvatar != remoteClient.AgentId)
                return;

            UUID inventoryID = part.ParentGroup.GetFromItemID();

            ScenePresence presence;
            if (m_scene.TryGetScenePresence(remoteClient.AgentId, out presence))
            {
                string reason;
                if (!m_scene.Permissions.CanRezObject(
                    part.ParentGroup.PrimCount, remoteClient.AgentId, presence.AbsolutePosition, out reason))
                    return;

                presence.Appearance.DetachAttachment(itemID);

                AvatarFactory.QueueAppearanceSave(remoteClient.AgentId);
                
                part.ParentGroup.DetachToGround();

                List<UUID> uuids = new List<UUID>();
                uuids.Add(inventoryID);
                m_scene.InventoryService.DeleteItems(remoteClient.AgentId, uuids);
                remoteClient.SendRemoveInventoryItem(inventoryID);
            }

            m_scene.EventManager.TriggerOnAttach(part.ParentGroup.LocalId, itemID, UUID.Zero);
        }

        // What makes this method odd and unique is it tries to detach using an UUID....     Yay for standards.
        // To LocalId or UUID, *THAT* is the question. How now Brown UUID??
        public void DetachSingleAttachmentToInv(UUID itemID, IClientAPI remoteClient, bool fireEvent)
        {
            if (itemID == UUID.Zero) // If this happened, someone made a mistake....
                return;

            // We can NOT use the dictionaries here, as we are looking
            // for an entity by the fromAssetID, which is NOT the prim UUID
            EntityBase[] detachEntities = m_scene.Entities.GetEntities();
            SceneObjectGroup group;

            foreach (EntityBase entity in detachEntities)
            {
                if (entity is SceneObjectGroup)
                {
                    group = (SceneObjectGroup)entity;
                    if (group.GetFromItemID() == itemID)
                    {
                        if (fireEvent)
                        {
                            m_scene.EventManager.TriggerOnAttach(group.LocalId, itemID, UUID.Zero);

                            group.DetachToInventoryPrep();
                        }
                        
                        m_log.Debug("[ATTACHMENTS MODULE]: Saving attachpoint: " + ((uint)group.GetAttachmentPoint()).ToString());
                        
                        //Update the saved attach points
                        group.RootPart.SavedAttachedPos = group.RootPart.AttachedPos;
                        group.RootPart.SavedAttachmentPoint = group.RootPart.AttachmentPoint;

                        // If an item contains scripts, it's always changed.
                        // This ensures script state is saved on detach
                        foreach (SceneObjectPart p in group.Parts)
                            if (p.Inventory.ContainsScripts())
                                group.HasGroupChanged = true;

                        UpdateKnownItem(remoteClient, group, group.GetFromItemID(), group.OwnerID);

                        IBackupModule backup = m_scene.RequestModuleInterface<IBackupModule>();
                        if (backup != null)
                            backup.DeleteSceneObjects(new SceneObjectGroup[1] { group }, true);
                    }
                }
            }
        }

        #endregion

        public void UpdateAttachmentPosition(IClientAPI client, SceneObjectGroup sog, Vector3 pos)
        {
            // If this is an attachment, then we need to save the modified
            // object back into the avatar's inventory. First we save the
            // attachment point information, then we update the relative 
            // positioning (which caused this method to get driven in the
            // first place. Then we have to mark the object as NOT an
            // attachment. This is necessary in order to correctly save
            // and retrieve GroupPosition information for the attachment.
            // Then we save the asset back into the appropriate inventory
            // entry. Finally, we restore the object's attachment status.
            byte attachmentPoint = (byte)sog.RootPart.AttachmentPoint;
            sog.UpdateGroupPosition(pos, true);
            sog.RootPart.IsAttachment = false;
            sog.AbsolutePosition = sog.RootPart.AttachedPos;
            UpdateKnownItem(client, sog, sog.GetFromItemID(), sog.OwnerID);
            sog.SetAttachmentPoint(attachmentPoint);
        }

        /// <summary>
        /// Update the attachment asset for the new sog details if they have changed.
        /// </summary>
        /// 
        /// This is essential for preserving attachment attributes such as permission.  Unlike normal scene objects,
        /// these details are not stored on the region.
        /// 
        /// <param name="remoteClient"></param>
        /// <param name="grp"></param>
        /// <param name="itemID"></param>
        /// <param name="agentID"></param>
        public void UpdateKnownItem(IClientAPI remoteClient, SceneObjectGroup grp, UUID itemID, UUID agentID)
        {
            if (grp != null)
            {
                if (!grp.HasGroupChanged)
                {
                    m_log.WarnFormat("[ATTACHMENTS MODULE]: Save request for {0} which is unchanged", grp.UUID);
                    return;
                }

                m_log.DebugFormat(
                    "[ATTACHMENTS MODULE]: Updating asset for attachment {0}, attachpoint {1}",
                    grp.UUID, grp.GetAttachmentPoint());

                string sceneObjectXml = SceneObjectSerializer.ToOriginalXmlFormat(grp);

                InventoryItemBase item = new InventoryItemBase(itemID, remoteClient.AgentId);
                item = m_scene.InventoryService.GetItem(item);

                if (item != null)
                {
                    AssetBase asset = new AssetBase(UUID.Random(), grp.GetPartName(grp.LocalId),
                        (sbyte)AssetType.Object, remoteClient.AgentId.ToString());
                    asset.Description = grp.GetPartDescription(grp.LocalId);
                    asset.Data = Utils.StringToBytes(sceneObjectXml);
                    m_scene.AssetService.Store(asset);

                    item.AssetID = asset.FullID;
                    item.Description = asset.Description;
                    item.Name = asset.Name;
                    item.AssetType = asset.Type;
                    item.InvType = (int)InventoryType.Object;

                    m_scene.InventoryService.UpdateItem(item);

                    // this gets called when the agent logs off!
                    if (remoteClient != null)
                        remoteClient.SendInventoryItemCreateUpdate(item, 0);
                }
            }
        }

        /// <summary>
        /// Attach this scene object to the given avatar.
        /// </summary>
        /// 
        /// This isn't publicly available since attachments should always perform the corresponding inventory 
        /// operation (to show the attach in user inventory and update the asset with positional information).
        /// 
        /// <param name="sp"></param>
        /// <param name="so"></param>
        /// <param name="attachmentpoint"></param>
        /// <param name="AttachOffset"></param>
        /// <param name="silent"></param>
        protected void AttachToAgent(ScenePresence avatar, SceneObjectGroup so, int attachmentpoint, Vector3 AttachOffset)
        {
            // don't attach attachments to child agents
            if (avatar.IsChildAgent) return;

            //                m_log.DebugFormat("[ATTACHMENTS MODULE]: Adding attachment {0} to avatar {1}", Name, avatar.Name);

            so.RootPart.AttachedAvatar = avatar.UUID;

            //Anakin Lohner bug #3839 
            SceneObjectPart[] parts = so.Parts;
            for (int i = 0; i < parts.Length; i++)
                parts[i].AttachedAvatar = avatar.UUID;

            if (so.RootPart.PhysActor != null)
            {
                m_scene.SceneGraph.PhysicsScene.RemovePrim(so.RootPart.PhysActor);
                so.RootPart.PhysActor = null;
            }

            so.AbsolutePosition = AttachOffset;
            so.RootPart.AttachedPos = AttachOffset;
            so.RootPart.IsAttachment = true;

            so.RootPart.SetParentLocalId(avatar.LocalId);
            so.SetAttachmentPoint(Convert.ToByte(attachmentpoint));

            AvatarAttachments attPlugin = avatar.RequestModuleInterface<AvatarAttachments>();
            if (attPlugin != null)
                attPlugin.AddAttachment(so);

            // Killing it here will cause the client to deselect it
            // It then reappears on the avatar, deselected
            // through the full update below
            //
            if (so.IsSelected)
            {
                m_scene.ForEachClient(delegate(IClientAPI client)
                {
                    client.SendKillObject(m_scene.RegionInfo.RegionHandle, new ISceneEntity[] { so.RootPart }); 
                });
                foreach (SceneObjectPart part in so.ChildrenList)
                {
                    part.CreateSelected = true;
                }
            }

            //NOTE: This MUST be here, otherwise we limit full updates during attachments when they are selected and it will block the first update.
            // So until that is changed, this MUST stay. The client will instantly reselect it, so this value doesn't stay borked for long.
            so.IsSelected = false;

            so.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);

            // In case it is later dropped again, don't let
            // it get cleaned up
            so.RootPart.RemFlag(PrimFlags.TemporaryOnRez);
            so.HasGroupChanged = false;
        }

        public SceneObjectGroup[] GetAttachmentsForAvatar(UUID avatarID)
        {
            SceneObjectGroup[] attachments = new SceneObjectGroup[0];

            ScenePresence presence = m_scene.GetScenePresence(avatarID);
            if (presence != null)
            {
                AvatarAttachments attPlugin = presence.RequestModuleInterface<AvatarAttachments>();
                if (attPlugin != null)
                    attachments = attPlugin.Get();
            }

            return attachments;
        }

        /// <summary>
        /// Send a script event to this scene presence's attachments
        /// </summary>
        /// <param name="avatarID">The avatar to fire the event for</param>
        /// <param name="eventName">The name of the event</param>
        /// <param name="args">The arguments for the event</param>
        public void SendScriptEventToAttachments(UUID avatarID, string eventName, Object[] args)
        {
            SceneObjectGroup[] attachments = GetAttachmentsForAvatar(avatarID);
            IScriptModule[] scriptEngines = m_scene.RequestModuleInterfaces<IScriptModule>();
            foreach (SceneObjectGroup grp in attachments)
            {
                foreach (IScriptModule m in scriptEngines)
                {
                    if (m == null) // No script engine loaded
                        continue;

                    m.PostObjectEvent(grp.RootPart.UUID, eventName, args);
                }
            }
        }

        public void ValidateAttachments(UUID avatarID)
        {
            SceneObjectGroup[] attachments = GetAttachmentsForAvatar(avatarID);
            ScenePresence presence = m_scene.GetScenePresence(avatarID);
            if (presence == null)
                return;
            if (attachments.Length > 0)
            {
                for (int i = 0; i < attachments.Length; i++)
                {
                    if (attachments[i].IsDeleted)
                    {
                        presence.ControllingClient.SendAlertMessage("System: A broken attachment was found, removing it from your avatar. Your attachments may be wrong.");
                        DetachObject(attachments[i].LocalId, presence.ControllingClient);
                        continue;
                    }
                    //Save it and prep it for transfer
                    DetachSingleAttachmentToInv(attachments[i].RootPart.FromItemID, presence.ControllingClient, false);
                }
            }
        }

        private class AvatarAttachments
        {
            private List<SceneObjectGroup> m_attachments = new List<SceneObjectGroup>();
            private ScenePresence m_presence;

            public AvatarAttachments(ScenePresence SP)
            {
                m_presence = SP;
                m_presence.RegisterModuleInterface<AvatarAttachments>(this);
            }

            public void AddAttachment(SceneObjectGroup attachment)
            {
                lock (m_attachments)
                {
                    if(!m_attachments.Contains(attachment))
                        m_attachments.Add(attachment);
                }
            }

            public void RemoveAttachment(SceneObjectGroup attachment)
            {
                lock (m_attachments)
                {
                    if (m_attachments.Contains(attachment))
                        m_attachments.Remove(attachment);
                }
            }

            public SceneObjectGroup[] Get()
            {
                SceneObjectGroup[] attachments = new SceneObjectGroup[m_attachments.Count];
                lock (m_attachments)
                {
                    m_attachments.CopyTo(attachments);
                }
                return attachments;
            }
        }
    }
}
