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

using Aurora.Framework;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using Aurora.Framework.Serialization;
using OpenMetaverse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Region
{
    public class SceneObjectPartInventory : IEntityInventory
    {
        private string m_inventoryFileName = String.Empty;
        private byte[] m_fileData = new byte[0];
        private uint m_inventoryFileNameSerial = 0;

        /// <value>
        ///     The part to which the inventory belongs.
        /// </value>
        private readonly SceneObjectPart m_part;

        /// <summary>
        ///     Serial count for inventory file , used to tell if inventory has changed
        ///     no need for this to be part of Database backup
        /// </summary>
        protected uint m_inventorySerial = 0;

        /// <summary>
        ///     Holds in memory prim inventory
        /// </summary>
        protected TaskInventoryDictionary m_items = new TaskInventoryDictionary();

        protected object m_itemsLock = new object();

        /// <summary>
        ///     Tracks whether inventory has changed since the last persistent backup
        /// </summary>
        internal bool m_HasInventoryChanged;

        public bool HasInventoryChanged
        {
            get { return m_HasInventoryChanged; }
            set
            {
                //Set the parent as well so that backup will occur
                if (value && m_part.ParentGroup != null)
                    m_part.ParentGroup.HasGroupChanged = true;
                m_HasInventoryChanged = value;
            }
        }

        /// <value>
        ///     Inventory serial number
        /// </value>
        protected internal uint Serial
        {
            get { return m_inventorySerial; }
            set { m_inventorySerial = value; }
        }

        /// <value>
        ///     Raw inventory data
        /// </value>
        protected internal TaskInventoryDictionary Items
        {
            get { return m_items; }
            set
            {
                m_items = value;
                m_inventorySerial++;
            }
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="part">
        ///     A <see cref="SceneObjectPart" />
        /// </param>
        public SceneObjectPartInventory(SceneObjectPart part)
        {
            m_part = part;
        }

        /// <summary>
        ///     Force the task inventory of this prim to persist at the next update sweep
        /// </summary>
        public void ForceInventoryPersistence()
        {
            HasInventoryChanged = true;
        }

        /// <summary>
        ///     Reset UUIDs for all the items in the prim's inventory.  This involves either generating
        ///     new ones or setting existing UUIDs to the correct parent UUIDs.
        ///     If this method is called and there are inventory items, then we regard the inventory as having changed.
        /// </summary>
        /// <param name="ChangeScripts"></param>
        public void ResetInventoryIDs(bool ChangeScripts)
        {
            if (null == m_part || null == m_part.ParentGroup)
                return;

            if (0 == m_items.Count)
                return;

            IList<TaskInventoryItem> items = GetInventoryItems();
            lock (m_itemsLock)
            {
                m_items.Clear();

                foreach (TaskInventoryItem item in items)
                {
                    //UUID oldItemID = item.ItemID;
                    item.ResetIDs(m_part.UUID);
                    m_items.Add(item.ItemID, item);
                    //LEAVE THIS COMMENTED!!!
                    // When an object is duplicated, this will be called and it will destroy the original prims scripts!!
                    // This needs to be moved to a place that is safer later
                    //  This was *originally* intended to be used on scripts that were crossing region borders
                    /*if (m_part.ParentGroup != null)
                    {
                        lock (m_part.ParentGroup)
                        {
                            if (m_part.ParentGroup.Scene != null)
                            {
                                foreach (IScriptModule engine in m_part.ParentGroup.Scene.RequestModuleInterfaces<IScriptModule>())
                                {
                                    engine.UpdateScriptToNewObject(oldItemID, item, m_part);
                                }
                            }
                        }
                    }*/
                }
                HasInventoryChanged = true;
            }
        }

        public void ResetObjectID()
        {
            if (Items.Count == 0)
            {
                return;
            }

            lock (m_itemsLock)
            {
                HasInventoryChanged = true;
                if (m_part.ParentGroup != null)
                {
                    m_part.ParentGroup.HasGroupChanged = true;
                }

                IList<TaskInventoryItem> items = Items.Values.ToList();
                Items.Clear();

                foreach (TaskInventoryItem item in items)
                {
                    //UUID oldItemID = item.ItemID;
                    item.ResetIDs(m_part.UUID);

                    //LEAVE THIS COMMENTED!!!
                    // When an object is duplicated, this will be called and it will destroy the original prims scripts!!
                    // This needs to be moved to a place that is safer later
                    //  This was *originally* intended to be used on scripts that were crossing region borders
                    /*if (m_part.ParentGroup != null)
                    {
                        lock (m_part.ParentGroup)
                        {
                            if (m_part.ParentGroup.Scene != null)
                            {
                                foreach (IScriptModule engine in m_part.ParentGroup.Scene.RequestModuleInterfaces<IScriptModule>())
                                {
                                    engine.UpdateScriptToNewObject(oldItemID, item, m_part);
                                }
                            }
                        }
                    }*/
                    Items.Add(item.ItemID, item);
                }
            }
        }

        /// <summary>
        ///     Change every item in this inventory to a new owner.
        /// </summary>
        /// <param name="ownerId"></param>
        public void ChangeInventoryOwner(UUID ownerId)
        {
            lock (m_itemsLock)
            {
                if (0 == Items.Count)
                {
                    return;
                }
            }

            HasInventoryChanged = true;
            List<TaskInventoryItem> items = GetInventoryItems();
            foreach (TaskInventoryItem item in items)
            {
                if (ownerId != item.OwnerID)
                {
                    item.LastOwnerID = item.OwnerID;
                    item.OwnerChanged = true;
                    item.OwnerID = ownerId;
                    item.PermsMask = 0;
                    item.PermsGranter = UUID.Zero;
                }
            }
        }

        /// <summary>
        ///     Change every item in this inventory to a new group.
        /// </summary>
        /// <param name="groupID"></param>
        public void ChangeInventoryGroup(UUID groupID)
        {
            lock (m_itemsLock)
            {
                if (0 == Items.Count)
                {
                    return;
                }
            }

            HasInventoryChanged = true;
            List<TaskInventoryItem> items = GetInventoryItems();
            foreach (TaskInventoryItem item in items)
            {
                if (groupID != item.GroupID)
                    item.GroupID = groupID;
            }
        }

        /// <summary>
        ///     Start all the scripts contained in this prim's inventory
        /// </summary>
        public void CreateScriptInstances(int startParam, bool postOnRez, StateSource stateSource, UUID RezzedFrom,
                                          bool clearStateSaves)
        {
            List<TaskInventoryItem> LSLItems = GetInventoryScripts();
            if (LSLItems.Count == 0)
                return;

            bool SendUpdate = m_part.AddFlag(PrimFlags.Scripted);
            m_part.ParentGroup.Scene.EventManager.TriggerRezScripts(
                m_part, LSLItems.ToArray(), startParam, postOnRez, stateSource, RezzedFrom, clearStateSaves);
            if (SendUpdate)
                m_part.ScheduleUpdate(PrimUpdateFlags.PrimFlags); //We only need to send a compressed
            ResumeScripts();
        }

        public List<TaskInventoryItem> GetInventoryScripts()
        {
            List<TaskInventoryItem> ret = new List<TaskInventoryItem>();

            lock (m_itemsLock)
            {
                ret.AddRange(
                    m_items.Values.Where(item => item.InvType == (int) InventoryType.LSL)
                           .Where(
                               item =>
                               m_part.ParentGroup.Scene.Permissions.CanRunScript(item.ItemID, m_part.UUID, item.OwnerID)));
            }

            return ret;
        }

        public ArrayList GetScriptErrors(UUID itemID)
        {
            IScriptModule engine = m_part.ParentGroup.Scene.RequestModuleInterface<IScriptModule>();
            if (engine == null) // No engine at all
            {
                ArrayList ret = new ArrayList {"No Script Engines available at this time."};
                return ret;
            }
            return engine.GetScriptErrors(itemID);
        }

        /// <summary>
        ///     Stop all the scripts in this prim.
        /// </summary>
        /// <param name="sceneObjectBeingDeleted">
        ///     Should be true if these scripts are being removed because the scene
        ///     object is being deleted.  This will prevent spurious updates to the client.
        /// </param>
        public void RemoveScriptInstances(bool sceneObjectBeingDeleted)
        {
            List<TaskInventoryItem> scripts = GetInventoryScripts();
            foreach (TaskInventoryItem item in scripts)
                RemoveScriptInstance(item.ItemID, sceneObjectBeingDeleted);
            HasInventoryChanged = true;
        }

        /// <summary>
        ///     Start a script which is in this prim's inventory.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="startParam"></param>
        /// <param name="postOnRez"></param>
        /// <param name="stateSource"></param>
        /// <returns></returns>
        public void CreateScriptInstance(TaskInventoryItem item, int startParam, bool postOnRez, StateSource stateSource)
        {
            // MainConsole.Instance.InfoFormat(
            //     "[PRIM INVENTORY]: " +
            //     "Starting script {0}, {1} in prim {2}, {3}",
            //     item.Name, item.ItemID, Name, UUID);

            if (!m_part.ParentGroup.Scene.Permissions.CanRunScript(item.ItemID, m_part.UUID, item.OwnerID))
                return;

            if (!m_part.ParentGroup.Scene.RegionInfo.RegionSettings.DisableScripts)
            {
                lock (m_itemsLock)
                {
                    m_items[item.ItemID].PermsMask = 0;
                    m_items[item.ItemID].PermsGranter = UUID.Zero;
                }

                bool SendUpdate = m_part.AddFlag(PrimFlags.Scripted);
                m_part.ParentGroup.Scene.EventManager.TriggerRezScripts(
                    m_part, new[] {item}, startParam, postOnRez, stateSource, UUID.Zero, false);
                if (SendUpdate)
                    m_part.ScheduleUpdate(PrimUpdateFlags.PrimFlags); //We only need to send a compressed
            }
            HasInventoryChanged = true;
            ResumeScript(item);
        }

        /// <summary>
        ///     Updates a script which is in this prim's inventory.
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="assetData"></param>
        /// <param name="startParam"></param>
        /// <param name="postOnRez"></param>
        /// <param name="stateSource"></param>
        /// <returns></returns>
        public void UpdateScriptInstance(UUID itemID, byte[] assetData, int startParam, bool postOnRez,
                                         StateSource stateSource)
        {
            TaskInventoryItem item = m_items[itemID];
            if (!m_part.ParentGroup.Scene.Permissions.CanRunScript(item.ItemID, m_part.UUID, item.OwnerID))
                return;

            m_part.AddFlag(PrimFlags.Scripted);

            if (!m_part.ParentGroup.Scene.RegionInfo.RegionSettings.DisableScripts)
            {
                lock (m_itemsLock)
                {
                    m_items[item.ItemID].PermsMask = 0;
                    m_items[item.ItemID].PermsGranter = UUID.Zero;
                }

                string script = Utils.BytesToString(assetData);
                IScriptModule[] modules = m_part.ParentGroup.Scene.RequestModuleInterfaces<IScriptModule>();
                foreach (IScriptModule module in modules)
                {
                    module.UpdateScript(m_part.UUID, item.ItemID, script, startParam, postOnRez, stateSource);
                }
                ResumeScript(item);
            }
            HasInventoryChanged = true;
        }

        /// <summary>
        ///     Start a script which is in this prim's inventory.
        /// </summary>
        /// <param name="itemId">
        ///     A <see cref="UUID" />
        /// </param>
        /// <param name="startParam"></param>
        /// <param name="postOnRez"></param>
        /// <param name="stateSource"></param>
        public void CreateScriptInstance(UUID itemId, int startParam, bool postOnRez, StateSource stateSource)
        {
            TaskInventoryItem item = GetInventoryItem(itemId);
            if (item != null)
                CreateScriptInstance(item, startParam, postOnRez, stateSource);
            else
                MainConsole.Instance.ErrorFormat(
                    "[PRIM INVENTORY]: " +
                    "Couldn't start script with ID {0} since it couldn't be found for prim {1}, {2} at {3} in {4}",
                    itemId, m_part.Name, m_part.UUID,
                    m_part.AbsolutePosition, m_part.ParentGroup.Scene.RegionInfo.RegionName);
        }

        /// <summary>
        ///     Stop a script which is in this prim's inventory.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="sceneObjectBeingDeleted">
        ///     Should be true if this script is being removed because the scene
        ///     object is being deleted.  This will prevent spurious updates to the client.
        /// </param>
        public void RemoveScriptInstance(UUID itemId, bool sceneObjectBeingDeleted)
        {
            bool scriptPresent = false;

            lock (m_itemsLock)
            {
                if (m_items.ContainsKey(itemId))
                    scriptPresent = true;
            }

            if (scriptPresent)
            {
                if (!sceneObjectBeingDeleted)
                    m_part.RemoveScriptEvents(itemId);

                m_part.ParentGroup.Scene.EventManager.TriggerRemoveScript(m_part.LocalId, itemId);
            }
            else
            {
                MainConsole.Instance.ErrorFormat(
                    "[PRIM INVENTORY]: " +
                    "Couldn't stop script with ID {0} since it couldn't be found for prim {1}, {2} at {3} in {4}",
                    itemId, m_part.Name, m_part.UUID,
                    m_part.AbsolutePosition, m_part.ParentGroup.Scene.RegionInfo.RegionName);
            }
        }

        /// <summary>
        ///     Check if the inventory holds an item with a given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool InventoryContainsName(string name)
        {
            lock (m_itemsLock)
            {
                if (m_items.Values.Any(item => item.Name == name))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     For a given item name, return that name if it is available.  Otherwise, return the next available
        ///     similar name (which is currently the original name with the next available numeric suffix).
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string FindAvailableInventoryName(string name)
        {
            if (!InventoryContainsName(name))
                return name;

            int suffix = 1;
            while (suffix < 256)
            {
                string tryName = String.Format("{0} {1}", name, suffix);
                if (!InventoryContainsName(tryName))
                    return tryName;
                suffix++;
            }
            return String.Empty;
        }

        /// <summary>
        ///     Add an item to this prim's inventory.  If an item with the same name already exists, then an alternative
        ///     name is chosen.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="allowedDrop"></param>
        public void AddInventoryItem(TaskInventoryItem item, bool allowedDrop)
        {
            AddInventoryItem(item.Name, item, allowedDrop);
        }

        /// <summary>
        ///     Add an item to this prim's inventory.  If an item with the same name already exists, it is replaced.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="allowedDrop"></param>
        public void AddInventoryItemExclusive(TaskInventoryItem item, bool allowedDrop)
        {
            List<TaskInventoryItem> il = GetInventoryItems();

            foreach (TaskInventoryItem i in il)
            {
                if (i.Name == item.Name)
                {
                    if (i.InvType == (int) InventoryType.LSL)
                        RemoveScriptInstance(i.ItemID, false);

                    RemoveInventoryItem(i.ItemID);
                    break;
                }
            }

            AddInventoryItem(item.Name, item, allowedDrop);
        }

        /// <summary>
        ///     Add an item to this prim's inventory.
        /// </summary>
        /// <param name="name">The name that the new item should have.</param>
        /// <param name="item">
        ///     The item itself.  The name within this structure is ignored in favour of the name
        ///     given in this method's arguments
        /// </param>
        /// <param name="allowedDrop">
        ///     Item was only added to inventory because AllowedDrop is set
        /// </param>
        protected void AddInventoryItem(string name, TaskInventoryItem item, bool allowedDrop)
        {
            name = FindAvailableInventoryName(name);
            if (name == String.Empty)
                return;

            item.ParentID = m_part.UUID;
            item.ParentPartID = m_part.UUID;
            item.Name = name;
            item.GroupID = m_part.GroupID;

            lock (m_itemsLock)
            {
                m_items.Add(item.ItemID, item);
            }

            m_part.TriggerScriptChangedEvent(allowedDrop ? Changed.ALLOWED_DROP : Changed.INVENTORY);

            m_inventorySerial++;
            //m_inventorySerial += 2;
            HasInventoryChanged = true;
        }

        /// <summary>
        ///     Restore a whole collection of items to the prim's inventory at once.
        ///     We assume that the items already have all their fields correctly filled out.
        ///     The items are not flagged for persistence to the database, since they are being restored
        ///     from persistence rather than being newly added.
        /// </summary>
        /// <param name="items"></param>
        public void RestoreInventoryItems(ICollection<TaskInventoryItem> items)
        {
            lock (m_itemsLock)
            {
                foreach (TaskInventoryItem item in items)
                {
                    m_items.Add(item.ItemID, item);
                    //                    m_part.TriggerScriptChangedEvent(Changed.INVENTORY);
                }
                m_inventorySerial++;
            }
        }

        /// <summary>
        ///     Returns an existing inventory item.  Returns the original, so any changes will be live.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns>null if the item does not exist</returns>
        public TaskInventoryItem GetInventoryItem(UUID itemId)
        {
            TaskInventoryItem item;

            lock (m_itemsLock)
            {
                m_items.TryGetValue(itemId, out item);
            }

            return item;
        }

        /// <summary>
        ///     Get inventory items by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>
        ///     A list of inventory items with that name.
        ///     If no inventory item has that name then an empty list is returned.
        /// </returns>
        public IList<TaskInventoryItem> GetInventoryItems(string name)
        {
            IList<TaskInventoryItem> items = new List<TaskInventoryItem>();

            lock (m_itemsLock)
            {
                foreach (TaskInventoryItem item in m_items.Values)
                {
                    if (item.Name == name)
                        items.Add(item);
                }
            }

            return items;
        }

        public ISceneEntity GetRezReadySceneObject(TaskInventoryItem item)
        {
            byte[] rezAsset = m_part.ParentGroup.Scene.AssetService.GetData(item.AssetID.ToString());

            if (null == rezAsset)
            {
                MainConsole.Instance.WarnFormat(
                    "[PRIM INVENTORY]: Could not find asset {0} for inventory item {1} in {2}",
                    item.AssetID, item.Name, m_part.Name);
                return null;
            }

            string xmlData = Utils.BytesToString(rezAsset);
            ISceneEntity group = SceneEntitySerializer.SceneObjectSerializer.FromOriginalXmlFormat(xmlData,
                                                                                                   m_part.ParentGroup
                                                                                                         .Scene);
            if (group == null)
                return null;

            group.IsDeleted = false;
            foreach (ISceneChildEntity part in group.ChildrenEntities())
            {
                part.IsLoading = false;
            }
            //Reset IDs, etc
            m_part.ParentGroup.Scene.SceneGraph.PrepPrimForAdditionToScene(group);

            ISceneChildEntity rootPart = group.GetChildPart(group.UUID);

            // Since renaming the item in the inventory does not affect the name stored
            // in the serialization, transfer the correct name from the inventory to the
            // object itself before we rez.
            rootPart.Name = item.Name;
            rootPart.Description = item.Description;

            List<ISceneChildEntity> partList = group.ChildrenEntities();

            group.SetGroup(m_part.GroupID, group.OwnerID, false);

            if ((rootPart.OwnerID != item.OwnerID) || (item.CurrentPermissions & 16) != 0)
            {
                if (m_part.ParentGroup.Scene.Permissions.PropagatePermissions())
                {
                    foreach (ISceneChildEntity part in partList)
                    {
                        part.EveryoneMask = item.EveryonePermissions;
                        part.NextOwnerMask = item.NextPermissions;
                    }

                    group.ApplyNextOwnerPermissions();
                }
            }

            foreach (ISceneChildEntity part in partList)
            {
                if ((part.OwnerID != item.OwnerID) || (item.CurrentPermissions & 16) != 0)
                {
                    part.LastOwnerID = part.OwnerID;
                    part.OwnerID = item.OwnerID;
                    part.Inventory.ChangeInventoryOwner(item.OwnerID);
                }

                part.EveryoneMask = item.EveryonePermissions;
                part.NextOwnerMask = item.NextPermissions;
            }

            rootPart.TrimPermissions();

            return group;
        }

        /// <summary>
        ///     Update an existing inventory item.
        /// </summary>
        /// <param name="item">
        ///     The updated item.  An item with the same id must already exist
        ///     in this prim's inventory.
        /// </param>
        /// <returns>false if the item did not exist, true if the update occurred successfully</returns>
        public bool UpdateInventoryItem(TaskInventoryItem item)
        {
            return UpdateInventoryItem(item, true);
        }

        public bool UpdateInventoryItem(TaskInventoryItem item, bool fireScriptEvents)
        {
            TaskInventoryItem it = GetInventoryItem(item.ItemID);
            if (it != null)
            {
                item.ParentID = m_part.UUID;
                item.ParentPartID = m_part.UUID;
                item.Flags = m_items[item.ItemID].Flags;

                // If group permissions have been set on, check that the groupID is up to date in case it has
                // changed since permissions were last set.
                if (item.GroupPermissions != (uint) PermissionMask.None)
                    item.GroupID = m_part.GroupID;

                if (item.AssetID == UUID.Zero)
                    item.AssetID = it.AssetID;

                lock (m_itemsLock)
                {
                    m_items[item.ItemID] = item;
                    m_inventorySerial++;
                }

                if (fireScriptEvents)
                    m_part.TriggerScriptChangedEvent(Changed.INVENTORY);

                HasInventoryChanged = true;
                return true;
            }
            MainConsole.Instance.ErrorFormat(
                "[PRIM INVENTORY]: " +
                "Tried to retrieve item ID {0} from prim {1}, {2} at {3} in {4} but the item does not exist in this inventory",
                item.ItemID, m_part.Name, m_part.UUID,
                m_part.AbsolutePosition, m_part.ParentGroup.Scene.RegionInfo.RegionName);
            return false;
        }

        /// <summary>
        ///     Remove an item from this prim's inventory
        /// </summary>
        /// <param name="itemID"></param>
        /// <returns>
        ///     Numeric asset type of the item removed.  Returns -1 if the item did not exist
        ///     in this prim's inventory.
        /// </returns>
        public int RemoveInventoryItem(UUID itemID)
        {
            TaskInventoryItem item = GetInventoryItem(itemID);
            if (item != null)
            {
                int type = m_items[itemID].InvType;
                if (type == 10) // Script
                {
                    m_part.RemoveScriptEvents(itemID);
                    m_part.ParentGroup.Scene.EventManager.TriggerRemoveScript(m_part.LocalId, itemID);
                }
                m_items.Remove(itemID);
                m_inventorySerial++;
                m_part.TriggerScriptChangedEvent(Changed.INVENTORY);

                HasInventoryChanged = true;

                if (!ContainsScripts())
                {
                    if (m_part.RemFlag(PrimFlags.Scripted))
                        m_part.ScheduleUpdate(PrimUpdateFlags.PrimFlags);
                }
                return type;
            }

            return -1;
        }

        /// <summary>
        ///     Returns true if the file needs to be rebuild, false if it does not
        /// </summary>
        /// <returns></returns>
        public bool GetInventoryFileName()
        {
            if (m_inventoryFileName == String.Empty ||
                m_inventoryFileNameSerial < m_inventorySerial)
            {
                m_inventoryFileName = "inventory_" + UUID.Random().ToString() + ".tmp";
                m_inventoryFileNameSerial = m_inventorySerial;
                return true; //We had to change the filename, need to rebuild the file
            }
            return false;
        }

        /// <summary>
        ///     Serialize all the metadata for the items in this prim's inventory ready for sending to the client
        /// </summary>
        /// <param name="client"></param>
        public void RequestInventoryFile(IClientAPI client)
        {
            IXfer xferManager = client.Scene.RequestModuleInterface<IXfer>();
            if (m_inventorySerial == 0)
            {
                //No inventory, no sending
                client.SendTaskInventory(m_part.UUID, 0, new byte[0]);
                return;
            }
            //If update == true, we need to recreate the file for the client
            bool Update = GetInventoryFileName();

            if (!Update)
            {
                //We don't need to update the fileData, so just send the cached info and exit out of this method
                if (m_fileData.Length > 2)
                {
                    client.SendTaskInventory(m_part.UUID, (short) m_inventorySerial,
                                             Utils.StringToBytes(m_inventoryFileName));

                    xferManager.AddNewFile(m_inventoryFileName, m_fileData);
                }
                else
                    client.SendTaskInventory(m_part.UUID, 0, new byte[0]);
                return;
            }

            // Confusingly, the folder item has to be the object id, while the 'parent id' has to be zero.  This matches
            // what appears to happen in the Second Life protocol.  If this isn't the case. then various functionality
            // isn't available (such as drag from prim inventory to agent inventory)
            InventoryStringBuilder invString = new InventoryStringBuilder(m_part.UUID, UUID.Zero);

            bool includeAssets = false;
            if (m_part.ParentGroup.Scene.Permissions.CanEditObjectInventory(m_part.UUID, client.AgentId))
                includeAssets = true;

            List<TaskInventoryItem> items = m_items.Clone2List();
            foreach (TaskInventoryItem item in items)
            {
                UUID ownerID = item.OwnerID;
                const uint everyoneMask = 0;
                uint baseMask = item.BasePermissions;
                uint ownerMask = item.CurrentPermissions;
                uint groupMask = item.GroupPermissions;

                invString.AddItemStart();
                invString.AddNameValueLine("item_id", item.ItemID.ToString());
                invString.AddNameValueLine("parent_id", m_part.UUID.ToString());

                invString.AddPermissionsStart();

                invString.AddNameValueLine("base_mask", Utils.UIntToHexString(baseMask));
                invString.AddNameValueLine("owner_mask", Utils.UIntToHexString(ownerMask));
                invString.AddNameValueLine("group_mask", Utils.UIntToHexString(groupMask));
                invString.AddNameValueLine("everyone_mask", Utils.UIntToHexString(everyoneMask));
                invString.AddNameValueLine("next_owner_mask", Utils.UIntToHexString(item.NextPermissions));

                invString.AddNameValueLine("creator_id", item.CreatorID.ToString());
                invString.AddNameValueLine("owner_id", ownerID.ToString());

                invString.AddNameValueLine("last_owner_id", item.LastOwnerID.ToString());

                invString.AddNameValueLine("group_id", item.GroupID.ToString());
                invString.AddSectionEnd();

                invString.AddNameValueLine("asset_id", includeAssets ? item.AssetID.ToString() : UUID.Zero.ToString());
                invString.AddNameValueLine("type", TaskInventoryItemHelpers.Types[item.Type]);
                invString.AddNameValueLine("inv_type", TaskInventoryItemHelpers.InvTypes[item.InvType]);
                invString.AddNameValueLine("flags", Utils.UIntToHexString(item.Flags));

                invString.AddSaleStart();
                invString.AddNameValueLine("sale_type", TaskInventoryItemHelpers.SaleTypes[item.SaleType]);
                invString.AddNameValueLine("sale_price", item.SalePrice.ToString());
                invString.AddSectionEnd();

                invString.AddNameValueLine("name", item.Name + "|");
                invString.AddNameValueLine("desc", item.Description + "|");

                invString.AddNameValueLine("creation_date", item.CreationDate.ToString());
                invString.AddSectionEnd();
            }
            string str = invString.GetString();
            if (str.Length > 0)
                str = str.Substring(0, str.Length - 1);
            m_fileData = Utils.StringToBytes(str);

            //MainConsole.Instance.Debug(Utils.BytesToString(fileData));
            //MainConsole.Instance.Debug("[PRIM INVENTORY]: RequestInventoryFile fileData: " + Utils.BytesToString(fileData));

            if (m_fileData.Length > 2)
            {
                client.SendTaskInventory(m_part.UUID, (short) m_inventorySerial,
                                         Utils.StringToBytes(m_inventoryFileName));
                xferManager.AddNewFile(m_inventoryFileName, m_fileData);
            }
            else
                client.SendTaskInventory(m_part.UUID, 0, new byte[0]);
        }

        public void SaveScriptStateSaves()
        {
            IScriptModule[] engines = m_part.ParentGroup.Scene.RequestModuleInterfaces<IScriptModule>();
            if (engines != null)
            {
                List<TaskInventoryItem> items = GetInventoryItems();
                foreach (TaskInventoryItem item in items)
                {
                    if (item.Type == (int) InventoryType.LSL)
                    {
                        foreach (IScriptModule engine in engines)
                        {
                            if (engine != null)
                            {
                                //NOTE: We will need to save the prim if we do this
                                engine.SaveStateSave(item.ItemID, m_part.UUID);
                            }
                        }
                    }
                }
            }
        }

        public class InventoryStringBuilder
        {
            private StringBuilder BuildString = new StringBuilder();
            private bool _hasAddeditems = false;

            public string GetString()
            {
                if (_hasAddeditems)
                    return BuildString.ToString();
                return "";
            }

            public InventoryStringBuilder(UUID folderID, UUID parentID)
            {
                BuildString.Append("\tinv_object\t0\n\t{\n");
                AddNameValueLine("obj_id", folderID.ToString());
                AddNameValueLine("parent_id", parentID.ToString());
                AddNameValueLine("type", "category");
                AddNameValueLine("name", "Contents|");
                AddSectionEnd();
            }

            public void AddItemStart()
            {
                _hasAddeditems = true;
                BuildString.Append("\tinv_item\t0\n");
                AddSectionStart();
            }

            public void AddPermissionsStart()
            {
                BuildString.Append("\tpermissions 0\n");
                AddSectionStart();
            }

            public void AddSaleStart()
            {
                BuildString.Append("\tsale_info\t0\n");
                AddSectionStart();
            }

            protected void AddSectionStart()
            {
                BuildString.Append("\t{\n");
            }

            public void AddSectionEnd()
            {
                BuildString.Append("\t}\n");
            }

            public void AddLine(string addLine)
            {
                BuildString.Append(addLine);
            }

            public void AddNameValueLine(string name, string value)
            {
                BuildString.Append("\t\t");
                BuildString.Append(name + "\t");
                BuildString.Append(value + "\n");
            }

            public void Close()
            {
            }
        }

        public uint MaskEffectivePermissions()
        {
            uint mask = 0x7fffffff;

            lock (m_itemsLock)
            {
                foreach (TaskInventoryItem item in m_items.Values)
                {
                    if (item.InvType != (int) InventoryType.Object)
                    {
                        if ((item.CurrentPermissions & item.NextPermissions & (uint) PermissionMask.Copy) == 0)
                            mask &= ~((uint) PermissionMask.Copy >> 13);
                        if ((item.CurrentPermissions & item.NextPermissions & (uint) PermissionMask.Transfer) == 0)
                            mask &= ~((uint) PermissionMask.Transfer >> 13);
                        if ((item.CurrentPermissions & item.NextPermissions & (uint) PermissionMask.Modify) == 0)
                            mask &= ~((uint) PermissionMask.Modify >> 13);
                    }
                    else
                    {
                        if ((item.CurrentPermissions & ((uint) PermissionMask.Copy >> 13)) == 0)
                            mask &= ~((uint) PermissionMask.Copy >> 13);
                        if ((item.CurrentPermissions & ((uint) PermissionMask.Transfer >> 13)) == 0)
                            mask &= ~((uint) PermissionMask.Transfer >> 13);
                        if ((item.CurrentPermissions & ((uint) PermissionMask.Modify >> 13)) == 0)
                            mask &= ~((uint) PermissionMask.Modify >> 13);
                    }

                    if ((item.CurrentPermissions & (uint) PermissionMask.Copy) == 0)
                        mask &= ~(uint) PermissionMask.Copy;
                    if ((item.CurrentPermissions & (uint) PermissionMask.Transfer) == 0)
                        mask &= ~(uint) PermissionMask.Transfer;
                    if ((item.CurrentPermissions & (uint) PermissionMask.Modify) == 0)
                        mask &= ~(uint) PermissionMask.Modify;
                }
            }

            return mask;
        }

        public void ApplyNextOwnerPermissions()
        {
            lock (m_itemsLock)
            {
                foreach (TaskInventoryItem item in m_items.Values)
                {
                    if (item.InvType == (int) InventoryType.Object && (item.CurrentPermissions & 7) != 0)
                    {
                        if ((item.CurrentPermissions & ((uint) PermissionMask.Copy >> 13)) == 0)
                            item.CurrentPermissions &= ~(uint) PermissionMask.Copy;
                        if ((item.CurrentPermissions & ((uint) PermissionMask.Transfer >> 13)) == 0)
                            item.CurrentPermissions &= ~(uint) PermissionMask.Transfer;
                        if ((item.CurrentPermissions & ((uint) PermissionMask.Modify >> 13)) == 0)
                            item.CurrentPermissions &= ~(uint) PermissionMask.Modify;
                    }
                    item.CurrentPermissions &= item.NextPermissions;
                    item.BasePermissions &= item.NextPermissions;
                    item.EveryonePermissions &= item.NextPermissions;
                    item.OwnerChanged = true;
                    item.PermsMask = 0;
                    item.PermsGranter = UUID.Zero;
                }
            }
        }

        public void ApplyGodPermissions(uint perms)
        {
            lock (m_itemsLock)
            {
                foreach (TaskInventoryItem item in m_items.Values)
                {
                    item.CurrentPermissions = perms;
                    item.BasePermissions = perms;
                }
            }
        }

        public bool ContainsScripts()
        {
            lock (m_itemsLock)
            {
                if (m_items.Values.Any(item => item.InvType == (int) InventoryType.LSL))
                {
                    return true;
                }
            }

            return false;
        }

        public List<UUID> GetInventoryList()
        {
            List<UUID> ret = new List<UUID>();

            lock (m_itemsLock)
            {
                ret.AddRange(m_items.Values.Select(item => item.ItemID));
            }

            return ret;
        }

        public List<TaskInventoryItem> GetInventoryItems()
        {
            List<TaskInventoryItem> ret = new List<TaskInventoryItem>();

            lock (m_itemsLock)
            {
                ret.AddRange(m_items.Values);
            }

            return ret;
        }

        public void ResumeScripts()
        {
            List<TaskInventoryItem> scripts = GetInventoryScripts();

            foreach (TaskInventoryItem item in scripts)
            {
                ResumeScript(item);
            }
        }

        private void ResumeScript(TaskInventoryItem item)
        {
            IScriptModule[] engines = m_part.ParentGroup.Scene.RequestModuleInterfaces<IScriptModule>();
            if (engines == null)
                return;
            foreach (IScriptModule engine in engines)
            {
                if (engine != null)
                {
                    engine.ResumeScript(item.ItemID);
                    if (item.OwnerChanged)
                        engine.PostScriptEvent(item.ItemID, m_part.UUID, "changed", new Object[] {(int) Changed.OWNER});
                    item.OwnerChanged = false;
                }
            }
        }
    }
}