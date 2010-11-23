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
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class MaintenanceThread
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IScriptDataConnector ScriptFrontend;
        private ScriptEngine m_ScriptEngine;
        private bool FiredStartupEvent = false;
        public AuroraThreadPool threadpool = null;
        public AuroraThreadPool Scriptthreadpool = null;
        public bool StateSaveIsRunning = false;
        public bool ScriptChangeIsRunning = false;
        public bool EventProcessorIsRunning = false;
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
                threadpool.QueueEvent(StateSaveQueue, 2);
                //Start the queue because it can't start itself
                threadpool.QueueEvent(CmdHandlerQueue, 2);
            }
        }

        /// <summary>
        /// Queue that handles the loading and unloading of scripts
        /// </summary>
        private StartPerformanceQueue LUQueue = new StartPerformanceQueue();

        /// <summary>
        /// Queue containing events waiting to be executed.
        /// </summary>
        private EventPerformanceQueue EventProcessorQueue = new EventPerformanceQueue();

        /// <summary>
        /// Queue containing scripts that need to have states saved or deleted.
        /// </summary>
        private Queue StateQueue = new Queue();

        /// <summary>
        /// Removes the script from the event queue so it does not fire anymore events.
        /// </summary>
        private Dictionary<UUID, int> NeedsRemoved = new Dictionary<UUID, int>();

        private EventManager EventManager = null;

        #endregion

        #region Constructor

        public MaintenanceThread(ScriptEngine Engine)
        {
            m_ScriptEngine = Engine;
            ScriptFrontend = Aurora.DataManager.DataManager.RequestPlugin<IScriptDataConnector>();
            EventManager = Engine.EventManager;

            RunInMainProcessingThread = Engine.Config.GetBoolean("RunInMainProcessingThread", false);

            RunInMainProcessingThread = false; // temporary false until code is fix to work with true

            //There IS a reason we start this, even if RunInMain is enabled
            // If this isn't enabled, we run into issues with the CmdHandlerQueue,
            // as it always must be async, so we must run the pool anyway
            AuroraThreadPoolStartInfo info = new AuroraThreadPoolStartInfo();
            info.priority = ThreadPriority.Normal;
            info.Threads = 4;
            info.MaxSleepTime = Engine.Config.GetInt("SleepTime", 300);
            threadpool = new AuroraThreadPool(info);


            MaxScriptThreads = Engine.Config.GetInt("Threads", 100); // leave control threads out of user option
            AuroraThreadPoolStartInfo sinfo = new AuroraThreadPoolStartInfo();
            sinfo.priority = ThreadPriority.Normal;
            sinfo.Threads = MaxScriptThreads + 1;
            sinfo.MaxSleepTime = Engine.Config.GetInt("SleepTime", 300);
            Scriptthreadpool = new AuroraThreadPool(sinfo);

            AppDomain.CurrentDomain.AssemblyResolve += m_ScriptEngine.AssemblyResolver.OnAssemblyResolve;


        }

        #endregion

        #region Loops

        public bool StateSaveQueue()
        {
            if (!Started) //Break early
                return true;

            if (m_ScriptEngine.ConsoleDisabled || m_ScriptEngine.Disabled)
                return true;

            StateSaveIsRunning = true;
            StateQueueItem item;
            lock (StateQueue)
            {
                if (StateQueue.Count != 0)
                    item = (StateQueueItem)StateQueue.Dequeue();
                else
                {
                    StateSaveIsRunning = false;
                    return true;
                }
            }
            if (item.ID == null)
                return false;

            if (item.Create)
                ScriptDataSQLSerializer.SaveState(item.ID, m_ScriptEngine);
            else
                RemoveState(item.ID);

            Thread.Sleep(10);
            threadpool.QueueEvent(StateSaveQueue, 3);
            return false;
        }

        /// <summary>
        /// This loop deals with starting and stoping scripts
        /// </summary>
        /// <returns></returns>
        public bool ScriptChangeQueue()
        {
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
                        item.ID.CloseAndDispose(false);
                    }
                    else if (item.Action == LUType.Load)
                    {
                        try
                        {
                            //Start
                            item.ID.Start(false);
                            NeedsFired.Add(item);
                        }
                        catch (Exception ex) { m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: LEAKED COMPILE ERROR: " + ex); }
                    }
                    else if (item.Action == LUType.Reupload)
                    {
                        try
                        {
                            //Start, but don't add to the queue's again
                            item.ID.Start(true);
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
                Thread.Sleep(20);
                threadpool.QueueEvent(ScriptChangeQueue, 2); //Requeue us
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

                        scene.EventManager.TriggerFinishedStartup("ScriptEngine", new List<string>(){m_ScriptEngine.ScriptFailCount.ToString(),
                                                                    m_ScriptEngine.ScriptErrorMessages}); //Tell that we are done
                    }
                }
            }
            ScriptChangeIsRunning = false;
            return false;
        }

        public bool CmdHandlerQueue()
        {
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
            Thread.Sleep(25); // don't burn cpu
            threadpool.QueueEvent(CmdHandlerQueue, 2);
            return false;
        }
        #endregion

        #region Add

        /// <summary>
        /// Adds the given item to the queue.
        /// </summary>
        /// <param name="ID">InstanceData that needs to be state saved</param>
        /// <param name="create">true: create a new state. false: remove the state.</param>
        public void AddToStateSaverQueue(ScriptData ID, bool create)
        {
            StateQueueItem SQ = new StateQueueItem();
            SQ.ID = ID;
            SQ.Create = create;

            if (RunInMainProcessingThread)
            {
                if (SQ.Create)
                    ScriptDataSQLSerializer.SaveState(SQ.ID, m_ScriptEngine);
                else
                    RemoveState(SQ.ID);
            }
            else
            {
                StateQueue.Enqueue(SQ);
                if (!StateSaveIsRunning)
                    StartThread("State");
            }
        }

        public void AddScriptChange(LUStruct[] items, LoadPriority priority)
        {
            if (RunInMainProcessingThread)
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
            ScriptFrontend.DeleteStateSave(ID.ItemID);
        }

        #endregion

        #region Start thread

        /// <summary>
        /// Queue the event loop given by thread
        /// </summary>
        /// <param name="thread"></param>
        private void StartThread(string thread)
        {
            if (thread == "State")
            {
                threadpool.QueueEvent(StateSaveQueue, 3);
            }
            else if (thread == "Change")
            {
                threadpool.QueueEvent(ScriptChangeQueue, 2);
            }
        }

        #endregion

        /// <summary>
        /// Makes sure that all the threads that need to be running are running and starts them if they need to be running
        /// </summary>
        public void PokeThreads()
        {
            if (StateQueue.Count != 0 && !StateSaveIsRunning)
                StartThread("State");
            if (LUQueue.Count() != 0 && !ScriptChangeIsRunning)
                StartThread("Change");
            // if (!EventProcessorIsRunning) //Can't check the count on this one, so poke it anyway
            // StartThread("Event");
        }
        #region Scripts events scheduler control
        /*
public class aJob
{
public QueueItemStruct QIS;
}
*/

        private LinkedList<ScriptData> ScriptIDs = new LinkedList<ScriptData>();
        private LinkedList<ScriptData> SleepingScriptIDs = new LinkedList<ScriptData>();
        private HashSet<ScriptData> ScriptInExec = new HashSet<ScriptData>();
        public int NScriptIDs = 0;
        public int NSleepingScriptIDs = 0;
        public int NScriptInExec = 0;

        public void RemoveFromEventSchQueue(ScriptData ID)
        {
            if (ID == null)
                return;
            bool wasign;
            lock (ID.EventsProcData)
            {
                ID.EventsProcDataLocked = true;
                wasign = ID.EventsProcData.IgnoreNew;
                ID.EventsProcData.IgnoreNew = true;
                ID.EventsProcData.EventsQueue.Clear();
                if (ID.InEventsProcData)
                {
                    if (ID.EventsProcData.State == (int)ScriptEventsState.InExec)
                        ID.EventsProcData.State = (int)ScriptEventsState.InExecAbort;
                    else if (ID.EventsProcData.State == (int)ScriptEventsState.Sleep)
                    {
                        lock (SleepingScriptIDs)
                        {
                            NSleepingScriptIDs--;
                            SleepingScriptIDs.Remove(ID);
                        }
                    }
                    else
                    {
                        lock (ScriptIDs)
                        {
                            NScriptIDs--;
                            ScriptIDs.Remove(ID);
                        }
                    }
                    ID.InEventsProcData = false;
                }
                ID.EventsProcData.IgnoreNew = wasign;
            }
            ID.EventsProcDataLocked = false;

            /* workers should leave by them selfs, so no worries about going <0
            lock (WorkersLock) // this may leave lost workers if timeslice doesn't return
            {
            WorkersLock.nWorkers--;
            if (WorkersLock.nWorkers < 0)
            WorkersLock.nWorkers=0;
            }
            */
        }

        public void FlushEventSchQueue(ScriptData ID, bool abortcur)
        {
            if (ID == null)
                return;
            lock (ID.EventsProcData)
            {
                ID.EventsProcDataLocked = true;
                ID.EventsProcData.EventsQueue.Clear();
            }
            ID.EventsProcDataLocked = false;
        }

        public void SetEventSchSetIgnoreNew(ScriptData ID, bool yes)
        {
            if (ID == null)
                return;
            lock (ID.EventsProcData)
            {
                ID.EventsProcData.IgnoreNew = yes;
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

            lock (ID.EventsProcData)
            {
                ID.EventsProcDataLocked = true;

                if (ID.EventsProcData.EventsQueue.Count > 100)
                {
                    ID.EventsProcDataLocked = false;
                    return;
                }

                ID.EventsProcData.EventsQueue.Enqueue(QIS);
                lock (ScriptIDs)
                {
                    if (!ScriptIDs.Contains(ID))
                    {
                        ID.EventsProcData.State = (int)ScriptEventsState.Idle;
                        ID.EventsProcData.thread = null;
                        ScriptIDs.AddLast(ID);
                        NScriptIDs++;
                        ID.InEventsProcData = true;
                    }
                }
            }
            ID.EventsProcDataLocked = false;

            lock (WorkersLock)
            {
                if (WorkersLock.nWorkers < MaxScriptThreads)
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

            lock (ID.EventsProcData)
            {
                ID.EventsProcDataLocked = true;

                if (ID.EventsProcData.EventsQueue.Count > 100)
                {
                    ID.EventsProcDataLocked = false;
                    return;
                }

                ID.EventsProcData.EventsQueue.Enqueue(QIS);
                lock (ScriptIDs)
                {
                    if (!ScriptIDs.Contains(ID))
                    {
                        ID.EventsProcData.State = (int)ScriptEventsState.Idle;
                        ID.EventsProcData.thread = null;
                        ScriptIDs.AddLast(ID);
                        NScriptIDs++;
                        ID.InEventsProcData = true;
                    }
                }
            }
            ID.EventsProcDataLocked = false;

            lock (WorkersLock)
            {
                if (WorkersLock.nWorkers < MaxScriptThreads)
                {
                    Scriptthreadpool.QueueEvent(loop, 2);
                }
            }
        }

        public bool loop()
        {
            ScriptData ID;
            ScriptData doID;
            DateTime Tnow;
            TimeSpan ToSleep;
            bool waslocked;
            bool WillSleep;

            lock (WorkersLock)
            {
                WorkersLock.nWorkers++;
            }

            while (true)
            {

                if (m_ScriptEngine.ConsoleDisabled || m_ScriptEngine.Disabled)
                    break;

                // check one sleeper

                waslocked = false;

                ID = null;

                lock (SleepingScriptIDs)
                {
                    if (NSleepingScriptIDs > 0)
                    {
                        NSleepingScriptIDs--;
                        ID = SleepingScriptIDs.First.Value;
                        SleepingScriptIDs.RemoveFirst();
                    }
                }

                if (ID != null)
                {
                    if (ID.EventsProcDataLocked)
                    {
                        lock (SleepingScriptIDs)
                        {
                            SleepingScriptIDs.AddLast(ID);
                            NSleepingScriptIDs++;
                            waslocked = true;
                        }
                    }
                    else
                    {
                        lock (ID.EventsProcData)
                        {
                            ID.EventsProcDataLocked = true;

                            // check if we still can exec
                            if (!ID.InEventsProcData || ID.Suspended || ID.Script == null || ID.Loading || ID.Disabled)
                            {
                                // forget this one
                                ID.InEventsProcData = false;
                            }

                            else if (ID.EventsProcData.State == (int)ScriptEventsState.Sleep)
                            {
                                Tnow = DateTime.Now;
                                ToSleep = ID.EventsProcData.TimeCheck.Subtract(Tnow);

                                if (ToSleep.TotalMilliseconds < 0)
                                {
                                    ID.EventsProcData.State = (int)ScriptEventsState.Running;
                                    ID.EventsProcData.TimeCheck = DateTime.Now.AddMilliseconds(100);
                                    lock (ScriptIDs)
                                    {
                                        ScriptIDs.AddLast(ID);
                                        NScriptIDs++;
                                    }
                                }
                                else
                                {
                                    lock (SleepingScriptIDs)
                                    {
                                        SleepingScriptIDs.AddLast(ID);
                                        NSleepingScriptIDs++;
                                    }
                                }
                            }
                        }
                        ID.EventsProcDataLocked = false;
                    }
                }

                // check one active

                doID = null;
                ID = null;

                lock (ScriptIDs)
                {
                    if (NScriptIDs > 0)
                    {
                        NScriptIDs--;
                        ID = ScriptIDs.First.Value;
                        ScriptIDs.RemoveFirst();
                    }
                }

                if (ID != null)
                {
                    if (ID.EventsProcDataLocked)
                    {
                        lock (ScriptIDs)
                        {
                            ScriptIDs.AddLast(ID);
                            NScriptIDs++;
                        }
                        waslocked = true;
                    }
                    else
                    {
                        lock (ID.EventsProcData)
                        {
                            ID.EventsProcDataLocked = true;

                            // check if we still can exec
                            if (!ID.InEventsProcData || ID.Suspended || ID.Script == null || ID.Loading || ID.Disabled)
                            {
                                // forget this one
                                ID.InEventsProcData = false;
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
                                            NScriptIDs++;
                                        }
                                        break;

                                    case (int)ScriptEventsState.Sleep:

                                        Tnow = DateTime.Now;
                                        ToSleep = ID.EventsProcData.TimeCheck.Subtract(Tnow);

                                        if (ToSleep.TotalMilliseconds < 0)
                                        {
                                            ID.EventsProcData.State = (int)ScriptEventsState.Running;
                                            lock (ScriptIDs)
                                            {
                                                ScriptIDs.AddLast(ID);
                                                NScriptIDs++;
                                            }
                                        }
                                        else
                                        {
                                            lock (SleepingScriptIDs)
                                            {
                                                SleepingScriptIDs.AddLast(ID);
                                                NSleepingScriptIDs++;
                                            }
                                        }
                                        break;

                                    case (int)ScriptEventsState.Idle:

                                        if (ID.EventsProcData.EventsQueue.Count > 0)
                                        {
                                            ID.EventsProcData.CurExecQIS = (QueueItemStruct)ID.EventsProcData.EventsQueue.Dequeue();
                                            if (ID.VersionID == ID.EventsProcData.CurExecQIS.VersionID)
                                            {
                                                ID.EventsProcData.State = (int)ScriptEventsState.InExec;
                                                ID.EventsProcData.TimeCheck = DateTime.Now.AddMilliseconds(200);
                                                doID = ID;
                                            }
                                            lock (ScriptIDs)
                                            {
                                                ScriptIDs.AddLast(ID);
                                                NScriptIDs++;
                                            }
                                        }
                                        else
                                            ID.InEventsProcData = false;

                                        break;

                                    case (int)ScriptEventsState.InExec:
                                    case (int)ScriptEventsState.InExecAbort:
                                        // if (Tnow < Ev.TimeCheck)
                                        lock (ScriptIDs)
                                        {
                                            ScriptIDs.AddLast(ID);
                                            NScriptIDs++;
                                        }
                                        // else
                                        break;

                                    case (int)ScriptEventsState.Delete:

                                        lock (ScriptIDs)
                                        {
                                            ID.EventsProcData.EventsQueue.Clear();
                                            ID.InEventsProcData = false;
                                        }
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                        ID.EventsProcDataLocked = false;
                    }
                }

                if (doID != null)
                {
                    try // this may not be ok
                    {
                        EventSchExec(ID);
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

                WillSleep = false;

                lock (ScriptIDs)
                {
                    //if (NScriptIDs == 0)
                    //{
                        lock (SleepingScriptIDs)
                        {
                            lock (WorkersLock)
                            {
                                if (NSleepingScriptIDs < WorkersLock.nWorkers &&
                                    NScriptIDs == 0)
                                    break;
                                else
                                    WillSleep = true;
                            }
                        }
                    //}
                }

                if (WillSleep)
                    Thread.Sleep(40);
            }

            lock (WorkersLock)
            {
                WorkersLock.nWorkers--;
                if (WorkersLock.nWorkers < 0)
                    WorkersLock.nWorkers = 0;
            }
            return false;
        }


        public void EventSchExec(ScriptData ID)
        {
            QueueItemStruct QIS;

            lock (ID.EventsProcData)
            {
                ID.EventsProcDataLocked = true;
                QIS = ID.EventsProcData.CurExecQIS;
                if (!ID.Running)
                {
                    //do only state_entry and on_rez
                    if (QIS.functionName != "state_entry"
                        || QIS.functionName != "on_rez")
                    {
                        ID.EventsProcDataLocked = false;
                        return;
                    }
                }
                ID.EventsProcData.thread = Thread.CurrentThread;
                lock (ScriptInExec)
                {
                    ScriptInExec.Remove(ID);
                }
            }
            ID.EventsProcDataLocked = false;

            lock (ScriptInExec)
            {
                ScriptInExec.Add(ID);
            }

            bool res = EventSchProcessQIS(ref QIS);

            lock (ScriptInExec)
            {
                ScriptInExec.Remove(ID);
            }

            lock (ID.EventsProcData)
            {
                ID.EventsProcDataLocked = true;
                ID.EventsProcData.thread = null;

                if (ID.EventsProcData.State == (int)ScriptEventsState.InExecAbort)
                    ID.EventsProcData.State = (int)ScriptEventsState.Delete;

                else if (!res || ID.VersionID != QIS.VersionID)
                    ID.EventsProcData.State = (int)ScriptEventsState.Idle;

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
            }

            ID.EventsProcDataLocked = false;
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
                if (QIS.ID.VersionID != QIS.VersionID)
                    return false;

                if (ex != null)
                {
                    //Check exceptions, some are ours to deal with, and others are to be logged
                    if (ex is SelfDeleteException)
                    {
                        if (QIS.ID.part != null && QIS.ID.part.ParentGroup != null)
                            QIS.ID.part.ParentGroup.Scene.DeleteSceneObject(
                                QIS.ID.part.ParentGroup, false, true);
                    }
                    else if (ex is ScriptDeleteException)
                    {
                        if (QIS.ID.part != null && QIS.ID.part.ParentGroup != null)
                            QIS.ID.part.Inventory.RemoveInventoryItem(QIS.ID.ItemID);
                    }
                    //Log it for the user
                    else if (!(ex is EventAbortException) &&
                        !(ex is MinEventDelayException))
                        QIS.ID.DisplayUserNotification(ex.Message, "", false, true);
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
                QIS.ID.DisplayUserNotification(ex.Message, "executing", false, true);
            }
            //Tell the event manager about it so that the events will be removed from the queue
            EventManager.EventComplete(QIS);
            return false;
        }

        #endregion
    }
}