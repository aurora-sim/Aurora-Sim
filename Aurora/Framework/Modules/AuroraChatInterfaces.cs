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
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Framework
{
    /// <summary>
    ///     Adds functionality so that modules can be developed to be triggered by chat said inworld by avatars
    /// </summary>
    public interface IChatPlugin
    {
        /// <summary>
        ///     Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        ///     Starts the module and gives the reference to the ChatModule
        /// </summary>
        /// <param name="module"></param>
        void Initialize(IChatModule module);

        /// <summary>
        ///     A new message has been said by an avatar
        /// </summary>
        /// <param name="message">The message said by the avatar</param>
        /// <param name="newmessage">What should be said (allows for modification of the message by modules)</param>
        /// <returns>Whether this message should be said at all</returns>
        bool OnNewChatMessageFromWorld(OSChatMessage message, out OSChatMessage newmessage);

        /// <summary>
        ///     A new client has entered the scene
        /// </summary>
        /// <param name="client"></param>
        void OnNewClient(IClientAPI client);

        /// <summary>
        ///     A client has left the scene
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="scene"></param>
        void OnClosingClient(UUID clientID, IScene scene);
    }

    public interface IChatModule
    {
        int SayDistance { get; set; }
        int ShoutDistance { get; set; }
        int WhisperDistance { get; set; }
        IConfig Config { get; }
        void RegisterChatPlugin(string main, IChatPlugin plugin);

        void TrySendChatMessage(IScenePresence presence, Vector3 fromPos, Vector3 regionPos,
                                UUID fromAgentID, string fromName, ChatTypeEnum type,
                                string message, ChatSourceType src, float Range);

        void OnChatFromWorld(Object sender, OSChatMessage c);

        void DeliverChatToAvatars(ChatSourceType chatSourceType, OSChatMessage message);

        void SimChatBroadcast(string message, ChatTypeEnum type, int channel, Vector3 fromPos, string fromName,
                              UUID fromID, bool fromAgent, UUID toAgentID, IScene scene);

        void SimChat(string message, ChatTypeEnum type, int channel, Vector3 fromPos, string fromName,
                     UUID fromID, bool fromAgent, IScene scene);

        void SimChat(string message, ChatTypeEnum type, int channel, Vector3 fromPos, string fromName,
                     UUID fromID, bool fromAgent, bool broadcast, float range, UUID ToAgentID, IScene scene);
    }
}