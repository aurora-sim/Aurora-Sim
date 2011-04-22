/*
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
        private bool FiredStartupEvent = false;
        public AuroraThreadPool threadpool = null;
        public AuroraThreadPool Scriptthreadpool = null;
        public bool ScriptChangeIsRunning = false;
        public bool EventProcessorIsRunning = false;
        public bool CmdHandlerQueueIsRunning = false;
        public bool RunInMainProcessingThread = false;
        public bool m_Started = false;

        public int MaxScriptThreads = 1;
        private class WorkersLockClk
        {
            public int nWorkers = 0;
        }
        private WorkersLockClk WorkersLock = new WorkersLockClk();

        public bool Started
        {
            get { return m_Started; }
            set
            {
                m_Started = true;

                WorkersLock.nWorkers = 0;

                threadpool.QueueEvent(ScriptChangeQueue, 2);
                //Start the queue because it can't start itself
                threadpool.QueueEvent(CmdHandlerQueue, 2);
            }
        }

        /// <summary>
        /// Queue that handles the loading and unloading of scripts
        /// </summary>
        private StartPerformanceQueue LUQueue = new StartPerformanceQueue();

        private EventManager EventManager = null;

        #endregion

        #region Constructor

        public MaintenanceThread(ScriptEngine Engine)
        {
            m_ScriptEngine = Engine;
            EventManager = Engine.EventManager;

            RunInMainProcessingThread = Engine.Config.GetBoolean("RunInMainProcessingThread", false);

            RunInMainProcessingThread = false; // temporary false until code is fix to work with true

            //There IS a reason we start this, even if RunInMain is enabled
            // If this isn't enabled, we run into issues with the CmdHandlerQueue,
            // as it always must be async, so we must run the pool anyway
            AuroraThreadPoolStartInfo info = new AuroraThreadPoolStartInfo();
            info.priority = ThreadPriority.Normal;
            info.Threads = 2;
            info.MaxSleepTime = Engine.Config.GetInt("SleepTime", 300);
            threadpool = new AuroraThreadPool(info);


            MaxScriptThreads = Engine.Config.GetInt("Threads", 100); // leave control threads out of user option
            AuroraThreadPoolStartInfo sinfo = new AuroraThreadPoolStartInfo();
            sinfo.priority = ThreadPriority.Normal;
            sinfo.Threads = MaxScriptThreads;
            sinfo.MaxSleepTime = Engine.Config.GetInt("SleepTime", 300);
            Scriptthreadpool = new AuroraThreadPool(sinfo);
            
            AppDomain.CurrentDomain.AssemblyResolve += m_ScriptEngine.AssemblyResolver.OnAssemblyResolve;
        }

        #endregion

        #region Loops

        /// <summary>
        /// This loop deals with starting and stoping scripts
        /// </summary>
        /// <returns></returns>
        public bool ScriptChangeQueue()
        {
            IMonitorModule module = m_ScriptEngine.Worlds[0].RequestModuleInterface<IMonitorModule>();
            int StartTime = Util.EnvironmentTickCount();

            if (!Started) //Break early
                return true;

            if (m_ScriptEngine.ConsoleDisabled || m_ScriptEngine.Disabled)
                return true;

            ScriptChangeIsRunning = true;

            object oitems;
            if (LUQueue.GetNext(out oitems))
            {
                LUStruct[] items = oitems as LUStruct[];
                List<LUStruct> NeedsFired = new List<LUStruct>();
                foreach (LUStruct item in items)
                {
                    if (item.Action == LUType.Unload)
                    {
                        //Close
                        item.ID.CloseAndDispose(true);
                    }
                    else if (item.Action == LUType.Load)
                    {
                        try
                        {
                            //Start
                            if(item.ID.Start(false))
                                NeedsFired.Add(item);
                        }
                        catch (Exception ex) { m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: LEAKED COMPILE ERROR: " + ex); }
                    }
                    else if (item.Action == LUType.Reupload)
                    {
                        try
                        {
                            //Start, but don't add to the queue's again
                            if(item.ID.Start(true))
                                NeedsFired.Add(item);
                        }
                        catch (Exception ex) { m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: LEAKED COMPILE ERROR: " + ex); }
                    }
                }
                foreach (LUStruct item in NeedsFired)
                {
                    //Fire the events afterward so that they all start at the same time
                    item.ID.FireEvents();
                }
                threadpool.QueueEvent(ScriptChangeQueue, 2); //Requeue us
                Thread.Sleep(5);
                return false;
            }

            if (!FiredStartupEvent)
            {
                //If we are empty, we are all done with script startup and can tell the region that we are all done
                if (LUQueue.Count() == 0)
                {
                    FiredStartupEvent = true;
                    foreach (OpenSim.Region.Framework.Scenes.Scene scene in m_ScriptEngine.Worlds)
                    {
                        scene.EventManager.TriggerEmptyScriptCompileQueue(m_ScriptEngine.ScriptFailCount,
                                                                        m_ScriptEngine.ScriptErrorMessages);

                        scene.EventManager.TriggerModuleFinishedStartup("ScriptEngine", new List<string>(){m_ScriptEngine.ScriptFailCount.ToString(),
                                                                    m_ScriptEngine.ScriptErrorMessages}); //Tell that we are done
                    }
                }
            }
            ScriptChangeIsRunning = false;
            Thread.Sleep(20);

            if (module != null)
            {
                foreach (Scene scene in m_ScriptEngine.Worlds)
                {
                    ITimeMonitor scriptMonitor = (ITimeMonitor)module.GetMonitor(scene.RegionInfo.RegionID.ToString(), "Script Frame Time");
                    scriptMonitor.AddTime(Util.EnvironmentTickCountSubtract(StartTime));
                }
            }

            return false;
        }

        public bool CmdHandlerQueue()
        {
            if (m_ScriptEngine.Worlds.Count == 0)
            {
                CmdHandlerQueueIsRunning = false;
                return false;
            }
            CmdHandlerQueueIsRunning = true;
            IMonitorModule module = m_ScriptEngine.Worlds[0].RequestModuleInterface<IMonitorModule>();
            int StartTime = Util.EnvironmentTickCount();

            if (!Started) //Break early
                return true;

            if (m_ScriptEngine.ConsoleDisabled || m_ScriptEngine.Disabled)
                return true;

            //Check timers, etc
            bool didAnything = false;
            try
            {
                didAnything = m_ScriptEngine.DoOneScriptPluginPass ();
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("[{0}]: Error in CmdHandlerPass, {1}", m_ScriptEngine.ScriptEngineName, ex);
            }
            Thread.Sleep(10); // don't burn cpu

            if (module != null)
            {
                foreach (Scene scene in m_ScriptEngine.Worlds)
                {
                    ITimeMonitor scriptMonitor = (ITimeMonitor)module.GetMonitor(scene.RegionInfo.RegionID.ToString(), "Script Frame Time");
                    scriptMonitor.AddTime(Util.EnvironmentTickCountSubtract(StartTime));
                }
            }

            if (didAnything)
            {
                CmdHandlerQueueIsRunning = true;
                threadpool.QueueEvent (CmdHandlerQueue, 2);
            }
            else
                CmdHandlerQueueIsRunning = false;
            return false;
        }
        #endregion

        #region Add

        public void AddScriptChange(LUStruct[] items, LoadPriority priority)
        {
            if (RunInMainProcessingThread)
            {
                List<LUStruct> NeedsFired = new List<LUStruct>();
                foreach (LUStruct item in items)
                {
                    if (item.Action == LUType.Unload)
                    {
                        item.ID.CloseAndDispose (true);
                    }
                    else if (item.Action == LUType.Load)
                    {
                        try
                        {
                            if(item.ID.Start(false))
                                NeedsFired.Add(item);
                        }
                        catch (Exception ex) { m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: LEAKED COMPILE ERROR: " + ex); }
                    }
                    else if (item.Action == LUType.Reupload)
                    {
                        try
                        {
                            if(item.ID.Start(true))
                                NeedsFired.Add(item);
                        }
                        catch (Exception ex) { m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: LEAKED COMPILE ERROR: " + ex); }
                    }
                }
                foreach (LUStruct item in NeedsFired)
                {
                    item.ID.FireEvents();
                }
            }
            else
            {
                LUQueue.Add(items, priority);
                if (!ScriptChangeIsRunning)
                    StartThread("Change");
            }
        }

        public void AddEvent(QueueItemStruct QIS, EventPriority priority)
        {
            AddEventSchQIS(QIS);
        }

        #endregion

        #region Remove

        public void RemoveState(ScriptData ID)
        {
            m_ScriptEngine.StateSave.DeleteFrom (ID);
        }

        #endregion

        #region Start thread

        /// <summary>
        /// Queue the event loop given by thread
        /// </summary>
        /// <param name="thread"></param>
        private void StartThread(string thread)
        {
            if (thread == "Change")
            {
                threadpool.QueueEvent(ScriptChangeQueue, 2);
            }
            else if (thread == "CmdHandlerQueue")
            {
                threadpool.QueueEvent (CmdHandlerQueue, 2);
            }
        }

        #endregion

        /// <summary>
        /// Makes sure that all the threads that need to be running are running and starts them if they need to be running
        /// </summary>
        public void PokeThreads()
        {
            if (LUQueue.Count () != 0 && !ScriptChangeIsRunning)
                StartThread ("Change");
            if (!CmdHandlerQueueIsRunning)
                StartThread ("CmdHandlerQueue");
            // if (!EventProcessorIsRunning) //Can't check the count on this one, so poke it anyway
            // StartThread("Event");
        }
        #region Scripts events scheduler control

        private LinkedList<ScriptData> ScriptIDs = new LinkedList<ScriptData>();
        private LinkedList<ScriptData> SleepingScriptIDs = new LinkedList<ScriptData>();
        private HashSet<ScriptData> ScriptInExec = new HashSet<ScriptData>();

        private int SleepingScriptIDsLock = 0;

        private int nEventScripts = 0;
        private int nScriptIDs = 0;

        private DateTime NextSleepersTest = DateTime.Now;

        public void RemoveFromEventSchQueue(ScriptData ID,bool abortcur)
            {
            if (ID == null)
                return;

            Interlocked.Exchange(ref ID.EventsProcDataLocked, 1);
            lock (ID.EventsProcData)
                {
                ID.EventsProcData.IgnoreNew = true;
                ID.EventsProcData.EventsQueue.Clear();
                if (ID.InEventsProcData)
                    {
                    if (ID.EventsProcData.State == (int)ScriptEventsState.InExec)
                        {
                        if(abortcur)
                            ID.EventsProcData.State = (int)ScriptEventsState.InExecAbort;
                        }
                    else
                        {
                        if (ID.EventsProcData.State == (int)ScriptEventsState.Sleep)
                            {
                            lock (SleepingScriptIDs)
                                SleepingScriptIDs.Remove(ID);
                            }
                        else
                            {
                            lock (ScriptIDs)
                                ScriptIDs.Remove(ID);
                            Interlocked.Decrement(ref nScriptIDs);
                            }
                        Interlocked.Decrement(ref nEventScripts);
                        ID.InEventsProcData = false;
                        }
                    }
                Interlocked.Exchange(ref ID.EventsProcDataLocked, 0);
                }
            }

        public void FlushEventSchQueue(ScriptData ID, bool ignorenew)
        {
            if (ID == null)
                return;
            Interlocked.Exchange(ref ID.EventsProcDataLocked, 1);
            lock (ID.EventsProcData)
                {
                ID.EventsProcData.EventsQueue.Clear();
                ID.EventsProcData.IgnoreNew = ignorenew;
                if (ID.EventsProcData.State == (int)ScriptEventsState.Sleep)
                    {
                    lock (SleepingScriptIDs)
                        SleepingScriptIDs.Remove(ID);
                    ID.EventsProcData.State = (int)ScriptEventsState.Idle;
                    }
                
                Interlocked.Exchange(ref ID.EventsProcDataLocked, 0);
                }
        }

        public void SetEventSchSetIgnoreNew(ScriptData ID, bool yes)
        {
            if (ID == null)
                return;
            Interlocked.Exchange(ref ID.EventsProcDataLocked, 1);
            lock (ID.EventsProcData)
                {
                ID.EventsProcData.IgnoreNew = yes;
                Interlocked.Exchange(ref ID.EventsProcDataLocked, 0);
                }
        }

        public void AddEventSchQueue(ScriptData ID, string FunctionName, DetectParams[] qParams, int VersionID, EventPriority priority, params object[] param)
            {
            QueueItemStruct QIS;

            if (ID == null || ID.EventsProcData.IgnoreNew)
                return;

            if (!ID.SetEventParams(FunctionName, qParams)) // check events delay rules
                return;

            QIS = new QueueItemStruct();
            QIS.ID = ID;
            QIS.functionName = FunctionName;
            QIS.llDetectParams = qParams;
            QIS.param = param;
            QIS.VersionID = VersionID;
            QIS.State = ID.State;
            QIS.CurrentlyAt = null;

            Interlocked.Exchange(ref ID.EventsProcDataLocked, 1);
            lock (ID.EventsProcData)
                {
                if (ID.EventsProcData.EventsQueue.Count > 100)
                    {
                    Interlocked.Exchange(ref ID.EventsProcDataLocked, 0);
                    return;
                    }

                ID.EventsProcData.EventsQueue.Enqueue(QIS);

                lock (ScriptIDs)
                    {
                    if (!ID.InEventsProcData)
                        {
                        ID.EventsProcData.State = (int)ScriptEventsState.Idle;
                        ID.EventsProcData.thread = null;
                        ScriptIDs.AddLast(ID);
                        ID.InEventsProcData = true;
                        Interlocked.Increment(ref nScriptIDs);
                        Interlocked.Increment(ref nEventScripts);
                        }
                    }
                Interlocked.Exchange(ref ID.EventsProcDataLocked, 0);
                }

            lock (WorkersLock)
                {
                if (WorkersLock.nWorkers < MaxScriptThreads && WorkersLock.nWorkers < nScriptIDs)
                    {
                    Scriptthreadpool.QueueEvent(loop, 2);
                    }
                }
            }


        public void AddEventSchQIS(QueueItemStruct QIS)
            {
            ScriptData ID;

            ID = QIS.ID;
            if (ID == null || ID.EventsProcData.IgnoreNew)
                return;

            if (!QIS.ID.SetEventParams(QIS.functionName, QIS.llDetectParams)) // check events delay rules
                return;

            QIS.CurrentlyAt = null;

            Interlocked.Exchange(ref ID.EventsProcDataLocked, 1);
            lock (ID.EventsProcData)
                {
                if (ID.EventsProcData.EventsQueue.Count > 100)
                    {
                    Interlocked.Exchange(ref ID.EventsProcDataLocked, 0);
                    return;
                    }

                ID.EventsProcData.EventsQueue.Enqueue(QIS);
                lock (ScriptIDs)
                    {
                    if (!ID.InEventsProcData)
                        {
                        ID.EventsProcData.State = (int)ScriptEventsState.Idle;
                        ID.EventsProcData.thread = null;
                        ScriptIDs.AddLast(ID);
                        Interlocked.Increment(ref nScriptIDs);
                        Interlocked.Increment(ref nEventScripts);
                        ID.InEventsProcData = true;
                        }
                    }
                Interlocked.Exchange(ref ID.EventsProcDataLocked, 0);
                }

            lock (WorkersLock)
                {
                if (WorkersLock.nWorkers < MaxScriptThreads && WorkersLock.nWorkers < nScriptIDs)
                    {
                    Scriptthreadpool.QueueEvent(loop, 2);
                    }
                }
            }

        private void InsertInSleepers(ScriptData ID)
            {
            // insert sorted by time to wakeup
            LinkedListNode<ScriptData> where=null;

            Interlocked.Exchange(ref SleepingScriptIDsLock, 1);
            lock (SleepingScriptIDs)
                {
                if (SleepingScriptIDs.Count > 0)
                    {
                    DateTime when = ID.EventsProcData.TimeCheck;
                    if (SleepingScriptIDs.First.Value.EventsProcData.TimeCheck.Ticks > when.Ticks)
                        SleepingScriptIDs.AddFirst(ID);
                    else if (SleepingScriptIDs.Last.Value.EventsProcData.TimeCheck.Ticks <= when.Ticks)
                        SleepingScriptIDs.AddLast(ID);
                    else 
                        {
                        where = SleepingScriptIDs.Last.Previous;
                        while (where != null && where.Value.EventsProcData.TimeCheck.Ticks > when.Ticks)
                            where = where.Previous;
                        if (where != null)
                            SleepingScriptIDs.AddAfter(where, ID);
                        else
                            SleepingScriptIDs.AddFirst(ID);
                        }
                    }
                else
                    SleepingScriptIDs.AddLast(ID);
                Interlocked.Exchange(ref SleepingScriptIDsLock, 0);
                }
            }

        public bool loop()
            {
            ScriptData ID;
            ScriptData doID;
            DateTime Tnow;
            bool waslocked;

            Interlocked.Increment(ref WorkersLock.nWorkers);

            while (true)
                {

                if (m_ScriptEngine.ConsoleDisabled || m_ScriptEngine.Disabled)
                    break;

                // check one sleeper

                waslocked = false;

                ID = null;
                if (Interlocked.Exchange(ref SleepingScriptIDsLock, 1) == 0)
                    {
                    lock (SleepingScriptIDs)
                        {
                        Tnow = DateTime.Now;
                        if (Tnow.Ticks > NextSleepersTest.Ticks)
                            {
                            NextSleepersTest = Tnow.AddMilliseconds(50);
                            while (SleepingScriptIDs.Count > 0)
                                {
                                ID = SleepingScriptIDs.First.Value;

                                if (ID == null || !ID.InEventsProcData || ID.Suspended || ID.Script == null || ID.Loading || ID.Disabled)
                                    {
                                    // forget this one
                                    SleepingScriptIDs.RemoveFirst();
                                    lock (ID.EventsProcData)
                                        ID.InEventsProcData = false;
                                    Interlocked.Decrement(ref nEventScripts);
                                    continue;
                                    }

                                DateTime Ttest = Tnow.AddMilliseconds(25);                             

                                if (Ttest.Ticks < ID.EventsProcData.TimeCheck.Ticks) // if gone after current time ene
                                    break;

                                // sleep expired
                                SleepingScriptIDs.RemoveFirst();

                                Interlocked.Exchange(ref ID.EventsProcDataLocked, 1);
                                lock (ID.EventsProcData)
                                    {
                                    ID.EventsProcData.State = (int)ScriptEventsState.Running;
                                    ID.EventsProcData.TimeCheck = Tnow.AddMilliseconds(100);
                                    Interlocked.Exchange(ref ID.EventsProcDataLocked, 0);
                                    }
                                Interlocked.Increment(ref nScriptIDs);
                                lock (ScriptIDs)
                                    ScriptIDs.AddLast(ID);

                                lock (WorkersLock) // try to get help
                                    {
                                    if (WorkersLock.nWorkers < MaxScriptThreads &&
                                            WorkersLock.nWorkers < nScriptIDs)
                                        {
                                        Scriptthreadpool.QueueEvent(loop, 2);
                                        }
                                    }
                                }
                            }
                        Interlocked.Exchange(ref SleepingScriptIDsLock, 0);
                        }
                    }

                // check one active

                doID = null;
                ID = null;

                lock (ScriptIDs)
                    {
                    if (ScriptIDs.Count > 0)
                        {
                        ID = ScriptIDs.First.Value;
                        ScriptIDs.RemoveFirst();
                        }
                    }

                if (ID != null)
                    {
                    if (Interlocked.Exchange(ref ID.EventsProcDataLocked, 1) == 1)
                        {
                        lock (ScriptIDs)
                            {
                            ScriptIDs.AddLast(ID);
                            }
                        waslocked = true;
                        }
                    else
                        {
                        lock (ID.EventsProcData)
                            {
                            // check if we still can exec
                            if (!ID.InEventsProcData || ID.Suspended || ID.Script == null || ID.Loading || ID.Disabled)
                                {
                                // forget this one
                                ID.InEventsProcData = false;
                                Interlocked.Decrement(ref nScriptIDs);
                                Interlocked.Decrement(ref nEventScripts);
                                }
                            else
                                {
                                switch (ID.EventsProcData.State)
                                    {
                                    case (int)ScriptEventsState.Running:

                                        ID.EventsProcData.State = (int)ScriptEventsState.InExec;
                                        ID.EventsProcData.TimeCheck = DateTime.Now.AddMilliseconds(100);
                                        doID = ID;
                                        lock (ScriptIDs)
                                            {
                                            ScriptIDs.AddLast(ID);
                                            }
                                        break;

                                    case (int)ScriptEventsState.Sleep:

                                        Tnow = DateTime.Now;

                                        if (Tnow.Ticks > ID.EventsProcData.TimeCheck.Ticks)
                                            {
                                            ID.EventsProcData.State = (int)ScriptEventsState.Running;
                                            lock (ScriptIDs)
                                                {
                                                ScriptIDs.AddLast(ID);
                                                }
                                            }
                                        else
                                            {
                                            Interlocked.Decrement(ref nScriptIDs);
                                            InsertInSleepers(ID);
                                            }
                                        break;

                                    case (int)ScriptEventsState.Idle:

                                        if (ID.EventsProcData.EventsQueue.Count > 0)
                                            {
                                            ID.EventsProcData.CurExecQIS = (QueueItemStruct)ID.EventsProcData.EventsQueue.Dequeue();
                                            if (ID.VersionID == ID.EventsProcData.CurExecQIS.VersionID)
                                                {
                                                    if (!ID.Loading)
                                                    {
                                                        ID.EventsProcData.State = (int)ScriptEventsState.InExec;
                                                        ID.EventsProcData.TimeCheck = DateTime.Now.AddMilliseconds(200);
                                                        doID = ID;
                                                    }
                                                    else
                                                    {
                                                        //If the script is loading, we need to wait until it is done, just re-enqueue the event
                                                        ID.EventsProcData.EventsQueue.Enqueue(ID.EventsProcData.CurExecQIS);
                                                    }
                                                }
                                            lock (ScriptIDs)
                                                {
                                                ScriptIDs.AddLast(ID);
                                                }
                                            }
                                        else
                                            {
                                            ID.InEventsProcData = false;
                                            Interlocked.Decrement(ref nScriptIDs);
                                            Interlocked.Decrement(ref nEventScripts);
                                            }
                                        break;

                                    case (int)ScriptEventsState.InExec:
                                    case (int)ScriptEventsState.InExecAbort:
                                        // if (Tnow < Ev.TimeCheck)
                                        lock (ScriptIDs)
                                            {
                                            ScriptIDs.AddLast(ID);
                                            }
                                        // else
                                        break;

                                    case (int)ScriptEventsState.Delete:

                                        lock (ScriptIDs)
                                            {
                                            ID.EventsProcData.EventsQueue.Clear();
                                            ID.InEventsProcData = false;
                                            Interlocked.Decrement(ref nScriptIDs);
                                            Interlocked.Decrement(ref nEventScripts);
                                            }
                                        break;

                                    default:
                                        break;
                                    }
                                }
                            Interlocked.Exchange(ref ID.EventsProcDataLocked, 0);
                            }

                        }
                    }

                if (doID != null)
                    {
                    try // this may not be ok
                        {
                        EventSchExec(doID);
                        }
                    catch
                        {
                        lock (doID.EventsProcData)
                            {
                            doID.EventsProcData.State = (int)ScriptEventsState.Idle;
                            }
                        }
                    }

                if (waslocked)
                    {
                    Thread.Sleep(20);
                    continue;
                    }

                if (nScriptIDs < WorkersLock.nWorkers)
                    {
                    if (SleepingScriptIDs.Count == 0)
                        break;
                    if (WorkersLock.nWorkers > 1)
                        break;
                    else
                        Thread.Sleep(20);
                    }
                }

            Interlocked.Decrement(ref WorkersLock.nWorkers);

            return false;
            }

        public void EventSchExec(ScriptData ID)
            {
            QueueItemStruct QIS;

            Interlocked.Exchange(ref ID.EventsProcDataLocked, 1);
            lock (ID.EventsProcData)
                {
                QIS = ID.EventsProcData.CurExecQIS;
                if (!ID.Running)
                    {
                    //do only state_entry and on_rez
                    if (QIS.functionName != "state_entry"
                        || QIS.functionName != "on_rez")
                        {
                        Interlocked.Exchange(ref ID.EventsProcDataLocked, 0);
                        return;
                        }
                    }
                ID.EventsProcData.thread = Thread.CurrentThread;
                Interlocked.Exchange(ref ID.EventsProcDataLocked, 0);
                }


            lock (ScriptInExec)
                {
                ScriptInExec.Add(ID);
                }

            bool res = EventSchProcessQIS(ref QIS);

            lock (ScriptInExec)
                {
                ScriptInExec.Remove(ID);
                }

            Interlocked.Exchange(ref ID.EventsProcDataLocked, 1);
            lock (ID.EventsProcData)
                {
                ID.EventsProcData.thread = null;

                if (ID.EventsProcData.State == (int)ScriptEventsState.InExecAbort)
                    ID.EventsProcData.State = (int)ScriptEventsState.Delete;

//                else if (!res || ID.VersionID != QIS.VersionID)
                else if (!res)
                    {
                    ID.EventsProcData.State = (int)ScriptEventsState.Idle;
                    }

                else
                    {
                    ID.EventsProcData.CurExecQIS = QIS;

                    if (QIS.CurrentlyAt.SleepTo.Ticks != 0)
                        {
                        ID.EventsProcData.TimeCheck = QIS.CurrentlyAt.SleepTo;
                        ID.EventsProcData.State = (int)ScriptEventsState.Sleep;
                        }
                    else
                        ID.EventsProcData.State = (int)ScriptEventsState.Running;
                    }
                Interlocked.Exchange(ref ID.EventsProcDataLocked, 0);
                }
            return;
            }

        public bool EventSchProcessQIS(ref QueueItemStruct QIS)
        {
            try
            {
                Exception ex = null;
                EnumeratorInfo Running = QIS.ID.Script.ExecuteEvent(QIS.State,
                            QIS.functionName,
                            QIS.param, QIS.CurrentlyAt, out ex);

                if (ex != null)
                {
                    //Check exceptions, some are ours to deal with, and others are to be logged
                    if (ex.Message.Contains("SelfDeleteException"))
                    {
                        if (QIS.ID.Part != null && QIS.ID.Part.ParentEntity != null)
                        {
                            IBackupModule backup = QIS.ID.Part.ParentEntity.Scene.RequestModuleInterface<IBackupModule> ();
                            if (backup != null)
                                backup.DeleteSceneObjects(
                                    new ISceneEntity[1] { QIS.ID.Part.ParentEntity }, true);
                        }
                    }
                    else if (ex.Message.Contains("ScriptDeleteException"))
                    {
                        if (QIS.ID.Part != null && QIS.ID.Part.ParentEntity != null)
                            QIS.ID.Part.Inventory.RemoveInventoryItem(QIS.ID.ItemID);
                    }
                    //Log it for the user
                    else if (!(ex.Message.Contains("EventAbortException")) &&
                        !(ex.Message.Contains("MinEventDelayException")))
                        QIS.ID.DisplayUserNotification(ex.ToString(), "executing", false, true);
                    return false;
                }
                else if (Running != null)
                {
                    //Did not finish so requeue it
                    QIS.CurrentlyAt = Running;
                    return true; //Do the return... otherwise we open the queue for this event back up
                }
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