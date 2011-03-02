using System.IO;
using System.Text;
using HttpServer.MVC.Rendering;
using HttpServer.MVC.Rendering.Tiny;
using Xunit;

namespace HttpServer.Test.Renderers
{
    
    public class TinyTest
    {
        private TinyGenerator _gen = new TinyGenerator();
        private StringBuilder _sb;
        TemplateCompiler _compiler = new TemplateCompiler(); 

        public TinyTest()
        {
            _sb  = new StringBuilder();
        }

        [Fact]
        public void TestEcho()
        {
            string temp = @"<html name=""<%= name %>"">";
            ParseAndGenerate(temp);
            Assert.Equal("sb.Append(@\"<html name=\"\"\");sb.Append( name );sb.Append(@\"\"\">\r\n\");", _sb.ToString());
        }

        [Fact]
        public void TestFormat()
        {
            string temp = @"Let's do <% string.Format(""Some stuff = {0}"", Dummy) %>.";
            ParseAndGenerate(temp);
            Assert.Equal("sb.Append(@\"Let's do \"); string.Format(\"Some stuff = {0}\", Dummy) sb.Append(@\".\r\n\");", _sb.ToString());
        }

        [Fact]
        public void TestCode()
        {
            string temp = @"<html><% if (a == 'a') { %>Hello<% } %></html>";
            ParseAndGenerate(temp);
            Assert.Equal("sb.Append(@\"<html>\"); if (a == 'a') { sb.Append(@\"Hello\"); } sb.Append(@\"</html>\r\n\");", _sb.ToString());
        }

        public void TestEchoGenerated()
        {
            ParseAndGenerate(@"<html name=""<%= name %>"">");
            TemplateArguments args = new TemplateArguments("name", "jonas");
            ITinyTemplate template = _compiler.Compile(args, _sb.ToString(), "nun");
            string result = template.Invoke(args, null);
            Assert.Equal("<html name=\"jonas\">\r\n", result);
        }

        public void TestCodeGenerated()
        {
            ParseAndGenerate(@"<html><% if (a == 'a') { %>Hello<% } %></html>");
            TemplateArguments args = new TemplateArguments("a", 'b');
			ITinyTemplate template = _compiler.Compile(args, _sb.ToString(), "nun");

            Assert.Equal("<html></html>\r\n", template.Invoke(args, null));
			Assert.Equal("<html>Hello</html>\r\n", template.Invoke(new TemplateArguments("a", 'a'), null));
        }

        private void ParseAndGenerate(string temp)
        {
            StringReader reader = new StringReader(temp);
            _gen.Parse(reader);

            StringWriter writer = new StringWriter(_sb);
            _gen.GenerateCode(writer);
        }
    }
}
