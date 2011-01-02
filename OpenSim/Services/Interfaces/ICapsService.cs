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
        /// Remove the user from the given region
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="regionHandle"></param>
        void RemoveCAPS(UUID AgentID, ulong regionHandle);

        /// <summary>
        /// Create the CAPS handler for the given user at the region given by the regionHandle
        /// </summary>
        /// <param name="AgentID">The agents UUID</param>
        /// <param name="SimCAPS">The CAPS request on the region the user is being added to</param>
        /// <param name="CAPS">The CAPS request, looks like '/CAPS/(UUID)0000/</param>
        /// <param name="regionHandle">The region handle of the region the user is being added to</param>
        /// <returns>Returns the CAPS URL that was created by the CAPS Service</returns>
        string CreateCAPS(UUID AgentID, string SimCAPS, string CAPS, string CAPSPath, ulong regionHandle);

        /// <summary>
        /// Add the given user's CAPS to the general service if it does not already exist
        /// </summary>
        /// <param name="handler"></param>
        void AddCapsService(IPrivateCapsService handler);

        /// <summary>
        /// Attempt to find a user's CAPS handler that already exists
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="agentID"></param>
        /// <returns></returns>
        IPrivateCapsService GetCapsService(ulong regionID, UUID agentID);

        /// <summary>
        /// The URI of the CAPS service with http:// and port
        /// </summary>
        string HostURI { get; }
    }

    /// <summary>
    /// The per user/per region CAPS Service
    /// </summary>
    public interface IPrivateCapsService
    {
        /// <summary>
        /// Set up this handler and add the ICAPSModule CAPS
        /// </summary>
        void Initialise();

        /// <summary>
        /// The client calls this method when it wishes to find out what CAP handlers exist for this sim.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="httpRequest"></param>
        /// <param name="httpResponse"></param>
        /// <returns></returns>
        string CapsRequest(string request, string path, string param,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse);

        /// <summary>
        /// Add the given CAPS method (method) and CAPS url (caps) to the list of known CAPS handlers
        /// </summary>
        /// <param name="method"></param>
        /// <param name="caps"></param>
        void AddCAPS(string method, string caps);

        /// <summary>
        /// Get the CAPS URL that the given method implements
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        string GetCAPS(string method);

        /// <summary>
        /// Create a new CAPS URL for the given method and add it to the list of known CAPS modules
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        string CreateCAPS(string method);

        /// <summary>
        /// Create a new CAPS URL for the given method with the appended path on the end and add it to the list of known CAPS modules
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        string CreateCAPS(string method, string appendedPath);

        /// <summary>
        /// Deregisters all CAPS for this region 
        /// Called when this reigon is being closed
        /// </summary>
        void RemoveCAPS();

        /// <summary>
        /// The OSDMap of parameters to tell the sim. Currently only used by the EventQueueService to send the password to the sim
        /// </summary>
        OSDMap PostToSendToSim { get; set; }

        /// <summary>
        /// The URL to the CAPS handler that we sent to the client
        /// </summary>
        string CapsURL { get; }

        /// <summary>
        /// The URL with no hostname or port. 
        /// Looks like '/CAPS/(UUID)0000/'
        /// </summary>
        string CapsBase { get; }

        /// <summary>
        /// The UUID of the CapsBase
        /// </summary>
        string CapsObjectPath { get; }

        /// <summary>
        /// The agent we are serving
        /// </summary>
        UUID AgentID { get; }

        /// <summary>
        /// The sim CAPS handler that we call to find out what CAPS requests the sim has to add
        /// </summary>
        string SimToInform { get; set; }

        /// <summary>
        /// The CAPSService that created this handler
        /// </summary>
        ICapsService PublicHandler { get; }

        /// <summary>
        /// The region that we are serving CAPS for regionHandle
        /// </summary>
        ulong RegionHandle { get; }

        /// <summary>
        /// The EventQueueService for the user in the region
        /// </summary>
        IInternalEventQueueService EventQueueService { get; }

        #region Required Services

        IAssetService AssetService { get; }
        IPresenceService PresenceService { get; }
        IInventoryService InventoryService { get; }
        ILibraryService LibraryService { get; }
        IGridUserService GridUserService { get; }
        IGridService GridService { get; }

        #endregion
    }

    /// <summary>
    /// The interface that modules that add CAP handlers need to implement
    /// </summary>
    public interface ICapsServiceConnector
    {
        /// <summary>
        /// Register all CAPS that the module has to offer at thsi time
        /// </summary>
        /// <param name="agentID"></param>
        /// <param name="server"></param>
        /// <param name="handler"></param>
        /// <returns>All the CAPS request the given module has</returns>
        List<IRequestHandler> RegisterCaps(UUID agentID, IHttpServer server, IPrivateCapsService handler); 
    }
}
