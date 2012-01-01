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

using System.Collections.Generic;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.MessagingService
{
    public class RemoteSyncMessagePosterService : ISyncMessagePosterService, IService
    {
        protected IRegistryCore m_registry;

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
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region ISyncMessagePosterService Members

        public void Post(OSDMap request, ulong RegionHandle)
        {
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(RegionHandle.ToString(),
                                                                                       "MessagingServerURI");
            if (serverURIs.Count > 0)
            {
                OSDMap message = CreateWebRequest(request);
                string postInfo = OSDParser.SerializeJsonString(message);
                foreach (string host in serverURIs)
                {
                    //Send it async
                    AsynchronousRestObjectRequester.MakeRequest("POST", host, postInfo);
                }
            }
        }

        public OSDMap Get(OSDMap request, UUID userID, ulong RegionHandle)
        {
            OSDMap retval = null;
            OSDMap message = CreateWebRequest(request);
            List<string> serverURIs =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(userID.ToString(),
                                                                                       RegionHandle.ToString(),
                                                                                       "MessagingServerURI");
            foreach (string host in serverURIs)
            {
                retval = WebUtils.PostToService(host, message, true, false);
            }
            return CreateWebResponse(retval);
        }

        #endregion

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
                return (OSDMap) OSDParser.DeserializeJson(request["_RawResult"]);
            }
            catch
            {
                return null;
            }
        }
    }
}