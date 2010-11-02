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
    public class VersionControlModule : ISharedRegionModule
    {
        private bool m_Enabled = false;

        //Auto OAR configs
        private Timer m_autoOARTimer = null;
        private bool m_autoOAREnabled = false;
        private float m_autoOARTime = 1; //In days

        private int nextVersion = 1;

        public string Name
        {
            get { return "VersionControlModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(Nini.Config.IConfigSource source)
        {
            if (source.Configs["VersionControl"] == null)
                return;
            IConfig config = source.Configs["VersionControl"];
            m_Enabled = config.GetBoolean("Enabled", false);

            //Auto OAR config
            m_autoOAREnabled = config.GetBoolean("AutoVersionEnabled", false);
            m_autoOARTime = config.GetFloat("AutoVersionTime", 1);
        }

        public void Close()
        {
        }

        public void PostInitialise()
        {
        }

        public void AddRegion(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;

            if (m_autoOAREnabled)
            {
                m_autoOARTimer = new Timer(m_autoOARTime * (TimeSpan.TicksPerDay / TimeSpan.TicksPerMillisecond));//Time in days
                m_autoOARTimer.Elapsed += SaveOAR;
                m_autoOARTimer.Enabled = true;
            }
            scene.AddCommand(this, "save version", "save version <description>", "Saves the current region as the next incremented version in the version control module.", SaveVersion);
            
        }

        private void SaveOAR(object sender, ElapsedEventArgs e)
        {
            SaveNext((Scene)MainConsole.Instance.ConsoleScene, "AutomaticBackup");
        }

        protected void SaveVersion(string module, string[] cmdparams)
        {
            if (cmdparams.Length < 3)
            {
                return;
            }

            string Desc = "";
            cmdparams[0] ="";
            cmdparams[1] ="";
            foreach(string param in cmdparams)
            {
                Desc += param;
            }

            SaveNext((Scene)MainConsole.Instance.ConsoleScene, Desc);
        }

        public void SaveNext(Scene nextScene, string Description)
        {
            Scene scene = (Scene)MainConsole.Instance.ConsoleScene; //Switch back later
            MainConsole.Instance.RunCommand("change region " + nextScene.RegionInfo.RegionName);
            string tag = "";
            tag += "Region."+nextScene.RegionInfo.RegionName;
            tag += ".Desc." + Description;
            tag += ".Version." + nextVersion;
            tag += ".Date." + DateTime.Now.Ticks;
            nextVersion++;
            MainConsole.Instance.RunCommand("save oar " + tag + ".oar.vc");
            //Change back
            MainConsole.Instance.RunCommand("change region " + scene.RegionInfo.RegionName);
        }
    }
}
