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
        private AuroraThreadPool threadpool = null;
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

                ScriptChangeQueue();
                StateSaveQueue();
//                EventQueue();

                //Start the queue because it can't start itself
                CmdHandlerQueue();
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
            //   If this isn't enabled, we run into issues with the CmdHandlerQueue,
            //    as it always must be async, so we must run the pool anyway
            AuroraThreadPoolStartInfo info = new AuroraThreadPoolStartInfo();
            info.priority = ThreadPriority.Normal;

            MaxScriptThreads = Engine.Config.GetInt("Threads", 100); // leave control threads out of user option
            info.Threads = MaxScriptThreads + 3;
            info.MaxSleepTime = Engine.Config.GetInt("SleepTime", 100);

            threadpool = new AuroraThreadPool(info);

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
                threadpool.QueueEvent(ScriptChangeQueue, 2); //Requeue us
            }
            else
                ScriptChangeIsRunning = false;

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
            return false;
        }
/*
        public bool EventQueue()
        {
            if (!Started) //Break early
                return true;

            if (m_ScriptEngine.ConsoleDisabled || m_ScriptEngine.Disabled)
                return true;

            bool SendUpSleepRequest = false;
            try
            {
                EventProcessorIsRunning = true;
                object QIS = null;
                if (EventProcessorQueue.GetNext(out QIS))
                {
                    if (QIS != null)
                    {
                        //This sets up a part to determine whether we should sleep on the next run so we don't kill the CPU
                        SendUpSleepRequest = ProcessQIS((QueueItemStruct)QIS);
                        threadpool.QueueEvent(EventQueue, 1);
                    }
                }
                else
                    EventProcessorIsRunning = false;
            }
            catch (Exception ex)
            {
                EventProcessorIsRunning = false;
                m_log.WarnFormat("[{0}]: Handled exception stage 2 in the Event Queue: " + ex.Message, m_ScriptEngine.ScriptEngineName);
            }
            EventProcessorIsRunning = false;
            return SendUpSleepRequest;
        }
*/
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
            Thread.Sleep(10);   // don't burn cpu
            threadpool.QueueEvent(CmdHandlerQueue, 2);
            return false;
        }

        #endregion

        #region Queue processing

        /// <summary>
        /// Processes and fires an event
        /// </summary>
        /// <param name="QIS"></param>
        /// <returns>Returns whether the calling thread should sleep after processing this request</returns>
        public bool ProcessQIS(QueueItemStruct QIS)
        {
            //Disabled, not running, suspended, null scripts, or loading scripts dont get events fired.
            if (QIS.ID.Suspended || QIS.ID.Script == null || 
                QIS.ID.Loading || QIS.ID.Disabled)
                return true;

            if (!QIS.ID.Running)
            {
                //Readd only state_entry and on_rez
                if (QIS.functionName == "state_entry"
                    || QIS.functionName == "on_rez")
                    EventProcessorQueue.Add(QIS, EventPriority.Continued);
                return true;
            }

            //Check if this event was fired with an old versionID
            if (NeedsRemoved.ContainsKey(QIS.ID.ItemID))
                if(NeedsRemoved[QIS.ID.ItemID] >= QIS.VersionID)
                    return true;

            try
            {
                //Either its currently doing something, or the script has set the event params for the new event
                if (QIS.CurrentlyAt != null || QIS.ID.SetEventParams(QIS.functionName, QIS.llDetectParams))
                {
                    //If this is true, there is/was a sleep occuring
                    if (QIS.CurrentlyAt != null && QIS.CurrentlyAt.SleepTo.Ticks != 0)
                    {
                        DateTime nowTicks = DateTime.Now;
                        if ((QIS.CurrentlyAt.SleepTo - nowTicks).TotalMilliseconds > 0)
                        {
                            //Its supposed to be sleeping....
                            // No processing!
                            EventProcessorQueue.Add(QIS, EventPriority.Continued);
                            return true;
                        }
                        else
                        {
                            //Reset the time so we don't keep checking
                            QIS.CurrentlyAt.SleepTo = DateTime.MinValue;
                        }
                    }
                    Exception ex = null;
                    EnumeratorInfo Running = QIS.ID.Script.ExecuteEvent(QIS.ID.State,
                                QIS.functionName,
                                QIS.param, QIS.CurrentlyAt, out ex);
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
                        else if(!(ex is EventAbortException) &&
                            !(ex is MinEventDelayException))
                            QIS.ID.DisplayUserNotification(ex.ToString(), "", false, true);
                        return false;
                    }
                    else if (Running != null)
                    {
                        //Did not finish so requeue it
                        QIS.CurrentlyAt = Running;
                        EventProcessorQueue.Add(QIS, EventPriority.Continued);
                        return false; //Do the return... otherwise we open the queue for this event back up
                    }
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
/*
            if (RunInMainProcessingThread)
            {
                ProcessQIS(QIS);
            }
            else
            {
                EventProcessorQueue.Add(QIS, priority);
                if (!EventProcessorIsRunning)
                    StartThread("Event");
            }
*/
            AddEventSchQIS(QIS);
        }

        #endregion

        #region Remove

        /// <summary>
        /// Sets the newest VersionID to the given versionID so taht we can block out ALL previous scripts
        /// </summary>
        /// <param name="ItemID"></param>
        /// <param name="VersionID"></param>
        public void RemoveFromEventQueue(UUID ItemID, int VersionID)
        {
            NeedsRemoved[ItemID] = VersionID;
        }

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
/*
            else if (thread == "Event")
            {
                threadpool.QueueEvent(EventQueue, 1);
            }
 */
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
//            if (!EventProcessorIsRunning) //Can't check the count on this one, so poke it anyway
//                StartThread("Event");
        }
        #region Scripts events scheduler control

        public class aJob
            {
            public QueueItemStruct QIS;
            }


        private LinkedList<ScriptData> ScriptIDs = new LinkedList<ScriptData>();
        private LinkedList<ScriptData> SleepingScriptIDs = new LinkedList<ScriptData>();

        public void FlushEventSchQueue(ScriptData ID)
            {
            lock (ID.EventsProcData)
                {
                ID.EventsProcDataLocked = true;
                ID.EventsProcData.EventsQueue.Clear();
                ID.EventsProcDataLocked = false;
                }
            }

        public void ResetEventSchQueue(ScriptData ID)
            {
            lock (ID.EventsProcData)
                {
                ID.EventsProcDataLocked = true;
                ID.EventsProcData.EventsQueue.Clear();
                if (ID.EventsProcData.State == (int)ScriptEventsState.InExec)
                    ID.EventsProcData.State = (int)ScriptEventsState.InExecAbort;
                ID.EventsProcDataLocked = false;
                }
            }

        public void FlushAndExecNowEventSchQueue(ScriptData ID, string FunctionName, DetectParams[] qParams, int VersionID, EventPriority priority, params object[] param)
            {
            // Create a structure and add data
            QueueItemStruct QIS = new QueueItemStruct();

            int i;
            bool res;


            QIS.ID = ID;
            QIS.functionName = FunctionName;
            QIS.llDetectParams = qParams;
            QIS.param = param;
            QIS.VersionID = VersionID;

            lock (ID.EventsProcData)
                {
                ID.EventsProcDataLocked = true;
                ID.EventsProcData.EventsQueue.Clear();
                if (ID.EventsProcData.State == (int)ScriptEventsState.InExec)
                    ID.EventsProcData.State = (int)ScriptEventsState.InExecAbort;

                i = 0;

                while (i++ < 50)
                    {
                    try
                        {
                        res = EventSchProcessQIS(ref QIS);
                        }
                    catch
                        {
                        res = false;
                        }
                    if (res)
                        Thread.Sleep(20);
                    else
                        break;
                    }

                ID.EventsProcDataLocked = false;
                }
            }

        public void AddEventSchQueue(ScriptData ID, string FunctionName, DetectParams[] qParams, int VersionID, EventPriority priority, params object[] param)
            {
            QueueItemStruct QIS;

            if (!ID.SetEventParams(FunctionName, qParams)) // check events delay rules
                return;

            QIS = new QueueItemStruct();
            QIS.ID = ID;
            QIS.functionName = FunctionName;
            QIS.llDetectParams = qParams;
            QIS.param = param;
            QIS.VersionID = VersionID;

            lock (ID.EventsProcData)
                {
                ID.EventsProcDataLocked = true;
                if (!ID.InEventsProcData)
                    {
                    ID.EventsProcData.State = (int)ScriptEventsState.Idle;
                    ID.EventsProcData.thread = null;
                    ID.InEventsProcData = true;
                    }

                if (ID.EventsProcData.EventsQueue.Count > 100)
                    {
                    ID.EventsProcDataLocked = false;
                    return;
                    }

                ID.EventsProcData.EventsQueue.Enqueue(QIS);
                lock (ScriptIDs)
                    {
                    ScriptIDs.AddLast(ID);
                    }
                ID.EventsProcDataLocked = false;
                }

            lock (WorkersLock)
                {
                if (WorkersLock.nWorkers < MaxScriptThreads)
                    {
                    WorkersLock.nWorkers++;
                    threadpool.QueueEvent(loop, 2);
                    }
                }
            }

        public void AddEventSchQIS(QueueItemStruct QIS)
            {
            ScriptData ID;

            if (!QIS.ID.SetEventParams(QIS.functionName, QIS.llDetectParams)) // check events delay rules
                return;

            ID = QIS.ID;

            lock (ID.EventsProcData)
                {
                ID.EventsProcDataLocked = true;
                if (!ID.InEventsProcData)
                    {
                    ID.EventsProcData.State = (int)ScriptEventsState.Idle;
                    ID.EventsProcData.thread = null;
                    ID.InEventsProcData = true;
                    }

                if (ID.EventsProcData.EventsQueue.Count > 100)
                    {
                    ID.EventsProcDataLocked = false;
                    return;
                    }

                ID.EventsProcData.EventsQueue.Enqueue(QIS);
                lock (ScriptIDs)
                    {
                    ScriptIDs.AddLast(ID);
                    }
                ID.EventsProcDataLocked = false;
                }

            lock (WorkersLock)
                {
                if (WorkersLock.nWorkers < MaxScriptThreads)
                    {
                    WorkersLock.nWorkers++;
                    threadpool.QueueEvent(loop, 2);
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

            while (true)
                {
                // check one sleeper

                waslocked = false;

                ID = null;
                lock (SleepingScriptIDs)
                    {
                    if (SleepingScriptIDs.Count > 0)
                        {
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
                                        }
                                    }
                                else
                                    {
                                    lock (SleepingScriptIDs)
                                        {
                                        SleepingScriptIDs.AddLast(ID);
                                        }
                                    }
                                }
                            ID.EventsProcDataLocked = false;
                            }
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
                    if (ID.EventsProcDataLocked)
                        {
                        ScriptIDs.AddLast(ID);
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
                                ID.EventsProcDataLocked = false;
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
                                        ID.EventsProcDataLocked = false;
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
                                                }
                                            }
                                        else
                                            {
                                            lock (SleepingScriptIDs)
                                                {
                                                SleepingScriptIDs.AddLast(ID);
                                                }
                                            }
                                        ID.EventsProcDataLocked = false;
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
                                                lock (ScriptIDs)
                                                    {
                                                    ScriptIDs.AddLast(ID);
                                                    }
                                                }
                                            }
                                        else
                                            ID.InEventsProcData = false;

                                        ID.EventsProcDataLocked = false;
                                        break;

                                    case (int)ScriptEventsState.InExec:
                                    case (int)ScriptEventsState.InExecAbort:
                                        //                            if (Tnow < Ev.TimeCheck)
                                        lock (ScriptIDs)
                                            {
                                            ScriptIDs.AddLast(ID);
                                            }
                                        //                            else
                                        ID.EventsProcDataLocked = false;
                                        break;

                                    case (int)ScriptEventsState.Delete:

                                        lock (ScriptIDs)
                                            {
                                            ID.EventsProcData.EventsQueue.Clear();
                                            ID.InEventsProcData = false;
                                            }
                                        ID.EventsProcDataLocked = false;
                                        break;

                                    default:
                                        ID.EventsProcDataLocked = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                if (doID != null)
                    EventSchExec(ID);

                if (waslocked)
                    {
                    Thread.Sleep(20);
                    continue;
                    }

                WillSleep = false;

                lock (ScriptIDs)
                    {
                    if (ScriptIDs.Count == 0)
                        {
                        lock (SleepingScriptIDs)
                            {
                            lock (WorkersLock)
                                {
                                if (SleepingScriptIDs.Count < WorkersLock.nWorkers)
                                    break;
                                else
                                    WillSleep = true;
                                }
                            }
                        }
                    }

                if (WillSleep)
                    Thread.Sleep(20);
                }

            Interlocked.Decrement(ref WorkersLock.nWorkers);

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
                ID.EventsProcDataLocked = false;
                }

            bool res = EventSchProcessQIS(ref QIS);

            lock (ID.EventsProcData)
                {
                ID.EventsProcDataLocked = true;
                ID.EventsProcData.thread = null;

                if (!res || ID.EventsProcData.State == (int)ScriptEventsState.InExecAbort || ID.VersionID != QIS.VersionID)
                    {
                    ID.EventsProcData.State = (int)ScriptEventsState.Idle;
                    ID.EventsProcDataLocked = false;
                    return;
                    }

                ID.EventsProcData.CurExecQIS = QIS;

                if (QIS.CurrentlyAt.SleepTo.Ticks != 0)
                    {
                    ID.EventsProcData.TimeCheck = QIS.CurrentlyAt.SleepTo;
                    ID.EventsProcData.State = (int)ScriptEventsState.Sleep;
                    }
                else
                    ID.EventsProcData.State = (int)ScriptEventsState.Running;
                ID.EventsProcDataLocked = false;
                return;
                }

            return;
            }

        public bool EventSchProcessQIS(ref QueueItemStruct QIS)
            {
            try
                {
                Exception ex = null;
                EnumeratorInfo Running = QIS.ID.Script.ExecuteEvent(QIS.ID.State,
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