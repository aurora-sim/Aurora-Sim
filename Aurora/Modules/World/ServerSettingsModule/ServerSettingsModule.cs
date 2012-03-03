using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Capabilities;

namespace Aurora.Modules
{
    public class ServerSettingsModule : INonSharedRegionModule, IServerSettings
    {
        private List<ServerSetting> m_settings = new List<ServerSetting>();
        public void Initialise (IConfigSource source)
        {
        }

        public void AddRegion (IScene scene)
        {
            scene.RegisterModuleInterface<IServerSettings>(this);
            scene.EventManager.OnRegisterCaps += EventManager_OnRegisterCaps;
        }

        OSDMap EventManager_OnRegisterCaps (UUID agentID, IHttpServer httpServer)
        {
            OSDMap map = new OSDMap();

            map["ServerFeatures"] = CapsUtil.CreateCAPS("ServerFeatures", "");
            httpServer.AddStreamHandler(new RestHTTPHandler("POST", map["ServerFeatures"],
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return SetServerFeature(m_dhttpMethod, agentID);
                                                      }));
            httpServer.AddStreamHandler(new RestHTTPHandler("GET", map["ServerFeatures"],
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return GetServerFeature(m_dhttpMethod, agentID);
                                                      }));

            return map;
        }

        private Hashtable SetServerFeature (Hashtable m_dhttpMethod, UUID agentID)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";

            return responsedata;
        }

        private Hashtable GetServerFeature (Hashtable m_dhttpMethod, UUID agentID)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = BuildSettingsXML();

            return responsedata;
        }

        public void RegionLoaded (IScene scene)
        {
        }

        public void RemoveRegion (IScene scene)
        {
        }

        public void Close ()
        {
        }

        public string Name
        {
            get { return "ServerSettingsModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string BuildSettingsXML ()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("<?xml version=\"1.0\" ?>");
            builder.AppendLine("<llsd>");
            builder.AppendLine("<map>");

            foreach(ServerSetting setting in m_settings)
            {
                builder.Append("<key>");
                builder.Append(setting.Name);
                builder.AppendLine("</key>");
                builder.AppendLine("<map>");
                builder.AppendLine("<key>Comment</key>");
			    builder.Append("<string>");
                builder.Append(setting.Comment);
                builder.AppendLine("</string>");
			    builder.AppendLine("<key>Type</key>");
			    builder.Append("<string>");
                builder.Append(setting.Type);
                builder.AppendLine("</string>");
			    builder.AppendLine("<key>Value</key>");
                builder.Append(setting.GetValue());
                builder.AppendLine("</map>");
            }

            builder.AppendLine("</map>");
            builder.AppendLine("</llsd>");

            return builder.ToString();
        }

        public void RegisterSetting (ServerSetting setting)
        {
            m_settings.Add(setting);
        }

        public void UnregisterSetting (ServerSetting setting)
        {
            m_settings.RemoveAll(s => s.Name == setting.Name);
        }
    }
}
