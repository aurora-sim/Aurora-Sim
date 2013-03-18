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
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse.Messages.Linden;

namespace Aurora.Services.DataService
{
    public class LocalUserStatsDataConnector : IUserStatsDataConnector
	{
        private IGenericData GD = null;
        private const string m_realm = "statsdata";

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("WebStatsDataConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                if (GD != null)
                    GD.ConnectToDatabase(defaultConnectionString, "Stats", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IUserStatsDataConnector"; }
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Add/Update a user's stats in the database
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="agentID"></param>
        /// <param name="regionID"></param>
        public void UpdateUserStats(ViewerStatsMessage uid, UUID agentID, UUID regionID)
        {
            Dictionary<string, object> rows = new Dictionary<string, object>();

            rows.Add("session_id", uid.SessionID);
            rows.Add("agent_id", agentID);
            rows.Add("region_id", regionID);
            rows.Add("agents_in_view", uid.AgentsInView);
            rows.Add("fps", uid.AgentFPS);
            rows.Add("a_language", uid.AgentLanguage);
            rows.Add("mem_use", uid.AgentMemoryUsed);
            rows.Add("meters_traveled", uid.MetersTraveled);
            rows.Add("ping", uid.AgentPing);
            rows.Add("regions_visited", uid.RegionsVisited);
            rows.Add("run_time", uid.AgentRuntime);
            rows.Add("sim_fps", uid.SimulatorFPS);
            rows.Add("start_time", Util.ToUnixTime(uid.AgentStartTime));
            rows.Add("client_version", uid.AgentVersion);
            rows.Add("s_cpu", uid.SystemCPU);
            rows.Add("s_gpu", uid.SystemGPU);
            rows.Add("s_gpuclass", uid.SystemGPUClass);
            rows.Add("s_gpuvendor", uid.SystemGPUVendor);
            rows.Add("s_gpuversion", uid.SystemGPUVersion);
            rows.Add("s_os", uid.SystemOS);
            rows.Add("s_ram", uid.SystemInstalledRam);
            rows.Add("d_object_kb", uid.object_kbytes);
            rows.Add("d_texture_kb", uid.texture_kbytes);
            rows.Add("d_world_kb", uid.world_kbytes);
            rows.Add("n_in_kb", uid.InKbytes);
            rows.Add("n_in_pk", uid.InPackets);
            rows.Add("n_out_kb", uid.OutKbytes);
            rows.Add("n_out_pk", uid.OutPackets);
            rows.Add("f_dropped", uid.StatsDropped);
            rows.Add("f_failed_resends", uid.StatsFailedResends);
            rows.Add("f_invalid", uid.FailuresInvalid);
            rows.Add("f_off_circuit", uid.FailuresOffCircuit);
            rows.Add("f_resent", uid.FailuresResent);
            rows.Add("f_send_packet", uid.FailuresSendPacket);

            GD.Replace(m_realm, rows);
        }

        public List<string> Get(string columnName)
        {
            QueryFilter filter = new QueryFilter();
            return GD.Query(new string[1] { columnName }, m_realm, filter, null, null, null);
        }

        public int GetCount(string columnName, KeyValuePair<string, object> whereCheck)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters.Add(whereCheck.Key, whereCheck.Value);
            return int.Parse(GD.Query(new string[1] { "count(" + columnName + ")" }, m_realm, filter, null, null, null)[0]);
        }

        public ViewerStatsMessage GetBySession(UUID sessionID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters.Add("session_id", sessionID);
            List<string> results = GD.Query(new string[1] { "*" }, m_realm, filter, null, null, null);
            return BuildSession(results, 0);
        }

        public void RemoveAllSessions()
        {
            GD.Delete(m_realm, null);
        }

        private ViewerStatsMessage BuildSession(List<string> results, int start)
        {
            ViewerStatsMessage message = new ViewerStatsMessage();
            for (int i = start; i < start + 33; i += 33)
            {
                message.SessionID = UUID.Parse(results[i + 0]);
                message.AgentsInView = int.Parse(results[i + 3]);
                message.AgentFPS = results[i + 4] == "" ? 0 : float.Parse(results[i + 4]);
                message.AgentLanguage = results[i + 5];
                message.AgentMemoryUsed = float.Parse(results[i + 6]);
                message.MetersTraveled = float.Parse(results[i + 7]);
                message.AgentPing = float.Parse(results[i + 8]);
                message.RegionsVisited = int.Parse(results[i + 9]);
                message.AgentRuntime = float.Parse(results[i + 10]);
                message.SimulatorFPS = float.Parse(results[i + 11]);
                message.AgentStartTime = Util.ToDateTime(int.Parse(results[i + 12]));
                message.AgentVersion = results[i + 13];
                message.SystemCPU = results[i + 14];
                message.SystemGPU = results[i + 15];
                message.SystemGPUClass = int.Parse(results[i + 16]);
                message.SystemGPUVendor = results[i + 17];
                message.SystemGPUVersion = results[i + 18];
                message.SystemOS = results[i + 19];
                message.SystemInstalledRam = int.Parse(results[i + 20]);
                message.object_kbytes = float.Parse(results[i + 21]);
                message.texture_kbytes = float.Parse(results[i + 22]);
                message.world_kbytes = float.Parse(results[i + 23]);
                message.InKbytes = float.Parse(results[i + 24]);
                message.InPackets = float.Parse(results[i + 25]);
                message.OutKbytes = float.Parse(results[i + 26]);
                message.OutPackets = float.Parse(results[i + 27]);
                message.StatsDropped = int.Parse(results[i + 28]);
                message.StatsFailedResends = int.Parse(results[i + 29]);
                message.FailuresInvalid = int.Parse(results[i + 30]);
                message.FailuresOffCircuit = int.Parse(results[i + 31]);
                message.FailuresResent = int.Parse(results[i + 32]);
                message.FailuresSendPacket = int.Parse(results[i + 33]);
            }
            return message;
        }
	}
}
