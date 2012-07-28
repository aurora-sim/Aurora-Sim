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
using System.IO;
using System.Linq;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using Aurora.Framework.Capabilities;
using Aurora.Simulation.Base;

namespace OpenSim.Services.Robust
{
    public class RobustCaps : INonSharedRegionModule
    {
        #region Declares

        private IScene m_scene;
        private bool m_enabled = false;

        #endregion

        #region IRegionModuleBase Members

        public void Initialise(Nini.Config.IConfigSource source)
        {
            m_enabled = source.Configs["Handlers"].GetBoolean("RobustCompatibility", m_enabled);
        }

        public void AddRegion (IScene scene)
        {
            if (!m_enabled)
                return;
            m_scene = scene;
            scene.EventManager.OnClosingClient += OnClosingClient;
            scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
            scene.EventManager.OnSetAgentLeaving += OnSetAgentLeaving;
            scene.AuroraEventManager.RegisterEventHandler ("NewUserConnection", OnGenericEvent);
            scene.AuroraEventManager.RegisterEventHandler ("UserStatusChange", OnGenericEvent);
            scene.AuroraEventManager.RegisterEventHandler ("SendingAttachments", SendAttachments);
        }

        void OnSetAgentLeaving(IScenePresence presence, Interfaces.GridRegion destination)
        {
            IAttachmentsModule attModule = presence.Scene.RequestModuleInterface<IAttachmentsModule>();
            if(attModule != null)
                m_userAttachments[presence.UUID] = attModule.GetAttachmentsForAvatar(presence.UUID);
        }

        private readonly Dictionary<UUID, ISceneEntity[]> m_userAttachments = new Dictionary<UUID, ISceneEntity[]>();
        public object SendAttachments (string funct, object param)
        {
            object[] parameters = (object[])param;
            IScenePresence sp = (IScenePresence)parameters[1];
            Interfaces.GridRegion dest = (Interfaces.GridRegion)parameters[0];
            // this is never used.. 
            IAttachmentsModule att = sp.Scene.RequestModuleInterface<IAttachmentsModule> ();
            if (m_userAttachments.ContainsKey(sp.UUID))
            {
                Util.FireAndForget (delegate
                                        {
                    foreach (ISceneEntity attachment in m_userAttachments[sp.UUID])
                    {
                        Connectors.Simulation.SimulationServiceConnector ssc = new Connectors.Simulation.SimulationServiceConnector ();
                        attachment.IsDeleted = false;//Fix this, we 'did' get removed from the sim already
                        //Now send it to them
                        ssc.CreateObject (dest, (ISceneObject)attachment);
                        attachment.IsDeleted = true;
                    }
                });
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
                WebUtils.ServiceOSDRequest (presence.CallbackURI, null, "DELETE", 10000);
                presence.CallbackURI = null;
            }
            Util.FireAndForget(o => DoPresenceUpdate(presence));
        }

        private void DoPresenceUpdate(IScenePresence presence)
        {
            ReportAgent(presence);
            IFriendsModule friendsModule = m_scene.RequestModuleInterface<IFriendsModule>();
            IAgentInfoService aservice = m_scene.RequestModuleInterface<IAgentInfoService>();
            if (friendsModule != null)
            {
                Interfaces.FriendInfo[] friends = friendsModule.GetFriends(presence.UUID);
                List<string> s = new List<string>();
                for (int i = 0; i < friends.Length; i++)
                {
                    s.Add(friends[i].Friend);
                }
                List<UserInfo> infos = aservice.GetUserInfos(s);
                foreach (UserInfo u in infos)
                {
                    if (u != null && u.IsOnline)
                        friendsModule.SendFriendsStatusMessage(presence.UUID, UUID.Parse(u.UserID), true);
                }
                foreach (UserInfo u in infos)
                {
                    if (u != null && u.IsOnline)
                    {
                        if (!IsLocal(u, presence))
                            DoNonLocalPresenceUpdateCall(u, presence);
                        else
                            friendsModule.SendFriendsStatusMessage(UUID.Parse(u.UserID), presence.UUID, true);
                    }
                }
            }
        }

        private void DoNonLocalPresenceUpdateCall(UserInfo u, IScenePresence presence)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            //sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "status";

            sendData["FromID"] = presence.UUID.ToString();
            sendData["ToID"] = u.UserID;
            sendData["Online"] = u.IsOnline.ToString();
            
            Call(m_scene.GridService.GetRegionByUUID(null, u.CurrentRegionID), sendData);
        }

        private void Call(OpenSim.Services.Interfaces.GridRegion region, Dictionary<string, object> sendData)
        {
            Util.FireAndForget(delegate(object o)
            {
                string reqString = WebUtils.BuildQueryString(sendData);
                //MainConsole.Instance.DebugFormat("[FRIENDS CONNECTOR]: queryString = {0}", reqString);
                if (region == null)
                    return;

                try
                {
                    string url = "http://" + region.ExternalHostName + ":" + region.HttpPort;
                    string a = SynchronousRestFormsRequester.MakeRequest("POST",
                            url + "/friends",
                            reqString);
                }
                catch (Exception)
                {
                }
            });
        }

        private bool IsLocal(UserInfo u, IScenePresence presence)
        {
            return presence.Scene.RequestModuleInterface<ISceneManager>().GetAllScenes().Any(scene => scene.GetScenePresence(UUID.Parse(u.UserID)) != null);
        }

        private void ReportAgent(IScenePresence presence)
        {
            IAgentInfoService aservice = m_scene.RequestModuleInterface<IAgentInfoService>();
            if (aservice != null)
                aservice.SetLoggedIn(presence.UUID.ToString(), true, false, presence.Scene.RegionInfo.RegionID);
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "login";

            sendData["UserID"] = presence.UUID.ToString();
            sendData["SessionID"] = presence.ControllingClient.SessionId.ToString();
            sendData["SecureSessionID"] = presence.ControllingClient.SecureSessionId.ToString();

            string reqString = WebUtils.BuildQueryString(sendData);
            List<string> urls = m_scene.RequestModuleInterface<IConfigurationService>().FindValueOf("PresenceServerURI");
            foreach (string url in urls)
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                           url,
                           reqString);
            }

            sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "report";

            sendData["SessionID"] = presence.ControllingClient.SessionId.ToString();
            sendData["RegionID"] = presence.Scene.RegionInfo.RegionID.ToString();

            reqString = WebUtils.BuildQueryString(sendData);
            // MainConsole.Instance.DebugFormat("[PRESENCE CONNECTOR]: queryString = {0}", reqString);
            foreach (string url in urls)
            {
                SynchronousRestFormsRequester.MakeRequest("POST",
                           url,
                           reqString);
            }
            if (aservice != null)
                aservice.SetLastPosition(presence.UUID.ToString(), presence.Scene.RegionInfo.RegionID, presence.AbsolutePosition, Vector3.Zero);
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
                    if (client.IsLoggingOut)
                    {
                        clientCaps.Close ();
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
                    AvatarAppearance appearance = m_scene.AvatarService.GetAppearance(circuit.AgentID);
                    if (appearance != null)
                        circuit.Appearance = appearance;
                    else
                        m_scene.AvatarService.SetAppearance(circuit.AgentID, circuit.Appearance);
                    //circuit.Appearance.Texture = new Primitive.TextureEntry(UUID.Zero);
                    circuit.child = false;//ONLY USE ROOT AGENTS, SINCE OPENSIM SENDS CHILD == TRUE ALL THE TIME
                    if (circuit.ServiceURLs != null && circuit.ServiceURLs.ContainsKey ("IncomingCAPSHandler"))
                    {
                        AddCapsHandler(circuit);
                    }
                    else
                    {
                        IClientCapsService clientService = service.GetOrCreateClientCapsService (circuit.AgentID);
                        clientService.RemoveCAPS (m_scene.RegionInfo.RegionHandle);
                        string caps = service.CreateCAPS (circuit.AgentID, CapsUtil.GetCapsSeedPath (circuit.CapsPath),
                            m_scene.RegionInfo.RegionHandle, true, circuit, MainServer.Instance.Port); //We ONLY use root agents because of OpenSim's inability to send the correct data
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

        private void AddCapsHandler(AgentCircuitData circuit)
        {
            //Used by incoming (home) agents from HG
            MainServer.Instance.AddStreamHandler(new GenericStreamHandler("POST", CapsUtil.GetCapsSeedPath(circuit.CapsPath),
                delegate(string path, Stream request, OSHttpRequest httpRequest,
                                                            OSHttpResponse httpResponse)
                    {
                        return CapsRequest(circuit.ServiceURLs["IncomingCAPSHandler"].ToString());
                    }));
        }

        public void RegionLoaded (IScene scene)
        {
        }

        public void RemoveRegion (IScene scene)
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

        public virtual byte[] CapsRequest(string url)
        {
            return System.Text.Encoding.UTF8.GetBytes (WebUtils.PostToService (url, new OSDMap ()));
        }

        #endregion
    }
}
