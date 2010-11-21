using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net;
using OpenSim.Framework;
using Aurora.Framework;

namespace Aurora.Framework
{
    public class AuroraThreadPoolStartInfo
    {
        public ThreadPriority priority;
        public int Threads = 0;
        public int InitialSleepTime = 5;
        public int MaxSleepTime = 300;
    }

    public class AuroraThreadPool
    {
        public delegate bool QueueItem();
        public delegate bool QueueItem2(object o);

        AuroraThreadPoolStartInfo m_info = null;
        Thread[] Threads = null;
        int[] Sleeping;
        Queue queue = new Queue();
        public int nthreads;
        public int nSleepingthreads;
        private int SleepTimeStep;

        public AuroraThreadPool(AuroraThreadPoolStartInfo info)
            {
            m_info = info;
            Threads = new Thread[m_info.Threads];
            Sleeping = new int[m_info.Threads];
            nthreads = 0;
            nSleepingthreads = 0;
            SleepTimeStep = m_info.MaxSleepTime / 3;
                // lets threads check for work a bit faster in case we have all sleeping and awake interrupt fails
            }

        private void ThreadStart(object number)
            {
            int OurSleepTime = 0;

            int[] numbers = number as int[];
            int ThreadNumber = numbers[0];

            while (true)
                {
                try
                    {
                    QueueItem item = null;
                    object[] o = null;
                    lock (queue)
                        {
                        if (queue.Count != 0)
                            {
                            object queueItem = queue.Dequeue();
                            if (queueItem is QueueItem)
                                item = queueItem as QueueItem;
                            else
                                o = queueItem as object[];
                            }
                        }

                    if (item == null && o == null)
                        {
                        OurSleepTime += SleepTimeStep;
                        if (OurSleepTime > m_info.MaxSleepTime)
                            {
                            Threads[ThreadNumber] = null;
                            Interlocked.Decrement(ref nthreads);
                            break;
                            }
                        else
                            {
                            Interlocked.Exchange(ref Sleeping[ThreadNumber], 1);
                            Interlocked.Increment(ref nSleepingthreads);
                            try { Thread.Sleep(m_info.MaxSleepTime); }
                            catch (ThreadInterruptedException e) { }
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
                        bool Rest = false;
                        OurSleepTime = 0;
                        if (item != null)
                            Rest = item.Invoke();
                        else
                            Rest = (o[0] as QueueItem2).Invoke(o[1]);
                        }
                    }
                catch { }
            }
        }

        public void QueueEvent(QueueItem delegat, int Priority)
        {
            if (delegat == null)
                return;
            lock (queue)
            {
                queue.Enqueue(delegat);
            }

            if (nthreads - nSleepingthreads < queue.Count && nthreads < Threads.Length)
                {
                lock (Threads)
                    {
                    for (int i = 0; i < Threads.Length; i++)
                        {
                        if (Threads[i] == null)
                            {
                            Thread thread = new Thread(ThreadStart);
                            thread.Priority = m_info.priority;
                            thread.Name = "AuroraThrPool#"+ i.ToString();
                            thread.IsBackground = true;
                            try
                                {
                                thread.Start(new int[] { i });
                                Threads[i] = thread;
                                Sleeping[i] = 0;
                                nthreads++;
                                }
                            catch { }
                            return;
                            }
                        }
                    }
                }

            else if(nSleepingthreads >0)
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

        public void QueueEvent2(QueueItem2 delegat, int Priority, object obj)
            {
            if (delegat == null)
                return;
            object[] o = new object[]{delegat, obj };
            lock (queue)
                {
                queue.Enqueue(o);
                }

            if (nthreads < queue.Count && nthreads < Threads.Length)
                {
                lock (Threads)
                    {
                    for (int i = 0; i < Threads.Length; i++)
                        {
                        if (Threads[i] == null)
                            {
                            Thread thread = new Thread(ThreadStart);
                            thread.Priority = m_info.priority;
                            thread.Name = "AuroraThrPool#" + i.ToString();
                            thread.IsBackground = true;
                            try
                                {
                                thread.Start(new int[] { i });
                                Threads[i] = thread;
                                Sleeping[i] = 0;
                                nthreads++;
                                }
                            catch { }
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
    }
}
