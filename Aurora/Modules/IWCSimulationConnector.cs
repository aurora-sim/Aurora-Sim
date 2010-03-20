/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using OpenSim.Server.Handlers.Simulation;

namespace Aurora.Modules
{
    public class IWCSimulationConnectorModule : ISharedRegionModule, ISimulationService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<Scene> m_sceneList = new List<Scene>();

        private IEntityTransferModule m_AgentTransferModule;
        protected IEntityTransferModule AgentTransferModule
        {
            get
            {
                if (m_AgentTransferModule == null)
                    m_AgentTransferModule = m_sceneList[0].RequestModuleInterface<IEntityTransferModule>();
                return m_AgentTransferModule;
            }
        }

        private bool m_ModuleEnabled = false;

        #region IRegionModule

        public void Initialise(IConfigSource config)
        {
            IConfig moduleConfig = config.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("SimulationServices", "");
                if (name == Name)
                {
                    m_ModuleEnabled = true;

                    m_log.Info("[SIMULATION CONNECTOR]: Local simulation enabled");
                }
            }
        }

        public void PostInitialise()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (!m_ModuleEnabled)
                return;

            Init(scene);
            MainServer.Instance.AddHTTPHandler("/agent/", new AgentHandler(this).Handler);
            scene.RegisterModuleInterface<ISimulationService>(this);
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_ModuleEnabled)
                return;

            RemoveScene(scene);
            scene.UnregisterModuleInterface<ISimulationService>(this);
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void Close()
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "IWCSimulationConnectorModule"; }
        }

        /// <summary>
        /// Can be called from other modules.
        /// </summary>
        /// <param name="scene"></param>
        public void RemoveScene(Scene scene)
        {
            lock (m_sceneList)
            {
                if (m_sceneList.Contains(scene))
                {
                    m_sceneList.Remove(scene);
                }
            }
        }

        /// <summary>
        /// Can be called from other modules.
        /// </summary>
        /// <param name="scene"></param>
        public void Init(Scene scene)
        {
            if (!m_sceneList.Contains(scene))
            {
                lock (m_sceneList)
                {
                    m_sceneList.Add(scene);
                }

            }
        }

        #endregion /* IRegionModule */

        #region ISimulation

        public IScene GetScene(ulong regionhandle)
        {
            foreach (Scene s in m_sceneList)
            {
                if (s.RegionInfo.RegionHandle == regionhandle)
                    return s;
            }
            // ? weird. should not happen
            return m_sceneList[0];
        }

        /**
         * Agent-related communications
         */
        protected virtual string AgentPath()
        {
            return "/agent/";
        }

        protected virtual OSDMap PackCreateAgentArguments(AgentCircuitData aCircuit, GridRegion destination, uint flags)
        {
            OSDMap args = null;
            try
            {
                args = aCircuit.PackAgentCircuitData();
            }
            catch (Exception e)
            {
                m_log.Debug("[REMOTE SIMULATION CONNECTOR]: PackAgentCircuitData failed with exception: " + e.Message);
                return null;
            }
            // Add the input arguments
            args["destination_x"] = OSD.FromString(destination.RegionLocX.ToString());
            args["destination_y"] = OSD.FromString(destination.RegionLocY.ToString());
            args["destination_name"] = OSD.FromString(destination.RegionName);
            args["destination_uuid"] = OSD.FromString(destination.RegionID.ToString());
            args["teleport_flags"] = OSD.FromString(flags.ToString());

            return args;
        }

        public bool CreateAgent(GridRegion destination, AgentCircuitData aCircuit, uint teleportFlags, out string reason)
        {
            reason = String.Empty;

            if (destination == null)
            {
                reason = "Destination is null";
                m_log.Debug("[IWC SIMULATION CONNECTOR]: Given destination is null");
                return false;
            }
            foreach (Scene s in m_sceneList)
            {
                if (s.RegionInfo.RegionHandle == destination.RegionHandle)
                {
                    m_log.DebugFormat("[IWC LOCAL SIMULATION CONNECTOR]: Found region {0} to send SendCreateChildAgent", destination.RegionName);
                    return s.NewUserConnection(aCircuit, teleportFlags, out reason);
                }
            }
            // Eventually, we want to use a caps url instead of the agentID
            string uri = string.Empty;
            try
            {
                uri = "http://" + destination.ExternalEndPoint.Address + ":" + destination.HttpPort + AgentPath() + aCircuit.AgentID + "/";
            }
            catch (Exception e)
            {
                m_log.Debug("[IWC SIMULATION CONNECTOR]: Unable to resolve external endpoint on agent create. Reason: " + e.Message);
                reason = e.Message;
                return false;
            }

            //Console.WriteLine("   >>> DoCreateChildAgentCall <<< " + uri);

            HttpWebRequest AgentCreateRequest = (HttpWebRequest)WebRequest.Create(uri);
            AgentCreateRequest.Method = "POST";
            AgentCreateRequest.ContentType = "application/json";
            AgentCreateRequest.Timeout = 10000;
            //AgentCreateRequest.KeepAlive = false;
            //AgentCreateRequest.Headers.Add("Authorization", authKey);

            // Fill it in
            OSDMap args = PackCreateAgentArguments(aCircuit, destination, teleportFlags);
            if (args == null)
                return false;

            string strBuffer = "";
            byte[] buffer = new byte[1];
            try
            {
                strBuffer = OSDParser.SerializeJsonString(args);
                Encoding str = Util.UTF8;
                buffer = str.GetBytes(strBuffer);

            }
            catch (Exception e)
            {
                m_log.WarnFormat("[IWC SIMULATION CONNECTOR]: Exception thrown on serialization of ChildCreate: {0}", e.Message);
                // ignore. buffer will be empty, caller should check.
            }

            Stream os = null;
            try
            { // send the Post
                AgentCreateRequest.ContentLength = buffer.Length;   //Count bytes to send
                os = AgentCreateRequest.GetRequestStream();
                os.Write(buffer, 0, strBuffer.Length);         //Send it
                m_log.InfoFormat("[IWC SIMULATION CONNECTOR]: Posted CreateAgent request to remote sim {0}, region {1}, x={2} y={3}",
                    uri, destination.RegionName, destination.RegionLocX, destination.RegionLocY);
            }
            //catch (WebException ex)
            catch
            {
                //m_log.InfoFormat("[REMOTE SIMULATION CONNECTOR]: Bad send on ChildAgentUpdate {0}", ex.Message);
                reason = "cannot contact remote region";
                return false;
            }
            finally
            {
                if (os != null)
                    os.Close();
            }

            // Let's wait for the response
            //m_log.Info("[REMOTE SIMULATION CONNECTOR]: Waiting for a reply after DoCreateChildAgentCall");

            WebResponse webResponse = null;
            StreamReader sr = null;
            try
            {
                webResponse = AgentCreateRequest.GetResponse();
                if (webResponse == null)
                {
                    m_log.Info("[IWC SIMULATION CONNECTOR]: Null reply on DoCreateChildAgentCall post");
                }
                else
                {

                    sr = new StreamReader(webResponse.GetResponseStream());
                    string response = sr.ReadToEnd().Trim();
                    m_log.InfoFormat("[IWC SIMULATION CONNECTOR]: DoCreateChildAgentCall reply was {0} ", response);

                    if (!String.IsNullOrEmpty(response))
                    {
                        try
                        {
                            // we assume we got an OSDMap back
                            OSDMap r = Util.GetOSDMap(response);
                            bool success = r["success"].AsBoolean();
                            reason = r["reason"].AsString();
                            return success;
                        }
                        catch (NullReferenceException e)
                        {
                            m_log.InfoFormat("[IWC SIMULATION CONNECTOR]: exception on reply of DoCreateChildAgentCall {0}", e.Message);

                            // check for old style response
                            if (response.ToLower().StartsWith("true"))
                                return true;

                            return false;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                m_log.InfoFormat("[IWC SIMULATION CONNECTOR]: exception on reply of DoCreateChildAgentCall {0}", ex.Message);
                reason = "Destination did not reply";
                return false;
            }
            finally
            {
                if (sr != null)
                    sr.Close();
            }

            return true;
        }

        public bool UpdateAgent(GridRegion destination, AgentData cAgentData)
        {
            if (destination == null)
                return false;

            foreach (Scene s in m_sceneList)
            {
                if (s.RegionInfo.RegionHandle == destination.RegionHandle)
                {
                    m_log.DebugFormat(
                        "[LOCAL SIMULATION CONNECTOR]: Found region {0} {1} to send AgentUpdate",
                        s.RegionInfo.RegionName, destination.RegionHandle);

                    s.IncomingChildAgentDataUpdate(cAgentData);
                    return true;
                }
            }

            //            m_log.DebugFormat("[LOCAL COMMS]: Did not find region {0} for ChildAgentUpdate", regionHandle);
            return false;
        }

        public bool UpdateAgent(GridRegion destination, AgentPosition cAgentData)
        {
            if (destination == null)
                return false;

            foreach (Scene s in m_sceneList)
            {
                if (s.RegionInfo.RegionHandle == destination.RegionHandle)
                {
                    //m_log.Debug("[LOCAL COMMS]: Found region to send ChildAgentUpdate");
                    s.IncomingChildAgentDataUpdate(cAgentData);
                    return true;
                }
            }
            //m_log.Debug("[LOCAL COMMS]: region not found for ChildAgentUpdate");
            return false;
        }

        public bool RetrieveAgent(GridRegion destination, UUID id, out IAgentData agent)
        {
            agent = null;

            if (destination == null)
                return false;

            foreach (Scene s in m_sceneList)
            {
                if (s.RegionInfo.RegionHandle == destination.RegionHandle)
                {
                    //m_log.Debug("[LOCAL COMMS]: Found region to send ChildAgentUpdate");
                    return s.IncomingRetrieveRootAgent(id, out agent);
                }
            }
            //m_log.Debug("[LOCAL COMMS]: region not found for ChildAgentUpdate");
            return false;
        }

        public bool ReleaseAgent(UUID origin, UUID id, string uri)
        {
            foreach (Scene s in m_sceneList)
            {
                if (s.RegionInfo.RegionID == origin)
                {
                    m_log.Debug("[LOCAL COMMS]: Found region to SendReleaseAgent");
                    AgentTransferModule.AgentArrivedAtDestination(id);
                    return true;
                    //                    return s.IncomingReleaseAgent(id);
                }
            }
            //m_log.Debug("[LOCAL COMMS]: region not found in SendReleaseAgent " + origin);
            return false;
        }

        public bool CloseAgent(GridRegion destination, UUID id)
        {
            if (destination == null)
                return false;

            foreach (Scene s in m_sceneList)
            {
                if (s.RegionInfo.RegionID == destination.RegionID)
                {
                    //m_log.Debug("[LOCAL COMMS]: Found region to SendCloseAgent");
                    return s.IncomingCloseAgent(id);
                }
            }
            //m_log.Debug("[LOCAL COMMS]: region not found in SendCloseAgent");
            return false;
        }

        /**
         * Object-related communications
         */

        public bool CreateObject(GridRegion destination, ISceneObject sog, bool isLocalCall)
        {
            if (destination == null)
                return false;

            foreach (Scene s in m_sceneList)
            {
                if (s.RegionInfo.RegionHandle == destination.RegionHandle)
                {
                    //m_log.Debug("[LOCAL COMMS]: Found region to SendCreateObject");
                    if (isLocalCall)
                    {
                        // We need to make a local copy of the object
                        ISceneObject sogClone = sog.CloneForNewScene();
                        sogClone.SetState(sog.GetStateSnapshot(), s);
                        return s.IncomingCreateObject(sogClone);
                    }
                    else
                    {
                        // Use the object as it came through the wire
                        return s.IncomingCreateObject(sog);
                    }
                }
            }
            return false;
        }

        public bool CreateObject(GridRegion destination, UUID userID, UUID itemID)
        {
            if (destination == null)
                return false;

            foreach (Scene s in m_sceneList)
            {
                if (s.RegionInfo.RegionHandle == destination.RegionHandle)
                {
                    return s.IncomingCreateObject(userID, itemID);
                }
            }
            return false;
        }


        #endregion /* IInterregionComms */

        #region Misc

        public bool IsLocalRegion(ulong regionhandle)
        {
            foreach (Scene s in m_sceneList)
                if (s.RegionInfo.RegionHandle == regionhandle)
                    return true;
            return false;
        }

        public bool IsLocalRegion(UUID id)
        {
            foreach (Scene s in m_sceneList)
                if (s.RegionInfo.RegionID == id)
                    return true;
            return false;
        }

        #endregion
    }
}
