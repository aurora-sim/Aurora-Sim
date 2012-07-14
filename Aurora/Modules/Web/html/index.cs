using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework.Servers.HttpServer;

namespace Aurora.Modules.Web
{
    public class IndexMain : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
				{
				    "html/index.html",
                    "html/javascripts/menu.js"
				};
            }
        }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
			var vars = new Dictionary<string, object>();

            #region Find pages

            List<Dictionary<string, object>> pages = new List<Dictionary<string, object>>();

            pages.Add(new Dictionary<string, object> { { "MenuItemID", "home" }, 
                { "ShowInMenu", true },
                { "MenuItemLocation", "index.html #content" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuHome") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuHome") } });

            pages.Add(new Dictionary<string, object> { { "MenuItemID", "register" }, 
                { "ShowInMenu", true },
                { "MenuItemLocation", "register.html" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuRegister") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuRegister") } });

            pages.Add(new Dictionary<string, object> { { "MenuItemID", "news" }, 
                { "ShowInMenu", true },
                { "MenuItemLocation", "news_list.html" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuNews") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuNews") } });

            pages.Add(new Dictionary<string, object> { { "MenuItemID", "world" }, 
                { "ShowInMenu", true },
                { "MenuItemLocation", "world.html" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuWorld") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuWorld") } });

            pages.Add(new Dictionary<string, object> { { "MenuItemID", "region_list" }, 
                { "ShowInMenu", true },
                { "MenuItemLocation", "region_list.html" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuRegion") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuRegion") } });

            pages.Add(new Dictionary<string, object> { { "MenuItemID", "online_users" }, 
                { "ShowInMenu", true },
                { "MenuItemLocation", "online_users.html" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuUser") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuUser") } });

            pages.Add(new Dictionary<string, object> { { "MenuItemID", "chat" }, 
                { "ShowInMenu", true },
                { "MenuItemLocation", "chat.html" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuChat") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuChat") } });

            pages.Add(new Dictionary<string, object> { { "MenuItemID", "help" }, 
                { "ShowInMenu", true },
                { "MenuItemLocation", "help.html" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuHelp") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuHelp") } });



            pages.Add(new Dictionary<string, object> { { "MenuItemID", "tweets" }, 
                { "ShowInMenu", false },
                { "MenuItemLocation", "tweets.html" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuTweets") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuTweets") } });

            pages.Add(new Dictionary<string, object> { { "MenuItemID", "agent_info" }, 
                { "ShowInMenu", false },
                { "MenuItemLocation", "agent_info.html" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuAgentInfo") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuAgentInfo") } });

            pages.Add(new Dictionary<string, object> { { "MenuItemID", "region_info" }, 
                { "ShowInMenu", false },
                { "MenuItemLocation", "region_info.html" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuRegionInfo") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuRegionInfo") } });

            #region Authenticated Pages

            if (Authenticator.CheckAuthentication(httpRequest))
            {
                pages.Add(new Dictionary<string, object> { { "MenuItemID", "changeuserinfo" }, 
                    { "ShowInMenu", true },
                    { "MenuItemLocation", "change_user_information.html" }, 
                    { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuChangeUserInformation") },
                    { "MenuItemTitle", translator.GetTranslatedString("MenuChangeUserInformation") } });

                pages.Add(new Dictionary<string, object> { { "MenuItemID", "logout" }, 
                    { "ShowInMenu", true },
                    { "MenuItemLocation", "logout.html" }, 
                    { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuLogout") },
                    { "MenuItemTitle", translator.GetTranslatedString("MenuLogout") } });
            }
            #endregion
            #region Non Authenticated Pages
            else
            {
                pages.Add(new Dictionary<string, object> { { "MenuItemID", "login" }, 
                    { "ShowInMenu", true },
                    { "MenuItemLocation", "login.html" }, 
                    { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuLogin") },
                    { "MenuItemTitle", translator.GetTranslatedString("MenuLogin") } });
            }
            #endregion

            vars.Add("MenuItems", pages);
            #endregion

            // Menu Buttons
			vars.Add("MenuHome", translator.GetTranslatedString("MenuHome"));
			vars.Add("MenuLogin", translator.GetTranslatedString("MenuLogin"));
			vars.Add("MenuRegister", translator.GetTranslatedString("MenuRegister"));
			vars.Add("MenuForgotPass", translator.GetTranslatedString("MenuForgotPass"));
			vars.Add("MenuNews", translator.GetTranslatedString("MenuNews"));
			vars.Add("MenuWorld", translator.GetTranslatedString("MenuWorld"));
			vars.Add("MenuRegion", translator.GetTranslatedString("MenuRegion"));
			vars.Add("MenuUser", translator.GetTranslatedString("MenuUser"));
			vars.Add("MenuChat", translator.GetTranslatedString("MenuChat"));
			vars.Add("MenuHelp", translator.GetTranslatedString("MenuHelp"));
			
			// Tooltips Menu Buttons
			vars.Add("TooltipsMenuHome", translator.GetTranslatedString("TooltipsMenuHome"));
			vars.Add("TooltipsMenuLogin", translator.GetTranslatedString("TooltipsMenuLogin"));
			vars.Add("TooltipsMenuRegister", translator.GetTranslatedString("TooltipsMenuRegister"));
			vars.Add("TooltipsMenuForgotPass", translator.GetTranslatedString("TooltipsMenuForgotPass"));
			vars.Add("TooltipsMenuNews", translator.GetTranslatedString("TooltipsMenuNews"));
			vars.Add("TooltipsMenuWorld", translator.GetTranslatedString("TooltipsMenuWorld"));
			vars.Add("TooltipsMenuRegion", translator.GetTranslatedString("TooltipsMenuRegion"));
			vars.Add("TooltipsMenuUser", translator.GetTranslatedString("TooltipsMenuUser"));
			vars.Add("TooltipsMenuChat", translator.GetTranslatedString("TooltipsMenuChat"));
			vars.Add("TooltipsMenuHelp", translator.GetTranslatedString("TooltipsMenuHelp"));
			
			// Tooltips Urls
			vars.Add("TooltipsWelcomeScreen", translator.GetTranslatedString("TooltipsWelcomeScreen"));
			vars.Add("TooltipsWorldMap", translator.GetTranslatedString("TooltipsWorldMap"));

			// Style Switcher
			vars.Add("styles1", translator.GetTranslatedString("styles1"));
			vars.Add("styles2", translator.GetTranslatedString("styles2"));
			vars.Add("styles3", translator.GetTranslatedString("styles3"));
			vars.Add("styles4", translator.GetTranslatedString("styles4"));
			vars.Add("styles5", translator.GetTranslatedString("styles5"));
			
			// Index Page
			vars.Add("HomeText", translator.GetTranslatedString("HomeText"));
			vars.Add("HomeTextWelcome", translator.GetTranslatedString("HomeTextWelcome"));
            vars.Add("HomeTextTips", translator.GetTranslatedString("HomeTextTips"));
            vars.Add("WelcomeScreen", translator.GetTranslatedString("WelcomeScreen"));
            vars.Add("WelcomeToText", translator.GetTranslatedString("WelcomeToText"));

            vars.Add("Maintenance", false);
            vars.Add("NoMaintenance", true);
			return vars;			
        }
    }
}
