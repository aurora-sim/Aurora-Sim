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
    public class OnlineUsersPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/online_users.html"
                       };
            }
        }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();
            var usersList = new List<Dictionary<string, object>>();

            uint amountPerQuery = 10;
            int start = httpRequest.Query.ContainsKey("Start") ? int.Parse(httpRequest.Query["Start"].ToString()) : 0;
            uint count = DataManager.DataManager.RequestPlugin<IAgentInfoConnector>().RecentlyOnline(5 * 60, true);
            int maxPages = (int)(count / amountPerQuery) - 1;

            if (start == -1)
                start = (int)(maxPages < 0 ? 0 : maxPages);

            vars.Add("CurrentPage", start);
            vars.Add("NextOne", start + 1 > maxPages ? start : start + 1);
            vars.Add("BackOne", start - 1 < 0 ? 0 : start - 1);

            var users = DataManager.DataManager.RequestPlugin<IAgentInfoConnector>().RecentlyOnline(5 * 60, true, new Dictionary<string, bool>(), (uint)start, amountPerQuery);
            IUserAccountService accountService = webInterface.Registry.RequestModuleInterface<IUserAccountService>();
            IGridService gridService = webInterface.Registry.RequestModuleInterface<IGridService>();
            foreach (var user in users)
            {
                var region = gridService.GetRegionByUUID(null, user.CurrentRegionID);
                var account = accountService.GetUserAccount(region.AllScopeIDs, UUID.Parse(user.UserID));
                if (account != null && region != null)
                    usersList.Add(new Dictionary<string, object> { { "UserName", account.Name }, 
                        { "UserRegion", region.RegionName }, { "UserID", user.UserID }, { "UserRegionID", region.RegionID } });
            }
            if (requestParameters.ContainsKey("Order"))
            {
                if (requestParameters["Order"].ToString() == "RegionName")
                    usersList.Sort((a, b) => a["UserRegion"].ToString().CompareTo(b["UserRegion"].ToString()));
                if (requestParameters["Order"].ToString() == "UserName")
                    usersList.Sort((a, b) => a["UserName"].ToString().CompareTo(b["UserName"].ToString()));
            }


            vars.Add("UsersOnlineList", usersList);
            vars.Add("OnlineUsersText", translator.GetTranslatedString("OnlineUsersText"));
            vars.Add("UserNameText", translator.GetTranslatedString("UserNameText"));
            vars.Add("RegionNameText", translator.GetTranslatedString("RegionNameText"));
            vars.Add("MoreInfoText", translator.GetTranslatedString("MoreInfoText"));

            vars.Add("FirstText", translator.GetTranslatedString("FirstText"));
            vars.Add("BackText", translator.GetTranslatedString("BackText"));
            vars.Add("NextText", translator.GetTranslatedString("NextText"));
            vars.Add("LastText", translator.GetTranslatedString("LastText"));
            vars.Add("CurrentPageText", translator.GetTranslatedString("CurrentPageText"));

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}
