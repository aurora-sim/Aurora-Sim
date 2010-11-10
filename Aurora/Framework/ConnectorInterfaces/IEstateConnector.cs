using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public interface IEstateConnector : IAuroraDataPlugin
	{
        /// <summary>
        /// Loads the estate data for the given region
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
		EstateSettings LoadEstateSettings(UUID regionID);

        /// <summary>
        /// Loads the estate data for the given estate ID
        /// </summary>
        /// <param name="estateID"></param>
        /// <returns></returns>
		EstateSettings LoadEstateSettings(int estateID);

        /// <summary>
        /// Updates the given Estate data in the database
        /// </summary>
        /// <param name="es"></param>
		void SaveEstateSettings(EstateSettings es);

        /// <summary>
        /// Gets the estates that have the given name
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
		List<int> GetEstates(string name);

        /// <summary>
        /// Add a new region to the estate, authenticates with the password
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="estateID"></param>
        /// <param name="password"></param>
        /// <returns></returns>
		bool LinkRegion(UUID regionID, int estateID, string password);

        /// <summary>
        /// Deletes the given estate by its estate ID, must be authenticated with the password
        /// </summary>
        /// <param name="estateID"></param>
        /// <param name="password"></param>
        /// <returns></returns>
		bool DeleteEstate(int estateID, string password);

        /// <summary>
        /// Creates a new estate from the given info, returns the updated info
        /// </summary>
        /// <param name="ES"></param>
        /// <param name="RegionID"></param>
        /// <returns></returns>
        EstateSettings CreateEstate(EstateSettings ES, UUID RegionID);
    }
}
