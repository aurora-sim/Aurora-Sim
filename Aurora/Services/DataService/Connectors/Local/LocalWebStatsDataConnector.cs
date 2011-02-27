using System;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using Nini.Config;

namespace Aurora.Services.DataService
{
    public class LocalWebStatsDataConnector : IWebStatsDataConnector
	{
        private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("WebStatsDataConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString, "WebStats", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IWebStatsDataConnector"; }
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Add/Update a user's stats in the database
        /// </summary>
        /// <param name="uid"></param>
        public void UpdateUserStats(UserSessionID uid)
        {
            if (uid.session_id == UUID.Zero)
                return;

            List<string> Keys = new List<string>();
            List<object> Values = new List<object>();

            Keys.Add("session_id");
            Keys.Add("agent_id");
            Keys.Add("region_id");
            Keys.Add("last_updated");
            Keys.Add("remote_ip");
            Keys.Add("name_f");
            Keys.Add("name_l");
            Keys.Add("avg_agents_in_view");
            Keys.Add("min_agents_in_view");
            Keys.Add("max_agents_in_view");
            Keys.Add("mode_agents_in_view");
            Keys.Add("avg_fps");
            Keys.Add("min_fps");
            Keys.Add("max_fps");
            Keys.Add("mode_fps");
            Keys.Add("a_language");
            Keys.Add("mem_use");
            Keys.Add("meters_traveled");
            Keys.Add("avg_ping");
            Keys.Add("min_ping");
            Keys.Add("max_ping");
            Keys.Add("mode_ping");
            Keys.Add("regions_visited");
            Keys.Add("run_time");
            Keys.Add("avg_sim_fps");
            Keys.Add("min_sim_fps");
            Keys.Add("max_sim_fps");
            Keys.Add("mode_sim_fps");
            Keys.Add("start_time");
            Keys.Add("client_version");
            Keys.Add("s_cpu");
            Keys.Add("s_gpu");
            Keys.Add("s_os");
            Keys.Add("s_ram");
            Keys.Add("d_object_kb");
            Keys.Add("d_texture_kb");
            Keys.Add("n_in_kb");
            Keys.Add("n_in_pk");
            Keys.Add("n_out_kb");
            Keys.Add("n_out_pk");
            Keys.Add("f_dropped");
            Keys.Add("f_failed_resends");
            Keys.Add("f_invalid");
            Keys.Add("f_off_circuit");
            Keys.Add("f_resent");
            Keys.Add("f_send_packet");

            Values.Add(uid.session_data.session_id);
            Values.Add(uid.session_data.agent_id);
            Values.Add(uid.session_data.region_id);
            Values.Add(uid.session_data.last_updated);
            Values.Add(uid.session_data.remote_ip);
            Values.Add(uid.session_data.name_f);
            Values.Add(uid.session_data.name_l);
            Values.Add(uid.session_data.avg_agents_in_view);
            Values.Add(uid.session_data.min_agents_in_view);
            Values.Add(uid.session_data.max_agents_in_view);
            Values.Add(uid.session_data.mode_agents_in_view);
            Values.Add(uid.session_data.avg_fps);
            Values.Add(uid.session_data.min_fps);
            Values.Add(uid.session_data.max_fps);
            Values.Add(uid.session_data.mode_fps);
            Values.Add(uid.session_data.a_language);
            Values.Add(uid.session_data.mem_use);
            Values.Add(uid.session_data.meters_traveled);
            Values.Add(uid.session_data.avg_ping);
            Values.Add(uid.session_data.min_ping);
            Values.Add(uid.session_data.max_ping);
            Values.Add(uid.session_data.mode_ping);
            Values.Add(uid.session_data.regions_visited);
            Values.Add(uid.session_data.run_time);
            Values.Add(uid.session_data.avg_sim_fps);
            Values.Add(uid.session_data.min_sim_fps);
            Values.Add(uid.session_data.max_sim_fps);
            Values.Add(uid.session_data.mode_sim_fps);
            Values.Add(uid.session_data.start_time);
            Values.Add(uid.session_data.client_version);
            Values.Add(uid.session_data.s_cpu);
            Values.Add(uid.session_data.s_gpu);
            Values.Add(uid.session_data.s_os);
            Values.Add(uid.session_data.s_ram);
            Values.Add(uid.session_data.d_object_kb);
            Values.Add(uid.session_data.d_texture_kb);
            Values.Add(uid.session_data.n_in_kb);
            Values.Add(uid.session_data.n_in_pk);
            Values.Add(uid.session_data.n_out_kb);
            Values.Add(uid.session_data.n_out_pk);
            Values.Add(uid.session_data.f_dropped);
            Values.Add(uid.session_data.f_failed_resends);
            Values.Add(uid.session_data.f_invalid);
            Values.Add(uid.session_data.f_off_circuit);
            Values.Add(uid.session_data.f_resent);
            Values.Add(uid.session_data.f_send_packet);
            GD.Replace("stats_session_data", Keys.ToArray(), Values.ToArray());
        }

        /// <summary>
        /// Get info on the sim status
        /// </summary>
        /// <returns></returns>
        public stats_default_page_values GetDefaultPageStats()
        {
            stats_default_page_values stats = new stats_default_page_values();
            List<string> retStr = GD.Query("", "", "stats_session_data",
                "COUNT(DISTINCT agent_id) as agents, COUNT(*) as sessions, AVG(avg_fps) as client_fps, " +
                                "AVG(avg_sim_fps) as savg_sim_fps, AVG(avg_ping) as sav_ping, SUM(n_out_kb) as num_in_kb, " +
                                "SUM(n_out_pk) as num_in_packets, SUM(n_in_kb) as num_out_kb, SUM(n_in_pk) as num_out_packets, AVG(mem_use) as sav_mem_use");

            if (retStr.Count == 0)
                return stats;

            for (int i = 0; i < retStr.Count; i += 8)
            {

                stats.total_num_users = Convert.ToInt32(retStr[i]);
                stats.total_num_sessions = Convert.ToInt32(retStr[i + 1]);
                stats.avg_client_fps = Convert.ToSingle(retStr[i + 2]);
                stats.avg_sim_fps = Convert.ToSingle(retStr[i + 3]);
                stats.avg_ping = Convert.ToSingle(retStr[i + 4]);
                stats.total_kb_out = Convert.ToSingle(retStr[i + 5]);
                stats.total_kb_in = Convert.ToSingle(retStr[i + 6]);
                stats.avg_client_mem_use = Convert.ToSingle(retStr[i + 7]);

            }
            return stats;
        }

        /// <summary>
        /// Get info on all clients that are in the region
        /// </summary>
        /// <returns></returns>
        public List<ClientVersionData> GetClientVersions()
        {
            List<ClientVersionData> clients = new List<ClientVersionData>();

            List<string> retStr = GD.Query("", "", "stats_session_data",
                "count(distinct region_id) as regcnt");

            if (retStr.Count == 0)
                return clients;

            int totalregions = totalregions = Convert.ToInt32(retStr[0]);
            int totalclients = 0;
            if (totalregions > 1)
            {
                retStr = GD.QueryFullData(" group by region_id, client_version order by region_id, count(*) desc;",
                "stats_session_data",
                "region_id, client_version, count(*) as cnt, avg(avg_sim_fps) as simfps");

                for (int i = 0; i < retStr.Count; i += 4)
                {
                    ClientVersionData udata = new ClientVersionData();
                    udata.region_id = UUID.Parse(retStr[i]);
                    udata.version = retStr[i + 1];
                    udata.count = int.Parse(retStr[i + 2]);
                    udata.fps = Convert.ToSingle(retStr[i + 3]);
                    clients.Add(udata);
                }
            }
            else
            {
                retStr = GD.QueryFullData(" group by region_id, client_version order by region_id, count(*) desc;",
                    "stats_session_data",
                    "region_id, client_version, count(*) as cnt, avg(avg_sim_fps) as simfps");

                for (int i = 0; i < retStr.Count; i += 4)
                {
                    ClientVersionData udata = new ClientVersionData();
                    udata.region_id = UUID.Parse(retStr[i]);
                    udata.version = retStr[i + 1];
                    udata.count = int.Parse(retStr[i + 2]);
                    udata.fps = Convert.ToSingle(retStr[i + 3]);
                    clients.Add(udata);
                    totalclients += udata.count;
                }
            }

            return clients;
        }

        /// <summary>
        /// Get a list of all the client sessions in the region
        /// </summary>
        /// <param name="puserUUID"></param>
        /// <param name="clientVersionString"></param>
        /// <returns></returns>
        public List<SessionList> GetSessionList(string puserUUID, string clientVersionString)
        {
            List<SessionList> sessionList = new List<SessionList>();
            string sql = " a LEFT OUTER JOIN stats_session_data b ON a.Agent_ID = b.Agent_ID";
            int queryparams = 0;

            if (puserUUID.Length > 0)
            {
                if (queryparams == 0)
                    sql += " WHERE";
                else
                    sql += " AND";

                sql += " b.agent_id='" + puserUUID + "'";
                queryparams++;
            }

            if (clientVersionString.Length > 0)
            {
                if (queryparams == 0)
                    sql += " WHERE";
                else
                    sql += " AND";

                sql += " b.client_version='" + clientVersionString + "'";
                queryparams++;
            }

            sql += " ORDER BY a.name_f, a.name_l, b.last_updated;";

            IDataReader sdr = GD.QueryDataFull(sql,
                "stats_session_data",
                "distinct a.name_f, a.name_l, a.Agent_ID, b.Session_ID, b.client_version, b.last_updated, b.start_time");

            if (sdr != null && sdr.FieldCount != 0)
            {
                UUID userUUID = UUID.Zero;

                SessionList activeSessionList = new SessionList();
                activeSessionList.user_id = UUID.Random();
                while (sdr.Read())
                {
                    UUID readUUID = UUID.Parse(sdr["agent_id"].ToString());
                    if (readUUID != userUUID)
                    {
                        activeSessionList = new SessionList();
                        activeSessionList.user_id = readUUID;
                        activeSessionList.firstname = sdr["name_f"].ToString();
                        activeSessionList.lastname = sdr["name_l"].ToString();
                        activeSessionList.sessions = new List<ShortSessionData>();
                        sessionList.Add(activeSessionList);
                    }

                    ShortSessionData ssd = new ShortSessionData();

                    ssd.last_update = Utils.UnixTimeToDateTime((uint)Convert.ToInt32(sdr["last_updated"]));
                    ssd.start_time = Utils.UnixTimeToDateTime((uint)Convert.ToInt32(sdr["start_time"]));
                    ssd.session_id = UUID.Parse(sdr["session_id"].ToString());
                    ssd.client_version = sdr["client_version"].ToString();
                    activeSessionList.sessions.Add(ssd);

                    userUUID = activeSessionList.user_id;
                }
            }

            return sessionList;
        }
	}
}
