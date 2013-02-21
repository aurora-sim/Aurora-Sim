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
using System.Threading;
using System.Windows.Forms;
using Nini.Config;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Management;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.ApplicationPlugins.RegionLoaderPlugin
{
    public class RegionLoaderDataBaseSystem : IRegionLoader
    {
        private ISimulationBase m_openSim;
        private IConfigSource m_configSource;
        private bool m_enabled = false;
        private bool m_default = false;
        private bool m_noGUI = false;

        public bool Enabled
        {
            get { return m_enabled; }
        }

        public bool Default
        {
            get { return m_default; }
        }

        public void Initialise(IConfigSource configSource, ISimulationBase openSim)
        {
            m_configSource = configSource;
            m_openSim = openSim;
            
            IConfig config = configSource.Configs["RegionStartup"];
            if (config != null)
            {
                m_enabled = config.GetBoolean(GetType().Name + "_Enabled", m_enabled);
                if (!m_enabled)
                    return;
                if (MainConsole.Instance != null)
                    MainConsole.Instance.Commands.AddCommand("open region manager", "open region manager", "Opens the region manager", OpenRegionManager);
                m_default = config.GetString("Default") == GetType().Name;
            }
            IConfig startupconfig = configSource.Configs["Startup"];
            if (startupconfig != null)
                m_noGUI = startupconfig.GetBoolean("NoGUI", false);

            m_openSim.ApplicationRegistry.StackModuleInterface<IRegionLoader>(this);
        }

        public void Close()
        {
        }

        public string Name
        {
            get
            {
                return "Database Plugin";
            }
        }

        public RegionInfo LoadRegion()
        {
            IRegionInfoConnector conn = DataManager.RequestPlugin<IRegionInfoConnector>();
            if (conn == null)
                return null;
            RegionInfo[] infos = conn.GetRegionInfos(true);
            return infos.Length == 0 ? null : infos[0];
        }

        public void CreateRegion()
        {
            RegionManager.StartSynchronously(true, RegionManagerPage.CreateRegion,
                m_openSim.ConfigSource, m_openSim.ApplicationRegistry.RequestModuleInterface<IRegionManagement>());
        }

        protected void OpenRegionManager(string[] cmdparams)
        {
            RegionManager.StartAsynchronously(false, RegionManagerPage.ViewRegions,
                m_openSim.ConfigSource, m_openSim.ApplicationRegistry.RequestModuleInterface<IRegionManagement>());
        }

        public void DeleteRegion(RegionInfo regionInfo)
        {
            IRegionInfoConnector connector = DataManager.RequestPlugin<IRegionInfoConnector>();
            if (connector != null)
            {
                connector.Delete(regionInfo);
            }
        }

        public bool FailedToStartRegions(string reason)
        {
            try
            {
                //Open the region manager for them
                MessageBox.Show (reason, "Startup failed, regions did not validate!");
                RegionManager.StartSynchronously(false, RegionManagerPage.ViewRegions,
                    m_openSim.ConfigSource, m_openSim.ApplicationRegistry.RequestModuleInterface<IRegionManagement>());
            }
            catch
            {
                MainConsole.Instance.Output(string.Format("Startup failed, regions did not validate - {0}!", reason));
            }
            return true;
        }

        public void Dispose()
        {
        }

        public void UpdateRegionInfo(string oldName, RegionInfo regionInfo)
        {
            IRegionInfoConnector connector = DataManager.RequestPlugin<IRegionInfoConnector>();
            if (connector != null)
            {
                //Make sure we have this region in the database
                if (connector.GetRegionInfo(oldName) == null)
                    return;
                RegionInfo copy = new RegionInfo();
                //Make an exact copy
                copy.UnpackRegionInfoData(regionInfo.PackRegionInfoData(true));

                //Fix the name of the region so we can delete the old one
                copy.RegionName = oldName;
                DeleteRegion(copy);
                //Now add the new one
                connector.UpdateRegionInfo(regionInfo);
            }
        }
    }
}
