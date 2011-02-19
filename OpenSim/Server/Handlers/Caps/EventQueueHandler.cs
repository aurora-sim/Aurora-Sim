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
    public class EventQueueHandler : IService, IGridRegistrationUrlModule
    {
        #region IService Members

        private IRegistryCore m_registry;
        private uint m_port = 0;

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueInHandler", "") != Name)
                return;

            m_registry = registry;
            m_port = handlerConfig.GetUInt("EventQueueInHandlerPort");

            if (handlerConfig.GetBoolean("UnsecureUrls", false))
            {
                string url = "/CAPS/EQMPOSTER";

                IHttpServer server = registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(m_port);

                server.AddStreamHandler(new EQMEventPoster(url, registry.RequestModuleInterface<IEventQueueService>(),
                registry.RequestModuleInterface<ICapsService>(), 0));
            }
            m_registry.RequestModuleInterface<IGridRegistrationService>().RegisterModule(this);
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region IGridRegistrationUrlModule Members

        public string UrlName
        {
            get { return "EventQueueServiceURI"; }
        }

        public uint Port
        {
            get { return m_port; }
        }

        public void AddExistingUrlForClient(UUID SessionID, ulong RegionHandle, string url)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(m_port);

            server.AddStreamHandler(new EQMEventPoster(url, m_registry.RequestModuleInterface<IEventQueueService>(),
                    m_registry.RequestModuleInterface<ICapsService>(), RegionHandle));
        }

        public string GetUrlForRegisteringClient(UUID SessionID, ulong RegionHandle)
        {
            string url = "/CAPS/EQMPOSTER" + UUID.Random();

            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(m_port);

            server.AddStreamHandler(new EQMEventPoster(url, m_registry.RequestModuleInterface<IEventQueueService>(),
                    m_registry.RequestModuleInterface<ICapsService>(), RegionHandle));
            return url;
        }

        #endregion
    }

    public class EQMEventPoster : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IEventQueueService m_eventQueueService;
        private ICapsService m_capsService;
        private ulong m_ourRegionHandle;

        public EQMEventPoster(string url, IEventQueueService handler, ICapsService capsService, ulong handle) :
            base("POST", url)
        {
            m_eventQueueService = handler;
            m_capsService = capsService;
            m_ourRegionHandle = handle;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            OSDMap request = WebUtils.GetOSDMap(body);
            if (request == null)
                return null;

            return ProcessEnqueueEQMMessage(request);
        }

        private byte[] ProcessEnqueueEQMMessage(OSDMap request)
        {
            OSDMap response = new OSDMap();
            response["success"] = false;
            try
            {
                UUID agentID = request["AgentID"].AsUUID();
                ulong regionHandle = m_ourRegionHandle == 0 ? request["RegionHandle"].AsULong() : m_ourRegionHandle;
                OSDArray events = new OSDArray();
                if (request.ContainsKey("Events") && request["Events"].Type == OSDType.Array)
                    events = (OSDArray)request["Events"];

                List<OSD> OSDEvents = new List<OSD>();

                foreach (OSD ev in events)
                {
                    OSD Event = OSDParser.DeserializeLLSDXml(ev.AsString());
                    OSDEvents.Add(Event);
                }

                IClientCapsService clientCaps = m_capsService.GetClientCapsService(agentID);
                if (clientCaps != null)
                {
                    IRegionClientCapsService regionClient = clientCaps.GetCapsService(regionHandle);
                    if (regionClient != null)
                    {
                        bool enqueueResult = false;
                        foreach (OSD ev in OSDEvents)
                        {
                            enqueueResult = m_eventQueueService.Enqueue(ev, agentID, regionHandle);
                            if (!enqueueResult) //Break if one fails
                                break;
                        }
                        response["success"] = enqueueResult;
                    }
                }
            }
            catch(Exception ex)
            {
                m_log.Error("[EQMHandler]: ERROR IN THE HANDLER: " + ex.ToString());
                response = new OSDMap();
                response["success"] = false;
            }
            string resp = OSDParser.SerializeJsonString(response);
            if (resp == "")
                return new byte[0];
            return Util.UTF8.GetBytes(resp);
        }
    }
}
