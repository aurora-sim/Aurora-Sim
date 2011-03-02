using System.IO;
using HttpServer.Helpers;
using Xunit;

namespace HttpServer.Test.Helpers
{
	/// <summary>
	/// Tester for the <see cref="ResourceManager"/>
	/// </summary>
	public class ResourceManagerTest : ResourceManager
	{
		private readonly ResourceManager _resouceManager = new ResourceManager();

		/// <summary>Tests mapping of resources</summary>
		[Fact]
		public void Test()
		{
			// Make sure the three resources in the project can be loaded properly
			Assert.Equal(3, _resouceManager.LoadResources("/testUri/tests/", GetType().Assembly, "HttpServer.Test.Helpers.ResourceFiles"));
			Assert.Equal(3, _resouceManager.LoadResources("/", GetType().Assembly, "HttpServer.Test.Helpers.ResourceFiles"));

			Assert.True(_resouceManager.ContainsResource("/test.test.xml"));
			Assert.True(_resouceManager.ContainsResource("teSt.test.txt"));
			Assert.True(_resouceManager.ContainsResource("\\test-Test.xml"));

			Assert.True(_resouceManager.ContainsResource("/testUri/tests/test.test.xml"));
			Assert.True(_resouceManager.ContainsResource("testUri\\tests\\teSt.test.txt"));
			Assert.True(_resouceManager.ContainsResource("/testUri/tests/test-Test.xml"));
			Assert.True(_resouceManager.ContainsResource("/teStUri/Tests/TeSt.teSt.xml"));
			Assert.True(_resouceManager.ContainsResource("/teStUri/Tests/TeSt.teSt.*"));

			using(Stream test = _resouceManager.GetResourceStream("/testUri/tests/test.test.xml"))
				Assert.NotNull(test);

			string[] files = _resouceManager.GetFiles("\\testUri\\tests\\test.test.*");
			Assert.NotNull(files);
			Assert.Equal(2, files.Length);

			files = _resouceManager.GetFiles("\\testUri\\tests\\test.test.xml");
			Assert.NotNull(files);
			Assert.Equal(1, files.Length);

			files = _resouceManager.GetFiles("/testUri/tests/test-test.*");
			Assert.NotNull(files);
			Assert.Equal(1, files.Length);
			Assert.True(files[0] == "/testUri/tests/test-test.xml");
		}
	}
}
