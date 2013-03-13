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
using System.Reflection;
using System.Timers;
using Nini.Config;
using Aurora.Framework;

namespace Aurora.Modules.SimProtection
{
    /// <summary>
    ///   This module helps keep the sim running when it begins to slow down, or if it freezes, restarts it
    /// </summary>
    public class SimProtection : INonSharedRegionModule
    {
        #region Declares

        protected bool AllowDisablePhysics = true;
        protected bool AllowDisableScripts = true;

        //Normal Sim FPS
        private float BaseRateFramesPerSecond = 45;
        // When BaseRate / current FPS is less than this percent, begin shutting down services
        protected DateTime DisabledPhysicsStartTime = DateTime.MinValue;
        protected DateTime DisabledScriptStartTime = DateTime.MinValue;
        protected bool KillSimOnZeroFPS = true;
        protected float MinutesBeforeZeroFPSKills = 1;
        protected float PercentToBeginShutDownOfServices = 50;
        protected DateTime SimZeroFPSStartTime = DateTime.MinValue;
        protected float TimeAfterToReenablePhysics = 20;
        protected float TimeAfterToReenableScripts;
        protected float TimeBetweenChecks = 1;
        protected Timer TimerToCheckHeartbeat;
        protected bool m_Enabled;
        protected IScene m_scene;
        protected ISimFrameMonitor m_statsReporter;

        #endregion

        #region INonSharedRegionModule

        public void Initialise(IConfigSource source)
        {
            if (!source.Configs.Contains("Protection"))
                return;
            TimeAfterToReenableScripts = TimeAfterToReenablePhysics*2;
            IConfig config = source.Configs["Protection"];
            m_Enabled = config.GetBoolean("Enabled", false);
            BaseRateFramesPerSecond = config.GetFloat("BaseRateFramesPerSecond", 45);
            PercentToBeginShutDownOfServices = config.GetFloat("PercentToBeginShutDownOfServices", 50);
            TimeAfterToReenablePhysics = config.GetFloat("TimeAfterToReenablePhysics", 20);
            AllowDisableScripts = config.GetBoolean("AllowDisableScripts", true);
            AllowDisablePhysics = config.GetBoolean("AllowDisablePhysics", true);
            KillSimOnZeroFPS = config.GetBoolean("RestartSimIfZeroFPS", true);
            MinutesBeforeZeroFPSKills = config.GetFloat("TimeBeforeZeroFPSKills", 1);
            TimeBetweenChecks = config.GetFloat("TimeBetweenChecks", 1);
        }

        public void Close()
        {
        }

        public void AddRegion(IScene scene)
        {
        }

        public string Name
        {
            get { return "SimProtection"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void RemoveRegion(IScene scene)
        {
            if (!m_Enabled)
                return;
            TimerToCheckHeartbeat.Stop();
        }

        public void RegionLoaded(IScene scene)
        {
            if (!m_Enabled)
                return;
            m_scene = scene;
            BaseRateFramesPerSecond = scene.BaseSimFPS;
            m_statsReporter =
                (ISimFrameMonitor)
                m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(),
                                                                            MonitorModuleHelper.SimFrameStats);
            if (m_statsReporter == null)
            {
                MainConsole.Instance.Warn("[SimProtection]: Cannot be used as SimStatsReporter does not exist.");
                return;
            }
            TimerToCheckHeartbeat = new Timer {Interval = TimeBetweenChecks*1000*60};
            //minutes
            TimerToCheckHeartbeat.Elapsed += OnCheck;
            TimerToCheckHeartbeat.Start();
        }

        #endregion

        #region Protection

        private void OnCheck(object sender, ElapsedEventArgs e)
        {
            IEstateModule mod = m_scene.RequestModuleInterface<IEstateModule>();
            if (AllowDisableScripts &&
                m_statsReporter.LastReportedSimFPS < BaseRateFramesPerSecond*(PercentToBeginShutDownOfServices/100) &&
                m_statsReporter.LastReportedSimFPS != 0)
            {
                //Less than the percent to start shutting things down... Lets kill some stuff
                if (mod != null)
                    mod.SetSceneCoreDebug(false, m_scene.RegionInfo.RegionSettings.DisableCollisions,
                                          m_scene.RegionInfo.RegionSettings.DisablePhysics);
                        //These are opposite of what you want the value to be... go figure
                DisabledScriptStartTime = DateTime.Now;
            }
            if (m_scene.RegionInfo.RegionSettings.DisableScripts &&
                AllowDisableScripts &&
                SimZeroFPSStartTime != DateTime.MinValue &&
                //This makes sure we don't screw up the setting if the user disabled physics manually
                SimZeroFPSStartTime.AddSeconds(TimeAfterToReenableScripts) > DateTime.Now)
            {
                DisabledScriptStartTime = DateTime.MinValue;
                if (mod != null)
                    mod.SetSceneCoreDebug(true, m_scene.RegionInfo.RegionSettings.DisableCollisions,
                                          m_scene.RegionInfo.RegionSettings.DisablePhysics);
                        //These are opposite of what you want the value to be... go figure
            }

            if (m_statsReporter.LastReportedSimFPS == 0 && KillSimOnZeroFPS)
            {
                if (SimZeroFPSStartTime == DateTime.MinValue)
                    SimZeroFPSStartTime = DateTime.Now;
                if (SimZeroFPSStartTime.AddMinutes(MinutesBeforeZeroFPSKills) > SimZeroFPSStartTime)
                    MainConsole.Instance.RunCommand("shutdown");
            }
            else if (SimZeroFPSStartTime != DateTime.MinValue)
                SimZeroFPSStartTime = DateTime.MinValue;

            float[] stats =
                m_scene.RequestModuleInterface<IMonitorModule>().GetRegionStats(m_scene.RegionInfo.RegionID.ToString());
            if (stats[2] /*PhysicsFPS*/< BaseRateFramesPerSecond*(PercentToBeginShutDownOfServices/100) &&
                stats[2] != 0 &&
                AllowDisablePhysics &&
                !m_scene.RegionInfo.RegionSettings.DisablePhysics)
                //Don't redisable physics again, physics will be frozen at the last FPS
            {
                DisabledPhysicsStartTime = DateTime.Now;
                if (mod != null)
                    mod.SetSceneCoreDebug(m_scene.RegionInfo.RegionSettings.DisableScripts,
                                          m_scene.RegionInfo.RegionSettings.DisableCollisions, false);
                        //These are opposite of what you want the value to be... go figure
            }

            if (m_scene.RegionInfo.RegionSettings.DisablePhysics &&
                AllowDisablePhysics &&
                DisabledPhysicsStartTime != DateTime.MinValue &&
                //This makes sure we don't screw up the setting if the user disabled physics manually
                DisabledPhysicsStartTime.AddSeconds(TimeAfterToReenablePhysics) > DateTime.Now)
            {
                DisabledPhysicsStartTime = DateTime.MinValue;
                if (mod != null)
                    mod.SetSceneCoreDebug(m_scene.RegionInfo.RegionSettings.DisableScripts,
                                          m_scene.RegionInfo.RegionSettings.DisableCollisions, true);
                        //These are opposite of what you want the value to be... go figure
            }
        }

        #endregion
    }
}