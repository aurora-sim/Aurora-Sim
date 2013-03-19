using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using System.Collections.Generic;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;

namespace Aurora.Modules.Web
{
    public class RegionBoxPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "html/welcomescreen/region_box.html",
                               "html/region_list.html"
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

            List<Dictionary<string, object>> RegionListVars = new List<Dictionary<string, object>>();
            var sortBy = new Dictionary<string, bool>();
            if (httpRequest.Query.ContainsKey("region"))
                sortBy.Add(httpRequest.Query["region"].ToString(), true);
            else if (httpRequest.Query.ContainsKey("Order"))
                sortBy.Add(httpRequest.Query["Order"].ToString(), true);

            uint amountPerQuery = 50;
            int start = httpRequest.Query.ContainsKey("Start") ? int.Parse(httpRequest.Query["Start"].ToString()) : 0;
            uint count = Framework.Utilities.DataManager.RequestPlugin<IRegionData>().Count((RegionFlags) 0,
                                                                                    RegionFlags.Hyperlink |
                                                                                    RegionFlags.Foreign |
                                                                                    RegionFlags.Hidden);
            int maxPages = (int) (count/amountPerQuery) - 1;

            if (start == -1)
                start = (int) (maxPages < 0 ? 0 : maxPages);

            vars.Add("CurrentPage", start);
            vars.Add("NextOne", start + 1 > maxPages ? start : start + 1);
            vars.Add("BackOne", start - 1 < 0 ? 0 : start - 1);

            var regions = Framework.Utilities.DataManager.RequestPlugin<IRegionData>().Get((RegionFlags) 0,
                                                                                   RegionFlags.Hyperlink |
                                                                                   RegionFlags.Foreign |
                                                                                   RegionFlags.Hidden,
                                                                                   (uint) (start*amountPerQuery),
                                                                                   amountPerQuery, sortBy);
            foreach (var region in regions)
                RegionListVars.Add(new Dictionary<string, object>
                                       {
                                           {"RegionLocX", region.RegionLocX/Constants.RegionSize},
                                           {"RegionLocY", region.RegionLocY/Constants.RegionSize},
                                           {"RegionName", region.RegionName},
                                           {"RegionID", region.RegionID}
                                       });

            vars.Add("RegionList", RegionListVars);
            vars.Add("RegionText", translator.GetTranslatedString("Region"));


            vars.Add("RegionNameText", translator.GetTranslatedString("RegionNameText"));
            vars.Add("RegionLocXText", translator.GetTranslatedString("RegionLocXText"));
            vars.Add("RegionLocYText", translator.GetTranslatedString("RegionLocYText"));
            vars.Add("SortByLocX", translator.GetTranslatedString("SortByLocX"));
            vars.Add("SortByLocY", translator.GetTranslatedString("SortByLocY"));
            vars.Add("SortByName", translator.GetTranslatedString("SortByName"));
            vars.Add("RegionListText", translator.GetTranslatedString("RegionListText"));
            vars.Add("FirstText", translator.GetTranslatedString("FirstText"));
            vars.Add("BackText", translator.GetTranslatedString("BackText"));
            vars.Add("NextText", translator.GetTranslatedString("NextText"));
            vars.Add("LastText", translator.GetTranslatedString("LastText"));
            vars.Add("CurrentPageText", translator.GetTranslatedString("CurrentPageText"));
            vars.Add("MoreInfoText", translator.GetTranslatedString("MoreInfoText"));
            vars.Add("RegionMoreInfo", translator.GetTranslatedString("RegionMoreInfo"));

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}