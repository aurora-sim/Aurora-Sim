using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.Framework
{
	public interface IEstateConnector
	{
		EstateSettings LoadEstateSettings(UUID regionID, bool create);
		EstateSettings LoadEstateSettings(int estateID);
		bool StoreEstateSettings(EstateSettings es);
		void SaveEstateSettings(EstateSettings es);
		List<int> GetEstates(string search);
		bool LinkRegion(UUID regionID, int estateID, string password);
		List<UUID> GetRegions(int estateID);
		bool DeleteEstate(int estateID);
	}
}
