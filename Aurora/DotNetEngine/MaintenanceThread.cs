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
using System.Threading;
using log4net;
using OpenSim.Framework;

namespace OpenSim.Region.ScriptEngine.DotNetEngine
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
		private ScriptEngine m_ScriptEngine;

        public MaintenanceThread(ScriptEngine Engine)
        {
        	m_ScriptEngine = Engine;
            ReadConfig();

            // Start maintenance thread
            StartMaintenanceThread();
        }

        ~MaintenanceThread()
        {
            StopMaintenanceThread();
        }

        public void ReadConfig()
        {
        	// Bad hack, but we need a m_ScriptEngine :)
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
                    	Thread.Sleep(MaintenanceLoopms); // Sleep before next pass

                    	// LOAD / UNLOAD SCRIPTS
                    	if (m_ScriptEngine.m_ScriptManager != null)
                    	{
                    		m_ScriptEngine.m_ScriptManager.DoScriptsLoadUnload();
                    	}

                        foreach(OpenMetaverse.UUID item in Resumeable)
                        {
                            m_ScriptEngine.ResumeScript(item);
                        }
                    	
                    	//Checks the Event Queue threads to make sure they are alive.
                    	m_ScriptEngine.m_EventQueueManager.CheckThreads();
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
        #endregion

        List<OpenMetaverse.UUID> Resumeable = new List<OpenMetaverse.UUID>();
        //This just lets the maintenance thread pick up the slack for finding the scripts that need to be resumed.
        internal void AddResumeScript(OpenMetaverse.UUID itemID)
        {
            if(!Resumeable.Contains(itemID))
                Resumeable.Add(itemID);
        }
    }
}
