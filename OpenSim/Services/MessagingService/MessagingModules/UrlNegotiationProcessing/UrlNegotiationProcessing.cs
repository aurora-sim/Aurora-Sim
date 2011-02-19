using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Services.MessagingService.MessagingModules.GridWideMessage
{
    public class UrlNegotiationProcessing : IService
    {
        #region Declares

        protected IRegistryCore m_registry;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            //MainConsole.Instance.Commands.AddCommand("UrlNegotiationProcessing", true, "switch servers",
            //    "switch servers", "Moves all regions to use a new URL base", NegotiateUrls);

            //Also look for incoming messages to display
            registry.RequestModuleInterface<IAsyncMessageRecievedService>().OnMessageReceived += OnMessageReceived;
        }

        public void FinishedStartup()
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region Commands

        protected void NegotiateUrls(string module, string[] cmd)
        {
            //Combine the params and figure out the message
            string message = CombineParams(cmd, 2);

            //Get required interfaces
            IAsyncMessagePostService messagePost = m_registry.RequestModuleInterface<IAsyncMessagePostService>();
            ICapsService capsService = m_registry.RequestModuleInterface<ICapsService>();
            List<IRegionCapsService> clients = capsService.GetRegionsCapsServices();

            //Go through all clients, and send the message asyncly to all agents that are root
            foreach (IRegionCapsService client in clients)
            {
                //Send the message to the region
                messagePost.Post(client.RegionHandle, BuildRequest("NegotiateUrl", message, UUID.Zero.ToString()));
            }
        }

        private string CombineParams(string[] commandParams, int pos)
        {
            string result = string.Empty;
            for (int i = pos; i < commandParams.Length; i++)
            {
                result += commandParams[i] + " ";
            }

            return result;
        }

        private OSDMap BuildRequest(string name, string value, string user)
        {
            OSDMap map = new OSDMap();

            map["Method"] = name;
            map["Value"] = value;
            map["User"] = user;

            return map;
        }

        #endregion

        #region Message Received

        protected OSDMap OnMessageReceived(OSDMap message)
        {
            if (message.ContainsKey("Method") && message["Method"] == "NegotiateUrl")
            {
                //We got a message, now display it
                string user = message["User"].AsString();
                string value = message["Value"].AsString();

                IConfigurationService service = m_registry.RequestModuleInterface<IConfigurationService>();
                if (service != null)
                    service.AddNewUrls("default", OSDParser.DeserializeJson(value));
            }
            return null;
        }

        #endregion
    }
}
