using HttpServer.MVC.Rendering;
using HttpServer.MVC.Rendering.Haml;
using Xunit;
/*
namespace HttpServer.Test.Renderers
{
	class TemplateManagerTest
	{
		private TemplateManager _manager;

		public TemplateManagerTest()
		{
			ResourceTemplateLoader loader = new ResourceTemplateLoader();
			loader.LoadTemplates("rendering/", loader.GetType().Assembly, "HttpServer.MVC.Rendering");
			_manager = new TemplateManager(loader);
		}

		[Fact]
		private void TestGetGeneratorForWildCard()
		{
			string resource = "rendering\\resourcetest.*";
			_manager.Add("haml", new HamlGenerator());
			_manager.Add("tiny", new Tiny.TinyGenerator());

			ITemplateGenerator gen = GetGeneratorForWildCard(ref resource);
			Assert.NotNull(gen);
			Assert.IsType(typeof(HamlGenerator), gen);
		}

		[Fact]
		private void TestMultipleLoaders()
		{
			const string resource = "rendering\\resourcetest.*";
			_manager.Add("haml", new HamlGenerator());
			_manager.Add("tiny", new Tiny.TinyGenerator());

			ResourceTemplateLoader loader = new ResourceTemplateLoader();
			loader.LoadTemplates("rendering/", loader.GetType().Assembly, "HttpServer.MVC.Rendering");
			_templateLoaders.Add(loader);
			string result = Render(resource, null);

			Assert.NotNull(result);
			Assert.True(result.StartsWith("This file is used to test the resource template loader"));

			((FileTemplateLoader)_templateLoaders[0]).PathPrefix = "..\\..\\";
			result = Render(resource, null);

			Assert.NotNull(result);
			Assert.True(result.StartsWith("This file is used to test the resource template loader"));
		}
	}
}
*/