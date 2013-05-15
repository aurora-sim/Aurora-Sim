/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
#if NET_4_5
using System.Net.Http;
using System.Threading.Tasks;
#endif
using System.Web;
using System.Xml;
using Aurora.Framework.Servers.HttpServer;

namespace Aurora.Framework.Utilities
{
    public static class WebUtils
    {
        private const int m_defaultTimeout = 10000;

#if NET_4_5

        /// <summary>
        ///     POST URL-encoded form data to a web service that returns LLSD or
        ///     JSON data
        /// </summary>
        public static string PostToService(string url, OSDMap data)
        {
            byte[] buffer = data != null ? Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(data, true)) : null;
            Task<byte[]> t = ServiceOSDRequest(url, buffer, "POST", m_defaultTimeout);
            t.Wait();
            return Encoding.UTF8.GetString(t.Result);
        }

        public static byte[] PostToService(string url, byte[] data)
        {
            Task<byte[]> t = ServiceOSDRequest(url, data, "POST", m_defaultTimeout);
            t.Wait();
            return t.Result;
        }

        /// <summary>
        ///     GET JSON-encoded data to a web service that returns LLSD or
        ///     JSON data
        /// </summary>
        public static string GetFromService(string url)
        {
            Task<byte[]> t = ServiceOSDRequest(url, null, "GET", m_defaultTimeout);
            t.Wait();
            return Encoding.UTF8.GetString(t.Result);
        }

        /// <summary>
        ///     PUT JSON-encoded data to a web service that returns LLSD or
        ///     JSON data
        /// </summary>
        public static string PutToService(string url, OSDMap data)
        {
            byte[] buffer = data != null ? Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(data, true)) : null;
            Task<byte[]> t = ServiceOSDRequest(url, buffer, "PUT", m_defaultTimeout);
            t.Wait();
            return Encoding.UTF8.GetString(t.Result);
        }

        /// <summary>
        ///     DELETE JSON-encoded data to a web service
        /// </summary>
        public static void DeleteFromService(string url)
        {
            Task<byte[]> t = ServiceOSDRequest(url, null, "DELETE", m_defaultTimeout);
            t.Wait();
        }

        public static async Task<byte[]> ServiceOSDRequest(string url, byte[] buffer, string method, int timeout)
        {
            string errorMessage = "";
            byte[] response = null;
            int tickstart = Util.EnvironmentTickCount(), tickelapsed = 0;
            try
            {
                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromMilliseconds(timeout);

                HttpResponseMessage httpresponse;
                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    switch (method)
                    {
                        case "PUT":
                            httpresponse = await client.PutAsync(url, new StreamContent(stream));
                            break;
                        case "DELETE":
                            httpresponse = await client.DeleteAsync(url);
                            break;
                        case "POST":
                            httpresponse = await client.PostAsync(url, new StreamContent(stream));
                            break;
                        case "GET":
                            httpresponse = await client.GetAsync(url);
                            break;
                        default:
                            httpresponse = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url) { Content = new StreamContent(stream) });
                            break;
                    }
                }
                httpresponse.EnsureSuccessStatusCode();

                response = await httpresponse.Content.ReadAsByteArrayAsync();
                tickelapsed = Util.EnvironmentTickCountSubtract(tickstart);

                if (MainConsole.Instance != null)
                {
                    if (errorMessage == "")//No error
                    {
                        // This just dumps a warning for any operation that takes more than 500 ms
                        if (MainConsole.Instance.IsDebugEnabled)
                        {
                            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();

                            MainConsole.Instance.Debug(
                                string.Format("[WebUtils]: request (URI:{0}, METHOD:{1}, UPSTACK(4):{3}) took {2}ms",
                                url, method, tickelapsed,
                                stackTrace.GetFrame(3).GetMethod().Name));
                        }
                        if (tickelapsed > 5000)
                            MainConsole.Instance.Info(
                                string.Format("[WebUtils]: request took too long (URI:{0}, METHOD:{1}) took {2}ms",
                                url, method, tickelapsed));
                    }
                }
            }
            catch (Exception ex)
            {
                if (MainConsole.Instance != null)
                    MainConsole.Instance.WarnFormat("[WebUtils] request failed: {0} to {1}", ex.ToString(), url);
            }
            return response;
        }

#else

        /// <summary>
        ///     POST URL-encoded form data to a web service that returns LLSD or
        ///     JSON data
        /// </summary>
        public static string PostToService(string url, OSDMap data)
        {
            byte[] buffer = data != null ? Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(data, true)) : null;
            return Encoding.UTF8.GetString(ServiceOSDRequest(url, buffer, "POST", m_defaultTimeout));
        }

        /// <summary>
        ///     POST URL-encoded form data to a web service that returns LLSD or
        ///     JSON data
        /// </summary>
        public static byte[] PostToService(string url, byte[] data)
        {
            return ServiceOSDRequest(url, data, "POST", m_defaultTimeout);
        }

        /// <summary>
        ///     GET JSON-encoded data to a web service that returns LLSD or
        ///     JSON data
        /// </summary>
        public static string GetFromService(string url)
        {
            return Encoding.UTF8.GetString(ServiceOSDRequest(url, null, "GET", m_defaultTimeout));
        }

        /// <summary>
        ///     PUT JSON-encoded data to a web service that returns LLSD or
        ///     JSON data
        /// </summary>
        public static string PutToService(string url, OSDMap data)
        {
            byte[] buffer = data != null ? Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(data, true)) : null;
            return Encoding.UTF8.GetString(ServiceOSDRequest(url, buffer, "PUT", m_defaultTimeout));
        }

        /// <summary>
        ///     PUT JSON-encoded data to a web service that returns LLSD or
        ///     JSON data
        /// </summary>
        public static string DeleteFromService(string url)
        {
            return Encoding.UTF8.GetString(ServiceOSDRequest(url, null, "DELETE", m_defaultTimeout));
        }

        public static byte[] ServiceOSDRequest(string url, byte[] buffer, string method, int timeout)
        {
            // MainConsole.Instance.DebugFormat("[WEB UTIL]: <{0}> start osd request for {1}, method {2}",reqnum,url,method);

            string errorMessage = "unknown error";
            int tickstart = Util.EnvironmentTickCount();
            int tickdata = 0;
            int tickserialize = 0;
            HttpWebRequest request = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = method;
                request.Timeout = timeout;
                request.KeepAlive = false;
                request.MaximumAutomaticRedirections = 10;
                request.ReadWriteTimeout = timeout / 4;
                request.SendChunked = true;

                // If there is some input, write it into the request
                if (buffer != null && buffer.Length > 0)
                {
                    request.ContentType = "application/json";
                    request.ContentLength = buffer.Length; //Count bytes to send
                    using (Stream requestStream = request.GetRequestStream())
                        HttpServerHandlerHelpers.WriteChunked(requestStream, buffer);
                }

                // capture how much time was spent writing, this may seem silly
                // but with the number concurrent requests, this often blocks
                tickdata = Util.EnvironmentTickCountSubtract(tickstart);

                using (WebResponse response = request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        tickserialize = Util.EnvironmentTickCountSubtract(tickstart) - tickdata;
                        return HttpServerHandlerHelpers.ReadBytes(responseStream);
                    }
                }
            }
            catch (WebException we)
            {
                errorMessage = we.Message;
                if (we.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse webResponse = (HttpWebResponse)we.Response;
                    if (webResponse.StatusCode == HttpStatusCode.BadRequest)
                        MainConsole.Instance.WarnFormat("[WebUtils]: bad request to {0}, data {1}", url,
                                                        buffer != null ? OSDParser.SerializeJsonString(buffer) : "");
                    else
                        MainConsole.Instance.Warn(string.Format("[WebUtils]: {0} to {1}, data {2}, response {3}",
                                                        webResponse.StatusCode, url,
                                                        buffer != null ? OSDParser.SerializeJsonString(buffer) : "",
                                                        webResponse.StatusDescription));
                    return new byte[0];
                }
                if (request != null)
                    request.Abort();
            }
            catch (Exception ex)
            {
                if (ex is System.UriFormatException)
                    errorMessage = ex.ToString();
                else
                    errorMessage = ex.Message;
                if (request != null)
                    request.Abort();
            }
            finally
            {
                if (MainConsole.Instance != null)
                {
                    if (errorMessage == "unknown error")
                    {
                        // This just dumps a warning for any operation that takes more than 500 ms
                        int tickdiff = Util.EnvironmentTickCountSubtract(tickstart);
                        if (MainConsole.Instance.IsTraceEnabled)
                        {
                            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();

                            MainConsole.Instance.Trace(
                                string.Format("[WebUtils]: osd request (URI:{0}, METHOD:{1}, UPSTACK(4):{5}) took {2}ms overall, {3}ms writing, {4}ms deserializing",
                                url, method, tickdiff, tickdata, tickserialize,
                                stackTrace.GetFrame(4).GetMethod().Name));
                        }
                        else if (MainConsole.Instance.IsDebugEnabled)
                            MainConsole.Instance.Debug(
                                string.Format("[WebUtils]: request (URI:{0}, METHOD:{1}) took {2}ms overall, {3}ms writing, {4}ms deserializing",
                                url, method, tickdiff, tickdata, tickserialize));
                        if (tickdiff > 5000)
                            MainConsole.Instance.Info(
                                string.Format("[WebUtils]: request took too long (URI:{0}, METHOD:{1}) took {2}ms overall, {3}ms writing, {4}ms deserializing",
                                url, method, tickdiff, tickdata, tickserialize));
                    }
                }
            }

            if (MainConsole.Instance != null)
                MainConsole.Instance.WarnFormat("[WebUtils]: request failed: {0} to {1}, data {2}", errorMessage,
                                                url, buffer != null ? OSDParser.SerializeJsonString(buffer) : "");
            return new byte[0];
        }

#endif

        /// <summary>
        ///     Takes the value of an Accept header and returns the preferred types
        ///     ordered by q value (if it exists).
        ///     Example input: image/jpg;q=0.7, image/png;q=0.8, image/jp2
        ///     Exmaple output: ["jp2", "png", "jpg"]
        ///     NOTE: This doesn't handle the semantics of *'s...
        /// </summary>
        /// <param name="accept"></param>
        /// <returns></returns>
        public static string[] GetPreferredImageTypes(string accept)
        {
            if (string.IsNullOrEmpty(accept))
                return new string[0];

            string[] types = accept.Split(new[] {','});
            if (types.Length > 0)
            {
                List<string> list = new List<string>(types);

                list.RemoveAll(s => !s.ToLower().StartsWith("image"));

                ArrayList tlist = new ArrayList(list);
                tlist.Sort(new QBasedComparer());

                string[] result = new string[tlist.Count];
                for (int i = 0; i < tlist.Count; i++)
                {
                    string mime = (string) tlist[i];
                    string[] parts = mime.Split(new[] {';'});
                    string[] pair = parts[0].Split(new[] {'/'});
                    if (pair.Length == 2)
                        result[i] = pair[1].ToLower();
                    else // oops, we don't know what this is...
                        result[i] = pair[0];
                }

                return result;
            }
            return new string[0];
        }

        public static OSDMap GetOSDMap(string data, bool doLogMessages)
        {
            if (data == "")
                return null;
            try
            {
                // We should pay attention to the content-type, but let's assume we know it's Json
                OSD buffer = OSDParser.DeserializeJson(data);
                if (buffer.Type == OSDType.Map)
                {
                    OSDMap args = (OSDMap) buffer;
                    return args;
                }
                // uh?
                if (doLogMessages)
                    MainConsole.Instance.Warn(("[WebUtils]: Got OSD of unexpected type " + buffer.Type.ToString()));
                return null;
            }
            catch (Exception ex)
            {
                if (doLogMessages)
                {
                    MainConsole.Instance.Warn("[WebUtils]: exception on parse of REST message " + ex);
                    MainConsole.Instance.Warn("[WebUtils]: bad data: " + data);
                }
                return null;
            }
        }

        #region Nested type: QBasedComparer

        public class QBasedComparer : IComparer
        {
            #region IComparer Members

            public int Compare(Object x, Object y)
            {
                float qx = GetQ(x);
                float qy = GetQ(y);
                if (qx < qy)
                    return -1;
                if (qx == qy)
                    return 0;
                return 1;
            }

            #endregion

            private float GetQ(Object o)
            {
                // Example: image/png;q=0.9

                if (o is String)
                {
                    string mime = (string) o;
                    string[] parts = mime.Split(new[] {';'});
                    if (parts.Length > 1)
                    {
                        string[] kvp = parts[1].Split(new[] {'='});
                        if (kvp.Length == 2 && kvp[0] == "q")
                        {
                            float qvalue = 1F;
                            float.TryParse(kvp[1], out qvalue);
                            return qvalue;
                        }
                    }
                }

                return 1F;
            }
        }

        #endregion
    }

    public static class XMLUtils
    {
        public static string BuildXmlResponse(Dictionary<string, object> data)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                                             "", "");
            // Set the encoding declaration.
            ((XmlDeclaration)xmlnode).Encoding = "UTF-8";
            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                                                       "");

            doc.AppendChild(rootElement);

            BuildXmlData(rootElement, data);

            return doc.InnerXml;
        }

        private static void BuildXmlData(XmlElement parent, Dictionary<string, object> data)
        {
            foreach (KeyValuePair<string, object> kvp in data)
            {
                if (kvp.Value == null)
                    continue;

                if (parent.OwnerDocument != null)
                {
                    XmlElement elem = parent.OwnerDocument.CreateElement("",
                                                                         kvp.Key, "");

                    if (kvp.Value is Dictionary<string, object>)
                    {
                        XmlAttribute type = parent.OwnerDocument.CreateAttribute("",
                                                                                 "type", "");
                        type.Value = "List";

                        elem.Attributes.Append(type);

                        BuildXmlData(elem, (Dictionary<string, object>)kvp.Value);
                    }
                    else if (kvp.Value is Dictionary<string, string>)
                    {
                        XmlAttribute type = parent.OwnerDocument.CreateAttribute("",
                                                                                 "type", "");
                        type.Value = "List";

                        elem.Attributes.Append(type);
#if (!ISWIN)
                        Dictionary<string, object> value = new Dictionary<string, object>();
                        foreach (KeyValuePair<string, string> pair in ((Dictionary<string, string>) kvp.Value))
                            value.Add(pair.Key, pair.Value);
#else
                        Dictionary<string, object> value = ((Dictionary<string, string>)kvp.Value).ToDictionary<KeyValuePair<string, string>, string, object>(pair => pair.Key, pair => pair.Value);
#endif
                        BuildXmlData(elem, value);
                    }
                    else
                    {
                        elem.AppendChild(parent.OwnerDocument.CreateTextNode(
                            kvp.Value.ToString()));
                    }

                    parent.AppendChild(elem);
                }
            }
        }

        public static Dictionary<string, object> ParseXmlResponse(string data)
        {
            //MainConsole.Instance.DebugFormat("[XXX]: received xml string: {0}", data);

            Dictionary<string, object> ret = new Dictionary<string, object>();

            XmlDocument doc = new XmlDocument();

            doc.LoadXml(data);

            XmlNodeList rootL = doc.GetElementsByTagName("ServerResponse");

            if (rootL.Count != 1)
                return ret;

            XmlNode rootNode = rootL[0];

            ret = ParseElement(rootNode);

            return ret;
        }

        private static Dictionary<string, object> ParseElement(XmlNode element)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();

            XmlNodeList partL = element.ChildNodes;

            foreach (XmlNode part in partL)
            {
                if (part.Attributes != null)
                {
                    XmlNode type = part.Attributes.GetNamedItem("type");
                    if (type == null || type.Value != "List")
                    {
                        ret[part.Name] = part.InnerText;
                    }
                    else
                    {
                        ret[part.Name] = ParseElement(part);
                    }
                }
            }

            return ret;
        }
    }
}