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
using Nini.Config;
using Aurora.Framework;

namespace Aurora.Modules.World.SimConsole
{
    /// <summary>
    /// This module allows for the console to be accessed in V2 viewers that support SimConsole
    /// This will eventually be extended in Imprudence so that full console support can be added into the viewer (this module already supports the eventual extension)
    /// </summary>
    public class SimConsole : ISharedRegionModule
    {
        #region Declares

        private Scene m_scene;
        private bool m_enabled = false;
        private Dictionary<UUID, Access> m_authorizedParticipants = new Dictionary<UUID, Access>();
        private DoubleValueDictionary<string, string, Access> m_userKeys = new DoubleValueDictionary<string, string, Access>();

        #region Enums

        private enum Access
        {
            ReadWrite,
            Read,
            Write,
            None
        }

        #endregion

        #endregion

        #region ISharedRegionModule

        public void Initialise(IConfigSource source)
        {
            IConfig config = source.Configs["SimConsole"];
            if(config != null)
            {
                m_enabled = config.GetBoolean("Enabled", false);
                if(!m_enabled)
                    return;
                string User = config.GetString("Users", "");
                string[] Users = User.Split('|');
                for (int i = 0; i < Users.Length; i+=3)
                {
                    if (!m_userKeys.ContainsKey(Users[i]))
                    {
                        m_userKeys.Add(Users[i], Users[i+1], (Access)Enum.Parse(typeof(Access), Users[i + 2]));
                    }
                }
            }
        }

        public void PostInitialise()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            MainConsole.OnIncomingLogWrite += IncomingLogWrite;
            scene.EventManager.OnRegisterCaps += OnRegisterCaps;
            m_scene = scene;
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

        #endregion

        #region CAPS

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

            //Is a god, or they authenticated to the server and have write access
            if ((SP.Scene.Permissions.CanRunConsoleCommand(SP.UUID) ||
                AuthenticateUser(SP.UUID, message)) && CanWrite(SP.UUID))
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

        #endregion

        #region Authentication

        private bool CanWrite(UUID AgentID)
        {
            if (m_authorizedParticipants.ContainsKey(AgentID))
            {
                return m_authorizedParticipants[AgentID] == Access.Write
                    || m_authorizedParticipants[AgentID] == Access.ReadWrite;
            }
            return false;
        }

        private bool CanRead(UUID AgentID)
        {
            if (m_authorizedParticipants.ContainsKey(AgentID))
            {
                return m_authorizedParticipants[AgentID] == Access.Read
                    || m_authorizedParticipants[AgentID] == Access.ReadWrite;
            }
            return false;
        }

        private bool AuthenticateUser(UUID AgentID, string message)
        {
            if (m_authorizedParticipants.ContainsKey(AgentID))
            {
                return true;
            }
            else
            {
                if (message.Contains("User:"))
                {
                    //The expected auth line looks like : "User:<NAME>/Password:<PASS>"
                    string[] splits = message.Split('/');
                    string username, password;
                    if (splits.Length != 2)
                        return false;
                    username = splits[0].Remove(0, 5);
                    password = splits[1].Remove(0, 9);
                    if (m_userKeys.ContainsKey(username))
                    {
                        if (m_userKeys[username, ""] == password)
                        {
                            m_authorizedParticipants.Add(AgentID, m_userKeys[username]);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        #endregion

        public void IncomingLogWrite(string text)
        {
            foreach (KeyValuePair<UUID, Access> kvp in m_authorizedParticipants)
            {
                if (kvp.Value == Access.ReadWrite || kvp.Value == Access.Read)
                {
                    //Send the EQM with the message to all people who have read access
                    SendConsoleEventEQM(kvp.Key, text);
                }
            }
        }

        private void SendConsoleEventEQM(UUID AgentID, string text)
        {
            OSDString t = (OSDString)OSD.FromString(text);
            IEventQueue eq = m_scene.RequestModuleInterface<IEventQueue>();
            if (eq != null)
                eq.Enqueue(t, AgentID);
        }
    }
}
