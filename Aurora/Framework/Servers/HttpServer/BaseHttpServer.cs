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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Xml;
using Nwc.XmlRpc;
using OpenMetaverse.StructuredData;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using Griffin.Networking;
using Griffin.Networking.Buffers;
using Griffin.Networking.Http.Handlers;
using Griffin.Networking.Http.Implementation;
using Griffin.Networking.Http.Messages;
using Griffin.Networking.Http.Protocol;
using Griffin.Networking.Http.Services;
using Griffin.Networking.Http.Services.Authentication;
using Griffin.Networking.Http.Services.BodyDecoders;
using Griffin.Networking.Http.Services.Errors;
using Griffin.Networking.Logging;
using Griffin.Networking.Messages;
using Griffin.Networking.Pipelines;
using HttpListener = Griffin.Networking.Http.HttpListener;

namespace Aurora.Framework.Servers.HttpServer
{
    public class BaseHttpServer : IHttpServer
    {
        public volatile bool HTTPDRunning = false;

        protected HttpListener _httpListener;
        protected Dictionary<string, XmlRpcMethod> m_rpcHandlers = new Dictionary<string, XmlRpcMethod>();
        protected Dictionary<string, bool> m_rpcHandlersKeepAlive = new Dictionary<string, bool>();
        protected Dictionary<string, LLSDMethod> m_llsdHandlers = new Dictionary<string, LLSDMethod>();
        protected Dictionary<string, IStreamedRequestHandler> m_streamHandlers = new Dictionary<string, IStreamedRequestHandler>();
        protected Dictionary<string, GenericHTTPMethod> m_HTTPHandlers = new Dictionary<string, GenericHTTPMethod>();
        protected Dictionary<string, IStreamedRequestHandler> m_HTTPStreamHandlers = new Dictionary<string, IStreamedRequestHandler>();
        protected X509Certificate2 m_cert;
        protected SslProtocols m_sslProtocol = SslProtocols.None;

        protected Dictionary<string, PollServiceEventArgs> m_pollHandlers = new Dictionary<string, PollServiceEventArgs>();

        protected bool m_isSecure;
        protected uint m_port;
        protected string m_hostName;

        protected IPAddress m_listenIPAddress = IPAddress.Any;

        internal PollServiceRequestManager PollServiceManager { get { return m_PollServiceManager; } }

        private PollServiceRequestManager m_PollServiceManager;

        public MessageHandler MessageHandler
        {
            get;
            private set;
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
        /// A well-formed URI for the host region server (namely "http://ExternalHostName:Port)
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

        public BaseHttpServer(uint port, string hostName, bool isSecure)
        {
            m_hostName = hostName;
            m_port = port;
            m_isSecure = isSecure;
        }

        public void SetSecureParams(string path, string password, SslProtocols protocol)
        {
            m_isSecure = true;
            m_cert = new X509Certificate2(path, password);
            m_sslProtocol = protocol;
        }

        /// <summary>
        /// Add a stream handler to the http server.  If the handler already exists, then nothing happens.
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
            return AddXmlRPCHandler(method, handler, true);
        }

        public bool AddXmlRPCHandler(string method, XmlRpcMethod handler, bool keepAlive)
        {
            lock (m_rpcHandlers)
            {
                m_rpcHandlers[method] = handler;
                m_rpcHandlersKeepAlive[method] = keepAlive; // default
            }

            return true;
        }

        public bool AddHTTPHandler(string methodName, GenericHTTPMethod handler)
        {
            //MainConsole.Instance.DebugFormat("[BASE HTTP SERVER]: Registering {0}", methodName);

            lock (m_HTTPHandlers)
            {
                if (!m_HTTPHandlers.ContainsKey(methodName))
                {
                    m_HTTPHandlers.Add(methodName, handler);
                    return true;
                }
            }

            //must already have a handler for that path so return false
            return false;
        }

        public bool AddHTTPHandler(IStreamedRequestHandler handler)
        {
            //MainConsole.Instance.DebugFormat("[BASE HTTP SERVER]: Registering {0}", methodName);

            lock (m_HTTPStreamHandlers)
            {
                if (!m_HTTPStreamHandlers.ContainsKey(handler.Path))
                {
                    m_HTTPStreamHandlers.Add(handler.Path, handler);
                    return true;
                }
            }

            //must already have a handler for that path so return false
            return false;
        }

        public bool AddPollServiceHTTPHandler(string methodName, GenericHTTPMethod handler, PollServiceEventArgs args)
        {
            bool pollHandlerResult = false;
            lock (m_pollHandlers)
            {
                if (!m_pollHandlers.ContainsKey(methodName))
                {
                    m_pollHandlers.Add(methodName, args);
                    pollHandlerResult = true;
                }
            }

            if (pollHandlerResult)
                return AddHTTPHandler(methodName, handler);

            return false;
        }

        public bool AddLLSDHandler(string path, LLSDMethod handler)
        {
            lock (m_llsdHandlers)
            {
                if (!m_llsdHandlers.ContainsKey(path))
                {
                    m_llsdHandlers.Add(path, handler);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Finding Handlers

        /// <summary>
        /// Checks if we have an Exact path in the LLSD handlers for the path provided
        /// </summary>
        /// <param name="path">URI of the request</param>
        /// <returns>true if we have one, false if not</returns>
        internal bool DoWeHaveALLSDHandler(string path)
        {
            string[] pathbase = path.Split('/');
            string searchquery = "/";

            if (pathbase.Length < 1)
                return false;

            for (int i = 1; i < pathbase.Length; i++)
            {
                searchquery += pathbase[i];
                if (pathbase.Length - 1 != i)
                    searchquery += "/";
            }

            string bestMatch = null;

            foreach (string pattern in m_llsdHandlers.Keys)
                if (searchquery.StartsWith(pattern) && searchquery.Length >= pattern.Length)
                    bestMatch = pattern;

            // extra kicker to remove the default XMLRPC login case..  just in case..
            if (path != "/" && bestMatch == "/" && searchquery != "/")
                return false;

            if (path == "/")
                return false;

            return !String.IsNullOrEmpty(bestMatch);
        }

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

        internal bool TryGetStreamHTTPHandler(string handlerKey, out IStreamedRequestHandler handle)
        {
            string bestMatch = null;

            lock (m_HTTPStreamHandlers)
            {
                if (m_HTTPStreamHandlers.TryGetValue(handlerKey, out handle))
                    return true;
                foreach (string pattern in m_HTTPStreamHandlers.Keys)
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
                    handle = null;
                    return false;
                }
                handle = m_HTTPStreamHandlers[bestMatch];
                return true;
            }
        }

        internal bool TryGetHTTPHandler(string handlerKey, out GenericHTTPMethod HTTPHandler)
        {
            //            MainConsole.Instance.DebugFormat("[BASE HTTP HANDLER]: Looking for HTTP handler for {0}", handlerKey);

            string bestMatch = null;

            lock (m_HTTPHandlers)
            {
                if (m_HTTPHandlers.TryGetValue(handlerKey, out HTTPHandler))
                    return true;
                foreach (string pattern in m_HTTPHandlers.Keys)
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
                    HTTPHandler = null;
                    return false;
                }
                HTTPHandler = m_HTTPHandlers[bestMatch];
                return true;
            }
        }

        /// <summary>
        /// Checks if we have an Exact path in the HTTP handlers for the path provided
        /// </summary>
        /// <param name="path">URI of the request</param>
        /// <returns>true if we have one, false if not</returns>
        internal bool DoWeHaveAHTTPHandler(string path)
        {
            string[] pathbase = path.Split('/');
            string searchquery = "/";

            if (pathbase.Length < 1)
                return false;

            for (int i = 1; i < pathbase.Length; i++)
            {
                searchquery += pathbase[i];
                if (pathbase.Length - 1 != i)
                    searchquery += "/";
            }

            string bestMatch = null;

            //MainConsole.Instance.DebugFormat("[BASE HTTP HANDLER]: Checking if we have an HTTP handler for {0}", searchquery);

            lock (m_HTTPHandlers)
            {
                foreach (string pattern in m_HTTPHandlers.Keys)
                {
                    if (searchquery.StartsWith(pattern) && searchquery.Length >= pattern.Length)
                    {
                        bestMatch = pattern;
                    }
                }

                // extra kicker to remove the default XMLRPC login case..  just in case..
                if (path == "/")
                    return false;

                return !String.IsNullOrEmpty(bestMatch);
            }
        }

        internal bool TryGetLLSDHandler(string path, out LLSDMethod llsdHandler)
        {
            llsdHandler = null;
            // Pull out the first part of the path
            // splitting the path by '/' means we'll get the following return..
            // {0}/{1}/{2}
            // where {0} isn't something we really control 100%

            string[] pathbase = path.Split('/');
            string searchquery = "/";

            if (pathbase.Length < 1)
                return false;

            for (int i = 1; i < pathbase.Length; i++)
            {
                searchquery += pathbase[i];
                if (pathbase.Length - 1 != i)
                    searchquery += "/";
            }

            // while the matching algorithm below doesn't require it, we're expecting a query in the form
            //
            //   [] = optional
            //   /resource/UUID/action[/action]
            //
            // now try to get the closest match to the reigstered path
            // at least for OGP, registered path would probably only consist of the /resource/

            string bestMatch = null;

            foreach (string pattern in m_llsdHandlers.Keys)
            {
                if (searchquery.ToLower().StartsWith(pattern.ToLower()))
                {
                    if (String.IsNullOrEmpty(bestMatch) || searchquery.Length > bestMatch.Length)
                    {
                        // You have to specifically register for '/' and to get it, you must specificaly request it
                        //
                        if (pattern == "/" && searchquery == "/" || pattern != "/")
                            bestMatch = pattern;
                    }
                }
            }

            if (String.IsNullOrEmpty(bestMatch))
            {
                llsdHandler = null;
                return false;
            }
            llsdHandler = m_llsdHandlers[bestMatch];
            return true;
        }

        internal bool TryGetXMLHandler(string methodName, out XmlRpcMethod handler)
        {
            lock (m_rpcHandlers)
                return m_rpcHandlers.TryGetValue(methodName, out handler);
        }

        internal bool GetXMLHandlerIsKeepAlive(string methodName)
        {
            return m_rpcHandlersKeepAlive[methodName];
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

        internal bool TryGetHTTPHandlerPathBased(string path, out GenericHTTPMethod httpHandler)
        {
            httpHandler = null;
            // Pull out the first part of the path
            // splitting the path by '/' means we'll get the following return..
            // {0}/{1}/{2}
            // where {0} isn't something we really control 100%

            string[] pathbase = path.Split('/');
            string searchquery = "/";

            if (pathbase.Length < 1)
                return false;

            for (int i = 1; i < pathbase.Length; i++)
            {
                searchquery += pathbase[i];
                if (pathbase.Length - 1 != i)
                    searchquery += "/";
            }

            // while the matching algorithm below doesn't require it, we're expecting a query in the form
            //
            //   [] = optional
            //   /resource/UUID/action[/action]
            //
            // now try to get the closest match to the reigstered path
            // at least for OGP, registered path would probably only consist of the /resource/

            string bestMatch = null;

            //            MainConsole.Instance.DebugFormat(
            //                "[BASE HTTP HANDLER]: TryGetHTTPHandlerPathBased() looking for HTTP handler to match {0}", searchquery);

            lock (m_HTTPHandlers)
            {
                foreach (string pattern in m_HTTPHandlers.Keys)
                {
                    if (searchquery.ToLower().StartsWith(pattern.ToLower()))
                    {
                        if (String.IsNullOrEmpty(bestMatch) || searchquery.Length > bestMatch.Length)
                        {
                            // You have to specifically register for '/' and to get it, you must specifically request it
                            if (pattern == "/" && searchquery == "/" || pattern != "/")
                                bestMatch = pattern;
                        }
                    }
                }

                if (String.IsNullOrEmpty(bestMatch))
                {
                    httpHandler = null;
                    return false;
                }
                if (bestMatch == "/" && searchquery != "/")
                    return false;

                httpHandler = m_HTTPHandlers[bestMatch];
                return true;
            }
        }

        #endregion

        public void Start()
        {
            try
            {
                var factory = new DelegatePipelineFactory();
                factory.AddDownstreamHandler(() => new ResponseEncoder());
                factory.AddUpstreamHandler(() => new HeaderDecoder(new HttpParser()));
                var decoder = new CompositeBodyDecoder();
                decoder.Add("application/json", new JsonBodyDecoder());
                decoder.Add("application/xml", new JsonBodyDecoder());
                decoder.Add("application/x-gzip", new JsonBodyDecoder());
                decoder.Add("application/llsd+json", new JsonBodyDecoder());
                decoder.Add("application/llsd+xml", new JsonBodyDecoder());
                decoder.Add("application/xml+llsd", new JsonBodyDecoder());
                decoder.Add("application/octet-stream", new JsonBodyDecoder());
                decoder.Add("text/html", new JsonBodyDecoder());
                decoder.Add("text/xml", new JsonBodyDecoder());
                decoder.Add("text/www-form-urlencoded", new JsonBodyDecoder());
                decoder.Add("text/x-www-form-urlencoded", new JsonBodyDecoder());

                factory.AddUpstreamHandler(() => new BodyDecoder(decoder, 65535, int.MaxValue));
                factory.AddUpstreamHandler(() => (MessageHandler = new MessageHandler(this)));
                _httpListener = new HttpListener(factory);
                _httpListener.Start(new IPEndPoint(IPAddress.Any, (int)Port));

                // Long Poll Service Manager with 3 worker threads a 25 second timeout for no events
                m_PollServiceManager = new PollServiceRequestManager(this, 3, 25000);
                HTTPDRunning = true;
                MainConsole.Instance.InfoFormat("[BASE HTTP SERVER]: Listening on port {0}", Port);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[BASE HTTP SERVER]: Error - " + e.Message);
                MainConsole.Instance.Error("[BASE HTTP SERVER]: Tip: Do you have permission to listen on port " + m_port + "?");

                // We want this exception to halt the entire server since in current configurations we aren't too
                // useful without inbound HTTP.
                throw;
            }
        }

        public void Stop()
        {
            HTTPDRunning = false;
            try
            {
                _httpListener.Stop();
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

        public void RemoveHTTPHandler(string httpMethod, string path)
        {
            lock (m_HTTPHandlers)
            {
                if (httpMethod == "")
                {
                    m_HTTPHandlers.Remove(path);
                    return;
                }

                m_HTTPHandlers.Remove(GetHandlerKey(httpMethod, path));
            }
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

            RemoveHTTPHandler(httpMethod, path);
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

        public bool RemoveLLSDHandler(string path, LLSDMethod handler)
        {
            try
            {
                if (handler == m_llsdHandlers[path])
                {
                    m_llsdHandlers.Remove(path);
                    return true;
                }
            }
            catch (KeyNotFoundException)
            {
                // This is an exception to prevent crashing because of invalid code
            }

            return false;
        }

        #endregion
    }

    public class MessageHandler : IUpstreamHandler, IDisposable
    {
        private BaseHttpServer _server;
        public MessageHandler(BaseHttpServer server)
        {
            _server = server;
        }

        /// <summary>
        /// Handle an message
        /// </summary>
        /// <param name="context">Context unique for this handler instance</param>
        /// <param name="message">Message to process</param>
        /// <remarks>
        /// All messages that can't be handled MUST be send up the chain using <see cref="IPipelineHandlerContext.SendUpstream"/>.
        /// </remarks>
        public void HandleUpstream(IPipelineHandlerContext context, IPipelineMessage message)
        {
            var msg = message as ReceivedHttpRequest;
            if (msg == null)
            {
                if (message is PipelineFailure)
                    MainConsole.Instance.ErrorFormat("[BaseHttpServer]: Failed to get message, {0}", (message as PipelineFailure).Exception);
                return;
            }

            //MainConsole.Instance.Warn("Taking in request " + msg.HttpRequest.Uri.ToString());

            var request = msg.HttpRequest;
            PollServiceEventArgs psEvArgs;
            OSHttpRequest req = new OSHttpRequest(context, request);

            if (_server.TryGetPollServiceHTTPHandler(request.Uri.AbsolutePath, out psEvArgs))
            {
                PollServiceHttpRequest psreq = new PollServiceHttpRequest(psEvArgs, context, request);

                if (psEvArgs.Request != null)
                {
                    string requestBody;
                    using (StreamReader reader = new StreamReader(req.InputStream, Encoding.UTF8))
                        requestBody = reader.ReadToEnd();

                    Hashtable keysvals = new Hashtable(), headervals = new Hashtable();

                    string[] querystringkeys = req.QueryString.AllKeys;
                    string[] rHeaders = req.Headers.AllKeys;

                    keysvals.Add("body", requestBody);
                    keysvals.Add("uri", req.RawUrl);
                    keysvals.Add("content-type", req.ContentType);
                    keysvals.Add("http-method", req.HttpMethod);

                    foreach (string queryname in querystringkeys)
                        keysvals.Add(queryname, req.QueryString[queryname]);

                    foreach (string headername in rHeaders)
                        headervals[headername] = req.Headers[headername];

                    keysvals.Add("headers", headervals);
                    keysvals.Add("querystringkeys", querystringkeys);

                    psEvArgs.Request(psreq.RequestID, keysvals);
                }

                _server.PollServiceManager.Enqueue(psreq);
            }
            else
                HandleRequest(req, req.MakeResponse(HttpStatusCode.OK, "OK"));
        }

        #region Generic HTTP Handlers

        internal void SendGenericHTTPResponse(Hashtable responsedata, OSHttpResponse response, OSHttpRequest request)
        {
            //MainConsole.Instance.Info("[BASE HTTP SERVER]: Doing HTTP Grunt work with response");
            byte[] buffer;
            if (responsedata.Count == 0)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                buffer = Encoding.UTF8.GetBytes("404");
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Send();
                return;
            }

            int responsecode = (int)responsedata["int_response_code"];
            string responseString = (string)responsedata["str_response_string"];
            string contentType = (string)responsedata["content_type"];

            if (responsedata.ContainsKey("error_status_text"))
                response.StatusDescription = (string)responsedata["error_status_text"];

            if (responsedata.ContainsKey("keepalive"))
                response.KeepAlive = (bool)responsedata["keepalive"];

            response.ContentType = string.IsNullOrEmpty(contentType) ? "text/html" : contentType;

            response.StatusCode = responsecode;

            if (responsecode == (int)OSHttpStatusCode.RedirectMovedPermanently)
            {
                response.RedirectLocation = (string)responsedata["str_redirect_location"];
                response.StatusCode = responsecode;
            }

            if (contentType != null && !(contentType.Contains("image")
                                         || contentType.Contains("x-shockwave-flash")
                                         || contentType.Contains("application/x-oar")
                                         || contentType.Contains("application/vnd.ll.mesh")))
            {
                // Text
                buffer = Encoding.UTF8.GetBytes(responseString);
            }
            else
            {
                // Binary!
                buffer = Convert.FromBase64String(responseString);
            }

            bool sendBuffer = true;

            string ETag = Util.SHA1Hash(buffer.ToString());
            response.AddHeader("ETag", ETag);
            List<string> rHeaders = request.Headers.AllKeys.ToList();
            if (rHeaders.Contains("if-none-match") && request.Headers["if-none-match"].IndexOf(ETag) >= 0)
            {
                response.StatusCode = 304;
                response.StatusDescription = "Not Modified";
                sendBuffer = false;
            }


            try
            {
                if (sendBuffer)
                {
                    response.ContentEncoding = Encoding.UTF8;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[HTTPD]: Error - " + ex);
            }
            finally
            {
                try
                {
                    response.Send();
                }
                catch (SocketException e)
                {
                    // This has to be here to prevent a Linux/Mono crash
                    MainConsole.Instance.WarnFormat("[BASE HTTP SERVER]: XmlRpcRequest issue {0}.\nNOTE: this may be spurious on Linux.", e);
                }
                catch (IOException e)
                {
                    MainConsole.Instance.Debug("[BASE HTTP SERVER]: XmlRpcRequest issue: " + e);
                }
            }
        }

        private void HandleHTTPRequest(OSHttpRequest request, OSHttpResponse response)
        {
            switch (request.HttpMethod)
            {
                case "OPTIONS":
                    response.StatusCode = (int)HttpStatusCode.OK;
                    return;

                default:
                    HandleContentVerbs(request, response);
                    return;
            }
        }

        private void HandleContentVerbs(OSHttpRequest request, OSHttpResponse response)
        {
            string requestBody = "";
            if (request.InputStream != null)
            {
                using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                    requestBody = reader.ReadToEnd();
            }

            Hashtable keysvals = new Hashtable();
            Hashtable headervals = new Hashtable();
            Hashtable requestVars = new Hashtable();

            string host = String.Empty;

            keysvals.Add("body", requestBody);
            keysvals.Add("uri", request.RawUrl);
            keysvals.Add("content-type", request.ContentType);
            keysvals.Add("http-method", request.HttpMethod);

            foreach (string queryname in request.QueryString.AllKeys)
            {
                keysvals.Add(queryname, request.QueryString[queryname]);
                requestVars.Add(queryname, keysvals[queryname]);
            }

            foreach (string headername in request.Headers.AllKeys)
                headervals[headername] = request.Headers[headername];

            if (headervals.Contains("Host"))
            {
                host = (string)headervals["Host"];
            }

            keysvals.Add("headers", headervals);
            keysvals.Add("querystringkeys", request.QueryString.AllKeys);
            keysvals.Add("requestvars", requestVars);
            //            keysvals.Add("form", request.Form);

            GenericHTTPMethod requestprocessor;
            IStreamedRequestHandler streamProcessor;
            if (keysvals.Contains("method"))
            {
                string method = (string)keysvals["method"];
                if (_server.TryGetHTTPHandler(method, out requestprocessor))
                    SendGenericHTTPResponse(requestprocessor(keysvals), response, request);
                else if (_server.TryGetStreamHTTPHandler(method, out streamProcessor))
                {
                    int respLength = 0;
                    if (request.InputStream != null)
                    {
                        request.InputStream.Close();
                        MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
                        request.InputStream = stream;
                    }
                    HandleStreamHandler(request, response, request.RawUrl, ref respLength, streamProcessor);
                }
                else
                    SendHTML404(response, host);
            }
            else
            {
                if (_server.TryGetHTTPHandlerPathBased(request.RawUrl, out requestprocessor))
                    SendGenericHTTPResponse(requestprocessor(keysvals), response, request);
                else if (_server.TryGetStreamHTTPHandler(request.RawUrl, out streamProcessor))
                {
                    int respLength = 0;
                    if (request.InputStream != null)
                    {
                        request.InputStream.Close();
                        MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
                        request.InputStream = stream;
                    }
                    HandleStreamHandler(request, response, request.RawUrl, ref respLength, streamProcessor);
                }
                else
                    SendHTML404(response, host);
            }
        }

        #endregion

        #region LLSD Handlers

        private OSDMap GenerateNoLLSDHandlerResponse()
        {
            OSDMap map = new OSDMap();
            map["reason"] = OSD.FromString("LLSDRequest");
            map["message"] = OSD.FromString("No handler registered for LLSD Requests");
            map["login"] = OSD.FromString("false");
            return map;
        }

        private void HandleLLSDRequests(OSHttpRequest request, OSHttpResponse response)
        {
            string requestBody;
            using(StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                requestBody = reader.ReadToEnd();

            response.KeepAlive = true;
            response.ContentEncoding = Encoding.UTF8;

            OSD llsdRequest = null, llsdResponse = null;

            bool LegacyLLSDLoginLibOMV = (requestBody.Contains("passwd") && requestBody.Contains("mac") && requestBody.Contains("viewer_digest"));

            if (requestBody.Length == 0)
                requestBody = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><llsd><map><key>request</key><string>get</string></map></llsd>";

            try
            {
                llsdRequest = OSDParser.Deserialize(requestBody);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[BASE HTTP SERVER]: Error - " + ex);
            }

            if (llsdRequest != null)
            {
                LLSDMethod llsdhandler = null;
                if (_server.TryGetLLSDHandler(request.RawUrl, out llsdhandler) && !LegacyLLSDLoginLibOMV)
                {
                    // we found a registered llsd handler to service this request
                    llsdResponse = llsdhandler(request.RawUrl, llsdRequest, request.RemoteIPEndPoint);
                }
                else
                {
                    // we didn't find a registered llsd handler to service this request
                    // .. give em the failed message
                    llsdResponse = GenerateNoLLSDHandlerResponse();
                }
            }
            else
                llsdResponse = GenerateNoLLSDHandlerResponse();

            byte[] buffer = new byte[0];
            if (llsdResponse.ToString() == "shutdown404!")
            {
                response.ContentType = "text/plain";
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusDescription = "Not Found";
                buffer = Encoding.UTF8.GetBytes("Not found");
            }
            else
            {
                // Select an appropriate response format
                buffer = BuildLLSDResponse(request, response, llsdResponse);
            }

            try
            {
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Send();
            }
            catch (IOException e)
            {
                MainConsole.Instance.WarnFormat("[BASE HTTP SERVER]: LLSD IOException {0}.", e);
            }
            catch (SocketException e)
            {
                // This has to be here to prevent a Linux/Mono crash
                MainConsole.Instance.WarnFormat("[BASE HTTP SERVER]: LLSD issue {0}.\nNOTE: this may be spurious on Linux.", e);
            }
        }

        private byte[] BuildLLSDResponse(OSHttpRequest request, OSHttpResponse response, OSD llsdResponse)
        {
            if (request.AcceptTypes != null && request.AcceptTypes.Length > 0)
            {
                foreach (string strAccept in request.AcceptTypes)
                {
                    switch (strAccept)
                    {
                        case "application/llsd+xml":
                        case "application/xml":
                        case "text/xml":
                            response.ContentType = strAccept;
                            return OSDParser.SerializeLLSDXmlBytes(llsdResponse);
                        case "application/llsd+json":
                        case "application/json":
                            response.ContentType = strAccept;
                            return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(llsdResponse));
                    }
                }
            }

            if (!String.IsNullOrEmpty(request.ContentType))
            {
                switch (request.ContentType)
                {
                    case "application/llsd+xml":
                    case "application/xml":
                    case "text/xml":
                        response.ContentType = request.ContentType;
                        return OSDParser.SerializeLLSDXmlBytes(llsdResponse);
                    case "application/llsd+json":
                    case "application/json":
                        response.ContentType = request.ContentType;
                        return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(llsdResponse));
                }
            }

            // response.ContentType = "application/llsd+json";
            // return Util.UTF8.GetBytes(OSDParser.SerializeJsonString(llsdResponse));
            response.ContentType = "application/llsd+xml";
            return OSDParser.SerializeLLSDXmlBytes(llsdResponse);
        }

        #endregion

        #region XML Handlers

        /// <summary>
        /// Try all the registered xmlrpc handlers when an xmlrpc request is received.
        /// Sends back an XMLRPC unknown request response if no handler is registered for the requested method.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        private bool HandleXmlRpcRequests(OSHttpRequest request, OSHttpResponse response)
        {
            XmlRpcRequest xmlRprcRequest = null;
            string requestBody;

            using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                requestBody = reader.ReadToEnd();

            requestBody = requestBody.Replace("<base64></base64>", "");

            try
            {
                if (requestBody.StartsWith("<?xml"))
                    xmlRprcRequest = (XmlRpcRequest)(new XmlRpcRequestDeserializer()).Deserialize(requestBody);
            }
            catch (XmlException)
            {
            }

            if (xmlRprcRequest != null)
            {
                string methodName = xmlRprcRequest.MethodName;
                byte[] buf;
                if (methodName != null)
                {
                    xmlRprcRequest.Params.Add(request.RemoteIPEndPoint);
                    XmlRpcResponse xmlRpcResponse;
                    XmlRpcMethod method;

                    if (_server.TryGetXMLHandler(methodName, out method))
                    {
                        xmlRprcRequest.Params.Add(request.Url); // Param[2]
                        xmlRprcRequest.Params.Add(request.Headers.Get("X-Forwarded-For")); // Param[3]

                        try
                        {
                            xmlRpcResponse = method(xmlRprcRequest, request.RemoteIPEndPoint);
                        }
                        catch (Exception e)
                        {
                            MainConsole.Instance.ErrorFormat("[BASE HTTP SERVER]: Requested method [{0}] from {1} threw exception: {2} {3}",
                                    methodName, request.RemoteIPEndPoint.Address, e, e.StackTrace);

                            // if the registered XmlRpc method threw an exception, we pass a fault-code along
                            xmlRpcResponse = new XmlRpcResponse();

                            // Code probably set in accordance with http://xmlrpc-epi.sourceforge.net/specs/rfc.fault_codes.php
                            xmlRpcResponse.SetFault(-32603, string.Format("Requested method [{0}] from {1} threw exception: {2} {3}",
                                    methodName, request.RemoteIPEndPoint.Address, e, e.StackTrace));
                        }

                        // if the method wasn't found, we can't determine KeepAlive state anyway, so lets do it only here
                        response.KeepAlive = _server.GetXMLHandlerIsKeepAlive(methodName);
                    }
                    else
                    {
                        xmlRpcResponse = new XmlRpcResponse();

                        // Code set in accordance with http://xmlrpc-epi.sourceforge.net/specs/rfc.fault_codes.php
                        xmlRpcResponse.SetFault(
                            XmlRpcErrorCodes.SERVER_ERROR_METHOD,
                            String.Format("Requested method [{0}] not found", methodName));
                    }

                    buf = Encoding.UTF8.GetBytes(XmlRpcResponseSerializer.Singleton.Serialize(xmlRpcResponse));
                    response.ContentType = "text/xml";
                }
                else
                {
                    response.ContentType = "text/plain";
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.StatusDescription = "Not Found";
                    buf = Encoding.UTF8.GetBytes("Not found");
                    response.ContentEncoding = Encoding.UTF8;
                }

                try
                {
                    response.OutputStream.Write(buf, 0, buf.Length);
                    response.Send();
                }
                catch (SocketException e)
                {
                    // This has to be here to prevent a Linux/Mono crash
                    MainConsole.Instance.WarnFormat("[BASE HTTP SERVER]: XmlRpcRequest issue {0}.\nNOTE: this may be spurious on Linux.", e);
                }
                catch (IOException e)
                {
                    MainConsole.Instance.Debug("[BASE HTTP SERVER]: XmlRpcRequest issue: " + e);
                }
                return true;
            }

            return false;
        }

        #endregion

        #region Default Error Code Handlers (404/500)

        private void SendHTML404(OSHttpResponse response, string host)
        {
            // I know this statuscode is dumb, but the client/MSIE doesn't respond to 404s and 500s
            response.StatusCode = (int)HttpStatusCode.OK;
            response.AddHeader("Content-type", "text/html");

            byte[] buffer = Encoding.UTF8.GetBytes(GetHTTP404(host));

            response.ContentEncoding = Encoding.UTF8;

            try
            {
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Send();
            }
            catch (SocketException e)
            {
                // This has to be here to prevent a Linux/Mono crash
                MainConsole.Instance.WarnFormat("[BASE HTTP SERVER]: XmlRpcRequest issue {0}.\nNOTE: this may be spurious on Linux.", e);
            }
        }

        private void SendHTML500(OSHttpResponse response)
        {
            // I know this statuscode is dumb, but the client/MSIE doesn't respond to 404s and 500s
            response.StatusCode = (int)HttpStatusCode.OK;
            response.AddHeader("Content-type", "text/html");

            byte[] buffer = Encoding.UTF8.GetBytes(GetHTTP500());

            response.ContentEncoding = Encoding.UTF8;
            try
            {
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Send();
            }
            catch (SocketException e)
            {
                // This has to be here to prevent a Linux/Mono crash
                MainConsole.Instance.WarnFormat("[BASE HTTP SERVER] XmlRpcRequest issue {0}.\nNOTE: this may be spurious on Linux.", e);
            }
        }

        private string GetHTTP404(string host)
        {
            string file = Path.Combine(".", "http_404.html");
            if (!File.Exists(file))
                return getDefaultHTTP404(host);

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
        private static string getDefaultHTTP404(string host)
        {
            return "<HTML><HEAD><TITLE>404 Page not found</TITLE><BODY><BR /><H1>Ooops!</H1><P>The page you requested has been obsconded with by knomes. Find hippos quick!</P><P>If you are trying to log-in, your link parameters should have: &quot;-loginpage http://" + host + "/?method=login -loginuri http://" + host + "/&quot; in your link </P></BODY></HTML>";
        }

        private static string getDefaultHTTP500()
        {
            return "<HTML><HEAD><TITLE>500 Internal Server Error</TITLE><BODY><BR /><H1>Ooops!</H1><P>The server you requested is overun by knomes! Find hippos quick!</P></BODY></HTML>";
        }

        #endregion

        /// <summary>
        /// This methods is the start of incoming HTTP request handling.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        public virtual void HandleRequest(OSHttpRequest request, OSHttpResponse response)
        {
            if (request.HttpMethod == String.Empty) // Can't handle empty requests, not wasting a thread
            {
                SendHTML500(response);
                return;
            }

            int tickstart = Environment.TickCount;
            string RawUrl = request.RawUrl;
            string HTTPMethod = request.HttpMethod;
            long contentLength = request.ContentLength;
            string path = request.RawUrl;
            int respcontentLength = -1;
            try
            {
                //Fix the current Culture
                Culture.SetCurrentCulture();

                response.KeepAlive = request.KeepAlive;

                IStreamedRequestHandler requestHandler;
                if (_server.TryGetStreamHandler(BaseHttpServer.GetHandlerKey(request.HttpMethod, path), out requestHandler))
                {
                    HandleStreamHandler(request, response, path, ref respcontentLength, requestHandler);
                    return;
                }

                if (request.AcceptTypes != null && request.AcceptTypes.Length > 0)
                {
                    if (request.AcceptTypes.Any(strAccept => strAccept.Contains("application/llsd+xml") || strAccept.Contains("application/llsd+json")))
                    {
                        HandleLLSDRequests(request, response);
                        return;
                    }
                }

                switch (request.ContentType)
                {
                    case null:
                    case "text/html":
                    case "application/x-www-form-urlencoded":
                    case "application/x-www-form-urlencoded; charset=UTF-8":
                        HandleHTTPRequest(request, response);
                        return;

                    case "application/llsd+xml":
                    case "application/xml+llsd":
                    case "application/llsd+json":
                        HandleLLSDRequests(request, response);
                        return;

                    case "text/xml":
                    case "application/xml":
                    case "application/json":
                    default:
                        if (request.ContentType == "application/x-gzip")
                        {
                            Stream inputStream = new System.IO.Compression.GZipStream(request.InputStream, System.IO.Compression.CompressionMode.Decompress);
                            request.InputStream = inputStream;
                        }
                        if (_server.DoWeHaveALLSDHandler(request.RawUrl))
                            HandleLLSDRequests(request, response);
                        else if (_server.DoWeHaveAHTTPHandler(request.RawUrl))
                            HandleHTTPRequest(request, response);
                        else if (!HandleXmlRpcRequests(request, response))
                            SendHTML404(response, "");

                        return;
                }
            }
            catch (SocketException e)
            {
                // At least on linux, it appears that if the client makes a request without requiring the response,
                // an unconnected socket exception is thrown when we close the response output stream.  There's no
                // obvious way to tell if the client didn't require the response, so instead we'll catch and ignore
                // the exception instead.
                //
                // An alternative may be to turn off all response write exceptions on the HttpListener, but let's go
                // with the minimum first
                MainConsole.Instance.WarnFormat("[BASE HTTP SERVER]: HandleRequest threw {0}.\nNOTE: this may be spurious on Linux", e);
            }
            catch (IOException)
            {
                MainConsole.Instance.ErrorFormat("[BASE HTTP SERVER]: HandleRequest() threw ");
                SendHTML500(response);
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[BASE HTTP SERVER]: HandleRequest() threw {0}", e);
                SendHTML500(response);
            }
            finally
            {
                int tickdiff = Environment.TickCount - tickstart;
                // Every month or so this will wrap and give bad numbers, not really a problem
                // since its just for reporting, 500ms limit can be adjusted
                if (tickdiff > 500)
                    MainConsole.Instance.InfoFormat("[BASE HTTP SERVER]: slow request for {0}/{1} on port {2} took {3} ms for a request sized {4}mb response sized {5}mb", HTTPMethod, RawUrl, _server.Port, tickdiff, ((float)contentLength) / 1024 / 1024, ((float)respcontentLength) / 1024 / 1024);
                else if (MainConsole.Instance.IsEnabled(log4net.Core.Level.Trace))
                    MainConsole.Instance.TraceFormat("[BASE HTTP SERVER]: request for {0}/{1} on port {2} took {3} ms", HTTPMethod, RawUrl, _server.Port, tickdiff);

                
            }
        }

        private static void HandleStreamHandler(OSHttpRequest request, OSHttpResponse response, string path, ref int respcontentLength, IStreamedRequestHandler requestHandler)
        {
            byte[] buffer = null;
            response.ContentType = requestHandler.ContentType; // Lets do this defaulting before in case handler has varying content type.
            
            try
            {
                IStreamedRequestHandler streamedRequestHandler = requestHandler as IStreamedRequestHandler;
                buffer = streamedRequestHandler.Handle(path, request.InputStream, request, response);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.WarnFormat("[BASE HTTP SERVER]: HTTP handler threw an exception " + ex + ".");
            }

            if (request.InputStream != null)
                request.InputStream.Dispose();

            if (buffer == null)
            {
                if (response.OutputStream.CanWrite)
                {
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    buffer = Encoding.UTF8.GetBytes("Internal Server Error");
                }
                else
                    return;//The handler took care of sending it for us
            }
            else if (buffer == MainServer.BadRequest)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                buffer = Encoding.UTF8.GetBytes("Bad Request");
            }

            respcontentLength = buffer.Length;
            try
            {
                if (buffer != MainServer.NoResponse)
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Send();
            }
            catch (SocketException e)
            {
                // This has to be here to prevent a Linux/Mono crash
                MainConsole.Instance.WarnFormat("[BASE HTTP SERVER]: XmlRpcRequest issue {0}.\nNOTE: this may be spurious on Linux.", e);
            }
            catch (HttpListenerException)
            {
                MainConsole.Instance.WarnFormat("[BASE HTTP SERVER]: HTTP request abnormally terminated.");
            }
            catch (IOException e)
            {
                MainConsole.Instance.Debug("[BASE HTTP SERVER]: XmlRpcRequest issue: " + e);
            }
            finally
            {
                buffer = null;
            }
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            _server = null;
        }

        #endregion
    }

    public class JsonBodyDecoder : IBodyDecoder
    {
        public void Decode(IRequest message)
        {
        }
    }
}
