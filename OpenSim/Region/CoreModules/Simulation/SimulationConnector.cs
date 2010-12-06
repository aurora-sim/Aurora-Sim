using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using OpenSim.Framework;

using OpenSim.Region.CoreModules.Avatar.Inventory.Archiver;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using Aurora.Simulation.Base;

using OpenMetaverse;
using log4net;
using Nini.Config;
using OpenSim.Framework.Servers.HttpServer;

namespace OpenSim.Region.CoreModules
{
    public class SimulationConnector : ISharedRegionModule, ISimulationBase
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected Dictionary<uint, BaseHttpServer> m_Servers =
            new Dictionary<uint, BaseHttpServer>();

        public void PostInitialise()
        {
        }

        public string Name
        {
            get { return "SimulationConnector"; }
        }

        public Type ReplaceableInterface
        {
            get {return null; }
        }

        public void Initialise(IConfigSource source)
        {
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            scene.RegisterInterface<ISimulationBase>(this);
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public IHttpServer GetHttpServer(uint port)
        {
            m_log.InfoFormat("[SERVER]: Requested port {0}", port);

            if (port == 0)
                return MainServer.Instance;

            if (m_Servers.ContainsKey(port))
                return m_Servers[port];

            m_Servers[port] = new BaseHttpServer(port);

            m_log.InfoFormat("[SERVER]: Starting new HTTP server on port {0}", port);
            m_Servers[port].Start();

            return m_Servers[port];
        }
    }
}
