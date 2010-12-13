using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Net;
using System.Reflection;
using System.Timers;
using System.Threading;
using log4net;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenSim;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;

namespace OpenSim.CoreApplicationPlugins
{
    public class StatsHandler : IApplicationPlugin
    {
        private ISimulationBase m_openSim;
        public void Initialize(ISimulationBase openSim)
        {
            m_openSim = openSim;
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
        }

        public void PostStart()
        {
            IConfig handlerConfig = m_openSim.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("StatsHandler", "") != Name)
                return;
        }

        public string Name
        {
            get { return "StatsHandler"; }
        }

        public void Close()
        {
        }

        public void Dispose()
        {
        }
    }
}
