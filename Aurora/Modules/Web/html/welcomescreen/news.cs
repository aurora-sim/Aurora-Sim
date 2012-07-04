using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenMetaverse;

namespace Aurora.Modules.Web
{
    public class NewsPage : IWebInterfacePage
    {
        public string FilePath { get { return "html/welcomescreen/news.html"; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, Hashtable query, OSHttpResponse httpResponse, Dictionary<string, object> requestParameters)
        {
            var vars = new Dictionary<string, object>();

            vars.Add("News", "News");

            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            var newsItems = connector.GetGenerics<GridNewsItem>(UUID.Zero, "WebGridNews");
            if (newsItems.Count == 0)
                newsItems.Add(GridNewsItem.NoNewsItem);
            vars.Add("NewsList", newsItems.ConvertAll<Dictionary<string, object>>(item => item.ToDictionary()));

            return vars;
        }
    }
}
