/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using OpenMetaverse;
using log4net;
using Nini.Config;
using System.Reflection;
using OpenSim.Services.Base;
using OpenSim.Data;
using OpenSim.Framework;

namespace OpenSim.Services.AuthenticationService
{
    // Generic Authentication service used for identifying
    // and authenticating principals.
    // Principals may be clients acting on users' behalf,
    // or any other components that need 
    // verifiable identification.
    //
    public class AuthenticationServiceBase : ServiceBase
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);
 
        protected IAuthenticationData m_Database;
        protected bool m_authenticateUsers = true;

        public bool CheckExists(UUID principalID)
        {
            return  m_Database.Get(principalID) != null;
        }

        public bool Verify(UUID principalID, string token, int lifetime)
        {
            return m_Database.CheckToken(principalID, token, lifetime);
        }

        public virtual bool Release(UUID principalID, string token)
        {
            return m_Database.CheckToken(principalID, token, 0);
        }

        public virtual bool SetPassword(UUID principalID, string password)
        {
            string passwordSalt = Util.Md5Hash(UUID.Random().ToString());
            string md5PasswdHash = Util.Md5Hash(Util.Md5Hash(password) + ":" + passwordSalt);

            AuthenticationData auth = m_Database.Get(principalID);
            if (auth == null)
            {
                auth = new AuthenticationData();
                auth.PrincipalID = principalID;
                auth.Data = new System.Collections.Generic.Dictionary<string, object>();
                auth.Data["accountType"] = "UserAccount";
                auth.Data["webLoginKey"] = UUID.Zero.ToString();
            }
            auth.Data["passwordHash"] = md5PasswdHash;
            auth.Data["passwordSalt"] = passwordSalt;
            if (!m_Database.Store(auth))
            {
                m_log.DebugFormat("[AUTHENTICATION DB]: Failed to store authentication data");
                return false;
            }

            m_log.InfoFormat("[AUTHENTICATION DB]: Set password for principalID {0}", principalID);
            return true;
        }
        
        protected string GetToken(UUID principalID, int lifetime)
        {
            UUID token = UUID.Random();

            if (m_Database.SetToken(principalID, token.ToString(), lifetime))
                return token.ToString();

            return String.Empty;
        }

        public virtual bool SetPasswordHashed(UUID principalID, string Hashedpassword)
        {
            string passwordSalt = Util.Md5Hash(UUID.Random().ToString());
            string md5PasswdHash = Util.Md5Hash(Hashedpassword + ":" + passwordSalt);

            AuthenticationData auth = m_Database.Get(principalID);
            if (auth == null)
            {
                auth = new AuthenticationData();
                auth.PrincipalID = principalID;
                auth.Data = new System.Collections.Generic.Dictionary<string, object>();
                auth.Data["accountType"] = "UserAccount";
                auth.Data["webLoginKey"] = UUID.Zero.ToString();
            }
            auth.Data["passwordHash"] = md5PasswdHash;
            auth.Data["passwordSalt"] = passwordSalt;
            if (!m_Database.Store(auth))
            {
                m_log.DebugFormat("[AUTHENTICATION DB]: Failed to store authentication data");
                return false;
            }

            m_log.InfoFormat("[AUTHENTICATION DB]: Set password for principalID {0}", principalID);
            return true;
        }
    }
}
