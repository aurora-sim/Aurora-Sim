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
    public class UserSearchPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/user_search.html"
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

            if (requestParameters.ContainsKey("Submit"))
            {
                IUserAccountService accountService = webInterface.Registry.RequestModuleInterface<IUserAccountService>();
                string username = requestParameters["username"].ToString();
                int start = httpRequest.Query.ContainsKey("Start") ? int.Parse(httpRequest.Query["Start"].ToString()) : 0;
                uint count = accountService.NumberOfUserAccounts(null, username);
                int maxPages = (int)(count / amountPerQuery) - 1;

                if (start == -1)
                    start = (int)(maxPages < 0 ? 0 : maxPages);

                vars.Add("CurrentPage", start);
                vars.Add("NextOne", start + 1 > maxPages ? start : start + 1);
                vars.Add("BackOne", start - 1 < 0 ? 0 : start - 1);

                var users = accountService.GetUserAccounts(null, username, (uint)start, amountPerQuery);
                foreach (var user in users)
                {
                    usersList.Add(new Dictionary<string, object> { { "UserName", user.Name }, 
                        { "UserID", user.PrincipalID } });
                }
            }
            else
            {
                vars.Add("CurrentPage", 0);
                vars.Add("NextOne", 0);
                vars.Add("BackOne", 0);
            }

            vars.Add("UsersList", usersList);
            vars.Add("UserSearchText", translator.GetTranslatedString("UserSearchText"));
            vars.Add("SearchForUserText", translator.GetTranslatedString("SearchForUserText"));
            vars.Add("UserNameText", translator.GetTranslatedString("UserNameText"));
            vars.Add("Search", translator.GetTranslatedString("Search"));
			vars.Add("SearchResultForUserText", translator.GetTranslatedString("SearchResultForUserText"));
            vars.Add("EditText", translator.GetTranslatedString("EditText"));
            vars.Add("EditUserAccountText", translator.GetTranslatedString("EditUserAccountText"));

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
