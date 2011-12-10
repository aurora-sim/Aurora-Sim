/*
 * 
 * See http://webserver.codeplex.com/discussions/236909 for more info
 * 
 
using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Simulation.Base;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using Aurora.Framework;
using Nini.Config;
using HttpServer.HttpModules;
using HttpServer;
using System.Net;

namespace Aurora.Example
{
    public class PHP : IService
    {
        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            HttpServer.HttpServer server = new HttpServer.HttpServer();
            AdvancedFileModule afm = new AdvancedFileModule("/", @"F:\Aurora\Aurora-WebUI\www", false, true);
            afm.ServeUnknownTypes(true, "php");
            afm.AddCgiApplication("php", @"C:\wamp\bin\php\php5.3.8\php-cgi.exe");
            server.Add(afm);
            server.Start(IPAddress.Any, 5555);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}
*/