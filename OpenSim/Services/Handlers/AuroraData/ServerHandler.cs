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
        protected static Dictionary<string, MethodImplementation> m_methods = new Dictionary<string, MethodImplementation>();

        public ServerHandler(string url, string SessionID, IRegistryCore registry) :
            base("POST", url)
        {
            m_SessionID = SessionID;
            m_registry = registry;
            foreach(IAuroraDataPlugin plugin in Aurora.DataManager.DataManager.GetPlugins())
            {
                foreach (MethodInfo method in plugin.GetType().GetMethods())
                {
                    if (!m_methods.ContainsKey(method.Name))
                        if (Attribute.GetCustomAttribute(method, typeof(CanBeReflected)) != null)
                            m_methods.Add(method.Name, new MethodImplementation() { Method = method, Reference = plugin });
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
                if (m_methods.TryGetValue(method, out methodInfo))
                {
                    if (!urlModule.CheckThreatLevel(m_SessionID, method, ((CanBeReflected)Attribute.GetCustomAttribute(methodInfo.Method, typeof(CanBeReflected))).ThreatLevel))
                        return new byte[0];

                    ParameterInfo[] paramInfo = methodInfo.Method.GetParameters();
                    object[] parameters = new object[paramInfo.Length];
                    int paramNum = 0;
                    foreach (ParameterInfo param in paramInfo)
                        if (Util.IsInstanceOfGenericType(typeof(List<>), param.ParameterType))
                            parameters[paramNum++] = MakeListFromArray((OSDArray)args[param.Name], param);
                        else if (Util.IsInstanceOfGenericType(typeof(Dictionary<,>), param.ParameterType))
                            parameters[paramNum++] = MakeDictionaryFromArray((OSDMap)args[param.Name], param);
                        else
                            parameters[paramNum++] = Util.OSDToObject(args[param.Name], param.ParameterType);
                    
                    object o = methodInfo.Method.Invoke(methodInfo.Reference, parameters);
                    OSDMap response = new OSDMap();
                    if (o == null)//void method
                        response["Value"] = true;
                    else
                        response["Value"] = MakeOSD(o, methodInfo);
                    response["Success"] = true;
                    return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(response));
                }
            }

            return new byte[0];
        }

        private object ToOSD(OSD o, Type type)
        {
            if (type == typeof(UUID))
                return o.AsUUID();
            if (type == typeof(string))
                return o.AsString();
            if (type == typeof(int))
                return o.AsInteger();
            if (type == typeof(byte[]))
                return o.AsBinary();
            if (type == typeof(bool))
                return o.AsBoolean();
            if (type == typeof(Color4))
                return o.AsColor4();
            if (type == typeof(DateTime))
                return o.AsDate();
            if (type == typeof(long))
                return o.AsLong();
            if (type == typeof(Quaternion))
                return o.AsQuaternion();
            if (type == typeof(float))
                return (float)o.AsReal();
            if (type == typeof(double))
                return o.AsReal();
            if (type == typeof(uint))
                return o.AsUInteger();
            if (type == typeof(ulong))
                return o.AsULong();
            if (type == typeof(Uri))
                return o.AsUri();
            if (type == typeof(Vector2))
                return o.AsVector2();
            if (type == typeof(Vector3))
                return o.AsVector3();
            if (type == typeof(Vector3d))
                return o.AsVector3d();
            if (type == typeof(Vector4))
                return o.AsVector4();
            MainConsole.Instance.Error("COULD NOT FIND OSD TYPE FOR " + type.ToString());
            return null;
        }

        private OSD MakeOSD(object o, MethodImplementation methodInfo)
        {
            if (o is OSD)
                return (OSD)o;
            OSD oo;
            if ((oo =OSD.FromObject(o)).Type != OSDType.Unknown)
                return (OSD)oo;
            if (o is IDataTransferable)
                return ((IDataTransferable)o).ToOSD();
            if (Util.IsInstanceOfGenericType(typeof(List<>), methodInfo.Method.ReturnParameter.ParameterType))
            {
                OSDArray array = new OSDArray();
                var list = Util.MakeList(Activator.CreateInstance(methodInfo.Method.ReturnParameter.ParameterType.GetGenericArguments()[0]));
                System.Collections.IList collection = (System.Collections.IList)o;
                foreach (object item in collection)
                {
                    array.Add(MakeOSD(item, methodInfo));
                }
                return array;
            }
            else if (Util.IsInstanceOfGenericType(typeof(Dictionary<,>), methodInfo.Method.ReturnParameter.ParameterType))
            {
                OSDMap array = new OSDMap();
                var list = Util.MakeDictionary(Activator.CreateInstance(methodInfo.Method.ReturnParameter.ParameterType.GetGenericArguments()[0]),
                    Activator.CreateInstance(methodInfo.Method.ReturnParameter.ParameterType.GetGenericArguments()[1]));
                System.Collections.IDictionary collection = (System.Collections.IDictionary)o;
                foreach (KeyValuePair<object, object> item in collection)
                {
                    array.Add(item.Key.ToString(), MakeOSD(item, methodInfo));
                }
                return array;
            }
            return null;
        }

        private object MakeListFromArray(OSDArray array, ParameterInfo param)
        {
            Type t = param.ParameterType.GetGenericArguments()[0];
            System.Collections.IList list = (System.Collections.IList)Util.OSDToObject(array, t);
            if (t.BaseType == typeof(IDataTransferable))
            {
                IDataTransferable defaultInstance = (IDataTransferable)Activator.CreateInstance(t);
                var newList = Util.MakeList(defaultInstance);
                foreach (object o in list)
                {
                    defaultInstance.FromOSD((OSDMap)o);
                    newList.Add(defaultInstance);
                    defaultInstance = (IDataTransferable)Activator.CreateInstance(t);
                }
                return newList;
            }

            return list;
        }

        private object MakeDictionaryFromArray(OSDMap array, ParameterInfo param)
        {
            var list = (System.Collections.IDictionary)Util.OSDToObject(array, param.ParameterType.GetGenericArguments()[1]);
            Type t = param.ParameterType.GetGenericArguments()[0];
            if (t.BaseType == typeof(IDataTransferable))
            {
                IDataTransferable defaultInstance = (IDataTransferable)Activator.CreateInstance(t);
                var newList = Util.MakeDictionary("", defaultInstance);
                foreach (KeyValuePair<object, object> o in list)
                {
                    defaultInstance.FromOSD((OSDMap)o.Value);
                    newList.Add(o.Key.ToString(), defaultInstance);
                    defaultInstance = (IDataTransferable)Activator.CreateInstance(t);
                }
                return newList;
            }
            return list;
        }
    }
}