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
using OpenSim.Framework.Console;
using OpenSim.Framework;

namespace OpenSim.ApplicationPlugins.RegionLoaderPlugin
{
    public class RegionLoaderDataBaseSystem : IRegionLoader
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IOpenSimBase m_openSim;
        private IRegionCreator m_creator;
        private IConfigSource m_configSource;

        public void Initialise(IConfigSource configSource, IRegionCreator creator, IOpenSimBase openSim)
        {
            m_configSource = configSource; ;
            m_creator = creator;
            m_openSim = openSim;
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
            FindOldRegionFiles();

            RegionInfo[] infos = Aurora.DataManager.DataManager.IRegionInfoConnector.GetRegionInfos();
            if (infos.Length == 0)
            {
                RegionManager manager = new RegionManager(true);
                System.Windows.Forms.Application.Run(manager);
                return LoadRegions();
            }
            else
                return infos;
        }

        public void AddRegion()
        {
            RegionManager manager = new RegionManager(true);
            manager.OnNewRegion += new RegionManager.NewRegion(manager_OnNewRegion);
            System.Windows.Forms.Application.Run(manager);
        }

        private void manager_OnNewRegion(RegionInfo info)
        {
            IScene scene;
            m_log.Debug("[LOADREGIONS]: Creating Region: " + info.RegionName + ")");
            m_log.Warn("ERROR: CANNOT LOAD REGION, REPORT THIS");
            //m_openSim.SceneManager.CreateRegion(info, true, out scene);
        }

        private void FindOldRegionFiles()
        {
            try
            {
                List<RegionInfo> RegionsToConvert = new List<RegionInfo>();
                string regionConfigPath = Path.Combine(Util.configDir(), "Regions");

                try
                {
                    IConfig startupConfig = (IConfig)m_configSource.Configs["Startup"];
                    regionConfigPath = startupConfig.GetString("regionload_regionsdir", regionConfigPath).Trim();
                }
                catch (Exception)
                {
                    // No INI setting recorded.
                }
                if (!Directory.Exists(regionConfigPath))
                    return;
                //
                string[] configFiles = Directory.GetFiles(regionConfigPath, "*.xml");
                string[] iniFiles = Directory.GetFiles(regionConfigPath, "*.ini");

                int i = 0;
                if (iniFiles.Length != 0)
                {
                    foreach (string file in iniFiles)
                    {
                        IConfigSource source = new IniConfigSource(file);

                        foreach (IConfig config in source.Configs)
                        {
                            RegionInfo regionInfo = new RegionInfo("REGION CONFIG #" + (i + 1), file, false, m_configSource, config.Name);
                            RegionsToConvert.Add(regionInfo);
                            i++;
                        }
                    }
                }
                if (configFiles.Length != 0)
                {
                    foreach (string file in configFiles)
                    {
                        RegionInfo regionInfo = new RegionInfo("REGION CONFIG #" + (i + 1), file, false, m_configSource);
                        RegionsToConvert.Add(regionInfo);
                        i++;
                    }
                }
                foreach (string file in iniFiles)
                {
                }
                foreach (RegionInfo info in RegionsToConvert)
                {
                    Aurora.DataManager.DataManager.IRegionInfoConnector.UpdateRegionInfo(info, false);
                }
                bool foundAll = false;
                foreach (RegionInfo info in RegionsToConvert)
                {
                    if (Aurora.DataManager.DataManager.IRegionInfoConnector.GetRegionInfo(info.RegionID) == null)
                        foundAll = false;
                }
                //Something went really wrong here... so lets not destroy anything
                if(!foundAll)
                    Directory.Delete(regionConfigPath, true);
                MessageBox.Show("All region .ini and .xml files have been successfully converted to the new region loader style. The regions folder has been cleared.");
            }
            catch
            {
            }
        }
    }
}
