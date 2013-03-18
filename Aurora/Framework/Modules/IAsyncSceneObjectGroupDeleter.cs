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

namespace Aurora.Framework
{
    public interface IAsyncSceneObjectGroupDeleter
    {
        /// <summary>
        ///   Deletes the given groups to the given user's inventory in the given folderID
        /// </summary>
        /// <param name = "action">The reason these objects are being sent to inventory</param>
        /// <param name = "folderID">The folder the objects will be added into, if you want the default folder, set this to UUID.Zero</param>
        /// <param name = "objectGroups">The groups to send to inventory</param>
        /// <param name = "AgentId">The agent who is deleting the given groups (not the owner of the objects necessarily)</param>
        /// <param name = "permissionToDelete">If true, the objects will be deleted from the sim as well</param>
        /// <param name = "permissionToTake">If true, the objects will be added to the user's inventory as well</param>
        void DeleteToInventory(DeRezAction action, UUID folderID,
                               List<ISceneEntity> objectGroups, UUID AgentId,
                               bool permissionToDelete, bool permissionToTake);
    }
}