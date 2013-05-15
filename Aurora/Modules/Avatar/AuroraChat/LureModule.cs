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
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using System;

namespace Aurora.Modules.Chat
{
    /// <summary>
    ///     This just supports god TP's and thats about it
    /// </summary>
    public class LureModule : INonSharedRegionModule
    {
        #region Declares

        private IScene m_scene;

        private IMessageTransferModule m_TransferModule;
        private bool m_Enabled = true;
        private bool m_allowGodTeleports = true;

        #endregion

        #region INonSharedRegionModule

        public void Initialise(IConfigSource source)
        {
            IConfig ccmModuleConfig = source.Configs["Messaging"];
            if (ccmModuleConfig != null)
            {
                m_Enabled = ccmModuleConfig.GetString("LureModule", Name) == Name;
                m_allowGodTeleports = ccmModuleConfig.GetBoolean("AllowGodTeleports", m_allowGodTeleports);
            }
        }

        public void AddRegion(IScene scene)
        {
            if (!m_Enabled)
                return;

            m_scene = scene;

            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
            scene.EventManager.OnIncomingInstantMessage += OnGridInstantMessage;
        }

        public void RemoveRegion(IScene scene)
        {
            if (!m_Enabled)
                return;

            m_scene = null;

            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
            scene.EventManager.OnIncomingInstantMessage -= OnGridInstantMessage;
        }

        public void RegionLoaded(IScene scene)
        {
            if (!m_Enabled)
                return;
            m_TransferModule = m_scene.RequestModuleInterface<IMessageTransferModule>();

            if (m_TransferModule == null)
                MainConsole.Instance.Error("[INSTANT MESSAGE]: No message transfer module, " +
                                           "lures will not work!");
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
            get { return "AuroraLureModule"; }
        }

        #endregion

        #region Client

        private void OnNewClient(IClientAPI client)
        {
            client.OnStartLure += OnStartLure;
            client.OnTeleportLureRequest += OnTeleportLureRequest;
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnStartLure -= OnStartLure;
            client.OnTeleportLureRequest -= OnTeleportLureRequest;
        }

        public void OnStartLure(byte lureType, string message, UUID targetid, IClientAPI client)
        {
            IScenePresence presence = client.Scene.GetScenePresence(client.AgentId);
            Vector3 position = presence.AbsolutePosition + new Vector3(2, 0, 0)*presence.Rotation;
            UUID dest = Util.BuildFakeParcelID(
                client.Scene.RegionInfo.RegionHandle,
                (uint) position.X,
                (uint) position.Y,
                (uint) position.Z);

            GridInstantMessage m = new GridInstantMessage()
                {
                    FromAgentID = client.AgentId,
                    FromAgentName = client.Name,
                    ToAgentID = targetid,
                    Dialog = (byte)InstantMessageDialog.RequestTeleport,
                    Message = "",
                    SessionID = dest,
                    Offline = 0,
                    Position = presence.AbsolutePosition,
                    BinaryBucket = new Byte[0],
                    RegionID = client.Scene.RegionInfo.RegionID
                };

            if (m_allowGodTeleports && client.Scene.Permissions.CanGodTeleport(client.AgentId, targetid))
            //if we are an admin and are in god mode
            {
                //God tp them
                m.Dialog = (byte)InstantMessageDialog.GodLikeRequestTeleport;
            }

            if (m_TransferModule != null)
                m_TransferModule.SendInstantMessage(m);
        }

        public void OnTeleportLureRequest(UUID lureID, uint teleportFlags, IClientAPI client)
        {
            ulong handle;
            uint x;
            uint y;
            uint z;

            Util.ParseFakeParcelID(lureID, out handle, out x, out y, out z);

            Vector3 position = new Vector3 {X = x, Y = y, Z = z};
            IEntityTransferModule entityTransfer = client.Scene.RequestModuleInterface<IEntityTransferModule>();
            if (entityTransfer != null)
            {
                entityTransfer.RequestTeleportLocation(client, handle, position,
                                                       Vector3.Zero, teleportFlags);
            }
        }

        private void OnGridInstantMessage(GridInstantMessage im)
        {
            if (im.Dialog == (byte) InstantMessageDialog.RequestTeleport)
            {
                MainConsole.Instance.DebugFormat(
                    "[HG LURE MODULE]: RequestTeleport sessionID={0}, regionID={1}, message={2}", im.SessionID,
                    im.RegionID, im.Message);

                // Forward. We do this, because the IM module explicitly rejects
                // IMs of this type
                if (m_TransferModule != null)
                    m_TransferModule.SendInstantMessage(im);
            }
        }

        #endregion
    }
}