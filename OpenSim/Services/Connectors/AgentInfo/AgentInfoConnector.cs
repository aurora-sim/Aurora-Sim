using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using Nini.Config;
using log4net;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Simulation.Base;

namespace OpenSim.Services.Connectors.AgentInfo
{
    public class AgentInfoConnector : IAgentInfoService, IService
    {
        #region Declares

        protected IRegistryCore m_registry;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AgentInfoHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IAgentInfoService>(this);
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        #endregion

        #region IAgentInfoService Members

        public UserInfo GetUserInfo(string userID)
        {
            List<string> urls = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(userID, "AgentInfoServerURI");
            foreach (string url in urls)
            {
                OSDMap request = new OSDMap();
                request["userID"] = userID;
                request["Method"] = "GetUserInfo";
                OSDMap result = WebUtils.PostToService(url, request);
                UserInfo info = new UserInfo();
                info.FromOSD((OSDMap)result["Result"]);
                return info;
            }
            return null;
        }

        public UserInfo[] GetUserInfos(string[] userIDs)
        {
            List<string> urls = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AgentInfoServerURI");
            foreach (string url in urls)
            {
                OSDMap request = new OSDMap();
                OSDArray requestArray = new OSDArray();
                for (int i = 0; i < userIDs.Length; i++)
                {
                    requestArray.Add(userIDs[i]);
                }
                request["userIDs"] = requestArray;
                request["Method"] = "GetUserInfos";
                OSDMap result = WebUtils.PostToService(url, request);
                OSDArray resultArray = (OSDArray)result["Result"];
                List<UserInfo> retVal = new List<UserInfo>();
                foreach (OSD o in resultArray)
                {
                    UserInfo info = new UserInfo();
                    info.FromOSD((OSDMap)o);
                    retVal.Add(info);
                }
                return retVal.ToArray();
            }
            return null;
        }

        public string[] GetAgentsLocations(string[] userIDs)
        {
            List<string> urls = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AgentInfoServerURI");
            foreach (string url in urls)
            {
                OSDMap request = new OSDMap();
                OSDArray requestArray = new OSDArray();
                for (int i = 0; i < userIDs.Length; i++)
                {
                    requestArray.Add(userIDs[i]);
                }
                request["userIDs"] = requestArray;
                request["Method"] = "GetAgentsLocations";
                OSDMap result = WebUtils.PostToService(url, request);
                OSDArray resultArray = (OSDArray)result["Result"];
                List<string> retVal = new List<string>();
                foreach (OSD o in resultArray)
                {
                    retVal.Add(o.AsString());
                }
                return retVal.ToArray();
            }
            return null;
        }

        public bool SetHomePosition(string userID, UUID homeID, Vector3 homePosition, Vector3 homeLookAt)
        {
            return false;
        }

        public void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
        }

        public void SetLoggedIn(string userID, bool loggingIn)
        {
        }

        #endregion
    }
}
