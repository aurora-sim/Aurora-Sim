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
    public class SimulationServiceInConnector : IService
    {
        private ISimulationService m_LocalSimulationService;
        private IConfigSource m_config;
        private IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_config = config;
            m_registry = registry;
            registry.RequestModuleInterface<ISimulationBase>().EventManager.RegisterEventHandler("PreRegisterRegion",
                                                                                                 EventManager_OnGenericEvent);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion

        private object EventManager_OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName != "PreRegisterRegion")
                return null;
            IConfig handlerConfig = m_config.Configs["Handlers"];
            if (handlerConfig.GetString("SimulationInHandler", "") != Name)
                return null;

            if (m_LocalSimulationService != null)
                return null;

            bool secure = handlerConfig.GetBoolean("SecureSimulation", true);
            IHttpServer server =
                m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(
                    (uint) handlerConfig.GetInt("SimulationInHandlerPort", 0));

            m_LocalSimulationService = m_registry.RequestModuleInterface<ISimulationService>();

            string path = "/" + UUID.Random().ToString() + "/agent/";

            IGridRegisterModule registerModule = m_registry.RequestModuleInterface<IGridRegisterModule>();
            if (registerModule != null && secure)
                registerModule.AddGenericInfo("SimulationAgent", path);
            else
            {
                secure = false;
                path = "/agent/";
            }

            server.AddHTTPHandler(path,
                                  new AgentHandler(m_LocalSimulationService.GetInnerService(), m_registry, secure).
                                      Handler);
            server.AddHTTPHandler("/object/",
                                  new ObjectHandler(m_LocalSimulationService.GetInnerService(), m_config).Handler);
            return null;
        }
    }
}