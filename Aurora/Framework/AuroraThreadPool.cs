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
        public int InitialSleepTime = 10;
        public int MaxSleepTime = 300;
    }

    public class AuroraThreadPool
    {
        public delegate bool QueueItem();
        public delegate bool QueueItem2(object o);

        AuroraThreadPoolStartInfo m_info = null;
        Thread[] Threads = null;
        Queue queue = new Queue();

        public AuroraThreadPool(AuroraThreadPoolStartInfo info)
        {
            m_info = info;
            Threads = new Thread[m_info.Threads];
        }

        private void ThreadStart(object number)
        {
            int OurSleepTime = m_info.InitialSleepTime;
            int ThreadCheckTime = 0; //Set this to 0 to start off the new thread with checking
            
            int[] numbers = number as int[];

            ThreadCheckTime = numbers[0];
            int ThreadNumber = numbers[1];
            while (true)
            {
                try
                {
                    Thread.Sleep(OurSleepTime);
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
                        OurSleepTime += 2;
                        if (OurSleepTime > m_info.MaxSleepTime) //Make sure we don't go waay over on how long we sleep
                        {
                            lock (Threads)
                            {
                                Threads[ThreadNumber] = null;
                                break;
                            }
                        }
                        continue;
                    }
                    bool Rest = false;

                    if (item != null)
                        Rest = item.Invoke();
                    else
                        Rest = (o[0] as QueueItem2).Invoke(o[1]);

                    if (Rest)
                    {
                        OurSleepTime += 10;
                        if (OurSleepTime > m_info.MaxSleepTime)
                            OurSleepTime = m_info.MaxSleepTime;
                    }
                    else
                        OurSleepTime = m_info.InitialSleepTime; //Reset sleep timer then

                    //Check to see if we need more help as we could be dumped with tons of requests and only one thread
                    if (ThreadCheckTime == 0)
                    {
                        int RunningThreads = 0;
                        lock (Threads)
                        {
                            for (int i = 0; i < Threads.Length; i++)
                            {
                                if (Threads[i] != null)
                                    RunningThreads++;
                            }
                        }
                        if (RunningThreads < queue.Count && RunningThreads != m_info.Threads)
                        {
                            //Use Math.Min to find which is smaller so we don't allocate too many or not enough threads
                            for (int i = 0; i < Math.Min(queue.Count - RunningThreads, m_info.Threads); i++)
                            {
                                Thread thread = new Thread(ThreadStart);
                                thread.Name = "Aurora Thread Pool Thread #" + i;
                                thread.Start(new int[]{15 + i, i}); //Set to fifteen plus i so the threads don't initially fight over the locked Threads and i for the thread num
                                Threads[i] = thread;
                            }
                        }
                        ThreadCheckTime = 10; //Reset the counter
                    }
                    else
                        ThreadCheckTime--; //Deincrement the time
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
            lock (Threads)
            {
                for (int i = 0; i < Threads.Length; i++)
                {
                    if (Threads[i] != null)
                        return;
                }
                Thread thread = new Thread(ThreadStart);
                thread.Name = "Aurora Thread Pool Thread #" + 0;
                thread.Start(new int[]{0,0}); //Set to 0 here to send the check for more threads the first time and 0 for the 0th thread
                Threads[0] = thread;
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
            lock (Threads)
            {
                for (int i = 0; i < Threads.Length; i++)
                {
                    if (Threads[i] != null)
                        return;
                }
                Thread thread = new Thread(ThreadStart);
                thread.Name = "Aurora Thread Pool Thread #" + 0;
                thread.Start(new int[] { 0, 0 }); //Set to 0 here to send the check for more threads the first time and 0 for the 0th thread
                Threads[0] = thread;
            }
        }
    }
}
