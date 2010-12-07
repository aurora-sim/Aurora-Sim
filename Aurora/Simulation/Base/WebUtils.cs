/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using System.Text;
using System.Web;
using log4net;
using OpenSim.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework.Servers.HttpServer;
using Nwc.XmlRpc;

namespace Aurora.Simulation.Base
{
    public static class WebUtils
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static  byte[] SerializeResult(XmlSerializer xs, object data)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, Util.UTF8);
            xw.Formatting = Formatting.Indented;
            xs.Serialize(xw, data);
            xw.Flush();

            ms.Seek(0, SeekOrigin.Begin);
            byte[] ret = ms.GetBuffer();
            Array.Resize(ref ret, (int)ms.Length);

            return ret;
        }

        public static Dictionary<string, object> ParseQueryString(string query)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            string[] terms = query.Split(new char[] {'&'});

            if (terms.Length == 0)
                return result;

            foreach (string t in terms)
            {
                string[] elems = t.Split(new char[] {'='});
                if (elems.Length == 0)
                    continue;

                string name = System.Web.HttpUtility.UrlDecode(elems[0]);
                string value = String.Empty;

                if (elems.Length > 1)
                    value = System.Web.HttpUtility.UrlDecode(elems[1]);

                if (name.EndsWith("[]"))
                {
                    string cleanName = name.Substring(0, name.Length - 2);
                    if (result.ContainsKey(cleanName))
                    {
                        if (!(result[cleanName] is List<string>))
                            continue;

                        List<string> l = (List<string>)result[cleanName];

                        l.Add(value);
                    }
                    else
                    {
                        List<string> newList = new List<string>();

                        newList.Add(value);

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

            string part;

            foreach (KeyValuePair<string, object> kvp in data)
            {
                if (kvp.Value is List<string>)
                {
                    List<string> l = (List<String>)kvp.Value;

                    foreach (string s in l)
                    {
                        part = System.Web.HttpUtility.UrlEncode(kvp.Key) +
                                "[]=" + System.Web.HttpUtility.UrlEncode(s);

                        if (qstring != String.Empty)
                            qstring += "&";

                        qstring += part;
                    }
                }
                else
                {
                    if (kvp.Value.ToString() != String.Empty)
                    {
                        part = System.Web.HttpUtility.UrlEncode(kvp.Key) +
                                "=" + System.Web.HttpUtility.UrlEncode(kvp.Value.ToString());
                    }
                    else
                    {
                        part = System.Web.HttpUtility.UrlEncode(kvp.Key);
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
                    Dictionary<string, object> value = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, string> pair in (Dictionary<string, string>)kvp.Value)
                    {
                        value.Add(pair.Key, pair.Value);
                    }
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

        public static Dictionary<string, object> ParseXmlResponse(string data)
        {
            //m_log.DebugFormat("[XXX]: received xml string: {0}", data);

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

            return ret;
        }

        /// <summary>
        /// Send LLSD to an HTTP client in application/llsd+json form
        /// </summary>
        /// <param name="response">HTTP response to send the data in</param>
        /// <param name="body">LLSD to send to the client</param>
        public static void SendJSONResponse(OSHttpResponse response, OSDMap body)
        {
            byte[] responseData = Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(body));

            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength = responseData.Length;
            response.ContentType = "application/llsd+json";
            response.Body.Write(responseData, 0, responseData.Length);
        }

        /// <summary>
        /// Send LLSD to an HTTP client in application/llsd+xml form
        /// </summary>
        /// <param name="response">HTTP response to send the data in</param>
        /// <param name="body">LLSD to send to the client</param>
        public static void SendXMLResponse(OSHttpResponse response, OSDMap body)
        {
            byte[] responseData = OSDParser.SerializeLLSDXmlBytes(body);

            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength = responseData.Length;
            response.ContentType = "application/llsd+xml";
            response.Body.Write(responseData, 0, responseData.Length);
        }

        /// <summary>
        /// Make a GET or GET-like request to a web service that returns LLSD
        /// or JSON data
        /// </summary>
        public static OSDMap ServiceRequest(string url, string httpVerb)
        {
            string errorMessage;

            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Method = httpVerb;

                using (WebResponse response = request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        try
                        {
                            string responseStr = responseStream.GetStreamString();
                            OSD responseOSD = OSDParser.Deserialize(responseStr);
                            if (responseOSD.Type == OSDType.Map)
                                return (OSDMap)responseOSD;
                            else
                                errorMessage = "Response format was invalid.";
                        }
                        catch
                        {
                            errorMessage = "Failed to parse the response.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.Warn(httpVerb + " on URL " + url + " failed: " + ex.Message);
                errorMessage = ex.Message;
            }

            return new OSDMap { { "Message", OSD.FromString("Service request failed. " + errorMessage) } };
        }

        /// <summary>
        /// POST URL-encoded form data to a web service that returns LLSD or
        /// JSON data
        /// </summary>
        public static OSDMap PostToService(string url, NameValueCollection data)
        {
            string errorMessage;

            try
            {
                string queryString = BuildQueryString(data);
                byte[] requestData = System.Text.Encoding.UTF8.GetBytes(queryString);

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Method = "POST";
                request.ContentLength = requestData.Length;
                request.ContentType = "application/x-www-form-urlencoded";

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(requestData, 0, requestData.Length);
                requestStream.Close();

                using (WebResponse response = request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        string responseStr = null;

                        try
                        {
                            responseStr = responseStream.GetStreamString();
                            OSD responseOSD = OSDParser.Deserialize(responseStr);
                            if (responseOSD.Type == OSDType.Map)
                                return (OSDMap)responseOSD;
                            else
                                errorMessage = "Response format was invalid.";
                        }
                        catch (Exception ex)
                        {
                            if (!String.IsNullOrEmpty(responseStr))
                                errorMessage = "Failed to parse the response:\n" + responseStr;
                            else
                                errorMessage = "Failed to retrieve the response: " + ex.Message;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.Warn("POST to URL " + url + " failed: " + ex);
                errorMessage = ex.Message;
            }

            return new OSDMap { { "Message", OSD.FromString("Service request failed. " + errorMessage) } };
        }

        public static string BuildQueryString(NameValueCollection requestArgs)
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kvp in requestArgs)
            {
                d[kvp.Key] = kvp.Value;
            }
            return BuildQueryString(d);
        }
    }

    /// <summary>Class supporting the request side of an XML-RPC transaction.</summary>
    public class ConfigurableKeepAliveXmlRpcRequest : XmlRpcRequest
    {
        private Encoding _encoding = new ASCIIEncoding();
        private XmlRpcRequestSerializer _serializer = new XmlRpcRequestSerializer();
        private XmlRpcResponseDeserializer _deserializer = new XmlRpcResponseDeserializer();
        private bool _disableKeepAlive = true;

        public string RequestResponse = String.Empty;

        /// <summary>Instantiate an <c>XmlRpcRequest</c> for a specified method and parameters.</summary>
        /// <param name="methodName"><c>String</c> designating the <i>object.method</i> on the server the request
        /// should be directed to.</param>
        /// <param name="parameters"><c>ArrayList</c> of XML-RPC type parameters to invoke the request with.</param>
        public ConfigurableKeepAliveXmlRpcRequest(String methodName, IList parameters, bool disableKeepAlive)
        {
            MethodName = methodName;
            _params = parameters;
            _disableKeepAlive = disableKeepAlive;
        }

        /// <summary>Send the request to the server.</summary>
        /// <param name="url"><c>String</c> The url of the XML-RPC server.</param>
        /// <returns><c>XmlRpcResponse</c> The response generated.</returns>
        public XmlRpcResponse Send(String url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (request == null)
                throw new XmlRpcException(XmlRpcErrorCodes.TRANSPORT_ERROR,
                              XmlRpcErrorCodes.TRANSPORT_ERROR_MSG + ": Could not create request with " + url);
            request.Method = "POST";
            request.ContentType = "text/xml";
            request.AllowWriteStreamBuffering = true;
            request.KeepAlive = !_disableKeepAlive;

            Stream stream = request.GetRequestStream();
            XmlTextWriter xml = new XmlTextWriter(stream, _encoding);
            _serializer.Serialize(xml, this);
            xml.Flush();
            xml.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader input = new StreamReader(response.GetResponseStream());

            string inputXml = input.ReadToEnd();
            XmlRpcResponse resp;
            try
            {
                resp = (XmlRpcResponse)_deserializer.Deserialize(inputXml);
            }
            catch (Exception e)
            {
                RequestResponse = inputXml;
                throw e;
            }
            input.Close();
            response.Close();
            return resp;
        }
    }
}
