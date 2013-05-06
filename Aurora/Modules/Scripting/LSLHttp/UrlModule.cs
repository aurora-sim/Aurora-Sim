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

using Aurora.Framework;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Nini.Config;
using OpenMetaverse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Aurora.Modules.Scripting
{
    public class UrlData
    {
        public UUID hostID;
        public UUID itemID;
        public IScriptModule engine;
        public string url;
        public UUID urlcode;
        public Dictionary<UUID, RequestData> requests;
    }

    public class RequestData
    {
        public UUID requestID;
        public Dictionary<string, string> headers;
        public string body;
        public int responseCode;
        public string responseBody;
        public string contentType;
        //public ManualResetEvent ev;
        public bool requestDone;
        public int startTime;
        public string uri;
    }

    public class UrlModule : INonSharedRegionModule, IUrlModule
    {
        private readonly Dictionary<UUID, UrlData> m_RequestMap =
            new Dictionary<UUID, UrlData>();

        private readonly Dictionary<string, UrlData> m_UrlMap =
            new Dictionary<string, UrlData>();


        private const int m_TotalUrls = 100;

        public string ExternalHostNameForLSL
        {
            get { return MainServer.Instance.HostName; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "UrlModule"; }
        }

        public void Initialise(IConfigSource config)
        {
        }

        public void AddRegion(IScene scene)
        {
            scene.RegisterModuleInterface<IUrlModule>(this);
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void Close()
        {
        }

        public UUID RequestURL(IScriptModule engine, ISceneChildEntity host, UUID itemID)
        {
            UUID urlcode = UUID.Random();

            lock (m_UrlMap)
            {
                if (m_UrlMap.Count >= m_TotalUrls)
                {
                    engine.PostScriptEvent(itemID, host.UUID, "http_request",
                                           new Object[] {urlcode.ToString(), "URL_REQUEST_DENIED", ""});
                    return urlcode;
                }
                string url = MainServer.Instance.ServerURI + "/lslhttp/" + urlcode.ToString() + "/";

                UrlData urlData = new UrlData
                                      {
                                          hostID = host.UUID,
                                          itemID = itemID,
                                          engine = engine,
                                          url = url,
                                          urlcode = urlcode,
                                          requests = new Dictionary<UUID, RequestData>()
                                      };


                m_UrlMap[url] = urlData;

                string uri = "/lslhttp/" + urlcode.ToString() + "/";
                MainServer.Instance.AddPollServiceHTTPHandler(uri, new PollServiceEventArgs(HttpRequestHandler, HasEvents,
                                                                                       GetEvents, NoEvents,
                                                                                       urlcode));

                engine.PostScriptEvent(itemID, host.UUID, "http_request",
                                       new Object[] {urlcode.ToString(), "URL_REQUEST_GRANTED", url});
            }

            return urlcode;
        }

        public UUID RequestSecureURL(IScriptModule engine, ISceneChildEntity host, UUID itemID)
        {
            UUID urlcode = UUID.Random();

            engine.PostScriptEvent(itemID, host.UUID, "http_request",
                                   new Object[] {urlcode.ToString(), "URL_REQUEST_DENIED", ""});

            return urlcode;
        }

        public void ReleaseURL(string url)
        {
            lock (m_UrlMap)
            {
                UrlData data;

                if (!m_UrlMap.TryGetValue(url, out data))
                {
                    return;
                }

                foreach (UUID req in data.requests.Keys)
                    m_RequestMap.Remove(req);

                RemoveUrl(data);
                m_UrlMap.Remove(url);
            }
        }

        public void SetContentType(UUID request, string content_type)
        {
            if (m_RequestMap.ContainsKey(request))
            {
                UrlData urlData = m_RequestMap[request];
                urlData.requests[request].contentType = content_type;
            }
        }

        public void HttpResponse(UUID request, int status, string body)
        {
            if (m_RequestMap.ContainsKey(request))
            {
                UrlData urlData = m_RequestMap[request];
                urlData.requests[request].responseCode = status;
                urlData.requests[request].responseBody = body;
                //urlData.requests[request].ev.Set();
                urlData.requests[request].requestDone = true;
            }
            else
            {
                MainConsole.Instance.Info("[HttpRequestHandler] There is no http-in request with id " +
                                          request.ToString());
            }
        }

        public string GetHttpHeader(UUID requestId, string header)
        {
            if (m_RequestMap.ContainsKey(requestId))
            {
                UrlData urlData = m_RequestMap[requestId];
                string value;
                if (urlData.requests[requestId].headers.TryGetValue(header, out value))
                    return value;
            }
            else
            {
                MainConsole.Instance.Warn("[HttpRequestHandler] There was no http-in request with id " + requestId);
            }
            return String.Empty;
        }

        public int GetFreeUrls()
        {
            return m_TotalUrls - m_UrlMap.Count;
        }

        public void ScriptRemoved(UUID itemID)
        {
            lock (m_UrlMap)
            {
                List<string> removeURLs = new List<string>();

                foreach (KeyValuePair<string, UrlData> url in m_UrlMap)
                {
                    if (url.Value.itemID == itemID)
                    {
                        RemoveUrl(url.Value);
                        removeURLs.Add(url.Key);
                        foreach (UUID req in url.Value.requests.Keys)
                            m_RequestMap.Remove(req);
                    }
                }

                foreach (string urlname in removeURLs)
                    m_UrlMap.Remove(urlname);
            }
        }

        public void ObjectRemoved(UUID objectID)
        {
            lock (m_UrlMap)
            {
                List<string> removeURLs = new List<string>();

                foreach (KeyValuePair<string, UrlData> url in m_UrlMap)
                {
                    if (url.Value.hostID == objectID)
                    {
                        RemoveUrl(url.Value);
                        removeURLs.Add(url.Key);
                        foreach (UUID req in url.Value.requests.Keys)
                            m_RequestMap.Remove(req);
                    }
                }

                foreach (string urlname in removeURLs)
                    m_UrlMap.Remove(urlname);
            }
        }


        private void RemoveUrl(UrlData data)
        {
            MainServer.Instance.RemovePollServiceHTTPHandler("", "/lslhttp/" + data.urlcode.ToString() + "/");
        }

        private byte[] NoEvents(UUID requestID, UUID sessionID, OSHttpResponse response)
        {
            UrlData url;
            lock (m_RequestMap)
            {
                if (!m_RequestMap.ContainsKey(requestID))
                    return MainServer.BlankResponse;
                url = m_RequestMap[requestID];
            }

            if (Environment.TickCount - url.requests[requestID].startTime > 25000)
            {
                response.StatusCode = 500;
                response.ContentType = "text/plain";

                //remove from map
                lock (url)
                {
                    url.requests.Remove(requestID);
                    m_RequestMap.Remove(requestID);
                }

                return Encoding.UTF8.GetBytes("Script timeout");
            }


            return MainServer.BlankResponse;
        }

        private bool HasEvents(UUID requestID, UUID sessionID)
        {
            UrlData url = null;

            lock (m_RequestMap)
            {
                if (!m_RequestMap.ContainsKey(requestID))
                {
                    return false;
                }
                url = m_RequestMap[requestID];
                if (!url.requests.ContainsKey(requestID))
                {
                    return false;
                }
            }

            if (Environment.TickCount - url.requests[requestID].startTime > 25000)
            {
                return true;
            }

            if (url.requests[requestID].requestDone)
                return true;
            return false;
        }

        private byte[] GetEvents(UUID requestID, UUID sessionID, string req, OSHttpResponse response)
        {
            UrlData url = null;
            RequestData requestData = null;

            lock (m_RequestMap)
            {
                if (!m_RequestMap.ContainsKey(requestID))
                    return NoEvents(requestID, sessionID, response);
                url = m_RequestMap[requestID];
                requestData = url.requests[requestID];
            }

            if (!requestData.requestDone)
                return NoEvents(requestID, sessionID, response);

            if (Environment.TickCount - requestData.startTime > 25000)
            {
                response.ContentType = "text/plain";
                response.StatusCode = 500;
                return Encoding.UTF8.GetBytes("Script timeout");
            }
            //put response
            response.StatusCode = requestData.responseCode;
            response.ContentType = requestData.contentType;
            //remove from map
            lock (url)
            {
                url.requests.Remove(requestID);
                m_RequestMap.Remove(requestID);
            }

            return Encoding.UTF8.GetBytes(requestData.responseBody);
        }

        public void HttpRequestHandler(UUID requestID, OSHttpRequest request)
        {
            lock (request)
            {
                string uri = request.RawUrl;

                try
                {
                    int pos1 = uri.IndexOf("/"); // /lslhttp
                    int pos2 = uri.IndexOf("/", pos1 + 1); // /lslhttp/
                    int pos3 = uri.IndexOf("/", pos2 + 1); // /lslhttp/<UUID>/
                    string uri_tmp = uri.Substring(0, pos3 + 1);
                    
                    string pathInfo = uri.Substring(pos3);

                    UrlData url = m_UrlMap[MainServer.Instance.ServerURI + uri_tmp];

                    //for llGetHttpHeader support we need to store original URI here
                    //to make x-path-info / x-query-string / x-script-url / x-remote-ip headers 
                    //as per http://wiki.secondlife.com/wiki/LlGetHTTPHeader

                    RequestData requestData = new RequestData
                                                  {
                                                      requestID = requestID,
                                                      requestDone = false,
                                                      startTime = Environment.TickCount,
                                                      uri = uri
                                                  };
                    if (requestData.headers == null)
                        requestData.headers = new Dictionary<string, string>();

                    foreach (KeyValuePair<string, string> header in request.Headers)
                    {
                        string key = header.Key;
                        string value = header.Value;
                        requestData.headers.Add(key, value);
                    }

                    //if this machine is behind DNAT/port forwarding, currently this is being
                    //set to address of port forwarding router
                    requestData.headers["x-remote-ip"] = requestData.headers["remote_addr"];
                    requestData.headers["x-path-info"] = pathInfo;
                    requestData.headers["x-query-string"] = request.QueryString.ToString();
                    requestData.headers["x-script-url"] = url.url;

                    //requestData.ev = new ManualResetEvent(false);
                    lock (url.requests)
                    {
                        url.requests.Add(requestID, requestData);
                    }
                    lock (m_RequestMap)
                    {
                        //add to request map
                        m_RequestMap.Add(requestID, url);
                    }

                    url.engine.PostScriptEvent(url.itemID, url.hostID, "http_request",
                                               new Object[]
                                                   {
                                                       requestID.ToString(), request.HttpMethod,
                                                       HttpServerHandlerHelpers.ReadString(request.InputStream)
                                                   });
                }
                catch (Exception we)
                {
                    //Hashtable response = new Hashtable();
                    MainConsole.Instance.Warn("[HttpRequestHandler]: http-in request failed");
                    MainConsole.Instance.Warn(we.Message);
                    MainConsole.Instance.Warn(we.StackTrace);
                }
            }
        }

        private void OnScriptReset(uint localID, UUID itemID)
        {
            ScriptRemoved(itemID);
        }
    }
}