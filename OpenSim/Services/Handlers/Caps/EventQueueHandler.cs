/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services
{
    public class EventQueueHandler : IService, IGridRegistrationUrlModule
    {
        private IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        public bool DoMultiplePorts { get { return false; } }

        #region IGridRegistrationUrlModule Members

        public string UrlName
        {
            get { return "EventQueueServiceURI"; }
        }

        public void AddExistingUrlForClient(string SessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);

            server.AddStreamHandler(new EQMEventPoster(url,
                                                       m_registry.RequestModuleInterface<IEventQueueService>().
                                                           InnerService,
                                                       m_registry.RequestModuleInterface<ICapsService>(), SessionID,
                                                       m_registry));
        }

        public string GetUrlForRegisteringClient(string SessionID, uint port)
        {
            string url = "/CAPS/EQMPOSTER" + UUID.Random();

            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);

            server.AddStreamHandler(new EQMEventPoster(url,
                                                       m_registry.RequestModuleInterface<IEventQueueService>().
                                                           InnerService,
                                                       m_registry.RequestModuleInterface<ICapsService>(), SessionID,
                                                       m_registry));
            return url;
        }

        public void RemoveUrlForClient(string sessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);
            server.RemoveHTTPHandler("POST", url);
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueInHandler", "") != Name)
                return;

            m_registry = registry;

            m_registry.RequestModuleInterface<IGridRegistrationService>().RegisterModule(this);
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }

    public class EQMEventPoster : BaseStreamHandler
    {
        private readonly string m_SessionID;
        private readonly ICapsService m_capsService;
        private readonly IEventQueueService m_eventQueueService;
        private readonly ulong m_ourRegionHandle;
        protected IRegistryCore m_registry;

        public EQMEventPoster(string url, IEventQueueService handler, ICapsService capsService, string SessionID,
                              IRegistryCore registry) :
                                  base("POST", url)
        {
            m_eventQueueService = handler;
            m_capsService = capsService;
            m_SessionID = SessionID;
            if (!ulong.TryParse(SessionID, out m_ourRegionHandle))
            {
                string[] split = SessionID.Split('|');
                if (split.Length == 2)
                    ulong.TryParse(split[1], out m_ourRegionHandle);
            }
            m_registry = registry;
        }

        public override byte[] Handle(string path, Stream requestData,
                                      OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            IGridRegistrationService urlModule =
                m_registry.RequestModuleInterface<IGridRegistrationService>();
            if (urlModule != null)
                if (!urlModule.CheckThreatLevel(m_SessionID, "EQM_Post", ThreatLevel.None))
                    return new byte[0];

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
                    events = (OSDArray) request["Events"];

#if (!ISWIN)
                List<OSD> OSDEvents = new List<OSD>();
                foreach (OSD ev in events)
                    OSDEvents.Add(OSDParser.DeserializeLLSDXml(ev.AsString()));
#else
                List<OSD> OSDEvents = events.Select(ev => OSDParser.DeserializeLLSDXml(ev.AsString())).ToList();
#endif

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
                    else
                        MainConsole.Instance.Error("[EQMHandler]: ERROR IN THE HANDLER, FAILED TO FIND CLIENT'S REGION");
                }
                else
                {
                    MainConsole.Instance.Error("[EQMHandler]: ERROR IN THE HANDLER, FAILED TO FIND CLIENT, IWC/HG OR BOT?");
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
            catch (Exception ex)
            {
                MainConsole.Instance.Error("[EQMHandler]: ERROR IN THE HANDLER: " + ex);
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