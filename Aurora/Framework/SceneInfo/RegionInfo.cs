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

using Aurora.Framework.Utilities;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;
using System;
using System.Net;

namespace Aurora.Framework
{
    public enum StartupType
    {
        Medium = 2,
        Normal = 3
    }

    [Serializable, ProtoContract(UseProtoMembersOnly = false)]
    public class RegionInfo : AllScopeIDImpl
    {
        private RegionSettings m_regionSettings;

        protected int m_objectCapacity = 0;
        protected string m_regionType = String.Empty;
        protected uint m_httpPort;
        protected string m_regionName = String.Empty;
        protected IPEndPoint m_internalEndPoint;
        protected int m_regionLocX;
        protected int m_regionLocY;
        protected int m_regionLocZ;
        private UUID m_GridSecureSessionID = UUID.Zero;
        public bool NewRegion = false;
        public bool HasBeenDeleted { get; set; }
        private bool m_seeIntoThisSimFromNeighbor = true;

        [ProtoMember(1)] public UUID RegionID = UUID.Zero;
        [ProtoMember(2)] public StartupType Startup = StartupType.Normal;

        [ProtoMember(3)] public OpenRegionSettings OpenRegionSettings = new OpenRegionSettings();

        [ProtoMember(4)] public OSD EnvironmentSettings = null;

        /// <summary>
        ///     The X length (in meters) that the region is
        ///     The default is 256m
        /// </summary>
        [ProtoMember(5)] public int RegionSizeX = 256;

        /// <summary>
        ///     The Y length (in meters) that the region is
        ///     The default is 256m
        /// </summary>
        [ProtoMember(6)] public int RegionSizeY = 256;

        /// <summary>
        ///     The Z height (in meters) that the region is (not supported currently)
        ///     The default is 1024m
        /// </summary>
        [ProtoMember(7)] public int RegionSizeZ = 4096;

        /// <summary>
        ///     The region flags (as set on the Grid Server in the database), cached on RegisterRegion call
        /// </summary>
        [ProtoMember(8)] public int RegionFlags = -1;

        [ProtoMember(9)]
        public EstateSettings EstateSettings { get; set; }

        [ProtoMember(10)]
        public RegionSettings RegionSettings
        {
            get { return m_regionSettings ?? (m_regionSettings = new RegionSettings()); }
            set { m_regionSettings = value; }
        }

        [ProtoMember(11)] public bool InfiniteRegion = false;

        [ProtoMember(13)]
        public bool SeeIntoThisSimFromNeighbor
        {
            get { return m_seeIntoThisSimFromNeighbor; }
            set { m_seeIntoThisSimFromNeighbor = value; }
        }

        [ProtoMember(15)]
        public int ObjectCapacity
        {
            get { return m_objectCapacity; }
            set { m_objectCapacity = value; }
        }

        [ProtoMember(16)]
        public byte AccessLevel
        {
            get { return Util.ConvertMaturityToAccessLevel((uint) RegionSettings.Maturity); }
            set { RegionSettings.Maturity = (int) Util.ConvertAccessLevelToMaturity(value); }
        }

        [ProtoMember(17)]
        public string RegionType
        {
            get { return m_regionType; }
            set { m_regionType = value; }
        }

        [ProtoMember(18)]
        public UUID GridSecureSessionID
        {
            get { return m_GridSecureSessionID; }
            set { m_GridSecureSessionID = value; }
        }

        [ProtoMember(19)]
        public string RegionName
        {
            get { return m_regionName; }
            set { m_regionName = value; }
        }

        [ProtoMember(20)]
        public IPEndPoint InternalEndPoint
        {
            get { return m_internalEndPoint; }
            set { m_internalEndPoint = value; }
        }

        [ProtoMember(21)]
        public int RegionLocX
        {
            get { return m_regionLocX; }
            set { m_regionLocX = value; }
        }

        [ProtoMember(22)]
        public int RegionLocY
        {
            get { return m_regionLocY; }
            set { m_regionLocY = value; }
        }

        [ProtoMember(23)]
        public int RegionLocZ
        {
            get { return m_regionLocZ; }
            set { m_regionLocZ = value; }
        }

        public ulong RegionHandle
        {
            get { return Utils.UIntsToLong((uint) RegionLocX, (uint) RegionLocY); }
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
            args["region_size_x"] = OSD.FromInteger(RegionSizeX);
            args["region_size_y"] = OSD.FromInteger(RegionSizeY);
            args["region_size_z"] = OSD.FromInteger(RegionSizeZ);
            args["InfiniteRegion"] = OSD.FromBoolean(InfiniteRegion);
            args["scope_id"] = OSD.FromUUID(ScopeID);
            args["all_scope_ids"] = AllScopeIDs.ToOSDArray();
            args["object_capacity"] = OSD.FromInteger(m_objectCapacity);
            args["region_type"] = OSD.FromString(RegionType);
            args["see_into_this_sim_from_neighbor"] = OSD.FromBoolean(SeeIntoThisSimFromNeighbor);
            args["startupType"] = OSD.FromInteger((int) Startup);
            args["RegionSettings"] = RegionSettings.ToOSD();
            args["GridSecureSessionID"] = GridSecureSessionID;
            if (EnvironmentSettings != null)
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

            if (args.ContainsKey("scope_id"))
                ScopeID = args["scope_id"].AsUUID();
            if (args.ContainsKey("all_scope_ids"))
                AllScopeIDs = ((OSDArray) args["all_scope_ids"]).ConvertAll<UUID>(o => o);

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
            if (args.ContainsKey("startupType"))
                Startup = (StartupType) args["startupType"].AsInteger();
            if (args.ContainsKey("InfiniteRegion"))
                InfiniteRegion = args["InfiniteRegion"].AsBoolean();
            if (args.ContainsKey("RegionSettings"))
            {
                RegionSettings = new RegionSettings();
                RegionSettings.FromOSD((OSDMap) args["RegionSettings"]);
            }
            if (args.ContainsKey("GridSecureSessionID"))
                GridSecureSessionID = args["GridSecureSessionID"];
            if (args.ContainsKey("OpenRegionSettings"))
            {
                OpenRegionSettings = new OpenRegionSettings();
                OpenRegionSettings.FromOSD((OSDMap) args["OpenRegionSettings"]);
            }
            else
                OpenRegionSettings = new OpenRegionSettings();
            if (args.ContainsKey("EnvironmentSettings"))
                EnvironmentSettings = args["EnvironmentSettings"];
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