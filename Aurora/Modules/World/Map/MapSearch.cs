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
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Timers;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Modules
{
    public class AuroraMapSearchModule : IRegionModule
	{
		private static readonly ILog m_log =
			LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		Scene m_scene = null; // only need one for communication with GridService
		List<Scene> m_scenes = new List<Scene>();
        private IRegionData RegionData = null;
		private IConfigSource m_config;
		private Dictionary<string, string> RegionsHidden = new Dictionary<string, string>();
		private double minutes = 30;
        private double oneminute = 60000;
		private Timer aTimer;
        private InterWorldComms IWC = null;
        bool m_Enabled = true;

		#region IRegionModule Members
		public void Initialise(Scene scene, IConfigSource source)
		{
            if (source.Configs["MapModule"] != null)
            {
                if (source.Configs["MapModule"].GetString(
                        "MapModule", "AuroraMapModule") !=
                        "AuroraMapModule")
                {
                    m_Enabled = false;
                    return;
                }
            }
			m_config = source;
			if (m_scene == null)
			{
				m_scene = scene;
			}
			m_scenes.Add(scene);

			scene.EventManager.OnNewClient += OnNewClient;
		}

		private void OnTimedEvent(object source, ElapsedEventArgs e)
		{
			m_log.DebugFormat("The Elapsed event was raised at {0}", DateTime.Now);
			foreach(Scene scene in m_scenes)
			{
				scene.CreateTerrainTexture();
			}
		}

		public void PostInitialise()
		{
            if (!m_Enabled)
                return;
            //Needs the new grid frontend
            //RegionData = Aurora.DataManager.DataManager.GetDefaultRegionPlugin();
            //RegionsHidden = RegionData.GetRegionHidden();
            aTimer = new System.Timers.Timer(oneminute /** minutes*/);
			aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        	aTimer.Enabled = true;
		}

		public void Close()
		{
			m_scene = null;
			m_scenes.Clear();
		}

		public string Name
		{
            get { return "AuroraMapSearchModule"; }
		}

		public bool IsSharedModule
		{
			get { return true; }
		}

		#endregion

		private void OnNewClient(IClientAPI client)
		{
			client.OnMapNameRequest += OnMapNameRequest;
		}

        private void OnMapNameRequest(IClientAPI remoteClient, string mapName)
        {
            if (mapName.Length < 1)
            {
                remoteClient.SendAlertMessage("Use a search string with at least 1 characters");
                return;
            }
            ScenePresence SP = null;
            Scene scene = GetClientScene(remoteClient);
            scene.TryGetScenePresence(remoteClient.AgentId, out SP);
            // try to fetch from GridServer
            List<GridRegion> regionInfos = m_scene.GridService.GetRegionsByName(UUID.Zero, mapName, 20);
            if (regionInfos == null)
            {
                m_log.Warn("[MAPSEARCHMODULE]: RequestNamedRegions returned null. Old gridserver?");
                return;
            }

            List<MapBlockData> blocks = new List<MapBlockData>();

            MapBlockData data;
            if (regionInfos.Count > 0)
            {
                foreach (OpenSim.Services.Interfaces.GridRegion info in regionInfos)
                {
                    if (SP.GodLevel == 0)
                    {
                        if (RegionsHidden.ContainsValue(info.RegionName))
                        {
                            if (info.EstateOwner == remoteClient.AgentId)
                            {
                                data = new MapBlockData();
                                data.Agents = 0;
                                data.Access = info.Access;
                                data.MapImageId = info.TerrainImage;
                                data.Name = info.RegionName;
                                data.RegionFlags = 0;
                                data.WaterHeight = 0;
                                data.X = (ushort)(info.RegionLocX / Constants.RegionSize);
                                data.Y = (ushort)(info.RegionLocY / Constants.RegionSize);
                                blocks.Add(data);
                            }
                        }
                        else
                        {
                            data = new MapBlockData();
                            data.Agents = 0;
                            data.Access = info.Access;
                            data.MapImageId = info.TerrainImage;
                            data.Name = info.RegionName;
                            data.RegionFlags = 0;
                            data.WaterHeight = 0;
                            data.X = (ushort)(info.RegionLocX / Constants.RegionSize);
                            data.Y = (ushort)(info.RegionLocY / Constants.RegionSize);
                            blocks.Add(data);
                        }
                    }
                    else
                    {
                        data = new MapBlockData();
                        data.Agents = 0;
                        data.Access = info.Access;
                        data.MapImageId = info.TerrainImage;
                        data.Name = info.RegionName;
                        data.RegionFlags = 0;
                        data.WaterHeight = 0;
                        data.X = (ushort)(info.RegionLocX / Constants.RegionSize);
                        data.Y = (ushort)(info.RegionLocY / Constants.RegionSize);
                        blocks.Add(data);
                    }
                }
            }
            // final block, closing the search result
            data = new MapBlockData();
            data.Agents = 0;
            data.Access = 255;
            data.MapImageId = UUID.Zero;
            data.Name = mapName;
            data.RegionFlags = 0;
            data.WaterHeight = 0; // not used
            data.X = 0;
            data.Y = 0;
            blocks.Add(data);

            remoteClient.SendMapBlock(blocks, 0);
        }   

		private bool IsHypergridOn()
		{
			return false;
		}

		private Scene GetClientScene(IClientAPI client)
		{
			foreach (Scene s in m_scenes)
			{
				if (client.Scene.RegionInfo.RegionHandle == s.RegionInfo.RegionHandle)
					return s;
			}
			return m_scene;
		}
	}
}
