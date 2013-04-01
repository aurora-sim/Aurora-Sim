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

using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Servers;
using Aurora.Framework.Utilities;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Net;

namespace Aurora.Framework.Services
{
    public interface IGridService
    {
        IGridService InnerService { get; }

        /// <summary>
        ///     The max size a region can be (meters)
        /// </summary>
        int GetMaxRegionSize();

        /// <summary>
        ///     The size (in meters) of how far neighbors will be found
        /// </summary>
        int GetRegionViewSize();

        /// <summary>
        ///     Register a region with the grid service.
        /// </summary>
        /// <param name="regionInfos"> </param>
        /// <param name="oldSessionID"></param>
        /// <param name="password"> </param>
        /// <param name="majorProtocolVersion"></param>
        /// <param name="minorProtocolVersion"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Thrown if region registration failed</exception>
        RegisterRegion RegisterRegion(GridRegion regionInfos, UUID oldSessionID, string password,
                                      int majorProtocolVersion, int minorProtocolVersion);

        /// <summary>
        ///     Deregister a region with the grid service.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Thrown if region deregistration failed</exception>
        bool DeregisterRegion(GridRegion region);

        /// <summary>
        ///     Get a specific region by UUID in the given scope
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <param name="regionID"></param>
        /// <returns></returns>
        GridRegion GetRegionByUUID(List<UUID> scopeIDs, UUID regionID);

        /// <summary>
        ///     Get the region at the given position (in meters)
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        GridRegion GetRegionByPosition(List<UUID> scopeIDs, int x, int y);

        /// <summary>
        ///     Get the first returning region by name in the given scope
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <param name="regionName"></param>
        /// <returns></returns>
        GridRegion GetRegionByName(List<UUID> scopeIDs, string regionName);

        /// <summary>
        ///     Get information about regions starting with the provided name.
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <param name="name"> The name to match against.</param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns>
        ///     A list of <see cref="RegionInfo" />s of regions with matching name. If the
        ///     grid-server couldn't be contacted or returned an error, return null.
        /// </returns>
        List<GridRegion> GetRegionsByName(List<UUID> scopeIDs, string name, uint? start, uint? count);

        /// <summary>
        ///     Get number of regions starting with the provided name.
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <param name="name">
        ///     The name to match against.
        /// </param>
        /// <returns>
        ///     A the count of <see cref="RegionInfo" />s of regions with matching name. If the
        ///     grid-server couldn't be contacted or returned an error, returns 0.
        /// </returns>
        uint GetRegionsByNameCount(List<UUID> scopeIDs, string name);

        /// <summary>
        ///     Get all regions within the range of (xmin - xmax, ymin - ymax) (in meters)
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <param name="xmin"></param>
        /// <param name="xmax"></param>
        /// <param name="ymin"></param>
        /// <param name="ymax"></param>
        /// <returns></returns>
        List<GridRegion> GetRegionRange(List<UUID> scopeIDs, int xmin, int xmax, int ymin, int ymax);

        /// <summary>
        ///     Get all regions within the range of specified center.
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <param name="centerX"></param>
        /// <param name="centerY"></param>
        /// <param name="squareRangeFromCenterInMeters"></param>
        /// <returns></returns>
        List<GridRegion> GetRegionRange(List<UUID> scopeIDs, float centerX, float centerY,
                                        uint squareRangeFromCenterInMeters);

        /// <summary>
        ///     Get the neighbors of the given region
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        List<GridRegion> GetNeighbors(List<UUID> scopeIDs, GridRegion region);

        /// <summary>
        ///     Get any default regions that have been set for users that are logging in that don't have a region to log into
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <returns></returns>
        List<GridRegion> GetDefaultRegions(List<UUID> scopeIDs);

        /// <summary>
        ///     If all the default regions are down, find any fallback regions that have been set near x,y
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        List<GridRegion> GetFallbackRegions(List<UUID> scopeIDs, int x, int y);

        /// <summary>
        ///     If there still are no regions after fallbacks have been checked, find any region near x,y
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        List<GridRegion> GetSafeRegions(List<UUID> scopeIDs, int x, int y);

        /// <summary>
        ///     Get the current flags of the given region
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <param name="regionID"></param>
        /// <returns></returns>
        int GetRegionFlags(List<UUID> scopeIDs, UUID regionID);

        /// <summary>
        ///     Update the map of the given region if the sessionID is correct
        /// </summary>
        /// <param name="region"></param>
        /// <param name="online"></param>
        /// <returns></returns>
        string UpdateMap(GridRegion region, bool online);

        /// <summary>
        ///     Get all map items of the given type for the given region
        /// </summary>
        /// <param name="scopeIDs"></param>
        /// <param name="regionHandle"></param>
        /// <param name="gridItemType"></param>
        /// <returns></returns>
        multipleMapItemReply GetMapItems(List<UUID> scopeIDs, ulong regionHandle, GridItemType gridItemType);

        /// <summary>
        ///     The region (RegionID) has been determined to be unsafe, don't let agents log into it if no other region is found
        /// </summary>
        /// <param name="id"></param>
        void SetRegionUnsafe(UUID id);

        /// <summary>
        ///     The region (RegionID) has been determined to be safe, allow agents to log into it again
        /// </summary>
        /// <param name="id"></param>
        void SetRegionSafe(UUID id);

        /// <summary>
        ///     Verify the given SessionID for the given region
        /// </summary>
        /// <param name="r"></param>
        /// <param name="SessionID"></param>
        /// <returns></returns>
        bool VerifyRegionSessionID(GridRegion r, UUID SessionID);

        void Configure(Nini.Config.IConfigSource config, IRegistryCore registry);

        void Start(Nini.Config.IConfigSource config, IRegistryCore registry);

        void FinishedStartup();
    }

    [ProtoContract()]
    public class RegisterRegion : IDataTransferable
    {
        [ProtoMember(1)]
        public string Error;
        [ProtoMember(2)]
        public List<GridRegion> Neighbors = new List<GridRegion>();
        [ProtoMember(3)]
        public UUID SessionID;
        [ProtoMember(4)]
        public int RegionFlags = 0;
        [ProtoMember(5)]
        public GridRegion Region;
        [ProtoMember(6)]
        public Dictionary<string, string> URIs = new Dictionary<string, string>();

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Error"] = Error;
            map["Neighbors"] = new OSDArray(Neighbors.ConvertAll<OSD>((region) => region.ToOSD()));
            map["SessionID"] = SessionID;
            map["RegionFlags"] = RegionFlags;
            if (Region != null)
                map["Region"] = Region.ToOSD();
            if (URIs != null)
                map["URIs"] = URIs.ToOSDMap();
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Error = map["Error"];
            OSDArray n = (OSDArray) map["Neighbors"];
            Neighbors = n.ConvertAll<GridRegion>((osd) =>
                                                     {
                                                         GridRegion r = new GridRegion();
                                                         r.FromOSD((OSDMap) osd);
                                                         return r;
                                                     });
            SessionID = map["SessionID"];
            RegionFlags = map["RegionFlags"];
            if (map.ContainsKey("Region"))
            {
                Region = new GridRegion();
                Region.FromOSD((OSDMap)map["Region"]);
            }
            if (map.ContainsKey("URIs"))
                URIs = ((OSDMap)map["URIs"]).ConvertMap<string>((o)=>o);
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class GridRegion : AllScopeIDImpl
    {
        #region GridRegion

        /// <summary>
        ///     The port by which http communication occurs with the region
        /// </summary>
        [ProtoMember(3)]
        public uint HttpPort { get; set; }

        [ProtoMember(4)]
        public string RegionName { get; set; }

        [ProtoMember(5)]
        public string RegionType { get; set; }

        [ProtoMember(6)]
        public int RegionLocX { get; set; }
        [ProtoMember(7)]
        public int RegionLocY { get; set; }
        [ProtoMember(8)]
        public int RegionLocZ { get; set; }
        [ProtoMember(9)]
        public UUID EstateOwner { get; set; }
        [ProtoMember(10)]
        public int RegionSizeX { get; set; }
        [ProtoMember(11)]
        public int RegionSizeY { get; set; }
        [ProtoMember(12)]
        public int RegionSizeZ { get; set; }
        [ProtoMember(13)]
        public int Flags { get; set; }
        [ProtoMember(14)]
        public UUID SessionID { get; set; }
        [ProtoMember(15)]
        public UUID RegionID { get; set; }
        [ProtoMember(16)]
        public UUID TerrainImage { get; set; }
        [ProtoMember(17)]
        public UUID TerrainMapImage { get; set; }
        [ProtoMember(18)]
        public UUID ParcelMapImage { get; set; }
        [ProtoMember(19)]
        public byte Access { get; set; }
        [ProtoMember(20)]
        public int LastSeen { get; set; }
        [ProtoMember(21)]
        public string ExternalHostName { get; set; }
        [ProtoMember(22)]
        public int InternalPort { get; set; }

        public bool IsOnline
        {
            get { return (Flags & (int) RegionFlags.RegionOnline) == 1; }
            set
            {
                if (value)
                    Flags |= (int) RegionFlags.RegionOnline;
                else
                    Flags &= (int) RegionFlags.RegionOnline;
            }
        }

        /// <summary>
        ///     A well-formed URI for the host region server (namely "http://" + ExternalHostName + : + HttpPort)
        /// </summary>
        public string ServerURI { get { return "http://" + ExternalHostName + ":" + HttpPort; } }

        public GridRegion()
        {
            Flags = 0;
        }

        public GridRegion(RegionInfo ConvertFrom)
        {
            Flags = 0;
            RegionName = ConvertFrom.RegionName;
            RegionType = ConvertFrom.RegionType;
            RegionLocX = ConvertFrom.RegionLocX;
            RegionLocY = ConvertFrom.RegionLocY;
            RegionLocZ = ConvertFrom.RegionLocZ;
            InternalPort = ConvertFrom.InternalEndPoint.Port;
            ExternalHostName = MainServer.Instance.HostName.Replace("https://", "").Replace("http://", "");
            HttpPort = MainServer.Instance.Port;
            RegionID = ConvertFrom.RegionID;
            TerrainImage = ConvertFrom.RegionSettings.TerrainImageID;
            TerrainMapImage = ConvertFrom.RegionSettings.TerrainMapImageID;
            ParcelMapImage = ConvertFrom.RegionSettings.ParcelMapImageID;
            Access = ConvertFrom.AccessLevel;
            if (ConvertFrom.EstateSettings != null)
                EstateOwner = ConvertFrom.EstateSettings.EstateOwner;
            RegionSizeX = ConvertFrom.RegionSizeX;
            RegionSizeY = ConvertFrom.RegionSizeY;
            RegionSizeZ = ConvertFrom.RegionSizeZ;
            ScopeID = ConvertFrom.ScopeID;
            AllScopeIDs = ConvertFrom.AllScopeIDs;
            SessionID = ConvertFrom.GridSecureSessionID;
            Flags |= (int) RegionFlags.RegionOnline;
        }

        #region Definition of equality

        /// <summary>
        ///     Define equality as two regions having the same, non-zero UUID.
        /// </summary>
        public bool Equals(GridRegion region)
        {
            if (region == null)
                return false;
            // Return true if the non-zero UUIDs are equal:
            return (RegionID != UUID.Zero) && RegionID.Equals(region.RegionID);
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;
            return Equals(obj as GridRegion);
        }

        public override int GetHashCode()
        {
            return RegionID.GetHashCode() ^ TerrainImage.GetHashCode();
        }

        #endregion

        /// <value>
        ///     This accessor can throw all the exceptions that Dns.GetHostAddresses can throw.
        ///     XXX Isn't this really doing too much to be a simple getter, rather than an explict method?
        /// </value>
        public IPEndPoint ExternalEndPoint
        {
            get
            {
                if (m_remoteEndPoint == null && ExternalHostName != null)
                    m_remoteEndPoint = NetworkUtils.ResolveEndPoint(ExternalHostName, InternalPort);

                return m_remoteEndPoint;
            }
        }
        private IPEndPoint m_remoteEndPoint = null;

        public ulong RegionHandle
        {
            get { return Util.IntsToUlong(RegionLocX, RegionLocY); }
        }

        /// <summary>
        ///     Returns whether the grid coordinate is inside of this region
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool PointIsInRegion(int x, int y)
        {
            if (x > RegionLocX && y > RegionLocY &&
                x < RegionLocX + RegionSizeX &&
                y < RegionLocY + RegionSizeY)
                return true;
            return false;
        }

        #endregion

        #region IDataTransferable

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["uuid"] = RegionID;
            map["locX"] = RegionLocX;
            map["locY"] = RegionLocY;
            map["locZ"] = RegionLocZ;
            map["regionName"] = RegionName;
            map["regionType"] = RegionType;
            map["serverIP"] = ExternalHostName; //ExternalEndPoint.Address.ToString();
            map["serverHttpPort"] = HttpPort;
            map["serverPort"] = InternalPort;
            map["regionMapTexture"] = TerrainImage;
            map["regionTerrainTexture"] = TerrainMapImage;
            map["ParcelMapImage"] = ParcelMapImage;
            map["access"] = (int) Access;
            map["owner_uuid"] = EstateOwner;
            map["sizeX"] = RegionSizeX;
            map["sizeY"] = RegionSizeY;
            map["sizeZ"] = RegionSizeZ;
            map["LastSeen"] = LastSeen;
            map["SessionID"] = SessionID;
            map["ScopeID"] = ScopeID;
            map["AllScopeIDs"] = AllScopeIDs.ToOSDArray();
            map["Flags"] = Flags;
            map["EstateOwner"] = EstateOwner;

            // We send it along too so that it doesn't need resolved on the other end
            if (ExternalEndPoint != null)
            {
                map["remoteEndPointIP"] = ExternalEndPoint.Address.GetAddressBytes();
                map["remoteEndPointPort"] = ExternalEndPoint.Port;
            }

            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            if (map.ContainsKey("uuid"))
                RegionID = map["uuid"].AsUUID();

            if (map.ContainsKey("locX"))
                RegionLocX = map["locX"].AsInteger();

            if (map.ContainsKey("locY"))
                RegionLocY = map["locY"].AsInteger();

            if (map.ContainsKey("locZ"))
                RegionLocZ = map["locZ"].AsInteger();

            if (map.ContainsKey("regionName"))
                RegionName = map["regionName"].AsString();

            if (map.ContainsKey("regionType"))
                RegionType = map["regionType"].AsString();

            ExternalHostName = map.ContainsKey("serverIP") ? map["serverIP"].AsString() : "127.0.0.1";

            InternalPort = map["serverPort"].AsInteger();

            if (map.ContainsKey("serverHttpPort"))
            {
                UInt32 port = map["serverHttpPort"].AsUInteger();
                HttpPort = port;
            }

            if (map.ContainsKey("regionMapTexture"))
                TerrainImage = map["regionMapTexture"].AsUUID();

            if (map.ContainsKey("regionTerrainTexture"))
                TerrainMapImage = map["regionTerrainTexture"].AsUUID();

            if (map.ContainsKey("ParcelMapImage"))
                ParcelMapImage = map["ParcelMapImage"].AsUUID();

            if (map.ContainsKey("access"))
                Access = (byte) map["access"].AsInteger();

            if (map.ContainsKey("owner_uuid"))
                EstateOwner = map["owner_uuid"].AsUUID();

            if (map.ContainsKey("EstateOwner"))
                EstateOwner = map["EstateOwner"].AsUUID();

            if (map.ContainsKey("sizeX"))
                RegionSizeX = map["sizeX"].AsInteger();

            if (map.ContainsKey("sizeY"))
                RegionSizeY = map["sizeY"].AsInteger();

            if (map.ContainsKey("sizeZ"))
                RegionSizeZ = map["sizeZ"].AsInteger();

            if (map.ContainsKey("LastSeen"))
                LastSeen = map["LastSeen"].AsInteger();

            if (map.ContainsKey("SessionID"))
                SessionID = map["SessionID"].AsUUID();

            if (map.ContainsKey("Flags"))
                Flags = map["Flags"].AsInteger();

            if (map.ContainsKey("ScopeID"))
                ScopeID = map["ScopeID"].AsUUID();

            if (map.ContainsKey("AllScopeIDs"))
                AllScopeIDs = ((OSDArray) map["AllScopeIDs"]).ConvertAll<UUID>(o => o);

            if (map.ContainsKey("remoteEndPointIP"))
            {
                IPAddress add = new IPAddress(map["remoteEndPointIP"].AsBinary());
                int port = map["remoteEndPointPort"].AsInteger();
                m_remoteEndPoint = new IPEndPoint(add, port);
            }
        }

        #endregion
    }

    /// <summary>
    ///     The threat level enum
    ///     Tells how much we trust another host
    /// </summary>
    public enum ThreatLevel
    {
        None = 1,
        Low = 2,
        Medium = 4,
        High = 8,
        Full = 16
    }
}