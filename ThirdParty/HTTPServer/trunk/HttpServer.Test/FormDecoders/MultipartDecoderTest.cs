using System;
using System.IO;
using System.Text;
using HttpServer.FormDecoders;
using Xunit;

namespace HttpServer.Test.FormDecoders
{
	
	public class MultipartDecoderTest
	{
		private readonly MultipartDecoder _decoder = new MultipartDecoder();

		private readonly string requestText =
			@"-----------------------------18506757825014
Content-Disposition: form-data; name=""VCardFile""; filename=""vCard.vcf""
Content-Type: text/x-vcard

BEGIN:VCARD
N:Dikman;David;;;
EMAIL;INTERNET:a05davdi@student.his.se
ORG:Gauffin Telecom
TEL;VOICE;WORK:023-6661214
END:VCARD

-----------------------------18506757825014
Content-Disposition: form-data; name=""HiddenField[monkeyAss]""

Hejsan
-----------------------------18506757825014
Content-Disposition: form-data; name=""HiddenField[buttomAss]""

Tjosan
-----------------------------18506757825014--
";
		/// <summary> Test the parsing information function </summary>
		public void TestCanParse()
		{
			Assert.Equal(false, _decoder.CanParse("not-a-content-type"));
			Assert.Equal(true, _decoder.CanParse("multipart/form-data"));
		}

		/// <summary> Test against a null stream </summary>
		[Fact]
		public void TestNull()
		{
		    Assert.Throws(typeof (ArgumentNullException), delegate
		                                                      {
		                                                          _decoder.Decode(null,
		                                                                          "multipart/fodsa-data; boundary=---------------------------18506757825014",
		                                                                          Encoding.UTF8);
		                                                      });
		}

		/// <summary> Test an incorrect request content type </summary>
		[Fact]
		public void TestIncorrectContentType()
		{
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(requestText)))
                Assert.Throws(typeof (InvalidOperationException), delegate
                                                                {
                                                                    _decoder.Decode(stream,
                                                                                    "mudfsltipart/fodsa-data; boundary=---------------------------18506757825014",
                                                                                    Encoding.UTF8);
                                                                });
		}

		/// <summary> Test an incorrect request content type </summary>
		[Fact]
		public void TestIncorrectContentType2()
		{
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(requestText)))
                Assert.Throws(typeof(InvalidDataException), delegate
                                                                      {
                                                                          _decoder.Decode(stream,
                                                                                          "multipart/form-data; boundary!---------------------------18506757825014",
                                                                                          Encoding.UTF8);
                                                                      });
		}

		/// <summary> Test an incorrect request string </summary>
		[Fact]
		public void TestIncorrectData()
		{
			string newRequest = requestText.Replace("Content", "snedSådan");
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(newRequest)))
                Assert.Throws(typeof (InvalidDataException), delegate
                                                                 {
                                                                     _decoder.Decode(stream,
                                                                                     "multipart/form-data; boundary=---------------------------18506757825014",
                                                                                     Encoding.UTF8);
                                                                 });
		}

		/// <summary> Test a correct decoding </summary>
		public void TestDecode()
		{
			MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(requestText));
			HttpForm form = _decoder.Decode(stream, "multipart/form-data; boundary=---------------------------18506757825014", Encoding.UTF8);

			Assert.True(form["HiddenField"].Contains("buttomAss"));
			Assert.True(form["HiddenField"].Contains("monkeyAss"));
			Assert.Equal("Hejsan", form["HiddenField"]["monkeyAss"].Value);
			Assert.Equal("Tjosan", form["HiddenField"]["buttomAss"].Value);
			Assert.NotNull(form.GetFile("VCardFile"));
			Assert.False(string.IsNullOrEmpty(form.GetFile("VCardFile").Filename));
			Assert.True(File.Exists(form.GetFile("VCardFile").Filename));
			form.GetFile("VCardFile").Dispose();

			stream.Dispose();
		}
	}
}
