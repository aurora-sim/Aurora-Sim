using HttpServer.HttpModules;
using HttpServer.Sessions;

namespace HttpServer.Test.HttpModules
{
    public class ProxyTestModule : HttpModule
    {
        /// <summary>
        /// Method that process the Uri
        /// </summary>
        /// <param name="request">Information sent by the browser about the request</param>
        /// <param name="response">Information that is being sent back to the client.</param>
        /// <param name="session">Session used to </param>
        /// <returns>true if this module handled the request.</returns>
        public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
        {
            return false;
        }
    }
}
