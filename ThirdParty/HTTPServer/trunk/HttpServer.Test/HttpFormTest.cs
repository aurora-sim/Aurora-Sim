using System;
using Xunit;

namespace HttpServer.Test
{
	
	public class HttpFormTest
	{
	    readonly HttpForm _form = new HttpForm();

		#region EmptyForm tests
		[Fact]
		public void TestEmptyAddFile()
		{
		    Assert.Throws(typeof (InvalidOperationException), delegate
		                                                          { HttpForm.EmptyForm.AddFile(null); });
		}

		[Fact]
		public void TestEmptyGetFile()
		{
            Assert.Throws(typeof(InvalidOperationException), delegate
		                                               {
		                                                   HttpForm.EmptyForm.GetFile(null);
		                                               });
		}

		[Fact]
		public void TestEmptyContainsFile()
		{
			Assert.Throws(typeof(InvalidOperationException), delegate
                                                       { HttpForm.EmptyForm.ContainsFile(null); });
		}
		#endregion


		[Fact]
		public void TestArgumentNullAddFile()
		{
            Assert.Throws(typeof(ArgumentNullException), delegate
		                                               {_form.AddFile(null); });
		}

		[Fact]
		public void TestArgumentNullGetFile()
		{
            Assert.Throws(typeof(ArgumentNullException), delegate
		                                               {_form.GetFile(string.Empty); });
		}

		public void TestModifications()
		{
			HttpFile file = new HttpFile("testFile", "nun", "nun");
			_form.AddFile(file);
			Assert.Equal(file, _form.GetFile("testFile"));

			_form.Add("valueName", "value");
			Assert.Equal("value", _form["valueName"].Value);

			_form.Clear();
			Assert.Null(_form.GetFile("testFile"));
			Assert.Null(_form["valueName"].Value);
		}
	}
}
