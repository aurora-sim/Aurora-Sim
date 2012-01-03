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
        protected IConfigurationService m_configService;
        protected string m_name;

        public void Init(IRegistryCore registry, string name)
        {
            m_registry = registry;
            m_configService = m_registry.RequestModuleInterface<IConfigurationService>();
            m_name = name;
        }

        public object DoRemote(params OSD[] o)
        {
            return DoRemoteForUser(UUID.Zero, o);
        }

        public object DoRemoteForUser(UUID userID, params OSD[] o)
        {
            StackTrace stackTrace = new StackTrace();
            MethodInfo method = (MethodInfo)stackTrace.GetFrame(1).GetMethod();
            string methodName = method.Name;
            OSDMap map = new OSDMap();
            map["Method"] = methodName;
            int i = 0;
            foreach(ParameterInfo info in method.GetParameters())
            {
                map.Add(info.Name, o[i]);
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
            if (!response)
                return null;
            object inst =  Activator.CreateInstance(method.ReturnType);
            if (inst is IDataTransferable)
            {
                IDataTransferable instance = (IDataTransferable)inst;
                instance.FromOSD(response);
                return instance;
            }
            else
                return response["Value"];
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