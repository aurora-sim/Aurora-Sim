using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;
using OpenMetaverse;

namespace Aurora.Modules.Web
{
    internal class PagesMigrator
    {
        public const string Schema = "WebPages";
        public readonly GridPage _rootPage;
        public const uint CurrentVersion = 1;

        public PagesMigrator()
        {
            _rootPage = new GridPage();

            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                MenuID = "home",
                Location = "index.html #content",
                MenuPosition = 0,
                MenuTitle = "MenuHome",
                MenuToolTip = "TooltipsMenuHome"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                MenuID = "news",
                Location = "news_list.html",
                MenuPosition = 1,
                MenuTitle = "MenuNews",
                MenuToolTip = "TooltipsMenuNews"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                MenuID = "world",
                Location = "world.html",
                MenuPosition = 2,
                MenuTitle = "MenuWorld",
                MenuToolTip = "TooltipsMenuWorld"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                MenuID = "region_list",
                Location = "region_list.html",
                MenuPosition = 2,
                MenuTitle = "MenuRegion",
                MenuToolTip = "TooltipsMenuRegion"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                MenuID = "region_list",
                Location = "region_list.html",
                MenuPosition = 3,
                MenuTitle = "MenuRegion",
                MenuToolTip = "TooltipsMenuRegion"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                MenuID = "online_users",
                Location = "online_users.html",
                MenuPosition = 4,
                MenuTitle = "MenuUser",
                MenuToolTip = "TooltipsMenuUser"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                MenuID = "chat",
                Location = "chat.html",
                MenuPosition = 5,
                MenuTitle = "MenuChat",
                MenuToolTip = "TooltipsMenuChat"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                MenuID = "help",
                Location = "help.html",
                MenuPosition = 6,
                MenuTitle = "MenuHelp",
                MenuToolTip = "TooltipsMenuHelp"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                MenuID = "help",
                Location = "help.html",
                MenuPosition = 6,
                MenuTitle = "MenuHelp",
                MenuToolTip = "TooltipsMenuHelp"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                LoggedInRequired = true,
                MenuID = "change_user_information",
                Location = "change_user_information.html",
                MenuPosition = 1,
                MenuTitle = "MenuChangeUserInformation",
                MenuToolTip = "TooltipsMenuChangeUserInformation"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                LoggedInRequired = true,
                MenuID = "logout",
                Location = "logout.html",
                MenuPosition = 7,
                MenuTitle = "MenuLogout",
                MenuToolTip = "TooltipsMenuLogout"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                LoggedOutRequired = true,
                MenuID = "register",
                Location = "register.html",
                MenuPosition = 7,
                MenuTitle = "MenuRegister",
                MenuToolTip = "TooltipsMenuRegister"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                LoggedOutRequired = true,
                MenuID = "login",
                Location = "login.html",
                MenuPosition = 7,
                MenuTitle = "MenuLogin",
                MenuToolTip = "TooltipsMenuLogin"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                AdminRequired = true,
                MenuID = "welcomescreen_manager",
                Location = "admin/welcomescreen_manager.html",
                MenuPosition = 8,
                MenuTitle = "MenuWelcomeScreenManager",
                MenuToolTip = "TooltipsMenuWelcomeScreenManager"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                AdminRequired = true,
                MenuID = "news_manager",
                Location = "admin/news_manager.html",
                MenuPosition = 8,
                MenuTitle = "MenuNewsManager",
                MenuToolTip = "TooltipsMenuNewsManager"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                AdminRequired = true,
                MenuID = "user_search",
                Location = "user_search.html",
                MenuPosition = 8,
                MenuTitle = "MenuUserManager",
                MenuToolTip = "TooltipsMenuUserManager"
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                AdminRequired = true,
                MenuID = "factory_reset",
                Location = "admin/factory_reset.html",
                MenuPosition = 8,
                MenuTitle = "MenuFactoryReset",
                MenuToolTip = "TooltipsMenuFactoryReset"
            });


            //Things added, but not used
            /*pages.Add(new Dictionary<string, object> { { "MenuItemID", "tweets" }, 
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
            pages.Add(new Dictionary<string, object> { { "MenuItemID", "add_news" }, 
                { "ShowInMenu", false },
                { "MenuItemLocation", "admin/add_news.html" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuNewsManager") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuNewsManager") } });
            pages.Add(new Dictionary<string, object> { { "MenuItemID", "edit_news" }, 
                { "ShowInMenu", false },
                { "MenuItemLocation", "admin/edit_news.html" }, 
                { "MenuItemTitleHelp", translator.GetTranslatedString("TooltipsMenuNewsManager") },
                { "MenuItemTitle", translator.GetTranslatedString("MenuNewsManager") } });*/
        }

        public bool RequiresUpdate()
        {
            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            OSDWrapper version = generics.GetGeneric<OSDWrapper>(UUID.Zero, Schema + "Version", "");
            return version == null || version.Info.AsInteger() < CurrentVersion;
        }

        public void ResetToDefaults()
        {
            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            
            //Remove all pages
            generics.RemoveGeneric(UUID.Zero, Schema);

            generics.AddGeneric(UUID.Zero, Schema, "Root", _rootPage.ToOSD());
            generics.AddGeneric(UUID.Zero, Schema + "Version", "", new OSDWrapper { Info = CurrentVersion }.ToOSD());
        }
    }
}
