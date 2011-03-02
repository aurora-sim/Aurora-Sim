using System.IO;
using System.Text;
using System.Web;
using Xunit;
using HttpServer.FormDecoders;

namespace HttpServer.Test.FormDecoders
{
    
    public class UrlDecoderTest
    {


        [Fact]
        public void Test()
        {
            UrlDecoder decoder = new UrlDecoder();
            Assert.True(decoder.CanParse("application/x-www-form-urlencoded"));
            Assert.False(decoder.CanParse("text/plain"));
            Assert.False(decoder.CanParse(string.Empty));
            Assert.False(decoder.CanParse(null));

            MemoryStream stream = new MemoryStream();
            string urlencoded = HttpUtility.UrlEncode(@"user[firstname]=jonas&user[extension][id]=1&myname=jonas&user[firstname]=arne");
            byte[] bytes = Encoding.ASCII.GetBytes(urlencoded);
            stream.Write(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);

            HttpInput input = decoder.Decode(stream, "application/x-www-form-urlencoded", Encoding.ASCII);
            Assert.Equal("jonas", input["myname"].Value);
            Assert.Equal(2, input["user"]["firstname"].Count);
            Assert.Equal("jonas", input["user"]["firstname"].Values[0]);
            Assert.Equal("arne", input["user"]["firstname"].Values[1]);
            Assert.Equal("1", input["user"]["extension"]["id"].Value);
            Assert.Null(input["unknow"].Value);
            Assert.Equal(HttpInputItem.Empty, input["unknown"]);
        }

        [Fact]
        public void TestLarge()
        {
            string url =
                "ivrMenu[Name]=Huvudmeny&ivrMenu[ExtensionId]=6&ivrMenu[OpenPhraseId]=267&ivrMenu[ScheduleId]=1&ivrMenu[ClosePhraseId]=268&ivrMenu[CloseActionId]=3&ivrMenu[CloseActionValue]=26&ivrMenu[TimeoutPhraseId]=267&ivrMenu[TimeoutActionId]=&ivrMenu[TimeoutActionValue]=&ivrMenu[TimeoutSeconds]=10&ivrMenu[Digits][1][Digit]=1&ivrMenu[Digits][1][ActionId]=1&ivrMenu[Digits][1][ActionValue]=49&ivrMenu[Digits][2][Digit]=2&ivrMenu[Digits][2][ActionId]=&ivrMenu[Digits][2][ActionValue]=&ivrMenu[Digits][3][Digit]=3&ivrMenu[Digits][3][ActionId]=&ivrMenu[Digits][3][ActionValue]=&ivrMenu[Digits][4][Digit]=4&ivrMenu[Digits][4][ActionId]=&ivrMenu[Digits][4][ActionValue]=&ivrMenu[Digits][5][Digit]=5&ivrMenu[Digits][5][ActionId]=&ivrMenu[Digits][5][ActionValue]=&ivrMenu[Digits][6][Digit]=6&ivrMenu[Digits][6][ActionId]=&ivrMenu[Digits][6][ActionValue]=&ivrMenu[Digits][7][Digit]=7&ivrMenu[Digits][7][ActionId]=&ivrMenu[Digits][7][ActionValue]=&ivrMenu[Digits][8][Digit]=8&ivrMenu[Digits][8][ActionId]=&ivrMenu[Digits][8][ActionValue]=&ivrMenu[Digits][9][Digit]=9&ivrMenu[Digits][9][ActionId]=&ivrMenu[Digits][9][ActionValue]=&ivrMenu[Digits][0][ActionId]=&ivrMenu[Digits][0][ActionValue]=&ivrMenu[Digits][*][ActionId]=&ivrMenu[Digits][*][ActionValue]=&ivrMenu[Digits][#][ActionId]=&ivrMenu[Digits][#][ActionValue]=";

            UrlDecoder decoder = new UrlDecoder();
            Assert.True(decoder.CanParse("application/x-www-form-urlencoded"));
            Assert.False(decoder.CanParse("text/plain"));
            Assert.False(decoder.CanParse(string.Empty));
            Assert.False(decoder.CanParse(null));

            MemoryStream stream = new MemoryStream();
            string urlencoded = HttpUtility.UrlEncode(url);
            byte[] bytes = Encoding.ASCII.GetBytes(urlencoded);
            stream.Write(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);

            HttpInput input = decoder.Decode(stream, "application/x-www-form-urlencoded", Encoding.ASCII);
            Assert.Equal("Huvudmeny", input["ivrMenu"]["Name"].Value);
            Assert.Equal("6", input["ivrMenu"]["ExtensionId"].Value);
            Assert.Equal("267", input["ivrMenu"]["OpenPhraseId"].Value);
            Assert.Equal("1", input["ivrMenu"]["Digits"]["1"]["Digit"].Value);
            Assert.Equal("1", input["ivrMenu"]["Digits"]["1"]["ActionId"].Value);
            Assert.Equal("49", input["ivrMenu"]["Digits"]["1"]["ActionValue"].Value);
        }

        [Fact]
        public void TestLogin()
        {
            string url =
                "email=somewhere%40gauffin.com&password=myPassWord";

            UrlDecoder decoder = new UrlDecoder();
            Assert.True(decoder.CanParse("application/x-www-form-urlencoded"));
            Assert.False(decoder.CanParse("text/plain"));
            Assert.False(decoder.CanParse(string.Empty));
            Assert.False(decoder.CanParse(null));

            MemoryStream stream = new MemoryStream();
            string urlencoded = url;
            byte[] bytes = Encoding.ASCII.GetBytes(urlencoded);
            stream.Write(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);

            HttpInput input = decoder.Decode(stream, "application/x-www-form-urlencoded", Encoding.ASCII);
            Assert.Equal("somewhere@gauffin.com", input["email"].Value);
            Assert.Equal("myPassWord", input["password"].Value);
        }

        [Fact]
        public void TestNull()
        {
            UrlDecoder decoder = new UrlDecoder();
            Assert.Null(decoder.Decode(new MemoryStream(), "application/x-www-form-urlencoded", Encoding.ASCII));
        }

        [Fact]
        public void TestSimple()
        {
            UrlDecoder decoder = new UrlDecoder();
            using (MemoryStream stream = new MemoryStream())
            {
                byte[] bytes = Encoding.ASCII.GetBytes(@"not encoded or anything");
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
            	HttpForm form = decoder.Decode(stream,
            	               "application/x-www-form-urlencoded",
            	               Encoding.ASCII);
				Assert.Equal("not encoded or anything", form[string.Empty].Value);
            }
        }

        [Fact]
        public void TestNull2()
        {
            UrlDecoder decoder = new UrlDecoder();
            decoder.Decode(null, null, null);
        }
    }
}
