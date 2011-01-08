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
using log4net;
using Nini.Config;
using OpenSim.Framework;
using Aurora.Modules.RegionLoader;
using OpenSim.Framework.Console;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.ApplicationPlugins.RegionLoaderPlugin
{
    public class RegionLoaderFileSystem : IRegionLoader
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IConfigSource m_configSource;
        private bool m_default = true;
        private ISimulationBase m_openSim;
        private string m_regionConfigPath = Path.Combine(Util.configDir(), "Regions");

        public bool Default
        {
            get { return m_default; }
        }

        public void Initialise(IConfigSource configSource, IRegionCreator creator, ISimulationBase openSim)
        {
            m_configSource = configSource;
            m_openSim = openSim;
            IConfig config = configSource.Configs["RegionStartup"];
            if (config != null)
            {
                m_default = config.GetString("Default", Name) == Name; //.ini loader defaults
                m_regionConfigPath = config.GetString("RegionsDirectory", m_regionConfigPath).Trim();
            }
            m_openSim.ApplicationRegistry.StackModuleInterface<IRegionLoader>(this);
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
                LoadRegionFromFile("DEFAULT REGION CONFIG", Path.Combine(m_regionConfigPath, "Regions.ini"), false, m_configSource, "");
                iniFiles = Directory.GetFiles(m_regionConfigPath, "*.ini");
            }

            List<RegionInfo> regionInfos = new List<RegionInfo>();

            int i = 0;
            foreach (string file in iniFiles)
            {
                IConfigSource source = new IniConfigSource(file, Nini.Ini.IniFileType.AuroraStyle);

                foreach (IConfig config in source.Configs)
                {
                    RegionInfo regionInfo = LoadRegionFromFile("REGION CONFIG #" + (i + 1), file, false, m_configSource, config.Name);
                    regionInfos.Add(regionInfo);
                    i++;
                }
            }

            foreach (string file in configFiles)
            {
                RegionInfo regionInfo = LoadRegionFromFile("REGION CONFIG #" + (i + 1), file, false, m_configSource, string.Empty);
                regionInfos.Add(regionInfo);
                i++;
            }

            return regionInfos.ToArray();
        }

        // File based loading
        //
        public RegionInfo LoadRegionFromFile(string description, string filename, bool skipConsoleConfig, IConfigSource configSource, string configName)
        {
            // m_configSource = configSource;
            RegionInfo region = new RegionInfo();
            if (filename.ToLower().EndsWith(".ini"))
            {
                if (!File.Exists(filename)) // New region config request
                {
                    IniConfigSource newFile = new IniConfigSource();

                    region.RegionFile = filename;

                    ReadNiniConfig(region, newFile, configName);
                    newFile.Save(filename);

                    return region;
                }

                IniConfigSource m_source = new IniConfigSource(filename, Nini.Ini.IniFileType.AuroraStyle);

                bool saveFile = false;
                if (m_source.Configs[configName] == null)
                    saveFile = true;

                region.RegionFile = filename;

                bool update = ReadNiniConfig(region, m_source, configName);

                if (configName != String.Empty && (saveFile || update))
                    m_source.Save(filename);

                return region;
            }

            try
            {
                // This will throw if it's not legal Nini XML format
                // and thereby toss it to the legacy loader
                //
                IConfigSource xmlsource = new XmlConfigSource(filename);

                ReadNiniConfig(region, xmlsource, configName);

                region.RegionFile = filename;

                return region;
            }
            catch (Exception)
            {
            }
            return null;
        }


        //Returns true if the source should be updated. Returns false if it does not.
        public bool ReadNiniConfig(RegionInfo region, IConfigSource source, string name)
        {
            //            bool creatingNew = false;

            if (name == String.Empty || source.Configs.Count == 0)
            {
                MainConsole.Instance.Output("=====================================\n");
                MainConsole.Instance.Output("We are now going to ask a couple of questions about your region.\n");
                MainConsole.Instance.Output("You can press 'enter' without typing anything to use the default\n");
                MainConsole.Instance.Output("the default is displayed between [ ] brackets.\n");
                MainConsole.Instance.Output("=====================================\n");
            }

            bool NeedsUpdate = false;
            if (name == String.Empty)
                name = MainConsole.Instance.CmdPrompt("New region name", name);
            if (name == String.Empty)
                throw new Exception("Cannot interactively create region with no name");

            if (source.Configs.Count == 0)
            {
                source.AddConfig(name);

                //                creatingNew = true;
                NeedsUpdate = true;
            }

            if (source.Configs[name] == null)
            {
                source.AddConfig(name);
                NeedsUpdate = true;
                //                creatingNew = true;
            }

            IConfig config = source.Configs[name];

            // UUID
            //
            string regionUUID = config.GetString("RegionUUID", string.Empty);

            if (regionUUID == String.Empty)
            {
                NeedsUpdate = true;
                UUID newID = UUID.Random();

                regionUUID = MainConsole.Instance.CmdPrompt("Region UUID for region " + name, newID.ToString());
                config.Set("RegionUUID", regionUUID);
            }

            region.RegionID = new UUID(regionUUID);

            region.RegionName = name;
            string location = config.GetString("Location", String.Empty);

            if (location == String.Empty)
            {
                NeedsUpdate = true;
                location = MainConsole.Instance.CmdPrompt("Region Location for region " + name, "1000,1000");
                config.Set("Location", location);
            }

            string[] locationElements = location.Split(new char[] { ',' });

            region.RegionLocX = Convert.ToInt32(locationElements[0]) * Constants.RegionSize;
            region.RegionLocY = Convert.ToInt32(locationElements[1]) * Constants.RegionSize;

            // Internal IP
            IPAddress address;

            if (config.Contains("InternalAddress"))
            {
                address = IPAddress.Parse(config.GetString("InternalAddress", String.Empty));
            }
            else
            {
                NeedsUpdate = true;
                address = IPAddress.Parse(MainConsole.Instance.CmdPrompt("Internal IP address for region " + name, "0.0.0.0"));
                config.Set("InternalAddress", address.ToString());
            }

            int port;

            if (config.Contains("InternalPort"))
            {
                port = config.GetInt("InternalPort", 9000);
            }
            else
            {
                NeedsUpdate = true;
                port = Convert.ToInt32(MainConsole.Instance.CmdPrompt("Internal port for region " + name, "9000"));
                config.Set("InternalPort", port);
            }

            region.InternalEndPoint = new IPEndPoint(address, port);

            if (config.Contains("AllowAlternatePorts"))
            {
                region.m_allow_alternate_ports = config.GetBoolean("AllowAlternatePorts", true);
            }
            else
            {
                NeedsUpdate = true;
                region.m_allow_alternate_ports = Convert.ToBoolean(MainConsole.Instance.CmdPrompt("Allow alternate ports", "False"));

                config.Set("AllowAlternatePorts", region.m_allow_alternate_ports.ToString());
            }

            // External IP
            //
            string externalName;

            if (config.Contains("ExternalHostName"))
            {
                externalName = config.GetString("ExternalHostName", "SYSTEMIP");
            }
            else
            {
                NeedsUpdate = true;
                externalName = MainConsole.Instance.CmdPrompt("External host name for region " + name, "SYSTEMIP");
                config.Set("ExternalHostName", externalName);
            }

            if (externalName == "SYSTEMIP")
            {
                region.ExternalHostName = Util.GetLocalHost().ToString();
                m_log.InfoFormat(
                    "[REGIONINFO]: Resolving SYSTEMIP to {0} for external hostname of region {1}",
                    region.ExternalHostName, name);
            }
            else
            {
                region.ExternalHostName = externalName;
            }

            region.RegionType = config.GetString("RegionType", region.RegionType);

            if (region.RegionType == String.Empty)
            {
                NeedsUpdate = true;
                region.RegionType = MainConsole.Instance.CmdPrompt("Region Type for region " + name, "Mainland");
                config.Set("RegionType", region.RegionType);
            }

            region.AllowPhysicalPrims = config.GetBoolean("AllowPhysicalPrims", region.AllowPhysicalPrims);

            region.AllowScriptCrossing = config.GetBoolean("AllowScriptCrossing", region.AllowScriptCrossing);

            region.TrustBinariesFromForeignSims = config.GetBoolean("TrustBinariesFromForeignSims", region.TrustBinariesFromForeignSims);

            region.SeeIntoThisSimFromNeighbor = config.GetBoolean("SeeIntoThisSimFromNeighbor", region.SeeIntoThisSimFromNeighbor);

            region.ObjectCapacity = config.GetInt("MaxPrims", region.ObjectCapacity);


            // Multi-tenancy
            //
            region.ScopeID = new UUID(config.GetString("ScopeID", region.ScopeID.ToString()));

            //Do this last so that we can save the password immediately if it doesn't exist
            UUID password = region.Password; //Save the pass as this TryParse will wipe it out
            if (!UUID.TryParse(config.GetString("NeighborPassword", ""), out region.Password))
            {
                region.Password = password;
                config.Set("NeighborPassword", password);
                region.WriteNiniConfig(source);
            }

            return NeedsUpdate;
        }

        public void AddRegion(ISimulationBase baseOS, string[] cmd)
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
                string regionFile = String.Format("{0}/{1}", m_regionConfigPath, cmd[3]);
                // Allow absolute and relative specifiers
                if (cmd[3].StartsWith("/") || cmd[3].StartsWith("\\") || cmd[3].StartsWith(".."))
                    regionFile = cmd[3];

                IScene scene;
                m_log.Debug("[LOADREGIONS]: Creating Region: " + cmd[2]);
                SceneManager manager = m_openSim.ApplicationRegistry.RequestModuleInterface<SceneManager>();
                manager.CreateRegion(LoadRegionFromFile(cmd[2], regionFile, false, m_configSource, cmd[2]), true, out scene);
            }
            else
            {
                MainConsole.Instance.Output("Usage: create region <region name> <region_file.ini>");
                return;
            }
        }

        /// <summary>
        /// Save the changes to the RegionInfo to the file that it came from in the Regions/ directory
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="regionInfo"></param>
        public void UpdateRegionInfo(string oldName, RegionInfo regionInfo)
        {
            string regionConfigPath = Path.Combine(Util.configDir(), "Regions");

            if (oldName == "")
                oldName = regionInfo.RegionName;
            try
            {
                IConfig config = m_configSource.Configs["RegionStartup"];
                if (config != null)
                {
                    regionConfigPath = config.GetString("RegionsDirectory", regionConfigPath).Trim();
                }
            }
            catch (Exception)
            {
                // No INI setting recorded.
            }
            if (!Directory.Exists(regionConfigPath))
                return;

            string[] iniFiles = Directory.GetFiles(regionConfigPath, "*.ini");
            foreach (string file in iniFiles)
            {
                IConfigSource source = new IniConfigSource(file, Nini.Ini.IniFileType.AuroraStyle);
                IConfig cnf = source.Configs[oldName];
                if (cnf != null)
                {
                    try
                    {
                        source.Configs.Remove(cnf);
                        cnf.Set("Location", regionInfo.RegionLocX / Constants.RegionSize + "," + regionInfo.RegionLocY / Constants.RegionSize);
                        cnf.Set("RegionType", regionInfo.RegionType);
                        cnf.Name = regionInfo.RegionName;
                        source.Configs.Add(cnf);
                    }
                    catch
                    {
                    }
                    source.Save();
                    break;
                }
            }
        }

        /// <summary>
        /// This deletes the region from the region.ini file or region.xml file and removes the file if there are no other regions in the file
        /// </summary>
        /// <param name="regionInfo"></param>
        public void DeleteRegion(RegionInfo regionInfo)
        {
            if (!String.IsNullOrEmpty(regionInfo.RegionFile))
            {
                if (regionInfo.RegionFile.ToLower().EndsWith(".xml"))
                {
                    File.Delete(regionInfo.RegionFile);
                    m_log.InfoFormat("[OPENSIM]: deleting region file \"{0}\"", regionInfo.RegionFile);
                }
                if (regionInfo.RegionFile.ToLower().EndsWith(".ini"))
                {
                    try
                    {
                        IniConfigSource source = new IniConfigSource(regionInfo.RegionFile, Nini.Ini.IniFileType.AuroraStyle);
                        if (source.Configs[regionInfo.RegionName] != null)
                        {
                            source.Configs.Remove(regionInfo.RegionName);

                            if (source.Configs.Count == 0)
                            {
                                File.Delete(regionInfo.RegionFile);
                            }
                            else
                            {
                                source.Save(regionInfo.RegionFile);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
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