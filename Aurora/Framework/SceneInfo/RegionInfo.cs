/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using System.Net;
using System.IO;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public enum StartupType
    {
        Medium = 2,
        Normal = 3
    }

    public class RegionInfo : AllScopeIDImpl
    {
        private RegionSettings m_regionSettings;

        private int m_objectCapacity = 0;
        private string m_regionType = String.Empty;
        protected uint m_httpPort;
        protected string m_serverURI;
        protected string m_regionName = String.Empty;
        protected IPEndPoint m_internalEndPoint;
        protected int m_regionLocX;
        protected int m_regionLocY;
        protected int m_regionLocZ;
        public UUID RegionID = UUID.Zero;
        public UUID Password = UUID.Random();
        private UUID m_GridSecureSessionID = UUID.Zero;
        public StartupType Startup = StartupType.Normal;
        public bool InfiniteRegion = false;
        public bool NewRegion = false;

        public OpenRegionSettings OpenRegionSettings = null;
        public OSD EnvironmentSettings = null;

        /// <summary>
        /// The X length (in meters) that the region is
        /// The default is 256m
        /// </summary>
        public int RegionSizeX = 256;

        /// <summary>
        /// The Y length (in meters) that the region is
        /// The default is 256m
        /// </summary>
        public int RegionSizeY = 256;

        /// <summary>
        /// The Z height (in meters) that the region is (not supported currently)
        /// The default is 1024m
        /// </summary>
        public int RegionSizeZ = 4096;

        /// <summary>
        /// The region flags (as set on the Grid Server in the database), cached on RegisterRegion call
        /// </summary>
        public int RegionFlags = -1;

        public EstateSettings EstateSettings { get; set; }

        public RegionSettings RegionSettings
        {
            get { return m_regionSettings ?? (m_regionSettings = new RegionSettings()); }

            set { m_regionSettings = value; }
        }

        public bool HasBeenDeleted { get; set; }

        public bool AllowScriptCrossing { get; set; }

        private List<int> m_UDPPorts = new List<int> ();
        public List<int> UDPPorts
        {
            get { return m_UDPPorts; }
            set { m_UDPPorts = value; }
        }

        public bool TrustBinariesFromForeignSims { get; set; }

        private bool m_seeIntoThisSimFromNeighbor = true;
        public bool SeeIntoThisSimFromNeighbor
        {
            get { return m_seeIntoThisSimFromNeighbor; }
            set { m_seeIntoThisSimFromNeighbor = value; }
        }

        private bool m_allowPhysicalPrims = true;

        public RegionInfo()
        {
            TrustBinariesFromForeignSims = false;
            AllowScriptCrossing = false;
        }

        public bool AllowPhysicalPrims
        {
            get { return m_allowPhysicalPrims; }
            set { m_allowPhysicalPrims = value; }
        }

        public int ObjectCapacity
        {
            get { return m_objectCapacity; }
            set { m_objectCapacity = value; }
        }

        public byte AccessLevel
        {
            get { return Util.ConvertMaturityToAccessLevel((uint)RegionSettings.Maturity); }
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

        public string RegionName
        {
            get { return m_regionName; }
            set { m_regionName = value; }
        }

        public IPEndPoint InternalEndPoint
        {
            get { return m_internalEndPoint; }
            set { m_internalEndPoint = value; }
        }

        public int RegionLocX
        {
            get { return m_regionLocX; }
            set { m_regionLocX = value; }
        }

        public int RegionLocY
        {
            get { return m_regionLocY; }
            set { m_regionLocY = value; }
        }

        public int RegionLocZ
        {
            get { return m_regionLocZ; }
            set { m_regionLocZ = value; }
        }

        public ulong RegionHandle
        {
            get { return Utils.UIntsToLong((uint)RegionLocX, (uint)RegionLocY); }
        }

        public OSDMap PackRegionInfoData()
        {
            OSDMap args = new OSDMap();
            args["region_id"] = OSD.FromUUID(RegionID);
            if ((RegionName != null) && !RegionName.Equals(""))
                args["region_name"] = OSD.FromString(RegionName);
            args["region_xloc"] = OSD.FromString(RegionLocX.ToString());
            args["region_yloc"] = OSD.FromString(RegionLocY.ToString());
            args["internal_ep_address"] = OSD.FromString(InternalEndPoint.Address.ToString());
            args["internal_ep_port"] = OSD.FromString(InternalEndPoint.Port.ToString());
            if (RegionType != String.Empty)
                args["region_type"] = OSD.FromString(RegionType);
            args["password"] = OSD.FromUUID(Password);
            args["region_size_x"] = OSD.FromInteger(RegionSizeX);
            args["region_size_y"] = OSD.FromInteger(RegionSizeY);
            args["region_size_z"] = OSD.FromInteger(RegionSizeZ);
            OSDArray ports = new OSDArray(UDPPorts.ConvertAll<OSD>(a => a));
            args["UDPPorts"] = ports;
            args["InfiniteRegion"] = OSD.FromBoolean(InfiniteRegion);
            args["scope_id"] = OSD.FromUUID(ScopeID);
            args["all_scope_ids"] = AllScopeIDs.ToOSDArray();
            args["object_capacity"] = OSD.FromInteger(m_objectCapacity);
            args["region_type"] = OSD.FromString(RegionType);
            args["see_into_this_sim_from_neighbor"] = OSD.FromBoolean(SeeIntoThisSimFromNeighbor);
            args["trust_binaries_from_foreign_sims"] = OSD.FromBoolean(TrustBinariesFromForeignSims);
            args["allow_script_crossing"] = OSD.FromBoolean(AllowScriptCrossing);
            args["allow_physical_prims"] = OSD.FromBoolean (AllowPhysicalPrims);
            args["startupType"] = OSD.FromInteger((int)Startup);
            args["RegionSettings"] = RegionSettings.ToOSD();
            args["GridSecureSessionID"] = GridSecureSessionID;
            if(EnvironmentSettings != null)
                args["EnvironmentSettings"] = EnvironmentSettings;
            args["OpenRegionSettings"] = OpenRegionSettings.ToOSD();
            return args;
        }

        public void UnpackRegionInfoData(OSDMap args)
        {
            if (args.ContainsKey("region_id"))
                RegionID = args["region_id"].AsUUID();
            if (args.ContainsKey("region_name"))
                RegionName = args["region_name"].AsString();
            if (args.ContainsKey("http_port"))
                UInt32.TryParse(args["http_port"].AsString(), out m_httpPort);
            if (args.ContainsKey("region_xloc"))
            {
                int locx;
                Int32.TryParse(args["region_xloc"].AsString(), out locx);
                RegionLocX = locx;
            }
            if (args.ContainsKey("region_yloc"))
            {
                int locy;
                Int32.TryParse(args["region_yloc"].AsString(), out locy);
                RegionLocY = locy;
            }
            IPAddress ip_addr = null;
            if (args.ContainsKey("internal_ep_address"))
            {
                IPAddress.TryParse(args["internal_ep_address"].AsString(), out ip_addr);
            }
            int port = 0;
            if (args.ContainsKey("internal_ep_port"))
            {
                Int32.TryParse(args["internal_ep_port"].AsString(), out port);
            }
            InternalEndPoint = new IPEndPoint(ip_addr, port);
            if (args.ContainsKey("region_type"))
                m_regionType = args["region_type"].AsString();
            if (args.ContainsKey("password"))
                Password = args["password"].AsUUID();

            if (args.ContainsKey("scope_id"))
                ScopeID = args["scope_id"].AsUUID();
            if (args.ContainsKey("all_scope_ids"))
                AllScopeIDs = ((OSDArray)args["all_scope_ids"]).ConvertAll<UUID>(o => o);

            if (args.ContainsKey("region_size_x"))
                RegionSizeX = args["region_size_x"].AsInteger();
            if (args.ContainsKey("region_size_y"))
                RegionSizeY = args["region_size_y"].AsInteger();
            if (args.ContainsKey("region_size_z"))
                RegionSizeZ = args["region_size_z"].AsInteger();

            if (args.ContainsKey("object_capacity"))
                m_objectCapacity = args["object_capacity"].AsInteger();
            if (args.ContainsKey("region_type"))
                RegionType = args["region_type"].AsString();
            if (args.ContainsKey("see_into_this_sim_from_neighbor"))
                SeeIntoThisSimFromNeighbor = args["see_into_this_sim_from_neighbor"].AsBoolean();
            if (args.ContainsKey("trust_binaries_from_foreign_sims"))
                TrustBinariesFromForeignSims = args["trust_binaries_from_foreign_sims"].AsBoolean();
            if (args.ContainsKey("allow_script_crossing"))
                AllowScriptCrossing = args["allow_script_crossing"].AsBoolean();
            if (args.ContainsKey("allow_physical_prims"))
                AllowPhysicalPrims = args["allow_physical_prims"].AsBoolean();
            if (args.ContainsKey ("startupType"))
                Startup = (StartupType)args["startupType"].AsInteger();
            if(args.ContainsKey("InfiniteRegion"))
                InfiniteRegion = args["InfiniteRegion"].AsBoolean();
            if (args.ContainsKey("RegionSettings"))
            {
                RegionSettings = new RegionSettings();
                RegionSettings.FromOSD((OSDMap)args["RegionSettings"]);
            }
            if (args.ContainsKey("GridSecureSessionID"))
                GridSecureSessionID = args["GridSecureSessionID"];
            if (args.ContainsKey ("UDPPorts"))
            {
                OSDArray ports = (OSDArray)args["UDPPorts"];
                foreach (OSD p in ports)
                    m_UDPPorts.Add (p.AsInteger ());
            }
            if (args.ContainsKey("OpenRegionSettings"))
            {
                OpenRegionSettings = new OpenRegionSettings();
                OpenRegionSettings.FromOSD((OSDMap)args["OpenRegionSettings"]);
            }
            else
                OpenRegionSettings = new OpenRegionSettings();
            if (args.ContainsKey("EnvironmentSettings"))
                EnvironmentSettings = args["EnvironmentSettings"];
            if (!m_UDPPorts.Contains (InternalEndPoint.Port))
                m_UDPPorts.Add (InternalEndPoint.Port);
        }

        public override void FromOSD(OSDMap map)
        {
            UnpackRegionInfoData(map);
        }

        public override OSDMap ToOSD()
        {
            return PackRegionInfoData();
        }
    }
}
