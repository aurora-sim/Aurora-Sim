using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IObjectCache
    {
        bool UseCachedObject(UUID AgentID, uint localID, uint CurrentEntityCRC);
    }
}
