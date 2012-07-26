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
    public class AgentPicksPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/webprofile/picks.html"
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

            IProfileConnector profileConnector = Aurora.DataManager.DataManager.RequestPlugin<IProfileConnector>();
            IUserProfileInfo profile = profileConnector == null ? null : profileConnector.GetUserProfile(account.PrincipalID);
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

                List<Dictionary<string, object>> picks = new List<Dictionary<string, object>>();
                foreach (var pick in profileConnector.GetPicks(profile.PrincipalID))
                {
                    url = "../images/icons/no_picture.jpg";
                    if (webhttpService != null && pick.SnapshotUUID != UUID.Zero)
                        url = webhttpService.GetTextureURL(pick.SnapshotUUID);
                    picks.Add(new Dictionary<string, object>
                    {
                        { "PickSnapshotURL", url },
                        { "PickName", pick.OriginalName },
                        { "PickSim", pick.SimName },
                        { "PickLocation", pick.GlobalPos }
                    });
                }
                vars.Add("Picks", picks);
            }

            vars.Add("UsersPicksText", translator.GetTranslatedString("UsersPicksText"));

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
