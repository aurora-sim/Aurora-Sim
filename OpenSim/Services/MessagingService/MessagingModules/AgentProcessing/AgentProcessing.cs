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
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Capabilities;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.MessagingService
{
    public class AgentProcessing : ConnectorBase, IService, IAgentProcessing
    {
        #region Declares

        protected int MaxVariableRegionSight = 512;
        protected bool VariableRegionSight;
        protected bool m_enabled = true;

        protected string _capsURL = "", _capsURLPassword = "";
        protected IConfigSource _config;

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
            {
                m_registry.RegisterModuleInterface<IAgentProcessing>(this);

                IConfig auroraConfig = _config.Configs["AuroraConnectors"];
                IGenericsConnector genericsConnector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
                if (!auroraConfig.GetBoolean("CapsServiceDoRemoteCalls", false))
                {
                    genericsConnector.AddGeneric(UUID.Zero, "CapsServiceURL", "CapsURL", new OSDWrapper { Info = MainServer.Instance.ServerURI }.ToOSD());
                    genericsConnector.AddGeneric(UUID.Zero, "CapsServiceURL", "CapsPassword", new OSDWrapper { Info = new System.Random().NextDouble() * 1000 }.ToOSD());
                }

                Init(registry, "CapsService");
            }
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public virtual void FinishedStartup()
        {
            //Also look for incoming messages to display
            if (m_enabled)
            {
                m_registry.RequestModuleInterface<IAsyncMessageRecievedService>().OnMessageReceived += OnMessageReceived;
                ReadRemoteCapsPassword();
            }
        }

        private void ReadRemoteCapsPassword()
        {
            IConfig handlerConfig = _config.Configs["AuroraConnectors"];
            IGenericsConnector genericsConnector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            if (handlerConfig.GetBoolean("CapsServiceDoRemoteCalls", false))
            {
                OSDWrapper wrapper = genericsConnector.GetGeneric<OSDWrapper>(UUID.Zero, "CapsServiceURL", "CapsURL");
                if (wrapper != null)
                {
                    _capsURL = wrapper.Info.AsString();
                    OSDWrapper w = genericsConnector.GetGeneric<OSDWrapper>(UUID.Zero, "CapsServiceURL", "CapsPassword");
                    _capsURLPassword = w.Info.AsString();
                    base.SetPassword(_capsURLPassword);
                }
            }
        }

        #endregion

        #region Message Received

        protected virtual OSDMap OnMessageReceived(OSDMap message)
        {
            if (!message.ContainsKey("Method"))
                return null;

            UUID AgentID = message["AgentID"].AsUUID();
            ulong requestingRegion = message["RequestingRegion"].AsULong();
            ICapsService capsService = m_registry.RequestModuleInterface<ICapsService>();
            if (capsService == null)
                return new OSDMap();

            IClientCapsService clientCaps = capsService.GetClientCapsService(AgentID);

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
                    int x, y;
                    Util.UlongToInts(requestingRegion, out x, out y);
                    GridRegion requestingGridRegion = GridService.GetRegionByPosition(null, x, y);
                    if (requestingGridRegion != null)
                        Util.FireAndForget((o) => EnableChildAgentsForRegion(requestingGridRegion));
                }
            }
            else if (message["Method"] == "DisableSimulator")
            {
                //KILL IT!
                if (regionCaps == null || clientCaps == null)
                    return null;
                IEventQueueService eventQueue = m_registry.RequestModuleInterface<IEventQueueService>();
                eventQueue.DisableSimulator(regionCaps.AgentID, regionCaps.RegionHandle);
                //regionCaps.Close();
                //clientCaps.RemoveCAPS(requestingRegion);
                regionCaps.Disabled = true;
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
                circuitData.UnpackAgentCircuitData((OSDMap) body["Circuit"]);

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
                if (regionCaps != null)
                {
                    if (regionCaps.RootAgent)
                    {
                        LogoutAgent(regionCaps, false); //The root is killing itself
                    }
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
                    pos.Unpack((OSDMap) body["AgentPos"]);

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
                    Circuit.UnpackAgentCircuitData((OSDMap) body["Circuit"]);

                    AgentData AgentData = new AgentData();
                    AgentData.Unpack((OSDMap) body["AgentData"]);
                    regionCaps.Disabled = false;
                    string ResponseURL = message["ResponseURL"].AsString();
                    if (ResponseURL == "")
                    {
                        OSDMap result = new OSDMap();
                        string reason = "";
                        result["success"] = TeleportAgent(ref destination, TeleportFlags, DrawDistance,
                                                          Circuit, AgentData, AgentID, requestingRegion, out reason);
                        result["Reason"] = reason;
                        //Remove the region flags, not the regions problem
                        destination.Flags = 0;
                        result["Destination"] = destination.ToOSD(); //Send back the new destination
                        return result;
                    }
                    else
                    {
                        Util.FireAndForget(delegate
                        {
                            OSDMap result = new OSDMap();
                            string reason = "";
                            result["success"] = TeleportAgent(ref destination, TeleportFlags, DrawDistance,
                                                              Circuit, AgentData, AgentID, requestingRegion, out reason);
                            result["Reason"] = reason;
                            //Remove the region flags, not the regions problem
                            destination.Flags = 0;
                            result["Destination"] = destination.ToOSD(); //Send back the new destination
                            WebUtils.PostToService(ResponseURL, result);
                        });
                        return new OSDMap() { new KeyValuePair<string, OSD>("WillHaveResponse", true) };
                    }
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
                    Circuit.UnpackAgentCircuitData((OSDMap) body["Circuit"]);
                    AgentData AgentData = new AgentData();
                    AgentData.Unpack((OSDMap) body["AgentData"]);
                    regionCaps.Disabled = false;

                    string ResponseURL = message["ResponseURL"].AsString();
                    if (ResponseURL == "")
                    {
                        OSDMap result = new OSDMap();
                        string reason = "";
                        result["success"] = CrossAgent(Region, pos, Vel, Circuit, AgentData,
                                                       AgentID, requestingRegion, out reason);
                        result["reason"] = reason;
                        return result;
                    }
                    else
                    {
                        Util.FireAndForget(delegate
                        {
                            OSDMap result = new OSDMap();
                            string reason = "";
                            result["success"] = CrossAgent(Region, pos, Vel, Circuit, AgentData,
                                                           AgentID, requestingRegion, out reason);
                            result["reason"] = reason;
                            if (ResponseURL != null)
                                WebUtils.PostToService(ResponseURL, result);
                        });
                        return new OSDMap() { new KeyValuePair<string, OSD>("WillHaveResponse", true) };
                    }
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
            if (SimulationService != null)
            {
                IGridService GridService = m_registry.RequestModuleInterface<IGridService>();
                if (GridService != null)
                {
#if (!ISWIN)
                    foreach (IRegionClientCapsService regionClient in regionCaps.ClientCaps.GetCapsServices())
                    {
                        if (regionClient.RegionHandle != regionCaps.RegionHandle && regionClient.Region != null)
                        {
                            SimulationService.CloseAgent(regionClient.Region, regionCaps.AgentID);
                        }
                    }
#else
                    foreach (IRegionClientCapsService regionClient in regionCaps.ClientCaps.GetCapsServices().Where(regionClient => regionClient.RegionHandle != regionCaps.RegionHandle && regionClient.Region != null))
                    {
                        SimulationService.CloseAgent(regionClient.Region, regionCaps.AgentID);
                    }
#endif
                }
            }
            if (kickRootAgent && regionCaps.Region != null) //Kick the root agent then
                SimulationService.CloseAgent(regionCaps.Region, regionCaps.AgentID);
            //Close all caps
            regionCaps.ClientCaps.Close();

            IAgentInfoService agentInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
            if (agentInfoService != null)
                agentInfoService.SetLoggedIn(regionCaps.AgentID.ToString(), false, true, UUID.Zero);

            ICapsService capsService = m_registry.RequestModuleInterface<ICapsService>();
            if (capsService != null)
                capsService.RemoveCAPS(regionCaps.AgentID);
        }

        public virtual void LogOutAllAgentsForRegion(ulong requestingRegion)
        {
            IRegionCapsService fullregionCaps =
                m_registry.RequestModuleInterface<ICapsService>().GetCapsForRegion(requestingRegion);
            if (fullregionCaps != null)
            {
                IEventQueueService eqs = m_registry.RequestModuleInterface<IEventQueueService>();
                if (eqs != null)
                {
                    foreach (IRegionClientCapsService regionClientCaps in fullregionCaps.GetClients())
                    {
                        //We can send this here, because we ONLY send this when the region is going down for a loong time
                        eqs.DisableSimulator(regionClientCaps.AgentID, regionClientCaps.RegionHandle);
                    }
                }
                //Now kill the region in the caps Service, DO THIS FIRST, otherwise you get an infinite loop later in the IClientCapsService when it tries to remove itself from the IRegionCapsService
                m_registry.RequestModuleInterface<ICapsService>().RemoveCapsForRegion(requestingRegion);
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

                IRegionCapsService regionCaps =
                    m_registry.RequestModuleInterface<ICapsService>().GetCapsForRegion(neighbor.RegionHandle);
                if (regionCaps == null) //If there isn't a region caps, there isn't an agent in this sim
                    continue;
                List<UUID> usersInformed = new List<UUID>();
                foreach (IRegionClientCapsService regionClientCaps in regionCaps.GetClients())
                {
                    if (usersInformed.Contains(regionClientCaps.AgentID) || !regionClientCaps.RootAgent || 
                        AllScopeIDImpl.CheckScopeIDs(regionClientCaps.ClientCaps.AccountInfo.AllScopeIDs, neighbor) == null)
                        //Only inform agents once
                        continue;

                    AgentCircuitData regionCircuitData = regionClientCaps.CircuitData.Copy();
                    regionCircuitData.child = true; //Fix child agent status
                    regionCircuitData.roothandle = requestingRegion.RegionHandle;
                    regionCircuitData.reallyischild = true;
                    string reason; //Tell the region about it
                    bool useCallbacks = false;
                    if (!InformClientOfNeighbor(regionClientCaps.AgentID, regionClientCaps.RegionHandle,
                                                regionCircuitData, ref requestingRegion, (uint) TeleportFlags.Default,
                                                null, out reason, out useCallbacks))
                        informed = false;
                    else
                        usersInformed.Add(regionClientCaps.AgentID);
                }
                count++;
            }
            return informed;
        }

        public virtual void EnableChildAgents(UUID AgentID, ulong requestingRegion, int DrawDistance,
                                              AgentCircuitData circuit)
        {
            Util.FireAndForget((o) =>
                                   {
                                       int count = 0;
                                       int x, y;
                                       ICapsService capsService = m_registry.RequestModuleInterface<ICapsService>();
                                       IClientCapsService clientCaps = capsService.GetClientCapsService(AgentID);
                                       Util.UlongToInts(requestingRegion, out x, out y);
                                       GridRegion ourRegion =
                                           m_registry.RequestModuleInterface<IGridService>().GetRegionByPosition(
                                               clientCaps.AccountInfo.AllScopeIDs, x, y);
                                       if (ourRegion == null)
                                       {
                                           MainConsole.Instance.Info(
                                               "[AgentProcessing]: Failed to inform neighbors about new agent, could not find our region.");
                                           return;
                                       }
                                       List<GridRegion> neighbors = GetNeighbors(clientCaps.AccountInfo.AllScopeIDs, ourRegion, DrawDistance);

                                       clientCaps.GetRootCapsService().CircuitData.DrawDistance = DrawDistance;
                                           //Fix the root agents dd
                                       foreach (GridRegion neighbor in neighbors)
                                       {
                                           if (neighbor.RegionHandle != requestingRegion &&
                                               clientCaps.GetCapsService(neighbor.RegionHandle) == null)
                                           {
                                               string reason;
                                               AgentCircuitData regionCircuitData =
                                                   clientCaps.GetRootCapsService().CircuitData.Copy();
                                               GridRegion nCopy = neighbor;
                                               regionCircuitData.child = true; //Fix child agent status
                                               regionCircuitData.roothandle = requestingRegion;
                                               regionCircuitData.reallyischild = true;
                                               regionCircuitData.DrawDistance = DrawDistance;
                                               bool useCallbacks = false;
                                               InformClientOfNeighbor(AgentID, requestingRegion, regionCircuitData,
                                                                      ref nCopy,
                                                                      (uint) TeleportFlags.Default, null, out reason,
                                                                      out useCallbacks);
                                           }
                                           count++;
                                       }
                                   });
        }

        public virtual void EnableChildAgentsForPosition(IRegionClientCapsService caps, Vector3 position)
        {
            Util.FireAndForget((o) =>
                                   {
                                       int count = 0;
                                       IGridService gridService = m_registry.RequestModuleInterface<IGridService>();
                                       int xMin = (caps.Region.RegionLocX) + (int)(position.X) - ((gridService != null ? gridService.GetRegionViewSize() : 1) * caps.Region.RegionSizeX);
                                       int xMax = (caps.Region.RegionLocX) + (int) (position.X) + 256;
                                       int yMin = (caps.Region.RegionLocY) + (int)(position.Y) - ((gridService != null ? gridService.GetRegionViewSize() : 1) * caps.Region.RegionSizeY);
                                       int yMax = (caps.Region.RegionLocY) + (int) (position.Y) + 256;

                                       //Ask the grid service about the range
                                       List<GridRegion> neighbors =
                                           m_registry.RequestModuleInterface<IGridService>().GetRegionRange(caps.ClientCaps.AccountInfo.AllScopeIDs,
                                                                                                            xMin, xMax,
                                                                                                            yMin, yMax);

                                       ICapsService capsService = m_registry.RequestModuleInterface<ICapsService>();
                                       IClientCapsService clientCaps = capsService.GetClientCapsService(caps.AgentID);
                                       foreach (GridRegion neighbor in neighbors)
                                       {
                                           if (neighbor.RegionHandle != caps.RegionHandle &&
                                               clientCaps.GetCapsService(neighbor.RegionHandle) == null)
                                           {
                                               string reason;
                                               AgentCircuitData regionCircuitData = caps.CircuitData.Copy();
                                               GridRegion nCopy = neighbor;
                                               regionCircuitData.child = true; //Fix child agent status
                                               regionCircuitData.roothandle = caps.RegionHandle;
                                               regionCircuitData.reallyischild = true;
                                               bool useCallbacks = false;
                                               InformClientOfNeighbor(caps.AgentID, caps.RegionHandle, regionCircuitData,
                                                                      ref nCopy,
                                                                      (uint) TeleportFlags.Default, null, out reason,
                                                                      out useCallbacks);
                                           }
                                           count++;
                                       }
                                   });
        }

        #endregion

        #region Inform Client Of Neighbor

        /// <summary>
        ///   Async component for informing client of which neighbors exist
        /// </summary>
        /// <remarks>
        ///   This needs to run asynchronously, as a network timeout may block the thread for a long while
        /// </remarks>
        /// <param name = "remoteClient"></param>
        /// <param name = "a"></param>
        /// <param name = "regionHandle"></param>
        /// <param name = "endPoint"></param>
        public virtual bool InformClientOfNeighbor(UUID AgentID, ulong requestingRegion, AgentCircuitData circuitData,
                                                   ref GridRegion neighbor,
                                                   uint TeleportFlags, AgentData agentData, out string reason,
                                                   out bool useCallbacks)
        {
            useCallbacks = true;
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
            MainConsole.Instance.Info("[AgentProcessing]: Starting to inform client about neighbor " + neighbor.RegionName);

            //Notes on this method
            // 1) the SimulationService.CreateAgent MUST have a fixed CapsUrl for the region, so we have to create (if needed)
            //       a new Caps handler for it.
            // 2) Then we can call the methods (EnableSimulator and EstatablishAgentComm) to tell the client the new Urls
            // 3) This allows us to make the Caps on the grid server without telling any other regions about what the
            //       Urls are.

            ISimulationService SimulationService = m_registry.RequestModuleInterface<ISimulationService>();
            if (SimulationService != null)
            {
                ICapsService capsService = m_registry.RequestModuleInterface<ICapsService>();
                IClientCapsService clientCaps = capsService.GetClientCapsService(AgentID);

                IRegionClientCapsService oldRegionService = clientCaps.GetCapsService(neighbor.RegionHandle);

                //If its disabled, it should be removed, so kill it!
                if (oldRegionService != null && oldRegionService.Disabled)
                {
                    clientCaps.RemoveCAPS(neighbor.RegionHandle);
                    oldRegionService = null;
                }

                bool newAgent = oldRegionService == null;
                IRegionClientCapsService otherRegionService = clientCaps.GetOrCreateCapsService(neighbor.RegionHandle,
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

                ICommunicationService commsService = m_registry.RequestModuleInterface<ICommunicationService>();
                if (commsService != null)
                    commsService.GetUrlsForUser(neighbor, circuitData.AgentID);
                        //Make sure that we make userURLs if we need to

                circuitData.CapsPath = CapsUtil.GetCapsPathFromCapsSeed(otherRegionService.CapsUrl); //For OpenSim
                circuitData.firstname = clientCaps.AccountInfo.FirstName;
                circuitData.lastname = clientCaps.AccountInfo.LastName;

                int requestedPort = 0;
                if (circuitData.child)
                    circuitData.reallyischild = true;
                bool regionAccepted = CreateAgent(neighbor, otherRegionService, ref circuitData, SimulationService, ref requestedPort, out reason);
                if (regionAccepted)
                {
                    IPAddress ipAddress = neighbor.ExternalEndPoint.Address;
                    string otherRegionsCapsURL;
                    //If the region accepted us, we should get a CAPS url back as the reason, if not, its not updated or not an Aurora region, so don't touch it.
                    if (reason != "" && reason != "authorized")
                    {
                        OSDMap responseMap = (OSDMap) OSDParser.DeserializeJson(reason);
                        OSDMap SimSeedCaps = (OSDMap) responseMap["CapsUrls"];
                        if (responseMap.ContainsKey("OurIPForClient"))
                        {
                            string ip = responseMap["OurIPForClient"].AsString();
                            if (!IPAddress.TryParse(ip, out ipAddress))
#pragma warning disable 618
                                ipAddress = Dns.GetHostByName(ip).AddressList[0];
#pragma warning restore 618
                        }
                        otherRegionService.AddCAPS(SimSeedCaps);
                        otherRegionsCapsURL = otherRegionService.CapsUrl;
                    }
                    else
                    {
                        //We are assuming an OpenSim region now!

                        #region OpenSim teleport compatibility!

                        useCallbacks = false; //OpenSim can't send us back a message, don't try it!
                        otherRegionsCapsURL = otherRegionService.Region.ServerURI +
                                              CapsUtil.GetCapsSeedPath(circuitData.CapsPath);
                        otherRegionService.CapsUrl = otherRegionsCapsURL;

                        #endregion
                    }
                    if (ipAddress == null)
                        ipAddress = neighbor.ExternalEndPoint.Address;
                    if (requestedPort == 0)
                        requestedPort = neighbor.ExternalEndPoint.Port;
                    otherRegionService = clientCaps.GetCapsService(neighbor.RegionHandle);
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
                                                          requestedPort, otherRegionsCapsURL, neighbor.RegionSizeX,
                                                          neighbor.RegionSizeY,
                                                          requestingRegion);

                    if (!useCallbacks)
                        Thread.Sleep(3000); //Give it a bit of time, only for OpenSim...

                    MainConsole.Instance.Info("[AgentProcessing]: Completed inform client about neighbor " + neighbor.RegionName);
                }
                else
                {
                    clientCaps.RemoveCAPS(neighbor.RegionHandle);
                    MainConsole.Instance.Error("[AgentProcessing]: Failed to inform client about neighbor " + neighbor.RegionName +
                                ", reason: " + reason);
                    return false;
                }
                return true;
            }
            reason = "SimulationService does not exist";
            MainConsole.Instance.Error("[AgentProcessing]: Failed to inform client about neighbor " + neighbor.RegionName +
                        ", reason: " + reason + "!");
            return false;
        }

        #endregion

        #region Teleporting

        private int CloseNeighborCall;

        public virtual bool TeleportAgent(ref GridRegion destination, uint TeleportFlags, int DrawDistance,
                                          AgentCircuitData circuit, AgentData agentData, UUID AgentID,
                                          ulong requestingRegion,
                                          out string reason)
        {
            IClientCapsService clientCaps =
                m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(AgentID);
            IRegionClientCapsService regionCaps = clientCaps.GetCapsService(requestingRegion);

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

                ISimulationService SimulationService = m_registry.RequestModuleInterface<ISimulationService>();
                if (SimulationService != null)
                {
                    //Set the user in transit so that we block duplicate tps and reset any cancelations
                    if (!SetUserInTransit(AgentID))
                    {
                        reason = "Already in a teleport";
                        return false;
                    }

                    bool useCallbacks = false;
                    //Note: we have to pull the new grid region info as the one from the region cannot be trusted
                    GridRegion oldRegion = destination;
                    IGridService GridService = m_registry.RequestModuleInterface<IGridService>();
                    if (GridService != null)
                    {
                        destination = GridService.GetRegionByUUID(clientCaps.AccountInfo.AllScopeIDs, destination.RegionID);
                        if (destination == null) //If its not in this grid
                            destination = oldRegion;
                        //Inform the client of the neighbor if needed
                        circuit.child = false; //Force child status to the correct type
                        circuit.roothandle = destination.RegionHandle;
                        if (!InformClientOfNeighbor(AgentID, requestingRegion, circuit, ref destination, TeleportFlags,
                                                    agentData, out reason, out useCallbacks))
                        {
                            ResetFromTransit(AgentID);
                            return false;
                        }
                    }
                    else
                    {
                        reason = "Could not find the grid service";
                        ResetFromTransit(AgentID);
                        return false;
                    }

                    IEventQueueService EQService = m_registry.RequestModuleInterface<IEventQueueService>();

                    IRegionClientCapsService otherRegion = clientCaps.GetCapsService(destination.RegionHandle);

                    EQService.TeleportFinishEvent(destination.RegionHandle, destination.Access,
                                                  otherRegion.LoopbackRegionIP,
                                                  otherRegion.CircuitData.RegionUDPPort,
                                                  otherRegion.CapsUrl,
                                                  4, AgentID, TeleportFlags,
                                                  destination.RegionSizeX, destination.RegionSizeY,
                                                  requestingRegion);

                    // TeleportFinish makes the client send CompleteMovementIntoRegion (at the destination), which
                    // trigers a whole shebang of things there, including MakeRoot. So let's wait for confirmation
                    // that the client contacted the destination before we send the attachments and close things here.

                    result = !useCallbacks || WaitForCallback(AgentID, out callWasCanceled);
                    if (!result)
                    {
                        //It says it failed, lets call the sim and check
                        AgentData data = null;
                        AgentCircuitData circuitData;
                        result = SimulationService.RetrieveAgent(destination, AgentID, false, out data, out circuitData);
                    }
                    if (!result)
                    {
                        if (!callWasCanceled)
                        {
                            MainConsole.Instance.Warn("[AgentProcessing]: Callback never came for teleporting agent " +
                                       AgentID + ". Resetting.");
                        }
                        //Close the agent at the place we just created if it isn't a neighbor
                        // 7/22 -- Kill the agent no matter what, it obviously is having issues getting there
                        //if (IsOutsideView (regionCaps.RegionX, destination.RegionLocX, regionCaps.Region.RegionSizeX, destination.RegionSizeX,
                        //    regionCaps.RegionY, destination.RegionLocY, regionCaps.Region.RegionSizeY, destination.RegionSizeY))
                        {
                            SimulationService.CloseAgent(destination, AgentID);
                            clientCaps.RemoveCAPS(destination.RegionHandle);
                        }
                        reason = !callWasCanceled ? "The teleport timed out" : "Cancelled";
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
                                       //Sleep for 15 seconds to give the agents a chance to cross and get everything right
                                       Thread.Sleep(15000);
                                       if (CloseNeighborCall != CloseNeighborCallNum)
                                           return; //Another was enqueued, kill this one

                                       //Now do a sanity check on the avatar
                                       IClientCapsService clientCaps =
                                           m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(
                                               AgentID);
                                       if (clientCaps == null)
                                           return;
                                       IRegionClientCapsService rootRegionCaps = clientCaps.GetRootCapsService();
                                       if (rootRegionCaps == null)
                                           return;
                                       IRegionClientCapsService ourRegionCaps =
                                           clientCaps.GetCapsService(destination.RegionHandle);
                                       if (ourRegionCaps == null)
                                           return;
                                       //If they handles arn't the same, the agent moved, and we can't be sure that we should close these agents
                                       if (rootRegionCaps.RegionHandle != ourRegionCaps.RegionHandle &&
                                           !clientCaps.InTeleport)
                                           return;

                                       IGridService service = m_registry.RequestModuleInterface<IGridService>();
                                       if (service != null)
                                       {
                                           List<GridRegion> NeighborsOfOldRegion = service.GetNeighbors(clientCaps.AccountInfo.AllScopeIDs, oldRegion);
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
                                               MainConsole.Instance.Info("[AgentProcessing]: Closing " + byebyeRegions.Count +
                                                          " child agents around " + oldRegion.RegionName);
                                               SendCloseChildAgent(AgentID, byebyeRegions);
                                           }
                                       }
                                   });
        }

        public virtual void SendCloseChildAgent(UUID agentID, IEnumerable<GridRegion> regionsToClose)
        {
            IClientCapsService clientCaps =
                m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(agentID);
            //Close all agents that we've been given regions for
            foreach (GridRegion region in regionsToClose)
            {
                MainConsole.Instance.Info("[AgentProcessing]: Closing child agent in " + region.RegionName);
                IRegionClientCapsService regionClientCaps = clientCaps.GetCapsService(region.RegionHandle);
                if (regionClientCaps != null)
                {
                    m_registry.RequestModuleInterface<ISimulationService>().CloseAgent(region, agentID);
                    clientCaps.RemoveCAPS(region.RegionHandle);
                }
            }
        }

        protected void ResetFromTransit(UUID AgentID)
        {
            IClientCapsService clientCaps =
                m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(AgentID);
            if (clientCaps != null)
            {
                clientCaps.InTeleport = false;
                clientCaps.RequestToCancelTeleport = false;
                clientCaps.CallbackHasCome = false;
            }
        }

        protected bool SetUserInTransit(UUID AgentID)
        {
            IClientCapsService clientCaps =
                m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(AgentID);

            if (clientCaps.InTeleport)
            {
                MainConsole.Instance.Warn("[AgentProcessing]: Got a request to teleport during another teleport for agent " + AgentID +
                           "!");
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
            IClientCapsService clientCaps =
                m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(AgentID);

            int count = 1000;
            while (!clientCaps.CallbackHasCome && count > 0)
            {
                //MainConsole.Instance.Debug("  >>> Waiting... " + count);
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
            IClientCapsService clientCaps =
                m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(AgentID);

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

        #region View Size

        /// <summary>
        ///   Check if the new position is outside of the range for the old position
        /// </summary>
        /// <param name = "x">old X pos (in meters)</param>
        /// <param name = "newRegionX">new X pos (in meters)</param>
        /// <param name = "y">old Y pos (in meters)</param>
        /// <param name = "newRegionY">new Y pos (in meters)</param>
        /// <returns></returns>
        public virtual bool IsOutsideView(int oldRegionX, int newRegionX, int oldRegionSizeX, int newRegionSizeX,
                                          int oldRegionY, int newRegionY, int oldRegionSizeY, int newRegionSizeY)
        {
            if (!CheckViewSize(oldRegionX, newRegionX, oldRegionSizeX, newRegionSizeX))
                return true;
            if (!CheckViewSize(oldRegionY, newRegionY, oldRegionSizeY, newRegionSizeY))
                return true;

            return false;
        }

        private bool CheckViewSize(int oldr, int newr, int oldSize, int newSize)
        {
            if (oldr - newr < 0)
            {
                if (
                    !(Math.Abs(oldr - newr + newSize) <=
                      m_registry.RequestModuleInterface<IGridService>().GetRegionViewSize()))
                    return false;
            }
            else
            {
                if (
                    !(Math.Abs(newr - oldr + oldSize) <=
                      m_registry.RequestModuleInterface<IGridService>().GetRegionViewSize()))
                    return false;
            }
            return true;
        }

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
                                       ulong requestingRegion, out string reason)
        {
            try
            {
                IClientCapsService clientCaps =
                    m_registry.RequestModuleInterface<ICapsService>().GetClientCapsService(AgentID);
                IRegionClientCapsService requestingRegionCaps = clientCaps.GetCapsService(requestingRegion);
                ISimulationService SimulationService = m_registry.RequestModuleInterface<ISimulationService>();
                if (SimulationService != null)
                {
                    //Note: we have to pull the new grid region info as the one from the region cannot be trusted
                    IGridService GridService = m_registry.RequestModuleInterface<IGridService>();
                    if (GridService != null)
                    {
                        //Set the user in transit so that we block duplicate tps and reset any cancelations
                        if (!SetUserInTransit(AgentID))
                        {
                            reason = "Already in a teleport";
                            return false;
                        }

                        bool result = false;

                        //We need to get it from the grid service again so that we can get the simulation service urls correctly
                        // as regions don't get that info
                        crossingRegion = GridService.GetRegionByUUID(clientCaps.AccountInfo.AllScopeIDs, crossingRegion.RegionID);
                        cAgent.IsCrossing = true;
                        if (!SimulationService.UpdateAgent(crossingRegion, cAgent))
                        {
                            MainConsole.Instance.Warn("[AgentProcessing]: Failed to cross agent " + AgentID +
                                       " because region did not accept it. Resetting.");
                            reason = "Failed to update an agent";
                        }
                        else
                        {
                            IEventQueueService EQService = m_registry.RequestModuleInterface<IEventQueueService>();

                            //Add this for the viewer, but not for the sim, seems to make the viewer happier
                            int XOffset = crossingRegion.RegionLocX - requestingRegionCaps.RegionX;
                            pos.X += XOffset;

                            int YOffset = crossingRegion.RegionLocY - requestingRegionCaps.RegionY;
                            pos.Y += YOffset;

                            IRegionClientCapsService otherRegion = clientCaps.GetCapsService(crossingRegion.RegionHandle);
                            //Tell the client about the transfer
                            EQService.CrossRegion(crossingRegion.RegionHandle, pos, velocity,
                                                  otherRegion.LoopbackRegionIP,
                                                  otherRegion.CircuitData.RegionUDPPort,
                                                  otherRegion.CapsUrl,
                                                  AgentID,
                                                  circuit.SessionID,
                                                  crossingRegion.RegionSizeX,
                                                  crossingRegion.RegionSizeY,
                                                  requestingRegion);

                            result = WaitForCallback(AgentID);
                            if (!result)
                            {
                                MainConsole.Instance.Warn("[AgentProcessing]: Callback never came in crossing agent " + circuit.AgentID +
                                           ". Resetting.");
                                reason = "Crossing timed out";
                            }
                            else
                            {
                                // Next, let's close the child agent connections that are too far away.
                                //Fix the root agent status
                                otherRegion.RootAgent = true;
                                requestingRegionCaps.RootAgent = false;

                                CloseNeighborAgents(requestingRegionCaps.Region, crossingRegion, AgentID);
                                reason = "";
                            }
                        }

                        //All done
                        ResetFromTransit(AgentID);
                        return result;
                    }
                    else
                        reason = "Could not find the GridService";
                }
                else
                    reason = "Could not find the SimulationService";
            }
            catch (Exception ex)
            {
                MainConsole.Instance.WarnFormat("[AgentProcessing]: Failed to cross an agent into a new region. {0}", ex);
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
                                                         agentpos.Position, lookAt);
                    }

                    //Also update the service itself
                    regionCaps.LastPosition = agentpos.Position;
                    if (agentpos.UserGoingOffline)
                        return; //It just needed a last pos update

                    //Tell all neighbor regions about the new position as well
                    List<GridRegion> ourNeighbors = GetRegions(regionCaps.ClientCaps);
                    foreach (GridRegion region in ourNeighbors.Where(region => region != null && !SimulationService.UpdateAgent(region, agentpos)))
                    {
                        MainConsole.Instance.Info("[AgentProcessing]: Failed to inform " + region.RegionName +
                                   " about updating agent. ");
                    }

                    EnableChildAgentsForPosition(regionCaps, agentpos.Position);
                }
            }
        }

        private List<GridRegion> GetRegions(IClientCapsService iClientCapsService)
        {
#if(!ISWIN)
            List<GridRegion> regions = new List<GridRegion>();
            foreach(IRegionClientCapsService rcc in iClientCapsService.GetCapsServices())
                regions.Add(rcc.Region);
            return regions;
#else
            return iClientCapsService.GetCapsServices().Select(rccs => rccs.Region).ToList();
#endif
        }

        #endregion

        #region Login initial agent

        [CanBeReflected(ThreatLevel = OpenSim.Services.Interfaces.ThreatLevel.None, UsePassword = true)]
        public virtual LoginAgentArgs LoginAgent(GridRegion region, AgentCircuitData aCircuit)
        {
            object retVal = base.DoRemoteByHTTP(_capsURL, region, aCircuit);
            if (retVal != null || m_doRemoteOnly)
            {
                if (retVal == null)
                {
                    ReadRemoteCapsPassword();
                    retVal = base.DoRemoteByHTTP(_capsURL, region, aCircuit);
                }
                return retVal == null ? null : (LoginAgentArgs)retVal;
            }

            bool success = false;
            string seedCap = "";
            string reason = "Could not find the simulation service";
            ICapsService capsService = m_registry.RequestModuleInterface<ICapsService>();
            ISimulationService SimulationService = m_registry.RequestModuleInterface<ISimulationService>();
            if (SimulationService != null)
            {
                // The client is in the region, we need to make sure it gets the right Caps
                // If CreateAgent is successful, it passes back a OSDMap of params that the client 
                //    wants to inform us about, and it includes the Caps SEED url for the region
                IRegionClientCapsService regionClientCaps = null;
                IClientCapsService clientCaps = null;
                if (capsService != null)
                {
                    //Remove any previous users
                    seedCap = capsService.CreateCAPS(aCircuit.AgentID,
                        CapsUtil.GetCapsSeedPath(aCircuit.CapsPath),
                        region.RegionHandle, true, aCircuit, 0);

                    clientCaps = capsService.GetClientCapsService(aCircuit.AgentID);
                    regionClientCaps = clientCaps.GetCapsService(region.RegionHandle);
                }


                ICommunicationService commsService = m_registry.RequestModuleInterface<ICommunicationService>();
                if (commsService != null)
                    commsService.GetUrlsForUser(region, aCircuit.AgentID);
                        //Make sure that we make userURLs if we need to

                int requestedUDPPort = 0;
                // As we are creating the agent, we must also initialize the CapsService for the agent
                success = CreateAgent(region, regionClientCaps, ref aCircuit, SimulationService, ref requestedUDPPort, out reason);
                if (requestedUDPPort == 0)
                    requestedUDPPort = region.ExternalEndPoint.Port;
                aCircuit.RegionUDPPort = requestedUDPPort;

                if (!success) // If it failed, do not set up any CapsService for the client
                {
                    if (reason != "")
                    {
                        try
                        {
                            OSDMap reasonMap = OSDParser.DeserializeJson(reason) as OSDMap;
                            if (reasonMap != null && reasonMap.ContainsKey("Reason"))
                                reason = reasonMap["Reason"].AsString();
                        }
                        catch
                        {
                            //If its already not JSON, it'll throw an exception, catch it
                        }
                    }
                    //Delete the Caps!
                    IAgentProcessing agentProcessor = m_registry.RequestModuleInterface<IAgentProcessing>();
                    if (agentProcessor != null && capsService != null)
                        agentProcessor.LogoutAgent(regionClientCaps, true);
                    else if (capsService != null)
                        capsService.RemoveCAPS(aCircuit.AgentID);
                    return new LoginAgentArgs { Success = success, CircuitData = aCircuit, Reason = reason, SeedCap = seedCap };
                }

                IPAddress ipAddress = regionClientCaps.Region.ExternalEndPoint.Address;
                if (capsService != null && reason != "")
                {
                    try
                    {
                        OSDMap responseMap = (OSDMap) OSDParser.DeserializeJson(reason);
                        OSDMap SimSeedCaps = (OSDMap) responseMap["CapsUrls"];
                        if (responseMap.ContainsKey("OurIPForClient"))
                        {
                            string ip = responseMap["OurIPForClient"].AsString();
                            if (!IPAddress.TryParse(ip, out ipAddress))
#pragma warning disable 618
                                ipAddress = Dns.GetHostByName(ip).AddressList[0];
#pragma warning restore 618
                        }
                        region.ExternalEndPoint.Address = ipAddress;
                            //Fix this so that it gets sent to the client that way
                        regionClientCaps.AddCAPS(SimSeedCaps);
                        regionClientCaps = clientCaps.GetCapsService(region.RegionHandle);
                        regionClientCaps.LoopbackRegionIP = ipAddress;
                        regionClientCaps.CircuitData.RegionUDPPort = requestedUDPPort;
                        regionClientCaps.RootAgent = true;
                    }
                    catch
                    {
                        //Delete the Caps!
                        IAgentProcessing agentProcessor = m_registry.RequestModuleInterface<IAgentProcessing>();
                        if (agentProcessor != null && capsService != null)
                            agentProcessor.LogoutAgent(regionClientCaps, true);
                        else if (capsService != null)
                            capsService.RemoveCAPS(aCircuit.AgentID);
                        success = false;
                    }
                }
            }
            return new LoginAgentArgs { Success = success, CircuitData = aCircuit, Reason = reason, SeedCap = seedCap };
        }

        private bool CreateAgent(GridRegion region, IRegionClientCapsService regionCaps, ref AgentCircuitData aCircuit,
            ISimulationService SimulationService, ref int requestedUDPPort, out string reason)
        {
            CachedUserInfo info = new CachedUserInfo();
            IAgentConnector con = Aurora.DataManager.DataManager.RequestPlugin<IAgentConnector>();
            if (con != null)
                info.AgentInfo = con.GetAgent(aCircuit.AgentID);
            info.UserAccount = regionCaps.ClientCaps.AccountInfo;

            IGroupsServiceConnector groupsConn = Aurora.DataManager.DataManager.RequestPlugin<IGroupsServiceConnector>();
            if (groupsConn != null)
            {
                info.ActiveGroup = groupsConn.GetGroupMembershipData(aCircuit.AgentID, UUID.Zero, aCircuit.AgentID);
                info.GroupMemberships = groupsConn.GetAgentGroupMemberships(aCircuit.AgentID, aCircuit.AgentID);
            }
            aCircuit.OtherInformation["CachedUserInfo"] = info.ToOSD();
            return SimulationService.CreateAgent(region, aCircuit, aCircuit.teleportFlags, null,
                                                    out requestedUDPPort, out reason);
        }

        #endregion

        #endregion
    }
}