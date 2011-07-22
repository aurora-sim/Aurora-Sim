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

//#define Debug

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
        public AuroraThreadPool cmdThreadpool = null;
        public AuroraThreadPool scriptChangeThreadpool = null;
        public AuroraThreadPool scriptThreadpool = null;
        public bool ScriptChangeIsRunning = false;
        public bool EventProcessorIsRunning = false;
        public Int64 CmdHandlerQueueIsRunning = 0;
        public bool RunInMainProcessingThread = false;
        public bool m_Started = false;

        private const int EMPTY_WORK_KILL_THREAD_TIME = 250;
        private Queue<QueueItemStruct> ScriptEvents = new Queue<QueueItemStruct> ();
        private float EventPerformance = 0.1f;
        private int ScriptEventCount = 0;
        private int m_CheckingEvents = 0;
        private Mischel.Collections.PriorityQueue<QueueItemStruct, Int64> SleepingScriptEvents = new Mischel.Collections.PriorityQueue<QueueItemStruct, Int64> (10, DateTimeComparer);
        private int SleepingScriptEventCount = 0;
        private DateTime NextSleepersTest = DateTime.Now;
        private int m_CheckingSleepers = 0;

        private static int DateTimeComparer (Int64 a, Int64 b)
        {
            return b.CompareTo (a);
        }

        public int MaxScriptThreads = 1;

        public bool Started
        {
            get { return m_Started; }
            set
            {
                m_Started = true;

                scriptChangeThreadpool.QueueEvent (ScriptChangeQueue, 2);
                //Start the queue because it can't start itself
                cmdThreadpool.ClearEvents ();
                cmdThreadpool.QueueEvent (CmdHandlerQueue, 2);
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
            info.Threads = 1;
            info.MaxSleepTime = Engine.Config.GetInt ("SleepTime", 100);
            info.SleepIncrementTime = Engine.Config.GetInt ("SleepIncrementTime", 1);
            info.Name = "Script Cmd Thread Pools";
            cmdThreadpool = new AuroraThreadPool (info);
            info.Name = "Script Loading Thread Pools";
            scriptChangeThreadpool = new AuroraThreadPool (info);


            MaxScriptThreads = Engine.Config.GetInt("Threads", 100); // leave control threads out of user option
            AuroraThreadPoolStartInfo sinfo = new AuroraThreadPoolStartInfo();
            sinfo.priority = ThreadPriority.Normal;
            sinfo.Threads = MaxScriptThreads;
            sinfo.MaxSleepTime = Engine.Config.GetInt ("SleepTime", 100);
            sinfo.SleepIncrementTime = Engine.Config.GetInt ("SleepIncrementTime", 1);
            sinfo.KillThreadAfterQueueClear = true;
            sinfo.Name = "Script Event Thread Pools";
            scriptThreadpool = new AuroraThreadPool (sinfo);
            
            AppDomain.CurrentDomain.AssemblyResolve += m_ScriptEngine.AssemblyResolver.OnAssemblyResolve;
        }

        #endregion

        #region Loops

        /// <summary>
        /// This loop deals with starting and stoping scripts
        /// </summary>
        /// <returns></returns>
        public void ScriptChangeQueue()
        {
            if (m_ScriptEngine.Worlds.Count == 0)
                return;

            IMonitorModule module = m_ScriptEngine.Worlds[0].RequestModuleInterface<IMonitorModule>();
            int StartTime = Util.EnvironmentTickCount();

            if (!Started) //Break early
                return;

            if (m_ScriptEngine.ConsoleDisabled || m_ScriptEngine.Disabled)
                return;

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
                scriptChangeThreadpool.QueueEvent (ScriptChangeQueue, 2); //Requeue us
                Thread.Sleep(5);
                return;
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
                    ITimeMonitor scriptMonitor = (ITimeMonitor)module.GetMonitor(scene.RegionInfo.RegionID.ToString(), MonitorModuleHelper.ScriptFrameTime);
                    scriptMonitor.AddTime(Util.EnvironmentTickCountSubtract(StartTime));
                }
            }
        }

        public void CmdHandlerQueue()
        {
            if (m_ScriptEngine.Worlds.Count == 0)
            {
                Interlocked.Exchange (ref CmdHandlerQueueIsRunning, 0);
                return;
            }
            Interlocked.Exchange (ref CmdHandlerQueueIsRunning, 1);
            IMonitorModule module = m_ScriptEngine.Worlds[0].RequestModuleInterface<IMonitorModule>();
            int StartTime = Util.EnvironmentTickCount();

            if (!Started) //Break early
                return;

            if (m_ScriptEngine.ConsoleDisabled || m_ScriptEngine.Disabled)
                return;

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

            if (module != null)
            {
                foreach (Scene scene in m_ScriptEngine.Worlds)
                {
                    ITimeMonitor scriptMonitor = (ITimeMonitor)module.GetMonitor(scene.RegionInfo.RegionID.ToString(), MonitorModuleHelper.ScriptFrameTime);
                    scriptMonitor.AddTime(Util.EnvironmentTickCountSubtract(StartTime));
                }
            }

            if (didAnything) //If we did something, run us again soon
                cmdThreadpool.QueueEvent (CmdHandlerQueue, 2);
            else
                Interlocked.Exchange (ref CmdHandlerQueueIsRunning, 0);
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
                scriptChangeThreadpool.QueueEvent (ScriptChangeQueue, 2);
            }
            else if (thread == "CmdHandlerQueue" && Interlocked.Read(ref CmdHandlerQueueIsRunning) == 0)
            {
                cmdThreadpool.ClearEvents ();
                cmdThreadpool.QueueEvent (CmdHandlerQueue, 2);
            }
        }

        /// <summary>
        /// Makes sure that all the threads that need to be running are running and starts them if they need to be running
        /// </summary>
        public void PokeThreads()
        {
            if (LUQueue.Count () > 0 && !ScriptChangeIsRunning)
                StartThread ("Change");
            if (Interlocked.Read(ref CmdHandlerQueueIsRunning) == 0)
                StartThread ("CmdHandlerQueue");
        }

        public void DisableThreads ()
        {
            Interlocked.Exchange (ref CmdHandlerQueueIsRunning, 0);
            EventProcessorIsRunning = false;
            ScriptChangeIsRunning = false;
            cmdThreadpool.ClearEvents ();
            scriptChangeThreadpool.ClearEvents ();
            scriptThreadpool.ClearEvents ();
        }

        #endregion

        #region Scripts events scheduler control

        public void RemoveFromEventSchQueue(ScriptData ID, bool abortcur)
        {
            if (ID == null)
                return;

            //Ignore any events to be added after this
            ID.IgnoreNew = true;
            //Clear out the old events
            Interlocked.Increment (ref ID.VersionID);
        }

        public void SetEventSchSetIgnoreNew(ScriptData ID, bool yes)
        {
            if (ID == null)
                return;
            ID.IgnoreNew = yes;
        }

        public void AddEventSchQueue(ScriptData ID, string FunctionName, DetectParams[] qParams, EventPriority priority, params object[] param)
        {
            QueueItemStruct QIS;

            if (ID == null || ID.Script == null || ID.IgnoreNew)
                return;

            if (!ID.SetEventParams(FunctionName, qParams)) // check events delay rules
                return;

            QIS = new QueueItemStruct();
            QIS.EventsProcData = new ScriptEventsProcData ();
            QIS.ID = ID;
            QIS.functionName = FunctionName;
            QIS.llDetectParams = qParams;
            QIS.param = param;
            QIS.VersionID = Interlocked.Read(ref ID.VersionID);
            QIS.State = ID.State;
            QIS.CurrentlyAt = null;

            lock (ScriptEvents)
            {
                ScriptEvents.Enqueue (QIS);
                ScriptEventCount++;
#if Debug
                m_log.Warn (ScriptEventCount + ", " + QIS.functionName);
#endif
            }

            long threadCount = Interlocked.Read (ref scriptThreadpool.nthreads);
            if (threadCount == 0 || threadCount < (ScriptEventCount + (SleepingScriptEventCount / 2)) * EventPerformance)
            {
                scriptThreadpool.QueueEvent (eventLoop, 2);
            }
        }

        public void AddEventSchQIS(QueueItemStruct QIS)
        {
            if (QIS.ID == null || QIS.ID.Script == null || QIS.ID.IgnoreNew)
            {
                EventManager.EventComplete (QIS);
                return;
            }

            if (!QIS.ID.SetEventParams (QIS.functionName, QIS.llDetectParams)) // check events delay rules
            {
                EventManager.EventComplete (QIS);
                return;
            }

            QIS.CurrentlyAt = null;
            
            lock (ScriptEvents)
            {
                ScriptEvents.Enqueue (QIS);
                ScriptEventCount++;
#if Debug
                m_log.Warn (ScriptEventCount + ", " + QIS.functionName);
#endif
            }
            long threadCount = Interlocked.Read (ref scriptThreadpool.nthreads);
            if (threadCount == 0 || threadCount < (ScriptEventCount + (SleepingScriptEventCount / 2)) * EventPerformance)
            {
                scriptThreadpool.QueueEvent (eventLoop, 2);
            }
        }

        public void eventLoop()
        {
            int numberOfEmptyWork = 0;
            while (!m_ScriptEngine.ConsoleDisabled && !m_ScriptEngine.Disabled)
            {
            processMoreScripts:
                QueueItemStruct QIS = null;

                const int minNumScriptsToProcess = 50;
                int numScriptsProcessed = 0;
                //Check whether it is time, and then do the thread safety piece
                if (Interlocked.CompareExchange (ref m_CheckingSleepers, 1, 0) == 0)
                {
                    lock (SleepingScriptEvents)
                    {
                        if (SleepingScriptEvents.Count > 0)
                        {
                            QIS = SleepingScriptEvents.Dequeue ().Value;
                        restart:
                            if (QIS.RunningNumber > 2 && SleepingScriptEventCount > 0)
                            {
                                QIS.RunningNumber = 1;
                                SleepingScriptEvents.Enqueue (QIS, QIS.EventsProcData.TimeCheck.Ticks);
                                QIS = null;
                                QIS = SleepingScriptEvents.Dequeue ().Value;
                                goto restart;
                            }
                            else
                                SleepingScriptEventCount--;
                        }
                    }
                    if (QIS != null)
                    {
                        if (QIS.EventsProcData.TimeCheck.Ticks < DateTime.Now.Ticks)
                        {
                            DateTime NextTime = DateTime.MaxValue;
                            lock (SleepingScriptEvents)
                            {
                                if (SleepingScriptEvents.Count > 0)
                                    NextTime = SleepingScriptEvents.Peek ().Value.EventsProcData.TimeCheck;
                            }

                            //Now add in the next sleep time
                            NextSleepersTest = NextTime;

                            //All done
                            Interlocked.Exchange (ref m_CheckingSleepers, 0);
                            //Execute the event
                            EventSchExec (QIS);
                            numScriptsProcessed++;
                        }
                        else
                        {
                            NextSleepersTest = QIS.EventsProcData.TimeCheck;
                            lock (SleepingScriptEvents)
                            {
                                SleepingScriptEvents.Enqueue (QIS, QIS.EventsProcData.TimeCheck.Ticks);
                                SleepingScriptEventCount++;
                            }
                            //All done
                            Interlocked.Exchange (ref m_CheckingSleepers, 0);
                        }
                    }
                    else //No more left, don't check again
                    {
                        NextSleepersTest = DateTime.MaxValue;
                        //All done
                        Interlocked.Exchange (ref m_CheckingSleepers, 0);
                    }
                }
                QIS = null;
                int timeToSleep = 5;
                //If we can, get the next event
                if (Interlocked.CompareExchange (ref m_CheckingEvents, 1, 0) == 0)
                {
                    lock (ScriptEvents)
                    {
                        if (ScriptEvents.Count > 0)
                        {
                            QIS = ScriptEvents.Dequeue ();
                            ScriptEventCount--;
                            Interlocked.Exchange (ref m_CheckingEvents, 0);
                        }
                        else
                            Interlocked.Exchange (ref m_CheckingEvents, 0);
                    }
                    if (QIS != null)
                    {
                        EventSchExec (QIS);
                        numScriptsProcessed++;
                    }
                }
                //Process a bunch each time
                if (ScriptEventCount > 0 && numScriptsProcessed < minNumScriptsToProcess)
                    goto processMoreScripts;

                if (ScriptEventCount == 0 && NextSleepersTest.Ticks != DateTime.MaxValue.Ticks)
                    timeToSleep = (int)(NextSleepersTest - DateTime.Now).TotalMilliseconds;
                if (timeToSleep < 5)
                    timeToSleep = 5;
                if (timeToSleep > 50)
                    timeToSleep = 50;

                if (SleepingScriptEventCount == 0 && ScriptEventCount == 0)
                {
                    numberOfEmptyWork++;
                    if (numberOfEmptyWork > EMPTY_WORK_KILL_THREAD_TIME) //Don't break immediately, otherwise we have to wait to spawn more threads
                    {
                        break; //No more events, end
                    }
                    else
                        timeToSleep += 10;
                }
                else if (Interlocked.Read (ref scriptThreadpool.nthreads) > (ScriptEventCount + (int)(((float)SleepingScriptEventCount / 2f + 0.5f))) ||
                    Interlocked.Read (ref scriptThreadpool.nthreads) > MaxScriptThreads)
                {
                    numberOfEmptyWork++;
                    if (numberOfEmptyWork > (EMPTY_WORK_KILL_THREAD_TIME / 2)) //Don't break immediately
                    {
                        break; //Too many threads, kill some off
                    }
                    else
                        timeToSleep += 5;
                }
                else
                    numberOfEmptyWork /= 2; //Cut it down, but don't zero it out, as this may just be one event
#if Debug
                m_log.Warn (timeToSleep);
#endif
                Interlocked.Increment (ref scriptThreadpool.nSleepingthreads);
                Thread.Sleep (timeToSleep);
                Interlocked.Decrement (ref scriptThreadpool.nSleepingthreads);
            }
        }

        public void EventSchExec (QueueItemStruct QIS)
        {
            if (QIS.ID == null)
                return;

            if (!QIS.ID.Running)
            {
                //do only state_entry and on_rez
                if (QIS.functionName != "state_entry"
                    || QIS.functionName != "on_rez")
                {
                    return;
                }
            }
            
            //Check the versionID so that we can kill events
            if (QIS.functionName != "link_message" && 
                QIS.VersionID != Interlocked.Read(ref QIS.ID.VersionID))
            {
                m_log.WarnFormat ("FOUND BAD VERSION ID, OLD {0}, NEW {1}, FUNCTION NAME {2}", QIS.VersionID, Interlocked.Read(ref QIS.ID.VersionID), QIS.functionName);
                //return;
            }

            if (!EventSchProcessQIS(ref QIS)) //Execute the event
            {
                //All done
                QIS.EventsProcData.State = ScriptEventsState.Idle;
            }
            else
            {
                if (QIS.CurrentlyAt.SleepTo.Ticks != 0)
                {
                    QIS.EventsProcData.TimeCheck = QIS.CurrentlyAt.SleepTo;
                    QIS.EventsProcData.State = ScriptEventsState.Sleep;
                    //If it is greater, we need to check sooner for this one
                    if (NextSleepersTest.Ticks > QIS.CurrentlyAt.SleepTo.Ticks)
                        NextSleepersTest = QIS.CurrentlyAt.SleepTo;
                    lock (SleepingScriptEvents)
                    {
                        SleepingScriptEvents.Enqueue (QIS, QIS.CurrentlyAt.SleepTo.Ticks);
                        SleepingScriptEventCount++;
                    }
                }
                else
                {
                    QIS.EventsProcData.State = ScriptEventsState.Running;
                    lock (ScriptEvents)
                    {
                        this.ScriptEvents.Enqueue (QIS);
                        ScriptEventCount++;
                    }
                }
            }
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
                    QIS.RunningNumber++;
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
