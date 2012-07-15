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
    public class SettingsManagerPage : IWebInterfacePage
    {
        public string[] FilePath { get
        {
            return new[]
                       {
                           "html/admin/settings_manager.html"
                       };
        } }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return true; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            var settings = connector.GetGeneric<GridSettings>(UUID.Zero, "WebSettings", "Settings");

            bool changed = false;
            if (requestParameters.ContainsKey("SetGridCenter"))
            {
                changed = true;
                settings.MapCenter.X = int.Parse(requestParameters["GridCenterX"].ToString());
                settings.MapCenter.Y = int.Parse(requestParameters["GridCenterY"].ToString());
                settings.GoogleMapsAPIKey = requestParameters["GoogleMapAPIKey"].ToString();
                connector.AddGeneric(UUID.Zero, "WebSettings", "Settings", settings.ToOSD());
            }
            vars.Add("GridCenterX", settings.MapCenter.X);
            vars.Add("GridCenterY", settings.MapCenter.Y);
            vars.Add("GoogleMapAPIKey", settings.GoogleMapsAPIKey);

            vars.Add("SettingsManager", translator.GetTranslatedString("SettingsManager"));
            vars.Add("GridCenterXText", translator.GetTranslatedString("GridCenterXText"));
            vars.Add("GridCenterYText", translator.GetTranslatedString("GridCenterYText"));
            vars.Add("GoogleMapAPIKeyText", translator.GetTranslatedString("GoogleMapAPIKeyText"));
            vars.Add("GoogleMapAPIKeyHelpText", translator.GetTranslatedString("GoogleMapAPIKeyHelpText"));
            vars.Add("Save", translator.GetTranslatedString("Save"));
            vars.Add("ChangesSavedSuccessfully", changed ? translator.GetTranslatedString("ChangesSavedSuccessfully") : "");

            return vars;
        }
    }
}
