using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Console;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using log4net;

namespace OpenSim.Services.MessagingService.MessagingModules.GridWideMessage
{
    public class AgentProcessing : IService
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected IRegistryCore m_registry;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            //Also look for incoming messages to display
            registry.RequestModuleInterface<IAsyncMessageRecievedService>().OnMessageReceived += OnMessageReceived;
        }

        #endregion

        #region Message Received

        protected OSDMap OnMessageReceived(OSDMap message)
        {
            if (message.ContainsKey("Method") && message["Method"] == "EnableChildAgents")
            {
                //Some notes on this message:
                // 1) This is a region > CapsService message ONLY, this should never be sent to the client!
                // 2) This just enables child agents in the regions given, as the region cannot do it,
                //       as regions do not have the ability to know what Cap Urls other regions have.
                // 3) We could do more checking here, but we don't really 'have' to at this point.
                //       If the sim was able to get it past the password checks and everything,
                //       it should be able to add the neighbors here. We could do the neighbor finding here
                //       as well, but it's not necessary at this time.
                OSDMap body = ((OSDMap)message["Message"]);

                //Parse the OSDMap
                int DrawDistance = body["DrawDistance"].AsInteger();
                ulong requestingRegion = body["RequestingRegion"].AsULong();
                UUID AgentID = body["AgentID"].AsUUID();

                AgentCircuitData circuitData = new AgentCircuitData();
                circuitData.UnpackAgentCircuitData((OSDMap)body["Circuit"]);

                //Now do the creation
                //Don't send it to the client at all, so return here
                EnableChildAgents(AgentID, requestingRegion, DrawDistance, circuitData);
            }
            return null;
        }

        #region EnableChildAgents

        public bool EnableChildAgents(UUID AgentID, ulong requestingRegion, int DrawDistance, AgentCircuitData circuit)
        {
            int count = 0;
            bool informed = true;
            INeighborService neighborService = m_registry.RequestModuleInterface<INeighborService>();
            if (neighborService != null)
            {
                uint x, y;
                Utils.LongToUInts(requestingRegion, out x, out y);
                GridRegion ourRegion = m_registry.RequestModuleInterface<IGridService>().GetRegionByPosition(UUID.Zero, (int)x, (int)y);
                if (ourRegion == null)
                {
                    m_log.Info("[EQMService]: Failed to inform neighbors about new agent, could not find our region. ");
                    return false;
                }
                List<GridRegion> neighbors = neighborService.GetNeighbors(ourRegion, DrawDistance);

                foreach (GridRegion neighbor in neighbors)
                {
                    //m_log.WarnFormat("--> Going to send child agent to {0}, new agent {1}", neighbour.RegionName, newAgent);

                    if (neighbor.RegionHandle != requestingRegion)
                    {
                        if (!InformClientOfNeighbor(AgentID, requestingRegion, circuit.Copy(), neighbor,
                            (uint)TeleportFlags.Default, null))
                            informed = false;
                    }
                    count++;
                }
            }
            return informed;
        }

        /// <summary>
        /// Async component for informing client of which neighbors exist
        /// </summary>
        /// <remarks>
        /// This needs to run asynchronously, as a network timeout may block the thread for a long while
        /// </remarks>
        /// <param name="remoteClient"></param>
        /// <param name="a"></param>
        /// <param name="regionHandle"></param>
        /// <param name="endPoint"></param>
        private bool InformClientOfNeighbor(UUID AgentID, ulong requestingRegion, AgentCircuitData circuitData, GridRegion neighbor,
            uint TeleportFlags, AgentData agentData)
        {
            m_log.Info("[EventQueueService]: Starting to inform client about neighbor " + neighbor.RegionName);

            //Notes on this method
            // 1) the SimulationService.CreateAgent MUST have a fixed CapsUrl for the region, so we have to create (if needed)
            //       a new Caps handler for it.
            // 2) Then we can call the methods (EnableSimulator and EstatablishAgentComm) to tell the client the new Urls
            // 3) This allows us to make the Caps on the grid server without telling any other regions about what the
            //       Urls are.

            string reason = String.Empty;
            ISimulationService SimulationService = m_registry.RequestModuleInterface<ISimulationService>();
            if (SimulationService != null)
            {
                //Make sure that we have a URL for the Caps on the grid server and one for the sim
                string newSeedCap = CapsUtil.GetCapsSeedPath(CapsUtil.GetRandomCapsObjectPath());
                //Leave this blank so that we can check below so that we use the same Url if the client has already been to that region
                string SimSeedCap = "";

                ICapsService capsService = m_registry.RequestModuleInterface<ICapsService>();

                IClientCapsService clientCaps = capsService.GetClientCapsService(AgentID);

                IRegionClientCapsService oldRegionService = clientCaps.GetCapsService(neighbor.RegionHandle);
                bool newAgent = oldRegionService == null;
                IRegionClientCapsService otherRegionService = clientCaps.GetOrCreateCapsService(neighbor.RegionHandle, newSeedCap, SimSeedCap);

                //ONLY UPDATE THE SIM SEED HERE
                //DO NOT PASS THE newSeedCap FROM ABOVE AS IT WILL BREAK THIS CODE
                // AS THE CLIENT EXPECTS THE SAME CAPS SEED IF IT HAS BEEN TO THE REGION BEFORE
                // AND FORCE UPDATING IT HERE WILL BREAK IT.
                string CapsBase = CapsUtil.GetRandomCapsObjectPath();
                if (newAgent)
                {
                    //Update 21-1-11 (Revolution) This is still very much needed for standalone mode
                    //The idea behind this is that this is the FIRST setup in standalone mode, so setting it to
                    //   this sets up the first initial URL, as if it was set as "", it would set it up wrong 
                    //   initially and all will go wrong from there
                    //Build the full URL
                    SimSeedCap
                        = neighbor.ServerURI
                      + CapsUtil.GetCapsSeedPath(CapsBase);
                    //Add the new Seed for this region
                }
                else if (!oldRegionService.Disabled)
                {
                    //Note: if the agent is already there, send an agent update then
                    if (agentData != null)
                        return SimulationService.UpdateAgent(neighbor, agentData);
                    return true;
                }
                //Fix the AgentCircuitData with the new CapsUrl
                circuitData.CapsPath = CapsBase;
                //Add the password too
                circuitData.OtherInformation["CapsPassword"] = otherRegionService.Password;

                //Offset the child avs position
                uint x, y;
                Utils.LongToUInts(requestingRegion, out x, out y);

                bool regionAccepted = SimulationService.CreateAgent(neighbor, circuitData,
                        TeleportFlags, agentData, out reason);
                if (regionAccepted)
                {
                    //If the region accepted us, we should get a CAPS url back as the reason, if not, its not updated or not an Aurora region, so don't touch it.
                    if (reason != "")
                    {
                        OSDMap responseMap = (OSDMap)OSDParser.DeserializeJson(reason);
                        SimSeedCap = responseMap["CapsUrl"].AsString();
                    }
                    //ONLY UPDATE THE SIM SEED HERE
                    //DO NOT PASS THE newSeedCap FROM ABOVE AS IT WILL BREAK THIS CODE
                    // AS THE CLIENT EXPECTS THE SAME CAPS SEED IF IT HAS BEEN TO THE REGION BEFORE
                    // AND FORCE UPDATING IT HERE WILL BREAK IT.
                    otherRegionService.AddSEEDCap("", SimSeedCap, otherRegionService.Password);
                    if (newAgent)
                    {
                        //We 'could' call Enqueue directly... but its better to just let it go and do it this way
                        IEventQueueService EQService = m_registry.RequestModuleInterface<IEventQueueService>();

                        EQService.EnableSimulator(neighbor.RegionHandle,
                            neighbor.ExternalEndPoint.Address.GetAddressBytes(),
                            neighbor.ExternalEndPoint.Port, AgentID,
                            neighbor.RegionSizeX, neighbor.RegionSizeY, requestingRegion);

                        // ES makes the client send a UseCircuitCode message to the destination, 
                        // which triggers a bunch of things there.
                        // So let's wait
                        Thread.Sleep(300);
                        EQService.EstablishAgentCommunication(AgentID, neighbor.RegionHandle,
                            neighbor.ExternalEndPoint.Address.GetAddressBytes(),
                            neighbor.ExternalEndPoint.Port, otherRegionService.UrlToInform, neighbor.RegionSizeX,
                            neighbor.RegionSizeY,
                            requestingRegion);

                        m_log.Info("[EventQueueService]: Completed inform client about neighbor " + neighbor.RegionName);
                    }
                }
                else
                {
                    m_log.Error("[EventQueueService]: Failed to inform client about neighbor " + neighbor.RegionName + ", reason: " + reason);
                    return false;
                }
                return true;
            }
            m_log.Error("[EventQueueService]: Failed to inform client about neighbor " + neighbor.RegionName + ", reason: SimulationService does not exist!");
            return false;
        }

        #endregion

        #endregion
    }
}
