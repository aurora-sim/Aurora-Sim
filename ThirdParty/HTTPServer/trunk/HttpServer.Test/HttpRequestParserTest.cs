using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using HttpServer.Exceptions;
using HttpServer.Parser;
using Xunit;

namespace HttpServer.Test
{
	/// <summary>
	/// Tests for <see cref="HttpRequestParser"/>.
	/// </summary>
    public class HttpRequestParserTest
    {
        private readonly HttpRequestParser _parser;
        private string _method;
        private string _version;
        private string _path;
        private readonly NameValueCollection _headers = new NameValueCollection();
        private readonly Stream _bodyStream = new MemoryStream();

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpRequestParserTest"/> class.
		/// </summary>
        public HttpRequestParserTest()
        {
            _parser = new HttpRequestParser(ConsoleLogWriter.Instance);
            _parser.RequestLineReceived += OnRequestLine;
            _parser.HeaderReceived += OnHeader;
            _parser.BodyBytesReceived += OnBodyBytes;
        }

        private void OnBodyBytes(object sender, BodyEventArgs e)
        {
            _bodyStream.Write(e.Buffer, e.Offset, e.Count);
        }

        private void OnHeader(object sender, HeaderEventArgs e)
        {
            _headers.Add(e.Name, e.Value);
        }

        private void OnRequestLine(object sender, RequestLineEventArgs e)
        {
            _method = e.HttpMethod;
            _version = e.HttpVersion;
            _path = e.UriPath;
        }

        [Fact]
        private void TestRequestLine()
        {
            Parse("GET / HTTP/1.0\r\n\r\n");
            Assert.Equal("GET", _method);
            Assert.Equal("HTTP/1.0", _version);
            Assert.Equal("/", _path);
            Assert.Null(_headers["host"]);
        }

        [Fact]
        private void TestSimpleHeader()
        {
            Parse(@"GET / HTTP/1.0
host: www.gauffin.com
accept: text/html

");
            
            Assert.Equal("GET", _method);
            Assert.Equal("HTTP/1.0", _version);
            Assert.Equal("/", _path);
            Assert.Equal("www.gauffin.com", _headers["host"]);
            Assert.Equal("text/html", _headers["accept"]);
        }


        [Fact]
        private void TestInvalidFirstLine()
        {
            Assert.Throws(typeof (BadRequestException), delegate { Parse("GET HTTP/1.0 /\r\n\r\n"); });
        }

        [Fact]
        private void TestJunkRequestLine()
        {
            StringBuilder sb = new StringBuilder();
            Random r = new Random((int)DateTime.Now.Ticks);
            for (int i = 0; i < 10000; ++i)
                sb.Append(r.Next(1, 254));

            Assert.Throws(typeof(BadRequestException), delegate { Parse(sb.ToString()); });
        }

        [Fact]
        private void TestJunkRequestLine2()
        {
            Assert.Throws(typeof (BadRequestException), delegate { Parse("\r\n\r\n"); });
        }

        [Fact]
        private void TestTooLargeHeader()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("GET / HTTP/1.0\r\n");
            Random r = new Random((int)DateTime.Now.Ticks);
            for (int i = 0; i < 300; ++i)
                sb.Append(r.Next('A', 'Z'));
            sb.Append(": ");
            for (int i = 0; i < 4000; ++i)
                sb.Append(r.Next('A', 'Z'));
            sb.Append("\r\n\r\n");
            Assert.Throws(typeof (BadRequestException), delegate { Parse(sb.ToString()); });
        }

        [Fact]
        private void TestTooLargeHeader2()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("GET / HTTP/1.0\r\n");
            Random r = new Random((int)DateTime.Now.Ticks);
            for (int i = 0; i < 40; ++i)
                sb.Append(r.Next('A', 'Z'));
            sb.Append(": ");
            for (int i = 0; i < 8000; ++i)
                sb.Append(r.Next('A', 'Z'));
            sb.Append("\r\n\r\n");
            Assert.Throws(typeof (BadRequestException), delegate { Parse(sb.ToString()); });
        }

        [Fact]
        private void TestSameHeader()
        {
            Parse("GET / HTTP/1.0\r\nmyh: test\r\nmyh: hello\r\n\r\n");
            Assert.Equal("test,hello", _headers["myh"]);
        }

        [Fact]
        private void TestCorrupHeader()
        {
            Assert.Throws(typeof (BadRequestException), delegate { Parse("GET / HTTP/1.0\r\n: test\r\n\r\n"); });
        }

/*        [Fact]
        [ExpectedException(typeof(BadRequestException))]
        private void TestEmptyHeader()
        {
            Parse("GET / HTTP/1.0\r\nname:\r\n\r\n");
        }
        */
		[Fact]
        private void TestMultipleLines()
        {
            Parse("GET / HTTP/1.0\r\nHost:\r\n  hello\r\n\r\n");
            Assert.Equal("hello", _headers["host"]);
        }

		[Fact]
		private void TestWhiteSpaces()
        {
            Parse("GET / HTTP/1.0\r\nHost        :           \r\n    hello\r\n\r\n");
            Assert.Equal("hello", _headers["host"]);
        }

		[Fact]
		private void TestSpannedHeader()
        {
            Parse("GET / HTTP/1.0\r\nmyheader: my long \r\n name of header\r\n\r\n");
            Assert.Equal("my long name of header", _headers["myheader"]);
        }

		[Fact]
		private void TestBlockParse()
        {
            Parse("GET / HTTP/1.0\r\n");
            Parse("host: myname\r\n");
            Parse("myvalue:");
            Parse("nextheader\r\n");
            Parse("\r\n");
            Assert.Equal("myname", _headers["host"]);
            Assert.Equal("nextheader", _headers["myvalue"]);
        }

		[Fact]
		private void TestVariousLineBreaks()
		{
			Parse("GET / HTTP/1.0\n");
			Parse("host: myname\n\r");
			Parse("myvalue:");
			Parse("nextheader\r\n");
			Parse("\r\n");
			Assert.Equal("myname", _headers["host"]);
			Assert.Equal("nextheader", _headers["myvalue"]);
		}

        [Fact]
        private void TestMultipleRequests()
        {
            byte[] bytes =
                Encoding.UTF8.GetBytes("GET / HTTP/1.0\r\nhost:bahs\r\n\r\nGET / HTTP/1.0\r\nusername:password\r\n\r\n");
            int bytesHandled = _parser.Parse(bytes, 0, bytes.Length);
            Assert.Equal("bahs", _headers["host"]);
            Assert.Equal(29, bytesHandled);
            byte[] buffer2 = new byte[40];
            Array.Copy(bytes, bytesHandled, buffer2, 0, bytes.Length - bytesHandled);
            Assert.Equal(37, _parser.Parse(buffer2, 0, bytes.Length - bytesHandled));
            Assert.Equal("password", _headers["username"]);
        }

        [Fact]
        private void TestCorrectRequest_InvalidRequest()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(@"GET / HTTP/1.0
host:bahs

GET incorrect HTP/11

GET / HTTP/1.0
username:password

");
            int bytesLeft = _parser.Parse(bytes, 0, bytes.Length);
            Assert.Equal("bahs", _headers["host"]);
            byte[] buffer2 = new byte[100];
            Array.Copy(bytes, bytes.Length - bytesLeft, buffer2, 0, bytesLeft);
            Assert.Throws(typeof (BadRequestException), delegate { _parser.Parse(buffer2, 0, bytesLeft); });
        }

        [Fact]
        private void TestTwoRequests()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(@"GET / HTTP/1.0
host:bahs

GET / HTTP/1.1
host: mah

GET / HTTP/1.0
username:password

");
            int bytesLeft = _parser.Parse(bytes, 0, bytes.Length);
            Assert.Equal("bahs", _headers["host"]);
            byte[] buffer2 = new byte[100];
            Array.Copy(bytes, bytes.Length - bytesLeft, buffer2, 0, bytesLeft);
            Assert.Throws(typeof(BadRequestException), delegate { _parser.Parse(buffer2, 0, bytesLeft); });
        }

		[Fact]
		private void TestTwoRequestsWithBodies()
		{
			byte[] bytes = Encoding.UTF8.GetBytes(@"GET / HTTP/1.1
host:bahs
content-type: text/plain
content-length: 22

01234567890123456789
GET / HTTP/1.1
host: mah
Content-Type: text/plain
Content-Length: 6

1234
");
			_parser.LogWriter = ConsoleLogWriter.Instance;
			int pos = _parser.Parse(bytes, 0, bytes.Length);
			_bodyStream.Flush();
			_bodyStream.Seek(0, SeekOrigin.Begin);
			StreamReader reader = new StreamReader(_bodyStream);
			string text = reader.ReadToEnd();
			Assert.Equal("01234567890123456789\r\n", text);

			_bodyStream.SetLength(0);
			_parser.Parse(bytes, pos, bytes.Length - pos);
			_bodyStream.Flush();
			_bodyStream.Seek(0, SeekOrigin.Begin);
			reader = new StreamReader(_bodyStream);
			text = reader.ReadToEnd();
			Assert.Equal("1234\r\n", text);
		}

		[Fact]
        private void TestFormPost()
        {
            byte[] bytes =
                Encoding.UTF8.GetBytes(
                    @"GET /user/dologin HTTP/1.1
Host: localhost:8081
User-Agent: Mozilla/5.0 (Windows; U; Windows NT 5.1; sv-SE; rv: 1.8.1.13) Gecko/20080311 Firefox/2.0.0.13
Accept: text/xml,application/xml,application/xhtml+xml,text/html;q=0.9,text/plain;q=0.8,image/png,*/*;q=0.5
Accept-Language: sv,en-us;q=0.7,en;q=0.3
Accept-Encoding: gzip,deflate
Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.7
Keep-Alive: 300
Connection: keep-alive
Referer: http: //localhost:8081/user/login
Cookie: style=Trend
Content-Type: application/x-www-form-urlencoded
Content-Length: 35

username=jonas&password=krakelkraka");
            int bytesHandled = _parser.Parse(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, bytesHandled);
            Assert.Equal(35, (int)_bodyStream.Length);
        }
        public int Parse(string s)
        {
            byte[] buffer = GetBytes(s);
            return _parser.Parse(buffer, 0, buffer.Length);
        }

        public byte[] GetBytes(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }
    }
}
