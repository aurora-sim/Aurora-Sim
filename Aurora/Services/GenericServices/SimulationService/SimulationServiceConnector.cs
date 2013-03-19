/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using GridRegion = Aurora.Framework.GridRegion;

namespace Aurora.Services
{
    public class SimulationServiceConnector : ISimulationService, IService
    {
        /// <summary>
        ///     These are regions that have timed out and we are not sending updates to until the (int) time passes
        /// </summary>
        protected Dictionary<string, int> m_blackListedRegions = new Dictionary<string, int>();

        protected LocalSimulationServiceConnector m_localBackend = new LocalSimulationServiceConnector();
        protected IRegistryCore m_registry;

        public IScene Scene
        {
            get { return null; }
        }

        public ISimulationService InnerService
        {
            get { return m_localBackend; }
        }

        #region IService Members

        public virtual void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlers = config.Configs["Handlers"];
            if (handlers.GetString("SimulationHandler", "") == "SimulationServiceConnector")
            {
                registry.RegisterModuleInterface<ISimulationService>(this);
            }
            m_registry = registry;
            m_localBackend.Initialize(config, registry);
        }

        public virtual void Start(IConfigSource config, IRegistryCore registry)
        {
            m_localBackend.Start(config, registry);
        }

        public void FinishedStartup()
        {
            m_localBackend.FinishedStartup();
        }

        #endregion

        #region Agents

        public virtual bool CreateAgent(GridRegion destination, AgentCircuitData aCircuit, uint teleportFlags,
                                        AgentData data, out int requestedUDPPort, out string reason)
        {
            // Try local first
            if (m_localBackend.CreateAgent(destination, aCircuit, teleportFlags, data, out requestedUDPPort,
                                           out reason))
                return true;
            reason = String.Empty;
            requestedUDPPort = destination.ExternalEndPoint.Port; //Just make sure..

            reason = String.Empty;

            string uri = MakeUri(destination, true) + aCircuit.AgentID + "/";

            try
            {
                OSDMap args = aCircuit.PackAgentCircuitData();

                args["destination_x"] = OSD.FromString(destination.RegionLocX.ToString());
                args["destination_y"] = OSD.FromString(destination.RegionLocY.ToString());
                args["destination_name"] = OSD.FromString(destination.RegionName);
                args["destination_uuid"] = OSD.FromString(destination.RegionID.ToString());
                args["teleport_flags"] = OSD.FromString(teleportFlags.ToString());
                if (data != null)
                    args["agent_data"] = data.Pack();

                string resultStr = WebUtils.PostToService(uri, args);
                //Pull out the result and set it as the reason
                if (resultStr == "")
                    return false;
                OSDMap result = OSDParser.DeserializeJson(resultStr) as OSDMap;
                reason = result["reason"] != null ? result["reason"].AsString() : "";
                if (result["success"].AsBoolean())
                {
                    //Not right... don't return true except for opensim combatibility :/
                    if (reason == "" || reason == "authorized")
                        return true;
                    //We were able to contact the region
                    try
                    {
                        //We send the CapsURLs through, so we need these
                        OSDMap responseMap = (OSDMap) OSDParser.DeserializeJson(reason);
                        if (responseMap.ContainsKey("Reason"))
                            reason = responseMap["Reason"].AsString();
                        if (responseMap.ContainsKey("requestedUDPPort"))
                            requestedUDPPort = responseMap["requestedUDPPort"];
                        return result["success"].AsBoolean();
                    }
                    catch
                    {
                        //Something went wrong
                        return false;
                    }
                }

                reason = result.ContainsKey("Message") ? result["Message"].AsString() : "Could not contact the region";
                return false;
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn("[REMOTE SIMULATION CONNECTOR]: CreateAgent failed with exception: " + e);
                reason = e.Message;
            }

            return false;
        }

        public bool UpdateAgent(GridRegion destination, AgentData data)
        {
            return UpdateAgent(destination, (IAgentData) data);
        }

        public bool UpdateAgent(GridRegion destination, AgentPosition data)
        {
            return UpdateAgent(destination, (IAgentData) data);
        }

        public bool FailedToMoveAgentIntoNewRegion(UUID AgentID, UUID RegionID)
        {
            if (m_localBackend.FailedToMoveAgentIntoNewRegion(AgentID, RegionID))
                return true;

            // Eventually, we want to use a caps url instead of the agentID
            string uri = MakeUri(m_registry.RequestModuleInterface<IGridService>().GetRegionByUUID(null, RegionID),
                                 true) + AgentID + "/" + RegionID.ToString() + "/";

            OSDMap data = new OSDMap();
            data["Method"] = "FailedToMoveAgentIntoNewRegion";
            try
            {
                WebUtils.PostToService(uri, data);
                return true;
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn(
                    "[REMOTE SIMULATION CONNECTOR]: FailedToMoveAgentIntoNewRegion failed with exception: " + e);
            }
            return false;
        }

        public bool MakeChildAgent(UUID AgentID, UUID leavingRegion, GridRegion destination, bool isCrossing)
        {
            if (m_localBackend.MakeChildAgent(AgentID, leavingRegion, destination, isCrossing))
                return true;

            // Eventually, we want to use a caps url instead of the agentID
            string uri = MakeUri(destination, true) + AgentID + "/" + destination.RegionID.ToString() + "/";

            OSDMap data = new OSDMap();
            data["Method"] = "MakeChildAgent";
            data["IsCrossing"] = isCrossing;
            data["LeavingRegion"] = leavingRegion;
            try
            {
                WebUtils.PostToService(uri, data);
                return true;
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn("[REMOTE SIMULATION CONNECTOR]: MakeChildAgent failed with exception: " + e);
            }
            return false;
        }

        public bool FailedToTeleportAgent(GridRegion destination, UUID failedRegionID, UUID AgentID, string reason,
                                          bool isCrossing)
        {
            if (m_localBackend.FailedToTeleportAgent(destination, failedRegionID, AgentID, reason, isCrossing))
                return true;

            // Eventually, we want to use a caps url instead of the agentID
            string uri = MakeUri(destination, true) + AgentID + "/" + destination.RegionID.ToString() + "/";

            OSDMap data = new OSDMap();
            data["Method"] = "FailedToTeleportAgent";
            data["Reason"] = reason;
            data["IsCrossing"] = isCrossing;
            data["FailedRegionID"] = failedRegionID;
            data["AgentID"] = AgentID;
            try
            {
                WebUtils.PostToService(uri, data);
                return true;
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn(
                    "[REMOTE SIMULATION CONNECTOR]: FailedToTeleportAgent failed with exception: " + e);
            }
            return false;
        }

        public bool RetrieveAgent(GridRegion destination, UUID id, bool agentIsLeaving, out AgentData agent,
                                  out AgentCircuitData circuitData)
        {
            // Try local first
            if (m_localBackend.RetrieveAgent(destination, id, agentIsLeaving, out agent, out circuitData))
                return true;

            // else do the remote thing
            if (m_localBackend.IsLocalRegion(destination.RegionHandle))
                return false;

            agent = null;
            circuitData = null;
            // Eventually, we want to use a caps url instead of the agentID
            string uri = MakeUri(destination, true) + id + "/" + destination.RegionID.ToString() + "/" +
                         agentIsLeaving.ToString() + "/";

            try
            {
                string resultStr = WebUtils.GetFromService(uri);
                if (resultStr != "")
                {
                    OSDMap result = OSDParser.DeserializeJson(resultStr) as OSDMap;
                    if (result["Result"] == "Not Found")
                        return false;
                    agent = new AgentData();

                    if (!result.ContainsKey("AgentData"))
                        return false; //Disable old simulators

                    agent.Unpack((OSDMap) result["AgentData"]);
                    circuitData = new AgentCircuitData();
                    circuitData.UnpackAgentCircuitData((OSDMap) result["CircuitData"]);
                    return true;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn("[REMOTE SIMULATION CONNECTOR]: UpdateAgent failed with exception: " + e);
            }

            return false;
        }

        public bool CloseAgent(GridRegion destination, UUID id)
        {
            // Try local first
            if (m_localBackend.CloseAgent(destination, id))
                return true;

            // else do the remote thing
            if (m_localBackend.IsLocalRegion(destination.RegionHandle))
                return false;

            string uri = MakeUri(destination, true) + id + "/" + destination.RegionID.ToString() + "/";

            try
            {
                WebUtils.ServiceOSDRequest(uri, null, "DELETE", 10000);
            }
            catch (Exception e)
            {
                MainConsole.Instance.WarnFormat("[REMOTE SIMULATION CONNECTOR] CloseAgent failed with exception; {0}", e);
            }

            return true;
        }

        protected virtual string AgentPath()
        {
            return "/agent/";
        }

        private bool UpdateAgent(GridRegion destination, IAgentData cAgentData)
        {
            if (cAgentData is AgentData)
            {
                if (m_localBackend.UpdateAgent(destination, (AgentData) cAgentData))
                    return true;
            }
            else if (cAgentData is AgentPosition)
            {
                if (m_localBackend.UpdateAgent(destination, (AgentPosition) cAgentData))
                    return true;
            }

            // else do the remote thing
            if (m_localBackend.IsLocalRegion(destination.RegionHandle))
                return false;

            // Try local first
            // Eventually, we want to use a caps url instead of the agentID
            string uri = MakeUri(destination, true) + cAgentData.AgentID + "/";

            if (m_blackListedRegions.ContainsKey(uri))
            {
                //Check against time
                if (m_blackListedRegions[uri] > 3 &&
                    Util.EnvironmentTickCountSubtract(m_blackListedRegions[uri]) > 0)
                {
                    MainConsole.Instance.Warn("[SimServiceConnector]: Blacklisted region " + destination.RegionName +
                                              " requested");
                    //Still blacklisted
                    return false;
                }
            }
            try
            {
                OSDMap args = cAgentData.Pack();

                args["destination_x"] = OSD.FromString(destination.RegionLocX.ToString());
                args["destination_y"] = OSD.FromString(destination.RegionLocY.ToString());
                args["destination_name"] = OSD.FromString(destination.RegionName);
                args["destination_uuid"] = OSD.FromString(destination.RegionID.ToString());

                string result = WebUtils.PutToService(uri, args);
                if (result == "")
                {
                    if (m_blackListedRegions.ContainsKey(uri))
                    {
                        if (m_blackListedRegions[uri] == 3)
                        {
                            //add it to the blacklist as the request completely failed 3 times
                            m_blackListedRegions[uri] = Util.EnvironmentTickCount() + 60*1000; //60 seconds
                        }
                        else if (m_blackListedRegions[uri] == 0)
                            m_blackListedRegions[uri]++;
                    }
                    else
                        m_blackListedRegions[uri] = 0;
                    return false;
                }
                //Clear out the blacklist if it went through
                m_blackListedRegions.Remove(uri);

                OSDMap innerResult = (OSDMap) OSDParser.DeserializeJson(result);
                return innerResult["Updated"].AsBoolean();
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn("[REMOTE SIMULATION CONNECTOR]: UpdateAgent failed with exception: " + e);
            }

            return false;
        }

        #endregion Agents

        #region Objects

        public bool CreateObject(GridRegion destination, ISceneEntity sog)
        {
            // Try local first
            if (m_localBackend != null && m_localBackend.CreateObject(destination, sog))
            {
                //MainConsole.Instance.Debug("[REST COMMS]: LocalBackEnd SendCreateObject succeeded");
                return true;
            }

            if (m_localBackend.IsLocalRegion(destination.RegionHandle))
                return false;

            bool successful = false;
            string uri = MakeUri(destination, false) + sog.UUID + "/";
            //MainConsole.Instance.Debug("   >>> DoCreateObjectCall <<< " + uri);

            OSDMap args = new OSDMap(7);
            args["sog"] = OSD.FromString(sog.ToXml2());
            // Add the input general arguments
            args["destination_x"] = OSD.FromString(destination.RegionLocX.ToString());
            args["destination_y"] = OSD.FromString(destination.RegionLocY.ToString());
            args["destination_name"] = OSD.FromString(destination.RegionName);
            args["destination_uuid"] = OSD.FromString(destination.RegionID.ToString());

            string result = WebUtils.PostToService(uri, args);
            bool.TryParse(result, out successful);
            return successful;
        }

        public bool CreateObject(GridRegion destination, UUID userID, UUID itemID)
        {
            // Try local first
            if (m_localBackend.CreateObject(destination, userID, itemID))
            {
                //MainConsole.Instance.Debug("[REST COMMS]: LocalBackEnd SendCreateObject succeeded");
                return true;
            }

            // else do the remote thing
            if (m_localBackend.IsLocalRegion(destination.RegionHandle))
                return false;

            bool successful = false;
            string uri = MakeUri(destination, false) + itemID + "/";
            //MainConsole.Instance.Debug("   >>> DoCreateObjectCall <<< " + uri);

            OSDMap args = new OSDMap(6);
            args["userID"] = OSD.FromUUID(userID);
            args["itemID"] = OSD.FromUUID(itemID);
            // Add the input general arguments
            args["destination_x"] = OSD.FromString(destination.RegionLocX.ToString());
            args["destination_y"] = OSD.FromString(destination.RegionLocY.ToString());
            args["destination_name"] = OSD.FromString(destination.RegionName);
            args["destination_uuid"] = OSD.FromString(destination.RegionID.ToString());

            bool.TryParse(WebUtils.PostToService(uri, args), out successful);
            return successful;
        }

        protected virtual string ObjectPath()
        {
            return "/object/";
        }

        #endregion Objects

        #region Misc

        private string MakeUri(GridRegion destination, bool isAgent)
        {
            if (isAgent && destination.GenericMap.ContainsKey("SimulationAgent"))
                return destination.ServerURI + destination.GenericMap["SimulationAgent"].AsString();
            if (!isAgent && destination.GenericMap.ContainsKey("SimulationObject"))
                return destination.ServerURI + destination.GenericMap["SimulationObject"].AsString();
            else
            {
                if (destination.ServerURI == null)
                    destination.ServerURI = "http://" + destination.ExternalHostName + ":" + destination.HttpPort;
                string url = destination.ServerURI.EndsWith("/")
                                 ? destination.ServerURI.Remove(destination.ServerURI.Length - 1, 1)
                                 : destination.ServerURI;
                return url + (isAgent ? AgentPath() : ObjectPath());
            }
        }

        #endregion
    }
}