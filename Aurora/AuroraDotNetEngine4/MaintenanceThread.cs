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

* Copyright (c) Contributors, http://opensimulator.org/
* See CONTRIBUTORS.TXT for a full list of copyright holders.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
* * Redistributions of source code must retain the above copyright
* notice, this list of conditions and the following disclaimer.
* * Redistributions in binary form must reproduce the above copyright
* notice, this list of conditions and the following disclaimer in the
* documentation and/or other materials provided with the distribution.
* * Neither the name of the OpenSimulator Project nor the
* names of its contributors may be used to endorse or promote products
* derived from this software without specific prior written permission.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class MaintenanceThread
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ScriptEngine m_ScriptEngine;
        public AuroraThreadPool threadpool = null;
        public bool m_Started = false;

        public bool Started
        {
            get { return m_Started; }
            set
            {
                m_Started = true;

                //Start the queue because it can't start itself
                threadpool.QueueEvent(CmdHandlerQueue, 2);
            }
        }

        private EventManager EventManager = null;

        #endregion

        #region Constructor

        public MaintenanceThread(ScriptEngine Engine)
        {
            m_ScriptEngine = Engine;
            EventManager = Engine.EventManager;

            //There IS a reason we start this, even if RunInMain is enabled
            // If this isn't enabled, we run into issues with the CmdHandlerQueue,
            // as it always must be async, so we must run the pool anyway
            AuroraThreadPoolStartInfo info = new AuroraThreadPoolStartInfo();
            info.priority = ThreadPriority.Normal;
            info.Threads = 4;
            info.MaxSleepTime = Engine.Config.GetInt("SleepTime", 300);
            info.Threads = 1;
            threadpool = new AuroraThreadPool(info);

            AppDomain.CurrentDomain.AssemblyResolve += m_ScriptEngine.AssemblyResolver.OnAssemblyResolve;
        }

        #endregion

        #region Loops

        public bool CmdHandlerQueue()
        {
            if (m_ScriptEngine.Worlds.Count == 0)
                return false;
            IMonitorModule module = m_ScriptEngine.Worlds[0].RequestModuleInterface<IMonitorModule>();
            int StartTime = Util.EnvironmentTickCount();

            if (!Started) //Break early
                return true;

            if (m_ScriptEngine.ConsoleDisabled || m_ScriptEngine.Disabled)
                return true;

            //Check timers, etc
            try
            {
                m_ScriptEngine.DoOneScriptPluginPass();
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("[{0}]: Error in CmdHandlerPass, {1}", m_ScriptEngine.ScriptEngineName, ex);
            }

            if (module != null)
            {
                foreach (Scene scene in m_ScriptEngine.Worlds)
                {
                    ITimeMonitor scriptMonitor = (ITimeMonitor)module.GetMonitor(scene.RegionInfo.RegionID.ToString(), "Script Frame Time");
                    scriptMonitor.AddTime(Util.EnvironmentTickCountSubtract(StartTime));
                }
            }

            threadpool.QueueEvent(CmdHandlerQueue, 2);
            return false;
        }
        #endregion

        #region Add

        public async Task AddScriptChange(LUStruct[] items)
        {
            await TaskEx.Run(() =>
                {
                    List<LUStruct> NeedsFired = new List<LUStruct>();
                    foreach (LUStruct item in items)
                    {
                        if (item.Action == LUType.Unload)
                        {
                            item.ID.CloseAndDispose(false);
                        }
                        else if (item.Action == LUType.Load)
                        {
                            try
                            {
                                item.ID.Start(false);
                                NeedsFired.Add(item);
                            }
                            catch (Exception ex) { m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: LEAKED COMPILE ERROR: " + ex); }
                        }
                        else if (item.Action == LUType.Reupload)
                        {
                            try
                            {
                                item.ID.Start(true);
                                NeedsFired.Add(item);
                            }
                            catch (Exception ex) { m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: LEAKED COMPILE ERROR: " + ex); }
                        }
                    }
                    foreach (LUStruct item in NeedsFired)
                    {
                        item.ID.FireEvents();
                    }
                });
        }

        #endregion

        #region Remove

        public async void RemoveState(ScriptData ID)
        {
            await m_ScriptEngine.StateSave.DeleteFrom (ID);
        }

        #endregion

        #region Scripts events scheduler control

        public void AddEventSchQueue(ScriptData ID, string FunctionName, DetectParams[] qParams, int VersionID, params object[] param)
        {
            QueueItemStruct QIS = new QueueItemStruct();
            QIS.ID = ID;
            QIS.functionName = FunctionName;
            QIS.llDetectParams = qParams;
            QIS.param = param;
            QIS.VersionID = VersionID;
            QIS.State = ID.State;

            AddEventSchQIS(QIS);
        }

        public void AddEventSchQIS(QueueItemStruct QIS)
        {
            ScriptData ID;

            ID = QIS.ID;
            if (ID == null)
                return;

            if (!QIS.ID.SetEventParams(QIS.functionName, QIS.llDetectParams)) // check events delay rules
                return;

            EventSchProcessQIS(ref QIS);
        }

        public bool EventSchProcessQIS(ref QueueItemStruct QIS)
        {
            try
            {
                if(QIS.ID.Script == null)
                    return false;
                QIS.ID.Script.ExecuteEvent(QIS.State,
                            QIS.functionName,
                            QIS.param);
//                if (QIS.ID.VersionID != QIS.VersionID)
//                    return false;

                /*if (ex != null)
                {
                    //Check exceptions, some are ours to deal with, and others are to be logged
                    if (ex is SelfDeleteException)
                    {
                        if (QIS.ID.Part != null && QIS.ID.Part.ParentEntity != null)
                        {
                            IBackupModule backup = QIS.ID.Part.ParentEntity.Scene.RequestModuleInterface<IBackupModule> ();
                            if (backup != null)
                                backup.DeleteSceneObjects(
                                    new ISceneEntity[1] { QIS.ID.Part.ParentEntity }, true);
                        }
                    }
                    else if (ex is ScriptDeleteException)
                    {
                        if (QIS.ID.Part != null && QIS.ID.Part.ParentEntity != null)
                            QIS.ID.Part.Inventory.RemoveInventoryItem(QIS.ID.ItemID);
                    }
                    //Log it for the user
                    else if (!(ex is EventAbortException) &&
                        !(ex is MinEventDelayException))
                        QIS.ID.DisplayUserNotification(ex.ToString(), "", false, true);
                    return false;
                }*/
            }
            catch (Exception ex)
            {
                //Error, tell the user
                QIS.ID.DisplayUserNotification(ex.ToString(), "executing", false, true);
            }
            //Tell the event manager about it so that the events will be removed from the queue
            EventManager.EventComplete(QIS);
            return false;
        }

        #endregion
    }
}
