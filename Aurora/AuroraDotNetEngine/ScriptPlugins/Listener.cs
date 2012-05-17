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

using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.Plugins
{
    public class ListenerPlugin : IScriptPlugin
    {
        // private static readonly ILog MainConsole.Instance = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly List<IWorldComm> m_modules = new List<IWorldComm>();
        public ScriptEngine m_ScriptEngine;

        #region IScriptPlugin Members

        public void Initialize(ScriptEngine engine)
        {
            m_ScriptEngine = engine;
        }

        public void AddRegion(IScene scene)
        {
            m_modules.Add(scene.RequestModuleInterface<IWorldComm>());
        }

        public bool Check()
        {
            bool needToContinue = false;
            foreach (IWorldComm comms in m_modules)
            {
                if (!needToContinue)
                    needToContinue = comms.HasListeners();
                if (comms.HasMessages())
                {
                    while (comms.HasMessages())
                    {
                        IWorldCommListenerInfo lInfo = comms.GetNextMessage();

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
            return needToContinue;
        }

        public OSD GetSerializationData(UUID itemID, UUID primID)
        {
#if (!ISWIN)
            foreach (IWorldComm comms in m_modules)
            {
                OSD r = comms.GetSerializationData(itemID, primID);
                if (r != null)
                {
                    return r;
                }
            }
#else
            foreach (OSD r in m_modules.Select(comms => comms.GetSerializationData(itemID, primID)).Where(r => r != null))
            {
                return r;
            }
#endif
            return new OSD();
        }

        public void CreateFromData(UUID itemID, UUID hostID,
                                   OSD data)
        {
            foreach (IWorldComm comms in m_modules)
            {
                comms.CreateFromData(itemID, hostID, data);
            }

            //Make sure that the cmd handler thread is running
            m_ScriptEngine.MaintenanceThread.PokeThreads(itemID);
        }

        public string Name
        {
            get { return "Listener"; }
        }

        public void RemoveScript(UUID primID, UUID itemID)
        {
            foreach (IWorldComm comms in m_modules)
            {
                comms.DeleteListener(itemID);
            }
        }

        #endregion

        public void Dispose()
        {
        }
    }
}