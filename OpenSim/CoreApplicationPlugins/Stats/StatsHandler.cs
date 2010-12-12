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
        private string userStatsURI = "";
        private ISimulationBase m_openSim;
        public void Initialize(ISimulationBase openSim)
        {
            m_openSim = openSim;

            IConfig handlerConfig = openSim.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("StatsHandler", "") != Name)
                return;

            IConfig statsConfig = openSim.ConfigSource.Configs["Stats"];
            if (statsConfig != null)
                userStatsURI = statsConfig.GetString("Stats_URI", String.Empty);
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
            IConfig handlerConfig = m_openSim.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("StatsHandler", "") != Name)
                return;

            MainServer.Instance.AddStreamHandler(new SimStatusHandler());
            if (userStatsURI != String.Empty)
                MainServer.Instance.AddStreamHandler(new UXSimStatusHandler(m_openSim, userStatsURI));
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

        /// <summary>
        /// Handler to supply the current status of this sim
        /// </summary>
        /// Currently this is always OK if the simulator is still listening for connections on its HTTP service
        public class SimStatusHandler : IStreamedRequestHandler
        {
            public byte[] Handle(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return Util.UTF8.GetBytes("OK");
            }

            public string ContentType
            {
                get { return "text/plain"; }
            }

            public string HttpMethod
            {
                get { return "GET"; }
            }

            public string Path
            {
                get { return "/simstatus/"; }
            }
        }

        /// <summary>
        /// Handler to supply the current extended status of this sim to a user configured URI
        /// Sends the statistical data in a json serialization 
        /// If the request contains a key, "callback" the response will be wrappend in the 
        /// associated value for jsonp used with ajax/javascript
        /// </summary>
        public class UXSimStatusHandler : IStreamedRequestHandler
        {
            ISimulationBase m_opensim;
            string osUXStatsURI = String.Empty;

            public UXSimStatusHandler(ISimulationBase sim, string userStatsURI)
            {
                m_opensim = sim;
                osUXStatsURI = userStatsURI;
            }

            public byte[] Handle(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return Util.UTF8.GetBytes(StatReport(httpRequest));
            }

            public string StatReport(OSHttpRequest httpRequest)
            {
                // If we catch a request for "callback", wrap the response in the value for jsonp
                if (httpRequest.Query.ContainsKey("callback"))
                {
                    return httpRequest.Query["callback"].ToString() + "(" + m_opensim.Stats.XReport((DateTime.Now - m_opensim.StartupTime).ToString(), m_opensim.Version) + ");";
                }
                else
                {
                    return m_opensim.Stats.XReport((DateTime.Now - m_opensim.StartupTime).ToString(), m_opensim.Version);
                }
            }

            public string ContentType
            {
                get { return "text/plain"; }
            }

            public string HttpMethod
            {
                get { return "GET"; }
            }

            public string Path
            {
                // This is for the OpenSimulator instance and is the user provided URI 
                get { return "/" + osUXStatsURI + "/"; }
            }
        }
    }
}
