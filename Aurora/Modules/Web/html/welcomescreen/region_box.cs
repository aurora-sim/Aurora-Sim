using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework;

namespace Aurora.Modules.Web
{
    public class RegionBoxPage : IWebInterfacePage
    {
        public string FilePath { get { return "html/welcomescreen/region_box.html"; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, Hashtable query, OSHttpResponse httpResponse, Dictionary<string, object> requestParameters)
        {
            var vars = new Dictionary<string, object>();

            List<Dictionary<string, object>> RegionListVars = new List<Dictionary<string, object>>();
            var sortBy = new Dictionary<string, bool>();
            if (query.ContainsKey("region"))
                sortBy.Add(query["region"].ToString(), true);
            var regions = DataManager.DataManager.RequestPlugin<IRegionData>().Get((Framework.RegionFlags)0,
                Framework.RegionFlags.Hyperlink | Framework.RegionFlags.Foreign | Framework.RegionFlags.Hidden,
                null, null, sortBy);
            foreach (var region in regions)
                RegionListVars.Add(new Dictionary<string, object> { { "RegionLocX", region.RegionLocX / Constants.RegionSize }, 
                    { "RegionLocY", region.RegionLocY / Constants.RegionSize }, { "RegionName", region.RegionName } });

            vars.Add("RegionList", RegionListVars);
            vars.Add("RegionText", "Region");

            return vars;
        }
    }
}
