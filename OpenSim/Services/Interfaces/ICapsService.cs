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
        /// Remove all instances of the agent from the Caps server
        /// </summary>
        /// <param name="AgentID"></param>
        void RemoveCAPS(UUID AgentID);

        /// <summary>
        /// Create the CAPS handler for the given user at the region given by the regionHandle
        /// </summary>
        /// <param name="AgentID">The agents UUID</param>
        /// <param name="SimCAPS">The CAPS request on the region the user is being added to</param>
        /// <param name="CAPS">The CAPS request, looks like '/CAPS/(UUID)0000/</param>
        /// <param name="regionHandle">The region handle of the region the user is being added to</param>
        /// <returns>Returns the CAPS URL that was created by the CAPS Service</returns>
        string CreateCAPS(UUID AgentID, string UrlToInform, string CAPSBase, ulong regionHandle);

        IClientCapsService GetOrCreateClientCapsService(UUID AgentID);
        IClientCapsService GetClientCapsService(UUID AgentID);

        /// <summary>
        /// The URI of the CAPS service with http:// and port
        /// </summary>
        string HostUri { get; }

        IRegistryCore Registry { get; }

        IHttpServer Server { get; }
    }

    /// <summary>
    /// The interface that modules that add CAP handlers need to implement
    /// </summary>
    public interface ICapsServiceConnector
    {
        void RegisterCaps(IRegionClientCapsService service);
        void DeregisterCaps();
        void EnteringRegion();
    }

    /// <summary>
    /// This is the client Caps Service.
    /// It is created once for every client and controls the creation and deletion of 
    ///    the IRegionClientCapsService interfaces.
    /// </summary>
    public interface IClientCapsService
    {
        UUID AgentID { get; }
        bool InTeleport { get; set; }
        bool RequestToCancelTeleport { get; set; }
        bool CallbackHasCome { get; set; }
        IRegistryCore Registry { get; }
        IHttpServer Server { get; }
        String HostUri { get; }

        void Initialise(ICapsService server, UUID agentID);
        void Close();
        IRegionClientCapsService GetCapsService(ulong regionID);
        IRegionClientCapsService GetOrCreateCapsService(ulong regionID, string CAPSBase, string UrlToInform);
        void RemoveCAPS(ulong regionHandle);
    }

    /// <summary>
    /// This is the per Region, per Client Caps Service
    /// This is created for every region the client enters, either as a child or root agent.
    /// </summary>
    public interface IRegionClientCapsService
    {
        ulong RegionHandle { get; }
        UUID AgentID { get; }
        OSDMap InfoToSendToUrl { get; set; }
        OSDMap RequestMap { get; set; }
        string UrlToInform { get; }
        String HostUri { get; }
        String CapsUrl { get; }
        IRegistryCore Registry { get; }
        IClientCapsService ClientCaps { get; }
        UUID Password { get; }

        void Initialise(IClientCapsService clientCapsService, ulong regionHandle, string capsBase, string urlToInform);
        void Close();
        void AddSEEDCap(string CapsUrl, string UrlToInform, UUID Password);
        string CreateCAPS(string method, string appendedPath);
        List<ICapsServiceConnector> GetServiceConnectors();
        void AddStreamHandler(string method, IRequestHandler handler);
        void RemoveStreamHandler(string method, string httpMethod, string path);

        void InformModulesOfRequest();
    }

    /// <summary>
    /// This interface is per region and keeps track of what agents are in the given region
    /// </summary>
    public interface IRegionCapsService
    {
        ulong RegionHandle { get; }
        UUID RegionID { get; }

        /// <summary>
        /// Initialise the service
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="regionID"></param>
        void Initialise(ulong regionHandle, UUID regionID);

        /// <summary>
        /// Add this client to the region
        /// </summary>
        /// <param name="service"></param>
        void AddClientToRegion(IRegionClientCapsService service);

        /// <summary>
        /// Remove the client from this region
        /// </summary>
        /// <param name="service"></param>
        void RemoveClientFromRegion(IRegionClientCapsService service);

        /// <summary>
        /// Get an agent's Caps by UUID
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        IRegionClientCapsService GetClient(UUID AgentID);

        /// <summary>
        /// Get all clients in this region
        /// </summary>
        /// <returns></returns>
        List<IRegionClientCapsService> GetClients();
    }
}
