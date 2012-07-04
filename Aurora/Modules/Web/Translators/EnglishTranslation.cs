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
                    return "User Name";
                case "Password":
                    return "Password";
                case "ForgotPassword":
                    return "Forgot Password?";
                case "Submit":
                    return "Submit";

                //English only so far
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
                    
            }
            return "UNKNOWN CHARACTER";
        }
    }
}
