using System;
using System.Collections.Generic;
using System.Linq;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Connectors;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.RobustCompat
{
    public class RobustAvatarServicesConnector : IAvatarService, IService
    {
        public string Name
        {
            get { return GetType().Name; }
        }

        public IAvatarService InnerService
        {
            get { return this; }
        }

        private IRegistryCore m_registry;

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AvatarHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IAvatarService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        public AvatarAppearance GetAppearance(UUID userID)
        {
            AvatarData avatar = GetAvatar(userID);
            return avatar.ToAvatarAppearance(userID);
        }

        public bool SetAppearance(UUID userID, AvatarAppearance appearance)
        {
            AvatarData avatar = new AvatarData(appearance);
            return SetAvatar(userID, avatar);
        }

        public AvatarData GetAvatar(UUID userID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getavatar";

            sendData["UserID"] = userID;

            string reply = string.Empty;
            string reqString = WebUtils.BuildQueryString(sendData);
            List<string> urls = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AvatarServerURI");
            foreach (string uri in urls)
            {
                // MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: queryString = {0}", reqString);
                try
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST", uri, reqString);
                    if (reply == null || (reply != null && reply == string.Empty))
                    {
                        MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: GetAgent received null or empty reply");
                        return null;
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: Exception when contacting presence server at {0}: {1}", uri, e.Message);
                }
            }

            Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
            AvatarData avatar = null;

            if ((replyData != null) && replyData.ContainsKey("result") && (replyData["result"] != null))
            {
                if (replyData["result"] is Dictionary<string, object>)
                {
                    avatar = new AvatarData((Dictionary<string, object>)replyData["result"]);
                }
            }

            return avatar;

        }

        public bool ResetAvatar(UUID userID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "resetavatar";

            sendData["UserID"] = userID.ToString();

            string reqString = WebUtils.BuildQueryString(sendData);
            List<string> urls = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AvatarServerURI");
            foreach (string uri in urls)
            {
                // MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: queryString = {0}", reqString);
                try
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", uri, reqString);
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
                        else
                            MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: SetItems reply data does not contain result field");

                    }
                    else
                        MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: SetItems received empty reply");
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: Exception when contacting presence server at {0}: {1}", uri, e.Message);
                }
            }

            return false;
        }

        public bool SetItems(UUID userID, string[] names, string[] values)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "setitems";

            sendData["UserID"] = userID.ToString();
            sendData["Names"] = new List<string>(names);
            sendData["Values"] = new List<string>(values);

            string reqString = WebUtils.BuildQueryString(sendData);
            List<string> urls = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AvatarServerURI");
            foreach (string uri in urls)
            {
                // MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: queryString = {0}", reqString);
                try
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", uri, reqString);
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
                        else
                            MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: SetItems reply data does not contain result field");

                    }
                    else
                        MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: SetItems received empty reply");
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: Exception when contacting presence server at {0}: {1}", uri, e.Message);
                }
            }

            return false;
        }

        public bool RemoveItems(UUID userID, string[] names)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "removeitems";

            sendData["UserID"] = userID.ToString();
            sendData["Names"] = new List<string>(names);

            string reqString = WebUtils.BuildQueryString(sendData);
            List<string> urls = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AvatarServerURI");
            foreach (string uri in urls)
            {
                // MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: queryString = {0}", reqString);
                try
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", uri, reqString);
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
                        else
                            MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: RemoveItems reply data does not contain result field");

                    }
                    else
                        MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: RemoveItems received empty reply");
                }
                catch (Exception e)
                {
                    MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: Exception when contacting presence server at {0}: {1}", uri, e.Message);
                }
            }

            return false;
        }

        public bool SetAvatar(UUID userID, AvatarData avatar)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "setavatar";

            sendData["UserID"] = userID.ToString();

            Dictionary<string, object> structData = avatar.ToKVP();

#if (!ISWIN)
            foreach (KeyValuePair<string, object> kvp in structData)
            {
                if (kvp.Key != "Textures") sendData[kvp.Key] = kvp.Value.ToString();
            }
#else
            foreach (KeyValuePair<string, object> kvp in structData.Where(kvp => kvp.Key != "Textures"))
                sendData[kvp.Key] = kvp.Value.ToString();
#endif

            ResetAvatar(userID);
            string reqString = WebUtils.BuildQueryString(sendData);
            //MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: queryString = {0}", reqString);
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AvatarServerURI");
                foreach (string mServerUri in serverURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData.ContainsKey("result"))
                        {
                            if (replyData["result"].ToString().ToLower() == "success")
                                return true;
                        }
                        else
                            MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: SetAvatar reply data does not contain result field");
                    }
                    else
                        MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: SetAvatar received empty reply");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: Exception when contacting avatar server: {0}", e.Message);
            }

            return false;
        }

        public void CacheWearableData(UUID principalID, AvatarWearable cachedWearable)
        {
        }
    }
}