/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

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
        protected Dictionary<string, ThreatLevel> m_cachedThreatLevels = new Dictionary<string, ThreatLevel>();
        protected LoadBalancerUrls m_loadBalancer = new LoadBalancerUrls();
        protected IGenericsConnector m_genericsConnector;
        protected ISimulationBase m_simulationBase;
        protected IConfig m_permissionConfig;
        protected IConfig m_configurationConfig;
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
            m_simulationBase.EventManager.RegisterEventHandler("GridRegionSuccessfullyRegistered", EventManager_OnGenericEvent);

            m_configurationConfig = config.Configs["Configuration"];
            m_loadBalancer.SetConfig (m_configurationConfig);

            if (m_configurationConfig != null)
                m_useSessionTime = m_configurationConfig.GetBoolean ("UseSessionTime", m_useSessionTime);
            if (m_configurationConfig != null)
                m_useRegistrationService = m_configurationConfig.GetBoolean ("UseRegistrationService", m_useRegistrationService);
            m_permissionConfig = config.Configs["RegionPermissions"];
            if (m_permissionConfig != null)
                ReadConfiguration(m_permissionConfig);
        }

        object EventManager_OnGenericEvent (string FunctionName, object parameters)
        {
            if (FunctionName == "GridRegionSuccessfullyRegistered")
            {
                object[] param = (object[])parameters;
                OSDMap resultMap = (OSDMap)param[0];
                UUID SecureSessionID = (UUID)param[1];
                GridRegion rinfo = (GridRegion)param[2];
                OSDMap urls = GetUrlForRegisteringClient (rinfo.RegionHandle.ToString());
                resultMap["URLs"] = urls;
                resultMap["TimeBeforeReRegister"] = m_registry.RequestModuleInterface<IGridRegistrationService> ().ExpiresTime;
                param[0] = resultMap;
                parameters = param;
            }

            return null;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
            m_registry.RequestModuleInterface<IAsyncMessageRecievedService> ().OnMessageReceived += OnMessageReceived;
            m_genericsConnector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector> ();
            LoadFromDatabase ();
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
                string SessionID = message["SessionID"];
                if (CheckThreatLevel (SessionID, "RegisterHandlers", ThreatLevel.None))
                    UpdateUrlsForClient(SessionID);
            }
            return null;
        }

        protected void ReadConfiguration(IConfig config)
        {
            PermissionSet.ReadFunctions(config);
            m_timeBeforeTimeout = config.GetInt("DefaultTimeout", m_timeBeforeTimeout);
            m_defaultRegionThreatLevel = (ThreatLevel)Enum.Parse(typeof(ThreatLevel), config.GetString("DefaultRegionThreatLevel", m_defaultRegionThreatLevel.ToString()));
        }

        private ThreatLevel FindRegionThreatLevel(string SessionID)
        {
            ThreatLevel regionThreatLevel = m_defaultRegionThreatLevel;
            if (m_cachedThreatLevels.TryGetValue (SessionID, out regionThreatLevel))
                return regionThreatLevel;
            regionThreatLevel = m_defaultRegionThreatLevel;
            int x, y;
            ulong handle;
            if (ulong.TryParse (SessionID, out handle))
            {
                Util.UlongToInts (handle, out x, out y);
                GridRegion region = m_registry.RequestModuleInterface<IGridService> ().GetRegionByPosition (UUID.Zero, x, y);
                if (region == null)
                    regionThreatLevel = ThreatLevel.None;
                else
                {
                    string rThreat = region.GenericMap["ThreatLevel"].AsString ();
                    if (rThreat != "")
                        regionThreatLevel = (ThreatLevel)Enum.Parse (typeof (ThreatLevel), rThreat);
                }
            }
            m_cachedThreatLevels[SessionID] = regionThreatLevel;
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
                if(url.HostNames == null || url.Ports == null || url.URLS == null)
                {
                    RemoveUrlsForClient(url.SessionID.ToString());
                }
                else
                {
                    foreach (IGridRegistrationUrlModule module in m_modules.Values)
                    {
                        module.AddExistingUrlForClient (url.SessionID.ToString (), url.URLS[module.UrlName], url.Ports[module.UrlName]);
                    }
                    if (m_useSessionTime && url.Expiration.AddHours(m_timeBeforeTimeout / 8) < DateTime.Now) //Check to see whether the expiration is soon before updating
                    {
                        //Fix the expiration time
                        url.Expiration = DateTime.Now.AddHours(m_timeBeforeTimeout);
                        InnerUpdateUrlsForClient(url);
                    }
                }
            }
        }

        #endregion

        #region IGridRegistrationService Members

        public OSDMap GetUrlForRegisteringClient(string SessionID)
        {
            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", SessionID.ToString (), new GridRegistrationURLs ());
            OSDMap retVal = new OSDMap();
            if (urls != null)
            {
                if(urls.HostNames == null || urls.Ports == null || urls.URLS == null)
                {
                    RemoveUrlsForClient(urls.SessionID.ToString());
                }
                else
                {
                    urls.Expiration = DateTime.Now.AddHours(m_timeBeforeTimeout);
                    urls.SessionID = SessionID;
                    foreach (KeyValuePair<string, OSD> module in urls.URLS)
                    {
                        //Build the URL
                        retVal[module.Key] = urls.HostNames[module.Key] + ":" + urls.Ports[module.Key] + module.Value.AsString ();
                    }
                }
                return retVal;
            }
            OSDMap databaseSave = new OSDMap();
            OSDMap ports = new OSDMap();
            OSDMap hostnames = new OSDMap();
            //Get the URLs from all the modules that have registered with us
            foreach (IGridRegistrationUrlModule module in m_modules.Values)
            {
                ports[module.UrlName] = m_loadBalancer.GetPort (module.UrlName);
                hostnames[module.UrlName] = m_loadBalancer.GetHost (module.UrlName);
                //Build the URL
                databaseSave[module.UrlName] = module.GetUrlForRegisteringClient (SessionID, ports[module.UrlName]);
            }
            foreach (KeyValuePair<string, OSD> module in databaseSave)
            {
                //Build the URL
                retVal[module.Key] = hostnames[module.Key] + ":" + ports[module.Key] + module.Value.AsString ();
            }

            //Save into the database so that we can rebuild later if the server goes offline
            urls = new GridRegistrationURLs();
            urls.URLS = databaseSave;
            urls.SessionID = SessionID;
            urls.Ports = ports;
            urls.HostNames = hostnames;
            urls.Expiration = DateTime.Now.AddHours(m_timeBeforeTimeout);
            m_genericsConnector.AddGeneric (UUID.Zero, "GridRegistrationUrls", SessionID.ToString (), urls.ToOSD ());

            return retVal;
        }

        public void RemoveUrlsForClient(string SessionID)
        {
            if (!m_useRegistrationService)
                return;

            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", SessionID.ToString (), new GridRegistrationURLs ());
            if (urls != null)
            {
                m_log.WarnFormat ("[GridRegService]: Removing URLs for {0}", SessionID);
                //Remove all the handlers from the HTTP Server
                foreach (IGridRegistrationUrlModule module in m_modules.Values)
                {
                    if (!urls.URLS.ContainsKey(module.UrlName))
                        continue;
                    module.RemoveUrlForClient (urls.SessionID, urls.URLS[module.UrlName], urls.Ports[module.UrlName]);
                }
                //Remove from the database so that they don't pop up later
                m_genericsConnector.RemoveGeneric (UUID.Zero, "GridRegistrationUrls", SessionID.ToString ());
            }
        }

        public void UpdateUrlsForClient(string SessionID)
        {
            if (!m_useRegistrationService)
                return;

            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", SessionID.ToString (), new GridRegistrationURLs ());
            InnerUpdateUrlsForClient(urls);
        }

        private void InnerUpdateUrlsForClient(GridRegistrationURLs urls)
        {
            if (urls != null)
            {
                urls.Expiration = DateTime.Now.AddHours(m_timeBeforeTimeout);
                m_genericsConnector.AddGeneric (UUID.Zero, "GridRegistrationUrls", urls.SessionID.ToString (), urls.ToOSD ());
                m_log.WarnFormat ("[GridRegistrationService]: Updated URLs for {0}", urls.SessionID);
            }
            else
                m_log.ErrorFormat ("[GridRegistrationService]: Failed to find URLs to update for {0}", urls.SessionID);
        }

        public void RegisterModule(IGridRegistrationUrlModule module)
        {
            //Add the module to our list
            m_modules.Add(module.UrlName, module);
        }

        public bool CheckThreatLevel(string SessionID, string function, ThreatLevel defaultThreatLevel)
        {
            if (!m_useRegistrationService)
                return true;

            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", SessionID.ToString (), new GridRegistrationURLs ());
            if (urls != null)
            {
                //Past time for it to expire
                if (m_useSessionTime && urls.Expiration < DateTime.Now)
                {
                    RemoveUrlsForClient(SessionID);
                    return false;
                }
                //First find the threat level that this setting has to have do be able to run
                ThreatLevel functionThreatLevel = PermissionSet.FindThreatLevelForFunction(function, defaultThreatLevel);
                //Now find the permission for that threat level
                //else, check it against the threat level that the region has
                ThreatLevel regionThreatLevel = FindRegionThreatLevel (SessionID);
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
            public DateTime Expiration;
            public OSDMap HostNames;
            public OSDMap Ports;

            public override OSDMap ToOSD()
            {
                OSDMap retVal = new OSDMap();
                retVal["URLS"] = URLS;
                retVal["SessionID"] = SessionID;
                retVal["Expiration"] = Expiration;
                retVal["HostName"] = HostNames;
                retVal["Port"] = Ports;
                return retVal;
            }

            public override void FromOSD(OSDMap retVal)
            {
                URLS = (OSDMap)retVal["URLS"];
                SessionID = retVal["SessionID"].AsString();
                Expiration = retVal["Expiration"].AsDate ();
                HostNames = retVal["HostName"] as OSDMap;
                Ports = retVal["Port"] as OSDMap;
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
            protected Dictionary<string, List<string>> m_urls = new Dictionary<string, List<string>> ();
            protected Dictionary<string, List<uint>> m_ports = new Dictionary<string, List<uint>> ();
            protected Dictionary<string, int> lastSetHost = new Dictionary<string, int>();
            protected Dictionary<string, int> lastSetPort = new Dictionary<string, int>();
            protected const uint m_defaultPort = 8002;
            protected const string m_defaultHostname = "127.0.0.1";
            protected IConfig m_configurationConfig;

            public void SetConfig (IConfig config)
            {
                m_configurationConfig = config;

                if (m_configurationConfig != null)
                {
                    SetDefaultUrls (m_configurationConfig.GetString ("HostNames", m_defaultHostname).Split (','));
                    SetDefaultPorts (m_configurationConfig.GetString ("Ports", m_defaultPort.ToString()).Split (','));
                }
            }

            #region Set accessors

            protected void SetDefaultUrls (string[] urls)
            {
                SetUrls ("default", urls);
            }

            protected void SetUrls (string name, string[] urls)
            {
                for (int i = 0; i < urls.Length; i++)
                {
                    if (urls[i].StartsWith (" "))
                        urls[i] = urls[i].Remove (0, 1);
                    //Remove any ports people may have added
                    urls[i] = urls[i].Replace ("http://", "");
                    urls[i] = urls[i].Split (':')[0];
                    //Readd the http://
                    urls[i] = "http://" + urls[i];
                }
                m_urls[name] = new List<string> (urls);
            }

            protected void SetDefaultPorts (string[] ports)
            {
                SetPorts ("default", ports);
            }

            protected void SetPorts (string name, string[] ports)
            {
                List<uint> uPorts = new List<uint> ();
                for (int i = 0; i < ports.Length; i++)
                {
                    if (ports[i].StartsWith (" "))
                        ports[i] = ports[i].Remove (0, 1);
                    uPorts.Add (uint.Parse (ports[i]));
                }
                m_ports[name] = uPorts;
            }

            #endregion

            #region Get accessors

            public string GetHost(string name)
            {
                if (!m_urls.ContainsKey (name))
                    SetUrls (name, m_configurationConfig.GetString (name + "Hostnames", m_defaultHostname).Split (','));
                if (!lastSetHost.ContainsKey (name))
                    lastSetHost.Add (name, 0);

                List<string> urls = m_urls[name];
                if (lastSetHost[name] < urls.Count)
                {
                    string url = urls[lastSetHost[name]];
                    lastSetHost[name]++;
                    if (lastSetHost[name] == urls.Count)
                        lastSetHost[name] = 0;
                    return url;
                }
                else if (urls.Count > 0)
                    return urls[0];
                return GetHost("default");
            }

            public uint GetPort (string name)
            {
                if (!m_ports.ContainsKey (name))
                    SetPorts (name, m_configurationConfig.GetString (name + "Ports", m_defaultPort.ToString()).Split (','));
                if (!lastSetPort.ContainsKey (name))
                    lastSetPort.Add (name, 0);

                List<uint> ports = m_ports[name];
                if (lastSetPort[name] < ports.Count)
                {
                    uint url = ports[lastSetPort[name]];
                    lastSetPort[name]++;
                    if (lastSetPort[name] == ports.Count)
                        lastSetPort[name] = 0;
                    return url;
                }
                else if (ports.Count > 0)
                    return ports[0];
                return GetPort ("default");
            }

            #endregion
        }

        #endregion
    }
}
