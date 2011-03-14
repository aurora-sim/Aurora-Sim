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
        #region IRegionModuleBase Members

        private Scene m_scene;
        private bool m_enabledForThisScene = false;
        private int m_waitTime = 0;

        public void Initialise (IConfigSource source)
        {
        }

        public void AddRegion (Scene scene)
        {
            if (scene.RegionInfo.Startup != StartupType.Normal)
            {
                m_enabledForThisScene = true;
                scene.ShouldRunHeartbeat = false;
                scene.AuroraEventManager.OnGenericEvent += OnGenericEvent;
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

        object OnGenericEvent (string FunctionName, object parameters)
        {
            if (FunctionName == "NewUserConnection")
            {
                OSDMap responseMap = (OSDMap)parameters;
                //Tell the caller that we will have to wait a bit possibly
                responseMap["WaitTime"] = m_waitTime;
                if (m_scene.RegionInfo.Startup == StartupType.Medium)
                {
                    m_scene.AuroraEventManager.FireGenericEventHandler ("MediumStartup", m_scene);
                    MediumStartup ();
                }
                else if (m_scene.RegionInfo.Startup == StartupType.Soft)
                {
                    m_scene.AuroraEventManager.FireGenericEventHandler ("SoftStartup", m_scene);
                    SoftStartup ();
                }
            }
            return null;
        }

        /// <summary>
        /// We havn't loaded prims, we need to do this now!
        /// </summary>
        private void SoftStartup ()
        {
            IBackupModule backup = m_scene.RequestModuleInterface<IBackupModule> ();
            if (backup != null)
            {
                backup.LoadPrimsFromStorage ();
            }
            GenericStartup ();
        }

        /// <summary>
        /// We've already loaded prims/parcels/land earlier, 
        /// we don't have anything else to load, 
        /// so we just need to get the heartbeats back on track
        /// </summary>
        private void MediumStartup ()
        {
            GenericStartup ();
        }

        /// <summary>
        /// This sets up the heartbeats so that they are running again, which is needed
        /// </summary>
        private void GenericStartup ()
        {
            m_scene.ShouldRunHeartbeat = true;
            m_scene.StartHeartbeat ();
        }
    }
}
