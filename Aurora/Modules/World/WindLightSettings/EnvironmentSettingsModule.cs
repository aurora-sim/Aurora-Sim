using System;
using System.Collections;
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
#if (!ISWIN)
            //Sets the windlight settings
            server.AddStreamHandler(new RestHTTPHandler("POST", retVal["EnvironmentSettings"],
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return SetEnvironment(m_dhttpMethod, agentID);
                                                      }));
            //Sets the windlight settings
            server.AddStreamHandler(new RestHTTPHandler("GET", retVal["EnvironmentSettings"],
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return EnvironmentSettings(m_dhttpMethod, agentID);
                                                      }));
#else
            //Sets the windlight settings
            server.AddStreamHandler(new RestHTTPHandler("POST", retVal["EnvironmentSettings"],
                                                        m_dhttpMethod => SetEnvironment(m_dhttpMethod, agentID)));
            //Sets the windlight settings
            server.AddStreamHandler(new RestHTTPHandler("GET", retVal["EnvironmentSettings"],
                                                        m_dhttpMethod => EnvironmentSettings(m_dhttpMethod, agentID)));
#endif
            return retVal;
        }

        private Hashtable SetEnvironment(Hashtable m_dhttpMethod, UUID agentID)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";

            IScenePresence SP = m_scene.GetScenePresence(agentID);
            if (SP == null)
                return responsedata; //They don't exist
            if (SP.Scene.Permissions.CanIssueEstateCommand(agentID, false))
            {
                m_info = OSDParser.DeserializeLLSDXml((string) m_dhttpMethod["requestbody"]);
                IGenericsConnector gc = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
                if (gc != null)
                    gc.AddGeneric(m_scene.RegionInfo.RegionID, "EnvironmentSettings", "",
                                  (new DatabaseWrapper {Info = m_info}).ToOSD());
                SP.ControllingClient.SendAlertMessage("Windlight Settings saved successfully");
            }
            else
                SP.ControllingClient.SendAlertMessage(
                    "You don't have the correct permissions to set the Windlight Settings");
            return responsedata;
        }

        private Hashtable EnvironmentSettings(Hashtable m_dhttpMethod, UUID agentID)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";

            IScenePresence SP = m_scene.GetScenePresence(agentID);
            if (SP == null)
                return responsedata; //They don't exist

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
                responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(m_info);
            return responsedata;
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