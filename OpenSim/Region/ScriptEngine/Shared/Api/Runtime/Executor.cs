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
using System.Reflection.Emit;
using System.Runtime.Remoting.Lifetime;
using OpenSim.Region.ScriptEngine.Shared;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;
using log4net;
using OpenSim.Region.ScriptEngine.Interfaces;

namespace OpenSim.Region.ScriptEngine.Shared.ScriptBase
{
    public delegate object FastInvokeHandler(object target, object[] paramters);

    public class Executor
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// Contains the script to execute functions in.
        /// </summary>
        protected IScript m_Script;

        protected Dictionary<string, scriptEvents> m_eventFlagsMap = new Dictionary<string, scriptEvents>();
        protected Dictionary<Guid, IEnumerator> m_enumerators = new Dictionary<Guid, IEnumerator>();

        [Flags]
        public enum scriptEvents : int
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
            object_rez = 4194304
        }

        // Cache functions by keeping a reference to them in a dictionary
        private Dictionary<string, MethodInfo> Events = new Dictionary<string, MethodInfo>();
        private Dictionary<string, scriptEvents> m_stateEvents = new Dictionary<string, scriptEvents>();

        public Executor(IScript script)
        {
            m_Script = script;
            initEventFlags();
        }

        public scriptEvents GetStateEventFlags(string state)
        {
            //m_log.Debug("Get event flags for " + state);

            // Check to see if we've already computed the flags for this state
            scriptEvents eventFlags = scriptEvents.None;
            if (m_stateEvents.ContainsKey(state))
            {
                m_stateEvents.TryGetValue(state, out eventFlags);
                return eventFlags;
            }

            Type type=m_Script.GetType();

            // Fill in the events for this state, cache the results in the map
            foreach (KeyValuePair<string, scriptEvents> kvp in m_eventFlagsMap)
            {
                string evname = state + "_event_" + kvp.Key;
                //m_log.Debug("Trying event "+evname);
                try
                {
                    MethodInfo mi = type.GetMethod(evname);
                    if (mi != null)
                    {
                        //m_log.Debug("Found handler for " + kvp.Key);
                        eventFlags |= kvp.Value;
                    }
                }
                catch(Exception)
                {
                    //m_log.Debug("Exeption in GetMethod:\n"+e.ToString());
                }
            }

            // Save the flags we just computed and return the result
            if (eventFlags != 0)
                m_stateEvents.Add(state, eventFlags);

            //m_log.Debug("Returning {0:x}", eventFlags);
            return (eventFlags);
        }

        public object ExecuteRemoteEvent(object[] parameters)
        {
            return null;
        }

        public Guid ExecuteEvent(string state, string FunctionName, object[] args, Guid Start)
        {
            try
            {
                // IMPORTANT: Types and MemberInfo-derived objects require a LOT of memory.
                // Instead use RuntimeTypeHandle, RuntimeFieldHandle and RunTimeHandle (IntPtr) instead!
                string EventName = state + "_event_" + FunctionName;

                //#if DEBUG
                m_log.Debug("ScriptEngine: Script event function name: " + EventName);
                //#endif

                if (!Events.ContainsKey(EventName))
                {
                    // Not found, create
                    Type type = m_Script.GetType();
                    try
                    {
                        MethodInfo mi = type.GetMethod(EventName);
                        Events.Add(EventName, mi);
                    }
                    catch
                    {
                        if (!Events.ContainsKey(EventName))
                            // Event name not found, cache it as not found
                            Events.Add(EventName, null);
                    }
                }
                // Get event
                MethodInfo ev = null;
                Events.TryGetValue(EventName, out ev);
                if (ev == null) // No event by that name!
                {
                    //Attempt to find it just by name

                    if (!Events.ContainsKey(FunctionName))
                    {
                        // Not found, create
                        Type type = m_Script.GetType();
                        try
                        {
                            MethodInfo mi = type.GetMethod(FunctionName);
                            Events.Add(FunctionName, mi);
                        }
                        catch
                        {
                            if (!Events.ContainsKey(FunctionName))
                                // Event name not found, cache it as not found
                                Events.Add(FunctionName, null);
                        }
                    }
                    Events.TryGetValue(EventName, out ev);
                    if (ev == null) // No event by that name!
                    {
                        //m_log.Debug("ScriptEngine Can not find any event named:" + EventName);
                        return new Guid();
                    }
                }
                try
                {

                    IEnumerator thread = null;
                    if (Start != Guid.Empty)
                    {
                        m_enumerators.TryGetValue(Start, out thread);
                    }
                    else
                    {
                        FastInvokeHandler fastInvoker = GetMethodInvoker(ev);
                        thread = (IEnumerator)fastInvoker(m_Script, args);
                    }
                    if (thread != null)
                    {
                        int i = 0;
                        bool running = false;
                        try
                        {
                            while (i < 5)
                            {
                                running = thread.MoveNext();
                                if (!running)
                                {
                                    lock (m_enumerators)
                                    {
                                        if (m_enumerators.ContainsKey(Start))
                                            m_enumerators.Remove(Start);
                                    }
                                    return Guid.Empty;
                                }
                                i++;
                            }
                        }
                        catch (TargetInvocationException tie)
                        {
                            // Grab the inner exception and rethrow it, unless the inner
                            // exception is an EventAbortException as this indicates event
                            // invocation termination due to a state change.
                            // DO NOT THROW JUST THE INNER EXCEPTION!
                            // FriendlyErrors depends on getting the whole exception!
                            //
                            if (!(tie.InnerException is EventAbortException))
                            {
                                throw;
                            }
                        }
                    }
                    else
                    {
                        FastInvokeHandler fastInvoker = GetMethodInvoker(ev);
                        fastInvoker(m_Script, args);
                        return Guid.Empty;
                    }
                    if (Start == Guid.Empty)
                    {
                        Start = System.Guid.NewGuid();
                        m_enumerators.Add(Start, thread);
                    }
                    return Start;
                }
                catch (TargetInvocationException ex)
                {
                    IEnumerator thread = null;
                    if (Start != Guid.Empty)
                    {
                        m_enumerators.TryGetValue(Start, out thread);
                    }
                    else
                    {
                        thread = (IEnumerator)ev.Invoke(m_Script, args);
                    }
                    int i = 0;
                    bool running = false;
                    while (i < i + 10)
                    {
                        i++;
                        try
                        {
                            running = thread.MoveNext();
                            if (!running)
                            {
                                lock (m_enumerators)
                                {
                                    if (m_enumerators.ContainsKey(Start))
                                        m_enumerators.Remove(Start);
                                }
                                return Guid.Empty;
                            }

                        }
                        catch (TargetInvocationException tie)
                        {
                            // Grab the inner exception and rethrow it, unless the inner
                            // exception is an EventAbortException as this indicates event
                            // invocation termination due to a state change.
                            // DO NOT THROW JUST THE INNER EXCEPTION!
                            // FriendlyErrors depends on getting the whole exception!
                            //
                            if (!(tie.InnerException is EventAbortException))
                            {
                                throw;
                            }
                        }
                    }
                    if (Start == Guid.Empty)
                    {
                        Start = System.Guid.NewGuid();
                        m_enumerators.Add(Start, thread);
                    }
                    return Start;
                }
                catch (System.Security.SecurityException ex)
                {
                    IEnumerator thread = null;
                    if (Start != Guid.Empty)
                    {
                        m_enumerators.TryGetValue(Start, out thread);
                    }
                    else
                    {
                        thread = (IEnumerator)ev.Invoke(m_Script, args);
                    }
                    int i = 0;
                    bool running = false;
                    while (i < i + 10)
                    {
                        i++;
                        try
                        {
                            running = thread.MoveNext();
                            if (!running)
                            {
                                lock (m_enumerators)
                                {
                                    if (m_enumerators.ContainsKey(Start))
                                        m_enumerators.Remove(Start);
                                }
                                return Guid.Empty;
                            }

                        }
                        catch (TargetInvocationException tie)
                        {
                            // Grab the inner exception and rethrow it, unless the inner
                            // exception is an EventAbortException as this indicates event
                            // invocation termination due to a state change.
                            // DO NOT THROW JUST THE INNER EXCEPTION!
                            // FriendlyErrors depends on getting the whole exception!
                            //
                            if (!(tie.InnerException is EventAbortException))
                            {
                                throw;
                            }
                        }
                    }
                    if (Start == Guid.Empty)
                    {
                        Start = System.Guid.NewGuid();
                        m_enumerators.Add(Start, thread);
                    }
                    return Start;
                }
            }
            catch
            {
                return Guid.Empty;
            }
        }

        #region From http://www.codeproject.com/KB/cs/FastMethodInvoker.aspx Thanks to Luyan for this code

        private static void EmitCastToReference(ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, type);
            }
            else
            {
                il.Emit(OpCodes.Castclass, type);
            }
        }

        private static void EmitBoxIfNeeded(ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
        }

        private static void EmitFastInt(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
            }

            if (value > -129 && value < 128)
            {
                il.Emit(OpCodes.Ldc_I4_S, (SByte)value);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, value);
            }
        }

        public static FastInvokeHandler GetMethodInvoker(MethodInfo methodInfo)
        {
            DynamicMethod dynamicMethod = new DynamicMethod(string.Empty,
                             typeof(object), new Type[] { typeof(object), 
                     typeof(object[]) },
                             methodInfo.DeclaringType.Module);
            ILGenerator il = dynamicMethod.GetILGenerator();
            ParameterInfo[] ps = methodInfo.GetParameters();
            Type[] paramTypes = new Type[ps.Length];
            for (int i = 0; i < paramTypes.Length; i++)
            {
                paramTypes[i] = ps[i].ParameterType;
            }
            LocalBuilder[] locals = new LocalBuilder[paramTypes.Length];
            for (int i = 0; i < paramTypes.Length; i++)
            {
                locals[i] = il.DeclareLocal(paramTypes[i]);
            }
            for (int i = 0; i < paramTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                EmitFastInt(il, i);
                il.Emit(OpCodes.Ldelem_Ref);
                EmitCastToReference(il, paramTypes[i]);
                il.Emit(OpCodes.Stloc, locals[i]);
            }
            il.Emit(OpCodes.Ldarg_0);
            for (int i = 0; i < paramTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldloc, locals[i]);
            }
            il.EmitCall(OpCodes.Call, methodInfo, null);
            if (methodInfo.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else
                EmitBoxIfNeeded(il, methodInfo.ReturnType);
            il.Emit(OpCodes.Ret);
            FastInvokeHandler invoder =
              (FastInvokeHandler)dynamicMethod.CreateDelegate(
              typeof(FastInvokeHandler));
            return invoder;
        }

        #endregion

        protected void initEventFlags()
        {
            // Initialize the table if it hasn't already been done
            if (m_eventFlagsMap.Count > 0)
            {
                return;
            }

            m_eventFlagsMap.Add("attach", scriptEvents.attach);
            m_eventFlagsMap.Add("at_rot_target", scriptEvents.at_rot_target);
            m_eventFlagsMap.Add("at_target", scriptEvents.at_target);
            // m_eventFlagsMap.Add("changed",(long)scriptEvents.changed);
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
            // m_eventFlagsMap.Add("link_message",scriptEvents.link_message);
            m_eventFlagsMap.Add("listen", scriptEvents.listen);
            m_eventFlagsMap.Add("money", scriptEvents.money);
            m_eventFlagsMap.Add("moving_end", scriptEvents.moving_end);
            m_eventFlagsMap.Add("moving_start", scriptEvents.moving_start);
            m_eventFlagsMap.Add("not_at_rot_target", scriptEvents.not_at_rot_target);
            m_eventFlagsMap.Add("not_at_target", scriptEvents.not_at_target);
            // m_eventFlagsMap.Add("no_sensor",(long)scriptEvents.no_sensor);
            // m_eventFlagsMap.Add("on_rez",(long)scriptEvents.on_rez);
            m_eventFlagsMap.Add("remote_data", scriptEvents.remote_data);
            m_eventFlagsMap.Add("run_time_permissions", scriptEvents.run_time_permissions);
            // m_eventFlagsMap.Add("sensor",(long)scriptEvents.sensor);
            m_eventFlagsMap.Add("state_entry", scriptEvents.state_entry);
            m_eventFlagsMap.Add("state_exit", scriptEvents.state_exit);
            m_eventFlagsMap.Add("timer", scriptEvents.timer);
            m_eventFlagsMap.Add("touch", scriptEvents.touch);
            m_eventFlagsMap.Add("touch_end", scriptEvents.touch_end);
            m_eventFlagsMap.Add("touch_start", scriptEvents.touch_start);
            m_eventFlagsMap.Add("object_rez", scriptEvents.object_rez);
        }
    }
}
