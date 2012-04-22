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

using System.Linq;
using System;
using System.Reflection;
using System.Xml;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Nini.Config;
using Aurora.Framework;
using OpenMetaverse.StructuredData;

namespace OpenSim.CoreApplicationPlugins
{
    public class GitHubUpdaterPlugin : IApplicationPlugin
    {

        private const string m_urlToCheckForUpdates = "https://api.github.com/repos/aurora-sim/aurora-sim/downloads";
        private const string m_regexRelease = "^Aurora(\\d+|\\d+\\.\\d+|\\d+\\.\\d+\\.\\d+|\\d+\\.\\d+\\.\\d+\\.\\d+)Release\\.zip$";

        public string Name
        {
            get { return "GitHubUpdater"; }
        }

        private void ErrorMsg(string msg){
            MainConsole.Instance.Error("[UpdaterPlugin:" + Name + "] " + msg);
        }
        private void InfoMsg(string msg)
        {
            MainConsole.Instance.Info("[UpdaterPlugin:" + Name + "] " + msg);
        }
        private void TraceMsg(string msg)
        {
            MainConsole.Instance.Trace("[UpdaterPlugin:" + Name + "] " + msg);
        }

        public void PreStartup(ISimulationBase simBase)
        {
        }

        public void Initialize(ISimulationBase openSim)
        {
            try
            {
                //Check whether this is enabled
                IConfig updateConfig = openSim.ConfigSource.Configs["Update"];
                if (updateConfig == null || updateConfig.GetString("Module", string.Empty) != Name || !updateConfig.GetBoolean("Enabled", false))
                {
                    return;
                }
                
                InfoMsg("Checking for updates...");

                string WebSite = m_urlToCheckForUpdates;

                // Call the github API
                OSD data = OSDParser.DeserializeJson(Utilities.ReadExternalWebsite(WebSite));
                if (data.Type != OSDType.Array)
                {
                    ErrorMsg("Failed to get downloads from github API.");
                    return;
                }
                OSDArray JsonData = (OSDArray)data;

                if (JsonData.Count == 0)
                {
                    ErrorMsg("No downloads found.");
                    return;
                }
                else
                {
                    TraceMsg(JsonData.Count + " downloads found, parsing.");
                }

                SortedDictionary<Version, OSDMap> releases = new SortedDictionary<Version, OSDMap>();
                foreach (OSD map in JsonData)
                {
                    if (map.Type == OSDType.Map)
                    {
                        OSDMap download = (OSDMap)map;
                        if (
                            download.ContainsKey("download_count") &&
                            download.ContainsKey("created_at") &&
                            download.ContainsKey("description") &&
                            download.ContainsKey("url") &&
                            download.ContainsKey("html_url") &&
                            download.ContainsKey("size") &&
                            download.ContainsKey("name") &&
                            download.ContainsKey("content_type") &&
                            download.ContainsKey("id") &&
                            download["content_type"].AsString() == ".zip" &&
                            Regex.IsMatch(download["name"].ToString(), m_regexRelease)
                        )
                        {
                            Match matches = Regex.Match(download["name"].ToString(), m_regexRelease);
                            releases[new Version(matches.Groups[1].ToString())] = download;
                        }
                    }
                }

                if (releases.Count < 1)
                {
                    ErrorMsg("No releases found");
                    return;
                }

                KeyValuePair<Version, OSDMap> latest = releases.OrderByDescending(kvp => kvp.Key).First();
                if (latest.Key <= new Version(VersionInfo.VERSION_NUMBER))
                {
                    InfoMsg("You are already using a newer version, no updated is necessary.");
                    return;
                }
                DialogResult result = MessageBox.Show("A new version of Aurora has been released, version " + latest.Key.ToString() + " (" + latest.Value["description"] + ")" + ", do you want to download the update?", "Aurora Update", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    Utilities.DownloadFile(latest.Value["html_url"], "Updates" + System.IO.Path.DirectorySeparatorChar + "AuroraVersion" + latest.Key.ToString() + ".zip");
                    MessageBox.Show(string.Format("Downloaded to {0}, exiting for user to upgrade.", "Updates" + System.IO.Path.DirectorySeparatorChar + "AuroraVersion" + latest.Key.ToString() + ".zip"), "Aurora Update");
                    Environment.Exit(0);
                }
                updateConfig.Set("LatestRelease", latest.Key.ToString());
                updateConfig.ConfigSource.Save();
            }
            catch
            {
            }
        }

        private bool Compare (string givenVersion, string CurrentVersion)
        {
            string[] given = givenVersion.Split ('.');
            string[] current = CurrentVersion.Split ('.');
            for (int i = 0; i < (int)Math.Max (given.Length, current.Length); i++)
            {
                if (i == given.Length || i == current.Length)
                    break;
                if (int.Parse (given[i]) > int.Parse (current[i]))
                    return true;
            }
            return false;
        }

        public void ReloadConfiguration (IConfigSource config)
        {
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
        }

        public void PostStart()
        {
        }

        public void Dispose()
        {
        }

        public void Close()
        {
        }
    }
}
