using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using HttpServer.Exceptions;
using Xunit;

namespace HttpServer.Test.Exceptions
{
    
    class HttpExceptionTest
    {
        [Fact]
        public void Test()
        {
            HttpException ex = new HttpException(HttpStatusCode.Forbidden, "mymessage");
            Assert.Equal(HttpStatusCode.Forbidden, ex.HttpStatusCode);
            Assert.Equal("mymessage", ex.Message);
        }
    }
}
