using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.DataManager;

namespace Aurora.Services.DataService
{
    public class LocalGridConnector : IRegionConnector, ISimMapDataConnector
	{
		private IGenericData GD = null;
		public LocalGridConnector()
		{
			GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
		}

		/// <summary>
		/// Gets the region's SimMap
		/// </summary>
		/// <param name="regionID"></param>
		/// <returns></returns>
		public SimMap GetSimMap(UUID regionID)
		{
			List<string> retval = GD.Query("RegionID", regionID, "simmap", "*");
            
            if (retval.Count == 1)
                return null;
            
            SimMap map = new SimMap();
            map.RegionID = UUID.Parse(retval[0]);
            map.EstateID = uint.Parse(retval[1]);
            map.RegionLocX = int.Parse(retval[2]);
            map.RegionLocY = int.Parse(retval[3]);
            map.SimMapTextureID = UUID.Parse(retval[4]);
            map.RegionName = retval[5];
            map.RegionFlags = uint.Parse(retval[6]);
            map.Access = uint.Parse(retval[7]);
            map.SimFlags = (SimMapFlags)int.Parse(retval[8]); 

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

            if (retval.Count == 1)
                return null;

            SimMap map = new SimMap();
            map.RegionID = UUID.Parse(retval[0]);
            map.EstateID = uint.Parse(retval[1]);
            map.RegionLocX = int.Parse(retval[2]);
            map.RegionLocY = int.Parse(retval[3]);
            map.SimMapTextureID = UUID.Parse(retval[4]);
            map.RegionName = retval[5];
            map.RegionFlags = uint.Parse(retval[6]);
            map.Access = uint.Parse(retval[7]);
            map.SimFlags = (SimMapFlags)int.Parse(retval[8]);

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
                GD.Insert("simmap", new object[]{map.RegionID,map.EstateID,
                    map.RegionLocX, map.RegionLocY, map.SimMapTextureID,
                    map.RegionName, map.RegionFlags, map.Access,
                    map.SimFlags});
            }
            else
            {
                GD.Update("simmap", new object[] { map.EstateID,
                map.RegionLocX, map.RegionLocY, map.SimMapTextureID,
                map.RegionName, map.RegionFlags, map.Access,
                map.SimFlags }, new string[] { "EstateID", "RegionLocX",
                "RegionLocY", "SimMapTextureID", "RegionName",
                "RegionFlags", "Access", "GridRegionFlags" },
                    new string[] { "RegionID" }, new object[] { map.RegionID });
            }
		}

		/// <summary>
		/// Adds a new telehub in the region. Replaces an old one automatically.
		/// </summary>
		/// <param name="regionID"></param>
		/// <param name="position">Telehub position</param>
		/// <param name="regionPosX">Region Position in meters</param>
		/// <param name="regionPosY">Region Position in meters</param>
		public void AddTelehub(Telehub telehub)
		{
			//Look for a telehub first.
			if (FindTelehub(new UUID(telehub.RegionID)) != null)
            {
				//Found one, time to update it.
				GD.Update("telehubs", new object[] {
					telehub.TelehubX,
					telehub.TelehubY,
					telehub.TelehubZ
				}, new string[] {
					"TelehubX",
					"TelehubY",
					"TelehubZ"
				}, new string[] { "RegionID" }, new object[] { telehub.RegionID });
			} else {
				//Make a new one
				List<object> values = new List<object>();
                values.Add(telehub.RegionID);
                values.Add(telehub.RegionLocX);
                values.Add(telehub.RegionLocY);
                values.Add(telehub.TelehubX);
                values.Add(telehub.TelehubY);
                values.Add(telehub.TelehubZ);
				GD.Insert("telehubs", values.ToArray());
			}
		}

		/// <summary>
		/// Removes the telehub if it exists.
		/// </summary>
		/// <param name="regionID"></param>
		public void RemoveTelehub(UUID regionID)
		{
			//Look for a telehub first.
			Vector3 oldPos = Vector3.Zero;
			if (FindTelehub(regionID) != null) {
				GD.Delete("telehubs", new string[] { "RegionID" }, new object[] { regionID });
			}
		}

		/// <summary>
		/// Attempts to find a telehub in the region; if one is not found, returns false.
		/// </summary>
		/// <param name="regionID">Region ID</param>
		/// <param name="position">The position of the telehub</param>
		/// <returns></returns>
        public Telehub FindTelehub(UUID regionID)
		{
            Telehub telehub = new Telehub();
			List<string> telehubposition = GD.Query("RegionID", regionID, "telehubs", "RegionLocX,RegionLocY,TelehubX,TelehubY,TelehubZ");
			//Not the right number of values, so its not there.
			if (telehubposition.Count != 5)
                return null;

            telehub.RegionLocX = Convert.ToInt32(telehubposition[4]);
            telehub.RegionLocY = Convert.ToInt32(telehubposition[4]);
            telehub.TelehubX = Convert.ToInt32(telehubposition[4]);
            telehub.TelehubY = Convert.ToInt32(telehubposition[4]);
            telehub.TelehubZ = Convert.ToInt32(telehubposition[4]);

            return telehub;
		}
	}
}
