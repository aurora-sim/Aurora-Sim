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
using System.Collections.Generic;
using System.IO;
using System.Net; // to be used for REST-->Grid shortly
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Mono.Data.SqliteClient;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Region.UserStatistics
{
    public class WebStatsModule : ISharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        //private static SqliteConnection dbConn;
        private Dictionary<UUID, UserSessionID> m_sessions = new Dictionary<UUID, UserSessionID>();
        private List<Scene> m_scene = new List<Scene>();
        private Dictionary<string, IStatsController> reports = new Dictionary<string, IStatsController>();
        private Dictionary<UUID, USimStatsData> m_simstatsCounters = new Dictionary<UUID, USimStatsData>(); 
        private const int updateStatsMod = 6;
        private int updateLogMod = 1;
        private volatile int updateLogCounter = 0;
        private volatile int concurrencyCounter = 0;
        private bool enabled = false;
        private string m_loglines = String.Empty;
        private volatile int lastHit = 12000;
        private IWebStatsDataConnector dataConnector = null;
        public virtual void Initialise(IConfigSource config)
        {
            IConfig cnfg;
            try
            {
                cnfg = config.Configs["WebStats"];
                enabled = cnfg.GetBoolean("enabled", false);
            } 
            catch (Exception)
            {
                enabled = false;
            }
        }

        public void AddRegion(Scene scene)
        {
            if (!enabled)
                return;
            
            lock (m_scene)
            {
                if (m_scene.Count == 0)
                {
                    dataConnector = Aurora.DataManager.DataManager.RequestPlugin<IWebStatsDataConnector>();
                    if (dataConnector == null)
                    {
                        enabled = false;
                        return;
                    }
                    Default_Report rep = new Default_Report();
                    Prototype_distributor protodep = new Prototype_distributor();
                    Updater_distributor updatedep = new Updater_distributor();
                    ActiveConnectionsAJAX ajConnections = new ActiveConnectionsAJAX();
                    SimStatsAJAX ajSimStats = new SimStatsAJAX();
                    LogLinesAJAX ajLogLines = new LogLinesAJAX();
                    Clients_report clientReport = new Clients_report();
                    Sessions_Report sessionsReport = new Sessions_Report();

                    reports.Add("home", rep);
                    reports.Add("", rep);
                    reports.Add("prototype.js", protodep);
                    reports.Add("updater.js", updatedep);
                    reports.Add("activeconnectionsajax.html", ajConnections);
                    reports.Add("simstatsajax.html", ajSimStats);
                    reports.Add("activelogajax.html", ajLogLines);
                    reports.Add("clients.report", clientReport);
                    reports.Add("sessions.report", sessionsReport);



                    ////
                    // Add Your own Reports here (Do Not Modify Lines here Devs!)
                    ////

                    ////
                    // End Own reports section
                    //// 


                    MainServer.Instance.AddHTTPHandler("/SStats/", HandleStatsRequest);
                    MainServer.Instance.AddHTTPHandler("/CAPS/VS/", HandleUnknownCAPSRequest);
                }

                m_scene.Add(scene);
                if (m_simstatsCounters.ContainsKey(scene.RegionInfo.RegionID))
                    m_simstatsCounters.Remove(scene.RegionInfo.RegionID);

                m_simstatsCounters.Add(scene.RegionInfo.RegionID, new USimStatsData(scene.RegionInfo.RegionID));
                IMonitorModule mod = scene.RequestModuleInterface<IMonitorModule>();
                if (mod != null)
                    mod.OnSendStatsResult += ReceiveClassicSimStatsPacket;
            }
        }

        public void RemoveRegion(Scene scene)
        {
            if (!enabled)
                return;

            m_sessions.Clear();
            m_scene.Clear();
            reports.Clear();
            m_simstatsCounters.Clear(); 
        }

        public void RegionLoaded(Scene scene)
        {
            if (!enabled)
                return;

            AddHandlers();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void ReceiveClassicSimStatsPacket(SimStats stats)
        {
            if (!enabled)
            {
                return;
            }

            try
            {
                // Ignore the update if there's a report running right now
                // ignore the update if there hasn't been a hit in 30 seconds.
                if (concurrencyCounter > 0 || Util.EnvironmentTickCount() - lastHit > 30000)
                    return;

                if ((updateLogCounter++ % updateLogMod) == 0)
                {
                    m_loglines = readLogLines(10);
                    if (updateLogCounter > 10000) updateLogCounter = 1;
                }

                USimStatsData ss = m_simstatsCounters[stats.RegionUUID];

                if ((++ss.StatsCounter % updateStatsMod) == 0)
                {
                    ss.ConsumeSimStats(stats);
                }
            } 
            catch (KeyNotFoundException)
            {
            }
        }
        
        public Hashtable HandleUnknownCAPSRequest(Hashtable request)
        {
            //string regpath = request["uri"].ToString();
            int response_code = 200;
            string contenttype = "text/html";
            dataConnector.UpdateUserStats(ParseViewerStats(request["body"].ToString(), UUID.Zero));
            Hashtable responsedata = new Hashtable();

            responsedata["int_response_code"] = response_code;
            responsedata["content_type"] = contenttype;
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = string.Empty;
            return responsedata;
        }

        public Hashtable HandleStatsRequest(Hashtable request)
        {
            lastHit = Util.EnvironmentTickCount();
            Hashtable responsedata = new Hashtable();
            string regpath = request["uri"].ToString();
            int response_code = 404;
            string contenttype = "text/html";
            
            string strOut = string.Empty;

            regpath = regpath.Remove(0, 8);
            if (reports.ContainsKey(regpath))
            {
                IStatsController rep = reports[regpath];
                Hashtable repParams = new Hashtable();

                if (request.ContainsKey("requestvars"))
                    repParams["RequestVars"] = request["requestvars"];
                else
                    repParams["RequestVars"] = new Hashtable();

                if (request.ContainsKey("querystringkeys"))
                    repParams["QueryStringKeys"] = request["querystringkeys"];
                else
                    repParams["QueryStringKeys"] = new string[0];


                //repParams["DatabaseConnection"] = dbConn;
                repParams["Scenes"] = m_scene;
                repParams["SimStats"] = m_simstatsCounters;
                repParams["LogLines"] = m_loglines;
                repParams["Reports"] = reports;
                
                concurrencyCounter++;

                strOut = rep.RenderView(rep.ProcessModel(repParams));

                if (regpath.EndsWith("js"))
                {
                    contenttype = "text/javascript";
                }

                concurrencyCounter--;
                
                response_code = 200;

            }
            else
            {
                strOut = MainServer.Instance.GetHTTP404("");
            }
            

            responsedata["int_response_code"] = response_code;
            responsedata["content_type"] = contenttype;
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = strOut;

            return responsedata;
        }

        public virtual void PostInitialise()
        {
        }

        public virtual void Close()
        {
        }

        public virtual string Name
        {
            get { return "ViewerStatsModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        public OSDMap OnRegisterCaps(UUID agentID, IHttpServer server)
        {
            m_log.DebugFormat("[VC]: OnRegisterCaps: agentID {0}", agentID);
            OSDMap retVal = new OSDMap();
            retVal["ViewerStats"] = CapsUtil.CreateCAPS("ViewerStats", "");

            server.AddStreamHandler(new RestStreamHandler("POST", retVal["ViewerStats"],
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ViewerStatsReport(request, path, param,
                                                                                  agentID);
                                                       }));
            return retVal;
        }

        public void OnDeRegisterCaps(UUID agentID, IRegionClientCapsService caps)
        {
            
        }

        protected virtual void AddHandlers()
        {
            lock (m_scene)
            {
                updateLogMod = m_scene.Count * 2;
                foreach (Scene scene in m_scene)
                {
                    scene.EventManager.OnRegisterCaps += OnRegisterCaps;
                    scene.EventManager.OnDeregisterCaps += OnDeRegisterCaps;
                    scene.EventManager.OnClientClosed += OnClientClosed;
                    scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
                    scene.EventManager.OnMakeChildAgent += OnMakeChildAgent;
                }
            }
        }

        public void OnMakeRootAgent(ScenePresence agent)
        {
            UUID regionUUID = GetRegionUUIDFromHandle(agent.Scene.RegionInfo.RegionHandle);

            lock (m_sessions)
            {
                if (!m_sessions.ContainsKey(agent.UUID))
                {
                    UserSessionData usd = UserSessionUtil.newUserSessionData();

                    UserSessionID uid = new UserSessionID();
                    uid.name_f = agent.Firstname;
                    uid.name_l = agent.Lastname;
                    uid.region_id = regionUUID;
                    uid.session_id = agent.ControllingClient.SessionId;
                    uid.session_data = usd;

                    m_sessions.Add(agent.UUID, uid);
                }
                else
                {
                    UserSessionID uid = m_sessions[agent.UUID];
                    uid.region_id = regionUUID;
                    uid.session_id = agent.ControllingClient.SessionId;
                    m_sessions[agent.UUID] = uid;
                }
            }
        }

        public void OnMakeChildAgent(ScenePresence agent)
        {
            
        }

        public void OnClientClosed(UUID agentID, Scene scene)
        {
            lock (m_sessions)
            {
                if (m_sessions.ContainsKey(agentID))
                {
                    m_sessions.Remove(agentID);
                }
            }

        }

        public string readLogLines(int amount)
        {
            Encoding encoding = Encoding.ASCII;
            int sizeOfChar = encoding.GetByteCount("\n");
            byte[] buffer = encoding.GetBytes("\n");
            string logfile = Util.logDir() + "/" + "OpenSim.log"; 
            FileStream fs = new FileStream(logfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Int64 tokenCount = 0;
            Int64 endPosition = fs.Length / sizeOfChar;

            for (Int64 position = sizeOfChar; position < endPosition; position += sizeOfChar)
            {
                fs.Seek(-position, SeekOrigin.End);
                fs.Read(buffer, 0, buffer.Length);

                if (encoding.GetString(buffer) == "\n")
                {
                    tokenCount++;
                    if (tokenCount == amount)
                    {
                        byte[] returnBuffer = new byte[fs.Length - fs.Position];
                        fs.Read(returnBuffer, 0, returnBuffer.Length);
                        fs.Close();
                        fs.Dispose();
                        return encoding.GetString(returnBuffer);
                    }
                }
            }

            // handle case where number of tokens in file is less than numberOfTokens
            fs.Seek(0, SeekOrigin.Begin);
            buffer = new byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            fs.Close();
            fs.Dispose();
            return encoding.GetString(buffer);

        }

        public UUID GetRegionUUIDFromHandle(ulong regionhandle)
        {
            lock (m_scene)
            {
                foreach (Scene scene in m_scene)
                {
                    if (scene.RegionInfo.RegionHandle == regionhandle)
                        return scene.RegionInfo.RegionID;
                }
            }
            return UUID.Zero;
        }
        /// <summary>
        /// Callback for a viewerstats cap
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="agentID"></param>
        /// <param name="caps"></param>
        /// <returns></returns>
        public string ViewerStatsReport(string request, string path, string param,
                                      UUID agentID)
        {
            //m_log.Debug(request);

            dataConnector.UpdateUserStats(ParseViewerStats(request, agentID));

            return String.Empty;
        }

        public UserSessionID ParseViewerStats(string request, UUID agentID)
        {
            UserSessionID uid = new UserSessionID();
            UserSessionData usd;
            OSD message = OSDParser.DeserializeLLSDXml(request);
            OSDMap mmap;
            lock (m_sessions)
            {
                if (agentID != UUID.Zero)
                {
                
                    if (!m_sessions.ContainsKey(agentID))
                    {
                        m_log.Warn("[VS]: no session for stat disclosure");
                        return new UserSessionID();
                    }
                    uid = m_sessions[agentID];
                }
                else
                {
                    // parse through the beginning to locate the session
                    if (message.Type != OSDType.Map)
                        return new UserSessionID();

                    mmap = (OSDMap)message;
                    {
                        UUID sessionID = mmap["session_id"].AsUUID();

                        if (sessionID == UUID.Zero)
                            return new UserSessionID();


                        // search through each session looking for the owner
                        foreach (UUID usersessionid in m_sessions.Keys)
                        {
                            // got it!
                            if (m_sessions[usersessionid].session_id == sessionID)
                            {
                                agentID = usersessionid;
                                uid = m_sessions[usersessionid];
                                break;
                            }

                        }

                        // can't find a session
                        if (agentID == UUID.Zero)
                        {
                            return new UserSessionID();
                        }
                    }
                }
            }
           
            usd = uid.session_data;

            

            if (message.Type != OSDType.Map)
                return new UserSessionID();

            mmap = (OSDMap)message;
            {
                if (mmap["agent"].Type != OSDType.Map)
                    return new UserSessionID();
                OSDMap agent_map = (OSDMap)mmap["agent"];
                usd.agent_id = agentID;
                usd.name_f = uid.name_f;
                usd.name_l = uid.name_l;
                usd.region_id = uid.region_id;
                usd.a_language = agent_map["language"].AsString();
                usd.mem_use = (float)agent_map["mem_use"].AsReal();
                usd.meters_traveled = (float)agent_map["meters_traveled"].AsReal();
                usd.regions_visited = agent_map["regions_visited"].AsInteger();
                usd.run_time = (float)agent_map["run_time"].AsReal();
                usd.start_time = (float)agent_map["start_time"].AsReal();
                usd.client_version = agent_map["version"].AsString();

                UserSessionUtil.UpdateMultiItems(ref usd, agent_map["agents_in_view"].AsInteger(),
                                                 (float)agent_map["ping"].AsReal(),
                                                 (float)agent_map["sim_fps"].AsReal(),
                                                 (float)agent_map["fps"].AsReal());

                if (mmap["downloads"].Type != OSDType.Map)
                    return new UserSessionID();
                OSDMap downloads_map = (OSDMap)mmap["downloads"];
                usd.d_object_kb = (float)downloads_map["object_kbytes"].AsReal();
                usd.d_texture_kb = (float)downloads_map["texture_kbytes"].AsReal();
                usd.d_world_kb = (float)downloads_map["workd_kbytes"].AsReal();


                usd.session_id = mmap["session_id"].AsUUID();

                if (mmap["system"].Type != OSDType.Map)
                    return new UserSessionID();
                OSDMap system_map = (OSDMap)mmap["system"];

                usd.s_cpu = system_map["cpu"].AsString();
                usd.s_gpu = system_map["gpu"].AsString();
                usd.s_os = system_map["os"].AsString();
                usd.s_ram = system_map["ram"].AsInteger();

                if (mmap["stats"].Type != OSDType.Map)
                    return new UserSessionID();

                OSDMap stats_map = (OSDMap)mmap["stats"];
                {

                    if (stats_map["failures"].Type != OSDType.Map)
                        return new UserSessionID();
                    OSDMap stats_failures = (OSDMap)stats_map["failures"];
                    usd.f_dropped = stats_failures["dropped"].AsInteger();
                    usd.f_failed_resends = stats_failures["failed_resends"].AsInteger();
                    usd.f_invalid = stats_failures["invalid"].AsInteger();
                    usd.f_resent = stats_failures["resent"].AsInteger();
                    usd.f_send_packet = stats_failures["send_packet"].AsInteger();

                    if (stats_map["net"].Type != OSDType.Map)
                        return new UserSessionID();
                    OSDMap stats_net = (OSDMap)stats_map["net"];
                    {
                        if (stats_net["in"].Type != OSDType.Map)
                            return new UserSessionID();

                        OSDMap net_in = (OSDMap)stats_net["in"];
                        usd.n_in_kb = (float)net_in["kbytes"].AsReal();
                        usd.n_in_pk = net_in["packets"].AsInteger();

                        if (stats_net["out"].Type != OSDType.Map)
                            return new UserSessionID();
                        OSDMap net_out = (OSDMap)stats_net["out"];

                        usd.n_out_kb = (float)net_out["kbytes"].AsReal();
                        usd.n_out_pk = net_out["packets"].AsInteger();
                    }


                }
            }

            uid.session_data = usd;
            m_sessions[agentID] = uid;
            return uid;
        }
    }
    public static class UserSessionUtil
    {
        public static UserSessionData newUserSessionData()
        {
            UserSessionData obj = ZeroSession(new UserSessionData());
            return obj;
        }

        public static void UpdateMultiItems(ref UserSessionData s, int agents_in_view, float ping, float sim_fps, float fps)
        {
            // don't insert zero values here or it'll skew the statistics.
            if (agents_in_view == 0 && fps == 0 && sim_fps == 0 && ping == 0)
                return;
            s._agents_in_view.Add(agents_in_view);
            s._fps.Add(fps);
            s._sim_fps.Add(sim_fps);
            s._ping.Add(ping);

            int[] __agents_in_view = s._agents_in_view.ToArray();

            s.avg_agents_in_view = ArrayAvg_i(__agents_in_view);
            s.min_agents_in_view = ArrayMin_i(__agents_in_view);
            s.max_agents_in_view = ArrayMax_i(__agents_in_view);
            s.mode_agents_in_view = ArrayMode_i(__agents_in_view);

            float[] __fps = s._fps.ToArray();
            s.avg_fps = ArrayAvg_f(__fps);
            s.min_fps = ArrayMin_f(__fps);
            s.max_fps = ArrayMax_f(__fps);
            s.mode_fps = ArrayMode_f(__fps);

            float[] __sim_fps = s._sim_fps.ToArray();
            s.avg_sim_fps = ArrayAvg_f(__sim_fps);
            s.min_sim_fps = ArrayMin_f(__sim_fps);
            s.max_sim_fps = ArrayMax_f(__sim_fps);
            s.mode_sim_fps = ArrayMode_f(__sim_fps);

            float[] __ping = s._ping.ToArray();
            s.avg_ping = ArrayAvg_f(__ping);
            s.min_ping = ArrayMin_f(__ping);
            s.max_ping = ArrayMax_f(__ping);
            s.mode_ping = ArrayMode_f(__ping);

        }

        #region Statistics

        public static int ArrayMin_i(int[] arr)
        {
            int cnt = arr.Length;
            if (cnt == 0)
                return 0;

            Array.Sort(arr);
            return arr[0];
        }

        public static int ArrayMax_i(int[] arr)
        {
            int cnt = arr.Length;
            if (cnt == 0)
                return 0;

            Array.Sort(arr);
            return arr[cnt-1];
        }

        public static float ArrayMin_f(float[] arr)
        {
            int cnt = arr.Length;
            if (cnt == 0)
                return 0;

            Array.Sort(arr);
            return arr[0];
        }

        public static float ArrayMax_f(float[] arr)
        {
            int cnt = arr.Length;
            if (cnt == 0)
                return 0;

            Array.Sort(arr);
            return arr[cnt - 1];
        }

        public static float ArrayAvg_i(int[] arr)
        {
            int cnt = arr.Length;

            if (cnt == 0)
                return 0;

            float result = arr[0];

            for (int i = 1; i < cnt; i++)
                result += arr[i];

            return result / cnt;
        }

        public static float ArrayAvg_f(float[] arr)
        {
            int cnt = arr.Length;

            if (cnt == 0)
                return 0;

            float result = arr[0];

            for (int i = 1; i < cnt; i++)
                result += arr[i];

            return result / cnt;
        }


        public static float ArrayMode_f(float[] arr)
        {
            List<float> mode = new List<float>();

            float[] srtArr = new float[arr.Length];
            float[,] freq = new float[arr.Length, 2];
            Array.Copy(arr, srtArr, arr.Length);
            Array.Sort(srtArr);

            float tmp = srtArr[0];
            int index = 0;
            int i = 0;
            while (i < srtArr.Length)
            {
                freq[index, 0] = tmp;

                while (tmp == srtArr[i])
                {
                    freq[index, 1]++;
                    i++;

                    if (i > srtArr.Length - 1)
                        break;
                }

                if (i < srtArr.Length)
                {
                    tmp = srtArr[i];
                    index++;
                }

            }

            Array.Clear(srtArr, 0, srtArr.Length);

            for (i = 0; i < srtArr.Length; i++)
                srtArr[i] = freq[i, 1];

            Array.Sort(srtArr);

            if ((srtArr[srtArr.Length - 1]) == 0 || (srtArr[srtArr.Length - 1]) == 1)
                return 0;

            float freqtest = (float)freq.Length / freq.Rank;

            for (i = 0; i < freqtest; i++)
            {
                if (freq[i, 1] == srtArr[index])
                    mode.Add(freq[i, 0]);

            }

            return mode.ToArray()[0];

        }


        public static int ArrayMode_i(int[] arr)
        {
            List<int> mode = new List<int>();

            int[] srtArr = new int[arr.Length];
            int[,] freq = new int[arr.Length, 2];
            Array.Copy(arr, srtArr, arr.Length);
            Array.Sort(srtArr);

            int tmp = srtArr[0];
            int index = 0;
            int i = 0;
            while (i < srtArr.Length)
            {
                freq[index, 0] = tmp;

                while (tmp == srtArr[i])
                {
                    freq[index, 1]++;
                    i++;

                    if (i > srtArr.Length - 1)
                        break;
                }

                if (i < srtArr.Length)
                {
                    tmp = srtArr[i];
                    index++;
                }

            }

            Array.Clear(srtArr, 0, srtArr.Length);

            for (i = 0; i < srtArr.Length; i++)
                srtArr[i] = freq[i, 1];

            Array.Sort(srtArr);

            if ((srtArr[srtArr.Length - 1]) == 0 || (srtArr[srtArr.Length - 1]) == 1)
                return 0;
           
            float freqtest = (float)freq.Length / freq.Rank;

            for (i = 0; i < freqtest; i++)
            {
                if (freq[i, 1] == srtArr[index])
                    mode.Add(freq[i, 0]);

            }

            return mode.ToArray()[0];

        }

        #endregion

        private static UserSessionData ZeroSession(UserSessionData s)
        {
            s.session_id = UUID.Zero;
            s.agent_id = UUID.Zero;
            s.region_id = UUID.Zero;
            s.last_updated = Util.UnixTimeSinceEpoch();
            s.remote_ip = "";
            s.name_f = "";
            s.name_l = "";
            s.avg_agents_in_view = 0;
            s.min_agents_in_view = 0;
            s.max_agents_in_view = 0;
            s.mode_agents_in_view = 0;
            s.avg_fps = 0;
            s.min_fps = 0;
            s.max_fps = 0;
            s.mode_fps = 0;
            s.a_language = "";
            s.mem_use = 0;
            s.meters_traveled = 0;
            s.avg_ping = 0;
            s.min_ping = 0;
            s.max_ping = 0;
            s.mode_ping = 0;
            s.regions_visited = 0;
            s.run_time = 0;
            s.avg_sim_fps = 0;
            s.min_sim_fps = 0;
            s.max_sim_fps = 0;
            s.mode_sim_fps = 0;
            s.start_time = 0;
            s.client_version = "";
            s.s_cpu = "";
            s.s_gpu = "";
            s.s_os = "";
            s.s_ram = 0;
            s.d_object_kb = 0;
            s.d_texture_kb = 0;
            s.d_world_kb = 0;
            s.n_in_kb = 0;
            s.n_in_pk = 0;
            s.n_out_kb = 0;
            s.n_out_pk = 0;
            s.f_dropped = 0;
            s.f_failed_resends = 0;
            s.f_invalid = 0;
            s.f_off_circuit = 0;
            s.f_resent = 0;
            s.f_send_packet = 0;
            s._ping = new List<float>();
            s._fps = new List<float>();
            s._sim_fps = new List<float>();
            s._agents_in_view = new List<int>();
            return s;
        }
    }
}
