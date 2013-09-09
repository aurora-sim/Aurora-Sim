namespace Aurora.Modules.Web.Translators
{
    public class DutchTranslation : ITranslator
    {
        public string LanguageName
        {
            get { return "nl"; }
        }

        public string GetTranslatedString(string key)
        {
            switch (key)
            {
                case "GridStatus":
                    return "Grid Status";
                case "Online":
                    return "Online";
                case "Offline":
                    return "Offline";
                case "TotalUserCount":
                    return "Totale Gebruikers";
                case "TotalRegionCount":
                    return "Totale Regios";
                case "UniqueVisitors":
                    return "Unieke Bezeoeker per 30 dagen";
                case "OnlineNow":
                    return "Nu online";
                case "HyperGrid":
                    return "HyperGrid (HG)";
                case "Voice":
                    return "Voice";
                case "Currency":
                    return "Currency";
                case "Disabled":
                    return "Uitgeschakeld";
                case "Enabled":
                    return "Ingeschakeld";
                case "News":
                    return "Nieuws";
                case "Region":
                    return "Regio";
                case "Login":
                    return "Login";
                case "UserName":
                case "UserNameText":
                    return "Gebruikersnaam";
                case "Password":
                case "PasswordText":
                    return "Wachtwoord";
                case "PasswordConfirmation":
                    return "Wachtwoord Bevestiging";
                case "ForgotPassword":
                    return "Wachtwoord vergeten?";
                case "Submit":
                    return "Verzenden";
                case "TypeUserNameToConfirm":
                    return "Geef de gebruikersnaam van dit account in om te bevestigen dat je dit account wilt verwijderen";

                case "SpecialWindowTitleText":
                    return "Special Info Window Titel";
                case "SpecialWindowTextText":
                    return "Special Info Window Tekst";
                case "SpecialWindowColorText":
                    return "Special Info Window Kleur";
                case "SpecialWindowStatusText":
                    return "Special Info Window Status";
                case "WelcomeScreenManagerFor":
                    return "Welkoms Scherm Manager voor";
                case "ChangesSavedSuccessfully":
                    return "Wijzigingen succesvol opgeslagen";


                case "AvatarNameText":
                    return "Avatar Naam";
                case "AvatarScopeText":
                    return "Avatar Scope ID";
                case "FirstNameText":
                    return "Uw Voornaam";
                case "LastNameText":
                    return "Uw Achternaam";
                case "UserAddressText":
                    return "Uw Adres";
                case "UserZipText":
                    return "Uw Postcode";
                case "UserCityText":
                    return "Uw Stad";
                case "UserCountryText":
                    return "Uw Land";
                case "UserDOBText":
                    return "Uw Geboortedatum (Maand Dag Jaar)";
                case "UserEmailText":
                    return "Uw Email";
                case "RegistrationText":
                    return "Avatar registratie";
                case "RegistrationsDisabled":
                    return "Registraties zijn op dit moment gesloten, probeert u het later nog eens.";
                case "TermsOfServiceText":
                    return "Terms of Service";
                case "TermsOfServiceAccept":
                    return "Accepteer u deze Terms of Service zoals boven beschreven?";
                case "Accept":
                    return "Accepteren";

                    // news
                case "OpenNewsManager":
                    return "Open de Nieuws manager";
                case "NewsManager":
                    return "Nieuws Manager";
                case "EditNewsItem":
                    return "Bewerk nieuws";
                case "AddNewsItem":
                    return "Voeg nieuw nieuws bericht toe";
                case "DeleteNewsItem":
                    return "Verwijder nieuws item";
                case "NewsDateText":
                    return "Nieuws Datum";
                case "NewsTitleText":
                    return "Nieuws Titel";
                case "NewsItemTitle":
                    return "Nieuws Item Titel";
                case "NewsItemText":
                    return "Nieuws Item Tekst";
                case "AddNewsText":
                    return "Nieuws toevoegen";
                case "DeleteNewsText":
                    return "Verwijder Nieuws";
                case "EditNewsText":
                    return "Bewerk Nieuws";
                case "UserProfileFor":
                    return "User Profiel Voor";
                case "UsersGroupsText":
                    return "Groepen";
                case "UsersPicksText":
                    return "Picks for";
                case "ResidentSince":
                    return "Resident Since";
                case "AccountType":
                    return "Account Type";
                case "PartnersName":
                    return "Partner's Naam";
                case "AboutMe":
                    return "Over Mij";
                case "IsOnlineText":
                    return "User Status";
                case "OnlineLocationText":
                    return "User Locatie";

                case "RegionInformationText":
                    return "Region Informatie";
                case "OwnerNameText":
                    return "Owner Naam";
                case "RegionLocationText":
                    return "Region Locatie";
                case "RegionSizeText":
                    return "Region Grootte";
                case "RegionNameText":
                    return "Region Naam";
                case "RegionTypeText":
                    return "Region Type";
                case "ParcelsInRegionText":
                    return "Parcels In Region";
                case "ParcelNameText":
                    return "Parcel Naam";
                case "ParcelOwnerText":
                    return "Parcel Owner's Naam";

                    // Region Page
                case "RegionInfoText":
                    return "Region Info";
                case "RegionListText":
                    return "Region List";
                case "RegionLocXText":
                    return "Region X";
                case "RegionLocYText":
                    return "Region Y";
                case "SortByLocX":
                    return "Sort By Region X";
                case "SortByLocY":
                    return "Sort By Region Y";
                case "SortByName":
                    return "Sort By Region Name";
                case "RegionMoreInfo":
                    return "More Informatie";
                case "RegionMoreInfoTooltips":
                    return "More info over";
                case "FirstText":
                    return "Eerste";
                case "BackText":
                    return "Terug";
                case "NextText":
                    return "Volgende";
                case "LastText":
                    return "Laatste";
                case "CurrentPageText":
                    return "Current Page";
                case "MoreInfoText":
                    return "Meer Info";
                case "OnlineUsersText":
                    return "Online Users";
                case "RegionOnlineText":
                    return "Region Status";
                case "NumberOfUsersInRegionText":
                    return "Number of Users in region";

                    // Menu Buttons
                case "MenuHome":
                    return "Home";
                case "MenuLogin":
                    return "Login";
                case "MenuLogout":
                    return "Logout";
                case "MenuRegister":
                    return "Registeer";
                case "MenuForgotPass":
                    return "Wachtwoord vergeten";
                case "MenuNews":
                    return "Nieuws";
                case "MenuWorld":
                    return "Wereld";
                case "MenuWorldMap":
                    return "Wereld Map";
                case "MenuRegion":
                    return "Region List";
                case "MenuUser":
                    return "Gebruiker";
                case "MenuOnlineUsers":
                    return "Online Gebruikers";
                case "MenuUserSearch":
                    return "Zoek Gebruiker";
                case "MenuRegionSearch":
                    return "Region Search";
                case "MenuChat":
                    return "Chat";
                case "MenuHelp":
                    return "Help";
                case "MenuViewerHelp":
                    return "Viewer Help";
                case "MenuChangeUserInformation":
                    return "Wijzig User Informatie";
                case "MenuWelcomeScreenManager":
                    return "Welcome Screen Manager";
                case "MenuNewsManager":
                    return "Nieuws Manager";
                case "MenuUserManager":
                    return "User Manager";
                case "MenuFactoryReset":
                    return "Factory Reset";
                case "ResetMenuInfoText":
                    return "Reset de menu items terug naar de default waardes";
                case "ResetSettingsInfoText":
                    return "Reset de Web Interface terug naar de default waardes";
                case "MenuPageManager":
                    return "Page Manager";
                case "MenuSettingsManager":
                    return "Settings Manager";
                case "MenuManager":
                    return "Admin";

                    // Tooltips Menu Buttons
                case "TooltipsMenuHome":
                    return "Home";
                case "TooltipsMenuLogin":
                    return "Login";
                case "TooltipsMenuLogout":
                    return "Logout";
                case "TooltipsMenuRegister":
                    return "Registeer";
                case "TooltipsMenuForgotPass":
                    return "Wachtwoord vergeten";
                case "TooltipsMenuNews":
                    return "Nieuws";
                case "TooltipsMenuWorld":
                    return "Wereld";
                case "TooltipsMenuWorldMap":
                    return "Wereld Map";
                case "TooltipsMenuRegion":
                    return "Region List";
                case "TooltipsMenuUser":
                    return "User";
                case "TooltipsMenuOnlineUsers":
                    return "Online Users";
                case "TooltipsMenuUserSearch":
                    return "User Search";
                case "TooltipsMenuRegionSearch":
                    return "Region Search";
                case "TooltipsMenuChat":
                    return "Chat";
                case "TooltipsMenuViewerHelp":
                    return "Viewer Help";
                case "TooltipsMenuHelp":
                    return "Help";
                case "TooltipsMenuChangeUserInformation":
                    return "Change User Information";
                case "TooltipsMenuWelcomeScreenManager":
                    return "Welcome Screen Manager";
                case "TooltipsMenuNewsManager":
                    return "Nieuws Manager";
                case "TooltipsMenuUserManager":
                    return "User Manager";
                case "TooltipsMenuFactoryReset":
                    return "Factory Reset";
                case "TooltipsMenuPageManager":
                    return "Page Manager";
                case "TooltipsMenuSettingsManager":
                    return "Settings Manager";
                case "TooltipsMenuManager":
                    return "Admin Management";

                    // Menu Region
                case "MenuRegionTitle":
                    return "Region";
                case "MenuParcelTitle":
                    return "Parcel";
                case "MenuOwnerTitle":
                    return "Owner";

                    // Menu Profile
                case "MenuProfileTitle":
                    return "Profile";
                case "MenuGroupTitle":
                    return "Group";
                case "MenuPicksTitle":
                    return "Picks";

                    // Urls
                case "WelcomeScreen":
                    return "Welcome Screen";

                    // Tooltips Urls
                case "TooltipsWelcomeScreen":
                    return "Welcome Screen";
                case "TooltipsWorldMap":
                    return "World Map";

                    // Style Switcher
                case "styles1":
                    return "Default Minimalist";
                case "styles2":
                    return "Light Degarde";
                case "styles3":
                    return "Blue Night";
                case "styles4":
                    return "Dark Degrade";
                case "styles5":
                    return "Luminus";

                case "StyleSwitcherStylesText":
                    return "Styles";
                case "StyleSwitcherLanguagesText":
                    return "Languages";
                case "StyleSwitcherChoiceText":
                    return "Choice";

                    // Language Switcher Tooltips
                case "en":
                    return "English";
                case "fr":
                    return "French";
                case "de":
                    return "German";
                case "it":
                    return "Italian";
                case "es":
                    return "Spanish";

                    // Index Page
                case "HomeText":
                    return "Home";
                case "HomeTextWelcome":
                    return "This is our New Virtual World! Join us now, and make a difference!";
                case "HomeTextTips":
                    return "New presentations";
                case "WelcomeToText":
                    return "Welcome to";

                    // World Map Page
                case "WorldMap":
                    return "World Map";
                case "WorldMapText":
                    return "Full Screen";

                    // Chat Page
                case "ChatText":
                    return "Chat Support";

                    // Help Page
                case "HelpText":
                    return "Help";
                case "HelpViewersConfigText":
                    return "Help Viewers Configuration";
                case "AngstormViewer":
                    return "Angstorm Viewer";
                case "VoodooViewer":
                    return "Voodoo Viewer";
                case "AstraViewer":
                    return "Astra Viewer";
                case "ImprudenceViewer":
                    return "Imprudence Viewer";
                case "PhoenixViewer":
                    return "Phoenix Viewer";
                case "SingularityViewer":
                    return "Singularity Viewer";
                case "ZenViewer":
                    return "Zen Viewer";

                    //Logout page
                case "Logout":
                    return "Logout";
                case "LoggedOutSuccessfullyText":
                    return "You have been logged out successfully.";

                    //Change user information page
                case "ChangeUserInformationText":
                    return "Change User Information";
                case "ChangePasswordText":
                    return "Change Password";
                case "NewPasswordText":
                    return "New Password";
                case "NewPasswordConfirmationText":
                    return "New Password (Confirmation)";
                case "ChangeEmailText":
                    return "Change Email Address";
                case "NewEmailText":
                    return "New Email Address";
                case "DeleteUserText":
                    return "Delete My Account";
                case "DeleteText":
                    return "Delete";
                case "DeleteUserInfoText":
                    return
                        "This will remove all information about you in the grid and remove your access to this service. If you wish to continue, enter your name and password and click Delete.";
                case "EditText":
                    return "Edit";
                case "EditUserAccountText":
                    return "Edit User Account";

                    //Maintenance page
                case "WebsiteDownInfoText":
                    return "Website is currently down, please try again soon.";
                case "WebsiteDownText":
                    return "Website offline";

                    //http_404 page
                case "Error404Text":
                    return "Error code";
                case "Error404InfoText":
                    return "404 Page Not Found";
                case "HomePage404Text":
                    return "home page";

                    //http_505 page
                case "Error505Text":
                    return "Error code";
                case "Error505InfoText":
                    return "505 Internal Server Error";
                case "HomePage505Text":
                    return "home page";

                    //user_search page
                case "Search":
                    return "Search";
                case "SearchText":
                    return "Search";
                case "SearchForUserText":
                    return "Search For A User";
                case "UserSearchText":
                    return "User Search";
                case "SearchResultForUserText":
                    return "Search Result For User";

                    //region_search page
                case "SearchForRegionText":
                    return "Search For A Region";
                case "RegionSearchText":
                    return "Region Search";
                case "SearchResultForRegionText":
                    return "Search Result For Region";

                    //Edit user page
                case "AdminDeleteUserText":
                    return "Delete User";
                case "AdminDeleteUserInfoText":
                    return "This deletes the account and destroys all information associated with it.";
                case "BanText":
                    return "Ban";
                case "UnbanText":
                    return "Unban";
                case "AdminTempBanUserText":
                    return "Temp Ban User";
                case "AdminTempBanUserInfoText":
                    return "This blocks the user from logging in for the set amount of time.";
                case "AdminBanUserText":
                    return "Ban User";
                case "AdminBanUserInfoText":
                    return "This blocks the user from logging in until the user is unbanned.";
                case "AdminUnbanUserText":
                    return "Unban User";
                case "AdminUnbanUserInfoText":
                    return "Removes temporary and permanent bans on the user.";
                case "AdminLoginInAsUserText":
                    return "Login as User";
                case "AdminLoginInAsUserInfoText":
                    return
                        "You will be logged out of your admin account, and logged in as this user, and will see everything as they see it.";
                case "TimeUntilUnbannedText":
                    return "Time until user is unbanned";
                case "DaysText":
                    return "Days";
                case "HoursText":
                    return "Hours";
                case "MinutesText":
                    return "Minutes";
                case "EdittingText":
                    return "Editting";
                case "BannedUntilText":
                    return "User banned until:";
                case "KickAUserText":
                    return "Kick User";
                case "KickAUserInfoText":
                    return "Kicks a user from the grid (logs them out within 30 seconds)";
                case "KickMessageText":
                    return "Message To User";
                case "KickUserText":
                    return "Kick User";
                case "MessageAUserText":
                    return "Send User A Message";
                case "MessageAUserInfoText":
                    return "Sends a user a blue-box message (will arrive within 30 seconds)";
                case "MessageUserText":
                    return "Message User";

                    //factory_reset
                case "FactoryReset":
                    return "Factory Reset";
                case "ResetMenuText":
                    return "Reset Menu To Factory Defaults";
                case "ResetSettingsText":
                    return "Reset Web Settings (Settings Manager page) To Factory Defaults";
                case "Reset":
                    return "Reset";
                case "Settings":
                    return "Settings";
                case "Pages":
                    return "Pages";
                case "DefaultsUpdated":
                    return
                        "defaults updated, go to Factory Reset to update or Settings Manager to disable this warning.";

                    //page_manager
                case "PageManager":
                    return "Page Manager";
                case "SaveMenuItemChanges":
                    return "Save Menu Item";
                case "SelectItem":
                    return "Select Item";
                case "DeleteItem":
                    return "Delete Item";
                case "AddItem":
                    return "Add Item";
                case "PageLocationText":
                    return "Page Location";
                case "PageIDText":
                    return "Page ID";
                case "PagePositionText":
                    return "Page Position";
                case "PageTooltipText":
                    return "Page Tooltip";
                case "PageTitleText":
                    return "Page Title";
                case "No":
                    return "No";
                case "Yes":
                    return "Yes";
                case "RequiresLoginText":
                    return "Requires Login To View";
                case "RequiresLogoutText":
                    return "Requires Logout To View";
                case "RequiresAdminText":
                    return "Requires Admin To View";
                case "RequiresAdminLevelText":
                    return "Required Admin Level To View";

                    //settings manager page
                case "Save":
                    return "Save";
                case "GridCenterXText":
                    return "Grid Center Location X";
                case "GridCenterYText":
                    return "Grid Center Location Y";
                case "SettingsManager":
                    return "Settings Manager";
                case "IgnorePagesUpdatesText":
                    return "Ignore pages update warning until next update";
                case "IgnoreSettingsUpdatesText":
                    return "Ignore settings update warning until next update";

                    //Times
                case "Sun":
                    return "Sun";
                case "Mon":
                    return "Mon";
                case "Tue":
                    return "Tue";
                case "Wed":
                    return "Wed";
                case "Thu":
                    return "Thu";
                case "Fri":
                    return "Fri";
                case "Sat":
                    return "Sat";
                case "Sunday":
                    return "Sunday";
                case "Monday":
                    return "Monday";
                case "Tuesday":
                    return "Tuesday";
                case "Wednesday":
                    return "Wednesday";
                case "Thursday":
                    return "Thursday";
                case "Friday":
                    return "Friday";
                case "Saturday":
                    return "Saturday";

                case "Jan_Short":
                    return "Jan";
                case "Feb_Short":
                    return "Feb";
                case "Mar_Short":
                    return "Mar";
                case "Apr_Short":
                    return "Apr";
                case "May_Short":
                    return "May";
                case "Jun_Short":
                    return "Jun";
                case "Jul_Short":
                    return "Jul";
                case "Aug_Short":
                    return "Aug";
                case "Sep_Short":
                    return "Sep";
                case "Oct_Short":
                    return "Oct";
                case "Nov_Short":
                    return "Nov";
                case "Dec_Short":
                    return "Dec";

                case "January":
                    return "January";
                case "February":
                    return "February";
                case "March":
                    return "March";
                case "April":
                    return "April";
                case "May":
                    return "May";
                case "June":
                    return "June";
                case "July":
                    return "July";
                case "August":
                    return "August";
                case "September":
                    return "September";
                case "October":
                    return "October";
                case "November":
                    return "November";
                case "December":
                    return "December";

                    // ColorBox
                case "ColorBoxImageText":
                    return "Image";
                case "ColorBoxOfText":
                    return "of";
                case "ColorBoxPreviousText":
                    return "Previous";
                case "ColorBoxNextText":
                    return "Next";
                case "ColorBoxCloseText":
                    return "Close";
                case "ColorBoxStartSlideshowText":
                    return "Start Slide Show";
                case "ColorBoxStopSlideshowText":
                    return "Stop Slide Show";


                    // English only so far
                case "NoAccountFound":
                    return "No account found";
                case "DisplayInMenu":
                    return "Display In Menu";
                case "ParentText":
                    return "Menu Parent";
                case "CannotSetParentToChild":
                    return "Cannot set menu item as a child to itself.";
                case "TopLevel":
                    return "Top Level";
                case "HideLanguageBarText":
                    return "Hide Language Selection Bar";
                case "HideStyleBarText":
                    return "Hide Style Selection Bar";
            }
            return "UNKNOWN CHARACTER";
        }
    }
}
