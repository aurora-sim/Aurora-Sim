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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;
using OpenMetaverse;
using log4net;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Scripting;
using OpenSim.Region.ScriptEngine.Shared;
using OpenSim.Region.ScriptEngine.Shared.CodeTools;

namespace OpenSim.Region.ScriptEngine.DotNetEngine
{
    // Because every thread needs some data set for it
    // (time started to execute current function), it will do its work
    // within a class
    public class EventQueueThreadClass : System.MarshalByRefObject
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // How many ms to sleep if queue is empty
        private static int nothingToDoSleepms;// = 50;
        private static ThreadPriority MyThreadPriority;

        public long LastExecutionStarted;
        public bool InExecution = false;
        public bool KillCurrentScript = false;

        //private EventQueueManager eventQueueManager;
        public Thread EventQueueThread;
        private static int ThreadCount = 0;

        private string ScriptEngineName = "ScriptEngine.Common";

        public EventQueueThreadClass()//EventQueueManager eqm
        {
            CultureInfo USCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = USCulture;

            //eventQueueManager = eqm;
            ReadConfig();
            Start();
        }

        ~EventQueueThreadClass()
        {
            Stop();
        }

        public void ReadConfig()
        {
            lock (ScriptEngine.ScriptEngines)
            {
                foreach (ScriptEngine m_ScriptEngine in
                        ScriptEngine.ScriptEngines)
                {
                    ScriptEngineName = m_ScriptEngine.ScriptEngineName;
                    nothingToDoSleepms =
                            m_ScriptEngine.ScriptConfigSource.GetInt(
                            "SleepTimeIfNoScriptExecutionMs", 50);

                    string pri = m_ScriptEngine.ScriptConfigSource.GetString(
                            "ScriptThreadPriority", "BelowNormal");

                    switch (pri.ToLower())
                    {
                        case "lowest":
                            MyThreadPriority = ThreadPriority.Lowest;
                            break;
                        case "belownormal":
                            MyThreadPriority = ThreadPriority.BelowNormal;
                            break;
                        case "normal":
                            MyThreadPriority = ThreadPriority.Normal;
                            break;
                        case "abovenormal":
                            MyThreadPriority = ThreadPriority.AboveNormal;
                            break;
                        case "highest":
                            MyThreadPriority = ThreadPriority.Highest;
                            break;
                        default:
                            MyThreadPriority = ThreadPriority.BelowNormal;
                            m_log.Error(
                                "[ScriptEngine.DotNetEngine]: Unknown "+
                                "priority type \"" + pri +
                                "\" in config file. Defaulting to "+
                                "\"BelowNormal\".");
                            break;
                    }
                }
            }
            // Now set that priority
            if (EventQueueThread != null)
                if (EventQueueThread.IsAlive)
                    EventQueueThread.Priority = MyThreadPriority;
        }

        /// <summary>
        /// Start thread
        /// </summary>
        private void Start()
        {
            EventQueueThread = Watchdog.StartThread(EventQueueThreadLoop, "EventQueueManagerThread_" + ThreadCount, MyThreadPriority, true);
            ThreadCount++;
        }

        public void Stop()
        {
            Watchdog.RemoveThread();
            if (EventQueueThread != null && EventQueueThread.IsAlive == true)
            {
                try
                {
                    EventQueueThread.Abort();               // Send abort
                }
                catch (Exception)
                {
                }
            }
        }

        // Queue processing thread loop
        private void EventQueueThreadLoop()
        {
            CultureInfo USCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = USCulture;

            try
            {
                while (true)
                {
                    DoProcessQueue();
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat(
                    "[{0}]: Event queue thread terminating with {1}.",
                    ScriptEngineName, e);
                EventQueueThread.Abort();
            }
        }


        public void DoProcessQueue()
        {
            try
            {
                foreach (ScriptEngine m_ScriptEngine in ScriptEngine.ScriptEngines)
                {
                    if (m_ScriptEngine.m_EventQueueManager == null ||
                            m_ScriptEngine.m_EventQueueManager.eventQueue == null)
                        continue;

                    if (m_ScriptEngine.m_EventQueueManager.eventQueue.Count != 0)
                    {
                        // Something in queue, process
                        lock (m_ScriptEngine.m_EventQueueManager.eventQueue)
                        {
                            for (int qc = 0; qc < m_ScriptEngine.m_EventQueueManager.eventQueue.Count; qc++)
                            {
                                // Get queue item
                                QueueItemStruct QIS = m_ScriptEngine.m_EventQueueManager.eventQueue.Dequeue();
                                if (m_ScriptEngine.World.PipeEventsForScript(
                                    QIS.localID))
                                {
                                    m_threads.Add(m_ScriptEngine.m_ScriptManager.ExecuteEvent(
                                        QIS.localID,
                                        QIS.itemID,
                                        QIS.functionName,
                                        QIS.llDetectParams,
                                        QIS.param).GetEnumerator());
                                }
                                else
                                {
                                    m_ScriptEngine.m_EventQueueManager.eventQueue.Enqueue(QIS, QIS.itemID);
                                }
                            }
                        }
                    }
                    
                    lock (m_threads)
                    {
                        if (m_threads.Count == 0)
                            return;
                        try
                        {
                            int i = 0;
                            while (m_threads.Count > 0 && i < m_threads.Count)
                            {
                                i++;

                                bool running = m_threads[i % m_threads.Count].MoveNext();


                                if (!running)
                                {
                                    m_threads.Remove(m_threads[i % m_threads.Count]);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            m_log.InfoFormat("[{0}]: Handled exception in the Event Queue: " + ex, ScriptEngineName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.WarnFormat("[{0}]: Unhandled exception in the Event Queue: " + ex, ScriptEngineName);
            }
        }

        private readonly List<IEnumerator> m_threads = new List<IEnumerator>();
    }
}
