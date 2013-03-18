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

using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace Aurora.Modules.SimProtection
{
    public class PhysicsMonitor : INonSharedRegionModule, IPhysicsMonitor
    {
        #region Private class

        protected class PhysicsStats
        {
            public float StatAvatarUpdatePosAndVelocity;
            public float StatCollisionAccountingTime;
            public float StatCollisionOptimizedTime;
            public float StatContactLoopTime;
            public float StatFindContactsTime;
            public float StatPhysicsMoveTime;
            public float StatPhysicsTaintTime;
            public float StatPrimUpdatePosAndVelocity;
            public float StatSendCollisionsTime;
            public float StatUnlockedArea;
        }

        #endregion

        #region Declares

        public bool m_collectingStats;

        protected Dictionary<UUID, PhysicsStats> m_currentPhysicsStats = new Dictionary<UUID, PhysicsStats>();
        protected Dictionary<UUID, PhysicsStats> m_lastPhysicsStats = new Dictionary<UUID, PhysicsStats>();
        protected DateTime m_lastUpdated = DateTime.Now;
        protected Timer m_physicsStatTimer;
        protected IScene m_Scene;
        protected int m_waitingForCollectionOfStats;

        #endregion

        #region IPhysicsMonitor Members

        public virtual void AddPhysicsStats(UUID RegionID, PhysicsScene scene)
        {
            if (!m_collectingStats)
                return;
            lock (m_currentPhysicsStats)
            {
                PhysicsStats stats;
                if (!m_currentPhysicsStats.TryGetValue(RegionID, out stats))
                {
                    stats = new PhysicsStats
                                {
                                    StatAvatarUpdatePosAndVelocity = scene.StatAvatarUpdatePosAndVelocity,
                                    StatCollisionOptimizedTime = scene.StatCollisionOptimizedTime,
                                    StatPhysicsMoveTime = scene.StatPhysicsMoveTime,
                                    StatPhysicsTaintTime = scene.StatPhysicsTaintTime,
                                    StatPrimUpdatePosAndVelocity = scene.StatPrimUpdatePosAndVelocity,
                                    StatSendCollisionsTime = scene.StatSendCollisionsTime,
                                    StatUnlockedArea = scene.StatUnlockedArea,
                                    StatFindContactsTime = scene.StatFindContactsTime,
                                    StatContactLoopTime = scene.StatContactLoopTime,
                                    StatCollisionAccountingTime = scene.StatCollisionAccountingTime
                                };
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
                    stats.StatFindContactsTime += scene.StatFindContactsTime;
                    stats.StatContactLoopTime += scene.StatContactLoopTime;
                    stats.StatCollisionAccountingTime += scene.StatCollisionAccountingTime;
                }

                m_currentPhysicsStats[RegionID] = stats;

                PhysicsStats ProfilerStats = new PhysicsStats
                                                 {
                                                     StatAvatarUpdatePosAndVelocity =
                                                         scene.StatAvatarUpdatePosAndVelocity,
                                                     StatCollisionOptimizedTime = scene.StatCollisionOptimizedTime,
                                                     StatPhysicsMoveTime = scene.StatPhysicsMoveTime,
                                                     StatPhysicsTaintTime = scene.StatPhysicsTaintTime,
                                                     StatPrimUpdatePosAndVelocity = scene.StatPrimUpdatePosAndVelocity,
                                                     StatSendCollisionsTime = scene.StatSendCollisionsTime,
                                                     StatUnlockedArea = scene.StatUnlockedArea,
                                                     StatFindContactsTime = scene.StatFindContactsTime,
                                                     StatContactLoopTime = scene.StatContactLoopTime,
                                                     StatCollisionAccountingTime = scene.StatCollisionAccountingTime
                                                 };

                //Add the stats to the profiler
                Profiler p = ProfilerManager.GetProfiler();
                p.AddStat("CurrentStatAvatarUpdatePosAndVelocity " + RegionID,
                          ProfilerStats.StatAvatarUpdatePosAndVelocity);
                p.AddStat("CurrentStatCollisionOptimizedTime " + RegionID,
                          ProfilerStats.StatCollisionOptimizedTime);
                p.AddStat("CurrentStatPhysicsMoveTime " + RegionID,
                          ProfilerStats.StatPhysicsMoveTime);
                p.AddStat("CurrentStatPhysicsTaintTime " + RegionID,
                          ProfilerStats.StatPhysicsTaintTime);
                p.AddStat("CurrentStatPrimUpdatePosAndVelocity " + RegionID,
                          ProfilerStats.StatPrimUpdatePosAndVelocity);
                p.AddStat("CurrentStatSendCollisionsTime " + RegionID,
                          ProfilerStats.StatSendCollisionsTime);
                p.AddStat("CurrentStatUnlockedArea " + RegionID,
                          ProfilerStats.StatUnlockedArea);
                p.AddStat("CurrentStatFindContactsTime " + RegionID,
                          ProfilerStats.StatFindContactsTime);
                p.AddStat("CurrentStatContactLoopTime " + RegionID,
                          ProfilerStats.StatContactLoopTime);
                p.AddStat("CurrentStatCollisionAccountingTime " + RegionID,
                          ProfilerStats.StatCollisionAccountingTime);
            }
        }

        #endregion

        #region INonSharedRegionModule Members

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
                m_physicsStatTimer = new Timer {Interval = 10000};
                m_physicsStatTimer.Elapsed += PhysicsStatsHeartbeat;
            }
        }

        public void Close()
        {
        }

        public void AddRegion(IScene scene)
        {
            m_Scene = scene;
            scene.RegisterModuleInterface<IPhysicsMonitor>(this);
            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand(
                    "physics stats", "physics stats", "physics stats <region>", PhysicsStatsCommand);
                MainConsole.Instance.Commands.AddCommand(
                    "physics profiler", "physics profiler", "physics profiler <region>", PhysicsProfilerCommand);
                MainConsole.Instance.Commands.AddCommand(
                    "physics current stats", "physics current stats",
                    "physics current stats <region> NOTE: these are not calculated and are in milliseconds per unknown time",
                    CurrentPhysicsStatsCommand);
            }
        }

        public void RemoveRegion(IScene scene)
        {
            m_Scene = null;
            scene.UnregisterModuleInterface<IPhysicsMonitor>(this);
        }

        public void RegionLoaded(IScene scene)
        {
        }

        #endregion

        protected virtual void PhysicsStatsCommand(string[] cmd)
        {
            if (cmd.Length == 3)
            {
                if (m_Scene.RegionInfo.RegionName != cmd[2])
                    return;
            }

            //Set all the bools to true
            m_collectingStats = true;
            m_waitingForCollectionOfStats = 1;
            //Start the timer as well
            m_physicsStatTimer.Start();
            MainConsole.Instance.Info("Collecting Stats Now... Please wait...");
            while (m_waitingForCollectionOfStats > 0)
            {
                Thread.Sleep(50);
            }

            PhysicsStats stats = null;
            while (stats == null)
            {
                m_lastPhysicsStats.TryGetValue(m_Scene.RegionInfo.RegionID, out stats);
            }
            DumpStatsToConsole(m_Scene, stats);
        }

        protected virtual void PhysicsProfilerCommand(string[] cmd)
        {
            if (cmd.Length == 3)
            {
                if (m_Scene.RegionInfo.RegionName != cmd[2])
                    return;
            }

            //Set all the bools to true
            m_collectingStats = true;
            m_waitingForCollectionOfStats = 1;
            //Start the timer as well
            m_physicsStatTimer.Start();
            MainConsole.Instance.Info("Collecting Stats Now... Please wait...");
            while (m_waitingForCollectionOfStats > 0)
            {
                Thread.Sleep(50);
            }

            Thread thread = new Thread(StartThread);
            thread.Start(new List<IScene>(){m_Scene});
        }

        private void StartThread(object scenes)
        {
            Culture.SetCurrentCulture();
            try
            {
                List<IScene> scenesToRun = (List<IScene>) scenes;
                Application.Run(new PhysicsProfilerForm(this, scenesToRun));
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("There was an error opening the form: " + ex);
            }
        }

        protected virtual void CurrentPhysicsStatsCommand(string[] cmd)
        {
            if (cmd.Length == 3)
            {
                if (m_Scene.RegionInfo.RegionName != cmd[2])
                    return;
            }

            //Set all the bools to true
            m_collectingStats = true;
            m_waitingForCollectionOfStats = 1;
            //Start the timer as well
            m_physicsStatTimer.Start();
            MainConsole.Instance.Info("Collecting Stats Now... Please wait...");
            while (m_waitingForCollectionOfStats > 0)
            {
                Thread.Sleep(50);
            }

            PhysicsStats stats = null;
            while (stats == null)
            {
                m_currentPhysicsStats.TryGetValue(m_Scene.RegionInfo.RegionID, out stats);
            }
            DumpStatsToConsole(m_Scene, stats);
        }

        protected virtual void DumpStatsToConsole(IScene scene, PhysicsStats stats)
        {
            MainConsole.Instance.Info("------  Physics Stats for region " + scene.RegionInfo.RegionName + "  ------");
            MainConsole.Instance.Info("   All stats are in milliseconds spent per second.");
            MainConsole.Instance.Info("   These are in the order they are run in the PhysicsScene.");
            MainConsole.Instance.Info(" PhysicsTaintTime: " + stats.StatPhysicsTaintTime);
            MainConsole.Instance.Info(" PhysicsMoveTime: " + stats.StatPhysicsMoveTime);
            MainConsole.Instance.Info(" FindContactsTime: " + stats.StatFindContactsTime);
            MainConsole.Instance.Info(" ContactLoopTime: " + stats.StatContactLoopTime);
            MainConsole.Instance.Info(" CollisionAccountingTime: " + stats.StatCollisionAccountingTime);
            MainConsole.Instance.Info(" CollisionOptimizedTime: " + stats.StatCollisionOptimizedTime);
            MainConsole.Instance.Info(" SendCollisionsTime: " + stats.StatSendCollisionsTime);
            MainConsole.Instance.Info(" AvatarUpdatePosAndVelocity: " + stats.StatAvatarUpdatePosAndVelocity);
            MainConsole.Instance.Info(" PrimUpdatePosAndVelocity: " + stats.StatPrimUpdatePosAndVelocity);
            MainConsole.Instance.Info(" UnlockedArea: " + stats.StatUnlockedArea);
            MainConsole.Instance.Info("");
        }

        protected virtual void PhysicsStatsHeartbeat(object sender, ElapsedEventArgs e)
        {
            if (!m_collectingStats || m_currentPhysicsStats.Count == 0)
                return;
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
                    m_lastPhysicsStats[kvp.Key].StatFindContactsTime /= 10;
                    m_lastPhysicsStats[kvp.Key].StatContactLoopTime /= 10;
                    m_lastPhysicsStats[kvp.Key].StatCollisionAccountingTime /= 10;
                    //Add the stats to the profiler
                    Profiler p = ProfilerManager.GetProfiler();
                    p.AddStat("StatAvatarUpdatePosAndVelocity " + kvp.Key,
                              m_lastPhysicsStats[kvp.Key].StatAvatarUpdatePosAndVelocity);
                    p.AddStat("StatCollisionOptimizedTime " + kvp.Key,
                              m_lastPhysicsStats[kvp.Key].StatCollisionOptimizedTime);
                    p.AddStat("StatPhysicsMoveTime " + kvp.Key,
                              m_lastPhysicsStats[kvp.Key].StatPhysicsMoveTime);
                    p.AddStat("StatPhysicsTaintTime " + kvp.Key,
                              m_lastPhysicsStats[kvp.Key].StatPhysicsTaintTime);
                    p.AddStat("StatPrimUpdatePosAndVelocity " + kvp.Key,
                              m_lastPhysicsStats[kvp.Key].StatPrimUpdatePosAndVelocity);
                    p.AddStat("StatSendCollisionsTime " + kvp.Key,
                              m_lastPhysicsStats[kvp.Key].StatSendCollisionsTime);
                    p.AddStat("StatUnlockedArea " + kvp.Key,
                              m_lastPhysicsStats[kvp.Key].StatUnlockedArea);
                    p.AddStat("StatFindContactsTime " + kvp.Key,
                              m_lastPhysicsStats[kvp.Key].StatFindContactsTime);
                    p.AddStat("StatContactLoopTime " + kvp.Key,
                              m_lastPhysicsStats[kvp.Key].StatContactLoopTime);
                    p.AddStat("StatCollisionAccountingTime " + kvp.Key,
                              m_lastPhysicsStats[kvp.Key].StatCollisionAccountingTime);
                }
                m_currentPhysicsStats.Clear();
            }
            m_lastUpdated = DateTime.Now;
            //If there are stats waiting, we just pulled them
            m_waitingForCollectionOfStats--;
        }
    }
}