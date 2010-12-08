using System;
using System.Collections.Generic;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    public interface ICapsService
    {
        List<ICapsServiceConnector> CapsModules { get; }
        void RemoveCAPS(UUID AgentID);
        void RemoveCAPS(UUID AgentID, ulong regionHandle);
        void CreateCAPS(UUID AgentID, string SimCAPS, string CAPS, ulong regionHandle);
        void AddCapsService(IPrivateCapsService handler, string CAPS, UUID agentID);
        IPrivateCapsService GetCapsService(ulong regionID, UUID agentID);
    }

    public interface IPrivateCapsService
    {
        void AddCAPS(string method, string caps);
        void Initialise();
        string CapsURL { get; }
        UUID AgentID { get; }
        string CapsRequest(string request, string path, string param,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse);
        OSDMap PostToSendToSim { get; set; }
        string GetCAPS(string method);
        string CreateCAPS(string method);
        string CreateCAPS(string method, string appendedPath);
        IPresenceService PresenceService { get; }
        IInventoryService InventoryService { get; }
        ILibraryService LibraryService { get; }
        IGridUserService GridUserService { get; }
        IGridService GridService { get; }
        string SimToInform { get; set; }
        string HostName { get; }
        ICapsService PublicHandler { get; }
        ulong RegionHandle { get; }
        IInternalEventQueueService EventQueueService { get; }
    }

    public interface ICapsServiceConnector
    {
        List<IRequestHandler> RegisterCaps(UUID agentID, IHttpServer server, IPrivateCapsService handler); 
    }
}
