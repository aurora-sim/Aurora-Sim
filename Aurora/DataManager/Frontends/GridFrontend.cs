using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.DataManager;

namespace Aurora.DataManager.Frontends
{
    public class GridFrontend
    {
        private IGenericData GD = null;
        public GridFrontend()
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
            if (flags.Count == 0 || flags[0] == " ")
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
        public void AddTelehub(UUID regionID, Vector3 position, int regionPosX, int regionPosY)
        {
            //Look for a telehub first.
            Vector3 oldPos = Vector3.Zero;
            if (FindTelehub(regionID, out oldPos))
            {
                //Found one, time to update it.
                GD.Update("telehubs", new object[] { position.X, position.Y, position.Z }, new string[] { "TelehubX", "TelehubY", "TelehubZ" }, new string[] { "RegionID" }, new object[] { regionID });
            }
            else
            {
                //Make a new one
                List<object> values = new List<object>();
                values.Add(regionID);
                values.Add(regionPosX);
                values.Add(regionPosY);
                values.Add(position.X);
                values.Add(position.Y);
                values.Add(position.Z);
                GD.Insert("telehubs",values.ToArray());
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
            if (FindTelehub(regionID, out oldPos))
            {
                GD.Delete("telehubs", new string[] { "RegionID" }, new object[] { regionID });
            }
        }

        /// <summary>
        /// Attempts to find a telehub in the region; if one is not found, returns false.
        /// </summary>
        /// <param name="regionID">Region ID</param>
        /// <param name="position">The position of the telehub</param>
        /// <returns></returns>
        public bool FindTelehub(UUID regionID, out Vector3 position)
        {
            position = Vector3.Zero;
            List<string> telehubposition = GD.Query("RegionID", regionID, "telehubs", "TelehubX,TelehubY,TelehubZ");
            //Not the right number of values, so its not there.
            if (telehubposition.Count != 3)
                return false;

            position = new Vector3(int.Parse(telehubposition[0]), int.Parse(telehubposition[1]), int.Parse(telehubposition[2]));
            return true;
        }
    }
    [Flags]
    public enum GridRegionFlags : int
    {
        PG = 1,
        Mature = 2,
        Adult = 4,
        Hidden = 8
    }
}
