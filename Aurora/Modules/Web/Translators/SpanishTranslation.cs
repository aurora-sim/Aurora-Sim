using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Modules.Web.Translators
{
    public class SpanishTranslation : ITranslator
    {
        public string LanguageName { get { return "es"; } }

        public string GetTranslatedString(string key)
        {
            switch (key)
            {
                case "GridStatus":
                    return "ESTADO DE LA GRID";
                case "Online":
                    return "EN LINEA";
                case "Offline":
                    return "OFFLINE";
                case "TotalUserCount":
                    return "total de Usuarios";
                case "TotalRegionCount":
                    return "Cuenta Total Región";
                case "UniqueVisitors":
                    return "Visitantes únicos últimos 30 días";
                case "OnlineNow":
                    return "En línea ahora";
                case "HyperGrid":
                    return "HyperGrid (HG)";
                case "Voice":
                    return "Voz";
                case "Currency":
                    return "Moneda";
                case "Disabled":
                    return "Discapacitado";
                case "Enabled":
                    return "Habilitado";
                case "News":
                    return "Nuevas";
                case "Region":
                    return "Región";
                case "Login":
                    return "Iniciar sesión";
                case "UserName":
                    return "Nombre de usuario";
                case "Password":
                    return "Contraseña";
                case "ForgotPassword":
                    return "¿Olvidó su contraseña?";
                case "Submit":
                    return "Enviar";
            }
            return "UNKNOWN CHARACTER";
        }
    }
}
