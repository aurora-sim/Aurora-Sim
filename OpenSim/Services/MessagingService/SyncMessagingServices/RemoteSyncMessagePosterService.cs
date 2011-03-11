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
        protected IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("SyncMessagePosterServiceHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<ISyncMessagePosterService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #region ISyncMessagePosterService Members

        public void Post(OSDMap request, ulong RegionHandle)
        {
            OSDMap message = CreateWebRequest(request);
            string postInfo = OSDParser.SerializeJsonString(message);
            List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(RegionHandle.ToString(), "MessagingServerURI");
            foreach (string host in serverURIs)
            {
                //Send it async
                AsynchronousRestObjectRequester.MakeRequest("POST", host, postInfo);
            }
        }

        public OSDMap Get(OSDMap request, ulong RegionHandle)
        {
            OSDMap retval = null;
            OSDMap message = CreateWebRequest(request);
            List<string> serverURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(RegionHandle.ToString(), "MessagingServerURI");
            foreach (string host in serverURIs)
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
            if (request == null)
                return null;
            try
            {
                return (OSDMap)OSDParser.DeserializeJson (request["_RawResult"]);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
