using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenMetaverse;
using System.Collections.Generic;

namespace Aurora.Modules.Web
{
    public class SettingsManagerPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "html/admin/settings_manager.html"
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
            var settings = connector.GetGeneric<GridSettings>(UUID.Zero, "WebSettings", "Settings");

            if (requestParameters.ContainsKey("Submit"))
            {
                settings.MapCenter.X = int.Parse(requestParameters["GridCenterX"].ToString());
                settings.MapCenter.Y = int.Parse(requestParameters["GridCenterY"].ToString());
                settings.HideLanguageTranslatorBar = requestParameters["HideLanguageBar"].ToString() == "1";
                settings.HideStyleBar = requestParameters["HideStyleBar"].ToString() == "1";
                connector.AddGeneric(UUID.Zero, "WebSettings", "Settings", settings.ToOSD());

                response = "Successfully updated settings.";

                return null;
            }
            else if (requestParameters.ContainsKey("IgnorePagesUpdates"))
            {
                settings.LastPagesVersionUpdateIgnored = PagesMigrator.CurrentVersion;
                connector.AddGeneric(UUID.Zero, "WebSettings", "Settings", settings.ToOSD());
            }
            else if (requestParameters.ContainsKey("IgnoreSettingsUpdates"))
            {
                settings.LastSettingsVersionUpdateIgnored = PagesMigrator.CurrentVersion;
                connector.AddGeneric(UUID.Zero, "WebSettings", "Settings", settings.ToOSD());
            }
            vars.Add("GridCenterX", settings.MapCenter.X);
            vars.Add("GridCenterY", settings.MapCenter.Y);
            vars.Add("HideLanguageBarNo", !settings.HideLanguageTranslatorBar ? "selected=\"selected\"" : "");
            vars.Add("HideLanguageBarYes", settings.HideLanguageTranslatorBar ? "selected=\"selected\"" : "");
            vars.Add("HideStyleBarNo", !settings.HideStyleBar ? "selected=\"selected\"" : "");
            vars.Add("HideStyleBarYes", settings.HideStyleBar ? "selected=\"selected\"" : "");
            vars.Add("IgnorePagesUpdates",
                     PagesMigrator.CheckWhetherIgnoredVersionUpdate(settings.LastPagesVersionUpdateIgnored)
                         ? ""
                         : "checked");
            vars.Add("IgnoreSettingsUpdates",
                     settings.LastSettingsVersionUpdateIgnored != SettingsMigrator.CurrentVersion ? "" : "checked");

            vars.Add("SettingsManager", translator.GetTranslatedString("SettingsManager"));
            vars.Add("IgnorePagesUpdatesText", translator.GetTranslatedString("IgnorePagesUpdatesText"));
            vars.Add("IgnoreSettingsUpdatesText", translator.GetTranslatedString("IgnoreSettingsUpdatesText"));
            vars.Add("GridCenterXText", translator.GetTranslatedString("GridCenterXText"));
            vars.Add("GridCenterYText", translator.GetTranslatedString("GridCenterYText"));
            vars.Add("HideLanguageBarText", translator.GetTranslatedString("HideLanguageBarText"));
            vars.Add("HideStyleBarText", translator.GetTranslatedString("HideStyleBarText"));
            vars.Add("Save", translator.GetTranslatedString("Save"));
            vars.Add("No", translator.GetTranslatedString("No"));
            vars.Add("Yes", translator.GetTranslatedString("Yes"));

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}