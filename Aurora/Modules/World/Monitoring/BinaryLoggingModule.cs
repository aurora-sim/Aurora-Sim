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
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Nini.Config;
using OpenMetaverse.Packets;
using System;
using System.IO;

namespace Aurora.Modules.Monitoring
{
    public class BinaryLoggingModule : INonSharedRegionModule
    {
        private static StatLogger m_statLog;
        private static TimeSpan m_statLogPeriod = TimeSpan.FromSeconds(300);
        private static string m_statsDir = String.Empty;
        private static readonly Object m_statLockObject = new Object();

        protected bool m_collectStats;
        protected IScene m_scene;

        #region INonSharedRegionModule Members

        public string Name
        {
            get { return "Binary Statistics Logging Module"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
            try
            {
                IConfig statConfig = source.Configs["Statistics.Binary"];
                if (statConfig != null)
                {
                    if (statConfig.Contains("enabled") && statConfig.GetBoolean("enabled"))
                    {
                        if (statConfig.Contains("collect_region_stats"))
                        {
                            if (statConfig.GetBoolean("collect_region_stats"))
                            {
                                m_collectStats = true;
                            }
                        }
                        if (statConfig.Contains("region_stats_period_seconds"))
                        {
                            m_statLogPeriod = TimeSpan.FromSeconds(statConfig.GetInt("region_stats_period_seconds"));
                        }
                        if (statConfig.Contains("stats_dir"))
                        {
                            m_statsDir = statConfig.GetString("stats_dir");
                        }
                    }
                }
            }
            catch
            {
                // if it doesn't work, we don't collect anything
            }
        }

        public void AddRegion(IScene scene)
        {
            m_scene = scene;
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            if (m_collectStats)
            {
                IMonitorModule reporter = m_scene.RequestModuleInterface<IMonitorModule>();
                if (reporter != null)
                    reporter.OnSendStatsResult += LogSimStats;
            }
        }

        public void Close()
        {
        }

        #endregion

        private void LogSimStats(SimStats stats)
        {
            SimStatsPacket pack = new SimStatsPacket
                                      {Region = stats.RegionBlock, Stat = stats.StatsBlock, Header = {Reliable = false}};

            // note that we are inside the reporter lock when called
            DateTime now = DateTime.Now;

            // hide some time information into the packet
            pack.Header.Sequence = (uint) now.Ticks;

            lock (m_statLockObject) // m_statLog is shared so make sure there is only executer here
            {
                try
                {
                    if (m_statLog == null || now > m_statLog.StartTime + m_statLogPeriod)
                    {
                        // First log file or time has expired, start writing to a new log file
                        if (m_statLog != null && m_statLog.Log != null)
                        {
                            m_statLog.Log.Close();
                        }
                        m_statLog = new StatLogger
                                        {
                                            StartTime = now,
                                            Path = (m_statsDir.Length > 0
                                                        ? m_statsDir + Path.DirectorySeparatorChar.ToString()
                                                        : "")
                                                   + String.Format("stats-{0}.log", now.ToString("yyyyMMddHHmmss"))
                                        };
                        m_statLog.Log = new BinaryWriter(File.Open(m_statLog.Path, FileMode.Append, FileAccess.Write));
                    }

                    // Write the serialized data to disk
                    if (m_statLog != null && m_statLog.Log != null)
                        m_statLog.Log.Write(pack.ToBytes());
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.ErrorFormat("statistics gathering failed: {0}", ex.ToString());
                    if (m_statLog != null && m_statLog.Log != null)
                    {
                        m_statLog.Log.Close();
                    }
                    m_statLog = null;
                }
            }
            return;
        }

        #region Nested type: StatLogger

        public class StatLogger
        {
            public BinaryWriter Log;
            public string Path;
            public DateTime StartTime;
        }

        #endregion
    }
}