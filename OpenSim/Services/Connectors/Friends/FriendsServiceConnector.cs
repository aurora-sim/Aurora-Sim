/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;

namespace OpenSim.Services.Connectors
{
    public class FriendsServicesConnector : IFriendsService, IService
    {
        private IRegistryCore m_registry;

        public virtual IFriendsService InnerService
        {
            get { return this; }
        }

        #region IFriendsService

        public FriendInfo[] GetFriends(UUID PrincipalID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["METHOD"] = "getfriends";

            string reqString = WebUtils.BuildQueryString(sendData);

            List<FriendInfo> finfos = new List<FriendInfo>();
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("FriendsServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in serverURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                 m_ServerURI,
                                                                                                                                 reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
                    if (replyData != null)
                    {
                        if (replyData.ContainsKey("result") && (replyData["result"].ToString().ToLower() == "null"))
                            continue;

                        Dictionary<string, object>.ValueCollection finfosList = replyData.Values;
                        //MainConsole.Instance.DebugFormat("[FRIENDS CONNECTOR]: get neighbours returned {0} elements", rinfosList.Count);
                        foreach (object f in finfosList)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                FriendInfo finfo = new FriendInfo((Dictionary<string, object>) f);
                                finfos.Add(finfo);
                            }
                            else
                                MainConsole.Instance.DebugFormat(
                                    "[FRIENDS CONNECTOR]: GetFriends {0} received invalid response type {1}",
                                    PrincipalID, f.GetType());
                        }
                    }

                    else
                        MainConsole.Instance.DebugFormat("[FRIENDS CONNECTOR]: GetFriends {0} received null response",
                                          PrincipalID);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[FRIENDS CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
            }

            // Success
            return finfos.ToArray();
        }

        public bool StoreFriend(UUID PrincipalID, string Friend, int flags)
        {
            FriendInfo finfo = new FriendInfo {PrincipalID = PrincipalID, Friend = Friend, MyFlags = flags};

            Dictionary<string, object> sendData = finfo.ToKeyValuePairs();

            sendData["METHOD"] = "storefriend";

            string reply = string.Empty;
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(PrincipalID.ToString(),
                                                                                           "FriendsServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      WebUtils.BuildQueryString(sendData));
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if ((replyData != null) && replyData.ContainsKey("Result") && (replyData["Result"] != null))
                        {
                            bool success = false;
                            Boolean.TryParse(replyData["Result"].ToString(), out success);
                            if (replyData["Result"].ToString() == "Success")
                                return true;
                            if (success)
                                return success;
                        }
                        else
                            MainConsole.Instance.DebugFormat("[FRIENDS CONNECTOR]: StoreFriend {0} {1} received null response",
                                              PrincipalID, Friend);
                    }
                    else
                        MainConsole.Instance.DebugFormat("[FRIENDS CONNECTOR]: StoreFriend received null reply");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[FRIENDS CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
                return false;
            }

            return false;
        }

        public bool Delete(UUID PrincipalID, string Friend)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["FRIEND"] = Friend;
            sendData["METHOD"] = "deletefriend";

            string reply = string.Empty;
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("FriendsServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      WebUtils.BuildQueryString(sendData));
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if ((replyData != null) && replyData.ContainsKey("Result") && (replyData["Result"] != null))
                        {
                            bool success = false;
                            Boolean.TryParse(replyData["Result"].ToString(), out success);
                            if (replyData["Result"].ToString() == "Success")
                                return true;
                            if (success)
                                return success;
                        }
                        else
                            MainConsole.Instance.DebugFormat("[FRIENDS CONNECTOR]: DeleteFriend {0} {1} received null response",
                                              PrincipalID, Friend);
                    }
                    else
                        MainConsole.Instance.DebugFormat("[FRIENDS CONNECTOR]: DeleteFriend received null reply");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[FRIENDS CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
                return false;
            }
            return false;
        }

        #endregion

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("FriendsHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IFriendsService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}