using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenSim.Server.Base;
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
    public class AuroraDataServiceConnector : ServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Aurora.Framework.IProfileConnector ProfileFrontend = null;

        public AuroraDataServiceConnector(IConfigSource config, IHttpServer server, string configName) :
                base(config, server, configName)
        {
            m_log.Debug("[AuroraDataConnectors]: Starting...");

            LocalDataService LDS = new Aurora.Services.DataService.LocalDataService();
            LDS.Initialise(config);
            ProfileFrontend = DataManager.IProfileConnector;
            server.AddStreamHandler(new AuroraDataServerPostHandler());
        }
    }
}
