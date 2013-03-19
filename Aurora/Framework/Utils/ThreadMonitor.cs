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
using System.Collections.Generic;
using System.Threading;

namespace Aurora.Framework
{
    public class ThreadMonitor
    {
        #region Delegates

        public delegate bool Heartbeat();

        #endregion

        protected List<InternalHeartbeat> m_heartbeats = new List<InternalHeartbeat>();
        protected Object m_lock = new Object();
        private int m_sleepTime;
        protected int m_timesToIterate;
        protected Thread m_thread;
        private bool startedshutdown = false;

        /// <summary>
        ///     Add this delegate to the tracker so that it can run.
        /// </summary>
        /// <param name="millisecondTimeOut">The time that the thread can run before it is forcefully stopped.</param>
        /// <param name="hb">The delegate to run.</param>
        public void StartTrackingThread(int millisecondTimeOut, Heartbeat hb)
        {
            lock (m_lock)
            {
                m_heartbeats.Add(new InternalHeartbeat {heartBeat = hb, millisecondTimeOut = millisecondTimeOut});
            }
        }

        /// <summary>
        ///     Start the thread and run through the threads that are given.
        /// </summary>
        /// <param name="timesToIterate">
        ///     The number of times to run the delegate.
        ///     <remarks>
        ///         If you set this parameter to 0, it will loop infinitely.
        ///     </remarks>
        /// </param>
        /// <param name="sleepTime">
        ///     The sleep time between each iteration.
        ///     <remarks>
        ///         If you set this parameter to 0, it will loop without sleeping at all.
        ///         The sleeping will have to be deal with in the delegates.
        ///     </remarks>
        /// </param>
        public void StartMonitor(int timesToIterate, int sleepTime)
        {
            m_timesToIterate = timesToIterate;
            m_sleepTime = sleepTime;

            m_thread = new Thread(Run)
                           {IsBackground = true, Name = "ThreadMonitor", Priority = ThreadPriority.Normal};
            m_thread.Start();
        }

        /// <summary>
        ///     Run the loop through the heartbeats.
        /// </summary>
        protected internal void Run()
        {
            Culture.SetCurrentCulture();
            try
            {
                List<InternalHeartbeat> hbToRemove = null;
                while ((m_timesToIterate >= 0) && (!startedshutdown))
                {
                    lock (m_lock)
                    {
                        foreach (InternalHeartbeat intHB in m_heartbeats)
                        {
                            bool isRunning = false;
                            if (!CallAndWait(intHB.millisecondTimeOut, intHB.heartBeat, out isRunning))
                            {
                                MainConsole.Instance.Warn(
                                    "[ThreadTracker]: Could not run Heartbeat in specified limits!");
                            }
                            else if (!isRunning)
                            {
                                if (hbToRemove == null)
                                    hbToRemove = new List<InternalHeartbeat>();
                                hbToRemove.Add(intHB);
                            }
                        }

                        if (hbToRemove != null)
                        {
                            foreach (InternalHeartbeat intHB in hbToRemove)
                            {
                                m_heartbeats.Remove(intHB);
                            }
                            //Renull it for later
                            hbToRemove = null;
                            if (m_heartbeats.Count == 0) //None left, break
                                break;
                        }
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
            }
            catch
            {
            }
            Thread.CurrentThread.Abort();
        }

        /// <summary>
        ///     Call the method and wait for it to complete or the max time.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="enumerator"></param>
        /// <param name="isRunning"></param>
        /// <returns></returns>
        public static bool CallAndWait(int timeout, Heartbeat enumerator, out bool isRunning)
        {
            isRunning = false;
            bool RetVal = false;
            if (timeout == 0)
            {
                isRunning = enumerator();
                RetVal = true;
            }
            else
            {
                //The action to fire
                FireEvent wrappedAction = delegate(Heartbeat en)
                                              {
                                                  // Set this culture for the thread 
                                                  // to en-US to avoid number parsing issues
                                                  Culture.SetCurrentCulture();
                                                  en();
                                                  RetVal = true;
                                              };

                //Async the action (yeah, this is bad, but otherwise we can't abort afaik)
                IAsyncResult result = wrappedAction.BeginInvoke(enumerator, null, null);
                if (((timeout != 0) && !result.IsCompleted) &&
                    (!result.AsyncWaitHandle.WaitOne(timeout, false) || !result.IsCompleted))
                {
                    isRunning = false;
                    return false;
                }
                else
                {
                    wrappedAction.EndInvoke(result);
                    isRunning = true;
                }
            }
            //Return what we got
            return RetVal;
        }

        public void Stop()
        {
            startedshutdown = true;
            lock (m_lock)
            {
                //Remove all
                m_heartbeats.Clear();
                //Kill it
                m_timesToIterate = -1;
                if (m_thread != null)
                    m_thread.Join();
                m_thread = null;
            }
        }

        #region Nested type: FireEvent

        protected internal delegate void FireEvent(Heartbeat thread);

        #endregion

        #region Nested type: InternalHeartbeat

        protected internal class InternalHeartbeat
        {
            public Heartbeat heartBeat;
            public int millisecondTimeOut;
        }

        #endregion
    }
}