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
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Interfaces;
using Aurora.Framework.Services;
using Nini.Config;
using OpenMetaverse;
using System.Collections.Generic;
using System.Linq;

namespace Aurora.Services
{
    public class CapsService : ICapsService, IService
    {
        #region Declares

        /// <summary>
        ///     A list of all clients and their Client Caps Handlers
        /// </summary>
        protected Dictionary<UUID, IClientCapsService> m_ClientCapsServices = new Dictionary<UUID, IClientCapsService>();

        /// <summary>
        ///     A list of all regions Caps Services
        /// </summary>
        protected Dictionary<UUID, IRegionCapsService> m_RegionCapsServices = new Dictionary<UUID, IRegionCapsService>();

        protected IRegistryCore m_registry;

        public IRegistryCore Registry
        {
            get { return m_registry; }
        }

        protected IHttpServer m_server;

        public IHttpServer Server
        {
            get { return m_server; }
        }

        public string HostUri
        {
            get { return m_server.ServerURI; }
        }

        #endregion

        #region IService members

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("CapsHandler", "") != Name)
                return;
            m_registry = registry;
            registry.RegisterModuleInterface<ICapsService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            ISimulationBase simBase = registry.RequestModuleInterface<ISimulationBase>();
            m_server = simBase.GetHttpServer(0);

            if (MainConsole.Instance != null)
                MainConsole.Instance.Commands.AddCommand("show presences", "show presences",
                                                         "Shows all presences in the grid", ShowUsers);
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region Console Commands

        protected void ShowUsers(string[] cmd)
        {
            //Check for all or full to show child agents
            bool showChildAgents = cmd.Length == 3 && (cmd[2] == "all" || (cmd[2] == "full"));
#if (!ISWIN)
            int count = 0;
            foreach (IRegionCapsService regionCaps in m_RegionCapsServices.Values)
                foreach (IRegionClientCapsService client in regionCaps.GetClients())
                {
                    if ((client.RootAgent || showChildAgents)) count++;
                }
#else
            int count =
                m_RegionCapsServices.Values.SelectMany(regionCaps => regionCaps.GetClients())
                                    .Count(clientCaps => (clientCaps.RootAgent || showChildAgents));
#endif
            MainConsole.Instance.WarnFormat("{0} agents found: ", count);
            foreach (IClientCapsService clientCaps in m_ClientCapsServices.Values)
            {
                foreach (IRegionClientCapsService caps in clientCaps.GetCapsServices())
                {
                    if ((caps.RootAgent || showChildAgents))
                    {
                        MainConsole.Instance.InfoFormat("Region - {0}, User {1}, {2}, {3}",
                                                        caps.Region.RegionName, clientCaps.AccountInfo.Name,
                                                        caps.RootAgent ? "Root Agent" : "Child Agent",
                                                        caps.Disabled ? "Disabled" : "Not Disabled");
                    }
                }
            }
        }

        #endregion

        #region ICapsService members

        #region Client Caps

        /// <summary>
        ///     Remove the all of the user's CAPS from the system
        /// </summary>
        /// <param name="AgentID"></param>
        public void RemoveCAPS(UUID AgentID)
        {
            if (m_ClientCapsServices.ContainsKey(AgentID))
            {
                IClientCapsService perClient = m_ClientCapsServices[AgentID];
                perClient.Close();
                m_ClientCapsServices.Remove(AgentID);
                m_registry.RequestModuleInterface<ISimulationBase>()
                          .EventManager.FireGenericEventHandler("UserLogout", AgentID);
            }
        }

        /// <summary>
        ///     Create a Caps URL for the given user/region. Called normally by the EventQueueService or the LLLoginService on login
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="CAPSBase"></param>
        /// <param name="regionID"></param>
        /// <param name="IsRootAgent">Will this child be a root agent</param>
        /// <param name="circuitData"></param>
        /// <param name="port">The port to use for the CAPS service</param>
        /// <returns></returns>
        public string CreateCAPS(UUID AgentID, string CAPSBase, UUID regionID, bool IsRootAgent,
                                 AgentCircuitData circuitData, uint port)
        {
            //Now make sure we didn't use an old one or something
            IClientCapsService service = GetOrCreateClientCapsService(AgentID);
            IRegionClientCapsService clientService = service.GetOrCreateCapsService(regionID, CAPSBase, circuitData,
                                                                                    port);

            //Fix the root agent status
            clientService.RootAgent = IsRootAgent;

            m_registry.RequestModuleInterface<ISimulationBase>()
                      .EventManager.FireGenericEventHandler("UserLogin", AgentID);
            MainConsole.Instance.Debug("[CapsService]: Adding Caps URL " + clientService.CapsUrl + " for agent " +
                                       AgentID);
            return clientService.CapsUrl;
        }

        /// <summary>
        ///     Get or create a new Caps Service for the given client
        ///     Note: This does not add them to a region if one is created.
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        public IClientCapsService GetOrCreateClientCapsService(UUID AgentID)
        {
            if (!m_ClientCapsServices.ContainsKey(AgentID))
            {
                PerClientBasedCapsService client = new PerClientBasedCapsService();
                client.Initialise(this, AgentID);
                m_ClientCapsServices.Add(AgentID, client);
            }
            return m_ClientCapsServices[AgentID];
        }

        /// <summary>
        ///     Get a Caps Service for the given client
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        public IClientCapsService GetClientCapsService(UUID AgentID)
        {
            if (!m_ClientCapsServices.ContainsKey(AgentID))
                return null;
            bool disabled = true;
            foreach (IRegionClientCapsService regionClients in m_ClientCapsServices[AgentID].GetCapsServices())
            {
                if (!regionClients.Disabled)
                {
                    disabled = false;
                    break;
                }
            }
            if (disabled)
            {
                RemoveCAPS(AgentID);
                return null;
            }
            return m_ClientCapsServices[AgentID];
        }

        public List<IClientCapsService> GetClientsCapsServices()
        {
            return new List<IClientCapsService>(m_ClientCapsServices.Values);
        }

        #endregion

        #region Region Caps

        /// <summary>
        ///     Get a region handler for the given region
        /// </summary>
        /// <param name="regionID"></param>
        public IRegionCapsService GetCapsForRegion(UUID regionID)
        {
            IRegionCapsService service;
            if (m_RegionCapsServices.TryGetValue(regionID, out service))
            {
                return service;
            }
            return null;
        }

        /// <summary>
        ///     Create a caps handler for the given region
        /// </summary>
        /// <param name="regionID"></param>
        public void AddCapsForRegion(UUID regionID)
        {
            if (!m_RegionCapsServices.ContainsKey(regionID))
            {
                IRegionCapsService service = new PerRegionCapsService();
                service.Initialise(regionID, Registry);

                m_RegionCapsServices.Add(regionID, service);
            }
        }

        /// <summary>
        ///     Remove the handler for the given region
        /// </summary>
        /// <param name="regionID"></param>
        public void RemoveCapsForRegion(UUID regionID)
        {
            if (m_RegionCapsServices.ContainsKey(regionID))
                m_RegionCapsServices.Remove(regionID);
        }

        public List<IRegionCapsService> GetRegionsCapsServices()
        {
            return new List<IRegionCapsService>(m_RegionCapsServices.Values);
        }

        #endregion

        #endregion
    }
}