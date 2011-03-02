#if TEST
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Fadd;
using Xunit;

namespace HttpServer.Test
{
    public class HttpClientContextTest : IDisposable
    {
        private readonly Socket _client;
        private readonly ManualResetEvent _disconnectEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent _event = new ManualResetEvent(false);
        private readonly HttpContextFactory _factory;
        private readonly Socket _remoteSocket;
        private IHttpClientContext _context;
        private int _counter;
        private bool _disconnected;
        private IHttpRequest _request;
        private Socket _listenSocket;

        public HttpClientContextTest()
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(new IPEndPoint(IPAddress.Any, 14862));
            _listenSocket.Listen(0);
            IAsyncResult res = _listenSocket.BeginAccept(null, null);
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _client.Connect("localhost", 14862);
            _remoteSocket = _listenSocket.EndAccept(res);

            _disconnectEvent.Reset();
            _event.Reset();
            _counter = 0;

            var requestParserFactory = new RequestParserFactory();
            _factory = new HttpContextFactory(NullLogWriter.Instance, 8192, requestParserFactory);
            _factory.RequestReceived += OnRequest;
            _context = _factory.CreateContext(_client);
            _context.Disconnected += OnDisconnect;
            //_context = new HttpClientContext(false, new IPEndPoint(IPAddress.Loopback, 21111), OnRequest, OnDisconnect, _client.GetStream(), ConsoleLogWriter.Instance);

            _request = null;
            _disconnected = false;
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="HttpClientContextTest"/> is reclaimed by garbage collection.
        /// </summary>
        ~HttpClientContextTest()
        {
            Dispose();
        }

        private void OnDisconnect(object source, DisconnectedEventArgs args)
        {
            _disconnected = true;
            _disconnectEvent.Set();
        }

        private void OnRequest(object source, RequestEventArgs args)
        {
            IHttpClientContext client = (IHttpClientContext)source;
            IHttpRequest request = args.Request;
            ++_counter;
            _request = (IHttpRequest) request.Clone();
            _event.Set();
        }

        [Fact]
        public void TestConstructor1()
        {
            Assert.Throws(typeof (CheckException), delegate
                                                       {
                                                           new HttpClientContext(true,
                                                                                    new IPEndPoint(IPAddress.Loopback,
                                                                                                   21111), null, null, 0);
                                                       });
        }

        [Fact]
        public void TestConstructor2()
        {
            Assert.Throws(typeof (CheckException), delegate
                                                       {
                                                           new HttpClientContext(true,
                                                                                    new IPEndPoint(IPAddress.Loopback,
                                                                                                   21111),
                                                                                    new MemoryStream(), null, 0);
                                                       });
        }

        [Fact]
        public void TestConstructor3()
        {
            var stream = new MemoryStream();
            stream.Close();
            Assert.Throws(typeof (CheckException), delegate
                                                       {
                                                           new HttpClientContext(true,
                                                                                    new IPEndPoint(IPAddress.Loopback,
                                                                                                   21111), stream, null,
                                                                                    0);
                                                       });
        }

        [Fact]
        public void TestConstructor4()
        {
            Assert.Throws(typeof (CheckException), delegate
                                                       {
                                                           new HttpClientContext(true,
                                                                                    new IPEndPoint(IPAddress.Loopback,
                                                                                                   21111), null, null,
                                                                                    0);
                                                       });
        }

        [Fact]
        public void TestPartial1()
        {
            WriteStream("GET / HTTP/1.0\r\nhost:");
            WriteStream("myhost");
            WriteStream("\r\n");
            WriteStream("accept:    all");
            WriteStream("\r\n");
            WriteStream("\r\n");
            _event.WaitOne(50000, true);
            Assert.NotNull(_request);
            Assert.Equal("GET", _request.Method);
            Assert.Equal(HttpHelper.HTTP10, _request.HttpVersion);
            Assert.Equal("all", _request.AcceptTypes[0]);
            Assert.Equal("myhost", _request.Uri.Host);
        }

        [Fact]
        public void TestPartials()
        {
            WriteStream("GET / ");
            WriteStream("HTTP/1.0\r\n");
            WriteStream("host:localhost\r\n");
            WriteStream("\r\n");
            _event.WaitOne(500, true);
            Assert.NotNull(_request);
            Assert.Equal("GET", _request.Method);
            Assert.Equal("HTTP/1.0", _request.HttpVersion);
            Assert.Equal("/", _request.Uri.AbsolutePath);
            Assert.Equal("localhost", _request.Uri.Host);
        }

        [Fact]
        public void TestPartialWithBody()
        {
            WriteStream("GET / ");
            WriteStream("HTTP/1.0\r\n");
            WriteStream("host:localhost\r\n");
            WriteStream("cOnTenT-LENGTH:11\r\n");
            WriteStream("Content-Type: text/plain");
            WriteStream("\r\n");
            WriteStream("\r\n");
            WriteStream("Hello");
            WriteStream(" World");
            _event.WaitOne(5000, false);
            Assert.NotNull(_request);
            Assert.Equal("GET", _request.Method);
            Assert.Equal("HTTP/1.0", _request.HttpVersion);
            Assert.Equal("/", _request.Uri.AbsolutePath);
            Assert.Equal("localhost", _request.Uri.Host);

            var reader = new StreamReader(_request.Body);
            Assert.Equal("Hello World", reader.ReadToEnd());
        }

		[Fact]
		public void TestTwoPartialsWithBodies()
		{
			_factory.UseTraceLogs = true;

			WriteStream("GET / ");
			WriteStream("HTTP/1.0\r\n");
			WriteStream("host:localhost\r\n");
			WriteStream("cOnTenT-LENGTH:11\r\n");
			WriteStream("Content-Type: text/plain");
			WriteStream("\r\n");
			WriteStream("\r\n");
			WriteStream("Hello");
			WriteStream(" World");
			_event.WaitOne(5000, false);
			Assert.NotNull(_request);
			Assert.Equal("GET", _request.Method);
			Assert.Equal("HTTP/1.0", _request.HttpVersion);
			Assert.Equal("/", _request.Uri.AbsolutePath);
			Assert.Equal("localhost", _request.Uri.Host);

			var reader = new StreamReader(_request.Body);
			Assert.Equal("Hello World", reader.ReadToEnd());

			_event.Reset();
			WriteStream("GET / ");
			WriteStream("HTTP/1.0\r\n");
			WriteStream("host:localhost\r\n");
			WriteStream("cOnTenT-LENGTH:7\r\n");
			WriteStream("Content-Type: text/plain");
			WriteStream("\r\n");
			WriteStream("\r\n");
			WriteStream("Goodbye");
			_event.WaitOne(5000, false);
			Assert.NotNull(_request);
			Assert.Equal("GET", _request.Method);
			Assert.Equal("HTTP/1.0", _request.HttpVersion);
			Assert.Equal("/", _request.Uri.AbsolutePath);
			Assert.Equal("localhost", _request.Uri.Host);

			reader = new StreamReader(_request.Body);
			_request.Body.Flush();
			_request.Body.Seek(0, SeekOrigin.Begin);
			Assert.Equal("Goodbye", reader.ReadToEnd());
		}

		[Fact]
		public void TestTwoCompleteWithBodies()
		{
			_factory.UseTraceLogs = true;

			WriteStream(@"GET / HTTP/1.0
host:localhost
cOnTenT-LENGTH:11
Content-Type: text/plain

Hello World");
			_event.WaitOne(5000, false);
			Assert.NotNull(_request);
			Assert.Equal("GET", _request.Method);
			Assert.Equal("HTTP/1.0", _request.HttpVersion);
			Assert.Equal("/", _request.Uri.AbsolutePath);
			Assert.Equal("localhost", _request.Uri.Host);

			var reader = new StreamReader(_request.Body);
			Assert.Equal("Hello World", reader.ReadToEnd());

			_event.Reset();
			WriteStream(@"GET / HTTP/1.0
host:localhost
cOnTenT-LENGTH:7
Content-Type: text/plain

Goodbye");
			_event.WaitOne(5000, false);
			Assert.NotNull(_request);
			Assert.Equal("GET", _request.Method);
			Assert.Equal("HTTP/1.0", _request.HttpVersion);
			Assert.Equal("/", _request.Uri.AbsolutePath);
			Assert.Equal("localhost", _request.Uri.Host);

			reader = new StreamReader(_request.Body);
			_request.Body.Flush();
			_request.Body.Seek(0, SeekOrigin.Begin);
			Assert.Equal("Goodbye", reader.ReadToEnd());
		}


        [Fact]
        public void TestRequest()
        {
            WriteStream("GET / HTTP/1.0\r\nhost: localhost\r\n\r\n");
            _event.WaitOne(5000, true);
            Assert.NotNull(_request);
            Assert.Equal("GET", _request.Method);
            Assert.Equal("/", _request.Uri.AbsolutePath);
            Assert.Equal(HttpHelper.HTTP10, _request.HttpVersion);
            Assert.Equal("localhost", _request.Uri.Host);
        }

        [Fact]
        public void TestTwoRequests()
        {
            WriteStream(@"GET / HTTP/1.0
host: localhost

GET / HTTP/1.1
host:shit.se
accept:all

");
            _event.WaitOne(500, true);
            _event.WaitOne(50, true);
            Assert.Equal(2, _counter);
            Assert.Equal("GET", _request.Method);
            Assert.Equal(HttpHelper.HTTP11, _request.HttpVersion);
            Assert.Equal("all", _request.AcceptTypes[0]);
            Assert.Equal("shit.se", _request.Uri.Host);
        }

        [Fact]
        public void TestValidInvalidValid()
        {
            WriteStream(@"GET / HTTP/1.0
host: localhost
connection: keep-alive

someshot jsj

GET / HTTP/1.1
host:shit.se
accept:all
connection:close

");
            _disconnectEvent.WaitOne(500000, true);
            Assert.True(_disconnected);
        }

        private void WriteStream(string s)
        {
            _event.Reset();
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            _remoteSocket.Send(bytes);
            Thread.Sleep(50);
        }

        #region IDisposable Members

        /// <summary>
        ///                     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            try
            {
                _client.Close();
                _listenSocket.Close();
                _remoteSocket.Close();
            }
            catch (SocketException)
            {
            }
        }

        #endregion
    }
}
#endif