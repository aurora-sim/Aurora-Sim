#if TEST
using System;
using HttpServer.HttpModules;
using HttpServer.Test.Controllers;
using HttpServer.Test.TestHelpers;

namespace HttpServer.Test.HttpModules
{
    /// <summary>
    /// A bit complicated test.
    /// We need to setup another web server to be able to serve the proxy requests.
    /// </summary>
    
    public class ReverseProxyTest
    {
        private IHttpRequest _request;
        private IHttpResponse _response;
        private IHttpClientContext _context;
        private MyStream _stream;
        private ReverseProxyModule _module;
        private HttpServer _server;

        public ReverseProxyTest()
        {
            _request = new HttpTestRequest {HttpVersion = "HTTP/1.1"};
        	_stream = new MyStream();
            _context = new HttpResponseContext();
        	_response = _request.CreateResponse(_context);
            _module = new ReverseProxyModule("http://localhost/", "http://localhost:4210/");
			_server = new HttpServer();
            
        }

        private void OnRequest(IHttpClientContext client, IHttpRequest request)
        {
            throw new NotImplementedException();
        }

    }
}
#endif