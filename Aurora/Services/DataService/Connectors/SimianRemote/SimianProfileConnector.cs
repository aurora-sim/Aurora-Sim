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
using OpenSim.Server.Base;

namespace Aurora.Services.DataService
{
    public class SimianProfileConnector : IProfileConnector, IAuroraDataPlugin
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public void Initialise(IGenericData unneeded, IConfigSource source, string DefaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("ProfileConnector", "LocalConnector") == "SimianConnector")
            {
                m_ServerURI = source.Configs["AuroraData"].GetString("RemoteServerURI", "");
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
            NameValueCollection requestArgs = new NameValueCollection
            {
                { "RequestMethod", "GetUser" },
                { "AgentID", PrincipalID.ToString() }
            };

            OSDMap result = PostData(PrincipalID, requestArgs);

            if (result == null)
                return null;

            Dictionary<string, object> dresult = Util.OSDToDictionary(result);
            IUserProfileInfo profile = new IUserProfileInfo(dresult);

            return profile;
        }

        public bool UpdateUserProfile(IUserProfileInfo Profile)
        {
            NameValueCollection requestArgs = new NameValueCollection
            {
                { "RequestMethod", "AddUserData" },
                { "AgentID", Profile.PrincipalID.ToString() },
                { "Profile", OSDParser.SerializeJsonString(Util.DictionaryToOSD(Profile.ToKeyValuePairs())) }
            };

            OSDMap result = PostData(Profile.PrincipalID, requestArgs);

            if (result == null)
                return false;

            bool success = result["Success"].AsBoolean();
            return success;
        }

        public void CreateNewProfile(UUID PrincipalID)
        {
            //No user creation from sims
        }

        #endregion

        #region Helpers

        private OSDMap PostData(UUID userID, NameValueCollection nvc)
        {
            OSDMap response = WebUtil.PostToService(m_ServerURI, nvc);
            if (response["Success"].AsBoolean() && response["User"] is OSDMap)
            {
                return (OSDMap)response["User"];
            }
            else
            {
                m_log.Error("[SIMIAN PROFILES]: Failed to fetch user data for " + userID + ": " + response["Message"].AsString());
            }

            return null;
        }

        #endregion
    }
}
