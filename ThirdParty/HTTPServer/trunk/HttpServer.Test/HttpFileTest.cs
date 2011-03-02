using System;
using System.IO;
using Xunit;

namespace HttpServer.Test
{
	
	public class HttpFileTest
	{
		/// <summary> Test to make sure files gets deleted upon disposing </summary>
		public void TestFileDeletion()
		{
			string path = Environment.CurrentDirectory + "\\tmptest";
			HttpFile file = new HttpFile("testFile", path, "nun");

			File.WriteAllText(path, "test");
			file.Dispose();

			Assert.Equal(File.Exists(path), false);
		}

		#region Disposed tests

		/// <summary> Test to make sure name cannot be retrieved from disposed object </summary>
		[Fact]
		public void TestDisposedName()
		{
			HttpFile file = new HttpFile("object", "object", "object");
			file.Dispose();

			#pragma warning disable 168
            Assert.Throws(typeof(ObjectDisposedException), delegate { string tmp = file.Name; });
			#pragma warning restore 168
		}

		/// <summary> Test to make sure contenttype cannot be retrieved from disposed object </summary>
		[Fact]
		public void TestDisposedContent()
		{
			HttpFile file = new HttpFile("object", "object", "object");
			file.Dispose();

			#pragma warning disable 168
		    Assert.Throws(typeof (ObjectDisposedException), delegate { string tmp = file.ContentType; });
			#pragma warning restore 168
		}

		/// <summary> Test to make sure filename cannot be retrieved from disposed object </summary>
		[Fact]
		public void TestDisposedFilename()
		{
			HttpFile file = new HttpFile("object", "object", "object");
			file.Dispose();

			#pragma warning disable 168
		    Assert.Throws(typeof (ObjectDisposedException), delegate { string tmp = file.Filename; });
			#pragma warning restore 168
		}
		#endregion

		#region Null tests
		[Fact]
		public void TestNullName()
		{
            Assert.Throws(typeof(ArgumentNullException), delegate { HttpFile file = new HttpFile(null, null, null); });
		}

		[Fact]
		public void TestNullFile()
		{
		    Assert.Throws(typeof (ArgumentNullException), delegate { HttpFile file = new HttpFile("nun", null, null); });
		}

		[Fact]
		public void TestNullContent()
		{
		    Assert.Throws(typeof (ArgumentNullException), delegate { HttpFile file = new HttpFile("nun", "nun", null); });
		}
		#endregion
	}
}
