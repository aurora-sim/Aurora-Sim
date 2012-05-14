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
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;

namespace Aurora.Modules.Attachments
{
    public class AttachmentsModule : IAttachmentsModule, INonSharedRegionModule
    {
        #region Declares

        protected IScene m_scene = null;
        protected bool m_allowMultipleAttachments = true;
        protected int m_maxNumberOfAttachments = 38;

        public string Name { get { return "Attachments Module"; } }
        public Type ReplaceableInterface { get { return null; } }
        public IAvatarFactory AvatarFactory = null;

        #endregion

        #region INonSharedRegionModule Methods

        public void Initialise(IConfigSource source)
        {
            if (source.Configs["Attachments"] != null)
            {
                m_maxNumberOfAttachments = source.Configs["Attachments"].GetInt("MaxNumberOfAttachments", m_maxNumberOfAttachments);
                m_allowMultipleAttachments = source.Configs["Attachments"].GetBoolean("EnableMultipleAttachments", m_allowMultipleAttachments);
            }
        }

        public void AddRegion (IScene scene)
        {
            m_scene = scene;
            m_scene.RegisterModuleInterface<IAttachmentsModule>(this);
            m_scene.EventManager.OnNewClient += SubscribeToClientEvents;
            m_scene.EventManager.OnClosingClient += UnsubscribeFromClientEvents;
            m_scene.EventManager.OnMakeRootAgent += ResumeAvatar;
            m_scene.EventManager.OnAgentFailedToLeave += ResumeAvatar;
            m_scene.EventManager.OnSetAgentLeaving += AgentIsLeaving;
        }

        public void RemoveRegion (IScene scene)
        {
            m_scene.UnregisterModuleInterface<IAttachmentsModule>(this);
            m_scene.EventManager.OnNewClient -= SubscribeToClientEvents;
            m_scene.EventManager.OnClosingClient -= UnsubscribeFromClientEvents;
            m_scene.EventManager.OnMakeRootAgent -= ResumeAvatar;
            m_scene.EventManager.OnAgentFailedToLeave -= ResumeAvatar;
            m_scene.EventManager.OnSetAgentLeaving -= AgentIsLeaving;
        }

        public void RegionLoaded (IScene scene) 
        {
            AvatarFactory = scene.RequestModuleInterface<IAvatarFactory>();
        }

        public void Close()
        {
            RemoveRegion(m_scene);
        }

        protected void AgentIsLeaving(IScenePresence presence, OpenSim.Services.Interfaces.GridRegion destination)
        {
            //If its a root agent, we need to save all attachments as well
            if(!presence.IsChildAgent)
                SuspendAvatar(presence, destination);
        }

        #endregion

        #region Suspend/Resume avatars

        private Dictionary<UUID, List<IScenePresence>> _usersToSendAttachmentsToWhenLoaded = new Dictionary<UUID, List<IScenePresence>>();
        public void ResumeAvatar(IScenePresence presence)
        {
            Util.FireAndForget(delegate
                                   {
                IAvatarAppearanceModule appearance = presence.RequestModuleInterface<IAvatarAppearanceModule>();
                if (null == appearance || null == appearance.Appearance)
                {
                    MainConsole.Instance.WarnFormat("[ATTACHMENT]: Appearance has not been initialized for agent {0}", presence.UUID);
                    return;
                }

                //Create the avatar attachments plugin for the av
                AvatarAttachments attachmentsPlugin = new AvatarAttachments(presence);
                presence.RegisterModuleInterface(attachmentsPlugin);

                List<AvatarAttachment> attachments = appearance.Appearance.GetAttachments();
                foreach (AvatarAttachment attach in attachments)
                {
                    try
                    {
                        RezSingleAttachmentFromInventory(presence.ControllingClient, attach.ItemID, attach.AssetID, 0, false);
                    }
                    catch (Exception e)
                    {
                        MainConsole.Instance.ErrorFormat("[ATTACHMENT]: Unable to rez attachment: {0}", e);
                    }
                }
                presence.AttachmentsLoaded = true;
                lock (_usersToSendAttachmentsToWhenLoaded)
                {
                    if (_usersToSendAttachmentsToWhenLoaded.ContainsKey(presence.UUID))
                    {
                        foreach (var id in _usersToSendAttachmentsToWhenLoaded[presence.UUID])
                        {
                            SendAttachmentsToPresence(id, presence);
                        }
                        _usersToSendAttachmentsToWhenLoaded.Remove(presence.UUID);
                    }
                }
            });
        }

        public void SendAttachmentsToPresence(IScenePresence receiver, IScenePresence sender)
        {
            if (sender.AttachmentsLoaded)
            {
                ISceneEntity[] entities = GetAttachmentsForAvatar(sender.UUID);
                foreach (ISceneEntity entity in entities)
                {
                    receiver.SceneViewer.QueuePartForUpdate(entity.RootChild, PrimUpdateFlags.ForcedFullUpdate);
#if (!ISWIN)
                    foreach (ISceneChildEntity child in entity.ChildrenEntities())
                    {
                        if (!child.IsRoot)
                        {
                            receiver.SceneViewer.QueuePartForUpdate(child, PrimUpdateFlags.ForcedFullUpdate);
                        }
                    }
#else
                                               foreach (ISceneChildEntity child in entity.ChildrenEntities().Where(child => !child.IsRoot))
                                               {
                                                   receiver.SceneViewer.QueuePartForUpdate(child, PrimUpdateFlags.ForcedFullUpdate);
                                               }
#endif
                }
            }
            else
            {
                lock (_usersToSendAttachmentsToWhenLoaded)
                    if (_usersToSendAttachmentsToWhenLoaded.ContainsKey(sender.UUID))
                        _usersToSendAttachmentsToWhenLoaded[sender.UUID].Add(receiver);
                    else
                        _usersToSendAttachmentsToWhenLoaded.Add(sender.UUID, new List<IScenePresence>() { receiver });
            }
        }

        public void SuspendAvatar(IScenePresence presence, OpenSim.Services.Interfaces.GridRegion destination)
        {
            presence.AttachmentsLoaded = false;
            ISceneEntity[] attachments = GetAttachmentsForAvatar(presence.UUID);
            foreach (ISceneEntity group in attachments)
            {
                if (group.RootChild.AttachedPos != group.RootChild.SavedAttachedPos ||
                    group.RootChild.SavedAttachmentPoint != group.RootChild.AttachmentPoint)
                {
                    group.RootChild.SavedAttachedPos = group.RootChild.AttachedPos;
                    group.RootChild.SavedAttachmentPoint = group.RootChild.AttachmentPoint;
                    //Make sure we get updated
                    group.HasGroupChanged = true;
                }

                // If an item contains scripts, it's always changed.
                // This ensures script state is saved on detach
                foreach (ISceneChildEntity p in group.ChildrenEntities())
                {
                    if (p.Inventory.ContainsScripts())
                    {
                        group.HasGroupChanged = true;
                        break;
                    }
                }
                if (group.HasGroupChanged)
                {
                    UUID assetID = UpdateKnownItem(presence.ControllingClient, group,
                        group.RootChild.FromUserInventoryItemID, group.OwnerID);
                    group.RootChild.FromUserInventoryAssetID = assetID;
                }
            }
            IAvatarAppearanceModule appearance = presence.RequestModuleInterface<IAvatarAppearanceModule>();
            if (appearance != null)
                appearance.Appearance.SetAttachments(attachments);
            IBackupModule backup = presence.Scene.RequestModuleInterface<IBackupModule>();
            if (backup != null)
            {
                bool sendUpdates = destination == null;
                if (!sendUpdates)
                {
                    List<OpenSim.Services.Interfaces.GridRegion> regions = presence.Scene.RequestModuleInterface<IGridRegisterModule>().GetNeighbors(presence.Scene);
                    regions.RemoveAll((r) => r.RegionID != destination.RegionID);
                    sendUpdates = regions.Count == 0;
                }
                backup.DeleteSceneObjects(attachments, false, sendUpdates);
            }
        }

        #endregion

        #region Client Events

        #region Subscribing to Client Events

        protected void SubscribeToClientEvents(IClientAPI client)
        {
            client.OnRezSingleAttachmentFromInv += ClientRezSingleAttachmentFromInventory;
            client.OnObjectAttach += ClientAttachObject;
            client.OnObjectDetach += ClientDetachObject;
            client.OnObjectDrop += ClientDropObject;
            client.OnDetachAttachmentIntoInv += DetachSingleAttachmentToInventory;
            client.OnUpdatePrimGroupPosition += ClientUpdateAttachmentPosition;
        }

        protected void UnsubscribeFromClientEvents(IClientAPI client)
        {
            client.OnRezSingleAttachmentFromInv -= ClientRezSingleAttachmentFromInventory;
            client.OnObjectAttach -= ClientAttachObject;
            client.OnObjectDetach -= ClientDetachObject;
            client.OnObjectDrop -= ClientDropObject;
            client.OnDetachAttachmentIntoInv -= DetachSingleAttachmentToInventory;
            client.OnUpdatePrimGroupPosition -= ClientUpdateAttachmentPosition;
        }

        #endregion

        protected UUID ClientRezSingleAttachmentFromInventory(
            IClientAPI remoteClient, UUID itemID, int AttachmentPt)
        {
            IScenePresence presence = m_scene.GetScenePresence(remoteClient.AgentId);
            if (presence != null && presence.SuccessfullyMadeRootAgent)
            {
                ISceneEntity att = RezSingleAttachmentFromInventory(remoteClient, itemID, UUID.Zero, AttachmentPt, true);

                if (null == att)
                    return UUID.Zero;
                return att.UUID;
            }
            return UUID.Zero;
        }

        protected void ClientDetachObject(uint objectLocalID, IClientAPI remoteClient)
        {
            ISceneEntity group = m_scene.GetGroupByPrim(objectLocalID);
            if (group != null)
            {
                //group.DetachToGround();
                DetachSingleAttachmentToInventory(group.RootChild.FromUserInventoryItemID, remoteClient);
            }
            else
                SendKillEntity(new SceneObjectPart { LocalId = objectLocalID });
        }

        protected void ClientDropObject(uint objectLocalID, IClientAPI remoteClient)
        {
            ISceneEntity group = m_scene.GetGroupByPrim (objectLocalID);
            if (group != null)
                DetachSingleAttachmentToGround(group.UUID, remoteClient);
        }

        protected void ClientAttachObject(IClientAPI remoteClient, uint objectLocalID, int AttachmentPt, bool silent)
        {
            MainConsole.Instance.Debug("[ATTACHMENTS MODULE]: Invoking AttachObject");

            try
            {
                // If we can't take it, we can't attach it!
                ISceneChildEntity part = m_scene.GetSceneObjectPart (objectLocalID);
                if (part == null)
                    return;

                // Calls attach with a Zero position
                AttachObjectFromInworldObject(objectLocalID, remoteClient, part.ParentEntity, AttachmentPt);
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[ATTACHMENTS MODULE]: exception upon Attach Object {0}", e);
            }
        }

        protected void ClientUpdateAttachmentPosition(uint objectLocalID, Vector3 pos, IClientAPI remoteClient, bool SaveUpdate)
        {
            ISceneEntity group = m_scene.GetGroupByPrim (objectLocalID);
            if (group != null)
            {
                if (group.IsAttachment || (group.RootChild.Shape.PCode == 9 && group.RootChild.Shape.State != 0))
                {
                    //Move has edit permission as well
                    if (m_scene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId))
                    {
                        //Only deal with attachments!
                        UpdateAttachmentPosition(remoteClient, group, objectLocalID, pos);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        #region Attach

        public bool AttachObjectFromInworldObject(uint localID, IClientAPI remoteClient,
            ISceneEntity group, int AttachmentPt)
        {
            if (m_scene.Permissions.CanTakeObject(group.UUID, remoteClient.AgentId))
                FindAttachmentPoint(remoteClient, localID, group, AttachmentPt, UUID.Zero, true);
            else
            {
                remoteClient.SendAgentAlertMessage(
                    "You don't have sufficient permissions to attach this object", false);

                return false;
            }

            return true;
        }

        public ISceneEntity RezSingleAttachmentFromInventory (
            IClientAPI remoteClient, UUID itemID, UUID assetID, int AttachmentPt, bool updateUUIDs)
        {
            MainConsole.Instance.DebugFormat(
                "[ATTACHMENTS MODULE]: Rezzing attachment to point {0} from item {1} for {2}",
                (AttachmentPoint)AttachmentPt, itemID, remoteClient.Name);
            IInventoryAccessModule invAccess = m_scene.RequestModuleInterface<IInventoryAccessModule>();
            if (invAccess != null)
            {
                InventoryItemBase item = null;
                SceneObjectGroup objatt = assetID == UUID.Zero ? invAccess.CreateObjectFromInventory(remoteClient,
                    itemID, out item) : invAccess.CreateObjectFromInventory(remoteClient, itemID, assetID);

                if (objatt != null)
                {
                    #region Set up object for attachment status
                    if (item != null)
                    {
                        assetID = item.AssetID;

                        // Since renaming the item in the inventory does not affect the name stored
                        // in the serialization, transfer the correct name from the inventory to the
                        // object itself before we rez.
                        objatt.RootPart.Name = item.Name;
                        objatt.RootPart.Description = item.Description;
                    }

                    objatt.RootPart.Flags |= PrimFlags.Phantom;
                    objatt.RootPart.IsAttachment = true;
                    objatt.SetFromItemID(itemID, assetID);

                    List<SceneObjectPart> partList = new List<SceneObjectPart>(objatt.ChildrenList);

                    foreach(SceneObjectPart part in partList)
                        part.AttachedAvatar = remoteClient.AgentId;

                    objatt.SetGroup(remoteClient.ActiveGroupId, remoteClient.AgentId, false);
                    if (objatt.RootPart.OwnerID != remoteClient.AgentId)
                    {
                        //Need to kill the for sale here
                        objatt.RootPart.ObjectSaleType = 0;
                        objatt.RootPart.SalePrice = 10;

                        if (m_scene.Permissions.PropagatePermissions())
                        {
                            if (item == null)
                            {
                                item = new InventoryItemBase(itemID, remoteClient.AgentId);
                                item = m_scene.InventoryService.GetItem(item);
                            }
                            if (item == null)
                                return null;
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
                        if (part.OwnerID != remoteClient.AgentId)
                        {
                            part.LastOwnerID = part.OwnerID;
                            part.OwnerID = remoteClient.AgentId;
                            part.Inventory.ChangeInventoryOwner(remoteClient.AgentId);
                        }
                    }
                    objatt.RootPart.TrimPermissions();
                    objatt.RootPart.IsAttachment = true;
                    objatt.IsDeleted = false;

                    //Update the ItemID with the new item
                    objatt.SetFromItemID(itemID, assetID);

                    //DO NOT SEND THIS KILL ENTITY
                    // If we send this, when someone copies an inworld object, then wears it, the inworld objects disapepars
                    // If a bug is caused by this, we need to figure out some other workaround.
                    //SendKillEntity(objatt.RootChild);
                    //We also have to reset the IDs so that it doesn't have the same IDs as one inworld (possibly)!
                    ISceneEntity[] atts = GetAttachmentsForAvatar(remoteClient.AgentId);
                    foreach (var obj in atts)
                        if (obj.UUID == objatt.UUID)
                            updateUUIDs = false;
                    bool forceUpdateOnNextDeattach = false;
                    try
                    {
                        if (updateUUIDs)
                        {
                            m_scene.SceneGraph.AddPrimToScene(objatt);
                            forceUpdateOnNextDeattach = true;//If the user has information stored about this object, we need to force updating next time
                        }
                        else
                        {
                            foreach (var prim in objatt.ChildrenEntities())
                            {
                                prim.LocalId = 0;
                            }
                            bool success = m_scene.SceneGraph.RestorePrimToScene(objatt, true);
                            if (!success)
                                MainConsole.Instance.Error("[AttachmentModule]: Failed to add attachment " + objatt.Name + " for user " + remoteClient.Name + "!");
                        }
                    }
                    catch { }

                    //If we updated the attachment, we need to save the change
                    IScenePresence presence = m_scene.GetScenePresence(remoteClient.AgentId);
                    if (presence != null)
                    {
                        FindAttachmentPoint(remoteClient, objatt.LocalId, objatt, AttachmentPt, assetID, forceUpdateOnNextDeattach);
                    }
                    else
                        objatt = null;//Presence left, kill the attachment
                    #endregion
                }
                else
                {
                    MainConsole.Instance.WarnFormat(
                        "[ATTACHMENTS MODULE]: Could not retrieve item {0} for attaching to avatar {1} at point {2}",
                        itemID, remoteClient.Name, AttachmentPt);
                }

                return objatt;
            }

            return null;
        }

        #endregion

        #region Detach

        public void DetachSingleAttachmentToInventory (UUID itemID, IClientAPI remoteClient)
        {
            ISceneEntity[] attachments = GetAttachmentsForAvatar (remoteClient.AgentId);
            IScenePresence presence;
            if (m_scene.TryGetScenePresence (remoteClient.AgentId, out presence))
            {
                IAvatarAppearanceModule appearance = presence.RequestModuleInterface<IAvatarAppearanceModule> ();
                if (!appearance.Appearance.DetachAttachment(itemID))
                {
                    bool found = false;
                    foreach (ISceneEntity grp in attachments)
                    {
                        if (grp.RootChild.FromUserInventoryItemID == itemID)
                            found = true;
                    }
                    if (!found)
                        return; //Its not attached! What are we doing!
                }

                MainConsole.Instance.Debug ("[ATTACHMENTS MODULE]: Detaching from UserID: " + remoteClient.AgentId + ", ItemID: " + itemID);
                if (AvatarFactory != null)
                    AvatarFactory.QueueAppearanceSave (remoteClient.AgentId);
            }

            DetachSingleAttachmentToInventoryInternal (itemID, remoteClient, true);
            //Find the attachment we are trying to edit by ItemID
            foreach (ISceneEntity grp in attachments)
            {
                if (grp.RootChild.FromUserInventoryItemID == itemID)
                {
                    //And from storage as well
                    IBackupModule backup = presence.Scene.RequestModuleInterface<IBackupModule> ();
                    if (backup != null)
                        backup.DeleteSceneObjects (new[]{grp}, false, true);
                }
            }
        }

        public void DetachSingleAttachmentToGround(UUID itemID, IClientAPI remoteClient)
        {
            ISceneChildEntity part = m_scene.GetSceneObjectPart (itemID);
            if (part == null || part.ParentEntity == null)
                return;

            if (part.ParentEntity.RootChild.AttachedAvatar != remoteClient.AgentId)
                return;

            UUID inventoryID = part.ParentEntity.RootChild.FromUserInventoryItemID;

            IScenePresence presence;
            if (m_scene.TryGetScenePresence(remoteClient.AgentId, out presence))
            {
                string reason;
                if (!m_scene.Permissions.CanRezObject(
                    part.ParentEntity.PrimCount, remoteClient.AgentId, presence.AbsolutePosition, out reason))
                    return;

                IAvatarAppearanceModule appearance = presence.RequestModuleInterface<IAvatarAppearanceModule> ();
                appearance.Appearance.DetachAttachment (itemID);

                AvatarFactory.QueueAppearanceSave(remoteClient.AgentId);

                part.ParentEntity.DetachToGround();

                List<UUID> uuids = new List<UUID> {inventoryID};
                m_scene.InventoryService.DeleteItems(remoteClient.AgentId, uuids);
                remoteClient.SendRemoveInventoryItem(inventoryID);
            }

            m_scene.EventManager.TriggerOnAttach(part.ParentEntity.LocalId, itemID, UUID.Zero);
        }

        #endregion

        #region Get/Update/Send

        /// <summary>
        /// Update the position of the given attachment
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ItemID"></param>
        /// <param name="pos"></param>
        public void UpdateAttachmentPosition(IClientAPI client, ISceneEntity sog, uint localID, Vector3 pos)
        {
            if (sog != null)
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
                byte attachmentPoint = (byte)sog.RootChild.AttachmentPoint;
                sog.UpdateGroupPosition(pos, true);
                sog.RootChild.AttachedPos = pos;
                sog.RootChild.FixOffsetPosition((pos), false);
                //sog.AbsolutePosition = sog.RootChild.AttachedPos;
                sog.SetAttachmentPoint(attachmentPoint);
                sog.ScheduleGroupUpdate(PrimUpdateFlags.TerseUpdate);
                //Don't update right now, wait until logout
                //UpdateKnownItem(client, sog, sog.GetFromItemID(), sog.OwnerID);
            }
            else
            {
                MainConsole.Instance.Warn("[Attachments]: Could not find attachment by ItemID!");
            }
        }

        /// <summary>
        /// Get all of the attachments for the given avatar
        /// </summary>
        /// <param name="avatarID">The avatar whose attachments will be returned</param>
        /// <returns>The avatar's attachments as SceneObjectGroups</returns>
        public ISceneEntity[] GetAttachmentsForAvatar (UUID avatarID)
        {
            ISceneEntity[] attachments = new ISceneEntity[0];

            IScenePresence presence = m_scene.GetScenePresence (avatarID);
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
            ISceneEntity[] attachments = GetAttachmentsForAvatar (avatarID);
            IScriptModule[] scriptEngines = m_scene.RequestModuleInterfaces<IScriptModule>();
            foreach (ISceneEntity grp in attachments)
            {
                foreach (IScriptModule m in scriptEngines)
                {
                    if (m == null) // No script engine loaded
                        continue;

                    m.PostObjectEvent(grp.UUID, eventName, args);
                }
            }
        }

        #endregion

        #endregion

        #region Internal Methods

        /// <summary>
        /// Attach the object to the avatar
        /// </summary>
        /// <param name="remoteClient">The client that is having the attachment done</param>
        /// <param name="localID">The localID (SceneObjectPart) that is being attached (for the attach script event)</param>
        /// <param name="group">The group (SceneObjectGroup) that is being attached</param>
        /// <param name="AttachmentPt">The point to where the attachment will go</param>
        /// <param name="item">If this is not null, it saves a query in this method to the InventoryService
        /// This is the Item that the object is in (if it is in one yet)</param>
        /// <param name="assetID"/>
        /// <param name="forceUpdatePrim">Force updating of the prim the next time the user attempts to deattach it</param>
        protected void FindAttachmentPoint (IClientAPI remoteClient, uint localID, ISceneEntity group,
            int AttachmentPt, UUID assetID, bool forceUpdatePrim)
        {
            //Make sure that we arn't over the limit of attachments
            ISceneEntity[] attachments = GetAttachmentsForAvatar (remoteClient.AgentId);
            if (attachments.Length + 1 > m_maxNumberOfAttachments)
            {
                //Too many
                remoteClient.SendAgentAlertMessage(
                    "You are wearing too many attachments. Take one off to attach this object", false);

                return;
            }
            Vector3 attachPos = group.GetAttachmentPos();
            bool hasMultipleAttachmentsSet = (AttachmentPt & 0x7f) != 0 || AttachmentPt == 0;
            if(!m_allowMultipleAttachments)
                hasMultipleAttachmentsSet = false;
            AttachmentPt &= 0x7f; //Disable it! Its evil!

            //Did the attachment change position or attachment point?
            bool changedPositionPoint = false;

            // If the attachment point isn't the same as the one previously used
            // set it's offset position = 0 so that it appears on the attachment point
            // and not in a weird location somewhere unknown.
            //Simplier terms: the attachment point changed, set it to the default 0,0,0 location
            if (AttachmentPt != 0 && AttachmentPt != (group.GetAttachmentPoint() & 0x7f))
            {
                attachPos = Vector3.Zero;
                changedPositionPoint = true;
            }
            else
            {
                // AttachmentPt 0 means the client chose to 'wear' the attachment.
                if (AttachmentPt == 0)
                {
                    // Check object for stored attachment point
                    AttachmentPt = group.GetSavedAttachmentPoint() & 0x7f;
                    attachPos = group.GetAttachmentPos();
                }

                //Check state afterwards... use the newer GetSavedAttachmentPoint and Pos above first
                if (AttachmentPt == 0)
                {
                    // Check object for older stored attachment point
                    AttachmentPt = group.RootChild.Shape.State & 0x7f;
                    //attachPos = group.AbsolutePosition;
                }

                // if we still didn't find a suitable attachment point, force it to the default
                //This happens on the first time an avatar 'wears' an object
                if (AttachmentPt == 0)
                {
                    // Stick it on right hand with Zero Offset from the attachment point.
                    AttachmentPt = (int)AttachmentPoint.RightHand;
                    //Default location
                    attachPos = Vector3.Zero;
                    changedPositionPoint = true;
                }
            }

            MainConsole.Instance.InfoFormat(
                "[ATTACHMENTS MODULE]: Retrieved single object {0} for attachment to {1} on point {2} localID {3}",
                group.Name, remoteClient.Name, AttachmentPt, group.LocalId);

            //Update where we are put
            group.SetAttachmentPoint((byte)AttachmentPt);
            //Fix the position with the one we found
            group.AbsolutePosition = attachPos;

            // Remove any previous attachments
            IScenePresence presence = m_scene.GetScenePresence (remoteClient.AgentId);
            if (presence == null)
                return;
            UUID itemID = UUID.Zero;
            //Check for multiple attachment bits and whether we should remove the old
            if(!hasMultipleAttachmentsSet) 
            {
                foreach (ISceneEntity grp in attachments)
                {
                    if (grp.GetAttachmentPoint() == (byte)AttachmentPt)
                    {
                        itemID = grp.RootChild.FromUserInventoryItemID;
                        break;
                    }
                }
                if (itemID != UUID.Zero)
                    DetachSingleAttachmentToInventory(itemID, remoteClient);
            }
            itemID = group.RootChild.FromUserInventoryItemID;

            group.RootChild.AttachedAvatar = presence.UUID;

            List<ISceneChildEntity> parts = group.ChildrenEntities();
            foreach (ISceneChildEntity t in parts)
                t.AttachedAvatar = presence.UUID;

            if (group.RootChild.PhysActor != null)
            {
                m_scene.PhysicsScene.DeletePrim (group.RootChild.PhysActor);
                group.RootChild.PhysActor = null;
            }

            group.RootChild.AttachedPos = attachPos;
            group.RootChild.IsAttachment = true;
            group.AbsolutePosition = attachPos;

            group.RootChild.SetParentLocalId (presence.LocalId);
            group.SetAttachmentPoint(Convert.ToByte(AttachmentPt));

            AvatarAttachments attPlugin = presence.RequestModuleInterface<AvatarAttachments>();
            if (attPlugin != null)
            {
                attPlugin.AddAttachment (group);
                presence.SetAttachments(attPlugin.Get());
                IAvatarAppearanceModule appearance = presence.RequestModuleInterface<IAvatarAppearanceModule>();

                bool save = appearance.Appearance.CheckWhetherAttachmentChanged(AttachmentPt, itemID, assetID);
                if (save)
                {
                    appearance.Appearance.SetAttachments(attPlugin.Get());
                    AvatarFactory.QueueAppearanceSave(remoteClient.AgentId);
                }
            }

            // Killing it here will cause the client to deselect it
            // It then reappears on the avatar, deselected
            // through the full update below
            //
            if (group.IsSelected)
            {
                foreach (ISceneChildEntity part in group.ChildrenEntities())
                {
                    part.CreateSelected = true;
                }
            }

            //NOTE: This MUST be here, otherwise we limit full updates during attachments when they are selected and it will block the first update.
            // So until that is changed, this MUST stay. The client will instantly reselect it, so this value doesn't stay borked for long.
            group.IsSelected = false;

            if (itemID == UUID.Zero)
            {
                //Delete the object inworld to inventory

                List<ISceneEntity> groups = new List<ISceneEntity> (1) { group };

                IInventoryAccessModule inventoryAccess = m_scene.RequestModuleInterface<IInventoryAccessModule>();
                if (inventoryAccess != null)
                    inventoryAccess.DeleteToInventory(DeRezAction.AcquireToUserInventory, UUID.Zero,
                        groups, remoteClient.AgentId, out itemID);
            }
            else
            {
                //it came from an item, we need to start the scripts

                // Fire after attach, so we don't get messy perms dialogs
                // 4 == AttachedRez
                group.CreateScriptInstances(0, true, StateSource.AttachedRez, UUID.Zero);
            }

            if (UUID.Zero == itemID)
            {
                MainConsole.Instance.Error("[ATTACHMENTS MODULE]: Unable to save attachment. Error inventory item ID.");
                remoteClient.SendAgentAlertMessage(
                    "Unable to save attachment. Error inventory item ID.", false);
                return;
            }

            // XXYY!!
            if (assetID == UUID.Zero)
            {
                InventoryItemBase item = new InventoryItemBase(itemID, remoteClient.AgentId);
                item = m_scene.InventoryService.GetItem(item);
                //Update the ItemID with the new item
                group.SetFromItemID(itemID, item.AssetID);

                //If we updated the attachment, we need to save the change
                IAvatarAppearanceModule appearance = presence.RequestModuleInterface<IAvatarAppearanceModule>();
                if (appearance.Appearance.SetAttachment(AttachmentPt, itemID, item.AssetID))
                    AvatarFactory.QueueAppearanceSave(remoteClient.AgentId);
            }


            // In case it is later dropped again, don't let
            // it get cleaned up
            group.RootChild.RemFlag(PrimFlags.TemporaryOnRez);
            group.HasGroupChanged = changedPositionPoint || forceUpdatePrim;
            //Now recreate it so that it is selected
            group.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);

            m_scene.EventManager.TriggerOnAttach(localID, group.RootChild.FromUserInventoryItemID, remoteClient.AgentId);
        }

        protected void SendKillEntity(ISceneChildEntity rootPart)
        {
#if (!ISWIN)
            m_scene.ForEachClient(delegate(IClientAPI client)
            {
                client.SendKillObject(m_scene.RegionInfo.RegionHandle, new IEntity[] { rootPart });
            });
#else
            m_scene.ForEachClient(
                client => client.SendKillObject(m_scene.RegionInfo.RegionHandle, new IEntity[] {rootPart}));
#endif
        }

        // What makes this method odd and unique is it tries to detach using an UUID....     Yay for standards.
        // To LocalId or UUID, *THAT* is the question. How now Brown UUID??
        protected void DetachSingleAttachmentToInventoryInternal(UUID itemID, IClientAPI remoteClient, bool fireEvent)
        {
            if (itemID == UUID.Zero) // If this happened, someone made a mistake....
                return;

            // We can NOT use the dictionaries here, as we are looking
            // for an entity by the fromAssetID, which is NOT the prim UUID
            ISceneEntity[] attachments = GetAttachmentsForAvatar (remoteClient.AgentId);

            foreach (ISceneEntity group in attachments)
            {
                if (group.RootChild.FromUserInventoryItemID == itemID)
                {
                    DetachSingleAttachmentGroupToInventoryInternal (itemID, remoteClient, fireEvent, group);
                    return;
                }
            }
        }

        private void DetachSingleAttachmentGroupToInventoryInternal (UUID itemID, IClientAPI remoteClient, bool fireEvent, ISceneEntity group)
        {
            if (fireEvent)
            {
                m_scene.EventManager.TriggerOnAttach (group.LocalId, itemID, UUID.Zero);

                group.DetachToInventoryPrep ();
            }

            IScenePresence presence = m_scene.GetScenePresence (remoteClient.AgentId);
            if (presence != null)
            {
                AvatarAttachments attModule = presence.RequestModuleInterface<AvatarAttachments> ();
                if (attModule != null)
                    attModule.RemoveAttachment (group);
                if (attModule != null) presence.SetAttachments(attModule.Get());
            }

            MainConsole.Instance.Debug ("[ATTACHMENTS MODULE]: Saving attachpoint: " + ((uint)group.GetAttachmentPoint ()).ToString ());

            //Update the saved attach points
            if (group.RootChild.AttachedPos != group.RootChild.SavedAttachedPos ||
                group.RootChild.SavedAttachmentPoint != group.RootChild.AttachmentPoint)
            {
                group.RootChild.SavedAttachedPos = group.RootChild.AttachedPos;
                group.RootChild.SavedAttachmentPoint = group.RootChild.AttachmentPoint;
                //Make sure we get updated
                group.HasGroupChanged = true;
            }

            // If an item contains scripts, it's always changed.
            // This ensures script state is saved on detach
            foreach (ISceneChildEntity p in group.ChildrenEntities ())
            {
                if (p.Inventory.ContainsScripts ())
                {
                    group.HasGroupChanged = true;
                    break;
                }
            }
            if(group.HasGroupChanged)
                UpdateKnownItem (remoteClient, group, group.RootChild.FromUserInventoryItemID, group.OwnerID);
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
        protected UUID UpdateKnownItem (IClientAPI remoteClient, ISceneEntity grp, UUID itemID, UUID agentID)
        {
            if (grp != null)
            {
                if (!grp.HasGroupChanged)
                {
                    //MainConsole.Instance.WarnFormat("[ATTACHMENTS MODULE]: Save request for {0} which is unchanged", grp.UUID);
                    return UUID.Zero;
                }

                //let things like state saves and another async things be performed before we serialize the object
                grp.BackupPreparation();

                MainConsole.Instance.InfoFormat(
                    "[ATTACHMENTS MODULE]: Updating asset for attachment {0}, attachpoint {1}",
                    grp.UUID, grp.GetAttachmentPoint());

                string sceneObjectXml = SceneObjectSerializer.ToOriginalXmlFormat((SceneObjectGroup)grp);

                AssetBase asset = new AssetBase(UUID.Random(), grp.Name,
                                                    AssetType.Object, remoteClient.AgentId)
                                          {
                                              Description = grp.RootChild.Description,
                                              Data = Utils.StringToBytes(sceneObjectXml)
                                          };
                asset.ID = m_scene.AssetService.Store(asset);


                m_scene.InventoryService.UpdateAssetIDForItem(itemID, asset.ID);

                // this gets called when the agent logs off!
                //remoteClient.SendInventoryItemCreateUpdate(item, 0);

                return asset.ID;
            }
            return UUID.Zero;
        }

        #endregion

        #region Per Presence Attachment Module

        private class AvatarAttachments
        {
            private readonly List<ISceneEntity> m_attachments = new List<ISceneEntity> ();

            public AvatarAttachments (IScenePresence SP)
            {
            }

            public void AddAttachment (ISceneEntity attachment)
            {
                lock (m_attachments)
                {
                    m_attachments.RemoveAll((a) => attachment.UUID == a.UUID);
                    m_attachments.Add(attachment);
                }
            }

            public void RemoveAttachment (ISceneEntity attachment)
            {
                lock (m_attachments)
                {
                    m_attachments.RemoveAll((a) => attachment.UUID == a.UUID);
                }
            }

            public ISceneEntity[] Get ()
            {
                ISceneEntity[] attachments = new ISceneEntity[m_attachments.Count];
                lock (m_attachments)
                {
                    m_attachments.CopyTo(attachments);
                }
                return attachments;
            }
        }

        #endregion
    }
}
