using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public interface IRegionInfoConnector
	{
        RegionInfo[] GetRegionInfos();
        RegionInfo GetRegionInfo(UUID regionID);
        RegionInfo GetRegionInfo(string regionName);
        void UpdateRegionInfo(RegionInfo region, bool Disabled);
	}
}
