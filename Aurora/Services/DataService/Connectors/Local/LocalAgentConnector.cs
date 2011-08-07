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
using OpenMetaverse;
using Aurora.Framework;
using log4net;
using Nini.Config;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;

namespace Aurora.Services.DataService
{
    public class LocalAgentConnector : IAgentConnector
	{
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            GD = GenericData;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            GD.ConnectToDatabase(defaultConnectionString, "Agent", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));
            DataManager.DataManager.RegisterPlugin(Name+"Local", this);

            if (source.Configs["AuroraConnectors"].GetString("AgentConnector", "LocalConnector") == "LocalConnector")
            {
                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IAgentConnector"; }
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Gets the info about the agent (TOS data, maturity info, language, etc)
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns></returns>
        public IAgentInfo GetAgent(UUID agentID)
		{
			IAgentInfo agent = new IAgentInfo();
            List<string> query = null;
            try
            {
                query = GD.Query(new[] { "ID", "`Key`" }, new object[] { agentID, "AgentInfo" }, "userdata", "Value");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return null; //Couldn't find it, return null then.

            OSDMap agentInfo = (OSDMap)OSDParser.DeserializeLLSDXml(query[0]);

            agent.FromOSD(agentInfo);
            agent.PrincipalID = agentID;
			return agent;
		}

        /// <summary>
        /// Updates the language and maturity params of the agent.
        /// Note: we only allow for this on the grid side
        /// </summary>
        /// <param name="agent"></param>
        public void UpdateAgent(IAgentInfo agent)
		{
            List<object> SetValues = new List<object> {OSDParser.SerializeLLSDXmlString(agent.ToOSD())};
            List<string> SetRows = new List<string> {"Value"};
           
            
            
            List<object> KeyValue = new List<object> {agent.PrincipalID, "AgentInfo"};

            List<string> KeyRow = new List<string> {"ID", "`Key`"};

            GD.Update("userdata", SetValues.ToArray(), SetRows.ToArray(), KeyRow.ToArray(), KeyValue.ToArray());
		}

        /// <summary>
        /// Creates a new database entry for the agent.
        /// Note: we only allow for this on the grid side
        /// </summary>
        /// <param name="agentID"></param>
        public void CreateNewAgent(UUID agentID)
		{
            List<object> values = new List<object> {agentID, "AgentInfo"};
            IAgentInfo info = new IAgentInfo {PrincipalID = agentID};
            values.Add(OSDParser.SerializeLLSDXmlString(info.ToOSD())); //Value which is a default Profile
            GD.Insert("userdata", values.ToArray());
		}

        /// <summary>
        /// Checks whether the mac address and viewer are allowed to connect to this grid.
        /// Note: we only allow for this on the grid side
        /// </summary>
        /// <param name="Mac"></param>
        /// <param name="viewer"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool CheckMacAndViewer(string Mac, string viewer, out string reason)
        {
            List<string> found = GD.Query("macaddress", Mac, "macban", "*");
            if (found.Count != 0)
            {
                //Found a mac that matched
                reason = "Your Mac Address has been banned, please contact a grid administrator.";
                m_log.InfoFormat("[AgentInfoConnector]: Mac '{0}' is in the ban list", Mac);
                return false;
            }

            //Viewer Ban Start
            List<string> clientfound = GD.Query("Client", viewer, "bannedviewers", "*");
            if (clientfound.Count != 0)
            {
                reason = "The viewer you are using has been banned, please use a different viewer.";
                m_log.InfoFormat("[AgentInfoConnector]: Viewer '{0}' is in the ban list", viewer);
                return false;
            }
            reason = "";
            return true;
        }
    }
}
