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
using OpenMetaverse;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Framework
{
    /// <summary>
    ///   An interface for connecting to the authentication datastore
    /// </summary>
    public interface IRegionData : IAuroraDataPlugin
    {
        GridRegion Get(UUID regionID, List<UUID> scopeIDs);
        List<GridRegion> Get(string regionName, List<UUID> scopeIDs, uint? start, uint? count);
        uint GetCount(string regionName, List<UUID> scopeIDs);
        GridRegion GetZero(int x, int y, List<UUID> scopeIDs);
        List<GridRegion> Get(int x, int y, List<UUID> scopeIDs);
        List<GridRegion> Get(RegionFlags regionFlags);
        List<GridRegion> Get(int xStart, int yStart, int xEnd, int yEnd, List<UUID> scopeIDs);
        List<GridRegion> Get(RegionFlags flags, Dictionary<string, bool> sort);
        List<GridRegion> Get(uint start, uint count, uint EstateID, RegionFlags flags, Dictionary<string, bool> sort);
        List<GridRegion> Get(RegionFlags includeFlags, RegionFlags excludeFlags, uint? start, uint? count, Dictionary<string, bool> sort);

        /// <summary>
        /// Gets the number of regions matching the specified flags
        /// </summary>
        /// <param name="includeFlags"></param>
        /// <param name="excludeFlags"></param>
        /// <returns></returns>
        uint Count(RegionFlags includeFlags, RegionFlags excludeFlags);

        /// <summary>
        /// Gets the neighbouring regions, taking into account variable-sized regions
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="ScopeID"></param>
        /// <param name="squareRangeFromCenterInMeters">because calculating circular radii would be a complex.</param>
        /// <returns>If the return result is of zero length the region does not exist.</returns>
        List<GridRegion> GetNeighbours(UUID regionID, List<UUID> scopeIDs, uint squareRangeFromCenterInMeters);

        /// <summary>
        /// Gets all regions within squareRangeFromCenterInMeters meters of centerX and centerY
        /// </summary>
        /// <param name="centerX"></param>
        /// <param name="centerY"></param>
        /// <param name="squareRangeFromCenterInMeters"></param>
        /// <returns></returns>
        List<GridRegion> Get(List<UUID> scopeIDs, UUID excludeRegion, float centerX, float centerY, uint squareRangeFromCenterInMeters);

        uint Count(uint EstateID, RegionFlags flags);

        bool Store(GridRegion data);

        bool Delete(UUID regionID);
        bool DeleteAll(string[] criteriaKey, object[] criteriaValue);

        List<GridRegion> GetDefaultRegions(List<UUID> scopeIDs);
        List<GridRegion> GetFallbackRegions(List<UUID> scopeIDs, int x, int y);
        List<GridRegion> GetSafeRegions(List<UUID> scopeIDs, int x, int y);
    }

    [Flags]
    public enum RegionFlags
    {
        DefaultRegion = 1, // Used for new Rez. Random if multiple defined
        FallbackRegion = 2, // Regions we redirect to when the destination is down
        RegionOnline = 4, // Set when a region comes online, unset when it unregisters and DeleteOnUnregister is false
        NoDirectLogin = 8, // Region unavailable for direct logins (by name)
        Persistent = 16, // Don't remove on unregister
        LockedOut = 32, // Don't allow registration
        NoMove = 64, // Don't allow moving this region
        Reservation = 128, // This is an inactive reservation
        Authenticate = 256, // Require authentication
        Hyperlink = 512, // Record represents a HG link
        Hidden = 1024, //Hides the sim except for those on the access list
        Safe = 2048, //Safe to login agents to
        Prelude = 4096, //Starting region that you can only go to once
        Foreign = 8192 //Region is not in this grid
    }
}