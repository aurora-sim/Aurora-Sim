using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.MessagingService
{
    public class LocalSyncMessagePosterService : ISyncMessagePosterService, IService
    {
        protected List<string> m_hosts = new List<string>();
        protected IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("SyncMessagePosterServiceHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<ISyncMessagePosterService>(this);
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_hosts = registry.RequestModuleInterface<IConfigurationService>().FindValueOf("MessagingServerURI");
        }

        public void FinishedStartup()
        {
        }

        #region ISyncMessagePosterService Members

        public void Post(OSDMap request)
        {
            m_registry.RequestModuleInterface<IAsyncMessageRecievedService>().FireMessageReceived(request);
        }

        public OSDMap Get(OSDMap request)
        {
            return m_registry.RequestModuleInterface<IAsyncMessageRecievedService>().FireMessageReceived(request);
        }

        private OSDMap CreateWebRequest(OSDMap request)
        {
            OSDMap message = new OSDMap();

            message["Method"] = "SyncPost";
            message["Message"] = request;

            return message;
        }

        #endregion
    }
}
