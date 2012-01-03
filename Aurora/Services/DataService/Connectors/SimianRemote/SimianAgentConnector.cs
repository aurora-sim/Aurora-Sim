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

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class SimianAgentConnector : IAgentConnector
    {
        private List<string> m_ServerURIs = new List<string>();

        #region IAgentConnector Members

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AgentConnector", "LocalConnector") == "SimianConnector")
            {
                m_ServerURIs = simBase.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IAgentConnector"; }
        }

        public IAgentInfo GetAgent(UUID PrincipalID)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetUser"},
                                                      {"UserID", PrincipalID.ToString()}
                                                  };

            OSDMap result = PostData(PrincipalID, requestArgs);

            if (result == null)
                return null;

            if (result.ContainsKey("AgentInfo"))
            {
                OSDMap agentmap = (OSDMap) OSDParser.DeserializeJson(result["AgentInfo"].AsString());

                IAgentInfo agent = new IAgentInfo();
                agent.FromOSD(agentmap);

                return agent;
            }

            return null;
        }

        public void UpdateAgent(IAgentInfo agent)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "AddUserData"},
                                                      {"UserID", agent.PrincipalID.ToString()},
                                                      {"AgentInfo", OSDParser.SerializeJsonString(agent.ToOSD())}
                                                  };

            PostData(agent.PrincipalID, requestArgs);
        }

        public void CreateNewAgent(UUID PrincipalID)
        {
            IAgentInfo info = new IAgentInfo {PrincipalID = PrincipalID};

            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "AddUserData"},
                                                      {"UserID", info.PrincipalID.ToString()},
                                                      {"AgentInfo", OSDParser.SerializeJsonString(info.ToOSD())}
                                                  };

            PostData(info.PrincipalID, requestArgs);
        }

        public bool CheckMacAndViewer(string Mac, string viewer, out string reason)
        {
            //Only local! You should not be calling this!! This method is only called 
            // from LLLoginHandlers.
            reason = "";
            return false;
        }

        #endregion

        #region Helpers

        private OSDMap PostData(UUID userID, NameValueCollection nvc)
        {
#if (!ISWIN)
            foreach (string mServerUri in m_ServerURIs)
            {
                OSDMap response = WebUtils.PostToService(mServerUri, nvc);
                if (response["Success"].AsBoolean() && response["User"] is OSDMap)
                {
                    return (OSDMap) response["User"];
                }
                else
                {
                    MainConsole.Instance.Error("[SIMIAN AGENTS CONNECTOR]: Failed to fetch agent info data for " + userID + ": " + response["Message"].AsString());
                }
            }
#else
            foreach (OSDMap response in m_ServerURIs.Select(m_ServerURI => WebUtils.PostToService(m_ServerURI, nvc)))
            {
                if (response["Success"].AsBoolean() && response["User"] is OSDMap)
                {
                    return (OSDMap) response["User"];
                }
                else
                {
                    MainConsole.Instance.Error("[SIMIAN AGENTS CONNECTOR]: Failed to fetch agent info data for " + userID + ": " +
                                response["Message"].AsString());
                }
            }
#endif

            return null;
        }

        #endregion

        public void Dispose()
        {
        }
    }
}