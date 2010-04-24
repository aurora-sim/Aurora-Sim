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

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    /// <summary>
    /// This class does maintenance on script engine.
    /// </summary>
    public class MaintenanceThread
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //public ScriptEngine m_ScriptEngine;
        private int MaintenanceLoopms;
        private int MaintenanceLoopTicks_ScriptLoadUnload;
        private int MaintenanceLoopTicks_Other;
        /// <summary>
        /// Used internally to specify how many threads should exit gracefully
        /// </summary>
        public static int ThreadsToExit;
        public static object ThreadsToExitLock = new object();
        private ScriptEngine m_ScriptEngine;
        internal static List<EventQueue> eventQueueThreads = new List<EventQueue>();                             // Thread pool that we work on
        private int SleepTime = 250;
        private int numberOfEventQueueThreads = 1;

        public MaintenanceThread(ScriptEngine Engine)
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            m_ScriptEngine = Engine;
            numberOfEventQueueThreads = m_ScriptEngine.numberOfEventQueueThreads;
            ReadConfig();

            // Start maintenance thread
            StartMaintenanceThread();
            AdjustNumberOfScriptThreads();
        }

        ~MaintenanceThread()
        {
            StopMaintenanceThread();
        }

        public void ReadConfig()
        {
        	// Bad hack, but we need a m_ScriptEngine :)
            SleepTime = m_ScriptEngine.ScriptConfigSource.GetInt("SleepTimeBetweenLoops", 250);
        	MaintenanceLoopms = m_ScriptEngine.ScriptConfigSource.GetInt("MaintenanceLoopms", 50);
        	MaintenanceLoopTicks_ScriptLoadUnload =
        		m_ScriptEngine.ScriptConfigSource.GetInt("MaintenanceLoopTicks_ScriptLoadUnload", 1);
        	MaintenanceLoopTicks_Other =
        		m_ScriptEngine.ScriptConfigSource.GetInt("MaintenanceLoopTicks_Other", 10);
        }

        #region " Maintenance thread "
        
        /// <summary>
        /// Maintenance thread. Enforcing max execution time for example.
        /// </summary>
        public Thread MaintenanceThreadThread;

        /// <summary>
        /// Starts maintenance thread
        /// </summary>
        private void StartMaintenanceThread()
        {
            if (MaintenanceThreadThread == null)
            {
                MaintenanceThreadThread = Watchdog.StartThread(MaintenanceLoop, "ScriptMaintenanceThread", ThreadPriority.Normal, true);
            }
        }

        /// <summary>
        /// Stops maintenance thread
        /// </summary>
        private void StopMaintenanceThread()
        {
            try
            {
                if (MaintenanceThreadThread != null && MaintenanceThreadThread.IsAlive)
                {
                    MaintenanceThreadThread.Abort();
                }
            }
            catch (Exception ex)
            {
                m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: Exception stopping maintenence thread: " + ex.ToString());
            }
        }

        /// <summary>
        /// A thread should run in this loop and check all running scripts
        /// </summary>
        public void MaintenanceLoop()
        {
            while (true)
            {
                try
                {
                    while (true)
                    {
                    	Thread.Sleep(MaintenanceLoopms * 200); // Sleep before next pass

                    	// LOAD / UNLOAD SCRIPTS
                        lock (m_ScriptEngine.LUQueue)
                        {
                            if (m_ScriptEngine.LUQueue.Count > 0)
                            {
                                DoScriptsLoadUnload();
                            }
                        }
                        if (m_ScriptEngine.StateQueue.Count != 0)
                        {
                            DoStateQueue();
                        }

                        for (int i = 0; i < Resumeable.Count; i++)
                        {
                            m_ScriptEngine.ResumeScript(Resumeable[i]);
                        }
                        AdjustNumberOfScriptThreads();
                    	//Checks the Event Queue threads to make sure they are alive.
                    	CheckThreads();
                    }
                }
                catch(ThreadAbortException)
                {
                    m_log.Error("Thread aborted in MaintenanceLoopThread.  If this is during shutdown, please ignore");
                }
                catch (Exception ex)
                {
                    m_log.ErrorFormat("Exception in MaintenanceLoopThread. Thread will recover after 5 sec throttle. Exception: {0}", ex.ToString());
                }
            }
        }
        public void DoStateQueue()
        {
            List<IEnumerator> Parts = new List<IEnumerator>();
            while (m_ScriptEngine.StateQueue.Count != 0)
            {
                StateQueueItem item = m_ScriptEngine.StateQueue.Dequeue();
                if (item.Create)
                    //Parts.Add(item.ID.Serialize());
                    item.ID.SerializeDatabase();
                else
                    RemoveState(item.ID);
                lock (Parts)
                {
                    int i = 0;
                    while (Parts.Count > 0 && i < 1000)
                    {
                        i++;

                        bool running = false;
                        try
                        {
                            running = Parts[i % Parts.Count].MoveNext();
                        }
                        catch (Exception ex)
                        {
                            m_log.Error(ex);
                        }

                        if (!running)
                            Parts.Remove(Parts[i % Parts.Count]);
                    }
                }
            }
        }

        public void RemoveState(ScriptData ID)
        {
            string savedState = Path.Combine(Path.GetDirectoryName(ID.AssemblyName),
                    ID.ItemID.ToString() + ".state");
            try
            {
                if (File.Exists(savedState))
                {
                    File.Delete(savedState);
                }
            }
            catch (Exception) { }
        }
        #endregion

        List<OpenMetaverse.UUID> Resumeable = new List<OpenMetaverse.UUID>();
        //This just lets the maintenance thread pick up the slack for finding the scripts that need to be resumed.
        internal void AddResumeScript(OpenMetaverse.UUID itemID)
        {
            if(!Resumeable.Contains(itemID))
                Resumeable.Add(itemID);
        }

        internal void RemoveResumeScript(OpenMetaverse.UUID itemID)
        {
            if (Resumeable.Contains(itemID))
                Resumeable.Remove(itemID);
        }

        private void StartNewThreadClass()
        {
            EventQueue eqtc = new EventQueue(m_ScriptEngine);
            eventQueueThreads.Add(eqtc);
            m_ScriptEngine.EventQueueThreadCount++;
        }

        private void AbortThreadClass(EventQueue threadClass)
        {
            if (eventQueueThreads.Contains(threadClass))
                eventQueueThreads.Remove(threadClass);

            try
            {
                threadClass.Stop();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Adjust number of script thread classes. It can start new, but if it needs to stop it will just set number of threads in "ThreadsToExit" and threads will have to exit themselves.
        /// Called from MaintenanceThread
        /// </summary>
        public void AdjustNumberOfScriptThreads()
        {
            // Is there anything here for us to do?
            if (eventQueueThreads.Count == numberOfEventQueueThreads)
                return;

            lock (eventQueueThreads)
            {
                int diff = numberOfEventQueueThreads - eventQueueThreads.Count;
                // Positive number: Start
                // Negative number: too many are running
                if (diff > 0)
                {
                    // We need to add more threads
                    for (int ThreadCount = eventQueueThreads.Count; ThreadCount < numberOfEventQueueThreads; ThreadCount++)
                    {
                        StartNewThreadClass();
                    }
                }
                if (diff < 0)
                {
                    // We need to kill some threads
                    lock (ThreadsToExitLock)
                    {
                        ThreadsToExit = Math.Abs(diff);
                    }
                }
            }
        }

        /// <summary>
        /// Check if any thread class has been executing an event too long
        /// </summary>
        public void CheckScriptMaxExecTime()
        {
            // Iterate through all ScriptThreadClasses and check how long their current function has been executing
            lock (eventQueueThreads)
            {
                foreach (EventQueue EventQueueThread in eventQueueThreads)
                {
                    // Is thread currently executing anything?
                    if (EventQueueThread.InExecution)
                    {
                        // Has execution time expired?
                        if (DateTime.Now.Ticks - EventQueueThread.LastExecutionStarted >
                            m_ScriptEngine.maxFunctionExecutionTimens)
                        {
                            // Yes! We need to kill this thread!

                            // Set flag if script should be removed or not
                            EventQueueThread.KillCurrentScript = m_ScriptEngine.KillScriptOnMaxFunctionExecutionTime;

                            // Abort this thread
                            AbortThreadClass(EventQueueThread);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks the event queue threads to see if they have been aborted.
        /// </summary>
        internal void CheckThreads()
        {
            int i = 0;
            while (i < eventQueueThreads.Count)
            {
                if (eventQueueThreads[i].EventQueueThread == null)
                {
                    i++;
                    continue;
                }
                if (!eventQueueThreads[i].EventQueueThread.IsAlive)
                {
                    m_log.WarnFormat("[{0}]: EventQueue Thread found dead... Restarting.", m_ScriptEngine.ScriptEngineName);
                    AbortThreadClass(eventQueueThreads[i]);
                    StartNewThreadClass();
                }
                i++;
            }
        }
        /// <summary>
        /// Main Loop that starts/stops all scripts in the LUQueue.
        /// </summary>
        public void DoScriptsLoadUnload()
        {
            List<IEnumerator> StartParts = new List<IEnumerator>();
            List<IEnumerator> StopParts = new List<IEnumerator>();
            List<IEnumerator> ReuploadParts = new List<IEnumerator>();
            List<ScriptData> FireEvents = new List<ScriptData>();
            lock (m_ScriptEngine.LUQueue)
            {
                if (m_ScriptEngine.LUQueue.Count > 0)
                {
                    int i = 0;
                    while (i < m_ScriptEngine.LUQueue.Count)
                    {
                        LUStruct item = m_ScriptEngine.LUQueue.Dequeue();

                        if (item.Action == LUType.Unload)
                        {
                            item.ID.CloseAndDispose();
                        }
                        else if (item.Action == LUType.Load)
                        {
                            FireEvents.Add(item.ID);
                            try
                            {
                                item.ID.Start(false);
                            }
                            catch (Exception) { }
                        }
                        else if (item.Action == LUType.Reupload)
                        {
                            FireEvents.Add(item.ID);
                            try
                            {
                                item.ID.Start(true);
                            }
                            catch (Exception) { }
                        }
                        i++;
                    }
                }
            }
            foreach (ScriptData ID in FireEvents)
            {
                ID.FireEvents();
            }
            FireEvents.Clear();
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.GetExecutingAssembly().FullName == args.Name ? Assembly.GetExecutingAssembly() : null;
        }
    }
}
