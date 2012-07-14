using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
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
            IGridInfo gridInfo = Registry.RequestModuleInterface<IGridInfo>();
            GridName = gridInfo.GridName;
        }

        #endregion

        #region Page Sending

        public byte[] FindAndSendPage(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            byte[] response = MainServer.BlankResponse;
            string filename = GetFileNameFromHTMLPath(path);
            httpResponse.ContentType = GetContentType(filename, httpResponse);
            if (httpResponse.ContentType == null)
                return MainServer.BadRequest;
            MainConsole.Instance.Debug("[WebInterface]: Serving " + filename);
            httpResponse.KeepAlive = false;

            if (_pages.ContainsKey(filename))
            {
                var requestParameters = request != null ? WebUtils.ParseQueryString(request.ReadUntilEnd()) : new Dictionary<string, object>();
                Dictionary<string, object> vars = AddVarsForPage(filename, httpRequest, httpResponse, requestParameters);
                if (httpResponse.StatusCode != 200)
                    return MainServer.NoResponse;
                if (vars == null)
                    return MainServer.BadRequest;
                response = Encoding.UTF8.GetBytes(ConvertHTML(File.ReadAllText(filename), httpRequest, httpResponse, requestParameters, vars));
            }
            else
                response = File.ReadAllBytes(filename);
            return response;
        }

        #endregion

        #region Helpers

        protected Dictionary<string, object> AddVarsForPage(string filename, OSHttpRequest httpRequest, OSHttpResponse httpResponse, Dictionary<string, object> requestParameters)
        {
            Dictionary<string, object> vars = new Dictionary<string, object>();
            if (_pages.ContainsKey(filename))
            {
                ITranslator translator = null;
                if (httpRequest.Query.ContainsKey("language"))
                    translator = _translators.FirstOrDefault(t => t.LanguageName == httpRequest.Query["language"].ToString());
                if (translator == null)
                    translator = _defaultTranslator;

                if (_pages[filename].RequiresAuthentication)
                {
                    if (!Authenticator.CheckAuthentication(httpRequest))
                        return null;
                    if (_pages[filename].RequiresAdminAuthentication)
                    {
                        if (!Authenticator.CheckAdminAuthentication(httpRequest))
                            return null;
                    }
                }
                vars = _pages[filename].Fill(this, filename, httpRequest, httpResponse, requestParameters, translator);
                vars.Add("SystemURL", MainServer.Instance.FullHostName + ":" + _port);
                vars.Add("SystemName", GridName);
                return vars;
            }
            return null;
        }

        protected string ConvertHTML(string file, OSHttpRequest request, OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, Dictionary<string, object> vars)
        {
            string html = CSHTMLCreator.BuildHTML(file, vars);

            string[] lines = html.Split('\n');
            List<string> newLines = new List<string>(lines);
            int newLinesPos = 0;
            for(int pos = 0; pos < lines.Length; pos++)
            {
                string line = lines[pos];
                if (line.Contains("<!--#include file="))
                {
                    string[] split = line.Split(new string[2] { "<!--#include file=\"", "\" -->" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = split.Length % 2 == 0 ? 0 : 1; i < split.Length; i += 2)
                    {
                        string filename = GetFileNameFromHTMLPath(split[i]);
                        Dictionary<string, object> newVars = AddVarsForPage(filename, request, httpResponse, requestParameters);
                        newLines[newLinesPos] = ConvertHTML(File.ReadAllText(filename), request, httpResponse, requestParameters, newVars);
                    }
                }
                else if (line.Contains("<!--#include folder="))
                {
                    string[] split = line.Split(new string[2] { "<!--#include folder=\"", "\" -->" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = split.Length % 2 == 0 ? 0 : 1; i < split.Length; i += 2)
                    {
                        string filename = GetFileNameFromHTMLPath(split[i]).Replace("index.html","");
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
                                newLines[newLinesPos] += ConvertHTML(File.ReadAllText(f), request, httpResponse,
                                                                    requestParameters, newVars2);
                            }
                        }
                    }
                }
                else if (line.Trim().StartsWith("{"))
                {
                    int ind;
                    if ((ind = line.IndexOf("ArrayBegin}")) != -1)
                    {
                        newLines.RemoveAt(newLinesPos--);
                        string keyToCheck = line.Substring(1, ind - 1);
                        int posToCheckFrom;
                        List<string> repeatedLines = FindLines(lines, newLines, pos, keyToCheck, "ArrayEnd", out posToCheckFrom);
                        for (int i = pos; i < posToCheckFrom; i++)
                            newLines.RemoveAt(newLinesPos + 1);
                        pos = posToCheckFrom;
                        if (vars.ContainsKey(keyToCheck))
                        {
                            List<Dictionary<string, object>> dicts = vars[keyToCheck] as List<Dictionary<string, object>>;
                            if (dicts != null)
                                foreach (var dict in dicts)
                                    newLines.Insert(newLinesPos++,
                                                    ConvertHTML(string.Join(" ", repeatedLines.ToArray()), request,
                                                                httpResponse, requestParameters, dict));
                        }
                    }
                    else if (line.Trim().StartsWith("{IsAuthenticatedBegin}"))
                    {
                        newLines.RemoveAt(newLinesPos--);
                        int posToCheckFrom;
                        List<string> repeatedLines = FindLines(lines, newLines, pos, "", "IsAuthenticatedEnd", out posToCheckFrom);
                        if (!Authenticator.CheckAuthentication(request))
                        {
                            for (int i = pos; i < posToCheckFrom; i++)
                                newLines.RemoveAt(newLinesPos + 1);
                            pos = posToCheckFrom;
                        }
                    }
                    else if (line.Trim().StartsWith("{IsNotAuthenticatedBegin}"))
                    {
                        newLines.RemoveAt(newLinesPos--);
                        int posToCheckFrom;
                        List<string> repeatedLines = FindLines(lines, newLines, pos, "", "IsNotAuthenticatedEnd", out posToCheckFrom);
                        if (Authenticator.CheckAuthentication(request))
                        {
                            for (int i = pos; i < posToCheckFrom; i++)
                                newLines.RemoveAt(newLinesPos + 1);
                            pos = posToCheckFrom;
                        }
                    }
                    else if (line.Trim().StartsWith("{IsAdminAuthenticatedBegin}"))
                    {
                        newLines.RemoveAt(newLinesPos--);
                        int posToCheckFrom;
                        List<string> repeatedLines = FindLines(lines, newLines, pos, "", "IsAdminAuthenticatedEnd", out posToCheckFrom);
                        if (!Authenticator.CheckAdminAuthentication(request))
                        {
                            for (int i = pos; i < posToCheckFrom; i++)
                                newLines.RemoveAt(newLinesPos + 1);
                            pos = posToCheckFrom;
                        }
                    }
                    else if (line.Trim().StartsWith("{IsNotAdminAuthenticatedBegin}"))
                    {
                        newLines.RemoveAt(newLinesPos--);
                        int posToCheckFrom;
                        List<string> repeatedLines = FindLines(lines, newLines, pos, "", "IsNotAdminAuthenticatedEnd", out posToCheckFrom);
                        if (Authenticator.CheckAdminAuthentication(request))
                        {
                            for (int i = pos; i < posToCheckFrom; i++)
                                newLines.RemoveAt(newLinesPos + 1);
                            pos = posToCheckFrom;
                        }
                    }
                }
                newLinesPos++;
            }
            return string.Join("\n", newLines.ToArray());
        }

        private static List<string> ExtractLines(string[] lines, List<string> newLines, int pos, string keyToCheck, string type, out int posToCheckFrom)
        {
            posToCheckFrom = pos + 1;
            List<string> repeatedLines = new List<string>();
            while (!lines[posToCheckFrom].Trim().StartsWith("{" + keyToCheck + type + "}"))
                repeatedLines.Add(lines[posToCheckFrom++]);

            for (int i = pos; i < posToCheckFrom + 1; i++)
                newLines.RemoveAt(pos);
            return repeatedLines;
        }

        private static List<string> FindLines(string[] lines, List<string> newLines, int pos, string keyToCheck, string type, out int posToCheckFrom)
        {
            posToCheckFrom = pos + 1;
            List<string> repeatedLines = new List<string>();
            while (!lines[posToCheckFrom].Trim().StartsWith("{" + keyToCheck + type + "}"))
                repeatedLines.Add(lines[posToCheckFrom++]);

            return repeatedLines;
        }

        protected string GetContentType(string filename, OSHttpResponse response)
        {
            if (!File.Exists(filename))
                return null;
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
}
