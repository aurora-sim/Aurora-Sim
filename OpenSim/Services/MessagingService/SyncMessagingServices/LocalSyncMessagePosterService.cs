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

using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.MessagingService
{
    public class SyncMessagePosterService : ConnectorBase, ISyncMessagePosterService, IService
    {
        public string Name
        {
            get { return GetType().Name; }
        }

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("SyncMessagePosterServiceHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<ISyncMessagePosterService>(this);
            Init(registry, Name, serverPath: "/messaging/");
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region ISyncMessagePosterService Members

        public void Post(UUID regionID, OSDMap request)
        {
            if (m_doRemoteCalls)
            {
                Util.FireAndForget((o) =>
                {
                    PostInternal(regionID, request);
                });
            }
            else
                m_registry.RequestModuleInterface<ISyncMessageRecievedService>().FireMessageReceived(request);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void PostInternal(UUID regionId, OSDMap request)
        {
            if (m_doRemoteCalls)
                DoRemoteByURL("MessagingServerURI", request);
            else
                m_registry.RequestModuleInterface<ISyncMessageRecievedService>().FireMessageReceived(request);
        }

        public void PostToServer(OSDMap request)
        {
            if (m_doRemoteCalls)
            {
                Util.FireAndForget((o) =>
                {
                    PostToServerInternal(request);
                });
            }
            else
                m_registry.RequestModuleInterface<ISyncMessageRecievedService>().FireMessageReceived(request);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void PostToServerInternal(OSDMap request)
        {
            if(m_doRemoteCalls)
                DoRemoteByURL("MessagingServerURI", request);
            else
                m_registry.RequestModuleInterface<ISyncMessageRecievedService>().FireMessageReceived(request);
        }

        public void Get(UUID regionID, OSDMap request, GetResponse response)
        {
            if (m_doRemoteCalls)
            {
                Util.FireAndForget((o) =>
                {
                    response(Get(regionID, request));
                });
            }
            else
                response(m_registry.RequestModuleInterface<ISyncMessageRecievedService>().FireMessageReceived(request));
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public OSDMap Get(UUID regionID, OSDMap request)
        {
            return m_registry.RequestModuleInterface<ISyncMessageRecievedService>().FireMessageReceived(request);
        }

        #endregion

        private OSDMap CreateWebRequest(OSDMap request)
        {
            OSDMap message = new OSDMap();

            message["Method"] = "SyncPost";
            message["Message"] = request;

            return message;
        }
    }
}