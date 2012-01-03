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

namespace Aurora.Services.DataService
{
    /*public class SimianProfileConnector : IProfileConnector
    {
        private static readonly ILog MainConsole.Instance =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private List<string> m_ServerURIs = new List<string>();

        public void Initialize(IGenericData unneeded, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("ProfileConnector", "LocalConnector") == "SimianConnector")
            {
                m_ServerURIs = simBase.RequestModuleInterface<IConfigurationService>().FindValueOf("RemoteServerURI");
                DataManager.DataManager.RegisterPlugin(this);
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
                { "UserID", PrincipalID.ToString() }
            };

            OSDMap result = PostUserData(PrincipalID, requestArgs);

            if (result == null)
                return null;

            if (result.ContainsKey("Profile"))
            {
                OSDMap profilemap = (OSDMap)OSDParser.DeserializeJson(result["Profile"].AsString());

                IUserProfileInfo profile = new IUserProfileInfo();
                profile.FromOSD(profilemap);

                return profile;
            }

            return null;
        }

        public bool UpdateUserProfile(IUserProfileInfo Profile)
        {
            NameValueCollection requestArgs = new NameValueCollection
            {
                { "RequestMethod", "AddUserData" },
                { "UserID", Profile.PrincipalID.ToString() },
                { "Profile", OSDParser.SerializeJsonString(Profile.ToOSD()) }
            };

            return PostData(Profile.PrincipalID, requestArgs);
        }

        public void CreateNewProfile(UUID PrincipalID)
        {
            //No user creation from sims
        }

        #endregion

        #region Helpers

        private bool PostData(UUID userID, NameValueCollection nvc)
        {
            foreach (string m_ServerURI in m_ServerURIs)
            {
                OSDMap response = WebUtils.PostToService(m_ServerURI, nvc);

                if (response.ContainsKey("Success"))
                    return response["Success"].AsBoolean();
            }
            return false;
        }

        private OSDMap PostUserData(UUID userID, NameValueCollection nvc)
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
                    MainConsole.Instance.Error("[SIMIAN PROFILES CONNECTOR]: Failed to fetch user data for " + userID + ": " + response["Message"].AsString());
                }
            }

            return null;
        }

        #endregion
    }*/
}