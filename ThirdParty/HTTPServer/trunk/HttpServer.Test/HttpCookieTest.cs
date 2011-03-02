using System;
using Xunit;

namespace HttpServer.Test
{
    
    public class HttpCookieTest
    {

        [Fact]
        public void Test()
        {
            DateTime expires = DateTime.Now;
            ResponseCookie cookie = new ResponseCookie("jonas", "mycontent", expires);
            Assert.Equal(expires, cookie.Expires);
            Assert.Equal("jonas", cookie.Name);
            Assert.Equal("mycontent", cookie.Value);
        }

        public void TestCookies()
        {
            RequestCookies cookies = new RequestCookies("name     =   value; name1=value1;\r\nname2\r\n=\r\nvalue2;name3=value3");
            Assert.Equal("value", cookies["name"].Value);
            Assert.Equal("value1", cookies["name1"].Value);
            Assert.Equal("value2", cookies["name2"].Value);
            Assert.Equal("value3", cookies["name3"].Value);
            Assert.Null(cookies["notfound"]);
            cookies.Clear();
            Assert.Equal(0, cookies.Count);
        }

        public void TestNullCookies()
        {
            RequestCookies cookies = new RequestCookies(null);
            Assert.Equal(0, cookies.Count);
            cookies = new RequestCookies(string.Empty);
            Assert.Equal(0, cookies.Count);
        }
        public void TestEmptyCookies()
        {
            ResponseCookies cookies = new ResponseCookies();
            Assert.Equal(0, cookies.Count);

            DateTime expires = DateTime.Now.AddDays(1);
            cookies.Add(new ResponseCookie("myname", "myvalue", expires));
            Assert.Equal(1, cookies.Count);
            Assert.Equal("myvalue", cookies["myname"].Value);
            Assert.Equal(expires, cookies["myname"].Expires);
        }
    }
}
