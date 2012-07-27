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
            var settings = generics.GetGeneric<GridSettings>(UUID.Zero, "WebSettings", "Settings");
            GridPage rootPage = generics.GetGeneric<GridPage>(UUID.Zero, "WebPages", "Root");
            rootPage.Children.Sort((a, b) => a.MenuPosition.CompareTo(b.MenuPosition));


            #region Form submission hack

            if (requestParameters.Count > 0 && httpRequest.Query.ContainsKey("page"))
            {
                string page = httpRequest.Query["page"].ToString();
                GridPage submitPage = rootPage.GetPage(page);

                var submitWebPage = webInterface.GetPage("html/" + submitPage.Location);
                if (submitWebPage != null)
                {
                    var submitVars = submitWebPage.Fill(webInterface, "html/" + submitPage.Location, httpRequest, httpResponse, requestParameters, translator);
                    if (httpResponse.StatusCode == 200)
                        webInterface.CookieLockPageVars("html/" + submitPage.Location, submitVars, httpResponse);
                    else
                        return vars;//It redirected
                }
            }

            #endregion

            foreach (GridPage page in rootPage.Children)
            {
                if (page.LoggedOutRequired && Authenticator.CheckAuthentication(httpRequest))
                    continue;
                if (page.LoggedInRequired && !Authenticator.CheckAuthentication(httpRequest))
                    continue;
                if (page.AdminRequired && !Authenticator.CheckAdminAuthentication(httpRequest, page.AdminLevelRequired))
                    continue;

                List<Dictionary<string, object>> childPages = new List<Dictionary<string, object>>();
                page.Children.Sort((a, b) => a.MenuPosition.CompareTo(b.MenuPosition));
                //page.Children.Add(page);
                foreach (GridPage childPage in page.Children)
                {
                    if (childPage.LoggedOutRequired && Authenticator.CheckAuthentication(httpRequest))
                        continue;
                    if (childPage.LoggedInRequired && !Authenticator.CheckAuthentication(httpRequest))
                        continue;
                    if (childPage.AdminRequired && !Authenticator.CheckAdminAuthentication(httpRequest, childPage.AdminLevelRequired))
                        continue;

                    childPages.Add(new Dictionary<string, object> {
                        { "ChildMenuItemID", childPage.MenuID },
                        { "ChildShowInMenu", childPage.ShowInMenu },
                        { "ChildMenuItemLocation", childPage.Location }, 
                        { "ChildMenuItemTitleHelp", GetTranslatedString(translator, childPage.MenuToolTip, childPage, true) },
                        { "ChildMenuItemTitle", GetTranslatedString(translator, childPage.MenuTitle, childPage, false) } });

                    //Add one for menu.js
                    pages.Add(new Dictionary<string, object> {
                        { "MenuItemID", childPage.MenuID },
                        { "ShowInMenu", false },
                        { "MenuItemLocation", childPage.Location } });
                }

                pages.Add(new Dictionary<string, object> { { "MenuItemID", page.MenuID }, 
                    { "ShowInMenu", page.ShowInMenu },
                    { "HasChildren", page.Children.Count > 0 },
                    { "ChildrenMenuItems", childPages },
                    { "MenuItemLocation", page.Location }, 
                    { "MenuItemTitleHelp", GetTranslatedString(translator, page.MenuToolTip, page, true) },
                    { "MenuItemTitle", GetTranslatedString(translator, page.MenuTitle, page, false) } });
            }
            vars.Add("MenuItems", pages);

            #endregion

            // Tooltips Urls
            vars.Add("TooltipsWelcomeScreen", translator.GetTranslatedString("TooltipsWelcomeScreen"));
            vars.Add("TooltipsWorldMap", translator.GetTranslatedString("TooltipsWorldMap"));

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

            // Index Page
            vars.Add("HomeText", translator.GetTranslatedString("HomeText"));
            vars.Add("HomeTextWelcome", translator.GetTranslatedString("HomeTextWelcome"));
            vars.Add("HomeTextTips", translator.GetTranslatedString("HomeTextTips"));
            vars.Add("WelcomeScreen", translator.GetTranslatedString("WelcomeScreen"));
            vars.Add("WelcomeToText", translator.GetTranslatedString("WelcomeToText"));

            if (PagesMigrator.RequiresUpdate() && PagesMigrator.CheckWhetherIgnoredVersionUpdate(settings.LastPagesVersionUpdateIgnored))
                vars.Add("PagesUpdateRequired", translator.GetTranslatedString("Pages") + " " + translator.GetTranslatedString("DefaultsUpdated"));
            else
                vars.Add("PagesUpdateRequired", "");
            if (SettingsMigrator.RequiresUpdate() && SettingsMigrator.CheckWhetherIgnoredVersionUpdate(settings.LastSettingsVersionUpdateIgnored))
                vars.Add("SettingsUpdateRequired", translator.GetTranslatedString("Settings") + " " + translator.GetTranslatedString("DefaultsUpdated"));
            else
                vars.Add("SettingsUpdateRequired", "");
            
            vars.Add("ShowLanguageTranslatorBar", !settings.HideLanguageTranslatorBar);
            vars.Add("ShowStyleBar", !settings.HideStyleBar);

            vars.Add("Maintenance", false);
            vars.Add("NoMaintenance", true);
            return vars;
        }

        private string GetTranslatedString(ITranslator translator, string name, GridPage page, bool isTooltip)
        {
            string retVal = translator.GetTranslatedString(name);
            if (retVal == "UNKNOWN CHARACTER")
                return isTooltip ? page.MenuToolTip : page.MenuTitle;
            return retVal;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}