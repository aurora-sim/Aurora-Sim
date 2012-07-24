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
                settings.HideLanguageTranslatorBar = requestParameters["HideLanguageBar"].ToString() == "1";
                settings.HideStyleBar = requestParameters["HideStyleBar"].ToString() == "1";
                connector.AddGeneric(UUID.Zero, "WebSettings", "Settings", settings.ToOSD());
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
            vars.Add("IgnorePagesUpdates", PagesMigrator.CheckWhetherIgnoredVersionUpdate(settings.LastPagesVersionUpdateIgnored) ? "" : "checked");
            vars.Add("IgnoreSettingsUpdates", settings.LastSettingsVersionUpdateIgnored != SettingsMigrator.CurrentVersion ? "" : "checked");

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
            vars.Add("ChangesSavedSuccessfully", changed ? translator.GetTranslatedString("ChangesSavedSuccessfully") : "");

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}
