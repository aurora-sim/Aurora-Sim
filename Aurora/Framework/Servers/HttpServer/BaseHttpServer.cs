/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Servers.HttpServer.Interfaces;
using Nwc.XmlRpc;
using OpenMetaverse.StructuredData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace Aurora.Framework.Servers.HttpServer
{
    public class BaseHttpServer : IHttpServer, IDisposable
    {
        public volatile bool HTTPDRunning = false;

        protected HttpListenerManager m_internalServer;
        protected Dictionary<string, XmlRpcMethod> m_rpcHandlers = new Dictionary<string, XmlRpcMethod>();
        
        protected Dictionary<string, IStreamedRequestHandler> m_streamHandlers =
            new Dictionary<string, IStreamedRequestHandler>();

        protected Dictionary<string, PollServiceEventArgs> m_pollHandlers =
            new Dictionary<string, PollServiceEventArgs>();

        public Action<HttpListenerContext> OnOverrideRequest = null;
        protected bool m_isSecure;
        protected uint m_port, m_threadCount;
        protected string m_hostName;
        protected int NotSocketErrors;
        protected IPAddress m_listenIPAddress = IPAddress.Any;
        private PollServiceRequestManager m_PollServiceManager;

        internal PollServiceRequestManager PollServiceManager
        {
            get { return m_PollServiceManager; }
        }

        public uint Port
        {
            get { return m_port; }
        }

        public bool Secure
        {
            get { return m_isSecure; }
        }

        public IPAddress ListenIPAddress
        {
            get { return m_listenIPAddress; }
            set { m_listenIPAddress = value; }
        }

        public string HostName
        {
            get { return m_hostName; }
            set { m_hostName = value; }
        }

        public string FullHostName
        {
            get
            {
                string protocol = "http://";
                if (Secure)
                    protocol = "https://";
                return protocol + m_hostName;
            }
        }

        /// <summary>
        ///     A well-formed URI for the host region server (namely "http://ExternalHostName:Port)
        /// </summary>
        public string ServerURI
        {
            get
            {
                string protocol = "http://";
                if (Secure)
                    protocol = "https://";
                return protocol + m_hostName + ":" + m_port.ToString();
            }
        }

        public BaseHttpServer(uint port, string hostName, bool isSecure, uint threadCount)
        {
            m_hostName = hostName;
            m_port = port;
            m_isSecure = isSecure;
            m_threadCount = threadCount;
        }

        /// <summary>
        ///     Add a stream handler to the http server.  If the handler already exists, then nothing happens.
        /// </summary>
        /// <param name="handler"></param>
        public void AddStreamHandler(IStreamedRequestHandler handler)
        {
            string httpMethod = handler.HttpMethod;
            string path = handler.Path;
            string handlerKey = GetHandlerKey(httpMethod, path);

            lock (m_streamHandlers)
            {
                if (!m_streamHandlers.ContainsKey(handlerKey))
                {
                    // MainConsole.Instance.DebugFormat("[BASE HTTP SERVER]: Adding handler key {0}", handlerKey);
                    m_streamHandlers.Add(handlerKey, handler);
                }
            }
        }

        internal static string GetHandlerKey(string httpMethod, string path)
        {
            return httpMethod + ":" + path;
        }

        #region Add Handlers

        public bool AddXmlRPCHandler(string method, XmlRpcMethod handler)
        {
            lock (m_rpcHandlers)
                m_rpcHandlers[method] = handler;
            return true;
        }

        public bool AddPollServiceHTTPHandler(string methodName, PollServiceEventArgs args)
        {
            lock (m_pollHandlers)
            {
                if (!m_pollHandlers.ContainsKey(methodName))
                {
                    m_pollHandlers.Add(methodName, args);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Finding Handlers

        internal bool TryGetStreamHandler(string handlerKey, out IStreamedRequestHandler streamHandler)
        {
            string bestMatch = null;

            lock (m_streamHandlers)
            {
                if (m_streamHandlers.TryGetValue(handlerKey, out streamHandler))
                    return true;
                foreach (string pattern in m_streamHandlers.Keys)
                {
                    if (handlerKey.StartsWith(pattern))
                    {
                        if (String.IsNullOrEmpty(bestMatch) || pattern.Length > bestMatch.Length)
                        {
                            bestMatch = pattern;
                        }
                    }
                }

                if (String.IsNullOrEmpty(bestMatch))
                {
                    streamHandler = null;
                    return false;
                }
                streamHandler = m_streamHandlers[bestMatch];
                return true;
            }
        }

        internal bool TryGetPollServiceHTTPHandler(string handlerKey, out PollServiceEventArgs oServiceEventArgs)
        {
            string bestMatch = null;

            lock (m_pollHandlers)
            {
                if (m_pollHandlers.TryGetValue(handlerKey, out oServiceEventArgs))
                    return true;
                foreach (string pattern in m_pollHandlers.Keys)
                {
                    if (handlerKey.StartsWith(pattern))
                    {
                        if (String.IsNullOrEmpty(bestMatch) || pattern.Length > bestMatch.Length)
                        {
                            bestMatch = pattern;
                        }
                    }
                }

                if (String.IsNullOrEmpty(bestMatch))
                {
                    oServiceEventArgs = null;
                    return false;
                }
                oServiceEventArgs = m_pollHandlers[bestMatch];
                return true;
            }
        }

        public XmlRpcMethod GetXmlRPCHandler(string method)
        {
            lock (m_rpcHandlers)
            {
                if (m_rpcHandlers.ContainsKey(method))
                {
                    return m_rpcHandlers[method];
                }
                return null;
            }
        }

        #endregion

        #region 400 and 500 responses

        private string GetHTTP404()
        {
            string file = Path.Combine(".", "http_404.html");
            if (!File.Exists(file))
                return getDefaultHTTP404();

            StreamReader sr = File.OpenText(file);
            string result = sr.ReadToEnd();
            sr.Close();
            return result;
        }

        private string GetHTTP500()
        {
            string file = Path.Combine(".", "http_500.html");
            if (!File.Exists(file))
                return getDefaultHTTP500();

            StreamReader sr = File.OpenText(file);
            string result = sr.ReadToEnd();
            sr.Close();
            return result;
        }

        // Fallback HTTP responses in case the HTTP error response files don't exist
        private string getDefaultHTTP404()
        {
            return
                "<HTML><HEAD><TITLE>404 Page not found</TITLE><BODY><BR /><H1>Ooops!</H1><P>The page you requested has been obsconded with by knomes. Find hippos quick!</P><P>If you are trying to log-in, your link parameters should have: &quot;-loginpage " +
                 ServerURI + "/?method=login -loginuri " + ServerURI + "/&quot; in your link </P></BODY></HTML>";
        }

        private static string getDefaultHTTP500()
        {
            return
                "<HTML><HEAD><TITLE>500 Internal Server Error</TITLE><BODY><BR /><H1>Ooops!</H1><P>The server you requested is overun by knomes! Find hippos quick!</P></BODY></HTML>";
        }

        #endregion

        private void OnRequest(HttpListenerContext context)
        {
            try
            {
                PollServiceEventArgs psEvArgs;

                if (TryGetPollServiceHTTPHandler(context.Request.Url.AbsolutePath, out psEvArgs))
                {
                    PollServiceHttpRequest psreq = new PollServiceHttpRequest(psEvArgs, context);

                    if (psEvArgs.Request != null)
                    {
                        OSHttpRequest req = new OSHttpRequest(context);
                        psEvArgs.Request(psreq.RequestID, req);
                    }

                    m_PollServiceManager.Enqueue(psreq);
                }
                else
                {
                    HandleRequest(context);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[BASE HTTP SERVER]: OnRequest() failed: {0} ", e.ToString());
            }
        }

        /// <summary>
        ///     This methods is the start of incoming HTTP request handling.
        /// </summary>
        /// <param name="context"></param>
        public virtual void HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            OSHttpRequest req = new OSHttpRequest(context);
            OSHttpResponse resp = new OSHttpResponse(context);
            if (request.HttpMethod == String.Empty) // Can't handle empty requests, not wasting a thread
            {
                byte[] buffer = GetHTML500(response);
                response.ContentLength64 = buffer.LongLength;
                response.Close(buffer, true);
                return;
            }

            response.KeepAlive = false;
            string requestMethod = request.HttpMethod;
            string uriString = request.RawUrl;
            int requestStartTick = Environment.TickCount;

            // Will be adjusted later on.
            int requestEndTick = requestStartTick;

            IStreamedRequestHandler requestHandler = null;

            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US",
                                                                                                            true);

                string path = request.RawUrl;
                string handlerKey = GetHandlerKey(request.HttpMethod, path);
                byte[] buffer = null;

                if ((request.ContentType == "application/xml" || request.ContentType == "text/xml") && GetXmlRPCHandler(request.RawUrl) != null)
                {
                    buffer = HandleXmlRpcRequests(req, resp);
                }
                else if (TryGetStreamHandler(handlerKey, out requestHandler))
                {
                    response.ContentType = requestHandler.ContentType;
                    // Lets do this defaulting before in case handler has varying content type.

                    buffer = requestHandler.Handle(path, request.InputStream, req, resp);
                }

                request.InputStream.Close();
                try
                {
                    if (buffer != null)
                    {
                        if (request.ProtocolVersion.Minor == 0)
                        {
                            //HTTP 1.0... no chunking
                            using (Stream stream = response.OutputStream)
                            {
                                HttpServerHandlerHelpers.WriteNonChunked(stream, buffer);
                            }
                        }
                        else
                        {
                            response.SendChunked = true;
                            using (Stream stream = response.OutputStream)
                            {
                                HttpServerHandlerHelpers.WriteChunked(stream, buffer);
                            }
                        }
                        //response.ContentLength64 = buffer.LongLength;
                        response.Close();
                    }
                    else
                        response.Close (new byte[0], true);
                }
                catch(Exception ex)
                {
                    MainConsole.Instance.WarnFormat(
                        "[BASE HTTP SERVER]: HandleRequest failed to write all data to the stream: {0}", ex.ToString());
                    response.Abort();
                }

                requestEndTick = Environment.TickCount;
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[BASE HTTP SERVER]: HandleRequest() threw {0} ", e.ToString());
                response.Abort();
            }
            finally
            {
                // Every month or so this will wrap and give bad numbers, not really a problem
                // since its just for reporting
                int tickdiff = requestEndTick - requestStartTick;
                if (tickdiff > 3000 && requestHandler != null)
                {
                    MainConsole.Instance.InfoFormat(
                        "[BASE HTTP SERVER]: Slow handling of {0} {1} took {2}ms",
                        requestMethod,
                        uriString,
                        tickdiff);
                }
                else if (MainConsole.Instance.IsTraceEnabled)
                {
                    MainConsole.Instance.TraceFormat(
                        "[BASE HTTP SERVER]: Handling {0} {1} took {2}ms",
                        requestMethod,
                        uriString,
                        tickdiff);
                }
            }
        }

        /// <summary>
        ///     Try all the registered xmlrpc handlers when an xmlrpc request is received.
        ///     Sends back an XMLRPC unknown request response if no handler is registered for the requested method.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        private byte[] HandleXmlRpcRequests(OSHttpRequest request, OSHttpResponse response)
        {
            string requestBody = HttpServerHandlerHelpers.ReadString(request.InputStream);

            requestBody = requestBody.Replace("<base64></base64>", "");
            string responseString = String.Empty;
            XmlRpcRequest xmlRprcRequest = null;

            try
            {
                xmlRprcRequest = (XmlRpcRequest) (new XmlRpcRequestDeserializer()).Deserialize(requestBody);
            }
            catch (XmlException e)
            {
                MainConsole.Instance.WarnFormat(
                    "[BASE HTTP SERVER]: Got XMLRPC request with invalid XML from {0}.  XML was '{1}'.  Sending blank response.  Exception: {2}",
                    request.RemoteIPEndPoint, requestBody, e.ToString());
            }

            if (xmlRprcRequest != null)
            {
                string methodName = xmlRprcRequest.MethodName;
                if (methodName != null)
                {
                    xmlRprcRequest.Params.Add(request.RemoteIPEndPoint); // Param[1]
                    XmlRpcResponse xmlRpcResponse;

                    XmlRpcMethod method;
                    bool methodWasFound;
                    lock (m_rpcHandlers)
                        methodWasFound = m_rpcHandlers.TryGetValue(methodName, out method);

                    if (methodWasFound)
                    {
                        xmlRprcRequest.Params.Add(request.Url); // Param[2]

                        string xff = "X-Forwarded-For";
                        string xfflower = xff.ToLower();
                        foreach (string s in request.Headers.AllKeys)
                        {
                            if (s != null && s.Equals(xfflower))
                            {
                                xff = xfflower;
                                break;
                            }
                        }
                        xmlRprcRequest.Params.Add(request.Headers.Get(xff)); // Param[3]

                        try
                        {
                            xmlRpcResponse = method(xmlRprcRequest, request.RemoteIPEndPoint);
                        }
                        catch (Exception e)
                        {
                            string errorMessage
                                = String.Format(
                                    "Requested method [{0}] from {1} threw exception: {2} {3}",
                                    methodName, request.RemoteIPEndPoint.Address, e.Message, e.StackTrace);

                            MainConsole.Instance.ErrorFormat("[BASE HTTP SERVER]: {0}", errorMessage);

                            // if the registered XmlRpc method threw an exception, we pass a fault-code along
                            xmlRpcResponse = new XmlRpcResponse();

                            // Code probably set in accordance with http://xmlrpc-epi.sourceforge.net/specs/rfc.fault_codes.php
                            xmlRpcResponse.SetFault(-32603, errorMessage);
                        }
                    }
                    else
                    {
                        xmlRpcResponse = new XmlRpcResponse();

                        // Code set in accordance with http://xmlrpc-epi.sourceforge.net/specs/rfc.fault_codes.php
                        xmlRpcResponse.SetFault(
                            XmlRpcErrorCodes.SERVER_ERROR_METHOD,
                            String.Format("Requested method [{0}] not found", methodName));
                    }

                    response.ContentType = "text/xml";
                    responseString = XmlRpcResponseSerializer.Singleton.Serialize(xmlRpcResponse);
                }
                else
                {
                    //HandleLLSDRequests(request, response);
                    response.ContentType = "text/plain";
                    response.StatusCode = 404;
                    response.StatusDescription = "Not Found";
                    responseString = "Not found";

                    MainConsole.Instance.ErrorFormat(
                        "[BASE HTTP SERVER]: Handler not found for http request {0} {1}",
                        request.HttpMethod, request.Url.PathAndQuery);
                }
            }

            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

            response.SendChunked = false;
            response.ContentEncoding = Encoding.UTF8;

            return buffer;
        }

        public byte[] GetHTML404(OSHttpResponse response)
        {
            // I know this statuscode is dumb, but the client doesn't respond to 404s and 500s
            response.StatusCode = 404;
            response.AddHeader("Content-type", "text/html");

            string responseString = GetHTTP404();
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);

            response.SendChunked = false;
            response.ContentEncoding = Encoding.UTF8;

            return buffer;
        }

        public byte[] GetHTML500(HttpListenerResponse response)
        {
            try
            {
                // I know this statuscode is dumb, but the client doesn't respond to 404s and 500s
                response.StatusCode = (int)HttpStatusCode.OK;
                response.AddHeader("Content-type", "text/html");

                string responseString = GetHTTP500();
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentEncoding = Encoding.UTF8;
                return buffer;
            }
            catch
            {
            }
            return null;
        }

        public void Start()
        {
            MainConsole.Instance.InfoFormat(
                "[BASE HTTP SERVER]: Starting {0} server on port {1}", Secure ? "HTTPS" : "HTTP", Port);

            try
            {
                //m_httpListener = new HttpListener();

                NotSocketErrors = 0;
                m_internalServer = new HttpListenerManager(m_threadCount, Secure);
                if (OnOverrideRequest != null)
                    m_internalServer.ProcessRequest += OnOverrideRequest;
                else
                    m_internalServer.ProcessRequest += OnRequest;

                m_internalServer.Start(m_port);

                // Long Poll Service Manager with 3 worker threads a 25 second timeout for no events
                m_PollServiceManager = new PollServiceRequestManager(this, 3, 25000);
                m_PollServiceManager.Start();
                HTTPDRunning = true;
            }
            catch (Exception e)
            {
                if (e is HttpListenerException && ((HttpListenerException) e).Message == "Access is denied")
                    MainConsole.Instance.Error("[BASE HTTP SERVER]: You must run this program as an administrator.");
                else
                {
                    MainConsole.Instance.Error("[BASE HTTP SERVER]: Error - " + e.Message);
                    MainConsole.Instance.Error("[BASE HTTP SERVER]: Tip: Do you have permission to listen on port " +
                                               m_port + "?");
                }

                // We want this exception to halt the entire server since in current configurations we aren't too
                // useful without inbound HTTP.
                throw e;
            }
        }

        public void Stop()
        {
            HTTPDRunning = false;
            try
            {
                m_PollServiceManager.Stop();
                m_internalServer.Stop();
            }
            catch (NullReferenceException)
            {
                MainConsole.Instance.Warn("[BASE HTTP SERVER]: Null Reference when stopping HttpServer.");
            }
        }

        #region Remove Handlers

        public void RemoveStreamHandler(string httpMethod, string path)
        {
            string handlerKey = GetHandlerKey(httpMethod, path);

            //MainConsole.Instance.DebugFormat("[BASE HTTP SERVER]: Removing handler key {0}", handlerKey);

            lock (m_streamHandlers) m_streamHandlers.Remove(handlerKey);
        }

        public void RemovePollServiceHTTPHandler(string httpMethod, string path)
        {
            lock (m_pollHandlers)
            {
                if (m_pollHandlers.ContainsKey(path))
                {
                    m_pollHandlers.Remove(path);
                }
            }
        }

        public void RemoveXmlRPCHandler(string method)
        {
            lock (m_rpcHandlers)
            {
                if (m_rpcHandlers.ContainsKey(method))
                {
                    m_rpcHandlers.Remove(method);
                }
            }
        }

        #endregion

        public void Dispose()
        {
            m_internalServer.Dispose();
        }
    }
}