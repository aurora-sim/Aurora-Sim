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
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.Connectors
{
    public class AvatarServicesConnector : IAvatarService, IService
    {
        protected IRegistryCore m_registry;

        #region IAvatarService

        public virtual AvatarAppearance GetAppearance(UUID userID)
        {
            MainConsole.Instance.Info("[AvatarServiceConnector] GetAppearance");
            AvatarData avatar = GetAvatar(userID);
            if (avatar == null)
                return null;
            return avatar.ToAvatarAppearance(userID);
        }

        public virtual bool SetAppearance(UUID userID, AvatarAppearance appearance)
        {
            AvatarData avatar = new AvatarData(appearance);
            return SetAvatar(userID, avatar);
        }

        public virtual AvatarData GetAvatar(UUID userID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getavatar";

            sendData["UserID"] = userID;

            string reply = string.Empty;
            string reqString = WebUtils.BuildQueryString(sendData);
            AvatarData avatar = null;
            // MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: queryString = {0}", reqString);
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AvatarServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                      m_ServerURI,
                                                                      reqString);
                    if (reply == null || (reply != null && reply == string.Empty))
                    {
                        MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: GetAgent received null or empty reply");
                        return null;
                    }
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if ((replyData != null) && replyData.ContainsKey("result") && (replyData["result"] != null))
                    {
                        if (replyData["result"] is Dictionary<string, object>)
                        {
                            avatar = new AvatarData((Dictionary<string, object>) replyData["result"]);
                            return avatar;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: Exception when contacting presence server: {0}", e.Message);
            }

            return avatar;
        }

        public virtual bool SetAvatar(UUID userID, AvatarData avatar)
        {
            m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler("SetAppearance",
                                                                                                      new object[2]
                                                                                                          {
                                                                                                              userID,
                                                                                                              avatar
                                                                                                          });
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "setavatar";

            sendData["UserID"] = userID.ToString();

            Dictionary<string, object> structData = avatar.ToKVP();

            foreach (KeyValuePair<string, object> kvp in structData)
                sendData[kvp.Key] = kvp.Value.ToString();


            string reqString = WebUtils.BuildQueryString(sendData);
            //MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: queryString = {0}", reqString);
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AvatarServerURI");
                foreach (string mServerUri in serverURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData.ContainsKey("result"))
                        {
                            if (replyData["result"].ToString().ToLower() == "success")
                                return true;
                        }
                        else
                            MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: SetAvatar reply data does not contain result field");
                    }
                    else
                        MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: SetAvatar received empty reply");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: Exception when contacting avatar server: {0}", e.Message);
            }

            return false;
        }

        public virtual bool ResetAvatar(UUID userID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "resetavatar";

            sendData["UserID"] = userID.ToString();

            string reqString = WebUtils.BuildQueryString(sendData);
            // MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: queryString = {0}", reqString);
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AvatarServerURI");
                foreach (string mServerUri in serverURIs)
                {
                    string reply = SynchronousRestFormsRequester.MakeRequest("POST", mServerUri, reqString);
                    if (reply != string.Empty)
                    {
                        Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                        if (replyData.ContainsKey("result"))
                        {
                            if (replyData["result"].ToString().ToLower() == "success")
                                return true;
                        }
                        else
                            MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: SetItems reply data does not contain result field");
                    }
                    else
                        MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: SetItems received empty reply");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: Exception when contacting avatar server: {0}", e.Message);
            }

            return false;
        }

        public virtual void CacheWearableData(UUID principalID, AvatarWearable cachedWearable)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "cachewearabledata";

            sendData["WEARABLES"] = OSDParser.SerializeJsonString(cachedWearable.Pack());

            string reqString = WebUtils.BuildQueryString(sendData);
            // MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: queryString = {0}", reqString);
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AvatarServerURI");
                foreach (string m_ServerURI in serverURIs)
                {
                    AsynchronousRestObjectRequester.MakeRequest("POST",
                                                                m_ServerURI,
                                                                reqString);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AVATAR CONNECTOR]: Exception when contacting avatar server: {0}", e.Message);
            }
        }

        #endregion

        public virtual string Name
        {
            get { return GetType().Name; }
        }

        #region IAvatarService Members

        public virtual IAvatarService InnerService
        {
            get { return this; }
        }

        #endregion

        #region IService Members

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AvatarHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IAvatarService>(this);
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual void FinishedStartup()
        {
        }

        #endregion
    }
}