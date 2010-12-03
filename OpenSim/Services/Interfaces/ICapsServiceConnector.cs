using System;
using System.Collections.Generic;
using System.Linq;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;

namespace OpenSim.Services.Interfaces
{
    public interface ICapsServiceConnector
    {
        List<IRequestHandler> RegisterCaps(UUID agentID); 
    }
}
