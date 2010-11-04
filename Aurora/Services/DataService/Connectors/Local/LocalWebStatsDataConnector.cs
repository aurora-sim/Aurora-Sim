using System;
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
    public class LocalWebStatsDataConnector : IWebStatsDataConnector, IAuroraDataPlugin
	{
        private IGenericData GD = null;

        public void Initialize(IGenericData GenericData, IConfigSource source, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("WebStatsDataConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString);

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
            Keys.Add("session_key");
            Keys.Add("agent_key");
            Keys.Add("region_key");

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
            Values.Add(uid.session_data.session_id);
            Values.Add(uid.session_data.agent_id);
            Values.Add(uid.session_data.region_id);
            GD.Replace("stats_session_data", Keys.ToArray(), Values.ToArray());
        }
	}
}
