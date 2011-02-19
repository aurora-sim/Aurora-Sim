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
        /// <param name="IsRootAgent">Whether this new Caps agent is a root agent in the sim</param>
        /// <param name="circuitData">The circuit data of the agent that is being created</param>/param>
        /// <returns>Returns the CAPS URL that was created by the CAPS Service</returns>
        string CreateCAPS(UUID AgentID, string UrlToInform, string CAPSBase, ulong regionHandle, bool IsRootAgent, AgentCircuitData circuitData);

        IClientCapsService GetOrCreateClientCapsService(UUID AgentID);
        IClientCapsService GetClientCapsService(UUID AgentID);

        /// <summary>
        /// The URI of the CAPS service with http:// and port
        /// </summary>
        string HostUri { get; }

        IRegistryCore Registry { get; }

        IHttpServer Server { get; }

        /// <summary>
        /// Create a caps handler for the given region
        /// </summary>
        /// <param name="RegionHandle"></param>
        void AddCapsForRegion(ulong RegionHandle);

        /// <summary>
        /// Remove the handler for the given region
        /// </summary>
        /// <param name="RegionHandle"></param>
        void RemoveCapsForRegion(ulong RegionHandle);

        /// <summary>
        /// Get a region handler for the given region
        /// </summary>
        /// <param name="RegionHandle"></param>
        IRegionCapsService GetCapsForRegion(ulong regionID);

        /// <summary>
        /// Get all agents caps services across the grid
        /// </summary>
        /// <returns></returns>
        List<IClientCapsService> GetClientsCapsServices();
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

        /// <summary>
        /// Start the caps service for this user
        /// </summary>
        /// <param name="server"></param>
        /// <param name="agentID"></param>
        void Initialise(ICapsService server, UUID agentID);
        
        /// <summary>
        /// Close all Caps connections and destroy any remaining data
        /// </summary>
        void Close();

        /// <summary>
        /// Get a regions Caps Service by region handle
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        IRegionClientCapsService GetCapsService(ulong regionHandle);
        /// <summary>
        /// Gets a list of all region caps services that the agent is currently in
        /// </summary>
        /// <returns></returns>
        List<IRegionClientCapsService> GetCapsServices();

        /// <summary>
        /// Get or create a new region caps service for the given region
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="CAPSBase"></param>
        /// <param name="UrlToInform"></param>
        /// <returns></returns>
        IRegionClientCapsService GetOrCreateCapsService(ulong regionID, string CAPSBase, string UrlToInform, AgentCircuitData circuitData);

        /// <summary>
        /// Remove the caps for this user from the given region
        /// </summary>
        /// <param name="regionHandle"></param>
        void RemoveCAPS(ulong regionHandle);
    }

    /// <summary>
    /// This is the per Region, per Client Caps Service
    /// This is created for every region the client enters, either as a child or root agent.
    /// </summary>
    public interface IRegionClientCapsService
    {
        int RegionX { get; }
        int RegionY { get; }
        ulong RegionHandle { get; }
        AgentCircuitData CircuitData { get; }
        UUID AgentID { get; }
        OSDMap InfoToSendToUrl { get; set; }
        OSDMap RequestMap { get; set; }
        string UrlToInform { get; }
        String HostUri { get; }
        String CapsUrl { get; }
        bool Disabled { get; set; }
        IRegistryCore Registry { get; }
        IClientCapsService ClientCaps { get; }
        UUID Password { get; }
        bool RootAgent { get; set; }

        void Initialise(IClientCapsService clientCapsService, ulong regionHandle, string capsBase, string urlToInform, AgentCircuitData circuitData);
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

        /// <summary>
        /// Initialise the service
        /// </summary>
        /// <param name="regionHandle"></param>
        void Initialise(ulong regionHandle);

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
