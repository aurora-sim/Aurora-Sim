using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public interface IParcelServiceConnector
    {
        /// <summary>
        /// Stores the changes to the parcel
        /// </summary>
        /// <param name="args"></param>
        void StoreLandObject(LandData args);

        /// <summary>
        /// Gets the parcel data for the given parcel in the region
        /// </summary>
        /// <param name="ParcelID"></param>
        /// <returns></returns>
        LandData GetLandData(UUID RegionID, UUID ParcelID);

        /// <summary>
        /// Loads all parcels by region
        /// </summary>
        /// <param name="regionUUID"></param>
        /// <returns></returns>
        List<LandData> LoadLandObjects(UUID regionUUID);

        /// <summary>
        /// Removes a parcel
        /// </summary>
        /// <param name="ParcelID"></param>
        void RemoveLandObject(UUID RegionID, UUID ParcelID);
    }
}
