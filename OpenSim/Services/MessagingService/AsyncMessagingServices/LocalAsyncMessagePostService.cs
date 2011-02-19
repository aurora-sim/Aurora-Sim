using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.MessagingService
{
    /// <summary>
    /// This class deals with putting async messages into the regions 'queues' and sending them to them
    ///   when they request them. This is used for Aurora.Server
    /// </summary>
    public class LocalAsyncMessagePostService : IService, IAsyncMessagePostService
    {
        protected IRegistryCore m_registry;
        protected IAsyncMessageRecievedService m_asyncReceiverService;

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AsyncMessagePostServiceHandler", "") != Name)
                return;

            m_registry = registry;
            registry.RegisterModuleInterface<IAsyncMessagePostService>(this);
            m_asyncReceiverService = registry.RequestModuleInterface<IAsyncMessageRecievedService>();
        }

        public void FinishedStartup()
        {
        }

        /// <summary>
        /// Post a new message to the given region by region handle
        /// </summary>
        /// <param name="RegionHandle"></param>
        /// <param name="request"></param>
        public void Post(ulong RegionHandle, OSDMap request)
        {
            m_asyncReceiverService.FireMessageReceived(request);
        }
    }
}
