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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

/*****************************************************
 *
 * ScriptsHttpRequests
 *
 * Implements the llHttpRequest and http_response
 * callback.
 *
 * Some stuff was already in LSLLongCmdHandler, and then
 * there was this file with a stub class in it.  So,
 * I am moving some of the objects and functions out of
 * LSLLongCmdHandler, such as the HttpRequestClass, the
 * start and stop methods, and setting up pending and
 * completed queues.  These are processed in the
 * LSLLongCmdHandler polling loop.  Similiar to the
 * XMLRPCModule, since that seems to work.
 *
 * This probably needs some throttling mechanism but
 * it's wide open right now.  This applies to
 * number of requests
 *
 * Linden puts all kinds of header fields in the requests.
 * User-Agent
 * X-SecondLife-Shard
 * X-SecondLife-Object-Name
 * X-SecondLife-Object-Key
 * X-SecondLife-Region
 * X-SecondLife-Local-Position
 * X-SecondLife-Local-Velocity
 * X-SecondLife-Local-Rotation
 * X-SecondLife-Owner-Name
 * X-SecondLife-Owner-Key
 *
 * HTTPS support
 *
 * Configurable timeout?
 * Configurable max response size?
 * Configurable
 *
 * **************************************************/

namespace OpenSim.Region.CoreModules.Scripting.HttpRequest
{
    public class HttpRequestModule : INonSharedRegionModule, IHttpRequestModule
    {
        private object HttpListLock = new object();
        private int httpTimeout = 30000;
        private string m_name = "HttpScriptRequests";
        private int DEFAULT_BODY_MAXLENGTH = 2048;

        private string m_proxyurl = "";
        private string m_proxyexcepts = "";

        // <itemID, HttpRequestClasss>
        private Dictionary<UUID, List<HttpRequestClass>> m_pendingRequests;
        // <reqID, itemID>
        private Dictionary<UUID, UUID> m_reqID2itemID = new Dictionary<UUID, UUID>();
        private Scene m_scene;
        public class HTTPMax
        {
            public int Number = 0;
            public long LastTicks = 0;
        }
        private Dictionary<UUID, HTTPMax> m_numberOfPrimHTTPRequests = new Dictionary<UUID, HTTPMax>();
        private int MaxNumberOfHTTPRequestsPerSecond = 1;
        // private Queue<HttpRequestClass> rpcQueue = new Queue<HttpRequestClass>();

        public HttpRequestModule()
        {
        }

        #region IHttpRequestModule Members

        public UUID MakeHttpRequest(string url, string parameters, string body)
        {
            return UUID.Zero;
        }

        public UUID StartHttpRequest(UUID primID, UUID itemID, string url, List<string> parameters, Dictionary<string, string> headers, string body)
        {
            UUID reqID = UUID.Random();
            HttpRequestClass htc = new HttpRequestClass();

            // Partial implementation: support for parameter flags needed
            //   see http://wiki.secondlife.com/wiki/LlHTTPRequest
            //
            // Parameters are expected in {key, value, ... , key, value}

            int BODY_MAXLENGTH = DEFAULT_BODY_MAXLENGTH;

            if (parameters != null)
            {
                string[] parms = parameters.ToArray();
                for (int i = 0; i < parms.Length; i += 2)
                {
                    switch (Int32.Parse(parms[i]))
                    {
                        case (int)HttpRequestConstants.HTTP_METHOD:

                            htc.HttpMethod = parms[i + 1];
                            break;

                        case (int)HttpRequestConstants.HTTP_MIMETYPE:

                            htc.HttpMIMEType = parms[i + 1];
                            break;

                        case (int)HttpRequestConstants.HTTP_BODY_MAXLENGTH:

                            BODY_MAXLENGTH = int.Parse(parms[i + 1]);
                            break;

                        case (int)HttpRequestConstants.HTTP_VERIFY_CERT:

                            // TODO implement me
                            break;
                    }
                }
            }

            bool ShouldProcess = true;

            HTTPMax r = null;
            if (!m_numberOfPrimHTTPRequests.TryGetValue(primID, out r))
                r = new HTTPMax();

            if (DateTime.Now.AddSeconds(1).Ticks > r.LastTicks)
                r.Number = 0;

            if (r.Number++ > MaxNumberOfHTTPRequestsPerSecond)
            {
                ShouldProcess = false; //Too many for this prim, return status 499
                htc.Status = (int)OSHttpStatusCode.ClientErrorJoker;
                htc.Finished = true;
            }

            htc.PrimID = primID;
            htc.ItemID = itemID;
            htc.Url = url;
            htc.MaxLength = BODY_MAXLENGTH;
            htc.ReqID = reqID;
            htc.HttpTimeout = httpTimeout;
            htc.OutboundBody = body;
            htc.ResponseHeaders = headers;
            htc.proxyurl = m_proxyurl;
            htc.proxyexcepts = m_proxyexcepts;

            lock (HttpListLock)
            {
                if (m_pendingRequests.ContainsKey(itemID))
                    m_pendingRequests[itemID].Add(htc);
                else
                {
                    m_reqID2itemID.Add(reqID, itemID);
                    m_pendingRequests.Add(itemID, new List<HttpRequestClass>() { htc });
                }
            }

            if(ShouldProcess)
                htc.Process();

            return reqID;
        }

        public void StopHttpRequest(UUID primID, UUID m_itemID)
        {
            //Kill all requests and return
            if (m_pendingRequests != null)
            {
                lock (HttpListLock)
                {
                    List<HttpRequestClass> tmpReqs;
                    if (m_pendingRequests.TryGetValue(m_itemID, out tmpReqs))
                    {
                        foreach (HttpRequestClass tmpReq in tmpReqs)
                        {
                            tmpReq.Stop();
                        }
                    }
                    m_pendingRequests.Remove(m_itemID);
                }
            }
        }

        public IServiceRequest GetNextCompletedRequest()
        {
            if (m_pendingRequests.Count == 0)
                return null;
            lock (HttpListLock)
            {
                foreach (List<HttpRequestClass> luids in m_pendingRequests.Values)
                {
                    foreach (HttpRequestClass luid in luids)
                    {
                        if (luid.Finished)
                            return luid;
                    }
                }
            }
            return null;
        }

        public void RemoveCompletedRequest(UUID reqid)
        {
            lock (HttpListLock)
            {
                List<HttpRequestClass> tmpReqs;
                UUID ItemID;
                if (m_reqID2itemID.TryGetValue(reqid, out ItemID))
                {
                    if (m_pendingRequests.TryGetValue(ItemID, out tmpReqs))
                    {
                        for(int i = 0; i < tmpReqs.Count; i++)
                        {
                            if (tmpReqs[i].ReqID == reqid)
                            {
                                tmpReqs[i].Stop();
                                tmpReqs.RemoveAt(i);
                            }
                        }
                        if (tmpReqs.Count == 1)
                            m_pendingRequests.Remove(ItemID);
                        else
                            m_pendingRequests[ItemID] = tmpReqs;
                    }
                }
            }
        }

        #endregion

        #region IRegionModule Members

        public void Initialise(IConfigSource config)
        {
            m_proxyurl = config.Configs["HTTPScriptModule"].GetString("HttpProxy");
            m_proxyexcepts = config.Configs["HTTPScriptModule"].GetString("HttpProxyExceptions");

            m_pendingRequests = new Dictionary<UUID, List<HttpRequestClass>>();
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;

            m_scene.RegisterModuleInterface<IHttpRequestModule>(this);
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {

        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return m_name; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #endregion
    }

    public class HttpRequestClass: IServiceRequest
    {
        // Constants for parameters
        // public const int HTTP_BODY_MAXLENGTH = 2;
        // public const int HTTP_METHOD = 0;
        // public const int HTTP_MIMETYPE = 1;
        // public const int HTTP_VERIFY_CERT = 3;
        private bool _finished;
        public bool Finished
        {
            get { return _finished; }
            set { _finished = value; }
        }
        // public int HttpBodyMaxLen = 2048; // not implemented

        // Parameter members and default values
        public string HttpMethod  = "GET";
        public string HttpMIMEType = "text/plain;charset=utf-8";
        public int HttpTimeout;
        // public bool HttpVerifyCert = true; // not implemented
        private Thread httpThread;

        // Request info
        private UUID _itemID;
        public UUID ItemID 
        {
            get { return _itemID; }
            set { _itemID = value; }
        }
        private UUID _primID;
        public UUID PrimID
        {
            get { return _primID; }
            set { _primID = value; }
        }
        public DateTime Next;
        public string proxyurl;
        public string proxyexcepts;
        public string OutboundBody;
        private UUID _reqID;
        public UUID ReqID 
        {
            get { return _reqID; }
            set { _reqID = value; }
        }
        public HttpWebRequest Request;
        public string ResponseBody;
        public List<string> ResponseMetadata;
        public Dictionary<string, string> ResponseHeaders;
        public int Status;
        public object[] Metadata = new object[0];
        public string Url;
        public int MaxLength = 0;

        public void Process()
        {
            httpThread = new Thread(SendRequest);
            httpThread.Name = "HttpRequestThread";
            httpThread.Priority = ThreadPriority.Lowest;
            httpThread.IsBackground = true;
            _finished = false;
            httpThread.Start();
        }

        /*
         * TODO: More work on the response codes.  Right now
         * returning 200 for success or 499 for exception
         */

        public void SendRequest()
        {
            HttpWebResponse response = null;
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            string tempString = null;
            int count = 0;

            try
            {
                Request = (HttpWebRequest) WebRequest.Create(Url);

                Request.Method = HttpMethod;
                Request.ContentType = HttpMIMEType;

                if (proxyurl != null && proxyurl.Length > 0) 
                {
                    if (proxyexcepts != null && proxyexcepts.Length > 0) 
                    {
                        string[] elist = proxyexcepts.Split(';');
                        Request.Proxy = new WebProxy(proxyurl, true, elist);
                    } 
                    else 
                    {
                        Request.Proxy = new WebProxy(proxyurl, true);
                    }
                }

                foreach (KeyValuePair<string, string> entry in ResponseHeaders)
                    if (entry.Key.ToLower().Equals("user-agent"))
                        Request.UserAgent = entry.Value;
                    else
                        Request.Headers[entry.Key] = entry.Value;

                // Encode outbound data
                if (OutboundBody.Length > 0) 
                {
                    byte[] data = Util.UTF8.GetBytes(OutboundBody);

                    Request.ContentLength = data.Length;
                    Stream bstream = Request.GetRequestStream();
                    bstream.Write(data, 0, data.Length);
                    bstream.Close();
                }

                Request.Timeout = HttpTimeout;
                // execute the request
                response = (HttpWebResponse) Request.GetResponse();

                Stream resStream = response.GetResponseStream();

                do
                {
                    // fill the buffer with data
                    count = resStream.Read(buf, 0, buf.Length);

                    // make sure we read some data
                    if (count != 0)
                    {
                        // translate from bytes to ASCII text
                        tempString = Util.UTF8.GetString(buf, 0, count);

                        // continue building the string
                        sb.Append(tempString);
                    }
                } while (count > 0); // any more data to read?

                ResponseBody = sb.ToString();

                if (ResponseBody.Length > MaxLength) //Cut it off then
                {
                    ResponseBody = ResponseBody.Remove(MaxLength);
                    //Add the metaData
                    Metadata = new object[2] { 0, MaxLength };
                }
            }
            catch (Exception e)
            {
                if (e is WebException && ((WebException)e).Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse webRsp = (HttpWebResponse)((WebException)e).Response;
                    Status = (int)webRsp.StatusCode;
                    ResponseBody = webRsp.StatusDescription;
                }
                else
                {
                    Status = (int)OSHttpStatusCode.ClientErrorJoker;
                    ResponseBody = e.Message;
                }

                _finished = true;
                return;
            }
            finally
            {
                if (response != null)
                    response.Close();
            }

            Status = (int)OSHttpStatusCode.SuccessOk;
            _finished = true;
        }

        public void Stop()
        {
            try
            {
                httpThread.Abort();
            }
            catch (Exception)
            {
            }
        }
    }
}
