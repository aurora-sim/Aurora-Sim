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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Nini.Config;
using Aurora.Simulation.Base;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.GridService
{
    public class GridRegistrationService : IService, IGridRegistrationService
    {
        #region Declares

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
        protected float m_timeBeforeTimeout = 24;
        public float ExpiresTime 
        { 
            get
            {
                return m_timeBeforeTimeout; 
            }
        }

        protected ThreatLevel m_defaultRegionThreatLevel = ThreatLevel.High;
        
        protected class PermissionSet
        {
            private static readonly Dictionary<string, ThreatLevel> PermittedFunctions = new Dictionary<string, ThreatLevel>();
            
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

            m_configurationConfig = config.Configs["Configuration"];
            m_loadBalancer.SetConfig(config, this);

            if (m_configurationConfig != null)
                m_useSessionTime = m_configurationConfig.GetBoolean ("UseSessionTime", m_useSessionTime);
            if (m_configurationConfig != null)
                m_useRegistrationService = m_configurationConfig.GetBoolean ("UseRegistrationService", m_useRegistrationService);
            m_permissionConfig = config.Configs["RegionPermissions"];
            if (m_permissionConfig != null)
                ReadConfiguration(m_permissionConfig);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
            m_genericsConnector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector> ();
            LoadFromDatabase ();
        }

        protected void ReadConfiguration(IConfig config)
        {
            PermissionSet.ReadFunctions(config);
            m_timeBeforeTimeout = config.GetFloat("DefaultTimeout", m_timeBeforeTimeout);
            m_defaultRegionThreatLevel = (ThreatLevel)Enum.Parse(typeof(ThreatLevel), config.GetString("DefaultRegionThreatLevel", m_defaultRegionThreatLevel.ToString()));
        }

        private ThreatLevel FindRegionThreatLevel(string SessionID)
        {
            ThreatLevel regionThreatLevel = m_defaultRegionThreatLevel;
            if (m_cachedThreatLevels.TryGetValue (SessionID, out regionThreatLevel))
                return regionThreatLevel;
            regionThreatLevel = m_defaultRegionThreatLevel;
            ulong handle;
            if (ulong.TryParse (SessionID, out handle))
            {
                int x;
                int y;
                Util.UlongToInts (handle, out x, out y);
                GridRegion region = m_registry.RequestModuleInterface<IGridService> ().GetRegionByPosition (null, x, y);
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
                UUID.Zero, "GridRegistrationUrls");

            foreach (GridRegistrationURLs url in urls)
            {
                ulong e;
                if (!ulong.TryParse (url.SessionID, out e))
                {
                    //Don't load links (yet)
                    continue;
                }
                if (url.HostNames == null || url.Ports == null ||
                    url.URLS == null ||
                    !CheckModuleNames(url) || url.VersionNumber < GridRegistrationURLs.CurrentVersionNumber)
                {
                    RemoveUrlsForClient(url.SessionID);
                }
                else
                {
                    foreach (IGridRegistrationUrlModule module in m_modules.Values)
                    {
                        if (url.URLS.ContainsKey(module.UrlName))//Make sure it exists
                        {
                            int i = 0;
                            if (url.HostNames[module.UrlName].Type == OSDType.Array)
                                foreach (OSD o in (OSDArray)url.URLS[module.UrlName])
                                    module.AddExistingUrlForClient(url.SessionID, ((OSDArray)url.URLS[module.UrlName])[i], ((OSDArray)url.Ports[module.UrlName])[i++]);
                            else
                                module.AddExistingUrlForClient(url.SessionID, url.URLS[module.UrlName], url.Ports[module.UrlName]);
                        }
                    }
                    if (m_useSessionTime && (url.Expiration.AddMinutes((m_timeBeforeTimeout * 60) * 0.9)) < DateTime.UtcNow) //Check to see whether the expiration is soon before updating
                    {
                        //Fix the expiration time
                        InnerUpdateUrlsForClient (url);
                    }
                }
            }
        }

        #endregion

        #region IGridRegistrationService Members

        public OSDMap RegionRemoteHandlerURL(GridRegion regionInfo, UUID sessionID, UUID oldSessionID)
        {
            List<GridRegistrationURLs> urls = m_genericsConnector.GetGenerics<GridRegistrationURLs>(
                regionInfo.RegionID, "RegionRegistrationUrls");
            m_genericsConnector.RemoveGeneric(regionInfo.RegionID, "RegionRegistrationUrls");
            if ((urls == null) || (urls.Count == 0) || (urls[0].Expiration < DateTime.UtcNow))
            {
                urls = new List<GridRegistrationURLs>();
                OSDMap hostnames = new OSDMap();
                OSDMap databaseSave = new OSDMap
                                          {
                                              {
                                                  "regionURI",
                                                  "http://" + regionInfo.ExternalEndPoint.Address + ":" + regionInfo.ExternalEndPoint.Port + "/region" + UUID.Random()
                                              }
                                          };
                OSDMap ports = new OSDMap { { "regionURI", regionInfo.InternalEndPoint.Port } };

                GridRegistrationURLs urls2 = new GridRegistrationURLs
                {
                    URLS = databaseSave,
                    SessionID = sessionID.ToString(),
                    Ports = ports,
                    HostNames = hostnames,
                    Expiration = DateTime.UtcNow.AddMinutes(m_timeBeforeTimeout * 60)
                };
                urls.Add(urls2);
            }
            m_genericsConnector.AddGeneric(regionInfo.RegionID, "RegionRegistrationUrls", sessionID.ToString(), urls[0].ToOSD());
            return urls[0].ToOSD();
        }
        public OSDMap GetUrlForRegisteringClient(string SessionID)
        {
            if (!m_useRegistrationService)
                return null;
            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", SessionID);
            OSDMap retVal = new OSDMap();
            if (urls != null)
            {
                if(urls.HostNames == null || urls.Ports == null ||
                    urls.URLS == null || urls.SessionID != SessionID ||
                    MainServer.Instance.HostName != urls._ParentHostName ||
                    !CheckModuleNames(urls) || urls.VersionNumber < GridRegistrationURLs.CurrentVersionNumber)
                {
                    if (urls.VersionNumber == GridRegistrationURLs.CurrentVersionNumber)
                    {
                        if (CheckModuleNames(urls))
                        {
                            MainConsole.Instance.Warn("[GridRegService]: Null stuff in GetUrls, HostNames " + (urls.HostNames == null) + ", Ports " +
                                (urls.Ports == null) + ", URLS " + (urls.URLS == null) + ", SessionID 1 " + SessionID + ", SessionID 2 " + urls.SessionID +
                                ", checkModuleNames: " + CheckModuleNames(urls));
                        }
                    }
                    RemoveUrlsForClient(urls.SessionID);
                }
                else
                {
                    urls.Expiration = DateTime.UtcNow.AddMinutes (m_timeBeforeTimeout * 60);
                    urls.SessionID = SessionID;
                    InnerUpdateUrlsForClient (urls);
                    foreach (KeyValuePair<string, OSD> module in urls.URLS)
                    {
                        if (module.Value.Type == OSDType.Array)
                        {
                            OSDArray array = new OSDArray();
                            int i = 0;
                            foreach (OSD o in (OSDArray)module.Value)
                            {
                                //Build the URL
                                if (o.AsString().StartsWith("http://") || o.AsString().StartsWith("https://"))
                                    array.Add(o.AsString());
                                else
                                    array.Add(((OSDArray)urls.HostNames[module.Key])[i] + ":" +
                                        (int)((OSDArray)urls.Ports[module.Key])[i].AsUInteger() +
                                        o.AsString());
                                i++;
                            }
                            retVal[module.Key] = array;
                        }
                        else
                        {
                            //Build the URL
                            if (module.Value.AsString().StartsWith("http://") || module.Value.AsString().StartsWith("https://"))
                                retVal[module.Key] = module.Value.AsString();
                            else
                                retVal[module.Key] = urls.HostNames[module.Key] + ":" + urls.Ports[module.Key] + module.Value.AsString();
                        }
                    }
                    return retVal;
                }
            }
            OSDMap databaseSave = new OSDMap();
            OSDMap ports = new OSDMap();
            OSDMap hostnames = new OSDMap();
            //Get the URLs from all the modules that have registered with us
            foreach (IGridRegistrationUrlModule module in m_modules.Values)
            {
                List<uint> port;
                List<string> hostName;
                List<string> innerURL;

                m_loadBalancer.GetHost (module.UrlName, module, SessionID, out port, out hostName, out innerURL);
                ports[module.UrlName] = port.Count == 1 ? (OSD)port[0] : (OSD)port.ToOSDArray();
                hostnames[module.UrlName] = hostName.Count == 1 ? (OSD)hostName[0] : (OSD)hostName.ToOSDArray();
                databaseSave[module.UrlName] = innerURL.Count == 1 ? (OSD)innerURL[0] : (OSD)innerURL.ToOSDArray();
            }
            foreach (KeyValuePair<string, OSD> module in databaseSave)
            {
                if (module.Value.Type == OSDType.Array)
                {
                    OSDArray array = new OSDArray();
                    int i = 0;
                    foreach (OSD o in (OSDArray)module.Value)
                    {
                        //Build the URL
                        if (o.AsString().StartsWith("http://") || o.AsString().StartsWith("https://"))
                            array.Add(o.AsString());
                        else
                            array.Add(((OSDArray)hostnames[module.Key])[i] + ":" +
                                (int)((OSDArray)ports[module.Key])[i].AsUInteger() +
                                o.AsString());
                        i++;
                    }
                    retVal[module.Key] = array;
                }
                else
                {
                    //Build the URL
                    if (module.Value.AsString().StartsWith("http://") || module.Value.AsString().StartsWith("https://"))
                        retVal[module.Key] = module.Value.AsString();
                    else
                        retVal[module.Key] = hostnames[module.Key] + ":" + ports[module.Key] + module.Value.AsString();
                }
            }

            //Save into the database so that we can rebuild later if the server goes offline
            urls = new GridRegistrationURLs
                       {
                           URLS = databaseSave,
                           SessionID = SessionID,
                           Ports = ports,
                           HostNames = hostnames,
                           Expiration = DateTime.UtcNow.AddMinutes(m_timeBeforeTimeout*60),
                           _ParentHostName = MainServer.Instance.HostName
                       };
            m_genericsConnector.AddGeneric (UUID.Zero, "GridRegistrationUrls", SessionID, urls.ToOSD ());

            return retVal;
        }

        private bool CheckModuleNames (GridRegistrationURLs urls)
        {
#if (!ISWIN)
            foreach (string urlName in m_modules.Keys)
            {
#if (!ISWIN)
                bool found = false;
                foreach (string o in urls.URLS.Keys)
                {
                    if (o == urlName)
                    {
                        found = true;
                        break;
                    }
                }
#else
                bool found = urls.URLS.Keys.Any(o => o == urlName);
#endif
                if (!found) return false;
            }
            return true;
#else
            return m_modules.Keys.Select(urlName => urls.URLS.Keys.Any(o => o == urlName)).All(found => found);
#endif
        }

        public void RemoveUrlsForClient(string SessionID)
        {
            if (!m_useRegistrationService)
                return;

            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", SessionID);
            if (urls != null)
            {
                MainConsole.Instance.WarnFormat ("[GridRegService]: Removing URLs for {0}", SessionID);
                //Remove all the handlers from the HTTP Server
                foreach (IGridRegistrationUrlModule module in m_modules.Values)
                {
                    if (!urls.URLS.ContainsKey(module.UrlName))
                        continue;
                    try
                    {
                        module.RemoveUrlForClient (urls.SessionID, urls.URLS[module.UrlName], urls.Ports[module.UrlName]);
                    }
                    catch
                    {
                    }
                }
            }
            //Remove from the database so that they don't pop up later
            m_genericsConnector.RemoveGeneric(UUID.Zero, "GridRegistrationUrls", SessionID);
        }

        public void UpdateUrlsForClient(string SessionID)
        {
            if (!m_useRegistrationService)
                return;

            GridRegistrationURLs urls = m_genericsConnector.GetGeneric<GridRegistrationURLs>(UUID.Zero,
                "GridRegistrationUrls", SessionID);
            InnerUpdateUrlsForClient(urls);
        }

        private void InnerUpdateUrlsForClient(GridRegistrationURLs urls)
        {
            if (urls != null)
            {
                urls.Expiration = DateTime.UtcNow.AddMinutes (m_timeBeforeTimeout * 60);
                m_genericsConnector.AddGeneric(UUID.Zero, "GridRegistrationUrls", urls.SessionID, urls.ToOSD());
                MainConsole.Instance.DebugFormat ("[GridRegistrationService]: Updated URLs for {0}", urls.SessionID);
            }
            else
                MainConsole.Instance.ErrorFormat ("[GridRegistrationService]: Failed to find URLs to update for {0}", "unknown");
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
                "GridRegistrationUrls", SessionID);
            if (urls != null)
            {
                //Past time for it to expire
                if (m_useSessionTime && urls.Expiration < DateTime.UtcNow)
                {
                    MainConsole.Instance.Warn ("[GridRegService]: URLs expired for " + SessionID);
                    RemoveUrlsForClient(SessionID);
                    return false;
                }
                //First find the threat level that this setting has to have do be able to run
                ThreatLevel functionThreatLevel = PermissionSet.FindThreatLevelForFunction(function, defaultThreatLevel);
                //Now find the permission for that threat level
                //else, check it against the threat level that the region has
                ThreatLevel regionThreatLevel = FindRegionThreatLevel (SessionID);
                //Return whether the region threat level is higher than the function threat level
                if(!(functionThreatLevel <= regionThreatLevel))
                    MainConsole.Instance.Warn ("[GridRegService]: checkThreatLevel (" + function + ") failed for " + SessionID + ", fperm " + functionThreatLevel + ", rperm " + regionThreatLevel + "!");
                return functionThreatLevel <= regionThreatLevel;
            }
            MainConsole.Instance.Warn ("[GridRegService]: Could not find URLs for checkThreatLevel for " + SessionID + "!");
            return false;
        }

        #endregion

        #region Classes

        public class GridRegistrationURLs : IDataTransferable
        {
            public static readonly int CurrentVersionNumber = 5;
            public OSDMap URLS;
            public string SessionID;
            public DateTime Expiration;
            public OSDMap HostNames;
            public OSDMap Ports;
            public int VersionNumber;
            public string _ParentHostName = "";

            public override OSDMap ToOSD()
            {
                OSDMap retVal = new OSDMap();
                retVal["URLS"] = URLS;
                retVal["SessionID"] = SessionID;
                retVal["Expiration"] = Expiration;
                retVal["HostName"] = HostNames;
                retVal["Port"] = Ports;
                retVal["VersionNumber"] = CurrentVersionNumber;
                retVal["_ParentHostName"] = _ParentHostName;
                return retVal;
            }

            public override void FromOSD(OSDMap retVal)
            {
                URLS = (OSDMap)retVal["URLS"];
                SessionID = retVal["SessionID"].AsString();
                Expiration = retVal["Expiration"].AsDate ();
                Expiration = Expiration.ToUniversalTime ();
                HostNames = retVal["HostName"] as OSDMap;
                Ports = retVal["Port"] as OSDMap;
                if (!retVal.ContainsKey("VersionNumber"))
                    VersionNumber = 0;
                else
                    VersionNumber = retVal["VersionNumber"].AsInteger();
                _ParentHostName = retVal["_ParentHostName"].AsString();
            }
        }

        public class LoadBalancerUrls
        {
            protected Dictionary<string, List<string>> m_urls = new Dictionary<string, List<string>> ();
            protected Dictionary<string, List<uint>> m_ports = new Dictionary<string, List<uint>> ();
            protected Dictionary<string, int> lastSet = new Dictionary<string, int> ();
            protected const uint m_defaultPort = 8003;
            protected string m_defaultHostname = "http://127.0.0.1";
            protected IConfig m_configurationConfig;

            protected int m_externalUrlCountTotal = 0;
            protected List<int> m_externalUrlCount = new List<int> ();
            protected uint m_remotePort = 8003;
            protected List<string> m_remoteLoadBalancingInstances = new List<string> ();
            protected string m_remotePassword = "";

            public void SetConfig (IConfigSource config, GridRegistrationService gridRegService)
            {
                m_configurationConfig = config.Configs["Configuration"];

                if (m_configurationConfig != null)
                {
                    m_defaultHostname = MainServer.Instance.FullHostName;
                    m_remotePassword = m_configurationConfig.GetString ("RemotePassword", "");
                    m_remotePort = m_configurationConfig.GetUInt ("RemoteLoadBalancingPort", m_defaultPort);
                    SetRemoteUrls (m_configurationConfig.GetString ("RemoteLoadBalancingUrls", "").Split (new[] { "," }, StringSplitOptions.RemoveEmptyEntries));
                    if (m_configurationConfig.GetBoolean("UseRemoteLoadBalancing", false))
                    {
                        //Set up the external handlers
                        IHttpServer server = gridRegService.m_registry.RequestModuleInterface<ISimulationBase> ().GetHttpServer (m_remotePort);

                        RemoteLoadBalancingPostHandler handler = new RemoteLoadBalancingPostHandler ("/LoadBalancing", m_remotePassword, gridRegService);
                        server.AddStreamHandler (handler);
                    }
                }
            }

            #region Set accessors

            protected void SetRemoteUrls (string[] urls)
            {
                for (int i = 0; i < urls.Length; i++)
                {
                    if (urls[i].StartsWith (" "))
                        urls[i] = urls[i].Remove(0, 1);
                    bool isSecure = urls[i].StartsWith("https://");
                    urls[i] = urls[i].Replace("http://", "");
                    urls[i] = urls[i].Replace("https://", "");
                    //Readd the http://
                    urls[i] = "http" + (isSecure ? "s" : "") + "://" + urls[i];
                }
                m_remoteLoadBalancingInstances = new List<string> (urls);
            }

            protected void SetUrls (string name, string[] urls)
            {
                for (int i = 0; i < urls.Length; i++)
                {
                    if (urls[i].StartsWith (" "))
                        urls[i] = urls[i].Remove (0, 1);
                    //Remove any ports people may have added
                    bool isSecure = urls[i].StartsWith("https://");
                    urls[i] = urls[i].Replace("http://", "");
                    urls[i] = urls[i].Replace("https://", "");
                    urls[i] = urls[i].Split (':')[0];
                    //Readd the http://
                    urls[i] = "http" + (isSecure ? "s" : "") + "://" + urls[i];
                }
                m_urls[name] = new List<string> (urls);
            }

            protected void AddPorts (string name, string[] ports)
            {
                List<uint> uPorts = new List<uint> ();
                for (int i = 0; i < ports.Length; i++)
                {
                    if (ports[i].StartsWith (" "))
                        ports[i] = ports[i].Remove (0, 1);
                    if (ports[i].Contains("-"))
                    {
                        uint first = (uint.Parse(ports[i].Split('-')[0]));
                        uint second = (uint.Parse(ports[i].Split('-')[1]));
                        for (uint port = first; port <= second; port++)
                            uPorts.Add(port);
                    }
                    else
                        uPorts.Add (uint.Parse (ports[i]));
                }
                if (!m_ports.ContainsKey (name))
                    m_ports[name] = new List<uint> ();
                m_ports[name].AddRange (uPorts);
            }

            #endregion

            #region Get accessors

            /// <summary>
            /// Gets a host and port for the given handler
            /// </summary>
            /// <param name="name"></param>
            /// <param name="SessionID"></param>
            /// <param name="port"></param>
            /// <param name="hostName"></param>
            /// <param name="module"></param>
            /// <param name="innerUrl"></param>
            /// <returns>Whether we need to create a handler or whether it is an external URL</returns>
            public void GetHost (string name, IGridRegistrationUrlModule module, string SessionID, out List<uint> ports, out List<string> hostNames, out List<string> innerUrls)
            {
                if (!m_urls.ContainsKey (name))
                {
                    SetUrls (name, m_configurationConfig.GetString (name + "Hostnames", m_defaultHostname).Split (','));
                    AddPorts (name, m_configurationConfig.GetString (name + "InternalPorts", m_defaultPort.ToString ()).Split (','));
                    GetExternalCounts (name);
                }
                if (!lastSet.ContainsKey (name))
                    lastSet.Add (name, 0);

                ports = new List<uint>();
                innerUrls = new List<string>();
                hostNames = new List<string>();
                //Add both internal and external hosts together for now
                List<string> urls = m_urls[name];

                if (module.DoMultiplePorts)
                {
                    ports = m_ports[name];
                    foreach(var port in ports)
                        hostNames.Add(urls[lastSet[name]]);
                    foreach(uint port in ports)
                        innerUrls.Add(module.GetUrlForRegisteringClient(SessionID, port));
                    return;
                }

                if (lastSet[name] < urls.Count + m_externalUrlCountTotal)
                {
                    if (lastSet[name] < urls.Count)
                    {
                        //Internal, just pull it from the lists
                        hostNames.Add(urls[lastSet[name]]);
                        ports.Add(m_ports[name][lastSet[name]]);
                        innerUrls.Add(module.GetUrlForRegisteringClient (SessionID, ports[0]));
                    }
                    else
                    {
                        uint port;
                        string hostName;
                        string innerUrl;
                        //Get the external Info
                        if (!GetExternalInfo(lastSet[name], name, SessionID, out port, out hostName, out innerUrl))
                        {
                            lastSet[name] = 0;//It went through all external, give up on them
                            GetHost(name, module, SessionID, out ports, out hostNames, out innerUrls);
                            return;
                        }
                        else
                        {
                            ports.Add(port);
                            hostNames.Add(hostName);
                            innerUrls.Add(innerUrl);
                        }
                    }
                    lastSet[name]++;
                    if (lastSet[name] == (urls.Count + m_externalUrlCountTotal))
                        lastSet[name] = 0;
                }
                else
                {
                    //We don't have any urls for this name, return defaults
                    if (m_ports[name].Count > 0)
                    {
                        ports.Add(m_ports[name][lastSet[name]]);
                        lastSet[name]++;
                        if (lastSet[name] == urls.Count)
                            lastSet[name] = 0;

                        hostNames.Add(m_defaultHostname);
                    }
                    else
                    {
                        ports.Add(m_defaultPort);
                        hostNames.Add(m_defaultHostname);
                    }
                    innerUrls.Add(module.GetUrlForRegisteringClient (SessionID, ports[0]));
                }
            }

            private bool GetExternalInfo (int lastSet2, string name, string SessionID, out uint port, out string hostName, out string innerUrl)
            {
                port = 0;
                hostName = "";
                innerUrl = "";
                string externalURL = "";
                int currentCount = m_urls.Count;//Start at the end of the urls
                int i = 0;
                for (i = 0; i < m_remoteLoadBalancingInstances.Count; i++)
                {
                    if (currentCount + m_externalUrlCount[i] > lastSet2)
                    {
                        externalURL = m_remoteLoadBalancingInstances[i];
                        break;
                    }
                    currentCount += m_externalUrlCount[i];
                }
                if (externalURL == "")
                    return false;

                OSDMap resp = MakeGenericCall (externalURL, "GetExternalInfo", name, SessionID);
                if (resp == null)
                    //Try again
                    return GetExternalInfo ((currentCount + m_externalUrlCount[i]), SessionID, name, out port, out hostName, out innerUrl);
                port = resp["Port"];
                hostName = resp["HostName"];
                innerUrl = resp["InnerUrl"];
                lastSet[name] = lastSet2;//Fix this if it has changed
                return true;
            }

            /// <summary>
            /// Gets hostnames and ports from an external instance
            /// </summary>
            /// <param name="name"></param>
            private void GetExternalCounts (string name)
            {
                int count = 0;
                foreach (string url in m_remoteLoadBalancingInstances)
                {
                    OSDMap resp = MakeGenericCall (url, "GetExternalCounts", name, "");
                    if (resp != null)
                    {
                        m_externalUrlCountTotal += resp["Count"];
                        m_externalUrlCount[count] = resp["Count"];
                    }
                    else
                        m_externalUrlCount[count] = 0;
                    count++;
                }
            }

            private OSDMap MakeGenericCall (string url, string method, string param, string param2)
            {
                OSDMap request = new OSDMap ();
                request["Password"] = m_remotePassword;
                request["Method"] = method;
                request["Param"] = param;
                request["Param2"] = param2;
                return OSDParser.DeserializeJson(WebUtils.PostToService (url + "/LoadBalancing", request)) as OSDMap;
            }

            #endregion

            #region Remote Handlers

            public class RemoteLoadBalancingPostHandler : BaseRequestHandler
            {
                private readonly GridRegistrationService m_service;
                private readonly string m_password;

                public RemoteLoadBalancingPostHandler (string url, string password, GridRegistrationService gridReg) :
                    base ("POST", url)
                {
                    m_password = password;
                    m_service = gridReg;
                }

                public override byte[] Handle (string path, Stream requestData,
                        OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                {
                    StreamReader sr = new StreamReader (requestData);
                    string body = sr.ReadToEnd ();
                    sr.Close ();
                    body = body.Trim ();
                    OSDMap request = WebUtils.GetOSDMap (body);
                    if(request["Password"] != m_password)
                        return null;
                    OSDMap response = new OSDMap ();

                    switch (response["Method"].AsString())
                    {
                        case "GetExternalCounts":
                            response["Count"] = m_service.m_loadBalancer.m_urls[request["Param"]].Count;
                            break;
                        case "GetExternalInfo":
                            string moduleName = request["Param"];
                            string SessionID = request["Param2"];
                            if (m_service.m_modules.ContainsKey (moduleName))
                            {
                                List<uint> port;
                                List<string> hostName;
                                List<string> innerUrl;
                                m_service.m_loadBalancer.GetHost (moduleName, m_service.m_modules[moduleName], SessionID, out port, out hostName, out innerUrl);
                                response["HostName"] = hostName[0];
                                response["InnerUrl"] = innerUrl[0];
                                response["Port"] = port[0];
                            }
                            break;
                    }

                    return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString (response));
                }
            }

            #endregion
        }

        #endregion
    }
}
