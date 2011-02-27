using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    public interface ICommunicationService
    {
        GridRegion GetRegionForGrid(string regionName, string url);
        OSDMap GetUrlsForUser(GridRegion region, UUID userID);
    }
}
