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
using System.Text;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.MessagingService
{
    public class MessagingServiceInHandler : IService, IAsyncMessageRecievedService, IGridRegistrationUrlModule
    {
        protected bool m_enabled = false;
        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterModuleInterface<IAsyncMessageRecievedService>(this);
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("MessagingServiceInHandler", "") != Name)
                return;
            m_enabled = true;
        }

        private IRegistryCore m_registry;
        public string Name
        {
            get { return GetType().Name; }
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (!m_enabled)
                return;
            IConfig handlerConfig = config.Configs["Handlers"];

            m_registry = registry;

            m_registry.RequestModuleInterface<IGridRegistrationService>().RegisterModule(this);
        }

        public void FinishedStartup()
        {
        }

        #region IAsyncMessageRecievedService Members

        public event MessageReceived OnMessageReceived;

        #endregion

        public OSDMap FireMessageReceived(OSDMap message)
        {
            OSDMap result = null;
            if (OnMessageReceived != null)
            {
                MessageReceived eventCopy = OnMessageReceived;
                foreach (MessageReceived messagedelegate in eventCopy.GetInvocationList ())
                {
                    OSDMap r = messagedelegate(message);
                    if (r != null)
                        result = r;
                }
            }
            return result;
        }

        #region IGridRegistrationUrlModule Members

        public string UrlName
        {
            get { return "MessagingServerURI"; }
        }

        public void AddExistingUrlForClient(string SessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);

            server.AddStreamHandler (new MessagingServiceInPostHandler (url, m_registry, this, SessionID));
        }

        public string GetUrlForRegisteringClient (string SessionID, uint port)
        {
            string url = "/messagingservice" + UUID.Random();

            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);

            server.AddStreamHandler (new MessagingServiceInPostHandler (url, m_registry, this, SessionID));

            return url;
        }

        public void RemoveUrlForClient (string sessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);
            server.RemoveHTTPHandler("POST", url);
        }

        #endregion
    }
}
