using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Modules.Web.Translators
{
    public class ItalianTranslation : ITranslator
    {
        public string LanguageName { get { return "it"; } }

        public string GetTranslatedString(string key)
        {
            switch (key)
            {
                case "GridStatus":
                    return "STATO DELLA GRID";
                case "Online":
                    return "ONLINE";
                case "Offline":
                    return "OFFLINE";
                case "TotalUserCount":
                    return "Utenti Totali";
                case "TotalRegionCount":
                    return "Regioni Totali";
                case "UniqueVisitors":
                    return "Visitatori unici ultimi 30 giorni";
                case "OnlineNow":
                    return "Online Adesso";
                case "HyperGrid":
                    return "HyperGrid (HG)";
                case "Voice":
                    return "Voice";
                case "Currency":
                    return "Valuta";
                case "Disabled":
                    return "Disabilitato";
                case "Enabled":
                    return "Abilitato";
                case "News":
                    return "News";
                case "Region":
                    return "Regione";
                case "Login":
                    return "Login";
                case "UserName":
                    return "Nome utente";
                case "Password":
                    return "Password";
                case "ForgotPassword":
                    return "Password dimenticata?";
                case "Submit":
                    return "Invia";
            }
            return "UNKNOWN CHARACTER";
        }
    }
}
