/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services
{
    public class AssetServiceConnector : IService, IGridRegistrationUrlModule
    {
        private string m_ConfigName = "AssetService";
        private bool m_allowDelete;
        private IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IGridRegistrationUrlModule Members

        public string UrlName
        {
            get { return "AssetServerURI"; }
        }

        public void AddExistingUrlForClient(string SessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);

            server.AddStreamHandler(new AssetServerGetHandler(GetService(SessionID != "").InnerService, url, SessionID,
                                                              m_registry));
            server.AddStreamHandler(new AssetServerPostHandler(GetService(SessionID != "").InnerService, url, SessionID,
                                                               m_registry));
            server.AddStreamHandler(new AssetServerDeleteHandler(GetService(SessionID != "").InnerService, m_allowDelete,
                                                                 url, SessionID, m_registry));
        }

        public string GetUrlForRegisteringClient(string SessionID, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);
            string url = "/assets" + UUID.Random();

            server.AddStreamHandler(new AssetServerGetHandler(GetService(SessionID != "").InnerService, url, SessionID,
                                                              m_registry));
            server.AddStreamHandler(new AssetServerPostHandler(GetService(SessionID != "").InnerService, url, SessionID,
                                                               m_registry));
            server.AddStreamHandler(new AssetServerDeleteHandler(GetService(SessionID != "").InnerService, m_allowDelete,
                                                                 url, SessionID, m_registry));

            return url;
        }

        public void RemoveUrlForClient(string sessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);
            server.RemoveHTTPHandler("POST", url);
            server.RemoveHTTPHandler("GET", url);
            server.RemoveHTTPHandler("DELETE", url);
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AssetInHandler", "") != Name)
                return;

            m_registry = registry;
            m_registry.RegisterModuleInterface(this);

            IConfig serverConfig = config.Configs[m_ConfigName];
            m_allowDelete = serverConfig != null && serverConfig.GetBoolean("AllowRemoteDelete", false);

            m_registry.RequestModuleInterface<IGridRegistrationService>().RegisterModule(this);
        }

        public void FinishedStartup()
        {
        }

        #endregion

        public IAssetService GetService(bool isSecure)
        {
            IAssetService assetService = m_registry.RequestModuleInterface<IExternalAssetService>();
            if (!isSecure && assetService != null)
                return assetService;
            return m_registry.RequestModuleInterface<IAssetService>();
        }
    }
}