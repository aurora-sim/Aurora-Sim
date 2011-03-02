using Xunit;

namespace HttpServer.Test
{
	/// <summary>
	/// Tests for <see cref="HttpRequest"/>.
	/// </summary>
    public class HttpRequestTest
    {
		readonly HttpRequest _request;


		/// <summary>
		/// Initializes a new instance of the <see cref="HttpRequestTest"/> class.
		/// </summary>
        public HttpRequestTest()
        {
            _request = new HttpRequest();
        }


		[Fact]
		private void TestUriPath()
		{
			_request.UriPath = "music/0%209.mp3";
			Assert.Equal("music/0 9.mp3", _request.UriPath);
			_request.UriPath = "music/0%209.mp3?abc=a%20c";
			Assert.Equal("music/0 9.mp3?abc=a%20c", _request.UriPath);
		}

		[Fact]
		private void TestEmptyObject()
		{
			Assert.Equal(string.Empty, _request.HttpVersion);
			Assert.Equal(string.Empty, _request.Method);
			Assert.Equal(HttpHelper.EmptyUri, _request.Uri);
			Assert.Equal(null, _request.AcceptTypes);
			Assert.Equal(0, (int)_request.Body.Length);
			Assert.Equal(0, _request.ContentLength);
			Assert.Equal(HttpInput.Empty, _request.QueryString);
			Assert.Equal(0, _request.Headers.Count);
		}

		[Fact]
		private void TestHeaders()
		{
			_request.AddHeader("connection", "keep-alive");
			_request.AddHeader("content-length", "10");
			_request.AddHeader("content-type", "text/html");
			_request.AddHeader("host", "www.gauffin.com");
			_request.AddHeader("accept", "gzip, text/html, bajs");

			Assert.Equal(10, _request.ContentLength);
			Assert.Equal("text/html", _request.Headers["content-type"]);
			//Assert.Equal("www.gauffin.com", Headers["host"]);
			Assert.Equal("gzip", _request.AcceptTypes[0]);
			Assert.Equal("text/html", _request.AcceptTypes[1]);
			Assert.Equal("bajs", _request.AcceptTypes[2]);
		}

    }
}
