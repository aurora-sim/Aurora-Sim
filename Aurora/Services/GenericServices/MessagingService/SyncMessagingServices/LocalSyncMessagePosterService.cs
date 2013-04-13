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

using Aurora.Framework;
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse.StructuredData;

namespace Aurora.Services
{
    public class SyncMessagePosterService : ConnectorBase, ISyncMessagePosterService, IService
    {
        public string Name
        {
            get { return GetType().Name; }
        }

        protected bool m_doRemote = false;

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("SyncMessagePosterServiceHandler", "") != Name)
                return;

            m_doRemote = handlerConfig.GetBoolean("SyncMessagePosterServiceDoRemote", false);
            registry.RegisterModuleInterface<ISyncMessagePosterService>(this);
            Init(registry, Name, serverPath: "/syncmessage/", serverHandlerName: "SyncMessageServerURI");
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region ISyncMessagePosterService Members

        public void Post(string url, OSDMap request)
        {
            if (m_doRemote)
            {
                Util.FireAndForget((o) => { PostInternal(true, url, request); });
            }
            else
                m_registry.RequestModuleInterface<ISyncMessageRecievedService>().FireMessageReceived(request);
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public void PostInternal(bool remote, string url, OSDMap request)
        {
            if (remote)
                DoRemoteCallPost(true, url + "/syncmessage/", false, url + "/syncmessage/", request);
            else
                m_registry.RequestModuleInterface<ISyncMessageRecievedService>().FireMessageReceived(request);
        }

        public void PostToServer(OSDMap request)
        {
            if (m_doRemote)
            {
                Util.FireAndForget((o) => { PostToServerInternal(true, request); });
            }
            else
                m_registry.RequestModuleInterface<ISyncMessageRecievedService>().FireMessageReceived(request);
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public void PostToServerInternal(bool remote, OSDMap request)
        {
            if (remote)
                DoRemoteCallPost(true, "SyncMessageServerURI", false, request);
            else
                m_registry.RequestModuleInterface<ISyncMessageRecievedService>().FireMessageReceived(request);
        }

        public void Get(string url, OSDMap request, GetResponse response)
        {
            if (m_doRemote)
            {
                Util.FireAndForget((o) => { response(GetInternal(true, url, request)); });
            }
            else
                response(m_registry.RequestModuleInterface<ISyncMessageRecievedService>().FireMessageReceived(request));
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public OSDMap GetInternal(bool remote, string url, OSDMap request)
        {
            if (remote)
            {
                if (url != "")
                {
                    url = (url.EndsWith("/syncmessage/") ? url : (url + "/syncmessage/"));
                    return DoRemoteCallGet(true, url, false, url, request) as OSDMap;
                }
                else
                    return DoRemoteCallGet(true, "SyncMessageServerURI", false, url, request) as OSDMap;
            }
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