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
using Aurora.Framework;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.Plugins
{
    public class XmlRequestPlugin : IScriptPlugin
    {
        private readonly List<IXMLRPC> m_modules = new List<IXMLRPC>();
        public ScriptEngine m_ScriptEngine;

        #region IScriptPlugin Members

        public bool RemoveOnStateChange
        {
            get { return false; }
        }

        public void Initialize(ScriptEngine ScriptEngine)
        {
            m_ScriptEngine = ScriptEngine;
        }

        public void AddRegion(IScene scene)
        {
            m_modules.Add(scene.RequestModuleInterface<IXMLRPC>());
        }

        public bool Check()
        {
            bool needToContinue = false;
            foreach (IXMLRPC xmlrpc in m_modules)
            {
                if (xmlrpc == null)
                    continue;
                IXmlRpcRequestInfo rInfo = xmlrpc.GetNextCompletedRequest();
                ISendRemoteDataRequest srdInfo = (ISendRemoteDataRequest) xmlrpc.GetNextCompletedSRDRequest();

                if (!needToContinue)
                    needToContinue = xmlrpc.hasRequests();

                if (rInfo == null && srdInfo == null)
                    continue;

                while (rInfo != null)
                {
                    xmlrpc.RemoveCompletedRequest(rInfo.GetMessageID());

                    //Deliver data to prim's remote_data handler
                    object[] resobj = new object[]
                                          {
                                              new LSL_Types.LSLInteger(2),
                                              new LSL_Types.LSLString(
                                                  rInfo.GetChannelKey().ToString()),
                                              new LSL_Types.LSLString(
                                                  rInfo.GetMessageID().ToString()),
                                              new LSL_Types.LSLString(String.Empty),
                                              new LSL_Types.LSLInteger(rInfo.GetIntValue()),
                                              new LSL_Types.LSLString(rInfo.GetStrVal())
                                          };

                    m_ScriptEngine.PostScriptEvent(
                        rInfo.GetItemID(), rInfo.GetPrimID(), new EventParams(
                                                                  "remote_data", resobj,
                                                                  new DetectParams[0]), EventPriority.Suspended);

                    rInfo = xmlrpc.GetNextCompletedRequest();
                }

                while (srdInfo != null)
                {
                    xmlrpc.RemoveCompletedSRDRequest(srdInfo.GetReqID());

                    //Deliver data to prim's remote_data handler
                    object[] resobj = new object[]
                                          {
                                              new LSL_Types.LSLInteger(3),
                                              new LSL_Types.LSLString(srdInfo.Channel),
                                              new LSL_Types.LSLString(srdInfo.GetReqID().ToString()),
                                              new LSL_Types.LSLString(String.Empty),
                                              new LSL_Types.LSLInteger(srdInfo.Idata),
                                              new LSL_Types.LSLString(srdInfo.Sdata)
                                          };

                    m_ScriptEngine.PostScriptEvent(
                        srdInfo.ItemID, srdInfo.PrimID, new EventParams(
                                                            "remote_data", resobj,
                                                            new DetectParams[0]), EventPriority.Suspended);

                    srdInfo = (ISendRemoteDataRequest) xmlrpc.GetNextCompletedSRDRequest();
                }
            }
            return needToContinue;
        }

        public string Name
        {
            get { return "XmlRequest"; }
        }

        public OSD GetSerializationData(UUID itemID, UUID primID)
        {
            return "";
        }

        public void CreateFromData(UUID itemID, UUID objectID, OSD data)
        {
        }

        public void RemoveScript(UUID primID, UUID itemID)
        {
            foreach (IXMLRPC xmlrpc in m_modules)
            {
                xmlrpc.DeleteChannels(itemID);
                xmlrpc.CancelSRDRequests(itemID);
            }
        }

        #endregion

        public void Dispose()
        {
        }
    }
}