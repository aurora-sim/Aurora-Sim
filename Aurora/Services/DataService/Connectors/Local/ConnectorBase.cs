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

namespace Aurora.Services.DataService
{
    public class ConnectorBase
    {
        protected IRegistryCore m_registry;
        protected IConfigurationService m_configService
        {
            get { return m_registry.RequestModuleInterface<IConfigurationService>(); }
        }
        protected bool m_doRemoteCalls = false;
        protected string m_name;

        public void Init(IRegistryCore registry, string name)
        {
            m_registry = registry;
            m_name = name;
            IConfigSource source = registry.RequestModuleInterface<ISimulationBase>().ConfigSource;
            IConfig config;
            if ((config = source.Configs["AuroraConnectors"]) != null)
                m_doRemoteCalls = config.GetBoolean("DoRemoteCalls", false);
        }

        public object DoRemote(params object[] o)
        {
            return DoRemoteForUser(UUID.Zero, o);
        }

        public object DoRemoteForUser(UUID userID, params object[] o)
        {
            if (!m_doRemoteCalls)
                return null;
            StackTrace stackTrace = new StackTrace();
            int upStack = 1;
            if (userID == UUID.Zero)
                upStack = 2;
            MethodInfo method = (MethodInfo)stackTrace.GetFrame(upStack).GetMethod();
            string methodName = method.Name;
            OSDMap map = new OSDMap();
            map["Method"] = methodName;
            int i = 0;
            foreach(ParameterInfo info in method.GetParameters())
            {
                map.Add(info.Name, Util.MakeOSD(o[i], o[i].GetType()));
                i++;
            }
            List<string> m_ServerURIs =
                    m_configService.FindValueOf(userID.ToString(), "ServerURI", false);
            OSDMap response = null;
            foreach (string uri in m_ServerURIs)
            {
                if (GetOSDMap(uri, map, out response))
                    break;
            }
            if (response == null || !response)
                return null;
            object inst =  Activator.CreateInstance(method.ReturnType);
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
                                                          OSDParser.SerializeJsonString(map));
            if (resp == "")
                return false;
            response = (OSDMap)OSDParser.DeserializeJson(resp);
            return response["Success"];
        }
    }
}