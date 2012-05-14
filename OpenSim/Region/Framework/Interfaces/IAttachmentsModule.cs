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
using OpenMetaverse;
using Aurora.Framework;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IAttachmentsModule
    {
        /// <summary>
        ///   Attach an object to an avatar.
        /// </summary>
        /// <param name = "localID"></param>
        /// <param name = "controllingClient"></param>
        /// <param name = "attachPoint"></param>
        /// <param name = "rot"></param>
        /// <param name = "attachPos"></param>
        /// <returns>true if the object was successfully attached, false otherwise</returns>
        bool AttachObjectFromInworldObject(
            uint localID, IClientAPI remoteClient, ISceneEntity grp, int AttachmentPt);

        /// <summary>
        ///   Rez an attachment from user inventory
        /// </summary>
        /// <param name = "remoteClient"></param>
        /// <param name = "itemID"></param>
        /// <param name = "AttachmentPt"></param>
        /// <param name = "updateinventoryStatus">
        /// <param name = "updateUUIDs">ONLY make this true if you know that the user will not be crossing or teleporting when this call will be happening</param>
        ///   <returns>The scene object that was attached.  Null if the scene object could not be found</returns>
        ISceneEntity RezSingleAttachmentFromInventory(
            IClientAPI remoteClient, UUID itemID, UUID assetID, int AttachmentPt, bool updateUUIDs);

        /// <summary>
        ///   Detach the given item to the ground.
        /// </summary>
        /// <param name = "itemID"></param>
        /// <param name = "remoteClient"></param>
        void DetachSingleAttachmentToGround(UUID itemID, IClientAPI remoteClient);

        /// <summary>
        ///   Update the user inventory to show a detach.
        /// </summary>
        /// <param name = "itemID">
        ///   A <see cref = "UUID" />
        /// </param>
        /// <param name = "remoteClient">
        ///   A <see cref = "IClientAPI" />
        /// </param>
        void DetachSingleAttachmentToInventory(UUID itemID, IClientAPI remoteClient);

        /// <summary>
        ///   Update the position of an attachment
        /// </summary>
        /// <param name = "client">The client whose attachment we are to move</param>
        /// <param name = "sog">The object to move</param>
        /// <param name = "localID">The localID of the object to move</param>
        /// <param name = "pos">The new position of the attachment</param>
        void UpdateAttachmentPosition(IClientAPI client, ISceneEntity sog, uint localID, Vector3 pos);

        /// <summary>
        ///   Get a list of the given avatar's attachments
        /// </summary>
        /// <param name = "avatarID"></param>
        /// <returns></returns>
        ISceneEntity[] GetAttachmentsForAvatar(UUID avatarID);

        /// <summary>
        ///   Send a script event to all attachments
        /// </summary>
        /// <param name = "avatarID"></param>
        /// <param name = "eventName"></param>
        /// <param name = "args"></param>
        void SendScriptEventToAttachments(UUID avatarID, string eventName, Object[] args);

        /// <summary>
        /// Send updates for all attachments to the given presences
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="sender"></param>
        void SendAttachmentsToPresence(IScenePresence receiver, IScenePresence sender);
    }
}