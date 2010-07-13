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
using OpenMetaverse;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.Plugins
{
    public class Timer
    {
        public ScriptEngine m_ScriptEngine;

        public Timer(ScriptEngine ScriptEngine)
        {
            m_ScriptEngine = ScriptEngine;
        }

        //
        // TIMER
        //
        static private string MakeTimerKey(UUID ID, UUID itemID)
        {
            return ID.ToString() + itemID.ToString();
        }

        private class TimerClass
        {
            public UUID ID;
            public UUID itemID;
            public long interval;
            public long next;
        }

        private Dictionary<string,TimerClass> Timers = new Dictionary<string,TimerClass>();
        private object TimerListLock = new object();

        public void SetTimerEvent(UUID m_ID, UUID m_itemID, double sec)
        {
            if (sec == 0) // Disabling timer
            {
                UnSetTimerEvents(m_ID, m_itemID);
                return;
            }

            // Add to timer
            TimerClass ts = new TimerClass();
            ts.ID = m_ID;
            ts.itemID = m_itemID;
            ts.interval = (long)(sec * 1000);

            //ts.next = DateTime.Now.ToUniversalTime().AddSeconds(ts.interval);
            ts.next = Environment.TickCount + ts.interval;

            string key = MakeTimerKey(m_ID, m_itemID);
            lock (TimerListLock)
            {
                // Adds if timer doesn't exist, otherwise replaces with new timer
                Timers[key] = ts;
            }
        }

        public void UnSetTimerEvents(UUID m_ID, UUID m_itemID)
        {
            // Remove from timer
            string key = MakeTimerKey(m_ID, m_itemID);
            lock (TimerListLock)
            {
                Timers.Remove(key);
            }
        }

        public void CheckTimerEvents()
        {
            // Nothing to do here?
            if (Timers.Count == 0)
                return;

            lock (TimerListLock)
            {
                // Go through all timers
                int TickCount = Environment.TickCount;
                foreach (TimerClass ts in Timers.Values)
                {
                    // Time has passed?
                    if (ts.next < TickCount)
                    {
                        // Add it to queue
                        m_ScriptEngine.PostScriptEvent(ts.itemID, ts.ID,
                                new EventParams("timer", new Object[0],
                                new DetectParams[0]), EventPriority.Continued);
                        // set next interval

                        ts.next = TickCount + ts.interval;
                    }
                }
            }
        }

        public Object[] GetSerializationData(UUID itemID, UUID primID)
        {
            List<Object> data = new List<Object>();
            string key = MakeTimerKey(primID, itemID);
            TimerClass timer;
            if(Timers.TryGetValue(key, out timer))
            {
                data.Add(timer.interval);
                data.Add(timer.next - Environment.TickCount);
            }
            return data.ToArray();
        }

        public void CreateFromData(UUID itemID, UUID objectID,
                                   Object[] data)
        {
            int idx = 0;

            while (idx < data.Length)
            {
                TimerClass ts = new TimerClass();

                ts.ID = objectID;
                ts.itemID = itemID;
                ts.interval =(long)Convert.ToDouble(data[idx]);
                ts.next = Environment.TickCount + (long)Convert.ToDouble(data[idx + 1]);
                idx += 2;

                lock (TimerListLock)
                {
                    Timers[MakeTimerKey(objectID, itemID)] = ts;
                }
            }
        }
    }
}
