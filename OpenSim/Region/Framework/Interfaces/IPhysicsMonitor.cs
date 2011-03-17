using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IPhysicsMonitor
    {
        void AddPhysicsStats(UUID regionID, PhysicsScene scene);
    }
}
