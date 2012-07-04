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
    public class LoginAttemptPage : IWebInterfacePage
    {
        public string FilePath { get { return "html/login_attempt.html"; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, Hashtable query, OSHttpResponse httpResponse, Dictionary<string, object> requestParameters)
        {
            var vars = new Dictionary<string, object>();

            string username = query["username"].ToString();
            string password = query["password"].ToString();

            ILoginService loginService = webInterface.Registry.RequestModuleInterface<ILoginService>();
            if (loginService.VerifyClient(UUID.Zero, username, "UserAccount", password, UUID.Zero))
            {
                httpResponse.StatusCode = (int)HttpStatusCode.Redirect;
                httpResponse.AddHeader("Location", "http://129.21.125.225:9000/welcomescreen/");
            }

            return vars;
        }
    }
}
