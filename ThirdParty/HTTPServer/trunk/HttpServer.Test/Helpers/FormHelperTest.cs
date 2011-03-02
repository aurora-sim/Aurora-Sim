using HttpServer.Helpers;
using Xunit;

namespace HttpServer.Test.Helpers
{/* todo: fix these tests
    
    public class FormHelperTest
    {
        [Fact]
        public void TestAjax()
        {
            string start = FormHelper.Start("myName", "/user/new/", true);
            Assert.Equal(@"<form name=""myName"" action=""/user/new/"" method=""post"" id=""myName"" onsubmit="""
                            + FormHelper.JSImplementation.AjaxFormOnSubmit() + "return false;\">", start);
        }

        [Fact]
        public void TestAjaxOnSubmit()
        {
            string start = FormHelper.Start("myName", "/user/new/", true, "onsubmit", "alert('hello world')");
            Assert.Equal(
                @"<form name=""myName"" action=""/user/new/"" id=""myName"" method=""post"" onsubmit=""alert('hello world');"
                + FormHelper.JSImplementation.AjaxFormOnSubmit() + "return false;\">", start);
        }

        [Fact]
        public void TestAjaxOnSubmit2()
        {
            string start = FormHelper.Start("myName", "/user/new/", true, "onsuccess:", "alert('Hello world!')");
            Assert.Equal(@"<form name=""myName"" action=""/user/new/"" id=""myName"" method=""post"" onsubmit="""
                            + FormHelper.JSImplementation.AjaxFormOnSubmit("onsuccess:", "alert(\'Hello world!\')") +
                            "return false;\">", start);
        }

        [Fact]
        public void TestAjaxOnSubmitAndDelete()
        {
            string start = FormHelper.Start("myName", "/user/new/", true, "onsubmit", "alert('hello world')", "method",
                                            "delete");
            Assert.Equal(
                @"<form name=""myName"" action=""/user/new/"" id=""myName"" onsubmit=""alert(\'hello world\');"
                + FormHelper.JSImplementation.AjaxFormOnSubmit() + @"return false;"" method=""delete"">", start);
        }

        [Fact]
        public void TestExtraAttributes()
        {
            string start = FormHelper.Start("myName", "/user/new/", false, "class", "worldClass", "style",
                                            "display:everything;");
            Assert.Equal(
                @"<form name=""myName"" action=""/user/new/"" id=""myName"" method=""post"" class=""worldClass"" style=""display:everything;"">",
                start);
        }

        [Fact]
        public void TestStart()
        {
            string start = FormHelper.Start("myName", "/user/new/", false);
            Assert.Equal(@"<form name=""myName"" action=""/user/new/"" id=""myName"" method=""post"">", start);
        }

        [Fact]
        public void TestStartDelete()
        {
            string start = FormHelper.Start("myName", "/user/new/", false, "method", "delete");
            Assert.Equal(@"<form name=""myName"" action=""/user/new/"" id=""myName"" method=""delete"">", start);
        }
    }
  * */
}