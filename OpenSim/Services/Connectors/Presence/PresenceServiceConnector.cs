/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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

using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Aurora.Simulation.Base;
using OpenMetaverse;

namespace OpenSim.Services.Connectors
{
    public class PresenceServicesConnector : IPresenceService, IService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = String.Empty;

        #region IPresenceService

        public bool LoginAgent(string userID, UUID sessionID, UUID secureSessionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "login";

            sendData["UserID"] = userID;
            sendData["SessionID"] = sessionID.ToString();
            sendData["SecureSessionID"] = secureSessionID.ToString();

            string reqString = WebUtils.BuildQueryString(sendData);
            // m_log.DebugFormat("[PRESENCE CONNECTOR]: queryString = {0}", reqString);
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/presence",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData.ContainsKey("result"))
                    {
                        if (replyData["result"].ToString().ToLower() == "success")
                            return true;
                        else
                            return false;
                    }
                    else
                        m_log.DebugFormat("[PRESENCE CONNECTOR]: LoginAgent reply data does not contain result field");

                }
                else
                    m_log.DebugFormat("[PRESENCE CONNECTOR]: LoginAgent received empty reply");
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[PRESENCE CONNECTOR]: Exception when contacting presence server: {0}", e.Message);
            }

            return false;

        }

        public bool LogoutAgent(UUID sessionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "logout";

            sendData["SessionID"] = sessionID.ToString();

            string reqString = WebUtils.BuildQueryString(sendData);
            // m_log.DebugFormat("[PRESENCE CONNECTOR]: queryString = {0}", reqString);
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/presence",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData.ContainsKey("result"))
                    {
                        if (replyData["result"].ToString().ToLower() == "success")
                            return true;
                        else
                            return false;
                    }
                    else
                        m_log.DebugFormat("[PRESENCE CONNECTOR]: LogoutAgent reply data does not contain result field");

                }
                else
                    m_log.DebugFormat("[PRESENCE CONNECTOR]: LogoutAgent received empty reply");
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[PRESENCE CONNECTOR]: Exception when contacting presence server: {0}", e.Message);
            }

            return false;
        }

        public bool LogoutRegionAgents(UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "logoutregion";

            sendData["RegionID"] = regionID.ToString();

            string reqString = WebUtils.BuildQueryString(sendData);
            // m_log.DebugFormat("[PRESENCE CONNECTOR]: queryString = {0}", reqString);
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/presence",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData.ContainsKey("result"))
                    {
                        if (replyData["result"].ToString().ToLower() == "success")
                            return true;
                        else
                            return false;
                    }
                    else
                        m_log.DebugFormat("[PRESENCE CONNECTOR]: LogoutRegionAgents reply data does not contain result field");

                }
                else
                    m_log.DebugFormat("[PRESENCE CONNECTOR]: LogoutRegionAgents received empty reply");
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[PRESENCE CONNECTOR]: Exception when contacting presence server: {0}", e.Message);
            }

            return false;
        }

        public void ReportAgent(UUID sessionID, UUID regionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "report";

            sendData["SessionID"] = sessionID.ToString();
            sendData["RegionID"] = regionID.ToString();

            string reqString = WebUtils.BuildQueryString(sendData);
            try
            {
                AsynchronousRestObjectRequester.MakeRequest("POST",
                    m_ServerURI + "/presence",
                    reqString);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[PRESENCE CONNECTOR]: Exception when contacting presence server: {0}", e.Message);
            }
        }

        public PresenceInfo GetAgent(UUID sessionID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getagent";

            sendData["SessionID"] = sessionID.ToString();

            string reply = string.Empty;
            string reqString = WebUtils.BuildQueryString(sendData);
            // m_log.DebugFormat("[PRESENCE CONNECTOR]: queryString = {0}", reqString);
            try
            {
                reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/presence",
                        reqString);
                if (reply == null || (reply != null && reply == string.Empty))
                {
                    m_log.DebugFormat("[PRESENCE CONNECTOR]: GetAgent received null or empty reply");
                    return null;
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[PRESENCE CONNECTOR]: Exception when contacting presence server: {0}", e.Message);
            }

            Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);
            PresenceInfo pinfo = null;

            if ((replyData != null) && replyData.ContainsKey("result") && (replyData["result"] != null))
            {
                if (replyData["result"] is Dictionary<string, object>)
                {
                    pinfo = new PresenceInfo((Dictionary<string, object>)replyData["result"]);
                }
            }

            return pinfo;
        }

        public PresenceInfo[] GetAgents(string[] userIDs)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getagents";

            sendData["uuids"] = new List<string>(userIDs);

            string reply = string.Empty;
            string reqString = WebUtils.BuildQueryString(sendData);
            //m_log.DebugFormat("[PRESENCE CONNECTOR]: queryString = {0}", reqString);
            try
            {
                reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/presence",
                        reqString);
                if (reply == null || (reply != null && reply == string.Empty))
                {
                    m_log.DebugFormat("[PRESENCE CONNECTOR]: GetAgents received null or empty reply");
                    return null;
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[PRESENCE CONNECTOR]: Exception when contacting presence server: {0}", e.Message);
            }

            List<PresenceInfo> rinfos = new List<PresenceInfo>();

            Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

            if (replyData != null)
            {
                if (replyData.ContainsKey("result") && 
                    (replyData["result"].ToString() == "null" || replyData["result"].ToString() == "Failure"))
                {
                    return new PresenceInfo[0];
                }

                Dictionary<string, object>.ValueCollection pinfosList = replyData.Values;
                //m_log.DebugFormat("[PRESENCE CONNECTOR]: GetAgents returned {0} elements", pinfosList.Count);
                foreach (object presence in pinfosList)
                {
                    if (presence is Dictionary<string, object>)
                    {
                        PresenceInfo pinfo = new PresenceInfo((Dictionary<string, object>)presence);
                        rinfos.Add(pinfo);
                    }
                    else
                        m_log.DebugFormat("[PRESENCE CONNECTOR]: GetAgents received invalid response type {0}",
                            presence.GetType());
                }
            }
            else
                m_log.DebugFormat("[PRESENCE CONNECTOR]: GetAgents received null response");

            return rinfos.ToArray();
        }

        public string[] GetAgentsLocations(string[] userIDs)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getagentslocations";

            sendData["uuids"] = new List<string>(userIDs);

            string reply = string.Empty;
            string reqString = WebUtils.BuildQueryString(sendData);
            //m_log.DebugFormat("[PRESENCE CONNECTOR]: queryString = {0}", reqString);
            try
            {
                reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/presence",
                        reqString);
                if (reply == null || (reply != null && reply == string.Empty))
                {
                    m_log.DebugFormat("[PRESENCE CONNECTOR]: GetAgents received null or empty reply");
                    return null;
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[PRESENCE CONNECTOR]: Exception when contacting presence server: {0}", e.Message);
            }

            List<string> locations = new List<string>();

            Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

            if (replyData != null)
            {
                if (replyData.ContainsKey("result") &&
                    (replyData["result"].ToString() == "null"))
                {
                    return new string[1]{"Failure"};
                }
                else if (replyData.ContainsKey("result") &&
                    (replyData["result"].ToString() == "noagents"))
                {
                    return new string[1] { "NoAgents" };
                }

                Dictionary<string, object>.ValueCollection pinfosList = replyData.Values;
                //m_log.DebugFormat("[PRESENCE CONNECTOR]: GetAgents returned {0} elements", pinfosList.Count);
                foreach (object presence in pinfosList)
                {
                    locations.Add(presence.ToString());
                }
                return locations.ToArray();
            }
            else
                m_log.DebugFormat("[PRESENCE CONNECTOR]: GetAgents received null response");
            
            return null;
        }

        #endregion


        #region IService Members

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("PresenceHandler", "") != Name)
                return;

            registry.RegisterInterface<IPresenceService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("PresenceHandler", "") != Name)
                return;

            string serviceURI = registry.Get<IAutoConfigurationService>().FindValueOf("PresenceServerURI",
                        "PresenceService");

            if (serviceURI == String.Empty)
            {
                m_log.Error("[PRESENCE CONNECTOR]: No Server URI named in section PresenceService");
                throw new Exception("Presence connector init error");
            }
            m_ServerURI = serviceURI;
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterInterface<IPresenceService>(this);
        }

        #endregion
    }
}
