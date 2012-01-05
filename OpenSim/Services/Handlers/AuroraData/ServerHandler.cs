/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services
{
    public class ServerConnector : IService, IGridRegistrationUrlModule
    {
        private IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IGridRegistrationUrlModule Members

        public string UrlName
        {
            get { return "ServerURI"; }
        }

        public void AddExistingUrlForClient(string SessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);

            server.AddStreamHandler(new ServerHandler(url, SessionID, m_registry));
        }

        public string GetUrlForRegisteringClient(string SessionID, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);
            string url = "/server" + UUID.Random();

            server.AddStreamHandler(new ServerHandler(url, SessionID, m_registry));

            return url;
        }

        public void RemoveUrlForClient(string sessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);
            server.RemoveHTTPHandler("POST", url);
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["AuroraConnectors"];
            if (!handlerConfig.GetBoolean("AllowRemoteCalls", false))
                return;

            m_registry = registry;

            m_registry.RequestModuleInterface<IGridRegistrationService>().RegisterModule(this);
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }

    public class MethodImplementation
    {
        public MethodInfo Method;
        public IAuroraDataPlugin Reference;
    }

    public unsafe class ServerHandler : BaseStreamHandler
    {
        protected string m_SessionID;
        protected IRegistryCore m_registry;
        protected static Dictionary<string, List<MethodImplementation>> m_methods = new Dictionary<string, List<MethodImplementation>>();

        public ServerHandler(string url, string SessionID, IRegistryCore registry) :
            base("POST", url)
        {
            m_SessionID = SessionID;
            m_registry = registry;
            m_methods = new Dictionary<string, List<MethodImplementation>>();
            List<string> alreadyRunPlugins = new List<string>();
            foreach(IAuroraDataPlugin plugin in Aurora.DataManager.DataManager.GetPlugins())
            {
                if (alreadyRunPlugins.Contains(plugin.Name))
                    continue;
                alreadyRunPlugins.Add(plugin.Name);
                foreach (MethodInfo method in plugin.GetType().GetMethods())
                {
                    if (Attribute.GetCustomAttribute(method, typeof(CanBeReflected)) != null)
                    {
                        List<MethodImplementation> methods = new List<MethodImplementation>();
                        MethodImplementation imp = new MethodImplementation() { Method = method, Reference = plugin };
                        if (!m_methods.TryGetValue(method.Name, out methods))
                            m_methods.Add(method.Name, (methods = new List<MethodImplementation>()));

                        methods.Add(imp);
                    }
                }
            }
        }

        public override byte[] Handle(string path, Stream requestData,
                                      OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            OSDMap args = WebUtils.GetOSDMap(body);
            if (args.ContainsKey("Method"))
            {
                IGridRegistrationService urlModule =
                    m_registry.RequestModuleInterface<IGridRegistrationService>();
                string method = args["Method"].AsString();

                MethodImplementation methodInfo;
                if (GetMethodInfo(method, args.Count - 1, out methodInfo))
                {
                    if (!urlModule.CheckThreatLevel(m_SessionID, method, ((CanBeReflected)Attribute.GetCustomAttribute(methodInfo.Method, typeof(CanBeReflected))).ThreatLevel))
                        return new byte[0];

                    ParameterInfo[] paramInfo = methodInfo.Method.GetParameters();
                    object[] parameters = new object[paramInfo.Length];
                    int paramNum = 0;
                    foreach (ParameterInfo param in paramInfo)
                        parameters[paramNum++] = Util.OSDToObject(args[param.Name], param.ParameterType);
                    
                    object o = methodInfo.Method.Invoke(methodInfo.Reference, parameters);
                    OSDMap response = new OSDMap();
                    if (o == null)//void method
                        response["Value"] = true;
                    else
                        response["Value"] = Util.MakeOSD(o, methodInfo.Method.ReturnType);
                    response["Success"] = true;
                    return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(response));
                }
            }

            return new byte[0];
        }

        private bool GetMethodInfo(string method, int parameters, out MethodImplementation methodInfo)
        {
            List<MethodImplementation> methods = new List<MethodImplementation>();
            if (m_methods.TryGetValue(method, out methods))
            {
                if (methods.Count == 1)
                {
                    methodInfo = methods[0];
                    return true;
                }
                foreach (MethodImplementation m in methods)
                {
                    if (m.Method.GetParameters().Length == parameters)
                    {
                        methodInfo = m;
                        return true;
                    }
                }
            }
            methodInfo = null;
            return false;
        }
    }
}