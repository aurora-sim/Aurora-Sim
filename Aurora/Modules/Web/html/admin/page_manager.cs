using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Web
{
    public class PageManagerPage : IWebInterfacePage
    {
        public string[] FilePath { get
        {
            return new[]
                       {
                           "html/admin/page_manager.html"
                       };
        } }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            
            #region Find pages

            List<Dictionary<string, object>> pages = new List<Dictionary<string, object>>();

            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            GridPage rootPage = generics.GetGeneric<GridPage>(UUID.Zero, "WebPages", "Root");
            rootPage.Children.Sort((a, b) => a.MenuPosition.CompareTo(b.MenuPosition));

            foreach (GridPage page in rootPage.Children)
            {
                pages.Add(new Dictionary<string, object> { { "Value", page.MenuID }, 
                    { "Name", translator.GetTranslatedString(page.MenuTitle) } });
            }
            vars.Add("PagesList", pages);

            #endregion

            bool changed = false;
            if (requestParameters.ContainsKey("SelectItem"))
            {
                string MenuItem = requestParameters["MenuItem"].ToString();

                GridPage page = GetPage(MenuItem, rootPage);

                vars.Add("PageTitle", page.MenuTitle);
                vars.Add("PageTooltip", page.MenuToolTip);
                vars.Add("PageID", page.MenuID);
                vars.Add("PagePosition", page.MenuPosition);
                vars.Add("PageLocation", page.Location);
                vars.Add("EdittingPageID", page.MenuID);
                vars.Add("RequiresLoginYes", page.LoggedInRequired ? "selected=\"selected\"" : "");
                vars.Add("RequiresLoginNo", !page.LoggedInRequired ? "selected=\"selected\"" : "");
                vars.Add("RequiresLogoutYes", page.LoggedOutRequired ? "selected=\"selected\"" : "");
                vars.Add("RequiresLogoutNo", !page.LoggedOutRequired ? "selected=\"selected\"" : "");
                vars.Add("RequiresAdminYes", page.AdminRequired ? "selected=\"selected\"" : "");
                vars.Add("RequiresAdminNo", !page.AdminRequired ? "selected=\"selected\"" : "");
            }
            else
            {
                vars.Add("PageTitle", "");
                vars.Add("PageTooltip", "");
                vars.Add("PageID", "");
                vars.Add("PagePosition", "");
                vars.Add("PageLocation", "");
                vars.Add("EdittingPageID", "");
                vars.Add("RequiresLoginYes", "");
                vars.Add("RequiresLoginNo", "");
                vars.Add("RequiresLogoutYes", "");
                vars.Add("RequiresLogoutNo", "");
                vars.Add("RequiresAdminYes", "");
                vars.Add("RequiresAdminNo", "");
            }
            if (requestParameters.ContainsKey("SaveMenuItem"))
            {
                changed = true;
                string edittingPageID = requestParameters["EdittingPageID"].ToString();
                string PageTitle = requestParameters["PageTitle"].ToString();
                string PageTooltip = requestParameters["PageTooltip"].ToString();
                string PagePosition = requestParameters["PagePosition"].ToString();
                string PageID = requestParameters["PageID"].ToString();
                string PageLocation = requestParameters["PageLocation"].ToString();
                bool RequiresLogin = bool.Parse(requestParameters["RequiresLogin"].ToString());
                bool RequiresLogout = bool.Parse(requestParameters["RequiresLogout"].ToString());
                bool RequiresAdmin = bool.Parse(requestParameters["RequiresAdmin"].ToString());

                GridPage page = GetPage(edittingPageID, rootPage);

                page.Location = PageLocation;
                page.MenuID = PageID;
                page.MenuPosition = int.Parse(PagePosition);
                page.MenuTitle = PageTitle;
                page.MenuToolTip = PageTooltip;
                page.LoggedInRequired = RequiresLogin;
                page.LoggedOutRequired = RequiresLogout;
                page.AdminRequired = RequiresAdmin;

                ReplacePage(edittingPageID, page, ref rootPage);

                generics.AddGeneric(UUID.Zero, "WebPages", "Root", rootPage.ToOSD());
            }

            vars.Add("PageTitleText", translator.GetTranslatedString("PageTitleText"));
            vars.Add("PageTooltipText", translator.GetTranslatedString("PageTooltipText"));
            vars.Add("PagePositionText", translator.GetTranslatedString("PagePositionText"));
            vars.Add("PageIDText", translator.GetTranslatedString("PageIDText"));
            vars.Add("PageLocationText", translator.GetTranslatedString("PageLocationText"));
            vars.Add("SaveMenuItemChanges", translator.GetTranslatedString("SaveMenuItemChanges"));
            vars.Add("RequiresLoginText", translator.GetTranslatedString("RequiresLoginText"));
            vars.Add("RequiresLogoutText", translator.GetTranslatedString("RequiresLogoutText"));
            vars.Add("RequiresAdminText", translator.GetTranslatedString("RequiresAdminText"));
            vars.Add("SelectItem", translator.GetTranslatedString("SelectItem"));
            vars.Add("PageManager", translator.GetTranslatedString("PageManager"));
            vars.Add("No", translator.GetTranslatedString("No"));
            vars.Add("Yes", translator.GetTranslatedString("Yes"));
            
            vars.Add("ChangesSavedSuccessfully", changed ? translator.GetTranslatedString("ChangesSavedSuccessfully") : "");

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }

        private GridPage GetPage(string MenuItem, GridPage rootPage)
        {
            foreach (var page in rootPage.Children)
            {
                if (page.MenuID == MenuItem)
                    return page;
                else if (page.Children.Count > 0)
                {
                    var p = GetPage(MenuItem, page);
                    if (p != null)
                        return p;
                }
            }
            return null;
        }

        private void ReplacePage(string MenuItem, GridPage replacePage, ref GridPage rootPage)
        {
            foreach (var page in rootPage.Children)
            {
                if (page.MenuID == MenuItem)
                {
                    page.FromOSD(replacePage.ToOSD());
                    return;
                }
                else if (page.Children.Count > 0)
                {
                    var p = GetPage(MenuItem, page);
                    if (p != null)
                    {
                        p.FromOSD(replacePage.ToOSD());
                        return;
                    }
                }
            }
        }
    }
}
