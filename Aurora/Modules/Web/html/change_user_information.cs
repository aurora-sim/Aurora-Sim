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
    public class ChangeUserInformationPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/change_user_information.html"
                       };
            }
        }

        public bool RequiresAuthentication { get { return true; } }
        public bool RequiresAdminAuthentication { get { return false; } }
        
        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest, 
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();

            string error = "";
            UUID user = Authenticator.GetAuthentication(httpRequest);

            if (requestParameters.ContainsKey("Submit") &&
                requestParameters["Submit"].ToString() == "SubmitPasswordChange")
            {
                string password = requestParameters["password"].ToString();
                string passwordconf = requestParameters["passwordconf"].ToString();

                if (passwordconf != password)
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

                IUserAccountService userService = webInterface.Registry.RequestModuleInterface<IUserAccountService>();
                if (userService != null)
                {
                    UserAccount account = userService.GetUserAccount(null, user);
                    account.Email = email;
                    userService.StoreUserAccount(account);
                }
                else
                    error = "No authentication service was available to change your password";
            }
            else if (requestParameters.ContainsKey("Submit") &&
                requestParameters["Submit"].ToString() == "SubmitDeleteUser")
            {
                string username = requestParameters["username"].ToString();
                string password = requestParameters["password"].ToString();

                ILoginService loginService = webInterface.Registry.RequestModuleInterface<ILoginService>();
                if (loginService.VerifyClient(UUID.Zero, username, "UserAccount", password))
                {
                    IUserAccountService userService = webInterface.Registry.RequestModuleInterface<IUserAccountService>();
                    if (userService != null)
                    {
                        userService.DeleteUser(user, password, true, false);
                        error = "Successfully deleted account.";
                    }
                    else
                        error = "User service unavailable, please try again later";
                }
                else
                    error = "Wrong username or password";
            }
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

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}
