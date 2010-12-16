using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Threading;
using System.Text;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;
using OpenMetaverse;
using Aurora.Framework;
using log4net;

namespace Aurora.Modules
{
    public class PhysicsMonitor : ISharedRegionModule, IPhysicsMonitor
    {
        #region Private class

        protected class PhysicsStats
        {
            public float StatPhysicsTaintTime;
            public float StatPhysicsMoveTime;
            public float StatCollisionOptimizedTime;
            public float StatSendCollisionsTime;
            public float StatAvatarUpdatePosAndVelocity;
            public float StatPrimUpdatePosAndVelocity;
            public float StatUnlockedArea;
        }

        #endregion

        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected DateTime m_lastUpdated = DateTime.Now;
        protected Dictionary<UUID, PhysicsStats> m_lastPhysicsStats = new Dictionary<UUID, PhysicsStats>();
        protected Dictionary<UUID, PhysicsStats> m_currentPhysicsStats = new Dictionary<UUID, PhysicsStats>();
        protected Timer m_physicsStatTimer;
        protected List<Scene> m_scenes = new List<Scene>();

        #endregion

        #region ISharedRegionModule Members

        public string Name
        {
            get { return "PhysicsMonitor"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
            if (m_physicsStatTimer == null)
            {
                m_physicsStatTimer = new Timer();
                m_physicsStatTimer.Interval = 10000;
                m_physicsStatTimer.Enabled = true;
                m_physicsStatTimer.Elapsed += PhysicsStatsHeartbeat;
                m_physicsStatTimer.Start();
            }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            m_scenes.Add(scene);
            scene.RegisterInterface<IPhysicsMonitor>(this);
            scene.AddCommand(this, "physics stats", "physics stats", "physics stats <region>", PhysicsStatsCommand);
            scene.AddCommand(this, "physics profiler", "physics profiler", "physics profiler <region>", PhysicsProfilerCommand);
            scene.AddCommand(this, "physics current stats", "physics current stats", "physics current stats <region> NOTE: these are not calculated and are in milliseconds per unknown time", CurrentPhysicsStatsCommand);
        }

        public void RemoveRegion(Scene scene)
        {
            m_scenes.Remove(scene);
            scene.UnregisterModuleInterface<IPhysicsMonitor>(this);
        }

        public void RegionLoaded(Scene scene)
        {
        }

        #endregion

        protected virtual void PhysicsStatsCommand(string module, string[] cmd)
        {
            List<Scene> scenesToRun = new List<Scene>();
            if (cmd.Length == 3)
            {
                foreach (Scene scene in m_scenes)
                {
                    if (scene.RegionInfo.RegionName == cmd[2])
                        scenesToRun.Add(scene);
                }
                if (scenesToRun.Count == 0)
                    scenesToRun = m_scenes;
            }
            else
                scenesToRun.AddRange(m_scenes);

            foreach (Scene scene in scenesToRun)
            {
                PhysicsStats stats = null;
                while (stats == null)
                {
                    m_lastPhysicsStats.TryGetValue(scene.RegionInfo.RegionID, out stats);
                }
                DumpStatsToConsole(scene, stats);
            }
        }

        protected virtual void PhysicsProfilerCommand(string module, string[] cmd)
        {
            List<Scene> scenesToRun = new List<Scene>();
            if (cmd.Length == 3)
            {
                foreach (Scene scene in m_scenes)
                {
                    if (scene.RegionInfo.RegionName == cmd[2])
                        scenesToRun.Add(scene);
                }
                if (scenesToRun.Count == 0)
                    scenesToRun = m_scenes;
            }
            else
                scenesToRun.AddRange(m_scenes);
            Thread thread = new Thread(StartThread);
            thread.Start(scenesToRun);
        }

        private void StartThread(object scenes)
        {
            try
            {
                List<Scene> scenesToRun = (List<Scene>)scenes;
                System.Windows.Forms.Application.Run(new PhysicsProfilerForm(scenesToRun));
            }
            catch(Exception ex)
            {
                m_log.Warn("There was an error opening the form: " + ex.ToString());
            }
        }

        protected virtual void CurrentPhysicsStatsCommand(string module, string[] cmd)
        {
            List<Scene> scenesToRun = new List<Scene>();
            if (cmd.Length == 3)
            {
                foreach (Scene scene in m_scenes)
                {
                    if (scene.RegionInfo.RegionName == cmd[2])
                        scenesToRun.Add(scene);
                }
                if (scenesToRun.Count == 0)
                    scenesToRun = m_scenes;
            }
            else
                scenesToRun.AddRange(m_scenes);

            foreach (Scene scene in scenesToRun)
            {
                PhysicsStats stats = null;
                while (stats == null)
                {
                    m_currentPhysicsStats.TryGetValue(scene.RegionInfo.RegionID, out stats);
                }
                DumpStatsToConsole(scene, stats);
            }
        }

        protected virtual void DumpStatsToConsole(Scene scene, PhysicsStats stats)
        {
            MainConsole.Instance.Output("------  Physics Stats for region " + scene.RegionInfo.RegionName + "  ------", "Normal");
            MainConsole.Instance.Output("   All stats are in milliseconds spent per second.", "Normal");
            MainConsole.Instance.Output("   These are in the order they are run in the PhysicsScene.", "Normal");
            MainConsole.Instance.Output(" PhysicsTaintTime: " + stats.StatPhysicsTaintTime, "Normal");
            MainConsole.Instance.Output(" PhysicsMoveTime: " + stats.StatPhysicsMoveTime, "Normal");
            MainConsole.Instance.Output(" CollisionOptimizedTime: " + stats.StatCollisionOptimizedTime, "Normal");
            MainConsole.Instance.Output(" SendCollisionsTime: " + stats.StatSendCollisionsTime, "Normal");
            MainConsole.Instance.Output(" AvatarUpdatePosAndVelocity: " + stats.StatAvatarUpdatePosAndVelocity, "Normal");
            MainConsole.Instance.Output(" PrimUpdatePosAndVelocity: " + stats.StatPrimUpdatePosAndVelocity, "Normal");
            MainConsole.Instance.Output(" UnlockedArea: " + stats.StatUnlockedArea, "Normal");
            MainConsole.Instance.Output("", "Normal");
        }

        protected virtual void PhysicsStatsHeartbeat(object sender, ElapsedEventArgs e)
        {
            lock (m_currentPhysicsStats)
            {
                foreach (KeyValuePair<UUID, PhysicsStats> kvp in m_currentPhysicsStats)
                {
                    //Save the stats in the last one so we can keep them for the console commands
                    //Divide by 10 so we get per second
                    m_lastPhysicsStats[kvp.Key] = kvp.Value;
                    m_lastPhysicsStats[kvp.Key].StatAvatarUpdatePosAndVelocity /= 10;
                    m_lastPhysicsStats[kvp.Key].StatCollisionOptimizedTime /= 10;
                    m_lastPhysicsStats[kvp.Key].StatPhysicsMoveTime /= 10;
                    m_lastPhysicsStats[kvp.Key].StatPhysicsTaintTime /= 10;
                    m_lastPhysicsStats[kvp.Key].StatPrimUpdatePosAndVelocity /= 10;
                    m_lastPhysicsStats[kvp.Key].StatSendCollisionsTime /= 10;
                    m_lastPhysicsStats[kvp.Key].StatUnlockedArea /= 10;
                    //Add the stats to the profiler
                    Profiler p = ProfilerManager.GetProfiler();
                    p.AddStat("StatAvatarUpdatePosAndVelocity " + kvp.Key,
                        new ProfilerValueInfo() { Value = m_lastPhysicsStats[kvp.Key].StatAvatarUpdatePosAndVelocity });
                    p.AddStat("StatCollisionOptimizedTime " + kvp.Key,
                        new ProfilerValueInfo() { Value = m_lastPhysicsStats[kvp.Key].StatCollisionOptimizedTime });
                    p.AddStat("StatPhysicsMoveTime " + kvp.Key,
                        new ProfilerValueInfo() { Value = m_lastPhysicsStats[kvp.Key].StatPhysicsMoveTime });
                    p.AddStat("StatPhysicsTaintTime " + kvp.Key,
                        new ProfilerValueInfo() { Value = m_lastPhysicsStats[kvp.Key].StatPhysicsTaintTime });
                    p.AddStat("StatPrimUpdatePosAndVelocity " + kvp.Key,
                        new ProfilerValueInfo() { Value = m_lastPhysicsStats[kvp.Key].StatPrimUpdatePosAndVelocity });
                    p.AddStat("StatSendCollisionsTime " + kvp.Key,
                        new ProfilerValueInfo() { Value = m_lastPhysicsStats[kvp.Key].StatSendCollisionsTime });
                    p.AddStat("StatUnlockedArea " + kvp.Key,
                        new ProfilerValueInfo() { Value = m_lastPhysicsStats[kvp.Key].StatUnlockedArea });
                }
            }
            m_lastUpdated = DateTime.Now;
        }

        public virtual void AddPhysicsStats(UUID RegionID, PhysicsScene scene)
        {
            lock (m_currentPhysicsStats)
            {
                PhysicsStats stats;
                if (!m_currentPhysicsStats.TryGetValue(RegionID, out stats))
                {
                    stats = new PhysicsStats();
                    stats.StatAvatarUpdatePosAndVelocity = scene.StatAvatarUpdatePosAndVelocity;
                    stats.StatCollisionOptimizedTime = scene.StatCollisionOptimizedTime;
                    stats.StatPhysicsMoveTime = scene.StatPhysicsMoveTime;
                    stats.StatPhysicsTaintTime = scene.StatPhysicsTaintTime;
                    stats.StatPrimUpdatePosAndVelocity = scene.StatPrimUpdatePosAndVelocity;
                    stats.StatSendCollisionsTime = scene.StatSendCollisionsTime;
                    stats.StatUnlockedArea = scene.StatUnlockedArea;
                }
                else
                {
                    stats.StatAvatarUpdatePosAndVelocity += scene.StatAvatarUpdatePosAndVelocity;
                    stats.StatCollisionOptimizedTime += scene.StatCollisionOptimizedTime;
                    stats.StatPhysicsMoveTime += scene.StatPhysicsMoveTime;
                    stats.StatPhysicsTaintTime += scene.StatPhysicsTaintTime;
                    stats.StatPrimUpdatePosAndVelocity += scene.StatPrimUpdatePosAndVelocity;
                    stats.StatSendCollisionsTime += scene.StatSendCollisionsTime;
                    stats.StatUnlockedArea += scene.StatUnlockedArea;
                }

                m_currentPhysicsStats[RegionID] = stats;

                PhysicsStats ProfilerStats = new PhysicsStats();
                ProfilerStats.StatAvatarUpdatePosAndVelocity = scene.StatAvatarUpdatePosAndVelocity;
                ProfilerStats.StatCollisionOptimizedTime = scene.StatCollisionOptimizedTime;
                ProfilerStats.StatPhysicsMoveTime = scene.StatPhysicsMoveTime;
                ProfilerStats.StatPhysicsTaintTime = scene.StatPhysicsTaintTime;
                ProfilerStats.StatPrimUpdatePosAndVelocity = scene.StatPrimUpdatePosAndVelocity;
                ProfilerStats.StatSendCollisionsTime = scene.StatSendCollisionsTime;
                ProfilerStats.StatUnlockedArea = scene.StatUnlockedArea;

                //Add the stats to the profiler
                Profiler p = ProfilerManager.GetProfiler();
                p.AddStat("CurrentStatAvatarUpdatePosAndVelocity " + RegionID,
                    new ProfilerValueInfo() { Value = ProfilerStats.StatAvatarUpdatePosAndVelocity });
                p.AddStat("CurrentStatCollisionOptimizedTime " + RegionID,
                    new ProfilerValueInfo() { Value = ProfilerStats.StatCollisionOptimizedTime });
                p.AddStat("CurrentStatPhysicsMoveTime " + RegionID,
                    new ProfilerValueInfo() { Value = ProfilerStats.StatPhysicsMoveTime });
                p.AddStat("CurrentStatPhysicsTaintTime " + RegionID,
                    new ProfilerValueInfo() { Value = ProfilerStats.StatPhysicsTaintTime });
                p.AddStat("CurrentStatPrimUpdatePosAndVelocity " + RegionID,
                    new ProfilerValueInfo() { Value = ProfilerStats.StatPrimUpdatePosAndVelocity });
                p.AddStat("CurrentStatSendCollisionsTime " + RegionID,
                    new ProfilerValueInfo() { Value = ProfilerStats.StatSendCollisionsTime });
                p.AddStat("CurrentStatUnlockedArea " + RegionID,
                    new ProfilerValueInfo() { Value = ProfilerStats.StatUnlockedArea });
            }
        }
    }
}
