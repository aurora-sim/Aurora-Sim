using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.MessagingService
{
    public class AsyncMessageRequesterService : IService
    {
        protected List<string> m_hosts = new List<string>();
        protected IRegistryCore m_registry;

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AsyncMessageRequesterServiceHandler", "") != Name)
                return;

            m_hosts = registry.RequestModuleInterface<IConfigurationService>().FindValueOf("MessagingServerURI");
            m_registry = registry;

            //Start the request timer
            Timer timer = new Timer();
            timer.Elapsed += requestAsyncMessages;
            timer.Interval = 30 * 1000; //30 secs
            timer.Start();
        }

        void requestAsyncMessages(object sender, ElapsedEventArgs e)
        {
            OSDMap message = CreateWebRequest();
            IAsyncMessageRecievedService service = m_registry.RequestModuleInterface<IAsyncMessageRecievedService>();
            foreach (string host in m_hosts)
            {
                OSDMap retval = WebUtils.PostToService(host, message);
                //Clean it up
                retval = CreateWebResponse(retval);

                OSDArray messages = (OSDArray)retval["Messages"];
                foreach (OSD asyncMessage in messages)
                {
                    service.FireMessageReceived((OSDMap)asyncMessage);
                }
            }
        }

        private OSDMap CreateWebRequest()
        {
            OSDMap message = new OSDMap();
            message["Method"] = "AsyncMessageRequest";
            OSDMap request = new OSDMap();
            request["Method"] = "AsyncMessageRequest";
            message["Message"] = request;
            return message;
        }

        private OSDMap CreateWebResponse(OSDMap request)
        {
            OSDMap message = new OSDMap();
            message["Response"] = OSDParser.DeserializeJson(request["_RawResult"]);
            return message;
        }
    }
}
