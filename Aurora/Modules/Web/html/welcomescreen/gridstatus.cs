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
    public class GridStatusPage : IWebInterfacePage
    {
        public string FilePath { get { return "html/welcomescreen/gridstatus.html"; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, Hashtable query, OSHttpResponse httpResponse,
            Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();

            IConfigSource config = webInterface.Registry.RequestModuleInterface<ISimulationBase>().ConfigSource;
            vars.Add("GridStatus", translator.GetTranslatedString("GridStatus"));
            vars.Add("GridOnline", webInterface._gridIsOnline);
            vars.Add("TotalUserCount", translator.GetTranslatedString("TotalUserCount"));
            vars.Add("UserCount", webInterface.Registry.RequestModuleInterface<IUserAccountService>().
                NumberOfUserAccounts(UUID.Zero, "").ToString());
            vars.Add("TotalRegionCount", translator.GetTranslatedString("TotalRegionCount"));
            vars.Add("RegionCount", DataManager.DataManager.RequestPlugin<IRegionData>().
                Count((Framework.RegionFlags)0, (Framework.RegionFlags)0).ToString());
            vars.Add("UniqueVisitors", translator.GetTranslatedString("UniqueVisitors"));
            IAgentInfoConnector users = DataManager.DataManager.RequestPlugin<IAgentInfoConnector>();
            vars.Add("UniqueVisitorCount", users.RecentlyOnline((uint)TimeSpan.FromDays(30).TotalSeconds, false).ToString());
            vars.Add("OnlineNow", translator.GetTranslatedString("OnlineNow"));
            vars.Add("OnlineNowCount", users.RecentlyOnline(5 * 60, true).ToString());
            vars.Add("HGActiveText", translator.GetTranslatedString("HyperGrid"));
            string disabled = translator.GetTranslatedString("Disabled"),
                enabled = translator.GetTranslatedString("Enabled");
            vars.Add("HGActive", disabled + "(TODO: FIX)");
            vars.Add("VoiceActiveLabel", translator.GetTranslatedString("Voice"));
            vars.Add("VoiceActive", config.Configs["Voice"] != null && config.Configs["Voice"].GetString("Module", "GenericVoice") != "GenericVoice" ? enabled : disabled);
            vars.Add("CurrencyActiveLabel", translator.GetTranslatedString("Currency"));
            vars.Add("CurrencyActive", webInterface.Registry.RequestModuleInterface<IMoneyModule>() != null ? enabled : disabled);

            return vars;
        }
    }
}
