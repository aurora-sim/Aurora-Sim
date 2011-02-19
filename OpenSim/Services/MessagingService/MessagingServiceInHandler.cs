using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.MessagingService
{
    public class MessagingServiceInHandler : IService, IAsyncMessageRecievedService, IGridRegistrationUrlModule
    {
        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("MessagingServiceInHandler", "") != Name)
                return;
            registry.RegisterModuleInterface<IAsyncMessageRecievedService>(this);
        }

        private IRegistryCore m_registry;
        private uint m_port = 0;
        public string Name
        {
            get { return GetType().Name; }
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("MessagingServiceInHandler", "") != Name)
                return;

            m_registry = registry;
            m_port = handlerConfig.GetUInt("MessagingServiceInHandlerPort");

            if (handlerConfig.GetBoolean("UnsecureUrls", false))
            {
                string url = "/messagingservice";

                IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(m_port);

                server.AddStreamHandler(new MessagingServiceInPostHandler(url, registry, this, 0));
            }
            m_registry.RequestModuleInterface<IGridRegistrationService>().RegisterModule(this);
        }

        public void FinishedStartup()
        {
        }

        #region IAsyncMessageRecievedService Members

        public event MessageReceived OnMessageReceived;

        #endregion

        public OSDMap FireMessageReceived(OSDMap message)
        {
            OSDMap result = null;
            if (OnMessageReceived != null)
            {
                foreach (MessageReceived messagedelegate in OnMessageReceived.GetInvocationList())
                {
                    OSDMap r = messagedelegate(message);
                    if (r != null)
                        result = r;
                }
            }
            return result;
        }

        #region IGridRegistrationUrlModule Members

        public string UrlName
        {
            get { return "MessagingServerURI"; }
        }

        public uint Port
        {
            get { return m_port; }
        }

        public void AddExistingUrlForClient(UUID SessionID, ulong RegionHandle, string url)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(m_port);

            server.AddStreamHandler(new MessagingServiceInPostHandler(url, m_registry, this, RegionHandle));
        }

        public string GetUrlForRegisteringClient(UUID SessionID, ulong RegionHandle)
        {
            string url = "/messagingservice" + UUID.Random();

            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(m_port);

            server.AddStreamHandler(new MessagingServiceInPostHandler(url, m_registry, this, RegionHandle));

            return url;
        }

        #endregion
    }
}
