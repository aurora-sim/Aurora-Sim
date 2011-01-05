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
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Xml;
using System.IO;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework.Console;

namespace OpenSim.Framework
{
    public class RegionInfo
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //The object is a GridRegion
        public delegate void TriggerOnRegionUp(object otherRegion);
        public event TriggerOnRegionUp OnRegionUp;

        public void TriggerRegionUp(object otherRegion)
        {
            if (OnRegionUp != null)
                OnRegionUp(otherRegion);
        }

        public bool commFailTF = false;
        public string RegionFile = String.Empty;
        public bool isSandbox = false;
        public bool Persistent = true;
        public bool Disabled = false;

        private EstateSettings m_estateSettings;
        private RegionSettings m_regionSettings;
        // private IConfigSource m_configSource = null;

        public string proxyUrl = "";
        public int ProxyOffset = 0;
        public string regionSecret = UUID.Random().ToString();

        public string osSecret;

        public string lastMapRefresh = "0";

        private int m_objectCapacity = 0;
        private string m_regionType = String.Empty;
        protected uint m_httpPort;
        protected string m_serverURI;
        protected string m_regionName = String.Empty;
        protected bool Allow_Alternate_Ports;
        public bool m_allow_alternate_ports;
        protected string m_externalHostName;
        protected IPEndPoint m_internalEndPoint;
        protected uint? m_regionLocX;
        protected uint? m_regionLocY;
        protected uint m_remotingPort;
        public UUID RegionID = UUID.Zero;
        public UUID Password = UUID.Random();
        public string RemotingAddress;
        public UUID ScopeID = UUID.Zero;
        private UUID m_GridSecureSessionID = UUID.Zero;
        private IniConfigSource m_source;
        public int NumberStartup = 0;

        /// <summary>
        /// The X length (in meters) that the region is
        /// The default is 256m
        /// </summary>
        public float RegionSizeX = 256;

        /// <summary>
        /// The Y length (in meters) that the region is
        /// The default is 256m
        /// </summary>
        public float RegionSizeY = 256;

        // File based loading
        //
        public RegionInfo(string description, string filename, bool skipConsoleConfig, IConfigSource configSource, string configName)
        {
            // m_configSource = configSource;

            if (filename.ToLower().EndsWith(".ini"))
            {
                if (!File.Exists(filename)) // New region config request
                {
                    IniConfigSource newFile = new IniConfigSource();

                    RegionFile = filename;

                    ReadNiniConfig(newFile, configName);
                    newFile.Save(filename);

                    return;
                }

                m_source = new IniConfigSource(filename, Nini.Ini.IniFileType.AuroraStyle);

                bool saveFile = false;
                if (m_source.Configs[configName] == null)
                    saveFile = true;

                RegionFile = filename;

                bool update = ReadNiniConfig(m_source, configName);

                if (configName != String.Empty && (saveFile || update))
                    m_source.Save(filename);

                return;
            }

            try
            {
                // This will throw if it's not legal Nini XML format
                // and thereby toss it to the legacy loader
                //
                IConfigSource xmlsource = new XmlConfigSource(filename);

                ReadNiniConfig(xmlsource, configName);

                RegionFile = filename;

                return;
            }
            catch (Exception)
            {
            }
        }

        public RegionInfo(uint regionLocX, uint regionLocY, IPEndPoint internalEndPoint, string externalUri)
        {
            m_regionLocX = regionLocX;
            m_regionLocY = regionLocY;

            m_internalEndPoint = internalEndPoint;
            m_externalHostName = externalUri;
        }

        public RegionInfo()
        {
        }

        public EstateSettings EstateSettings
        {
            get
            {
                if (m_estateSettings == null)
                {
                    m_estateSettings = new EstateSettings();
                }

                return m_estateSettings;
            }

            set { m_estateSettings = value; }
        }

        public RegionSettings RegionSettings
        {
            get
            {
                if (m_regionSettings == null)
                {
                    m_regionSettings = new RegionSettings();
                }

                return m_regionSettings;
            }

            set { m_regionSettings = value; }
        }

        private bool m_allowScriptCrossing = false;
        public bool AllowScriptCrossing
        {
            get { return m_allowScriptCrossing; }
            set { m_allowScriptCrossing = value; }
        }

        private bool m_trustBinariesFromForeignSims = false;
        public bool TrustBinariesFromForeignSims
        {
            get { return m_trustBinariesFromForeignSims; }
            set { m_trustBinariesFromForeignSims = value; }
        }

        private bool m_seeIntoThisSimFromNeighbor = true;
        public bool SeeIntoThisSimFromNeighbor
        {
            get { return m_seeIntoThisSimFromNeighbor; }
            set { m_seeIntoThisSimFromNeighbor = value; }
        }

        private bool m_allowPhysicalPrims = true;
        public bool AllowPhysicalPrims
        {
            get { return m_allowPhysicalPrims; }
            set { m_allowPhysicalPrims = value; }
        }

        public int ObjectCapacity
        {
            get { return m_objectCapacity; }
            set
            {
                if (m_objectCapacity == 0) 
                    m_objectCapacity = value;
            }
        }

        public byte AccessLevel
        {
            get { return (byte)Util.ConvertMaturityToAccessLevel((uint)RegionSettings.Maturity); }
            set { RegionSettings.Maturity = (int)Util.ConvertAccessLevelToMaturity(value); }
        }

        public string RegionType
        {
            get { return m_regionType; }
            set { m_regionType = value; }
        }

        public UUID GridSecureSessionID
        {
            get { return m_GridSecureSessionID; }
            set { m_GridSecureSessionID = value; }
        }

        /// <summary>
        /// The port by which http communication occurs with the region (most noticeably, CAPS communication)
        /// </summary>
        public uint HttpPort
        {
            get { return m_httpPort; }
            set { m_httpPort = value; }
        }

        /// <summary>
        /// A well-formed URI for the host region server (namely "http://" + ExternalHostName)
        /// </summary>
        public string ServerURI
        {
            get { return m_serverURI; }
            set { m_serverURI = value; }
        }

        public string RegionName
        {
            get { return m_regionName; }
            set { m_regionName = value; }
        }

        public uint RemotingPort
        {
            get { return m_remotingPort; }
            set { m_remotingPort = value; }
        }

        /// <value>
        /// This accessor can throw all the exceptions that Dns.GetHostAddresses can throw.
        ///
        /// XXX Isn't this really doing too much to be a simple getter, rather than an explict method?
        /// </value>
        public IPEndPoint ExternalEndPoint
        {
            get
            {
                // Old one defaults to IPv6
                //return new IPEndPoint(Dns.GetHostAddresses(m_externalHostName)[0], m_internalEndPoint.Port);

                IPAddress ia = null;
                // If it is already an IP, don't resolve it - just return directly
                if (IPAddress.TryParse(m_externalHostName, out ia))
                    return new IPEndPoint(ia, m_internalEndPoint.Port);

                // Reset for next check
                ia = null;
                try
                {
                    foreach (IPAddress Adr in Dns.GetHostAddresses(m_externalHostName))
                    {
                        if (ia == null)
                            ia = Adr;

                        if (Adr.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ia = Adr;
                            break;
                        }
                    }
                }
                catch (SocketException e)
                {
                    throw new Exception(
                        "Unable to resolve local hostname " + m_externalHostName + " innerException of type '" +
                        e + "' attached to this exception", e);
                }
                return new IPEndPoint(ia, m_internalEndPoint.Port);
            }

            set { m_externalHostName = value.ToString(); }
        }

        public string ExternalHostName
        {
            get { return m_externalHostName; }
            set { m_externalHostName = value; }
        }

        private bool m_FindExternalIP = true;
        public bool FindExternalAutomatically
        {
            get { return m_FindExternalIP; }
            set { m_FindExternalIP = value; }
        }

        public IPEndPoint InternalEndPoint
        {
            get { return m_internalEndPoint; }
            set { m_internalEndPoint = value; }
        }

        public uint RegionLocX
        {
            get { return m_regionLocX.Value; }
            set { m_regionLocX = value; }
        }

        public uint RegionLocY
        {
            get { return m_regionLocY.Value; }
            set { m_regionLocY = value; }
        }

        public ulong RegionHandle
        {
            get { return Util.UIntsToLong((RegionLocX * (uint) Constants.RegionSize), (RegionLocY * (uint) Constants.RegionSize)); }
        }

        public void SetEndPoint(string ipaddr, int port)
        {
            IPAddress tmpIP = IPAddress.Parse(ipaddr);
            IPEndPoint tmpEPE = new IPEndPoint(tmpIP, port);
            m_internalEndPoint = tmpEPE;
        }

        //Returns true if the source should be updated. Returns false if it does not.
        private bool ReadNiniConfig(IConfigSource source, string name)
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

            RegionID = new UUID(regionUUID);
            
            RegionName = name;
            string location = config.GetString("Location", String.Empty);

            if (location == String.Empty)
            {
                NeedsUpdate = true;
                location = MainConsole.Instance.CmdPrompt("Region Location for region " + name, "1000,1000");
                config.Set("Location", location);
            }

            string[] locationElements = location.Split(new char[] {','});

            m_regionLocX = Convert.ToUInt32(locationElements[0]);
            m_regionLocY = Convert.ToUInt32(locationElements[1]);

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

            m_internalEndPoint = new IPEndPoint(address, port);

            if (config.Contains("AllowAlternatePorts"))
            {
                m_allow_alternate_ports = config.GetBoolean("AllowAlternatePorts", true);
            }
            else
            {
                NeedsUpdate = true;
                m_allow_alternate_ports = Convert.ToBoolean(MainConsole.Instance.CmdPrompt("Allow alternate ports", "False"));

                config.Set("AllowAlternatePorts for region " + name, m_allow_alternate_ports.ToString());
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
                m_externalHostName = Util.GetLocalHost().ToString();
                m_log.InfoFormat(
                    "[REGIONINFO]: Resolving SYSTEMIP to {0} for external hostname of region {1}",
                    m_externalHostName, name);
            }
            else
            {
                m_externalHostName = externalName;
            }

            m_regionType = config.GetString("RegionType", m_regionType);

            if (m_regionType == String.Empty)
            {
                NeedsUpdate = true;
                m_regionType = MainConsole.Instance.CmdPrompt("Region Type for region " + name, "Mainland");
                config.Set("RegionType", m_regionType);
            }

            m_allowPhysicalPrims = config.GetBoolean("AllowPhysicalPrims", m_allowPhysicalPrims);

            m_allowScriptCrossing = config.GetBoolean("AllowScriptCrossing", m_allowScriptCrossing);

            m_trustBinariesFromForeignSims = config.GetBoolean("TrustBinariesFromForeignSims", m_trustBinariesFromForeignSims);

            m_seeIntoThisSimFromNeighbor = config.GetBoolean("SeeIntoThisSimFromNeighbor", m_seeIntoThisSimFromNeighbor);

            m_objectCapacity = config.GetInt("MaxPrims", m_objectCapacity);


            // Multi-tenancy
            //
            ScopeID = new UUID(config.GetString("ScopeID", ScopeID.ToString()));

            //Do this last so that we can save the password immediately if it doesn't exist
            UUID password = Password; //Save the pass as this TryParse will wipe it out
            if (!UUID.TryParse(config.GetString("NeighborPassword", ""), out Password))
            {
                Password = password;
                config.Set("NeighborPassword", password);
                WriteNiniConfig(source);
            }

            return NeedsUpdate;
        }

        public void WriteNiniConfig()
        {
            WriteNiniConfig(m_source);
        }

        public void WriteNiniConfig(IConfigSource source)
        {
            try
            {
                //MUST reload or it will overwrite other changes!
                source = new IniConfigSource(RegionFile, Nini.Ini.IniFileType.AuroraStyle);
            }
            catch (FileNotFoundException)
            {
                //If this happens, it is the first time a user has opened Aurora and the RegionFile doesn't exist 
                // yet, so just let it gracefully fail and create itself later
                return;
            }

            CreateIConfig(source);

            source.Save();
        }

        public void CreateIConfig(IConfigSource source)
        {
            IConfig config = source.Configs[RegionName];

            if (config != null)
                source.Configs.Remove(config);

            config = source.AddConfig(RegionName);

            config.Set("RegionUUID", RegionID.ToString());

            string location = String.Format("{0},{1}", m_regionLocX, m_regionLocY);
            config.Set("Location", location);

            config.Set("InternalAddress", m_internalEndPoint.Address.ToString());
            config.Set("InternalPort", m_internalEndPoint.Port);

            config.Set("AllowAlternatePorts", m_allow_alternate_ports.ToString());

            config.Set("ExternalHostName", m_externalHostName);

            if (m_objectCapacity != 0)
                config.Set("MaxPrims", m_objectCapacity);

            if (ScopeID != UUID.Zero)
                config.Set("ScopeID", ScopeID.ToString());

            if (RegionType != String.Empty)
                config.Set("RegionType", RegionType);

            config.Set("AllowPhysicalPrims", AllowPhysicalPrims);
            config.Set("AllowScriptCrossing", AllowScriptCrossing);
            config.Set("TrustBinariesFromForeignSims", TrustBinariesFromForeignSims);
            config.Set("SeeIntoThisSimFromNeighbor", SeeIntoThisSimFromNeighbor);

            config.Set("NeighborPassword", Password.ToString());
        }

        public void SaveRegionToFile(string description, string filename)
        {
            if (filename.ToLower().EndsWith(".ini"))
            {
                IniConfigSource source = new IniConfigSource();
                try
                {
                    source = new IniConfigSource(filename, Nini.Ini.IniFileType.AuroraStyle); // Load if it exists
                }
                catch (Exception)
                {
                }

                WriteNiniConfig(source);

                source.Save(filename);
            }
        }

        public OSDMap PackRegionInfoData()
        {
            return PackRegionInfoData(false);
        }

        public OSDMap PackRegionInfoData(bool secure)
        {
            OSDMap args = new OSDMap();
            args["region_id"] = OSD.FromUUID(RegionID);
            if ((RegionName != null) && !RegionName.Equals(""))
                args["region_name"] = OSD.FromString(RegionName);
            args["external_host_name"] = OSD.FromString(ExternalHostName);
            args["http_port"] = OSD.FromString(HttpPort.ToString());
            args["server_uri"] = OSD.FromString(ServerURI);
            args["region_xloc"] = OSD.FromString(RegionLocX.ToString());
            args["region_yloc"] = OSD.FromString(RegionLocY.ToString());
            args["internal_ep_address"] = OSD.FromString(InternalEndPoint.Address.ToString());
            args["internal_ep_port"] = OSD.FromString(InternalEndPoint.Port.ToString());
            if ((RemotingAddress != null) && !RemotingAddress.Equals(""))
                args["remoting_address"] = OSD.FromString(RemotingAddress);
            args["remoting_port"] = OSD.FromString(RemotingPort.ToString());
            args["allow_alt_ports"] = OSD.FromBoolean(m_allow_alternate_ports);
            if ((proxyUrl != null) && !proxyUrl.Equals(""))
                args["proxy_url"] = OSD.FromString(proxyUrl);
            if (RegionType != String.Empty)
                args["region_type"] = OSD.FromString(RegionType);
            args["password"] = OSD.FromUUID(Password);
            if (secure)
            {
                args["disabled"] = OSD.FromBoolean(Disabled);
                args["scope_id"] = OSD.FromUUID(ScopeID);
                args["object_capacity"] = OSD.FromInteger(m_objectCapacity);
                args["region_type"] = OSD.FromString(RegionType);
                args["see_into_this_sim_from_neighbor"] = OSD.FromBoolean(SeeIntoThisSimFromNeighbor);
                args["trust_binaries_from_foreign_sims"] = OSD.FromBoolean(TrustBinariesFromForeignSims);
                args["allow_script_crossing"] = OSD.FromBoolean(AllowScriptCrossing);
                args["allow_physical_prims"] = OSD.FromBoolean(AllowPhysicalPrims);
                args["number_startup"] = OSD.FromInteger(NumberStartup);
            }
            return args;
        }

        public void UnpackRegionInfoData(OSDMap args)
        {
            if (args["region_id"] != null)
                RegionID = args["region_id"].AsUUID();
            if (args["region_name"] != null)
                RegionName = args["region_name"].AsString();
            if (args["external_host_name"] != null)
                ExternalHostName = args["external_host_name"].AsString();
            if (args["http_port"] != null)
                UInt32.TryParse(args["http_port"].AsString(), out m_httpPort);
            if (args["server_uri"] != null)
                ServerURI = args["server_uri"].AsString();
            if (args["region_xloc"] != null)
            {
                uint locx;
                UInt32.TryParse(args["region_xloc"].AsString(), out locx);
                RegionLocX = locx;
            }
            if (args["region_yloc"] != null)
            {
                uint locy;
                UInt32.TryParse(args["region_yloc"].AsString(), out locy);
                RegionLocY = locy;
            }
            IPAddress ip_addr = null;
            if (args["internal_ep_address"] != null)
            {
                IPAddress.TryParse(args["internal_ep_address"].AsString(), out ip_addr);
            }
            int port = 0;
            if (args["internal_ep_port"] != null)
            {
                Int32.TryParse(args["internal_ep_port"].AsString(), out port);
            }
            InternalEndPoint = new IPEndPoint(ip_addr, port);
            if (args["remoting_address"] != null)
                RemotingAddress = args["remoting_address"].AsString();
            if (args["remoting_port"] != null)
                UInt32.TryParse(args["remoting_port"].AsString(), out m_remotingPort);
            if (args["allow_alt_ports"] != null)
                m_allow_alternate_ports = args["allow_alt_ports"].AsBoolean();
            if (args["proxy_url"] != null)
                proxyUrl = args["proxy_url"].AsString();
            if (args["region_type"] != null)
                m_regionType = args["region_type"].AsString();
            if (args["password"] != null)
                Password = args["password"].AsUUID();

            if (args["disabled"] != null)
                Disabled = args["disabled"].AsBoolean();
            if (args["scope_id"] != null)
                ScopeID = args["scope_id"].AsUUID();
            if (args["scope_id"] != null)
                ScopeID = args["scope_id"].AsUUID();
            if (args["object_capacity"] != null)
                m_objectCapacity = args["object_capacity"].AsInteger();
            if (args["region_type"] != null)
                RegionType = args["region_type"].AsString();
            if (args["see_into_this_sim_from_neighbor"] != null)
                SeeIntoThisSimFromNeighbor = args["see_into_this_sim_from_neighbor"].AsBoolean();
            if (args["trust_binaries_from_foreign_sims"] != null)
                TrustBinariesFromForeignSims = args["trust_binaries_from_foreign_sims"].AsBoolean();
            if (args["allow_script_crossing"] != null)
                AllowScriptCrossing = args["allow_script_crossing"].AsBoolean();
            if (args["allow_physical_prims"] != null)
                AllowPhysicalPrims = args["allow_physical_prims"].AsBoolean();
            if(args["number_startup"] != null)
                NumberStartup = args["number_startup"].AsInteger();
        }

        public static RegionInfo Create(UUID regionID, string regionName, uint regX, uint regY, string externalHostName, uint httpPort, uint simPort, uint remotingPort, string serverURI)
        {
            RegionInfo regionInfo;
            IPEndPoint neighborInternalEndPoint = new IPEndPoint(Util.GetHostFromDNS(externalHostName), (int)simPort);
            regionInfo = new RegionInfo(regX, regY, neighborInternalEndPoint, externalHostName);
            regionInfo.RemotingPort = remotingPort;
            regionInfo.RemotingAddress = externalHostName;
            regionInfo.HttpPort = httpPort;
            regionInfo.RegionID = regionID;
            regionInfo.RegionName = regionName;
            regionInfo.ServerURI = serverURI;
            return regionInfo;
        }

        public int getInternalEndPointPort()
        {
            return m_internalEndPoint.Port;
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
    }
}
