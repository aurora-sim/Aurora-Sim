using System.IO;
using System.Text;
using HttpServer.MVC.Rendering.Haml;
using Xunit;

namespace HttpServer.Test.Renderers
{
	
	public class TagNodeTest
	{
		[Fact]
		public void TestSelfClosing()
		{
			HamlGenerator parser = new HamlGenerator();

			TextReader input = new StringReader("%img{src=\"bild\",border=\"1\"}");
			parser.Parse(input);

			StringWriter output = new StringWriter(new StringBuilder());
			parser.GenerateCode(output);

			Assert.Equal("sb.Append(@\"<img src=\"\"bild\"\" border=\"\"1\"\"/>\");", output.ToString());
		}
	}
}
