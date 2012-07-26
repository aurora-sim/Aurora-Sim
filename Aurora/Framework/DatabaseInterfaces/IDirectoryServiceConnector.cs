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

using OpenMetaverse;
using EventFlags = OpenMetaverse.DirectoryManager.EventFlags;

using Aurora.Framework;

namespace Aurora.Framework
{
    public interface IDirectoryServiceConnector : IAuroraDataPlugin
    {
        #region Regions

        /// <summary>
        ///   Adds a region into search
        /// </summary>
        /// <param name = "args"></param>
        void AddRegion(List<LandData> args);

        /// <summary>
        ///   Removes a region from search
        /// </summary>
        /// <param name = "regionID"></param>
        /// <param name = "args"></param>
        void ClearRegion(UUID regionID);

        #endregion

        #region Parcels

        /// <summary>
        ///   Gets a parcel from the search database by Info UUID (the true cross instance parcel ID)
        /// </summary>
        /// <param name = "ParcelID"></param>
        /// <returns></returns>
        LandData GetParcelInfo(UUID ParcelID);

        /// <summary>
        /// Gets the first parcel from the search database in the specified region with the specified name
        /// </summary>
        /// <param name="RegionID"></param>
        /// <param name="ParcelName"></param>
        /// <returns></returns>
        LandData GetParcelInfo(UUID RegionID, string ParcelName);

        /// <summary>
        ///   Gets all parcels owned by the given user
        /// </summary>
        /// <param name = "OwnerID"></param>
        /// <returns></returns>
        List<ExtendedLandData> GetParcelByOwner(UUID OwnerID);

        /// <summary>
        /// Gets all parcels in a region, optionally filtering by owner, parcel flags and category.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <param name="RegionID"></param>
        /// <param name="scopeID"></param>
        /// <param name="owner"></param>
        /// <param name="flags"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        List<LandData> GetParcelsByRegion(uint start, uint count, UUID RegionID, UUID owner, ParcelFlags flags, ParcelCategory category);

        /// <summary>
        /// Get the number of parcels in the specified region that match the specified filters.
        /// </summary>
        /// <param name="RegionID"></param>
        /// <param name="scopeID"></param>
        /// <param name="owner"></param>
        /// <param name="flags"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        uint GetNumberOfParcelsByRegion(UUID RegionID, UUID owner, ParcelFlags flags, ParcelCategory category);

        /// <summary>
        /// Get a list of parcels in a region with the specified name.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <param name="RegionID"></param>
        /// <param name="ScopeID"></param>
        /// <param name="name"></param>
        /// <param name="flags"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        List<LandData> GetParcelsWithNameByRegion(uint start, uint count, UUID RegionID, string name);

        /// <summary>
        /// Get the number of parcels in the specified region with the specified name
        /// </summary>
        /// <param name="RegionID"></param>
        /// <param name="ScopeID"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        uint GetNumberOfParcelsWithNameByRegion(UUID RegionID, string name);

        /// <summary>
        ///   Searches for parcels around the grid
        /// </summary>
        /// <param name = "queryText"></param>
        /// <param name = "category"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        List<DirPlacesReplyData> FindLand(string queryText, string category, int StartQuery, uint Flags, UUID scopeID);

        /// <summary>
        ///   Searches for parcels for sale around the grid
        /// </summary>
        /// <param name = "searchType"></param>
        /// <param name = "price"></param>
        /// <param name = "area"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        List<DirLandReplyData> FindLandForSale(string searchType, uint price, uint area, int StartQuery, uint Flags, UUID scopeID);

        /// <summary>
        ///   Searches for parcels for sale around the grid
        /// </summary>
        /// <param name = "searchType"></param>
        /// <param name = "price"></param>
        /// <param name = "area"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        List<DirLandReplyData> FindLandForSaleInRegion(string searchType, uint price, uint area, int StartQuery, uint Flags, UUID regionID);

        /// <summary>
        ///   Searches for the most popular places around the grid
        /// </summary>
        /// <param name = "searchType"></param>
        /// <param name = "price"></param>
        /// <param name = "area"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        List<DirPopularReplyData> FindPopularPlaces(uint queryFlags, UUID scopeID);

        #endregion

        #region Classifieds

        /// <summary>
        ///   Searches for classifieds
        /// </summary>
        /// <param name = "queryText"></param>
        /// <param name = "category"></param>
        /// <param name = "queryFlags"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        List<DirClassifiedReplyData> FindClassifieds(string queryText, string category, uint queryFlags, int StartQuery, UUID scopeID);

        /// <summary>
        ///   Gets all classifieds in the given region
        /// </summary>
        /// <param name = "regionName"></param>
        /// <returns></returns>
        List<Classified> GetClassifiedsInRegion(string regionName);

        Classified GetClassifiedByID(UUID id);

        #endregion

        #region Events

        /// <summary>
        ///   Searches for events with the given parameters
        /// </summary>
        /// <param name = "queryText"></param>
        /// <param name = "flags"></param>
        /// <param name = "StartQuery"></param>
        /// <returns></returns>
        List<DirEventsReplyData> FindEvents(string queryText, uint flags, int StartQuery, UUID scopeID);

        /// <summary>
        ///   Retrives all events in the given region by their maturity level
        /// </summary>
        /// <param name = "regionName"></param>
        /// <param name = "maturity">Uses DirectoryManager.EventFlags to determine the maturity requested</param>
        /// <returns></returns>
        List<DirEventsReplyData> FindAllEventsInRegion(string regionName, int maturity);

        /// <summary>
        ///   Gets more info about the event by the events unique event ID
        /// </summary>
        /// <param name = "EventID"></param>
        /// <returns></returns>
        EventData GetEventInfo(uint EventID);


        /// <summary>
        /// creates an event
        /// </summary>
        /// <param name="creator"></param>
        /// <param name="region"></param>
        /// <param name="parcel"></param>
        /// <param name="date"></param>
        /// <param name="cover"></param>
        /// <param name="maturity"></param>
        /// <param name="flags"></param>
        /// <param name="duration"></param>
        /// <param name="localPos"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        EventData CreateEvent(UUID creator, UUID region, UUID parcel, DateTime date, uint cover, EventFlags maturity, uint flags, uint duration, Vector3 localPos, string name, string description, string category);

        /// <summary>
        /// Gets a list of events with optional filters
        /// </summary>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <param name="sort"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        List<EventData> GetEvents(uint start, uint count, Dictionary<string, bool> sort, Dictionary<string, object> filter);

        /// <summary>
        /// Get the number of events matching the specified filters
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        uint GetNumberOfEvents(Dictionary<string, object> filter);

        /// <summary>
        /// Gets the highest event ID
        /// </summary>
        /// <returns></returns>
        uint GetMaxEventID();

        #endregion
    }
}