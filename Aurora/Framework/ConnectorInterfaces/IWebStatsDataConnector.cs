using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

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

    public struct stats_default_page_values
    {
        public int total_num_users;
        public int total_num_sessions;
        public float avg_client_fps;
        public float avg_client_mem_use;
        public float avg_sim_fps;
        public float avg_ping;
        public float total_kb_out;
        public float total_kb_in;
        public float avg_client_resends;
        public IScene[] all_scenes;
        public Dictionary<UUID, USimStatsData> sim_stat_data;
        public Dictionary<string, IStatsController> stats_reports;
    }

    public interface IStatsController
    {
        string ReportName { get; }
        Hashtable ProcessModel(Hashtable pParams);
        string RenderView(Hashtable pModelResult);
    }

    public class USimStatsData
    {
        private UUID m_regionID = UUID.Zero;
        private volatile int m_statcounter = 0;
        private volatile float m_timeDilation;
        private volatile float m_simFps;
        private volatile float m_physicsFps;
        private volatile float m_agentUpdates;
        private volatile float m_rootAgents;
        private volatile float m_childAgents;
        private volatile float m_totalPrims;
        private volatile float m_activePrims;
        private volatile float m_totalFrameTime;
        private volatile float m_netFrameTime;
        private volatile float m_physicsFrameTime;
        private volatile float m_otherFrameTime;
        private volatile float m_imageFrameTime;
        private volatile float m_inPacketsPerSecond;
        private volatile float m_outPacketsPerSecond;
        private volatile float m_unackedBytes;
        private volatile float m_agentFrameTime;
        private volatile float m_pendingDownloads;
        private volatile float m_pendingUploads;
        private volatile float m_activeScripts;
        private volatile float m_scriptLinesPerSecond;

        public UUID RegionId { get { return m_regionID; } }
        public int StatsCounter { get { return m_statcounter; } set { m_statcounter = value; } }
        public float TimeDilation { get { return m_timeDilation; } }
        public float SimFps { get { return m_simFps; } }
        public float PhysicsFps { get { return m_physicsFps; } }
        public float AgentUpdates { get { return m_agentUpdates; } }
        public float RootAgents { get { return m_rootAgents; } }
        public float ChildAgents { get { return m_childAgents; } }
        public float TotalPrims { get { return m_totalPrims; } }
        public float ActivePrims { get { return m_activePrims; } }
        public float TotalFrameTime { get { return m_totalFrameTime; } }
        public float NetFrameTime { get { return m_netFrameTime; } }
        public float PhysicsFrameTime { get { return m_physicsFrameTime; } }
        public float OtherFrameTime { get { return m_otherFrameTime; } }
        public float ImageFrameTime { get { return m_imageFrameTime; } }
        public float InPacketsPerSecond { get { return m_inPacketsPerSecond; } }
        public float OutPacketsPerSecond { get { return m_outPacketsPerSecond; } }
        public float UnackedBytes { get { return m_unackedBytes; } }
        public float AgentFrameTime { get { return m_agentFrameTime; } }
        public float PendingDownloads { get { return m_pendingDownloads; } }
        public float PendingUploads { get { return m_pendingUploads; } }
        public float ActiveScripts { get { return m_activeScripts; } }
        public float ScriptLinesPerSecond { get { return m_scriptLinesPerSecond; } }

        public USimStatsData(UUID pRegionID)
        {
            m_regionID = pRegionID;
        }

        public void ConsumeSimStats(SimStats stats)
        {
            m_regionID = stats.RegionUUID;
            m_timeDilation = stats.StatsBlock[0].StatValue;
            m_simFps = stats.StatsBlock[1].StatValue;
            m_physicsFps = stats.StatsBlock[2].StatValue;
            m_agentUpdates = stats.StatsBlock[3].StatValue;
            m_rootAgents = stats.StatsBlock[4].StatValue;
            m_childAgents = stats.StatsBlock[5].StatValue;
            m_totalPrims = stats.StatsBlock[6].StatValue;
            m_activePrims = stats.StatsBlock[7].StatValue;
            m_totalFrameTime = stats.StatsBlock[8].StatValue;
            m_netFrameTime = stats.StatsBlock[9].StatValue;
            m_physicsFrameTime = stats.StatsBlock[10].StatValue;
            m_otherFrameTime = stats.StatsBlock[11].StatValue;
            m_imageFrameTime = stats.StatsBlock[12].StatValue;
            m_inPacketsPerSecond = stats.StatsBlock[13].StatValue;
            m_outPacketsPerSecond = stats.StatsBlock[14].StatValue;
            m_unackedBytes = stats.StatsBlock[15].StatValue;
            m_agentFrameTime = stats.StatsBlock[16].StatValue;
            m_pendingDownloads = stats.StatsBlock[17].StatValue;
            m_pendingUploads = stats.StatsBlock[18].StatValue;
            m_activeScripts = stats.StatsBlock[19].StatValue;
            m_scriptLinesPerSecond = stats.StatsBlock[20].StatValue;
        }
    }

    public struct ClientVersionData
    {
        public UUID region_id;
        public string version;
        public int count;
        public float fps;
    }

    public struct ShortSessionData
    {
        public UUID session_id;
        public string client_version;
        public DateTime last_update;
        public DateTime start_time;
    }

    public class SessionList
    {
        public string firstname;
        public string lastname;
        public UUID user_id;
        public List<ShortSessionData> sessions;
    }

    public interface IWebStatsDataConnector
    {
        void UpdateUserStats(UserSessionID uid);
        stats_default_page_values GetDefaultPageStats();
        List<ClientVersionData> GetClientVersions();
        List<SessionList> GetSessionList(string puserUUID, string clientVersionString);
    }
}
