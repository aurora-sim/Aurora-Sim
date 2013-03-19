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
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Servers.HttpServer.Interfaces;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.IO;

namespace Aurora.Modules.Gods
{
    public class GodsModule : INonSharedRegionModule, IGodsModule
    {
        /// <summary>
        ///     Special UUID for actions that apply to all agents
        /// </summary>
        private static readonly UUID ALL_AGENTS = new UUID("44e87126-e794-4ded-05b3-7c42da3d5cdb");

        protected IDialogModule m_dialogModule;
        protected IScene m_scene;

        #region IGodsModule Members

        public void RequestGodlikePowers(
            UUID agentID, UUID sessionID, UUID token, bool godLike, IClientAPI controllingClient)
        {
            IScenePresence sp = m_scene.GetScenePresence(agentID);

            if (sp != null)
            {
                if (godLike == false)
                {
                    //Unconditionally remove god levels
                    sp.GodLevel = 0;
                    sp.ControllingClient.SendAdminResponse(token, (uint) sp.GodLevel);
                    return;
                }

                // First check that this is the sim owner
                if (m_scene.Permissions.IsAdministrator(sp.UUID))
                {
                    sp.GodLevel = sp.UserLevel;
                    if (sp.GodLevel == 0)
                        sp.GodLevel = 250;

                    MainConsole.Instance.Info("[GODS]: God level set for " + sp.Name + ", level " +
                                              sp.GodLevel.ToString());
                    sp.ControllingClient.SendAdminResponse(token, (uint) sp.GodLevel);
                }
                else
                {
                    if (m_dialogModule != null)
                        m_dialogModule.SendAlertToUser(agentID,
                                                       "Request for god powers denied. This request has been logged.");
                    MainConsole.Instance.Info("[GODS]: God powers requested by " + sp.Name +
                                              ", user is not allowed to have god powers");
                }
            }
        }

        public void KickUser(UUID godID, UUID sessionID, UUID agentID, uint kickflags, string reason)
        {
            UUID kickUserID = ALL_AGENTS;

            IScenePresence sp = m_scene.GetScenePresence(agentID);

            if (sp != null || agentID == kickUserID)
            {
                if (m_scene.Permissions.IsGod(godID))
                {
                    if (kickflags == 0)
                    {
                        if (agentID == kickUserID)
                        {
                            m_scene.ForEachClient(
                                delegate(IClientAPI controller)
                                    {
                                        if (controller.AgentId != godID)
                                            controller.Kick(reason);
                                    }
                                );

                            //This does modify this list, but we make a copy of it
                            foreach (IScenePresence p in m_scene.GetScenePresences())
                            {
                                if (p.UUID != godID)
                                {
                                    IEntityTransferModule transferModule =
                                        m_scene.RequestModuleInterface<IEntityTransferModule>();
                                    if (transferModule != null)
                                        transferModule.IncomingCloseAgent(m_scene, p.UUID);
                                }
                            }
                        }
                        else
                        {
                            IEntityTransferModule transferModule =
                                m_scene.RequestModuleInterface<IEntityTransferModule>();
                            if (transferModule != null)
                                transferModule.IncomingCloseAgent(m_scene, sp.UUID);
                        }
                    }
                    else if (kickflags == 1)
                    {
                        sp.Frozen = true;
                        m_dialogModule.SendAlertToUser(agentID, reason);
                        m_dialogModule.SendAlertToUser(godID, "User Frozen");
                    }
                    else if (kickflags == 2)
                    {
                        sp.Frozen = false;
                        m_dialogModule.SendAlertToUser(agentID, reason);
                        m_dialogModule.SendAlertToUser(godID, "User Unfrozen");
                    }
                }
                else
                {
                    m_dialogModule.SendAlertToUser(godID, "Kick request denied");
                }
            }
        }

        #endregion

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(IScene scene)
        {
            m_scene = scene;
            m_scene.RegisterModuleInterface<IGodsModule>(this);
            m_scene.EventManager.OnNewClient += SubscribeToClientEvents;
            m_scene.EventManager.OnClosingClient += UnsubscribeFromClientEvents;
            m_scene.EventManager.OnRegisterCaps += RegisterCaps;
        }

        public void RemoveRegion(IScene scene)
        {
            m_scene.UnregisterModuleInterface<IGodsModule>(this);
            m_scene.EventManager.OnNewClient -= SubscribeToClientEvents;
            m_scene.EventManager.OnClosingClient -= UnsubscribeFromClientEvents;
            m_scene.EventManager.OnRegisterCaps -= RegisterCaps;
        }

        public void RegionLoaded(IScene scene)
        {
            m_dialogModule = m_scene.RequestModuleInterface<IDialogModule>();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "Gods Module"; }
        }

        #endregion

        public void SubscribeToClientEvents(IClientAPI client)
        {
            client.OnGodKickUser += KickUser;
            client.OnRequestGodlikePowers += RequestGodlikePowers;
        }

        public void UnsubscribeFromClientEvents(IClientAPI client)
        {
            client.OnGodKickUser -= KickUser;
            client.OnRequestGodlikePowers -= RequestGodlikePowers;
        }

        public OSDMap RegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["UntrustedSimulatorMessage"] = CapsUtil.CreateCAPS("UntrustedSimulatorMessage", "");

            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["UntrustedSimulatorMessage"],
                                                             delegate(string path, Stream request,
                                                                      OSHttpRequest httpRequest,
                                                                      OSHttpResponse httpResponse)
                                                                 { return UntrustedSimulatorMessage(agentID, request); }));
            return retVal;
        }

        private byte[] UntrustedSimulatorMessage(UUID AgentID, Stream request)
        {
            OSDMap rm = (OSDMap) OSDParser.DeserializeLLSDXml(request);
            if (rm["message"] == "GodKickUser")
            {
                OSDArray innerArray = ((OSDArray) ((OSDMap) rm["body"])["UserInfo"]);
                OSDMap innerMap = (OSDMap) innerArray[0];
                UUID toKick = innerMap["AgentID"].AsUUID();
                UUID sessionID = innerMap["GodSessionID"].AsUUID();
                string reason = innerMap["Reason"].AsString();
                uint kickFlags = innerMap["KickFlags"].AsUInteger();
                KickUser(AgentID, sessionID, toKick, kickFlags, reason);
            }
            return new byte[0];
        }

        /// <summary>
        ///     Kicks User specified from the simulator. This logs them off of the grid
        ///     If the client gets the UUID: 44e87126e7944ded05b37c42da3d5cdb it assumes
        ///     that you're kicking it even if the avatar's UUID isn't the UUID that the
        ///     agent is assigned
        /// </summary>
        /// <param name="godID">The person doing the kicking</param>
        /// <param name="sessionID">The session of the person doing the kicking</param>
        /// <param name="agentID">the person that is being kicked</param>
        /// <param name="kickflags">Tells what to do to the user</param>
        /// <param name="reason">The message to send to the user after it's been turned into a field</param>
        public void KickUser(UUID godID, UUID sessionID, UUID agentID, uint kickflags, byte[] reason)
        {
            KickUser(godID, sessionID, agentID, kickflags, Utils.BytesToString(reason));
        }
    }
}