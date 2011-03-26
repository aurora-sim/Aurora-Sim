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
                m_loadBalancer.SetUrls(ConfigurationConfig.GetString("HostNames", "http://localhost").Split(','));

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
        OSDMap OnMessageReceived(OSDMap message)
        {
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
                //Fix the expiration time
                url.Expiration = DateTime.Now.AddHours (m_timeBeforeTimeout);
                //Now resave it to the database
                m_genericsConnector.AddGeneric (UUID.Zero, "GridRegistrationUrls", url.RegionHandle.ToString(), url.ToOSD ());

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
                m_log.WarnFormat("[GridRegistrationService]: Updated URLs for {0}", RegionHandle);
            }
            else
                m_log.ErrorFormat("[GridRegistrationService]: Failed to find URLs to update for {0}", RegionHandle);
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
                //First find the threat level that this setting has to have do be able to run
                ThreatLevel functionThreatLevel = PermissionSet.FindThreatLevelForFunction(function, defaultThreatLevel);
                //Now find the permission for that threat level
                //else, check it against the threat level that the region has
                GridRegion region = FindRegion(RegionHandle);
                if (region == null)
                    return false;
                string rThreat = region.GenericMap["ThreatLevel"].AsString();
                ThreatLevel regionThreatLevel = m_defaultRegionThreatLevel;
                if (rThreat != "")
                    regionThreatLevel = (ThreatLevel)Enum.Parse(typeof(ThreatLevel), rThreat);
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
