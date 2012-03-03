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
using System.Reflection;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Services.DataService
{
    public class LocalAgentConnector : ConnectorBase, IAgentConnector
    {
        private IGenericData GD;
        private GenericAccountCache<IAgentInfo> m_cache = new GenericAccountCache<IAgentInfo>();

        #region IAgentConnector Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            GD = GenericData;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase(defaultConnectionString, "Agent",
                                 source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));
            DataManager.DataManager.RegisterPlugin(Name + "Local", this);

            if (source.Configs["AuroraConnectors"].GetString("AgentConnector", "LocalConnector") == "LocalConnector")
            {
                DataManager.DataManager.RegisterPlugin(this);
            }

            Init(simBase, Name);
        }

        public string Name
        {
            get { return "IAgentConnector"; }
        }

        /// <summary>
        ///   Gets the info about the agent (TOS data, maturity info, language, etc)
        /// </summary>
        /// <param name = "agentID"></param>
        /// <returns></returns>
        [CanBeReflected(ThreatLevel=OpenSim.Services.Interfaces.ThreatLevel.Low)]
        public IAgentInfo GetAgent(UUID agentID)
        {
            IAgentInfo agent = new IAgentInfo();
            if (m_cache.Get(agentID, out agent))
                return agent;
            else
                agent = new IAgentInfo();

            object remoteValue = DoRemoteForUser(agentID, agentID);
            if (remoteValue != null || m_doRemoteOnly)
            {
                m_cache.Cache(agentID, (IAgentInfo)remoteValue);
                return (IAgentInfo)remoteValue;
            }

            List<string> query = null;
            try
            {
                QueryFilter filter = new QueryFilter();
                filter.andFilters["ID"] = agentID;
                filter.andFilters["`Key`"] = "AgentInfo";
                query = GD.Query(new string[1] { "`Value`" }, "userdata", filter, null, null, null);
            }
            catch
            {
            }

            if (query == null || query.Count == 0)
            {
                m_cache.Cache(agentID, null);
                return null; //Couldn't find it, return null then.
            }

            OSDMap agentInfo = (OSDMap) OSDParser.DeserializeLLSDXml(query[0]);

            agent.FromOSD(agentInfo);
            agent.PrincipalID = agentID;
            m_cache.Cache(agentID, agent);
            return agent;
        }

        /// <summary>
        ///   Updates the language and maturity params of the agent.
        ///   Note: we only allow for this on the grid side
        /// </summary>
        /// <param name = "agent"></param>
        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.Full)]
        public void UpdateAgent(IAgentInfo agent)
        {
            CacheAgent(agent);
            object remoteValue = DoRemoteForUser(agent.PrincipalID, agent.ToOSD());
            if (remoteValue != null || m_doRemoteOnly)
                return;

            Dictionary<string, object> values = new Dictionary<string, object>(1);
            values["Value"] = OSDParser.SerializeLLSDXmlString(agent.ToOSD());

            QueryFilter filter = new QueryFilter();
            filter.andFilters["ID"] = agent.PrincipalID;
            filter.andFilters["`Key`"] = "AgentInfo";

            GD.Update("userdata", values, null, filter, null, null);
        }

        public void CacheAgent(IAgentInfo agent)
        {
            m_cache.Cache(agent.PrincipalID, agent);
        }

        /// <summary>
        ///   Creates a new database entry for the agent.
        ///   Note: we only allow for this on the grid side
        /// </summary>
        /// <param name = "agentID"></param>
        public void CreateNewAgent(UUID agentID)
        {
            List<object> values = new List<object> {agentID, "AgentInfo"};
            IAgentInfo info = new IAgentInfo {PrincipalID = agentID};
            values.Add(OSDParser.SerializeLLSDXmlString(info.ToOSD())); //Value which is a default Profile
            GD.Insert("userdata", values.ToArray());
        }

        /// <summary>
        ///   Checks whether the mac address and viewer are allowed to connect to this grid.
        ///   Note: we only allow for this on the grid side
        /// </summary>
        /// <param name = "Mac"></param>
        /// <param name = "viewer"></param>
        /// <param name = "reason"></param>
        /// <returns></returns>
        public bool CheckMacAndViewer(string Mac, string viewer, out string reason)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["macaddress"] = Mac;
            List<string> found = GD.Query(new string[] { "*" }, "macban", filter, null, null, null);

            if (found.Count != 0)
            {
                //Found a mac that matched
                reason = "Your Mac Address has been banned, please contact a grid administrator.";
                MainConsole.Instance.InfoFormat("[AgentInfoConnector]: Mac '{0}' is in the ban list", Mac);
                return false;
            }

            //Viewer Ban Start
            filter.andFilters.Remove("macaddress");
            filter.andFilters["Client"] = viewer;
            found = GD.Query(new string[] { "*" }, "bannedviewers", filter, null, null, null);

            if (found.Count != 0)
            {
                reason = "The viewer you are using has been banned, please use a different viewer.";
                MainConsole.Instance.InfoFormat("[AgentInfoConnector]: Viewer '{0}' is in the ban list", viewer);
                return false;
            }
            reason = "";
            return true;
        }

        #endregion

        public void Dispose()
        {
        }
    }
}