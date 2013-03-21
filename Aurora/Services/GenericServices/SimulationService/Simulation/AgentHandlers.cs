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
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using GridRegion = Aurora.Framework.Services.GridRegion;

namespace Aurora.Services
{
    public class AgentHandler
    {
        private readonly ISimulationService m_SimulationService;

        protected bool m_Proxy;
        protected IRegistryCore m_registry;
        protected bool m_secure = true;

        public AgentHandler()
        {
        }

        public AgentHandler(ISimulationService sim, IRegistryCore registry, bool secure)
        {
            m_registry = registry;
            m_SimulationService = sim.InnerService;
            m_secure = secure;
        }

        public byte[] Handler(string path, Stream request, OSHttpRequest httpRequest,
                                        OSHttpResponse httpResponse)
        {
            //MainConsole.Instance.Debug("[CONNECTION DEBUGGING]: AgentHandler Called");

            //MainConsole.Instance.Debug("---------------------------");
            //MainConsole.Instance.Debug(" >> uri=" + request["uri"]);
            //MainConsole.Instance.Debug(" >> content-type=" + request["content-type"]);
            //MainConsole.Instance.Debug(" >> http-method=" + request["http-method"]);
            //MainConsole.Instance.Debug("---------------------------\n");

            httpResponse.ContentType = "text/html";
            httpResponse.StatusCode = (int)HttpStatusCode.OK;


            UUID agentID;
            UUID regionID;
            string action;
            string other;
            string uri = httpRequest.RawUrl;
            if (m_secure)
                uri = uri.Remove(0, 37); //Remove the secure UUID from the uri
            if (!WebUtils.GetParams(uri, out agentID, out regionID, out action, out other))
            {
                MainConsole.Instance.InfoFormat("[AGENT HANDLER]: Invalid parameters for agent message {0}",
                                                httpRequest.RawUrl);
                httpResponse.StatusCode = 404;
                return Encoding.UTF8.GetBytes("false");
            }

            // Next, let's parse the verb
            string method = httpRequest.HttpMethod;
            if (method.Equals("PUT"))
            {
                return DoAgentPut(request, httpRequest, httpResponse);
            }
            else if (method.Equals("POST"))
            {
                OSDMap map = null;
                try
                {
                    if (httpRequest.ContentType == "application/x-gzip")
                    {
                        System.IO.Stream inputStream =
                            new System.IO.Compression.GZipStream(request,
                                System.IO.Compression.CompressionMode.Decompress);
                        map = (OSDMap)OSDParser.DeserializeJson(inputStream);
                        inputStream.Close();
                    }
                    else
                        map = (OSDMap) OSDParser.DeserializeJson(request);
                }
                catch
                {
                }
                if (map != null)
                {
                    if (map["Method"] == "MakeChildAgent")
                        DoMakeChildAgent(agentID, map["LeavingRegion"].AsUUID(), regionID, map["IsCrossing"].AsBoolean());
                    else if (map["Method"] == "FailedToMoveAgentIntoNewRegion")
                        FailedToMoveAgentIntoNewRegion(agentID, regionID);
                    else if (map["Method"] == "FailedToTeleportAgent")
                        FailedToTeleportAgent(map["FailedRegionID"].AsUUID(), agentID, map["Reason"].AsString(),
                                              map["IsCrossing"].AsBoolean());
                    else
                        return DoAgentPost(map, httpRequest, httpResponse, agentID);
                    return MainServer.BlankResponse;
                }
                httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                return Encoding.UTF8.GetBytes("Bad request");
            }
            else if (method.Equals("GET"))
            {
                return DoAgentGet(request, httpRequest, httpResponse, agentID, regionID, bool.Parse(action));
            }
            else if (method.Equals("DELETE"))
            {
                return DoAgentDelete(request, httpRequest, httpResponse, agentID, action, regionID);
            }
            else
            {
                MainConsole.Instance.WarnFormat("[AGENT HANDLER]: method {0} not supported in agent message", method);

                httpResponse.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return Encoding.UTF8.GetBytes("Method not allowed");
            }
        }

        private void DoMakeChildAgent(UUID agentID, UUID leavingRegion, UUID regionID, bool isCrossing)
        {
            m_SimulationService.MakeChildAgent(agentID, leavingRegion, new GridRegion {RegionID = regionID}, isCrossing);
        }

        public bool FailedToMoveAgentIntoNewRegion(UUID AgentID, UUID RegionID)
        {
            return m_SimulationService.FailedToMoveAgentIntoNewRegion(AgentID, RegionID);
        }

        public bool FailedToTeleportAgent(UUID failedRegionID, UUID AgentID, string reason, bool isCrossing)
        {
            return m_SimulationService.FailedToTeleportAgent(null, failedRegionID,
                                                             AgentID, reason, isCrossing);
        }

        protected byte[] DoAgentPost(OSDMap args, OSHttpRequest httpRequest, OSHttpResponse httpResponse, UUID id)
        {
            // retrieve the input arguments
            int x = 0, y = 0;
            UUID uuid = UUID.Zero;
            string regionname = string.Empty;
            uint teleportFlags = 0;
            if (args.ContainsKey("destination_x") && args["destination_x"] != null)
                Int32.TryParse(args["destination_x"].AsString(), out x);
            else
                MainConsole.Instance.WarnFormat("  -- request didn't have destination_x");
            if (args.ContainsKey("destination_y") && args["destination_y"] != null)
                Int32.TryParse(args["destination_y"].AsString(), out y);
            else
                MainConsole.Instance.WarnFormat("  -- request didn't have destination_y");
            if (args.ContainsKey("destination_uuid") && args["destination_uuid"] != null)
                UUID.TryParse(args["destination_uuid"].AsString(), out uuid);
            if (args.ContainsKey("destination_name") && args["destination_name"] != null)
                regionname = args["destination_name"].ToString();
            if (args.ContainsKey("teleport_flags") && args["teleport_flags"] != null)
                teleportFlags = args["teleport_flags"].AsUInteger();

            AgentData agent = null;
            if (args.ContainsKey("agent_data") && args["agent_data"] != null)
            {
                try
                {
                    OSDMap agentDataMap = (OSDMap) args["agent_data"];
                    agent = new AgentData();
                    agent.Unpack(agentDataMap);
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.InfoFormat("[AGENT HANDLER]: exception on unpacking ChildCreate message {0}",
                                                    ex);
                }
            }

            GridRegion destination = new GridRegion
                                         {RegionID = uuid, RegionLocX = x, RegionLocY = y, RegionName = regionname};

            AgentCircuitData aCircuit = new AgentCircuitData();
            try
            {
                aCircuit.UnpackAgentCircuitData(args);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.InfoFormat("[AGENT HANDLER]: exception on unpacking ChildCreate message {0}", ex);
                httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                return Encoding.UTF8.GetBytes("Bad request");
            }

            OSDMap resp = new OSDMap(3);
            string reason = String.Empty;

            int requestedUDPPort = 0;
            // This is the meaning of POST agent
            bool result = CreateAgent(destination, aCircuit, teleportFlags, agent, out requestedUDPPort, out reason);

            resp["reason"] = reason;
            resp["requestedUDPPort"] = requestedUDPPort;
            resp["success"] = OSD.FromBoolean(result);

            httpResponse.StatusCode = (int)HttpStatusCode.OK;
            return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(resp));
        }

        // subclasses can override this
        protected virtual bool CreateAgent(GridRegion destination, AgentCircuitData aCircuit, uint teleportFlags,
                                           AgentData agent, out int requestedUDPPort, out string reason)
        {
            return m_SimulationService.CreateAgent(destination, aCircuit, teleportFlags, agent, out requestedUDPPort,
                                                   out reason);
        }

        protected byte[] DoAgentPut(Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            OSDMap args = WebUtils.GetOSDMap(request.ReadUntilEnd());
            if (args == null)
            {
                httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                return Encoding.UTF8.GetBytes("Bad request");
            }

            // retrieve the input arguments
            int x = 0, y = 0;
            UUID uuid = UUID.Zero;
            string regionname = string.Empty;
            if (args.ContainsKey("destination_x") && args["destination_x"] != null)
                Int32.TryParse(args["destination_x"].AsString(), out x);
            if (args.ContainsKey("destination_y") && args["destination_y"] != null)
                Int32.TryParse(args["destination_y"].AsString(), out y);
            if (args.ContainsKey("destination_uuid") && args["destination_uuid"] != null)
                UUID.TryParse(args["destination_uuid"].AsString(), out uuid);
            if (args.ContainsKey("destination_name") && args["destination_name"] != null)
                regionname = args["destination_name"].ToString();

            GridRegion destination = new GridRegion
                                         {RegionID = uuid, RegionLocX = x, RegionLocY = y, RegionName = regionname};

            string messageType;
            if (args["message_type"] != null)
                messageType = args["message_type"].AsString();
            else
            {
                MainConsole.Instance.Warn("[AGENT HANDLER]: Agent Put Message Type not found. ");
                messageType = "AgentData";
            }

            bool result = true;
            if ("AgentData".Equals(messageType))
            {
                AgentData agent = new AgentData();
                try
                {
                    agent.Unpack(args);
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.InfoFormat(
                        "[AGENT HANDLER]: exception on unpacking ChildAgentUpdate message {0}", ex);
                    httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Encoding.UTF8.GetBytes("Bad request");
                }

                //agent.Dump();
                // This is one of the meanings of PUT agent
                result = UpdateAgent(destination, agent);
            }
            else if ("AgentPosition".Equals(messageType))
            {
                AgentPosition agent = new AgentPosition();
                try
                {
                    agent.Unpack(args);
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.InfoFormat(
                        "[AGENT HANDLER]: exception on unpacking ChildAgentUpdate message {0}", ex);
                    httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Encoding.UTF8.GetBytes("Bad request");
                }
                //agent.Dump();
                // This is one of the meanings of PUT agent
                result = m_SimulationService.UpdateAgent(destination, agent);
            }
            OSDMap resp = new OSDMap();
            resp["Updated"] = result;
            httpResponse.StatusCode = (int)HttpStatusCode.OK;
            return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(resp));
        }

        // subclasses can override this
        protected virtual bool UpdateAgent(GridRegion destination, AgentData agent)
        {
            return m_SimulationService.UpdateAgent(destination, agent);
        }

        protected virtual byte[] DoAgentGet(Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse, UUID id, UUID regionID,
                                          bool agentIsLeaving)
        {
            if (m_SimulationService == null)
            {
                httpResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                // ignore. buffer will be empty, caller should check.
                return MainServer.BlankResponse;
            }

            GridRegion destination = new GridRegion {RegionID = regionID};

            AgentData agent = null;
            AgentCircuitData circuitData;
            bool result = m_SimulationService.RetrieveAgent(destination, id, agentIsLeaving, out agent, out circuitData);
            OSDMap map = new OSDMap();
            string strBuffer = "";
            if (result)
            {
                if (agent != null) // just to make sure
                {
                    map["AgentData"] = agent.Pack();
                    map["CircuitData"] = circuitData.PackAgentCircuitData();
                    try
                    {
                        strBuffer = OSDParser.SerializeJsonString(map);
                    }
                    catch (Exception e)
                    {
                        MainConsole.Instance.WarnFormat(
                            "[AGENT HANDLER]: Exception thrown on serialization of DoAgentGet: {0}", e);
                        httpResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                        // ignore. buffer will be empty, caller should check.
                        return MainServer.BlankResponse;
                    }
                }
                else
                {
                    map = new OSDMap();
                    map["Result"] = "Internal error";
                }
            }
            else
            {
                map = new OSDMap();
                map["Result"] = "Not Found";
            }

            httpResponse.StatusCode = (int)HttpStatusCode.OK;
            httpResponse.ContentType = "application/json";
            return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(map));
        }

        protected byte[] DoAgentDelete(Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse, UUID id, string action, UUID regionID)
        {
            MainConsole.Instance.Debug(" >>> DoDelete action:" + action + "; RegionID:" + regionID);

            GridRegion destination = new GridRegion {RegionID = regionID};

            if (action.Equals("release"))
            {
                object[] o = new object[2];
                o[0] = id;
                o[1] = destination;
                //This is an OpenSim event... fire an event so that the OpenSim compat handlers can grab it
                m_registry.RequestModuleInterface<ISimulationBase>().EventManager.FireGenericEventHandler(
                    "ReleaseAgent", o);
            }
            else
                m_SimulationService.CloseAgent(destination, id);
            MainConsole.Instance.Debug("[AGENT HANDLER]: Agent Released/Deleted.");

            httpResponse.StatusCode = (int)HttpStatusCode.OK;
            httpResponse.ContentType = "application/json";
            OSDMap map = new OSDMap();
            map["Agent"] = id;
            return Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(map));
        }
    }
}