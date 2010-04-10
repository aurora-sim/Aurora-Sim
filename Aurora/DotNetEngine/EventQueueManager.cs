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
using OpenMetaverse;
using OpenSim.Region.ScriptEngine.Shared;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.Framework.Scenes;
using log4net;

namespace OpenSim.Region.ScriptEngine.DotNetEngine
{
    /// <summary>
    /// EventQueueManager handles event queues
    /// Events are queued and executed in separate thread
    /// </summary>
    [Serializable]
    public class EventQueueManager
    {
        //
        // Class is instanced in "ScriptEngine" and used by "EventManager" which is also instanced in "ScriptEngine".
        //
        // Class purpose is to queue and execute functions that are received by "EventManager":
        //   - allowing "EventManager" to release its event thread immediately, thus not interrupting server execution.
        //   - allowing us to prioritize and control execution of script functions.
        // Class can use multiple threads for simultaneous execution. Mutexes are used for thread safety.
        //
        // 1. Hold an execution queue for scripts
        // 2. Use threads to process queue, each thread executes one script function on each pass.
        // 3. Catch any script error and process it
        //
        //
        // Notes:
        // * Current execution load balancing is optimized for 1 thread, and can cause unfair execute balancing between scripts.
        //   Not noticeable unless server is under high load.
        //

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ScriptEngine m_ScriptEngine;

        /// <summary>
        /// List of threads (classes) processing event queue
        /// Note that this may or may not be a reference to a static object depending on PrivateRegionThreads config setting.
        /// </summary>
        internal static List<EventQueueThreadClass> eventQueueThreads = new List<EventQueueThreadClass>();                             // Thread pool that we work on
        /// <summary>
        /// Locking access to eventQueueThreads AND staticGlobalEventQueueThreads.
        /// </summary>
//        private object eventQueueThreadsLock = new object();
        // Static objects for referencing the objects above if we don't have private threads:
        //internal static List<EventQueueThreadClass> staticEventQueueThreads;                // A static reference used if we don't use private threads
//        internal static object staticEventQueueThreadsLock;                                 // Statick lock object reference for same reason

        /// <summary>
        /// Global static list of all threads (classes) processing event queue -- used by max enforcment thread
        /// </summary>
        //private List<EventQueueThreadClass> staticGlobalEventQueueThreads = new List<EventQueueThreadClass>();

        /// <summary>
        /// Used internally to specify how many threads should exit gracefully
        /// </summary>
        public static int ThreadsToExit;
        public static object ThreadsToExitLock = new object();


        //public object queueLock = new object(); // Mutex lock object

        /// <summary>
        /// How many threads to process queue with
        /// </summary>
        internal static int numberOfThreads;

        internal static int EventExecutionMaxQueueSize;

        /// <summary>
        /// Maximum time one function can use for execution before we perform a thread kill.
        /// </summary>
        private static int maxFunctionExecutionTimems
        {
            get { return (int)(maxFunctionExecutionTimens / 10000); }
            set { maxFunctionExecutionTimens = value * 10000; }
        }

        /// <summary>
        /// Contains nanoseconds version of maxFunctionExecutionTimems so that it matches time calculations better (performance reasons).
        /// WARNING! ONLY UPDATE maxFunctionExecutionTimems, NEVER THIS DIRECTLY.
        /// </summary>
        public static long maxFunctionExecutionTimens;
        
        /// <summary>
        /// Enforce max execution time
        /// </summary>
        public static bool EnforceMaxExecutionTime;
        
        /// <summary>
        /// Kill script (unload) when it exceeds execution time
        /// </summary>
        private static bool KillScriptOnMaxFunctionExecutionTime;

        /// <summary>
        /// Queue containing events waiting to be executed
        /// </summary>
        public ScriptEventQueue<QueueItemStruct> eventQueue = new ScriptEventQueue<QueueItemStruct>();
        public Queue<QueueItemStruct> EventQueue2 = new Queue<QueueItemStruct>();
        public List<UUID> NeedsRemoved = new List<UUID>();
        public Scene m_scene;
        
        #region " Initialization / Startup "
        public EventQueueManager(ScriptEngine _ScriptEngine, Scene scene)
        {
            m_ScriptEngine = _ScriptEngine;
            m_scene = scene;
            ReadConfig();
            AdjustNumberOfScriptThreads();
        }

        public void ReadConfig()
        {
            // Refresh config
            numberOfThreads = m_ScriptEngine.ScriptConfigSource.GetInt("NumberOfScriptThreads", 2);
            maxFunctionExecutionTimems = m_ScriptEngine.ScriptConfigSource.GetInt("MaxEventExecutionTimeMs", 5000);
            EnforceMaxExecutionTime = m_ScriptEngine.ScriptConfigSource.GetBoolean("EnforceMaxEventExecutionTime", true);
            KillScriptOnMaxFunctionExecutionTime = m_ScriptEngine.ScriptConfigSource.GetBoolean("DeactivateScriptOnTimeout", false);
            EventExecutionMaxQueueSize = m_ScriptEngine.ScriptConfigSource.GetInt("EventExecutionMaxQueueSize", 300);

            // Now refresh config in all threads
            lock (eventQueueThreads)
            {
                foreach (EventQueueThreadClass EventQueueThread in eventQueueThreads)
                {
                    EventQueueThread.ReadConfig();
                }
            }
        }

        #endregion

        #region " Shutdown all threads "
        ~EventQueueManager()
        {
            Stop();
        }

        private void Stop()
        {
            if (eventQueueThreads != null)
            {
                // Kill worker threads
                lock (eventQueueThreads)
                {
                    foreach (EventQueueThreadClass EventQueueThread in eventQueueThreads)
                    {
                        AbortThreadClass(EventQueueThread);
                    }
                }
            }

            // Remove all entries from our event queue
            lock (EventQueue2)
            {
            	EventQueue2.Clear();
            }
        }

        #endregion

        #region " Start / stop script execution threads (ThreadClasses) "
        
        private void StartNewThreadClass()
        {
            EventQueueThreadClass eqtc = new EventQueueThreadClass(m_ScriptEngine);
            eventQueueThreads.Add(eqtc);
        }

        private void AbortThreadClass(EventQueueThreadClass threadClass)
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
        #endregion
        
        #region " Add/Remove events to execution queue "
        
        /// <summary>
        /// Posts event to all objects in the group.
        /// </summary>
        /// <param name="localID">Region object ID</param>
        /// <param name="FunctionName">Name of the function, will be state + "_event_" + FunctionName</param>
        /// <param name="param">Array of parameters to match event mask</param>
        public bool AddToObjectQueue(uint localID, string FunctionName, DetectParams[] qParams, params object[] param)
        {
            // Determine all scripts in Object and add to their queue
            IInstanceData[] datas = m_ScriptEngine.ScriptProtection.GetScript(localID);
            
            if(datas == null)
            	//No scripts to post to... so it is firing all the events it needs to
            	return true;
            
            foreach (IInstanceData ID in datas)
            {
                // Add to each script in that object
                AddToScriptQueue((InstanceData)ID, FunctionName, qParams, param);
            }
            return true;
        }

        /// <summary>
        /// Posts event to the given object.
        /// NOTE: Use AddToScriptQueue with InstanceData instead if possible.
        /// </summary>
        /// <param name="localID">Region object ID</param>
        /// <param name="itemID">Region script ID</param>
        /// <param name="FunctionName">Name of the function, will be state + "_event_" + FunctionName</param>
        /// <param name="param">Array of parameters to match event mask</param>
        public bool AddToScriptQueue(uint localID, UUID itemID, string FunctionName, DetectParams[] qParams, params object[] param)
        {
            lock (EventQueue2)
            {
                if (EventQueue2.Count >= EventExecutionMaxQueueSize)
                {
                    m_log.WarnFormat("[{0}]: Event Queue is above the MaxQueueSize.", m_ScriptEngine.ScriptEngineName);
                    return false;
                }

                InstanceData id = m_ScriptEngine.m_ScriptManager.GetScript(localID, itemID);
                if (id == null)
                {
                	m_log.Warn("RETURNING FALSE IN ASQ");
                	return false;
                }

                return AddToScriptQueue(id, FunctionName, qParams, param);
            }
        }

        /// <summary>
        /// Posts the event to the given object.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="FunctionName"></param>
        /// <param name="qParams"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool AddToScriptQueue(InstanceData ID, string FunctionName, DetectParams[] qParams, params object[] param)
        {
            lock (EventQueue2)
            {
                if (EventQueue2.Count >= EventExecutionMaxQueueSize)
                {
                    m_log.WarnFormat("[{0}]: Event Queue is above the MaxQueueSize.", m_ScriptEngine.ScriptEngineName);
                    return false;
                }
                // Create a structure and add data
                QueueItemStruct QIS = new QueueItemStruct();
                QIS.ID = ID;
                QIS.functionName = FunctionName;
                QIS.llDetectParams = qParams;
                QIS.param = param;
                QIS.LineMap = ID.LineMap;
                if (m_ScriptEngine.World.PipeEventsForScript(
                	QIS.ID.localID))
                {
                	// Add it to queue
                	EventQueue2.Enqueue(QIS);
                }
            }
            return true;
        }

        /// <summary>
        /// Prepares to remove the script from the event queue.
        /// </summary>
        /// <param name="itemID"></param>
        public void RemoveFromQueue(UUID itemID)
        {
        	NeedsRemoved.Add(itemID);
        }

        #endregion

        #region " Maintenance thread "

        /// <summary>
        /// Adjust number of script thread classes. It can start new, but if it needs to stop it will just set number of threads in "ThreadsToExit" and threads will have to exit themselves.
        /// Called from MaintenanceThread
        /// </summary>
        public void AdjustNumberOfScriptThreads()
        {
            // Is there anything here for us to do?
            if (eventQueueThreads.Count == numberOfThreads)
                return;

            lock (eventQueueThreads)
            {
                int diff = numberOfThreads - eventQueueThreads.Count;
                // Positive number: Start
                // Negative number: too many are running
                if (diff > 0)
                {
                    // We need to add more threads
                    for (int ThreadCount = eventQueueThreads.Count; ThreadCount < numberOfThreads; ThreadCount++)
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
                foreach (EventQueueThreadClass EventQueueThread in eventQueueThreads)
                {
                    // Is thread currently executing anything?
                    if (EventQueueThread.InExecution)
                    {
                        // Has execution time expired?
                        if (DateTime.Now.Ticks - EventQueueThread.LastExecutionStarted >
                            maxFunctionExecutionTimens)
                        {
                            // Yes! We need to kill this thread!

                            // Set flag if script should be removed or not
                            EventQueueThread.KillCurrentScript = KillScriptOnMaxFunctionExecutionTime;

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
            	if(eventQueueThreads[i].EventQueueThread == null)
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

        #endregion
    }

    #region " Queue structures "
    /// <summary>
    /// Queue item structure
    /// </summary>
    public class QueueItemStruct
    {
        public InstanceData ID;
        public string functionName;
        public DetectParams[] llDetectParams;
        public object[] param;
        public Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>>
                LineMap;
    }

    public class ScriptEventQueue<T>: IEnumerable<T>
    {
        private List<T> queue;
        private List<UUID> queueIDs; // ItemID
        
        public ScriptEventQueue()
        {
            queue = new List<T>();
            queueIDs = new List<UUID>();
        }

        public void Clear()
        {
            queue.Clear();
            queueIDs.Clear();
        }

        public void Enqueue(T queueItem, UUID ID)
        {
            queue.Add(queueItem);
            queueIDs.Add(ID);
        }

        public T Dequeue()
        {
            int i = 0;
            lock (queueIDs)
            {
                lock (queue)
                {
                    if (queue.Count != 0)
                    {
                        T queueItem = queue.GetRange(0, 1)[0];
                        queue.RemoveAt(i);
                        queueIDs.RemoveAt(i);
                        return queueItem;
                    }
                    return default(T);
                }
            }
        }

        public void Remove(UUID queueID)
        {
            lock (queueIDs)
            {
                lock (queue)
                {
                    int i = 0;
                    while(i < queueIDs.Count)
                    {
                        if (queueIDs[i] == queueID)
                        {
                            queue.RemoveAt(i);
                            queueIDs.RemoveAt(i);
                        }
                        i++;
                    }
                }
            }
        }
        public int Count
        {
            get { return queue.Count; }
        }

        public int CountOf(UUID itemID)
        {
            int i = 0;
            int count = 0;
            foreach (UUID queueID in queueIDs)
            {
                if (queueID == itemID)
                {
                    count++;
                }
                i++;
            }
            return count;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return queue.GetEnumerator();
        }

        internal T Dequeue(UUID itemID)
        {
            int i = 0;
            lock (queueIDs)
            {
                lock (queue)
                {
                    while (i < queue.Count)
                    {
                        UUID queueID = queueIDs.GetRange(i, 1)[0];
                        if (queueID == itemID)
                        {
                            T queueItem = queue.GetRange(i, 1)[0];
                            queue.RemoveAt(i);
                            queueIDs.RemoveAt(i);
                            return queueItem;
                        }
                        i++;
                    }
                }
            }
            return default(T);
        }
        #endregion

    }
}
