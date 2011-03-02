using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using HttpServer.Exceptions;
using HttpServer.FormDecoders;

namespace HttpServer.Test.TestHelpers
{
    class HttpTestRequest : IHttpRequest
    {
        #region Implementation of ICloneable
        private bool _bodyIsComplete;
        private string[] _acceptTypes;
        private Stream _body = new MemoryStream();
        private ConnectionType _connection;
        private int _contentLength;
        private NameValueCollection _headers = new NameValueCollection();
        private string _httpVersion;
        private string _method;
        private HttpInput _queryString;
        private Uri _uri;
        private string[] _uriParts;
        private HttpParam _param;
        private HttpForm _form;
        private bool _isAjax;
        private RequestCookies _cookies;

        public HttpTestRequest()
        {
            _queryString = new HttpInput("QueryString");
            _form = new HttpForm();
            _param = new HttpParam(_form, _queryString);
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public object Clone()
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region Implementation of IHttpRequest

        /// <summary>
        /// Have all body content bytes been received?
        /// </summary>
        public bool BodyIsComplete
        {
            get { return _bodyIsComplete; }
            set { _bodyIsComplete = value; }
        }

        /// <summary>
        /// Kind of types accepted by the client.
        /// </summary>
        public string[] AcceptTypes
        {
            get { return _acceptTypes; }
            set { _acceptTypes = value; }
        }

        /// <summary>
        /// Submitted body contents
        /// </summary>
        public Stream Body
        {
            get { return _body; }
            set { _body = value; }
        }

        /// <summary>
        /// Kind of connection used for the session.
        /// </summary>
        public ConnectionType Connection
        {
            get { return _connection; }
            set { _connection = value; }
        }

        /// <summary>
        /// Number of bytes in the body
        /// </summary>
        public int ContentLength
        {
            get { return _contentLength; }
            set { _contentLength = value; }
        }

        /// <summary>
        /// Headers sent by the client. All names are in lower case.
        /// </summary>
        public NameValueCollection Headers
        {
            get { return _headers; }
        }

        /// <summary>
        /// Version of http. 
        /// Probably HttpHelper.HTTP10 or HttpHelper.HTTP11
        /// </summary>
        /// <seealso cref="HttpHelper"/>
        public string HttpVersion
        {
            get { return _httpVersion; }
            set { _httpVersion = value; }
        }

        /// <summary>
        /// Requested method, always upper case.
        /// </summary>
        /// <see cref="IHttpRequest.Method"/>
        public string Method
        {
            get { return _method; }
            set { _method = value; }
        }

        /// <summary>
        /// Variables sent in the query string
        /// </summary>
        public HttpInput QueryString
        {
            get { return _queryString; }
            set { _queryString = value; }
        }

        /// <summary>
        /// Requested URI (url)
        /// </summary>
        /// <seealso cref="IHttpRequest.UriPath"/>
        public Uri Uri
        {
            get { return _uri; }
            set
            {
                _uri = value;
                _uriParts = _uri.AbsolutePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        /// <summary>
        /// Uri absolute path splitted into parts.
        /// </summary>
        /// <example>
        /// // uri is: http://gauffin.com/code/tiny/
        /// Console.WriteLine(request.UriParts[0]); // result: code
        /// Console.WriteLine(request.UriParts[1]); // result: tiny
        /// </example>
        /// <remarks>
        /// If you're using controllers than the first part is controller name,
        /// the second part is method name and the third part is Id property.
        /// </remarks>
        /// <seealso cref="IHttpRequest.Uri"/>
        public string[] UriParts
        {
            get { return _uriParts; }
            set { _uriParts = value; }
        }

        /// <summary>
        /// Gets or sets path and query.
        /// </summary>
        /// <see cref="IHttpRequest.Uri"/>
        /// <remarks>
        /// Are only used during request parsing. Cannot be set after "Host" header have been
        /// added.
        /// </remarks>
        public string UriPath
        {
            get { return _uri.AbsolutePath; }
            set { }
        }

        /// <summary>
        /// Check's both QueryString and Form after the parameter.
        /// </summary>
        public HttpParam Param
        {
            get { return _param; }
            set { _param = value; }
        }

        /// <summary>
        /// Form parameters.
        /// </summary>
        public HttpForm Form
        {
            get { return _form; }
            set { _form = value; }
        }

        /// <summary>Returns true if the request was made by Ajax (Asyncronous Javascript)</summary>
        public bool IsAjax
        {
            get { return _isAjax; }
            set { _isAjax = value; }
        }

        /// <summary>Returns set cookies for the request</summary>
        public RequestCookies Cookies
        {
            get { return _cookies; }
            set { _cookies = value; }
        }

        /// <summary>
        /// Decode body into a form.
        /// </summary>
        /// <param name="providers">A list with form decoders.</param>
        /// <exception cref="InvalidDataException">If body contents is not valid for the chosen decoder.</exception>
        /// <exception cref="InvalidOperationException">If body is still being transferred.</exception>
        public void DecodeBody(FormDecoderProvider providers)
        {
            _form = providers.Decode(_headers["content-type"], _body, Encoding.UTF8);
        }

        /// <summary>
        /// Sets the cookies.
        /// </summary>
        /// <param name="cookies">The cookies.</param>
        public void SetCookies(RequestCookies cookies)
        {
            _cookies = cookies;
        }

    	/// <summary>
    	/// Create a response object.
    	/// </summary>
    	/// <param name="context">Context for the connected client.</param>
    	/// <returns>A new <see cref="IHttpResponse"/>.</returns>
    	public IHttpResponse CreateResponse(IHttpClientContext context)
    	{
    		return new HttpResponse(context, this);
    	}

    	/// <summary>
        /// Called during parsing of a <see cref="IHttpRequest"/>.
        /// </summary>
        /// <param name="name">Name of the header, should not be URI encoded</param>
		/// <param name="value">Value of the header, should not be URI encoded</param>
        /// <exception cref="BadRequestException">If a header is incorrect.</exception>
        public void AddHeader(string name, string value)
        {
            _headers.Add(name, value);
        }

        /// <summary>
        /// Add bytes to the body
        /// </summary>
        /// <param name="bytes">buffer to read bytes from</param>
        /// <param name="offset">where to start read</param>
        /// <param name="length">number of bytes to read</param>
        /// <returns>Number of bytes actually read (same as length unless we got all body bytes).</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException">If body is not writable</exception>
        public int AddToBody(byte[] bytes, int offset, int length)
        {
            _body.Write(bytes, offset, length);
            return length;
        }

        /// <summary>
        /// Clear everything in the request
        /// </summary>
        public void Clear()
        {
            _uri = new Uri(string.Empty);
            _method = string.Empty;
        }

        #endregion
    }
}
