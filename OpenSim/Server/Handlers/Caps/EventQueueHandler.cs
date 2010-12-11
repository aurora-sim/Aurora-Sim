using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;

namespace OpenSim.Server.Handlers
{
    public class EventQueueHandler : IService
    {
        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueInHandler", "") != Name)
                return;
            IHttpServer server = registry.Get<ISimulationBase>().GetHttpServer((uint)handlerConfig.GetInt("EventQueueInHandlerPort"));
            ICapsService service = registry.Get<ICapsService>();
            server.AddStreamHandler(new EQMEventPoster(service));
        }

        #endregion
    }

    public class EQMEventPoster : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ICapsService m_handler;

        public EQMEventPoster(ICapsService handler) :
            base("POST", "/CAPS/EQMPOSTER")
        {
            m_handler = handler;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            //m_log.DebugFormat("[XXX]: query String: {0}", body);
            string method = string.Empty;
            try
            {
                Dictionary<string, object> request = new Dictionary<string, object>();
                request = WebUtils.ParseQueryString(body);
                if (request.Count == 1)
                    request = WebUtils.ParseXmlResponse(body);
                object value = null;
                request.TryGetValue("<?xml version", out value);
                if (value != null)
                    request = WebUtils.ParseXmlResponse(body);

                return ProcessEnqueueEQMMessage(request);
            }
            catch (Exception)
            {
            }

            return null;

        }

        private byte[] ProcessEnqueueEQMMessage(Dictionary<string, object> m_dhttpMethod)
        {
            UUID agentID = UUID.Parse((string)m_dhttpMethod["AGENTID"]);
            ulong regionHandle = ulong.Parse((string)m_dhttpMethod["REGIONHANDLE"]);
            UUID password = UUID.Parse((string)m_dhttpMethod["PASS"]);
            string llsd = (string)m_dhttpMethod["LLSD"];
            //This is called by the user server
            IPrivateCapsService privateCaps = m_handler.GetCapsService(regionHandle, agentID);

            if (!privateCaps.EventQueueService.AuthenticateRequest(agentID, password))
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                result.Add("result", "false");
                string xmlString = WebUtils.BuildXmlResponse(result);
                UTF8Encoding encoding = new UTF8Encoding();
                return encoding.GetBytes(xmlString);
            }
            else
            {
                privateCaps.EventQueueService.Enqueue(OSDParser.DeserializeLLSDXml(llsd), agentID);
                Dictionary<string, object> result = new Dictionary<string, object>();
                result.Add("result", "true");
                string xmlString = WebUtils.BuildXmlResponse(result);
                UTF8Encoding encoding = new UTF8Encoding();
                return encoding.GetBytes(xmlString);
            }
        }
    }
}
