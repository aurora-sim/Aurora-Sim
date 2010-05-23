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
                    map.SimFlags});
            }
            else
            {
                GD.Update("simmap", new object[] { map.RegionHandle, map.EstateID,
                map.RegionLocX, map.RegionLocY, map.SimMapTextureID,
                map.RegionName, map.RegionFlags, map.Access,
                map.SimFlags }, new string[] { "RegionHandle", "EstateID", "RegionLocX",
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
					telehub.TelehubLocX,
					telehub.TelehubLocY,
					telehub.TelehubLocZ,
                    telehub.TelehubRotX,
					telehub.TelehubRotY,
					telehub.TelehubRotZ
				}, new string[] {
					"TelehubLocX",
					"TelehubLocY",
					"TelehubLocZ",
                    "TelehubRotX",
					"TelehubRotY",
					"TelehubRotZ"
				}, new string[] { "RegionID" }, new object[] { telehub.RegionID });
			} else {
				//Make a new one
				List<object> values = new List<object>();
                values.Add(telehub.RegionID);
                values.Add(telehub.RegionLocX);
                values.Add(telehub.RegionLocY);
                values.Add(telehub.TelehubLocX);
                values.Add(telehub.TelehubLocY);
                values.Add(telehub.TelehubLocZ);
                values.Add(telehub.TelehubRotX);
                values.Add(telehub.TelehubRotY);
                values.Add(telehub.TelehubRotZ);
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
            List<string> telehubposition = GD.Query("RegionID", regionID, "telehubs", "RegionLocX,RegionLocY,TelehubLocX,TelehubLocY,TelehubLocZ,TelehubRotX,TelehubRotY,TelehubRotZ");
			//Not the right number of values, so its not there.
			if (telehubposition.Count != 8)
                return null;

            telehub.RegionID = regionID.ToString();
            telehub.RegionLocX = float.Parse(telehubposition[0]);
            telehub.RegionLocY = float.Parse(telehubposition[1]);
            telehub.TelehubLocX = float.Parse(telehubposition[2]);
            telehub.TelehubLocY = float.Parse(telehubposition[3]);
            telehub.TelehubLocZ = float.Parse(telehubposition[4]);
            telehub.TelehubRotX = float.Parse(telehubposition[5]);
            telehub.TelehubRotY = float.Parse(telehubposition[6]);
            telehub.TelehubRotZ = float.Parse(telehubposition[7]);

            return telehub;
		}
	}
}
