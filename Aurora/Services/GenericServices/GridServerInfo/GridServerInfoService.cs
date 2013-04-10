using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.Servers;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aurora.Services.GenericServices
{
    public class GridServerInfoService : ConnectorBase, IGridServerInfoService, IService
    {
        protected Dictionary<string, List<string>> m_gridURIs = new Dictionary<string, List<string>>();
        protected bool m_remoteCalls = false, m_enabled = false;
        protected int m_defaultURICount = 11;

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig conf = config.Configs["GridServerInfoService"];
            if (conf == null || !conf.GetBoolean("Enabled"))
                return;
            m_enabled = true;
            registry.RegisterModuleInterface<IGridServerInfoService>(this);
            m_remoteCalls = conf.GetBoolean("DoRemote");
            m_defaultURICount = conf.GetInt("DefaultURICount", m_defaultURICount);
            Init(registry, GetType().Name);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
            if (!m_enabled) return;
            Dictionary<string, string> uris = new Dictionary<string, string>();
            foreach (ConnectorBase connector in ConnectorRegistry.ServerHandlerConnectors)
            {
                uris.Add(connector.ServerHandlerName, MainServer.Instance.FullHostName + ":" +
                    connector.ServerHandlerPort + connector.ServerHandlerPath);
            }
            new Timer(SendGridURIsAsync, uris, 3000, System.Threading.Timeout.Infinite);
        }

        private void SendGridURIsAsync(object state)
        {
            SendGridURIs((Dictionary<string, string>)state);
        }

        public List<string> GetGridURIs(string key)
        {
            if (!m_gridURIs.ContainsKey(key))
                return new List<string>();
            return m_gridURIs[key];
        }

        public string GetGridURI(string key)
        {
            if (!m_gridURIs.ContainsKey(key))
                return "";
            return m_gridURIs[key][0];
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.High)]
        public Dictionary<string, List<string>> RetrieveAllGridURIs(bool secure)
        {
            if (m_remoteCalls)
                return (Dictionary<string, List<string>>)base.DoRemoteCallGet(true, "ServerURI", secure);

            if (m_gridURIs.Count < m_defaultURICount)
            {
                MainConsole.Instance.WarnFormat("[GridServerInfoService]: Retrieve URIs failed, only had {0} of {1} URIs needed", m_gridURIs.Count, m_defaultURICount);
                return new Dictionary<string, List<string>>();
            }

            if (secure)
                return m_gridURIs;
            else
            {
                Dictionary<string, List<string>> uris = new Dictionary<string, List<string>>();
                foreach (KeyValuePair<string, List<string>> kvp in m_gridURIs)
                    if (kvp.Key != "ExternalCaps")
                        uris.Add(kvp.Key, new List<string>(kvp.Value));
                return uris;
            }
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.High)]
        public void SendGridURIs(Dictionary<string, string> uri)
        {
            if (m_remoteCalls)
            {
                base.DoRemoteCallPost(true, "ServerURI", uri);
                return;
            }


            MainConsole.Instance.InfoFormat("[GridServerInfoService]: Adding {0} uris", uri.Count);

            foreach (KeyValuePair<string, string> kvp in uri)
            {
                if (!m_gridURIs.ContainsKey(kvp.Key))
                    m_gridURIs.Add(kvp.Key, new List<string>());
                m_gridURIs[kvp.Key].Add(kvp.Value);
            }

            m_registry.RequestModuleInterface<IGridInfo>().UpdateGridInfo();
        }

        public void AddURI(string key, string value)
        {
            if (m_remoteCalls)
            {
                new Timer((o) => AddURIInternal(key, value), null, 2000, System.Threading.Timeout.Infinite);
                return;
            }

            AddURIInternal(key, value);
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.High)]
        public void AddURIInternal(string key, string value)
        {
            if (m_remoteCalls)
            {
                base.DoRemoteCallPost(true, "ServerURI", key, value);
                return;
            }

            if (!m_gridURIs.ContainsKey(key))
                m_gridURIs.Add(key, new List<string>());
            m_gridURIs[key].Add(value);
            m_registry.RequestModuleInterface<IGridInfo>().UpdateGridInfo();

            MainConsole.Instance.InfoFormat("[GridServerInfoService]: Adding 1 uri");
        }
    }
}
