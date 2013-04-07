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
using System.Collections.Concurrent;
using System.Threading;

namespace Aurora.Framework.Utilities
{
    public class AuroraThreadPoolStartInfo
    {
        public int InitialSleepTime = 10;
        public bool KillThreadAfterQueueClear;
        public int MaxSleepTime = 100;
        public string Name = "";
        public int SleepIncrementTime = 10;
        public int Threads;
        public ThreadPriority priority;
    }

    public class AuroraThreadPool
    {
        private readonly int[] Sleeping;
        private readonly Thread[] Threads;
        private readonly AuroraThreadPoolStartInfo m_info;
        private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
        public long nSleepingthreads;
        public long nthreads;

        public AuroraThreadPool(AuroraThreadPoolStartInfo info)
        {
            m_info = info;
            Threads = new Thread[m_info.Threads];
            Sleeping = new int[m_info.Threads];
            nthreads = 0;
            nSleepingthreads = 0;
            // lets threads check for work a bit faster in case we have all sleeping and awake interrupt fails
        }

        private void ThreadStart(object number)
        {
            Culture.SetCurrentCulture();
            int OurSleepTime = 0;

            int[] numbers = number as int[];
            int ThreadNumber = numbers[0];

            while (true)
            {
                try
                {
                    Action item = null;
                    if (!queue.TryDequeue(out item))
                    {
                        OurSleepTime += m_info.SleepIncrementTime;
                        if (m_info.KillThreadAfterQueueClear || OurSleepTime > m_info.MaxSleepTime)
                        {
                            Threads[ThreadNumber] = null;
                            Interlocked.Decrement(ref nthreads);
                            break;
                        }
                        else
                        {
                            Interlocked.Exchange(ref Sleeping[ThreadNumber], 1);
                            Interlocked.Increment(ref nSleepingthreads);
                            try
                            {
                                Thread.Sleep(OurSleepTime);
                            }
                            catch (ThreadInterruptedException)
                            {
                            }
                            Interlocked.Decrement(ref nSleepingthreads);
                            Interlocked.Exchange(ref Sleeping[ThreadNumber], 0);
                            continue;
                        }
                    }
                    else
                    {
                        // workers have no business on pool waiting times
                        // that whould make interrelations very hard to debug
                        // If a worker wants to delay its requeue, then he should for now sleep before
                        // asking to be requeued.
                        // in future we should add a trigger time delay as parameter to the queue request.
                        // so to release the thread sooner, like .net and mono can now do.
                        // This control loop whould then have to look for those delayed requests.
                        // UBIT
                        OurSleepTime = m_info.InitialSleepTime;
                        item.Invoke();
                    }
                }
                catch
                {
                }
                Thread.Sleep(OurSleepTime);
            }
        }

        public void QueueEvent(Action delegat, int Priority)
        {
            if (delegat == null)
                return;

            queue.Enqueue(delegat);

            if (nthreads == 0 || (nthreads - nSleepingthreads < queue.Count - 1 && nthreads < Threads.Length))
            {
                lock (Threads)
                {
                    for (int i = 0; i < Threads.Length; i++)
                    {
                        if (Threads[i] == null)
                        {
                            Thread thread = new Thread(ThreadStart)
                                                {
                                                    Priority = m_info.priority,
                                                    Name =
                                                        (m_info.Name == "" ? "AuroraThreadPool" : m_info.Name) + "#" +
                                                        i.ToString(),
                                                    IsBackground = true
                                                };
                            try
                            {
                                thread.Start(new[] {i});
                                Threads[i] = thread;
                                Sleeping[i] = 0;
                                nthreads++;
                            }
                            catch
                            {
                            }
                            return;
                        }
                    }
                }
            }
            else if (nSleepingthreads > 0)
            {
                lock (Threads)
                {
                    for (int i = 0; i < Threads.Length; i++)
                    {
                        if (Sleeping[i] == 1 && Threads[i].ThreadState == ThreadState.WaitSleepJoin)
                        {
                            Threads[i].Interrupt(); // if we have a sleeping one awake it
                            return;
                        }
                    }
                }
            }
        }

        public void AbortThread(Thread thread)
        {
            int i;
            lock (Threads)
            {
                for (i = 0; i < Threads.Length; i++)
                {
                    if (Threads[i] == thread)
                        break;
                }
                if (i == Threads.Length)
                    return;

                Threads[i] = null;
                nthreads--;
            }
            try
            {
                thread.Abort("Shutdown");
            }
            catch
            {
            }
        }

        public void Restart()
        {
            ClearEvents();
            var threads = new Thread[0];
            lock (Threads)
            {
                threads = new Thread[Threads.Length];
                Threads.CopyTo(threads, 0);
            }
            foreach (Thread t in threads)
            {
                AbortThread(t);
            }
        }

        public void ClearEvents()
        {
            Action itm;
            while (queue.TryDequeue(out itm))
            {
            }
        }

        public Thread[] GetThreads()
        {
            var threads = new Thread[0];
            lock (Threads)
            {
                threads = new Thread[Threads.Length];
                Threads.CopyTo(threads, 0);
            }
            return threads;
        }
    }
}