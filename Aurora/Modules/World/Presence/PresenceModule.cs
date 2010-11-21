using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using OpenSim.Framework;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.Modules
{
    /// <summary>
    /// This module fixes issues with avatars not being logged out if they crash or the region crashing.
    /// All this module does is update all presences in the region in the PresenceService.
    /// </summary>
    public class PresenceModule : INonSharedRegionModule
    {
        #region Declares

        private System.Timers.Timer timer = null;
        private Scene m_scene;

        #endregion

        #region ISharedRegionModule

        public void Initialise(Nini.Config.IConfigSource source)
        {
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
            if (timer == null)
            {
                timer = new Timer(1000 * 60 * 55); //Every 55 minutes to reload all avatars
                timer.Elapsed += new ElapsedEventHandler(UpdateAgents);
                timer.Start();
                m_scene = scene;
            }
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public string Name
        {
            get { return "PresenceModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }
        #endregion

        #region Update Timer

        private void UpdateAgents(object sender, ElapsedEventArgs e)
        {
            foreach (ScenePresence SP in m_scene.ScenePresences)
            {
                //Give them the new LastSeen value in the database
                m_scene.PresenceService.ReportAgent(SP.ControllingClient.SessionId, m_scene.RegionInfo.RegionID);
            }
        }

        #endregion
    }
}
