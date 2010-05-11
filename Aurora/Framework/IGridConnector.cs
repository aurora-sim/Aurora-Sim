using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Framework
{
	public interface IGridConnector
	{
		GridRegionFlags GetRegionFlags(UUID regionID);
		void SetRegionFlags(UUID regionID, GridRegionFlags flags);
		void CreateRegion(UUID regionID);
		void AddTelehub(UUID regionID, Vector3 position, int regionPosX, int regionPosY);
		void RemoveTelehub(UUID regionID);
		bool FindTelehub(UUID regionID, out Vector3 position);
    }
    [Flags()]
    public enum GridRegionFlags : int
    {
        PG = 1,
        Mature = 2,
        Adult = 4,
        Hidden = 8
    }
}
