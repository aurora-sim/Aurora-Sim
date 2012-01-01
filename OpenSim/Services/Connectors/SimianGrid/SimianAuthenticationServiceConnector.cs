/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.Connectors.SimianGrid
{
    /// <summary>
    ///   Connects authentication/authorization to the SimianGrid backend
    /// </summary>
    public class SimianAuthenticationServiceConnector : IAuthenticationService, IService
    {
        private string m_serverUrl = String.Empty;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IAuthenticationService Members

        public bool CheckExists(UUID principalID, string authType)
        {
            return false;
        }

        public string Authenticate(UUID principalID, string authType, string password, int lifetime)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetIdentities"},
                                                      {"UserID", principalID.ToString()}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean() && response["Identities"] is OSDArray)
            {
                bool md5hashFound = false;

                OSDArray identities = (OSDArray) response["Identities"];
#if (!ISWIN)
                foreach (OSD osd in identities)
                {
                    OSDMap identity = osd as OSDMap;
                    if (identity != null)
                    {
                        if (identity["Type"].AsString() == "md5hash")
                        {
                            string authorizeResult;
                            if (CheckPassword(principalID, password, identity["Credential"].AsString(), out authorizeResult))
                                return authorizeResult;

                            md5hashFound = true;
                            break;
                        }
                    }
                }
#else
                foreach (OSDMap identity in identities.OfType<OSDMap>().Where(identity => identity["Type"].AsString() == "md5hash"))
                {
                    string authorizeResult;
                    if (CheckPassword(principalID, password, identity["Credential"].AsString(),
                                      out authorizeResult))
                        return authorizeResult;

                    md5hashFound = true;
                    break;
                }
#endif

                if (!md5hashFound)
                    MainConsole.Instance.Warn("[SIMIAN AUTH CONNECTOR]: Authentication failed for " + principalID +
                               ", no md5hash identity found");
            }
            else
            {
                MainConsole.Instance.Warn("[SIMIAN AUTH CONNECTOR]: Failed to retrieve identities for " + principalID + ": " +
                           response["Message"].AsString());
            }

            return String.Empty;
        }

        public bool Verify(UUID principalID, string authType, string token, int lifetime)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetSession"},
                                                      {"SessionID", token}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean())
            {
                return true;
            }
            else
            {
                MainConsole.Instance.Warn("[SIMIAN AUTH CONNECTOR]: Could not verify session for " + principalID + ": " +
                           response["Message"].AsString());
            }

            return false;
        }

        public bool Release(UUID principalID, string token, string authType)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "RemoveSession"},
                                                      {"UserID", principalID.ToString()}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean())
            {
                return true;
            }
            else
            {
                MainConsole.Instance.Warn("[SIMIAN AUTH CONNECTOR]: Failed to remove session for " + principalID + ": " +
                           response["Message"].AsString());
            }

            return false;
        }

        public bool SetPassword(UUID principalID, string authType, string passwd)
        {
            // Fetch the user name first
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetUser"},
                                                      {"UserID", principalID.ToString()}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean() && response["User"] is OSDMap)
            {
                OSDMap userMap = (OSDMap) response["User"];
                string identifier = userMap["Name"].AsString();

                if (!String.IsNullOrEmpty(identifier))
                {
                    // Add/update the md5hash identity
                    // TODO: Support salts when AddIdentity does
                    // TODO: Create an a1hash too for WebDAV logins
                    requestArgs = new NameValueCollection
                                      {
                                          {"RequestMethod", "AddIdentity"},
                                          {"Identifier", identifier},
                                          {"Credential", "$1$" + Utils.MD5String(passwd)},
                                          {"Type", "md5hash"},
                                          {"UserID", principalID.ToString()}
                                      };

                    response = WebUtils.PostToService(m_serverUrl, requestArgs);
                    bool success = response["Success"].AsBoolean();

                    if (!success)
                        MainConsole.Instance.WarnFormat("[SIMIAN AUTH CONNECTOR]: Failed to set password for {0} ({1})", identifier,
                                         principalID);

                    return success;
                }
            }
            else
            {
                MainConsole.Instance.Warn("[SIMIAN AUTH CONNECTOR]: Failed to retrieve identities for " + principalID + ": " +
                           response["Message"].AsString());
            }

            return false;
        }

        public bool SetPlainPassword(UUID principalID, string authType, string passwd)
        {
            // Fetch the user name first
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetUser"},
                                                      {"UserID", principalID.ToString()}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean() && response["User"] is OSDMap)
            {
                OSDMap userMap = (OSDMap) response["User"];
                string identifier = userMap["Name"].AsString();

                if (!String.IsNullOrEmpty(identifier))
                {
                    // Add/update the md5hash identity
                    // TODO: Support salts when AddIdentity does
                    // TODO: Create an a1hash too for WebDAV logins
                    requestArgs = new NameValueCollection
                                      {
                                          {"RequestMethod", "AddIdentity"},
                                          {"Identifier", identifier},
                                          {"Credential", "$1$" + Utils.MD5String(Utils.MD5String(passwd))},
                                          {"Type", "md5hash"},
                                          {"UserID", principalID.ToString()}
                                      };

                    response = WebUtils.PostToService(m_serverUrl, requestArgs);
                    bool success = response["Success"].AsBoolean();

                    if (!success)
                        MainConsole.Instance.WarnFormat("[SIMIAN AUTH CONNECTOR]: Failed to set password for {0} ({1})", identifier,
                                         principalID);

                    return success;
                }
            }
            else
            {
                MainConsole.Instance.Warn("[SIMIAN AUTH CONNECTOR]: Failed to retrieve identities for " + principalID + ": " +
                           response["Message"].AsString());
            }

            return false;
        }

        public bool SetPasswordHashed(UUID principalID, string authType, string passwd)
        {
            return false;
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("AuthenticationHandler", "") != Name)
                return;

            CommonInit(config);
            registry.RegisterModuleInterface<IAuthenticationService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion

        private void CommonInit(IConfigSource source)
        {
            IConfig gridConfig = source.Configs["AuthenticationService"];
            if (gridConfig != null)
            {
                string serviceUrl = gridConfig.GetString("AuthenticationServerURI");
                if (!String.IsNullOrEmpty(serviceUrl))
                {
                    if (!serviceUrl.EndsWith("/") && !serviceUrl.EndsWith("="))
                        serviceUrl = serviceUrl + '/';
                    m_serverUrl = serviceUrl;
                }
            }

            if (String.IsNullOrEmpty(m_serverUrl))
                MainConsole.Instance.Info("[SIMIAN AUTH CONNECTOR]: No AuthenticationServerURI specified, disabling connector");
        }

        private bool CheckPassword(UUID userID, string password, string simianGridCredential, out string authorizeResult)
        {
            if (simianGridCredential.Contains(":"))
            {
                // Salted version
                int idx = simianGridCredential.IndexOf(':');
                string finalhash = simianGridCredential.Substring(0, idx);
                string salt = simianGridCredential.Substring(idx + 1);

                if (finalhash == Utils.MD5String(password + ":" + salt))
                {
                    authorizeResult = Authorize(userID);
                    return true;
                }
                else
                {
                    MainConsole.Instance.Warn("[SIMIAN AUTH CONNECTOR]: Authentication failed for " + userID +
                               " using md5hash " + Utils.MD5String(password) + ":" + salt);
                }
            }
            else
            {
                // Unsalted version
                if (password == simianGridCredential ||
                    "$1$" + password == simianGridCredential ||
                    "$1$" + Utils.MD5String(password) == simianGridCredential ||
                    Utils.MD5String(password) == simianGridCredential ||
                    "$1$" + Utils.MD5String(password + ":") == simianGridCredential)
                {
                    authorizeResult = Authorize(userID);
                    return true;
                }
                else
                {
                    MainConsole.Instance.Warn("[SIMIAN AUTH CONNECTOR]: Authentication failed for " + userID +
                               " using md5hash $1$" + Utils.MD5String(password));
                }
            }

            authorizeResult = null;
            return false;
        }

        private string Authorize(UUID userID)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "AddSession"},
                                                      {"UserID", userID.ToString()}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean())
                return response["SessionID"].AsUUID().ToString();
            else
                return String.Empty;
        }
    }
}