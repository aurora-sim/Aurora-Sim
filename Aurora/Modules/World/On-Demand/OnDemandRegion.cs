using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Modules.World.On_Demand
{
    /// <summary>
    /// Some notes on this module, this module just modifies when/where the startup code is executed
    /// This module has a few different settings for the region to startup with, 
    /// Soft, Medium, and Normal (no change)
    /// 
    /// -- Soft --
    /// Disables the heartbeats (not scripts, as its instance-wide)
    /// Only loads land and parcels, no prims
    /// 
    /// -- Medium -- 
    /// Same as Soft, except it loads prims (same as normal, but no threads)
    /// 
    /// -- Normal --
    /// Same as always
    /// </summary>
    public class OnDemandRegionModule : INonSharedRegionModule
    {
        #region Declares

        private Scene m_scene;
        private int m_waitTime = 0;
        private bool m_isShuttingDown = false;
        private bool m_isStartingUp = false;
        private bool m_isRunning = false;
        private List<UUID> m_zombieAgents = new List<UUID> ();

        #endregion

        #region IRegionModuleBase Members

        public void Initialise (IConfigSource source)
        {
        }

        public void AddRegion (Scene scene)
        {
            if (scene.RegionInfo.Startup != StartupType.Normal)
            {
                m_scene = scene;
                //Disable the heartbeat for this region
                scene.ShouldRunHeartbeat = false;

                scene.EventManager.OnRemovePresence += OnRemovePresence;
                scene.AuroraEventManager.RegisterEventHandler("NewUserConnection", OnGenericEvent);
                scene.AuroraEventManager.RegisterEventHandler ("AgentIsAZombie", OnGenericEvent);
            }
        }

        public void RegionLoaded (Scene scene)
        {
        }

        public void RemoveRegion (Scene scene)
        {
        }

        public void Close ()
        {
        }

        public string Name
        {
            get { return "OnDemandRegionModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        #region Private Events

        private object OnGenericEvent (string FunctionName, object parameters)
        {
            if (FunctionName == "NewUserConnection")
            {
                if (!m_isRunning)
                {
                    m_isRunning = true;
                    object[] obj = (object[])parameters;
                    OSDMap responseMap = (OSDMap)obj[0];
                    //Tell the caller that we will have to wait a bit possibly
                    responseMap["WaitTime"] = m_waitTime;
                    if (m_scene.RegionInfo.Startup == StartupType.Medium)
                    {
                        m_scene.AuroraEventManager.FireGenericEventHandler ("MediumStartup", m_scene);
                        MediumStartup ();
                    }
                }
            }
            else if (FunctionName == "AgentIsAZombie")
                m_zombieAgents.Add ((UUID)parameters);
            return null;
        }

        private void OnRemovePresence (IScenePresence presence)
        {
            if (m_scene.GetScenePresences ().Count == 1) //This presence hasn't been removed yet, so we check against one
            {
                if (m_zombieAgents.Contains (presence.UUID))
                {
                    m_zombieAgents.Remove (presence.UUID);
                    return; //It'll be readding an agent, don't kill the sim immediately
                }
                //If all clients are out of the region, we can close it again
                if (m_scene.RegionInfo.Startup == StartupType.Medium)
                {
                    m_scene.AuroraEventManager.FireGenericEventHandler ("MediumShutdown", m_scene);
                    MediumShutdown ();
                }
                m_isRunning = false;
            }
        }

        #endregion

        #region Private Shutdown Methods

        private void MediumShutdown ()
        {
            //Only shut down one at a time
            if (m_isShuttingDown)
                return;
            m_isShuttingDown = true;
            GenericShutdown ();
            m_isShuttingDown = false;
        }

        /// <summary>
        /// This shuts down the heartbeats so that everything is dead again
        /// </summary>
        private void GenericShutdown ()
        {
            //After the next iteration, the threads will kill themselves
            m_scene.ShouldRunHeartbeat = false;
        }

        #endregion

        #region Private Startup Methods

        /// <summary>
        /// We've already loaded prims/parcels/land earlier, 
        /// we don't have anything else to load, 
        /// so we just need to get the heartbeats back on track
        /// </summary>
        private void MediumStartup ()
        {
            //Only start up one at a time
            if (m_isStartingUp)
                return;
            m_isStartingUp = true;

            GenericStartup ();

            m_isStartingUp = false;
        }

        /// <summary>
        /// This sets up the heartbeats so that they are running again, which is needed
        /// </summary>
        private void GenericStartup ()
        {
            m_scene.ShouldRunHeartbeat = true;
            m_scene.StartHeartbeat ();
        }

        #endregion
    }
}
