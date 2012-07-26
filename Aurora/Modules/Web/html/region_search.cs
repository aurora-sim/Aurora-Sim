using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Web
{
    public class RegionSearchPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/region_search.html"
                       };
            }
        }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();
            var regionslist = new List<Dictionary<string, object>>();

            uint amountPerQuery = 10;

            if (requestParameters.ContainsKey("Submit"))
            {
                IGridService gridService = webInterface.Registry.RequestModuleInterface<IGridService>();
                string regionname = requestParameters["regionname"].ToString();
                int start = httpRequest.Query.ContainsKey("Start") ? int.Parse(httpRequest.Query["Start"].ToString()) : 0;
                uint count = gridService.GetRegionsByNameCount(null, regionname);
                int maxPages = (int)(count / amountPerQuery) - 1;

                if (start == -1)
                    start = (int)(maxPages < 0 ? 0 : maxPages);

                vars.Add("CurrentPage", start);
                vars.Add("NextOne", start + 1 > maxPages ? start : start + 1);
                vars.Add("BackOne", start - 1 < 0 ? 0 : start - 1);

                var regions = gridService.GetRegionsByName(null, regionname, (uint)start, amountPerQuery);
                if (regions != null)
                {
                    foreach (var region in regions)
                    {
                        regionslist.Add(new Dictionary<string, object> { { "RegionName", region.RegionName }, 
                        { "RegionID", region.RegionID } });
                    }
                }
            }
            else
            {
                vars.Add("CurrentPage", 0);
                vars.Add("NextOne", 0);
                vars.Add("BackOne", 0);
            }
					
            vars.Add("RegionsList", regionslist);
            vars.Add("RegionSearchText", translator.GetTranslatedString("RegionSearchText"));
            vars.Add("SearchForRegionText", translator.GetTranslatedString("SearchForRegionText"));
            vars.Add("RegionNameText", translator.GetTranslatedString("RegionNameText"));
            vars.Add("Search", translator.GetTranslatedString("Search"));

            vars.Add("FirstText", translator.GetTranslatedString("FirstText"));
            vars.Add("BackText", translator.GetTranslatedString("BackText"));
            vars.Add("NextText", translator.GetTranslatedString("NextText"));
            vars.Add("LastText", translator.GetTranslatedString("LastText"));
            vars.Add("CurrentPageText", translator.GetTranslatedString("CurrentPageText"));

            vars.Add("SearchResultForRegionText", translator.GetTranslatedString("SearchResultForRegionText"));
            vars.Add("RegionMoreInfo", translator.GetTranslatedString("RegionMoreInfo"));
			vars.Add("MoreInfoText", translator.GetTranslatedString("MoreInfoText"));
			
            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}
