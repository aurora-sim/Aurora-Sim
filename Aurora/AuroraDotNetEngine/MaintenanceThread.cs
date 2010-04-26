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
using Amib.Threading;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    /// <summary>
    /// This class does maintenance on script engine.
    /// </summary>
    public class MaintenanceThread
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ScriptEngine m_ScriptEngine;
        
        public MaintenanceThread(ScriptEngine Engine)
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            m_ScriptEngine = Engine;
            for (int i = 0; i <= Engine.NumberOfEventQueueThreads; i++)
            {
                EventQueue eqtc = new EventQueue(m_ScriptEngine);
                Engine.m_ThreadPool.QueueWorkItem(new WorkItemCallback(eqtc.DoProcessQueue), Engine.SleepTime);
            }
            for (int i = 0; i <= Engine.NumberOfStateSavingThreads; i++)
            {
                Engine.m_ThreadPool.QueueWorkItem(new WorkItemCallback(this.StateSavingMaintenance), Engine.SleepTime);
            }
            for (int i = 0; i <= Engine.NumberOfStartStopThreads; i++)
            {
                Engine.m_ThreadPool.QueueWorkItem(new WorkItemCallback(this.StartEndScriptMaintenance), Engine.SleepTime);
            }
        }

        #region " Maintenance thread "

        public object StartEndScriptMaintenance(object sleepTime)
        {
            try
            {
                Thread.Sleep((int)sleepTime); // Sleep before next pass

                // LOAD / UNLOAD SCRIPTS
                DoScriptsLoadUnload();
                lock (Resumeable)
                {
                    for (int i = 0; i < Resumeable.Count; i++)
                    {
                        m_ScriptEngine.ResumeScript(Resumeable[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("Exception in StartEndScriptMaintenance. Exception: {0}", ex.ToString());
            }
            m_ScriptEngine.m_ThreadPool.QueueWorkItem(new WorkItemCallback(this.StartEndScriptMaintenance), sleepTime);
            return 0;
        }

        public object StateSavingMaintenance(object sleepTime)
        {
            try
            {
                Thread.Sleep((int)sleepTime); // Sleep before next pass
                DoStateQueue();
            }
            catch (Exception ex)
            {
                m_log.ErrorFormat("Exception in StateSavingMaintenance. Exception: {0}", ex.ToString());
            }
            m_ScriptEngine.m_ThreadPool.QueueWorkItem(new WorkItemCallback(this.StateSavingMaintenance), sleepTime);
            return 0;
        }

        #endregion

        #region State Queue

        public void DoStateQueue()
        {
            lock (m_ScriptEngine.StateQueue)
            {
                if (m_ScriptEngine.StateQueue.Count != 0)
                {
                    StateQueueItem item = m_ScriptEngine.StateQueue.Dequeue();
                    if (item.Create)
                        item.ID.SerializeDatabase();
                    else
                        RemoveState(item.ID);
                }
            }
        }

        public void RemoveState(ScriptData ID)
        {
            ID.GenericData.Delete("auroraDotNetStateSaves", new string[] { "ItemID" }, new string[] { ID.ItemID.ToString() });
        }

        #endregion

        #region Resume Scripts

        //Scripts that need to be unsuspended, but havnt finished starting yet.
        List<OpenMetaverse.UUID> Resumeable = new List<OpenMetaverse.UUID>();

        //This just lets the maintenance thread pick up the slack for finding the scripts that need to be resumed.
        internal void AddResumeScript(OpenMetaverse.UUID itemID)
        {
            if(!Resumeable.Contains(itemID))
                Resumeable.Add(itemID);
        }

        //Removes a script from the list after it has been successfully resumed.
        internal void RemoveResumeScript(OpenMetaverse.UUID itemID)
        {
            if (Resumeable.Contains(itemID))
                Resumeable.Remove(itemID);
        }

        #endregion

        #region Script Load and Unload Queue

        /// <summary>
        /// Main Loop that starts/stops all scripts in the LUQueue.
        /// </summary>
        public void DoScriptsLoadUnload()
        {
            lock (m_ScriptEngine.LUQueue)
            {
                if (m_ScriptEngine.LUQueue.Count == 0)
                    return;

                bool successfullyLoaded = true;
                LUStruct item = m_ScriptEngine.LUQueue.Dequeue();

                if (item.Action == LUType.Unload)
                {
                    try
                    {
                        //Never fire events for an unload.
                        successfullyLoaded = false;
                        item.ID.CloseAndDispose();
                    }
                    catch (Exception ex) { m_log.Warn(ex); }
                }
                else if (item.Action == LUType.Load)
                {
                    try
                    {
                        item.ID.Start(false);
                    }
                    catch (Exception ex) { m_log.Warn(ex); successfullyLoaded = false; }
                }
                else if (item.Action == LUType.Reupload)
                {
                    try
                    {
                        item.ID.Start(true);
                    }
                    catch (Exception ex) { m_log.Warn(ex); successfullyLoaded = false; }
                }
                if(successfullyLoaded)
                    item.ID.FireEvents();
            }
        }

        #endregion

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.GetExecutingAssembly().FullName == args.Name ? Assembly.GetExecutingAssembly() : null;
        }
    }
}
