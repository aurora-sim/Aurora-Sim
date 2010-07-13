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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using Amib.Threading;
using Aurora.Framework;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class AuroraThreadPoolStartInfo
    {
        public ThreadPriority priority;
        public int Threads = 0;
        public int InitialSleepTime = 1;
        public int MaxSleepTime = 300;
    }

    public class AuroraThreadPool
    {
        AuroraThreadPoolStartInfo m_info = null;
        List<Thread> Threads = new List<Thread>();
        Queue queue = new Queue();
        Queue queueTwo = new Queue();
        Queue queueThree = new Queue();

        public AuroraThreadPool(AuroraThreadPoolStartInfo info)
        {
            m_info = info;
            for (int i = 0; i < m_info.Threads; i++)
            {
                Thread thread = new Thread(ThreadStart);
                thread.Name = "Aurora Thread Pool Thread #" + i;
                thread.Start(i);
                Threads.Add(thread);
            }
        }

        private void ThreadStart(object ScriptNumber)
        {
            int OurSleepTime = m_info.InitialSleepTime;
            while (true)
            {
                QueueItem item = null;
                Thread.Sleep(OurSleepTime);
                if (queue.Count == 0)
                {
                    OurSleepTime += 2;
                    if (OurSleepTime > m_info.MaxSleepTime) //Make sure we don't go waay over on how long we sleep
                        OurSleepTime = m_info.MaxSleepTime;
                    continue;
                }
                else
                {
                    item = queue.Dequeue() as QueueItem;
                    if (item == null)
                        continue;
                    item.Invoke();
                }
                OurSleepTime = m_info.InitialSleepTime; //Reset sleep timer then
            }
        }

        public delegate void QueueItem();

        public void QueueEvent(QueueItem delegat, int Priority)
        {
            queue.Enqueue(delegat);
        }
    }
    /// <summary>
    /// This class does maintenance on script engine.
    /// </summary>
    public class MaintenanceThread
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IScriptDataConnector ScriptFrontend;
        private ScriptEngine m_ScriptEngine;
        private bool InitialStart = true;
        private bool ScriptsLoaded = false;
        private AuroraThreadPool threadpool = null;
        public bool StateSaveIsRunning = false;
        public bool ScriptChangeIsRunning = false;
        public bool EventProcessorIsRunning = false;

        public MaintenanceThread(ScriptEngine Engine)
        {
            AuroraThreadPoolStartInfo info = new AuroraThreadPoolStartInfo();
            info.priority = ThreadPriority.Lowest;
            info.Threads = 1;
            threadpool = new AuroraThreadPool(info);
            m_ScriptEngine = Engine;
            ScriptFrontend = Aurora.DataManager.DataManager.RequestPlugin<IScriptDataConnector>("IScriptDataConnector");
            CmdHandlerQueue();
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.OnAssemblyResolve;
        }

        public void OnScriptsLoadingComplete()
        {
            ScriptsLoaded = true;
        }

        public void RemoveState(ScriptData ID)
        {
            ScriptFrontend.DeleteStateSave(ID.ItemID);
        }

        public void NewStateSaveQueue()
        {
            StateSaveIsRunning = true;
            if (ScriptEngine.StateQueue.Count != 0)
            {
                StateQueueItem item = ScriptEngine.StateQueue.Dequeue() as StateQueueItem;
                if (item == null || item.ID == null)
                    return;
                if (item.Create)
                    item.ID.SerializeDatabase();
                else
                    RemoveState(item.ID);
                threadpool.QueueEvent(NewStateSaveQueue, 3);
                return;
            }
            StateSaveIsRunning = false;
            return;
        }

        public void NewScriptChangeQueue()
        {
            ScriptChangeIsRunning = true;
            if (InitialStart)
            {
                InitialStart = false;
                foreach (OpenSim.Region.Framework.Scenes.Scene scene in m_ScriptEngine.Worlds)
                {
                    // No scripts on region, so won't get triggered later
                    // by the queue becoming empty so we trigger it here
                    scene.EventManager.TriggerEmptyScriptCompileQueue(0, String.Empty);
                }
            }

            object oitems;
            if (m_ScriptEngine.LUQueue.GetNext(out oitems))
            {
                LUStruct[] items = oitems as LUStruct[];
                List<LUStruct> NeedsFired = new List<LUStruct>();
                foreach (LUStruct item in items)
                {
                    if (item.Action == LUType.Unload)
                    {
                        try
                        {
                            item.ID.CloseAndDispose(false);
                        }
                        catch (Exception ex) { m_log.Warn(ex); }
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
                threadpool.QueueEvent(NewScriptChangeQueue, 2); //Requeue us
            }
            else
            {
                ScriptChangeIsRunning = false;
            }

            if (ScriptsLoaded)
            {
                foreach (OpenSim.Region.Framework.Scenes.Scene scene in m_ScriptEngine.Worlds)
                {
                    scene.EventManager.TriggerEmptyScriptCompileQueue(m_ScriptEngine.ScriptFailCount,
                                                                    m_ScriptEngine.ScriptErrorMessages);
                }
            }
            m_ScriptEngine.ScriptFailCount = 0;
        }

        public void NewEventQueue()
        {
            try
            {
                EventProcessorIsRunning = true;
                object QIS = null;
                if (ScriptEngine.EventPerformanceTestQueue.GetNext(out QIS))
                {
                    if (QIS != null)
                    {
                        ProcessQIS(QIS as QueueItemStruct);
                        threadpool.QueueEvent(NewEventQueue, 1);
                    }
                }
                else
                {
                    EventProcessorIsRunning = false;
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("[{0}]: Handled exception stage 2 in the Event Queue: " + ex.Message, m_ScriptEngine.ScriptEngineName);
            }
        }

        public void CmdHandlerQueue()
        {
            //Check timers, etc
            m_ScriptEngine.DoOneCmdHandlerPass();
            threadpool.QueueEvent(NewEventQueue, 2);
        }

        public void ProcessQIS(QueueItemStruct QIS)
        {
            //Suspended scripts get readded
            if (QIS.ID.Suspended || QIS.ID.Script == null || QIS.ID.Loading)
            {
                //ScriptEngine.EventPerformanceTestQueue.Add(QIS, EventPriority.Suspended);
                return;
            }

            int Version = 0;
            if (ScriptEngine.NeedsRemoved.TryGetValue(QIS.ID.ItemID, out Version))
            {
                if (Version >= QIS.VersionID)
                    return;
            }

            //Disabled or not running scripts dont get events saved.
            if (QIS.ID.Disabled || !QIS.ID.Running)
                return;
            try
            {
                Guid Running;
                Exception ex;
                QIS.ID.SetEventParams(QIS.llDetectParams);
                Running = QIS.ID.Script.ExecuteEvent(QIS.ID.State,
                            QIS.functionName,
                            QIS.param, QIS.CurrentlyAt, out ex);
                if (ex != null)
                    throw ex;
                //Finished with nothing left.
                if (Running == Guid.Empty)
                {
                    if (QIS.functionName == "timer")
                        QIS.ID.TimerQueued = false;
                    if (QIS.functionName == "control")
                    {
                        if (QIS.ID.ControlEventsInQueue > 0)
                            QIS.ID.ControlEventsInQueue--;
                    }
                    if (QIS.functionName == "collision")
                        QIS.ID.CollisionInQueue = false;
                    if (QIS.functionName == "touch")
                        QIS.ID.TouchInQueue = false;
                    if (QIS.functionName == "land_collision")
                        QIS.ID.LandCollisionInQueue = false;
                    if (QIS.functionName == "changed")
                    {
                        Changed changed = (Changed)(new LSL_Types.LSLInteger(QIS.param[0].ToString()).value);
                        lock (QIS.ID.ChangedInQueue)
                        {
                            if (QIS.ID.ChangedInQueue.Contains(changed))
                                QIS.ID.ChangedInQueue.Remove(changed);
                        }
                    }
                    return;
                }
                else
                {
                    //Did not finish so requeue it
                    QIS.CurrentlyAt = Running;
                    ScriptEngine.EventPerformanceTestQueue.Add(QIS, EventPriority.Continued);
                }
            }
            catch (SelfDeleteException) // Must delete SOG
            {
                if (QIS.ID.part != null && QIS.ID.part.ParentGroup != null)
                    m_ScriptEngine.findPrimsScene(QIS.ID.part.UUID).DeleteSceneObject(
                        QIS.ID.part.ParentGroup, false, true);
            }
            catch (ScriptDeleteException) // Must delete item
            {
                if (QIS.ID.part != null && QIS.ID.part.ParentGroup != null)
                    QIS.ID.part.Inventory.RemoveInventoryItem(QIS.ID.ItemID);
            }
            catch (EventAbortException) // Changing state, all is ok
            {
            }
            catch (Exception ex)
            {
                QIS.ID.DisplayUserNotification(ex.Message, "executing", false, true);
            }
        }

        internal void StartThread(string p)
        {
            if (p == "State")
            {
                threadpool.QueueEvent(NewStateSaveQueue, 3);
            }
            else if (p == "Change")
            {
                threadpool.QueueEvent(NewScriptChangeQueue, 2);
            }
            else if (p == "Event")
            {
                threadpool.QueueEvent(NewEventQueue, 1);
            }
        }
    }
}
