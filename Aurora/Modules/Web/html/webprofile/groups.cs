using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Web
{
    public class AgentGroupsPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/webprofile/groups.html"
                       };
            }
        }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();

            string username = filename.Split('/').LastOrDefault();
            UserAccount account = null;
            if (httpRequest.Query.ContainsKey("userid"))
            {
                string userid = httpRequest.Query["userid"].ToString();

                account = webInterface.Registry.RequestModuleInterface<IUserAccountService>().
                    GetUserAccount(null, UUID.Parse(userid));
            }
            else if (httpRequest.Query.ContainsKey("name") || username.Contains('.'))
            {
                string name = httpRequest.Query.ContainsKey("name") ? httpRequest.Query["name"].ToString() : username;
                name = name.Replace('.', ' ');
                account = webInterface.Registry.RequestModuleInterface<IUserAccountService>().
                    GetUserAccount(null, name);
            }
            else
            {
                username = username.Replace("%20", " ");
                account = webInterface.Registry.RequestModuleInterface<IUserAccountService>().
                    GetUserAccount(null, username);
            }

            if (account == null)
                return vars;

            vars.Add("UserName", account.Name);
            vars.Add("UserType", account.UserTitle == "" ? "Resident" : account.UserTitle);

            IUserProfileInfo profile = Aurora.DataManager.DataManager.RequestPlugin<IProfileConnector>().
                GetUserProfile(account.PrincipalID);
            IWebHttpTextureService webhttpService = webInterface.Registry.RequestModuleInterface<IWebHttpTextureService>();
            if (profile != null)
            {
                if (profile.Partner != UUID.Zero)
                {
                    account = webInterface.Registry.RequestModuleInterface<IUserAccountService>().
                        GetUserAccount(null, profile.Partner);
                    vars.Add("UserPartner", account.Name);
                }
                else
                    vars.Add("UserPartner", "No partner");
                vars.Add("UserAboutMe", profile.AboutText == "" ? "Nothing here" : profile.AboutText);
                string url = "../images/icons/no_picture.jpg";
                if (webhttpService != null && profile.Image != UUID.Zero)
                    url = webhttpService.GetTextureURL(profile.Image);
                vars.Add("UserPictureURL", url);
            }

            vars.Add("UsersGroupsText", translator.GetTranslatedString("UsersGroupsText"));

            IGroupsServiceConnector groupsConnector = Aurora.DataManager.DataManager.RequestPlugin<IGroupsServiceConnector>();
            if (groupsConnector != null)
            {
                List<Dictionary<string, object>> groups = new List<Dictionary<string, object>>();
                foreach (var grp in groupsConnector.GetAgentGroupMemberships(account.PrincipalID, account.PrincipalID))
                {
                    var grpData = groupsConnector.GetGroupProfile(account.PrincipalID, grp.GroupID);
                    string url = "../images/icons/no_picture.jpg";
                    if (webhttpService != null && grpData.InsigniaID != UUID.Zero)
                        url = webhttpService.GetTextureURL(grpData.InsigniaID);
                    groups.Add(new Dictionary<string, object>
                    {
                        { "GroupPictureURL", url },
                        { "GroupName", grp.GroupName }
                    });
                }
                vars.Add("Groups", groups);
                vars.Add("GroupsJoined", groups.Count);
            }

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            httpResponse.ContentType = "text/html";
            text = File.ReadAllText("html/webprofile/index.html");
            return true;
        }
    }
}
