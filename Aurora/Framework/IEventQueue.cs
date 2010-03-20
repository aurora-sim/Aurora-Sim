using System;
using System.Collections.Generic;
using System.Text;

namespace Aurora.Framework
{
    public interface IEventQueueManager
    {
        void AddEventPlugin(string UserIdentifier, IEventQueuePlugin Event);
        IEventQueuePlugin GetPlugin(string identifier);
        void RemoveEventPlugin(string UserIdentifier, IEventQueuePlugin Event);
        List<IEventQueuePlugin> EventQueuePlugins { get; }
    }
    public interface IEventQueuePlugin
    {
        //This allows plugins to attach to incoming clients.
        void IncomingClient(IEventQueueManager manager, AuroraProfileData client);
        //This sends the closing.
        void ClosingClient(AuroraProfileData client);
        void Fire(string EventType, object Event);
        void RegisterCallback(string EventType, object callback);
        string Identifier { get; }
    }
}
