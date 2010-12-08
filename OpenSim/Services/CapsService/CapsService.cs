using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;
using OpenSim.Framework.Capabilities;
using OpenSim.Services.Base;

using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.CapsService
{
    #region ICapsService

    /// <summary>
    /// This handles requests from the user server about clients that need a CAPS seed URL.
    /// </summary>
    public class CapsService : ICapsService, IService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected IHttpServer m_server;
        protected IGridUserService m_GridUserService;
        protected IGridService m_GridService;
        protected IPresenceService m_PresenceService;
        protected IInventoryService m_InventoryService;
        protected ILibraryService m_LibraryService;
        protected string m_hostName;
        protected uint m_port;
        protected string HostURI
        {
            get { return m_hostName + ":" + m_port; }
        }
        protected Dictionary<UUID, Dictionary<ulong, IPrivateCapsService>> m_CapsServices = new Dictionary<UUID, Dictionary<ulong, IPrivateCapsService>>();
        protected List<ICapsServiceConnector> m_CapsModules = new List<ICapsServiceConnector>();
        public List<ICapsServiceConnector> CapsModules
        {
            get { return m_CapsModules; }
        }
        
        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_log.Debug("[AuroraCAPSService]: Starting...");
            IConfig m_CAPSServerConfig = config.Configs["CAPSService"];
            if (m_CAPSServerConfig == null)
                throw new Exception(String.Format("No section CAPSService in config file"));

            m_hostName = m_CAPSServerConfig.GetString("HostName", String.Empty);
            //Sanitize the results, remove / and :
            m_hostName = m_hostName.EndsWith("/") ? m_hostName.Remove(m_hostName.Length - 1) : m_hostName;
            m_hostName = m_hostName.EndsWith(":") ? m_hostName.Remove(m_hostName.Length - 1) : m_hostName;
            
            m_port = m_CAPSServerConfig.GetUInt("Port", m_port);
            registry.RegisterInterface<ICapsService>(this);
        }

        public void PostInitialize(IRegistryCore registry)
        {
            m_CapsModules = Aurora.Framework.AuroraModuleLoader.PickupModules<ICapsServiceConnector>();
            m_InventoryService = registry.Get<IInventoryService>();
            m_LibraryService = registry.Get<ILibraryService>();
            m_GridUserService = registry.Get<IGridUserService>();
            m_PresenceService = registry.Get<IPresenceService>();
            m_GridService = registry.Get<IGridService>();
            ISimulationBase simBase = registry.Get<ISimulationBase>();
            m_server = simBase.GetHttpServer(m_port);
        }

        public void RemoveCAPS(UUID AgentID)
        {
            m_CapsServices.Remove(AgentID);
        }

        public void RemoveCAPS(UUID AgentID, ulong regionHandle)
        {
            m_CapsServices[AgentID].Remove(regionHandle);
        }

        public void CreateCAPS(UUID AgentID, string SimCAPS, string CAPS, ulong regionHandle)
        {
            //Add the HostURI so that it ends up here
            CAPS = HostURI + CAPS;
            //This makes the new SEED url on the CAPS server
            AddCapsService(new PrivateCapsService(m_server, m_InventoryService, m_LibraryService, m_GridUserService, m_GridService, m_PresenceService, SimCAPS, AgentID, m_hostName, true, regionHandle, this, CAPS), CAPS, AgentID);
        }

        public IPrivateCapsService GetCapsService(ulong regionID, UUID agentID)
        {
            if (m_CapsServices.ContainsKey(agentID) && m_CapsServices[agentID].ContainsKey(regionID))
                return m_CapsServices[agentID][regionID];
            return null;
        }

        public void AddCapsService(IPrivateCapsService handler, string CAPS, UUID agentID)
        {
            if (!m_CapsServices.ContainsKey(agentID))
                m_CapsServices.Add(agentID, new Dictionary<ulong,IPrivateCapsService>());
            if (!m_CapsServices[agentID].ContainsKey(handler.RegionHandle))
            {
                m_CapsServices[agentID][handler.RegionHandle] = handler;
                handler.Initialise();
                m_server.AddStreamHandler(new RestStreamHandler("POST", CAPS, handler.CapsRequest));
            }
        }
    }

    #endregion
}
