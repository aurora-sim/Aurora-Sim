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
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using Griffin.Networking;
using Griffin.Networking.Http.Protocol;

namespace Aurora.Framework.Servers.HttpServer
{
    public class OSHttpRequest
    {
        private readonly Hashtable _query;
        private readonly NameValueCollection _queryString;
        private IPEndPoint _remoteIPEndPoint;

        protected IRequest _httpRequest;
        protected IPipelineHandlerContext _httpContext;

        public OSHttpRequest()
        {
        }

        public OSHttpRequest(IPipelineHandlerContext context, IRequest req)
        {
            _httpContext = context;
            _httpRequest = req;

            _queryString = new NameValueCollection();
            _query = new Hashtable();
            try
            {
                foreach (var item in req.QueryString)
                {
                    try
                    {
                        _queryString.Add(item.Name, item.Value);
                        _query[item.Name] = item.Value;
                    }
                    catch (InvalidCastException)
                    {
                        MainConsole.Instance.DebugFormat("[OSHttpRequest]: error parsing {0} query item, skipping it", item.Name);
                        continue;
                    }
                }
            }
            catch (Exception)
            {
                MainConsole.Instance.ErrorFormat("[OSHttpRequest]: Error parsing querystring");
            }

//            Form = new Hashtable();
//            foreach (HttpInputItem item in req.Form)
//            {
//                MainConsole.Instance.DebugFormat("[OSHttpRequest]: Got form item {0}={1}", item.Name, item.Value);
//                Form.Add(item.Name, item.Value);
//            }
        }

        public string[] AcceptTypes
        {
            get { return new string[0]; }
        }

        public Encoding ContentEncoding
        {
            get { return _httpRequest.ContentEncoding; }
        }

        public long ContentLength
        {
            get { return _httpRequest.ContentLength; }
        }

        public string ContentType
        {
            get { return _httpRequest.ContentType; }
        }

        public HttpCookieCollection Cookies
        {
            get
            {
                var cookies = _httpRequest.Cookies;
                HttpCookieCollection httpCookies = new HttpCookieCollection();
                foreach (var cookie in cookies)
                    httpCookies.Add(new HttpCookie(cookie.Name, cookie.Value));
                return httpCookies;
            }
        }

        public bool HasEntityBody
        {
            get { return _httpRequest.ContentLength != 0; }
        }

        public NameValueCollection Headers
        {
            get 
            {
                NameValueCollection nvc = new NameValueCollection();
                foreach (var header in _httpRequest.Headers)
                    nvc.Add(header.Name, header.Value);
                return nvc;
            }
        }

        public string HttpMethod
        {
            get { return _httpRequest.Method; }
        }

        public Stream InputStream
        {
            get { return _httpRequest.Body; }
            set { _httpRequest.Body = value; }
        }

        public bool KeepAlive
        {
            get { return _httpRequest.KeepAlive; }
        }

        public NameValueCollection QueryString
        {
            get { return _queryString; }
        }

        public Hashtable Query
        {
            get { return _query; }
        }

        /// <value>
        ///   POST request values, if applicable
        /// </value>
//        public Hashtable Form { get; private set; }
        public string RawUrl
        {
            get { return _httpRequest.Uri.AbsolutePath; }
        }

        public IPEndPoint RemoteIPEndPoint
        {
            get
            {
                if (_remoteIPEndPoint == null)
                    _remoteIPEndPoint = NetworkUtils.ResolveEndPoint(_httpRequest.Headers["Host"].Value.Split(':')[0], int.Parse(_httpRequest.Headers["Host"].Value.Split(':')[1]));

                return _remoteIPEndPoint;
            }
        }

        public Uri Url
        {
            get { return _httpRequest.Uri; }
        }

        public override string ToString()
        {
            StringBuilder me = new StringBuilder();
            me.Append(String.Format("OSHttpRequest: {0} {1}\n", HttpMethod, RawUrl));
            foreach (string k in Headers.AllKeys)
            {
                me.Append(String.Format("    {0}: {1}\n", k, Headers[k]));
            }
            if (null != RemoteIPEndPoint)
            {
                me.Append(String.Format("    IP: {0}\n", RemoteIPEndPoint));
            }

            return me.ToString();
        }

        internal OSHttpResponse MakeResponse(HttpStatusCode code, string reason)
        {
            return new OSHttpResponse(_httpContext, _httpRequest, _httpRequest.CreateResponse(code, reason));
        }
    }
}