using Aurora.Framework.Servers.HttpServer;
using System.Collections.Generic;

namespace Aurora.Modules.Web
{
    public class WorldMain : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/world.html"
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
			
			vars.Add("WorldMap", translator.GetTranslatedString("WorldMap"));
			vars.Add("WorldMapText", translator.GetTranslatedString("WorldMapText"));
			
			return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}
