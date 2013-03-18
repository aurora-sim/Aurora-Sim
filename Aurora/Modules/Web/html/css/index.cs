using Aurora.Framework.Servers.HttpServer;
using System.Collections.Generic;

namespace Aurora.Modules.Web
{
    public class CSSLanguageSetterPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/css/"
                       };
            }
        }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator, out string response)
        {
            response = null;
            var vars = new Dictionary<string, object>();

            vars.Add("DisplayLG1", "display: none;");
            vars.Add("DisplayLG2", "display: none;");
            vars.Add("DisplayLG3", "display: none;");
            vars.Add("DisplayLG4", "display: none;");
            vars.Add("DisplayLG5", "display: none;");
            if (translator.LanguageName == "en")
                vars["DisplayLG1"] = "";
            if (translator.LanguageName == "fr")
                vars["DisplayLG2"] = "";
            if (translator.LanguageName == "de")
                vars["DisplayLG3"] = "";
            if (translator.LanguageName == "it")
                vars["DisplayLG4"] = "";
            if (translator.LanguageName == "es")
                vars["DisplayLG5"] = "";

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = null;
            return false;
        }
    }
}
