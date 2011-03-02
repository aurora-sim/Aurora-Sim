using System;
using HttpServer.Authentication;
using Xunit;

namespace HttpServer.Test.Authentication
{
    
    public class BasicAuthTest
    {
        private BasicAuthentication _auth;

        /* Wikipedia:
         * http://en.wikipedia.org/wiki/Basic_access_authentication
         * 
         * "Aladdin:open sesame"
         * should equal
         * "QWxhZGRpbjpvcGVuIHNlc2FtZQ=="
         */

        public BasicAuthTest()
        {
            _auth = new BasicAuthentication(OnAuth, OnRequired);
        }

        private void OnAuth(string realm, string userName, ref string password, out object login)
        {
            Assert.Equal("myrealm", realm);
            Assert.Equal("Aladdin", userName);

            password = "open sesame";
            login = "mylogin";
        }

        private bool OnRequired(IHttpRequest request)
        {
            return true;
        }

        [Fact]
        public void TestResponse1()
        {
            Assert.Throws(typeof(ArgumentNullException), delegate { _auth.CreateResponse(null, false); });
        }

        [Fact]
        public void TestResponse2()
        {
            string response = _auth.CreateResponse("myrealm", false);
            Assert.Equal("Basic realm=\"myrealm\"", response);
        }

        [Fact]
        public void TestAuth()
        {
            _auth.Authenticate("Basic " + "QWxhZGRpbjpvcGVuIHNlc2FtZQ==", "myrealm", "POST", false);
            //OnAuth will to the checks
        }

    }
}
