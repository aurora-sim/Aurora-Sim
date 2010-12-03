using System;
using System.Collections.Generic;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    public interface IPrivateCapsService
    {
        string HostName { get; set; }
        OSDMap PostToSendToSim { get; set; }
    }

    public interface ICapsServiceConnector
    {
        List<IRequestHandler> RegisterCaps(UUID agentID, IHttpServer server, IPrivateCapsService handler); 
    }
}
