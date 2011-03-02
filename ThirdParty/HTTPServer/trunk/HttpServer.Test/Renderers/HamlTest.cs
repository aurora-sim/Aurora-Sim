using System.IO;
using System.Text;
using HttpServer.MVC.Rendering;
using HttpServer.MVC.Rendering.Haml;
using Xunit;

namespace HttpServer.Test.Renderers
{
    
    public class HamlTest
    {
        private readonly TemplateManager _mgr = new TemplateManager();
        private readonly HamlGenerator _generator = new HamlGenerator();

        public HamlTest()
        {
            _mgr.Add("haml", _generator);
        }


        public void Test1()
        {
            Assert.Equal("<head><title>MyTitle</title></head>", _mgr.Render("hamlsamples/test1.haml", new TemplateArguments()));
        }

        public void Test2()
        {
            string realText =
                @"<html>
	<head>
		<title>This is my superfine title</title>
		<script>alert(""Welcome to my world"");</script>
	</head>
</html>
";
            string text = _mgr.Render("hamlsamples/test2.haml", new TemplateArguments());
            Assert.Equal(realText, text);
        }

        public void Test4()
        {
            StringReader reader = new StringReader(@"%script{type=""text/javascript""}
	function selectAll(source, cat) {
		var elems = $('tbl').getElementsByClassName(cat);
		for each (var item in elems)
			$(item).checked = source.checked;
	}");
            _generator.Parse(reader);
            _generator.PrintDocument();

            StringBuilder sb = new StringBuilder();
            StringWriter writer = new StringWriter(sb);
            _generator.GenerateCode(writer);

        }
        public void Test3()
        {
            string text = _mgr.Render("hamlsamples/test3.haml", new TemplateArguments("a", 1));
        }

        public void TestLayout()
        {
            string text = _mgr.Render("hamlsamples/testlayout.haml", new TemplateArguments("data", "shit"));
        }

        public void TestCodeTags()
        {
            string text = _mgr.Render("hamlsamples/CodeSample.haml", new TemplateArguments("i", 1));
        }

		public void TestMultiLine()
		{
			TextReader input = new StringReader(@"
%html
	%head
		%title theTitle
	%body
		Show for loop
		- for(int i = 0; i < |
		2; i++)
			= ""for loop"" |
			+ "" entry = "" + |
			i
");

			string expectedOutput = @"<html>
	<head>
		<title>
theTitle		</title>
	</head>
	<body>
		Show for loop
		<% for(int i = 0; i < 2; i++)
<%=  ""for loop"" + "" entry = "" + i %>		%>
	</body>
</html>
";			
			_generator.Parse(input);
			TextWriter output = new StringWriter(new StringBuilder());
			_generator.GenerateHtml(output);

			Assert.Equal(expectedOutput, output.ToString());
		}
    }
}
