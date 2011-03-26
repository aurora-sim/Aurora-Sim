/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Aurora.Modules.RegionLoader;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using Aurora.DataManager;
using Aurora.Framework;

namespace OpenSim.ApplicationPlugins.RegionLoaderPlugin
{
    public class RegionLoaderDataBaseSystem : IRegionLoader
    {
        private ISimulationBase m_openSim;
        private IConfigSource m_configSource;
        private bool m_enabled = false;
        private bool m_default = false;

        public bool Enabled
        {
            get { return m_enabled; }
        }

        public void Initialise(IConfigSource configSource, ISimulationBase openSim)
        {
            m_configSource = configSource;
            m_openSim = openSim;
            
            IConfig config = configSource.Configs["RegionStartup"];
            if (config != null)
            {
                m_enabled = config.GetBoolean(Name + "_Enabled", m_enabled);
                if (!m_enabled)
                    return;
                MainConsole.Instance.Commands.AddCommand("base", false, "open region manager", "open region manager", "Opens the region manager", OpenRegionManager);
                m_default = config.GetString("Default") == Name;

                //Add the console command if it is the default
                if (m_default)
                    MainConsole.Instance.Commands.AddCommand("region", false, "create region", "create region", "Create a new region.", AddRegion);
            }

            m_openSim.ApplicationRegistry.StackModuleInterface<IRegionLoader>(this);
        }

        public void Close()
        {
        }

        public string Name
        {
            get
            {
                return "RegionLoaderDataBaseSystem";
            }
        }

        public RegionInfo[] LoadRegions()
        {
            //Grab old region files
            if(m_default)
                FindOldRegionFiles();

            IRegionInfoConnector conn = DataManager.RequestPlugin<IRegionInfoConnector>();
            if (conn == null)
                return null;
            RegionInfo[] infos = conn.GetRegionInfos(true);
            if (infos.Length == 0 && m_default)
            {
                //Load up the GUI to make a new region
                RegionManager manager = new RegionManager(true, false, m_openSim);
                System.Windows.Forms.Application.Run(manager);
                return LoadRegions();
            }
            else if (infos.Length == 0)
                return null;
            else
                return infos;
        }

        /// <summary>
        /// Creates a new region based on the parameters specified.   This will ask the user questions on the console
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmd">0,1,region name, region XML file</param>
        public void AddRegion(string module, string[] cmd)
        {
            RegionManager manager = new RegionManager(false, true, m_openSim);
            System.Windows.Forms.Application.Run(manager);
        }

        protected void OpenRegionManager(string module, string[] cmdparams)
        {
            System.Threading.Thread thread = new Thread(StartRegionManagerThread);
            thread.Start();
        }

        protected void StartRegionManagerThread()
        {
            RegionManager manager = new RegionManager(false, false, m_openSim);
            System.Windows.Forms.Application.Run(manager);
        }

        private void FindOldRegionFiles()
        {
            try
            {
                //Load the file loader and set it up and make sure that we pull any regions from it
                RegionLoaderFileSystem system = new RegionLoaderFileSystem();
                system.Initialise(m_configSource, m_openSim);
                RegionInfo[] regionsToConvert = system.LoadRegions();
                if (regionsToConvert == null)
                    return;

                //Now load all the regions into the database
                IRegionInfoConnector conn = DataManager.RequestPlugin<IRegionInfoConnector>();
                foreach (RegionInfo info in regionsToConvert)
                {
                    conn.UpdateRegionInfo(info);
                }

                //Make sure all the regions got saved
                bool foundAll = true;
                foreach (RegionInfo info in regionsToConvert)
                {
                    if (conn.GetRegionInfo(info.RegionID) == null)
                        foundAll = false;
                }
                //Something went really wrong here... so lets not destroy anything
                if (foundAll && regionsToConvert.Length != 0)
                {
                    MessageBox.Show("All region .ini and .xml files have been successfully converted to the new region loader style.");
                }
            }
            catch
            {
            }
        }

        public void DeleteRegion(RegionInfo regionInfo)
        {
            IRegionInfoConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IRegionInfoConnector>();
            if (connector != null)
            {
                connector.Delete(regionInfo);
            }
        }

        public bool FailedToStartRegions(string reason)
        {
            //Open the region manager for them
            MessageBox.Show("Startup failed, regions did not validate!", reason);
            StartRegionManagerThread();
            return true;
        }

        public void Dispose()
        {
        }

        public void UpdateRegionInfo(string oldName, RegionInfo regionInfo)
        {
            IRegionInfoConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IRegionInfoConnector>();
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
