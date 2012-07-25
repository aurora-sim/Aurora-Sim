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
    public class JQueryColorBoxPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                       {
                           "html/javascripts/jquery.colorbox.js"
                       };
            }
        }

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
            OSHttpResponse httpResponse, Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();
			
			vars.Add("ColorBoxImageText", translator.GetTranslatedString("ColorBoxImageText"));
			vars.Add("ColorBoxOfText", translator.GetTranslatedString("ColorBoxOfText"));
			vars.Add("ColorBoxPreviousText", translator.GetTranslatedString("ColorBoxPreviousText"));
			vars.Add("ColorBoxNextText", translator.GetTranslatedString("ColorBoxNextText"));
			vars.Add("ColorBoxCloseText", translator.GetTranslatedString("ColorBoxCloseText"));
			vars.Add("ColorBoxStartSlideshowText", translator.GetTranslatedString("ColorBoxStartSlideshowText"));
			vars.Add("ColorBoxStopSlideshowText", translator.GetTranslatedString("ColorBoxStopSlideshowText"));

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}
