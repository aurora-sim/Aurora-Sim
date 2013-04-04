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
using System.IO;
using System.Reflection;
using System.Text;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Servers.HttpServer.Interfaces;
using Aurora.Framework.Utilities;
using Aurora.Framework.ConsoleFramework;

namespace Aurora.Framework.Servers.HttpServer
{
    public delegate void ReQueuePollServiceItem(PollServiceHttpRequest req);

    public class PollServiceWorkerThread
    {
        public event ReQueuePollServiceItem ReQueue;

        private readonly IHttpServer m_server;
        private BlockingQueue<PollServiceHttpRequest> m_request;
        private bool m_running = true;
        private int m_timeout = 250;

        public PollServiceWorkerThread(IHttpServer pSrv, int pTimeout)
        {
            m_request = new BlockingQueue<PollServiceHttpRequest>();
            m_server = pSrv;
            m_timeout = pTimeout;
        }

        public void ThreadStart()
        {
            Run();
        }

        public void Run()
        {
            while (m_running)
            {
                PollServiceHttpRequest req = m_request.Dequeue();

                try
                {
                    if (req.PollServiceArgs.HasEvents(req.RequestID, req.PollServiceArgs.Id))
                    {
                        StreamReader str;
                        try
                        {
                            str = new StreamReader(req.Context.Request.InputStream);
                        }
                        catch (System.ArgumentException)
                        {
                            // Stream was not readable means a child agent
                            // was closed due to logout, leaving the
                            // Event Queue request orphaned.
                            continue;
                        }

                        OSHttpResponse response = new OSHttpResponse(req.Context);

                        byte[] buffer = req.PollServiceArgs.GetEvents(req.RequestID, req.PollServiceArgs.Id,
                                                                               str.ReadToEnd(), response);

                        response.SendChunked = false;
                        response.ContentLength64 = buffer.Length;
                        response.ContentEncoding = Encoding.UTF8;

                        try
                        {
                            response.OutputStream.Write(buffer, 0, buffer.Length);
                        }
                        catch (Exception ex)
                        {
                            MainConsole.Instance.WarnFormat("[POLL SERVICE WORKER THREAD]: Error: {0}", ex.ToString());
                        }
                        finally
                        {
                            //response.OutputStream.Close();
                            try
                            {
                                response.OutputStream.Close();
                                response.Send();

                                //if (!response.KeepAlive && response.ReuseContext)
                                //    response.FreeContext();
                            }
                            catch (Exception e)
                            {
                                MainConsole.Instance.WarnFormat("[POLL SERVICE WORKER THREAD]: Error: {0}", e.ToString());
                            }
                        }
                    }
                    else
                    {
                        if ((Environment.TickCount - req.RequestTime) > m_timeout)
                        {
                            OSHttpResponse response = new OSHttpResponse(req.Context);

                            byte[] buffer = req.PollServiceArgs.NoEvents(req.RequestID, req.PollServiceArgs.Id, response);

                            response.SendChunked = false;
                            response.ContentLength64 = buffer.Length;
                            response.ContentEncoding = Encoding.UTF8;

                            try
                            {
                                response.OutputStream.Write(buffer, 0, buffer.Length);
                            }
                            catch (Exception ex)
                            {
                                MainConsole.Instance.WarnFormat("[POLL SERVICE WORKER THREAD]: Error: {0}", ex.ToString());
                            }
                            finally
                            {
                                //response.OutputStream.Close();
                                try
                                {
                                    response.OutputStream.Close();
                                    response.Send();

                                    //if (!response.KeepAlive && response.ReuseContext)
                                    //    response.FreeContext();
                                }
                                catch (Exception e)
                                {
                                    MainConsole.Instance.WarnFormat("[POLL SERVICE WORKER THREAD]: Error: {0}", e.ToString());
                                }
                            }
                        }
                        else
                        {
                            ReQueuePollServiceItem reQueueItem = ReQueue;
                            if (reQueueItem != null)
                                reQueueItem(req);
                        }
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("Exception in poll service thread: {0}", e.ToString());
                }
            }
        }

        internal void Enqueue(PollServiceHttpRequest pPollServiceHttpRequest)
        {
            m_request.Enqueue(pPollServiceHttpRequest);
        }
    }
}