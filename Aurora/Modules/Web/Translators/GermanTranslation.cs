using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Modules.Web.Translators
{
    public class GermanTranslation : ITranslator
    {
        public string LanguageName { get { return "de"; } }

        public string GetTranslatedString(string key)
        {
            switch (key)
            {
                case "GridStatus":
                    return "STATUS";
                case "Online":
                    return "ONLINE";
                case "Offline":
                    return "OFFLINE";
                case "TotalUserCount":
                    return "Einwohner";
                case "TotalRegionCount":
                    return "Regionen";
                case "UniqueVisitors":
                    return "Aktive Nutzer letzten 30 Tage";
                case "OnlineNow":
                    return "Jetzt Online";
                case "HyperGrid":
                    return "HyperGrid (HG)";
                case "Voice":
                    return "Stimme";
                case "Currency":
                    return "Devisen";
                case "Disabled":
                    return "Deaktiviert";
                case "Enabled":
                    return "Aktiviert";
                case "News":
                    return "Nachrichten";
                case "Region":
                    return "Region";
                case "Login":
                    return "Einloggen";
                case "UserName":
                    return "Nutzername";
                case "Password":
                    return "Passwort";
                case "ForgotPassword":
                    return "Passwort vergessen?";
                case "Submit":
                    return "Einreichen";
            }
            return "UNKNOWN CHARACTER";
        }
    }
}
