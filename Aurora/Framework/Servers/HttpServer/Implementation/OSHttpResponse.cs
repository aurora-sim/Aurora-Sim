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
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Griffin.Networking;
using Griffin.Networking.Http;
using Griffin.Networking.Http.Implementation;
using Griffin.Networking.Http.Protocol;
using Griffin.Networking.Http.Messages;

namespace Aurora.Framework.Servers.HttpServer
{
    /// <summary>
    ///   OSHttpResponse is the OpenSim representation of an HTTP
    ///   response.
    /// </summary>
    public class OSHttpResponse : IDisposable
    {
        protected IResponse _httpResponse;
        protected IRequest _httpRequest;
        protected IPipelineHandlerContext _httpContext;

        public OSHttpResponse()
        {
        }

        public OSHttpResponse(IPipelineHandlerContext context, IRequest request, IResponse response)
        {
            _httpContext = context;
            _httpRequest = request;
            _httpResponse = response;

            _httpResponse.AddHeader("remote_addr", MainServer.Instance.HostName);
            _httpResponse.AddHeader("remote_port", MainServer.Instance.Port.ToString());
        }

        public System.Web.HttpCookieCollection Cookies
        {
            get
            {
                var cookies = _httpResponse.Cookies;
                HttpCookieCollection httpCookies = new HttpCookieCollection();
                foreach (var cookie in cookies)
                    httpCookies.Add(new System.Web.HttpCookie(cookie.Name, cookie.Value));
                return httpCookies;
            }
        }

        public void AddCookie(System.Web.HttpCookie cookie)
        {
            _httpResponse.Cookies[cookie.Name] = new HttpResponseCookie()
            {
                Expires = cookie.Expires, 
                Name = cookie.Name, Path = cookie.Path, Value = cookie.Value
            };
        }

        /// <summary>
        ///   Content type property.
        /// </summary>
        /// <remarks>
        ///   Setting this property will also set IsContentTypeSet to
        ///   true.
        /// </remarks>
        public virtual string ContentType
        {
            get { return _httpResponse.ContentType; }

            set { _httpResponse.ContentType = value; }
        }

        /// <summary>
        ///   Length of the body content; 0 if there is no body.
        /// </summary>
        public long ContentLength
        {
            get { return _httpResponse.ContentLength; }
        }

        /// <summary>
        ///   Encoding of the body content.
        /// </summary>
        public Encoding ContentEncoding
        {
            get { return _httpResponse.ContentEncoding; }

            set { _httpResponse.ContentEncoding = value; }
        }

        public bool KeepAlive
        {
            get { return _httpResponse.KeepAlive; }

            set {
                _httpResponse.KeepAlive = value;
            }
        }

        /// <summary>
        ///   Return the output stream feeding the body.
        /// </summary>
        /// <remarks>
        ///   On its way out...
        /// </remarks>
        public Stream OutputStream
        {
            get
            {
                if (_httpResponse.Body == null)
                    _httpResponse.Body = new MemoryStream();

                return _httpResponse.Body; 
            }
        }

        public string ProtocolVersion
        {
            get { return _httpResponse.ProtocolVersion; }
        }

        /// <summary>
        ///   Set a redirct location.
        /// </summary>
        public string RedirectLocation
        {
            set { _httpResponse.Redirect(value); }
        }

        /// <summary>
        ///   HTTP status code.
        /// </summary>
        public virtual int StatusCode
        {
            get { return (int) _httpResponse.StatusCode; }

            set { _httpResponse.StatusCode = value; }
        }


        /// <summary>
        ///   HTTP status description.
        /// </summary>
        public string StatusDescription
        {
            get { return _httpResponse.StatusDescription; }

            set { _httpResponse.StatusDescription = value; }
        }

        /// <summary>
        ///   Add a header field and content to the response.
        /// </summary>
        /// <param name = "key">string containing the header field
        ///   name</param>
        /// <param name = "value">string containing the header field
        ///   value</param>
        public void AddHeader(string key, string value)
        {
            _httpResponse.AddHeader(key, value);
        }

        /// <summary>
        ///   Send the response back to the remote client
        /// </summary>
        public void Send()
        {
            if (_httpResponse.Body != null)
                _httpResponse.Body.Position = 0;
            _httpContext.SendDownstream(new SendHttpResponse(_httpRequest, _httpResponse));
            if (_httpResponse.Body != null)
                _httpResponse.Body.Dispose();
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            _httpResponse.Body = null;
            _httpResponse = null;
            _httpRequest.Body = null;
            _httpRequest = null;
            _httpContext = null;
        }

        #endregion
    }
}