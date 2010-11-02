using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public interface IDirectoryServiceConnector
	{
        /// <summary>
        /// Adds a region into search
        /// </summary>
        /// <param name="args"></param>
        /// <param name="regionID"></param>
        /// <param name="forSale"></param>
        /// <param name="EstateID"></param>
        /// <param name="showInSearch"></param>
        /// <param name="InfoUUID"></param>
		void AddLandObject(LandData args);
        
        /// <summary>
        /// Gets a parcel from the search database by Info UUID (the true cross instance parcel ID)
        /// </summary>
        /// <param name="ParcelID"></param>
        /// <returns></returns>
        LandData GetParcelInfo(UUID ParcelID);

        /// <summary>
        /// Gets all parcels owned by the given user
        /// </summary>
        /// <param name="OwnerID"></param>
        /// <returns></returns>
        LandData[] GetParcelByOwner(UUID OwnerID);

        /// <summary>
        /// Searches for parcels around the grid
        /// </summary>
        /// <param name="queryText"></param>
        /// <param name="category"></param>
        /// <param name="StartQuery"></param>
        /// <returns></returns>
        DirPlacesReplyData[] FindLand(string queryText, string category, int StartQuery, uint Flags);
		
        /// <summary>
        /// Searches for parcels for sale around the grid
        /// </summary>
        /// <param name="searchType"></param>
        /// <param name="price"></param>
        /// <param name="area"></param>
        /// <param name="StartQuery"></param>
        /// <returns></returns>
        DirLandReplyData[] FindLandForSale(string searchType, string price, string area, int StartQuery, uint Flags);
		
        /// <summary>
        /// Searches for events with the given parameters
        /// </summary>
        /// <param name="queryText"></param>
        /// <param name="flags"></param>
        /// <param name="StartQuery"></param>
        /// <returns></returns>
        DirEventsReplyData[] FindEvents(string queryText, string flags, int StartQuery);
		
        /// <summary>
        /// Retrives all events in the given region
        /// </summary>
        /// <param name="regionName"></param>
        /// <returns></returns>
        DirEventsReplyData[] FindAllEventsInRegion(string regionName);

        /// <summary>
        /// Searches for classifieds
        /// </summary>
        /// <param name="queryText"></param>
        /// <param name="category"></param>
        /// <param name="queryFlags"></param>
        /// <param name="StartQuery"></param>
        /// <returns></returns>
		DirClassifiedReplyData[] FindClassifieds(string queryText, string category, string queryFlags, int StartQuery);
		
        /// <summary>
        /// Gets more info about the event by the events unique event ID
        /// </summary>
        /// <param name="EventID"></param>
        /// <returns></returns>
        EventData GetEventInfo(string EventID);

        /// <summary>
        /// Gets all classifieds in the given region
        /// </summary>
        /// <param name="regionName"></param>
        /// <returns></returns>
		Classified[] GetClassifiedsInRegion(string regionName);
	}
}
