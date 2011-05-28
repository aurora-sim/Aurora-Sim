using System;
using System.Collections.Generic;
using System.Reflection;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using log4net;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Framework
{
    /// <summary>
    /// Adds functionality so that modules can be developed to be triggered by chat said inworld by avatars
    /// </summary>
    public interface IChatPlugin
    {
        /// <summary>
        /// Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        /// Starts the module and gives the reference to the ChatModule
        /// </summary>
        /// <param name="module"></param>
        void Initialize(IChatModule module);

        /// <summary>
        /// A new message has been said by an avatar
        /// </summary>
        /// <param name="message">The message said by the avatar</param>
        /// <param name="newmessage">What should be said (allows for modification of the message by modules)</param>
        /// <returns>Whether this message should be said at all</returns>
        bool OnNewChatMessageFromWorld(OSChatMessage message, out OSChatMessage newmessage);

        /// <summary>
        /// A new client has entered the scene
        /// </summary>
        /// <param name="client"></param>
        void OnNewClient(IClientAPI client);

        /// <summary>
        /// A client has left the scene
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="scene"></param>
        void OnClosingClient (UUID clientID, IScene scene);
    }

    public interface IChatModule
    {
        void RegisterChatPlugin(string main, IChatPlugin plugin);
        int SayDistance { get; set; }
        int ShoutDistance { get; set; }
        int WhisperDistance { get; set; }
        IConfig Config { get; }
        void TrySendChatMessage (IScenePresence presence, Vector3 fromPos, Vector3 regionPos,
                                                  UUID fromAgentID, string fromName, ChatTypeEnum type,
                                                  string message, ChatSourceType src, float Range);
        void OnChatFromWorld(Object sender, OSChatMessage c);

        void DeliverChatToAvatars(ChatSourceType chatSourceType, OSChatMessage message);

        void SimChatBroadcast(string message, ChatTypeEnum type, int channel, Vector3 fromPos, string fromName,
                                     UUID fromID, bool fromAgent, UUID ToAgentID, IScene scene);
        void SimChat(string message, ChatTypeEnum type, int channel, Vector3 fromPos, string fromName,
                            UUID fromID, bool fromAgent, IScene scene);
        void SimChat(string message, ChatTypeEnum type, int channel, Vector3 fromPos, string fromName,
                               UUID fromID, bool fromAgent, bool broadcast, float range, UUID ToAgentID, IScene scene);
    }
}
