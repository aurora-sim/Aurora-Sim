using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;

namespace Aurora.Modules
{
    /// <summary>
    /// This module helps keep the sim running when it begins to slow down, or if it freezes, restarts it
    /// </summary>
    public class SimProtection : INonSharedRegionModule
    {
        #region Declares

        //Normal Sim FPS
        private float BaseRateFramesPerSecond = 60;
        // When BaseRate / current FPS is less than this percent, begin shutting down services
        private float PercentToBeginShutDownOfServices = 50;
        private Scene m_scene;
        private bool m_Enabled = false;
        private float TimeAfterToReenablePhysics = 20;
        private DateTime DisabledPhysicsStartTime = DateTime.MinValue;
        private bool AllowDisableScripts = true;
        private bool AllowDisablePhysics = true;
        //Time before a sim sitting at 0FPS is restarted automatically
        private float MinutesBeforeZeroFPSKills = 1;
        private bool KillSimOnZeroFPS = true;
        private DateTime SimZeroFPSStartTime = DateTime.MinValue;
        private Timer TimerToCheckHeartbeat = null;
        private float TimeBetweenChecks = 1;

        #endregion

        #region INonSharedRegionModule

        public void Initialise(IConfigSource source)
        {
            if (!source.Configs.Contains("Protection"))
                return;
            IConfig config = source.Configs["Protection"];
            m_Enabled = config.GetBoolean("Enabled", false);
            BaseRateFramesPerSecond = config.GetFloat("BaseRateFramesPerSecond", 60);
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

        public void AddRegion(Scene scene)
        {
        }

        public void PostInitialise()
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

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;
            TimerToCheckHeartbeat.Stop();
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;
            m_scene = scene;
            TimerToCheckHeartbeat = new Timer();
            TimerToCheckHeartbeat.Interval = TimeBetweenChecks * 1000 * 60;//minutes
            TimerToCheckHeartbeat.Elapsed += OnCheck;
            TimerToCheckHeartbeat.Start();
        }

        #endregion

        #region Protection

        private void OnCheck(object sender, ElapsedEventArgs e)
        {
            if (m_scene.StatsReporter.getLastReportedSimFPS() < BaseRateFramesPerSecond * (PercentToBeginShutDownOfServices / 100) && m_scene.StatsReporter.getLastReportedSimFPS() != 0 && AllowDisableScripts)
            {
                //Less than the percent to start shutting things down... Lets kill some stuff
                IScriptModule scriptEngine = m_scene.RequestModuleInterface<IScriptModule>();
                if (scriptEngine != null)
                {
                    scriptEngine.StopAllScripts();
                }
            }

            if (m_scene.StatsReporter.getLastReportedSimFPS() == 0 && KillSimOnZeroFPS)
            {
                if (SimZeroFPSStartTime == DateTime.MinValue)
                    SimZeroFPSStartTime = DateTime.Now;
                if (SimZeroFPSStartTime.AddMinutes(MinutesBeforeZeroFPSKills) > SimZeroFPSStartTime)
                    MainConsole.Instance.RunCommand("shutdown");
            }
            else if (SimZeroFPSStartTime != DateTime.MinValue)
                SimZeroFPSStartTime = DateTime.MinValue;

            if (m_scene.StatsReporter.getLastReportedSimStats()[2]/*PhysicsFPS*/ < BaseRateFramesPerSecond * (PercentToBeginShutDownOfServices / 100) &&
                m_scene.StatsReporter.getLastReportedSimStats()[2] != 0 &&
                AllowDisablePhysics &&
                !m_scene.RegionInfo.RegionSettings.DisablePhysics) //Don't redisable physics again, physics will be frozen at the last FPS
            {
                DisabledPhysicsStartTime = DateTime.Now;
                m_scene.SetSceneCoreDebug(m_scene.RegionInfo.RegionSettings.DisableScripts, m_scene.RegionInfo.RegionSettings.DisableCollisions, false); //These are opposite of what you want the value to be... go figure
            }

            if (m_scene.RegionInfo.RegionSettings.DisablePhysics &&
                AllowDisablePhysics &&
                DisabledPhysicsStartTime != DateTime.MinValue && //This makes sure we don't screw up the setting if the user disabled physics manually
                DisabledPhysicsStartTime.AddSeconds(TimeAfterToReenablePhysics) > DateTime.Now)
            {
                DisabledPhysicsStartTime = DateTime.MinValue;
                m_scene.SetSceneCoreDebug(m_scene.RegionInfo.RegionSettings.DisableScripts, m_scene.RegionInfo.RegionSettings.DisableCollisions, true);//These are opposite of what you want the value to be... go figure
            }
        }

        #endregion
    }
}
