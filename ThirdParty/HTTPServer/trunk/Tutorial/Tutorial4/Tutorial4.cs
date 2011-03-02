using System;
using System.Net;
using HttpServer;
using HttpServer.Authentication;
using HttpServer.Exceptions;
using HttpServer.Rules;

namespace Tutorial.Tutorial4
{
    public class Tutorial4 : Tutorial
    {
		private HttpServer.HttpServer _server;

        class User
        {
            public int id;
            public string userName;
            public User(int id, string userName)
            {
                this.id = id;
                this.userName = userName;
            }
        }

        #region Tutorial Members

        public void StartTutorial()
        {
            _server = new HttpServer.HttpServer();

            // Let's use Digest authentication which is superior to basic auth since it
            // never sends password in clear text.
            DigestAuthentication auth = new DigestAuthentication(OnAuthenticate, OnAuthenticationRequired);
            _server.AuthenticationModules.Add(auth);

			// simple example of an regexp redirect rule. Go to http://localhost:8081/profile/arne to get redirected.
			_server.Add(new RegexRedirectRule("/profile/(?<first>[a-zA-Z0-9]+)", "/user/view/${first}"));

            // Let's reuse our module from previous tutorial to handle pages.
            _server.Add(new Tutorial3.MyModule());

            // and start the server.
            _server.Start(IPAddress.Any, 8081);

            Console.WriteLine("Goto http://localhost:8081/membersonly to get authenticated.");
            Console.WriteLine("Password is 'morsOlle', and userName is 'arne'");
        }

        private bool OnAuthenticationRequired(IHttpRequest request)
        {
            // only required authentication for "/membersonly"
            return request.Uri.AbsolutePath.StartsWith("/membersonly");
        }

        /// <summary>
        /// Delegate used to let authentication modules authenticate the user name and password.
        /// </summary>
        /// <param name="realm">Realm that the user want to authenticate in</param>
        /// <param name="userName">User name specified by client</param>
        /// <param name="password">Password supplied by the delegate</param>
        /// <param name="login">object that will be stored in a session variable called <see cref="AuthenticationModule.AuthenticationTag"/> if authentication was successful.</param>
        /// <exception cref="ForbiddenException">throw forbidden exception if too many attempts have been made.</exception>
        private void OnAuthenticate(string realm, string userName, ref string password, out object login)
        {
            // digest authentication encrypts password which means that
            // we need to provide the authenticator with a stored password.

            // you should really query a DB or something
            if (userName == "arne")
            {
                password = "morsOlle";

                // login can be fetched from IHttpSession in all modules
                login = new User(1, "arne");
            }
            else
            {
                password = string.Empty;
                login = null;
            }
        }

        public void EndTutorial()
        {
            _server.Stop();
        }

        public string Name
        {
            get { return "Using HTTP authentication."; }
        }

        #endregion
    }
}
