using System;
using System.Collections.Generic;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    /// <summary>
    /// This controls what regions and users have Caps SEED requests and all of the Cap handlers associated with those requests
    /// </summary>
    public interface ICapsService
    {
        /// <summary>
        /// All of the loaded modules that add CAP requests
        /// </summary>
        List<ICapsServiceConnector> CapsModules { get; }
        /// <summary>
        /// Remove all instances of the agent from the Caps server
        /// </summary>
        /// <param name="AgentID"></param>
        void RemoveCAPS(UUID AgentID);
        /// <summary>
        /// Remove the user from the given region
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="regionHandle"></param>
        void RemoveCAPS(UUID AgentID, ulong regionHandle);
        string CreateCAPS(UUID AgentID, string SimCAPS, string CAPS, ulong regionHandle);
        void AddCapsService(IPrivateCapsService handler);
        IPrivateCapsService GetCapsService(ulong regionID, UUID agentID);
        string HostURI { get; }
    }

    public interface IPrivateCapsService
    {
        void AddCAPS(string method, string caps);
        void Initialise();
        string CapsURL { get; }
        string CapsBase { get; }
        UUID AgentID { get; }
        string CapsRequest(string request, string path, string param,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse);
        OSDMap PostToSendToSim { get; set; }
        string GetCAPS(string method);
        string CreateCAPS(string method);
        string CreateCAPS(string method, string appendedPath);
        IAssetService AssetService { get; }
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

        void RemoveCAPS();
    }

    public interface ICapsServiceConnector
    {
        List<IRequestHandler> RegisterCaps(UUID agentID, IHttpServer server, IPrivateCapsService handler); 
    }
}
