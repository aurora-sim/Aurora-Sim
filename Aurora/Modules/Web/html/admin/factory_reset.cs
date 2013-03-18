using Aurora.Framework.Servers.HttpServer;
using System.Collections.Generic;

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

        public bool RequiresAuthentication { get { return true; } }
        public bool RequiresAdminAuthentication { get { return true; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator, out string response)
        {
            response = null;
            var vars = new Dictionary<string, object>();
            
            if (requestParameters.ContainsKey("ResetMenu"))
            {
                PagesMigrator.ResetToDefaults();
                response = translator.GetTranslatedString("ChangesSavedSuccessfully");
                return null;
            }
            if (requestParameters.ContainsKey("ResetSettings"))
            {
                SettingsMigrator.ResetToDefaults();
                response = translator.GetTranslatedString("ChangesSavedSuccessfully");
                return null;
            }

            vars.Add("FactoryReset", translator.GetTranslatedString("FactoryReset"));
            vars.Add("ResetMenuText", translator.GetTranslatedString("ResetMenuText"));
            vars.Add("ResetSettingsText", translator.GetTranslatedString("ResetSettingsText"));
            vars.Add("ResetMenuInfoText", translator.GetTranslatedString("ResetMenuText"));
            vars.Add("ResetSettingsInfoText", translator.GetTranslatedString("ResetSettingsInfoText"));
            vars.Add("Reset", translator.GetTranslatedString("Reset"));

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}
