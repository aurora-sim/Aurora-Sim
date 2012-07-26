using System;
using System.Collections.Generic;
using System.Linq;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.Robust
{
    public class RobustPresence : IAgentInfoService, IService
    {
        protected IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IAgentInfoService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AgentInfoHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IAgentInfoService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        public UserInfo GetUserInfo(string userID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getgriduserinfo";

            sendData["UserID"] = userID;

            return Get(sendData);
        }

        public List<UserInfo> GetUserInfos(List<string> userIDs)
        {
            List<UserInfo> us =new List<UserInfo>();
            for (int i = 0; i < userIDs.Count; i++)
                us.Add(GetUserInfo(userIDs[i]));
            return us;
        }

        public bool SetHomePosition(string userID, UUID homeID, Vector3 homePosition, Vector3 homeLookAt)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "sethome";

            return Set(sendData, userID, homeID, homePosition, homeLookAt);
        }

        public void SetLastPosition(string userID, UUID regionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "setposition";

            if (regionID != UUID.Zero)
                Set(sendData, userID, regionID, lastPosition, lastLookAt);
        }

        public void SetLoggedIn(string userID, bool loggingIn, bool fireLoggedInEvent, UUID enteringRegion)
        {
            if (!loggingIn)
            {
                Dictionary<string, object> sendData = new Dictionary<string, object>();
                //sendData["SCOPEID"] = scopeID.ToString();
                sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
                sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
                sendData["METHOD"] = "loggedout";
                UserInfo u;
                if ((u = GetUserInfo(userID)) != null)
                    Set(sendData, userID, u.CurrentRegionID, Vector3.Zero, Vector3.Zero);
            }
            else
            {
                Dictionary<string, object> sendData = new Dictionary<string, object>();
                //sendData["SCOPEID"] = scopeID.ToString();
                sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
                sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
                sendData["METHOD"] = "loggedin";

                sendData["UserID"] = userID;

                Get(sendData);
            }
        }

        public void LockLoggedInStatus(string userID, bool locked)
        {
        }

        public IAgentInfoService InnerService
        {
            get { return null; }
        }

        public List<string> GetAgentsLocations(string requestor, List<string> userIDs)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getagents";

            sendData["uuids"] = userIDs;

            List<string> rinfos = new List<string>();
            List<string> urls =
                m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("PresenceServerURI");
            foreach (string url in urls)
            {
                string reply = string.Empty;
                string reqString = WebUtils.BuildQueryString(sendData);
                //MainConsole.Instance.DebugFormat("[PRESENCE CONNECTOR]: queryString = {0}", reqString);
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      url,
                                                                      reqString);
                    if (reply == null || (reply != null && reply == string.Empty))
                        return null;
                }
                catch (Exception)
                {
                }

                Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                if (replyData != null)
                {
                    if (replyData.ContainsKey("result") &&
                        (replyData["result"].ToString() == "null" || replyData["result"].ToString() == "Failure"))
                    {
                        return new List<string>();
                    }

                    Dictionary<string, object>.ValueCollection pinfosList = replyData.Values;
#if (!ISWIN)
                    foreach (object presence in pinfosList)
                    {
                        if (presence is Dictionary<string, object>)
                        {
                            string regionUUID = ((Dictionary<string, object>)presence)["RegionID"].ToString();
                            rinfos.Add(GetRegionService(UUID.Parse(regionUUID)));
                        }
                    }
#else
                    rinfos.AddRange(pinfosList.OfType<Dictionary<string, object>>().Select(presence => (presence)["RegionID"].ToString()).Select(regionUUID => GetRegionService(UUID.Parse(regionUUID))));
#endif
                }
            }

            return rinfos;
        }

        #endregion

        private string GetRegionService(UUID regionID)
        {
            IGridService gs = m_registry.RequestModuleInterface<IGridService>();
            if (gs != null && regionID != UUID.Zero)
            {
                GridRegion region = gs.GetRegionByUUID(null, regionID);
                if (region != null)
                    return region.ServerURI;
            }
            return "NonExistant";
        }

        protected bool Set(Dictionary<string, object> sendData, string userID, UUID regionID, Vector3 position, Vector3 lookAt)
        {
            sendData["UserID"] = userID;
            sendData["RegionID"] = regionID.ToString();
            sendData["Position"] = position.ToString();
            sendData["LookAt"] = lookAt.ToString();

            string reqString = WebUtils.BuildQueryString(sendData);
            // MainConsole.Instance.DebugFormat("[GRID USER CONNECTOR]: queryString = {0}", reqString);
            try
            {
                List<string> urls = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridUserServerURI");
                foreach (string url in urls)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                               url,
                               reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData.ContainsKey("result"))
                        {
                            if (replyData["result"].ToString().ToLower() == "success")
                                return true;
                            else
                                return false;
                        }
                    }
                }
            }
            catch (Exception)
            { }

            return false;
        }

        protected UserInfo Get(Dictionary<string, object> sendData)
        {
            string reqString = WebUtils.BuildQueryString(sendData);
            // MainConsole.Instance.DebugFormat("[GRID USER CONNECTOR]: queryString = {0}", reqString);
            try
            {
                List<string> urls =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("GridUserServerURI");
                foreach (string url in urls)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                             url,
                                                                             reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
                        UserInfo guinfo = null;

                        if ((replyData != null) && replyData.ContainsKey("result") && (replyData["result"] != null))
                        {
                            if (replyData["result"] is Dictionary<string, object>)
                            {
                                guinfo = new UserInfo();
                                Dictionary<string, object> kvp = (Dictionary<string, object>) replyData["result"];
                                guinfo.UserID = kvp["UserID"].ToString();
                                guinfo.HomeRegionID = UUID.Parse(kvp["HomeRegionID"].ToString());
                                guinfo.CurrentRegionID = UUID.Parse(kvp["LastRegionID"].ToString());
                                guinfo.CurrentPosition = Vector3.Parse(kvp["LastPosition"].ToString());
                                guinfo.HomePosition = Vector3.Parse(kvp["HomePosition"].ToString());
                                guinfo.IsOnline = bool.Parse(kvp["Online"].ToString());
                                guinfo.LastLogin = DateTime.Parse(kvp["Login"].ToString());
                                guinfo.LastLogout = DateTime.Parse(kvp["Logout"].ToString());
                            }
                        }

                        return guinfo;
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}