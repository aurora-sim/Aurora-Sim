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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using Nini.Config;
using Nini.Ini;
using Aurora.Framework;

namespace OpenSim.ApplicationPlugins.RegionLoaderPlugin
{
    public class RegionLoaderWebServer : IRegionLoader
    {
        private IConfigSource m_configSource;
        private bool m_enabled;

        #region IRegionLoader Members

        public string Name
        {
            get { return "Web Server Plugin"; }
        }

        public bool Enabled
        {
            get { return m_enabled; }
        }

        public bool Default
        {
            get { return false; }
        }

        public void Initialise(IConfigSource configSource, ISimulationBase openSim)
        {
            m_configSource = configSource;
            IConfig config = configSource.Configs["RegionStartup"];
            if (config != null)
                m_enabled = config.GetBoolean(GetType().Name + "_Enabled", m_enabled);
            if (m_enabled)
                openSim.ApplicationRegistry.StackModuleInterface<IRegionLoader>(this);
        }

        public void AddRegion(string[] cmd)
        {
        }

        public RegionInfo[] LoadRegions()
        {
            IConfig RegionStartupConfig = m_configSource.Configs["RegionStartup"];
            if (RegionStartupConfig != null)
            {
                string url = RegionStartupConfig.GetString("WebServerURL", String.Empty).Trim();
                if (url == String.Empty)
                {
                    //MainConsole.Instance.Error("[WEBLOADER]: Unable to load webserver URL - URL was empty.");
                    return null;
                }
                HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(url);
                webRequest.Timeout = 30000; //30 Second Timeout

                MainConsole.Instance.Debug("[WEBLOADER]: Sending Download Request...");
                HttpWebResponse webResponse = (HttpWebResponse) webRequest.GetResponse();

                MainConsole.Instance.Info("[WEBLOADER]: Downloading Region Information From Remote Server...");
                StreamReader reader = new StreamReader(webResponse.GetResponseStream());

                MainConsole.Instance.Debug("[WEBLOADER]: Done downloading region information from server.");

                List<RegionInfo> regionInfos = new List<RegionInfo>();

                IConfigSource source =
                    new IniConfigSource(new IniDocument(reader.BaseStream, IniFileType.AuroraStyle));

                int i = 0;
                foreach (IConfig config in source.Configs)
                {
                    RegionInfo region = new RegionInfo();
                    //Use this to load the config from the file
                    RegionLoaderFileSystem system = new RegionLoaderFileSystem();
                    system.LoadRegionFromFile("REGION CONFIG #" + (i + 1), "", false, m_configSource, config.Name);
                    regionInfos.Add(region);
                    i++;
                }
                return regionInfos.ToArray();
            }
            return null;
        }

        public void UpdateRegionInfo(string oldName, RegionInfo regionInfo)
        {
            //Can't add regions to remote locations
        }

        public void DeleteRegion(RegionInfo region)
        {
        }

        public bool FailedToStartRegions(string reason)
        {
            //Can't deal with it
            return false;
        }

        #endregion

        public void Dispose()
        {
        }
    }
}