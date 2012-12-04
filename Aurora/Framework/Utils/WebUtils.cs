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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using log4net.Core;

namespace Aurora.Simulation.Base
{
    public static class WebUtils
    {
        // this is the header field used to communicate the local request id
        // used for performance and debugging
        public const string OSHeaderRequestID = "opensim-request-id";

        // number of milliseconds a call can take before it is considered
        // a "long" call for warning & debugging purposes
        public const int LongCallTime = 500;
        
        private const int m_defaultTimeout = 20000;

        public static Dictionary<string, object> ParseQueryString(string query)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            string[] terms = query.Split(new[] {'&'});

            if (terms.Length == 0)
                return result;

            foreach (string t in terms)
            {
                string[] elems = t.Split(new[] {'='});
                if (elems.Length == 0)
                    continue;

                string name = HttpUtility.UrlDecode(elems[0]);
                string value = String.Empty;

                if (elems.Length > 1)
                    value = HttpUtility.UrlDecode(elems[1]);

                if (name.EndsWith("[]"))
                {
                    string cleanName = name.Substring(0, name.Length - 2);
                    if (result.ContainsKey(cleanName))
                    {
                        if (!(result[cleanName] is List<string>))
                            continue;

                        List<string> l = (List<string>) result[cleanName];

                        l.Add(value);
                    }
                    else
                    {
                        List<string> newList = new List<string> {value};

                        result[cleanName] = newList;
                    }
                }
                else
                {
                    if (!result.ContainsKey(name))
                        result[name] = value;
                }
            }

            return result;
        }

        public static string BuildQueryString(Dictionary<string, object> data)
        {
            string qstring = String.Empty;

            foreach (KeyValuePair<string, object> kvp in data)
            {
                string part;
                if (kvp.Value is List<string>)
                {
                    List<string> l = (List<String>) kvp.Value;

                    foreach (string s in l)
                    {
                        part = HttpUtility.UrlEncode(kvp.Key) +
                               "[]=" + HttpUtility.UrlEncode(s);

                        if (qstring != String.Empty)
                            qstring += "&";

                        qstring += part;
                    }
                }
                else
                {
                    if (kvp.Value.ToString() != String.Empty)
                    {
                        part = HttpUtility.UrlEncode(kvp.Key) +
                               "=" + HttpUtility.UrlEncode(kvp.Value.ToString());
                    }
                    else
                    {
                        part = HttpUtility.UrlEncode(kvp.Key);
                    }

                    if (qstring != String.Empty)
                        qstring += "&";

                    qstring += part;
                }
            }

            return qstring;
        }

        public static string BuildXmlResponse(Dictionary<string, object> data)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                                             "", "");
            // Set the encoding declaration.
            ((XmlDeclaration) xmlnode).Encoding = "UTF-8";
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

                        BuildXmlData(elem, (Dictionary<string, object>) kvp.Value);
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
                        Dictionary<string, object> value = ((Dictionary<string, string>) kvp.Value).ToDictionary<KeyValuePair<string, string>, string, object>(pair => pair.Key, pair => pair.Value);
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

        /// <summary>
        ///   POST URL-encoded form data to a web service that returns LLSD or
        ///   JSON data
        /// </summary>
        public static string PostToService(string url, OSDMap data)
        {
            return ServiceOSDRequest(url, data, "POST", m_defaultTimeout);
        }

        /// <summary>
        ///   GET JSON-encoded data to a web service that returns LLSD or
        ///   JSON data
        /// </summary>
        public static string GetFromService(string url)
        {
            return ServiceOSDRequest(url, null, "GET", m_defaultTimeout);
        }

        /// <summary>
        ///   PUT JSON-encoded data to a web service that returns LLSD or
        ///   JSON data
        /// </summary>
        public static string PutToService(string url, OSDMap data)
        {
            return ServiceOSDRequest(url, data, "PUT", m_defaultTimeout);
        }

        /// <summary>
        /// Dictionary of end points
        /// </summary>
        private static Dictionary<string, object> m_endpointSerializer = new Dictionary<string, object>();

        private static object EndPointLock(string url)
        {
            System.Uri uri = new System.Uri(url);
            string endpoint = string.Format("{0}:{1}", uri.Host, uri.Port);

            lock (m_endpointSerializer)
            {
                object eplock = null;

                if (!m_endpointSerializer.TryGetValue(endpoint, out eplock))
                {
                    eplock = new object();
                    m_endpointSerializer.Add(endpoint, eplock);
                    // m_log.WarnFormat("[WEB UTIL] add a new host to end point serializer {0}",endpoint);
                }

                return eplock;
            }
        }

        public static string ServiceOSDRequest(string url, OSDMap data, string method, int timeout)
        {
            // MainConsole.Instance.DebugFormat("[WEB UTIL]: <{0}> start osd request for {1}, method {2}",reqnum,url,method);

            //lock (EndPointLock(url))
            {
                string errorMessage = "unknown error";
                int tickstart = Util.EnvironmentTickCount();
                int tickdata = 0;
                int tickserialize = 0;
                byte[] buffer = data != null ? Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(data, true)) : null;
                HttpWebRequest request = null;
                try
                {
                    request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = method;
                    request.Timeout = timeout;
                    request.KeepAlive = false;
                    request.MaximumAutomaticRedirections = 10;
                    request.ReadWriteTimeout = timeout / 4;

                    // If there is some input, write it into the request
                    if (buffer != null && buffer.Length > 0)
                    {
                        request.ContentType = "application/json";
                        request.ContentLength = buffer.Length; //Count bytes to send
                        using (Stream requestStream = request.GetRequestStream())
                            requestStream.Write(buffer, 0, buffer.Length); //Send it
                    }

                    // capture how much time was spent writing, this may seem silly
                    // but with the number concurrent requests, this often blocks
                    tickdata = Util.EnvironmentTickCountSubtract(tickstart);

                    using (WebResponse response = request.GetResponse())
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            // capture how much time was spent writing, this may seem silly
                            // but with the number concurrent requests, this often blocks
                            tickserialize = Util.EnvironmentTickCountSubtract(tickstart) - tickdata;
                            string responseStr = responseStream.GetStreamString();
                            // MainConsole.Instance.DebugFormat("[WEB UTIL]: <{0}> response is <{1}>",reqnum,responseStr);
                            return responseStr;
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
                                             data != null ? OSDParser.SerializeJsonString(data) : "");
                        else
                            MainConsole.Instance.WarnFormat("[WebUtils]: {0} to {1}, data {2}, response {3}", webResponse.StatusCode, url,
                                             data != null ? OSDParser.SerializeJsonString(data) : "", webResponse.StatusDescription);
                        return "";
                    }
                    if (request != null)
                        request.Abort();
                }
                catch (Exception ex)
                {
                    if (ex is System.UriFormatException)
                        errorMessage = ex.ToString() + " " + new System.Diagnostics.StackTrace().ToString();
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
                            if (MainConsole.Instance.IsEnabled(Level.All))
                            {
                                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();

                                MainConsole.Instance.TraceFormat(
                                    "[WebUtils]: osd request (URI:{0}, METHOD:{1}, UPSTACK(4):{5}) took {2}ms overall, {3}ms writing, {4}ms deserializing",
                                    url, method, tickdiff, tickdata, tickserialize, stackTrace.GetFrame(4).GetMethod().Name + " @ " + stackTrace.GetFrame(4).GetFileName() + ":" + stackTrace.GetFrame(4).GetFileLineNumber());
                            }
                            else
                                MainConsole.Instance.TraceFormat(
                                    "[WebUtils]: osd request (URI:{0}, METHOD:{1}) took {2}ms overall, {3}ms writing, {4}ms deserializing",
                                    url, method, tickdiff, tickdata, tickserialize);
                            if (tickdiff > 5000)
                                MainConsole.Instance.InfoFormat(
                                    "[WebUtils]: osd request took too long (URI:{0}, METHOD:{1}) took {2}ms overall, {3}ms writing, {4}ms deserializing",
                                    url, method, tickdiff, tickdata, tickserialize);
                        }
                    }
                }

                if (MainConsole.Instance != null)
                    MainConsole.Instance.WarnFormat("[WebUtils] osd request failed: {0} to {1}, data {2}", errorMessage, url,
                                     data != null ? data.AsString() : "");
                return "";
            }
        }

        /// <summary>
        ///   Takes the value of an Accept header and returns the preferred types
        ///   ordered by q value (if it exists).
        ///   Example input: image/jpg;q=0.7, image/png;q=0.8, image/jp2
        ///   Exmaple output: ["jp2", "png", "jpg"]
        ///   NOTE: This doesn't handle the semantics of *'s...
        /// </summary>
        /// <param name = "accept"></param>
        /// <returns></returns>
        public static string[] GetPreferredImageTypes(string accept)
        {
            if (string.IsNullOrEmpty(accept))
                return new string[0];

            string[] types = accept.Split(new[] {','});
            if (types.Length > 0)
            {
                List<string> list = new List<string>(types);
#if (!ISWIN)
                list.RemoveAll(delegate(string s) { return !s.ToLower().StartsWith("image"); });
#else
                list.RemoveAll(s => !s.ToLower().StartsWith("image"));
#endif
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

        /// <summary>
        ///   Extract the param from an uri.
        /// </summary>
        /// <param name = "uri">Something like this: /agent/uuid/ or /agent/uuid/handle/release/other</param>
        /// <param name = "uuid">uuid on uuid field</param>
        /// <param name="regionID"></param>
        /// <param name = "action">optional action</param>
        /// <param name = "other">Any other data</param>
        public static bool GetParams(string uri, out UUID uuid, out UUID regionID, out string action, out string other)
        {
            uuid = UUID.Zero;
            regionID = UUID.Zero;
            action = "";
            other = "";

            uri = uri.Trim(new[] {'/'});
            string[] parts = uri.Split('/');
            if (parts.Length <= 1)
            {
                return false;
            }
            if (!UUID.TryParse(parts[1], out uuid))
                return false;

            if (parts.Length >= 3)
                UUID.TryParse(parts[2], out regionID);
            if (parts.Length >= 4)
                action = parts[3];
            if (parts.Length >= 5)
                other = parts[4];

            return true;
        }

        /// <summary>
        ///   Extract the param from an uri.
        /// </summary>
        /// <param name = "uri">Something like this: /agent/uuid/ or /agent/uuid/handle/release</param>
        /// <param name = "uuid">uuid on uuid field</param>
        /// <param name="regionID"></param>
        /// <param name = "action">optional action</param>
        public static bool GetParams(string uri, out UUID uuid, out UUID regionID, out string action)
        {
            uuid = UUID.Zero;
            regionID = UUID.Zero;
            action = "";

            uri = uri.Trim(new[] {'/'});
            string[] parts = uri.Split('/');
            if (parts.Length <= 1)
            {
                return false;
            }
            if (!UUID.TryParse(parts[1], out uuid))
                return false;

            if (parts.Length >= 3)
                UUID.TryParse(parts[2], out regionID);
            if (parts.Length >= 4)
                action = parts[3];

            return true;
        }

        public static OSDMap GetOSDMap(string data)
        {
            return GetOSDMap(data, true);
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
                if(doLogMessages)
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

    /// <summary>
    ///   Class supporting the request side of an XML-RPC transaction.
    /// </summary>
    public sealed class ConfigurableKeepAliveXmlRpcRequest : XmlRpcRequest
    {
        private readonly XmlRpcResponseDeserializer _deserializer = new XmlRpcResponseDeserializer();
        private readonly bool _disableKeepAlive = true;
        private readonly Encoding _encoding = new ASCIIEncoding();
        private readonly XmlRpcRequestSerializer _serializer = new XmlRpcRequestSerializer();

        public string RequestResponse = String.Empty;

        /// <summary>
        ///   Instantiate an <c>XmlRpcRequest</c> for a specified method and parameters.
        /// </summary>
        /// <param name = "methodName"><c>String</c> designating the <i>object.method</i> on the server the request
        ///   should be directed to.</param>
        /// <param name = "parameters"><c>ArrayList</c> of XML-RPC type parameters to invoke the request with.</param>
        /// <param name="disableKeepAlive"></param>
        public ConfigurableKeepAliveXmlRpcRequest(String methodName, IList parameters, bool disableKeepAlive)
        {
            MethodName = methodName;
            _params = parameters;
            _disableKeepAlive = disableKeepAlive;
        }

        /// <summary>
        ///   Send the request to the server.
        /// </summary>
        /// <param name = "url"><c>String</c> The url of the XML-RPC server.</param>
        /// <returns><c>XmlRpcResponse</c> The response generated.</returns>
        public XmlRpcResponse Send(String url)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            if (request == null)
                throw new XmlRpcException(XmlRpcErrorCodes.TRANSPORT_ERROR,
                                          XmlRpcErrorCodes.TRANSPORT_ERROR_MSG + ": Could not create request with " +
                                          url);
            request.Method = "POST";
            request.ContentType = "text/xml";
            request.AllowWriteStreamBuffering = true;
            request.KeepAlive = !_disableKeepAlive;

            Stream stream = request.GetRequestStream();
            XmlTextWriter xml = new XmlTextWriter(stream, _encoding);
            _serializer.Serialize(xml, this);
            xml.Flush();
            xml.Close();

            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            StreamReader input = new StreamReader(response.GetResponseStream());

            string inputXml = input.ReadToEnd();
            XmlRpcResponse resp;
            try
            {
                resp = (XmlRpcResponse) _deserializer.Deserialize(inputXml);
            }
            catch (Exception)
            {
                RequestResponse = inputXml;
                throw;
            }
            input.Close();
            response.Close();
            return resp;
        }
    }
}