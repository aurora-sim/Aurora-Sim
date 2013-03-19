using Aurora.Framework.Servers.HttpServer;
using System.Collections.Generic;

namespace Aurora.Modules.Web
{
    public class JQueryClickPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "html/javascripts/jquery.jclock.js"
                           };
            }
        }

        public bool RequiresAuthentication
        {
            get { return false; }
        }

        public bool RequiresAdminAuthentication
        {
            get { return false; }
        }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
                                               OSHttpResponse httpResponse, Dictionary<string, object> requestParameters,
                                               ITranslator translator, out string response)
        {
            response = null;
            var vars = new Dictionary<string, object>();
            vars.Add("Sun", translator.GetTranslatedString("Sun"));
            vars.Add("Mon", translator.GetTranslatedString("Mon"));
            vars.Add("Tue", translator.GetTranslatedString("Tue"));
            vars.Add("Wed", translator.GetTranslatedString("Wed"));
            vars.Add("Thu", translator.GetTranslatedString("Thu"));
            vars.Add("Fri", translator.GetTranslatedString("Fri"));
            vars.Add("Sat", translator.GetTranslatedString("Sat"));
            vars.Add("Sunday", translator.GetTranslatedString("Sunday"));
            vars.Add("Monday", translator.GetTranslatedString("Monday"));
            vars.Add("Tuesday", translator.GetTranslatedString("Tuesday"));
            vars.Add("Wednesday", translator.GetTranslatedString("Wednesday"));
            vars.Add("Thursday", translator.GetTranslatedString("Thursday"));
            vars.Add("Friday", translator.GetTranslatedString("Friday"));
            vars.Add("Saturday", translator.GetTranslatedString("Saturday"));
            vars.Add("Jan", translator.GetTranslatedString("Jan"));
            vars.Add("Feb", translator.GetTranslatedString("Feb"));
            vars.Add("Mar", translator.GetTranslatedString("Mar"));
            vars.Add("Apr", translator.GetTranslatedString("Apr"));
            vars.Add("May", translator.GetTranslatedString("May"));
            vars.Add("Jun", translator.GetTranslatedString("Jun"));
            vars.Add("Jul", translator.GetTranslatedString("Jul"));
            vars.Add("Aug", translator.GetTranslatedString("Aug"));
            vars.Add("Sep", translator.GetTranslatedString("Sep"));
            vars.Add("Oct", translator.GetTranslatedString("Oct"));
            vars.Add("Nov", translator.GetTranslatedString("Nov"));
            vars.Add("Dec", translator.GetTranslatedString("Dec"));
            vars.Add("January", translator.GetTranslatedString("January"));
            vars.Add("February", translator.GetTranslatedString("February"));
            vars.Add("March", translator.GetTranslatedString("March"));
            vars.Add("April", translator.GetTranslatedString("April"));
            vars.Add("June", translator.GetTranslatedString("June"));
            vars.Add("July", translator.GetTranslatedString("July"));
            vars.Add("August", translator.GetTranslatedString("August"));
            vars.Add("September", translator.GetTranslatedString("September"));
            vars.Add("October", translator.GetTranslatedString("October"));
            vars.Add("November", translator.GetTranslatedString("November"));
            vars.Add("December", translator.GetTranslatedString("December"));
            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}