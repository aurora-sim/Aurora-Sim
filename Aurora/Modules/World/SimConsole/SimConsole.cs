using System;
using System.Collections;
using System.Collections.Generic;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Modules.World.SimConsole
{
    public class SimConsole : ISharedRegionModule
    {
        private string Password = "";
        private Scene m_scene;
        private bool m_enabled = false;
        private List<UUID> m_authorizedParticipants = new List<UUID>();
        private Dictionary<string, string> m_userKeys = new Dictionary<string, string>();

        public void Initialise(Nini.Config.IConfigSource source)
        {
            if(!m_userKeys.ContainsKey("ConsoleTesting"))
                m_userKeys.Add("ConsoleTesting", "TestPassword");
            m_enabled = true;
        }

        public void PostInitialise()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            MainConsole.OnIncomingLogWrite += new MainConsole.IncomingLogWrite(MainConsole_OnIncomingLogWrite);
            scene.EventManager.OnRegisterCaps += OnRegisterCaps;
            m_scene = scene;
        }

        public void MainConsole_OnIncomingLogWrite(string text)
        {
        }

        public void OnRegisterCaps(UUID agentID, OpenSim.Framework.Capabilities.Caps caps)
        {
            UUID capuuid = UUID.Random();

            caps.RegisterHandler("SimConsole",
                                new RestHTTPHandler("POST", "/CAPS/" + capuuid + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return SimConsoleResponder(m_dhttpMethod, capuuid, agentID);
                                                      }));
        }

        private Hashtable SimConsoleResponder(Hashtable m_dhttpMethod, UUID capuuid, UUID agentID)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";

            ScenePresence SP = m_scene.GetScenePresence(agentID);
            if (SP == null)
                return responsedata; //They don't exist

            OSD rm = OSDParser.DeserializeLLSDXml((string)m_dhttpMethod["requestbody"]);

            string message = rm.AsString();
            string response = "Finished.";

            if (SP.Scene.Permissions.CanRunConsoleCommand(SP.UUID) ||
                AuthenticateUser(SP.UUID, message))
            {
                MainConsole.Instance.RunCommand(message);
                responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(OSD.FromString(response));
            }
            else
            {
                response = "You have failed to log into the console.";
                responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(OSD.FromString(response));
            }
            return responsedata;
        }

        private bool AuthenticateUser(UUID AgentID, string message)
        {
            if (m_authorizedParticipants.Contains(AgentID))
            {
                return true;
            }
            else
            {
                if (message.Contains("User:"))
                {
                    //User:<NAME>/Password:<PASS>
                    string[] splits = message.Split('/');
                    string username, password;
                    if (splits.Length != 2)
                        return false;
                    username = splits[0].Remove(0,5);
                    password = splits[1].Remove(0, 9);
                    if (m_userKeys.ContainsKey(username))
                    {
                        if (m_userKeys[username] == password)
                        {
                            m_authorizedParticipants.Add(AgentID);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "SimConsole"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }
    }
}
