/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Net;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework.Services
{
    /// <summary>
    ///     This controls what regions and users have Caps SEED requests and all of the Cap handlers associated with those requests
    /// </summary>
    public interface ICapsService
    {
        /// <summary>
        ///     The URI of the CAPS service with http:// and port
        /// </summary>
        string HostUri { get; }

        /// <summary>
        ///     The registry where all the interfaces are pulled from
        /// </summary>
        IRegistryCore Registry { get; }

        /// <summary>
        ///     The HTTP server that all CAP handlers will be added to
        /// </summary>
        IHttpServer Server { get; }

        /// <summary>
        ///     Remove all instances of the agent from the Caps server
        /// </summary>
        /// <param name="AgentID"></param>
        void RemoveCAPS(UUID AgentID);

        /// <summary>
        ///     Create the CAPS handler for the given user at the region given by the regionHandle
        /// </summary>
        /// <param name="AgentID">The agents UUID</param>
        /// <param name="CAPSBase">The CAPS request, looks like '/CAPS/(UUID)0000/</param>
        /// <param name="regionHandle">The region handle of the region the user is being added to</param>
        /// <param name="IsRootAgent">Whether this new Caps agent is a root agent in the sim</param>
        /// <param name="circuitData">The circuit data of the agent that is being created</param>
        /// <param name="port">The port to use for the CAPS service</param>
        /// <returns>Returns the CAPS URL that was created by the CAPS Service</returns>
        string CreateCAPS(UUID AgentID, string CAPSBase, UUID regionHandle, bool IsRootAgent,
                          AgentCircuitData circuitData, uint port);

        /// <summary>
        ///     Get a client's caps service (contains all child and root agents) if it exists, otherwise
        ///     create a new caps service for the client
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        IClientCapsService GetOrCreateClientCapsService(UUID AgentID);

        /// <summary>
        ///     Get a client's caps service (contains all child and root agents)
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        IClientCapsService GetClientCapsService(UUID AgentID);

        /// <summary>
        ///     Create a caps handler for the given region
        /// </summary>
        /// <param name="RegionHandle"></param>
        void AddCapsForRegion(UUID RegionHandle);

        /// <summary>
        ///     Remove the handler for the given region
        /// </summary>
        /// <param name="RegionHandle"></param>
        void RemoveCapsForRegion(UUID RegionHandle);

        /// <summary>
        ///     Get a region handler for the given region
        /// </summary>
        /// <param name="regionID"></param>
        IRegionCapsService GetCapsForRegion(UUID regionID);

        /// <summary>
        ///     Get all agents caps services across the grid
        /// </summary>
        /// <returns></returns>
        List<IClientCapsService> GetClientsCapsServices();

        /// <summary>
        ///     Gets all region caps services across the grid
        /// </summary>
        /// <returns></returns>
        List<IRegionCapsService> GetRegionsCapsServices();
    }

    /// <summary>
    ///     The interface that modules that add CAP handlers need to implement
    /// </summary>
    public interface ICapsServiceConnector
    {
        /// <summary>
        ///     Add any CAP handlers to the service that the module implements
        /// </summary>
        /// <param name="service"></param>
        void RegisterCaps(IRegionClientCapsService service);

        /// <summary>
        ///     Remove all CAP handlers as the module is being closed
        /// </summary>
        void DeregisterCaps();

        /// <summary>
        ///     The agent is entering the region
        /// </summary>
        void EnteringRegion();
    }

    /// <summary>
    ///     This is the client Caps Service.
    ///     It is created once for every client and controls the creation and deletion of
    ///     the IRegionClientCapsService interfaces.
    /// </summary>
    public interface IClientCapsService
    {
        /// <summary>
        ///     The ID of the agent we are serving
        /// </summary>
        UUID AgentID { get; }

        /// <summary>
        ///     Whether the user is currently teleporting/crossings
        /// </summary>
        bool InTeleport { get; set; }

        /// <summary>
        ///     Whether a request to cancel a given teleport has come for this agent
        /// </summary>
        bool RequestToCancelTeleport { get; set; }

        /// <summary>
        ///     Whether a callback (used for teleporting) has come for this agent
        ///     <note>Tells the AgentProcessor that the teleport has been successful</note>
        /// </summary>
        bool CallbackHasCome { get; set; }

        /// <summary>
        ///     The registry where all the interfaces are pulled from
        /// </summary>
        IRegistryCore Registry { get; }

        /// <summary>
        ///     The HTTP Server that handlers will be added to
        /// </summary>
        IHttpServer Server { get; }

        /// <summary>
        ///     The hostname of the CapsService (http://IP:Port)
        /// </summary>
        String HostUri { get; }

        /// <summary>
        ///     The User's account info
        /// </summary>
        UserAccount AccountInfo { get; }

        /// <summary>
        ///     Start the caps service for this user
        /// </summary>
        /// <param name="server"></param>
        /// <param name="agentID"></param>
        void Initialise(ICapsService server, UUID agentID);

        /// <summary>
        ///     Close all Caps connections and destroy any remaining data
        /// </summary>
        void Close();

        /// <summary>
        ///     Get a regions Caps Service by region handle
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <returns></returns>
        IRegionClientCapsService GetCapsService(UUID regionHandle);

        /// <summary>
        ///     Get the root agent's caps service
        /// </summary>
        /// <returns></returns>
        IRegionClientCapsService GetRootCapsService();

        /// <summary>
        ///     Gets a list of all region caps services that the agent is currently in
        /// </summary>
        /// <returns></returns>
        List<IRegionClientCapsService> GetCapsServices();

        /// <summary>
        ///     Get or create a new region caps service for the given region
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="CAPSBase"></param>
        /// <param name="circuitData"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        IRegionClientCapsService GetOrCreateCapsService(UUID regionID, string CAPSBase, AgentCircuitData circuitData,
                                                        uint port);

        /// <summary>
        ///     Remove the caps for this user from the given region
        /// </summary>
        /// <param name="regionHandle"></param>
        void RemoveCAPS(UUID regionHandle);
    }

    /// <summary>
    ///     This is the per Region, per Client Caps Service
    ///     This is created for every region the client enters, either as a child or root agent.
    /// </summary>
    public interface IRegionClientCapsService
    {
        /// <summary>
        ///     The region Handle of this region
        /// </summary>
        ulong RegionHandle { get; }

        /// <summary>
        ///     The Region X Location (in meters)
        /// </summary>
        int RegionX { get; }

        /// <summary>
        ///     The Region Y Location (in meters)
        /// </summary>
        int RegionY { get; }

        /// <summary>
        ///     The UUID of the Region
        /// </summary>
        UUID RegionID { get; }

        /// <summary>
        ///     The Region for this caps
        /// </summary>
        GridRegion Region { get; }

        /// <summary>
        ///     The HTTP server for this caps instance
        /// </summary>
        IHttpServer Server { get; }

        /// <summary>
        ///     The last circuit data we were updated with
        /// </summary>
        AgentCircuitData CircuitData { get; }

        /// <summary>
        ///     The IP of the region adjusted for loopback
        /// </summary>
        IPAddress LoopbackRegionIP { get; set; }

        /// <summary>
        ///     The last position that we have for this agent
        /// </summary>
        Vector3 LastPosition { get; set; }

        /// <summary>
        ///     The ID of the Agent we are serving
        /// </summary>
        UUID AgentID { get; }

        /// <summary>
        ///     The host URI of this CAPS Servie (http://IP:port)
        /// </summary>
        String HostUri { get; }

        /// <summary>
        ///     The URL (http:// included) to the SEED CAP handler
        /// </summary>
        String CapsUrl { get; set; }

        /// <summary>
        ///     Have we been disabled? (Should be deleted soon)
        /// </summary>
        bool Disabled { get; set; }

        /// <summary>
        ///     The registry that holds the interfaces
        /// </summary>
        IRegistryCore Registry { get; }

        /// <summary>
        ///     Our parent client that we are attached to
        /// </summary>
        IClientCapsService ClientCaps { get; }

        /// <summary>
        ///     Our parent region that we are attached to
        /// </summary>
        IRegionCapsService RegionCaps { get; }

        /// <summary>
        ///     Whether the agent is a root agent
        /// </summary>
        bool RootAgent { get; set; }

        /// <summary>
        ///     Sets up the region for the given client
        /// </summary>
        /// <param name="clientCapsService"></param>
        /// <param name="regionCapsService"></param>
        /// <param name="capsBase"></param>
        /// <param name="circuitData"></param>
        /// <param name="port">port to start the CAPS service on (0 means default)</param>
        void Initialise(IClientCapsService clientCapsService, IRegionCapsService regionCapsService, string capsBase,
                        AgentCircuitData circuitData, uint port);

        /// <summary>
        ///     Closes the region caps, removes all caps handlers and removes itself
        /// </summary>
        void Close();

        /// <summary>
        ///     Add a new SEED CAP for the region at the given CapsUrl unless one already exists
        /// </summary>
        /// <param name="CapsUrl"></param>
        void AddSEEDCap(string CapsUrl);

        void AddCAPS(string method, string caps);

        void AddCAPS(OSDMap caps);

        /// <summary>
        ///     Add the given CAPS method to the list that will be given to the client
        /// </summary>
        /// <param name="method">Name of the Method</param>
        /// <param name="appendedPath">The path (no http://) to the Caps Method</param>
        /// <returns></returns>
        string CreateCAPS(string method, string appendedPath);

        /// <summary>
        ///     Get all CapsService modules
        /// </summary>
        /// <returns></returns>
        List<ICapsServiceConnector> GetServiceConnectors();

        /// <summary>
        ///     Add a new handler for a CAPS message
        /// </summary>
        /// <param name="method"></param>
        /// <param name="handler"></param>
        void AddStreamHandler(string method, IStreamedRequestHandler handler);

        /// <summary>
        ///     Remove an old handler for a CAPS message
        /// </summary>
        /// <param name="method"></param>
        /// <param name="httpMethod"></param>
        void RemoveStreamHandler(string method, string httpMethod);

        /// <summary>
        ///     Remove an old handler for a CAPS message
        /// </summary>
        /// <param name="method"></param>
        /// <param name="httpMethod"></param>
        /// <param name="path"></param>
        void RemoveStreamHandler(string method, string httpMethod, string path);

        /// <summary>
        ///     A new request was made to add an agent to this region, tell the modules about it
        /// </summary>
        void InformModulesOfRequest();
    }

    /// <summary>
    ///     This interface is per region and keeps track of what agents are in the given region
    /// </summary>
    public interface IRegionCapsService
    {
        /// <summary>
        ///     The RegionHandle of the region we represent
        /// </summary>
        ulong RegionHandle { get; }

        /// <summary>
        ///     The Region X Location (in meters)
        /// </summary>
        int RegionX { get; }

        /// <summary>
        ///     The Region Y Location (in meters)
        /// </summary>
        int RegionY { get; }

        /// <summary>
        ///     The Region for this caps
        /// </summary>
        GridRegion Region { get; }

        /// <summary>
        ///     Initialise the service
        /// </summary>
        /// <param name="RegionID"></param>
        /// <param name="registry"></param>
        void Initialise(UUID RegionID, IRegistryCore registry);

        /// <summary>
        ///     Close the service and all underlieing services
        /// </summary>
        void Close();

        /// <summary>
        ///     Add this client to the region
        /// </summary>
        /// <param name="service"></param>
        void AddClientToRegion(IRegionClientCapsService service);

        /// <summary>
        ///     Remove the client from this region
        /// </summary>
        /// <param name="service"></param>
        void RemoveClientFromRegion(IRegionClientCapsService service);

        /// <summary>
        ///     Get an agent's Caps by UUID
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        IRegionClientCapsService GetClient(UUID AgentID);

        /// <summary>
        ///     Get all clients in this region
        /// </summary>
        /// <returns></returns>
        List<IRegionClientCapsService> GetClients();
    }

    public interface IExternalCapsHandler
    {
        OSDMap GetExternalCaps(UUID agentID, GridRegion region);
        void RemoveExternalCaps(UUID agentID, GridRegion region);
    }

    public interface IExternalCapsRequestHandler
    {
        string Name { get; }
        void IncomingCapsRequest(UUID agentID, GridRegion region, ISimulationBase simbase, ref OSDMap capURLs);
        void IncomingCapsDestruction();
    }
}