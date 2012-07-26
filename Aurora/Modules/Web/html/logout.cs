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
    public class LogoutPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/logout.html"
                       };
            }
        }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();
			
			vars.Add("Logout", translator.GetTranslatedString("Logout"));
            vars.Add("LoggedOutSuccessfullyText", translator.GetTranslatedString("LoggedOutSuccessfullyText"));
			
            Authenticator.RemoveAuthentication(httpRequest);
			
            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}