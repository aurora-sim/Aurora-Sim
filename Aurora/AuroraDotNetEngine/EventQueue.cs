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
using OpenSim.Region.ScriptEngine.Shared;
using OpenSim.Region.ScriptEngine.Shared.CodeTools;
using Amib.Threading;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class EventQueue
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static ScriptEngine m_ScriptEngine;

        private int SleepTime;

        public EventQueue(ScriptEngine engine, int sleep)
        {
            m_ScriptEngine = engine;
            SleepTime = sleep;
        }

        public void DoProcessQueue()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(SleepTime);
                    QueueItemStruct QIS = null;
                    while (ScriptEngine.EventQueue.Dequeue(out QIS))
                    {
                        ProcessQIS(QIS);
                    }
                }
                catch (Exception ex)
                {
                    m_log.WarnFormat("[{0}]: Handled exception stage 2 in the Event Queue: " + ex.Message, m_ScriptEngine.ScriptEngineName);
                }
            }
        }

        public static void ProcessQIS(QueueItemStruct QIS)
        {
            //Suspended scripts get readded
            if (QIS.ID.Suspended || QIS.ID.Script == null || QIS.ID.Loading)
            {
                ScriptEngine.EventQueue.Enqueue(QIS, ScriptEngine.EventPriority.Suspended);
                return;
            }

            if (ScriptEngine.NeedsRemoved.ContainsKey(QIS.ID.ItemID))
            {
                int Version = 0;
                ScriptEngine.NeedsRemoved.TryGetValue(QIS.ID.ItemID, out Version);
                if (QIS.functionName == "state_entry" || QIS.functionName == "on_rez")
                {
                }
                if (Version >= QIS.VersionID)
                    return;
            }
            if (QIS.functionName == "state_entry" || QIS.functionName == "on_rez")
            {
            }
                
            //Disabled or not running scripts dont get events saved.
            if (QIS.ID.Disabled || !QIS.ID.Running)
                return;
            try
            {
                Guid Running = Guid.Empty;
                Exception ex;
                QIS.ID.SetEventParams(QIS.llDetectParams);
                Running = new Guid(QIS.ID.Script.ExecuteEvent(QIS.ID.State,
                            QIS.functionName,
                            QIS.param, QIS.CurrentlyAt, out ex).ToString());
                if (ex != null)
                    throw ex;
                //Finished with nothing left.
                if (Running == Guid.Empty)
                {
                    if (QIS.functionName == "timer")
                        QIS.ID.TimerQueued = false;
                    if (QIS.functionName == "control")
                    {
                        if (QIS.ID.ControlEventsInQueue > 0)
                            QIS.ID.ControlEventsInQueue--;
                    }
                    if (QIS.functionName == "collision")
                        QIS.ID.CollisionInQueue = false;
                    if (QIS.functionName == "touch")
                        QIS.ID.TouchInQueue = false;
                    if (QIS.functionName == "land_collision")
                        QIS.ID.LandCollisionInQueue = false;
                    if (QIS.functionName == "changed")
                    {
                        Changed changed = (Changed)(new LSL_Types.LSLInteger(QIS.param[0].ToString()).value);
                        lock (QIS.ID.ChangedInQueue)
                        {
                            if (QIS.ID.ChangedInQueue.Contains(changed))
                                QIS.ID.ChangedInQueue.Remove(changed);
                        }
                    }
                    return;
                }
                else
                {
                    //Did not finish so requeue it
                    QIS.CurrentlyAt = Running;
                    ScriptEngine.EventQueue.Enqueue(QIS, ScriptEngine.EventPriority.Continued);
                }
            }
            catch (SelfDeleteException) // Must delete SOG
            {
                if (QIS.ID.part != null && QIS.ID.part.ParentGroup != null)
                    m_ScriptEngine.findPrimsScene(QIS.ID.part.UUID).DeleteSceneObject(
                        QIS.ID.part.ParentGroup, false, true);
            }
            catch (ScriptDeleteException) // Must delete item
            {
                if (QIS.ID.part != null && QIS.ID.part.ParentGroup != null)
                    QIS.ID.part.Inventory.RemoveInventoryItem(QIS.ID.ItemID);
            }
            catch (Exception ex)
            {
                QIS.ID.ShowError(ex, "executing", false);
            }
        }
    }
}
