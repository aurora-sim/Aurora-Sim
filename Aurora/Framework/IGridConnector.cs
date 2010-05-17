using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Framework
{
	public interface IGridConnector
	{
        SimMap GetSimMap(UUID regionID);
        SimMap GetSimMap(int regionX, int regionY);
        void SetSimMap(SimMap map);
		void CreateRegion(UUID regionID);
		void AddTelehub(Telehub telehub);
		void RemoveTelehub(UUID regionID);
        Telehub FindTelehub(UUID regionID);
    }
    [Flags()]
    public enum SimMapFlags : int
    {
        PG = 1, // PG sim
        Mature = 2, // Mature sim
        Adult = 4, // Adult sim
        Hidden = 8, // Hidden from all but estate managers
        LockedOut = 16, // Don't allow registration
        NoMove = 32, // Don't allow moving this region
        Reservation = 64, // This is an inactive reservation
        External = 128 // Record represents a HG link or other link
    }
}
