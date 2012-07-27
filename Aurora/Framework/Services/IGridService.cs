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
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    public interface IGridService
    {
        IGridService InnerService { get; }

        /// <summary>
        /// The max size a region can be (meters)
        /// </summary>
        int GetMaxRegionSize();

        /// <summary>
        /// The size (in meters) of how far neighbors will be found
        /// </summary>
        int GetRegionViewSize();

        /// <summary>
        /// Register a region with the grid service.
        /// </summary>
        /// <param name="regionInfos"> </param>
        /// <param name="oldSessionID"></param>
        /// <param name="password"> </param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Thrown if region registration failed</exception>
        RegisterRegion RegisterRegion(GridRegion regionInfos, UUID oldSessionID, string password);

        /// <summary>
        /// Deregister a region with the grid service.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Thrown if region deregistration failed</exception>
        bool DeregisterRegion(GridRegion region);

        /// <summary>
        /// Get a specific region by UUID in the given scope
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="regionID"></param>
        /// <returns></returns>
        GridRegion GetRegionByUUID(List<UUID> scopeIDs, UUID regionID);

        /// <summary>
        /// Get the region at the given position (in meters)
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        GridRegion GetRegionByPosition(List<UUID> scopeIDs, int x, int y);

        /// <summary>
        /// Get the first returning region by name in the given scope
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="regionName"></param>
        /// <returns></returns>
        GridRegion GetRegionByName(List<UUID> scopeIDs, string regionName);

        /// <summary>
        /// Get information about regions starting with the provided name. 
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="name">
        /// The name to match against.
        /// </param>
        /// <returns>
        /// A list of <see cref="RegionInfo"/>s of regions with matching name. If the
        /// grid-server couldn't be contacted or returned an error, return null. 
        /// </returns>
        List<GridRegion> GetRegionsByName(List<UUID> scopeIDs, string name, uint? start, uint? count);

        /// <summary>
        /// Get number of regions starting with the provided name. 
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="name">
        /// The name to match against.
        /// </param>
        /// <returns>
        /// A the count of <see cref="RegionInfo"/>s of regions with matching name. If the
        /// grid-server couldn't be contacted or returned an error, returns 0. 
        /// </returns>
        uint GetRegionsByNameCount(List<UUID> scopeIDs, string name);

        /// <summary>
        /// Get all regions within the range of (xmin - xmax, ymin - ymax) (in meters)
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="xmin"></param>
        /// <param name="xmax"></param>
        /// <param name="ymin"></param>
        /// <param name="ymax"></param>
        /// <returns></returns>
        List<GridRegion> GetRegionRange(List<UUID> scopeIDs, int xmin, int xmax, int ymin, int ymax);

        /// <summary>
        /// Get all regions within the range of specified center.
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="centerX"></param>
        /// <param name="centerY"></param>
        /// <param name="squareRangeFromCenterInMeters"></param>
        /// <returns></returns>
        List<GridRegion> GetRegionRange(List<UUID> scopeIDs, float centerX, float centerY, uint squareRangeFromCenterInMeters);

        /// <summary>
        /// Get the neighbors of the given region
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        List<GridRegion> GetNeighbors(List<UUID> scopeIDs, GridRegion region);

        /// <summary>
        /// Get any default regions that have been set for users that are logging in that don't have a region to log into
        /// </summary>
        /// <param name="scopeID"></param>
        /// <returns></returns>
        List<GridRegion> GetDefaultRegions(List<UUID> scopeIDs);

        /// <summary>
        /// If all the default regions are down, find any fallback regions that have been set near x,y
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        List<GridRegion> GetFallbackRegions(List<UUID> scopeIDs, int x, int y);

        /// <summary>
        /// If there still are no regions after fallbacks have been checked, find any region near x,y
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        List<GridRegion> GetSafeRegions(List<UUID> scopeIDs, int x, int y);

        /// <summary>
        /// Get the current flags of the given region
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="regionID"></param>
        /// <returns></returns>
        int GetRegionFlags(List<UUID> scopeIDs, UUID regionID);

        /// <summary>
        /// Update the map of the given region if the sessionID is correct
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        string UpdateMap(GridRegion region);

        /// <summary>
        /// Get all map items of the given type for the given region
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="gridItemType"></param>
        /// <returns></returns>
        multipleMapItemReply GetMapItems (List<UUID> scopeIDs, ulong regionHandle, GridItemType gridItemType);

        /// <summary>
        /// The region (RegionID) has been determined to be unsafe, don't let agents log into it if no other region is found
        /// </summary>
        /// <param name="id"></param>
        void SetRegionUnsafe (UUID id);

        /// <summary>
        /// The region (RegionID) has been determined to be safe, allow agents to log into it again
        /// </summary>
        /// <param name="id"></param>
        void SetRegionSafe (UUID id);

        /// <summary>
        /// Verify the given SessionID for the given region
        /// </summary>
        /// <param name="r"></param>
        /// <param name="SessionID"></param>
        /// <returns></returns>
        bool VerifyRegionSessionID(GridRegion r, UUID SessionID);

        void Configure (Nini.Config.IConfigSource config, IRegistryCore registry);

        void Start (Nini.Config.IConfigSource config, IRegistryCore registry);

        void FinishedStartup ();
    }

    public class RegisterRegion : IDataTransferable
    {
        public string Error;
        public List<GridRegion> Neighbors = new List<GridRegion>();
        public UUID SessionID;
        public int RegionFlags = 0;
        public OSDMap Urls = new OSDMap();
        public OSDMap RegionRemote;
        public GridRegion Region;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Error"] = Error;
            map["Neighbors"] = new OSDArray(Neighbors.ConvertAll<OSD>((region) => region.ToOSD()));
            map["SessionID"] = SessionID;
            map["RegionFlags"] = RegionFlags;
            if (Urls != null)
                map["Urls"] = Urls;
            if (RegionRemote != null)
                map["RegionRemote"] = RegionRemote;
            if (Region != null)
                map["Region"] = Region.ToOSD();
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Error = map["Error"];
            OSDArray n = (OSDArray)map["Neighbors"];
            Neighbors = n.ConvertAll<GridRegion>((osd) => { GridRegion r = new GridRegion(); r.FromOSD((OSDMap)osd); return r; });
            SessionID = map["SessionID"];
            RegionFlags = map["RegionFlags"];
            if (map.ContainsKey("Urls"))
                Urls = (OSDMap)map["Urls"];
            if (map.ContainsKey("RegionRemote"))
                RegionRemote = (OSDMap)map["RegionRemote"];
            if (map.ContainsKey("Region"))
            {
                Region = new GridRegion();
                Region.FromOSD((OSDMap)map["Region"]);
            }
        }
    }

    public class GridRegion : AllScopeIDImpl
    {
        #region GridRegion

        /// <summary>
        /// The port by which http communication occurs with the region 
        /// </summary>
        public uint HttpPort
        {
            get { return m_httpPort; }
            set { m_httpPort = value; }
        }
        protected uint m_httpPort;

        /// <summary>
        /// A well-formed URI for the host region server (namely "http://" + ExternalHostName + : + HttpPort)
        /// </summary>
        public string ServerURI
        {
            get 
            {
                if(string.IsNullOrEmpty(m_serverURI))
                    return "http://" + ExternalHostName + ":" + HttpPort;
                return m_serverURI; 
            }
            set { m_serverURI = value; }
        }
        protected string m_serverURI;

        public string RegionName
        {
            get { return m_regionName; }
            set { m_regionName = value; }
        }
        protected string m_regionName = String.Empty;

        public string RegionType
        {
            get { return m_regionType; }
            set { m_regionType = value; }
        }
        protected string m_regionType = String.Empty;

        public int RegionLocX
        {
            get { return m_regionLocX; }
            set { m_regionLocX = value; }
        }
        protected int m_regionLocX;

        public int RegionLocY
        {
            get { return m_regionLocY; }
            set { m_regionLocY = value; }
        }
        protected int m_regionLocY;

        public int RegionLocZ
        {
            get { return m_regionLocZ; }
            set { m_regionLocZ = value; }
        }
        protected int m_regionLocZ;

        protected UUID m_estateOwner;

        public UUID EstateOwner
        {
            get { return m_estateOwner; }
            set { m_estateOwner = value; }
        }

        public int RegionSizeX
        {
            get { return m_RegionSizeX; }
            set { m_RegionSizeX = value; }
        }

        public int RegionSizeY
        {
            get { return m_RegionSizeY; }
            set { m_RegionSizeY = value; }
        }

        public int RegionSizeZ
        {
            get { return m_RegionSizeZ; }
        }

        public int Flags { get; set; }

        public UUID SessionID
        {
            get { return m_SessionID; }
            set { m_SessionID = value; }
        }

        private int m_RegionSizeX = 256;
        private int m_RegionSizeY = 256;
        private int m_RegionSizeZ = 256;
        public UUID RegionID = UUID.Zero;
        private UUID m_SessionID = UUID.Zero;

        public UUID TerrainImage = UUID.Zero;
        public UUID TerrainMapImage = UUID.Zero;
        public UUID ParcelMapImage = UUID.Zero;
        public byte Access;
        public string AuthToken = string.Empty;
        private IPEndPoint m_remoteEndPoint = null;
        protected string m_externalHostName;
        protected IPEndPoint m_internalEndPoint;
        public int LastSeen = 0;
        protected OSDMap m_genericMap = new OSDMap();
        public OSDMap GenericMap
        {
            get { return m_genericMap; }
            set { m_genericMap = value; }
        }

        public bool IsOnline
        {
            get { return (Flags & (int)Aurora.Framework.RegionFlags.RegionOnline) == 1; }
            set
            {
                if (value)
                    Flags |= (int)Aurora.Framework.RegionFlags.RegionOnline;
                else
                    Flags &= (int)Aurora.Framework.RegionFlags.RegionOnline;
            }

        }

        public GridRegion()
        {
            Flags = 0;
        }

        public GridRegion(RegionInfo ConvertFrom)
        {
            Flags = 0;
            m_regionName = ConvertFrom.RegionName;
            m_regionType = ConvertFrom.RegionType;
            m_regionLocX = ConvertFrom.RegionLocX;
            m_regionLocY = ConvertFrom.RegionLocY;
            m_regionLocZ = ConvertFrom.RegionLocZ;
            m_internalEndPoint = ConvertFrom.InternalEndPoint;
            m_externalHostName = MainServer.Instance.HostName;
            m_externalHostName = m_externalHostName.Replace("https://", "");
            m_externalHostName = m_externalHostName.Replace("http://", "");
            m_httpPort = MainServer.Instance.Port;
            RegionID = ConvertFrom.RegionID;
            ServerURI = MainServer.Instance.ServerURI;
            TerrainImage = ConvertFrom.RegionSettings.TerrainImageID;
            TerrainMapImage = ConvertFrom.RegionSettings.TerrainMapImageID;
            ParcelMapImage = ConvertFrom.RegionSettings.ParcelMapImageID;
            Access = ConvertFrom.AccessLevel;
            if(ConvertFrom.EstateSettings != null)
                EstateOwner = ConvertFrom.EstateSettings.EstateOwner;
            m_RegionSizeX = ConvertFrom.RegionSizeX;
            m_RegionSizeY = ConvertFrom.RegionSizeY;
            m_RegionSizeZ = ConvertFrom.RegionSizeZ;
            ScopeID = ConvertFrom.ScopeID;
            AllScopeIDs = ConvertFrom.AllScopeIDs;
            SessionID = ConvertFrom.GridSecureSessionID;
            Flags |= (int)Aurora.Framework.RegionFlags.RegionOnline;
        }

        #region Definition of equality

        /// <summary>
        /// Define equality as two regions having the same, non-zero UUID.
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
        /// This accessor can throw all the exceptions that Dns.GetHostAddresses can throw.
        ///
        /// XXX Isn't this really doing too much to be a simple getter, rather than an explict method?
        /// </value>
        public IPEndPoint ExternalEndPoint
        {
            get
            {
                if (m_remoteEndPoint == null && m_externalHostName != null && m_internalEndPoint != null)
                    m_remoteEndPoint = NetworkUtils.ResolveEndPoint(m_externalHostName, m_internalEndPoint.Port);

                return m_remoteEndPoint;
            }
        }

        public string ExternalHostName
        {
            get { return m_externalHostName; }
            set { m_externalHostName = value; }
        }

        public IPEndPoint InternalEndPoint
        {
            get { return m_internalEndPoint; }
            set { m_internalEndPoint = value; }
        }

        public ulong RegionHandle
        {
            get { return Util.IntsToUlong(RegionLocX, RegionLocY); }
            set
            {
                Util.UlongToInts(value, out m_regionLocX, out m_regionLocY);
            }
        }

        /// <summary>
        /// Returns whether the grid coordinate is inside of this region
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
            map["serverURI"] = ServerURI;
            if(InternalEndPoint != null)
                map["serverPort"] = InternalEndPoint.Port;
            map["regionMapTexture"] = TerrainImage;
            map["regionTerrainTexture"] = TerrainMapImage;
            map["ParcelMapImage"] = ParcelMapImage;
            map["access"] = (int)Access;
            map["owner_uuid"] = EstateOwner;
            map["AuthToken"] = AuthToken;
            map["sizeX"] = RegionSizeX;
            map["sizeY"] = RegionSizeY;
            map["sizeZ"] = RegionSizeZ;
            map["LastSeen"] = LastSeen;
            map["SessionID"] = SessionID;
            map["ScopeID"] = ScopeID;
            map["AllScopeIDs"] = AllScopeIDs.ToOSDArray();
            map["Flags"] = Flags;
            map["GenericMap"] = GenericMap;
            map["EstateOwner"] = EstateOwner;

            // We send it along too so that it doesn't need resolved on the other end
            if (ExternalEndPoint != null)
            {
                map["remoteEndPointIP"] = ExternalEndPoint.Address.GetAddressBytes ();
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

            if (map.ContainsKey("serverPort"))
            {
                Int32 port = map["serverPort"].AsInteger();
                InternalEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port);
            }

            if (map.ContainsKey("serverHttpPort"))
            {
                UInt32 port = map["serverHttpPort"].AsUInteger();
                HttpPort = port;
            }

            if (map.ContainsKey("serverURI"))
                ServerURI = map["serverURI"];

            if (map.ContainsKey("regionMapTexture"))
                TerrainImage = map["regionMapTexture"].AsUUID();

            if (map.ContainsKey("regionTerrainTexture"))
                TerrainMapImage = map["regionTerrainTexture"].AsUUID();

            if (map.ContainsKey("ParcelMapImage"))
                ParcelMapImage = map["ParcelMapImage"].AsUUID();

            if (map.ContainsKey("access"))
                Access = (byte)map["access"].AsInteger();

            if (map.ContainsKey("owner_uuid"))
                EstateOwner = map["owner_uuid"].AsUUID();

            if (map.ContainsKey("EstateOwner"))
                EstateOwner = map["EstateOwner"].AsUUID();

            if (map.ContainsKey("AuthToken"))
                AuthToken = map["AuthToken"].AsString();

            if (map.ContainsKey("sizeX"))
                m_RegionSizeX = map["sizeX"].AsInteger();

            if (map.ContainsKey("sizeY"))
                m_RegionSizeY = map["sizeY"].AsInteger();

            if (map.ContainsKey("sizeZ"))
                m_RegionSizeZ = map["sizeZ"].AsInteger();

            if (map.ContainsKey("LastSeen"))
                LastSeen = map["LastSeen"].AsInteger();

            if (map.ContainsKey("SessionID"))
                SessionID = map["SessionID"].AsUUID();

            if (map.ContainsKey("Flags"))
                Flags = map["Flags"].AsInteger();

            if (map.ContainsKey("ScopeID"))
                ScopeID = map["ScopeID"].AsUUID();

            if (map.ContainsKey("AllScopeIDs"))
                AllScopeIDs = ((OSDArray)map["AllScopeIDs"]).ConvertAll<UUID>(o => o);

            if (map.ContainsKey("GenericMap"))
                GenericMap = (OSDMap)map["GenericMap"];

            if (map.ContainsKey("remoteEndPointIP"))
            {
                IPAddress add = new IPAddress(map["remoteEndPointIP"].AsBinary());
                int port = map["remoteEndPointPort"].AsInteger();
                m_remoteEndPoint = new IPEndPoint(add, port);
            }
        }

        public override Dictionary<string, object> ToKVP()
        {
            return Util.OSDToDictionary(ToOSD());
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }


        #endregion
    }

    /// <summary>
    /// This is the main service that collects URLs for registering clients.
    /// Call this if you want to get secure URLs for the given SessionID
    /// </summary>
    public interface IGridRegistrationService
    {
        /// <summary>
        /// Time before handlers will need to reregister (in hours)
        /// </summary>
        float ExpiresTime { get; }

        /// <summary>
        /// Gets a list of secure URLs for the given RegionHandle and SessionID
        /// </summary>
        /// <param name="SessionID"></param>
        /// <returns></returns>
        OSDMap GetUrlForRegisteringClient(string SessionID);

        /// <summary>
        /// Registers a module that will be requested when GetUrlForRegisteringClient is called
        /// </summary>
        /// <param name="module"></param>
        void RegisterModule(IGridRegistrationUrlModule module);

        /// <summary>
        /// Remove the URLs for the given region
        /// </summary>
        /// <param name="SessionID"></param>
        void RemoveUrlsForClient(string SessionID);

        /// <summary>
        /// Checks that the given client can access the function that it is calling
        /// </summary>
        /// <param name="SessionID"></param>
        /// <param name="function"></param>
        /// <param name="defaultThreatLevel"></param>
        /// <returns></returns>
        bool CheckThreatLevel(string SessionID, string function, ThreatLevel defaultThreatLevel);

        /// <summary>
        /// Updates the time so that the region does not timeout
        /// </summary>
        /// <param name="p"></param>
        void UpdateUrlsForClient(string SessionID);
		OSDMap RegionRemoteHandlerURL(GridRegion regionInfo, UUID sessionID, UUID oldSessionID);
    }

    /// <summary>
    /// The threat level enum
    /// Tells how much we trust another host
    /// </summary>
    public enum ThreatLevel
    {
        None = 1,
        Low = 2,
        Medium = 4,
        High = 8,
        Full = 16
    }

    /// <summary>
    /// This is the sub service of the IGridRegistrationService that is implemented by other modules
    ///   so that they can be queried for URLs to return.
    /// </summary>
    public interface IGridRegistrationUrlModule
    {
        /// <summary>
        /// Name of the Url
        /// </summary>
        string UrlName { get; }

        /// <summary>
        /// Give the region all of the ports assigned for this module
        /// </summary>
        bool DoMultiplePorts { get; }

        /// <summary>
        /// Get the Url for the given sessionID
        /// </summary>
        /// <param name="SessionID"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        string GetUrlForRegisteringClient (string SessionID, uint port);

        /// <summary>
        /// Adds an existing URL to the module for the given SessionID and RegionHandle
        /// </summary>
        /// <param name="SessionID"></param>
        /// <param name="url"></param>
        /// <param name="port"></param>
        void AddExistingUrlForClient (string SessionID, string url, uint port);

        /// <summary>
        /// Removes the given region from the http server so that the URLs cannot be used anymore
        /// </summary>
        /// <param name="sessionID"></param>
        /// <param name="url"></param>
        /// <param name="port"></param>
        void RemoveUrlForClient (string sessionID, string url, uint port);
    }
}
