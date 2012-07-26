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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Capabilities;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using OpenSim.Services.Interfaces;
using Nini.Config;

namespace Aurora.Modules.Inventory
{
    public class LLClientInventory : INonSharedRegionModule, ILLClientInventory
    {
        #region Declares

        protected string m_DefaultLSLScript = "default\n{\n    state_entry()\n    {\n        llSay(0, \"Script running.\");\n    }\n    touch_start(integer number)\n    {\n        llSay(0,\"Touched.\");\n    }\n}\n";

        /// <summary>
        /// The default LSL script that will be added when a client creates
        /// a new script in inventory or in the task object inventory
        /// </summary>
        public string DefaultLSLScript
        {
            get { return m_DefaultLSLScript; }
            set { m_DefaultLSLScript = value; }
        }

        protected IScene m_scene;

        
        #endregion

        #region INonSharedRegionModule members

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion (IScene scene)
        {
            m_scene = scene;

            scene.RegisterModuleInterface<ILLClientInventory>(this);

            scene.EventManager.OnRegisterCaps += EventManagerOnRegisterCaps;
            scene.EventManager.OnNewClient += EventManager_OnNewClient;
            scene.EventManager.OnClosingClient += EventManager_OnClosingClient;
        }

        public void RegionLoaded (IScene scene)
        {
        }

        public void RemoveRegion (IScene scene)
        {
            scene.UnregisterModuleInterface<ILLClientInventory>(this);

            scene.EventManager.OnNewClient -= EventManager_OnNewClient;
            scene.EventManager.OnClosingClient -= EventManager_OnClosingClient;

            m_scene = null;
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "LLClientInventoryModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        #region Client events

        /// <summary>
        /// Hook up to the client inventory events
        /// </summary>
        /// <param name="client"></param>
        protected void EventManager_OnNewClient(IClientAPI client)
        {
            client.OnCreateNewInventoryItem += CreateNewInventoryItem;
            client.OnLinkInventoryItem += HandleLinkInventoryItem;
            client.OnCreateNewInventoryFolder += HandleCreateInventoryFolder;
            client.OnUpdateInventoryFolder += HandleUpdateInventoryFolder;
            client.OnMoveInventoryFolder += HandleMoveInventoryFolder; // 2; //!!
#if UDP_INVENTORY
            client.OnFetchInventoryDescendents += HandleFetchInventoryDescendents;
            client.OnFetchInventory += HandleFetchInventory;
#endif
            client.OnPurgeInventoryDescendents += HandlePurgeInventoryDescendents; // 2; //!!
            client.OnUpdateInventoryItem += UpdateInventoryItemAsset;
            client.OnChangeInventoryItemFlags += ChangeInventoryItemFlags;
            client.OnCopyInventoryItem += CopyInventoryItem;
            client.OnMoveInventoryItem += MoveInventoryItem;
            client.OnRemoveInventoryItem += RemoveInventoryItem;
            client.OnRemoveInventoryFolder += RemoveInventoryFolder;
            client.OnRezScript += RezScript;
            client.OnRequestTaskInventory += RequestTaskInventory;
            client.OnRemoveTaskItem += RemoveTaskInventory;
            client.OnUpdateTaskInventory += UpdateTaskInventory;
            client.OnMoveTaskItem += ClientMoveTaskInventoryItemToUserInventory;
            client.OnDeRezObject += DeRezObjects;
        }

        /// <summary>
        /// Remove ourselves from the inventory events
        /// </summary>
        /// <param name="client"></param>
        protected void EventManager_OnClosingClient(IClientAPI client)
        {
            client.OnCreateNewInventoryItem -= CreateNewInventoryItem;
            client.OnCreateNewInventoryFolder -= HandleCreateInventoryFolder;
            client.OnUpdateInventoryFolder -= HandleUpdateInventoryFolder;
            client.OnMoveInventoryFolder -= HandleMoveInventoryFolder;
#if UDP_INVENTORY
            client.OnFetchInventoryDescendents -= HandleFetchInventoryDescendents;
            client.OnFetchInventory -= HandleFetchInventory;
#endif
            client.OnPurgeInventoryDescendents -= HandlePurgeInventoryDescendents;
            client.OnUpdateInventoryItem -= UpdateInventoryItemAsset;
            client.OnCopyInventoryItem -= CopyInventoryItem;
            client.OnMoveInventoryItem -= MoveInventoryItem;
            client.OnRemoveInventoryItem -= RemoveInventoryItem;
            client.OnRemoveInventoryFolder -= RemoveInventoryFolder;
            client.OnRezScript -= RezScript;
            client.OnRequestTaskInventory -= RequestTaskInventory;
            client.OnRemoveTaskItem -= RemoveTaskInventory;
            client.OnUpdateTaskInventory -= UpdateTaskInventory;
            client.OnMoveTaskItem -= ClientMoveTaskInventoryItemToUserInventory;
            client.OnDeRezObject -= DeRezObjects;
        }

#if UDP_INVENTORY
        
        /// <summary>
        /// Handle a fetch inventory request from the client
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="itemID"></param>
        /// <param name="ownerID"></param>
        protected void HandleFetchInventory(IClientAPI remoteClient, UUID itemID, UUID ownerID)
        {
            //MainConsole.Instance.Warn("[Scene.PacketHandler]: Depriated UDP Inventory request!");
            InventoryItemBase item = new InventoryItemBase(itemID, remoteClient.AgentId);
            item = m_scene.InventoryService.GetItem(item);

            if (item != null)
            {
                remoteClient.SendInventoryItemDetails(ownerID, item);
            }
            // else shouldn't we send an alert message?
        }

        /// <summary>
        /// Tell the client about the various child items and folders contained in the requested folder.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="folderID"></param>
        /// <param name="ownerID"></param>
        /// <param name="fetchFolders"></param>
        /// <param name="fetchItems"></param>
        /// <param name="sortOrder"></param>
        protected void HandleFetchInventoryDescendents(IClientAPI remoteClient, UUID folderID, UUID ownerID,
                                                    bool fetchFolders, bool fetchItems, int sortOrder)
        {
            //MainConsole.Instance.Warn("[Scene.PacketHandler]: Depriated UDP FetchInventoryDescendents request!");
            if (folderID == UUID.Zero)
                return;

            // FIXME MAYBE: We're not handling sortOrder!
            // We're going to send the reply async, because there may be
            // an enormous quantity of packets -- basically the entire inventory!
            // We don't want to block the client thread while all that is happening.
            SendInventoryDelegate d = SendInventoryAsync;
            d.BeginInvoke(remoteClient, folderID, ownerID, fetchFolders, fetchItems, sortOrder, SendInventoryComplete, d);
        }

        delegate void SendInventoryDelegate(IClientAPI remoteClient, UUID folderID, UUID ownerID, bool fetchFolders, bool fetchItems, int sortOrder);

        protected void SendInventoryAsync(IClientAPI remoteClient, UUID folderID, UUID ownerID, bool fetchFolders, bool fetchItems, int sortOrder)
        {
            SendInventoryUpdate(remoteClient, new InventoryFolderBase(folderID), fetchFolders, fetchItems);
        }

        protected void SendInventoryComplete(IAsyncResult iar)
        {
            SendInventoryDelegate d = (SendInventoryDelegate)iar.AsyncState;
            d.EndInvoke(iar);
        }

#endif

        /// <summary>
        /// Handle an inventory folder creation request from the client.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="folderID"></param>
        /// <param name="folderType"></param>
        /// <param name="folderName"></param>
        /// <param name="parentID"></param>
        protected void HandleCreateInventoryFolder(IClientAPI remoteClient, UUID folderID, ushort folderType,
                                                string folderName, UUID parentID)
        {
            InventoryFolderBase folder = new InventoryFolderBase(folderID, folderName, remoteClient.AgentId, (short)folderType, parentID, 1);
            if (!m_scene.InventoryService.AddFolder(folder))
            {
                MainConsole.Instance.WarnFormat(
                     "[AGENT INVENTORY]: Failed to create folder for user {0} {1}",
                     remoteClient.Name, remoteClient.AgentId);
            }
        }

        /// <summary>
        /// Handle a client request to update the inventory folder
        /// </summary>
        ///
        /// FIXME: We call add new inventory folder because in the data layer, we happen to use an SQL REPLACE
        /// so this will work to rename an existing folder.  Needless to say, to rely on this is very confusing,
        /// and needs to be changed.
        ///
        /// <param name="remoteClient"></param>
        /// <param name="folderID"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="parentID"></param>
        protected void HandleUpdateInventoryFolder(IClientAPI remoteClient, UUID folderID, ushort type, string name,
                                                UUID parentID)
        {
            //            MainConsole.Instance.DebugFormat(
            //                "[AGENT INVENTORY]: Updating inventory folder {0} {1} for {2} {3}", folderID, name, remoteClient.Name, remoteClient.AgentId);

            InventoryFolderBase folder = new InventoryFolderBase(folderID, remoteClient.AgentId);
            folder = m_scene.InventoryService.GetFolder(folder);
            if (folder != null)
            {
                folder.Name = name;
                folder.Type = (short)type;
                folder.ParentID = parentID;
                if (!m_scene.InventoryService.UpdateFolder(folder))
                {
                    MainConsole.Instance.ErrorFormat(
                         "[AGENT INVENTORY]: Failed to update folder for user {0} {1}",
                         remoteClient.Name, remoteClient.AgentId);
                }
            }
        }

        /// <summary>
        /// Move the inventory folder to another place in the user's inventory
        /// </summary>
        /// <param name="remoteClient">The client that requested the change</param>
        /// <param name="folderID">The folder UUID to move</param>
        /// <param name="parentID">The folder to move the folder (folderID) into</param>
        protected void HandleMoveInventoryFolder(IClientAPI remoteClient, UUID folderID, UUID parentID)
        {
            InventoryFolderBase folder = new InventoryFolderBase(folderID, remoteClient.AgentId);
            folder = m_scene.InventoryService.GetFolder(folder);
            if (folder != null)
            {
                folder.ParentID = parentID;
                if (!m_scene.InventoryService.MoveFolder(folder))
                    MainConsole.Instance.WarnFormat("[AGENT INVENTORY]: could not move folder {0}", folderID);
                else
                    MainConsole.Instance.DebugFormat("[AGENT INVENTORY]: folder {0} moved to parent {1}", folderID, parentID);
            }
            else
            {
                MainConsole.Instance.WarnFormat("[AGENT INVENTORY]: request to move folder {0} but folder not found", folderID);
            }
        }

        /// <summary>
        /// This should delete all the items and folders in the given directory.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="folderID"></param>
        protected void HandlePurgeInventoryDescendents(IClientAPI remoteClient, UUID folderID)
        {
            InventoryFolderBase folder = new InventoryFolderBase(folderID, remoteClient.AgentId);
            Util.FireAndForget(PurgeFolderAsync, folder);
        }

        private void PurgeFolderAsync(object folder)
        {
            if (m_scene.InventoryService.PurgeFolder((InventoryFolderBase)folder))
                MainConsole.Instance.DebugFormat("[AGENT INVENTORY]: folder {0} purged successfully", ((InventoryFolderBase)folder).ID);
            else
                MainConsole.Instance.WarnFormat("[AGENT INVENTORY]: could not purge folder {0} for client {1}", ((InventoryFolderBase)folder).ID, ((InventoryFolderBase)folder).Owner);
        }

        /// <summary>
        /// Delete the given objects from the Scene and move them into the client's inventory
        /// </summary>
        /// <param name="remoteClient">The client requesting the change (can be null if returning objects)</param>
        /// <param name="localIDs">A list of all the localIDs of the groups to delete</param>
        /// <param name="groupID">the GroupID of the objects</param>
        /// <param name="action">What type of action is causing this</param>
        /// <param name="destinationID">The folder ID to put the inventory items in</param>
        protected void DeRezObjects(IClientAPI remoteClient, List<uint> localIDs,
                UUID groupID, DeRezAction action, UUID destinationID)
        {
            // First, see of we can perform the requested action and
            // build a list of eligible objects
            List<uint> deleteIDs = new List<uint>();
            List<ISceneEntity> deleteGroups = new List<ISceneEntity> ();

            #region Permission Check

            // Start with true for both, then remove the flags if objects
            // that we can't derez are part of the selection
            bool permissionToTake = true;
            bool permissionToTakeCopy = true;
            bool permissionToDelete = true;

            foreach (uint localID in localIDs)
            {
                // Invalid id
                ISceneChildEntity part = m_scene.GetSceneObjectPart (localID);
                if (part == null)
                    continue;

                // Already deleted by someone else
                if (part.ParentEntity == null || part.ParentEntity.IsDeleted)
                    continue;

                // Can't delete child prims
                if (part != part.ParentEntity.RootChild)
                    continue;

                ISceneEntity grp = part.ParentEntity;

                deleteIDs.Add(localID);
                deleteGroups.Add(grp);

                IScenePresence SP = remoteClient == null ? null : m_scene.GetScenePresence (remoteClient.AgentId);

                if (SP == null)
                {
                    // Autoreturn has a null client. Nothing else does. So
                    // allow only returns
                    if (action != DeRezAction.Return)
                        return;

                    permissionToTakeCopy = false;
                }
                else
                {
                    if (!m_scene.Permissions.CanTakeCopyObject(grp.UUID, SP.UUID))
                        permissionToTakeCopy = false;
                    if (!m_scene.Permissions.CanTakeObject(grp.UUID, SP.UUID))
                        permissionToTake = false;

                    if (!m_scene.Permissions.CanDeleteObject(grp.UUID, SP.UUID))
                        permissionToDelete = false;
                }
            }

            #endregion

            // Handle god perms
            if ((remoteClient != null) && m_scene.Permissions.IsGod(remoteClient.AgentId))
            {
                permissionToTake = true;
                permissionToTakeCopy = true;
                permissionToDelete = true;
            }

            // If we're re-saving, we don't even want to delete
            if (action == DeRezAction.SaveToExistingUserInventoryItem)
                permissionToDelete = false;

            // if we want to take a copy, we also don't want to delete
            // Note: after this point, the permissionToTakeCopy flag
            // becomes irrelevant. It already includes the permissionToTake
            // permission and after excluding no copy items here, we can
            // just use that.
            if (action == DeRezAction.AcquireToUserInventory)
            {
                // If we don't have permission, stop right here
                if (!permissionToTakeCopy)
                    return;

                permissionToTake = true;
                // Don't delete
                permissionToDelete = false;
            }

            if (action == DeRezAction.Return)
            {
                if (remoteClient != null && m_scene.Permissions.CanReturnObjects(
                                    null,
                                    remoteClient.AgentId,
                                    deleteGroups))
                {
                    permissionToTake = true;
                    permissionToDelete = true;

                    IParcelManagementModule parcelManagement = m_scene.RequestModuleInterface<IParcelManagementModule>();
                    if (parcelManagement != null)
                    {
                        parcelManagement.AddReturns(deleteGroups[0].OwnerID, deleteGroups[0].Name, deleteGroups[0].AbsolutePosition, "Parcel Owner Return", deleteGroups);
                    }
                }
                else // Auto return passes through here with null agent
                {
                    permissionToTake = true;
                    permissionToDelete = true;
                }
            }

            IAsyncSceneObjectGroupDeleter asyncDelete = m_scene.RequestModuleInterface<IAsyncSceneObjectGroupDeleter>();
            if (asyncDelete != null)
            {
                asyncDelete.DeleteToInventory(
                       action, destinationID, deleteGroups, remoteClient == null ? UUID.Zero : remoteClient.AgentId,
                           permissionToDelete, permissionToTake);
            }
        }

        /// <summary>
        /// Remove an inventory item for the client's inventory
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="itemIDs"></param>
        protected void RemoveInventoryItem(IClientAPI remoteClient, List<UUID> itemIDs)
        {
            //MainConsole.Instance.Debug("[SCENE INVENTORY]: user " + remoteClient.AgentId);
            m_scene.InventoryService.DeleteItems(remoteClient.AgentId, itemIDs);
        }

        /// <summary>
        /// Removes an inventory folder.  This packet is sent when the user
        /// right-clicks a folder that's already in trash and chooses "purge"
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="folderIDs"></param>
        protected void RemoveInventoryFolder(IClientAPI remoteClient, List<UUID> folderIDs)
        {
            MainConsole.Instance.DebugFormat("[SCENE INVENTORY]: RemoveInventoryFolders count {0}", folderIDs.Count);
            m_scene.InventoryService.DeleteFolders(remoteClient.AgentId, folderIDs);
        }

        /// <summary>
        /// Create a new Inventory Item
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="creatorData"></param>
        /// <param name="folderID"></param>
        /// <param name="flags"></param>
        /// <param name="callbackID"></param>
        /// <param name="asset"></param>
        /// <param name="invType"></param>
        /// <param name="everyoneMask"></param>
        /// <param name="nextOwnerMask"></param>
        /// <param name="groupMask"></param>
        /// <param name="creationDate"></param>
        /// <param name="creatorID"></param>
        /// <param name="name"></param>
        /// <param name="baseMask"></param>
        /// <param name="currentMask"></param>
        protected void CreateNewInventoryItem(
            IClientAPI remoteClient, string creatorID, string creatorData, UUID folderID, string name, uint flags, uint callbackID, AssetBase asset, sbyte invType,
            uint baseMask, uint currentMask, uint everyoneMask, uint nextOwnerMask, uint groupMask, int creationDate)
        {
            InventoryItemBase item = new InventoryItemBase
                                         {
                                             Owner = remoteClient.AgentId,
                                             CreatorId = creatorID,
                                             CreatorData = creatorData,
                                             ID = UUID.Random(),
                                             AssetID = asset.ID,
                                             Description = asset.Description,
                                             Name = name,
                                             Flags = flags,
                                             AssetType = asset.Type,
                                             InvType = invType,
                                             Folder = folderID,
                                             CurrentPermissions = currentMask,
                                             NextPermissions = nextOwnerMask,
                                             EveryOnePermissions = everyoneMask,
                                             GroupPermissions = groupMask,
                                             BasePermissions = baseMask,
                                             CreationDate = creationDate
                                         };
            m_scene.InventoryService.AddItemAsync(item, () =>
            {
                IAvatarFactory avFactory = m_scene.RequestModuleInterface<IAvatarFactory>();
                if (avFactory != null)
                    avFactory.NewAppearanceLink(item);
                remoteClient.SendInventoryItemCreateUpdate(item, callbackID);
            });
        }

        /// <summary>
        /// Create a new inventory item.  Called when the client creates a new item directly within their
        /// inventory (e.g. by selecting a context inventory menu option).
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="transactionID"></param>
        /// <param name="folderID"></param>
        /// <param name="callbackID"></param>
        /// <param name="description"></param>
        /// <param name="name"></param>
        /// <param name="invType"></param>
        /// <param name="assetType"></param>
        /// <param name="wearableType"></param>
        /// <param name="nextOwnerMask"></param>
        /// <param name="creationDate"></param>
        protected void CreateNewInventoryItem(IClientAPI remoteClient, UUID transactionID, UUID folderID,
                                           uint callbackID, string description, string name, sbyte invType,
                                           sbyte assetType,
                                           byte wearableType, uint nextOwnerMask, int creationDate)
        {
            //MainConsole.Instance.DebugFormat("[AGENT INVENTORY]: Received request to create inventory item {0} in folder {1}", name, folderID);

            if (!m_scene.Permissions.CanCreateUserInventory(invType, remoteClient.AgentId))
                return;

            if (transactionID == UUID.Zero)
            {
                IScenePresence presence;
                if (m_scene.TryGetScenePresence(remoteClient.AgentId, out presence))
                {
                    byte[] data = null;

                    if (invType == (sbyte)InventoryType.Landmark && presence != null)
                    {
                        if (m_scene.Permissions.CanTakeLandmark(remoteClient.AgentId))
                        {
                            data = BuildLandmark (presence, ref name);
                        }
                        else
                        {
                            remoteClient.SendAlertMessage("You cannot create a landmark here.");
                        }
                    }
                    if(invType == (sbyte)InventoryType.LSL)
                    {
                        data = Encoding.ASCII.GetBytes(DefaultLSLScript);
                    }
                    if(invType == (sbyte)InventoryType.CallingCard)
                    {
                        return;
                    }
                    if (invType == (sbyte)InventoryType.Notecard)
                    {
                        data = Encoding.ASCII.GetBytes(" ");
                    }
                    if (invType == (sbyte)InventoryType.Gesture)
                    {
                        data = /*Default empty gesture*/ new byte[13] { 50, 10, 50, 53, 53, 10, 48, 10, 10, 10, 48, 10, 0 };
                    }

                    AssetBase asset = new AssetBase(UUID.Random(), name, (AssetType)assetType,
                                                    remoteClient.AgentId) {Data = data, Description = description};
                    asset.ID = m_scene.AssetService.Store(asset);

                    CreateNewInventoryItem(
                        remoteClient, remoteClient.AgentId.ToString(), "", folderID, name, 0, callbackID, asset, invType,
                        (uint)PermissionMask.All, (uint)PermissionMask.All, 0, nextOwnerMask, 0, creationDate);
                }
                else
                {
                    MainConsole.Instance.ErrorFormat(
                        "ScenePresence for agent uuid {0} unexpectedly not found in CreateNewInventoryItem",
                        remoteClient.AgentId);
                }
            }
            else
            {
                IAgentAssetTransactions agentTransactions = m_scene.RequestModuleInterface<IAgentAssetTransactions>();
                if (agentTransactions != null)
                {
                    agentTransactions.HandleItemCreationFromTransaction(
                        remoteClient, transactionID, folderID, callbackID, description,
                        name, invType, assetType, wearableType, nextOwnerMask);
                }
            }
        }

        private byte[] BuildLandmark (IScenePresence presence, ref string name)
        {
            //See whether we have a gatekeeperURL
            IConfigurationService configService = m_scene.RequestModuleInterface<IConfigurationService> ();
            List<string> mainGridURLs = configService.FindValueOf ("MainGridURL");
            string gatekeeperURL = MainServer.Instance.ServerURI + "/";//Assume the default
            if (mainGridURLs.Count > 0)//Then check whether we were given one
                gatekeeperURL = mainGridURLs[0];
            //We have one!
            UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.AllScopeIDs, presence.UUID);
            if (account == null)
                name = "HG " + name;//We don't have an account for them, add the HG ref 
            name += " @ " + gatekeeperURL;
            string gatekeeperdata = string.Format ("gatekeeper {0}\n", gatekeeperURL);
            Vector3 pos = presence.AbsolutePosition;
            string strdata = String.Format (
                "Landmark version 2\nregion_id {0}\nlocal_pos {1} {2} {3}\nregion_handle {4}\n{5}",
                presence.Scene.RegionInfo.RegionID,
                pos.X, pos.Y, pos.Z,
                presence.Scene.RegionInfo.RegionHandle,
                gatekeeperdata);
            return Encoding.ASCII.GetBytes (strdata);
        }

        /// <summary>
        /// Create a new 'link' to another inventory item
        /// Used in Viewer 2 for appearance.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="transActionID"></param>
        /// <param name="folderID"></param>
        /// <param name="callbackID"></param>
        /// <param name="description"></param>
        /// <param name="name"></param>
        /// <param name="invType"></param>
        /// <param name="type"></param>
        /// <param name="olditemID"></param>
        protected void HandleLinkInventoryItem(IClientAPI remoteClient, UUID transActionID, UUID folderID,
                                             uint callbackID, string description, string name,
                                             sbyte invType, sbyte type, UUID olditemID)
        {
            //MainConsole.Instance.DebugFormat("[AGENT INVENTORY]: Received request to create inventory item link {0} in folder {1} pointing to {2}", name, folderID, olditemID);

            if (!m_scene.Permissions.CanCreateUserInventory(invType, remoteClient.AgentId))
                return;

            IScenePresence presence;
            if (m_scene.TryGetScenePresence(remoteClient.AgentId, out presence))
            {
                if (olditemID == AvatarWearable.DEFAULT_EYES_ITEM ||
                            olditemID == AvatarWearable.DEFAULT_BODY_ITEM ||
                            olditemID == AvatarWearable.DEFAULT_HAIR_ITEM ||
                            olditemID == AvatarWearable.DEFAULT_PANTS_ITEM ||
                            olditemID == AvatarWearable.DEFAULT_SHIRT_ITEM ||
                            olditemID == AvatarWearable.DEFAULT_SKIN_ITEM)
                {
                    return;
                }
                AssetBase asset = new AssetBase {ID = olditemID, Type = type, Name = name, Description = description};

                CreateNewInventoryItem(
                    remoteClient, remoteClient.AgentId.ToString(), "", folderID, name, 0, callbackID, asset, invType,
                    (uint)PermissionMask.All, (uint)PermissionMask.All, (uint)PermissionMask.All,
                    (uint)PermissionMask.All, (uint)PermissionMask.All, Util.UnixTimeSinceEpoch());
            }
            else
            {
                MainConsole.Instance.ErrorFormat(
                    "ScenePresence for agent uuid {0} unexpectedly not found in HandleLinkInventoryItem",
                    remoteClient.AgentId);
            }
        }

        /// <summary>
        /// Move an item within the agent's inventory.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="items"></param>
        protected void MoveInventoryItem(IClientAPI remoteClient, List<InventoryItemBase> items)
        {
            //MainConsole.Instance.DebugFormat(
            //    "[AGENT INVENTORY]: Moving {0} items for user {1}", items.Count, remoteClient.AgentId);

            m_scene.InventoryService.MoveItemsAsync(remoteClient.AgentId, items, null);
        }
        
        /// <summary>
        /// Copy an inventory item in the user's inventory
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="callbackID"></param>
        /// <param name="oldAgentID"></param>
        /// <param name="oldItemID"></param>
        /// <param name="newFolderID"></param>
        /// <param name="newName"></param>
        protected void CopyInventoryItem(IClientAPI remoteClient, uint callbackID, UUID oldAgentID, UUID oldItemID,
                                      UUID newFolderID, string newName)
        {
            MainConsole.Instance.DebugFormat(
                "[AGENT INVENTORY]: CopyInventoryItem received by {0} with oldAgentID {1}, oldItemID {2}, new FolderID {3}, newName {4}",
                remoteClient.AgentId, oldAgentID, oldItemID, newFolderID, newName);

            InventoryItemBase item = new InventoryItemBase(oldItemID, remoteClient.AgentId);
            item = m_scene.InventoryService.GetItem(item);
            if (item == null)
            {
                MainConsole.Instance.Error("[AGENT INVENTORY]: Failed to find item " + oldItemID.ToString());
                return;
            }

            if ((item.CurrentPermissions & (uint)PermissionMask.Copy) == 0)
                return;

            m_scene.AssetService.Get(item.AssetID.ToString(), null, (id, sender, asset) =>
                {
                    if (asset != null)
                    {
                        if (newName != String.Empty)
                        {
                            asset.Name = newName;
                        }
                        else
                        {
                            newName = item.Name;
                        }

                        if (remoteClient.AgentId == oldAgentID)
                        {
                            CreateNewInventoryItem(
                                remoteClient, item.CreatorId, item.CreatorData, newFolderID, newName, item.Flags, callbackID, asset, (sbyte)item.InvType,
                                item.BasePermissions, item.CurrentPermissions, item.EveryOnePermissions, item.NextPermissions, item.GroupPermissions, Util.UnixTimeSinceEpoch());
                        }
                        else
                        {
                            // If item is transfer or permissions are off or calling agent is allowed to copy item owner's inventory item.
                            if (((item.CurrentPermissions & (uint)PermissionMask.Transfer) != 0) && (m_scene.Permissions.BypassPermissions() || m_scene.Permissions.CanCopyUserInventory(remoteClient.AgentId, oldItemID)))
                            {
                                CreateNewInventoryItem(
                                    remoteClient, item.CreatorId, item.CreatorData, newFolderID, newName, item.Flags, callbackID, asset, (sbyte)item.InvType,
                                    item.NextPermissions, item.NextPermissions, item.EveryOnePermissions & item.NextPermissions, item.NextPermissions, item.GroupPermissions, Util.UnixTimeSinceEpoch());
                            }
                        }
                    }
                    else
                    {
                        MainConsole.Instance.ErrorFormat(
                            "[AGENT INVENTORY]: Could not copy item {0} since asset {1} could not be found",
                            item.Name, item.AssetID);
                    }
                });
        }

        /// <summary>
        /// Update an item which is either already in the client's inventory or is within
        /// a transaction
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="transactionID">The transaction ID.  If this is UUID.Zero we will
        /// assume that we are not in a transaction</param>
        /// <param name="itemID">The ID of the updated item</param>
        /// <param name="itemUpd"></param>
        protected void UpdateInventoryItemAsset(IClientAPI remoteClient, UUID transactionID,
                                             UUID itemID, InventoryItemBase itemUpd)
        {
            // This one will let people set next perms on items in agent
            // inventory. Rut-Roh. Whatever. Make this secure. Yeah.
            //
            // Passing something to another avatar or a an object will already
            InventoryItemBase item = new InventoryItemBase(itemID, remoteClient.AgentId);
            item = m_scene.InventoryService.GetItem(item);

            if (item != null)
            {
                if (UUID.Zero == transactionID)
                {
                    uint oldnextperms = item.NextPermissions;
                    bool hasPermissionsChanged = item.NextPermissions != (itemUpd.NextPermissions & item.BasePermissions);
                    item.Name = itemUpd.Name;
                    item.Description = itemUpd.Description;
                    item.NextPermissions = itemUpd.NextPermissions & item.BasePermissions;
                    item.EveryOnePermissions = itemUpd.EveryOnePermissions & item.BasePermissions;
                    item.GroupPermissions = itemUpd.GroupPermissions & item.BasePermissions;
                    item.GroupID = itemUpd.GroupID;
                    item.GroupOwned = itemUpd.GroupOwned;
                    item.CreationDate = itemUpd.CreationDate;
                    // The client sends zero if its newly created?

                    item.CreationDate = itemUpd.CreationDate == 0 ? Util.UnixTimeSinceEpoch() : itemUpd.CreationDate;

                    // TODO: Check if folder changed and move item
                    //item.NextPermissions = itemUpd.Folder;
                    item.InvType = itemUpd.InvType;
                    item.SalePrice = itemUpd.SalePrice;
                    item.SaleType = itemUpd.SaleType;
                    item.Flags = itemUpd.Flags;

                    if ((hasPermissionsChanged) && (item.AssetType == (int)InventoryType.Object))
                    {
						AssetBase asset = m_scene.AssetService.Get(item.AssetID.ToString());
						if (asset != null)
						{
							SceneObjectGroup group =
								SceneObjectSerializer.FromOriginalXmlFormat(Utils.BytesToString(asset.Data), m_scene);

							bool didchange = false;
							//copy
							if ((((PermissionMask)oldnextperms & PermissionMask.Copy) == PermissionMask.Copy) &&
								(((PermissionMask)item.NextPermissions & PermissionMask.Copy) != PermissionMask.Copy))
							{
								didchange = true;
								group.UpdatePermissions(remoteClient.AgentId, 16, 1, (uint)PermissionMask.Copy, 0);
							}
							else if ((((PermissionMask)oldnextperms & PermissionMask.Copy) != PermissionMask.Copy) &&
								(((PermissionMask)item.NextPermissions & PermissionMask.Copy) == PermissionMask.Copy))
							{
								didchange = true;
								group.UpdatePermissions(remoteClient.AgentId, 16, 1, (uint)PermissionMask.Copy, 1);
							}

							//mod
							if ((((PermissionMask)oldnextperms & PermissionMask.Modify) == PermissionMask.Modify) &&
								(((PermissionMask)item.NextPermissions & PermissionMask.Modify) != PermissionMask.Modify))
							{
								didchange = true;
								group.UpdatePermissions(remoteClient.AgentId, 16, 1, (uint)PermissionMask.Modify, 0);
							}
							else if ((((PermissionMask)oldnextperms & PermissionMask.Modify) != PermissionMask.Modify) &&
								(((PermissionMask)item.NextPermissions & PermissionMask.Modify) == PermissionMask.Modify))
							{
								didchange = true;
								group.UpdatePermissions(remoteClient.AgentId, 16, 1, (uint)PermissionMask.Modify, 1);
							}

							//trans
							if ((((PermissionMask)oldnextperms & PermissionMask.Transfer) == PermissionMask.Transfer) &&
								(((PermissionMask)item.NextPermissions & PermissionMask.Transfer) != PermissionMask.Transfer))
							{
								didchange = true;
								group.UpdatePermissions(remoteClient.AgentId, 16, 1, (uint)PermissionMask.Transfer, 0);
							}
							else if ((((PermissionMask)oldnextperms & PermissionMask.Transfer) != PermissionMask.Transfer) &&
								(((PermissionMask)item.NextPermissions & PermissionMask.Transfer) == PermissionMask.Transfer))
							{
								didchange = true;
								group.UpdatePermissions(remoteClient.AgentId, 16, 1, (uint)PermissionMask.Transfer, 1);
							}

							if (didchange)
							{
								asset.Data = Encoding.ASCII.GetBytes(group.ToXml2());
								asset.ID = m_scene.AssetService.Store(asset);
								item.AssetID = asset.ID;
							}
						}
                    }
                    m_scene.InventoryService.UpdateItem(item);
                }
                else
                {
                    IAgentAssetTransactions agentTransactions = m_scene.RequestModuleInterface<IAgentAssetTransactions>();
                    if (agentTransactions != null)
                    {
                        agentTransactions.HandleItemUpdateFromTransaction(
                                     remoteClient, transactionID, item);
                    }
                }
            }
            else
            {
                MainConsole.Instance.Error(
                    "[AGENTINVENTORY]: Item ID " + itemID + " not found for an inventory item update.");
            }
        }

        /// <summary>
        /// Change an inventory items flags
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="itemID"></param>
        /// <param name="Flags"></param>
        protected void ChangeInventoryItemFlags(IClientAPI remoteClient, UUID itemID, uint Flags)
        {
            InventoryItemBase item = new InventoryItemBase(itemID, remoteClient.AgentId);
            item = m_scene.InventoryService.GetItem(item);

            if (item != null)
            {
                item.Flags = Flags;

                m_scene.InventoryService.UpdateItem(item);
                remoteClient.SendInventoryItemDetails(item.Owner, item);
            }
            else
            {
                MainConsole.Instance.Error(
                    "[AGENTINVENTORY]: Item ID " + itemID + " not found for an inventory item update.");
            }
        }

        /// <summary>
        /// Send the details of a prim's inventory to the client.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="primLocalID"></param>
        protected void RequestTaskInventory(IClientAPI remoteClient, uint primLocalID)
        {
            ISceneChildEntity part = m_scene.GetSceneObjectPart (primLocalID);
            if (part != null)
            {
                part.Inventory.RequestInventoryFile (remoteClient);
            }
            else
            {
                MainConsole.Instance.ErrorFormat (
                    "[PRIM INVENTORY]: " +
                    "Couldn't find part {0} to request inventory data",
                    primLocalID);
            }
        }

        /// <summary>
        /// Remove an item from a prim (task) inventory
        /// </summary>
        /// <param name="remoteClient">Unused at the moment but retained since the avatar ID might
        /// be necessary for a permissions check at some stage.</param>
        /// <param name="itemID"></param>
        /// <param name="localID"></param>
        protected void RemoveTaskInventory(IClientAPI remoteClient, UUID itemID, uint localID)
        {
            ISceneChildEntity part = m_scene.GetSceneObjectPart (localID);
            if (m_scene.Permissions.CanDeleteObjectInventory(itemID, part.UUID, remoteClient.AgentId))
            {
                ISceneEntity group = part.ParentEntity;
                if (group != null)
                {
                    TaskInventoryItem item = group.GetInventoryItem(localID, itemID);
                    if (item == null)
                        return;

                    group.RemoveInventoryItem(localID, itemID);
                    part.GetProperties(remoteClient);
                }
                else
                {
                    MainConsole.Instance.ErrorFormat(
                        "[PRIM INVENTORY]: " +
                        "Removal of item {0} requested of prim {1} but this prim does not exist",
                        itemID,
                        localID);
                }
            }
        }

        #region Move

        /// <summary>
        /// Move the inventory folder to another place in the user's inventory
        /// </summary>
        /// <param name="remoteClient">The client that requested the change</param>
        /// <param name="folderId">The folderID that the task (object) item will be moved into</param>
        /// <param name="primLocalId">The localID of the prim the item is in</param>
        /// <param name="itemId">The UUID of the item to move</param>
        protected void ClientMoveTaskInventoryItemToUserInventory(IClientAPI remoteClient, UUID folderId, uint primLocalId, UUID itemId)
        {
            ISceneChildEntity part = m_scene.GetSceneObjectPart (primLocalId);

            if (null == part)
            {
                MainConsole.Instance.WarnFormat(
                    "[PRIM INVENTORY]: " +
                    "Move of inventory item {0} from prim with local id {1} failed because the prim could not be found",
                    itemId, primLocalId);

                return;
            }

            TaskInventoryItem taskItem = part.Inventory.GetInventoryItem(itemId);

            if (null == taskItem)
            {
                MainConsole.Instance.WarnFormat("[PRIM INVENTORY]: Move of inventory item {0} from prim with local id {1} failed"
                    + " because the inventory item could not be found",
                    itemId, primLocalId);

                return;
            }

            if ((taskItem.CurrentPermissions & (uint)PermissionMask.Copy) == 0)
            {
                // If the item to be moved is no copy, we need to be able to
                // edit the prim.
                if (!m_scene.Permissions.CanEditObjectInventory(part.UUID, remoteClient.AgentId))
                    return;
            }
            else
            {
                // If the item is copiable, then we just need to have perms
                // on it. The delete check is a pure rights check
                if (!m_scene.Permissions.CanDeleteObject(part.UUID, remoteClient.AgentId))
                    return;
            }

            MoveTaskInventoryItemToUserInventory(remoteClient, folderId, part, itemId, true);
        }

        /// <summary>
        /// Move the given item in the given prim to a folder in the client's inventory
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="folderId"></param>
        /// <param name="part"></param>
        /// <param name="itemId"></param>
        /// <param name="checkPermissions"></param>
        protected InventoryItemBase MoveTaskInventoryItemToUserInventory (IClientAPI remoteClient, UUID folderId, ISceneChildEntity part, UUID itemId, bool checkPermissions)
        {
            InventoryItemBase agentItem = CreateAgentInventoryItemFromTask(remoteClient.AgentId, part, itemId);
            if (!checkPermissions || m_scene.Permissions.CanCopyObjectInventory(itemId, part.UUID, remoteClient.AgentId))
            {
                if (agentItem == null)
                    return null;

                agentItem.Folder = folderId;
                AddInventoryItem(remoteClient, agentItem);
                return agentItem;
            }
            return null;
        }

        #endregion

        /// <summary>
        /// Send an update to the client about the given folder
        /// </summary>
        /// <param name="client">The client to send the update to</param>
        /// <param name="folder">The folder that we need to send</param>
        /// <param name="fetchFolders">Should we fetch folders inside of this folder</param>
        /// <param name="fetchItems">Should we fetch items inside of this folder</param>
        protected void SendInventoryUpdate(IClientAPI client, InventoryFolderBase folder, bool fetchFolders, bool fetchItems)
        {
            if (folder == null)
                return;

            // Fetch the folder contents
            InventoryCollection contents = m_scene.InventoryService.GetFolderContent(client.AgentId, folder.ID);

            // Fetch the folder itself to get its current version
            InventoryFolderBase containingFolder = new InventoryFolderBase(folder.ID, client.AgentId);
            containingFolder = m_scene.InventoryService.GetFolder(containingFolder);

            //MainConsole.Instance.DebugFormat("[AGENT INVENTORY]: Sending inventory folder contents ({0} nodes) for \"{1}\" to {2} {3}",
            //    contents.Folders.Count + contents.Items.Count, containingFolder.Name, client.FirstName, client.LastName);

            if (containingFolder != null)
                client.SendInventoryFolderDetails(client.AgentId, folder.ID, contents.Items, contents.Folders, containingFolder.Version, fetchFolders, fetchItems);
        }

        /// <summary>
        /// Update an item in a prim (task) inventory.
        /// This method does not handle scripts, <see>RezScript(IClientAPI, UUID, unit)</see>
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="transactionID"></param>
        /// <param name="itemInfo"></param>
        /// <param name="primLocalID"></param>
        protected void UpdateTaskInventory(IClientAPI remoteClient, UUID transactionID, TaskInventoryItem itemInfo,
                                        uint primLocalID)
        {
            UUID itemID = itemInfo.ItemID;

            // Find the prim we're dealing with
            ISceneChildEntity part = m_scene.GetSceneObjectPart (primLocalID);

            if (part != null)
            {
                TaskInventoryItem currentItem = part.Inventory.GetInventoryItem(itemID);
                bool allowInventoryDrop = (part.GetEffectiveObjectFlags()
                                           & (uint)PrimFlags.AllowInventoryDrop) != 0;

                // Explicity allow anyone to add to the inventory if the
                // AllowInventoryDrop flag has been set. Don't however let
                // them update an item unless they pass the external checks
                //
                if (!m_scene.Permissions.CanEditObjectInventory(part.UUID, remoteClient.AgentId)
                    && (currentItem != null || !allowInventoryDrop))
                    return;

                if (currentItem == null)
                {
                    UUID copyID = UUID.Random();
                    if (itemID != UUID.Zero)
                    {
                        InventoryItemBase item = new InventoryItemBase(itemID, remoteClient.AgentId);
                        item = m_scene.InventoryService.GetItem(item);

                        // If we've found the item in the user's inventory or in the library
                        if (item != null)
                        {
                            part.ParentEntity.AddInventoryItem (remoteClient, primLocalID, item, copyID);
                            MainConsole.Instance.InfoFormat(
                                "[PRIM INVENTORY]: Update with item {0} requested of prim {1} for {2}",
                                item.Name, primLocalID, remoteClient.Name);
                            part.GetProperties(remoteClient);
                            if (!m_scene.Permissions.BypassPermissions())
                            {
                                if ((item.CurrentPermissions & (uint)PermissionMask.Copy) == 0)
                                {
                                    List<UUID> uuids = new List<UUID> {itemID};
                                    RemoveInventoryItem(remoteClient, uuids);
                                }
                            }
                        }
                        else
                        {
                            MainConsole.Instance.ErrorFormat(
                                "[PRIM INVENTORY]: Could not find inventory item {0} to update for {1}!",
                                itemID, remoteClient.Name);
                        }
                    }
                }
                else // Updating existing item with new perms etc
                {
                    IAgentAssetTransactions agentTransactions = m_scene.RequestModuleInterface<IAgentAssetTransactions>();
                    if (agentTransactions != null)
                    {
                        agentTransactions.HandleTaskItemUpdateFromTransaction(
                            remoteClient, part, transactionID, currentItem);

                        if ((InventoryType)itemInfo.InvType == InventoryType.Notecard)
                            remoteClient.SendAgentAlertMessage("Notecard saved", false);
                        else if ((InventoryType)itemInfo.InvType == InventoryType.LSL)
                            remoteClient.SendAgentAlertMessage("Script saved", false);
                        else
                            remoteClient.SendAgentAlertMessage("Item saved", false);
                    }

                    // Base ALWAYS has move
                    currentItem.BasePermissions |= (uint)PermissionMask.Move;

                    // Check if we're allowed to mess with permissions
                    if (!m_scene.Permissions.IsGod(remoteClient.AgentId)) // Not a god
                    {
                        if (remoteClient.AgentId != part.OwnerID) // Not owner
                        {
                            // Friends and group members can't change any perms
                            itemInfo.BasePermissions = currentItem.BasePermissions;
                            itemInfo.EveryonePermissions = currentItem.EveryonePermissions;
                            itemInfo.GroupPermissions = currentItem.GroupPermissions;
                            itemInfo.NextPermissions = currentItem.NextPermissions;
                            itemInfo.CurrentPermissions = currentItem.CurrentPermissions;
                        }
                        else
                        {
                            // Owner can't change base, and can change other
                            // only up to base
                            itemInfo.BasePermissions = currentItem.BasePermissions;
                            itemInfo.EveryonePermissions &= currentItem.BasePermissions;
                            itemInfo.GroupPermissions &= currentItem.BasePermissions;
                            itemInfo.CurrentPermissions &= currentItem.BasePermissions;
                            itemInfo.NextPermissions &= currentItem.BasePermissions;
                        }

                    }

                    // Next ALWAYS has move
                    itemInfo.NextPermissions |= (uint)PermissionMask.Move;

                    if (part.Inventory.UpdateInventoryItem(itemInfo))
                    {
                        part.GetProperties(remoteClient);
                    }
                }
            }
            else
            {
                MainConsole.Instance.WarnFormat(
                    "[PRIM INVENTORY]: " +
                    "Update with item {0} requested of prim {1} for {2} but this prim does not exist",
                    itemID, primLocalID, remoteClient.Name);
            }
        }

        /// <summary>
        /// Rez a script into a prim's inventory, either ex nihilo or from an existing avatar inventory
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="transactionID"></param>
        /// <param name="localID"></param>
        /// <param name="itemBase"></param>
        protected void RezScript(IClientAPI remoteClient, InventoryItemBase itemBase, UUID transactionID, uint localID)
        {
            UUID itemID = itemBase.ID;
            UUID copyID = UUID.Random();

            if (itemID != UUID.Zero)  // transferred from an avatar inventory to the prim's inventory
            {
                InventoryItemBase item = new InventoryItemBase(itemID, remoteClient.AgentId);
                item = m_scene.InventoryService.GetItem(item);

                if (item != null)
                {
                    ISceneChildEntity part = m_scene.GetSceneObjectPart (localID);
                    if (part != null)
                    {
                        if (!m_scene.Permissions.CanEditObjectInventory(part.UUID, remoteClient.AgentId))
                            return;

                        part.ParentEntity.AddInventoryItem (remoteClient, localID, item, copyID);
                        part.Inventory.CreateScriptInstance(copyID, 0, false, 0);

                        //                        MainConsole.Instance.InfoFormat("[PRIMINVENTORY]: " +
                        //                                         "Rezzed script {0} into prim local ID {1} for user {2}",
                        //                                         item.inventoryName, localID, remoteClient.Name);
                        part.GetProperties(remoteClient);
                    }
                    else
                    {
                        MainConsole.Instance.ErrorFormat(
                            "[PRIM INVENTORY]: " +
                            "Could not rez script {0} into prim local ID {1} for user {2}"
                            + " because the prim could not be found in the region!",
                            item.Name, localID, remoteClient.Name);
                    }
                }
                else
                {
                    MainConsole.Instance.ErrorFormat(
                        "[PRIM INVENTORY]: Could not find script inventory item {0} to rez for {1}!",
                        itemID, remoteClient.Name);
                }
            }
            else  // script has been rezzed directly into a prim's inventory
            {
                ISceneChildEntity part = m_scene.GetSceneObjectPart (itemBase.Folder);
                if (part == null)
                    return;

                if (!m_scene.Permissions.CanCreateObjectInventory(
                    itemBase.InvType, part.UUID, remoteClient.AgentId))
                    return;

                AssetBase asset = new AssetBase(UUID.Random(), itemBase.Name, (AssetType)itemBase.AssetType,
                                                remoteClient.AgentId)
                                      {
                                          Description = itemBase.Description,
                                          Data = Encoding.ASCII.GetBytes(DefaultLSLScript)
                                      };
                asset.ID = m_scene.AssetService.Store(asset);

                TaskInventoryItem taskItem = new TaskInventoryItem();

                taskItem.ResetIDs(itemBase.Folder);
                taskItem.ParentID = itemBase.Folder;
                taskItem.CreationDate = (uint)itemBase.CreationDate;
                taskItem.Name = itemBase.Name;
                taskItem.Description = itemBase.Description;
                taskItem.Type = itemBase.AssetType;
                taskItem.InvType = itemBase.InvType;
                taskItem.OwnerID = itemBase.Owner;
                taskItem.CreatorID = itemBase.CreatorIdAsUuid;
                taskItem.CreatorData = itemBase.CreatorData;
                taskItem.BasePermissions = itemBase.BasePermissions;
                taskItem.CurrentPermissions = itemBase.CurrentPermissions;
                taskItem.EveryonePermissions = itemBase.EveryOnePermissions;
                taskItem.GroupPermissions = itemBase.GroupPermissions;
                taskItem.NextPermissions = itemBase.NextPermissions;
                taskItem.GroupID = itemBase.GroupID;
                taskItem.GroupPermissions = 0;
                taskItem.Flags = itemBase.Flags;
                taskItem.PermsGranter = UUID.Zero;
                taskItem.PermsMask = 0;
                taskItem.AssetID = asset.ID;
                taskItem.SalePrice = itemBase.SalePrice;
                taskItem.SaleType = itemBase.SaleType;

                part.Inventory.AddInventoryItem(taskItem, false);
                part.GetProperties(remoteClient);

                part.Inventory.CreateScriptInstance(taskItem, 0, false, StateSource.NewRez);
            }
        }

        #endregion

        #region Private members

        /// <summary>
        /// Change a task inventory item to a user inventory item
        /// </summary>
        /// <param name="destAgent">The agent who will own the inventory item</param>
        /// <param name="part">The object that the item is in</param>
        /// <param name="itemId">The item to convert</param>
        /// <returns></returns>
        private InventoryItemBase CreateAgentInventoryItemFromTask (UUID destAgent, ISceneChildEntity part, UUID itemId)
        {
            TaskInventoryItem taskItem = part.Inventory.GetInventoryItem(itemId);

            if (null == taskItem)
            {
                MainConsole.Instance.ErrorFormat(
                    "[PRIM INVENTORY]: Tried to retrieve item ID {0} from prim {1}, {2} for creating an avatar"
                        + " inventory item from a prim's inventory item "
                        + " but the required item does not exist in the prim's inventory",
                    itemId, part.Name, part.UUID);

                return null;
            }

            if ((destAgent != taskItem.OwnerID) && ((taskItem.CurrentPermissions & (uint)PermissionMask.Transfer) == 0))
            {
                return null;
            }

            InventoryItemBase agentItem = new InventoryItemBase
                                              {
                                                  ID = UUID.Random(),
                                                  CreatorId = taskItem.CreatorID.ToString(),
                                                  CreatorData = taskItem.CreatorData,
                                                  Owner = destAgent,
                                                  AssetID = taskItem.AssetID,
                                                  Description = taskItem.Description,
                                                  Name = taskItem.Name,
                                                  AssetType = taskItem.Type,
                                                  InvType = taskItem.InvType,
                                                  Flags = taskItem.Flags,
                                                  SalePrice = taskItem.SalePrice,
                                                  SaleType = taskItem.SaleType
                                              };


            if ((part.OwnerID != destAgent) && m_scene.Permissions.PropagatePermissions())
            {
                agentItem.BasePermissions = taskItem.BasePermissions & (taskItem.NextPermissions | (uint)PermissionMask.Move);
                if (taskItem.InvType == (int)InventoryType.Object)
                    agentItem.CurrentPermissions = agentItem.BasePermissions & (((taskItem.CurrentPermissions & 7) << 13) | (taskItem.CurrentPermissions & (uint)PermissionMask.Move));
                else
                    agentItem.CurrentPermissions = agentItem.BasePermissions & taskItem.CurrentPermissions;
                
                agentItem.CurrentPermissions |= 16; // Slam
                agentItem.NextPermissions = taskItem.NextPermissions;
                agentItem.EveryOnePermissions = taskItem.EveryonePermissions & (taskItem.NextPermissions | (uint)PermissionMask.Move);
                agentItem.GroupPermissions = taskItem.GroupPermissions & taskItem.NextPermissions;
            }
            else
            {
                agentItem.BasePermissions = taskItem.BasePermissions;
                agentItem.CurrentPermissions = taskItem.CurrentPermissions;
                agentItem.NextPermissions = taskItem.NextPermissions;
                agentItem.EveryOnePermissions = taskItem.EveryonePermissions;
                agentItem.GroupPermissions = taskItem.GroupPermissions;
            }

            if (!m_scene.Permissions.BypassPermissions())
            {
                if ((taskItem.CurrentPermissions & (uint)PermissionMask.Copy) == 0)
                    part.Inventory.RemoveInventoryItem(itemId);
            }

            return agentItem;
        }

        #endregion

        #region ILLCLientInventory Members

        /// <summary>
        /// Add the given inventory item to a user's inventory.
        /// </summary>
        /// <param name="item">The item to add</param>
        public void AddInventoryItem(InventoryItemBase item)
        {
            m_scene.InventoryService.AddItemAsync(item, null);
        }

        /// <summary>
        /// Add an inventory item to an avatar's inventory.
        /// </summary>
        /// <param name="remoteClient">The remote client controlling the avatar</param>
        /// <param name="item">The item.  This structure contains all the item metadata, including the folder
        /// in which the item is to be placed.</param>
        public void AddInventoryItem(IClientAPI remoteClient, InventoryItemBase item)
        {
            m_scene.InventoryService.AddItemAsync(item, 
                () => remoteClient.SendInventoryItemCreateUpdate(item, 0));
        }

        /// <summary>
        /// Rez a script into a prim's inventory from another prim
        /// This is used for the LSL function llRemoteLoadScriptPin and requires a valid pin to be used
        /// </summary>
        /// <param name="srcId">The UUID of the script that is going to be copied</param>
        /// <param name="srcPart">The prim that the script that is going to be copied from</param>
        /// <param name="destId">The UUID of the prim that the </param>
        /// <param name="pin">The ScriptAccessPin of the prim</param>
        /// <param name="running">Whether the script should be running when it is started</param>
        /// <param name="start_param">The start param to pass to the script</param>
        public void RezScript (UUID srcId, ISceneChildEntity srcPart, UUID destId, int pin, int running, int start_param)
        {
            TaskInventoryItem srcTaskItem = srcPart.Inventory.GetInventoryItem(srcId);

            if (srcTaskItem == null)
            {
                MainConsole.Instance.ErrorFormat(
                    "[PRIM INVENTORY]: Tried to retrieve item ID {0} from prim {1}, {2} for rezzing a script but the "
                        + " item does not exist in this inventory",
                    srcId, srcPart.Name, srcPart.UUID);
                return;
            }

            ISceneChildEntity destPart = m_scene.GetSceneObjectPart (destId);

            if (destPart == null)
            {
                MainConsole.Instance.ErrorFormat(
                        "[PRIM INVENTORY]: " +
                        "Could not find script for ID {0}",
                        destId);
                return;
            }

            // Must own the object, and have modify rights
            if (srcPart.OwnerID != destPart.OwnerID)
            {
                // Group permissions
                if ((destPart.GroupID == UUID.Zero) || (destPart.GroupID != srcPart.GroupID) ||
                    ((destPart.GroupMask & (uint)PermissionMask.Modify) == 0))
                    return;
            }
            else
            {
                if ((destPart.OwnerMask & (uint)PermissionMask.Modify) == 0)
                    return;
            }

            if (destPart.ScriptAccessPin != pin)
            {
                MainConsole.Instance.WarnFormat(
                        "[PRIM INVENTORY]: " +
                        "Script in object {0} : {1}, attempted to load script {2} : {3} into object {4} : {5} with invalid pin {6}",
                        srcPart.Name, srcId, srcTaskItem.Name, srcTaskItem.ItemID, destPart.Name, destId, pin);
                // the LSL Wiki says we are supposed to shout on the DEBUG_CHANNEL -
                //   "Object: Task Object trying to illegally load script onto task Other_Object!"
                // How do we shout from in here?
                return;
            }

            TaskInventoryItem destTaskItem = new TaskInventoryItem
                                                 {
                                                     ItemID = UUID.Random(),
                                                     CreatorID = srcTaskItem.CreatorID,
                                                     CreatorData = srcTaskItem.CreatorData,
                                                     AssetID = srcTaskItem.AssetID,
                                                     GroupID = destPart.GroupID,
                                                     OwnerID = destPart.OwnerID,
                                                     ParentID = destPart.UUID,
                                                     ParentPartID = destPart.UUID,
                                                     BasePermissions = srcTaskItem.BasePermissions,
                                                     EveryonePermissions = srcTaskItem.EveryonePermissions,
                                                     GroupPermissions = srcTaskItem.GroupPermissions,
                                                     CurrentPermissions = srcTaskItem.CurrentPermissions,
                                                     NextPermissions = srcTaskItem.NextPermissions,
                                                     Flags = srcTaskItem.Flags,
                                                     SalePrice = srcTaskItem.SalePrice,
                                                     SaleType = srcTaskItem.SaleType
                                                 };



            if (destPart.OwnerID != srcPart.OwnerID)
            {
                if (m_scene.Permissions.PropagatePermissions())
                {
                    destTaskItem.CurrentPermissions = srcTaskItem.CurrentPermissions &
                            srcTaskItem.NextPermissions;
                    destTaskItem.GroupPermissions = srcTaskItem.GroupPermissions &
                            srcTaskItem.NextPermissions;
                    destTaskItem.EveryonePermissions = srcTaskItem.EveryonePermissions &
                            srcTaskItem.NextPermissions;
                    destTaskItem.BasePermissions = srcTaskItem.BasePermissions &
                            srcTaskItem.NextPermissions;
                    destTaskItem.CurrentPermissions |= 16; // Slam!
                }
            }

            destTaskItem.Description = srcTaskItem.Description;
            destTaskItem.Name = srcTaskItem.Name;
            destTaskItem.InvType = srcTaskItem.InvType;
            destTaskItem.Type = srcTaskItem.Type;

            destPart.Inventory.AddInventoryItemExclusive(destTaskItem, false);

            if (running > 0)
                destPart.Inventory.CreateScriptInstance(destTaskItem, start_param, false, StateSource.NewRez);

            IScenePresence avatar;
            if (m_scene.TryGetScenePresence(srcTaskItem.OwnerID, out avatar))
                destPart.GetProperties(avatar.ControllingClient);
        }

        /// <summary>
        /// Return the given objects to the agent given
        /// </summary>
        /// <param name="returnobjects">The objects to return</param>
        /// <param name="AgentId">The agent UUID that will get the inventory items for these objects</param>
        /// <returns></returns>
        public bool ReturnObjects(ISceneEntity[] returnobjects,
                UUID AgentId)
        {
            if (returnobjects.Length == 0)
                return true;
            //AddReturns(returnobjects[0].OwnerID, returnobjects[0].Name, returnobjects.Length, returnobjects[0].AbsolutePosition, "parcel owner return");
#if (!ISWIN)
            List<uint> IDs = new List<uint>();
            foreach (ISceneEntity grp in returnobjects)
                IDs.Add(grp.LocalId);
#else
            List<uint> IDs = returnobjects.Select(grp => grp.LocalId).ToList();
#endif
            IClientAPI client;
            m_scene.ClientManager.TryGetValue(AgentId, out client);
            //Its ok if the client is null, its taken care of
            DeRezObjects(client, IDs, returnobjects[0].RootChild.GroupID, DeRezAction.Return, UUID.Zero);
            return true;
        }

        /// <summary>
        /// Copy a task (prim) inventory item to another task (prim)
        /// </summary>
        /// <param name="destId"></param>
        /// <param name="part"></param>
        /// <param name="itemId"></param>
        public void MoveTaskInventoryItemToObject (UUID destId, ISceneChildEntity part, UUID itemId)
        {
            TaskInventoryItem srcTaskItem = part.Inventory.GetInventoryItem(itemId);

            if (srcTaskItem == null)
            {
                MainConsole.Instance.ErrorFormat(
                    "[PRIM INVENTORY]: Tried to retrieve item ID {0} from prim {1}, {2} for moving"
                        + " but the item does not exist in this inventory",
                    itemId, part.Name, part.UUID);

                return;
            }

            ISceneChildEntity destPart = m_scene.GetSceneObjectPart (destId);

            if (destPart == null)
            {
                MainConsole.Instance.ErrorFormat(
                        "[PRIM INVENTORY]: " +
                        "Could not find prim for ID {0}",
                        destId);
                return;
            }

            // Can't transfer this
            //
            if ((part.OwnerID != destPart.OwnerID) && ((srcTaskItem.CurrentPermissions & (uint)PermissionMask.Transfer) == 0))
                return;

            if (part.OwnerID != destPart.OwnerID && (destPart.GetEffectiveObjectFlags() & (uint)PrimFlags.AllowInventoryDrop) == 0)
            {
                // object cannot copy items to an object owned by a different owner
                // unless llAllowInventoryDrop has been called

                return;
            }

            // must have both move and modify permission to put an item in an object
            if ((part.OwnerMask & ((uint)PermissionMask.Move | (uint)PermissionMask.Modify)) == 0)
            {
                return;
            }

            TaskInventoryItem destTaskItem = new TaskInventoryItem
                                                 {
                                                     ItemID = UUID.Random(),
                                                     CreatorID = srcTaskItem.CreatorID,
                                                     CreatorData = srcTaskItem.CreatorData,
                                                     AssetID = srcTaskItem.AssetID,
                                                     GroupID = destPart.GroupID,
                                                     OwnerID = destPart.OwnerID,
                                                     ParentID = destPart.UUID,
                                                     ParentPartID = destPart.UUID,
                                                     BasePermissions = srcTaskItem.BasePermissions,
                                                     EveryonePermissions = srcTaskItem.EveryonePermissions,
                                                     GroupPermissions = srcTaskItem.GroupPermissions,
                                                     CurrentPermissions = srcTaskItem.CurrentPermissions,
                                                     NextPermissions = srcTaskItem.NextPermissions,
                                                     Flags = srcTaskItem.Flags,
                                                     SalePrice = srcTaskItem.SalePrice,
                                                     SaleType = srcTaskItem.SaleType
                                                 };



            if (destPart.OwnerID != part.OwnerID)
            {
                if (m_scene.Permissions.PropagatePermissions())
                {
                    destTaskItem.CurrentPermissions = srcTaskItem.CurrentPermissions &
                            (srcTaskItem.NextPermissions | (uint)PermissionMask.Move);
                    destTaskItem.GroupPermissions = srcTaskItem.GroupPermissions &
                            (srcTaskItem.NextPermissions | (uint)PermissionMask.Move);
                    destTaskItem.EveryonePermissions = srcTaskItem.EveryonePermissions &
                            (srcTaskItem.NextPermissions | (uint)PermissionMask.Move);
                    destTaskItem.BasePermissions = srcTaskItem.BasePermissions &
                            (srcTaskItem.NextPermissions | (uint)PermissionMask.Move);
                    destTaskItem.CurrentPermissions |= 16; // Slam!
                }
            }

            destTaskItem.Description = srcTaskItem.Description;
            destTaskItem.Name = srcTaskItem.Name;
            destTaskItem.InvType = srcTaskItem.InvType;
            destTaskItem.Type = srcTaskItem.Type;

            destPart.Inventory.AddInventoryItem(destTaskItem, part.OwnerID != destPart.OwnerID);

            if ((srcTaskItem.CurrentPermissions & (uint)PermissionMask.Copy) == 0)
                part.Inventory.RemoveInventoryItem(itemId);

            IScenePresence avatar;

            if (m_scene.TryGetScenePresence(srcTaskItem.OwnerID, out avatar))
                destPart.GetProperties(avatar.ControllingClient);
        }

        /// <summary>
        /// Move the given item from the object task inventory to the agent's inventory
        /// </summary>
        /// <param name="avatarId"></param>
        /// <param name="folderId">
        /// The user inventory folder to move (or copy) the item to.  If null, then the most
        /// suitable system folder is used (e.g. the Objects folder for objects).  If there is no suitable folder, then
        /// the item is placed in the user's root inventory folder
        /// </param>
        /// <param name="part"></param>
        /// <param name="itemId"></param>
        /// <param name="checkPermissions"></param>
        public InventoryItemBase MoveTaskInventoryItemToUserInventory (UUID avatarId, UUID folderId, ISceneChildEntity part, UUID itemId, bool checkPermissions)
        {
            IScenePresence avatar;

            if (m_scene.TryGetScenePresence(avatarId, out avatar))
            {
                return MoveTaskInventoryItemToUserInventory (avatar.ControllingClient, folderId, part, itemId, checkPermissions);
            }
            InventoryItemBase agentItem = CreateAgentInventoryItemFromTask(avatarId, part, itemId);

            if (agentItem == null)
                return null;

            agentItem.Folder = folderId;

            AddInventoryItem(agentItem);

            return agentItem;
        }

        /// <summary>
        /// Move the given items from the object task inventory to the agent's inventory
        /// </summary>
        /// <param name="destID"></param>
        /// <param name="name"></param>
        /// <param name="host"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public UUID MoveTaskInventoryItemsToUserInventory (UUID destID, string name, ISceneChildEntity host, List<UUID> items)
        {
            InventoryFolderBase rootFolder = m_scene.InventoryService.GetRootFolder(destID);

            UUID newFolderID = UUID.Random();

            InventoryFolderBase newFolder = new InventoryFolderBase(newFolderID, name, destID, -1, rootFolder.ID, rootFolder.Version);
            m_scene.InventoryService.AddFolder(newFolder);

            foreach (UUID itemID in items)
            {
                InventoryItemBase agentItem = CreateAgentInventoryItemFromTask(destID, host, itemID);

                if (agentItem != null)
                {
                    agentItem.Folder = newFolderID;

                    AddInventoryItem(agentItem);
                }
            }

            IScenePresence avatar;
            if (m_scene.TryGetScenePresence(destID, out avatar))
            {
                SendInventoryUpdate(avatar.ControllingClient, rootFolder, true, false);
                SendInventoryUpdate(avatar.ControllingClient, newFolder, false, true);
            }

            return newFolderID;
        }

        #endregion

        #region Caps

        /// <summary>
        /// Register the Caps for inventory
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="server"></param>
        private OSDMap EventManagerOnRegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["UpdateScriptTaskInventory"] = CapsUtil.CreateCAPS("UpdateScriptTaskInventory", "");
            retVal["UpdateScriptTask"] = retVal["UpdateScriptTaskInventory"];

            //Region Server bound
            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["UpdateScriptTask"],
                delegate(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                {
                    return ScriptTaskInventory(agentID, path, request, httpRequest, httpResponse);
                }));

            retVal["UpdateGestureTaskInventory"] = CapsUtil.CreateCAPS("UpdateGestureTaskInventory", "");
            retVal["UpdateNotecardTaskInventory"] = retVal["UpdateGestureTaskInventory"];

            //Region Server bound
            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["UpdateGestureTaskInventory"],
                delegate(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                {
                    return TaskInventoryUpdaterHandle(agentID, path, request, httpRequest, httpResponse);
                }));

            retVal["UpdateScriptAgentInventory"] = CapsUtil.CreateCAPS("UpdateScriptAgentInventory", "");
            retVal["UpdateNotecardAgentInventory"] = retVal["UpdateScriptAgentInventory"];
            retVal["UpdateGestureAgentInventory"] = retVal["UpdateScriptAgentInventory"];
            retVal["UpdateScriptAgent"] = retVal["UpdateScriptAgentInventory"];
            //Unless the script engine goes, region server bound
            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["UpdateScriptAgentInventory"], delegate(
                string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return NoteCardAgentInventory(agentID, path, request, httpRequest, httpResponse);
            }));
            return retVal;
        }

        /// <summary>
        /// Called by the script task update handler.  Provides a URL to which the client can upload a new asset.
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="httpRequest">HTTP request header object</param>
        /// <param name="httpResponse">HTTP response header object</param>
        /// <returns></returns>
        public byte[] ScriptTaskInventory(UUID AgentID, string path, Stream request, OSHttpRequest httpRequest,
                                                                    OSHttpResponse httpResponse)
        {
            try
            {
                MainConsole.Instance.Debug("[Scene]: ScriptTaskInventory Request in region: " + m_scene.RegionInfo.RegionName);
                //MainConsole.Instance.DebugFormat("[CAPS]: request: {0}, path: {1}, param: {2}", request, path, param);
                OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(request);
                UUID item_id = map["item_id"].AsUUID();
                UUID task_id = map["task_id"].AsUUID();
                int is_script_running = map["is_script_running"].AsInteger();
                string capsBase = "/CAPS/" + UUID.Random();
                string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");

                TaskInventoryScriptUpdater uploader =
                    new TaskInventoryScriptUpdater(
                        m_scene,
                        item_id,
                        task_id,
                        is_script_running,
                        capsBase + uploaderPath,
                        MainServer.Instance,
                        AgentID);

                MainServer.Instance.AddStreamHandler(
                    new GenericStreamHandler("POST", capsBase + uploaderPath, uploader.uploaderCaps));

                string uploaderURL = MainServer.Instance.ServerURI + capsBase +
                                     uploaderPath;

                map = new OSDMap();
                map["uploader"] = uploaderURL;
                map["state"] = "upload";
                return OSDParser.SerializeLLSDXmlBytes(map);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[CAPS]: " + e);
            }

            return null;
        }

        /// <summary>
        /// Called by the script task update handler.  Provides a URL to which the client can upload a new asset.
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="httpRequest">HTTP request header object</param>
        /// <param name="httpResponse">HTTP response header object</param>
        /// <returns></returns>
        public byte[] TaskInventoryUpdaterHandle(UUID AgentID, string path, Stream request, OSHttpRequest httpRequest,
                                                                    OSHttpResponse httpResponse)
        {
            try
            {
                OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(request);
                UUID item_id = map["item_id"].AsUUID();
                UUID task_id = map["task_id"].AsUUID();
                string capsBase = "/CAPS/" + UUID.Random();
                string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");

                TaskInventoryUpdater uploader =
                    new TaskInventoryUpdater(
                        m_scene,
                        item_id,
                        task_id,
                        capsBase + uploaderPath,
                        MainServer.Instance,
                        AgentID);

                MainServer.Instance.AddStreamHandler(
                    new GenericStreamHandler("POST", capsBase + uploaderPath, uploader.uploaderCaps));

                string uploaderURL = MainServer.Instance.ServerURI + capsBase +
                                     uploaderPath;

                map = new OSDMap();
                map["uploader"] = uploaderURL;
                map["state"] = "upload";
                return OSDParser.SerializeLLSDXmlBytes(map);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[CAPS]: " + e);
            }

            return null;
        }

        /// <summary>
        /// Called by the notecard update handler.  Provides a URL to which the client can upload a new asset.
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="httpRequest"></param>
        /// <param name="httpResponse"></param>
        /// <returns></returns>
        public byte[] NoteCardAgentInventory(UUID AgentID, string path, Stream request, OSHttpRequest httpRequest,
                                                                    OSHttpResponse httpResponse)
        {
            //MainConsole.Instance.Debug("[CAPS]: NoteCardAgentInventory Request in region: " + m_regionName + "\n" + request);
            //MainConsole.Instance.Debug("[CAPS]: NoteCardAgentInventory Request is: " + request);

            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(request);

            string capsBase = "/CAPS/" + UUID.Random();
            string uploaderPath = Util.RandomClass.Next(5000, 8000).ToString("0000");

            ItemUpdater uploader =
                new ItemUpdater(AgentID, m_scene, map["item_id"].AsUUID(), capsBase + uploaderPath, MainServer.Instance);

            MainServer.Instance.AddStreamHandler(
                new GenericStreamHandler("POST", capsBase + uploaderPath, uploader.uploaderCaps));

            string uploaderURL = MainServer.Instance.ServerURI + capsBase +
                                 uploaderPath;

            map = new OSDMap();
            map["uploader"] = uploaderURL;
            map["state"] = "upload";
            return OSDParser.SerializeLLSDXmlBytes(map);
        }

        /// <summary>
        /// This class is a callback invoked when a client sends asset data to
        /// an agent inventory notecard update url
        /// </summary>
        public class ItemUpdater
        {
            private readonly string uploaderPath = String.Empty;
            private readonly UUID inventoryItemID;
            private readonly IHttpServer httpListener;
            private readonly UUID agentID;
            private readonly IScene m_scene;

            public ItemUpdater (UUID AgentID, IScene scene, UUID inventoryItem, string path, IHttpServer httpServer)
            {
                inventoryItemID = inventoryItem;
                uploaderPath = path;
                httpListener = httpServer;
                agentID = AgentID;
                m_scene = scene;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="data"></param>
            /// <param name="path"></param>
            /// <param name="param"></param>
            /// <returns></returns>
            public byte[] uploaderCaps(string path, Stream request,
                                                            OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                byte[] data = HttpServerHandlerHelpers.ReadFully(request);
                UUID inv = inventoryItemID;
                IClientAPI client;
                string res = "";
                if (m_scene.ClientManager.TryGetValue(agentID, out client))
                {
                    IInventoryAccessModule invAccess = m_scene.RequestModuleInterface<IInventoryAccessModule>();
                    if (invAccess != null)
                        res = invAccess.CapsUpdateInventoryItemAsset(client, inv, data);
                }

                httpListener.RemoveStreamHandler("POST", uploaderPath);

                return Encoding.UTF8.GetBytes(res);
            }
        }

        /// <summary>
        /// This class is a callback invoked when a client sends asset data to
        /// a task inventory script update url
        /// </summary>
        public class TaskInventoryScriptUpdater
        {
            private readonly string uploaderPath = String.Empty;
            private readonly UUID inventoryItemID;
            private readonly UUID primID;
            private readonly bool isScriptRunning;
            private readonly IHttpServer httpListener;
            private readonly IScene m_scene;
            private readonly UUID AgentID;

            public TaskInventoryScriptUpdater(IScene scene, UUID inventoryItemID, UUID primID, int isScriptRunning2,
                                              string path, IHttpServer httpServer, UUID agentID)
            {

                this.inventoryItemID = inventoryItemID;
                this.primID = primID;
                AgentID = agentID;
                m_scene = scene;

                // This comes in over the packet as an integer, but actually appears to be treated as a bool
                isScriptRunning = (0 != isScriptRunning2);

                uploaderPath = path;
                httpListener = httpServer;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="data"></param>
            /// <param name="path"></param>
            /// <param name="param"></param>
            /// <returns></returns>
            public byte[] uploaderCaps(string path, Stream request,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                try
                {
                    //                    MainConsole.Instance.InfoFormat("[CAPS]: " +
                    //                                     "TaskInventoryScriptUpdater received data: {0}, path: {1}, param: {2}",
                    //                                     data, path, param));

                    IClientAPI client;
                    m_scene.ClientManager.TryGetValue(AgentID, out client);
                    UUID newAssetID = UUID.Zero;
                    byte[] data = HttpServerHandlerHelpers.ReadFully(request);
                    ArrayList errors = CapsUpdateTaskInventoryScriptAsset(client, inventoryItemID, primID, isScriptRunning, data, out newAssetID);

                    OSDMap map = new OSDMap();
                    map["new_asset"] = newAssetID;
                    map["compiled"] = !(errors.Count > 0);
                    map["state"] = "complete";
                    OSDArray array = new OSDArray();
                    foreach (object o in errors)
                        array.Add(OSD.FromObject(o));

                    map["errors"] = array;
                    httpListener.RemoveStreamHandler("POST", uploaderPath);
                    return OSDParser.SerializeLLSDXmlBytes(map);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[CAPS]: " + e.ToString());
                }

                // XXX Maybe this should be some meaningful error packet
                return null;
            }

            /// <summary>
            /// Capability originating call to update the asset of a script in a prim's (task's) inventory
            /// </summary>
            /// <param name="remoteClient"></param>
            /// <param name="itemId"></param>
            /// <param name="primId">The prim which contains the item to update</param>
            /// <param name="isScriptRunning2">Indicates whether the script to update is currently running</param>
            /// <param name="data"></param>
            public ArrayList CapsUpdateTaskInventoryScriptAsset(IClientAPI remoteClient, UUID itemId,
                                                           UUID primId, bool isScriptRunning2, byte[] data, out UUID newID)
            {
                ArrayList errors = new ArrayList();
                if (!m_scene.Permissions.CanEditScript(itemId, primId, remoteClient.AgentId))
                {
                    newID = UUID.Zero;
                    errors.Add("Insufficient permissions to edit script");
                    return errors;
                }

                // Retrieve group
                ISceneChildEntity part = m_scene.GetSceneObjectPart (primId);
                if(null == part || null == part.ParentEntity)
                {
                    MainConsole.Instance.ErrorFormat(
                        "[PRIM INVENTORY]: " +
                        "Prim inventory update requested for item ID {0} in prim ID {1} but this prim does not exist",
                        itemId, primId);

                    newID = UUID.Zero;
                    errors.Add("Unable to find requested prim to update.");
                    return errors;
                }

                // Retrieve item
                TaskInventoryItem item = part.Inventory.GetInventoryItem(itemId);

                if (null == item)
                {
                    MainConsole.Instance.ErrorFormat(
                        "[PRIM INVENTORY]: Tried to retrieve item ID {0} from prim {1}, {2} for caps script update "
                            + " but the item does not exist in this inventory",
                        itemId, part.Name, part.UUID);

                    newID = UUID.Zero;
                    errors.Add("Unable to find requested inventory item in prim to update.");
                    return errors;
                }

                // Trigger rerunning of script (use TriggerRezScript event, see RezScript)

                // Update item with new asset
                if ((newID = m_scene.AssetService.UpdateContent(item.AssetID, data)) == UUID.Zero)
                {
                    errors.Add("Failed to save script to asset storage. Please try again later.");
                }
                else
                {
                    item.AssetID = newID;

                    part.Inventory.UpdateInventoryItem(item);
                    
                    if (isScriptRunning2)
                    {
                        // Needs to determine which engine was running it and use that
                        //
                        part.Inventory.UpdateScriptInstance(item.ItemID, data, 0, false, StateSource.NewRez);
                        errors = part.Inventory.GetScriptErrors(item.ItemID);
                    }
                    else
                        errors.Add("Script saved successfully.");

                    part.GetProperties(remoteClient);
                }
                return errors;
            }
        }

        /// <summary>
        /// This class is a callback invoked when a client sends asset data to
        /// a task inventory script update url
        /// </summary>
        public class TaskInventoryUpdater
        {
            private readonly string uploaderPath = String.Empty;
            private readonly UUID inventoryItemID;
            private readonly UUID primID;
            private readonly IHttpServer httpListener;
            private readonly IScene m_scene;
            private readonly UUID AgentID;

            public TaskInventoryUpdater(IScene scene, UUID inventoryItemID, UUID primID,
                                              string path, IHttpServer httpServer, UUID agentID)
            {

                this.inventoryItemID = inventoryItemID;
                this.primID = primID;
                AgentID = agentID;
                m_scene = scene;

                uploaderPath = path;
                httpListener = httpServer;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="data"></param>
            /// <param name="path"></param>
            /// <param name="param"></param>
            /// <returns></returns>
            public byte[] uploaderCaps(string path, Stream request,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                try
                {
                    IClientAPI client;
                    m_scene.ClientManager.TryGetValue(AgentID, out client);
                    UUID newAssetID = UUID.Zero;
                    byte[] data = HttpServerHandlerHelpers.ReadFully(request);
                    ISceneChildEntity part = m_scene.GetSceneObjectPart(primID);
                    if (part != null)
                    {
                        // Retrieve item
                        TaskInventoryItem item = part.Inventory.GetInventoryItem(inventoryItemID);

                        if (item != null)
                        {
                            if ((item.Type == (int)InventoryType.Notecard || item.Type == (int)InventoryType.Gesture || item.Type == 21 /* Gesture... again*/)
                                && m_scene.Permissions.CanViewNotecard(inventoryItemID, primID, AgentID))
                            {
                                if ((newAssetID = m_scene.AssetService.UpdateContent(item.AssetID, data)) != UUID.Zero)
                                {
                                    item.AssetID = newAssetID;
                                    part.Inventory.UpdateInventoryItem(item);
                                }
                            }
                        }
                    }
                    OSDMap map = new OSDMap();
                    map["new_asset"] = newAssetID;
                    map["state"] = "complete";
                    httpListener.RemoveStreamHandler("POST", uploaderPath);
                    return OSDParser.SerializeLLSDXmlBytes(map);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Error("[CAPS]: " + e.ToString());
                }

                // XXX Maybe this should be some meaningful error packet
                return null;
            }
        }

        #endregion
    }
}
