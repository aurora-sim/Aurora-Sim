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

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, Hashtable query, OSHttpResponse httpResponse, Dictionary<string, object> requestParameters)
        {
            var vars = new Dictionary<string, object>();

            IConfigSource config = webInterface.Registry.RequestModuleInterface<ISimulationBase>().ConfigSource;
            vars.Add("GridStatus", "GRID STATUS");
            vars.Add("GridOnline", webInterface._gridIsOnline);
            vars.Add("TotalUserCount", "Total Users");
            vars.Add("UserCount", webInterface.Registry.RequestModuleInterface<IUserAccountService>().
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
            vars.Add("CurrencyActive", webInterface.Registry.RequestModuleInterface<IMoneyModule>() != null ? "Enabled" : "Disabled");

            return vars;
        }
    }
}
