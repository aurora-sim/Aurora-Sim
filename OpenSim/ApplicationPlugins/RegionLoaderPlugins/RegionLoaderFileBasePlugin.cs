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
using System.Reflection;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using Aurora.Modules.RegionLoader;
using OpenSim.Framework.Console;

namespace OpenSim.ApplicationPlugins.RegionLoaderPlugin
{
    public class RegionLoaderFileSystem : IRegionLoader
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IConfigSource m_configSource;
        private bool m_default = true;
        private OpenSimBase m_openSim;
        private string m_regionConfigPath = Path.Combine(Util.configDir(), "Regions");

        public void Initialise(IConfigSource configSource, IRegionCreator creator, IOpenSimBase openSim)
        {
            m_configSource = configSource;
            m_openSim = (OpenSimBase)openSim;
            IConfig config = configSource.Configs["RegionStartup"];
            if (config != null)
            {
                m_default = config.GetString("Default", Name) == Name; //.ini loader defaults
                m_regionConfigPath = config.GetString("RegionsDirectory", m_regionConfigPath).Trim();
            }
        }

        public RegionInfo[] LoadRegions()
        {
            if (!Directory.Exists(m_regionConfigPath))
                Directory.CreateDirectory(m_regionConfigPath);

            string[] configFiles = Directory.GetFiles(m_regionConfigPath, "*.xml");
            string[] iniFiles = Directory.GetFiles(m_regionConfigPath, "*.ini");

            if (configFiles.Length == 0 && iniFiles.Length == 0)
            {
                if (!m_default)
                    return null;
                new RegionInfo("DEFAULT REGION CONFIG", Path.Combine(m_regionConfigPath, "Regions.ini"), false, m_configSource, "");
                iniFiles = Directory.GetFiles(m_regionConfigPath, "*.ini");
            }

            List<RegionInfo> regionInfos = new List<RegionInfo>();

            int i = 0;
            foreach (string file in iniFiles)
            {
                IConfigSource source = new IniConfigSource(file, Nini.Ini.IniFileType.AuroraStyle);

                foreach (IConfig config in source.Configs)
                {
                    RegionInfo regionInfo = new RegionInfo("REGION CONFIG #" + (i + 1), file, false, m_configSource, config.Name);
                    regionInfos.Add(regionInfo);
                    i++;
                }
            }

            foreach (string file in configFiles)
            {
                RegionInfo regionInfo = new RegionInfo("REGION CONFIG #" + (i + 1), file, false, m_configSource, string.Empty);
                regionInfos.Add(regionInfo);
                i++;
            }

            return regionInfos.ToArray();
        }

        public void AddRegion(IOpenSimBase baseOS, string[] cmd)
        {
            if (!m_default)
                return;
            if (cmd.Length < 4)
            {
                MainConsole.Instance.Output("Usage: create region <region name> <region_file.ini>");
                return;
            }
            else if (cmd[3].EndsWith(".ini"))
            {
                string regionsDir = m_configSource.Configs["Startup"].GetString("regionload_regionsdir", "Regions").Trim();
                string regionFile = String.Format("{0}/{1}", regionsDir, cmd[3]);
                // Allow absolute and relative specifiers
                if (cmd[3].StartsWith("/") || cmd[3].StartsWith("\\") || cmd[3].StartsWith(".."))
                    regionFile = cmd[3];

                IScene scene;
                m_log.Debug("[LOADREGIONS]: Creating Region: " + cmd[2]);
                m_openSim.SceneManager.CreateRegion(new RegionInfo(cmd[2], regionFile, false, m_configSource, cmd[2]), true, out scene);
            }
            else
            {
                MainConsole.Instance.Output("Usage: create region <region name> <region_file.ini>");
                return;
            }
        }

        public string Name
        {
            get { return "RegionLoaderFileSystem"; }
        }

        public void Dispose()
        {
        }
    }
}