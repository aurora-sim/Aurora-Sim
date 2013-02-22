using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using OpenSim.Services.LLLoginService;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Nini.Config;
using System.Collections;
using System.IO;

namespace OpenSim.Services.LLLoginService
{
    public class BannedUserLoginModule : ILoginModule
    {
        protected IAuthenticationService m_AuthenticationService;
        protected ILoginService m_LoginService;
        protected bool m_UseTOS = false;
        protected string m_TOSLocation = "";

        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(ILoginService service, IConfigSource config, IRegistryCore registry)
        {
            IConfig loginServerConfig = config.Configs["LoginService"];
            if (loginServerConfig != null)
            {
                m_UseTOS = loginServerConfig.GetBoolean("UseTermsOfServiceOnFirstLogin", false);
                m_TOSLocation = loginServerConfig.GetString("FileNameOfTOS", "");
            }
            m_AuthenticationService = registry.RequestModuleInterface<IAuthenticationService>();
            m_LoginService = service;
        }

        public LoginResponse Login(Hashtable request, UserAccount account, IAgentInfo agentInfo, string authType, string password, out object data)
        {
            IAgentConnector agentData = Aurora.DataManager.DataManager.RequestPlugin<IAgentConnector>();
            data = null;

            if (request == null)
                return null;//If its null, its just a verification request, allow them to see things even if they are banned

            bool tosExists = false;
            string tosAccepted = "";
            if (request.ContainsKey("agree_to_tos"))
            {
                tosExists = true;
                tosAccepted = request["agree_to_tos"].ToString();
            }

            //MAC BANNING START
            string mac = (string)request["mac"];
            if (mac == "")
                return new LLFailedLoginResponse(LoginResponseEnum.Indeterminant, "Bad Viewer Connection", false);

            /*string channel = "Unknown";
            if (request.Contains("channel") && request["channel"] != null)
                channel = request["channel"].ToString();*/

            bool AcceptedNewTOS = false;
            //This gets if the viewer has accepted the new TOS
            if (!agentInfo.AcceptTOS && tosExists)
            {
                if (tosAccepted == "0")
                    AcceptedNewTOS = false;
                else if (tosAccepted == "1")
                    AcceptedNewTOS = true;
                else
                    AcceptedNewTOS = bool.Parse(tosAccepted);

                if (agentInfo.AcceptTOS != AcceptedNewTOS)
                {
                    agentInfo.AcceptTOS = AcceptedNewTOS;
                    agentData.UpdateAgent(agentInfo);
                }
            }
            if (!AcceptedNewTOS && !agentInfo.AcceptTOS && m_UseTOS)
            {
                StreamReader reader = new StreamReader(Path.Combine(Environment.CurrentDirectory, m_TOSLocation));
                string TOS = reader.ReadToEnd();
                reader.Close();
                reader.Dispose();
                return new LLFailedLoginResponse(LoginResponseEnum.ToSNeedsSent, TOS, false);
            }

            if ((agentInfo.Flags & IAgentFlags.PermBan) == IAgentFlags.PermBan)
            {
                MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Login failed for user {0}, reason: user is permanently banned.", account.Name);
                return LLFailedLoginResponse.PermanentBannedProblem;
            }

            if ((agentInfo.Flags & IAgentFlags.TempBan) == IAgentFlags.TempBan)
            {
                bool IsBanned = true;
                string until = "";

                if (agentInfo.OtherAgentInformation.ContainsKey("TemperaryBanInfo"))
                {
                    DateTime bannedTime = agentInfo.OtherAgentInformation["TemperaryBanInfo"].AsDate();
                    until = string.Format(" until {0} {1}", bannedTime.ToShortDateString(), bannedTime.ToLongTimeString());

                    //Check to make sure the time hasn't expired
                    if (bannedTime.Ticks < DateTime.Now.Ticks)
                    {
                        //The banned time is less than now, let the user in.
                        IsBanned = false;
                    }
                }

                if (IsBanned)
                {
                    MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Login failed for user {0}, reason: user is temporarily banned {1}.", account.Name, until);
                    return new LLFailedLoginResponse(LoginResponseEnum.Indeterminant, string.Format("You are blocked from connecting to this service{0}.", until), false);
                }
            }
            return null;
        }
    }
}
