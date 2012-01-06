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
using System.Diagnostics;
using System.Data;
using System.Reflection;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class ConnectorRegistry
    {
        public static List<ConnectorBase> Connectors = new List<ConnectorBase>();
        public static void RegisterConnector(ConnectorBase con)
        {
            Connectors.Add(con);
        }
    }

    public class ConnectorBase
    {
        protected IRegistryCore m_registry;
        protected IConfigurationService m_configService
        {
            get { return m_registry.RequestModuleInterface<IConfigurationService>(); }
        }
        protected bool m_doRemoteCalls = false;
        protected string m_name;

        public string PluginName
        {
            get { return m_name; }
        }

        public bool Enabled
        {
            get;
            set;
        }

        public void Init(IRegistryCore registry, string name)
        {
            Enabled = true;
            m_registry = registry;
            m_name = name;
            IConfigSource source = registry.RequestModuleInterface<ISimulationBase>().ConfigSource;
            IConfig config;
            if ((config = source.Configs["AuroraConnectors"]) != null)
                m_doRemoteCalls = config.GetBoolean("DoRemoteCalls", false);
            ConnectorRegistry.RegisterConnector(this);
        }

        public object DoRemote(params object[] o)
        {
            return DoRemoteCall(false, "ServerURI", UUID.Zero, o);
        }

        public object DoRemoteForced(params object[] o)
        {
            return DoRemoteCall(true, "ServerURI", UUID.Zero, o);
        }

        public object DoRemoteForUser(UUID userID, params object[] o)
        {
            return DoRemoteCall(false, "ServerURI", UUID.Zero, o);
        }

        public object DoRemoteByURL(string url, params object[] o)
        {
            return DoRemoteCall(false, url, UUID.Zero, o);
        }

        public object DoRemoteCall(bool forced, string url, UUID userID, params object[] o)
        {
            if (!m_doRemoteCalls && !forced)
                return null;
            StackTrace stackTrace = new StackTrace();
            int upStack = 1;
            if (userID == UUID.Zero)
                upStack = 2;
            MethodInfo method = (MethodInfo)stackTrace.GetFrame(upStack).GetMethod();
            CanBeReflected reflection = (CanBeReflected)Attribute.GetCustomAttribute(method, typeof(CanBeReflected));
            string methodName = reflection != null && reflection.RenamedMethod != "" ? reflection.RenamedMethod : method.Name;
            OSDMap map = new OSDMap();
            map["Method"] = methodName;
            int i = 0;
            foreach(ParameterInfo info in method.GetParameters())
            {
                OSD osd = Util.MakeOSD(o[i], o[i].GetType());
                if(osd != null)
                    map.Add(info.Name, osd);
                i++;
            }
            List<string> m_ServerURIs =
                    m_configService.FindValueOf(userID.ToString(), url, false);
            OSDMap response = null;
            foreach (string uri in m_ServerURIs)
            {
                if (GetOSDMap(uri, map, out response))
                    break;
            }
            if (response == null || !response)
                return null;
            object inst = null;
            try
            {
                inst = Activator.CreateInstance(method.ReturnType);
            }
            catch
            {
                if (method.ReturnType == typeof(string))
                    inst = string.Empty;
            }
            if (inst is IDataTransferable)
            {
                IDataTransferable instance = (IDataTransferable)inst;
                instance.FromOSD((OSDMap)response["Value"]);
                return instance;
            }
            else
                return Util.OSDToObject(response["Value"], method.ReturnType);
        }

        public bool GetOSDMap(string url, OSDMap map, out OSDMap response)
        {
            response = null;
            string resp = SynchronousRestFormsRequester.MakeRequest("POST",
                                                          url,
                                                          OSDParser.SerializeJsonString(map, true));
            if (resp == "")
                return false;
            try
            {
                response = (OSDMap)OSDParser.DeserializeJson(resp);
            }
            catch
            {
                response = null;
                return false;
            }
            return response["Success"];
        }
    }
}