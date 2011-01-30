using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Services.Connectors.ConfigurationService;

namespace Aurora.Modules.Communications.InterWorldComms
{
    public class IWCConfigurationService : ConfigurationService
    {
        protected Dictionary<string, OSDMap> m_knownUsers = new Dictionary<string, OSDMap>();

        public override string Name
        {
            get { return GetType().Name; }
        }

        public override void Initialize(ISimulationBase openSim)
        {
            m_config = openSim.ConfigSource;

            IConfig handlerConfig = m_config.Configs["Handlers"];
            if (handlerConfig.GetString("ConfigurationHandler", "") != Name)
                return;

            //Register us
            openSim.ApplicationRegistry.RegisterModuleInterface<IConfigurationService>(this);

            FindConfiguration(m_config.Configs["Configuration"]);
        }

        public override void AddNewUser(string userID, OSDMap urls)
        {
            m_knownUsers[userID] = urls;
        }

        public override List<string> FindValueOf(string userID, string key)
        {
            if (m_knownUsers.ContainsKey(userID))
            {
                List<string> urls = new List<string>();
                return base.FindValueOfFromOSDMap(key, m_knownUsers[userID]);
            }
            return base.FindValueOf(userID, key);
        }
    }
}
