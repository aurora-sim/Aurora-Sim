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
        bool[] Sleeping = null;
        Queue queue = new Queue();
        int nthreads;

        public AuroraThreadPool(AuroraThreadPoolStartInfo info)
            {
            m_info = info;
            Threads = new Thread[m_info.Threads];
            Sleeping = new bool[m_info.Threads];
            nthreads = 0;
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
                        if (OurSleepTime++ >3 ) //Make sure we don't go waay over on how long we sleep
                            {
                            lock (Threads)
                                {
                                Threads[ThreadNumber] = null;
                                nthreads--;
                                break;
                                }
                            }
                        else
                            {
                            lock (Threads)
                                Sleeping[ThreadNumber] = true;

                            Thread.Sleep(m_info.MaxSleepTime);

                            lock (Threads)
                                Sleeping[ThreadNumber] = false;

                            continue;
                            }
                        }
                    bool Rest = false;

                    if (item != null)
                        Rest = item.Invoke();
                    else
                        Rest = (o[0] as QueueItem2).Invoke(o[1]);
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
                            thread.Name = "Aurora Thread Pool Thread #" + 0;
                            thread.IsBackground = true;
                            Sleeping[i] = false;
                            Threads[i] = thread;
                            nthreads++;
                            thread.Start(new int[] { i });
                            return;
                            }

                        else if (Sleeping[i] && Threads[i].ThreadState == ThreadState.WaitSleepJoin)
                            Threads[i].Interrupt(); // if we have a sleeping one awake it
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
                            thread.Name = "Aurora Thread Pool Thread #" + 0;
                            thread.IsBackground = true;
                            Sleeping[i] = false;
                            Threads[i] = thread;
                            nthreads++;
                            thread.Start(new int[] { i });
                            return;
                            }

                        else if (Sleeping[i] && Threads[i].ThreadState == ThreadState.WaitSleepJoin)
                            Threads[i].Interrupt(); // if we have a sleeping one awake it
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
