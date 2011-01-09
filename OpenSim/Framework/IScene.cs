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

using System.Collections.Generic;
using System.Net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework.Console;

namespace OpenSim.Framework
{
    /// <value>
    /// Indicate what action to take on an object derez request
    /// </value>
    public enum DeRezAction : byte
    {	
        SaveToExistingUserInventoryItem = 0,
        AcquireToUserInventory = 1,		// try to leave copy in world
        SaveIntoTaskInventory = 2,
        Attachment = 3,
        Take = 4,
        GodTakeCopy = 5,   // force take copy
        Delete = 6,
        AttachmentToInventory = 7,
        AttachmentExists = 8,
        Return = 9,           // back to owner's inventory
        ReturnToLastOwner = 10    // deeded object back to last owner's inventory
    };

    public interface IScene : IRegistryCore
    {
        RegionInfo RegionInfo { get; }
        
        IConfigSource Config { get; }

        void AddNewClient(IClientAPI client);

        bool TryGetScenePresence(UUID agentID, out IScenePresence scenePresence);

        bool CheckClient(UUID agentID, IPEndPoint ep);
    }
}
