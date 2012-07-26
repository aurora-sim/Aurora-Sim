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
using System.Linq;
using System.Net;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.CapsService
{
    public class PerClientBasedCapsService : IClientCapsService
    {
        protected ICapsService m_CapsService;

        protected Dictionary<ulong, IRegionClientCapsService> m_RegionCapsServices =
            new Dictionary<ulong, IRegionClientCapsService>();

        protected UserAccount m_account;
        protected UUID m_agentID;
        protected bool m_callbackHasCome;
        protected IPEndPoint m_clientEndPoint;
        protected bool m_inTeleport;
        protected bool m_requestToCancelTeleport;

        #region IClientCapsService Members

        public UUID AgentID
        {
            get { return m_agentID; }
        }

        public IPEndPoint ClientEndPoint
        {
            get { return m_clientEndPoint; }
        }

        public UserAccount AccountInfo
        {
            get { return m_account; }
        }

        public bool InTeleport
        {
            get { return m_inTeleport; }
            set { m_inTeleport = value; }
        }

        public bool RequestToCancelTeleport
        {
            get { return m_requestToCancelTeleport; }
            set { m_requestToCancelTeleport = value; }
        }

        public bool CallbackHasCome
        {
            get { return m_callbackHasCome; }
            set { m_callbackHasCome = value; }
        }

        public IHttpServer Server
        {
            get { return m_CapsService.Server; }
        }

        public IRegistryCore Registry
        {
            get { return m_CapsService.Registry; }
        }

        public String HostUri
        {
            get { return m_CapsService.HostUri; }
        }

        public void Initialise(ICapsService server, UUID agentID)
        {
            m_CapsService = server;
            m_agentID = agentID;
            m_account = Registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(null, agentID);
        }

        /// <summary>
        ///   Close out all of the CAPS for this user
        /// </summary>
        public void Close()
        {
            List<ulong> handles = new List<ulong>(m_RegionCapsServices.Keys);
            foreach (ulong regionHandle in handles)
            {
                RemoveCAPS(regionHandle);
            }
            m_RegionCapsServices.Clear();
        }

        /// <summary>
        ///   Attempt to find the CapsService for the given user/region
        /// </summary>
        /// <param name = "regionID"></param>
        /// <param name = "agentID"></param>
        /// <returns></returns>
        public IRegionClientCapsService GetCapsService(ulong regionID)
        {
            if (m_RegionCapsServices.ContainsKey(regionID))
                return m_RegionCapsServices[regionID];
            return null;
        }

        /// <summary>
        ///   Attempt to find the CapsService for the root user/region
        /// </summary>
        /// <param name = "regionID"></param>
        /// <param name = "agentID"></param>
        /// <returns></returns>
        public IRegionClientCapsService GetRootCapsService()
        {
#if (!ISWIN)
            foreach (IRegionClientCapsService clientCaps in m_RegionCapsServices.Values)
            {
                if (clientCaps.RootAgent) return clientCaps;
            }
            return null;
#else
            return m_RegionCapsServices.Values.FirstOrDefault(clientCaps => clientCaps.RootAgent);
#endif
        }

        public List<IRegionClientCapsService> GetCapsServices()
        {
            return new List<IRegionClientCapsService>(m_RegionCapsServices.Values);
        }

        /// <summary>
        ///   Find, or create if one does not exist, a Caps Service for the given region
        /// </summary>
        /// <param name = "regionID"></param>
        /// <returns></returns>
        public IRegionClientCapsService GetOrCreateCapsService(ulong regionID, string CAPSBase,
                                                               AgentCircuitData circuitData, uint port)
        {
            //If one already exists, don't add a new one
            if (m_RegionCapsServices.ContainsKey(regionID))
            {
                if (port == 0 || m_RegionCapsServices[regionID].Server.Port == port)
                {
                    m_RegionCapsServices[regionID].InformModulesOfRequest();
                    return m_RegionCapsServices[regionID];
                }
                else
                    RemoveCAPS(regionID);
            }
            //Create a new one, and then call Get to find it
            AddCapsServiceForRegion(regionID, CAPSBase, circuitData, port);
            return GetCapsService(regionID);
        }

        /// <summary>
        ///   Remove the CAPS for the given user in the given region
        /// </summary>
        /// <param name = "AgentID"></param>
        /// <param name = "regionHandle"></param>
        public void RemoveCAPS(ulong regionHandle)
        {
            if (!m_RegionCapsServices.ContainsKey(regionHandle))
                return;

            //Remove the agent from the region caps
            IRegionCapsService regionCaps = m_CapsService.GetCapsForRegion(regionHandle);
            if (regionCaps != null)
                regionCaps.RemoveClientFromRegion(m_RegionCapsServices[regionHandle]);

            //Remove all the CAPS handlers
            m_RegionCapsServices[regionHandle].Close();
            m_RegionCapsServices.Remove(regionHandle);
        }

        #endregion

        /// <summary>
        ///   Add a new Caps Service for the given region if one does not already exist
        /// </summary>
        /// <param name = "regionHandle"></param>
        protected void AddCapsServiceForRegion(ulong regionHandle, string CAPSBase, AgentCircuitData circuitData,
                                               uint port)
        {
            if (m_clientEndPoint == null && circuitData.ClientIPEndPoint != null)
                m_clientEndPoint = circuitData.ClientIPEndPoint;
            if (m_clientEndPoint == null)
            {
                ///Should only happen in grid HG/OpenSim situtations
                IPAddress test = null;
                if (IPAddress.TryParse(circuitData.IPAddress, out test))
                    m_clientEndPoint = new IPEndPoint(test, 0); //Dunno the port, so leave it alone
            }
            if (!m_RegionCapsServices.ContainsKey(regionHandle))
            {
                //Now add this client to the region caps
                //Create if needed
                m_CapsService.AddCapsForRegion(regionHandle);
                IRegionCapsService regionCaps = m_CapsService.GetCapsForRegion(regionHandle);

                PerRegionClientCapsService regionClient = new PerRegionClientCapsService();
                regionClient.Initialise(this, regionCaps, CAPSBase, circuitData, port);
                m_RegionCapsServices[regionHandle] = regionClient;

                //Now get and add them
                regionCaps.AddClientToRegion(regionClient);
            }
        }
    }
}