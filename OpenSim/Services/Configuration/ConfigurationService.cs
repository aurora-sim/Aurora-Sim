using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using log4net;

namespace OpenSim.Services.ConfigurationService
{
    /// <summary>
    /// This is an application plugin so that it loads asap as it is used by many things (IService modules especially)
    /// </summary>
    public class ConfigurationService : IConfigurationService, IApplicationPlugin
    {
        #region Declares

        protected static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
        protected IConfigSource m_config;
        protected OSDMap m_autoConfig = new OSDMap();
        protected Dictionary<string, OSDMap> m_allConfigs = new Dictionary<string, OSDMap>();
        protected Dictionary<string, OSDMap> m_knownUsers = new Dictionary<string, OSDMap>();

        #endregion

        #region IApplicationPlugin Members

        public void Initialize(ISimulationBase openSim)
        {
            m_config = openSim.ConfigSource;

            IConfig handlerConfig = m_config.Configs["Handlers"];
            if (handlerConfig.GetString("ConfigurationHandler", "") != Name)
                return;

            //Register us
            openSim.ApplicationRegistry.RegisterModuleInterface<IConfigurationService>(this);

            FindConfiguration(m_config.Configs["Configuration"]);
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
        }

        public void PostStart()
        {
        }

        public void Close()
        {
        }

        public void ReloadConfiguration(IConfigSource m_config)
        {
            IConfig handlerConfig = m_config.Configs["Handlers"];
            if (handlerConfig.GetString("ConfigurationHandler", "") != Name)
                return;

            FindConfiguration(m_config.Configs["Configuration"]);
        }

        public void Dispose()
        {
        }

        #endregion

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        protected void FindConfiguration(IConfig autoConfig)
        {
            if (autoConfig == null)
                return;

            string serverURL = autoConfig.GetString("RegistrationURI", "");
            OSDMap request = new OSDMap();
            if (serverURL == "")
            {
                //Get the urls from the config
                GetConfigFor("GridServerURI", request);
                request["RegistrationURI"] = request["GridServerURI"];
                GetConfigFor("GridUserServerURI", request);
                GetConfigFor("AssetServerURI", request);
                GetConfigFor("InventoryServerURI", request);
                GetConfigFor("AvatarServerURI", request);
                GetConfigFor("PresenceServerURI", request);
                GetConfigFor("UserAccountServerURI", request);
                GetConfigFor("AuthenticationServerURI", request);
                GetConfigFor("FriendsServerURI", request);
                GetConfigFor("RemoteServerURI", request);
                GetConfigFor("EventQueueServiceURI", request);
                AddNewUrls("default", request);
            }
            else
            {
                GetConfigFor("RegistrationURI", request);
                AddNewUrls("default", request);
            }
        }

        public void GetConfigFor(string name, OSDMap request)
        {
            request[name] = m_config.Configs["Configuration"].GetString(name, "");
        }

        public virtual void AddNewUser(string userID, OSDMap urls)
        {
            m_knownUsers[userID] = urls;
        }

        public virtual void AddNewUrls(string key, OSDMap urls)
        {
            foreach (KeyValuePair<string, OSD> kvp in urls)
            {
                if (!m_autoConfig.ContainsKey(kvp.Key))
                    m_autoConfig[kvp.Key] = kvp.Value;
                else
                {
                    //Combine with ',' seperating
                    m_autoConfig[kvp.Key] = m_autoConfig[kvp.Key].AsString() + "," + kvp.Value.AsString();
                }
            }
            m_allConfigs[key] = urls;
        }

        public virtual OSDMap GetValues()
        {
            return m_autoConfig;
        }

        public virtual OSDMap GetValuesFor(string key)
        {
            return m_allConfigs[key];
        }

        public virtual List<string> FindValueOf(string key)
        {
            List<string> keys = new List<string>();

            if (m_autoConfig.ContainsKey(key))
            {
                keys = FindValueOfFromOSDMap(key, m_autoConfig);
            }
            else
            {
                keys = FindValueOfFromConfiguration(key);
            }
            return keys;
        }

        public virtual List<string> FindValueOf(string userID, string key)
        {
            if (m_knownUsers.ContainsKey(userID))
            {
                List<string> urls = new List<string>();
                return FindValueOfFromOSDMap(key, m_knownUsers[userID]);
            }
            else if (m_allConfigs.ContainsKey(userID))
            {
                List<string> urls = new List<string>();
                return FindValueOfFromOSDMap(key, m_allConfigs[userID]);
            }
            else
            {
                foreach (string name in m_allConfigs.Keys)
                {
                    if (m_allConfigs[name].ContainsKey(key))
                    {
                        return FindValueOfFromOSDMap(key, m_allConfigs[name]);
                    }
                }
            }
            return FindValueOf(key);
        }

        public virtual List<string> FindValueOf(string userID, string regionID, string key)
        {
            if (m_knownUsers.ContainsKey(userID))
            {
                List<string> urls = new List<string>();
                return FindValueOfFromOSDMap(key, m_knownUsers[userID]);
            }
            else if (m_allConfigs.ContainsKey(userID))
            {
                List<string> urls = new List<string>();
                return FindValueOfFromOSDMap(key, m_allConfigs[userID]);
            }
            else
            {
                if (m_knownUsers.ContainsKey(regionID))
                {
                    List<string> urls = new List<string>();
                    return FindValueOfFromOSDMap(key, m_knownUsers[regionID]);
                }
                else if (m_allConfigs.ContainsKey(regionID))
                {
                    List<string> urls = new List<string>();
                    return FindValueOfFromOSDMap(key, m_allConfigs[regionID]);
                }
                foreach (string name in m_allConfigs.Keys)
                {
                    if (m_allConfigs[name].ContainsKey(key))
                    {
                        return FindValueOfFromOSDMap(key, m_allConfigs[name]);
                    }
                }
            }
            return FindValueOf(key);
        }

        public virtual List<string> FindValueOfFromOSDMap(string key, OSDMap urls)
        {
            List<string> keys = new List<string>();

            string[] configKeys = urls[key].AsString().Split(',');
            keys.AddRange(configKeys);

            return keys;
        }

        public virtual List<string> FindValueOfFromConfiguration(string key)
        {
            List<string> keys = new List<string>();

            if (m_config.Configs["Configuration"] != null)
            {
                string[] configKeys = m_config.Configs["Configuration"].GetString(key, "").Split(',');
                keys.AddRange(configKeys);
            }

            return keys;
        }
    }
}
