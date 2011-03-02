#if TEST
using System;
using System.IO;
using HttpServer.Test.TestHelpers;
using Xunit;
using HttpServer.Sessions;

namespace HttpServer.Test.Controllers
{
    
    class RequestControllerTest
    {
        private readonly MyController _controller;
        private readonly IHttpRequest _request;
        private readonly IHttpResponse _response;
        private readonly HttpResponseContext _context;
        private readonly MyStream _stream;

        public RequestControllerTest()
        {
            _controller = new MyController();
            _request = new HttpTestRequest {HttpVersion = "HTTP/1.1"};
        	_stream = new MyStream();
            _context = new HttpResponseContext();
            _response = _request.CreateResponse(_context);
        }

        [Fact]
        public void TestTextMethod()
        {
            _request.Uri = new Uri("http://localhost/my/helloworld");
            Assert.True(_controller.Process(_request, _response, new MemorySession("myid")));

            _response.Body.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(_response.Body);
            Assert.Equal("HelloWorld", reader.ReadToEnd());
        }

        [Fact]
        public void TestBinaryMethod()
        {
            _request.Uri = new Uri("http://localhost/my/raw");
            _response.Connection = ConnectionType.KeepAlive;
            Assert.True(_controller.Process(_request, _response, new MemorySession("myid")));

            _stream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(_stream);
            string httpResponse = reader.ReadToEnd();
            Assert.NotNull(httpResponse);

            int pos = httpResponse.IndexOf("\r\n\r\n");
            Assert.True(pos >= 0);

            httpResponse = httpResponse.Substring(pos + 4);
            Assert.Equal("Hello World", httpResponse);
            _stream.Signal();
        }

        [Fact]
        public void TestUnknownMethod()
        {
            _request.Uri = new Uri("http://localhost/wasted/beer");
            _response.Connection = ConnectionType.KeepAlive;
            Assert.False(_controller.Process(_request, _response, new MemorySession("myid")));
        }


    }
}
#endif