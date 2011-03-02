#if TEST
using System;
using System.IO;
using HttpServer.HttpModules;
using HttpServer.Test.TestHelpers;
using Xunit;
using HttpServer.Sessions;
using HttpServer.Exceptions;

namespace HttpServer.Test.HttpModules
{
    
    public class FileModuleTest
    {
        private readonly IHttpRequest _request;
        private readonly IHttpResponse _response;
        private readonly HttpResponseContext _context;
        private readonly FileModule _module;

        public FileModuleTest()
        {
            _request = new HttpTestRequest {HttpVersion = "HTTP/1.1"};
        	_context = new HttpResponseContext();
        	_response = _request.CreateResponse(_context);
            _module = new FileModule("/files/", Environment.CurrentDirectory);
            _module.MimeTypes.Add("txt", "text/plain");
        }


        [Fact]
        public void TestTextFile()
        {
            //MyStream is not working.
            _request.Uri = new Uri("http://localhost/files/HttpModules/TextFile1.txt");
            _module.Process(_request, _response, new MemorySession("name"));

            _context.Stream.Seek(0, SeekOrigin.Begin);
            TextReader reader = new StreamReader(_context.Stream);
            string text = reader.ReadToEnd();

            int pos = text.IndexOf("\r\n\r\n");
            Assert.True(pos >= 0);

            text = text.Substring(pos + 4);
            Assert.Equal("Hello World!", text);
        }

        [Fact]
        public void TestForbiddenExtension()
        {
            _request.Uri = new Uri("http://localhost/files/HttpModules/Forbidden.xml");
            Assert.Throws(typeof(ForbiddenException), delegate { _module.Process(_request, _response, new MemorySession("name")); });
        }

        [Fact]
        public void TestNotFound()
        {
            _request.Uri = new Uri("http://localhost/files/notfound.txt");
            Assert.False(_module.Process(_request, _response, new MemorySession("name")));
        }

        [Fact]
        public void TestNotFound2()
        {
            _request.Uri = new Uri("http://localhost/files/notfound.txt");
            Assert.False(_module.CanHandle(_request.Uri));
        }

        [Fact]
        public void TestCanHandle()
        {
            _request.Uri = new Uri("http://localhost/files/HttpModules/Forbidden.xml");
            Assert.True(_module.CanHandle(_request.Uri));
        }

        //todo: Test security exceptions (filesystem security)
    }
}
#endif