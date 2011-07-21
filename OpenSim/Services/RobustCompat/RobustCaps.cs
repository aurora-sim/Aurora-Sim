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
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Capabilities;
using Aurora.Simulation.Base;

namespace OpenSim.Services.RobustCompat
{
    public class RobustCaps : INonSharedRegionModule
    {
        #region Declares

        private Scene m_scene;
        private bool m_enabled = false;

        #endregion

        #region IRegionModuleBase Members

        public void Initialise(Nini.Config.IConfigSource source)
        {
            m_enabled = source.Configs["Handlers"].GetBoolean("RobustCompatibility", m_enabled);
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;
            m_scene = scene;
            scene.EventManager.OnClosingClient += OnClosingClient;
            scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
            scene.AuroraEventManager.RegisterEventHandler ("NewUserConnection", OnGenericEvent);
            scene.AuroraEventManager.RegisterEventHandler ("UserStatusChange", OnGenericEvent);

            scene.AuroraEventManager.RegisterEventHandler ("DetachingAllAttachments", DetachingAllAttachments);
            scene.AuroraEventManager.RegisterEventHandler ("SendingAttachments", SendAttachments);
        }

        private Dictionary<UUID, ISceneEntity[]> m_userAttachments = new Dictionary<UUID, ISceneEntity[]> ();
        public object DetachingAllAttachments (string funct, object param)
        {
            ISceneEntity[] attachments = (ISceneEntity[])param;
            if(attachments.Length > 0)
                m_userAttachments[attachments[0].OwnerID] = attachments; 
            return null;
        }

        public object SendAttachments (string funct, object param)
        {
            object[] parameters = (object[])param;
            IScenePresence sp = (IScenePresence)parameters[1];
            OpenSim.Services.Interfaces.GridRegion dest = (OpenSim.Services.Interfaces.GridRegion)parameters[0];
            IAttachmentsModule att = sp.Scene.RequestModuleInterface<IAttachmentsModule> ();
            if (m_userAttachments.ContainsKey(sp.UUID))
            {
                foreach (ISceneEntity attachment in m_userAttachments[sp.UUID])
                {
                    OpenSim.Services.Connectors.Simulation.SimulationServiceConnector ssc = new Connectors.Simulation.SimulationServiceConnector ();
                    attachment.IsDeleted = false;//Fix this, we 'did' get removed from the sim already
                    //Now send it to them
                    ssc.CreateObject (dest, (ISceneObject)attachment);
                    attachment.IsDeleted = true;
                }
            }
            return null;
        }

        void OnMakeRootAgent (IScenePresence presence)
        {
            ICapsService service = m_scene.RequestModuleInterface<ICapsService> ();
            if (service != null)
            {
                IClientCapsService clientCaps = service.GetClientCapsService (presence.UUID);
                if (clientCaps != null)
                {
                    IRegionClientCapsService regionCaps = clientCaps.GetCapsService (m_scene.RegionInfo.RegionHandle);
                    if (regionCaps != null)
                    {
                        regionCaps.RootAgent = true;
                        foreach (IRegionClientCapsService regionClientCaps in clientCaps.GetCapsServices ())
                        {
                            if (regionCaps.RegionHandle != regionClientCaps.RegionHandle)
                                regionClientCaps.RootAgent = false; //Reset any other agents that we might have
                        }
                    }
                }
            }
            if ((presence.CallbackURI != null) && !presence.CallbackURI.Equals (""))
            {
                WebUtils.ServiceOSDRequest (presence.CallbackURI, null, "DELETE", 10000, false, false, false);
                presence.CallbackURI = null;
            }
        }

        void OnClosingClient(IClientAPI client)
        {
            ICapsService service = m_scene.RequestModuleInterface<ICapsService>();
            if (service != null)
            {
                IClientCapsService clientCaps = service.GetClientCapsService(client.AgentId);
                if (clientCaps != null)
                {
                    IRegionClientCapsService regionCaps = clientCaps.GetCapsService(m_scene.RegionInfo.RegionHandle);
                    if (regionCaps != null)
                    {
                        regionCaps.Close();
                        clientCaps.RemoveCAPS(m_scene.RegionInfo.RegionHandle);
                    }
                }
            }

        }

        protected object OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "NewUserConnection")
            {
                ICapsService service = m_scene.RequestModuleInterface<ICapsService>();
                if (service != null)
                {
                    object[] obj = (object[])parameters;
                    OSDMap param = (OSDMap)obj[0];
                    AgentCircuitData circuit = (AgentCircuitData)obj[1];
                    if (circuit.reallyischild)//If Aurora is sending this, it'll show that it really is a child agent
                        return null;
                    circuit.child = false;//ONLY USE ROOT AGENTS, SINCE OPENSIM SENDS CHILD == TRUE ALL THE TIME
                    if (circuit.ServiceURLs != null && circuit.ServiceURLs.ContainsKey ("IncomingCAPSHandler"))
                    {
                        //Used by incoming (home) agents from HG
                        MainServer.Instance.AddStreamHandler (new RestStreamHandler ("POST", CapsUtil.GetCapsSeedPath (circuit.CapsPath),
                            delegate (string request, string path, string param2,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                            {
                                return CapsRequest (request, path, param2, httpRequest, httpResponse, circuit.ServiceURLs["IncomingCAPSHandler"].ToString ());
                            }));
                    }
                    else
                    {
                        IClientCapsService clientService = service.GetOrCreateClientCapsService (circuit.AgentID);
                        clientService.RemoveCAPS (m_scene.RegionInfo.RegionHandle);
                        service.CreateCAPS (circuit.AgentID, CapsUtil.GetCapsSeedPath (circuit.CapsPath),
                            m_scene.RegionInfo.RegionHandle, true, circuit, m_scene.RegionInfo.HttpPort); //We ONLY use root agents because of OpenSim's inability to send the correct data
                        MainConsole.Instance.Output ("setting up on " + clientService.HostUri + CapsUtil.GetCapsSeedPath (circuit.CapsPath));
                        IClientCapsService clientCaps = service.GetClientCapsService (circuit.AgentID);
                        if (clientCaps != null)
                        {
                            IRegionClientCapsService regionCaps = clientCaps.GetCapsService (m_scene.RegionInfo.RegionHandle);
                            if (regionCaps != null)
                            {
                                regionCaps.AddCAPS ((OSDMap)param["CapsUrls"]);
                            }
                        }
                    }
                }
            }
            else if (FunctionName == "UserStatusChange")
            {
                object[] info = (object[])parameters;
                if (!bool.Parse(info[1].ToString())) //Logging out
                {
                    ICapsService service = m_scene.RequestModuleInterface<ICapsService>();
                    if (service != null)
                    {
                        service.RemoveCAPS (UUID.Parse (info[0].ToString ()));
                    }
                }
            }
            return null;
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "RobustCaps"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        #region Virtual caps handler

        public virtual string CapsRequest (string request, string path, string param,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse, string url)
        {
            OSDMap response = WebUtils.PostToService (url, new OSDMap (), true, false, true);
            return OSDParser.SerializeLLSDXmlString (response);
        }

        #endregion
    }
}
