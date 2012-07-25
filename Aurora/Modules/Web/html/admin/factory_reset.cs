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
    public class FactoryResetPage : IWebInterfacePage
    {
        public string[] FilePath 
        { 
            get
            {
                return new[]
                           {
                               "html/admin/factory_reset.html"
                           };
            } 
        }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();
            
            bool changed = false;
            if (requestParameters.ContainsKey("ResetMenu"))
            {
                changed = true;
                PagesMigrator.ResetToDefaults();
            }
            if (requestParameters.ContainsKey("ResetSettings"))
            {
                changed = true;
                SettingsMigrator.ResetToDefaults();
            }

            vars.Add("FactoryReset", translator.GetTranslatedString("FactoryReset"));
            vars.Add("ResetMenuText", translator.GetTranslatedString("ResetMenuText"));
            vars.Add("ResetSettingsText", translator.GetTranslatedString("ResetSettingsText"));
            vars.Add("ResetMenuInfoText", translator.GetTranslatedString("ResetMenuText"));
            vars.Add("ResetSettingsInfoText", translator.GetTranslatedString("ResetSettingsInfoText"));
            vars.Add("Reset", translator.GetTranslatedString("Reset"));
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
