using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework.Servers.HttpServer;

namespace Aurora.Modules.Web
{
    public class WelcomeScreenMain : IWebInterfacePage
    {
        public string FilePath { get { return "html/welcomescreen/index.html"; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, Hashtable query, OSHttpResponse httpResponse,
            Dictionary<string, object> requestParameters, ITranslator translator)
        {
            return new Dictionary<string, object>();
        }
    }
}
