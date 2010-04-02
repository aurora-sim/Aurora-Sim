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
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using TPFlags = OpenSim.Framework.Constants.TeleportFlags;

namespace Aurora.Modules
{
	public class LureModule : IRegionModule
	{
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private List<Scene> m_scenes = new List<Scene>();

		private IMessageTransferModule m_TransferModule = null;

		public void Initialise(Scene scene, IConfigSource config)
		{
			m_scenes.Add(scene);
			scene.EventManager.OnNewClient += OnNewClient;
			scene.EventManager.OnIncomingInstantMessage += OnGridInstantMessage;
		}

		void OnNewClient(IClientAPI client)
		{
			client.OnInstantMessage += OnInstantMessage;
			client.OnStartLure += OnStartLure;
			client.OnTeleportLureRequest += OnTeleportLureRequest;
		}

		public void PostInitialise()
		{
			m_TransferModule = m_scenes[0].RequestModuleInterface<IMessageTransferModule>();

			if (m_TransferModule == null)
				m_log.Error("[INSTANT MESSAGE]: No message transfer module, "+
				            "lures will not work!");
		}

		public void Close()
		{
		}

		public string Name
		{
            get { return "AuroraLureModule"; }
		}

		public bool IsSharedModule
		{
			get { return true; }
		}

		public void OnInstantMessage(IClientAPI client, GridInstantMessage im)
		{
		}
		private Scene GetClientScene(IClientAPI client)
		{
			foreach(Scene scene in m_scenes)
			{
				if (client.Scene.RegionInfo.RegionHandle == scene.RegionInfo.RegionHandle)
					return scene;
			}
			return null;
		}
		public void OnStartLure(byte lureType, string message, UUID targetid, IClientAPI client)
		{
			if (!(client.Scene is Scene))
				return;
			Scene scene = (Scene)(client.Scene);
			ScenePresence presence = scene.GetScenePresence(client.AgentId);
			ScenePresence SP = null;
			ScenePresence SPTemp;
			foreach(Scene scene1 in m_scenes)
			{
				scene1.TryGetScenePresence(targetid,out SPTemp);
				if(SPTemp != null)
				{
					if(!SPTemp.IsChildAgent)
					{
						SP = SPTemp;
					}
				}
			}
			UUID dest = Util.BuildFakeParcelID(
				scene.RegionInfo.RegionHandle,
				(uint)presence.AbsolutePosition.X,
				(uint)presence.AbsolutePosition.Y,
				(uint)presence.AbsolutePosition.Z);
			GridInstantMessage m;
			if(presence.GodLevel >= 1)
			{
				if(SP.GodLevel >= 1)
				{
					m = new GridInstantMessage(scene, client.AgentId,
					                           client.FirstName+" "+client.LastName, targetid,
					                           (byte)InstantMessageDialog.RequestTeleport, false,
					                           message, dest, false, presence.AbsolutePosition,
					                           new Byte[0]);
				}
				else
				{
					m = new GridInstantMessage(scene, client.AgentId,
					                           client.FirstName+" "+client.LastName, targetid,
					                           (byte)InstantMessageDialog.GodLikeRequestTeleport, false,
					                           "", dest, false, presence.AbsolutePosition,
					                           new Byte[0]);
				}
			}
			else
			{
				m = new GridInstantMessage(scene, client.AgentId,
				                           client.FirstName+" "+client.LastName, targetid,
				                           (byte)InstantMessageDialog.RequestTeleport, false,
				                           message, dest, false, presence.AbsolutePosition,
				                           new Byte[0]);
			}
			if (m_TransferModule != null)
			{
				m_TransferModule.SendInstantMessage(m,
				                                    delegate(bool success) { });
			}
		}

		public void OnTeleportLureRequest(UUID lureID, uint teleportFlags, IClientAPI client)
		{
			if (!(client.Scene is Scene))
				return;
			Scene scene = (Scene)(client.Scene);

			ulong handle = 0;
			uint x = 128;
			uint y = 128;
			uint z = 70;

			Util.ParseFakeParcelID(lureID, out handle, out x, out y, out z);

			Vector3 position = new Vector3();
			position.X = (float)x;
			position.Y = (float)y;
			position.Z = (float)z;
			try
			{
				ScenePresence spTPTemp = null;
				ScenePresence spTP = null;
				foreach(Scene scene3 in m_scenes)
				{
					scene3.TryGetScenePresence(lureID, out spTPTemp);
					if(spTPTemp != null)
					{
						if(!spTPTemp.IsChildAgent)
						{
							spTP = spTPTemp;
						}
					}
				}
				scene.RequestTeleportLocation(client, handle, position,
				                              Vector3.Zero, teleportFlags);
			}
			catch(Exception ex)
			{
				ex = new Exception();
			}
			
		}

		private void OnGridInstantMessage(GridInstantMessage msg)
		{
			// Forward remote teleport requests
			//
			if (msg.dialog != 22)
				return;

			if (m_TransferModule != null)
			{
				m_TransferModule.SendInstantMessage(msg,
				                                    delegate(bool success) { });
			}
		}
	}
}
