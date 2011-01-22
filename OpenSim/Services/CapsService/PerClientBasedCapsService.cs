using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Capabilities;

using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.CapsService
{
    public class PerClientBasedCapsService : IClientCapsService
    {
        protected Dictionary<ulong, IRegionClientCapsService> m_RegionCapsServices = new Dictionary<ulong, IRegionClientCapsService>();
        protected ICapsService m_CapsService;
        protected UUID m_agentID;
        protected bool m_inTeleport = false;
        protected bool m_requestToCancelTeleport = false;
        protected bool m_callbackHasCome = false;

        public UUID AgentID
        {
            get { return m_agentID; }
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
        }

        /// <summary>
        /// Close out all of the CAPS for this user
        /// </summary>
        public void Close()
        {
            foreach (ulong regionHandle in m_RegionCapsServices.Keys)
            {
                if (m_RegionCapsServices[regionHandle] == null)
                    continue;
                m_RegionCapsServices[regionHandle].Close();
            }
            m_RegionCapsServices.Clear();
        }

        /// <summary>
        /// Add a new Caps Service for the given region if one does not already exist
        /// </summary>
        /// <param name="regionHandle"></param>
        protected void AddCapsServiceForRegion(ulong regionHandle, string CAPSBase, string UrlToInform)
        {
            if (!m_RegionCapsServices.ContainsKey(regionHandle))
            {
                PerRegionClientCapsService regionClient = new PerRegionClientCapsService();
                regionClient.Initialise(this, regionHandle, CAPSBase, UrlToInform);
                m_RegionCapsServices.Add(regionHandle, regionClient);
            }
        }

        /// <summary>
        /// Attempt to find the CapsService for the given user/region
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="agentID"></param>
        /// <returns></returns>
        public IRegionClientCapsService GetCapsService(ulong regionID)
        {
            if (m_RegionCapsServices.ContainsKey(regionID))
                return m_RegionCapsServices[regionID];
            return null;
        }

        /// <summary>
        /// Find, or create if one does not exist, a Caps Service for the given region
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        public IRegionClientCapsService GetOrCreateCapsService(ulong regionID, string CAPSBase, string UrlToInform)
        {
            //If one already exists, don't add a new one
            if (m_RegionCapsServices.ContainsKey(regionID))
            {
                m_RegionCapsServices[regionID].InformModulesOfRequest();
                return m_RegionCapsServices[regionID];
            }
            //Create a new one, and then call Get to find it
            AddCapsServiceForRegion(regionID, CAPSBase, UrlToInform);
            return GetCapsService(regionID);
        }

        /// <summary>
        /// Remove the CAPS for the given user in the given region
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="regionHandle"></param>
        public void RemoveCAPS(ulong regionHandle)
        {
            if (!m_RegionCapsServices.ContainsKey(regionHandle))
                return;
            //Remove all the CAPS handlers
            m_RegionCapsServices[regionHandle].Close();
            m_RegionCapsServices.Remove(regionHandle);
        }
    }
}
