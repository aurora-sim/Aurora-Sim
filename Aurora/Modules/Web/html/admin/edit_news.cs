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
    public class EditNewPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/admin/edit_news.html"
                       };
            }
        }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            GridNewsItem news;
            if (requestParameters.ContainsKey("Submit"))
            {
                string title = requestParameters["NewsTitle"].ToString();
                string text = requestParameters["NewsText"].ToString();
                string id = requestParameters["NewsID"].ToString();
                news = connector.GetGeneric<GridNewsItem>(UUID.Zero, "WebGridNews", id);
                connector.RemoveGeneric(UUID.Zero, "WebGridNews", id);
                GridNewsItem item = new GridNewsItem { Text = text, Time = news.Time, Title = title, ID = int.Parse(id) };
                connector.AddGeneric(UUID.Zero, "WebGridNews", id, item.ToOSD());
                vars["ErrorMessage"] = "News item edit successfully";
                webInterface.Redirect(httpResponse, "index.html?page=news_manager", filename);
                return vars;
            }
            else
                vars["ErrorMessage"] = "";


            news = connector.GetGeneric<GridNewsItem>(UUID.Zero, "WebGridNews", httpRequest.Query["newsid"].ToString());
            vars.Add("NewsTitle", news.Title);
            vars.Add("NewsText", news.Text);
            vars.Add("NewsID", news.ID.ToString());

            vars.Add("NewsItemTitle", translator.GetTranslatedString("NewsItemTitle"));
            vars.Add("NewsItemText", translator.GetTranslatedString("NewsItemText"));
            vars.Add("EditNewsText", translator.GetTranslatedString("EditNewsText"));
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
