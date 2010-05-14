using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.DataManager;

namespace Aurora.Services.DataService
{
	public class LocalGridConnector : IGridConnector
	{
		private IGenericData GD = null;
		public LocalGridConnector()
		{
			GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
		}

		/// <summary>
		/// Gets the region's flags
		/// </summary>
		/// <param name="regionID"></param>
		/// <returns></returns>
		public GridRegionFlags GetRegionFlags(UUID regionID)
		{
			List<string> flags = GD.Query("RegionID", regionID, "regionflags", "Flags");
			//The region doesn't exist, so make sure to not return a valid value.
            if (flags.Count == 0 || flags[0] == " " || flags[0] == "")
				return (GridRegionFlags)(-1);

			GridRegionFlags regionFlags = (GridRegionFlags)int.Parse(flags[0]);
			return regionFlags;
		}

		/// <summary>
		/// Updates the region's flags
		/// </summary>
		/// <param name="regionID"></param>
		/// <param name="flags"></param>
		public void SetRegionFlags(UUID regionID, GridRegionFlags flags)
		{
			GD.Update("regionflags", new object[] { flags }, new string[] { "Flags" }, new string[] { "RegionID" }, new object[] { regionID });
		}

		/// <summary>
		/// Creates the regionflags entry in the database
		/// </summary>
		/// <param name="regionID"></param>
		public void CreateRegion(UUID regionID)
		{
			List<object> values = new List<object>();
			values.Add(regionID);
			values.Add(0);
			GD.Insert("regionflags", values.ToArray());
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
