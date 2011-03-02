using Fadd.Logging;
using HttpServer.Helpers;
using Xunit;
/*
namespace HttpServer.Test.Renderers
{
	class ResourceManagerTest
	{
		private ResourceManager _manager;

		public ResourceManagerTest()
		{
			_manager = new ResourceManager();
		}

		[Fact]
		private void TestGetFiles()
		{
			_manager.LoadResources("/test/", _manager.GetType().Assembly, _manager.GetType().Namespace);
			string[] files = _manager.GetFiles("/test/", "resourcetest.xml");
			Assert.Equal(1, files.Length);
			Assert.Equal("test/resourcetest.xml", files[0]);

			files = _manager.GetFiles("/test/", "resourcetest.*");
			Assert.Equal(2, files.Length);

			files = _manager.GetFiles("/test/haml/", "resourcetest2.haml");
			Assert.Equal(1, files.Length);

			files = _manager.GetFiles("/test/haml/resourcetest2.haml");
			Assert.Equal(1, files.Length);

			files = _manager.GetFiles("/test/resourcetest.*");
			Assert.Equal(2, files.Length);
		}

		[Fact]
		private void TestGetResourceStream()
		{
			_manager.LoadResources("/", _manager.GetType().Assembly, "HttpServer.MVC.Rendering");
			Assert.NotNull(_manager.GetResourceStream("resourcetest.haml"));
			Assert.NotNull(_manager.GetResourceStream("\\resourcetest.haml"));
		}

		[Fact]
		private void TestParseName()
		{
			string extension;
			string filename = "/uSEr/test/hej.*";
			_manager.ParseName(ref filename, out extension);
			Assert.Equal("*", extension);
			Assert.Equal("user/test/hej", filename);

			filename = "test/teSt.xMl";
			_manager.ParseName(ref filename, out extension);
			Assert.Equal("xml", extension);
			Assert.Equal("test/test", filename);

			filename = "test/TeSt";
			_manager.ParseName(ref filename, out extension);
			Assert.Equal(string.Empty, extension);
			Assert.Equal("test/test", filename);
		}


		[Fact]
		private void TestLoadTemplates()
		{
			LogManager.SetProvider(new NullLogProvider());

			_manager.LoadResources("/test/", _manager.GetType().Assembly, _manager.GetType().Namespace);
			Assert.NotNull(_manager._loadedResources["test/resourcetest"]);
			Assert.Equal("haml", _manager._loadedResources["test/resourcetest"][0].Extension);
			Assert.Equal(_manager.GetType().Namespace + ".resourcetest.haml", _manager._loadedResources["test/resourcetest"][0].Name);

			_manager._loadedResources.Clear();
			_manager.LoadResources("/user", _manager.GetType().Assembly, _manager.GetType().Namespace);
			Assert.Equal(_manager.GetType().Namespace + ".resourcetest.haml", _manager._loadedResources["user/resourcetest"][0].Name);

			_manager._loadedResources.Clear();
			_manager.LoadResources("/user/test/", _manager.GetType().Assembly, _manager.GetType().Namespace);
			Assert.Equal(_manager.GetType().Namespace + ".resourcetest.haml", _manager._loadedResources["user/test/resourcetest"][0].Name);

			_manager._loadedResources.Clear();
			_manager.LoadResources("/", _manager.GetType().Assembly, _manager.GetType().Namespace);
			Assert.Equal(_manager.GetType().Namespace + ".resourcetest.haml", _manager._loadedResources["resourcetest"][0].Name);
		}

		[Fact]
		private void TestContainsResource()
		{
			_manager.LoadResources("/test/", _manager.GetType().Assembly, _manager.GetType().Namespace);
			Assert.True(_manager.ContainsResource("/test/resourcetest.xml"));
			Assert.True(_manager.ContainsResource("/test/resourcetest.haml"));
			Assert.True(_manager.ContainsResource("/test/resourcetest.*"));
			Assert.True(_manager.ContainsResource("/test/haml/resourcetest2.*"));
			Assert.True(_manager.ContainsResource("/test/haml/resourcetest2.haml"));

			Assert.False(_manager.ContainsResource("/test/resourcetest"));
			Assert.False(_manager.ContainsResource("/test/rwerourcetest.xml"));
			Assert.False(_manager.ContainsResource("/test/resourcetest.qaml"));
			Assert.False(_manager.ContainsResource("/wrong/rwerourcetest.xml"));
			Assert.False(_manager.ContainsResource("/test/haml/resourcetest2.xml"));

			_manager._loadedResources.Clear();
			_manager.LoadResources("/", _manager.GetType().Assembly, _manager.GetType().Namespace);
			Assert.True(_manager.ContainsResource("/resourcetest.*"));
			Assert.True(_manager.ContainsResource("resourcetest.haml"));
		}
	}
}
*/