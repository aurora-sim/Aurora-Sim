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
        protected IAsyncMessageRecievedService m_asyncService;

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
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_asyncService = registry.RequestModuleInterface<IAsyncMessageRecievedService>();
            m_hosts = registry.RequestModuleInterface<IConfigurationService>().FindValueOf("MessagingServerURI");
        }

        #region ISyncMessagePosterService Members

        public void Post(OSDMap request)
        {
            OSDMap message = CreateWebRequest(request);
            m_asyncService.FireMessageReceived(message);
        }

        public OSDMap Get(OSDMap request)
        {
            OSDMap message = CreateWebRequest(request);
            return m_asyncService.FireMessageReceived(message);
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
