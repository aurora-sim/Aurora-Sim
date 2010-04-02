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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Caps=OpenSim.Framework.Capabilities.Caps;
using OSDArray=OpenMetaverse.StructuredData.OSDArray;
using OSDMap=OpenMetaverse.StructuredData.OSDMap;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules
{
    public class AuroraWorldMapModule : INonSharedRegionModule, IWorldMapModule
	{
		private static readonly ILog m_log =
			LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static readonly UUID STOP_UUID = UUID.Random();
		private static readonly string m_mapLayerPath = "0001/";

		private OpenSim.Framework.BlockingQueue<MapRequestState> requests = new OpenSim.Framework.BlockingQueue<MapRequestState>();

		//private IConfig m_config;
		protected Scene m_scene;
		private List<MapBlockData> cachedMapBlocks = new List<MapBlockData>();
		private byte[] myMapImageJPEG;
		protected volatile bool m_Enabled = false;
		private Dictionary<UUID, MapRequestState> m_openRequests = new Dictionary<UUID, MapRequestState>();
		private List<UUID> m_rootAgents = new List<UUID>();
		private volatile bool threadrunning = false;
        private IRegionData GenericData = Aurora.DataManager.DataManager.GetRegionPlugin();
		private IConfigSource m_config;
		private Dictionary<string, string> RegionsHidden = new Dictionary<string, string>();
        private InterWorldComms IWC;
		//private int CacheRegionsDistance = 256;

		#region INonSharedRegionModule Members
		public virtual void Initialise (IConfigSource config)
		{
            m_log.Info("[AuroraWorldMap] Initializing");
			m_config = config;
			m_Enabled = true;
			RegionsHidden = GenericData.GetRegionHidden();
		}

		public virtual void AddRegion (Scene scene)
		{
			if (!m_Enabled)
				return;
			lock (scene)
			{
				m_scene = scene;
				m_scene.RegisterModuleInterface<IWorldMapModule>(this);

				AddHandlers();
			}

		}

		public virtual void RemoveRegion (Scene scene)
		{
			if (!m_Enabled)
				return;

			lock (m_scene)
			{
				m_Enabled = false;
				RemoveHandlers();
				m_scene = null;
			}
		}

		public virtual void RegionLoaded (Scene scene)
		{
            IWC = scene.RequestModuleInterface<InterWorldComms>();
		}


		public virtual void Close()
		{
		}

		public Type ReplaceableInterface
		{
			get { return null; }
		}

		public virtual string Name
		{
            get { return "AuroraWorldMapModule"; }
		}

		#endregion
		// this has to be called with a lock on m_scene
		protected virtual void AddHandlers()
		{
			myMapImageJPEG = new byte[0];

			string regionimage = "regionImage" + m_scene.RegionInfo.RegionID.ToString();
			regionimage = regionimage.Replace("-", "");
			m_log.Info("[WORLD MAP]: JPEG Map location: http://" + m_scene.RegionInfo.ExternalEndPoint.Address.ToString() + ":" + m_scene.RegionInfo.HttpPort.ToString() + "/index.php?method=" + regionimage);

			m_scene.EventManager.OnRegisterCaps += OnRegisterCaps;
			m_scene.EventManager.OnNewClient += OnNewClient;
			m_scene.EventManager.OnClientClosed += ClientLoggedOut;
			m_scene.EventManager.OnMakeChildAgent += MakeChildAgent;
			m_scene.EventManager.OnMakeRootAgent += MakeRootAgent;
		}

		// this has to be called with a lock on m_scene
		protected virtual void RemoveHandlers()
		{
			m_scene.EventManager.OnClientClosed -= ClientLoggedOut;
			m_scene.EventManager.OnNewClient -= OnNewClient;
			m_scene.EventManager.OnRegisterCaps -= OnRegisterCaps;
			m_scene.EventManager.OnMakeChildAgent -= MakeChildAgent;
			m_scene.EventManager.OnMakeRootAgent -= MakeRootAgent;

			string regionimage = "regionImage" + m_scene.RegionInfo.RegionID.ToString();
			regionimage = regionimage.Replace("-", "");
			MainServer.Instance.RemoveHTTPHandler("", regionimage);
		}

		public void OnRegisterCaps(UUID agentID, Caps caps)
		{
			//m_log.DebugFormat("[WORLD MAP]: OnRegisterCaps: agentID {0} caps {1}", agentID, caps);
			string capsBase = "/CAPS/" + caps.CapsObjectPath;
			caps.RegisterHandler("MapLayer",
			                     new RestStreamHandler("POST", capsBase + m_mapLayerPath,
			                                           delegate(string request, string path, string param,
			                                                    OSHttpRequest httpRequest, OSHttpResponse httpResponse)
			                                           {
			                                           	return MapLayerRequest(request, path, param,
			                                           	                       agentID, caps);
			                                           }));
		}

		/// <summary>
		/// Callback for a map layer request
		/// </summary>
		/// <param name="request"></param>
		/// <param name="path"></param>
		/// <param name="param"></param>
		/// <param name="agentID"></param>
		/// <param name="caps"></param>
		/// <returns></returns>
		public string MapLayerRequest(string request, string path, string param,
		                              UUID agentID, Caps caps)
		{
			// this is here because CAPS map requests work even beyond the 10,000 limit.
			ScenePresence avatarPresence;
			m_scene.TryGetAvatar(agentID,out avatarPresence);
            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, agentID);
            List<MapBlockData> mapBlocks = new List<MapBlockData>(); ;

			List<GridRegion> regions = m_scene.GridService.GetRegionRange(m_scene.RegionInfo.ScopeID,
			                                                              (int)(m_scene.RegionInfo.RegionLocX - 50) * (int)Constants.RegionSize,
			                                                              (int)(m_scene.RegionInfo.RegionLocX + 50) * (int)Constants.RegionSize,
			                                                              (int)(m_scene.RegionInfo.RegionLocY - 50) * (int)Constants.RegionSize,
			                                                              (int)(m_scene.RegionInfo.RegionLocY + 50) * (int)Constants.RegionSize);
			foreach (OpenSim.Services.Interfaces.GridRegion r in regions)
			{
				if(account.UserLevel == 0)
				{
					if(RegionsHidden.ContainsValue(r.RegionName))
					{
						if(r.EstateOwner == agentID)
						{
							MapBlockData block = new MapBlockData();
							MapBlockFromGridRegion(block, r);
							mapBlocks.Add(block);
						}
					}
					else
					{
						MapBlockData block = new MapBlockData();
						MapBlockFromGridRegion(block, r);
						mapBlocks.Add(block);
					}
				}
				else
				{
					MapBlockData block = new MapBlockData();
					MapBlockFromGridRegion(block, r);
					mapBlocks.Add(block);
				}
			}
            avatarPresence.ControllingClient.SendMapBlock(mapBlocks, 0);
			
			
			LLSDMapLayerResponse mapResponse = new LLSDMapLayerResponse();
			mapResponse.LayerData.Array.Add(GetOSDMapLayerResponse());
			return mapResponse.ToString();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="mapReq"></param>
		/// <returns></returns>
		public LLSDMapLayerResponse GetMapLayer(LLSDMapRequest mapReq)
		{
			m_log.Debug("[WORLD MAP]: MapLayer Request in region: " + m_scene.RegionInfo.RegionName);
			LLSDMapLayerResponse mapResponse = new LLSDMapLayerResponse();
			mapResponse.LayerData.Array.Add(GetOSDMapLayerResponse());
			return mapResponse;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		protected static OSDMapLayer GetOSDMapLayerResponse()
		{
			OSDMapLayer mapLayer = new OSDMapLayer();
			mapLayer.Right = 5000;
			mapLayer.Top = 5000;
			mapLayer.ImageID = new UUID("00000000-0000-1111-9999-000000000006");

			return mapLayer;
		}
		#region EventHandlers

		/// <summary>
		/// Registered for event
		/// </summary>
		/// <param name="client"></param>
		private void OnNewClient(IClientAPI client)
		{
			client.OnRequestMapBlocks += RequestMapBlocks;
			client.OnMapItemRequest += HandleMapItemRequest;
		}

		/// <summary>
		/// Client logged out, check to see if there are any more root agents in the simulator
		/// If not, stop the mapItemRequest Thread
		/// Event handler
		/// </summary>
		/// <param name="AgentId">AgentID that logged out</param>
		private void ClientLoggedOut(UUID AgentId, Scene scene)
		{
			List<ScenePresence> presences = m_scene.GetAvatars();
			int rootcount = 0;
			for (int i=0;i<presences.Count;i++)
			{
				if (presences[i] != null)
				{
					if (!presences[i].IsChildAgent)
						rootcount++;
				}
			}
			if (rootcount <= 1)
				StopThread();

			lock (m_rootAgents)
			{
				if (m_rootAgents.Contains(AgentId))
				{
					m_rootAgents.Remove(AgentId);
				}
			}
		}
		#endregion

		/// <summary>
		/// Enqueues a 'stop thread' MapRequestState.  Causes the MapItemRequest thread to end
		/// </summary>
		private void StopThread()
		{
			MapRequestState st = new MapRequestState();
			st.agentID=STOP_UUID;
			st.EstateID=0;
			st.flags=0;
			st.godlike=false;
			st.itemtype=0;
			st.regionhandle=0;

			requests.Enqueue(st);
		}

		public virtual void HandleMapItemRequest(IClientAPI remoteClient, uint flags,
		                                         uint EstateID, bool godlike, uint itemtype, ulong regionhandle)
		{
			lock (m_rootAgents)
			{
				if (!m_rootAgents.Contains(remoteClient.AgentId))
					return;
			}
            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, remoteClient.AgentId);
            uint xstart = 0;
			uint ystart = 0;
		    OpenMetaverse.Utils.LongToUInts(m_scene.RegionInfo.RegionHandle, out xstart, out ystart);
			if (itemtype == 6) // we only sevice 6 right now (avatar green dots)
			{
				if (regionhandle == 0 || regionhandle == m_scene.RegionInfo.RegionHandle)
				{
					// Local Map Item Request
					List<ScenePresence> avatars = m_scene.GetAvatars();
					int tc = Environment.TickCount;
					List<mapItemReply> mapitems = new List<mapItemReply>();
					mapItemReply mapitem = new mapItemReply();
					if (avatars.Count == 0 || avatars.Count == 1)
					{
                        if (account.UserLevel == 0)
						{
							if(RegionsHidden.ContainsValue(m_scene.RegionInfo.RegionName))
							{
								if(m_scene.RegionInfo.EstateSettings.EstateOwner == remoteClient.AgentId)
								{
									mapitem = new mapItemReply();
									mapitem.x = (uint)(xstart + 1);
									mapitem.y = (uint)(ystart + 1);
									mapitem.id = UUID.Zero;
									mapitem.name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString());
									mapitem.Extra = 0;
									mapitem.Extra2 = 0;
									mapitems.Add(mapitem);
								}
							}
							else
							{
								mapitem = new mapItemReply();
								mapitem.x = (uint)(xstart + 1);
								mapitem.y = (uint)(ystart + 1);
								mapitem.id = UUID.Zero;
								mapitem.name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString());
								mapitem.Extra = 0;
								mapitem.Extra2 = 0;
								mapitems.Add(mapitem);
							}
						}
						else
						{
							mapitem = new mapItemReply();
							mapitem.x = (uint)(xstart + 1);
							mapitem.y = (uint)(ystart + 1);
							mapitem.id = UUID.Zero;
							mapitem.name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString());
							mapitem.Extra = 0;
							mapitem.Extra2 = 0;
							mapitems.Add(mapitem);
						}
					}
					else
					{
						foreach (ScenePresence av in avatars)
						{
							if(av.GodLevel == 0)
							{
								// Don't send a green dot for yourself
								if (av.UUID != remoteClient.AgentId)
								{
                                    if (account.UserLevel == 0)
									{
										if(RegionsHidden.ContainsValue(m_scene.RegionInfo.RegionName))
										{
											if(m_scene.RegionInfo.EstateSettings.EstateOwner == remoteClient.AgentId)
											{
												mapitem = new mapItemReply();
												mapitem.x = (uint)(xstart + av.AbsolutePosition.X);
												mapitem.y = (uint)(ystart + av.AbsolutePosition.Y);
												mapitem.id = UUID.Zero;
												mapitem.name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString());
												mapitem.Extra = 1;
												mapitem.Extra2 = 0;
												mapitems.Add(mapitem);
											}
										}
										else
										{
											mapitem = new mapItemReply();
											mapitem.x = (uint)(xstart + av.AbsolutePosition.X);
											mapitem.y = (uint)(ystart + av.AbsolutePosition.Y);
											mapitem.id = UUID.Zero;
											mapitem.name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString());
											mapitem.Extra = 1;
											mapitem.Extra2 = 0;
											mapitems.Add(mapitem);
										}
									}
									else
									{
										mapitem = new mapItemReply();
										mapitem.x = (uint)(xstart + av.AbsolutePosition.X);
										mapitem.y = (uint)(ystart + av.AbsolutePosition.Y);
										mapitem.id = UUID.Zero;
										mapitem.name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString());
										mapitem.Extra = 1;
										mapitem.Extra2 = 0;
										mapitems.Add(mapitem);
									}
								}
							}
						}
					}
					remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
				}
				
				else
				{
					// Remote Map Item Request

					// ensures that the blockingqueue doesn't get borked if the GetAgents() timing changes.
					// Note that we only start up a remote mapItem Request thread if there's users who could
					// be making requests
					if (!threadrunning)
					{
						m_log.Warn("[WORLD MAP]: Starting new remote request thread manually.  This means that AvatarEnteringParcel never fired!  This needs to be fixed!  Don't Mantis this, as the developers can see it in this message");
						StartThread(new object());
					}

					RequestMapItems("",remoteClient.AgentId,flags,EstateID,godlike,itemtype,regionhandle);
				}
				
			}
		}
		private void StartThread(object o)
		{
			if (threadrunning) return;
			threadrunning = true;

			//m_log.Debug("[WORLD MAP]: Starting remote MapItem request thread");

			Watchdog.StartThread(process, "MapItemRequestThread", ThreadPriority.BelowNormal, true);
		}
		public void process()
		{
			try
			{
				while (true)
				{
					MapRequestState st = requests.Dequeue(1000);

					// end gracefully
					if (st.agentID == STOP_UUID)
						break;

					if (st.agentID != UUID.Zero)
					{
						bool dorequest = true;
						lock (m_rootAgents)
						{
							if (!m_rootAgents.Contains(st.agentID))
								dorequest = false;
						}

						if (dorequest)
						{
						}
					}

					Watchdog.UpdateThread();
				}
			}
			catch (Exception e)
			{
				m_log.ErrorFormat("[WORLD MAP]: Map item request thread terminated abnormally with exception {0}", e);
			}

			threadrunning = false;
			Watchdog.RemoveThread();
		}

		/// <summary>
		/// Enqueues the map item request into the processing thread
		/// </summary>
		/// <param name="state"></param>
		public void EnqueueMapItemRequest(MapRequestState state)
		{
			requests.Enqueue(state);
		}

		/// <summary>
		/// Sends the mapitem response to the IClientAPI
		/// </summary>
		/// <param name="response">The OSDMap Response for the mapitem</param>
		private void RequestMapItemsCompleted(OSDMap response)
		{
			UUID requestID = response["requestID"].AsUUID();

			if (requestID != UUID.Zero)
			{
				MapRequestState mrs = new MapRequestState();
				mrs.agentID = UUID.Zero;
				lock (m_openRequests)
				{
					if (m_openRequests.ContainsKey(requestID))
					{
						mrs = m_openRequests[requestID];
						m_openRequests.Remove(requestID);
					}
				}

				if (mrs.agentID != UUID.Zero)
				{
					ScenePresence av = null;
					m_scene.TryGetAvatar(mrs.agentID, out av);
					if (av != null)
					{
						if (response.ContainsKey(mrs.itemtype.ToString()))
						{
							List<mapItemReply> returnitems = new List<mapItemReply>();
							OSDArray itemarray = (OSDArray)response[mrs.itemtype.ToString()];
							for (int i = 0; i < itemarray.Count; i++)
							{
								OSDMap mapitem = (OSDMap)itemarray[i];
								mapItemReply mi = new mapItemReply();
								mi.x = (uint)mapitem["X"].AsInteger();
								mi.y = (uint)mapitem["Y"].AsInteger();
								mi.id = mapitem["ID"].AsUUID();
								mi.Extra = mapitem["Extra"].AsInteger();
								mi.Extra2 = mapitem["Extra2"].AsInteger();
								mi.name = mapitem["Name"].AsString();
								returnitems.Add(mi);
							}
							av.ControllingClient.SendMapItemReply(returnitems.ToArray(), mrs.itemtype, mrs.flags);
						}
					}
				}
			}
		}

		/// <summary>
		/// Enqueue the MapItem request for remote processing
		/// </summary>
		/// <param name="httpserver">blank string, we discover this in the process</param>
		/// <param name="id">Agent ID that we are making this request on behalf</param>
		/// <param name="flags">passed in from packet</param>
		/// <param name="EstateID">passed in from packet</param>
		/// <param name="godlike">passed in from packet</param>
		/// <param name="itemtype">passed in from packet</param>
		/// <param name="regionhandle">Region we're looking up</param>
		public void RequestMapItems(string httpserver, UUID id, uint flags,
		                            uint EstateID, bool godlike, uint itemtype, ulong regionhandle)
		{
			MapRequestState st = new MapRequestState();
			st.agentID = id;
			st.flags = flags;
			st.EstateID = EstateID;
			st.godlike = godlike;
			st.itemtype = itemtype;
			st.regionhandle = regionhandle;
			EnqueueMapItemRequest(st);
		}

		/// <summary>
		/// Requests map blocks in area of minX, maxX, minY, MaxY in world cordinates
		/// </summary>
		/// <param name="minX"></param>
		/// <param name="minY"></param>
		/// <param name="maxX"></param>
		/// <param name="maxY"></param>
		public virtual void RequestMapBlocks(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flag)
		{
			if ((flag & 0x10000) != 0)  // user clicked on the map a tile that isn't visible
			{
				List<MapBlockData> response = new List<MapBlockData>();

				// this should return one mapblock at most.
				// (diva note: why?? in that case we should GetRegionByPosition)
				// But make sure: Look whether the one we requested is in there
				List<GridRegion> regions = m_scene.GridService.GetRegionRange(m_scene.RegionInfo.ScopeID,
				                                                              minX * (int)Constants.RegionSize,
				                                                              maxX * (int)Constants.RegionSize,
				                                                              minY * (int)Constants.RegionSize,
				                                                              maxY * (int)Constants.RegionSize);
                UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, remoteClient.AgentId);
				if (regions != null)
				{
					foreach (OpenSim.Services.Interfaces.GridRegion r in regions)
					{
						if ((r.RegionLocX == minX * (int)Constants.RegionSize) &&
						    (r.RegionLocY == minY * (int)Constants.RegionSize))
						{
                            if(account.UserLevel == 0)
                            {
								if(RegionsHidden.ContainsValue(r.RegionName))
								{
									if(r.EstateOwner == remoteClient.AgentId)
									{
										MapBlockData block = new MapBlockData();
										MapBlockFromGridRegion(block, r);
										response.Add(block);
									}
								}
								else
								{
									// found it => add it to response
									MapBlockData block = new MapBlockData();
									MapBlockFromGridRegion(block, r);
									response.Add(block);
								}
							}
							else
							{
								MapBlockData block = new MapBlockData();
								MapBlockFromGridRegion(block, r);
								response.Add(block);
							}
						}
						
					}
				}
                if (response.Count == 0)
				{
					// response still empty => couldn't find the map-tile the user clicked on => tell the client
					MapBlockData block = new MapBlockData();
					block.X = (ushort)minX;
					block.Y = (ushort)minY;
					block.Access = 254; // == not there
					response.Add(block);
				}
				remoteClient.SendMapBlock(response, 0);
			}
			else
			{
				// normal mapblock request. Use the provided values
				GetAndSendBlocks(remoteClient, minX, minY, maxX, maxY, flag);
			}
		}

		protected virtual void GetAndSendBlocks(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flag)
		{
			//UserProfileData UPD = m_scene.CommsManager.UserService.GetUserProfile(remoteClient.AgentId);
			
			List<MapBlockData> mapBlocks = new List<MapBlockData>();
			List<GridRegion> regions = m_scene.GridService.GetRegionRange(m_scene.RegionInfo.ScopeID,
			                                                              (minX - 4) * (int)Constants.RegionSize,
			                                                              (maxX + 4) * (int)Constants.RegionSize,
			                                                              (minY - 4) * (int)Constants.RegionSize,
			                                                              (maxY + 4) * (int)Constants.RegionSize);
			foreach (OpenSim.Services.Interfaces.GridRegion r in regions)
			{
				/*if(UPD.GodLevel == 0)
				{*/
					if(RegionsHidden.ContainsValue(r.RegionName))
					{
						if(r.EstateOwner == remoteClient.AgentId)
						{
							MapBlockData block = new MapBlockData();
							MapBlockFromGridRegion(block, r);
							mapBlocks.Add(block);
						}
					}
					else
					{
						MapBlockData block = new MapBlockData();
						MapBlockFromGridRegion(block, r);
						mapBlocks.Add(block);
					}
				/*}
				else
				{
					MapBlockData block = new MapBlockData();
					MapBlockFromGridRegion(block, r);
					mapBlocks.Add(block);
				}*/
			}
            foreach (OpenSim.Services.Interfaces.GridRegion region in IWC.IWCConnectedRegions.Keys)
            {
                MapBlockData block = new MapBlockData();
                MapBlockFromGridRegion(block, region);
                mapBlocks.Add(block);
            }
			remoteClient.SendMapBlock(mapBlocks, flag);
		}

		protected void MapBlockFromGridRegion(MapBlockData block, GridRegion r)
		{
			block.Access = r.Access;
			block.MapImageId = r.TerrainImage;
			block.Name = r.RegionName;
			block.X = (ushort)(r.RegionLocX / Constants.RegionSize);
			block.Y = (ushort)(r.RegionLocY / Constants.RegionSize);
		}
		private void MakeRootAgent(ScenePresence avatar)
		{
			// You may ask, why this is in a threadpool to start with..
			// The reason is so we don't cause the thread to freeze waiting
			// for the 1 second it costs to start a thread manually.
			if (!threadrunning)
				Util.FireAndForget(this.StartThread);

			lock (m_rootAgents)
			{
				if (!m_rootAgents.Contains(avatar.UUID))
				{
					m_rootAgents.Add(avatar.UUID);
				}
			}
		}
		private Scene GetClientScene(IClientAPI client)
		{
			if (client.Scene.RegionInfo.RegionHandle == m_scene.RegionInfo.RegionHandle)
				return m_scene;
			return m_scene;
		}
		private void MakeChildAgent(ScenePresence avatar)
		{
			List<ScenePresence> presences = m_scene.GetAvatars();
			int rootcount = 0;
			for (int i = 0; i < presences.Count; i++)
			{
				if (presences[i] != null)
				{
					if (!presences[i].IsChildAgent)
						rootcount++;
				}
			}
			if (rootcount <= 1)
				StopThread();

			lock (m_rootAgents)
			{
				if (m_rootAgents.Contains(avatar.UUID))
				{
					m_rootAgents.Remove(avatar.UUID);
				}
			}
		}
		public void LazySaveGeneratedMaptile(byte[] data, bool temporary)
		{
			// Overwrites the local Asset cache with new maptile data
			// Assets are single write, this causes the asset server to ignore this update,
			// but the local asset cache does not

			// this is on purpose!  The net result of this is the region always has the most up to date
			// map tile while protecting the (grid) asset database from bloat caused by a new asset each
			// time a mapimage is generated!

			UUID lastMapRegionUUID = m_scene.RegionInfo.lastMapUUID;

			int lastMapRefresh = 0;
			int twoDays = 172800;
			int RefreshSeconds = twoDays;

			try
			{
				lastMapRefresh = Convert.ToInt32(m_scene.RegionInfo.lastMapRefresh);
			}
			catch (ArgumentException)
			{
			}
			catch (FormatException)
			{
			}
			catch (OverflowException)
			{
			}

			UUID TerrainImageUUID = UUID.Random();

			if (lastMapRegionUUID == UUID.Zero || (lastMapRefresh + RefreshSeconds) < Util.UnixTimeSinceEpoch())
			{
				m_scene.RegionInfo.SaveLastMapUUID(TerrainImageUUID);

				m_log.Debug("[MAPTILE]: STORING MAPTILE IMAGE");
			}
			else
			{
				TerrainImageUUID = lastMapRegionUUID;
				m_log.Debug("[MAPTILE]: REUSING OLD MAPTILE IMAGE ID");
			}

			m_scene.RegionInfo.RegionSettings.TerrainImageID = TerrainImageUUID;

            AssetBase asset = new AssetBase(
                m_scene.RegionInfo.RegionSettings.TerrainImageID,
                "terrainImage_" + m_scene.RegionInfo.RegionID.ToString() + "_" + lastMapRefresh.ToString(),
                (sbyte)AssetType.Texture,
                m_scene.RegionInfo.RegionID.ToString());
			asset.Data = data;
			asset.Description = m_scene.RegionInfo.RegionName;
			asset.Temporary = temporary;
			m_scene.AssetService.Store(asset);
		}
	}

	public struct MapRequestState
	{
		public UUID agentID;
		public uint flags;
		public uint EstateID;
		public bool godlike;
		public uint itemtype;
		public ulong regionhandle;
	}
}
