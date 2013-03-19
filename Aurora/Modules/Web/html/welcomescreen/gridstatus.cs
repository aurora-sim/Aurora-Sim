using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using Nini.Config;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using RegionFlags = Aurora.Framework.Services.RegionFlags;

namespace Aurora.Modules.Web
{
    public class GridStatusPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "html/welcomescreen/gridstatus.html"
                           };
            }
        }

        public bool RequiresAuthentication
        {
            get { return false; }
        }

        public bool RequiresAdminAuthentication
        {
            get { return false; }
        }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
                                               OSHttpResponse httpResponse, Dictionary<string, object> requestParameters,
                                               ITranslator translator, out string response)
        {
            response = null;
            var vars = new Dictionary<string, object>();

            IAgentInfoConnector users = Framework.Utilities.DataManager.RequestPlugin<IAgentInfoConnector>();
            IGenericsConnector connector = Framework.Utilities.DataManager.RequestPlugin<IGenericsConnector>();
            GridWelcomeScreen welcomeInfo = connector.GetGeneric<GridWelcomeScreen>(UUID.Zero, "GridWelcomeScreen",
                                                                                    "GridWelcomeScreen");
            if (welcomeInfo == null)
                welcomeInfo = GridWelcomeScreen.Default;

            IConfigSource config = webInterface.Registry.RequestModuleInterface<ISimulationBase>().ConfigSource;
            vars.Add("GridStatus", translator.GetTranslatedString("GridStatus"));
            vars.Add("GridOnline",
                     welcomeInfo.GridStatus
                         ? translator.GetTranslatedString("Online")
                         : translator.GetTranslatedString("Offline"));
            vars.Add("TotalUserCount", translator.GetTranslatedString("TotalUserCount"));
            vars.Add("UserCount", webInterface.Registry.RequestModuleInterface<IUserAccountService>().
                                               NumberOfUserAccounts(null, "").ToString());
            vars.Add("TotalRegionCount", translator.GetTranslatedString("TotalRegionCount"));
            vars.Add("RegionCount", Framework.Utilities.DataManager.RequestPlugin<IRegionData>().
                                                Count((RegionFlags) 0, (RegionFlags) 0).ToString());
            vars.Add("UniqueVisitors", translator.GetTranslatedString("UniqueVisitors"));
            vars.Add("UniqueVisitorCount",
                     users.RecentlyOnline((uint) TimeSpan.FromDays(30).TotalSeconds, false).ToString());
            vars.Add("OnlineNow", translator.GetTranslatedString("OnlineNow"));
            vars.Add("OnlineNowCount", users.RecentlyOnline(5*60, true).ToString());
            vars.Add("HGActiveText", translator.GetTranslatedString("HyperGrid"));
            string disabled = translator.GetTranslatedString("Disabled"),
                   enabled = translator.GetTranslatedString("Enabled");
            vars.Add("HGActive", disabled + "(TODO: FIX)");
            vars.Add("VoiceActiveLabel", translator.GetTranslatedString("Voice"));
            vars.Add("VoiceActive",
                     config.Configs["Voice"] != null &&
                     config.Configs["Voice"].GetString("Module", "GenericVoice") != "GenericVoice"
                         ? enabled
                         : disabled);
            vars.Add("CurrencyActiveLabel", translator.GetTranslatedString("Currency"));
            vars.Add("CurrencyActive",
                     webInterface.Registry.RequestModuleInterface<IMoneyModule>() != null ? enabled : disabled);

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}