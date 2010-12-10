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

namespace OpenSim.Services.Connectors.AutoConfiguration
{
    public class AutoConfigurationService : IAutoConfigurationService, IService
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

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            //Register by default as this only gets used in remote grid mode
            registry.RegisterInterface<IAutoConfigurationService>(this);

            m_config = config;

            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AutoConfigurationHandler", "") != Name)
                return;

            IConfig autoConfig = config.Configs["AutoConfiguration"];
            if (autoConfig == null)
                return;

            string serverURL = autoConfig.GetString("AutoConfigurationURL", "");
            //Clean up the URL so that it isn't too hard for users
            serverURL = serverURL.EndsWith("/") ? serverURL.Remove(serverURL.Length - 1) : serverURL;
            serverURL += "/autoconfig";
            string resp = SynchronousRestFormsRequester.MakeRequest("POST", serverURL, "");

            if (resp == "")
            {
                m_log.ErrorFormat("[AutoConfiguration]: Failed to find the configuration for {0}! This may break this startup!", serverURL);
                return;
            }

            m_autoConfig = (OSDMap)OSDParser.DeserializeJson(resp);
        }

        public void PostInitialize(IRegistryCore registry)
        {
        }

        public string FindValueOf(string key, string configurationSource)
        {
            if (m_autoConfig.ContainsKey(key))
                return m_autoConfig[key].AsString();

            IConfig config = m_config.Configs[configurationSource];
            if(config == null)
                return "";

            return config.GetString(key, "");
        }
    }
}
