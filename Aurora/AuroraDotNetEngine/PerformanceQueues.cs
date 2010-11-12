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
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    #region StartPerformanceQueue

    public class StartPerformanceQueue
    {
        Queue FirstStartQueue = new Queue(10000);
        Queue SuspendedQueue = new Queue(100); //Smaller, we don't get this very often
        Queue ContinuedQueue = new Queue(10000);
        public bool GetNext(out object Item)
        {
            Item = null;
            if (FirstStartQueue.Count != 0)
            {
                lock (FirstStartQueue)
                    Item = FirstStartQueue.Dequeue();
                return true;
            }
            if (SuspendedQueue.Count != 0)
            {
                lock (SuspendedQueue)
                    Item = SuspendedQueue.Dequeue();
                return true;
            }
            if (ContinuedQueue.Count != 0)
            {
                lock (ContinuedQueue)
                    Item = ContinuedQueue.Dequeue();
                return true;
            }
            return false;
        }

        public int Count()
        {
            return ContinuedQueue.Count + SuspendedQueue.Count + FirstStartQueue.Count;
        }

        public void Add(object item, LoadPriority priority)
        {
            if (priority == LoadPriority.FirstStart)
                lock (FirstStartQueue)
                    FirstStartQueue.Enqueue(item);
            if (priority == LoadPriority.Restart)
                lock (SuspendedQueue)
                    SuspendedQueue.Enqueue(item);
            if (priority == LoadPriority.Stop)
                lock (ContinuedQueue)
                    ContinuedQueue.Enqueue(item);
        }
    }

    #endregion

    #region EventPerformanceQueue

    public class EventPerformanceQueue
    {
        Queue FirstStartQueue = new Queue(10000);
        Queue ContinuedQueue = new Queue(10000);
        public bool GetNext(out object Item)
        {
            Item = null;
            lock (FirstStartQueue)
            {
                if (FirstStartQueue.Count != 0)
                {
                    Item = FirstStartQueue.Dequeue();
                    return true;
                }
            }
            lock (ContinuedQueue)
            {
                if (ContinuedQueue.Count != 0)
                {
                    Item = ContinuedQueue.Dequeue();
                    return true;
                }
            }
            return false;
        }

        public void Add(object item, EventPriority priority)
        {
            if (priority == EventPriority.FirstStart)
                lock (FirstStartQueue)
                    FirstStartQueue.Enqueue(item);
            else if (priority == EventPriority.Suspended)
                lock (ContinuedQueue)
                    ContinuedQueue.Enqueue(item);
            else if (priority == EventPriority.Continued)
                lock (ContinuedQueue)
                    ContinuedQueue.Enqueue(item);
        }
    }

    #endregion
}
