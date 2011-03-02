using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using HttpServer.Authentication;
using Xunit;

namespace HttpServer.Test.Authentication
{
    /// <summary>
    /// http://en.wikipedia.org/wiki/Digest_authentication
    /// </summary>
    
    public class DigestAuthTest
    {

        public DigestAuthTest()
        {
            DigestAuthentication.DisableNonceCheck = true;
        }

        [Fact]
        public void TestResponse()
        {
            DigestAuthentication digest = new DigestAuthentication(OnTestAuth, null);
            string response = digest.CreateResponse("blaj", false);
            Assert.Equal("Digest ", response.Substring(0, 7));

            NameValueCollection parts = Decode(response);
            Assert.NotNull(parts["realm"]);
            Assert.NotNull(parts["qop"]);
            Assert.NotNull(parts["nonce"]);
            Assert.NotNull(parts["opaque"]);
            Assert.Equal("blaj", parts["realm"]);
        }

        [Fact]
        public void TestDecoder()
        {
            NameValueCollection col = DigestAuthentication.Decode(@"Digest username=""Mufasa"",
                      realm=""testrealm@host.com"",
                      nonce=""dcd98b7102dd2f0e8b11d0f600bfb0c093"",
                      uri=""/dir/index.html"",
                      qop=auth,
                      nc=00000001,
                      cnonce=""0a4f113b"",
                      response=""6629fae49393a05397450978507c4ef1"",
                      opaque=""5ccc069c403ebaf9f0171e9517f40e41"" ", Encoding.ASCII);
            Assert.Equal("testrealm@host.com", col["realm"]);
            Assert.Equal("dcd98b7102dd2f0e8b11d0f600bfb0c093", col["nonce"]);
            Assert.Equal("/dir/index.html", col["uri"]);
            Assert.Equal("auth", col["qop"]);
            Assert.Equal("00000001", col["nc"]);
            Assert.Equal("0a4f113b", col["cnonce"]);
            Assert.Equal("6629fae49393a05397450978507c4ef1", col["response"]);
            Assert.Equal("5ccc069c403ebaf9f0171e9517f40e41", col["opaque"]);
        }

        [Fact]
        public void TestDecoderFailure()
        {
            Assert.Null(DigestAuthentication.Decode("NoDigest", Encoding.ASCII));

            NameValueCollection col = DigestAuthentication.Decode("Digest \x5real=\"\"", Encoding.ASCII);
            Assert.Null(col);
        }

        [Fact]
        public void TestAuth()
        {
            DigestAuthentication auth = new DigestAuthentication(OnTestAuth, null);
            object res = auth.Authenticate(
                @"Digest username=""Mufasa"",
                      realm=""testrealm@host.com"",
                      nonce=""dcd98b7102dd2f0e8b11d0f600bfb0c093"",
                      uri=""/dir/index.html"",
                      qop=auth,
                      nc=00000001,
                      cnonce=""0a4f113b"",
                      response=""6629fae49393a05397450978507c4ef1"",
                      opaque=""5ccc069c403ebaf9f0171e9517f40e41"" ", "testrealm@host.com", "GET", false);

            Assert.NotNull(res);
            Assert.Equal("testobj", (string)res);
        }

        private static void OnTestAuth(string realm, string userName, ref string password, out object login)
        {
            Assert.Equal("testrealm@host.com", realm);
            Assert.Equal("Mufasa", userName);
            password = "Circle Of Life";
            login = "testobj";
        }

        [Fact]
        public void TestAuth2()
        {
            string realm = "myrealm";
            string userName = "Jonas";
            string password = "morsOlle";
            DigestAuthentication auth = new DigestAuthentication(OnAuth2, null);
            string server = auth.CreateResponse(realm);

            NameValueCollection args = Decode(server);
            string cnonce = "a773bd8";

            string response = CreateResponse(userName, realm, password, args["nonce"], cnonce, args["qop"]);

            string client = string.Format(
                "Digest username=\"{6}\", realm=\"{5}\", nonce={0}, uri=\"{1}\", qop=auth, nc=00000001, cnonce=\"{2}\", response=\"{3}\", opaque=\"{4}\"",
                args["nonce"],
                "/membersonly/",
                cnonce,
                response,
                args["opaque"],
                realm,
                userName);

            object obj = auth.Authenticate(client, realm, "GET");
            Assert.NotNull(obj);
            Assert.Equal("hello", (string)obj);
        }


        private string CreateResponse(string userName, string realm, string password, string nonce, string cnonce, string qop)
        {
            string ha1 = HashString(string.Format("{0}:{1}:{2}", userName, realm, password));
            string ha2 = HashString(string.Format("{0}:{1}", "GET", "/membersonly/"));
            return HashString(string.Format("{0}:{1}:{2}:{3}:{4}:{5}", ha1, nonce, "00000001", cnonce, qop, ha2));
        }

        private static void OnAuth2(string realm, string userName, ref string password, out object login)
        {
            password = "morsOlle";
            login = "hello";
        }

        public string HashString(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("x2"));

            return sb.ToString();
        }

        public static NameValueCollection Decode(string response)
        {
            NameValueCollection reqInfo = new NameValueCollection();
            string[] elems = response.Substring(7).Split(new char[] { ',' });
            foreach (string elem in elems)
            {
                // form key="value"
                string[] parts = elem.Split(new char[] { '=' }, 2);
                string key = parts[0].Trim(new char[] { ' ', '\"' });
                string val = parts[1].Trim(new char[] { ' ', '\"' });
                reqInfo.Add(key, val);
            }

            return reqInfo;
        }
    }
}
