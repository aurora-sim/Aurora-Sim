using System;
using System.Net;
using HttpServer.HttpModules;
using HttpServer.MVC;
using HttpServer.MVC.Rendering;
using HttpServer.MVC.Rendering.Haml;
using HttpServer.Sample.Controllers;

namespace HttpServer.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Template generators are used to render templates 
            // (convert code + html to pure html).
            TemplateManager mgr = new TemplateManager();
            mgr.Add("haml", new HamlGenerator());

            // The httpserver is quite dumb and will only serve http, nothing else.
            HttpServer server  = new HttpServer();

            // a controller mode implements a MVC pattern
            // You'll add all controllers to the same module.
            ControllerModule mod = new ControllerModule();
            mod.Add(new UserController(mgr));
            server.Add(mod);

            // file module will be handling files
            FileModule fh = new FileModule("/", Environment.CurrentDirectory);
            fh.AddDefaultMimeTypes();
            server.Add(fh);

            // Let's start pure HTTP, we can also start a HTTPS listener.
            server.Start(IPAddress.Any, 8081);

            Console.ReadLine();
        }
    }
}
