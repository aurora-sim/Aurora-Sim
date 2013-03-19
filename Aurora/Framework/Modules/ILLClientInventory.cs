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

using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using Aurora.Framework.Services.ClassHelpers.Inventory;
using OpenMetaverse;
using System.Collections.Generic;

namespace Aurora.Framework
{
    public interface ILLClientInventory
    {
        /// <summary>
        ///     The default LSL script that will be added when a client creates
        ///     a new script in inventory or in the task object inventory
        /// </summary>
        string DefaultLSLScript { get; set; }

        /// <summary>
        ///     Add the given inventory item to a user's inventory asyncronously.
        /// </summary>
        /// <param name="item">The item to add</param>
        void AddInventoryItemAsync(InventoryItemBase item);

        /// <summary>
        ///     Add the given inventory item to a user's inventory.
        /// </summary>
        /// <param name="item">The item to add</param>
        void AddInventoryItem(InventoryItemBase item);

        /// <summary>
        ///     Add an inventory item to an avatar's inventory.
        /// </summary>
        /// <param name="remoteClient">The remote client controlling the avatar</param>
        /// <param name="item">
        ///     The item.  This structure contains all the item metadata, including the folder
        ///     in which the item is to be placed.
        /// </param>
        void AddInventoryItemAsync(IClientAPI remoteClient, InventoryItemBase item);

        /// <summary>
        ///     Return the given objects to the agent given
        /// </summary>
        /// <param name="sceneObjectGroup">The objects to return</param>
        /// <param name="uuid">The agent UUID that will get the inventory items for these objects</param>
        /// <returns></returns>
        bool ReturnObjects(ISceneEntity[] sceneObjectGroup, UUID uuid);

        /// <summary>
        ///     Move the given item from the object task inventory to the agent's inventory
        /// </summary>
        /// <param name="destID"></param>
        /// <param name="uuid"></param>
        /// <param name="m_host"></param>
        /// <param name="objID"></param>
        /// <param name="checkPermissions"></param>
        InventoryItemBase MoveTaskInventoryItemToUserInventory(UUID destID, UUID uuid, ISceneChildEntity m_host,
                                                               UUID objID, bool checkPermissions);

        /// <summary>
        ///     Move the given items from the object task inventory to the agent's inventory
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="p"></param>
        /// <param name="part"></param>
        /// <param name="invList"></param>
        /// <returns></returns>
        UUID MoveTaskInventoryItemsToUserInventory(UUID uuid, string p, ISceneChildEntity part, List<UUID> invList);

        /// <summary>
        ///     Copy a task (prim) inventory item to another task (prim)
        /// </summary>
        /// <param name="destId"></param>
        /// <param name="m_host"></param>
        /// <param name="objId"></param>
        void MoveTaskInventoryItemToObject(UUID destId, ISceneChildEntity m_host, UUID objId);

        /// <summary>
        ///     Rez a script into a prim's inventory from another prim
        ///     This is used for the LSL function llRemoteLoadScriptPin and requires a valid pin to be used
        /// </summary>
        /// <param name="srcId">The UUID of the script that is going to be copied</param>
        /// <param name="m_host">The prim that the script that is going to be copied from</param>
        /// <param name="destId">The UUID of the prim that the </param>
        /// <param name="pin">The ScriptAccessPin of the prim</param>
        /// <param name="running">Whether the script should be running when it is started</param>
        /// <param name="start_param">The start param to pass to the script</param>
        void RezScript(UUID srcId, ISceneChildEntity m_host, UUID destId, int pin, int running, int start_param);
    }
}