using System.Collections.Generic;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IRegionManagement
    {
        /// <summary>
        /// Gets whether a region is online or not
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        bool GetWhetherRegionIsOnline(UUID regionID);

        /// <summary>
        /// Starts an existing region
        /// </summary>
        /// <param name="region"></param>
        void StartRegion();

        /// <summary>
        /// Attempts to stop the currently running region
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="secondsBeforeShutdown"></param>
        /// <returns></returns>
        bool StopRegion(UUID regionID, int secondsBeforeShutdown);

        /// <summary>
        /// Clears all objects from the region
        /// </summary>
        /// <param name="regionID"></param>
        void ResetRegion(UUID regionID);

        /// <summary>
        /// Deletes an active region's data from the database
        /// </summary>
        /// <param name="regionID"></param>
        void DeleteRegion(UUID regionID);

        /// <summary>
        /// Returns a region with the given UUID
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        RegionInfo GetRegionInfo(UUID regionID);

        /// <summary>
        /// Get an HTML page that allows for the get/set of OpenRegionSettings for the region
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        string GetOpenRegionSettingsHTMLPage(UUID regionID);

        /// <summary>
        /// Checks whether remote connections are working
        /// </summary>
        /// <returns></returns>
        bool ConnectionIsWorking();

        /// <summary>
        /// Gets a list of all estates that the user has
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        List<string> GetEstatesForUser(string name);

        /// <summary>
        /// Changes the current estate of the region given
        /// </summary>
        /// <param name="ownerName"></param>
        /// <param name="estateToJoin"></param>
        /// <param name="regionID"></param>
        void ChangeEstate(string ownerName, string estateToJoin, UUID regionID);

        /// <summary>
        /// Create a new estate with the given information
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="estateName"></param>
        /// <param name="ownerName"></param>
        /// <returns></returns>
        bool CreateNewEstate(UUID regionID, string estateName, string ownerName);

        /// <summary>
        /// Get the current estate name for the region
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        string GetCurrentEstate(UUID regionID);

        /// <summary>
        /// Get the estate owner's name for the given region
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        string GetEstateOwnerName(UUID regionID);

        /// <summary>
        /// Gets all users with the given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        List<UserAccount> GetUserAccounts(string name);

        /// <summary>
        /// Creates a user for the grid
        /// </summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <param name="email"></param>
        /// <param name="userID"></param>
        /// <param name="scopeID"></param>
        void CreateUser(string name, string password, string email, UUID userID, UUID scopeID);
    }
}
