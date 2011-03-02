using System;
using System.Net;

namespace Tutorial.Tutorial3
{
    class Tutorial3 : Tutorial
    {
        private HttpServer.HttpServer _server;

        #region Tutorial Members

        public void StartTutorial()
        {
            Console.WriteLine("Welcome to Tutorial #3 - Building a own HTTP Module");
            Console.WriteLine("");
            Console.WriteLine("A http module do not handle a spefic url, instead all modules");
            Console.WriteLine("are asked by the server until one tells it that it have handled the url.");
            Console.WriteLine("");
            Console.WriteLine("In this way you can even let multiple modules handle the same, but handle");
            Console.WriteLine("different content types.");
            Console.WriteLine("");
            Console.WriteLine("Browse to http://localhost:8081/anything/you/want");

            _server = new HttpServer.HttpServer();

            // MyModule does currently handle all urls with the same message.
            _server.Add(new MyModule());

            _server.Start(IPAddress.Any, 8081);
        }

        public void EndTutorial()
        {
            _server.Stop();
        }

        public string Name
        {
            get { return "Building own HTTP module."; }
        }

        #endregion
    }
}
