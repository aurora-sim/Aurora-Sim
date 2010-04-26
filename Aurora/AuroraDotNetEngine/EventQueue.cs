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
using Amib.Threading;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class EventQueue
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ScriptEngine m_ScriptEngine;

        public EventQueue(ScriptEngine engine)
        {
        	m_ScriptEngine = engine;
        }

        public object DoProcessQueue(object SleepTime)
        {
            Thread.Sleep((int)SleepTime);
            try
        	{
                lock (ScriptEngine.EventQueue)
                {
                    if (ScriptEngine.EventQueue.Count == 0)
                    {
                        m_ScriptEngine.m_ThreadPool.QueueWorkItem(new WorkItemCallback(this.DoProcessQueue), SleepTime);
                        return 0;
                    }
                    // Something in queue, process
                    QueueItemStruct QIS = ScriptEngine.EventQueue.Dequeue();

                    //Suspended scripts get readded
                    if (QIS.ID.Suspended)
                    {
                        ScriptEngine.EventQueue.Enqueue(QIS);
                        m_ScriptEngine.m_ThreadPool.QueueWorkItem(new WorkItemCallback(this.DoProcessQueue), SleepTime);
                        return 0;
                    }
                    //Disabled or not running scripts dont get events saved.
                    if (QIS.ID.Disabled)
                    {
                        m_ScriptEngine.m_ThreadPool.QueueWorkItem(new WorkItemCallback(this.DoProcessQueue), SleepTime);
                        return 0;
                    }
                    if (!QIS.ID.Running)
                    {
                        m_ScriptEngine.m_ThreadPool.QueueWorkItem(new WorkItemCallback(this.DoProcessQueue), SleepTime);
                        return 0;
                    }

                    //Clear scripts that shouldn't be in the queue anymore
                    if (!m_ScriptEngine.NeedsRemoved.Contains(QIS.ID.ItemID))
                    {
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
                                m_ScriptEngine.m_ThreadPool.QueueWorkItem(new WorkItemCallback(this.DoProcessQueue), SleepTime);
                                return 0;
                            }
                            //Did not finish and returned where it should start now
                            if (Running != 0 && !m_ScriptEngine.NeedsRemoved.Contains(QIS.ID.ItemID))
                            {
                                QIS.CurrentlyAt = Running;
                                ScriptEngine.EventQueue.Enqueue(QIS);
                            }
                        }
                        catch (SelfDeleteException) // Must delete SOG
                        {
                            if (QIS.ID.part != null && QIS.ID.part.ParentGroup != null)
                                m_ScriptEngine.World.DeleteSceneObject(
                                    QIS.ID.part.ParentGroup, false);
                        }
                        catch (ScriptDeleteException) // Must delete item
                        {
                            if (QIS.ID.part != null && QIS.ID.part.ParentGroup != null)
                                QIS.ID.part.Inventory.RemoveInventoryItem(QIS.ID.ItemID);
                        }
                        catch (Exception) { }
                    }
                }
        	}
        	catch (Exception ex)
        	{
                m_log.WarnFormat("[{0}]: Handled exception stage 2 in the Event Queue: " + ex.Message, m_ScriptEngine.ScriptEngineName);
        	}
            m_ScriptEngine.m_ThreadPool.QueueWorkItem(new WorkItemCallback(this.DoProcessQueue), SleepTime);
            return 0;
        }
    }
}
