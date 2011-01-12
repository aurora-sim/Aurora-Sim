using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework;
using OpenSim.Services.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface ILLClientInventory
    {
        /// <summary>
        /// The default LSL script that will be added when a client creates
        /// a new script in inventory or in the task object inventory
        /// </summary>
        string DefaultLSLScript { get; set; }

        /// <summary>
        /// Add the given inventory item to a user's inventory.
        /// </summary>
        /// <param name="item">The item to add</param>
        bool AddInventoryItem(InventoryItemBase item);

        /// <summary>
        /// Add an inventory item to an avatar's inventory.
        /// </summary>
        /// <param name="remoteClient">The remote client controlling the avatar</param>
        /// <param name="item">The item.  This structure contains all the item metadata, including the folder
        /// in which the item is to be placed.</param>
        void AddInventoryItem(IClientAPI remoteClient, InventoryItemBase item);

        /// <summary>
        /// Give an inventory item from one user to another
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="senderId">ID of the sender of the item</param>
        /// <param name="itemId"></param>
        /// <param name="recipientFolderId">
        /// The id of the folder in which the copy item should go.  If UUID.Zero then the item is placed in the most
        /// appropriate default folder.
        /// </param>
        /// <returns>
        /// The inventory item copy given, null if the give was unsuccessful
        /// </returns>
        InventoryItemBase GiveInventoryItem(
            UUID recipient, UUID senderId, UUID itemId, UUID recipientFolderId);

        /// <summary>
        /// Give an entire inventory folder from one user to another.  The entire contents (including all descendent
        /// folders) is given.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="senderId">ID of the sender of the item</param>
        /// <param name="folderId"></param>
        /// <param name="recipientParentFolderId">
        /// The id of the receipient folder in which the send folder should be placed.  If UUID.Zero then the
        /// recipient folder is the root folder
        /// </param>
        /// <returns>
        /// The inventory folder copy given, null if the copy was unsuccessful
        /// </returns>
        InventoryFolderBase GiveInventoryFolder(
            UUID recipientId, UUID senderId, UUID folderId, UUID recipientParentFolderId);

        /// <summary>
        /// Return the given objects to the agent given
        /// </summary>
        /// <param name="returnobjects">The objects to return</param>
        /// <param name="AgentId">The agent UUID that will get the inventory items for these objects</param>
        /// <returns></returns>
        bool ReturnObjects(SceneObjectGroup[] sceneObjectGroup, UUID uUID);

        /// <summary>
        /// Move the given item from the object task inventory to the agent's inventory
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="folderID">
        /// The user inventory folder to move (or copy) the item to.  If null, then the most
        /// suitable system folder is used (e.g. the Objects folder for objects).  If there is no suitable folder, then
        /// the item is placed in the user's root inventory folder
        /// </param>
        /// <param name="part"></param>
        /// <param name="itemID"></param>
        InventoryItemBase MoveTaskInventoryItemToUserInventory(UUID destId, UUID uUID, SceneObjectPart m_host, UUID objId);

        /// <summary>
        /// Move the given items from the object task inventory to the agent's inventory
        /// </summary>
        /// <param name="destID"></param>
        /// <param name="name"></param>
        /// <param name="host"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        UUID MoveTaskInventoryItemsToUserInventory(UUID uUID, string p, SceneObjectPart part, List<UUID> invList);

        /// <summary>
        /// Copy a task (prim) inventory item to another task (prim)
        /// </summary>
        /// <param name="destId"></param>
        /// <param name="part"></param>
        /// <param name="itemId"></param>
        void MoveTaskInventoryItemToObject(UUID destId, SceneObjectPart m_host, UUID objId);

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
        void RezScript(UUID srcId, SceneObjectPart m_host, UUID destId, int pin, int running, int start_param);
    }
}
