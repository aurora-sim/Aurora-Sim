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

        private ScriptEngine m_ScriptEngine;

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
                    if (ScriptEngine.EventQueue.Count == 0)
                    {
                        Thread.Sleep(SleepTime);
                        continue;
                    }
                    while (ScriptEngine.EventQueue.Count != 0)
                    {
                        m_log.Warn("ScriptEngine.Count " + ScriptEngine.EventQueue.Count);
                        // Something in queue, process
                        QueueItemStruct QIS = null;
                        ScriptEngine.EventQueue.Dequeue(out QIS);
                        if (QIS == null)
                            continue;

                        //Suspended scripts get readded
                        if (QIS.ID.Suspended)
                        {
                            ScriptEngine.EventQueue.Enqueue(QIS);
                            continue;
                        }
                        //Disabled or not running scripts dont get events saved.
                        if (QIS.ID.Disabled)
                        {
                            continue;
                        }
                        if (!QIS.ID.Running)
                        {
                            continue;
                        }

                        //Clear scripts that shouldn't be in the queue anymore
                        if (ScriptEngine.NeedsRemoved.ContainsKey(QIS.ID.ItemID))
                        {
                            //Check the localID too...
                            uint localID = 0;
                            ScriptEngine.NeedsRemoved.TryGetValue(QIS.ID.ItemID, out localID);
                            if (localID == QIS.ID.localID)
                            {
                                continue;
                            }
                        }
                        try
                        {
                            QIS.ID.SetEventParams(QIS.llDetectParams);
                            int Running = 0;
                            Running = QIS.ID.Script.ExecuteEvent(
                                QIS.ID.State,
                                QIS.functionName,
                                QIS.param, QIS.CurrentlyAt);
                            //Finished with nothing left.
                            if (Running == 0)
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
                                continue;
                            }
                            //Did not finish and returned where it should start now
                            if (Running != 0)
                            {
                                if (ScriptEngine.NeedsRemoved.ContainsKey(QIS.ID.ItemID))
                                {
                                    //Check the localID too...
                                    uint localID = 0;
                                    ScriptEngine.NeedsRemoved.TryGetValue(QIS.ID.ItemID, out localID);
                                    if (localID == QIS.ID.localID)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        //Remove it then.
                                        ScriptEngine.NeedsRemoved.Remove(QIS.ID.ItemID);
                                    }
                                }
                                QIS.CurrentlyAt = Running;
                                ScriptEngine.EventQueue.Enqueue(QIS);
                            }
                        }
                        catch (SelfDeleteException) // Must delete SOG
                        {
                            if (QIS.ID.part != null && QIS.ID.part.ParentGroup != null)
                                m_ScriptEngine.World.DeleteSceneObject(
                                    QIS.ID.part.ParentGroup, false, true);
                        }
                        catch (ScriptDeleteException) // Must delete item
                        {
                            if (QIS.ID.part != null && QIS.ID.part.ParentGroup != null)
                                QIS.ID.part.Inventory.RemoveInventoryItem(QIS.ID.ItemID);
                        }
                        catch (Exception) { }
                    }
                }
                catch (Exception ex)
                {
                    m_log.WarnFormat("[{0}]: Handled exception stage 2 in the Event Queue: " + ex.Message, m_ScriptEngine.ScriptEngineName);
                }
            }
        }
    }
}
