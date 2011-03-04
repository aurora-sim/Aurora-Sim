using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.GridService
{
    public class GridRegistrationService : IService, IGridRegistrationService
    {
        #region Declares

        protected Dictionary<string, IGridRegistrationUrlModule> m_modules = new Dictionary<string, IGridRegistrationUrlModule>();
        protected LoadBalancerUrls m_loadBalancer = new LoadBalancerUrls();
        protected IGenericsConnector m_genericsConnector;
        protected ISimulationBase m_simulationBase;
        protected IConfig m_permissionConfig;
        protected IRegistryCore m_registry;
        /// <summary>
        /// Timeout before the handlers expire (in hours)
        /// </summary>
        protected int m_timeBeforeTimeout = 24;
        public int ExpiresTime 
        { 
            get
            {
                return m_timeBeforeTimeout; 
            }
        }

        protected ThreatLevel m_defaultRegionThreatLevel = ThreatLevel.Full;
        protected Dictionary<ThreatLevel, PermissionSet> Permissions = new Dictionary<ThreatLevel, PermissionSet>();

        protected class PermissionSet
        {
            private string[] PermittedFunctions;
            private IRegistryCore m_registry;

            public PermissionSet(IRegistryCore registry)
            {
                m_registry = registry;
            }

            public void ReadFunctions(IConfig config, ThreatLevel threatLevel)
            {
                string list = config.GetString("Threat_Level_" + threatLevel.ToString(), "");
                if (list != "")
                    PermittedFunctions = list.Split(' ');
            }

            public bool CheckPermission(string function, ulong RegionHandle)
            {
                if (PermittedFunctions.Contains(function))
                {
                    return true;
                }
                return false;
            }
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterModuleInterface<IGridRegistrationService>(this);
            m_registry = registry;
            m_simulationBase = registry.RequestModuleInterface<ISimulationBase>();

            m_loadBalancer.SetUrls(config.Configs["Configuration"].GetString("HostNames", "http://localhost").Split(','));

            m_permissionConfig = config.Configs["RegionPermissions"];
            if (m_permissionConfig != null)
                ReadConfiguration(m_permissionConfig);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
            m_registry.RequestModuleInterface<IAsyncMessageRecievedService>().OnMessageReceived += OnMessageReceived;
            m_genericsConnector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            LoadFromDatabase();
        }
        
        /// <summary>
        /// We do handle the RegisterHandlers message here, as we deal with all of the handlers in this module
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        OSDMap OnMessageReceived(OSDMap message)
        {
            if (message.ContainsKey("Method") && message["Method"] == "RegisterHandlers")
            {
                ulong regionHandle = message["RegionHandle"].AsULong();
                if (CheckThreatLevel("", regionHandle, "RegisterHandlers", ThreatLevel.None))
                {
                    UpdateUrlsForClient(regionHandle);
                }
            }
            return null;
        }

        protected void ReadConfiguration(IConfig config)
        {
            m_timeBeforeTimeout = config.GetInt("DefaultTimeout", m_timeBeforeTimeout);
            m_defaultRegionThreatLevel = (ThreatLevel)Enum.Parse(typeof(ThreatLevel), config.GetString("DefaultRegionThreatLevel", m_defaultRegionThreatLevel.ToString()));

            PermissionSet nonePermissions = new PermissionSet(m_registry);
            nonePermissions.ReadFunctions(config, ThreatLevel.None);
            Permissions.Add(ThreatLevel.None, nonePermissions);

            PermissionSet lowPermissions = new PermissionSet(m_registry);
            lowPermissions.ReadFunctions(config, ThreatLevel.Low);
            Permissions.Add(ThreatLevel.Low, lowPermissions);

            PermissionSet mediumPermissions = new PermissionSet(m_registry);
            mediumPermissions.ReadFunctions(config, ThreatLevel.Medium);
            Permissions.Add(ThreatLevel.Medium, mediumPermissions);

            PermissionSet highPermissions = new PermissionSet(m_registry);
            highPermissions.ReadFunctions(config, ThreatLevel.High);
            Permissions.Add(ThreatLevel.High, highPermissions);

            PermissionSet fullPermissions = new PermissionSet(m_registry);
            fullPermissions.ReadFunctions(config, ThreatLevel.Full);
            Permissions.Add(ThreatLevel.Full, fullPermissions);
        }

        protected ThreatLevel FindThreatLevelForFunction(string function, ThreatLevel defaultThreatLevel, ulong RegionHandle)
        {
            GridRegion region = FindRegion(RegionHandle);
            if (region == null)
                return ThreatLevel.None;
            string rThreat = region.GenericMap["ThreatLevel"].AsString();
            ThreatLevel regionThreatLevel = m_defaultRegionThreatLevel;
            if (rThreat != "")
                regionThreatLevel = (ThreatLevel)Enum.Parse(typeof(ThreatLevel), rThreat);

            string permission = m_permissionConfig.GetString(function, "");
            if (permission == "")
                return FindMoreRestrictiveThreatLevel(defaultThreatLevel, regionThreatLevel);
            ThreatLevel permissionThreatLevel = (ThreatLevel)Enum.Parse(typeof(ThreatLevel), permission);
            return FindMoreRestrictiveThreatLevel(permissionThreatLevel, regionThreatLevel);
        }

        protected ThreatLevel FindMoreRestrictiveThreatLevel(ThreatLevel a, ThreatLevel b)
        {
            return a < b ? a : b;
        }

        protected PermissionSet FindPermissionsForThreatLevel(ThreatLevel threatLevel)
        {
            if (Permissions.ContainsKey(threatLevel))
                return Permissions[threatLevel];
            else
                return Permissions[ThreatLevel.None];
        }

        private GridRegion FindRegion(ulong RegionHandle)
        {
            int x, y;
            Util.UlongToInts(RegionHandle, out x, out y);
            return m_registry.RequestModuleInterface<IGridService>().GetRegionByPosition(UUID.Zero, x, y);
        }

        protected void LoadFromDatabase()
        {
            List<GridRegistrationURLs> urls = m_genericsConnector.GetGenerics<GridRegistrationURLs>(
                UUID.Zero, "GridRegistrationUrls", new GridRegistrationURLs());

            foreach (GridRegistrationURLs url in urls)
            {
                foreach (IGridRegistrationUrlModule module in m_modules.Values)
                {
                    module.AddExistingUrlForClient(url.SessionID.ToString(), url.RegionHandle, url.URLS[module.UrlName]);
                }
            }
        }

        #endregion

        #region IGridRegistrationService Members

        public OSDMap GetUrlForRegisteringClient(string SessionID, ulong RegionHandle)
        {
            OSDMap databaseSave = new OSDMap();
            //Get the URLs from all the modules that have registered with us
            foreach (IGridRegistrationUrlModule module in m_modules.Values)
            {
                //Build the URL
                databaseSave[module.UrlName] = module.GetUrlForRegisteringClient(SessionID, RegionHandle);
            }
            OSDMap retVal = new OSDMap();
            foreach (KeyValuePair<string, OSD> module in databaseSave)
            {
                //Build the URL
                retVal[module.Key] = m_loadBalancer.GetHost() + ":" + m_modules[module.Key].Port + module.Value.AsString();
            }

            //Save into the database so that we can rebuild later if the server goes offline
            GridRegistrationURLs urls = new GridRegistrationURLs();
            urls.URLS = databaseSave;
            urls.RegionHandle = RegionHandle;
            urls.SessionID = SessionID;
            urls.Expiration = DateTime.Now.AddHours(m_timeBeforeTimeout);
            m_genericsConnector.AddGeneric(UUID.Zero, "GridRegistrationUrls", RegionHandle.ToString(), urls.ToOSD());

            return retVal;
        }

        public void RemoveUrlsForClient(string SessionID, ulong RegionHandle)
        {
            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", RegionHandle.ToString(), new GridRegistrationURLs());
            if (urls != null)
            {
                //Remove all the handlers from the HTTP Server
                foreach (KeyValuePair<string, OSD> kvp in urls.URLS)
                {
                    IGridRegistrationUrlModule module = m_modules[kvp.Key];
                    IHttpServer server = m_simulationBase.GetHttpServer(module.Port);
                    server.RemoveHTTPHandler("POST", kvp.Value);
                }
                //Remove from the database so that they don't pop up later
                m_genericsConnector.RemoveGeneric(UUID.Zero, "GridRegistrationUrls", RegionHandle.ToString());
            }
        }

        public void UpdateUrlsForClient(ulong RegionHandle)
        {
            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", RegionHandle.ToString(), new GridRegistrationURLs());
            if (urls != null)
            {
                urls.Expiration = DateTime.Now.AddHours(m_timeBeforeTimeout);
                m_genericsConnector.AddGeneric(UUID.Zero, "GridRegistrationUrls", RegionHandle.ToString(), urls.ToOSD());
            }
        }

        public void RegisterModule(IGridRegistrationUrlModule module)
        {
            //Add the module to our list
            m_modules.Add(module.UrlName, module);
        }

        public bool CheckThreatLevel(string SessionID, ulong RegionHandle, string function, ThreatLevel defaultThreatLevel)
        {
            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", RegionHandle.ToString(), new GridRegistrationURLs());
            if (urls != null)
            {
                //Past time for it to expire
                if (urls.Expiration < DateTime.Now)
                {
                    RemoveUrlsForClient(SessionID, RegionHandle);
                    return false;
                }
                //Find the permission set by function name in the config if it exists
                PermissionSet permissions = FindPermissionsForThreatLevel(FindThreatLevelForFunction(function, defaultThreatLevel, RegionHandle));
                return permissions.CheckPermission(function, RegionHandle);
            }
            return false;
        }

        #endregion

        #region Classes

        public class GridRegistrationURLs : IDataTransferable
        {
            public OSDMap URLS;
            public string SessionID;
            public ulong RegionHandle;
            public DateTime Expiration;

            public override OSDMap ToOSD()
            {
                OSDMap retVal = new OSDMap();
                retVal["URLS"] = URLS;
                retVal["SessionID"] = SessionID;
                retVal["RegionHandle"] = RegionHandle;
                retVal["Expiration"] = Expiration;
                return retVal;
            }

            public override void FromOSD(OSDMap retVal)
            {
                URLS = (OSDMap)retVal["URLS"];
                SessionID = retVal["SessionID"].AsString();
                RegionHandle = retVal["RegionHandle"].AsULong();
                Expiration = retVal["Expiration"].AsDate();
            }

            public override IDataTransferable Duplicate()
            {
                GridRegistrationURLs url = new GridRegistrationURLs();
                url.FromOSD(ToOSD());
                return url;
            }
        }

        public class LoadBalancerUrls
        {
            protected List<string> m_urls = new List<string>();
            protected int lastSetHost = 0;

            public void AddUrls(string[] urls)
            {
                m_urls.AddRange(urls);
            }

            public void SetUrls(string[] urls)
            {
                m_urls = new List<string>(urls);
            }

            public string GetHost()
            {
                if (lastSetHost < m_urls.Count)
                {
                    string url = m_urls[lastSetHost];
                    lastSetHost++;
                    if (lastSetHost == m_urls.Count)
                        lastSetHost = 0;
                    return url;
                }
                else if (m_urls.Count > 0)
                    return m_urls[0];
                return "";
            }
        }

        #endregion
    }
}
