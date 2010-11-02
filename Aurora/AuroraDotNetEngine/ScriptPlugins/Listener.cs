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
using log4net;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.CoreModules.Scripting.WorldComm;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.Plugins
{
    public class ListenerPlugin : INonSharedScriptPlugin
    {
        // private static readonly ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ScriptEngine m_ScriptEngine;
        private IWorldComm comms;

        public void Initialize(ScriptEngine ScriptEngine, Scene scene)
        {
            m_ScriptEngine = ScriptEngine;
            comms = scene.RequestModuleInterface<IWorldComm>();
        }

        public void Check()
        {
            if (comms.HasMessages())
            {
                while (comms.HasMessages())
                {
                    ListenerInfo lInfo = (ListenerInfo)comms.GetNextMessage();

                    //Deliver data to prim's listen handler
                    object[] resobj = new object[]
                    {
                        new LSL_Types.LSLInteger(lInfo.GetChannel()),
                        new LSL_Types.LSLString(lInfo.GetName()),
                        new LSL_Types.LSLString(lInfo.GetID().ToString()),
                        new LSL_Types.LSLString(lInfo.GetMessage())
                    };

                    m_ScriptEngine.PostScriptEvent(
                                lInfo.GetItemID(), lInfo.GetHostID(), new EventParams(
                                "listen", resobj,
                                new DetectParams[0]), EventPriority.Suspended);
                }
            }
        }

        public Object[] GetSerializationData(UUID itemID, UUID primID)
        {
            object[] listeners = comms.GetSerializationData(itemID);
            
            List<Object> data = new List<object>();
            data.Add(Name);
            data.Add(listeners.Length);
            data.AddRange(listeners);
            
            return data.ToArray();
        }

        public void CreateFromData(UUID itemID, UUID hostID,
                Object[] data)
        {
            comms.CreateFromData(itemID, hostID, data);
        }

        public string Name
        {
            get { return "Listener"; }
        }

        public void Dispose()
        {
        }

        public void RemoveScript(UUID primID, UUID itemID)
        {
            comms.DeleteListener(itemID);
        }
    }
}
