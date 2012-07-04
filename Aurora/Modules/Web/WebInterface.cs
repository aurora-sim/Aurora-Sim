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

namespace Aurora.Modules.Communications
{
    public class WebInterface : IService, IWebInterfaceModule
    {
        private IRegistryCore _registry;
        private string _infoMessageTitle = "Nothing to report at this time.";
        private string _infoMessageText = "Grid is up and running.";
        private string _infoMessageColor = "white";
        private string _gridIsOnline = "OFFLINE";
        private uint _port = 8002;
        private bool _enabled = true;

        public string LoginScreenURL { get { return MainServer.Instance.FullHostName + ":" + _port + "/welcomescreen/"; } }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            _registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig con = config.Configs["WebInterface"];
            if (con != null)
            {
                _enabled = con.GetString("Module", "BuiltIn") == "BuiltIn";
                _port = con.GetUInt("Port", _port);
                _infoMessageTitle = con.GetString("InfoMessageTitle", _infoMessageTitle);
                _infoMessageText = con.GetString("InfoMessageText", _infoMessageText);
                _infoMessageColor = con.GetString("InfoMessageColor", _infoMessageColor);
                _gridIsOnline = con.GetString("GridIsOnline", _gridIsOnline);
            }
            if (_enabled)
            {
                _registry.RegisterModuleInterface<IWebInterfaceModule>(this);
                var server = registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(_port);
                server.AddHTTPHandler("/welcomescreen/", new GenericStreamHandler("GET", "/welcomescreen/", WelcomePage));

                MainConsole.Instance.Commands.AddCommand("web add news item", "web add news item", "Adds a news item to the web interface", (s) => AddNewsItem());
                MainConsole.Instance.Commands.AddCommand("web remove news item", "web remove news item", "Removes a news item to the web interface", (s) => RemoveNewsItem());
            }
        }

        private void AddNewsItem()
        {
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            GridNewsItem item = new GridNewsItem();
            item.Time = DateTime.Now;
            item.Title = MainConsole.Instance.Prompt("News Item Title: ");
            MainConsole.Instance.Info("News Item Text (will continue reading until a blank line is inputted): ");
            string curText;
            while ((curText = Console.ReadLine()) != "")
                item.Text += "\n" + curText;
            item.ID = connector.GetGenericCount(UUID.Zero, "WebGridNews");
            connector.AddGeneric(UUID.Zero, "WebGridNews", item.ID.ToString(), item.ToOSD());
            MainConsole.Instance.Info("News item was added");
        }

        private void RemoveNewsItem()
        {
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            var items = connector.GetGenerics<GridNewsItem>(UUID.Zero, "WebGridNews");
            string title = MainConsole.Instance.Prompt("News item title to remove: ");
            var foundItems = from i in items where i.Title == title select i;
            var item = foundItems.FirstOrDefault();
            if (item == null)
            {
                MainConsole.Instance.Info("No item found");
                return;
            }
            connector.RemoveGeneric(UUID.Zero, "WebGridNews", item.ID.ToString());
            MainConsole.Instance.Info("News item was removed");
        }

        public void FinishedStartup()
        {
        }

        public byte[] WelcomePage(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            byte[] response = MainServer.BlankResponse;
            string filename = GetFileNameFromHTMLPath(path);
            httpResponse.ContentType = GetContentType(filename);

            MainConsole.Instance.Warn("Serving " + filename);

            if (httpResponse.ContentType == "text/html")
            {
                Dictionary<string, object> vars = new Dictionary<string, object>();
                AddVars(Path.GetFileNameWithoutExtension(filename), httpRequest.Query, ref vars);
                response = Encoding.UTF8.GetBytes(ConvertHTML(File.ReadAllText(filename), httpRequest.Query, vars));
            }
            else
                response = File.ReadAllBytes(filename);
            return response;
        }

        private string ConvertHTML(string file, Hashtable query, Dictionary<string, object> vars)
        {
            string html = CSHTMLCreator.BuildHTML(file, vars);

            string[] lines = html.Split('\n');
            List<string> newLines = new List<string>(lines);
            int pos = 0;
            foreach (string line in lines)
            {
                if (line.Contains("<!--#include file="))
                {
                    string[] split = line.Split(new string[2] { "<!--#include file=\"", "\" -->" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = split.Length % 2 == 0 ? 0 : 1; i < split.Length; i += 2)
                    {
                        string filename = GetFileNameFromHTMLPath(split[i]);
                        Dictionary<string, object> newVars = new Dictionary<string, object>();
                        AddVars(Path.GetFileNameWithoutExtension(filename), query, ref newVars);
                        newLines[pos] = ConvertHTML(File.ReadAllText(filename), query, newVars);
                    }
                }
                else if(line.Trim().StartsWith("{"))
                {
                    int ind;
                    if ((ind = line.IndexOf("ArrayBegin}")) != -1)
                    {
                        string keyToCheck = line.Substring(1, ind - 1);
                        int posToCheckFrom = pos + 1;
                        List<string> repeatedLines = new List<string>();
                        while (!lines[posToCheckFrom].Trim().StartsWith("{" + keyToCheck + "ArrayEnd}"))
                            repeatedLines.Add(lines[posToCheckFrom++]);

                        for (int i = pos; i < posToCheckFrom + 1; i++)
                            newLines.RemoveAt(pos);
                        if (vars.ContainsKey(keyToCheck))
                        {
                            foreach (var dict in vars[keyToCheck] as List<Dictionary<string, object>>)
                                newLines.Insert(pos++, ConvertHTML(string.Join("\n", repeatedLines.ToArray()), query, dict));
                        }
                    }
                }
                pos++;
            }
            return string.Join("\n", newLines.ToArray());
        }

        private void AddVars(string filename, Hashtable query, ref Dictionary<string, object> vars)
        {
            vars.Add("SystemURL", MainServer.Instance.FullHostName + ":" + _port);
            vars.Add("SystemName", "Testing Grid!");

            if (filename == "gridstatus")
            {
                IConfigSource config = _registry.RequestModuleInterface<ISimulationBase>().ConfigSource;
                vars.Add("GridStatus", "GRID STATUS");
                vars.Add("GridOnline", _gridIsOnline);
                vars.Add("TotalUserCount", "Total Users");
                vars.Add("UserCount", _registry.RequestModuleInterface<IUserAccountService>().
                    NumberOfUserAccounts(UUID.Zero, "").ToString());
                vars.Add("TotalRegionCount", "Total Region Count");
                vars.Add("RegionCount", DataManager.DataManager.RequestPlugin<IRegionData>().
                    Count((Framework.RegionFlags)0, (Framework.RegionFlags)0).ToString());
                vars.Add("UniqueVisitors", "Unique Visitors last 30 days");
                IAgentInfoConnector users = DataManager.DataManager.RequestPlugin<IAgentInfoConnector>();
                vars.Add("UniqueVisitorCount", users.RecentlyOnline((uint)TimeSpan.FromDays(30).TotalSeconds, false).ToString());
                vars.Add("OnlineNow", "Online Now");
                vars.Add("OnlineNowCount", users.RecentlyOnline(5 * 60, true).ToString());
                vars.Add("HGActiveText", "HyperGrid (HG)");
                vars.Add("HGActive", "Disabled (TODO: FIX)");
                vars.Add("VoiceActiveLabel", "Voice");
                vars.Add("VoiceActive", config.Configs["Voice"] != null && config.Configs["Voice"].GetString("Module", "GenericVoice") != "GenericVoice" ? "Enabled" : "Disabled");
                vars.Add("CurrencyActiveLabel", "Currency");
                vars.Add("CurrencyActive", _registry.RequestModuleInterface<IMoneyModule>() != null ? "Enabled" : "Disabled");
            }
            else if (filename == "region_box")
            {
                List<Dictionary<string, object>> RegionListVars = new List<Dictionary<string, object>>();
                var sortBy = new Dictionary<string, bool>();
                if (query.ContainsKey("region"))
                    sortBy.Add(query["region"].ToString(), true);
                var regions = DataManager.DataManager.RequestPlugin<IRegionData>().Get((Framework.RegionFlags)0, 
                    Framework.RegionFlags.Hyperlink | Framework.RegionFlags.Foreign | Framework.RegionFlags.Hidden,
                    null, null, sortBy);
                foreach(var region in regions)
                    RegionListVars.Add(new Dictionary<string, object> { { "RegionLocX", region.RegionLocX / Constants.RegionSize }, 
                    { "RegionLocY", region.RegionLocY / Constants.RegionSize }, { "RegionName", region.RegionName } });

                vars.Add("RegionList", RegionListVars);
                vars.Add("RegionText", "Region");
            }
            else if (filename == "info_box")
            {
                vars.Add("Title", _infoMessageTitle);
                vars.Add("Text", _infoMessageText);
                vars.Add("Color", _infoMessageColor);
            }
            else if (filename == "news")
            {
                vars.Add("News", "News");

                IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
                var newsItems = connector.GetGenerics<GridNewsItem>(UUID.Zero, "WebGridNews");
                if (newsItems.Count == 0)
                    newsItems.Add(GridNewsItem.NoNewsItem);
                vars.Add("NewsList", newsItems.ConvertAll<Dictionary<string, object>>(item => item.ToDictionary()));
                vars.Add("Color", _infoMessageColor);
            }
        }

        private string GetContentType(string filename)
        {
            switch(Path.GetExtension(filename))
            {
                case ".jpeg":
                case ".jpg":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                case ".png":
                    return "image/png";
                case ".tiff":
                    return "image/tiff";
                case ".html":
                case ".htm":
                    return "text/html";
                case ".css":
                    return "text/css";
            }
            return "text/plain";
        }

        private string GetFileNameFromHTMLPath(string path)
        {
            string file = Path.Combine("html/", path.StartsWith("/") ? path.Remove(0, 1) : path);
            if (Path.GetFileName(file) == "")
                file = Path.Combine(file, "index.html");
            return file;
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
            map["Text"] = Title;
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
}
