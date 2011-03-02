using System;
using System.Net;
using System.Net.Sockets;

namespace HttpServer.Test
{
	class HttpContext : IHttpClientContext
	{
		public HttpContext()
		{
#pragma warning disable 618,612
			Secured = false;
#pragma warning restore 618,612
			IsSecured = false;
		}
		/// <summary>
		/// Using SSL or other encryption method.
		/// </summary>
		[Obsolete("Use IsSecured instead.")]
		public bool Secured { get; private set; }

		/// <summary>
		/// Using SSL or other encryption method.
		/// </summary>
		public bool IsSecured { get; private set; }

		/// <summary>
		/// Disconnect from client
		/// </summary>
		/// <param name="error">error to report in the <see cref="Disconnected"/> event.</param>
		public void Disconnect(SocketError error)
		{
		}

		/// <summary>
		/// Send a response.
		/// </summary>
		/// <param name="httpVersion">Either <see cref="HttpHelper.HTTP10"/> or <see cref="HttpHelper.HTTP11"/></param>
		/// <param name="statusCode">HTTP status code</param>
		/// <param name="reason">reason for the status code.</param>
		/// <param name="body">HTML body contents, can be null or empty.</param>
		/// <param name="contentType">A content type to return the body as, i.e. 'text/html' or 'text/plain', defaults to 'text/html' if null or empty</param>
		/// <exception cref="ArgumentException">If <paramref name="httpVersion"/> is invalid.</exception>
		public void Respond(string httpVersion, HttpStatusCode statusCode, string reason, string body, string contentType)
		{
		}

		/// <summary>
		/// Send a response.
		/// </summary>
		/// <param name="httpVersion">Either <see cref="HttpHelper.HTTP10"/> or <see cref="HttpHelper.HTTP11"/></param>
		/// <param name="statusCode">HTTP status code</param>
		/// <param name="reason">reason for the status code.</param>
		public void Respond(string httpVersion, HttpStatusCode statusCode, string reason)
		{
		}

		/// <summary>
		/// Send a response.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public void Respond(string body)
		{
		}

		/// <summary>
		/// send a whole buffer
		/// </summary>
		/// <param name="buffer">buffer to send</param>
		/// <exception cref="ArgumentNullException"></exception>
		public void Send(byte[] buffer)
		{
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
		}

		public event EventHandler<DisconnectedEventArgs> Disconnected;

		public event EventHandler<RequestEventArgs> RequestReceived;

		public void RemoveWarnings()
		{
			Disconnected(this, new DisconnectedEventArgs(SocketError.Success));
			RequestReceived(this, new RequestEventArgs(null));

		}
	}
}
