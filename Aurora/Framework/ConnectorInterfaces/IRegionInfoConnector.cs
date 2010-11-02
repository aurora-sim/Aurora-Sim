using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using OpenMetaverse;
using OpenSim.Framework;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public interface IRegionInfoConnector
	{
        /// <summary>
        /// Gets RegionInfos for the database region connector
        /// </summary>
        /// <returns></returns>
        RegionInfo[] GetRegionInfos();
        
        /// <summary>
        /// Gets a specific region info for the given region ID
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        RegionInfo GetRegionInfo(UUID regionID);

        /// <summary>
        /// Gets a specific region info for the given region name
        /// </summary>
        /// <param name="regionName"></param>
        /// <returns></returns>
        RegionInfo GetRegionInfo(string regionName);

        /// <summary>
        /// Updates the region info for the given region
        /// </summary>
        /// <param name="region"></param>
        /// <param name="Disabled"></param>
        void UpdateRegionInfo(RegionInfo region, bool Disabled);

        /// <summary>
        /// Loads stored WindLight settings for the given region
        /// </summary>
        /// <param name="regionUUID"></param>
        /// <returns></returns>
        Dictionary<float, RegionLightShareData> LoadRegionWindlightSettings(UUID regionUUID);

        /// <summary>
        /// Stores WindLight settings for the given region
        /// </summary>
        /// <param name="wl"></param>
        void StoreRegionWindlightSettings(UUID RegionID, UUID ID, RegionLightShareData lsd);
	}
}
