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
    public class RemoteProfileConnector : IProfileConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public RemoteProfileConnector(string serverURI)
        {
            m_ServerURI = serverURI;
        }

        #region IProfileConnector Members

        public Classified ReadClassifiedInfoRow(string classifiedID)
        {
            throw new NotImplementedException();
        }

        public ProfilePickInfo ReadPickInfoRow(string pickID)
        {
            throw new NotImplementedException();
        }

        public void UpdateUserNotes(UUID agentID, UUID targetAgentID, string notes, IUserProfileInfo UPI)
        {
            throw new NotImplementedException();
        }

        public IUserProfileInfo GetUserProfile(UUID PrincipalID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["METHOD"] = "getprofile";

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
                            return null;
                        }

                        
                        Dictionary<string, object>.ValueCollection replyvalues = replyData.Values;
                        IUserProfileInfo profile = null;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                profile = new IUserProfileInfo((Dictionary<string, object>)f);
                            }
                            else
                                m_log.DebugFormat("[AuroraRemoteProfileConnector]: GetProfile {0} received invalid response type {1}",
                                    PrincipalID, f.GetType());
                        }
                        // Success
                        return profile;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: GetProfile {0} received null response",
                            PrincipalID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public bool UpdateUserProfile(IUserProfileInfo Profile)
        {
            throw new NotImplementedException();
        }

        public void CreateNewProfile(UUID UUID, string firstName, string lastName)
        {
            throw new NotImplementedException();
        }

        public void RemoveFromCache(UUID ID)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
