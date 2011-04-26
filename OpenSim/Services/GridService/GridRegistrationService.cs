using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using log4net;

namespace OpenSim.Services.GridService
{
    public class GridRegistrationService : IService, IGridRegistrationService
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected Dictionary<string, IGridRegistrationUrlModule> m_modules = new Dictionary<string, IGridRegistrationUrlModule>();
        protected Dictionary<ulong, ThreatLevel> m_cachedThreatLevels = new Dictionary<ulong, ThreatLevel>();
        protected LoadBalancerUrls m_loadBalancer = new LoadBalancerUrls();
        protected IGenericsConnector m_genericsConnector;
        protected ISimulationBase m_simulationBase;
        protected IConfig m_permissionConfig;
        protected IRegistryCore m_registry;
        protected bool m_useSessionTime = true;
        protected bool m_useRegistrationService = true;
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
        
        protected class PermissionSet
        {
            private static Dictionary<string, ThreatLevel> PermittedFunctions = new Dictionary<string, ThreatLevel>();
            
            public static void ReadFunctions(IConfig config)
            {
                //Combine all threat level configs for ones that are less than our given threat level as well
                foreach (ThreatLevel allThreatLevel in Enum.GetValues(typeof(ThreatLevel)))
                {
                    string list = config.GetString("Threat_Level_" + allThreatLevel.ToString(), "");
                    if (list != "")
                    {
                        string[] functions = list.Split(',');
                        foreach (string function in functions)
                        {
                            string f = function;
                            //Clean them up
                            f = f.Replace(" ", "");
                            f = f.Replace("\r", "");
                            f = f.Replace("\n", "");
                            PermittedFunctions[f] = allThreatLevel;
                        }
                    }
                }
            }

            public static ThreatLevel FindThreatLevelForFunction(string function, ThreatLevel requestedLevel)
            {
                if (PermittedFunctions.ContainsKey(function))
                {
                    return PermittedFunctions[function];
                }
                return requestedLevel;
            }
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterModuleInterface<IGridRegistrationService>(this);
            m_registry = registry;
            m_simulationBase = registry.RequestModuleInterface<ISimulationBase>();

            IConfig ConfigurationConfig = config.Configs["Configuration"];

            if(ConfigurationConfig != null)
                m_loadBalancer.SetUrls (ConfigurationConfig.GetString ("HostNames", "http://localhost").Split (','));
            if (ConfigurationConfig != null)
                m_useSessionTime = ConfigurationConfig.GetBoolean ("UseSessionTime", m_useSessionTime);
            if (ConfigurationConfig != null)
                m_useRegistrationService = ConfigurationConfig.GetBoolean ("UseRegistrationService", m_useRegistrationService);
            m_permissionConfig = config.Configs["RegionPermissions"];
            if (m_permissionConfig != null)
                ReadConfiguration(m_permissionConfig);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_registry.RequestModuleInterface<IAsyncMessageRecievedService>().OnMessageReceived += OnMessageReceived;
            m_genericsConnector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            LoadFromDatabase();
        }

        public void FinishedStartup()
        {
        }
        
        /// <summary>
        /// We do handle the RegisterHandlers message here, as we deal with all of the handlers in this module
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private OSDMap OnMessageReceived(OSDMap message)
        {
            if (!m_useRegistrationService)
                return null;

            if (message.ContainsKey("Method") && message["Method"].AsString() == "RegisterHandlers")
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
            PermissionSet.ReadFunctions(config);
            m_timeBeforeTimeout = config.GetInt("DefaultTimeout", m_timeBeforeTimeout);
            m_defaultRegionThreatLevel = (ThreatLevel)Enum.Parse(typeof(ThreatLevel), config.GetString("DefaultRegionThreatLevel", m_defaultRegionThreatLevel.ToString()));
        }

        private ThreatLevel FindRegionThreatLevel(ulong RegionHandle)
        {
            ThreatLevel regionThreatLevel = m_defaultRegionThreatLevel;
            if (m_cachedThreatLevels.TryGetValue(RegionHandle, out regionThreatLevel))
                return regionThreatLevel;
            regionThreatLevel = m_defaultRegionThreatLevel;
            int x, y;
            Util.UlongToInts(RegionHandle, out x, out y);
            GridRegion region = m_registry.RequestModuleInterface<IGridService>().GetRegionByPosition(UUID.Zero, x, y);
            if (region == null)
                regionThreatLevel = ThreatLevel.None;
            else
            {
                string rThreat = region.GenericMap["ThreatLevel"].AsString();
                if (rThreat != "")
                    regionThreatLevel = (ThreatLevel)Enum.Parse(typeof(ThreatLevel), rThreat);
            }
            m_cachedThreatLevels[RegionHandle] = regionThreatLevel;
            return regionThreatLevel;
        }

        protected void LoadFromDatabase()
        {
            if (!m_useRegistrationService)
                return;

            List<GridRegistrationURLs> urls = m_genericsConnector.GetGenerics<GridRegistrationURLs>(
                UUID.Zero, "GridRegistrationUrls", new GridRegistrationURLs());

            foreach (GridRegistrationURLs url in urls)
            {
                foreach (IGridRegistrationUrlModule module in m_modules.Values)
                {
                    module.AddExistingUrlForClient(url.SessionID.ToString(), url.RegionHandle, url.URLS[module.UrlName]);
                }
                if (m_useSessionTime && url.Expiration.AddHours(m_timeBeforeTimeout / 8) < DateTime.Now) //Check to see whether the expiration is soon before updating
                {
                    //Fix the expiration time
                    url.Expiration = DateTime.Now.AddHours(m_timeBeforeTimeout);
                    InnerUpdateUrlsForClient(url);
                }
            }
        }

        #endregion

        #region IGridRegistrationService Members

        public OSDMap GetUrlForRegisteringClient(string SessionID, ulong RegionHandle)
        {
            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", RegionHandle.ToString(), new GridRegistrationURLs());
            OSDMap retVal = new OSDMap();
            if (urls != null)
            {
                urls.Expiration = DateTime.Now.AddHours(m_timeBeforeTimeout);
                urls.SessionID = SessionID;
                urls.RegionHandle = RegionHandle;
                foreach (KeyValuePair<string, OSD> module in urls.URLS)
                {
                    //Build the URL
                    retVal[module.Key] = m_loadBalancer.GetHost() + ":" + m_modules[module.Key].Port + module.Value.AsString();
                }
                return retVal;
            }
            OSDMap databaseSave = new OSDMap();
            //Get the URLs from all the modules that have registered with us
            foreach (IGridRegistrationUrlModule module in m_modules.Values)
            {
                //Build the URL
                databaseSave[module.UrlName] = module.GetUrlForRegisteringClient(SessionID, RegionHandle);
            }
            foreach (KeyValuePair<string, OSD> module in databaseSave)
            {
                //Build the URL
                retVal[module.Key] = m_loadBalancer.GetHost() + ":" + m_modules[module.Key].Port + module.Value.AsString();
            }

            //Save into the database so that we can rebuild later if the server goes offline
            urls = new GridRegistrationURLs();
            urls.URLS = databaseSave;
            urls.RegionHandle = RegionHandle;
            urls.SessionID = SessionID;
            urls.Expiration = DateTime.Now.AddHours(m_timeBeforeTimeout);
            m_genericsConnector.AddGeneric(UUID.Zero, "GridRegistrationUrls", RegionHandle.ToString(), urls.ToOSD());

            return retVal;
        }

        public void RemoveUrlsForClient(string SessionID, ulong RegionHandle)
        {
            if (!m_useRegistrationService)
                return;

            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", RegionHandle.ToString(), new GridRegistrationURLs());
            if (urls != null)
            {
                m_log.WarnFormat("[GridRegService]: Removing URLs for {0}", RegionHandle);
                //Remove all the handlers from the HTTP Server
                foreach (IGridRegistrationUrlModule module in m_modules.Values)
                {
                    if (!urls.URLS.ContainsKey(module.UrlName))
                        continue;
                    module.RemoveUrlForClient(urls.RegionHandle, urls.SessionID, urls.URLS[module.UrlName].AsString());
                }
                //Remove from the database so that they don't pop up later
                m_genericsConnector.RemoveGeneric(UUID.Zero, "GridRegistrationUrls", RegionHandle.ToString());
            }
        }

        public void UpdateUrlsForClient(ulong RegionHandle)
        {
            if (!m_useRegistrationService)
                return;

            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", RegionHandle.ToString(), new GridRegistrationURLs());
            InnerUpdateUrlsForClient(urls);
        }

        private void InnerUpdateUrlsForClient(GridRegistrationURLs urls)
        {
            if (urls != null)
            {
                urls.Expiration = DateTime.Now.AddHours(m_timeBeforeTimeout);
                m_genericsConnector.AddGeneric(UUID.Zero, "GridRegistrationUrls", urls.RegionHandle.ToString(), urls.ToOSD());
                m_log.WarnFormat("[GridRegistrationService]: Updated URLs for {0}", urls.RegionHandle);
            }
            else
                m_log.ErrorFormat("[GridRegistrationService]: Failed to find URLs to update for {0}", urls.RegionHandle);
        }

        public void RegisterModule(IGridRegistrationUrlModule module)
        {
            //Add the module to our list
            m_modules.Add(module.UrlName, module);
        }

        public bool CheckThreatLevel(string SessionID, ulong RegionHandle, string function, ThreatLevel defaultThreatLevel)
        {
            if (!m_useRegistrationService)
                return true;

            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", RegionHandle.ToString(), new GridRegistrationURLs());
            if (urls != null)
            {
                //Past time for it to expire
                if (m_useSessionTime && urls.Expiration < DateTime.Now)
                {
                    RemoveUrlsForClient(SessionID, RegionHandle);
                    return false;
                }
                //First find the threat level that this setting has to have do be able to run
                ThreatLevel functionThreatLevel = PermissionSet.FindThreatLevelForFunction(function, defaultThreatLevel);
                //Now find the permission for that threat level
                //else, check it against the threat level that the region has
                ThreatLevel regionThreatLevel = FindRegionThreatLevel(RegionHandle);
                //Return whether the region threat level is higher than the function threat level
                return functionThreatLevel <= regionThreatLevel;
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
                for (int i = 0; i < urls.Length; i++)
                {
                    //Remove any ports people may have added
                    urls[i] = urls[i].Replace ("http://", "");
                    urls[i] = urls[i].Split (':')[0];
                    //Readd the http://
                    urls[i] = "http://" + urls[i];
                }
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
