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
    public class AgentInfoPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/webprofile/index.html",
                           "html/webprofile/"
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
                    GetUserAccount(UUID.Zero, UUID.Parse(userid));
            }
            else if (httpRequest.Query.ContainsKey("name") || username.Contains('.'))
            {
                string name = username.Contains('.') ? username : httpRequest.Query["name"].ToString();
                name = name.Replace('.', ' ');
                account = webInterface.Registry.RequestModuleInterface<IUserAccountService>().
                    GetUserAccount(UUID.Zero, name);
            }

            if (account == null)
                return vars;

            vars.Add("UserName", account.Name);
            vars.Add("UserBorn", Util.ToDateTime(account.Created).ToShortDateString());
            vars.Add("UserType", account.UserTitle == "" ? "Resident" : account.UserTitle);

            IUserProfileInfo profile = Aurora.DataManager.DataManager.RequestPlugin<IProfileConnector>().
                GetUserProfile(account.PrincipalID);
            if (profile != null)
            {
                if (profile.Partner != UUID.Zero)
                {
                    account = webInterface.Registry.RequestModuleInterface<IUserAccountService>().
                        GetUserAccount(UUID.Zero, profile.Partner);
                    vars.Add("UserPartner", account.Name);
                }
                else
                    vars.Add("UserPartner", "No partner");
                vars.Add("UserAboutMe", profile.AboutText == "" ? "Nothing here" : profile.AboutText);
                string url = "../images/info.jpg";
                IWebHttpTextureService webhttpService = webInterface.Registry.RequestModuleInterface<IWebHttpTextureService>();
                if (webhttpService != null && profile.Image != UUID.Zero)
                    url = webhttpService.GetTextureURL(profile.Image);
                vars.Add("UserPictureURL", url);
            }
            vars.Add("UserProfileFor", translator.GetTranslatedString("UserProfileFor"));
            vars.Add("ResidentSince", translator.GetTranslatedString("ResidentSince"));
            vars.Add("AccountType", translator.GetTranslatedString("AccountType"));
            vars.Add("PartnersName", translator.GetTranslatedString("PartnersName"));
            vars.Add("AboutMe", translator.GetTranslatedString("AboutMe"));

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
