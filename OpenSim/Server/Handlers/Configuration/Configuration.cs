using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Server.Handlers
{
    public class ConfigurationInHandler : IService
    {
        private OSDMap configMap = new OSDMap();
        private IConfig m_config = null;

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("ConfigurationInHandler", "") != Name)
                return;
            IConfig autoConfConfig = config.Configs["Configuration"];
            if (autoConfConfig == null)
                return;
            m_config = autoConfConfig;
            GetConfigFor("GridServerURI");
            GetConfigFor("GridUserServerURI");
            GetConfigFor("AssetServerURI");
            GetConfigFor("InventoryServerURI");
            GetConfigFor("AvatarServerURI");
            GetConfigFor("PresenceServerURI");
            GetConfigFor("UserAccountServerURI");
            GetConfigFor("AuthenticationServerURI");
            GetConfigFor("FriendsServerURI");
            GetConfigFor("RemoteServerURI");
            GetConfigFor("EventQueueServiceURI");
            GetConfigFor("FreeswitchServiceURL");
        }

        public void GetConfigFor(string name)
        {
            configMap[name] = m_config.GetString(name, "");
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostStart(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("ConfigurationInHandler", "") != Name)
                return;
            IConfig autoConfConfig = config.Configs["Configuration"];
            if (autoConfConfig == null)
                return;
            IHttpServer server = registry.RequestModuleInterface<ISimulationBase>().GetHttpServer((uint)handlerConfig.GetInt("ConfigurationInHandlerPort"));

            server.AddStreamHandler(new ConfigurationPostHandler(configMap));
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
        }
    }
}
