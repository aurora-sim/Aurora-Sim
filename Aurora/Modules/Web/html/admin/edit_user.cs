using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Web
{
    public class EditUserPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/admin/edit_user.html"
                       };
            }
        }

        public bool RequiresAuthentication { get { return true; } }
        public bool RequiresAdminAuthentication { get { return true; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();

            string error = "";
            UUID user = httpRequest.Query.ContainsKey("userid") ? UUID.Parse(httpRequest.Query["userid"].ToString()) :
                 UUID.Parse(requestParameters["userid"].ToString());

            IUserAccountService userService = webInterface.Registry.RequestModuleInterface<IUserAccountService>();
            var agentService = Aurora.DataManager.DataManager.RequestPlugin<IAgentConnector>();
            UserAccount account = userService.GetUserAccount(null, user);
            IAgentInfo agent = agentService.GetAgent(user);
            if (requestParameters.ContainsKey("Submit") &&
                requestParameters["Submit"].ToString() == "SubmitPasswordChange")
            {
                string password = requestParameters["password"].ToString();
                string passwordconf = requestParameters["passwordconf"].ToString();

                if (password != passwordconf)
                    error = "Passwords do not match";
                else
                {
                    IAuthenticationService authService = webInterface.Registry.RequestModuleInterface<IAuthenticationService>();
                    if (authService != null)
                        error = authService.SetPassword(user, "UserAccount", password) ? "" : "Failed to set your password, try again later";
                    else
                        error = "No authentication service was available to change your password";
                }
            }
            else if (requestParameters.ContainsKey("Submit") &&
                requestParameters["Submit"].ToString() == "SubmitEmailChange")
            {
                string email = requestParameters["email"].ToString();

                if (userService != null)
                {
                    account.Email = email;
                    userService.StoreUserAccount(account);
                }
                else
                    error = "No authentication service was available to change your password";
            }
            else if (requestParameters.ContainsKey("Submit") &&
                requestParameters["Submit"].ToString() == "SubmitDeleteUser")
            {
                userService.DeleteUser(user, "", false, false);
            }
            if (requestParameters.ContainsKey("Submit") &&
                requestParameters["Submit"].ToString() == "SubmitTempBanUser")
            {
                int timeDays = int.Parse(requestParameters["TimeDays"].ToString());
                int timeHours = int.Parse(requestParameters["TimeHours"].ToString());
                int timeMinutes = int.Parse(requestParameters["TimeMinutes"].ToString());
                agent.Flags |= IAgentFlags.TempBan;
                DateTime until = DateTime.Now.AddDays(timeDays).AddHours(timeHours).AddMinutes(timeMinutes);
                agent.OtherAgentInformation["TemperaryBanInfo"] = until;
                agentService.UpdateAgent(agent);
            }
            if (requestParameters.ContainsKey("Submit") &&
                requestParameters["Submit"].ToString() == "SubmitBanUser")
            {
                agent.Flags |= IAgentFlags.PermBan;
                agentService.UpdateAgent(agent);
            }
            if (requestParameters.ContainsKey("Submit") &&
                requestParameters["Submit"].ToString() == "SubmitUnbanUser")
            {
                agent.Flags &= ~IAgentFlags.TempBan;
                agent.Flags &= ~IAgentFlags.PermBan;
                agent.OtherAgentInformation.Remove("TemperaryBanInfo");
                agentService.UpdateAgent(agent);
            }
            if (requestParameters.ContainsKey("Submit") &&
                requestParameters["Submit"].ToString() == "SubmitLoginAsUser")
            {
                Authenticator.ChangeAuthentication(httpRequest, account);
                webInterface.Redirect(httpResponse, "/", filename);
                return vars;
            }
            if (requestParameters.ContainsKey("Submit") &&
                requestParameters["Submit"].ToString() == "SubmitKickUser")
            {
                string message = requestParameters["KickMessage"].ToString();
                IGridWideMessageModule messageModule = webInterface.Registry.RequestModuleInterface<IGridWideMessageModule>();
                if (messageModule != null)
                    messageModule.KickUser(account.PrincipalID, message);
            }
            string bannedUntil = "";
            bool userBanned = agent == null ? false : ((agent.Flags & IAgentFlags.PermBan) == IAgentFlags.PermBan || (agent.Flags & IAgentFlags.TempBan) == IAgentFlags.TempBan);
            if (userBanned)
            {
                if ((agent.Flags & IAgentFlags.TempBan) == IAgentFlags.TempBan && agent.OtherAgentInformation["TemperaryBanInfo"].AsDate() < DateTime.Now)
                {
                    userBanned = false;
                    agent.Flags &= ~IAgentFlags.TempBan;
                    agent.Flags &= ~IAgentFlags.PermBan;
                    agent.OtherAgentInformation.Remove("TemperaryBanInfo");
                    agentService.UpdateAgent(agent);
                }
                else
                {
                    DateTime bannedTime = agent.OtherAgentInformation["TemperaryBanInfo"].AsDate();
                    bannedUntil = string.Format("{0} {1}", bannedTime.ToShortDateString(), bannedTime.ToLongTimeString());
                }
            }
            vars.Add("NotUserBanned", !userBanned);
            vars.Add("UserBanned", userBanned);
            vars.Add("BannedUntil", bannedUntil);
            vars.Add("EmailValue", account.Email);
            vars.Add("UserID", account.PrincipalID);
            vars.Add("UserName", account.Name);
            vars.Add("ErrorMessage", error);
            vars.Add("ChangeUserInformationText", translator.GetTranslatedString("ChangeUserInformationText"));
            vars.Add("ChangePasswordText", translator.GetTranslatedString("ChangePasswordText"));
            vars.Add("NewPasswordText", translator.GetTranslatedString("NewPasswordText"));
            vars.Add("NewPasswordConfirmationText", translator.GetTranslatedString("NewPasswordConfirmationText"));
            vars.Add("ChangeEmailText", translator.GetTranslatedString("ChangeEmailText"));
            vars.Add("NewEmailText", translator.GetTranslatedString("NewEmailText"));
            vars.Add("UserNameText", translator.GetTranslatedString("UserNameText"));
            vars.Add("PasswordText", translator.GetTranslatedString("PasswordText"));
            vars.Add("DeleteUserText", translator.GetTranslatedString("DeleteUserText"));
            vars.Add("DeleteText", translator.GetTranslatedString("DeleteText"));
            vars.Add("DeleteUserInfoText", translator.GetTranslatedString("DeleteUserInfoText"));
            vars.Add("Submit", translator.GetTranslatedString("Submit"));
            vars.Add("Login", translator.GetTranslatedString("Login"));

            vars.Add("AdminLoginInAsUserText", translator.GetTranslatedString("AdminLoginInAsUserText"));
			vars.Add("AdminLoginInAsUserInfoText", translator.GetTranslatedString("AdminLoginInAsUserInfoText"));
            vars.Add("AdminDeleteUserText", translator.GetTranslatedString("AdminDeleteUserText"));
            vars.Add("AdminDeleteUserInfoText", translator.GetTranslatedString("AdminDeleteUserInfoText"));
            vars.Add("AdminUnbanUserText", translator.GetTranslatedString("AdminUnbanUserText"));
            vars.Add("AdminTempBanUserText", translator.GetTranslatedString("AdminTempBanUserText"));
            vars.Add("AdminTempBanUserInfoText", translator.GetTranslatedString("AdminTempBanUserInfoText"));
            vars.Add("AdminBanUserText", translator.GetTranslatedString("AdminBanUserText"));
            vars.Add("AdminBanUserInfoText", translator.GetTranslatedString("AdminBanUserInfoText"));
            vars.Add("BanText", translator.GetTranslatedString("BanText"));
            vars.Add("UnbanText", translator.GetTranslatedString("UnbanText"));
            vars.Add("TimeUntilUnbannedText", translator.GetTranslatedString("TimeUntilUnbannedText"));
            vars.Add("EdittingText", translator.GetTranslatedString("EdittingText"));
            vars.Add("BannedUntilText", translator.GetTranslatedString("BannedUntilText"));

            vars.Add("KickAUserText", translator.GetTranslatedString("KickAUserText"));
            vars.Add("KickMessageText", translator.GetTranslatedString("KickMessageText"));
            vars.Add("KickUserText", translator.GetTranslatedString("KickUserText"));

            List<Dictionary<string, object>> daysArgs = new List<Dictionary<string, object>>();
            for (int i = 0; i <= 100; i++)
                daysArgs.Add(new Dictionary<string, object> { { "Value", i } });

            List<Dictionary<string, object>> hoursArgs = new List<Dictionary<string, object>>();
            for (int i = 0; i <= 23; i++)
                hoursArgs.Add(new Dictionary<string, object> { { "Value", i } });

            List<Dictionary<string, object>> minutesArgs = new List<Dictionary<string, object>>();
            for (int i = 0; i <= 59; i++)
                minutesArgs.Add(new Dictionary<string, object> { { "Value", i } });

            vars.Add("Days", daysArgs);
            vars.Add("Hours", hoursArgs);
            vars.Add("Minutes", minutesArgs);
            vars.Add("DaysText", translator.GetTranslatedString("DaysText"));
            vars.Add("HoursText", translator.GetTranslatedString("HoursText"));
            vars.Add("MinutesText", translator.GetTranslatedString("MinutesText"));

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}