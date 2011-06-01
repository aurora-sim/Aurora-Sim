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
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.CoreModules.Scripting.HttpRequest;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.Plugins
{
    public class HttpRequestPlugin : IScriptPlugin
    {
        public ScriptEngine m_ScriptEngine;
        private List<IHttpRequestModule> m_modules = new List<IHttpRequestModule> ();

        public void Initialize(ScriptEngine engine)
        {
            m_ScriptEngine = engine;
        }

        public void AddRegion (Scene scene)
        {
            m_modules.Add(scene.RequestModuleInterface<IHttpRequestModule> ());
        }

        public void Check()
        {
            foreach (IHttpRequestModule iHttpReq in m_modules)
            {
                IServiceRequest httpInfo = null;

                if (iHttpReq != null)
                    httpInfo = iHttpReq.GetNextCompletedRequest ();

                if (httpInfo == null)
                    return;

                while (httpInfo != null)
                {
                    HttpRequestClass info = (HttpRequestClass)httpInfo;
                    //m_log.Debug("[AsyncLSL]:" + httpInfo.response_body + httpInfo.status);

                    // Deliver data to prim's remote_data handler

                    iHttpReq.RemoveCompletedRequest (info);

                    object[] resobj = new object[]
                {
                    new LSL_Types.LSLString(info.ReqID.ToString()),
                    new LSL_Types.LSLInteger(info.Status),
                    new LSL_Types.list(info.Metadata),
                    new LSL_Types.LSLString(info.ResponseBody)
                };

                    m_ScriptEngine.AddToObjectQueue (info.PrimID, "http_response", new DetectParams[0], -1, resobj);
                    httpInfo = iHttpReq.GetNextCompletedRequest ();
                }
            }
        }

        public string Name
        {
            get { return "HttpRequest"; }
        }

        public void Dispose()
        {
        }

        public OSD GetSerializationData (UUID itemID, UUID primID)
        {
            return "";
        }

        public void CreateFromData (UUID itemID, UUID objectID, OSD data)
        {
        }

        public void RemoveScript(UUID primID, UUID itemID)
        {
            foreach (IHttpRequestModule iHttpReq in m_modules)
            {
                iHttpReq.StopHttpRequest (primID, itemID);
            }
        }
    }
}
