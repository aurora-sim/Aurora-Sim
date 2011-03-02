using System;
using HttpServer.MVC;
using HttpServer.Test.TestHelpers;
using HttpServer.Sessions;
using Xunit;

namespace HttpServer.Test.HttpModules
{
    public class ControllerModuleTest
    {
        private readonly TestController _controller;
        private readonly IHttpRequest _request;
        private readonly IHttpResponse _response;
        private readonly IHttpClientContext _context;
        private readonly ControllerModule _module;

        public ControllerModuleTest()
        {
            _controller = new TestController();
            _request = new HttpTestRequest {HttpVersion = "HTTP/1.1"};
        	_context = new HttpResponseContext();
        	_response = _request.CreateResponse(_context);
            _module = new ControllerModule();
        }

        [Fact]
        public void TestNoController()
        {
            _module.Add(_controller);
            _request.Uri = new Uri("http://localhost/test/nomethod/");
            Assert.False(_module.Process(_request, _response, new MemorySession("name")));
            Assert.Equal(null, _controller.Method);

            _request.Uri = new Uri("http://localhost/tedst/nomethod/");
            Assert.False(_module.Process(_request, _response, new MemorySession("name")));
            Assert.Equal(null, _controller.Method);
        }

        [Fact]
        public void Test()
        {
            _module.Add(_controller);
            Assert.Same(_controller, _module["test"]);

            _request.Uri = new Uri("http://localhost/test/mytest/");
            _module.Process(_request, _response, new MemorySession("name"));
            Assert.Equal("MyTest", _controller.Method);

            _request.Uri = new Uri("http://localhost/test/myraw/");
            _module.Process(_request, _response, new MemorySession("name"));
            Assert.Equal("MyRaw", _controller.Method);
        }
    }
}
