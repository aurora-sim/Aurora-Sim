using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Profile;
using Aurora.Framework.Utilities;
using OpenMetaverse;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
                               "html/webprofile/base.html",
                               "html/webprofile/"
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

            string username = filename.Split('/').LastOrDefault();
            UserAccount account = null;
            if (httpRequest.Query.ContainsKey("userid"))
            {
                string userid = httpRequest.Query["userid"].ToString();

                account = webInterface.Registry.RequestModuleInterface<IUserAccountService>().
                                       GetUserAccount(null, UUID.Parse(userid));
            }
            else if (httpRequest.Query.ContainsKey("name"))
            {
                string name = httpRequest.Query.ContainsKey("name") ? httpRequest.Query["name"].ToString() : username;
                name = name.Replace('.', ' ');
                name = name.Replace("%20", " ");
                account = webInterface.Registry.RequestModuleInterface<IUserAccountService>().
                                       GetUserAccount(null, name);
            }
            else
            {
                username = username.Replace("%20", " ");
                webInterface.Redirect(httpResponse, "/webprofile/?name=" + username);
                return vars;
            }

            if (account == null)
                return vars;

            vars.Add("UserName", account.Name);
            vars.Add("UserBorn", Util.ToDateTime(account.Created).ToShortDateString());
            vars.Add("UserType", account.UserTitle == "" ? "Resident" : account.UserTitle);

            IUserProfileInfo profile = Framework.Utilities.DataManager.RequestPlugin<IProfileConnector>().
                                              GetUserProfile(account.PrincipalID);
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
                IWebHttpTextureService webhttpService =
                    webInterface.Registry.RequestModuleInterface<IWebHttpTextureService>();
                if (webhttpService != null && profile.Image != UUID.Zero)
                    url = webhttpService.GetTextureURL(profile.Image);
                vars.Add("UserPictureURL", url);
            }
            UserAccount ourAccount = Authenticator.GetAuthentication(httpRequest);
            if (ourAccount != null)
            {
                IFriendsService friendsService = webInterface.Registry.RequestModuleInterface<IFriendsService>();
                var friends = friendsService.GetFriends(account.PrincipalID);
                UUID friendID = UUID.Zero;
                if (friends.Any(f => UUID.TryParse(f.Friend, out friendID) && friendID == ourAccount.PrincipalID))
                {
                    IAgentInfoService agentInfoService =
                        webInterface.Registry.RequestModuleInterface<IAgentInfoService>();
                    IGridService gridService = webInterface.Registry.RequestModuleInterface<IGridService>();
                    UserInfo ourInfo = agentInfoService.GetUserInfo(account.PrincipalID.ToString());
                    if (ourInfo != null && ourInfo.IsOnline)
                        vars.Add("OnlineLocation", gridService.GetRegionByUUID(null, ourInfo.CurrentRegionID).RegionName);
                    vars.Add("UserIsOnline", ourInfo != null && ourInfo.IsOnline);
                    vars.Add("IsOnline",
                             ourInfo != null && ourInfo.IsOnline
                                 ? translator.GetTranslatedString("Online")
                                 : translator.GetTranslatedString("Offline"));
                }
                else
                {
                    vars.Add("OnlineLocation", "");
                    vars.Add("UserIsOnline", false);
                    vars.Add("IsOnline", translator.GetTranslatedString("Offline"));
                }
            }
            else
            {
                vars.Add("OnlineLocation", "");
                vars.Add("UserIsOnline", false);
                vars.Add("IsOnline", translator.GetTranslatedString("Offline"));
            }

            // Menu Profile
            vars.Add("MenuProfileTitle", translator.GetTranslatedString("MenuProfileTitle"));
            vars.Add("MenuGroupTitle", translator.GetTranslatedString("MenuGroupTitle"));
            vars.Add("MenuPicksTitle", translator.GetTranslatedString("MenuPicksTitle"));

            vars.Add("UserProfileFor", translator.GetTranslatedString("UserProfileFor"));
            vars.Add("ResidentSince", translator.GetTranslatedString("ResidentSince"));
            vars.Add("AccountType", translator.GetTranslatedString("AccountType"));
            vars.Add("PartnersName", translator.GetTranslatedString("PartnersName"));
            vars.Add("AboutMe", translator.GetTranslatedString("AboutMe"));
            vars.Add("IsOnlineText", translator.GetTranslatedString("IsOnlineText"));
            vars.Add("OnlineLocationText", translator.GetTranslatedString("OnlineLocationText"));

            // Style Switcher
            vars.Add("styles1", translator.GetTranslatedString("styles1"));
            vars.Add("styles2", translator.GetTranslatedString("styles2"));
            vars.Add("styles3", translator.GetTranslatedString("styles3"));
            vars.Add("styles4", translator.GetTranslatedString("styles4"));
            vars.Add("styles5", translator.GetTranslatedString("styles5"));

            vars.Add("StyleSwitcherStylesText", translator.GetTranslatedString("StyleSwitcherStylesText"));
            vars.Add("StyleSwitcherLanguagesText", translator.GetTranslatedString("StyleSwitcherLanguagesText"));
            vars.Add("StyleSwitcherChoiceText", translator.GetTranslatedString("StyleSwitcherChoiceText"));

            // Language Switcher
            vars.Add("en", translator.GetTranslatedString("en"));
            vars.Add("fr", translator.GetTranslatedString("fr"));
            vars.Add("de", translator.GetTranslatedString("de"));
            vars.Add("it", translator.GetTranslatedString("it"));
            vars.Add("es", translator.GetTranslatedString("es"));

            IGenericsConnector generics = Framework.Utilities.DataManager.RequestPlugin<IGenericsConnector>();
            var settings = generics.GetGeneric<GridSettings>(UUID.Zero, "WebSettings", "Settings");

            vars.Add("ShowLanguageTranslatorBar", !settings.HideLanguageTranslatorBar);
            vars.Add("ShowStyleBar", !settings.HideStyleBar);

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