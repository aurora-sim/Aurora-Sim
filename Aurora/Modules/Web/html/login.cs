using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Services;
using OpenMetaverse;
using System;
using System.Collections.Generic;

namespace Aurora.Modules.Web
{
    public class LoginPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "html/login.html"
                           };
            }
        }

        public bool RequiresAuthentication
        {
            get { return false; }
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
            if (requestParameters.ContainsKey("username") && requestParameters.ContainsKey("password"))
            {
                string username = requestParameters["username"].ToString();
                string password = requestParameters["password"].ToString();

                ILoginService loginService = webInterface.Registry.RequestModuleInterface<ILoginService>();
                if (loginService.VerifyClient(UUID.Zero, username, "UserAccount", password))
                {
                    UUID sessionID = UUID.Random();
                    UserAccount account =
                        webInterface.Registry.RequestModuleInterface<IUserAccountService>()
                                    .GetUserAccount(null, username);
                    Authenticator.AddAuthentication(sessionID, account);
                    if (account.UserLevel > 0)
                        Authenticator.AddAdminAuthentication(sessionID, account);
                    httpResponse.AddCookie(new System.Web.HttpCookie("SessionID", sessionID.ToString())
                                               {
                                                   Expires =
                                                       DateTime
                                                       .MinValue,
                                                   Path = ""
                                               });

                    response = "<h3>Successfully logged in, redirecting to main page</h3>" +
                               "<script language=\"javascript\">" +
                               "setTimeout(function() {window.location.href = \"index.html\";}, 0);" +
                               "</script>";
                }
                else
                    response = "<h3>Failed to verify user name and password</h3>";
                return null;
            }

            vars.Add("ErrorMessage", error);
            vars.Add("Login", translator.GetTranslatedString("Login"));
            vars.Add("UserNameText", translator.GetTranslatedString("UserName"));
            vars.Add("PasswordText", translator.GetTranslatedString("Password"));
            vars.Add("ForgotPassword", translator.GetTranslatedString("ForgotPassword"));
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