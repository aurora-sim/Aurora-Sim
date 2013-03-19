using Aurora.Framework;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Servers.HttpServer.Interfaces;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Aurora.Modules
{
    public class ServerSettingsModule : INonSharedRegionModule, IServerSettings
    {
        private List<ServerSetting> m_settings = new List<ServerSetting>();

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(IScene scene)
        {
            scene.RegisterModuleInterface<IServerSettings>(this);
            scene.EventManager.OnRegisterCaps += EventManager_OnRegisterCaps;
        }

        private OSDMap EventManager_OnRegisterCaps(UUID agentID, IHttpServer httpServer)
        {
            OSDMap map = new OSDMap();

            map["ServerFeatures"] = CapsUtil.CreateCAPS("ServerFeatures", "");
            httpServer.AddStreamHandler(new GenericStreamHandler("POST", map["ServerFeatures"],
                                                                 delegate(string path, Stream request,
                                                                          OSHttpRequest httpRequest,
                                                                          OSHttpResponse httpResponse)
                                                                     { return SetServerFeature(request, agentID); }));
            httpServer.AddStreamHandler(new GenericStreamHandler("GET", map["ServerFeatures"],
                                                                 delegate(string path, Stream request,
                                                                          OSHttpRequest httpRequest,
                                                                          OSHttpResponse httpResponse)
                                                                     { return GetServerFeature(request, agentID); }));

            return map;
        }

        private byte[] SetServerFeature(Stream request, UUID agentID)
        {
            return new byte[0];
        }

        private byte[] GetServerFeature(Stream request, UUID agentID)
        {
            return Encoding.UTF8.GetBytes(BuildSettingsXML());
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void Close()
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

        public string BuildSettingsXML()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("<?xml version=\"1.0\" ?>");
            builder.AppendLine("<llsd>");
            builder.AppendLine("<map>");

            foreach (ServerSetting setting in m_settings)
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

        public void RegisterSetting(ServerSetting setting)
        {
            m_settings.Add(setting);
        }

        public void UnregisterSetting(ServerSetting setting)
        {
            m_settings.RemoveAll(s => s.Name == setting.Name);
        }
    }
}