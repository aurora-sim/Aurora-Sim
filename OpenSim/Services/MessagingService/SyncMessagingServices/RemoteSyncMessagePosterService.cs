using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.MessagingService
{
    public class RemoteSyncMessagePosterService : ISyncMessagePosterService, IService
    {
        protected List<string> m_hosts = new List<string>();

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("SyncMessagePosterServiceHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<ISyncMessagePosterService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_hosts = registry.RequestModuleInterface<IConfigurationService>().FindValueOf("MessagingServerURI");
        }

        public void FinishedStartup()
        {
        }

        #region ISyncMessagePosterService Members

        public void Post(OSDMap request)
        {
            OSDMap message = CreateWebRequest(request);
            string postInfo = OSDParser.SerializeJsonString(message);
            foreach (string host in m_hosts)
            {
                //Send it async
                AsynchronousRestObjectRequester.MakeRequest("POST", host, postInfo);
            }
        }

        public OSDMap Get(OSDMap request)
        {
            OSDMap retval = null;
            OSDMap message = CreateWebRequest(request);
            foreach (string host in m_hosts)
            {
                retval = WebUtils.PostToService(host, message);
            }
            return CreateWebResponse(retval);
        }

        private OSDMap CreateWebRequest(OSDMap request)
        {
            OSDMap message = new OSDMap();

            message["Method"] = "SyncPost";
            message["Message"] = request;

            return message;
        }

        private OSDMap CreateWebResponse(OSDMap request)
        {
            OSDMap message = new OSDMap();
            message["Response"] = OSDParser.DeserializeJson(request["_RawResult"]);
            return message;
        }

        #endregion
    }
}
