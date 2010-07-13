/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using OpenSim.Region.CoreModules.Scripting.XMLRPC;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.Plugins
{
    public class XmlRequest
    {
        public ScriptEngine m_ScriptEngine;
        private IXMLRPC xmlrpc = null;

        public XmlRequest(ScriptEngine ScriptEngine, Scene scene)
        {
            xmlrpc = scene.RequestModuleInterface<IXMLRPC>();
            m_ScriptEngine = ScriptEngine;
        }

        public void CheckXMLRPCRequests()
        {
            if (xmlrpc == null)
                return;
            RPCRequestInfo rInfo = (RPCRequestInfo)xmlrpc.GetNextCompletedRequest();
            SendRemoteDataRequest srdInfo = (SendRemoteDataRequest)xmlrpc.GetNextCompletedSRDRequest();

            if (rInfo == null && srdInfo == null)
                return;
            
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

                rInfo = (RPCRequestInfo)xmlrpc.GetNextCompletedRequest();
            }

            while (srdInfo != null)
            {
                xmlrpc.RemoveCompletedSRDRequest(srdInfo.GetReqID());

                //Deliver data to prim's remote_data handler
                object[] resobj = new object[]
                    {
                        new LSL_Types.LSLInteger(3),
                        new LSL_Types.LSLString(srdInfo.Channel.ToString()),
                        new LSL_Types.LSLString(srdInfo.GetReqID().ToString()),
                        new LSL_Types.LSLString(String.Empty),
                        new LSL_Types.LSLInteger(srdInfo.Idata),
                        new LSL_Types.LSLString(srdInfo.Sdata)
                    };

                m_ScriptEngine.PostScriptEvent(
                            srdInfo.ItemID, srdInfo.PrimID, new EventParams(
                                "remote_data", resobj,
                                new DetectParams[0]), EventPriority.Suspended);

                srdInfo = (SendRemoteDataRequest)xmlrpc.GetNextCompletedSRDRequest();
            }
        }
    }
}
