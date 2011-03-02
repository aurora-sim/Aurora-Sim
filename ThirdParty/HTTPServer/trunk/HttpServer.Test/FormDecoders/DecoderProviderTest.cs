using System.IO;
using HttpServer.FormDecoders;
using Xunit;

namespace HttpServer.Test.FormDecoders
{
    
    public class DecoderProviderTest
    {
        private readonly FormDecoderProvider _provider;
        private readonly Stream _stream;
        private readonly MyDefaultDecoder _myDecoder;

        public DecoderProviderTest()
        {
            _myDecoder = new MyDefaultDecoder();
            _provider = new FormDecoderProvider();
            _provider.Add(_myDecoder);
            _provider.Add(new XmlDecoder());
            _stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(_stream);
            writer.WriteLine("<user><firstname>jonas</firstname></user>");
            writer.Flush();
            _stream.Seek(0, SeekOrigin.Begin);
        }

        [Fact]
        public void Test()
        {
            HttpInput input = _provider.Decode("text/xml", _stream, null);
            Assert.Equal("jonas", input["user"]["firstname"].Value);
        }

        [Fact]
        public void TestExceptions1()
        {
            Assert.Throws(typeof(System.ArgumentException), delegate { _provider.Decode(null, null, null); });
        }

        [Fact]
        public void TestDefaultDecoder()
        {
            _provider.DefaultDecoder = _myDecoder;
            _provider.Decode(null, _stream, null);
            Assert.True(_myDecoder.Called);
        }

        
    }
}
