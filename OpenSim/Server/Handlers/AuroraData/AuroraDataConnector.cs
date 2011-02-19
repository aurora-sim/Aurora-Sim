using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using log4net;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;

using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Nwc.XmlRpc;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;
 
namespace OpenSim.Server.Handlers.AuroraData
{
    public class AuroraDataServiceConnector : IService, IGridRegistrationUrlModule
    {
        private IRegistryCore m_registry;
        private uint m_port = 0;
        
        public string Name
        {
            get { return GetType().Name; }
        }

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AuroraDataHandler", "") != Name)
                return;

            m_registry = registry;
            m_port = handlerConfig.GetUInt("AuroraDataHandlerPort");

            if (handlerConfig.GetBoolean("UnsecureUrls", false))
            {
                string url = "/auroradata";

                IHttpServer server = registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(m_port);

                server.AddStreamHandler(new AuroraDataServerPostHandler(url));
            }
            m_registry.RequestModuleInterface<IGridRegistrationService>().RegisterModule(this);
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region IGridRegistrationUrlModule Members

        public string UrlName
        {
            get { return "RemoteServerURI"; }
        }

        public uint Port
        {
            get { return m_port; }
        }

        public void AddExistingUrlForClient(UUID SessionID, ulong RegionHandle, string url)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(m_port);
            
            IAssetService m_AssetService = m_registry.RequestModuleInterface<IAssetService>();
            server.AddStreamHandler(new AuroraDataServerPostHandler(url));
        }

        public string GetUrlForRegisteringClient(UUID SessionID, ulong RegionHandle)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(m_port);
            string url = "/auroradata" + UUID.Random();

            IAssetService m_AssetService = m_registry.RequestModuleInterface<IAssetService>();
            server.AddStreamHandler(new AuroraDataServerPostHandler(url));

            return url;
        }

        #endregion
    }
}
