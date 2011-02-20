using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using log4net;
using Nini.Config;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;

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

            GD.ConnectToDatabase(defaultConnectionString, "Agent");
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
                query = GD.Query(new string[] { "ID", "`Key`" }, new object[] { agentID, "AgentInfo" }, "userdata", "Value");
            }
            catch
            {
            }
            if (query == null || query.Count == 0)
                return null; //Couldn't find it, return null then.

            OSDMap agentInfo = (OSDMap)OSDParser.DeserializeLLSDXml(query[0]);

            agent.FromOSD(agentInfo);
			return agent;
		}

        /// <summary>
        /// Updates the language and maturity params of the agent.
        /// Note: we only allow for this on the grid side
        /// </summary>
        /// <param name="agent"></param>
        public void UpdateAgent(IAgentInfo agent)
		{
            List<object> SetValues = new List<object>();
            List<string> SetRows = new List<string>();
            SetRows.Add("Value");
            SetValues.Add(OSDParser.SerializeLLSDXmlString(agent.ToOSD()));
            
            
            List<object> KeyValue = new List<object>();
            List<string> KeyRow = new List<string>();
            KeyRow.Add("ID");
            KeyValue.Add(agent.PrincipalID);
            KeyRow.Add("`Key`");
            KeyValue.Add("AgentInfo");
            GD.Update("userdata", SetValues.ToArray(), SetRows.ToArray(), KeyRow.ToArray(), KeyValue.ToArray());
		}

        /// <summary>
        /// Creates a new database entry for the agent.
        /// Note: we only allow for this on the grid side
        /// </summary>
        /// <param name="agentID"></param>
        public void CreateNewAgent(UUID agentID)
		{
            List<object> values = new List<object>();
            values.Add(agentID);
            values.Add("AgentInfo");
            IAgentInfo info = new IAgentInfo();
            info.PrincipalID = agentID;
            values.Add(OSDParser.SerializeLLSDXmlString(info.ToOSD())); //Value which is a default Profile
            GD.Insert("userdata", values.ToArray());
		}

        /// <summary>
        /// Checks whether the mac address and viewer are allowed to connect to this grid.
        /// Note: we only allow for this on the grid side
        /// </summary>
        /// <param name="Mac"></param>
        /// <param name="viewer"></param>
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
