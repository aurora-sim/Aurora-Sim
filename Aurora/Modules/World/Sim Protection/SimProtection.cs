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

        private static readonly log4net.ILog m_log
            = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //Normal Sim FPS
        private float BaseRateFramesPerSecond = 60;
        // When BaseRate / current FPS is less than this percent, begin shutting down services
        protected float PercentToBeginShutDownOfServices = 50;
        protected Scene m_scene;
        protected bool m_Enabled = false;
        protected float TimeAfterToReenablePhysics = 20;
        protected DateTime DisabledPhysicsStartTime = DateTime.MinValue;
        protected bool AllowDisableScripts = true;
        protected bool AllowDisablePhysics = true;
        //Time before a sim sitting at 0FPS is restarted automatically
        protected float MinutesBeforeZeroFPSKills = 1;
        protected bool KillSimOnZeroFPS = true;
        protected DateTime SimZeroFPSStartTime = DateTime.MinValue;
        protected Timer TimerToCheckHeartbeat = null;
        protected float TimeBetweenChecks = 1;
        protected ISimFrameMonitor m_statsReporter = null;

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
            m_statsReporter =  (ISimFrameMonitor)m_scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_scene.RegionInfo.RegionID.ToString(), "SimFrameStats");
            if (m_statsReporter == null)
            {
                m_log.Warn("[SimProtection]: Cannot be used as SimStatsReporter does not exist.");
                return;
            }
            TimerToCheckHeartbeat = new Timer();
            TimerToCheckHeartbeat.Interval = TimeBetweenChecks * 1000 * 60;//minutes
            TimerToCheckHeartbeat.Elapsed += OnCheck;
            TimerToCheckHeartbeat.Start();
        }

        #endregion

        #region Protection

        private void OnCheck(object sender, ElapsedEventArgs e)
        {
            if (m_statsReporter.LastReportedSimFPS < BaseRateFramesPerSecond * (PercentToBeginShutDownOfServices / 100) && m_statsReporter.LastReportedSimFPS != 0 && AllowDisableScripts)
            {
                //Less than the percent to start shutting things down... Lets kill some stuff
                IScriptModule scriptEngine = m_scene.RequestModuleInterface<IScriptModule>();
                if (scriptEngine != null)
                {
                    scriptEngine.StopAllScripts();
                }
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
            
            IEstateModule mod = m_scene.RequestModuleInterface<IEstateModule>();
            
            float[] stats = m_scene.RequestModuleInterface<IMonitorModule>().GetRegionStats(m_scene.RegionInfo.RegionID.ToString());
            if (stats[2]/*PhysicsFPS*/ < BaseRateFramesPerSecond * (PercentToBeginShutDownOfServices / 100) &&
                stats[2] != 0 &&
                AllowDisablePhysics &&
                !m_scene.RegionInfo.RegionSettings.DisablePhysics) //Don't redisable physics again, physics will be frozen at the last FPS
            {
                DisabledPhysicsStartTime = DateTime.Now;
                if (mod != null)
                    mod.SetSceneCoreDebug(m_scene.RegionInfo.RegionSettings.DisableScripts, m_scene.RegionInfo.RegionSettings.DisableCollisions, false); //These are opposite of what you want the value to be... go figure
            }

            if (m_scene.RegionInfo.RegionSettings.DisablePhysics &&
                AllowDisablePhysics &&
                DisabledPhysicsStartTime != DateTime.MinValue && //This makes sure we don't screw up the setting if the user disabled physics manually
                DisabledPhysicsStartTime.AddSeconds(TimeAfterToReenablePhysics) > DateTime.Now)
            {
                DisabledPhysicsStartTime = DateTime.MinValue;
                if (mod != null)
                    mod.SetSceneCoreDebug(m_scene.RegionInfo.RegionSettings.DisableScripts, m_scene.RegionInfo.RegionSettings.DisableCollisions, true);//These are opposite of what you want the value to be... go figure
            }
        }

        #endregion
    }
}
