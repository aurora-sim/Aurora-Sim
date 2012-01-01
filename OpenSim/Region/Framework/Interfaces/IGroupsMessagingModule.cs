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
using Aurora.Framework;

namespace OpenSim.Region.Framework.Interfaces
{
    /// <summary>
    ///   Provide mechanisms for messaging groups.
    /// </summary>
    public interface IGroupsMessagingModule
    {
        /// <summary>
        ///   Send a message to an entire group.
        /// </summary>
        /// <param name = "im">
        ///   The message itself.  The fields that must be populated are
        /// 
        ///   imSessionID - Populate this with the group ID (session ID and group ID are currently identical)
        ///   fromAgentName - Populate this with whatever arbitrary name you want to show up in the chat dialog
        ///   message - The message itself
        ///   dialog - This must be (byte)InstantMessageDialog.SessionSend
        /// </param>
        /// <param name = "groupID"></param>
        void SendMessageToGroup(GridInstantMessage im, UUID groupID);

        /// <summary>
        ///   Ensures that a group chat is created and users are added to it
        /// </summary>
        /// <param name = "groupID"></param>
        void EnsureGroupChatIsStarted(UUID groupID);
    }
}