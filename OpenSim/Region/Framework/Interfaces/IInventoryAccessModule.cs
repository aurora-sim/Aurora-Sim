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
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IInventoryAccessModule
    {
        string CapsUpdateInventoryItemAsset(IClientAPI remoteClient, UUID itemID, byte[] data);

        UUID DeleteToInventory(DeRezAction action, UUID folderID, List<ISceneEntity> objectGroups, UUID agentId,
                               out UUID itemID);

        /// <summary>
        /// Saves the given objects as an asset and returns the UUID of it
        /// </summary>
        /// <param name="list"></param>
        /// <param name="asset"></param>
        /// <returns></returns>
        UUID SaveAsAsset(List<ISceneEntity> list, out AssetBase asset);

        /// <summary>
        ///   Create a SceneObjectGroup representation of an asset xml of the given item
        /// </summary>
        /// <param name = "remoteClient"></param>
        /// <param name = "itemID"></param>
        /// <param name = "item"></param>
        /// <returns></returns>
        SceneObjectGroup CreateObjectFromInventory(IClientAPI remoteClient, UUID itemID, out InventoryItemBase item);

        /// <summary>
        ///   Create a SceneObjectGroup representation of an asset xml of the given item
        /// </summary>
        /// <param name = "remoteClient"></param>
        /// <param name = "itemID"></param>
        /// <returns></returns>
        SceneObjectGroup CreateObjectFromInventory(IClientAPI remoteClient, UUID itemID, UUID assetID);

        /// <summary>
        ///   Rez an object from inventory and add it to the scene
        /// </summary>
        /// <param name = "remoteClient"></param>
        /// <param name = "itemID"></param>
        /// <param name = "RayEnd"></param>
        /// <param name = "RayStart"></param>
        /// <param name = "RayTargetID"></param>
        /// <param name = "BypassRayCast"></param>
        /// <param name = "RayEndIsIntersection"></param>
        /// <param name = "RezSelected"></param>
        /// <param name = "RemoveItem"></param>
        /// <param name = "fromTaskID"></param>
        /// <returns></returns>
        SceneObjectGroup RezObject(IClientAPI remoteClient, UUID itemID, Vector3 RayEnd, Vector3 RayStart,
                                   UUID RayTargetID, byte BypassRayCast, bool RayEndIsIntersection,
                                   bool RezSelected, bool RemoveItem, UUID fromTaskID);

        bool GetAgentInventoryItem(IClientAPI remoteClient, UUID itemID, UUID requestID);

        // Must be here because of textures in user's inventory
        bool IsForeignUser(UUID userID, out string assetServerURL);

        //For mega-regions... after they are fixed, this can be removed
        void OnNewClient(IClientAPI client);
        void OnClosingClient(IClientAPI client);
    }
}