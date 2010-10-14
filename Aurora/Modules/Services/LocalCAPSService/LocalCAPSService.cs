using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Caps = OpenSim.Framework.Capabilities.Caps;
using Aurora.DataManager;
using Aurora.Framework;
using Mono.Addins;
using OpenSim.Services.CapsService;
using OpenSim.Server.Base;

namespace Aurora.Modules
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class LocalCAPSService : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private AuroraCAPSHandler m_CapsService;

        public void PostInitialise()
        {
        }

        public string Name
        {
            get { return "LocalCAPSService"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["LocalCapsService"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("CapsService", "LocalCapsService");
                if (name == Name)
                {
                    string serviceDll = moduleConfig.GetString("LocalServiceModule",
                            String.Empty);

                    if (serviceDll == String.Empty)
                    {
                        m_log.Error("[LOCAL CAPS SERVICES CONNECTOR]: No LocalServiceModule named in section LocalCapsService");
                        return;
                    }

                    Object[] args = new Object[] { source };
                    m_CapsService = ServerUtils.LoadPlugin<AuroraCAPSHandler>(serviceDll, args);

                    if (m_CapsService == null)
                    {
                        m_log.Error("[LOCAL CAPS SERVICES CONNECTOR]: Can't load caps service");
                        return;
                    }
                }
            }
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
        }
    }
}
