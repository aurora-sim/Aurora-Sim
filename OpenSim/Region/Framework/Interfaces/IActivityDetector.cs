using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IActivityDetector
    {
        void AgentIsAZombie(UUID agentID);
    }
}
