using System.Collections.Specialized;
using Fadd;
using Fadd.Globalization;
using Xunit;

namespace HttpServer.Test.Helpers
{
	public class FormValidatorTest
	{
	    readonly NameValueCollection _errors;
	    readonly Validator _validator;

        public FormValidatorTest()
		{
			_errors = new NameValueCollection();
			_validator = new Validator(_errors, LanguageNode.Empty);
		}

        [Fact]
		public void TestHex()
		{
			_errors.Clear();
			Assert.Equal("1234567890ABCDEF", _validator.Hex("hex", "1234567890ABCDEF", false));
			Assert.Equal("abcdef1234567890", _validator.Hex("hex", "abcdef1234567890", false));
			Assert.Equal(string.Empty, _validator.Letters(null, null, false));
			Assert.Equal(0, _errors.Count);

			Assert.Equal(string.Empty, _validator.Hex("hex", "###", false));
			Assert.Equal(1, _errors.Count);
			_errors.Clear();

			Assert.Equal(string.Empty, _validator.Hex("hex", "", true));
			Assert.Equal(1, _errors.Count);
			_errors.Clear();
		}

        [Fact]
        public void TestLetters()
		{
			_errors.Clear();
			Assert.Equal("abcde XYZ", _validator.Letters(string.Empty, "abcde XYZ", false));
			Assert.Equal("abcdeXYZ≈ƒ÷Â‰ˆ", _validator.Letters(string.Empty, "abcdeXYZ≈ƒ÷Â‰ˆ", false));
			Assert.Equal("abc ab ab c", _validator.Letters(string.Empty, "abc ab ab c", false, "abc abc abc"));
			Assert.Equal(string.Empty, _validator.Letters(null, null, false));
			Assert.Equal(0, _errors.Count);

			Assert.Equal(string.Empty, _validator.Letters(string.Empty, string.Empty, true));
			Assert.Equal(1, _errors.Count);
			_errors.Clear();

            // need to be fixed in fadd
            /*
			Assert.Equal(string.Empty, _validator.Letters("MyName", "Test206", false));
			Assert.Equal(1, _errors.Count);
			_errors.Clear();

			Assert.Equal(string.Empty, _validator.Letters(string.Empty, "Test206", false, " 1"));
			Assert.Equal(1, _errors.Count);
			_errors.Clear();

			Assert.Equal(string.Empty, _validator.Letters(string.Empty, "Test206", false, " 1"));
			Assert.Equal(1, _errors.Count);
			_errors.Clear();
             * */
		}
	}
}
