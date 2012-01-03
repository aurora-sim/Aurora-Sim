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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.Connectors
{
    public class AbuseReportsConnector : IAbuseReports, IService
    {
        private IRegistryCore m_registry;

        #region IAbuseReports

        public void AddAbuseReport(AbuseReport abuse_report)
        {
            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AbuseReportsServerURI");
                foreach (string m_ServerURI in m_ServerURIs)
                {
                    Dictionary<string, object> ar = abuse_report.ToKVP();
                    ar.Add("METHOD", "AddAbuseReport");

                    SynchronousRestFormsRequester.MakeRequest("POST",
                                                              m_ServerURI,
                                                              WebUtils.BuildQueryString(ar));
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[ABUSEREPORT CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
            }
        }

        public AbuseReport GetAbuseReport(int Number, string Password)
        {
            try
            {
                Dictionary<string, object> send = new Dictionary<string, object>
                                                      {{"Password", Password}, {"METHOD", "GetAbuseReport"}};
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AbuseReportsServerURI");
                AbuseReport ar = new AbuseReport();
                ar.FromKVP(WebUtils.ParseXmlResponse(SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                           m_ServerURIs[
                                                                                                               0],
                                                                                                           WebUtils.
                                                                                                               BuildQueryString
                                                                                                               (send))));
                return ar;
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[ABUSEREPORT CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
                return null;
            }
        }

        public List<AbuseReport> GetAbuseReports(int start, int count, string filter)
        {
            try
            {
                Dictionary<string, object> send = new Dictionary<string, object>
                                                      {
                                                          {"start", start},
                                                          {"count", count},
                                                          {"filter", filter},
                                                          {"METHOD", "GetAbuseReports"}
                                                      };
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AbuseReportsServerURI");
                Dictionary<string, object> ars =
                    WebUtils.ParseXmlResponse(SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                        m_ServerURIs[0],
                                                                                        WebUtils.BuildQueryString(send)));
                return ars.Cast<AbuseReport>().ToList();
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[ABUSEREPORT CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
                return null;
            }
        }

        public void UpdateAbuseReport(AbuseReport report, string Password)
        {
            try
            {
                Dictionary<string, object> send = report.ToKVP();
                send.Add("Password", Password);
                send.Add("METHOD", "AddAbuseReport");
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf("AbuseReportsServerURI");
                SynchronousRestFormsRequester.MakeRequest("POST",
                                                          m_ServerURIs[0],
                                                          WebUtils.BuildQueryString(send));
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[ABUSEREPORT CONNECTOR]: Exception when contacting friends server: {0}", e.Message);
            }
        }

        #endregion

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IService Members

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