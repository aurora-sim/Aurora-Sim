using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenMetaverse;
using System;
using System.Collections.Generic;

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

        public bool RequiresAuthentication { get { return true; } }
        public bool RequiresAdminAuthentication { get { return true; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator, out string response)
        {
            response = null;
            var vars = new Dictionary<string, object>();
            if (requestParameters.ContainsKey("Submit"))
            {
                string title = requestParameters["NewsTitle"].ToString();
                string text = requestParameters["NewsText"].ToString();
                IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
                GridNewsItem item = new GridNewsItem { Text = text, Time = DateTime.Now, Title = title };
                item.ID = connector.GetGenericCount(UUID.Zero, "WebGridNews") + 1;
                connector.AddGeneric(UUID.Zero, "WebGridNews", item.ID.ToString(), item.ToOSD());
                response = "<h3>News item added successfully, redirecting to main page</h3>" +
                    "<script language=\"javascript\">" +
                    "setTimeout(function() {window.location.href = \"index.html?page=news_manager\";}, 0);" +
                    "</script>";
                return null;
            }

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
