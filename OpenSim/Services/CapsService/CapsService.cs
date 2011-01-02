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
using OpenSim.Framework.Capabilities;

using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.CapsService
{
    /// <summary>
    /// This handles requests from the user server about clients that need a CAPS seed URL.
    /// </summary>
    public class CapsService : ICapsService, IService
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected IHttpServer m_server;
        protected IGridUserService m_GridUserService;
        protected IGridService m_GridService;
        protected IPresenceService m_PresenceService;
        protected IInventoryService m_InventoryService;
        protected ILibraryService m_LibraryService;
        protected IAssetService m_AssetService;
        protected string m_hostName;
        protected uint m_port;
        public string HostURI
        {
            get { return m_hostName + ":" + m_port; }
        }
        protected Dictionary<UUID, Dictionary<ulong, IPrivateCapsService>> m_CapsServices = new Dictionary<UUID, Dictionary<ulong, IPrivateCapsService>>();
        
        #endregion

        #region IService members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_log.Debug("[AuroraCAPSService]: Starting...");
            IConfig m_CAPSServerConfig = config.Configs["CAPSService"];
            if (m_CAPSServerConfig == null)
                throw new Exception(String.Format("No section CAPSService in config file"));

            m_hostName = m_CAPSServerConfig.GetString("HostName", String.Empty);
            //Sanitize the results, remove / and :
            m_hostName = m_hostName.EndsWith("/") ? m_hostName.Remove(m_hostName.Length - 1) : m_hostName;
            
            m_port = m_CAPSServerConfig.GetUInt("Port", m_port);
            registry.RegisterModuleInterface<ICapsService>(this);
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostStart(IConfigSource config, IRegistryCore registry)
        {
            m_InventoryService = registry.RequestModuleInterface<IInventoryService>();
            m_LibraryService = registry.RequestModuleInterface<ILibraryService>();
            m_GridUserService = registry.RequestModuleInterface<IGridUserService>();
            m_PresenceService = registry.RequestModuleInterface<IPresenceService>();
            m_GridService = registry.RequestModuleInterface<IGridService>();
            m_AssetService = registry.RequestModuleInterface<IAssetService>();
            ISimulationBase simBase = registry.RequestModuleInterface<ISimulationBase>();
            m_server = simBase.GetHttpServer(m_port);
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterModuleInterface<ICapsService>(this);
        }

        #endregion

        #region ICapsService members

        #region Remove user/region CAPS handlers

        /// <summary>
        /// Remove the all of the user's CAPS from the system
        /// </summary>
        /// <param name="AgentID"></param>
        public void RemoveCAPS(UUID AgentID)
        {
            if(m_CapsServices.ContainsKey(AgentID))
            {
                List<ulong> regionHandles = new List<ulong>(m_CapsServices[AgentID].Keys);
                foreach (ulong regionHandle in regionHandles)
                {
                    RemoveCAPS(AgentID, regionHandle);
                }
                m_CapsServices.Remove(AgentID);
            }
        }

        /// <summary>
        /// Remove the CAPS for the given user in the given region
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="regionHandle"></param>
        public void RemoveCAPS(UUID AgentID, ulong regionHandle)
        {
            //Remove all the CAPS handlers
            m_CapsServices[AgentID][regionHandle].RemoveCAPS();
            //Remove the SEED cap
            m_server.RemoveStreamHandler("POST", m_CapsServices[AgentID][regionHandle].CapsURL);
            m_CapsServices[AgentID].Remove(regionHandle);
        }

        #endregion

        #region Create user/region CAPS handlers

        /// <summary>
        /// Create a Caps URL for the given user/region. Called normally by the EventQueueService or the LLLoginService on login
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="SimCAPS"></param>
        /// <param name="CAPS"></param>
        /// <param name="regionHandle"></param>
        /// <returns></returns>
        public string CreateCAPS(UUID AgentID, string SimCAPS, string CAPS, string CAPSPath, ulong regionHandle)
        {
            //Add the HostURI so that it ends up here
            string CAPSBase = CAPS;
            CAPS = HostURI + CAPS;
            //This makes the new SEED url on the CAPS server
            AddCapsService(new PrivateCapsService(m_server, m_InventoryService, m_LibraryService, 
                m_GridUserService, m_GridService, m_PresenceService, m_AssetService, SimCAPS,
                AgentID, regionHandle, this, CAPS, CAPSBase, CAPSPath));
            //Now make sure we didn't use an old one or something
            IPrivateCapsService service = GetCapsService(regionHandle, AgentID);
            return service.CapsURL;
        }

        /// <summary>
        /// Add a new CapsService if the CapsService doesn't already exist for the given user/region
        /// </summary>
        /// <param name="handler"></param>
        public void AddCapsService(IPrivateCapsService handler)
        {
            if (!m_CapsServices.ContainsKey(handler.AgentID))
                m_CapsServices.Add(handler.AgentID, new Dictionary<ulong, IPrivateCapsService>());
            if (!m_CapsServices[handler.AgentID].ContainsKey(handler.RegionHandle))
            {
                //It doesn't exist, add it
                m_CapsServices[handler.AgentID][handler.RegionHandle] = handler;
                handler.Initialise();
                m_server.AddStreamHandler(new RestStreamHandler("POST", handler.CapsBase, handler.CapsRequest));
            }
        }

        #endregion

        #region Get user/region CapsService handlers

        /// <summary>
        /// Attempt to find the CapsService for the given user/region
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="agentID"></param>
        /// <returns></returns>
        public IPrivateCapsService GetCapsService(ulong regionID, UUID agentID)
        {
            if (m_CapsServices.ContainsKey(agentID) && m_CapsServices[agentID].ContainsKey(regionID))
                return m_CapsServices[agentID][regionID];
            return null;
        }

        #endregion

        #endregion
    }

    public class NewCapsService : ICapsService/*, IService*/
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected IRegistryCore m_registry;
        public IRegistryCore Registry
        {
            get { return m_registry; }
        }
        protected IHttpServer m_server;
        public IHttpServer Server
        {
            get { return m_server; }
        }
        protected string m_hostName;
        protected uint m_port;
        public string HostURI
        {
            get { return m_hostName + ":" + m_port; }
        }
        protected Dictionary<UUID, PerClientBasedCapsService> m_CapsServices = new Dictionary<UUID, PerClientBasedCapsService>();
        
        #endregion

        #region IService members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_log.Debug("[AuroraCAPSService]: Starting...");
            IConfig m_CAPSServerConfig = config.Configs["CAPSService"];
            if (m_CAPSServerConfig == null)
                throw new Exception(String.Format("No section CAPSService in config file"));

            m_hostName = m_CAPSServerConfig.GetString("HostName", String.Empty);
            //Sanitize the results, remove / and :
            m_hostName = m_hostName.EndsWith("/") ? m_hostName.Remove(m_hostName.Length - 1) : m_hostName;
            
            m_port = m_CAPSServerConfig.GetUInt("Port", m_port);
            m_registry = registry;
            registry.RegisterModuleInterface<ICapsService>(this);
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostStart(IConfigSource config, IRegistryCore registry)
        {
            ISimulationBase simBase = registry.RequestModuleInterface<ISimulationBase>();
            m_server = simBase.GetHttpServer(m_port);
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterModuleInterface<ICapsService>(this);
        }

        #endregion

        #region ICapsService members

        /// <summary>
        /// Remove the all of the user's CAPS from the system
        /// </summary>
        /// <param name="AgentID"></param>
        public void RemoveCAPS(UUID AgentID)
        {
            if(m_CapsServices.ContainsKey(AgentID))
            {
                PerClientBasedCapsService perClient = m_CapsServices[AgentID];
                perClient.Close();
                m_CapsServices.Remove(AgentID);
            }
        }

        /// <summary>
        /// Create a Caps URL for the given user/region. Called normally by the EventQueueService or the LLLoginService on login
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="SimCAPS"></param>
        /// <param name="CAPS"></param>
        /// <param name="regionHandle"></param>
        /// <returns></returns>
        public string CreateCAPS(UUID AgentID, string SimCAPS, string CAPS, string CAPSPath, ulong regionHandle)
        {
            //Add the HostURI so that it ends up here
            string CAPSBase = CAPS;
            CAPS = HostURI + CAPS;
            //This makes the new SEED url on the CAPS server
            
            //Now make sure we didn't use an old one or something
            PerClientBasedCapsService service = GetOrCreateClientCapsService(AgentID);
            PerRegionClientCapsService clientService = service.GetOrCreateCapsService(regionHandle);
            return clientService.CapsUrl;
        }

        /// <summary>
        /// Get or create a new Caps Service for the given client
        /// Note: This does not add them to a region if one is created. 
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        public PerClientBasedCapsService GetOrCreateClientCapsService(UUID AgentID)
        {
            if (!m_CapsServices.ContainsKey(AgentID))
            {
                PerClientBasedCapsService client = new PerClientBasedCapsService();
                client.Initialise(this, AgentID);
                m_CapsServices.Add(AgentID, client);
            }
            return m_CapsServices[AgentID];
        }

        #endregion
    }

    public class PerClientBasedCapsService
    {
        protected Dictionary<ulong, PerRegionClientCapsService> m_RegionCapsServices = new Dictionary<ulong, PerRegionClientCapsService>();
        protected NewCapsService m_CapsService;
        protected IHttpServer m_server;
        protected UUID m_agentID;
        public UUID AgentID
        {
            get { return m_agentID; }
        }

        public void Initialise(NewCapsService server, UUID agentID)
        {
            m_CapsService = server;
            m_agentID = agentID;
            m_server = server.Server;
        }

        /// <summary>
        /// Add a new Caps Service for the given region if one does not already exist
        /// </summary>
        /// <param name="regionHandle"></param>
        public void AddCapsServiceForRegion(ulong regionHandle)
        {
            if (!m_RegionCapsServices.ContainsKey(regionHandle))
            {
                PerRegionClientCapsService regionClient = new PerRegionClientCapsService();
                regionClient.Initialise(m_CapsService.Registry, m_CapsService.Server, this);
                m_RegionCapsServices.Add(regionHandle, regionClient);
            }
        }

        /// <summary>
        /// Attempt to find the CapsService for the given user/region
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="agentID"></param>
        /// <returns></returns>
        public PerRegionClientCapsService GetCapsService(ulong regionID)
        {
            if (m_RegionCapsServices.ContainsKey(regionID))
                return m_RegionCapsServices[regionID];
            return null;
        }

        /// <summary>
        /// Find, or create if one does not exist, a Caps Service for the given region
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        public PerRegionClientCapsService GetOrCreateCapsService(ulong regionID)
        {
            //If one already exists, don't add a new one
            if (m_RegionCapsServices.ContainsKey(regionID))
                return m_RegionCapsServices[regionID];
            //Create a new one, and then call Get to find it
            AddCapsServiceForRegion(regionID);
            return GetCapsService(regionID);
        }

        /// <summary>
        /// Remove the CAPS for the given user in the given region
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="regionHandle"></param>
        public void RemoveCAPS(ulong regionHandle)
        {
            if (!m_RegionCapsServices.ContainsKey(regionHandle))
                return;
            //Remove all the CAPS handlers
            m_RegionCapsServices[regionHandle].RemoveCAPS();
            //Remove the SEED cap
            m_CapsService.Server.RemoveStreamHandler("POST", m_RegionCapsServices[regionHandle].CapsUrl);
            m_RegionCapsServices.Remove(regionHandle);
        }

        /// <summary>
        /// Close out all of the CAPS for this user
        /// </summary>
        public void Close()
        {
            foreach (ulong regionHandle in m_RegionCapsServices.Keys)
            {
                m_RegionCapsServices[regionHandle].RemoveCAPS();
                //Remove the SEED cap
                m_server.RemoveStreamHandler("POST", m_RegionCapsServices[regionHandle].CapsUrl);
            }
            m_RegionCapsServices.Clear();
        }
    }

    public class PerRegionClientCapsService
    {
        protected IRegistryCore m_registry;
        protected PerClientBasedCapsService m_clientCapsService;
        protected IHttpServer m_server;
        private List<ICapsServiceConnector> m_connectors = new List<ICapsServiceConnector>();
        protected ulong m_regionHandle;
        public UUID AgentID
        {
            get { return m_clientCapsService.AgentID; }
        }

        protected String m_CapsUrl;
        public String CapsUrl
        {
            get { return m_CapsUrl; }
        }

        public void Initialise(IRegistryCore registry, IHttpServer server, PerClientBasedCapsService perClientBasedCapsService)
        {
            m_registry = registry;
            m_clientCapsService = perClientBasedCapsService;
            m_server = server;
        }

        public void AddCAPS()
        {
            List<ICapsServiceConnector> connectors = GetServiceConnectors();
            foreach (ICapsServiceConnector connector in connectors)
            {
                connector.RegisterCaps(this, m_server, m_registry);
            }
            m_server.AddStreamHandler(new RestStreamHandler("POST", handler.CapsBase, handler.CapsRequest));
        }

        public void RemoveCAPS()
        {
            List<ICapsServiceConnector> connectors = GetServiceConnectors();
            foreach (ICapsServiceConnector connector in connectors)
            {
                connector.DeregisterCaps();
            }
            m_server.RemoveStreamHandler("POST", handler.CapsBase);
        }

        public List<ICapsServiceConnector> GetServiceConnectors()
        {
            if (m_connectors.Count == 0)
            {
                m_connectors = AuroraModuleLoader.PickupModules<ICapsServiceConnector>();
            }
            return m_connectors;
        }


        //X cap name to path
        public OSDMap registeredCAPS = new OSDMap();
        //Paths to X cap
        public OSDMap registeredCAPSPath = new OSDMap();

        public string CreateCAPS(string method, string appendedPath)
        {
            string caps = "/CAPS/" + method + "/" + UUID.Random() + appendedPath + "/";
            AddCAPS(method, caps);
            return caps;
        }

        public void AddCAPS(string method, string caps)
        {
            if (method == null || caps == null)
                return;
            string CAPSPath = this.PublicHandler.HostURI + caps;
            registeredCAPS[method] = CAPSPath;
            registeredCAPSPath[CAPSPath] = method;
        }

        public string GetCAPS(string method)
        {
            if (registeredCAPS.ContainsKey(method))
                return registeredCAPS[method].ToString();
            return "";
        }
    }
}
