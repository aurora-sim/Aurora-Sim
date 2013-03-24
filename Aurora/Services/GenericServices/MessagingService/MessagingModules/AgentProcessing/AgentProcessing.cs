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

using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.DatabaseInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using GridRegion = Aurora.Framework.Services.GridRegion;

namespace Aurora.Services
{
    public class AgentProcessing : IService, IAgentProcessing
    {
        #region Declares

        protected int MaxVariableRegionSight = 512;
        protected bool VariableRegionSight;
        protected bool m_enabled = true;
        protected IConfigSource _config;
        protected IRegistryCore m_registry;
        protected ICapsService m_capsService;

        #endregion

        #region IService Members

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            _config = config;
            m_registry = registry;
            IConfig agentConfig = config.Configs["AgentProcessing"];
            if (agentConfig != null)
            {
                m_enabled = agentConfig.GetString("Module", "AgentProcessing") == "AgentProcessing";
                VariableRegionSight = agentConfig.GetBoolean("UseVariableRegionSightDistance", VariableRegionSight);
                MaxVariableRegionSight = agentConfig.GetInt("MaxDistanceVariableRegionSightDistance",
                                                            MaxVariableRegionSight);
            }
            if (m_enabled)
                m_registry.RegisterModuleInterface<IAgentProcessing>(this);
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual void FinishedStartup()
        {
            m_capsService = m_registry.RequestModuleInterface<ICapsService>();
            //Also look for incoming messages to display
            if (m_enabled)
                m_registry.RequestModuleInterface<ISyncMessageRecievedService>().OnMessageReceived += OnMessageReceived;
        }

        #endregion

        #region Message Received

        protected virtual OSDMap OnMessageReceived(OSDMap message)
        {
            if (!message.ContainsKey("Method"))
                return null;

            if (m_capsService == null)
                return null;

            UUID AgentID = message["AgentID"].AsUUID();
            UUID requestingRegion = message["RequestingRegion"].AsUUID();

            IClientCapsService clientCaps = m_capsService.GetClientCapsService(AgentID);

            IRegionClientCapsService regionCaps = null;
            if (clientCaps != null)
                regionCaps = clientCaps.GetCapsService(requestingRegion);
            if (message["Method"] == "LogoutRegionAgents")
            {
                LogOutAllAgentsForRegion(requestingRegion);
            }
            else if (message["Method"] == "RegionIsOnline")
                //This gets fired when the scene is fully finished starting up
            {
                //Log out all the agents first, then add any child agents that should be in this region
                LogOutAllAgentsForRegion(requestingRegion);
                IGridService GridService = m_registry.RequestModuleInterface<IGridService>();
                if (GridService != null)
                {
                    GridRegion requestingGridRegion = GridService.GetRegionByUUID(null, requestingRegion);
                    if (requestingGridRegion != null)
                        Util.FireAndForget((o) => EnableChildAgentsForRegion(requestingGridRegion));
                }
            }
            else if (message["Method"] == "ArrivedAtDestination")
            {
                if (regionCaps == null || clientCaps == null)
                    return null;
                //Recieved a callback
                if (clientCaps.InTeleport) //Only set this if we are in a teleport, 
                    //  otherwise (such as on login), this won't check after the first tp!
                    clientCaps.CallbackHasCome = true;

                regionCaps.Disabled = false;

                //The agent is getting here for the first time (eg. login)
                OSDMap body = ((OSDMap) message["Message"]);

                //Parse the OSDMap
                int DrawDistance = body["DrawDistance"].AsInteger();

                AgentCircuitData circuitData = new AgentCircuitData();
                circuitData.FromOSD((OSDMap)body["Circuit"]);

                //Now do the creation
                EnableChildAgents(AgentID, requestingRegion, DrawDistance, circuitData);
            }
            else if (message["Method"] == "CancelTeleport")
            {
                if (regionCaps == null || clientCaps == null)
                    return null;
                //Only the region the client is root in can do this
                IRegionClientCapsService rootCaps = clientCaps.GetRootCapsService();
                if (rootCaps != null && rootCaps.RegionHandle == regionCaps.RegionHandle)
                {
                    //The user has requested to cancel the teleport, stop them.
                    clientCaps.RequestToCancelTeleport = true;
                    regionCaps.Disabled = false;
                }
            }
            else if (message["Method"] == "AgentLoggedOut")
            {
                //ONLY if the agent is root do we even consider it
                if (regionCaps != null && regionCaps.RootAgent)
                {
                    OSDMap body = ((OSDMap) message["Message"]);

                    AgentPosition pos = new AgentPosition();
                    pos.FromOSD((OSDMap)body["AgentPos"]);

                    regionCaps.Disabled = true;

                    Util.FireAndForget((o) =>
                                           {
                                               LogoutAgent(regionCaps, false); //The root is killing itself
                                               SendChildAgentUpdate(pos, regionCaps);
                                           });
                }
            }
            else if (message["Method"] == "SendChildAgentUpdate")
            {
                if (regionCaps == null || clientCaps == null)
                    return null;
                IRegionClientCapsService rootCaps = clientCaps.GetRootCapsService();
                if (rootCaps != null && rootCaps.RegionHandle == regionCaps.RegionHandle) //Has to be root
                {
                    OSDMap body = ((OSDMap) message["Message"]);

                    AgentPosition pos = new AgentPosition();
                    pos.FromOSD((OSDMap) body["AgentPos"]);

                    SendChildAgentUpdate(pos, regionCaps);
                    regionCaps.Disabled = false;
                }
            }
            else if (message["Method"] == "TeleportAgent")
            {
                if (regionCaps == null || clientCaps == null)
                    return null;
                IRegionClientCapsService rootCaps = clientCaps.GetRootCapsService();
                if (rootCaps != null && rootCaps.RegionHandle == regionCaps.RegionHandle)
                {
                    OSDMap body = ((OSDMap) message["Message"]);

                    GridRegion destination = new GridRegion();
                    destination.FromOSD((OSDMap) body["Region"]);

                    uint TeleportFlags = body["TeleportFlags"].AsUInteger();
                    int DrawDistance = body["DrawDistance"].AsInteger();

                    AgentCircuitData Circuit = new AgentCircuitData();
                    Circuit.FromOSD((OSDMap)body["Circuit"]);

                    AgentData AgentData = new AgentData();
                    AgentData.FromOSD((OSDMap)body["AgentData"]);
                    regionCaps.Disabled = false;

                    //Don't need to wait for this to finish on the main http thread
                    Util.FireAndForget((o) =>
                                           {
                                               string reason = "";
                                               TeleportAgent(ref destination, TeleportFlags,
                                                             Circuit, AgentData, AgentID, requestingRegion, out reason);
                                           });
                    return null;
                }
            }
            else if (message["Method"] == "CrossAgent")
            {
                if (regionCaps == null || clientCaps == null)
                    return null;
                IRegionClientCapsService rootCaps = clientCaps.GetRootCapsService();
                if (rootCaps == null || rootCaps.RegionHandle == regionCaps.RegionHandle)
                {
                    //This is a simulator message that tells us to cross the agent
                    OSDMap body = ((OSDMap) message["Message"]);

                    Vector3 pos = body["Pos"].AsVector3();
                    Vector3 Vel = body["Vel"].AsVector3();
                    GridRegion Region = new GridRegion();
                    Region.FromOSD((OSDMap) body["Region"]);
                    AgentCircuitData Circuit = new AgentCircuitData();
                    Circuit.FromOSD((OSDMap)body["Circuit"]);
                    AgentData AgentData = new AgentData();
                    AgentData.FromOSD((OSDMap)body["AgentData"]);
                    regionCaps.Disabled = false;

                    Util.FireAndForget((o) =>
                                           {
                                               string reason = "";
                                               CrossAgent(Region, pos, Vel, Circuit, AgentData,
                                                          AgentID, requestingRegion, out reason);
                                           });
                    return null;
                }
                else if (clientCaps.InTeleport)
                {
                    OSDMap result = new OSDMap();
                    result["success"] = false;
                    result["Note"] = false;
                    return result;
                }
                else
                {
                    OSDMap result = new OSDMap();
                    result["success"] = false;
                    result["Note"] = false;
                    return result;
                }
            }
            return null;
        }

        #region Logout Agent

        public virtual void LogoutAgent(IRegionClientCapsService regionCaps, bool kickRootAgent)
        {
            //Close all neighbor agents as well, the root is closing itself, so don't call them
            ISimulationService SimulationService = m_registry.RequestModuleInterface<ISimulationService>();
            IGridService GridService = m_registry.RequestModuleInterface<IGridService>();
            IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
            IEventQueueService eventQueue = m_registry.RequestModuleInterface<IEventQueueService>();

            if (SimulationService != null && GridService != null)
            {
                foreach (IRegionClientCapsService regionClient in regionCaps.ClientCaps.GetCapsServices().
                                                                             Where(
                                                                                 regionClient =>
                                                                                 regionClient.RegionHandle !=
                                                                                 regionCaps.RegionHandle &&
                                                                                 regionClient.Region != null))
                {
                    SimulationService.CloseAgent(regionClient.Region, regionCaps.AgentID);
                }
            }
            if (kickRootAgent && regionCaps.Region != null) //Kick the root agent then
                SimulationService.CloseAgent(regionCaps.Region, regionCaps.AgentID);
            //Close all caps
            regionCaps.ClientCaps.Close();

            if (agentInfoService != null)
                agentInfoService.SetLoggedIn(regionCaps.AgentID.ToString(), false, UUID.Zero, "");

            m_capsService.RemoveCAPS(regionCaps.AgentID);
        }

        public virtual void LogOutAllAgentsForRegion(UUID requestingRegion)
        {
            IRegionCapsService fullregionCaps = m_capsService.GetCapsForRegion(requestingRegion);
            IEventQueueService eqs = m_registry.RequestModuleInterface<IEventQueueService>();
            if (fullregionCaps != null && eqs != null)
            {
                foreach (IRegionClientCapsService regionClientCaps in fullregionCaps.GetClients())
                {
                    //We can send this here, because we ONLY send this when the region is going down for a loong time
                    eqs.DisableSimulator(regionClientCaps.AgentID, regionClientCaps.RegionHandle,
                                         regionClientCaps.Region.RegionID);
                }
                //Now kill the region in the caps Service, DO THIS FIRST, otherwise you get an infinite loop later in the IClientCapsService when it tries to remove itself from the IRegionCapsService
                m_capsService.RemoveCapsForRegion(requestingRegion);
                //Close all regions and remove them from the region
                fullregionCaps.Close();
            }
        }

        #endregion

        #region EnableChildAgents

        public virtual bool EnableChildAgentsForRegion(GridRegion requestingRegion)
        {
            int count = 0;
            bool informed = true;
            List<GridRegion> neighbors = GetNeighbors(null, requestingRegion, 0);

            foreach (GridRegion neighbor in neighbors)
            {
                //MainConsole.Instance.WarnFormat("--> Going to send child agent to {0}, new agent {1}", neighbour.RegionName, newAgent);

                IRegionCapsService regionCaps = m_capsService.GetCapsForRegion(neighbor.RegionID);
                if (regionCaps == null) //If there isn't a region caps, there isn't an agent in this sim
                    continue;
                List<UUID> usersInformed = new List<UUID>();
                foreach (IRegionClientCapsService regionClientCaps in regionCaps.GetClients())
                {
                    if (usersInformed.Contains(regionClientCaps.AgentID) || !regionClientCaps.RootAgent ||
                        AllScopeIDImpl.CheckScopeIDs(regionClientCaps.ClientCaps.AccountInfo.AllScopeIDs, neighbor) ==
                        null)
                        //Only inform agents once
                        continue;

                    AgentCircuitData regionCircuitData = regionClientCaps.CircuitData.Copy();
                    regionCircuitData.IsChildAgent = true;
                    string reason; //Tell the region about it
                    if (!InformClientOfNeighbor(regionClientCaps.AgentID, regionClientCaps.Region.RegionID,
                                                regionCircuitData, ref requestingRegion, (uint) TeleportFlags.Default,
                                                null, out reason))
                        informed = false;
                    else
                        usersInformed.Add(regionClientCaps.AgentID);
                }
                count++;
            }
            return informed;
        }

        public virtual void EnableChildAgents(UUID AgentID, UUID requestingRegion, int DrawDistance, AgentCircuitData circuit)
        {
            Util.FireAndForget((o) =>
                                   {
                                       int count = 0;
                                       IClientCapsService clientCaps = m_capsService.GetClientCapsService(AgentID);
                                       GridRegion ourRegion =
                                           m_registry.RequestModuleInterface<IGridService>().GetRegionByUUID(
                                               clientCaps.AccountInfo.AllScopeIDs, requestingRegion);
                                       if (ourRegion == null)
                                       {
                                           MainConsole.Instance.Info(
                                               "[AgentProcessing]: Failed to inform neighbors about new agent, could not find our region.");
                                           return;
                                       }
                                       List<GridRegion> neighbors = GetNeighbors(clientCaps.AccountInfo.AllScopeIDs,
                                                                                 ourRegion, DrawDistance);

                                       //Fix the root agents dd
                                       foreach (GridRegion neighbor in neighbors)
                                       {
                                           if (neighbor.RegionID != requestingRegion &&
                                               clientCaps.GetCapsService(neighbor.RegionID) == null)
                                           {
                                               string reason;
                                               AgentCircuitData regionCircuitData =
                                                   clientCaps.GetRootCapsService().CircuitData.Copy();
                                               GridRegion nCopy = neighbor;
                                               regionCircuitData.IsChildAgent = true;
                                               InformClientOfNeighbor(AgentID, requestingRegion, regionCircuitData,
                                                                      ref nCopy,
                                                                      (uint) TeleportFlags.Default, null, out reason);
                                           }
                                           count++;
                                       }
                                   });
        }

        #endregion

        #region Inform Client Of Neighbor

        /// <summary>
        ///     Async component for informing client of which neighbors exist
        /// </summary>
        /// <remarks>
        ///     This needs to run asynchronously, as a network timeout may block the thread for a long while
        /// </remarks>
        /// <param name="AgentID"></param>
        /// <param name="requestingRegion"></param>
        /// <param name="circuitData"></param>
        /// <param name="neighbor"></param>
        /// <param name="TeleportFlags"></param>
        /// <param name="agentData"></param>
        /// <param name="reason"></param>
        public virtual bool InformClientOfNeighbor(UUID AgentID, UUID requestingRegion, AgentCircuitData circuitData,
                                                   ref GridRegion neighbor,
                                                   uint TeleportFlags, AgentData agentData, out string reason)
        {
            if (neighbor == null || neighbor.RegionHandle == 0)
            {
                reason = "Could not find neighbor to inform";
                return false;
            }
            /*if ((neighbor.Flags & (int)Aurora.Framework.RegionFlags.RegionOnline) == 0 &&
                (neighbor.Flags & (int)(Aurora.Framework.RegionFlags.Foreign | Aurora.Framework.RegionFlags.Hyperlink)) == 0)
            {
                reason = "The region you are attempting to teleport to is offline";
                return false;
            }*/
            MainConsole.Instance.Info("[AgentProcessing]: Starting to inform client about neighbor " +
                                      neighbor.RegionName);

            //Notes on this method
            // 1) the SimulationService.CreateAgent MUST have a fixed CapsUrl for the region, so we have to create (if needed)
            //       a new Caps handler for it.
            // 2) Then we can call the methods (EnableSimulator and EstatablishAgentComm) to tell the client the new Urls
            // 3) This allows us to make the Caps on the grid server without telling any other regions about what the
            //       Urls are.

            ISimulationService SimulationService = m_registry.RequestModuleInterface<ISimulationService>();
            if (SimulationService != null)
            {
                IClientCapsService clientCaps = m_capsService.GetClientCapsService(AgentID);

                IRegionClientCapsService oldRegionService = clientCaps.GetCapsService(neighbor.RegionID);

                //If its disabled, it should be removed, so kill it!
                if (oldRegionService != null && oldRegionService.Disabled)
                {
                    clientCaps.RemoveCAPS(neighbor.RegionID);
                    oldRegionService = null;
                }

                bool newAgent = oldRegionService == null;
                IRegionClientCapsService otherRegionService = clientCaps.GetOrCreateCapsService(neighbor.RegionID,
                                                                                                CapsUtil.GetCapsSeedPath
                                                                                                    (CapsUtil.
                                                                                                         GetRandomCapsObjectPath
                                                                                                         ()),
                                                                                                circuitData, 0);

                if (!newAgent)
                {
                    //Note: if the agent is already there, send an agent update then
                    bool result = true;
                    if (agentData != null)
                    {
                        agentData.IsCrossing = false;
                        result = SimulationService.UpdateAgent(neighbor, agentData);
                    }
                    if (result)
                        oldRegionService.Disabled = false;
                    reason = "";
                    return result;
                }

                int requestedPort = 0;
                CreateAgentResponse createAgentResponse;
                bool regionAccepted = CreateAgent(neighbor, otherRegionService, ref circuitData, SimulationService,
                                                  out createAgentResponse);
                reason = createAgentResponse.Reason;
                if (regionAccepted)
                {
                    IPAddress ipAddress = neighbor.ExternalEndPoint.Address;
                    //If the region accepted us, we should get a CAPS url back as the reason, if not, its not updated or not an Aurora region, so don't touch it.
                    string ip = createAgentResponse.OurIPForClient;
                    if (!IPAddress.TryParse(ip, out ipAddress))
#pragma warning disable 618
                        ipAddress = Dns.GetHostByName(ip).AddressList[0];
#pragma warning restore 618
                    otherRegionService.AddCAPS(createAgentResponse.CapsURIs);

                    if (ipAddress == null)
                        ipAddress = neighbor.ExternalEndPoint.Address;
                    if (requestedPort == 0)
                        requestedPort = neighbor.ExternalEndPoint.Port;
                    otherRegionService = clientCaps.GetCapsService(neighbor.RegionID);
                    otherRegionService.LoopbackRegionIP = ipAddress;
                    otherRegionService.CircuitData.RegionUDPPort = requestedPort;
                    circuitData.RegionUDPPort = requestedPort; //Fix the port

                    IEventQueueService EQService = m_registry.RequestModuleInterface<IEventQueueService>();

                    EQService.EnableSimulator(neighbor.RegionHandle,
                                              ipAddress.GetAddressBytes(),
                                              requestedPort, AgentID,
                                              neighbor.RegionSizeX, neighbor.RegionSizeY, requestingRegion);

                    // EnableSimulator makes the client send a UseCircuitCode message to the destination, 
                    // which triggers a bunch of things there.
                    // So let's wait
                    Thread.Sleep(300);
                    EQService.EstablishAgentCommunication(AgentID, neighbor.RegionHandle,
                                                          ipAddress.GetAddressBytes(),
                                                          requestedPort, otherRegionService.CapsUrl,
                                                          neighbor.RegionSizeX,
                                                          neighbor.RegionSizeY,
                                                          requestingRegion);

                    MainConsole.Instance.Info("[AgentProcessing]: Completed inform client about neighbor " +
                                              neighbor.RegionName);
                }
                else
                {
                    clientCaps.RemoveCAPS(neighbor.RegionID);
                    reason = "Could not contact simulator";
                    MainConsole.Instance.Error("[AgentProcessing]: Failed to inform client about neighbor " +
                                               neighbor.RegionName +
                                               ", reason: " + reason);
                    return false;
                }
                return true;
            }
            reason = "SimulationService does not exist";
            MainConsole.Instance.Error("[AgentProcessing]: Failed to inform client about neighbor " +
                                       neighbor.RegionName +
                                       ", reason: " + reason + "!");
            return false;
        }

        #endregion

        #region Teleporting

        private int CloseNeighborCall;

        public virtual bool TeleportAgent(ref GridRegion destination, uint TeleportFlags,
                                          AgentCircuitData circuit, AgentData agentData, UUID AgentID,
                                          UUID requestingRegion,
                                          out string reason)
        {
            IClientCapsService clientCaps = m_capsService.GetClientCapsService(AgentID);
            IRegionClientCapsService regionCaps = clientCaps.GetCapsService(requestingRegion);
            ISimulationService SimulationService = m_registry.RequestModuleInterface<ISimulationService>();

            if (regionCaps == null || !regionCaps.RootAgent)
            {
                reason = "";
                ResetFromTransit(AgentID);
                return false;
            }

            bool result = false;
            try
            {
                bool callWasCanceled = false;

                if (SimulationService != null)
                {
                    //Set the user in transit so that we block duplicate tps and reset any cancelations
                    if (!SetUserInTransit(AgentID))
                    {
                        reason = "Already in a teleport";
                        SimulationService.FailedToTeleportAgent(regionCaps.Region, destination.RegionID,
                                                                AgentID, reason, false);
                        return false;
                    }

                    IGridService GridService = m_registry.RequestModuleInterface<IGridService>();
                    if (GridService != null)
                    {
                        //Inform the client of the neighbor if needed
                        circuit.IsChildAgent = false; //Force child status to the correct type
                        if (!InformClientOfNeighbor(AgentID, requestingRegion, circuit, ref destination, TeleportFlags,
                                                    agentData, out reason))
                        {
                            ResetFromTransit(AgentID);
                            return false;
                        }
                    }
                    else
                    {
                        reason = "Could not find the grid service";
                        ResetFromTransit(AgentID);
                        SimulationService.FailedToTeleportAgent(regionCaps.Region, destination.RegionID,
                                                                AgentID, reason, false);
                        return false;
                    }

                    IEventQueueService EQService = m_registry.RequestModuleInterface<IEventQueueService>();

                    IRegionClientCapsService otherRegion = clientCaps.GetCapsService(destination.RegionID);

                    EQService.TeleportFinishEvent(destination.RegionHandle, destination.Access,
                                                  otherRegion.LoopbackRegionIP,
                                                  otherRegion.CircuitData.RegionUDPPort,
                                                  otherRegion.CapsUrl,
                                                  4, AgentID, TeleportFlags,
                                                  destination.RegionSizeX, destination.RegionSizeY,
                                                  otherRegion.Region.RegionID);

                    // TeleportFinish makes the client send CompleteMovementIntoRegion (at the destination), which
                    // trigers a whole shebang of things there, including MakeRoot. So let's wait for confirmation
                    // that the client contacted the destination before we send the attachments and close things here.

                    result = WaitForCallback(AgentID, out callWasCanceled);
                    if (!result)
                    {
                        reason = !callWasCanceled ? "The teleport timed out" : "Cancelled";
                        if (!callWasCanceled)
                        {
                            MainConsole.Instance.Warn("[AgentProcessing]: Callback never came for teleporting agent " +
                                                      AgentID + ". Resetting.");
                            //Tell the region about it as well
                            SimulationService.FailedToTeleportAgent(regionCaps.Region, destination.RegionID,
                                                                    AgentID, reason, false);
                        }
                        //Close the agent at the place we just created if it isn't a neighbor
                        // 7/22 -- Kill the agent no matter what, it obviously is having issues getting there
                        //if (IsOutsideView (regionCaps.RegionX, destination.RegionLocX, regionCaps.Region.RegionSizeX, destination.RegionSizeX,
                        //    regionCaps.RegionY, destination.RegionLocY, regionCaps.Region.RegionSizeY, destination.RegionSizeY))
                        {
                            SimulationService.CloseAgent(destination, AgentID);
                            clientCaps.RemoveCAPS(destination.RegionID);
                        }
                    }
                    else
                    {
                        //Fix the root agent status
                        otherRegion.RootAgent = true;
                        regionCaps.RootAgent = false;

                        // Next, let's close the child agent connections that are too far away.
                        //if (useCallbacks || oldRegion != destination)//Only close it if we are using callbacks (Aurora region)
                        //Why? OpenSim regions need closed too, even if the protocol is kinda stupid
                        CloseNeighborAgents(regionCaps.Region, destination, AgentID);
                        IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
                        if (agentInfoService != null)
                            agentInfoService.SetLastPosition(AgentID.ToString(), destination.RegionID,
                                                             agentData.Position, Vector3.Zero, destination.ServerURI);

                        SimulationService.MakeChildAgent(AgentID, regionCaps.Region, false);
                        reason = "";
                    }
                }
                else
                    reason = "No SimulationService found!";
            }
            catch (Exception ex)
            {
                MainConsole.Instance.WarnFormat("[AgentProcessing]: Exception occured during agent teleport, {0}", ex);
                reason = "Exception occured.";
                if (SimulationService != null)
                    SimulationService.FailedToTeleportAgent(regionCaps.Region, destination.RegionID,
                                                            AgentID, reason, false);
            }
            //All done
            ResetFromTransit(AgentID);
            return result;
        }

        public virtual void CloseNeighborAgents(GridRegion oldRegion, GridRegion destination, UUID AgentID)
        {
            CloseNeighborCall++;
            int CloseNeighborCallNum = CloseNeighborCall;
            Util.FireAndForget(delegate
                                   {
                                       //Sleep for 10 seconds to give the agents a chance to cross and get everything right
                                       Thread.Sleep(10000);
                                       if (CloseNeighborCall != CloseNeighborCallNum)
                                           return; //Another was enqueued, kill this one

                                       //Now do a sanity check on the avatar
                                       IClientCapsService clientCaps = m_capsService.GetClientCapsService(
                                           AgentID);
                                       if (clientCaps == null)
                                           return;
                                       IRegionClientCapsService rootRegionCaps = clientCaps.GetRootCapsService();
                                       if (rootRegionCaps == null)
                                           return;
                                       IRegionClientCapsService ourRegionCaps =
                                           clientCaps.GetCapsService(destination.RegionID);
                                       if (ourRegionCaps == null)
                                           return;
                                       //If they handles arn't the same, the agent moved, and we can't be sure that we should close these agents
                                       if (rootRegionCaps.RegionHandle != ourRegionCaps.RegionHandle &&
                                           !clientCaps.InTeleport)
                                           return;

                                       IGridService service = m_registry.RequestModuleInterface<IGridService>();
                                       if (service != null)
                                       {
                                           List<GridRegion> NeighborsOfOldRegion =
                                               service.GetNeighbors(clientCaps.AccountInfo.AllScopeIDs, oldRegion);
                                           List<GridRegion> NeighborsOfDestinationRegion =
                                               service.GetNeighbors(clientCaps.AccountInfo.AllScopeIDs, destination);

                                           List<GridRegion> byebyeRegions = new List<GridRegion>(NeighborsOfOldRegion)
                                                                                {oldRegion};
                                           //Add the old region, because it might need closed too

                                           byebyeRegions.RemoveAll(delegate(GridRegion r)
                                                                       {
                                                                           if (r.RegionID == destination.RegionID)
                                                                               return true;
                                                                           else if (
                                                                               NeighborsOfDestinationRegion.Contains
                                                                                   (r))
                                                                               return true;
                                                                           return false;
                                                                       });

                                           if (byebyeRegions.Count > 0)
                                           {
                                               MainConsole.Instance.Info("[AgentProcessing]: Closing " +
                                                                         byebyeRegions.Count +
                                                                         " child agents around " + oldRegion.RegionName);
                                               SendCloseChildAgent(AgentID, byebyeRegions);
                                           }
                                       }
                                   });
        }

        public virtual void SendCloseChildAgent(UUID agentID, IEnumerable<GridRegion> regionsToClose)
        {
            IClientCapsService clientCaps = m_capsService.GetClientCapsService(agentID);
            //Close all agents that we've been given regions for
            foreach (GridRegion region in regionsToClose)
            {
                MainConsole.Instance.Info("[AgentProcessing]: Closing child agent in " + region.RegionName);
                IRegionClientCapsService regionClientCaps = clientCaps.GetCapsService(region.RegionID);
                if (regionClientCaps != null)
                {
                    m_registry.RequestModuleInterface<ISimulationService>().CloseAgent(region, agentID);
                    clientCaps.RemoveCAPS(region.RegionID);
                }
            }
        }

        protected void ResetFromTransit(UUID AgentID)
        {
            IClientCapsService clientCaps = m_capsService.GetClientCapsService(AgentID);
            if (clientCaps != null)
            {
                clientCaps.InTeleport = false;
                clientCaps.RequestToCancelTeleport = false;
                clientCaps.CallbackHasCome = false;
            }
        }

        protected bool SetUserInTransit(UUID AgentID)
        {
            IClientCapsService clientCaps = m_capsService.GetClientCapsService(AgentID);

            if (clientCaps.InTeleport)
            {
                MainConsole.Instance.Warn(
                    "[AgentProcessing]: Got a request to teleport during another teleport for agent " + AgentID + "!");
                return false; //What??? Stop here and don't go forward
            }

            clientCaps.InTeleport = true;
            clientCaps.RequestToCancelTeleport = false;
            clientCaps.CallbackHasCome = false;
            return true;
        }

        #region Callbacks

        protected bool WaitForCallback(UUID AgentID, out bool callWasCanceled)
        {
            IClientCapsService clientCaps = m_capsService.GetClientCapsService(AgentID);

            int count = 1000;
            while (!clientCaps.CallbackHasCome && count > 0)
            {
                if (clientCaps.RequestToCancelTeleport)
                {
                    //If the call was canceled, we need to break here 
                    //   now and tell the code that called us about it
                    callWasCanceled = true;
                    return true;
                }
                Thread.Sleep(10);
                count--;
            }
            //If we made it through the whole loop, we havn't been canceled,
            //    as we either have timed out or made it, so no checks are needed
            callWasCanceled = false;
            return clientCaps.CallbackHasCome;
        }

        protected bool WaitForCallback(UUID AgentID)
        {
            IClientCapsService clientCaps = m_capsService.GetClientCapsService(AgentID);

            int count = 100;
            while (!clientCaps.CallbackHasCome && count > 0)
            {
                //MainConsole.Instance.Debug("  >>> Waiting... " + count);
                Thread.Sleep(100);
                count--;
            }
            return clientCaps.CallbackHasCome;
        }

        #endregion

        #endregion

        #region Neighbors

        public virtual List<GridRegion> GetNeighbors(List<UUID> scopeIDs, GridRegion region, int userDrawDistance)
        {
            List<GridRegion> neighbors = new List<GridRegion>();
            if (VariableRegionSight && userDrawDistance != 0)
            {
                //Enforce the max draw distance
                if (userDrawDistance > MaxVariableRegionSight)
                    userDrawDistance = MaxVariableRegionSight;

                //Query how many regions fit in this size
                int xMin = (region.RegionLocX) - (userDrawDistance);
                int xMax = (region.RegionLocX) + (userDrawDistance);
                int yMin = (region.RegionLocY) - (userDrawDistance);
                int yMax = (region.RegionLocY) + (userDrawDistance);

                //Ask the grid service about the range
                neighbors = m_registry.RequestModuleInterface<IGridService>().GetRegionRange(scopeIDs,
                                                                                             xMin, xMax, yMin, yMax);
            }
            else
                neighbors = m_registry.RequestModuleInterface<IGridService>().GetNeighbors(scopeIDs, region);

            return neighbors;
        }

        #endregion

        #region Crossing

        public virtual bool CrossAgent(GridRegion crossingRegion, Vector3 pos,
                                       Vector3 velocity, AgentCircuitData circuit, AgentData cAgent, UUID AgentID,
                                       UUID requestingRegion, out string reason)
        {
            IClientCapsService clientCaps = m_capsService.GetClientCapsService(AgentID);
            IRegionClientCapsService requestingRegionCaps = clientCaps.GetCapsService(requestingRegion);
            ISimulationService SimulationService = m_registry.RequestModuleInterface<ISimulationService>();
            IGridService GridService = m_registry.RequestModuleInterface<IGridService>();
            try
            {
                if (SimulationService != null || GridService != null)
                {
                    //Set the user in transit so that we block duplicate tps and reset any cancelations
                    if (!SetUserInTransit(AgentID))
                    {
                        reason = "Already in a teleport";
                        SimulationService.FailedToTeleportAgent(requestingRegionCaps.Region, crossingRegion.RegionID,
                                                                AgentID, reason, true);
                        return false;
                    }

                    bool result = false;

                    //We need to get it from the grid service again so that we can get the simulation service urls correctly
                    // as regions don't get that info
                    crossingRegion = GridService.GetRegionByUUID(clientCaps.AccountInfo.AllScopeIDs,
                                                                 crossingRegion.RegionID);
                    cAgent.IsCrossing = true;
                    if (!SimulationService.UpdateAgent(crossingRegion, cAgent))
                    {
                        MainConsole.Instance.Warn("[AgentProcessing]: Failed to cross agent " + AgentID +
                                                  " because region did not accept it. Resetting.");
                        reason = "Failed to update an agent";
                        SimulationService.FailedToTeleportAgent(requestingRegionCaps.Region, crossingRegion.RegionID,
                                                                AgentID, reason, true);
                    }
                    else
                    {
                        IEventQueueService EQService = m_registry.RequestModuleInterface<IEventQueueService>();

                        //Add this for the viewer, but not for the sim, seems to make the viewer happier
                        int XOffset = crossingRegion.RegionLocX - requestingRegionCaps.RegionX;
                        pos.X += XOffset;

                        int YOffset = crossingRegion.RegionLocY - requestingRegionCaps.RegionY;
                        pos.Y += YOffset;

                        IRegionClientCapsService otherRegion = clientCaps.GetCapsService(crossingRegion.RegionID);

                        if (otherRegion == null)
                        {
                            if (
                                !InformClientOfNeighbor(AgentID, requestingRegion, circuit, ref crossingRegion, 0,
                                                        cAgent, out reason))
                            {
                                ResetFromTransit(AgentID);
                                SimulationService.FailedToTeleportAgent(requestingRegionCaps.Region,
                                                                        crossingRegion.RegionID,
                                                                        AgentID, reason, true);
                                return false;
                            }
                            else
                                otherRegion = clientCaps.GetCapsService(crossingRegion.RegionID);
                        }

                        //Tell the client about the transfer
                        EQService.CrossRegion(crossingRegion.RegionHandle, pos, velocity,
                                              otherRegion.LoopbackRegionIP,
                                              otherRegion.CircuitData.RegionUDPPort,
                                              otherRegion.CapsUrl,
                                              AgentID,
                                              circuit.SessionID,
                                              crossingRegion.RegionSizeX,
                                              crossingRegion.RegionSizeY,
                                              requestingRegionCaps.Region.RegionID);

                        result = WaitForCallback(AgentID);
                        if (!result)
                        {
                            MainConsole.Instance.Warn("[AgentProcessing]: Callback never came in crossing agent " +
                                                      circuit.AgentID +
                                                      ". Resetting.");
                            reason = "Crossing timed out";
                            SimulationService.FailedToTeleportAgent(requestingRegionCaps.Region, crossingRegion.RegionID,
                                                                    AgentID, reason, true);
                        }
                        else
                        {
                            // Next, let's close the child agent connections that are too far away.
                            //Fix the root agent status
                            otherRegion.RootAgent = true;
                            requestingRegionCaps.RootAgent = false;

                            CloseNeighborAgents(requestingRegionCaps.Region, crossingRegion, AgentID);
                            reason = "";
                            IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
                            if (agentInfoService != null)
                                agentInfoService.SetLastPosition(AgentID.ToString(), crossingRegion.RegionID,
                                                                 pos, Vector3.Zero, crossingRegion.ServerURI);
                            SimulationService.MakeChildAgent(AgentID, requestingRegionCaps.Region, true);
                        }
                    }

                    //All done
                    ResetFromTransit(AgentID);
                    return result;
                }
                else
                    reason = "Could not find the SimulationService";
            }
            catch (Exception ex)
            {
                MainConsole.Instance.WarnFormat("[AgentProcessing]: Failed to cross an agent into a new region. {0}", ex);
                if (SimulationService != null)
                    SimulationService.FailedToTeleportAgent(requestingRegionCaps.Region, crossingRegion.RegionID,
                                                            AgentID, "Exception occured", true);
            }
            ResetFromTransit(AgentID);
            reason = "Exception occured";
            return false;
        }

        #endregion

        #region Agent Update

        public virtual void SendChildAgentUpdate(AgentPosition agentpos, IRegionClientCapsService regionCaps)
        {
            Util.FireAndForget(delegate { SendChildAgentUpdateAsync(agentpos, regionCaps); });
        }

        public virtual void SendChildAgentUpdateAsync(AgentPosition agentpos, IRegionClientCapsService regionCaps)
        {
            //We need to send this update out to all the child agents this region has
            IGridService service = m_registry.RequestModuleInterface<IGridService>();
            if (service != null)
            {
                ISimulationService SimulationService = m_registry.RequestModuleInterface<ISimulationService>();
                if (SimulationService != null)
                {
                    //Set the last location in the database
                    IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
                    if (agentInfoService != null)
                    {
                        //Find the lookAt vector
                        Vector3 lookAt = new Vector3(agentpos.AtAxis.X, agentpos.AtAxis.Y, 0);

                        if (lookAt != Vector3.Zero)
                            lookAt = Util.GetNormalizedVector(lookAt);
                        //Update the database
                        agentInfoService.SetLastPosition(regionCaps.AgentID.ToString(), regionCaps.Region.RegionID,
                                                         agentpos.Position, lookAt, regionCaps.Region.ServerURI);
                    }

                    //Also update the service itself
                    regionCaps.LastPosition = agentpos.Position;
                    if (agentpos.UserGoingOffline)
                        return; //It just needed a last pos update

                    //Tell all neighbor regions about the new position as well
                    List<GridRegion> ourNeighbors = GetRegions(regionCaps.ClientCaps);
                    foreach (
                        GridRegion region in
                            ourNeighbors.Where(
                                region => region != null && !SimulationService.UpdateAgent(region, agentpos)))
                    {
                        MainConsole.Instance.Info("[AgentProcessing]: Failed to inform " + region.RegionName +
                                                  " about updating agent. ");
                    }
                }
            }
        }

        private List<GridRegion> GetRegions(IClientCapsService iClientCapsService)
        {
            return iClientCapsService.GetCapsServices().Select(rccs => rccs.Region).ToList();
        }

        #endregion

        #region Login initial agent

        public virtual LoginAgentArgs LoginAgent(GridRegion region, AgentCircuitData aCircuit)
        {
            bool success = false;
            string seedCap = "";
            string reason = "Could not find the simulation service";
            ISimulationService SimulationService = m_registry.RequestModuleInterface<ISimulationService>();
            if (SimulationService != null)
            {
                // The client is in the region, we need to make sure it gets the right Caps
                // If CreateAgent is successful, it passes back a OSDMap of params that the client 
                //    wants to inform us about, and it includes the Caps SEED url for the region
                IRegionClientCapsService regionClientCaps = null;
                IClientCapsService clientCaps = null;
                if (m_capsService != null)
                {
                    //Remove any previous users
                    seedCap = m_capsService.CreateCAPS(aCircuit.AgentID,
                                                       CapsUtil.GetCapsSeedPath(CapsUtil.GetRandomCapsObjectPath()),
                                                       region.RegionID, true, aCircuit, 0);

                    clientCaps = m_capsService.GetClientCapsService(aCircuit.AgentID);
                    regionClientCaps = clientCaps.GetCapsService(region.RegionID);
                }


                ICommunicationService commsService = m_registry.RequestModuleInterface<ICommunicationService>();
                if (commsService != null)
                    commsService.GetUrlsForUser(region, aCircuit.AgentID);
                //Make sure that we make userURLs if we need to

                int requestedUDPPort = 0;
                CreateAgentResponse createAgentResponse;
                // As we are creating the agent, we must also initialize the CapsService for the agent
                success = CreateAgent(region, regionClientCaps, ref aCircuit, SimulationService,
                                      out createAgentResponse);

                reason = createAgentResponse.Reason;
                if (!success) // If it failed, do not set up any CapsService for the client
                {
                    //Delete the Caps!
                    IAgentProcessing agentProcessor = m_registry.RequestModuleInterface<IAgentProcessing>();
                    if (agentProcessor != null && m_capsService != null)
                        agentProcessor.LogoutAgent(regionClientCaps, true);
                    else if (m_capsService != null)
                        m_capsService.RemoveCAPS(aCircuit.AgentID);
                    return new LoginAgentArgs
                               {
                                   Success = success,
                                   CircuitData = aCircuit,
                                   Reason = reason,
                                   SeedCap = seedCap
                               };
                }
                requestedUDPPort = createAgentResponse.RequestedUDPPort;
                if (requestedUDPPort == 0)
                    requestedUDPPort = region.ExternalEndPoint.Port;
                aCircuit.RegionUDPPort = requestedUDPPort;

                IPAddress ipAddress = regionClientCaps.Region.ExternalEndPoint.Address;
                if (m_capsService != null)
                {
                    //If the region accepted us, we should get a CAPS url back as the reason, if not, its not updated or not an Aurora region, so don't touch it.
                    string ip = createAgentResponse.OurIPForClient;
                    if (!IPAddress.TryParse(ip, out ipAddress))
#pragma warning disable 618
                        ipAddress = Dns.GetHostByName(ip).AddressList[0];
#pragma warning restore 618
                    region.ExternalEndPoint.Address = ipAddress;
                    //Fix this so that it gets sent to the client that way
                    regionClientCaps.AddCAPS(createAgentResponse.CapsURIs);
                    regionClientCaps = clientCaps.GetCapsService(region.RegionID);
                    regionClientCaps.LoopbackRegionIP = ipAddress;
                    regionClientCaps.CircuitData.RegionUDPPort = requestedUDPPort;
                    regionClientCaps.RootAgent = true;
                }
            }
            return new LoginAgentArgs {Success = success, CircuitData = aCircuit, Reason = reason, SeedCap = seedCap};
        }

        private bool CreateAgent(GridRegion region, IRegionClientCapsService regionCaps, ref AgentCircuitData aCircuit,
                                 ISimulationService SimulationService, out CreateAgentResponse response)
        {
            CachedUserInfo info = new CachedUserInfo();
            IAgentConnector con = Framework.Utilities.DataManager.RequestPlugin<IAgentConnector>();
            if (con != null)
                info.AgentInfo = con.GetAgent(aCircuit.AgentID);
            info.UserAccount = regionCaps.ClientCaps.AccountInfo;

            IGroupsServiceConnector groupsConn = Framework.Utilities.DataManager.RequestPlugin<IGroupsServiceConnector>();
            if (groupsConn != null)
            {
                info.ActiveGroup = groupsConn.GetGroupMembershipData(aCircuit.AgentID, UUID.Zero, aCircuit.AgentID);
                info.GroupMemberships = groupsConn.GetAgentGroupMemberships(aCircuit.AgentID, aCircuit.AgentID);
            }

            IOfflineMessagesConnector offlineMessConn =
                Framework.Utilities.DataManager.RequestPlugin<IOfflineMessagesConnector>();
            if (offlineMessConn != null)
                info.OfflineMessages = offlineMessConn.GetOfflineMessages(aCircuit.AgentID);

            IMuteListConnector muteConn = Framework.Utilities.DataManager.RequestPlugin<IMuteListConnector>();
            if (muteConn != null)
                info.MuteList = muteConn.GetMuteList(aCircuit.AgentID);

            IAvatarService avatarService = m_registry.RequestModuleInterface<IAvatarService>();
            if (avatarService != null)
                info.Appearance = avatarService.GetAppearance(aCircuit.AgentID);

            aCircuit.CachedUserInfo = info;
            return SimulationService.CreateAgent(region, aCircuit, aCircuit.TeleportFlags,
                                                 out response);
        }

        #endregion

        #endregion
    }
}