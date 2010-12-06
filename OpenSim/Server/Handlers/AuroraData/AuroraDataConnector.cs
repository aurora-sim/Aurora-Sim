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
    public class AuroraDataServiceConnector : IServiceConnector, IService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public string Name
        {
            get { return GetType().Name; }
        }

        //IService below, this gets loaded first and sets up AuroraData for the whole instance
        #region IService

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AuroraDataHandler", "") != Name)
                return;

            LocalDataService LDS = new Aurora.Services.DataService.LocalDataService();
            LDS.Initialise(config);
            registry.RegisterInterface<LocalDataService>(LDS);
        }

        public void PostInitialize(IRegistryCore registry)
        {
        }

        #endregion

        //This is IServiceConnector and it is loaded second and sets up the remote connections
        public void Initialize(IConfigSource config, ISimulationBase simBase, string configName, IRegistryCore sim)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AuroraDataHandler", Name) != Name)
                return;
            IHttpServer server = simBase.GetHttpServer((uint)handlerConfig.GetInt("AuroraDataHandlerPort"));

            m_log.Debug("[AuroraDataConnectors]: Starting...");

            server.AddStreamHandler(new AuroraDataServerPostHandler());
        }
    }
}
