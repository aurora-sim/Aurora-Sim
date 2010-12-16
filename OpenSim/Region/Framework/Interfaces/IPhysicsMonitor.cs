using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenSim.Region.Physics.Manager;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IPhysicsMonitor
    {
        void AddPhysicsStats(UUID regionID, PhysicsScene scene);
    }
}
