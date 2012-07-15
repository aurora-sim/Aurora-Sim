using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;
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

            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            GridPage rootPage = generics.GetGeneric<GridPage>(UUID.Zero, "WebPages", "Root");
            rootPage.Children.Sort((a, b) => a.MenuPosition.CompareTo(b.MenuPosition));

            foreach (GridPage page in rootPage.Children)
            {
                if (page.LoggedOutRequired && Authenticator.CheckAuthentication(httpRequest))
                    continue;
                if (page.LoggedInRequired && !Authenticator.CheckAuthentication(httpRequest))
                    continue;
                if (page.AdminRequired && !Authenticator.CheckAdminAuthentication(httpRequest))
                    continue;

                pages.Add(new Dictionary<string, object> { { "MenuItemID", page.MenuID }, 
                    { "ShowInMenu", page.ShowInMenu },
                    { "MenuItemLocation", page.Location }, 
                    { "MenuItemTitleHelp", translator.GetTranslatedString(page.MenuToolTip) },
                    { "MenuItemTitle", translator.GetTranslatedString(page.MenuTitle) } });
            }
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