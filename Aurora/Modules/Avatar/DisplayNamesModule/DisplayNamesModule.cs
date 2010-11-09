using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Caps = OpenSim.Framework.Capabilities.Caps;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Modules
{
    /// <summary>
    /// Module to deal with setting of alternative names on clients
    /// </summary>
    public class DisplayNamesModule : ISharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;
        private List<Scene> m_scenes = new List<Scene>();
        private bool m_enabled = false;
        private IProfileConnector m_profileConnector = null;
        private List<string> bannedNames = new List<string>();
        private IEventQueue m_eventQueue = null;

        public void Initialise(IConfigSource source)
        {
            IConfig displayNamesConfig = source.Configs["DisplayNamesModule"];
            if (displayNamesConfig != null)
            {
                m_enabled = displayNamesConfig.GetBoolean("Enabled", true);
                string bannedNamesString = displayNamesConfig.GetString("BannedUserNames", "");
                if(bannedNamesString != "")
                    bannedNames = new List<string>(bannedNamesString.Split(','));
            }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            m_scene = scene;
            m_scenes.Add(scene);
            m_scene.EventManager.OnRegisterCaps += RegisterCaps;
            m_scene.EventManager.OnNewClient += OnNewClient;
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
            m_eventQueue = scene.RequestModuleInterface<IEventQueue>();
            m_profileConnector = Aurora.DataManager.DataManager.RequestPlugin<IProfileConnector>();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise()
        {
        }

        /// <summary>
        /// Set up the CAPS for display names
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="caps"></param>
        public void RegisterCaps(UUID agentID, Caps caps)
        {
            UUID capuuid = UUID.Random();

            caps.RegisterHandler("SetDisplayName",
                                new RestHTTPHandler("POST", "/CAPS" + capuuid + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return ProcessSetDisplayName(m_dhttpMethod, agentID);
                                                      }));

            capuuid = UUID.Random();

            caps.RegisterHandler("GetDisplayNames",
                                new RestHTTPHandler("POST", "/CAPS" + capuuid + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return ProcessGetDisplayName(m_dhttpMethod, agentID);
                                                      }));
        }

        /// <summary>
        /// Tell the client about all other display names in the region as well as send ours to all others
        /// </summary>
        /// <param name="client"></param>
        public void OnNewClient(IClientAPI client)
        {
            ScenePresence us = ((Scene)client.Scene).GetScenePresence(client.AgentId);
            IUserProfileInfo info = m_profileConnector.GetUserProfile(client.AgentId);
            if(info != null)
                DisplayNameUpdate(info.DisplayName, info.DisplayName, us, client.AgentId);

            foreach (ScenePresence SP in ((Scene)client.Scene).ScenePresences)
            {
                info = m_profileConnector.GetUserProfile(SP.UUID);
                //Send to the incoming user all known display names of avatar's around the client
                if(info != null)
                    DisplayNameUpdate(info.DisplayName, info.DisplayName, SP, client.AgentId);
            }
        }

        /// <summary>
        /// Set the display name for the given user
        /// </summary>
        /// <param name="mDhttpMethod"></param>
        /// <param name="agentID"></param>
        /// <returns></returns>
        private Hashtable ProcessSetDisplayName(Hashtable mDhttpMethod, UUID agentID)
        {
            try
            {
                ScenePresence m_avatar = FindAv(agentID);
                OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
                OSDArray display_name = (OSDArray)rm["display_name"];
                string oldDisplayName = display_name[0].AsString();
                string newDisplayName = display_name[1].AsString();

                //Check to see if their name contains a banned character
                foreach (string bannedUserName in bannedNames)
                {
                    string BannedUserName = bannedUserName.Replace(" ", "");
                    if (newDisplayName.ToLower().Contains(BannedUserName.ToLower()))
                    {
                        //Revert the name to the original and send them a warning
                        m_log.Warn("[DisplayNamesModule]: " + m_avatar.Firstname + " " + m_avatar.Lastname + " attempted to set their display name to a banned name '" + newDisplayName + "'.");
                        newDisplayName = m_avatar.Firstname + " " + m_avatar.Lastname;
                        m_avatar.ControllingClient.SendAlertMessage("You cannot update your display name to the name chosen, your name has been reverted. This request has been logged.");
                        break; //No more checking
                    }
                }

                IUserProfileInfo info = m_profileConnector.GetUserProfile(agentID);
                if (info == null)
                {
                    m_avatar.ControllingClient.SendAlertMessage("You cannot update your display name currently as your profile cannot be found.");
                }
                else
                {
                    //Set the name
                    info.DisplayName = newDisplayName;
                    m_profileConnector.UpdateUserProfile(info);
                }

                
                //One for us
                DisplayNameUpdate(newDisplayName, oldDisplayName, m_avatar, m_avatar.UUID);

                foreach (Scene scene in m_scenes)
                {
                    foreach (ScenePresence SP in scene.ScenePresences)
                    {
                        //Enable this after we do checking for draw distance!
                        //if (Vector3.Distance(SP.AbsolutePosition, m_avatar.AbsolutePosition) < SP.DrawDistance)
                        //{
                            //Update all others
                            DisplayNameUpdate(newDisplayName, oldDisplayName, m_avatar, SP.UUID);
                        //}
                    }
                }
                //The reply
                SetDisplayNameReply(newDisplayName, oldDisplayName, m_avatar);
            }
            catch
            {
            }
            //Send back data
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";

            return responsedata;
        }

        /// <summary>
        /// Find the av in the known regions
        /// </summary>
        /// <param name="uUID"></param>
        /// <returns></returns>
        private ScenePresence FindAv(UUID uUID)
        {
            ScenePresence Sp = null;
            foreach (Scene scene in m_scenes)
            {
                if (scene.TryGetScenePresence(uUID, out Sp))
                    return Sp;
            }
            return Sp;
        }

        /// <summary>
        /// Get the user's display name, currently not used?
        /// </summary>
        /// <param name="mDhttpMethod"></param>
        /// <param name="agentID"></param>
        /// <returns></returns>
        private Hashtable ProcessGetDisplayName(Hashtable mDhttpMethod, UUID agentID)
        {
            //I've never seen this come in, so for now... do nothing
            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);

            m_log.Error("[DisplayNamesModule] : Report this! GetDisplayName : " + rm.ToString());
            IUserProfileInfo info = m_profileConnector.GetUserProfile(agentID);
                
            //Send back data
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";
            return responsedata;
        }

        public void Close()
        {

        }

        public string Name
        {
            get { return "DisplayNamesModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        #region Event Queue

        /// <summary>
        /// Send the user a display name update
        /// </summary>
        /// <param name="newDisplayName"></param>
        /// <param name="oldDisplayName"></param>
        /// <param name="InfoFromAv"></param>
        /// <param name="ToAgentID"></param>
        public void DisplayNameUpdate(string newDisplayName, string oldDisplayName, ScenePresence InfoFromAv, UUID ToAgentID)
        {
            if (m_eventQueue != null)
            {
                //If the DisplayName is blank, the client refuses to do anything, so we send the name by default
                if (newDisplayName == "")
                    newDisplayName = InfoFromAv.Name;

                bool isDefaultName = false;
                if (newDisplayName == InfoFromAv.Name)
                    isDefaultName = true;
                else if (newDisplayName == InfoFromAv.Firstname + "." + InfoFromAv.Lastname)
                    isDefaultName = true;

                OSD item = DisplayNameUpdate(newDisplayName, oldDisplayName, InfoFromAv.UUID, isDefaultName, InfoFromAv.Firstname, InfoFromAv.Lastname, InfoFromAv.Firstname + "." + InfoFromAv.Lastname);
                m_eventQueue.Enqueue(item, ToAgentID);
            }
        }

        /// <summary>
        /// Reply to the set display name reply
        /// </summary>
        /// <param name="newDisplayName"></param>
        /// <param name="oldDisplayName"></param>
        /// <param name="m_avatar"></param>
        public void SetDisplayNameReply(string newDisplayName, string oldDisplayName, ScenePresence m_avatar)
        {
            if (m_eventQueue != null)
            {
                bool isDefaultName = false;
                if (newDisplayName == m_avatar.Name)
                    isDefaultName = true;
                else if (newDisplayName == m_avatar.Firstname + "." + m_avatar.Lastname)
                    isDefaultName = true;

                OSD item = DisplayNameReply(newDisplayName, oldDisplayName, m_avatar.UUID, isDefaultName, m_avatar.Firstname, m_avatar.Lastname, m_avatar.Firstname + "." + m_avatar.Lastname);
                m_eventQueue.Enqueue(item,  m_avatar.UUID);
            }
        }

        /// <summary>
        /// Tell the user about an update
        /// </summary>
        /// <param name="newDisplayName"></param>
        /// <param name="oldDisplayName"></param>
        /// <param name="ID"></param>
        /// <param name="isDefault"></param>
        /// <param name="First"></param>
        /// <param name="Last"></param>
        /// <param name="Account"></param>
        /// <returns></returns>
        public OSD DisplayNameUpdate(string newDisplayName, string oldDisplayName, UUID ID, bool isDefault, string First, string Last, string Account)
        {
            OSDMap nameReply = new OSDMap();
            nameReply.Add("message", OSD.FromString("DisplayNameUpdate"));

            OSDMap body = new OSDMap();
            OSDMap agentData = new OSDMap();
            agentData.Add("display_name", OSD.FromString(newDisplayName));
            agentData.Add("display_name_next_update", OSD.FromDate(DateTime.ParseExact("1970-01-01 00:00:00 +0", "yyyy-MM-dd hh:mm:ss z", System.Globalization.DateTimeFormatInfo.InvariantInfo).ToUniversalTime()));
            agentData.Add("id", OSD.FromUUID(ID));
            agentData.Add("is_display_name_default", OSD.FromBoolean(isDefault));
            agentData.Add("legacy_first_name", OSD.FromString(First));
            agentData.Add("legacy_last_name", OSD.FromString(Last));
            agentData.Add("username", OSD.FromString(Account));
            body.Add("agent", agentData);
            body.Add("agent_id", OSD.FromUUID(ID));
            body.Add("old_display_name", OSD.FromString(oldDisplayName));

            nameReply.Add("body", body);

            return nameReply;
        }

        /// <summary>
        /// Send back a user's display name
        /// </summary>
        /// <param name="newDisplayName"></param>
        /// <param name="oldDisplayName"></param>
        /// <param name="ID"></param>
        /// <param name="isDefault"></param>
        /// <param name="First"></param>
        /// <param name="Last"></param>
        /// <param name="Account"></param>
        /// <returns></returns>
        public OSD DisplayNameReply(string newDisplayName, string oldDisplayName, UUID ID, bool isDefault, string First, string Last, string Account)
        {
            OSDMap nameReply = new OSDMap();

            OSDMap body = new OSDMap();
            OSDMap content = new OSDMap();
            OSDMap agentData = new OSDMap();
            content.Add("display_name", OSD.FromString(newDisplayName));
            content.Add("display_name_next_update", OSD.FromDate(DateTime.ParseExact("1970-01-01 00:00:00 +0", "yyyy-MM-dd hh:mm:ss z", System.Globalization.DateTimeFormatInfo.InvariantInfo).ToUniversalTime()));
            content.Add("id", OSD.FromUUID(ID));
            content.Add("is_display_name_default", OSD.FromBoolean(isDefault));
            content.Add("legacy_first_name", OSD.FromString(First));
            content.Add("legacy_last_name", OSD.FromString(Last));
            content.Add("username", OSD.FromString(Account));
            body.Add("content", content);
            body.Add("agent", agentData);
            body.Add("reason", OSD.FromString("OK"));
            body.Add("status", OSD.FromInteger(200));

            nameReply.Add("body", body);
            nameReply.Add("message", OSD.FromString("SetDisplayNameReply"));

            return nameReply;
        }

        #endregion
    }
}
