using System.IO;
using System.Text;
using HttpServer.MVC.Rendering;
using HttpServer.MVC.Rendering.Haml;
using Xunit;

namespace HttpServer.Test.Renderers
{
	
	public class PartialNodeTest
	{
		[Fact]
		public void TestParamsSimple()
		{
			string output = Parse("_\"/user/new/\"{parameterName=\"parameterValue\",parameterValue2=parameter}");

			Assert.Equal("sb.Append(@\"\");sb.Append(hiddenTemplateManager.RenderPartial(\"user\\\\new.haml\", args, new TemplateArguments(\"parameterName\", \"parameterValue\", \"parameterValue2\", parameter)));", output);
		}

		[Fact]
		public void TestParamsAdvanced()
		{
			string output = Parse("_\"/test/\"{user=CurrentUser:typeof(User)}");

			Assert.Equal("sb.Append(@\"\");sb.Append(hiddenTemplateManager.RenderPartial(\"test.haml\", args, new TemplateArguments(\"user\", CurrentUser, typeof(User))));", output);
		}

		[Fact]
		public void TestInvalidParanthesis()
		{
		    Assert.Throws(typeof (CodeGeneratorException), delegate { Parse("_\"/test/"); });
		}

		[Fact]
		public void TestInvalidModifier()
		{
		    Assert.Throws(typeof (CodeGeneratorException), delegate { Parse("_\"/test/\"{test=\"test\"}{class=\"test\"}"); });
	}

		protected static string Parse(string text)
		{
			HamlGenerator parser = new HamlGenerator();
			TextReader input = new StringReader(text);
			parser.Parse(input);

			StringBuilder sb = new StringBuilder();
			TextWriter output = new StringWriter(sb);
			parser.GenerateCode(output);

			return output.ToString();
		}
	}
}
