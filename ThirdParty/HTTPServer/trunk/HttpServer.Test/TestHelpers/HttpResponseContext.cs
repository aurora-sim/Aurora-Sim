using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HttpServer.Test.TestHelpers
{
    class HttpResponseContext : IHttpClientContext
    {
        #region Implementation of IHttpClientContext
        private readonly MemoryStream _stream = new MemoryStream();
        private bool _secured;
        private bool _disconnected;

        /// <summary>
        /// Using SSL or other encryption method.
        /// </summary>
        public bool Secured
        {
            get { return _secured; }
            set { _secured = value; }
        }

        /// <summary>
        /// Using SSL or other encryption method.
        /// </summary>
        public bool IsSecured
        {
            get { return _secured; }
        }

        public MemoryStream Stream
        {
            get { return _stream; }
        }

        public bool Disconnected
        {
            get { return _disconnected; }
        }

        /// <summary>
        /// Disconnect from client
        /// </summary>
        /// <param name="error">error to report in the <see cref="Disconnected"/> event.</param>
        public void Disconnect(SocketError error)
        {
            _disconnected = true;
        }

        /// <summary>
        /// Send a response.
        /// </summary>
        /// <param name="httpVersion">Either HttpHelper.HTTP10 or HttpHelper.HTTP11</param>
        /// <param name="statusCode">http status code</param>
        /// <param name="reason">reason for the status code.</param>
        /// <param name="body">html body contents, can be null or empty.</param>
        /// <param name="contentType">A content type to return the body as, ie 'text/html' or 'text/plain', defaults to 'text/html' if null or empty</param>
        /// <exception cref="ArgumentException">If httpVersion is invalid.</exception>
        public void Respond(string httpVersion, HttpStatusCode statusCode, string reason, string body, string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                contentType = "text/html";

            if (string.IsNullOrEmpty(httpVersion) || !httpVersion.StartsWith("HTTP/1"))
                throw new ArgumentException("Invalid HTTP version");

            byte[] buffer;
            if (string.IsNullOrEmpty(body))
                buffer = Encoding.ASCII.GetBytes(httpVersion + " " + (int)statusCode + " " + reason + "\r\n\r\n");
            else
            {
                buffer =
                    Encoding.ASCII.GetBytes(
                        string.Format("{0} {1} {2}\r\nContent-Type: {5}\r\nContent-Length: {3}\r\n\r\n{4}",
                                      httpVersion, (int)statusCode, reason ?? statusCode.ToString(), body.Length, body, contentType));
            }

            Send(buffer);
        }

        /// <summary>
        /// Send a response.
        /// </summary>
        /// <param name="httpVersion">Either HttpHelper.HTTP10 or HttpHelper.HTTP11</param>
        /// <param name="statusCode">http status code</param>
        /// <param name="reason">reason for the status code.</param>
        public void Respond(string httpVersion, HttpStatusCode statusCode, string reason)
        {
            Respond(httpVersion, statusCode, reason, null, null);
        }

        /// <summary>
        /// Send a response.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public void Respond(string body)
        {
            if (body == null)
                throw new ArgumentNullException("body");
            Respond("HTTP/1.1", HttpStatusCode.OK, HttpStatusCode.OK.ToString(), body, null);
        }

        /// <summary>
        /// send a whole buffer
        /// </summary>
        /// <param name="buffer">buffer to send</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Send(byte[] buffer)
        {
            _stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Send data using the stream
        /// </summary>
        /// <param name="buffer">Contains data to send</param>
        /// <param name="offset">Start position in buffer</param>
        /// <param name="size">number of bytes to send</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Send(byte[] buffer, int offset, int size)
        {
            _stream.Write(buffer, offset, size);
        }

        event EventHandler<DisconnectedEventArgs> IHttpClientContext.Disconnected
        {
            add { throw new System.NotImplementedException(); }
            remove { throw new System.NotImplementedException(); }
        }

        public event EventHandler<RequestEventArgs> RequestReceived;

		public void RemoveWarnings()
		{
			RequestReceived(this, new RequestEventArgs(null));
		}

        #endregion
    }
}
