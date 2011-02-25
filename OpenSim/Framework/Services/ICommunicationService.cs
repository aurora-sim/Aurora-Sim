using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSim.Services.Interfaces
{
    public interface ICommunicationService
    {
        GridRegion GetRegionForGrid(string regionName, string url);
    }
}
