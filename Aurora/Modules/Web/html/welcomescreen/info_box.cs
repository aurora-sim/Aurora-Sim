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
    public class InfoBoxPage : IWebInterfacePage
    {
        public string FilePath { get { return "html/welcomescreen/info_box.html"; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, Hashtable query, OSHttpResponse httpResponse,
            Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();

            vars.Add("Title", webInterface._infoMessageTitle);
            vars.Add("Text", webInterface._infoMessageText);
            vars.Add("Color", webInterface._infoMessageColor);

            return vars;
        }
    }
}
