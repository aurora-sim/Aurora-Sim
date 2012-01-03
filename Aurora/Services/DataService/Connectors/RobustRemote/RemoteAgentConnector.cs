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
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class RemoteAgentConnector : IAgentConnector
    {
        private readonly ExpiringCache<UUID, IAgentInfo> m_cache = new ExpiringCache<UUID, IAgentInfo>();
        private IRegistryCore m_registry;

        #region IAgentConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AgentConnector", "LocalConnector") == "RemoteConnector")
            {
                m_registry = simBase;
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IAgentConnector"; }
        }

        public IAgentInfo GetAgent(UUID PrincipalID)
        {
            IAgentInfo agent;
            if (!m_cache.TryGetValue(PrincipalID, out agent))
                return agent;

            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["METHOD"] = "getagent";

            string reqString = WebUtils.BuildQueryString(sendData);

            try
            {
                List<string> m_ServerURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(PrincipalID.ToString(),
                                                                                           "RemoteServerURI");
                foreach (Dictionary<string, object> replyData in from m_ServerURI in m_ServerURIs select SynchronousRestFormsRequester.MakeRequest("POST",
                                                                                                                                   m_ServerURI + "/auroradata",
                                                                                                                                   reqString) into reply where reply != string.Empty select WebUtils.ParseXmlResponse(reply))
                {
                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("result"))
                            return null;

                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                agent = new IAgentInfo();
                                agent.FromKVP((Dictionary<string, object>) f);
                                m_cache.AddOrUpdate(PrincipalID, agent, new TimeSpan(0, 30, 0));
                            }
                            else
                                MainConsole.Instance.DebugFormat(
                                    "[AuroraRemoteAgentConnector]: GetAgent {0} received invalid response type {1}",
                                    PrincipalID, f.GetType());
                        }
                        // Success
                        return agent;
                    }

                    else
                        MainConsole.Instance.DebugFormat("[AuroraRemoteAgentConnector]: GetAgent {0} received null response",
                                          PrincipalID);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteAgentConnector]: Exception when contacting server: {0}", e);
            }

            return null;
        }

        public void UpdateAgent(IAgentInfo agent)
        {
            try
            {
                List<string> serverURIs =
                    m_registry.RequestModuleInterface<IConfigurationService>().FindValueOf(
                        agent.PrincipalID.ToString(), "RemoteServerURI");
                foreach (string url in serverURIs)
                {
                    OpenMetaverse.StructuredData.OSDMap map = new OpenMetaverse.StructuredData.OSDMap();
                    map["Method"] = "updateagent";
                    map["Agent"] = agent.ToOSD();
                    WebUtils.PostToService(url + "osd", map, false, false);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[AuroraRemoteAgentConnector]: Exception when contacting server: {0}", e);
            }
        }

        public void CreateNewAgent(UUID PrincipalID)
        {
            //No creating from sims!
        }

        public bool CheckMacAndViewer(string Mac, string viewer, out string reason)
        {
            //Only local! You should not be calling this!! This method is only called 
            // from LLLoginHandlers.
            reason = "";
            return false;
        }

        #endregion

        public void Dispose()
        {
        }
    }
}