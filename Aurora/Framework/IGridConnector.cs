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
		void AddTelehub(Telehub telehub);
		void RemoveTelehub(UUID regionID);
        Telehub FindTelehub(UUID regionID);
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
