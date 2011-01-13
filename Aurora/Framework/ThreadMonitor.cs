using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aurora.Framework
{
    public class ThreadMonitor
    {
        protected internal class InternalHeartbeat
        {
            public Heartbeat heartBeat;
            public int millisecondTimeOut;
        }
        public delegate void Heartbeat();
        protected internal delegate void FireEvent(Heartbeat thread);
        protected Object m_lock = new Object();
        protected List<InternalHeartbeat> m_heartbeats = new List<InternalHeartbeat>();
        protected int m_timesToIterate = 0;
        private int m_sleepTime = 0;

        /// <summary>
        /// Add this delegate to the tracker so that it can run.
        /// </summary>
        /// <param name="millisecondTimeOut">The time that the thread can run before it is forcefully stopped.</param>
        /// <param name="hb">The delegate to run.</param>
        public void StartTrackingThread(int millisecondTimeOut, Heartbeat hb)
        {
            lock (m_lock)
            {
                m_heartbeats.Add(new InternalHeartbeat() { heartBeat = hb, millisecondTimeOut = millisecondTimeOut });
            }
        }

        /// <summary>
        /// Start the thread and run through the threads that are given.
        /// </summary>
        /// <param name="timesToIterate">The number of times to run the delegate.
        /// <remarks>If you set this parameter to 0, it will loop infinitely.</remarks></param>
        /// <param name="sleepTime">The sleep time between each iteration.
        /// <remarks>If you set this parameter to 0, it will loop without sleeping at all.
        /// The sleeping will have to be deal with in the delegates.</remarks></param>
        public void StartMonitor(int timesToIterate, int sleepTime)
        {
            m_timesToIterate = timesToIterate;
            m_sleepTime = sleepTime;

            Thread thread = new Thread(Run);
            thread.IsBackground = true;
            thread.Name = "ThreadMonitor";
            thread.Priority = ThreadPriority.BelowNormal;
            thread.Start();
        }

        /// <summary>
        /// Run the loop through the heartbeats.
        /// </summary>
        protected internal void Run()
        {
            while (m_timesToIterate >= 0)
            {
                InternalHeartbeat[] heartbeats = new InternalHeartbeat[m_heartbeats.Count];
                //Pull them out so we don't have issues with locking this the whole time we are doing the async pieces
                lock (m_lock)
                {
                    m_heartbeats.CopyTo(heartbeats);
                }
                foreach (InternalHeartbeat intHB in heartbeats)
                {
                    if (!CallAndWait(intHB.millisecondTimeOut, intHB.heartBeat))
                        Console.WriteLine("WARNING: Could not run Heartbeat in specified limits!");
                }
                //0 is infinite
                if (m_timesToIterate != 0)
                {
                    //Subtract, then see if it is 0, and if it is, it is time to stop
                    m_timesToIterate--;
                    if (m_timesToIterate == 0)
                        break;
                }
                if (m_timesToIterate == -1) //Kill signal
                    break;
                if (m_sleepTime != 0)
                    Thread.Sleep(m_sleepTime);
            }
            Thread.CurrentThread.Abort();
        }

        /// <summary>
        /// Call the method and wait for it to complete or the max time.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="enumerator"></param>
        /// <returns></returns>
        protected bool CallAndWait(int timeout, Heartbeat enumerator)
        {
            bool RetVal = false;
            //The action to fire
            FireEvent wrappedAction = delegate(Heartbeat en)
            {
                en();
                RetVal = true;
            };

            //Async the action (yeah, this is bad, but otherwise we can't abort afaik)
            IAsyncResult result = wrappedAction.BeginInvoke(enumerator, null, null);
            if (((timeout != 0) && !result.IsCompleted) &&
                (!result.AsyncWaitHandle.WaitOne(timeout, false) || !result.IsCompleted))
            {
                return false;
            }
            else
            {
                wrappedAction.EndInvoke(result);
            }
            //Return what we got
            return RetVal;
        }

        public void Stop()
        {
            lock (m_lock)
            {
                //Remove all
                m_heartbeats.Clear();
                //Kill it
                m_timesToIterate = -1;
            }
        }
    }
}
