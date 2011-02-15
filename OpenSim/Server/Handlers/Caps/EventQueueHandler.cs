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

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueInHandler", "") != Name)
                return;
            IHttpServer server = registry.RequestModuleInterface<ISimulationBase>().GetHttpServer((uint)handlerConfig.GetInt("EventQueueInHandlerPort"));

            server.AddStreamHandler(new EQMEventPoster(registry.RequestModuleInterface<IEventQueueService>(),
                registry.RequestModuleInterface<ICapsService>()));
        }

        #endregion
    }

    public class EQMEventPoster : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IEventQueueService m_eventQueueService;
        private ICapsService m_capsService;

        public EQMEventPoster(IEventQueueService handler, ICapsService capsService) :
            base("POST", "/CAPS/EQMPOSTER")
        {
            m_eventQueueService = handler;
            m_capsService = capsService;
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
                ulong regionHandle = request["RegionHandle"].AsULong();
                UUID password = request["Password"].AsUUID();
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
                        if (regionClient.Password != password)
                        {
                            m_log.Error("[EventQueueHandler]: Failed to authenticate EventQueueMessage for user " +
                                agentID + " calling with password " + password + " in region " + regionHandle);
                            response["success"] = false;
                        }
                        else
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
