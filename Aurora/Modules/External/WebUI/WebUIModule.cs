using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using HttpServer;
using HttpServer.HttpModules;
using HttpServer.Rules;
using HttpServer.Sessions;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using HttpListener = HttpServer.HttpListener;
using Mono.Addins;

namespace Aurora.Modules.WebUI
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class WebUIModule : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Dictionary<Scene,SceneCommandExecutor> executors = new Dictionary<Scene, SceneCommandExecutor>();
        private IConfigSource m_config;
        private HttpServer.HttpServer server;
        private int HttpPort = 5050;
        private bool m_Enabled = true;

        public void Initialise(IConfigSource config)
        {
            if (config.Configs["WebUI"] != null)
            {
                if (config.Configs["WebUI"].GetBoolean(
                        "Enabled", true) !=
                        true)
                {
                    m_Enabled = false;
                    return;
                }
            }
            m_config = config;
            HttpPort = config.Configs["WebUI"].GetInt("Port", 5050);
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;
            if (!executors.ContainsKey(scene))
            {
                var executor = new SceneCommandExecutor(scene);
                executors.Add(scene, executor);
                scene.EventManager.OnFrame += executor.ExecuteCommands;
            }
            if (server == null)
            {
                server = new HttpServer.HttpServer();
                var fm = new FileModule("/", "Aurora/www");

                fm.AddDefaultMimeTypes();
                server.Add(new OpenSimServicesModule(executors));
                server.Add(fm);
            }
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;
            Thread t = new Thread(new ThreadStart(RunServer));
            t.Start();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise()
        {
        }

        private void RunServer()
        {
            server.Start(IPAddress.Any, HttpPort);
        }

        private class RestartSceneCommand:ISceneCommand
        {
            public void Execute(Scene scene)
            {
                scene.RestartNow();
            }
        }

        private class OpenSimServicesModule : HttpModule
        {
            private Dictionary<Scene, SceneCommandExecutor> executors;

            public OpenSimServicesModule(Dictionary<Scene, SceneCommandExecutor> executors)
            {
                this.executors = executors;
            }

            public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
            {
                if (request.UriPath == "/Aurora/Reset")
                {
                    string listOfScenes = "";
                    foreach (var scene in executors.Keys)
                    {
                        if( listOfScenes != "")
                        {
                            listOfScenes += ", ";
                        }
                        listOfScenes += scene.RegionInfo.RegionName;
                        executors[scene].Add(new RestartSceneCommand());
                    }

                    string ret = "Triggering reset for " + listOfScenes;

                    MemoryStream mem = new MemoryStream();
                    var bytes = Utils.StringToBytes(ret);
                    mem.Write(bytes,0,bytes.Length);
                    response.Body = mem;
                    response.Status = HttpStatusCode.OK;
                    return true;    
                }

                if (request.UriPath == "/Aurora/ActiveRegions")
                {
                    string listOfScenes = "";
                    foreach (var scene in executors.Keys)
                    {
                        if (listOfScenes != "")
                        {
                            listOfScenes += ", ";
                        }
                        listOfScenes += scene.RegionInfo.RegionName;
                    }

                    string ret = listOfScenes;

                    MemoryStream mem = new MemoryStream();
                    var bytes = Utils.StringToBytes(ret);
                    mem.Write(bytes, 0, bytes.Length);
                    response.Body = mem;
                    response.Status = HttpStatusCode.OK;
                    return true;
                }

                return false;
            }
        }

        private interface ISceneCommand
        {
            void Execute(Scene scene);
        }

        private class SceneCommandExecutor
        {
            Queue<ISceneCommand> commands = new Queue<ISceneCommand>();
            private Scene scene;

            public SceneCommandExecutor(Scene scene)
            {
                this.scene = scene;
            }

            public void Add(ISceneCommand command)
            {
                commands.Enqueue(command);
            }

            public void ExecuteCommands()
            {
                while( commands.Count > 0 )
                {
                    var command = commands.Dequeue();
                    command.Execute(scene);
                }
            }
        }


        public void Close()
        {
            server.Stop();
        }

        public string Name
        {
            get { return "WebUI Module"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
    }
}