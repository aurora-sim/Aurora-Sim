using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Web
{
    public class WebInterface : IService, IWebInterfaceModule
    {
        #region Declares

        protected const int CLIENT_CACHE_TIME = 86400;//1 day
        protected uint _port = 8002;
        protected bool _enabled = true;
        protected Dictionary<string, IWebInterfacePage> _pages = new Dictionary<string, IWebInterfacePage>();
        protected List<ITranslator> _translators = new List<ITranslator>();
        protected ITranslator _defaultTranslator;

        #endregion

        #region Public Properties

        public IRegistryCore Registry { get; protected set; }

        public string GridName { get; private set; }
        public string LoginScreenURL { get { return MainServer.Instance.FullHostName + ":" + _port + "/welcomescreen/"; } }
        public string WebProfileURL { get { return MainServer.Instance.FullHostName + ":" + _port + "/webprofile/"; } }
        public string RegistrationScreenURL { get { return MainServer.Instance.FullHostName + ":" + _port + "/register.html"; } }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            Registry = registry;

            var webPages = Aurora.Framework.AuroraModuleLoader.PickupModules<IWebInterfacePage>();
            foreach (var pages in webPages)
            {
                foreach (var page in pages.FilePath)
                {
                    _pages.Add(page, pages);
                }
            }

            _translators = AuroraModuleLoader.PickupModules<ITranslator>();
            _defaultTranslator = _translators[0];
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig con = config.Configs["WebInterface"];
            if (con != null)
            {
                _enabled = con.GetString("Module", "BuiltIn") == "BuiltIn";
                _port = con.GetUInt("Port", _port);
                string defaultLanguage = con.GetString("DefaultLanguage", "en");
                _defaultTranslator = _translators.FirstOrDefault(t => t.LanguageName == defaultLanguage);
                if (_defaultTranslator == null)
                    _defaultTranslator = _translators[0];
            }
            if (_enabled)
            {
                Registry.RegisterModuleInterface<IWebInterfaceModule>(this);
                var server = registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(_port);
                server.AddHTTPHandler(new GenericStreamHandler("GET", "/", FindAndSendPage));
            }
        }

        public void FinishedStartup()
        {
            if (_enabled)
            {
                IGridInfo gridInfo = Registry.RequestModuleInterface<IGridInfo>();
                GridName = gridInfo.GridName;

                if (PagesMigrator.RequiresInitialUpdate())
                    PagesMigrator.ResetToDefaults();
                if (SettingsMigrator.RequiresInitialUpdate())
                    SettingsMigrator.ResetToDefaults();
            }
        }

        #endregion

        #region Page Sending

        protected class CookieLock
        {
            public UUID CookieUUID;
            public Dictionary<string, object> Vars;
        }

        protected PreAddedDictionary<string, List<CookieLock>> _cookieLockedVars = new PreAddedDictionary<string, List<CookieLock>>(() => new List<CookieLock>());
        public void CookieLockPageVars(string path, Dictionary<string, object> vars, OSHttpResponse response)
        {
            UUID random = UUID.Random();
            response.AddCookie(new System.Web.HttpCookie(random.ToString()));
            lock(_cookieLockedVars)
                _cookieLockedVars[path].Add(new CookieLock { CookieUUID = random, Vars = vars });
        }

        protected bool CheckCookieLocked(string path, OSHttpRequest request, OSHttpResponse response, out Dictionary<string, object> vars)
        {
            vars = null;
            List<CookieLock> locks = new List<CookieLock>();
            lock(_cookieLockedVars)
            {
                if(!_cookieLockedVars.TryGetValue(path, out locks))
                    return false;
            }
            foreach(var l in locks)
            {
                foreach(var c in request.Cookies.Keys)
                {
                    UUID cookieID;
                    if(UUID.TryParse(c.ToString(), out cookieID))
                    {
                        if(l.CookieUUID == cookieID)
                        {
                            vars = l.Vars;
                            lock(_cookieLockedVars)
                                _cookieLockedVars[path].Remove(l);
                            //Attempt to nuke the cookie now
                            response.AddCookie(new System.Web.HttpCookie(c.ToString()) { Expires = Util.UnixEpoch });
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public IWebInterfacePage GetPage(string path)
        {
            IWebInterfacePage page;
            string directory = string.Join("/", path.Split('/'), 0, path.Split('/').Length - 1) + "/";
            if (!_pages.TryGetValue(path, out page) &&
                !_pages.TryGetValue(directory, out page))
                page = null;
            return page;
        }

        protected byte[] FindAndSendPage(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            byte[] response = MainServer.BlankResponse;
            string filename = GetFileNameFromHTMLPath(path);
            MainConsole.Instance.Debug("[WebInterface]: Serving " + filename);
            httpResponse.KeepAlive = false;
            IWebInterfacePage page = GetPage(filename);
            if (page != null)
            {
                httpResponse.ContentType = GetContentType(filename, httpResponse);
                string text;
                if (!File.Exists(filename))
                {
                    if (!page.AttemptFindPage(filename, ref httpResponse, out text))
                        return MainServer.BadRequest;
                }
                else
                    text = File.ReadAllText(filename);

                var requestParameters = request != null ? WebUtils.ParseQueryString(request.ReadUntilEnd()) : new Dictionary<string, object>();
                if (filename.EndsWith(".xsl"))
                {
                    AuroraXmlDocument vars = GetXML(filename, httpRequest, httpResponse, requestParameters);

                    var xslt = new XslCompiledTransform();
                    if (File.Exists(path)) xslt.Load(GetFileNameFromHTMLPath(path));
                    else if (text != "")
                    {
                        XslCompiledTransform objXslTrans = new XslCompiledTransform();
                        xslt.Load(new XmlTextReader(new StringReader(text)));
                    }
                    var stm = new MemoryStream();
                    xslt.Transform(vars, null, stm);
                    stm.Position = 1;
                    var sr = new StreamReader(stm);
                    string results = sr.ReadToEnd().Trim();
                    return Encoding.UTF8.GetBytes(Regex.Replace(results, @"[^\u0000-\u007F]", string.Empty));
                }
                else
                {
                    Dictionary<string, object> vars;
                    if(!CheckCookieLocked(filename, httpRequest, httpResponse, out vars))
                        vars = AddVarsForPage(filename, httpRequest, httpResponse, requestParameters);

                    AddDefaultVarsForPage(ref vars);

                    if (httpResponse.StatusCode != 200)
                        return MainServer.NoResponse;
                    if (vars == null)
                        return MainServer.BadRequest;
                    response = Encoding.UTF8.GetBytes(ConvertHTML(text, httpRequest, httpResponse, requestParameters, vars));
                }
            }
            else
            {
                httpResponse.ContentType = GetContentType(filename, httpResponse);
                if (httpResponse.ContentType == null)
                    return MainServer.BadRequest;
                response = File.ReadAllBytes(filename);
            }
            return response;
        }

        #endregion

        #region Helpers

        protected void AddDefaultVarsForPage(ref Dictionary<string, object> vars)
        {
            vars.Add("SystemURL", MainServer.Instance.FullHostName + ":" + _port);
            vars.Add("SystemName", GridName);
        }

        protected Dictionary<string, object> AddVarsForPage(string filename, OSHttpRequest httpRequest, OSHttpResponse httpResponse, Dictionary<string, object> requestParameters)
        {
            Dictionary<string, object> vars = new Dictionary<string, object>();
            IWebInterfacePage page = GetPage(filename);
            if (page != null)
            {
                ITranslator translator = null;
                if (httpRequest.Query.ContainsKey("language"))
                    translator = _translators.FirstOrDefault(t => t.LanguageName == httpRequest.Query["language"].ToString());
                if (translator == null)
                    translator = _defaultTranslator;

                if (page.RequiresAuthentication)
                {
                    if (!Authenticator.CheckAuthentication(httpRequest))
                        return null;
                    if (page.RequiresAdminAuthentication)
                    {
                        if (!Authenticator.CheckAdminAuthentication(httpRequest))
                            return null;
                    }
                }
                vars = page.Fill(this, filename, httpRequest, httpResponse, requestParameters, translator);
                return vars;
            }
            return null;
        }

        private AuroraXmlDocument GetXML(string filename, OSHttpRequest httpRequest, OSHttpResponse httpResponse, Dictionary<string, object> requestParameters)
        {
            IWebInterfacePage page = GetPage(filename);
            if (page != null)
            {
                ITranslator translator = null;
                if (httpRequest.Query.ContainsKey("language"))
                    translator = _translators.FirstOrDefault(t => t.LanguageName == httpRequest.Query["language"].ToString());
                if (translator == null)
                    translator = _defaultTranslator;

                if (page.RequiresAuthentication)
                {
                    if (!Authenticator.CheckAuthentication(httpRequest))
                        return null;
                    if (page.RequiresAdminAuthentication)
                    {
                        if (!Authenticator.CheckAdminAuthentication(httpRequest))
                            return null;
                    }
                }
                return (AuroraXmlDocument)page.Fill(this, filename, httpRequest, httpResponse, requestParameters, translator)["xml"];
            }
            return null;
        }
        protected string ConvertHTML(string file, OSHttpRequest request, OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, Dictionary<string, object> vars)
        {
            string html = CSHTMLCreator.BuildHTML(file, vars);

            string[] lines = html.Split('\n');
            StringBuilder sb = new StringBuilder();
            for (int pos = 0; pos < lines.Length; pos++)
            {
                string line = lines[pos];
                string cleanLine = line.Trim();
                if (cleanLine.StartsWith("<!--#include file="))
                {
                    string[] split = line.Split(new string[2] { "<!--#include file=\"", "\" -->" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = split.Length % 2 == 0 ? 0 : 1; i < split.Length; i += 2)
                    {
                        string filename = GetFileNameFromHTMLPath(split[i]);
                        Dictionary<string, object> newVars = AddVarsForPage(filename, 
                            request, httpResponse, requestParameters);
                        sb.AppendLine(ConvertHTML(File.ReadAllText(filename), 
                            request, httpResponse, requestParameters, newVars));
                    }
                }
                else if (cleanLine.StartsWith("<!--#include folder="))
                {
                    string[] split = line.Split(new string[2] { "<!--#include folder=\"", "\" -->" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = split.Length % 2 == 0 ? 0 : 1; i < split.Length; i += 2)
                    {
                        string filename = GetFileNameFromHTMLPath(split[i]).Replace("index.html", "");
                        if (Directory.Exists(filename))
                        {
                            Dictionary<string, object> newVars = AddVarsForPage(filename, request, httpResponse,
                                                                                requestParameters);
                            string[] files = Directory.GetFiles(filename);
                            foreach (string f in files)
                            {
                                if (!f.EndsWith(".html")) continue;
                                Dictionary<string, object> newVars2 = AddVarsForPage(f, request, httpResponse, requestParameters) ??
                                                                      new Dictionary<string, object>();
                                foreach (KeyValuePair<string, object> pair in newVars.Where(pair => !newVars2.ContainsKey(pair.Key)))
                                    newVars2.Add(pair.Key, pair.Value);
                                sb.AppendLine(ConvertHTML(File.ReadAllText(f), request, httpResponse,
                                                                    requestParameters, newVars2));
                            }
                        }
                    }
                }
                else if (cleanLine.StartsWith("{"))
                {
                    int indBegin, indEnd;
                    if ((indEnd = cleanLine.IndexOf("ArrayBegin}")) != -1)
                    {
                        string keyToCheck = cleanLine.Substring(1, indEnd - 1);
                        int posToCheckFrom;
                        List<string> repeatedLines = ExtractLines(lines, pos, keyToCheck, "ArrayEnd", out posToCheckFrom);
                        pos = posToCheckFrom;
                        if (vars.ContainsKey(keyToCheck))
                        {
                            List<Dictionary<string, object>> dicts = vars[keyToCheck] as List<Dictionary<string, object>>;
                            if (dicts != null)
                                foreach (var dict in dicts)
                                    sb.AppendLine(ConvertHTML(string.Join("\n", repeatedLines.ToArray()), request,
                                                                httpResponse, requestParameters, dict));
                        }
                    }
                    else if ((indEnd = cleanLine.IndexOf("AuthenticatedBegin}")) != -1)
                    {
                        string key = cleanLine.Substring(1, indEnd - 1) + "AuthenticatedEnd";
                        int posToCheckFrom = FindLines(lines, pos, "", key);
                        if (!CheckAuth(cleanLine, request))
                            pos = posToCheckFrom;
                    }
                    else if ((indBegin = cleanLine.IndexOf("{If")) != -1 &&
                        (indEnd = cleanLine.IndexOf("Begin}")) != -1)
                    {
                        string key = cleanLine.Substring(indBegin + 3, indEnd - indBegin - 3);
                        int posToCheckFrom = FindLines(lines, pos, "If" + key, "End");
                        if (!vars.ContainsKey(key) || ((bool)vars[key]) == false)
                            pos = posToCheckFrom;
                    }
                    else if ((indBegin = cleanLine.IndexOf("{If")) != -1 &&
                        (indEnd = cleanLine.IndexOf("End}")) != -1)
                    {
                        //end of an if statement, just ignore it
                    }
                    else if ((indBegin = cleanLine.IndexOf("{Is")) != -1 &&
                        (indEnd = cleanLine.IndexOf("End}")) != -1)
                    {
                        //end of an if statement, just ignore it
                    }
                    else
                        sb.AppendLine(line);
                }
                else
                    sb.AppendLine(line);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns false if the authentication was wrong
        /// </summary>
        /// <param name="p"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private bool CheckAuth(string p, OSHttpRequest request)
        {
            if (p.StartsWith("{IsAuthenticatedBegin}"))
            {
                return Authenticator.CheckAuthentication(request);
            }
            else if (p.StartsWith("{IsNotAuthenticatedBegin}"))
            {
                return !Authenticator.CheckAuthentication(request);
            }
            else if (p.StartsWith("{IsAdminAuthenticatedBegin}"))
            {
                return Authenticator.CheckAdminAuthentication(request);
            }
            else if (p.StartsWith("{IsNotAdminAuthenticatedBegin}"))
            {
                return !Authenticator.CheckAdminAuthentication(request);
            }
            return false;
        }

        private static int FindLines(string[] lines, int pos, string keyToCheck, string type)
        {
            int posToCheckFrom = pos + 1;
            while (!lines[posToCheckFrom++].TrimStart().StartsWith("{" + keyToCheck + type + "}"))
                continue;

            return posToCheckFrom - 1;
        }

        private static List<string> ExtractLines(string[] lines, int pos, 
            string keyToCheck, string type, out int posToCheckFrom)
        {
            posToCheckFrom = pos + 1;
            List<string> repeatedLines = new List<string>();
            while (!lines[posToCheckFrom].Trim().StartsWith("{" + keyToCheck + type + "}"))
                repeatedLines.Add(lines[posToCheckFrom++]);
            return repeatedLines;
        }

        protected string GetContentType(string filename, OSHttpResponse response)
        {
            switch(Path.GetExtension(filename))
            {
                case ".jpeg":
                case ".jpg":
                    response.AddHeader("Cache-Control", CLIENT_CACHE_TIME.ToString());
                    return "image/jpeg";
                case ".gif":
                    response.AddHeader("Cache-Control", CLIENT_CACHE_TIME.ToString());
                    return "image/gif";
                case ".png":
                    response.AddHeader("Cache-Control", CLIENT_CACHE_TIME.ToString());
                    return "image/png";
                case ".tiff":
                    response.AddHeader("Cache-Control", CLIENT_CACHE_TIME.ToString());
                    return "image/tiff";
                case ".html":
                case ".htm":
                case ".xsl":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "application/javascript";
            }
            return "text/plain";
        }

        protected string GetFileNameFromHTMLPath(string path)
        {
            string file = Path.Combine("html/", path.StartsWith("/") ? path.Remove(0, 1) : path);
			if (!Path.GetFullPath(file).StartsWith(Path.GetFullPath("html/"))) return "html/index.html";
            if (Path.GetFileName(file) == "")
                file = Path.Combine(file, "index.html");
            return file;
        }

        #endregion

        internal void Redirect(OSHttpResponse httpResponse, string url)
        {
            httpResponse.StatusCode = (int)HttpStatusCode.Redirect;
            httpResponse.AddHeader("Location", url);
        }
    }

    internal class GridNewsItem : IDataTransferable
    {
        public static readonly GridNewsItem NoNewsItem = new GridNewsItem() { ID = -1, Text = "No news to report", Time = DateTime.Now, Title = "No news to report" };
        public string Title;
        public string Text;
        public DateTime Time;
        public int ID;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Title"] = Title;
            map["Text"] = Text;
            map["Time"] = Time;
            map["ID"] = ID;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Title = map["Title"];
            Text = map["Text"];
            Time = map["Time"];
            ID = map["ID"];
        }

        public Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            dictionary.Add("NewsDate", Time.ToShortDateString());
            dictionary.Add("NewsTitle", Title);
            dictionary.Add("NewsText", Text);
            dictionary.Add("NewsID", ID);

            return dictionary;
        }
    }

    internal class GridWelcomeScreen : IDataTransferable
    {
        public static readonly GridWelcomeScreen Default = new GridWelcomeScreen
        {
            SpecialWindowMessageTitle = "Nothing to report at this time.",
            SpecialWindowMessageText = "Grid is up and running.",
            SpecialWindowMessageColor = "white",
            SpecialWindowActive = true,
            GridStatus = true
        };

        public string SpecialWindowMessageTitle;
        public string SpecialWindowMessageText;
        public string SpecialWindowMessageColor;
        public bool SpecialWindowActive;
        public bool GridStatus;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["SpecialWindowMessageTitle"] = SpecialWindowMessageTitle;
            map["SpecialWindowMessageText"] = SpecialWindowMessageText;
            map["SpecialWindowMessageColor"] = SpecialWindowMessageColor;
            map["SpecialWindowActive"] = SpecialWindowActive;
            map["GridStatus"] = GridStatus;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            SpecialWindowMessageTitle = map["SpecialWindowMessageTitle"];
            SpecialWindowMessageText = map["SpecialWindowMessageText"];
            SpecialWindowMessageColor = map["SpecialWindowMessageColor"];
            SpecialWindowActive = map["SpecialWindowActive"];
            GridStatus = map["GridStatus"];
        }
    }

    internal class GridPage : IDataTransferable
    {
        public List<GridPage> Children = new List<GridPage>();
        public bool ShowInMenu = false;
        public int MenuPosition = -1;
        public string MenuID = "";
        public string MenuTitle = "";
        public string MenuToolTip = "";
        public string Location = "";
        public bool LoggedInRequired = false;
        public bool LoggedOutRequired = false;
        public bool AdminRequired = false;

        public GridPage() { } 
        public GridPage(OSD map) { FromOSD(map as OSDMap); }

        public override void FromOSD(OSDMap map)
        {
            ShowInMenu = map["ShowInMenu"];
            MenuPosition = map["MenuPosition"];
            MenuID = map["MenuID"];
            MenuTitle = map["MenuTitle"];
            MenuToolTip = map["MenuToolTip"];
            Location = map["Location"];
            LoggedInRequired = map["LoggedInRequired"];
            LoggedOutRequired = map["LoggedOutRequired"];
            AdminRequired = map["AdminRequired"];
            Children = ((OSDArray)map["Children"]).ConvertAll<GridPage>(o => new GridPage(o));
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();

            map["ShowInMenu"] = ShowInMenu;
            map["MenuPosition"] = MenuPosition;
            map["MenuID"] = MenuID;
            map["MenuTitle"] = MenuTitle;
            map["MenuToolTip"] = MenuToolTip;
            map["Location"] = Location;
            map["LoggedInRequired"] = LoggedInRequired;
            map["LoggedOutRequired"] = LoggedOutRequired;
            map["AdminRequired"] = AdminRequired;
            map["Children"] = Children.ToOSDArray();
            return map;
        }

        public GridPage GetPage(string item)
        {
            return GetPage(item, null);
        }

        public GridPage GetPage(string item, GridPage rootPage)
        {
            if (rootPage == null)
                rootPage = this;
            foreach (var page in rootPage.Children)
            {
                if (page.MenuID == item)
                    return page;
                else if (page.Children.Count > 0)
                {
                    var p = GetPage(item, page);
                    if (p != null)
                        return p;
                }
            }
            return null;
        }

        public void ReplacePage(string MenuItem, GridPage replacePage)
        {
            foreach (var page in this.Children)
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

        public void RemovePage(string MenuItem, GridPage replacePage)
        {
            GridPage foundPage = null;
            foreach (var page in this.Children)
            {
                if (page.MenuID == MenuItem)
                {
                    foundPage = page;
                    break;
                }
                else if (page.Children.Count > 0)
                {
                    var p = GetPage(MenuItem, page);
                    if (p != null)
                    {
                        page.Children.Remove(p);
                        return;
                    }
                }
            }
            if (foundPage != null)
                this.Children.Remove(foundPage);
        }
    }

    internal class GridSettings : IDataTransferable
    {
        public Vector2 MapCenter = Vector2.Zero;
        public uint LastPagesVersionUpdateIgnored = 0;
        public uint LastSettingsVersionUpdateIgnored = 0;

        public GridSettings() { }
        public GridSettings(OSD map) { FromOSD(map as OSDMap); }

        public override void FromOSD(OSDMap map)
        {
            MapCenter = map["MapCenter"];
            LastPagesVersionUpdateIgnored = map["LastPagesVersionUpdateIgnored"];
            LastSettingsVersionUpdateIgnored = map["LastSettingsVersionUpdateIgnored"];
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();

            map["MapCenter"] = MapCenter;
            map["LastPagesVersionUpdateIgnored"] = LastPagesVersionUpdateIgnored;
            map["LastSettingsVersionUpdateIgnored"] = LastSettingsVersionUpdateIgnored;
            return map;
        }
    }
}
