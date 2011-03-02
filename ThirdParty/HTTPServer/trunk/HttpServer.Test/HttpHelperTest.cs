using Xunit;

namespace HttpServer.Test
{
	/// <summary>
	/// Test fixture for the <see cref="HttpHelper"/>
	/// </summary>
	public class HttpHelperTest
	{
		/// <summary>
		/// Test to make sure an empty query string can be passed without problem
		/// </summary>
		[Fact]
		public void TestNoQueryValues()
		{
			HttpInput result = HttpHelper.ParseQueryString("");
			Assert.False(result.GetEnumerator().MoveNext());
		}

		/// <summary>
		/// Test to see that simple queries gets parsed
		/// </summary>
		[Fact]
		public void TestSimpleQueryString()
		{
			HttpInput result = HttpHelper.ParseQueryString("key1=value1&key2=value2");
			Assert.Equal("value1", result["key1"].Value);
			Assert.Equal("value2", result["key2"].Value);
		}

		/// <summary>
		/// Test to see that simple queries gets parsed
		/// </summary>
		[Fact]
		public void TestSimpleValue()
		{
			HttpInput result = HttpHelper.ParseQueryString("key1");
			Assert.Equal("key1", result[string.Empty].Value);
		}

		/// <summary>
		/// Test to see that simple queries gets parsed
		/// </summary>
		[Fact]
		public void TestPairAndSimpleValue()
		{
			HttpInput result = HttpHelper.ParseQueryString("a=2&key1");
			Assert.Equal("2", result["a"].Value);
			Assert.Equal("key1", result[string.Empty].Value);
		}

		/// <summary>
		/// Test to see that simple queries gets parsed
		/// </summary>
		[Fact]
		public void TestKeyNotGiven()
		{
			HttpInput result = HttpHelper.ParseQueryString("value");
			Assert.Equal("value", result[string.Empty].Value);
		}

		[Fact]
		public void TestLarge()
		{
			HttpInput result = HttpHelper.ParseQueryString("user%5bfirstname%5d%3djonas%26user%5bextension%5d%5bid%5d%3d1%26myname%3djonas%26user%5bfirstname%5d%3darne");
			Assert.Equal("jonas", result["myname"].Value);
		}

		[Fact]
		private void TestSpace()
		{
			HttpInput result = HttpHelper.ParseQueryString("/music/0%209.mp3");
			
		}

		
	}
}
