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

namespace Aurora.Services.DataService
{
    public class LocalAgentConnector : IAgentConnector
	{
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("AgentConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString);

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
            else
            {
                //Check to make sure that something else exists
                string m_ServerURI = source.Configs["AuroraData"].GetString("RemoteServerURI", "");
                if (m_ServerURI == "") //Blank, not set up
                {
                    OpenSim.Framework.Console.MainConsole.Instance.Output("[AuroraDataService]: Falling back on local connector for " + "AgentConnector", "None");
                    GD = GenericData;

                    if (source.Configs[Name] != null)
                        defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                    GD.ConnectToDatabase(defaultConnectionString);

                    DataManager.DataManager.RegisterPlugin(Name, this);
                }
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

            agent.PrincipalID = agentID;
            agent.AcceptTOS = agentInfo["AcceptTOS"].AsInteger() == 1;
            agent.Flags = (IAgentFlags)agentInfo["AcceptTOS"].AsUInteger();
            agent.MaturityRating = agentInfo["MaturityRating"].AsInteger();
            agent.MaxMaturity = agentInfo["MaxMaturity"].AsInteger();
            agent.Language = agentInfo["Language"].AsString();
            agent.LanguageIsPublic = agentInfo["LanguageIsPublic"].AsInteger() == 1;
			return agent;
		}

        /// <summary>
        /// Updates the language and maturity params of the agent.
        /// Note: we only allow for this on the grid side
        /// </summary>
        /// <param name="agent"></param>
        public void UpdateAgent(IAgentInfo agent)
		{
            Dictionary<string, object> Values = new Dictionary<string, object>();
            Values.Add("AcceptTOS", agent.AcceptTOS ? 1 : 0);
            //Values.Add("Flags", agent.Flags); Don't allow regions to set this
            Values.Add("MaturityRating", agent.MaturityRating);
            Values.Add("MaxMaturity", agent.MaxMaturity);
            Values.Add("Language", agent.Language);
            Values.Add("LanguageIsPublic", agent.LanguageIsPublic ? 1 : 0);
            
            
            List<object> SetValues = new List<object>();
            List<string> SetRows = new List<string>();
            SetRows.Add("Value");

            OSDMap map = Util.DictionaryToOSD(Values);
            SetValues.Add(OSDParser.SerializeLLSDXmlString(map));
            
            
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
            values.Add(OSDParser.SerializeLLSDXmlString(Util.DictionaryToOSD(info.ToKeyValuePairs()))); //Value which is a default Profile
            GD.Insert("userdata", values.ToArray());
		}

        /// <summary>
        /// Checks whether the mac address and viewer are allowed to connect to this grid.
        /// Note: we only allow for this on the grid side
        /// </summary>
        /// <param name="Mac"></param>
        /// <param name="viewer"></param>
        /// <returns></returns>
        public bool CheckMacAndViewer(string Mac, string viewer)
        {
            List<string> found = GD.Query("macaddress", Mac, "macban", "*");
            if (found.Count != 0)
            {
                //Found a mac that matched
                m_log.InfoFormat("[AgentInfoConnector]: Mac '{0}' is in the ban list", Mac);
                return false;
            }

            //Viewer Ban Start
            List<string> clientfound = GD.Query("Client", viewer, "bannedviewers", "*");
            if (clientfound.Count != 0)
            {
                m_log.InfoFormat("[AgentInfoConnector]: Viewer '{0}' is in the ban list", viewer);
                return false;
            }
            return true;
        }
    }
}
