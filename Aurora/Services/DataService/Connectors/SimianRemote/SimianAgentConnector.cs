using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using log4net;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using Aurora.Simulation.Base;

namespace Aurora.Services.DataService
{
    public class SimianAgentConnector : IAgentConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private List<string> m_ServerURIs = new List<string>();

        public void Initialize(IGenericData unneeded, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("AgentConnector", "LocalConnector") == "SimianConnector")
            {
                m_ServerURIs = simBase.ApplicationRegistry.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
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

        #region IAgentConnector Members

        public IAgentInfo GetAgent(UUID PrincipalID)
        {
            NameValueCollection requestArgs = new NameValueCollection
            {
                { "RequestMethod", "GetUser" },
                { "UserID", PrincipalID.ToString() }
            };

            OSDMap result = PostData(PrincipalID, requestArgs);

            if (result == null)
                return null;

            if (result.ContainsKey("AgentInfo"))
            {
                OSDMap agentmap = (OSDMap)OSDParser.DeserializeJson(result["AgentInfo"].AsString());

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
                { "RequestMethod", "AddUserData" },
                { "UserID", agent.PrincipalID.ToString() },
                { "AgentInfo", OSDParser.SerializeJsonString(agent.ToOSD()) }
            };

            PostData(agent.PrincipalID, requestArgs);
        }

        public void CreateNewAgent(UUID PrincipalID)
        {
            IAgentInfo info = new IAgentInfo();
            info.PrincipalID = PrincipalID;

            NameValueCollection requestArgs = new NameValueCollection
            {
                { "RequestMethod", "AddUserData" },
                { "UserID", info.PrincipalID.ToString() },
                { "AgentInfo", OSDParser.SerializeJsonString(info.ToOSD()) }
            };

            PostData(info.PrincipalID, requestArgs);
        }

        public bool CheckMacAndViewer(string Mac, string viewer)
        {
            //Only local! You should not be calling this!! This method is only called 
            // from LLLoginHandlers.
            return false;
        }

        #endregion

        #region Helpers

        private OSDMap PostData(UUID userID, NameValueCollection nvc)
        {
            foreach (string m_ServerURI in m_ServerURIs)
            {
                OSDMap response = WebUtils.PostToService(m_ServerURI, nvc);
                if (response["Success"].AsBoolean() && response["User"] is OSDMap)
                {
                    return (OSDMap)response["User"];
                }
                else
                {
                    m_log.Error("[SIMIAN AGENTS CONNECTOR]: Failed to fetch agent info data for " + userID + ": " + response["Message"].AsString());
                }
            }

            return null;
        }

        #endregion
    }
}
