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
using OpenSim.Server.Handlers.Base;

using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Nwc.XmlRpc;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;
 
namespace OpenSim.Server.Handlers.AuroraData
{
    public class AuroraDataServiceConnector : IServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IHttpServer server, string configName, IRegistryCore sim)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AuroraDataHandler", Name) != Name)
                return;

            m_log.Debug("[AuroraDataConnectors]: Starting...");

            LocalDataService LDS = new Aurora.Services.DataService.LocalDataService();
            LDS.Initialise(config);
            server.AddStreamHandler(new AuroraDataServerPostHandler());
        }
    }
}
