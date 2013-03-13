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

using OpenMetaverse;
using FriendInfo = Aurora.Framework.FriendInfo;

namespace Aurora.Framework
{
    public interface IFriendsModule
    {
        /// <summary>
        ///   Get the permissions that PrincipalID has for FriendID
        /// </summary>
        /// <param name = "PrincipalID"></param>
        /// <param name = "FriendID"></param>
        /// <returns></returns>
        int GetFriendPerms(UUID PrincipalID, UUID FriendID);

        /// <summary>
        ///   Send a 'user is on/offline' message to the given FriendToInformID about UserID
        /// </summary>
        /// <param name = "FriendToInformID"></param>
        /// <param name = "UserID"></param>
        /// <param name = "NewStatus">On/Offline</param>
        void SendFriendsStatusMessage(UUID FriendToInformID, UUID UserID, bool NewStatus);

        /// <summary>
        ///   Gets all the given friends of the user (only if the user is in the region)
        /// </summary>
        /// <param name = "agentID"></param>
        /// <returns></returns>
        FriendInfo[] GetFriends(UUID agentID);
    }
}