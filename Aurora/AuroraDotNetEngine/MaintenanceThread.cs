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
using Aurora.Framework;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    /// <summary>
    /// This class does maintenance on script engine.
    /// </summary>
    public class MaintenanceThread
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IScriptDataConnector ScriptFrontend;
        private ScriptEngine m_ScriptEngine;
        
        public MaintenanceThread(ScriptEngine Engine)
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            m_ScriptEngine = Engine;
            ScriptFrontend = Aurora.DataManager.DataManager.IScriptDataConnector;
            for (int i = 0; i < Engine.NumberOfEventQueueThreads; i++)
            {
                EventQueue eqtc = new EventQueue(m_ScriptEngine, Engine.SleepTime);
                Watchdog.StartThread(eqtc.DoProcessQueue, "EventQueueThread", ThreadPriority.BelowNormal, true);
            }
            for (int i = 0; i < Engine.NumberOfStartStopThreads; i++)
            {
                Watchdog.StartThread(StartEndScriptMaintenance, "StartEndScriptMaintenance", ThreadPriority.BelowNormal, true);
            }
        }

        #region " Maintenance thread "

        public void StartEndScriptMaintenance()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(m_ScriptEngine.SleepTime); // Sleep before next pass
                    // LOAD / UNLOAD SCRIPTS
                    DoScriptsLoadUnload();
                    // Save states
                    DoStateQueue();
                }
                catch (Exception ex)
                {
                    m_log.ErrorFormat("Exception in StartEndScriptMaintenance : {0}", ex.ToString());
                }
            }
        }

        #endregion

        #region State Queue

        public void DoStateQueue()
        {
            StateQueueItem item = null;
            if (ScriptEngine.StateQueue.Dequeue(out item))
            {
                ScriptEngine.StateQueue.Dequeue(out item);
                if (item == null || item.ID == null)
                    return;
                if (item.Create)
                    item.ID.SerializeDatabase();
                else
                    RemoveState(item.ID);
            }
        }

        public void RemoveState(ScriptData ID)
        {
            ScriptFrontend.DeleteStateSave(ID.ItemID);
        }

        #endregion

        #region Script Load and Unload Queue

        /// <summary>
        /// Main Loop that starts/stops all scripts in the LUQueue.
        /// </summary>
        public void DoScriptsLoadUnload()
        {
            LUStruct[] items;
            while (m_ScriptEngine.LUQueue.Dequeue(out items))
            {
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
                        }
                        catch (Exception ex) { m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: LEAKED COMPILE ERROR: " + ex); }
                    }
                    else if (item.Action == LUType.Reupload)
                    {
                        try
                        {
                            item.ID.Start(true);
                        }
                        catch (Exception ex) { m_log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: LEAKED COMPILE ERROR: " + ex); }
                    }
                }
                foreach (LUStruct item in items)
                {
                    item.ID.FireEvents();
                }
            }
        }

        #endregion

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.GetExecutingAssembly().FullName == args.Name ? Assembly.GetExecutingAssembly() : null;
        }
    }
}
