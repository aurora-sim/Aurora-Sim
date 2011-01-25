using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using log4net;

namespace OpenSim.Services.Connectors.ConfigurationService
{
    public class ConfigurationService : IConfigurationService, IApplicationPlugin
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
        protected IConfigSource m_config;
        protected OSDMap m_autoConfig = new OSDMap();

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(ISimulationBase openSim)
        {
            //Register by default as this only gets used in remote grid mode
            openSim.ApplicationRegistry.RegisterModuleInterface<IConfigurationService>(this);

            m_config = openSim.ConfigSource;

            IConfig handlerConfig = m_config.Configs["Handlers"];
            if (handlerConfig.GetString("ConfigurationHandler", "") != Name)
                return;

            IConfig autoConfig = m_config.Configs["Configuration"];
            if (autoConfig == null)
                return;

            string serverURL = autoConfig.GetString("ConfigurationURL", "");
            //Clean up the URL so that it isn't too hard for users
            serverURL = serverURL.EndsWith("/") ? serverURL.Remove(serverURL.Length - 1) : serverURL;
            serverURL += "/autoconfig";
            string resp = SynchronousRestFormsRequester.MakeRequest("POST", serverURL, "");

            if (resp == "")
            {
                m_log.ErrorFormat("[Configuration]: Failed to find the configuration for {0}! This may break this startup!", serverURL);
                return;
            }

            m_autoConfig = (OSDMap)OSDParser.DeserializeJson(resp);
        }

        public void ReloadConfiguration(IConfigSource config)
        {
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

        public void Dispose()
        {
        }

        public List<string> FindValueOf(string key)
        {
            List<string> keys = new List<string>();

            if (m_autoConfig.ContainsKey(key))
            {
                string[] configKeys = m_autoConfig[key].AsString().Split(',');
                keys.AddRange(configKeys);
            }
            else
            {
                //We can safely assume that because we are registered, this will not be null
                string[] configKeys = m_config.Configs["Configuration"].GetString(key, "").Split(',');
                keys.AddRange(configKeys);
            }
            return keys;
        }
    }
}
