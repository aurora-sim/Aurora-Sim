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
    /*public class RobustAvatarServicesConnector : AvatarServicesConnector
    {
        public override string Name
        {
            get { return GetType().Name; }
        }

        public override void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AvatarHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IAvatarService>(this);
        }

        public override bool SetAvatar(UUID userID, AvatarData avatar)
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
    }*/
}