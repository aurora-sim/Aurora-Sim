using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Modules.Web.Translators
{
    public class FrenchTranslation : ITranslator
    {
        public string LanguageName { get { return "fr"; } }

        public string GetTranslatedString(string key)
        {
            switch (key)
            {
                case "GridStatus":
                    return "ETAT DE LA GRILLE";
                case "Online":
                    return "EN LIGNE";
                case "Offline":
                    return "HORS LIGNE";
                case "TotalUserCount":
                    return "Nombre total d'utilisateurs";
                case "TotalRegionCount":
                    return "Nombre total de régions";
                case "UniqueVisitors":
                    return "Visiteurs unique (30 jours)";
                case "OnlineNow":
                    return "En ligne maintenant";
                case "HyperGrid":
                    return "HyperGrid (HG)";
                case "Voice":
                    return "Voix";
                case "Currency":
                    return "Monnaie";
                case "Disabled":
                    return "Désactivé";
                case "Enabled":
                    return "Activé";
                case "News":
                    return "Nouveautés";
                case "Region":
                    return "Région";
                case "Login":
                    return "Connection";
                case "UserName":
                    return "Nom d'utilisateur";
                case "Password":
                    return "Mot de passe";
                case "ForgotPassword":
                    return "Mot de passe oublié?";
                case "Submit":
                    return "Connection";
            }
            return "UNKNOWN CHARACTER";
        }
    }
}
