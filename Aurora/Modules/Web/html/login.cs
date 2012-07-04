using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Web
{
    public class LoginPage : IWebInterfacePage
    {
        public string FilePath { get { return "html/login.html"; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, Hashtable query, OSHttpResponse httpResponse, Dictionary<string, object> requestParameters)
        {
            var vars = new Dictionary<string, object>();

            vars.Add("Login", "Login");
            vars.Add("UserNameText", "User Name");
            vars.Add("PasswordText", "Password");
            vars.Add("ForgotPassword", "Forgot Password?");
            vars.Add("Submit", "Submit");

            return vars;
        }
    }
}
