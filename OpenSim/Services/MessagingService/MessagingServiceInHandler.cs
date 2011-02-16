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
    public class MessagingServiceInHandler : IService, IAsyncMessageRecievedService
    {
        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("MessagingServiceInHandler", "") != Name)
                return;
            registry.RegisterModuleInterface<IAsyncMessageRecievedService>(this);
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("MessagingServiceInHandler", "") != Name)
                return;
            IHttpServer server = registry.RequestModuleInterface<ISimulationBase>().GetHttpServer((uint)handlerConfig.GetInt("ConfigurationInHandlerPort"));

            server.AddStreamHandler(new MessagingServiceInPostHandler(registry, this));
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
    }
}
