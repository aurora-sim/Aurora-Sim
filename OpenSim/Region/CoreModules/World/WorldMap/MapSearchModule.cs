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

using System;
using System.Collections.Generic;
using System.Reflection;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Region.CoreModules.World.WorldMap
{
    public class MapSearchModule : ISharedRegionModule
    {
        private readonly List<IScene> m_scenes = new List<IScene>();
        private bool Enabled = true;
        private IScene m_scene; // only need one for communication with GridService

        public bool IsSharedModule
        {
            get { return true; }
        }

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
            if (source.Configs["MapModule"] != null)
            {
                if (source.Configs["MapModule"].GetString(
                    "WorldMapModule", "AuroraWorldMapModule") !=
                    "WorldMapModule")
                {
                    Enabled = false;
                }
            }
            else
            {
                Enabled = false;
            }
        }

        public void AddRegion(IScene scene)
        {
            if (!Enabled)
                return;

            if (m_scene == null)
                m_scene = scene;

            m_scenes.Add(scene);
            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
        }

        public void RemoveRegion(IScene scene)
        {
            m_scenes.Remove(scene);
            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public void RegionLoaded(IScene scene)
        {
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
            m_scene = null;
            m_scenes.Clear();
        }

        public string Name
        {
            get { return "MapSearchModule"; }
        }

        #endregion

        private void OnNewClient(IClientAPI client)
        {
            client.OnMapNameRequest += OnMapNameRequest;
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnMapNameRequest -= OnMapNameRequest;
        }

        private void OnMapNameRequest(IClientAPI remoteClient, string mapName, uint flags)
        {
            if (mapName.Length < 3)
            {
                remoteClient.SendAlertMessage("Use a search string with at least 3 characters");
                return;
            }

            // try to fetch from GridServer
            List<GridRegion> regionInfos = m_scene.GridService.GetRegionsByName(UUID.Zero, mapName, 20);
            List<MapBlockData> blocks = new List<MapBlockData>();

            MapBlockData data;
            if (regionInfos != null && regionInfos.Count > 0)
            {
                foreach (GridRegion info in regionInfos)
                {
                    data = new MapBlockData
                               {
                                   Agents = 0,
                                   Access = info.Access,
                                   MapImageID = info.TerrainImage,
                                   Name = info.RegionName,
                                   RegionFlags = 0,
                                   WaterHeight = 0,
                                   X = (ushort) (info.RegionLocX/Constants.RegionSize),
                                   Y = (ushort) (info.RegionLocY/Constants.RegionSize),
                                   SizeX = (ushort) (info.RegionSizeX),
                                   SizeY = (ushort) (info.RegionSizeY)
                               };
                    // not used
                    blocks.Add(data);
                }
            }

            // final block, closing the search result
            data = new MapBlockData
                       {
                           Agents = 0,
                           Access = 255,
                           MapImageID = UUID.Zero,
                           Name = mapName,
                           RegionFlags = 0,
                           WaterHeight = 0,
                           X = 0,
                           Y = 0,
                           SizeX = 256,
                           SizeY = 256
                       };
            // not used
            blocks.Add(data);

            remoteClient.SendMapBlock(blocks, 2);
        }
    }
}