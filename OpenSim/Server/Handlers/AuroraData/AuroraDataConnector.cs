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
    public class AuroraDataServiceConnector : IServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, ISimulationBase simBase, string configName, IRegistryCore sim)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AuroraDataHandler", "") != Name)
                return;
            IHttpServer server = simBase.GetHttpServer((uint)handlerConfig.GetInt("AuroraDataHandlerPort"));

            m_log.Debug("[AuroraDataConnectors]: Starting...");

            server.AddStreamHandler(new AuroraDataServerPostHandler());
        }
    }
}
