using System;
using System.Collections;
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
using OpenSim.Server.Base;

namespace Aurora.Services.DataService
{
    public class RemoteAgentConnector : IAgentConnector, IAuroraDataPlugin
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";
        private ExpiringCache<UUID, IAgentInfo> m_cache = new ExpiringCache<UUID, IAgentInfo>();

        public void Initialise(IGenericData unneeded, IConfigSource source, string DefaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AgentConnector", "LocalConnector") == "RemoteConnector")
            {
                m_ServerURI = source.Configs["AuroraData"].GetString("RemoteServerURI", "");
                if (m_ServerURI != "")
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
            IAgentInfo agent;
            if (!m_cache.TryGetValue(PrincipalID, out agent))
                return agent;
                        
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["METHOD"] = "getagent";

            string reqString = ServerUtils.BuildQueryString(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("result"))
                            return null;

                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                agent = new IAgentInfo((Dictionary<string, object>)f);
                                m_cache.AddOrUpdate(PrincipalID, agent, new TimeSpan(0,30,0));
                            }
                            else
                                m_log.DebugFormat("[AuroraRemoteAgentConnector]: GetAgent {0} received invalid response type {1}",
                                    PrincipalID, f.GetType());
                        }
                        // Success
                        return agent;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteAgentConnector]: GetAgent {0} received null response",
                            PrincipalID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteAgentConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public void UpdateAgent(IAgentInfo agent)
        {
            //No creating from sims!
        }

        public void CreateNewAgent(UUID PrincipalID)
        {
            //No creating from sims!
        }

        public bool CheckMacAndViewer(string Mac, string viewer)
        {
            //Only local! You should not be calling this!! This method is only called 
            // from LLLoginHandlers.
            return false;
        }

        #endregion
    }
}
