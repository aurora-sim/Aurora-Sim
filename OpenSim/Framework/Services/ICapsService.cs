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

        /// <summary>
        /// Get a client's caps service (contains all child and root agents) if it exists, otherwise
        ///   create a new caps service for the client
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        IClientCapsService GetOrCreateClientCapsService(UUID AgentID);

        /// <summary>
        /// Get a client's caps service (contains all child and root agents)
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        IClientCapsService GetClientCapsService(UUID AgentID);

        /// <summary>
        /// The URI of the CAPS service with http:// and port
        /// </summary>
        string HostUri { get; }

        /// <summary>
        /// The registry where all the interfaces are pulled from
        /// </summary>
        IRegistryCore Registry { get; }

        /// <summary>
        /// The HTTP server that all CAP handlers will be added to
        /// </summary>
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
        /// <summary>
        /// Add any CAP handlers to the service that the module implements
        /// </summary>
        /// <param name="service"></param>
        void RegisterCaps(IRegionClientCapsService service);

        /// <summary>
        /// Remove all CAP handlers as the module is being closed
        /// </summary>
        void DeregisterCaps();

        /// <summary>
        /// The agent is entering the region
        /// </summary>
        void EnteringRegion();
    }

    /// <summary>
    /// This is the client Caps Service.
    /// It is created once for every client and controls the creation and deletion of 
    ///    the IRegionClientCapsService interfaces.
    /// </summary>
    public interface IClientCapsService
    {
        /// <summary>
        /// The ID of the agent we are serving
        /// </summary>
        UUID AgentID { get; }

        /// <summary>
        /// Whether the user is currently teleporting/crossings
        /// </summary>
        bool InTeleport { get; set; }

        /// <summary>
        /// Whether a request to cancel a given teleport has come for this agent
        /// </summary>
        bool RequestToCancelTeleport { get; set; }

        /// <summary>
        /// Whether a callback (used for teleporting) has come for this agent
        /// <note>Tells the AgentProcessor that the teleport has been successful</note>
        /// </summary>
        bool CallbackHasCome { get; set; }

        /// <summary>
        /// The registry where all the interfaces are pulled from
        /// </summary>
        IRegistryCore Registry { get; }

        /// <summary>
        /// The HTTP Server that handlers will be added to
        /// </summary>
        IHttpServer Server { get; }

        /// <summary>
        /// The hostname of the CapsService (http://IP:Port)
        /// </summary>
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
        /// Get the root agent's caps service
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        IRegionClientCapsService GetRootCapsService();

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
        /// <summary>
        /// The region Handle of this region
        /// </summary>
        ulong RegionHandle { get; }

        /// <summary>
        /// The Region X Location (in meters)
        /// </summary>
        int RegionX { get; }

        /// <summary>
        /// The Region Y Location (in meters)
        /// </summary>
        int RegionY { get; }

        /// <summary>
        /// The last circuit data we were updated with
        /// </summary>
        AgentCircuitData CircuitData { get; }

        /// <summary>
        /// The last position that we have for this agent
        /// </summary>
        Vector3 LastPosition { get; set; }

        /// <summary>
        /// The ID of the Agent we are serving
        /// </summary>
        UUID AgentID { get; }

        /// <summary>
        /// The URL to inform that a client has called our CAPS SEED URL
        /// </summary>
        string UrlToInform { get; }

        /// <summary>
        /// The host URI of this CAPS Servie (http://IP:port)
        /// </summary>
        String HostUri { get; }

        /// <summary>
        /// The URL (http:// included) to the SEED CAP handler
        /// </summary>
        String CapsUrl { get; }

        /// <summary>
        /// Have we been disabled? (Should be deleted soon)
        /// </summary>
        bool Disabled { get; set; }

        /// <summary>
        /// The registry that holds the interfaces
        /// </summary>
        IRegistryCore Registry { get; }

        /// <summary>
        /// Our parent client that we are attached to
        /// </summary>
        IClientCapsService ClientCaps { get; }

        /// <summary>
        /// Whether the agent is a root agent
        /// </summary>
        bool RootAgent { get; set; }

        /// <summary>
        /// Sets up the region for the given client
        /// </summary>
        /// <param name="clientCapsService"></param>
        /// <param name="regionHandle"></param>
        /// <param name="capsBase"></param>
        /// <param name="urlToInform"></param>
        /// <param name="circuitData"></param>
        void Initialise(IClientCapsService clientCapsService, ulong regionHandle, string capsBase, string urlToInform, AgentCircuitData circuitData);
        
        /// <summary>
        /// Closes the region caps, removes all caps handlers and removes itself
        /// </summary>
        void Close();

        /// <summary>
        /// Add a new SEED CAP for the region at the given CapsUrl unless one already exists
        ///   Will start infomring UrlToInform if no other is set
        /// </summary>
        /// <param name="CapsUrl"></param>
        /// <param name="UrlToInform"></param>
        void AddSEEDCap(string CapsUrl, string UrlToInform);

        /// <summary>
        /// Add the given CAPS method to the list that will be given to the client
        /// </summary>
        /// <param name="method">Name of the Method</param>
        /// <param name="appendedPath">The path (no http://) to the Caps Method</param>
        /// <returns></returns>
        string CreateCAPS(string method, string appendedPath);
        
        /// <summary>
        /// Get all CapsService modules
        /// </summary>
        /// <returns></returns>
        List<ICapsServiceConnector> GetServiceConnectors();

        /// <summary>
        /// Add a new handler for a CAPS message
        /// </summary>
        /// <param name="method"></param>
        /// <param name="handler"></param>
        void AddStreamHandler(string method, IRequestHandler handler);

        /// <summary>
        /// Remove an old handler for a CAPS message
        /// </summary>
        /// <param name="method"></param>
        /// <param name="httpMethod"></param>
        /// <param name="path"></param>
        void RemoveStreamHandler(string method, string httpMethod, string path);

        /// <summary>
        /// A new request was made to add an agent to this region, tell the modules about it
        /// </summary>
        void InformModulesOfRequest();
    }

    /// <summary>
    /// This interface is per region and keeps track of what agents are in the given region
    /// </summary>
    public interface IRegionCapsService
    {
        /// <summary>
        /// The RegionHandle of the region we represent
        /// </summary>
        ulong RegionHandle { get; }

        /// <summary>
        /// Initialise the service
        /// </summary>
        /// <param name="regionHandle"></param>
        void Initialise(ulong regionHandle);

        /// <summary>
        /// Close the service and all underlieing services
        /// </summary>
        /// <param name="regionHandle"></param>
        void Close();

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
