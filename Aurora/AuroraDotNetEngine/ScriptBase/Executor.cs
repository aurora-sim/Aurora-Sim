/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
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
using Aurora.Framework;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.Runtime
{
    public class Executor
    {
        #region scriptEvents enum

        [Flags]
        public enum scriptEvents : long
        {
            None = 0,
            attach = 1,
            collision = 16,
            collision_end = 32,
            collision_start = 64,
            control = 128,
            dataserver = 256,
            email = 512,
            http_response = 1024,
            land_collision = 2048,
            land_collision_end = 4096,
            land_collision_start = 8192,
            at_target = 16384,
            at_rot_target = 16777216,
            listen = 32768,
            money = 65536,
            moving_end = 131072,
            moving_start = 262144,
            not_at_rot_target = 524288,
            not_at_target = 1048576,
            remote_data = 8388608,
            run_time_permissions = 268435456,
            state_entry = 1073741824,
            state_exit = 2,
            timer = 4,
            touch = 8,
            touch_end = 536870912,
            touch_start = 2097152,
            object_rez = 4194304,
            changed = 2147483648,
            link_message = 4294967296,
            no_sensor = 8589934592,
            on_rez = 17179869184,
            sensor = 34359738368,
            transaction_event = 68719476736
        }

        #endregion

        protected static Dictionary<string, scriptEvents> m_eventFlagsMap = new Dictionary<string, scriptEvents>();

        // Cache functions by keeping a reference to them in a dictionary
        private readonly Dictionary<string, scriptEvents> m_stateEvents = new Dictionary<string, scriptEvents>();

        private bool InTimeSlice;

        private Int32 MaxTimeSlice = 60; // script timeslice execution time in ms , hardwired for now
        private int TimeSliceEnd;

        /// <summary>
        ///   Contains the script to execute functions in.
        /// </summary>
        protected IScript m_Script;

        protected Dictionary<Guid, IEnumerator> m_enumerators = new Dictionary<Guid, IEnumerator>();
        private Type m_scriptType;


        public Executor(IScript script)
        {
            InTimeSlice = false;
            m_Script = script;
            initEventFlags();
        }

        public scriptEvents GetStateEventFlags(string state)
        {
            // Check to see if we've already computed the flags for this state
            scriptEvents eventFlags = scriptEvents.None;
            if (m_stateEvents.TryGetValue(state, out eventFlags))
                return eventFlags;

            if (m_scriptType == null)
                m_scriptType = m_Script.GetType();
            // Fill in the events for this state, cache the results in the map
            foreach (KeyValuePair<string, scriptEvents> kvp in m_eventFlagsMap)
            {
                try
                {
                    MethodInfo ev = null;
                    string evname = state == "" ? "" : state + "_event_";
                    evname += kvp.Key;
                    //MainConsole.Instance.Debug("Trying event "+evname);

                    ev = m_scriptType.GetMethod(evname);
                    if (ev != null)
                        //MainConsole.Instance.Debug("Found handler for " + kvp.Key);
                        eventFlags |= kvp.Value;
                }
                catch (Exception)
                {
                    //MainConsole.Instance.Debug("Exeption in GetMethod:\n"+e.ToString());
                }
            }

            // Save the flags we just computed and return the result
            if (eventFlags != 0)
                m_stateEvents.Add(state, eventFlags);

            //MainConsole.Instance.Debug("Returning {0:x}", eventFlags);
            return eventFlags;
        }

        public void OpenTimeSlice(EnumeratorInfo Start)
        {
            TimeSliceEnd = Start == null ? Util.EnvironmentTickCountAdd(MaxTimeSlice) : Util.EnvironmentTickCountAdd(MaxTimeSlice / 2);
            InTimeSlice = true;
        }

        public void CloseTimeSlice()
        {
            InTimeSlice = false;
        }

        public bool CheckSlice()
        {
            return TimeSliceEnd < Util.EnvironmentTickCount();
        }

        public EnumeratorInfo ExecuteEvent(string state, string FunctionName, object[] args, EnumeratorInfo Start,
                                           out Exception ex)
        {
            ex = null;
            string EventName = state == "" ? FunctionName : state + "_event_" + FunctionName;

            if (InTimeSlice)
                //MainConsole.Instance.Output("ScriptEngine TimeSlice Overlap " + FunctionName);
                return Start;

            IEnumerator thread = null;

            OpenTimeSlice(Start);
            bool running = true;

            try
            {
                if (Start != null)
                {
                    lock (m_enumerators)
                        m_enumerators.TryGetValue(Start.Key, out thread);
                }
                else
                    thread = m_Script.FireEvent(EventName, args);

                if (thread != null)
                    running = thread.MoveNext();
            }
            catch (Exception tie)
            {
                // Grab the inner exception and rethrow it, unless the inner
                // exception is an EventAbortException as this indicates event
                // invocation termination due to a state change.
                // DO NOT THROW JUST THE INNER EXCEPTION!
                // FriendlyErrors depends on getting the whole exception!
                //
                if (!(tie is EventAbortException) &&
                    !(tie is MinEventDelayException) &&
                    !(tie.InnerException != null &&
                      ((tie.InnerException.Message.Contains("EventAbortException")) ||
                       (tie.InnerException.Message.Contains("MinEventDelayException")))))
                    ex = tie;
                if (Start != null)
                {
                    lock (m_enumerators)
                    {
                        m_enumerators.Remove(Start.Key);
                    }
                }
                CloseTimeSlice();
                return null;
            }

            if (running && thread != null)
            {
                if (Start == null)
                {
                    Start = new EnumeratorInfo { Key = UUID.Random().Guid };
                }
                lock (m_enumerators)
                {
                    m_enumerators[Start.Key] = thread;
                }

                if (thread.Current is DateTime)
                    Start.SleepTo = (DateTime)thread.Current;
                else if (thread.Current is string)
                {
                    ex = new Exception((string)thread.Current);
                    running = false;
                    lock (m_enumerators)
                    {
                        m_enumerators.Remove(Start.Key);
                    }
                }
                CloseTimeSlice();
                return Start;
            }
            else
            {
                //No enumerator.... errr.... something went really wrong here
                if (Start != null)
                {
                    lock (m_enumerators)
                    {
                        m_enumerators.Remove(Start.Key);
                    }
                }
                CloseTimeSlice();
                return null;
            }
        }


        protected void initEventFlags()
        {
            // Initialize the table if it hasn't already been done
            if (m_eventFlagsMap.Count > 0)
                return;

            m_eventFlagsMap.Add("attach", scriptEvents.attach);
            m_eventFlagsMap.Add("at_rot_target", scriptEvents.at_rot_target);
            m_eventFlagsMap.Add("at_target", scriptEvents.at_target);
            m_eventFlagsMap.Add("changed", scriptEvents.changed);
            m_eventFlagsMap.Add("collision", scriptEvents.collision);
            m_eventFlagsMap.Add("collision_end", scriptEvents.collision_end);
            m_eventFlagsMap.Add("collision_start", scriptEvents.collision_start);
            m_eventFlagsMap.Add("control", scriptEvents.control);
            m_eventFlagsMap.Add("dataserver", scriptEvents.dataserver);
            m_eventFlagsMap.Add("email", scriptEvents.email);
            m_eventFlagsMap.Add("http_response", scriptEvents.http_response);
            m_eventFlagsMap.Add("land_collision", scriptEvents.land_collision);
            m_eventFlagsMap.Add("land_collision_end", scriptEvents.land_collision_end);
            m_eventFlagsMap.Add("land_collision_start", scriptEvents.land_collision_start);
            m_eventFlagsMap.Add("link_message", scriptEvents.link_message);
            m_eventFlagsMap.Add("listen", scriptEvents.listen);
            m_eventFlagsMap.Add("money", scriptEvents.money);
            m_eventFlagsMap.Add("moving_end", scriptEvents.moving_end);
            m_eventFlagsMap.Add("moving_start", scriptEvents.moving_start);
            m_eventFlagsMap.Add("not_at_rot_target", scriptEvents.not_at_rot_target);
            m_eventFlagsMap.Add("not_at_target", scriptEvents.not_at_target);
            m_eventFlagsMap.Add("no_sensor", scriptEvents.no_sensor);
            m_eventFlagsMap.Add("on_rez", scriptEvents.on_rez);
            m_eventFlagsMap.Add("remote_data", scriptEvents.remote_data);
            m_eventFlagsMap.Add("run_time_permissions", scriptEvents.run_time_permissions);
            m_eventFlagsMap.Add("sensor", scriptEvents.sensor);
            m_eventFlagsMap.Add("state_entry", scriptEvents.state_entry);
            m_eventFlagsMap.Add("state_exit", scriptEvents.state_exit);
            m_eventFlagsMap.Add("timer", scriptEvents.timer);
            m_eventFlagsMap.Add("touch", scriptEvents.touch);
            m_eventFlagsMap.Add("touch_end", scriptEvents.touch_end);
            m_eventFlagsMap.Add("touch_start", scriptEvents.touch_start);
            m_eventFlagsMap.Add("transaction_event", scriptEvents.transaction_event);
            m_eventFlagsMap.Add("object_rez", scriptEvents.object_rez);
        }

        public void ResetStateEventFlags()
        {
            m_stateEvents.Clear();
            m_enumerators.Clear();
            m_scriptType = null;
        }
    }
}