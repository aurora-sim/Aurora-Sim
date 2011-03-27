using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Services.Connectors;
using OpenSim.Services;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Nini.Config;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules 
{
    public class IWCAgentInfoConnector : IAgentInfoService, IService
    {
        protected AgentInfoService m_localService;
        protected AgentInfoConnector m_remoteService;
        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public IAgentInfoService InnerService
        {
            get { return m_localService; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AgentInfoHandler", "") != Name)
                return;

            m_localService = new AgentInfoService();
            m_localService.Initialize(config, registry);
            m_remoteService = new AgentInfoConnector();
            m_remoteService.Initialize(config, registry);
            registry.RegisterModuleInterface<IAgentInfoService>(this);
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

        #endregion

        #region IAgentInfoService Members

        public UserInfo GetUserInfo(string userID)
        {
            UserInfo info = m_localService.GetUserInfo(userID);
            if (info == null)
                info = m_remoteService.GetUserInfo(userID);
            return info;
        }

        public UserInfo[] GetUserInfos(string[] userIDs)
        {
            UserInfo[] info = m_localService.GetUserInfos(userIDs);
            if (info == null)
                info = m_remoteService.GetUserInfos(userIDs);
            return info;
        }

        public string[] GetAgentsLocations(string[] userIDs)
        {
            string[] info = m_localService.GetAgentsLocations(userIDs);
            if (info == null)
                info = m_remoteService.GetAgentsLocations(userIDs);
            return info;
        }

        public bool SetHomePosition(string userID, UUID homeID, Vector3 homePosition, Vector3 homeLookAt)
        {
            return m_localService.SetHomePosition(userID, homeID, homePosition, homeLookAt);
        }

        public void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            m_localService.SetLastPosition(userID, regionID, lastPosition, lastLookAt);
        }

        public void SetLoggedIn(string userID, bool loggingIn, bool fireLoggedInEvent)
        {
            m_localService.SetLoggedIn(userID, loggingIn, fireLoggedInEvent);
        }

        #endregion
    }
}
