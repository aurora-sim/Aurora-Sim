using System;
using System.Collections;
using System.IO;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Capabilities;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.Modules.WindlightSettings
{
    public class EnvironmentSettingsModule : INonSharedRegionModule
    {
        private OSD m_info;
        private IScene m_scene;

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(IScene scene)
        {
            m_scene = scene;
            scene.EventManager.OnRegisterCaps += EventManager_OnRegisterCaps;
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
            get { return "EnvironmentSettingsModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        private OSDMap EventManager_OnRegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["EnvironmentSettings"] = CapsUtil.CreateCAPS("EnvironmentSettings", "");
            //Sets the windlight settings
            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["EnvironmentSettings"],
                                                      delegate(string path, Stream request,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                      {
                                                          return SetEnvironment(request, agentID);
                                                      }));
            //Sets the windlight settings
            server.AddStreamHandler(new GenericStreamHandler("GET", retVal["EnvironmentSettings"],
                                                      delegate(string path, Stream request,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                      {
                                                          return EnvironmentSettings(request, agentID);
                                                      }));
            return retVal;
        }

        private byte[] SetEnvironment(Stream request, UUID agentID)
        {
            IScenePresence SP = m_scene.GetScenePresence(agentID);
            if (SP == null)
                return new byte[0]; //They don't exist
            if (SP.Scene.Permissions.CanIssueEstateCommand(agentID, false))
            {
                m_info = OSDParser.DeserializeLLSDXml(request);
                IGenericsConnector gc = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
                if (gc != null)
                    gc.AddGeneric(m_scene.RegionInfo.RegionID, "EnvironmentSettings", "",
                                  (new DatabaseWrapper {Info = m_info}).ToOSD());
                SP.ControllingClient.SendAlertMessage("Windlight Settings saved successfully");
            }
            else
                SP.ControllingClient.SendAlertMessage(
                    "You don't have the correct permissions to set the Windlight Settings");
            return new byte[0];
        }

        private byte[] EnvironmentSettings(Stream request, UUID agentID)
        {
            IScenePresence SP = m_scene.GetScenePresence(agentID);
            if (SP == null)
                return new byte[0]; //They don't exist

            if (m_info == null)
            {
                IGenericsConnector gc = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
                if (gc != null)
                {
                    DatabaseWrapper d = gc.GetGeneric<DatabaseWrapper>(m_scene.RegionInfo.RegionID, "EnvironmentSettings", "");
                    if (d != null)
                        m_info = d.Info;
                }
            }
            if (m_info != null)
                return OSDParser.SerializeLLSDXmlBytes(m_info);
            return new byte[0];
        }

        #region Nested type: DatabaseWrapper

        private class DatabaseWrapper : IDataTransferable
        {
            public OSD Info;

            public override void FromOSD(OSDMap map)
            {
                if(map != null)
                    Info = map["Info"];
            }

            public override OSDMap ToOSD()
            {
                OSDMap map = new OSDMap();
                map["Info"] = Info;
                return map;
            }
        }

        #endregion
    }
}