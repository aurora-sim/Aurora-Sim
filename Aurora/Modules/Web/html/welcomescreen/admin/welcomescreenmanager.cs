using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Web
{
    public class WelcomeScreenManagerPage : IWebInterfacePage
    {
        public string FilePath { get { return "html/welcomescreen/admin/index.html"; } }
        
        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, Hashtable query, OSHttpResponse httpResponse,
            Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            bool changed = false;
            if (requestParameters.ContainsKey("submit"))
            {
                changed = true;
                httpResponse.KeepAlive = false;
                GridWelcomeScreen submittedInfo = new GridWelcomeScreen();
                submittedInfo.SpecialWindowMessageTitle = requestParameters["SpecialWindowTitle"].ToString();
                submittedInfo.SpecialWindowMessageText = requestParameters["SpecialWindowText"].ToString();
                submittedInfo.SpecialWindowMessageColor = requestParameters["SpecialWindowColor"].ToString();
                submittedInfo.SpecialWindowActive = requestParameters["SpecialWindowStatus"].ToString() == "1";
                submittedInfo.GridStatus = requestParameters["GridStatus"].ToString() == "1";

                connector.AddGeneric(UUID.Zero, "GridWelcomeScreen", "GridWelcomeScreen", submittedInfo.ToOSD());
            }

            GridWelcomeScreen welcomeInfo = connector.GetGeneric<GridWelcomeScreen>(UUID.Zero, "GridWelcomeScreen", "GridWelcomeScreen");
            if (welcomeInfo == null)
                welcomeInfo = GridWelcomeScreen.Default;
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
            vars.Add("ChangesSavedSuccessfully", changed ? translator.GetTranslatedString("ChangesSavedSuccessfully") : "");

            return vars;
        }
    }
}
