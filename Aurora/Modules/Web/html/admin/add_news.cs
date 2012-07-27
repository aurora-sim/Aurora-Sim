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
    public class AddNewPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/admin/add_news.html"
                       };
            }
        }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();
            if (requestParameters.ContainsKey("Submit"))
            {
                string title = requestParameters["NewsTitle"].ToString();
                string text = requestParameters["NewsText"].ToString();
                IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
                GridNewsItem item = new GridNewsItem { Text = text, Time = DateTime.Now, Title = title };
                item.ID = connector.GetGenericCount(UUID.Zero, "WebGridNews") + 1;
                connector.AddGeneric(UUID.Zero, "WebGridNews", item.ID.ToString(), item.ToOSD());
                vars["ErrorMessage"] = "News item added successfully";
                webInterface.Redirect(httpResponse, "index.html?page=news_manager", filename);
                return vars;
            }
            else
                vars["ErrorMessage"] = "";

            vars.Add("NewsItemTitle", translator.GetTranslatedString("NewsItemTitle"));
            vars.Add("NewsItemText", translator.GetTranslatedString("NewsItemText"));
            vars.Add("AddNewsText", translator.GetTranslatedString("AddNewsText"));
            vars.Add("Submit", translator.GetTranslatedString("Submit"));

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}
