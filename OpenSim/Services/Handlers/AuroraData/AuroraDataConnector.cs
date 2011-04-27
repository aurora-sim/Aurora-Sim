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

namespace OpenSim.Services
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

        public void AddExistingUrlForClient (string SessionID, ulong RegionHandle, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);

            server.AddStreamHandler (new AuroraDataServerPostHandler (url, RegionHandle, m_registry));
            server.AddStreamHandler (new AuroraDataServerPostOSDHandler (url + "osd", RegionHandle, m_registry));
        }

        public string GetUrlForRegisteringClient (string SessionID, ulong RegionHandle, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);
            string url = "/auroradata" + UUID.Random ();

            server.AddStreamHandler (new AuroraDataServerPostHandler (url, RegionHandle, m_registry));
            server.AddStreamHandler (new AuroraDataServerPostOSDHandler (url + "osd", RegionHandle, m_registry));

            return url;
        }

        public void RemoveUrlForClient (ulong regionHandle, string sessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);
            server.RemoveHTTPHandler("POST", url);
            server.RemoveHTTPHandler("POST", url + "osd");
        }

        #endregion
    }
}
