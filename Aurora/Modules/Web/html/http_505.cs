using Aurora.Framework.Servers.HttpServer;
using System.Collections.Generic;

namespace Aurora.Modules.Web
{
    public class Http505Page : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/http_505.html"
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
            vars.Add("Error505Text", translator.GetTranslatedString("Error505Text"));
            vars.Add("Error505InfoText", translator.GetTranslatedString("Error505InfoText"));
            vars.Add("HomePage505Text", translator.GetTranslatedString("HomePage505Text"));
			return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}