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
using Aurora.Simulation.Base;

namespace Aurora.Services.DataService
{
    public class RemoteProfileConnector : IProfileConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public void Initialize(IGenericData unneeded, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("ProfileConnector", "LocalConnector") == "RemoteConnector")
            {
                m_ServerURI = simBase.ApplicationRegistry.RequestModuleInterface<IAutoConfigurationService>().FindValueOf("RemoteServerURI", "AuroraData");
                if (m_ServerURI != "")
                    DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IProfileConnector"; }
        }

        public void Dispose()
        {
        }

        #region IProfileConnector Members

        public IUserProfileInfo GetUserProfile(UUID PrincipalID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["PRINCIPALID"] = PrincipalID.ToString();
            sendData["METHOD"] = "getprofile";

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("result"))
                            return null;

                        IUserProfileInfo profile = null;
                        foreach (object f in replyData.Values)
                        {
                            if (f is Dictionary<string, object>)
                            {
                                profile = new IUserProfileInfo();
                                profile.FromKVP((Dictionary<string, object>)f);
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
            Dictionary<string, object> sendData = Profile.ToKeyValuePairs();

            sendData["PRINCIPALID"] = Profile.PrincipalID.ToString();
            sendData["METHOD"] = "updateprofile";

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/auroradata",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

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
            //No user creation from sims
        }

        #endregion
    }
}
