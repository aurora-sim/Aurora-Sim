using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using OpenMetaverse;
using System.Collections.Generic;

namespace Aurora.Modules.Web
{
    public class WelcomeScreenManagerPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "html/admin/welcomescreen_manager.html"
                           };
            }
        }

        public bool RequiresAuthentication
        {
            get { return true; }
        }

        public bool RequiresAdminAuthentication
        {
            get { return true; }
        }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
                                               OSHttpResponse httpResponse, Dictionary<string, object> requestParameters,
                                               ITranslator translator, out string response)
        {
            response = null;
            var vars = new Dictionary<string, object>();
            IGenericsConnector connector = Framework.Utilities.DataManager.RequestPlugin<IGenericsConnector>();

            if (requestParameters.ContainsKey("Submit"))
            {
                GridWelcomeScreen submittedInfo = new GridWelcomeScreen();
                submittedInfo.SpecialWindowMessageTitle = requestParameters["SpecialWindowTitle"].ToString();
                submittedInfo.SpecialWindowMessageText = requestParameters["SpecialWindowText"].ToString();
                submittedInfo.SpecialWindowMessageColor = requestParameters["SpecialWindowColor"].ToString();
                submittedInfo.SpecialWindowActive = requestParameters["SpecialWindowStatus"].ToString() == "1";
                submittedInfo.GridStatus = requestParameters["GridStatus"].ToString() == "1";

                connector.AddGeneric(UUID.Zero, "GridWelcomeScreen", "GridWelcomeScreen", submittedInfo.ToOSD());

                response = "Successfully saved data";
                return null;
            }

            GridWelcomeScreen welcomeInfo = connector.GetGeneric<GridWelcomeScreen>(UUID.Zero, "GridWelcomeScreen",
                                                                                    "GridWelcomeScreen");
            if (welcomeInfo == null)
                welcomeInfo = GridWelcomeScreen.Default;
            vars.Add("OpenNewsManager", translator.GetTranslatedString("OpenNewsManager"));
            vars.Add("SpecialWindowTitleText", translator.GetTranslatedString("SpecialWindowTitleText"));
            vars.Add("SpecialWindowTextText", translator.GetTranslatedString("SpecialWindowTextText"));
            vars.Add("SpecialWindowColorText", translator.GetTranslatedString("SpecialWindowColorText"));
            vars.Add("SpecialWindowStatusText", translator.GetTranslatedString("SpecialWindowStatusText"));
            vars.Add("WelcomeScreenManagerFor", translator.GetTranslatedString("WelcomeScreenManagerFor"));
            vars.Add("GridStatus", translator.GetTranslatedString("GridStatus"));
            vars.Add("Online", translator.GetTranslatedString("Online"));
            vars.Add("Offline", translator.GetTranslatedString("Offline"));
            vars.Add("Enabled", translator.GetTranslatedString("Enabled"));
            vars.Add("Disabled", translator.GetTranslatedString("Disabled"));

            vars.Add("SpecialWindowTitle", welcomeInfo.SpecialWindowMessageTitle);
            vars.Add("SpecialWindowMessage", welcomeInfo.SpecialWindowMessageText);
            vars.Add("SpecialWindowActive", welcomeInfo.SpecialWindowActive ? "selected" : "");
            vars.Add("SpecialWindowInactive", welcomeInfo.SpecialWindowActive ? "" : "selected");
            vars.Add("GridActive", welcomeInfo.GridStatus ? "selected" : "");
            vars.Add("GridInactive", welcomeInfo.GridStatus ? "" : "selected");
            vars.Add("SpecialWindowColorRed", welcomeInfo.SpecialWindowMessageColor == "red" ? "selected" : "");
            vars.Add("SpecialWindowColorYellow", welcomeInfo.SpecialWindowMessageColor == "yellow" ? "selected" : "");
            vars.Add("SpecialWindowColorGreen", welcomeInfo.SpecialWindowMessageColor == "green" ? "selected" : "");
            vars.Add("SpecialWindowColorWhite", welcomeInfo.SpecialWindowMessageColor == "white" ? "selected" : "");
            vars.Add("Submit", translator.GetTranslatedString("Submit"));

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}