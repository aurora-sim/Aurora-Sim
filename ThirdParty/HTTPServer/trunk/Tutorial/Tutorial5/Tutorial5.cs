using System;
using System.Net;
using System.Reflection;
using Fadd;
using Fadd.Globalization;
using Fadd.Globalization.Yaml;
using HttpServer.Helpers;
using HttpServer.HttpModules;
using HttpServer.MVC;
using HttpServer.MVC.Rendering;
using HttpServer.MVC.Rendering.Haml;
using Tutorial.Tutorial5.Controllers;

namespace Tutorial.Tutorial5
{
    /// <summary>
    /// Tutorial 5.
    /// </summary>
    public class Tutorial5 : Tutorial
    {
        private readonly HttpServer.HttpServer _server = new HttpServer.HttpServer();
        private readonly LanguageNode _language = new MemLanguageNode(1033, "Root");

        public void StartTutorial()
        {
            // load language from a YAML file.
            new YamlWatcher(_language, "..\\..\\tutorial5\\language.yaml"); // "..\\..\\" since we run the tutorial in vstudio
            Validator.Language = _language.GetChild("Validator") ?? LanguageNode.Empty;
            
            // since we do not use files on disk, we'll just add the resource template loader.
            ResourceTemplateLoader templateLoader = new ResourceTemplateLoader();
            templateLoader.LoadTemplates("/", Assembly.GetExecutingAssembly(), "Tutorial.Tutorial5.views");
            TemplateManager templateManager = new TemplateManager(templateLoader);
            templateManager.AddType(typeof (WebHelper));
            templateManager.Add("haml", new HamlGenerator());
            

            // we've just one controller. Add it.
            ControllerModule controllerModule = new ControllerModule();
            controllerModule.Add(new UserController(templateManager, _language));
            _server.Add(controllerModule);

            // add file module, to be able to handle files
            ResourceFileModule fileModule = new ResourceFileModule();
            fileModule.AddResources("/", Assembly.GetExecutingAssembly(), "Tutorial.Tutorial5.public");
            _server.Add(fileModule);

            // ok. We should be done. Start the server.
            _server.Start(IPAddress.Any, 8081);

            Console.WriteLine("Tutorial 5 is running. Go to http://localhost:8081/user/");
            Console.WriteLine("Try to add '?lcid=1053' and '?lcid=1033' to address to switch language (i.e: http://localhost:8081/user/?lcid=1053).");
        }

        public void EndTutorial()
        {
            _server.Stop();
        }

        public string Name
        {
            get { return "Demo of Models, Views, Localization and Validations"; }
        }
    }
}
