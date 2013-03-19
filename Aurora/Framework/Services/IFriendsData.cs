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

namespace Aurora.Framework
{
    /// <summary>
    ///     An interface for connecting to the friends datastore
    /// </summary>
    public interface IFriendsData : IAuroraDataPlugin
    {
        /// <summary>
        ///     Adds/updates a friend in the database
        /// </summary>
        /// <param name="PrincipalID">The initiator (user that we are saving for</param>
        /// <param name="Friend">The friend of the PrincipalID</param>
        /// <param name="flags">Flags between PrincipalID > Friend</param>
        /// <param name="offered"></param>
        /// <returns></returns>
        bool Store(UUID PrincipalID, string Friend, int flags, int offered);

        /// <summary>
        ///     Removes the friendship between the two users
        /// </summary>
        /// <param name="ownerID"></param>
        /// <param name="friend"></param>
        /// <returns></returns>
        bool Delete(UUID ownerID, string friend);

        /// <summary>
        ///     Gets all friends of the user
        /// </summary>
        /// <param name="principalID"></param>
        /// <returns></returns>
        FriendInfo[] GetFriends(UUID principalID);

        /// <summary>
        ///     Gets all friends of the user
        /// </summary>
        /// <param name="principalID"></param>
        /// <returns></returns>
        FriendInfo[] GetFriendsRequest(UUID principalID);
    }
}