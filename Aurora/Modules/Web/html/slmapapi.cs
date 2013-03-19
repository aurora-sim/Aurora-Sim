using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using System.Collections.Generic;
using Aurora.Framework.Services;

namespace Aurora.Modules.Web
{
    public class SLMapAPIPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "html/map/slmapapi.js"
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

            IMapService mapService = webInterface.Registry.RequestModuleInterface<IMapService>();

            vars.Add("WorldMapServiceURL", mapService.MapServiceURL.Remove(mapService.MapServiceURL.Length - 1));
            vars.Add("WorldMapAPIServiceURL", mapService.MapServiceAPIURL.Remove(mapService.MapServiceAPIURL.Length - 1));

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}