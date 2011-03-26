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
using Aurora.Simulation.Base;
using OpenMetaverse;

namespace OpenSim.Services.Connectors
{
    public class AbuseReportsConnector : IAbuseReports, IService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private IRegistryCore m_registry;

        #region IAbuseReports

        public void AddAbuseReport(AbuseReport abuse_report)
        {
            try
            {
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    Dictionary<string, object> ar = abuse_report.ToKeyValuePairs();
                    ar.Add("METHOD", "AddAbuseReport");

                    SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/abusereport",
                        WebUtils.BuildQueryString(ar));
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[ABUSEREPORT CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
            }
        }

        public AbuseReport GetAbuseReport(int Number, string Password)
        {
            try
            {
                Dictionary<string, object> send = new Dictionary<string, object>();
                send.Add("Password", Password);
                send.Add("METHOD", "GetAbuseReport");
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                return new AbuseReport(WebUtils.ParseXmlResponse(SynchronousRestFormsRequester.MakeRequest("POST",
                    m_ServerURIs[0] + "/abusereport",
                    WebUtils.BuildQueryString(send))));
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[ABUSEREPORT CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
                return null;
            }
        }

        public List<AbuseReport> GetAbuseReports(int start, int count, string filter)
        {
            try
            {
                Dictionary<string, object> send = new Dictionary<string, object>();
                send.Add("start", start);
                send.Add("count", count);
                send.Add("filter", filter);
                send.Add("METHOD", "GetAbuseReports");
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                Dictionary<string, object> ars = WebUtils.ParseXmlResponse(SynchronousRestFormsRequester.MakeRequest("POST",
                    m_ServerURIs[0] + "/abusereport",
                    WebUtils.BuildQueryString(send)));
                List<AbuseReport> returnvalue = new List<AbuseReport>();
                foreach (object ar in ars)
                    returnvalue.Add((AbuseReport)ar);
                return returnvalue;
                    
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[ABUSEREPORT CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
                return null;
            }
        }

        public void UpdateAbuseReport(AbuseReport report, string Password)
        {
            try
            {
                Dictionary<string, object> send = report.ToKeyValuePairs();
                send.Add("Password", Password);
                send.Add("METHOD", "AddAbuseReport");
                List<string> m_ServerURIs = m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                SynchronousRestFormsRequester.MakeRequest("POST",
                    m_ServerURIs[0] + "/abusereport",
                    WebUtils.BuildQueryString(send));
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[ABUSEREPORT CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
            }
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

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AbuseReportHandler", "") != Name)
                return;

            m_registry = registry;
            registry.RegisterModuleInterface<IAbuseReports>(this);
        }

        public void FinishedStartup()
        {

        }

        #endregion
    }
}