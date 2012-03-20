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
using Aurora.Framework;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.ApplicationPlugins.RegionLoaderPlugin
{
    public class RegionLoaderFileSystem : IRegionLoader
    {
        private IConfigSource m_configSource;
        private bool m_default = true;
        private bool m_enabled = true;
        private ISimulationBase m_openSim;
        private string m_regionConfigPath = Path.Combine(Util.configDir(), "Regions");

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
                m_default = config.GetString("Default") == GetType().Name;
                m_regionConfigPath = config.GetString("RegionsDirectory", m_regionConfigPath).Trim();

                //Add the console command if it is the default
                if (m_default)
                    MainConsole.Instance.Commands.AddCommand("create region", "create region", "Create a new region.", AddRegion);
            }
            
            m_openSim.ApplicationRegistry.StackModuleInterface<IRegionLoader>(this);
        }

        public RegionInfo[] LoadRegions()
        {
            return InternalLoadRegions (false);
        }

        public RegionInfo[] InternalLoadRegions (bool checkOnly)
        {
            if (!Directory.Exists (m_regionConfigPath))
                if (checkOnly)
                    return null;
                else
                    Directory.CreateDirectory (m_regionConfigPath);

            string[] configFiles = Directory.GetFiles (m_regionConfigPath, "*.xml");
            string[] iniFiles = Directory.GetFiles (m_regionConfigPath, "*.ini");

            if (configFiles.Length == 0 && iniFiles.Length == 0)
            {
                if (!m_default || checkOnly)
                    return null;
                LoadRegionFromFile ("DEFAULT REGION CONFIG", Path.Combine (m_regionConfigPath, "Regions.ini"), false, m_configSource, "");
                iniFiles = Directory.GetFiles (m_regionConfigPath, "*.ini");
            }

            List<RegionInfo> regionInfos = new List<RegionInfo> ();

            int i = 0;
            foreach (string file in iniFiles)
            {
                IConfigSource source = new IniConfigSource (file, Nini.Ini.IniFileType.AuroraStyle);

                foreach (IConfig config in source.Configs)
                {
                    RegionInfo regionInfo = LoadRegionFromFile ("REGION CONFIG #" + (i + 1), file, false, m_configSource, config.Name);
                    regionInfos.Add (regionInfo);
                    i++;
                }
            }

            foreach (string file in configFiles)
            {
                RegionInfo regionInfo = LoadRegionFromFile ("REGION CONFIG #" + (i + 1), file, false, m_configSource, string.Empty);
                regionInfos.Add (regionInfo);
                i++;
            }

            return regionInfos.ToArray ();
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
                MainConsole.Instance.Info ("=====================================\n");
                MainConsole.Instance.Info ("We are now going to ask a couple of questions about your region.\n");
                MainConsole.Instance.Info ("You can press 'enter' without typing anything to use the default\n");
                MainConsole.Instance.Info ("the default is displayed between [ ] brackets.\n");
                MainConsole.Instance.Info ("=====================================\n");
            }

            bool NeedsUpdate = false;
            if (name == String.Empty)
                name = MainConsole.Instance.Prompt("New region name", name);
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

                regionUUID = MainConsole.Instance.Prompt("Region UUID for region " + name, newID.ToString());
                config.Set("RegionUUID", regionUUID);
            }

            region.RegionID = new UUID(regionUUID);

            region.RegionName = name;
            string location = config.GetString("Location", String.Empty);

            if (location == String.Empty)
            {
                NeedsUpdate = true;
                location = MainConsole.Instance.Prompt("Region Location for region " + name, "1000,1000");
                config.Set("Location", location);
            }

            string[] locationElements = location.Split(new[] { ',' });

            region.RegionLocX = Convert.ToInt32(locationElements[0]) * Constants.RegionSize;
            region.RegionLocY = Convert.ToInt32(locationElements[1]) * Constants.RegionSize;

            int regionSizeX = config.GetInt("RegionSizeX", 0);
            if (regionSizeX == 0 || ((region.RegionSizeX % Constants.MinRegionSize) != 0))
            {
                NeedsUpdate = true;
                while (true)
                {
                    if (int.TryParse(MainConsole.Instance.Prompt("Region X Size for region " + name, "256"), out regionSizeX))
                        break;
                }
                config.Set("RegionSizeX", regionSizeX);
            }
            region.RegionSizeX = Convert.ToInt32(regionSizeX);

            int regionSizeY = config.GetInt("RegionSizeY", 0);
            if (regionSizeY == 0 || ((region.RegionSizeY % Constants.MinRegionSize) != 0))
            {
                NeedsUpdate = true;
                while(true)
                {
                    if(int.TryParse(MainConsole.Instance.Prompt("Region Y Size for region " + name, "256"), out regionSizeY))
                        break;
                }
                config.Set("RegionSizeY", regionSizeY);
            }
            region.RegionSizeY = regionSizeY;

            int regionSizeZ = config.GetInt("RegionSizeZ", 1024);
            //if (regionSizeZ == String.Empty)
            //{
            //    NeedsUpdate = true;
            //    regionSizeZ = MainConsole.Instance.CmdPrompt("Region Z Size for region " + name, "1024");
            //    config.Set("RegionSizeZ", regionSizeZ);
            //}
            region.RegionSizeZ = regionSizeZ;

            // Internal IP
            IPAddress address;

            if (config.Contains("InternalAddress"))
            {
                address = IPAddress.Parse(config.GetString("InternalAddress", String.Empty));
            }
            else
            {
                NeedsUpdate = true;
                address = IPAddress.Parse(MainConsole.Instance.Prompt("Internal IP address for region " + name, "0.0.0.0"));
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
                port = Convert.ToInt32(MainConsole.Instance.Prompt("Internal port for region " + name, "9000"));
                config.Set("InternalPort", port);
            }
            region.UDPPorts.Add (port);
            region.InternalEndPoint = new IPEndPoint(address, port);

            // External IP
            //
            string externalName;
            if (config.Contains("ExternalHostName"))
            {
                //Let's know our external IP (by Enrico Nirvana)
                externalName = config.GetString("ExternalHostName", Aurora.Framework.Utilities.GetExternalIp());
            }
            else
            {
                NeedsUpdate = true;
                //Let's know our external IP (by Enrico Nirvana)
                externalName = MainConsole.Instance.Prompt("External host name for region " + name, Aurora.Framework.Utilities.GetExternalIp());
                config.Set("ExternalHostName", externalName);
                //ended here (by Enrico Nirvana)
            }

            region.RegionType = config.GetString("RegionType", region.RegionType);

            if (region.RegionType == String.Empty)
            {
                NeedsUpdate = true;
                region.RegionType = MainConsole.Instance.Prompt("Region Type for region " + name, "Mainland");
                config.Set("RegionType", region.RegionType);
            }

            region.AllowPhysicalPrims = config.GetBoolean("AllowPhysicalPrims", region.AllowPhysicalPrims);

            region.AllowScriptCrossing = config.GetBoolean("AllowScriptCrossing", region.AllowScriptCrossing);

            region.TrustBinariesFromForeignSims = config.GetBoolean("TrustBinariesFromForeignSims", region.TrustBinariesFromForeignSims);

            region.SeeIntoThisSimFromNeighbor = config.GetBoolean("SeeIntoThisSimFromNeighbor", region.SeeIntoThisSimFromNeighbor);

            region.ObjectCapacity = config.GetInt ("MaxPrims", region.ObjectCapacity);

            region.Startup = (StartupType)Enum.Parse(typeof(StartupType), config.GetString ("StartupType", region.Startup.ToString()));


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

        /// <summary>
        /// Creates a new region based on the parameters specified.   This will ask the user questions on the console
        /// </summary>
        /// <param name="cmd">0,1,region name, region XML file</param>
        public void AddRegion(string[] cmd)
        {
            string fileName = MainConsole.Instance.Prompt ("File Name", "Regions.ini");
            string regionName = MainConsole.Instance.Prompt ("Region Name", "New Region");

            if (fileName.EndsWith (".ini"))
            {
                string regionFile = String.Format ("{0}/{1}", m_regionConfigPath, fileName);
                // Allow absolute and relative specifiers
                if (fileName.StartsWith ("/") || fileName.StartsWith ("\\") || fileName.StartsWith (".."))
                    regionFile = fileName;

                MainConsole.Instance.Debug ("[LOADREGIONS]: Creating Region: " + regionName);
                SceneManager manager = m_openSim.ApplicationRegistry.RequestModuleInterface<SceneManager>();
                manager.AllRegions++;
                manager.StartNewRegion(LoadRegionFromFile(regionName, regionFile, false, m_configSource, regionName));
            }
            else
            {
                MainConsole.Instance.Info ("The file name must end with .ini");
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
                    MainConsole.Instance.InfoFormat("[OPENSIM]: deleting region file \"{0}\"", regionInfo.RegionFile);
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

        public void DeleteAllRegionFiles ()
        {
            string regionConfigPath = Path.Combine (Util.configDir (), "Regions");
            IConfig config = m_configSource.Configs["RegionStartup"];
            if (config != null)
                regionConfigPath = config.GetString ("RegionsDirectory", regionConfigPath).Trim ();
            if (!Directory.Exists (regionConfigPath))
                return;
            Directory.Delete (regionConfigPath);
        }

        public bool FailedToStartRegions(string reason)
        {
            //Can't deal with it
            return false;
        }

        public string Name
        {
            get { return "File Based Plugin"; }
        }

        public void Dispose()
        {
        }
    }
}
