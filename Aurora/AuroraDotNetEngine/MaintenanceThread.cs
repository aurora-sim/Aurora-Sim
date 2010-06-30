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
        private bool InitialStart = true;
        private bool ScriptsLoaded = false;
        
        public MaintenanceThread(ScriptEngine Engine)
        {
            m_ScriptEngine = Engine;
            ScriptFrontend = Aurora.DataManager.DataManager.RequestPlugin<IScriptDataConnector>("IScriptDataConnector");
            for (int i = 0; i < Engine.NumberOfEventQueueThreads; i++)
            {
                EventQueue eqtc = new EventQueue(m_ScriptEngine, Engine.SleepTime);
                Watchdog.StartThread(eqtc.DoProcessQueue, "EventQueueThread", ThreadPriority.Lowest, true);
            }
            for (int i = 0; i < Engine.NumberOfStartStopThreads; i++)
            {
                Watchdog.StartThread(StartEndScriptMaintenance, "StartEndScriptMaintenance", ThreadPriority.Lowest, true);
            }
            AppDomain.CurrentDomain.AssemblyResolve += OpenSim.Region.ScriptEngine.Shared.AssemblyResolver.OnAssemblyResolve;
        }

        public void OnScriptsLoadingComplete()
        {
            ScriptsLoaded = true;
        }

        #region " Maintenance thread "

        public void StartEndScriptMaintenance()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(m_ScriptEngine.SleepTime * 5); // Sleep before next pass
                    if (InitialStart)
                    {
                        InitialStart = false;
                        foreach (OpenSim.Region.Framework.Scenes.Scene scene in m_ScriptEngine.Worlds)
                        {
                            // No scripts on region, so won't get triggered later
                            // by the queue becoming empty so we trigger it here
                            scene.EventManager.TriggerEmptyScriptCompileQueue(0, String.Empty);
                        }
                    }
                    // LOAD / UNLOAD SCRIPTS
                    bool DoneSomething = DoScriptsLoadUnload();
                    // Save states
                    bool DoneSomethingElse = DoStateQueue();
                    if (DoneSomething && DoneSomethingElse)
                    {
                        //Assist the event queue if it needs it if we have nothing to do.
                        QueueItemStruct QIS = null;
                        int i = 0;
                        while (ScriptEngine.EventQueue.Dequeue(out QIS) && i < 5)
                        {
                            EventQueue.ProcessQIS(QIS);
                            i++;
                        }
                    }
                    if (ScriptsLoaded)
                    {
                        foreach (OpenSim.Region.Framework.Scenes.Scene scene in m_ScriptEngine.Worlds)
                        {
                            scene.EventManager.TriggerEmptyScriptCompileQueue(m_ScriptEngine.ScriptFailCount,
                                                                            m_ScriptEngine.ScriptErrorMessages);
                        }
                    }
                    m_ScriptEngine.ScriptFailCount = 0;
                }
                catch (Exception ex)
                {
                    m_log.ErrorFormat("Exception in StartEndScriptMaintenance : {0}", ex.ToString());
                }
            }
        }

        #endregion

        #region State Queue

        public bool DoStateQueue()
        {
            StateQueueItem item = null;
            bool DoneSomething = false;
            if (ScriptEngine.StateQueue.Dequeue(out item))
            {
                DoneSomething = true;
                ScriptEngine.StateQueue.Dequeue(out item);
                if (item == null || item.ID == null)
                    return true;
                if (item.Create)
                    item.ID.SerializeDatabase();
                else
                    RemoveState(item.ID);
            }
            return DoneSomething;
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
        public bool DoScriptsLoadUnload()
        {
            LUStruct[] items;
            bool DoneSomething = false;
            while (m_ScriptEngine.LUQueue.Dequeue(out items))
            {
                DoneSomething = true;
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
            return DoneSomething;
        }

        #endregion
    }
}
