using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HttpServer;
using HttpListener=HttpServer.HttpListener;

namespace Tutorial.Tutorial2
{
    class Tutorial2 : Tutorial
    {
        private HttpListener _listener;
        private X509Certificate2 _cert;

        #region Tutorial Members

        public void StartTutorial()
        {
            if (!File.Exists("../../certInProjectFolder.p12"))
            {
                Console.WriteLine("Create a certificate first. ");
                Console.WriteLine("OpenSSL: http://www.openssl.org/");
                Console.WriteLine("Create a certificate: http://www.towersoft.com/sdk/doku.php?id=ice:setting_up_an_ice_server_to_use_ssl");
                Console.WriteLine();
                Console.WriteLine("Create the cert and place it in the tutorial project folder with the name 'certInProjectFolder.p12'.");
                return;
            }

            Console.WriteLine("Welcome to tutorial number 2, which will demonstrate how to setup HttpListener for secure requests.");
            Console.WriteLine();
            Console.WriteLine("You will need to create a certificate yourself. A good guide for OpenSSL can be found here:");
            Console.WriteLine("http://www.towersoft.com/sdk/doku.php?id=ice:setting_up_an_ice_server_to_use_ssl");
            Console.WriteLine();
            Console.WriteLine("Browse to https://localhost/hello when you have installed your certificate.");

            _cert = new X509Certificate2("../../certInProjectFolder.p12", "yourCertPassword");
            _listener = HttpListener.Create(IPAddress.Any, 443, _cert);
            _listener.RequestReceived+= OnSecureRequest;
            _listener.Start(5);
        }

        private void OnSecureRequest(object source, RequestEventArgs args)
        {
            IHttpClientContext context = (IHttpClientContext)source;
            IHttpRequest request = args.Request;

            // Here we create a response object, instead of using the client directly.
            // we can use methods like Redirect etc with it,
            // and we dont need to keep track of any headers etc.
        	IHttpResponse response = request.CreateResponse(context);

            byte[] body = Encoding.UTF8.GetBytes("Hello secure you!");
            response.Body.Write(body, 0, body.Length);
            response.Send();
        }

        public void EndTutorial()
        {
            _listener.Stop();
        }

        public string Name
        {
            get { return "Demo of HttpListener in HTTPS mode."; }
        }

        #endregion
    }
}
