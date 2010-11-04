using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public struct UserSessionID
    {
        public UUID session_id;
        public UUID region_id;
        public string name_f;
        public string name_l;
        public UserSessionData session_data;
    }

    public struct UserSessionData
    {
        public UUID session_id;
        public UUID agent_id;
        public UUID region_id;
        public float last_updated;
        public string remote_ip;
        public string name_f;
        public string name_l;
        public float avg_agents_in_view;
        public float min_agents_in_view;
        public float max_agents_in_view;
        public float mode_agents_in_view;
        public float avg_fps;
        public float min_fps;
        public float max_fps;
        public float mode_fps;
        public string a_language;
        public float mem_use;
        public float meters_traveled;
        public float avg_ping;
        public float min_ping;
        public float max_ping;
        public float mode_ping;
        public int regions_visited;
        public float run_time;
        public float avg_sim_fps;
        public float min_sim_fps;
        public float max_sim_fps;
        public float mode_sim_fps;
        public float start_time;
        public string client_version;
        public string s_cpu;
        public string s_gpu;
        public string s_os;
        public int s_ram;
        public float d_object_kb;
        public float d_texture_kb;
        public float d_world_kb;
        public float n_in_kb;
        public int n_in_pk;
        public float n_out_kb;
        public int n_out_pk;
        public int f_dropped;
        public int f_failed_resends;
        public int f_invalid;
        public int f_off_circuit;
        public int f_resent;
        public int f_send_packet;
        public List<float> _ping;
        public List<float> _fps;
        public List<float> _sim_fps;
        public List<int> _agents_in_view;
    }

    public interface IWebStatsDataConnector
    {
        void UpdateUserStats(UserSessionID uid);
    }
}
