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

using System.Collections;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{

    #region StartPerformanceQueue

    public class StartPerformanceQueue
    {
        private readonly Queue ContinuedQueue = new Queue(10000);
        private readonly Queue FirstStartQueue = new Queue(10000);
        private readonly Queue SuspendedQueue = new Queue(100); //Smaller, we don't get this very often
        private int ContinuedQueueCount;

        private int FirstStartQueueCount;
        private int SuspendedQueueCount;

        public bool GetNext(out object Item)
        {
            Item = null;
            lock (FirstStartQueue)
            {
                if (FirstStartQueue.Count != 0)
                {
                    FirstStartQueueCount--;
                    Item = FirstStartQueue.Dequeue();
                    return true;
                }
            }
            lock (SuspendedQueue)
            {
                if (SuspendedQueue.Count != 0)
                {
                    SuspendedQueueCount--;
                    Item = SuspendedQueue.Dequeue();
                    return true;
                }
            }
            lock (ContinuedQueue)
            {
                if (ContinuedQueue.Count != 0)
                {
                    ContinuedQueueCount--;
                    Item = ContinuedQueue.Dequeue();
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            lock (ContinuedQueue)
            {
                lock (SuspendedQueue)
                {
                    lock (FirstStartQueue)
                    {
                        ContinuedQueue.Clear();
                        SuspendedQueue.Clear();
                        FirstStartQueue.Clear();
                        ContinuedQueueCount = SuspendedQueueCount = FirstStartQueueCount = 0;
                    }
                }
            }
        }

        public int Count()
        {
            return ContinuedQueueCount + SuspendedQueueCount + FirstStartQueueCount;
        }

        public void Add(object item, LoadPriority priority)
        {
            if (priority == LoadPriority.FirstStart)
                lock (FirstStartQueue)
                {
                    FirstStartQueueCount++;
                    FirstStartQueue.Enqueue(item);
                }
            if (priority == LoadPriority.Restart)
                lock (SuspendedQueue)
                {
                    SuspendedQueueCount++;
                    SuspendedQueue.Enqueue(item);
                }
            if (priority == LoadPriority.Stop)
                lock (ContinuedQueue)
                {
                    ContinuedQueueCount++;
                    ContinuedQueue.Enqueue(item);
                }
        }
    }

    #endregion
}