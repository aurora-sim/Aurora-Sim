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

using System.Linq;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Services.MessagingService;

namespace Aurora.Modules
{
    public class IWCMessagingServiceInHandler : IService, IAsyncMessageRecievedService, IGridRegistrationUrlModule
    {
        protected bool m_enabled;

        private IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        public bool DoMultiplePorts { get { return false; } }

        #region IAsyncMessageRecievedService Members

        public event MessageReceived OnMessageReceived;

        public OSDMap FireMessageReceived(string SessionID, OSDMap message)
        {
            OSDMap result = null;
            ulong reg;
            if (ulong.TryParse(SessionID, out reg)) //Local region
            {
                if (OnMessageReceived != null)
                {
                    MessageReceived eventCopy = OnMessageReceived;
                    foreach (OSDMap r in from MessageReceived messagedelegate in eventCopy.GetInvocationList() select messagedelegate(message) into r where r != null select r)
                    {
                        result = r;
                    }
                }
            }
            else //IWC
            {
                string[] session = SessionID.Split('|');
                ISyncMessagePosterService smps = m_registry.RequestModuleInterface<ISyncMessagePosterService>();
                //Forward it on
                result = smps.Get(message, UUID.Parse(session[0]), ulong.Parse(session[1]));
            }

            return result;
        }

        #endregion

        #region IGridRegistrationUrlModule Members

        public string UrlName
        {
            get { return "MessagingServerURI"; }
        }

        public void AddExistingUrlForClient(string SessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);

            server.AddStreamHandler(new MessagingServiceInPostHandler(url, m_registry, this, SessionID));
        }

        public string GetUrlForRegisteringClient(string SessionID, uint port)
        {
            string url = "/messagingservice" + UUID.Random();

            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);

            server.AddStreamHandler(new MessagingServiceInPostHandler(url, m_registry, this, SessionID));

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
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("MessagingServiceInHandler", "") != Name)
                return;
            registry.RegisterModuleInterface<IAsyncMessageRecievedService>(this);
            m_enabled = true;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (!m_enabled)
                return;

            m_registry = registry;
            m_registry.RequestModuleInterface<IGridRegistrationService>().RegisterModule(this);
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}