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
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["CLASSIFIEDID"] = classifiedID.ToString();
            sendData["METHOD"] = "getclassified";

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
                        Classified classified = null;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                classified = new Classified((Dictionary<string, object>)f);
                            }
                            else
                                m_log.DebugFormat("[AuroraRemoteProfileConnector]: GetClassified {0} received invalid response type {1}",
                                    classifiedID, f.GetType());
                        }
                        // Success
                        return classified;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: GetClassified {0} received null response",
                            classifiedID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public ProfilePickInfo ReadPickInfoRow(string pickID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PICKID"] = pickID.ToString();
            sendData["METHOD"] = "getpick";

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
                        ProfilePickInfo pick = null;
                        foreach (object f in replyvalues)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                pick = new ProfilePickInfo((Dictionary<string, object>)f);
                            }
                            else
                                m_log.DebugFormat("[AuroraRemoteProfileConnector]: GetPick {0} received invalid response type {1}",
                                    pickID, f.GetType());
                        }
                        // Success
                        return pick;
                    }

                    else
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: GetPick {0} received null response",
                            pickID);

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }

            return null;
        }

        public void UpdateUserNotes(UUID agentID, UUID targetAgentID, string notes, IUserProfileInfo UPI)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = agentID.ToString();
            sendData["TARGETID"] = targetAgentID.ToString();
            sendData["NOTES"] = notes.ToString();
            sendData["PROFILE"] = UPI.ToKeyValuePairs();
            sendData["METHOD"] = "updateusernotes";

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
                            m_log.DebugFormat("[AuroraRemoteProfileConnector]: UpdateUserNotes {0} received null response",
                                UPI.PrincipalID);
                        }
                    }

                    else
                    {
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: UpdateUserNotes {0} received null response",
                            UPI.PrincipalID);
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }
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
                        if (!replyData.ContainsKey("result"))
                            return null;

                        
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
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = Profile.PrincipalID.ToString();
            sendData["PROFILE"] = Profile.ToKeyValuePairs();
            sendData["METHOD"] = "updateprofile";

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
                            m_log.DebugFormat("[AuroraRemoteProfileConnector]: UpdateProfile {0} received null response",
                                Profile.PrincipalID);
                            return false;
                        }
                    }

                    else
                    {
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: UpdateProfile {0} received null response",
                            Profile.PrincipalID);
                        return false;
                    }

                }
                return true;
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }
            return false;
        }

        public void CreateNewProfile(UUID PrincipalID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["METHOD"] = "createprofile";

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
                            m_log.DebugFormat("[AuroraRemoteProfileConnector]: CreateNewProfile {0} received null response",
                                PrincipalID);
                        }
                    }

                    else
                    {
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: CreateNewProfile {0} received null response",
                            PrincipalID);
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void RemoveFromCache(UUID PrincipalID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["METHOD"] = "removefromcache";

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
                            m_log.DebugFormat("[AuroraRemoteProfileConnector]: RemoveFromCache {0} received null response",
                                PrincipalID);
                        }
                    }

                    else
                    {
                        m_log.DebugFormat("[AuroraRemoteProfileConnector]: RemoveFromCache {0} received null response",
                            PrincipalID);
                    }

                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraRemoteProfileConnector]: Exception when contacting server: {0}", e.Message);
            }
        }

        public void AddClassified(Classified classified)
        {
            throw new NotImplementedException();
        }

        public void DeleteClassified(UUID ID, UUID agentID)
        {
            throw new NotImplementedException();
        }

        public void AddPick(ProfilePickInfo pick)
        {
            throw new NotImplementedException();
        }

        public void UpdatePick(ProfilePickInfo pick)
        {
            throw new NotImplementedException();
        }

        public void DeletePick(UUID ID, UUID agentID)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
