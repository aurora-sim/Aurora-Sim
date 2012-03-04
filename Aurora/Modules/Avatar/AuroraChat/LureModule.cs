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
using System.Reflection;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules.Chat
{
    /// <summary>
    /// This just supports god TP's and thats about it
    /// </summary>
	public class LureModule : ISharedRegionModule
    {
        #region Declares

        private readonly List<IScene> m_scenes = new List<IScene> ();

		private IMessageTransferModule m_TransferModule;
        private bool m_Enabled = true;
        private bool m_allowGodTeleports = true;
        private readonly ExpiringCache<UUID, GridInstantMessage> m_PendingLures = new ExpiringCache<UUID, GridInstantMessage> ();

        #endregion

        #region ISharedRegionModule

        public void Initialise(IConfigSource source)
		{
            IConfig ccmModuleConfig = source.Configs["Messaging"];
            if (ccmModuleConfig != null)
            {
                m_Enabled = ccmModuleConfig.GetString ("LureModule", Name) == Name;
                m_allowGodTeleports = ccmModuleConfig.GetBoolean ("AllowGodTeleports", m_allowGodTeleports);
            }
		}

        public void AddRegion (IScene scene)
        {
            if (!m_Enabled)
                return;

            m_scenes.Add(scene);

            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
            scene.EventManager.OnIncomingInstantMessage += OnGridInstantMessage;
        }

        public void RemoveRegion (IScene scene)
        {
            if (!m_Enabled)
                return;

            m_scenes.Remove(scene);

            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
            scene.EventManager.OnIncomingInstantMessage -= OnGridInstantMessage;
        }

        public void RegionLoaded (IScene scene)
        {
            if (!m_Enabled)
                return;
            m_TransferModule = m_scenes[0].RequestModuleInterface<IMessageTransferModule>();

            if (m_TransferModule == null)
                MainConsole.Instance.Error("[INSTANT MESSAGE]: No message transfer module, " +
                            "lures will not work!");
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

		public void PostInitialise()
		{
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
            IScenePresence presence = client.Scene.GetScenePresence (client.AgentId);
            UUID dest = Util.BuildFakeParcelID(
                client.Scene.RegionInfo.RegionHandle,
				(uint)presence.AbsolutePosition.X,
				(uint)presence.AbsolutePosition.Y,
                (uint)presence.AbsolutePosition.Z);

            string mainGridURL = GetMainGridURL ();
            message += "@" + mainGridURL;//Add it to the message

			GridInstantMessage m;

            if (m_allowGodTeleports && client.Scene.Permissions.CanGodTeleport(client.AgentId, targetid))//if we are an admin and are in god mode
            {
                //God tp them
                m = new GridInstantMessage(client.Scene, client.AgentId,
                                           client.FirstName + " " + client.LastName, targetid,
                                           (byte)InstantMessageDialog.GodLikeRequestTeleport, false,
                                           "", dest, false, presence.AbsolutePosition,
                                           new Byte[0]);
            }
            else
            {
                //Not a god, so no god tp
                m = new GridInstantMessage(client.Scene, client.AgentId,
                                           client.FirstName + " " + client.LastName, targetid,
                                           (byte)InstantMessageDialog.RequestTeleport, false,
                                           message, dest, false, presence.AbsolutePosition,
                                           new Byte[0]);
            }
            m_PendingLures.Add (m.imSessionID, m, 7200); // 2 hours

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
            IEntityTransferModule entityTransfer = client.Scene.RequestModuleInterface<IEntityTransferModule> ();
            if (entityTransfer != null)
            {
                GridInstantMessage im;
                if (m_PendingLures.TryGetValue (lureID, out im))
                {
                    string[] parts = im.message.Split (new[] { '@' });
                    if (parts.Length > 1)
                    {
                        string url = parts[parts.Length - 1]; // the last part
                        if (url.Trim (new[] { '/' }) != GetMainGridURL ().Trim (new[] { '/' }))
                        {
                            GridRegion gatekeeper = new GridRegion
                                                        {
                                                            ServerURI = url,
                                                            RegionID = im.RegionID,
                                                            Flags =
                                                                (int)
                                                                (Framework.RegionFlags.Foreign |
                                                                 Framework.RegionFlags.Hyperlink)
                                                        };
                            entityTransfer.RequestTeleportLocation (client, gatekeeper, position,
                                Vector3.Zero, teleportFlags);
                            return;
                        }
                    }
                }
                entityTransfer.RequestTeleportLocation(client, handle, position,
                                      Vector3.Zero, teleportFlags);
            }
        }

        private string GetMainGridURL ()
        {
            IConfigurationService configService = m_scenes[0].RequestModuleInterface<IConfigurationService> ();
            List<string> mainGridURLs = configService.FindValueOf ("MainGridURL");
            string mainGridURL = MainServer.Instance.ServerURI + "/";//Assume the default
            if (mainGridURLs.Count > 0)//Then check whether we were given one
                mainGridURL = mainGridURLs[0];
            return mainGridURL;
        }

        void OnGridInstantMessage (GridInstantMessage im)
        {
            if (im.dialog == (byte)InstantMessageDialog.RequestTeleport)
            {
                UUID sessionID = new UUID (im.imSessionID);
                MainConsole.Instance.DebugFormat ("[HG LURE MODULE]: RequestTeleport sessionID={0}, regionID={1}, message={2}", im.imSessionID, im.RegionID, im.message);
                m_PendingLures.Add (sessionID, im, 7200); // 2 hours

                // Forward. We do this, because the IM module explicitly rejects
                // IMs of this type
                if (m_TransferModule != null)
                    m_TransferModule.SendInstantMessage (im);
            }
        }

        #endregion
    }
}
