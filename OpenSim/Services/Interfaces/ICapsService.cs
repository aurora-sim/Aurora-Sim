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
        void RegisterCaps(IRegionClientCapsService perRegionClientCapsService);
        void DeregisterCaps();
    }

    /// <summary>
    /// This is the client Caps Service.
    /// It is created once for every client and controls the creation and deletion of 
    ///    the IRegionClientCapsService interfaces.
    /// </summary>
    public interface IClientCapsService
    {
        UUID AgentID { get; }
        IRegistryCore Registry { get; }
        IHttpServer Server { get; }
        String HostUri { get; }

        void Initialise(ICapsService server, UUID agentID);
        void Close();
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
        string UrlToInform { get; }
        String HostUri { get; }
        String CapsUrl { get; }
        IRegistryCore Registry { get; }

        void Initialise(IClientCapsService clientCapsService, string capsBase, string urlToInform);
        void Close();
        string CreateCAPS(string method, string appendedPath);
        void AddStreamHandler(string method, IRequestHandler handler);
        void RemoveStreamHandler(string method, string httpMethod, string path);
    }
}
