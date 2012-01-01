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
using System.Linq;
using System.Timers;
using Nini.Config;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.Modules.VersionControl
{
    public class VersionControlModule : ISharedRegionModule
    {
        private bool m_Enabled;

        //Auto OAR configs
        private bool m_autoOAREnabled;
        private float m_autoOARTime = 1; //In days
        private Timer m_autoOARTimer;

        private int nextVersion = 1;

        #region ISharedRegionModule Members

        public string Name
        {
            get { return "VersionControlModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
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

        public void AddRegion(IScene scene)
        {
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            if (!m_Enabled)
                return;

            if (m_autoOAREnabled)
            {
                m_autoOARTimer = new Timer(m_autoOARTime*1000*60*60*24); //Time in days
                m_autoOARTimer.Elapsed += SaveOAR;
                m_autoOARTimer.Enabled = true;
            }
            //scene.AddCommand(this, "save version", "save version <description>", "Saves the current region as the next incremented version in the version control module.", SaveVersion);
        }

        #endregion

        private void SaveOAR(object sender, ElapsedEventArgs e)
        {
            SaveNext(MainConsole.Instance.ConsoleScene, "AutomaticBackup");
        }

        protected void SaveVersion(string module, string[] cmdparams)
        {
            if (cmdparams.Length < 3)
                return;

            cmdparams[0] = "";
            cmdparams[1] = "";
#if (!ISWIN)
            string Desc = "";
            foreach (string param in cmdparams)
            {
                Desc += param;
            }
#else
            string Desc = cmdparams.Aggregate("", (current, param) => current + param);
#endif

            SaveNext(MainConsole.Instance.ConsoleScene, Desc);
        }

        public void SaveNext(IScene nextScene, string Description)
        {
            IScene scene = MainConsole.Instance.ConsoleScene; //Switch back later
            MainConsole.Instance.RunCommand("change region " + nextScene.RegionInfo.RegionName);
            string tag = "";
            tag += "Region." + nextScene.RegionInfo.RegionName;
            tag += ".Desc." + Description;
            tag += ".Version." + nextVersion;
            tag += ".Date." + DateTime.Now.Year + "." + DateTime.Now.Month + "." + DateTime.Now.Day + "." +
                   DateTime.Now.Hour;
            nextVersion++;
            MainConsole.Instance.RunCommand("save oar " + tag + ".vc.oar");
            //Change back
            MainConsole.Instance.RunCommand("change region " + scene.RegionInfo.RegionName);
        }
    }
}