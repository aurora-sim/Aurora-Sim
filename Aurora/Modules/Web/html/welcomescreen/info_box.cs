using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using OpenMetaverse;
using System.Collections.Generic;

namespace Aurora.Modules.Web
{
    public class InfoBoxPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "html/welcomescreen/info_box.html"
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

            IGenericsConnector connector = Framework.Utilities.DataManager.RequestPlugin<IGenericsConnector>();
            GridWelcomeScreen welcomeInfo = connector.GetGeneric<GridWelcomeScreen>(UUID.Zero, "GridWelcomeScreen",
                                                                                    "GridWelcomeScreen");
            if (welcomeInfo == null)
                welcomeInfo = GridWelcomeScreen.Default;

            vars.Add("Title", welcomeInfo.SpecialWindowMessageTitle);
            vars.Add("Text", welcomeInfo.SpecialWindowMessageText);
            vars.Add("Color", welcomeInfo.SpecialWindowMessageColor);
            vars.Add("Active", welcomeInfo.SpecialWindowActive);
            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}