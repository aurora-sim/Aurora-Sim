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
using System.Linq;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services;
using OpenSim.Services.Connectors;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules
{
    public class IWCAgentInfoConnector : ConnectorBase, IAgentInfoService, IService
    {
        protected IAgentInfoService m_localService;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IAgentInfoService Members

        public IAgentInfoService InnerService
        {
            get
            {
                //If we are getting URls for an IWC connection, we don't want to be calling other things, as they are calling us about only our info
                //If we arn't, its ar region we are serving, so give it everything we know
                if (m_registry.RequestModuleInterface<InterWorldCommunications>().IsGettingUrlsForIWCConnection)
                    return m_localService;
                else
                    return this;
            }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AgentInfoHandler", "") != Name)
                return;

            string localAssetHandler = handlerConfig.GetString("LocalAgentInfoHandler", "AgentInfoService");
            List<IAgentInfoService> services = AuroraModuleLoader.PickupModules<IAgentInfoService>();
#if (!ISWIN)
            foreach (IAgentInfoService s in services)
            {
                if (s.GetType().Name == localAssetHandler) m_localService = s;
            }
#else
                foreach (IAgentInfoService s in services.Where(s => s.GetType().Name == localAssetHandler))
                m_localService = s;
#endif
            if (m_localService == null)
                m_localService = new AgentInfoService();
            m_localService.Initialize(config, registry);
            registry.RegisterModuleInterface<IAgentInfoService>(this);
            m_registry = registry;
            Init(registry, Name);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (m_localService != null)
                m_localService.Start(config, registry);
        }

        public void FinishedStartup()
        {
            if (m_localService != null)
                m_localService.FinishedStartup();
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public UserInfo GetUserInfo(string userID)
        {
            UserInfo info = m_localService.GetUserInfo(userID);
            if (info == null)
                info = (UserInfo)DoRemoteForced(userID);
            return info;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<UserInfo> GetUserInfos(List<string> userIDs)
        {
            List<UserInfo> info = m_localService.GetUserInfos(userIDs);
            if (info == null)
                info = (List<UserInfo>)DoRemoteForced(userIDs);
            return info;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public List<string> GetAgentsLocations(string requestor, List<string> userIDs)
        {
            List<string> info = m_localService.GetAgentsLocations(requestor, userIDs);
            List<string> info2 = (List<string>)DoRemoteForced(userIDs);
            if (info == null || info.Count == 0)
                info = info2;
            else
            {
                for (int i = 0; i < userIDs.Count; i++)
                {
                    if (info[i] == "NotOnline" && info2.Count < i)
                        info[i] = info2[i];
                }
            }
            return info;
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public bool SetHomePosition(string userID, UUID homeID, Vector3 homePosition, Vector3 homeLookAt)
        {
            return m_localService.SetHomePosition(userID, homeID, homePosition, homeLookAt);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            m_localService.SetLastPosition(userID, regionID, lastPosition, lastLookAt);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public void SetLoggedIn(string userID, bool loggingIn, bool fireLoggedInEvent, UUID enteringRegion)
        {
            m_localService.SetLoggedIn(userID, loggingIn, fireLoggedInEvent, enteringRegion);
        }

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public void LockLoggedInStatus(string userID, bool locked)
        {
            m_localService.LockLoggedInStatus(userID, locked);
        }

        #endregion
    }
}