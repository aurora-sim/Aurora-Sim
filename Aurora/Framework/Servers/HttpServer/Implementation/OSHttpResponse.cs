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

using System.IO;
using System.Net;
using System.Text;

namespace Aurora.Framework.Servers.HttpServer.Implementation
{
    /// <summary>
    ///     OSHttpResponse is the OpenSim representation of an HTTP
    ///     response.
    /// </summary>
    public class OSHttpResponse
    {
        /// <summary>
        ///     Content type property.
        /// </summary>
        /// <remarks>
        ///     Setting this property will also set IsContentTypeSet to
        ///     true.
        /// </remarks>
        public virtual string ContentType
        {
            get { return _httpResponse.ContentType; }

            set { _httpResponse.ContentType = value; }
        }

        /// <summary>
        ///     Encoding of the body content.
        /// </summary>
        public Encoding ContentEncoding
        {
            get { return _httpResponse.ContentEncoding; }

            set { _httpResponse.ContentEncoding = value; }
        }

        public bool KeepAlive
        {
            get { return _httpResponse.KeepAlive; }

            set { _httpResponse.KeepAlive = value; }
        }

        /// <summary>
        ///     Set a redirct location.
        /// </summary>
        public string RedirectLocation
        {
            // get { return _redirectLocation; }
            set { _httpResponse.Redirect(value); }
        }

        /// <summary>
        ///     Chunk transfers.
        /// </summary>
        public bool SendChunked
        {
            get { return _httpResponse.SendChunked; }

            set { _httpResponse.SendChunked = value; }
        }

        /// <summary>
        ///     HTTP status code.
        /// </summary>
        public virtual int StatusCode
        {
            get { return _httpResponse.StatusCode; }

            set { _httpResponse.StatusCode = value; }
        }


        /// <summary>
        ///     HTTP status description.
        /// </summary>
        public string StatusDescription
        {
            get { return _httpResponse.StatusDescription; }

            set { _httpResponse.StatusDescription = value; }
        }

        public System.Web.HttpCookieCollection Cookies
        {
            get
            {
                var cookies = _httpResponse.Cookies;
                System.Web.HttpCookieCollection httpCookies = new System.Web.HttpCookieCollection();
                foreach (Cookie cookie in cookies)
                    httpCookies.Add(new System.Web.HttpCookie(cookie.Name, cookie.Value));
                return httpCookies;
            }
        }

        public void AddCookie(System.Web.HttpCookie cookie)
        {
            _httpResponse.Cookies.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain)
                                          {
                                              Expires =
                                                  cookie
                                                  .Expires
                                          });
        }

        protected HttpListenerResponse _httpResponse;
        private HttpListenerContext _httpClientContext;

        public OSHttpResponse(HttpListenerContext context)
        {
            _httpResponse = context.Response;
            _httpClientContext = context;
        }

        /// <summary>
        ///     Add a header field and content to the response.
        /// </summary>
        /// <param name="key">
        ///     string containing the header field
        ///     name
        /// </param>
        /// <param name="value">
        ///     string containing the header field
        ///     value
        /// </param>
        public void AddHeader(string key, string value)
        {
            _httpResponse.AddHeader(key, value);
        }
    }
}