using System.IO;
using System.Text;
using Xunit;
using HttpServer.FormDecoders;

namespace HttpServer.Test.FormDecoders
{
    
    public class XmlDecoderTest
    {
        [Fact]
        public void Test()
        {
            XmlDecoder decoder = new XmlDecoder();
            Assert.True(decoder.CanParse("text/xml"));
            Assert.False(decoder.CanParse("text/plain"));
            Assert.False(decoder.CanParse("xml"));
            Assert.False(decoder.CanParse("text"));
            Assert.False(decoder.CanParse(null));

            MemoryStream stream = new MemoryStream();
            byte[] bytes = Encoding.ASCII.GetBytes(@"<user lastname=""gauffin""><firstname>jonas</firstname></user>");
            stream.Write(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);

            HttpInput input = decoder.Decode(stream, "text/xml", Encoding.ASCII);
            Assert.Equal("gauffin", input["user"]["lastname"].Value);
            Assert.Equal("jonas", input["user"]["firstname"].Value);
            Assert.Null(input["unknow"].Value);
            Assert.Equal(HttpInputItem.Empty, input["unknown"]);
        }

        [Fact]
        public void TestNull()
        {
            XmlDecoder decoder = new XmlDecoder();
            Assert.Null(decoder.Decode(new MemoryStream(), "text/xml", Encoding.ASCII));
        }

        [Fact]
        public void TestInvalidData()
        {
            XmlDecoder decoder = new XmlDecoder();
            MemoryStream stream = new MemoryStream();
            byte[] bytes = Encoding.ASCII.GetBytes(@"<user lastname=""gauffin""><firstname>jonas</firstname>");
            stream.Write(bytes, 0, bytes.Length);
            stream.Seek(0, SeekOrigin.Begin);
            Assert.Throws(typeof (InvalidDataException), delegate
                                                             {
                                                                 decoder.Decode(stream, "text/xml",
                                                                                Encoding.ASCII);
                                                             });
        }

        [Fact]
        public void TestNull2()
        {
            XmlDecoder decoder = new XmlDecoder();
            decoder.Decode(null, null, null);
        }
    }
}
