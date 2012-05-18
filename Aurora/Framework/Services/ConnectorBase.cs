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
using System.Diagnostics;
using System.Data;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class ConnectorRegistry
    {
        public static List<ConnectorBase> Connectors = new List<ConnectorBase>();
        public static void RegisterConnector(ConnectorBase con)
        {
            Connectors.Add(con);
        }
    }

    public class ConnectorBase
    {
        protected IRegistryCore m_registry;
        protected IConfigurationService m_configService
        {
            get { return m_registry.RequestModuleInterface<IConfigurationService>(); }
        }
        protected bool m_doRemoteCalls = false;
        protected string m_name;
        protected bool m_doRemoteOnly = false;
        protected int m_OSDRequestTimeout = 10000;
        protected int m_OSDRequestTryCount = 7;

        public string PluginName
        {
            get { return m_name; }
        }

        public bool Enabled
        {
            get;
            set;
        }

        public void Init(IRegistryCore registry, string name)
        {
            Enabled = true;
            m_registry = registry;
            m_name = name;
            IConfigSource source = registry.RequestModuleInterface<ISimulationBase>().ConfigSource;
            IConfig config;
            if ((config = source.Configs["AuroraConnectors"]) != null)
                m_doRemoteCalls = config.GetBoolean("DoRemoteCalls", false);
            if ((config = source.Configs["Configuration"]) != null)
            {
                m_OSDRequestTimeout = config.GetInt("OSDRequestTimeout", m_OSDRequestTimeout);
                m_OSDRequestTryCount = config.GetInt("OSDRequestTryCount", m_OSDRequestTryCount);
            }
            if (m_doRemoteCalls)
                m_doRemoteOnly = true;//Lock out local + remote for now
            ConnectorRegistry.RegisterConnector(this);
        }

        public object DoRemote(params object[] o)
        {
            return DoRemoteCall(false, "ServerURI", UUID.Zero, o);
        }

        public object DoRemoteForced(params object[] o)
        {
            return DoRemoteCall(true, "ServerURI", UUID.Zero, o);
        }

        public object DoRemoteForUser(UUID userID, params object[] o)
        {
            return DoRemoteCall(false, "ServerURI", UUID.Zero, o);
        }

        public object DoRemoteByURL(string url, params object[] o)
        {
            return DoRemoteCall(false, url, UUID.Zero, o);
        }

        public object DoRemoteCall(bool forced, string url, UUID userID, params object[] o)
        {
            if (!m_doRemoteCalls && !forced)
                return null;
            StackTrace stackTrace = new StackTrace();
            int upStack = 1;
            if (userID == UUID.Zero)
                upStack = 2;
            MethodInfo method = (MethodInfo)stackTrace.GetFrame(upStack).GetMethod();
            CanBeReflected reflection = (CanBeReflected)Attribute.GetCustomAttribute(method, typeof(CanBeReflected));
            string methodName = reflection != null && reflection.RenamedMethod != "" ? reflection.RenamedMethod : method.Name;
            OSDMap map = new OSDMap();
            map["Method"] = methodName;
            int i = 0;
            var parameters = method.GetParameters();
            if (o.Length != parameters.Length)
            {
                MainConsole.Instance.ErrorFormat("FAILED TO GET VALID NUMBER OF PARAMETERS TO SEND REMOTELY FOR {0}, EXPECTED {1}, GOT {2}", methodName, parameters.Length, o.Length);
                return null;
            }
            foreach(ParameterInfo info in parameters)
            {
                OSD osd = o[i] == null ? null : Util.MakeOSD(o[i], o[i].GetType());
                if(osd != null)
                    map.Add(info.Name, osd);
                i++;
            }
            List<string> m_ServerURIs =
                    m_configService.FindValueOf(userID.ToString(), url, false);
            OSDMap response = null;
            int loops2Do = (m_ServerURIs.Count < m_OSDRequestTryCount) ? m_ServerURIs.Count : m_OSDRequestTryCount;
            for (int index = 0; index < loops2Do; index++)
            {
                string uri = m_ServerURIs[index];
                if (GetOSDMap(uri, map, out response))
                    break;
            }
            if (response == null || !response)
                return null;
            object inst = null;
            try
            {
                if (method.ReturnType == typeof(string))
                    inst = string.Empty;
                else if (method.ReturnType == typeof(void))
                    return null;
                else
                    inst = Activator.CreateInstance(method.ReturnType);
            }
            catch
            {
                if (method.ReturnType == typeof(string))
                    inst = string.Empty;
            }
            if (response["Value"] == "null")
                return null;
            var instance = inst as IDataTransferable;
            if (instance != null)
            {
                instance.FromOSD((OSDMap)response["Value"]);
                return instance;
            }
            return Util.OSDToObject(response["Value"], method.ReturnType);
        }

        public bool GetOSDMap(string url, OSDMap map, out OSDMap response)
        {
            response = null;
            string resp = ServiceOSDRequest(url, map, "POST", m_OSDRequestTimeout);
            
            if (resp == "" || resp.StartsWith("<"))
                return false;
            try
            {
                response = (OSDMap)OSDParser.DeserializeJson(resp);
            }
            catch
            {
                response = null;
                return false;
            }
            return response["Success"];
        }

        public static string ServiceOSDRequest(string url, OSDMap data, string method, int timeout)
        {
            // MainConsole.Instance.DebugFormat("[WEB UTIL]: <{0}> start osd request for {1}, method {2}",reqnum,url,method);

            string errorMessage = "unknown error";
            int tickstart = Util.EnvironmentTickCount();
            int tickdata = 0;
            int tickserialize = 0;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = method;
                request.Timeout = timeout;
                request.KeepAlive = false;
                request.MaximumAutomaticRedirections = 10;
                request.ReadWriteTimeout = timeout / 2;

                // If there is some input, write it into the request
                if (data != null)
                {
                    string strBuffer = OSDParser.SerializeJsonString(data, true);
                    byte[] buffer = Encoding.UTF8.GetBytes(strBuffer);

                    if (buffer.Length <= 0)
                    {
                    }
                    else
                    {
                        request.ContentType = "application/json";
                        request.ContentLength = buffer.Length; //Count bytes to send
                        using (Stream requestStream = request.GetRequestStream())
                            requestStream.Write(buffer, 0, buffer.Length); //Send it
                    }
                }

                // capture how much time was spent writing, this may seem silly
                // but with the number concurrent requests, this often blocks
                tickdata = Util.EnvironmentTickCountSubtract(tickstart);

                using (WebResponse response = request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        // capture how much time was spent writing, this may seem silly
                        // but with the number concurrent requests, this often blocks
                        tickserialize = Util.EnvironmentTickCountSubtract(tickstart) - tickdata;
                        string responseStr = responseStream.GetStreamString();
                        // MainConsole.Instance.DebugFormat("[WEB UTIL]: <{0}> response is <{1}>",reqnum,responseStr);
                        return responseStr;
                    }
                }
            }
            catch (WebException we)
            {
                errorMessage = we.Message;
                if (we.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse webResponse = (HttpWebResponse)we.Response;
                    errorMessage = String.Format("[{0}] {1}", webResponse.StatusCode, webResponse.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            finally
            {
                if (errorMessage == "unknown error")
                {
                    // This just dumps a warning for any operation that takes more than 500 ms
                    int tickdiff = Util.EnvironmentTickCountSubtract(tickstart);
                    MainConsole.Instance.TraceFormat(
                        "[WebUtils]: osd request took too long (URI:{0}, METHOD:{1}) took {2}ms overall, {3}ms writing, {4}ms deserializing",
                        url, method, tickdiff, tickdata, tickserialize);
                    if (tickdiff > 5000)
                        MainConsole.Instance.InfoFormat(
                            "[WebUtils]: osd request took too long (URI:{0}, METHOD:{1}) took {2}ms overall, {3}ms writing, {4}ms deserializing",
                            url, method, tickdiff, tickdata, tickserialize);
                }
            }

            MainConsole.Instance.WarnFormat("[WebUtils] osd request failed: {0} to {1}, data {2}", errorMessage, url,
                             data != null ? data.AsString() : "");
            return "";
        }
    }
}