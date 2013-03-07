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
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    /// <summary>
    ///   Callback used when a user's inventory is received from the inventory service
    /// </summary>
    public delegate void InventoryReceiptCallback(
        ICollection<InventoryFolderImpl> folders, ICollection<InventoryItemBase> items);

    public interface IExternalInventoryService : IInventoryService
    {
        //This is the same as the normal inventory interface, but it is used to load the inventory service for external transactions (outside of this simulator/grid)
    }

    public interface IInventoryService
    {
        /// <summary>
        ///   Create the entire inventory for a given user (local only)
        /// </summary>
        /// <param name = "user"></param>
        /// <returns></returns>
        bool CreateUserInventory(UUID user, bool createDefaultItems);

        /// <summary>
        ///   Create the entire inventory for a given user (local only)
        /// </summary>
        /// <param name = "user"></param>
        /// <returns></returns>
        bool CreateUserInventory(UUID user, bool createDefaultItems, out List<InventoryItemBase> defaultInventoryItems);

        /// <summary>
        ///   Gets the skeleton of the inventory -- folders only (local only)
        /// </summary>
        /// <param name = "userId"></param>
        /// <returns></returns>
        List<InventoryFolderBase> GetInventorySkeleton(UUID userId);

        /// <summary>
        ///   Retrieve the root inventory folder for the given user.
        /// </summary>
        /// <param name = "userID"></param>
        /// <returns>null if no root folder was found</returns>
        InventoryFolderBase GetRootFolder(UUID userID);

        /// <summary>
        /// Gets a folder by name for the given user
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="FolderName"></param>
        /// <returns></returns>
        InventoryFolderBase GetFolderByOwnerAndName(UUID userID, string FolderName);

        /// <summary>
        ///   Retrieve the root inventory folder for the given user. - local only
        /// </summary>
        /// <param name = "userID"></param>
        /// <returns>null if no root folder was found</returns>
        List<InventoryFolderBase> GetRootFolders(UUID userID);

        /// <summary>
        ///   Gets the user folder for the given folder-type
        /// </summary>
        /// <param name = "userID"></param>
        /// <param name = "invType"></param>
        /// <param name = "type"></param>
        /// <returns></returns>
        InventoryFolderBase GetFolderForType(UUID userID, InventoryType invType, AssetType type);

        /// <summary>
        ///   Gets everything (folders and items) inside a folder
        /// </summary>
        /// <param name = "userId"></param>
        /// <param name = "folderID"></param>
        /// <returns></returns>
        InventoryCollection GetFolderContent(UUID userID, UUID folderID);

        /// <summary>
        ///   Gets the folders inside a folder (local only)
        /// </summary>
        /// <param name = "userID"></param>
        /// <param name = "folderID"></param>
        /// <returns></returns>
        List<InventoryFolderBase> GetFolderFolders(UUID userID, UUID folderID);

        /// <summary>
        ///   Gets the items inside a folder - local only
        /// </summary>
        /// <param name = "userID"></param>
        /// <param name = "folderID"></param>
        /// <returns></returns>
        List<InventoryItemBase> GetFolderItems(UUID userID, UUID folderID);

        /// <summary>
        ///   Add a new folder to the user's inventory
        /// </summary>
        /// <param name = "folder"></param>
        /// <returns>true if the folder was successfully added</returns>
        bool AddFolder(InventoryFolderBase folder);

        /// <summary>
        ///   Update a folder in the user's inventory
        /// </summary>
        /// <param name = "folder"></param>
        /// <returns>true if the folder was successfully updated</returns>
        bool UpdateFolder(InventoryFolderBase folder);

        /// <summary>
        ///   Move an inventory folder to a new location
        /// </summary>
        /// <param name = "folder">A folder containing the details of the new location</param>
        /// <returns>true if the folder was successfully moved</returns>
        bool MoveFolder(InventoryFolderBase folder);

        /// <summary>
        ///   Delete an item from the user's inventory
        /// </summary>
        /// <param name = "item"></param>
        /// <returns>true if the item was successfully deleted</returns>
        bool DeleteFolders(UUID userID, List<UUID> folderIDs);

        /// <summary>
        ///   Force Deletes a folder (LOCAL ONLY)
        /// </summary>
        /// <param name = "folder"></param>
        /// <returns></returns>
        bool ForcePurgeFolder(InventoryFolderBase folder);

        /// <summary>
        ///   Purge an inventory folder of all its items and subfolders.
        /// </summary>
        /// <param name = "folder"></param>
        /// <returns>true if the folder was successfully purged</returns>
        bool PurgeFolder(InventoryFolderBase folder);

        /// <summary>
        ///   Add a new item to the user's inventory
        /// </summary>
        /// <param name = "item">
        ///   The item to be added.  If item.FolderID == UUID.Zero then the item is added to the most suitable system
        ///   folder.  If there is no suitable folder then the item is added to the user's root inventory folder.
        /// </param>
        /// <returns>true if the item was successfully added, false if it was not</returns>
        bool AddItem(InventoryItemBase item);

        /// <summary>
        ///   Update an item in the user's inventory
        /// </summary>
        /// <param name = "item"></param>
        /// <returns>true if the item was successfully updated</returns>
        bool UpdateItem(InventoryItemBase item);

        /// <summary>
        /// Update the assetID for the given item
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="assetID"></param>
        /// <returns></returns>
        bool UpdateAssetIDForItem(UUID itemID, UUID assetID);

        /// <summary>
        ///   Move the given items to the folder given in the inventory item
        /// </summary>
        /// <param name = "ownerID"></param>
        /// <param name = "items"></param>
        /// <returns></returns>
        bool MoveItems(UUID ownerID, List<InventoryItemBase> items);

        /// <summary>
        ///   Delete an item from the user's inventory
        /// </summary>
        /// <param name = "item"></param>
        /// <returns>true if the item was successfully deleted</returns>
        bool DeleteItems(UUID userID, List<UUID> itemIDs);

        /// <summary>
        ///   Get an item, given by its UUID
        /// </summary>
        /// <param name = "item"></param>
        /// <returns></returns>
        InventoryItemBase GetItem(UUID userID, UUID inventoryID);

        /// <summary>
        ///   Get a folder, given by its UUID
        /// </summary>
        /// <param name = "folder"></param>
        /// <returns></returns>
        InventoryFolderBase GetFolder(InventoryFolderBase folder);

        /// <summary>
        ///   Get the active gestures of the agent.
        /// </summary>
        /// <param name = "userId"></param>
        /// <returns></returns>
        List<InventoryItemBase> GetActiveGestures(UUID userId);

        /// <summary>
        /// Gives an inventory item to another user (LOCAL ONLY)
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="senderId"></param>
        /// <param name="item"></param>
        /// <param name="recipientFolderId"></param>
        /// <param name="doOwnerCheck"></param>
        /// <returns></returns>
        InventoryItemBase InnerGiveInventoryItem(UUID recipient, UUID senderId, InventoryItemBase item, UUID recipientFolderId, bool doOwnerCheck);

        #region OSD methods

        /// <summary>
        ///   Get the item serialized as an OSDArray - local only
        /// </summary>
        /// <param name = "itemID"></param>
        /// <returns></returns>
        OSDArray GetOSDItem(UUID avatarID, UUID itemID);

        #endregion

        #region Async methods

        /// <summary>
        /// Adds a new item to the user's inventory asynchronously
        /// </summary>
        /// <param name="item"></param>
        /// <param name="success"></param>
        void AddItemAsync(InventoryItemBase item, NoParam success);

        /// <summary>
        /// Moves multiple items to a new folder in the user's inventory
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="items"></param>
        /// <param name="success"></param>
        void MoveItemsAsync(UUID agentID, List<InventoryItemBase> items, NoParam success);

        /// <summary>
        /// Gives an inventory item to another user asychronously
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="senderId"></param>
        /// <param name="itemId"></param>
        /// <param name="recipientFolderId"></param>
        /// <param name="doOwnerCheck"></param>
        /// <param name="success"></param>
        void GiveInventoryItemAsync(UUID recipient, UUID senderId, UUID itemId,
            UUID recipientFolderId, bool doOwnerCheck, GiveItemParam success);

        /// <summary>
        /// Gives an entire inventory folder to another user asynchronously
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="senderId"></param>
        /// <param name="folderId"></param>
        /// <param name="recipientParentFolderId"></param>
        /// <param name="success"></param>
        void GiveInventoryFolderAsync(
            UUID recipientId, UUID senderId, UUID folderId, UUID recipientParentFolderId, GiveFolderParam success);

        #endregion

        UUID GetItemAssetID(UUID uUID, UUID itemID);
    }

    public delegate void GiveFolderParam(InventoryFolderBase folder);
    public delegate void GiveItemParam(InventoryItemBase item);

    public interface IInventoryData : IAuroraDataPlugin
    {
        List<InventoryFolderBase> GetFolders(string[] fields, string[] vals);
        List<InventoryItemBase> GetItems(UUID avatarID, string[] fields, string[] vals);
        OSDArray GetLLSDItems(string[] fields, string[] vals);

        bool HasAssetForUser(UUID userID, UUID assetID);
        string GetItemNameByAsset(UUID assetID);

        bool StoreFolder(InventoryFolderBase folder);
        bool StoreItem(InventoryItemBase item);

        bool UpdateAssetIDForItem(UUID itemID, UUID assetID);

        bool DeleteFolders(string field, string val, bool safe);
        bool DeleteItems(string field, string val);

        bool MoveItem(string id, string newParent);
        InventoryItemBase[] GetActiveGestures(UUID principalID);

        byte[] FetchInventoryReply(OSDArray fetchRequest, UUID AgentID, UUID forceOwnerID, UUID libraryOwnerID);

        void IncrementFolder(UUID folderID);
        void IncrementFolderByItem(UUID folderID);

        List<UUID> GetItemAssetIDs(UUID userID, string[] p1, string[] p2);
    }
}