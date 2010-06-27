using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.DataManager;

namespace Aurora.Services.DataService
{
    public class LocalSimMapConnector : ISimMapDataConnector
	{
		private IGenericData GD = null;
        public LocalSimMapConnector(IGenericData GenericData)
        {
            GD = GenericData;
		}

		/// <summary>
		/// Gets the region's SimMap
		/// </summary>
		/// <param name="regionID"></param>
		/// <returns></returns>
		public SimMap GetSimMap(UUID regionID)
		{
			List<string> retval = GD.Query("RegionID", regionID, "simmap", "*");
            
            if (retval.Count == 0)
                return null;
            
            SimMap map = new SimMap();
            map.RegionID = UUID.Parse(retval[0]);
            map.RegionHandle = ulong.Parse(retval[1]);
            map.EstateID = uint.Parse(retval[2]);
            map.RegionLocX = int.Parse(retval[3]);
            map.RegionLocY = int.Parse(retval[4]);
            map.SimMapTextureID = UUID.Parse(retval[5]);
            map.RegionName = retval[6];
            map.RegionFlags = uint.Parse(retval[7]);
            map.Access = uint.Parse(retval[8]);
            map.SimFlags = (SimMapFlags)int.Parse(retval[9]);
            map.RegionType = retval[10];

            return map;
		}

        /// <summary>
        /// Gets the region's SimMap
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        public SimMap GetSimMap(ulong regionHandle)
        {
            List<string> retval = GD.Query("RegionHandle", regionHandle, "simmap", "*");

            if (retval.Count == 0)
                return null;

            SimMap map = new SimMap();
            map.RegionID = UUID.Parse(retval[0]);
            map.RegionHandle = ulong.Parse(retval[1]);
            map.EstateID = uint.Parse(retval[2]);
            map.RegionLocX = int.Parse(retval[3]);
            map.RegionLocY = int.Parse(retval[4]);
            map.SimMapTextureID = UUID.Parse(retval[5]);
            map.RegionName = retval[6];
            map.RegionFlags = uint.Parse(retval[7]);
            map.Access = uint.Parse(retval[8]);
            map.SimFlags = (SimMapFlags)int.Parse(retval[9]);
            map.RegionType = retval[10];

            return map;
        }

        /// <summary>
        /// Gets the region's SimMap
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        public SimMap GetSimMap(int regionX, int regionY)
        {
            List<string> retval = GD.Query(new string[]{"RegionLocX","RegionLocY"}, new object[]{regionX,regionY}, "simmap", "*");

            if (retval.Count == 0)
                return null;

            SimMap map = new SimMap();
            map.RegionID = UUID.Parse(retval[0]);
            map.RegionHandle = ulong.Parse(retval[1]);
            map.EstateID = uint.Parse(retval[2]);
            map.RegionLocX = int.Parse(retval[3]);
            map.RegionLocY = int.Parse(retval[4]);
            map.SimMapTextureID = UUID.Parse(retval[5]);
            map.RegionName = retval[6];
            map.RegionFlags = uint.Parse(retval[7]);
            map.Access = uint.Parse(retval[8]);
            map.SimFlags = (SimMapFlags)int.Parse(retval[9]);
            map.RegionType = retval[10];

            return map;
        }

		/// <summary>
		/// Updates the region's SimMap
		/// </summary>
		/// <param name="regionID"></param>
		/// <param name="flags"></param>
		public void SetSimMap(SimMap map)
		{
            if (GetSimMap(map.RegionID) == null)
            {
                GD.Insert("simmap", new object[]{map.RegionID, map.RegionHandle, map.EstateID,
                    map.RegionLocX, map.RegionLocY, map.SimMapTextureID,
                    map.RegionName, map.RegionFlags, map.Access,
                    map.SimFlags, map.RegionType});
            }
            else
            {
                GD.Update("simmap", new object[] { map.RegionHandle, map.EstateID,
                map.RegionLocX, map.RegionLocY, map.SimMapTextureID,
                map.RegionName, map.RegionFlags, map.Access,
                map.SimFlags, map.RegionType }, new string[] { "RegionHandle", "EstateID", "RegionLocX",
                "RegionLocY", "SimMapTextureID", "RegionName",
                "RegionFlags", "Access", "GridRegionFlags", "RegionType" },
                    new string[] { "RegionID" }, new object[] { map.RegionID });
            }
		}
	}
}
