using System.IO;
using HttpServer;
using HttpServer.HttpModules;
using HttpServer.Sessions;

namespace Tutorial.Tutorial3
{
    class MyModule : HttpModule
    {
        /// <summary>
        /// Method that process the URL
        /// </summary>
        /// <param name="request">Information sent by the browser about the request</param>
        /// <param name="response">Information that is being sent back to the client.</param>
        /// <param name="session">Session used to </param>
        /// <returns>true if this module handled the request.</returns>
        public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
        {
            if (session["times"] == null)
                session["times"] = 1;
            else
                session["times"] = ((int) session["times"]) + 1;

            StreamWriter writer = new StreamWriter(response.Body);
            writer.WriteLine("Hello dude, you have been here " + session["times"] + " times.");
            writer.Flush();

            // return true to tell webserver that we've handled the url
            return true;
        }
    }
}
