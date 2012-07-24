using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework.Servers.HttpServer;

namespace Aurora.Modules.Web
{
    public class WelcomeScreenMain : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/welcomescreen/index.html"
                       };
            }
        }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
			var vars = new Dictionary<string, object>();
			
            // Style Switcher
            vars.Add("styles1", translator.GetTranslatedString("styles1"));
            vars.Add("styles2", translator.GetTranslatedString("styles2"));
            vars.Add("styles3", translator.GetTranslatedString("styles3"));
            vars.Add("styles4", translator.GetTranslatedString("styles4"));
            vars.Add("styles5", translator.GetTranslatedString("styles5"));

			vars.Add("StyleSwitcherStylesText", translator.GetTranslatedString("StyleSwitcherStylesText"));
			vars.Add("StyleSwitcherLanguagesText", translator.GetTranslatedString("StyleSwitcherLanguagesText"));
			vars.Add("StyleSwitcherChoiceText", translator.GetTranslatedString("StyleSwitcherChoiceText"));

            // Language Switcher
            vars.Add("en", translator.GetTranslatedString("en"));
            vars.Add("fr", translator.GetTranslatedString("fr"));
            vars.Add("de", translator.GetTranslatedString("de"));
            vars.Add("it", translator.GetTranslatedString("it"));
            vars.Add("es", translator.GetTranslatedString("es"));
			
			return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}
