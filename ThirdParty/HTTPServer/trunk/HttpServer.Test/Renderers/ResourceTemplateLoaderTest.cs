using System.IO;
using HttpServer.MVC.Rendering;
using Xunit;

namespace HttpServer.Test.Renderers
{
	class ResourceTemplateLoaderTest
	{
		[Fact]
		private static void TestLoadTemplate()
		{
			ResourceTemplateLoader loader = new ResourceTemplateLoader();
			loader.LoadTemplates("/test/", loader.GetType().Assembly, loader.GetType().Namespace);
			TextReader reader = loader.LoadTemplate("test\\resourcetest.haml");
			Assert.NotNull(reader);
			reader.Dispose();
		}

	}
}
