using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenMetaverse;
using System.Collections.Generic;

namespace Aurora.Modules.Web
{
    public class NewsManagerPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "html/admin/news_manager.html"
                           };
            }
        }

        public bool RequiresAuthentication
        {
            get { return true; }
        }

        public bool RequiresAdminAuthentication
        {
            get { return true; }
        }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
                                               OSHttpResponse httpResponse, Dictionary<string, object> requestParameters,
                                               ITranslator translator, out string response)
        {
            response = null;
            var vars = new Dictionary<string, object>();
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            if (httpRequest.Query.Contains("delete"))
            {
                string newsID = httpRequest.Query["delete"].ToString();
                connector.RemoveGeneric(UUID.Zero, "WebGridNews", newsID);
                vars["Success"] = "Successfully deleted the news item";
            }
            else
                vars["Success"] = "";
            var newsItems = connector.GetGenerics<GridNewsItem>(UUID.Zero, "WebGridNews");
            vars.Add("News", newsItems.ConvertAll<Dictionary<string, object>>(item => item.ToDictionary()));
            vars.Add("NewsManager", translator.GetTranslatedString("NewsManager"));
            vars.Add("EditNewsItem", translator.GetTranslatedString("EditNewsItem"));
            vars.Add("AddNewsItem", translator.GetTranslatedString("AddNewsItem"));
            vars.Add("DeleteNewsItem", translator.GetTranslatedString("DeleteNewsItem"));
            vars.Add("NewsTitleText", translator.GetTranslatedString("NewsTitleText"));
            vars.Add("NewsDateText", translator.GetTranslatedString("NewsDateText"));
            vars.Add("EditNewsText", translator.GetTranslatedString("EditNewsText"));
            vars.Add("DeleteNewsText", translator.GetTranslatedString("DeleteNewsText"));
            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}