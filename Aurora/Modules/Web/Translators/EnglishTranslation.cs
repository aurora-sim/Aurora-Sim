using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Modules.Web.Translators
{
    public class EnglishTranslation : ITranslator
    {
        public string LanguageName { get { return "en"; } }

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
                    return "Total Users";
                case "TotalRegionCount":
                    return "Total Region Count";
                case "UniqueVisitors":
                    return "Unique Visitors last 30 days";
                case "OnlineNow":
                    return "Online Now";
                case "HyperGrid":
                    return "HyperGrid (HG)";
                case "Voice":
                    return "Voice";
                case "Currency":
                    return "Currency";
                case "Disabled":
                    return "Disabled";
                case "Enabled":
                    return "Enabled";
                case "News":
                    return "News";
                case "Region":
                    return "Region";
                case "Login":
                    return "Login";
                case "UserName":
                case "UserNameText":
                    return "User Name";
                case "Password":
                case "PasswordText":
                    return "Password";
                case "PasswordConfirmation":
                    return "Password Confirmation";
                case "ForgotPassword":
                    return "Forgot Password?";
                case "Submit":
                    return "Submit";

                // English only so far
                case "SpecialWindowTitleText":
                    return "Special Info Window Title";
                case "SpecialWindowTextText":
                    return "Special Info Window Text";
                case "SpecialWindowColorText":
                    return "Special Info Window Color";
                case "SpecialWindowStatusText":
                    return "Special Info Window Status";
                case "WelcomeScreenManagerFor":
                    return "Welcome Screen Manager For";
                case "ChangesSavedSuccessfully":
                    return "Changes Saved Successfully";


                case "AvatarNameText":
                    return "Avatar Name";
                case "AvatarScopeText":
                    return "Avatar Scope ID";
                case "FirstNameText":
                    return "Your First Name";
                case "LastNameText":
                    return "Your Last Name";
                case "UserAddressText":
                    return "Your Address";
                case "UserZipText":
                    return "Your Zip Code";
                case "UserCityText":
                    return "Your City";
                case "UserCountryText":
                    return "Your Country";
                case "UserDOBText":
                    return "Your Date Of Birth (Month Day Year)";
                case "UserEmailText":
                    return "Your Email";
                case "RegistrationText":
                    return "Avatar registration";
                case "RegistrationsDisabled":
                    return "Registrations are currently disabled, please try again soon.";
                case "TermsOfServiceText":
                    return "Terms of Service";
                case "TermsOfServiceAccept":
                    return "Do you accept the Terms of Service as detailed above?";
					
				// news
                case "OpenNewsManager":
                    return "Open the news manager";
                case "NewsManager":
                    return "News Manager";
                case "EditNewsItem":
                    return "Edit news item";
                case "AddNewsItem":
                    return "Add new news item";
                case "DeleteNewsItem":
                    return "Delete news item";
                case "NewsDateText":
                    return "News Date";
                case "NewsTitleText":
                    return "News Title";
                case "NewsItemTitle":
                    return "News Item Title";
                case "NewsItemText":
                    return "News Item Text";
                case "AddNewsText":
                    return "Add News";
                case "DeleteNewsText":
                    return "Delete News";
                case "EditNewsText":
                    return "Edit News";
                case "UserProfileFor":
                    return "User Profile For";
                case "ResidentSince":
                    return "Resident Since";
                case "AccountType":
                    return "Account Type";
                case "PartnersName":
                    return "Partner's Name";
                case "AboutMe":
                    return "About Me";
                case "IsOnlineText":
                    return "User Status";
                case "OnlineLocationText":
                    return "User Location";
					
                case "RegionInformationText":
                    return "Region Information";
                case "OwnerNameText":
                    return "Owner Name";
                case "RegionLocationText":
                    return "Region Location";
                case "RegionSizeText":
                    return "Region Size";
                case "RegionNameText":
                    return "Region Name";
                case "RegionTypeText":
                    return "Region Type";
                case "ParcelsInRegionText":
                    return "Parcels In Region";
                case "ParcelNameText":
                    return "Parcel Name";
                case "ParcelOwnerText":
                    return "Parcel Owner's Name";

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
                    return "More Information";
                case "RegionMoreInfoTooltips":
                    return "More info about";
                case "FirstText":
                    return "First";
                case "BackText":
                    return "Back";
                case "NextText":
                    return "Next";
                case "LastText":
                    return "Last";
                case "CurrentPageText":
                    return "Current Page";
                case "MoreInfoText":
                    return "More Info";
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
                    return "Register";
                case "MenuForgotPass":
                    return "Forgot Password";
                case "MenuNews":
                    return "News";
                case "MenuWorld":
                    return "World";
                case "MenuWorldMap":
                    return "World Map";
                case "MenuRegion":
                    return "Region List";
                case "MenuUser":
                    return "User";
                case "MenuOnlineUsers":
                    return "Online Users";
                case "MenuUserSearch":
                    return "User Search";
                case "MenuRegionSearch":
                    return "Region Search";
                case "MenuChat":
                    return "Chat";
                case "MenuHelp":
                    return "Help";
                case "MenuChangeUserInformation":
                    return "Change User Information";
                case "MenuWelcomeScreenManager":
                    return "Welcome Screen Manager";
                case "MenuNewsManager":
                    return "News Manager";
                case "MenuUserManager":
                    return "User Manager";
                case "MenuFactoryReset":
                    return "Factory Reset";
                case "ResetMenuInfoText":
                    return "Resets the menu items back to the most updated defaults";
                case "ResetSettingsInfoText":
                    return "Resets the Web Interface settings back to the most updated defaults";
                case "MenuPageManager":
                    return "Page Manager";
                case "MenuSettingsManager":
                    return "Settings Manager";
                case "MenuManager":
                    return "Admin Management";

				// Tooltips Menu Buttons
                case "TooltipsMenuHome":
                    return "Home";
                case "TooltipsMenuLogin":
                    return "Login";
                case "TooltipsMenuLogout":
                    return "Logout";
                case "TooltipsMenuRegister":
                    return "Register";
                case "TooltipsMenuForgotPass":
                    return "Forgot Password";
                case "TooltipsMenuNews":
                    return "News";
                case "TooltipsMenuWorld":
                    return "World";
                case "TooltipsMenuWorldMap":
                    return "World Map";
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
                case "TooltipsMenuHelp":
                    return "Help";
                case "TooltipsMenuChangeUserInformation":
                    return "Change User Information";
                case "TooltipsMenuWelcomeScreenManager":
                    return "Welcome Screen Manager";
                case "TooltipsMenuNewsManager":
                    return "News Manager";
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
				
                //Logout page
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
                    return "This will remove all information about you in the grid and remove your access to this service. If you wish to continue, enter your name and password and click Delete.";
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

                //Edit user page
                case "AdminDeleteUserText":
                    return "Delete User: This deletes the account and destroys all information associated with it.";
                case "BanText":
                    return "Ban";
                case "UnbanText":
                    return "Unban";
                case "AdminTempBanUserText":
                    return "Temp Ban User: This blocks the user from logging in for the set amount of time.";
                case "AdminBanUserText":
                    return "Ban User: This blocks the user from logging in until the user is unbanned.";
                case "AdminUnbanUserText":
                    return "Unban User: Removes temporary and permanent bans on the user.";
                case "AdminLoginInAsUserText":
                    return "Login as User: You will be logged out of your admin account, and logged in as this user, and will see everything as they see it.";
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
                    return "defaults updated, go to Factory Reset to update or Settings Manager to disable this warning.";

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


                case "Jan":
                    return "Jan";
                case "Feb":
                    return "Feb";
                case "Mar":
                    return "Mar";
                case "Apr":
                    return "Apr";
                case "May_Short":
                    return "May";
                case "Jun":
                    return "Jun";
                case "Jul":
                    return "Jul";
                case "Aug":
                    return "Aug";
                case "Sep":
                    return "Sep";
                case "Oct":
                    return "Oct";
                case "Nov":
                    return "Nov";
                case "Dec":
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
					
                //Language Switcher Tooltips
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
            }
            return "UNKNOWN CHARACTER";
        }
    }
}