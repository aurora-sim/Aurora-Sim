/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using Aurora.DataManager;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Capabilities;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.CapsService
{
    public class DisplayNamesCAPS : ICapsServiceConnector
    {
        private List<string> bannedNames = new List<string>();
        private IEventQueueService m_eventQueue;
        private IProfileConnector m_profileConnector;
        private IRegionClientCapsService m_service;
        private IUserAccountService m_userService;
        
        #region ICapsServiceConnector Members

        public void RegisterCaps(IRegionClientCapsService service)
        {
            IConfig displayNamesConfig =
                service.ClientCaps.Registry.RequestModuleInterface<ISimulationBase>().ConfigSource.Configs[
                    "DisplayNamesModule"];
            if (displayNamesConfig != null)
            {
                if (!displayNamesConfig.GetBoolean("Enabled", true))
                    return;
                string bannedNamesString = displayNamesConfig.GetString("BannedUserNames", "");
                if (bannedNamesString != "")
                    bannedNames = new List<string>(bannedNamesString.Split(','));
            }
            m_service = service;
            m_profileConnector = DataManager.RequestPlugin<IProfileConnector>();
            m_eventQueue = service.Registry.RequestModuleInterface<IEventQueueService>();
            m_userService = service.Registry.RequestModuleInterface<IUserAccountService>();

            string post = CapsUtil.CreateCAPS("SetDisplayName", "");
            service.AddCAPS("SetDisplayName", post);
            service.AddStreamHandler("SetDisplayName", new GenericStreamHandler("POST", post,
                                                                           ProcessSetDisplayName));

            post = CapsUtil.CreateCAPS("GetDisplayNames", "");
            service.AddCAPS("GetDisplayNames", post);
            service.AddStreamHandler("GetDisplayNames", new GenericStreamHandler("GET", post,
                                                                          ProcessGetDisplayName));
        }

        public void EnteringRegion()
        {
        }

        public void DeregisterCaps()
        {
            if (m_service == null)
                return;//If display names aren't enabled
            m_service.RemoveStreamHandler("SetDisplayName", "POST");
            m_service.RemoveStreamHandler("GetDisplayNames", "GET");
        }

        #endregion

        #region Caps Messages

        /// <summary>
        ///   Set the display name for the given user
        /// </summary>
        /// <param name = "mDhttpMethod"></param>
        /// <param name = "agentID"></param>
        /// <returns></returns>
        private byte[] ProcessSetDisplayName(string path, Stream request,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            try
            {
                OSDMap rm = (OSDMap) OSDParser.DeserializeLLSDXml(request);
                OSDArray display_name = (OSDArray) rm["display_name"];
                string oldDisplayName = display_name[0].AsString();
                string newDisplayName = display_name[1].AsString();

                //Check to see if their name contains a banned character
                if (bannedNames.Select(bannedUserName => bannedUserName.Replace(" ", "")).Any(BannedUserName => newDisplayName.ToLower().Contains(BannedUserName.ToLower())))
                {
                    newDisplayName = m_service.ClientCaps.AccountInfo.Name;
                }

                IUserProfileInfo info = m_profileConnector.GetUserProfile(m_service.AgentID);
                if (info == null)
                {
                    //m_avatar.ControllingClient.SendAlertMessage ("You cannot update your display name currently as your profile cannot be found.");
                }
                else
                {
                    //Set the name
                    info.DisplayName = newDisplayName;
                    m_profileConnector.UpdateUserProfile(info);

                    //One for us
                    DisplayNameUpdate(newDisplayName, oldDisplayName, m_service.ClientCaps.AccountInfo, m_service.AgentID);

                    foreach (IRegionClientCapsService avatar in m_service.RegionCaps.GetClients().Where(avatar => avatar.AgentID != m_service.AgentID))
                    {
                        //Update all others
                        DisplayNameUpdate(newDisplayName, oldDisplayName, m_service.ClientCaps.AccountInfo, avatar.AgentID);
                    }
                    //The reply
                    SetDisplayNameReply(newDisplayName, oldDisplayName, m_service.ClientCaps.AccountInfo);
                }
            }
            catch
            {
            }

            return MainServer.BlankResponse;
        }

        /// <summary>
        ///   Get the user's display name, currently not used?
        /// </summary>
        /// <param name = "mDhttpMethod"></param>
        /// <param name = "agentID"></param>
        /// <returns></returns>
        private byte[] ProcessGetDisplayName(string path, Stream request, OSHttpRequest httpRequest,
                                             OSHttpResponse httpResponse)
        {
            //I've never seen this come in, so for now... do nothing
            NameValueCollection query = HttpUtility.ParseQueryString(httpRequest.Url.Query);
            string[] ids = query.GetValues("ids");
            string username = query.GetOne("username");

            OSDMap map = new OSDMap();
            OSDArray agents = new OSDArray();
            OSDArray bad_ids = new OSDArray();
            OSDArray bad_usernames = new OSDArray();

            if (ids != null)
            {
                foreach (string id in ids)
                {
                    UserAccount account = m_userService.GetUserAccount(m_service.ClientCaps.AccountInfo.AllScopeIDs, UUID.Parse(id));
                    if (account != null)
                    {
                        IUserProfileInfo info =
                            DataManager.RequestPlugin<IProfileConnector>().GetUserProfile(account.PrincipalID);
                        if (info != null)
                            PackUserInfo(info, account, ref agents);
                        else
                            PackUserInfo(info, account, ref agents);
                        //else //Technically is right, but needs to be packed no matter what for OS based grids
                        //    bad_ids.Add (id);
                    }
                }
            }
            else if (username != null)
            {
                UserAccount account = m_userService.GetUserAccount(m_service.ClientCaps.AccountInfo.AllScopeIDs, username.Replace('.', ' '));
                if (account != null)
                {
                    IUserProfileInfo info =
                        DataManager.RequestPlugin<IProfileConnector>().GetUserProfile(account.PrincipalID);
                    if (info != null)
                        PackUserInfo(info, account, ref agents);
                    else
                        bad_usernames.Add(username);
                }
            }

            map["agents"] = agents;
            map["bad_ids"] = bad_ids;
            map["bad_usernames"] = bad_usernames;

            return OSDParser.SerializeLLSDXmlBytes(map);
        }

        private void PackUserInfo(IUserProfileInfo info, UserAccount account, ref OSDArray agents)
        {
            OSDMap agentMap = new OSDMap();
            agentMap["username"] = account.Name;
            agentMap["display_name"] = (info == null || info.DisplayName == "") ? account.Name : info.DisplayName;
            agentMap["display_name_next_update"] =
                OSD.FromDate(
                    DateTime.ParseExact("1970-01-01 00:00:00 +0", "yyyy-MM-dd hh:mm:ss z",
                                        DateTimeFormatInfo.InvariantInfo).ToUniversalTime());
            agentMap["legacy_first_name"] = account.FirstName;
            agentMap["legacy_last_name"] = account.LastName;
            agentMap["id"] = account.PrincipalID;
            agentMap["is_display_name_default"] = isDefaultDisplayName(account.FirstName, account.LastName, account.Name,
                                                                       info == null ? account.Name : info.DisplayName);

            agents.Add(agentMap);
        }

        #region Event Queue

        /// <summary>
        ///   Send the user a display name update
        /// </summary>
        /// <param name = "newDisplayName"></param>
        /// <param name = "oldDisplayName"></param>
        /// <param name = "InfoFromAv"></param>
        /// <param name = "ToAgentID"></param>
        public void DisplayNameUpdate(string newDisplayName, string oldDisplayName, UserAccount InfoFromAv,
                                      UUID ToAgentID)
        {
            if (m_eventQueue != null)
            {
                //If the DisplayName is blank, the client refuses to do anything, so we send the name by default
                if (newDisplayName == "")
                    newDisplayName = InfoFromAv.Name;

                bool isDefaultName = isDefaultDisplayName(InfoFromAv.FirstName, InfoFromAv.LastName, InfoFromAv.Name,
                                                          newDisplayName);

                OSD item = DisplayNameUpdate(newDisplayName, oldDisplayName, InfoFromAv.PrincipalID, isDefaultName,
                                             InfoFromAv.FirstName, InfoFromAv.LastName,
                                             InfoFromAv.FirstName + "." + InfoFromAv.LastName);
                m_eventQueue.Enqueue(item, ToAgentID, m_service.RegionHandle);
            }
        }

        private bool isDefaultDisplayName(string first, string last, string name, string displayName)
        {
            if (displayName == name)
                return true;
            else if (displayName == first + "." + last)
                return true;
            return false;
        }

        /// <summary>
        ///   Reply to the set display name reply
        /// </summary>
        /// <param name = "newDisplayName"></param>
        /// <param name = "oldDisplayName"></param>
        /// <param name = "m_avatar"></param>
        public void SetDisplayNameReply(string newDisplayName, string oldDisplayName, UserAccount m_avatar)
        {
            if (m_eventQueue != null)
            {
                bool isDefaultName = isDefaultDisplayName(m_avatar.FirstName, m_avatar.LastName, m_avatar.Name,
                                                          newDisplayName);

                OSD item = DisplayNameReply(newDisplayName, oldDisplayName, m_avatar.PrincipalID, isDefaultName,
                                            m_avatar.FirstName, m_avatar.LastName,
                                            m_avatar.FirstName + "." + m_avatar.LastName);
                m_eventQueue.Enqueue(item, m_avatar.PrincipalID, m_service.RegionHandle);
            }
        }

        /// <summary>
        ///   Tell the user about an update
        /// </summary>
        /// <param name = "newDisplayName"></param>
        /// <param name = "oldDisplayName"></param>
        /// <param name = "ID"></param>
        /// <param name = "isDefault"></param>
        /// <param name = "First"></param>
        /// <param name = "Last"></param>
        /// <param name = "Account"></param>
        /// <returns></returns>
        public OSD DisplayNameUpdate(string newDisplayName, string oldDisplayName, UUID ID, bool isDefault, string First,
                                     string Last, string Account)
        {
            OSDMap nameReply = new OSDMap {{"message", OSD.FromString("DisplayNameUpdate")}};

            OSDMap body = new OSDMap();
            OSDMap agentData = new OSDMap
                                   {
                                       {"display_name", OSD.FromString(newDisplayName)},
                                       {
                                           "display_name_next_update", OSD.FromDate(
                                               DateTime.ParseExact("1970-01-01 00:00:00 +0", "yyyy-MM-dd hh:mm:ss z",
                                                                   DateTimeFormatInfo.InvariantInfo).ToUniversalTime())
                                           },
                                       {"id", OSD.FromUUID(ID)},
                                       {"is_display_name_default", OSD.FromBoolean(isDefault)},
                                       {"legacy_first_name", OSD.FromString(First)},
                                       {"legacy_last_name", OSD.FromString(Last)},
                                       {"username", OSD.FromString(Account)}
                                   };
            body.Add("agent", agentData);
            body.Add("agent_id", OSD.FromUUID(ID));
            body.Add("old_display_name", OSD.FromString(oldDisplayName));

            nameReply.Add("body", body);

            return nameReply;
        }

        /// <summary>
        ///   Send back a user's display name
        /// </summary>
        /// <param name = "newDisplayName"></param>
        /// <param name = "oldDisplayName"></param>
        /// <param name = "ID"></param>
        /// <param name = "isDefault"></param>
        /// <param name = "First"></param>
        /// <param name = "Last"></param>
        /// <param name = "Account"></param>
        /// <returns></returns>
        public OSD DisplayNameReply(string newDisplayName, string oldDisplayName, UUID ID, bool isDefault, string First,
                                    string Last, string Account)
        {
            OSDMap nameReply = new OSDMap();

            OSDMap body = new OSDMap();
            OSDMap content = new OSDMap();
            OSDMap agentData = new OSDMap();
            content.Add("display_name", OSD.FromString(newDisplayName));
            content.Add("display_name_next_update",
                        OSD.FromDate(
                            DateTime.ParseExact("1970-01-01 00:00:00 +0", "yyyy-MM-dd hh:mm:ss z",
                                                DateTimeFormatInfo.InvariantInfo).ToUniversalTime()));
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

        #endregion
    }
}