#if TEST
using System;
using System.Net;
using System.Text.RegularExpressions;
using HttpServer.Rules;
using HttpServer.Test.TestHelpers;
using Xunit;

namespace HttpServer.Test
{
	
	public class RegexRedirectRuleTest
	{
		#region Null tests
		[Fact]
		public void TestNullParameter0()
		{
		    Assert.Throws(typeof (ArgumentNullException), delegate { new RegexRedirectRule(null, "nun"); });
		}

		[Fact]
		public void TestNullParameter1()
		{
            Assert.Throws(typeof(ArgumentNullException), delegate { new RegexRedirectRule("", "nun"); });
		}

		[Fact]
		public void TestNullParameter2()
		{
			Assert.Throws(typeof (ArgumentNullException), delegate { new RegexRedirectRule("nun", ""); });
		}

		[Fact]
		public void TestNullParameter3()
		{
			RegexRedirectRule regexRule = new RegexRedirectRule("nun", "nun");
			Assert.Throws(typeof (ArgumentNullException), delegate { regexRule.Process(null, null); });
		}

		[Fact]
		public void TestNullParameter4()
		{
			RegexRedirectRule regexRule = new RegexRedirectRule("nun", "nun");
		    Assert.Throws(typeof (ArgumentNullException), delegate { regexRule.Process(new HttpTestRequest(), null); });
		}
		#endregion

		[Fact]
		public void TestCorrect()
		{
			RegexRedirectRule rule = new RegexRedirectRule("/(?<first>[a-z]+)/(?<second>[a-z]+/?)", "/test/?parameter=${second}&ignore=${first}", RegexOptions.IgnoreCase);
            IHttpRequest request = new HttpTestRequest
                                   	{
                                   		HttpVersion = "1.0",
                                   		Uri = new Uri("http://www.google.com/above/all/", UriKind.Absolute)
                                   	};
			IHttpResponse response = request.CreateResponse(new HttpContext());
			rule.Process(request, response);
			Assert.Equal(HttpStatusCode.Redirect, response.Status);
		}

		[Fact]
        public void TestCorrectNoRedirect()
        {
            RegexRedirectRule rule = new RegexRedirectRule("/(?<first>[a-z]+)/(?<second>[a-z]+)/?", "/test/?ignore=${second}&parameters=${first}", RegexOptions.IgnoreCase, false);
            IHttpRequest request = new HttpTestRequest
                                   	{
                                   		HttpVersion = "1.0",
                                   		Uri = new Uri("http://www.google.com/above/all/", UriKind.Absolute)
                                   	};
			IHttpResponse response = request.CreateResponse(new HttpContext());
            rule.Process(request, response);
            Assert.Equal(request.Uri.ToString(), "http://www.google.com/test/?ignore=all&parameters=above");
        }
	}
}
#endif