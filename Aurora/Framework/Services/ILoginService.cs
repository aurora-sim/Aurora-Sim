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

using System.Collections;
using System.Net;
using Aurora.Framework.Modules;
using Aurora.Framework.Services.ClassHelpers.Profile;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework.Services
{
    public abstract class LoginResponse
    {
        public abstract Hashtable ToHashtable();
    }

    public class LoginResponseEnum
    {
        public static string PasswordIncorrect = "key"; //Password is wrong
        public static string InternalError = "Internal Error"; //Something inside went wrong
        public static string MessagePopup = "critical"; //Makes a message pop up in the viewer
        public static string ToSNeedsSent = "tos"; //Pops up the ToS acceptance box
        public static string Update = "update"; //Informs the client that they must update the viewer to login
        public static string OptionalUpdate = "optional"; //Informs the client that they have an optional update

        public static string PresenceIssue = "presence";
        //Used by opensim to tell the viewer that the agent is already logged in

        public static string OK = "true"; //Login went fine
        public static string Indeterminant = "indeterminate"; //Unknown exactly what this does
        public static string Redirect = "redirect"; //Redirect! TBA!
    }

    public class LLFailedLoginResponse : LoginResponse
    {
        public static LLFailedLoginResponse AuthenticationProblem;
        public static LLFailedLoginResponse AccountProblem;
        public static LLFailedLoginResponse GridProblem;
        public static LLFailedLoginResponse InventoryProblem;
        public static LLFailedLoginResponse DeadRegionProblem;
        public static LLFailedLoginResponse LoginBlockedProblem;
        public static LLFailedLoginResponse AlreadyLoggedInProblem;
        public static LLFailedLoginResponse InternalError;
        public static LLFailedLoginResponse PermanentBannedProblem;
        protected string m_key;
        protected bool m_login;
        protected string m_value;

        public string Value { get { return m_value; } }

        static LLFailedLoginResponse()
        {
            AuthenticationProblem = new LLFailedLoginResponse(LoginResponseEnum.PasswordIncorrect,
                                                              "Could not authenticate your avatar. Please check your username and password, and check the grid if problems persist.",
                                                              false);
            AccountProblem = new LLFailedLoginResponse(LoginResponseEnum.PasswordIncorrect,
                                                       "Could not find an account for your avatar. Please check that your username is correct or make a new account.",
                                                       false);
            PermanentBannedProblem = new LLFailedLoginResponse(LoginResponseEnum.PasswordIncorrect,
                                                               "You have been blocked from using this service.",
                                                               false);
            GridProblem = new LLFailedLoginResponse(LoginResponseEnum.InternalError,
                                                    "Error connecting to the desired location. Try connecting to another region.",
                                                    false);
            InventoryProblem = new LLFailedLoginResponse(LoginResponseEnum.InternalError,
                                                         "The inventory service is not responding.  Please notify your login region operator.",
                                                         false);
            DeadRegionProblem = new LLFailedLoginResponse(LoginResponseEnum.InternalError,
                                                          "The region you are attempting to log into is not responding. Please select another region and try again.",
                                                          false);
            LoginBlockedProblem = new LLFailedLoginResponse(LoginResponseEnum.InternalError,
                                                            "Logins are currently restricted. Please try again later.",
                                                            false);
            AlreadyLoggedInProblem = new LLFailedLoginResponse(LoginResponseEnum.PresenceIssue,
                                                               "You appear to be already logged in. " +
                                                               "If this is not the case please wait for your session to timeout. " +
                                                               "If this takes longer than a few minutes please contact the grid owner. " +
                                                               "Please wait 5 minutes if you are going to connect to a region nearby to the region you were at previously.",
                                                               false);
            InternalError = new LLFailedLoginResponse(LoginResponseEnum.InternalError, "Error generating Login Response",
                                                      false);
        }

        public LLFailedLoginResponse(string key, string value, bool login)
        {
            m_key = key;
            m_value = value;
            m_login = login;
        }

        public override Hashtable ToHashtable()
        {
            Hashtable loginError = new Hashtable();
            loginError["reason"] = m_key;
            loginError["message"] = m_value;
            loginError["login"] = m_login.ToString().ToLower();
            return loginError;
        }
    }

    public interface ILoginModule
    {
        string Name { get; }
        void Initialize(ILoginService service, IConfigSource config, IRegistryCore registry);

        LoginResponse Login(Hashtable request, UserAccount acc, IAgentInfo agentInfo, string authType, string password,
                            out object data);
    }

    public interface ILoginService
    {
        int MinLoginLevel { get; }

        bool VerifyClient(UUID AgentID, string name, string authType, string passwd);

        LoginResponse Login(UUID AgentID, string Name, string authType, string passwd, string startLocation,
                            string clientVersion, string channel, string mac, string id0, IPEndPoint clientIP,
                            Hashtable requestData);

        Hashtable SetLevel(string firstName, string lastName, string passwd, int level, IPEndPoint clientIP);
    }
}