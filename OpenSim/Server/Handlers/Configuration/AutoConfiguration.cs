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
    public class AutoConfigurationInHandler : IServiceConnector
    {
        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, ISimulationBase simBase, string configName, IRegistryCore sim)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AutoConfigurationInHandler", "") != Name)
                return;
            IConfig autoConfConfig = config.Configs["AutoConfiguration"];
            if (autoConfConfig == null)
                return;
            IHttpServer server = simBase.GetHttpServer((uint)handlerConfig.GetInt("AutoConfigurationInHandlerPort"));
            OSDMap configMap = new OSDMap();
            configMap["GridServerURI"] = autoConfConfig.GetString("GridServerURI", "");
            configMap["GridUserServerURI"] = autoConfConfig.GetString("GridUserServerURI", "");
            configMap["AssetServerURI"] = autoConfConfig.GetString("AssetServerURI", "");
            configMap["InventoryServerURI"] = autoConfConfig.GetString("InventoryServerURI", "");
            configMap["AvatarServerURI"] = autoConfConfig.GetString("AvatarServerURI", "");
            configMap["PresenceServerURI"] = autoConfConfig.GetString("PresenceServerURI", "");
            configMap["UserAccountServerURI"] = autoConfConfig.GetString("UserAccountServerURI", "");
            configMap["AuthenticationServerURI"] = autoConfConfig.GetString("AuthenticationServerURI", "");
            configMap["FriendsServerURI"] = autoConfConfig.GetString("FriendsServerURI", "");
            configMap["RemoteServerURI"] = autoConfConfig.GetString("RemoteServerURI", "");
            configMap["EventQueueServiceURI"] = autoConfConfig.GetString("EventQueueServiceURI", "");
            configMap["FreeswitchServiceURL"] = autoConfConfig.GetString("FreeswitchServiceURL", "");
            server.AddStreamHandler(new AutoConfigurationPostHandler(configMap));
        }
    }
}
