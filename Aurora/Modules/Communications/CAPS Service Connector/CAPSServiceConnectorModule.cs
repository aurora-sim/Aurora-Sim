using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using OpenSim.Framework.Servers.HttpServer;
using Nini.Config;
using Aurora.Framework;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Server.Base;
using log4net;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Aurora.DataManager;
using Mono.Addins;

namespace Aurora.Modules
{
    //[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class CAPSServiceConnectorModule : INonSharedRegionModule
    {
        string CAPSServiceURL = "";
        Scene m_scene = null;

        public void PostInitialise()
        {
        }

        public string Name
        {
            get { return "CAPSModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig m_CAPSServerConfig = source.Configs["CAPSService"];
            if(m_CAPSServerConfig != null)
                CAPSServiceURL = m_CAPSServerConfig.GetString("CAPSServiceURL", String.Empty);
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;
            if(CAPSServiceURL != "")
                scene.EventManager.OnRegisterCaps += RegisterCaps;
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RegisterCaps(UUID agentID, OpenSim.Framework.Capabilities.Caps caps)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["PASS"] = CAPSPass;
            sendData["SIMCAPS"] = "http://" + m_scene.RegionInfo.ExternalEndPoint.ToString();

            sendData["CAPSSEEDPATH"] = "UpdateAgentInformation";
            sendData["AGENTID"] = agentID;
            PostCAPS(caps, sendData);

            sendData = new Dictionary<string, object>();
            sendData["CAPSSEEDPATH"] = "UpdateAgentLanguage";
            sendData["AGENTID"] = agentID;
            PostCAPS(caps, sendData);

            sendData = new Dictionary<string, object>();
            sendData["CAPSSEEDPATH"] = "WebFetchInventoryDescendents";
            sendData["AGENTID"] = agentID;
            PostCAPS(caps, sendData);

            sendData = new Dictionary<string, object>();
            sendData["CAPSSEEDPATH"] = "FetchInventoryDescendents";
            sendData["AGENTID"] = agentID;
            PostCAPS(caps, sendData);
        }

        private void PostCAPS(OpenSim.Framework.Capabilities.Caps caps, Dictionary<string, object> sendData)
        {
            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        CAPSServiceURL + "/CAPS/REGISTER",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        object value;
                        if (!replyData.TryGetValue("URL", out value))
                            return;

                        string URL = value.ToString();
                        
                        // Success
                        // Add the CAP to the client
                        caps.RegisterHandler((string)sendData["Type"],
                                            new RestHTTPHandler("POST", "http://auroraserver.ath.cx:8007" + URL,
                                                                  delegate(Hashtable m_dhttpMethod)
                                                                  {
                                                                      return null;
                                                                  }));
                    }

                }
            }
            catch (Exception e)
            {
            }
        }
    }
}
