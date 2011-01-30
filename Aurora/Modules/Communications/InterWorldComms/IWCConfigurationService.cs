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
            string resp = "";
            m_config = openSim.ConfigSource;

            IConfig handlerConfig = m_config.Configs["Handlers"];
            if (handlerConfig.GetString("ConfigurationHandler", "") != Name)
                return;

            //Register by default as this only gets used in remote grid mode
            openSim.ApplicationRegistry.RegisterModuleInterface<IConfigurationService>(this);

            IConfig autoConfig = m_config.Configs["Configuration"];
            if (autoConfig == null)
                return;

            while (resp == "")
            {
                string serverURL = autoConfig.GetString("ConfigurationURL", "");
                //Clean up the URL so that it isn't too hard for users
                serverURL = serverURL.EndsWith("/") ? serverURL.Remove(serverURL.Length - 1) : serverURL;
                serverURL += "/autoconfig";
                resp = SynchronousRestFormsRequester.MakeRequest("POST", serverURL, "");

                if (resp == "")
                {
                    m_log.ErrorFormat("[Configuration]: Failed to find the configuration for {0}!"
                        + " This may break this startup!", serverURL);
                    MainConsole.Instance.CmdPrompt("Press enter when you are ready to continue.");
                }
            }

            m_autoConfig = (OSDMap)OSDParser.DeserializeJson(resp); base.Initialize(openSim);
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
