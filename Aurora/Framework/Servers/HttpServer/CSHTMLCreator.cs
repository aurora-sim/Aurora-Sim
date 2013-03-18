using Aurora.Framework.Servers.HttpServer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace Aurora.Framework
{
    public delegate string HTTPReturned(Dictionary<string, string> variables);
    public class CSHTMLCreator
    {
        public static string AddHTMLPage(string html, string urlToAppend, string methodName, Dictionary<string, object> variables, HTTPReturned eventDelegate)
        {
            string secret = Util.RandomClass.Next(0, int.MaxValue).ToString();
            string secret2 = Util.RandomClass.Next(0, int.MaxValue).ToString();
            string navUrl = MainServer.Instance.ServerURI +
                (urlToAppend == "" ? "" : "/") + urlToAppend + "/index.php?method=" + methodName + secret2;
            string url = MainServer.Instance.ServerURI +
                (urlToAppend == "" ? "" : "/") + urlToAppend + "/index.php?method=" + methodName + secret;
            MainServer.Instance.RemoveHTTPHandler(null, methodName + secret);
            MainServer.Instance.RemoveHTTPHandler(null, methodName + secret2);
            variables["url"] = url;
            MainServer.Instance.AddHTTPHandler(new GenericStreamHandler("GET", methodName + secret2, delegate(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                MainServer.Instance.RemoveHTTPHandler(null, methodName + secret2);
                return SetUpWebpage(httpResponse, url, html, variables);
            }));
            MainServer.Instance.AddHTTPHandler(new GenericStreamHandler("GET", "/index.php?method=" + methodName + secret, delegate(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                MainServer.Instance.RemoveHTTPHandler(null, "/index.php?method=" + methodName + secret);
                return HandleResponse(httpRequest, httpResponse, request, urlToAppend, variables, eventDelegate);
            }));
            return navUrl;
        }

        private static byte[] HandleResponse(OSHttpRequest httpRequest, OSHttpResponse response, Stream stream, string urlToAppend, Dictionary<string, object> variables, HTTPReturned eventHandler)
        {
            Uri myUri = new Uri("http://localhost/index.php?" + stream.ReadUntilEnd());
            Dictionary<string, string> newVars = new Dictionary<string, string>();
            foreach (string key in variables.Keys)
            {
                newVars[key] = HttpUtility.ParseQueryString(myUri.Query).Get(key);
            }
            string url = eventHandler(newVars);

            string html = "<html>" +
                (url == "" ? "" :
("<head>" +
"<meta http-equiv=\"REFRESH\" content=\"0;url=" + url + "\"></HEAD>")) +
"</HTML>";
            response.ContentType = "text/html";

            return Encoding.UTF8.GetBytes(html);
        }

        private static byte[] SetUpWebpage(OSHttpResponse response, string url, string html, Dictionary<string, object> vars)
        {
            response.ContentType = "text/html";
            return Encoding.UTF8.GetBytes(BuildHTML(html, vars));
        }

        public static string BuildHTML(string html, Dictionary<string, object> vars)
        {
            if (vars == null) return html;
            foreach (KeyValuePair<string, object> kvp in vars)
            {
                if(!(kvp.Value is IList))
                    html = html.Replace("{" + kvp.Key + "}", kvp.Value.ToString());
            }
            return html;
        }
    }
}
