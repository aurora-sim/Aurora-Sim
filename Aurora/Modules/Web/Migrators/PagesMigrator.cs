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
        public static readonly string Schema = "WebPages";
        private static GridPage _rootPage;
        public static readonly uint CurrentVersion = 5;

        private static void InitializeDefaults()
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
                MenuToolTip = "TooltipsMenuWorld",
                Children = new List<GridPage>
                {
                    new GridPage
                    {
                        ShowInMenu = true,
                        MenuID = "world",
                        Location = "world.html",
                        MenuPosition = 2,
                        MenuTitle = "MenuWorldMap",
                        MenuToolTip = "TooltipsMenuWorldMap"
                    },
                    new GridPage
                    {
                        ShowInMenu = true,
                        MenuID = "region_search",
                        Location = "region_search.html",
                        MenuPosition = 2,
                        MenuTitle = "MenuRegionSearch",
                        MenuToolTip = "TooltipsMenuRegionSearch"
                    },
                    new GridPage
                    {
                        ShowInMenu = true,
                        MenuID = "region_list",
                        Location = "region_list.html",
                        MenuPosition = 2,
                        MenuTitle = "MenuRegion",
                        MenuToolTip = "TooltipsMenuRegion"
                    }
                }
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                MenuID = "users",
                Location = "user_search.html",
                MenuPosition = 3,
                MenuTitle = "MenuUser",
                MenuToolTip = "TooltipsMenuUser",
                Children = new List<GridPage> {
                    new GridPage
                    {
                        ShowInMenu = true,
                        LoggedInRequired = true,
                        MenuID = "change_user_information",
                        Location = "change_user_information.html",
                        MenuPosition = 1,
                        MenuTitle = "MenuChangeUserInformation",
                        MenuToolTip = "TooltipsMenuChangeUserInformation"
                    },
                    new GridPage
                    {
                        ShowInMenu = true,
                        MenuID = "online_users",
                        Location = "online_users.html",
                        MenuPosition = 2,
                        MenuTitle = "MenuOnlineUsers",
                        MenuToolTip = "TooltipsMenuOnlineUsers"
                    },
                    new GridPage
                    {
                        ShowInMenu = true,
                        MenuID = "user_search",
                        Location = "user_search.html",
                        MenuPosition = 3,
                        MenuTitle = "MenuUserSearch",
                        MenuToolTip = "TooltipsMenuUserSearch"
                    } }
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                MenuID = "help",
                Location = "help.html",
                MenuPosition = 5,
                MenuTitle = "MenuHelp",
                MenuToolTip = "TooltipsMenuHelp",
                Children = new List<GridPage> { new GridPage
                    {
                        ShowInMenu = true,
                        MenuID = "chat",
                        Location = "chat.html",
                        MenuPosition = 4,
                        MenuTitle = "MenuChat",
                        MenuToolTip = "TooltipsMenuChat"
                    } }
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
                MenuPosition = 8,
                MenuTitle = "MenuLogin",
                MenuToolTip = "TooltipsMenuLogin",
                Children = new List<GridPage>
                {
                    new GridPage
                    {
                        ShowInMenu = true,
                        MenuID = "forgot_pass",
                        Location = "forgot_pass.html",
                        MenuPosition = 1,
                        MenuTitle = "MenuForgotPass",
                        MenuToolTip = "TooltipsMenuForgotPass"
                    }
                }
            });
            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = true,
                AdminRequired = true,
                MenuID = "manager",
                Location = "admin/manager.html",
                MenuPosition = 9,
                MenuTitle = "MenuManager",
                MenuToolTip = "TooltipsMenuManager",
                Children = new List<GridPage>
                {
                    new GridPage
                    {
                        ShowInMenu = true,
                        AdminRequired = true,
                        MenuID = "news_manager",
                        Location = "admin/news_manager.html",
                        MenuPosition = 8,
                        MenuTitle = "MenuNewsManager",
                        MenuToolTip = "TooltipsMenuNewsManager"
                    },
                    new GridPage
                    {
                        ShowInMenu = true,
                        AdminRequired = true,
                        MenuID = "user_search",
                        Location = "user_search.html",
                        MenuPosition = 8,
                        MenuTitle = "MenuUserManager",
                        MenuToolTip = "TooltipsMenuUserManager"
                    },
                    new GridPage
                    {
                        ShowInMenu = true,
                        AdminRequired = true,
                        MenuID = "factory_reset",
                        Location = "admin/factory_reset.html",
                        MenuPosition = 8,
                        MenuTitle = "MenuFactoryReset",
                        MenuToolTip = "TooltipsMenuFactoryReset"
                    },
                    new GridPage
                    {
                        ShowInMenu = true,
                        AdminRequired = true,
                        MenuID = "page_manager",
                        Location = "admin/page_manager.html",
                        MenuPosition = 8,
                        MenuTitle = "MenuPageManager",
                        MenuToolTip = "TooltipsMenuPageManager"
                    },
                    new GridPage
                    {
                        ShowInMenu = true,
                        AdminRequired = true,
                        MenuID = "settings_manager",
                        Location = "admin/settings_manager.html",
                        MenuPosition = 8,
                        MenuTitle = "MenuSettingsManager",
                        MenuToolTip = "TooltipsMenuSettingsManager"
                    },
                    new GridPage
                    {
                        ShowInMenu = true,
                        AdminRequired = true,
                        MenuID = "welcomescreen_manager",
                        Location = "admin/welcomescreen_manager.html",
                        MenuPosition = 8,
                        MenuTitle = "MenuWelcomeScreenManager",
                        MenuToolTip = "TooltipsMenuWelcomeScreenManager"
                    }
                }
            });


            _rootPage.Children.Add(new GridPage 
            {
                MenuID = "add_news",
                ShowInMenu = false,
                AdminRequired = true,
                MenuPosition = 8,
                Location = "admin/add_news.html"
            });
            _rootPage.Children.Add(new GridPage
            {
                MenuID = "edit_news",
                ShowInMenu = false,
                AdminRequired = true,
                MenuPosition = 8,
                Location = "admin/edit_news.html"
            });
            _rootPage.Children.Add(new GridPage
            {
                MenuID = "edit_user",
                ShowInMenu = false,
                AdminRequired = true,
                MenuPosition = 8,
                Location = "admin/edit_user.html"
            });

            _rootPage.Children.Add(new GridPage
            {
                ShowInMenu = false,
                MenuID = "news_info",
                Location = "news.html"
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

        public static bool RequiresUpdate()
        {
            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            OSDWrapper version = generics.GetGeneric<OSDWrapper>(UUID.Zero, Schema + "Version", "");
            return version == null || version.Info.AsInteger() < CurrentVersion;
        }

        public static uint GetVersion()
        {
            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            OSDWrapper version = generics.GetGeneric<OSDWrapper>(UUID.Zero, Schema + "Version", "");
            return version == null ? 0 : (uint)version.Info.AsInteger();
        }

        public static bool RequiresInitialUpdate()
        {
            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            OSDWrapper version = generics.GetGeneric<OSDWrapper>(UUID.Zero, Schema + "Version", "");
            return version == null || version.Info.AsInteger() < 1;
        }

        public static void ResetToDefaults()
        {
            InitializeDefaults();
            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            
            //Remove all pages
            generics.RemoveGeneric(UUID.Zero, Schema);

            generics.AddGeneric(UUID.Zero, Schema, "Root", _rootPage.ToOSD());
            generics.AddGeneric(UUID.Zero, Schema + "Version", "", new OSDWrapper { Info = CurrentVersion }.ToOSD());
        }

        public static bool CheckWhetherIgnoredVersionUpdate(uint version)
        {
            return version != PagesMigrator.CurrentVersion;
        }
    }
}
