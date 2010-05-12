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
    public class RemoteAgentConnector : IAgentConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public RemoteAgentConnector(string serverURI)
        {
            m_ServerURI = serverURI;
        }

        #region IAgentConnector Members

        public IAgentInfo GetAgent(UUID PrincipalID)
        {
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
                        IAgentInfo agent = null;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                agent = new IAgentInfo((Dictionary<string, object>)f);
                            }
                            else
                                m_log.DebugFormat("[AuroraRemoteProfileConnector]: GetAgent {0} received invalid response type {1}",
                                    PrincipalID, f.GetType());
                        }
                        // Success
                        return agent;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: GetAgent {0} received null response",
                            PrincipalID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public void UpdateAgent(IAgentInfo agent)
        {
            Dictionary<string, object> sendData = agent.ToKeyValuePairs();

            sendData["PRINCIPALID"] = agent.PrincipalID.ToString();
            sendData["METHOD"] = "updateagent";

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
                        if (replyData.ContainsKey("result") && (replyData["result"].ToString().ToLower() == "null"))
                        {
                            m_log.DebugFormat("[AuroraRemoteProfileConnector]: UpdateAgent {0} received null response",
                                agent.PrincipalID);
                        }
                    }

                    else
                    {
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: UpdateAgent {0} received null response",
                            agent.PrincipalID);
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void CreateNewAgent(UUID PrincipalID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["METHOD"] = "createagent";

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
                        if (replyData.ContainsKey("result") && (replyData["result"].ToString().ToLower() == "null"))
                        {
                            m_log.DebugFormat("[AuroraRemoteProfileConnector]: CreateNewAgent {0} received null response",
                                PrincipalID);
                        }
                    }

                    else
                    {
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: CreateNewAgent {0} received null response",
                            PrincipalID);
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        #endregion
    }
}
