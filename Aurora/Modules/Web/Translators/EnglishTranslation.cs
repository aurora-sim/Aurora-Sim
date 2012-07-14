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

				// Region Page
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

				// Menu Buttons
                case "MenuHome":
                    return "Home";
                case "MenuLogin":
                    return "Login";
                case "MenuRegister":
                    return "Register";
                case "MenuForgotPass":
                    return "Forgot Password";
                case "MenuNews":
                    return "News";
                case "MenuWorld":
                    return "World";
                case "MenuRegion":
                    return "Region";
                case "MenuUser":
                    return "User";
                case "MenuChat":
                    return "Chat";
                case "MenuHelp":
                    return "Help";

				// Tooltips Menu Buttons
                case "TooltipsMenuHome":
                    return "Home";
                case "TooltipsMenuLogin":
                    return "Login";
                case "TooltipsMenuRegister":
                    return "Register";
                case "TooltipsMenuForgotPass":
                    return "Forgot Password";
                case "TooltipsMenuNews":
                    return "News";
                case "TooltipsMenuWorld":
                    return "World";
                case "TooltipsMenuRegion":
                    return "Region";
                case "TooltipsMenuUser":
                    return "User";
                case "TooltipsMenuChat":
                    return "Chat";
                case "TooltipsMenuHelp":
                    return "Help";

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
				
            }
            return "UNKNOWN CHARACTER";
        }
    }
}
