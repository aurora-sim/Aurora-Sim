using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenMetaverse;
using System.Collections.Generic;

namespace Aurora.Modules.Web
{
    public class WorldMapPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/world_map.html"
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

            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            var settings = connector.GetGeneric<GridSettings>(UUID.Zero, "WebSettings", "Settings");

            vars.Add("GridCenterX", settings.MapCenter.X);
            vars.Add("GridCenterY", settings.MapCenter.Y);

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}
