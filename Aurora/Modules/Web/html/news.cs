using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenMetaverse;
using System.Collections.Generic;

namespace Aurora.Modules.Web
{
    public class NewsPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "html/news.html"
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
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            GridNewsItem news = connector.GetGeneric<GridNewsItem>(UUID.Zero, "WebGridNews",
                                                                   httpRequest.Query["newsid"].ToString());
            if (news != null)
            {
                vars.Add("NewsTitle", news.Title);
                vars.Add("NewsText", news.Text);
                vars.Add("NewsID", news.ID.ToString());
            }
            else
            {
                if (httpRequest.Query["newsid"].ToString() == "-1")
                {
                    vars.Add("NewsTitle", "No news to report");
                    vars.Add("NewsText", "");
                }
                else
                {
                    vars.Add("NewsTitle", "Invalid News Item");
                    vars.Add("NewsText", "");
                }
                vars.Add("NewsID", "-1");
            }

            vars.Add("News", translator.GetTranslatedString("News"));
            vars.Add("NewsItemTitle", translator.GetTranslatedString("NewsItemTitle"));
            vars.Add("NewsItemText", translator.GetTranslatedString("NewsItemText"));
            vars.Add("EditNewsText", translator.GetTranslatedString("EditNewsText"));
            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}