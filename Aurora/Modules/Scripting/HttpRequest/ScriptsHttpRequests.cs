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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;

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

namespace Aurora.Modules.Scripting
{
    public class HttpRequestModule : INonSharedRegionModule, IHttpRequestModule
    {
        private readonly object HttpListLock = new object();
        private readonly Dictionary<UUID, HTTPMax> m_numberOfPrimHTTPRequests = new Dictionary<UUID, HTTPMax>();
        private readonly Dictionary<UUID, UUID> m_reqID2itemID = new Dictionary<UUID, UUID>();
        private int DEFAULT_BODY_MAXLENGTH = 2048;
        private int MaxNumberOfHTTPRequestsPerSecond = 1;
        private int httpTimeout = 30000;
        private string m_name = "HttpScriptRequests";

        // <itemID, HttpRequestClasss>
        private Dictionary<UUID, List<HttpRequestClass>> m_pendingRequests;
        private string m_proxyexcepts = "";
        private string m_proxyurl = "";
        // <reqID, itemID>
        private IScene m_scene;
        private IScriptModule m_scriptModule;

        // private Queue<HttpRequestClass> rpcQueue = new Queue<HttpRequestClass>();

        public HttpRequestModule()
        {
            ServicePointManager.ServerCertificateValidationCallback += ValidateServerCertificate;
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #region IHttpRequestModule Members

        public UUID MakeHttpRequest(string url, string parameters, string body)
        {
            //Make sure that the cmd handler thread is running
            m_scriptModule.PokeThreads(UUID.Zero);
            return UUID.Zero;
        }

        public UUID StartHttpRequest(UUID primID, UUID itemID, string url, List<string> parameters,
                                     Dictionary<string, string> headers, string body)
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
                        case (int) HttpRequestConstants.HTTP_METHOD:

                            htc.HttpMethod = parms[i + 1];
                            break;

                        case (int) HttpRequestConstants.HTTP_MIMETYPE:

                            htc.HttpMIMEType = parms[i + 1];
                            break;

                        case (int) HttpRequestConstants.HTTP_BODY_MAXLENGTH:

                            BODY_MAXLENGTH = int.Parse(parms[i + 1]);
                            break;

                        case (int)HttpRequestConstants.HTTP_VERIFY_CERT:

                            htc.HttpVerifyCert = (int.Parse(parms[i + 1]) != 0);
                            break;

                        case (int)HttpRequestConstants.HTTP_VERBOSE_THROTTLE:

                            htc.VerbroseThrottle = (int.Parse(parms[i + 1]) != 0);
                            break;

                        case (int)HttpRequestConstants.HTTP_PRAGMA_NO_CACHE:

                            if (int.Parse(parms[i + 1]) != 0)
                            {
                                headers["Pragma"] = "no-cache";
                            }
                            break;

                        case (int)HttpRequestConstants.HTTP_CUSTOM_HEADER:

                            string name = parms[i + 1];
                            string value = parms[i + 2];
                            i++;//Move forward one, since we pulled out 3 instead of 2
                            headers[name] = value;
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
                htc.Status = (int) OSHttpStatusCode.ClientErrorJoker;
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
                    m_pendingRequests.Add(itemID, new List<HttpRequestClass> {htc});
                }
            }

            if (ShouldProcess)
                htc.Process();
            //Make sure that the cmd handler thread is running
            m_scriptModule.PokeThreads(itemID);

            return reqID;
        }

        public void StopHttpRequest(UUID primID, UUID m_itemID)
        {
            //Make sure that the cmd handler thread is running
            m_scriptModule.PokeThreads(m_itemID);
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

        public int GetRequestCount()
        {
            return m_pendingRequests.Count;
        }

        public IServiceRequest GetNextCompletedRequest()
        {
            if (m_pendingRequests.Count == 0)
                return null;
            lock (HttpListLock)
            {
                foreach (HttpRequestClass luid in from luids in m_pendingRequests.Values from luid in luids where luid.Finished select luid)
                {
                    return luid;
                }
            }
            return null;
        }

        public void RemoveCompletedRequest(IServiceRequest reqid)
        {
            HttpRequestClass req = (HttpRequestClass) reqid;
            lock (HttpListLock)
            {
                List<HttpRequestClass> tmpReqs;
                if (m_pendingRequests.TryGetValue(req.ItemID, out tmpReqs))
                {
                    for (int i = 0; i < tmpReqs.Count; i++)
                    {
                        if (tmpReqs[i].ReqID == req.ReqID)
                        {
                            tmpReqs[i].Stop();
                            tmpReqs.RemoveAt(i);
                        }
                    }
                    if (tmpReqs.Count == 1)
                        m_pendingRequests.Remove(req.ItemID);
                    else
                        m_pendingRequests[req.ItemID] = tmpReqs;
                }
            }
        }

        #endregion

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
            m_proxyurl = config.Configs["HTTPScriptModule"].GetString("HttpProxy");
            m_proxyexcepts = config.Configs["HTTPScriptModule"].GetString("HttpProxyExceptions");

            m_pendingRequests = new Dictionary<UUID, List<HttpRequestClass>>();
        }

        public void AddRegion(IScene scene)
        {
            m_scene = scene;

            m_scene.RegisterModuleInterface<IHttpRequestModule>(this);
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            m_scriptModule = scene.RequestModuleInterface<IScriptModule>();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return m_name; }
        }

        #endregion

        public static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            HttpWebRequest Request = (HttpWebRequest) sender;

            if (Request.Headers.Get("NoVerifyCert") != null)
            {
                return true;
            }
            if ((((int) sslPolicyErrors) & ~4) != 0)
                return false;
#pragma warning disable 618
            if (ServicePointManager.CertificatePolicy != null)
            {
                ServicePoint sp = Request.ServicePoint;
                return ServicePointManager.CertificatePolicy.CheckValidationResult(sp, certificate, Request, 0);
            }
#pragma warning restore 618
            return true;
        }

        public void PostInitialise()
        {
        }

        #region Nested type: HTTPMax

        public class HTTPMax
        {
            public long LastTicks;
            public int Number;
        }

        #endregion
    }

    public class HttpRequestClass : IHttpRequestClass
    {
        // Constants for parameters
        // public const int HTTP_BODY_MAXLENGTH = 2;
        // public const int HTTP_METHOD = 0;
        // public const int HTTP_MIMETYPE = 1;
        // public const int HTTP_VERIFY_CERT = 3;
        public string HttpMIMEType = "text/plain;charset=utf-8";
        public string HttpMethod = "GET";
        public int HttpTimeout;
        public bool HttpVerifyCert = true;
        private bool _VerbroseThrottle = false;
        public bool VerbroseThrottle
        {
            get { return _VerbroseThrottle; }
            set { _VerbroseThrottle = value; }
        }
        public int MaxLength;
        public object[] _Metadata = new object[0];
        public object[] Metadata
        {
            get { return _Metadata; }
            set { _Metadata = value; }
        }

        public DateTime Next;
        public string OutboundBody;

        public HttpWebRequest Request;
        public string ResponseBody { get; set; }
        public Dictionary<string, string> ResponseHeaders;
        public List<string> ResponseMetadata;
        public int Status { get; set; }
        public string Url;
        private bool _finished;
        private Thread httpThread;
        public string proxyexcepts;
        public string proxyurl;

        #region IServiceRequest Members

        public bool Finished
        {
            get { return _finished; }
            set { _finished = value; }
        }

        public UUID ItemID { get; set; }

        public UUID PrimID { get; set; }
        public UUID ReqID { get; set; }

        public void Process()
        {
            httpThread = new Thread(SendRequest)
                             {Name = "HttpRequestThread", Priority = ThreadPriority.Lowest, IsBackground = true};
            _finished = false;
            httpThread.Start();
        }

        /*
         * TODO: More work on the response codes.  Right now
         * returning 200 for success or 499 for exception
         */

        public void SendRequest()
        {
            Culture.SetCurrentCulture();
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

                if (!HttpVerifyCert)
                {
                    // Connection Group Name is probably not used so we hijack it to identify
                    // a desired security exception
//                  Request.ConnectionGroupName="NoVerify";
                    Request.Headers.Add("NoVerifyCert", "true");
                }
//              else
//              {
//                  Request.ConnectionGroupName="Verify";
//              }

                if (!string.IsNullOrEmpty(proxyurl))
                {
                    if (!string.IsNullOrEmpty(proxyexcepts))
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
                try
                {
                    // execute the request
                    response = (HttpWebResponse) Request.GetResponse();
                }
                catch (WebException e)
                {
                    if (e.Status != WebExceptionStatus.ProtocolError)
                    {
                        throw;
                    }
                    response = (HttpWebResponse)e.Response;
                }

                Status = (int)response.StatusCode;

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
                    Metadata = new object[2] {0, MaxLength};
                }
            }
            catch (Exception e)
            {
                Status = (int)OSHttpStatusCode.ClientErrorJoker;
                ResponseBody = e.Message;

                _finished = true;
                return;
            }
            finally
            {
                if (response != null)
                    response.Close();
            }

            Status = (int)HttpStatusCode.OK;
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

        #endregion
    }
}