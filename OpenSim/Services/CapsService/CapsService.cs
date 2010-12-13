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
        protected List<ICapsServiceConnector> m_CapsModules = new List<ICapsServiceConnector>();
        public List<ICapsServiceConnector> CapsModules
        {
            get { return m_CapsModules; }
        }

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
            registry.RegisterInterface<ICapsService>(this);
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostStart(IConfigSource config, IRegistryCore registry)
        {
            m_CapsModules = Aurora.Framework.AuroraModuleLoader.PickupModules<ICapsServiceConnector>();
            m_InventoryService = registry.Get<IInventoryService>();
            m_LibraryService = registry.Get<ILibraryService>();
            m_GridUserService = registry.Get<IGridUserService>();
            m_PresenceService = registry.Get<IPresenceService>();
            m_GridService = registry.Get<IGridService>();
            m_AssetService = registry.Get<IAssetService>();
            ISimulationBase simBase = registry.Get<ISimulationBase>();
            m_server = simBase.GetHttpServer(m_port);
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
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
        public string CreateCAPS(UUID AgentID, string SimCAPS, string CAPS, ulong regionHandle)
        {
            //Add the HostURI so that it ends up here
            string CAPSBase = CAPS;
            CAPS = HostURI + CAPS;
            //This makes the new SEED url on the CAPS server
            AddCapsService(new PrivateCapsService(m_server, m_InventoryService, m_LibraryService, m_GridUserService, m_GridService, m_PresenceService, m_AssetService, SimCAPS, AgentID, regionHandle, this, CAPS, CAPSBase));
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
}
