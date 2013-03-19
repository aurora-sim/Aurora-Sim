using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Services;
using OpenMetaverse;
using System.Collections.Generic;

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

        public bool RequiresAuthentication
        {
            get { return true; }
        }

        public bool RequiresAdminAuthentication
        {
            get { return false; }
        }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
                                               OSHttpResponse httpResponse, Dictionary<string, object> requestParameters,
                                               ITranslator translator, out string response)
        {
            response = null;
            var vars = new Dictionary<string, object>();

            string error = "";
            UserAccount user = Authenticator.GetAuthentication(httpRequest);

            if (requestParameters.ContainsKey("Submit") &&
                requestParameters["Submit"].ToString() == "SubmitPasswordChange")
            {
                string password = requestParameters["password"].ToString();
                string passwordconf = requestParameters["passwordconf"].ToString();
                response = "Success";
                if (passwordconf != password)
                    response = "Passwords do not match";
                else
                {
                    IAuthenticationService authService =
                        webInterface.Registry.RequestModuleInterface<IAuthenticationService>();
                    if (authService != null)
                        error = authService.SetPassword(user.PrincipalID, "UserAccount", password)
                                    ? ""
                                    : "Failed to set your password, try again later";
                    else
                        response = "No authentication service was available to change your password";
                }
                return null;
            }
            else if (requestParameters.ContainsKey("Submit") &&
                     requestParameters["Submit"].ToString() == "SubmitEmailChange")
            {
                string email = requestParameters["email"].ToString();

                IUserAccountService userService = webInterface.Registry.RequestModuleInterface<IUserAccountService>();
                if (userService != null)
                {
                    user.Email = email;
                    userService.StoreUserAccount(user);
                    response = "Success";
                }
                else
                    response = "No authentication service was available to change your password";
                return null;
            }
            else if (requestParameters.ContainsKey("Submit") &&
                     requestParameters["Submit"].ToString() == "SubmitDeleteUser")
            {
                string username = requestParameters["username"].ToString();
                string password = requestParameters["password"].ToString();

                ILoginService loginService = webInterface.Registry.RequestModuleInterface<ILoginService>();
                if (loginService.VerifyClient(UUID.Zero, username, "UserAccount", password))
                {
                    IUserAccountService userService =
                        webInterface.Registry.RequestModuleInterface<IUserAccountService>();
                    if (userService != null)
                    {
                        userService.DeleteUser(user.PrincipalID, password, true, false);
                        response = "Successfully deleted account.";
                    }
                    else
                        response = "User service unavailable, please try again later";
                }
                else
                    response = "Wrong username or password";
                return null;
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