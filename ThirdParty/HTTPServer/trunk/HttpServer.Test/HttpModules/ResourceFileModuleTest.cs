using HttpServer.Exceptions;
using HttpServer.HttpModules;
using Xunit;

namespace HttpServer.Test.HttpModules
{
	class ResourceFileModuleTest
	{
		[Fact]
		private static void TestProcess()
		{
			ResourceFileModule module = new ResourceFileModule();
			module.AddResources("/base/", module.GetType().Assembly, "HttpServer.MVC.Rendering");
			module.MimeTypes.Add("haml", "application/hamltype");
			/*
			string contentType;
			Assert.NotNull(module.GetResourceStream("\\base\\resourcetest.haml", out contentType));
			Assert.Equal("application/hamltype", contentType);

			Assert.Throws(typeof(ForbiddenException), delegate { module.GetResourceStream("\\base\\resourcetest.xml", out contentType); });
			Assert.Throws(typeof(InternalServerException), delegate { module.GetResourceStream("\\base\\resourcetest", out contentType); });
			Assert.Null(module.GetResourceStream("\\base\\incorrecttest.haml", out contentType));
			 * */
		}
	}
}
