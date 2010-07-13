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

namespace Aurora.Modules
{
    public interface IChatPlugin : IPlugin
    {
        void Initialize(IChatModule module);
        bool OnNewChatMessageFromWorld(OSChatMessage message, out OSChatMessage newmessage);

        void OnNewClient(IClientAPI client);

        void OnClosingClient(UUID clientID, Scene scene);
    }

    public interface IChatModule
    {
        void RegisterChatPlugin(string main, IChatPlugin plugin);
        int SayDistance { get; set; }
        int ShoutDistance { get; set; }
        int WhisperDistance { get; set; }
        IConfig Config { get; }
        void TrySendChatMessage(ScenePresence presence, Vector3 fromPos, Vector3 regionPos,
                                                  UUID fromAgentID, string fromName, ChatTypeEnum type,
                                                  string message, ChatSourceType src, float Range);
        void OnChatFromWorld(Object sender, OSChatMessage c);
    }
}
