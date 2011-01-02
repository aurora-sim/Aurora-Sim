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
    public class CapsService : ICapsService, IService
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// A list of all clients and their Client Caps Handlers
        /// </summary>
        protected Dictionary<UUID, IClientCapsService> m_CapsServices = new Dictionary<UUID, IClientCapsService>();

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
        public string HostUri
        {
            get { return m_hostName + ":" + m_port; }
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
                IClientCapsService perClient = m_CapsServices[AgentID];
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
        public string CreateCAPS(UUID AgentID, string UrlToInform, string CAPSBase, ulong regionHandle)
        {
            //Now make sure we didn't use an old one or something
            IClientCapsService service = GetOrCreateClientCapsService(AgentID);
            IRegionClientCapsService clientService = service.GetOrCreateCapsService(regionHandle, CAPSBase, UrlToInform);
            return clientService.CapsUrl;
        }

        /// <summary>
        /// Get or create a new Caps Service for the given client
        /// Note: This does not add them to a region if one is created. 
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        public IClientCapsService GetOrCreateClientCapsService(UUID AgentID)
        {
            if (!m_CapsServices.ContainsKey(AgentID))
            {
                PerClientBasedCapsService client = new PerClientBasedCapsService();
                client.Initialise(this, AgentID);
                m_CapsServices.Add(AgentID, client);
            }
            return m_CapsServices[AgentID];
        }

        /// <summary>
        /// Get a Caps Service for the given client
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        public IClientCapsService GetClientCapsService(UUID AgentID)
        {
            if (!m_CapsServices.ContainsKey(AgentID))
                return null;
            return m_CapsServices[AgentID];
        }

        #endregion
    }

    public class PerClientBasedCapsService : IClientCapsService
    {
        protected Dictionary<ulong, IRegionClientCapsService> m_RegionCapsServices = new Dictionary<ulong, IRegionClientCapsService>();
        protected ICapsService m_CapsService;
        protected UUID m_agentID;
        public UUID AgentID
        {
            get { return m_agentID; }
        }

        public IHttpServer Server
        {
            get { return m_CapsService.Server; }
        }

        public IRegistryCore Registry
        {
            get { return m_CapsService.Registry; }
        }

        public String HostUri
        {
            get { return m_CapsService.HostUri; }
        }

        public void Initialise(ICapsService server, UUID agentID)
        {
            m_CapsService = server;
            m_agentID = agentID;
        }

        /// <summary>
        /// Close out all of the CAPS for this user
        /// </summary>
        public void Close()
        {
            foreach (ulong regionHandle in m_RegionCapsServices.Keys)
            {
                m_RegionCapsServices[regionHandle].Close();
            }
            m_RegionCapsServices.Clear();
        }

        /// <summary>
        /// Add a new Caps Service for the given region if one does not already exist
        /// </summary>
        /// <param name="regionHandle"></param>
        protected void AddCapsServiceForRegion(ulong regionHandle, string CAPSBase, string UrlToInform)
        {
            if (!m_RegionCapsServices.ContainsKey(regionHandle))
            {
                PerRegionClientCapsService regionClient = new PerRegionClientCapsService();
                regionClient.Initialise(this, regionHandle, CAPSBase, UrlToInform);
                m_RegionCapsServices.Add(regionHandle, regionClient);
            }
        }

        /// <summary>
        /// Attempt to find the CapsService for the given user/region
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="agentID"></param>
        /// <returns></returns>
        public IRegionClientCapsService GetCapsService(ulong regionID)
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
        public IRegionClientCapsService GetOrCreateCapsService(ulong regionID, string CAPSBase, string UrlToInform)
        {
            //If one already exists, don't add a new one
            if (m_RegionCapsServices.ContainsKey(regionID))
                return m_RegionCapsServices[regionID];
            //Create a new one, and then call Get to find it
            AddCapsServiceForRegion(regionID, CAPSBase, UrlToInform);
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
            m_RegionCapsServices[regionHandle].Close();
            m_RegionCapsServices.Remove(regionHandle);
        }
    }

    public class PerRegionClientCapsService : IRegionClientCapsService
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<ICapsServiceConnector> m_connectors = new List<ICapsServiceConnector>();

        protected ulong m_regionHandle;
        public ulong RegionHandle
        {
            get { return m_regionHandle; }
        }
        protected IClientCapsService m_clientCapsService;
        public IClientCapsService ClientCaps
        {
            get { return m_clientCapsService; }
        }

        public IRegistryCore Registry
        {
            get { return m_clientCapsService.Registry; }
        }
        protected IHttpServer Server
        {
            get { return m_clientCapsService.Server; }
        }
        public UUID AgentID
        {
            get { return m_clientCapsService.AgentID; }
        }

        protected OSDMap m_InfoToSendToUrl = new OSDMap();
        /// <summary>
        /// This OSDMap is sent to the url set in UrlToInform below when telling it about the new Cap request
        /// </summary>
        public OSDMap InfoToSendToUrl
        {
            get { return m_InfoToSendToUrl; }
            set { m_InfoToSendToUrl = value; }
        }

        private string m_UrlToInform;
        /// <summary>
        /// An optional Url that will be called to retrieve more Caps for the client.
        /// </summary>
        public string UrlToInform
        {
            get { return m_UrlToInform; }
            set { m_UrlToInform = value; }
        }

        /// <summary>
        /// This is the /CAPS/UUID 0000/ string
        /// </summary>
        protected String m_capsUrlBase;
        /// <summary>
        /// This is the full URL to the Caps SEED request
        /// </summary>
        public String CapsUrl
        {
            get { return m_clientCapsService.HostUri + m_capsUrlBase; }
        }

        public String HostUri
        {
            get { return m_clientCapsService.HostUri; }
        }

        #endregion

        #region Initialise/Close

        public void Initialise(IClientCapsService clientCapsService, ulong regionHandle, string capsBase, string urlToInform)
        {
            m_clientCapsService = clientCapsService;
            m_capsUrlBase = capsBase;
            m_UrlToInform = urlToInform;
            m_regionHandle = regionHandle;

            AddCAPS();
        }

        public void Close()
        {
            RemoveCAPS();
        }

        #endregion

        #region Add/Remove known caps

        protected void AddCAPS()
        {
            List<ICapsServiceConnector> connectors = GetServiceConnectors();
            foreach (ICapsServiceConnector connector in connectors)
            {
                connector.RegisterCaps(this);
            }
            //Add our SEED cap
            AddStreamHandler("SEED", new RestStreamHandler("POST", m_capsUrlBase, CapsRequest));
        }

        protected void RemoveCAPS()
        {
            List<ICapsServiceConnector> connectors = GetServiceConnectors();
            foreach (ICapsServiceConnector connector in connectors)
            {
                connector.DeregisterCaps();
            }
            //Remove our SEED cap
            RemoveStreamHandler("SEED", "POST", CapsUrl);
        }

        public List<ICapsServiceConnector> GetServiceConnectors()
        {
            if (m_connectors.Count == 0)
            {
                m_connectors = AuroraModuleLoader.PickupModules<ICapsServiceConnector>();
            }
            return m_connectors;
        }

        #endregion

        #region SEED cap request

        protected string CapsRequest(string request, string path, string param,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            try
            {
                if (UrlToInform != "")
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                            UrlToInform,
                            OSDParser.SerializeLLSDXmlString(m_InfoToSendToUrl));
                    m_log.Debug("[CAPSService]: Seed request was added for region " + UrlToInform + " at " + CapsUrl);
                    if (reply != "")
                    {
                        OSDMap hash = (OSDMap)OSDParser.DeserializeLLSDXml(Utils.StringToBytes(reply));
                        foreach (string key in hash.Keys)
                        {
                            if (key == null || hash[key] == null)
                                continue;
                            if (!registeredCAPS.ContainsKey(key))
                                registeredCAPS[key] = hash[key].AsString();
                        }
                    }
                }
            }
            catch
            {
            }
            return OSDParser.SerializeLLSDXmlString(registeredCAPS);
        }

        #endregion

        #region Add/Remove Caps from the known caps OSDMap

        //X cap name to path
        protected OSDMap registeredCAPS = new OSDMap();
        //Paths to X cap
        protected OSDMap registeredCAPSPath = new OSDMap();

        public string CreateCAPS(string method, string appendedPath)
        {
            string caps = "/CAPS/" + method + "/" + UUID.Random() + appendedPath + "/";
            return caps;
        }

        protected void AddCAPS(string method, string caps)
        {
            if (method == null || caps == null)
                return;
            string CAPSPath = this.m_clientCapsService.HostUri + caps;
            registeredCAPS[method] = CAPSPath;
            registeredCAPSPath[CAPSPath] = method;
        }

        protected void RemoveCaps(string method)
        {
            OSD CapsPath = "";
            if (!registeredCAPS.TryGetValue(method, out CapsPath))
                return;
            registeredCAPS.Remove(method);
            registeredCAPSPath.Remove(CapsPath.AsString());
        }

        #endregion

        #region Overriden Http Server methods

        public void AddStreamHandler(string method, IRequestHandler handler)
        {
            Server.AddStreamHandler(handler);
            AddCAPS(method, handler.Path);
        }

        public void RemoveStreamHandler(string method, string httpMethod, string path)
        {
            Server.RemoveStreamHandler(httpMethod, path);
            RemoveCaps(method);
        }

        #endregion
    }
}
