/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using System.Linq;
using System.Threading;
using HttpServer;

namespace Aurora.Framework.Servers.HttpServer
{
    public class PollServiceRequestManager
    {
        private static readonly Queue m_requests = Queue.Synchronized(new Queue());
        private readonly PollServiceWorkerThread[] m_PollServiceWorkerThreads;
        private readonly uint m_WorkerThreadCount;
        private readonly BaseHttpServer m_server;
        private readonly int m_timeOut;
        private readonly Thread[] m_workerThreads;
        private bool m_running = true;
        private bool m_threadRunning;
        private Thread m_watcherThread;

        public PollServiceRequestManager(BaseHttpServer pSrv, uint pWorkerThreadCount, int pTimeout)
        {
            m_server = pSrv;
            m_WorkerThreadCount = pWorkerThreadCount;
            m_workerThreads = new Thread[m_WorkerThreadCount];
            m_PollServiceWorkerThreads = new PollServiceWorkerThread[m_WorkerThreadCount];
            m_timeOut = pTimeout;
        }

        internal void ReQueueEvent(PollServiceHttpRequest req)
        {
            try
            {
                // Do accounting stuff here
                Enqueue(req);
            }
            catch
            {
            }
        }

        public void Enqueue(PollServiceHttpRequest req)
        {
            lock (m_requests)
            {
                PokeThreads();
                m_requests.Enqueue(req);
            }
        }

        private void PokeThreads()
        {
            if (m_threadRunning)
                return;

            m_threadRunning = true;
            //startup worker threads
            for (uint i = 0; i < m_WorkerThreadCount; i++)
            {
                if (m_PollServiceWorkerThreads[i] == null)
                {
                    m_PollServiceWorkerThreads[i] = new PollServiceWorkerThread(m_server, m_timeOut);
                    m_PollServiceWorkerThreads[i].ReQueue += ReQueueEvent;

                    m_workerThreads[i] = new Thread(m_PollServiceWorkerThreads[i].ThreadStart)
                                             {Name = String.Format("PollServiceWorkerThread{0}", i)};
                    //Can't add to thread Tracker here Referencing Aurora.Framework creates circular reference
                    m_workerThreads[i].Start();
                }
            }

            //start watcher threads
            m_watcherThread = new Thread(ThreadStart) {Name = "PollServiceWatcherThread"};
            m_watcherThread.Start();
        }

        public void ThreadStart(object o)
        {
            while (m_running)
            {
                try
                {
                    if (!ProcessQueuedRequests())
                    {
                        m_threadRunning = false;
                        return;
                    }
                }
                catch
                {
                }
                Thread.Sleep(1000);
            }
        }

        private bool ProcessQueuedRequests()
        {
            lock (m_requests)
            {
                if (m_requests.Count == 0)
                    return false;

                int reqperthread = (int) (m_requests.Count/m_WorkerThreadCount) + 1;
                // For Each WorkerThread
                for (int tc = 0; tc < m_WorkerThreadCount && m_requests.Count > 0; tc++)
                {
                    //Loop over number of requests each thread handles.
                    for (int i = 0; i < reqperthread && m_requests.Count > 0; i++)
                    {
                        try
                        {
                            m_PollServiceWorkerThreads[tc].Enqueue((PollServiceHttpRequest) m_requests.Dequeue());
                        }
                        catch (InvalidOperationException)
                        {
                            // The queue is empty, we did our calculations wrong!
                            return false;
                        }
                    }
                }
            }
            return true;
        }


        ~PollServiceRequestManager()
        {
            foreach (PollServiceHttpRequest req in m_requests.Cast<PollServiceHttpRequest>())
            {
                m_server.DoHTTPGruntWork(
                    req.PollServiceArgs.NoEvents(req.RequestID, req.PollServiceArgs.Id),
                    new OSHttpResponse(new HttpResponse(req.HttpContext, req.Request), req.HttpContext),
                    new OSHttpRequest(req.HttpContext, req.Request)
                );
            }

            m_requests.Clear();

#if (!ISWIN)
            foreach (Thread t in m_workerThreads)
            {
                if (t != null)
                {
                    t.Abort();
                }
            }
#else
            foreach (Thread t in m_workerThreads.Where(t => t != null))
            {
                t.Abort();
            }
#endif
            m_running = false;
        }
    }
}