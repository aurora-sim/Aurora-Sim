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
using System.Reflection;
using System.Net;
using System.Timers;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.CoreModules.World.Terrain.FileLoaders;
using OpenSim.Region.CoreModules.World.Terrain.FloodBrushes;
using OpenSim.Region.CoreModules.World.Terrain.PaintBrushes;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.CoreModules.World.Terrain
{
    public class TerrainModule : INonSharedRegionModule, ITerrainModule
    {
        #region StandardTerrainEffects enum

        /// <summary>
        /// A standard set of terrain brushes and effects recognised by viewers
        /// </summary>
        public enum StandardTerrainEffects : byte
        {
            Flatten = 0,
            Raise = 1,
            Lower = 2,
            Smooth = 3,
            Noise = 4,
            Revert = 5,

            // Extended brushes for Aurora
            Erode = 255,
            Weather = 254,
            Olsen = 253
        }

        #endregion

        private static List<Scene> m_scenes = new List<Scene>();
        private static List<TerrainModule> m_terrainModules = new List<TerrainModule>();

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private readonly Dictionary<StandardTerrainEffects, ITerrainFloodEffect> m_floodeffects =
            new Dictionary<StandardTerrainEffects, ITerrainFloodEffect>();

        private readonly Dictionary<string, ITerrainLoader> m_loaders = new Dictionary<string, ITerrainLoader>();

        private readonly Dictionary<StandardTerrainEffects, ITerrainPaintableEffect> m_painteffects =
            new Dictionary<StandardTerrainEffects, ITerrainPaintableEffect>();

        private ITerrainChannel m_channel;
        private ITerrainChannel m_waterChannel;
        private Dictionary<string, ITerrainEffect> m_plugineffects;
        private ITerrainChannel m_revert;
        private ITerrainChannel m_waterRevert;
        private Scene m_scene;

        public ITerrainChannel TerrainMap
        {
            get { return m_channel; }
        }

        public ITerrainChannel TerrainRevertMap
        {
            get { return m_revert; }
        }

        private long m_queueNextSave = 0;
        private int m_savetime = 2; // seconds to wait before saving terrain
        private Timer m_queueTimer = new Timer();

        private const int MAX_HEIGHT = 250;
        private const int MIN_HEIGHT = 0;
        private readonly UndoStack<LandUndoState> m_undo = new UndoStack<LandUndoState>(5);
        private bool m_sendTerrainUpdatesByViewDistance = false;
        protected Dictionary<UUID, bool[,]> m_terrainPatchesSent = new Dictionary<UUID, bool[,]>();
        protected bool m_use3DWater = false;

        #region INonSharedRegionModule Members

        /// <summary>
        /// Creates and initialises a terrain module for a region
        /// </summary>
        /// <param name="scene">Region initialising</param>
        /// <param name="config">Config for the region</param>
        public void Initialise(IConfigSource config)
        {
            if (config.Configs["TerrainModule"] != null)
            {
                m_sendTerrainUpdatesByViewDistance = config.Configs["TerrainModule"].GetBoolean("SendTerrainByViewDistance", m_sendTerrainUpdatesByViewDistance);
                m_use3DWater = config.Configs["TerrainModule"].GetBoolean("Use3DWater", m_use3DWater);
                m_savetime = config.Configs["TerrainModule"].GetInt("QueueSaveTime", m_savetime);
            }
        }

        public void AddRegion(Scene scene)
        {
            bool firstScene = m_scenes.Count == 0;
            m_scene = scene;
            m_scenes.Add(scene);
            m_terrainModules.Add(this);

            LoadWorldHeightmap();
            LoadWorldWaterMap();
            scene.PhysicsScene.SetTerrain(m_channel.GetSerialised(scene));
            UpdateWaterHeight(scene.RegionInfo.RegionSettings.WaterHeight);

            m_scene.RegisterModuleInterface<ITerrainModule>(this);
            m_scene.EventManager.OnNewClient += EventManager_OnNewClient;
            m_scene.EventManager.OnClosingClient += OnClosingClient;
            m_scene.EventManager.OnSignificantClientMovement += EventManager_OnSignificantClientMovement;
            m_scene.AuroraEventManager.OnGenericEvent += AuroraEventManager_OnGenericEvent;
            m_scene.EventManager.OnNewPresence += OnNewPresence;

            if (firstScene)
                AddConsoleCommands();

            InstallDefaultEffects();
            LoadPlugins();

            m_queueTimer.Enabled = false;
            m_queueTimer.AutoReset = true;
            m_queueTimer.Interval = m_savetime * 1000;
            m_queueTimer.Elapsed += TerrainUpdateTimer;
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
            lock (m_scene)
            {
                // remove the event-handlers
                m_scene.EventManager.OnNewClient -= EventManager_OnNewClient;
                m_scene.EventManager.OnClosingClient -= OnClosingClient;
                m_scene.EventManager.OnSignificantClientMovement -= EventManager_OnSignificantClientMovement;
                m_scene.AuroraEventManager.OnGenericEvent -= AuroraEventManager_OnGenericEvent;
                m_scene.EventManager.OnNewPresence -= OnNewPresence;

                // remove the interface
                m_scene.UnregisterModuleInterface<ITerrainModule>(this);
            }
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
            get { return "TerrainModule"; }
        }

        #endregion

        #region ITerrainModule Members

        public void TerrainUpdateTimer(object sender, EventArgs ea)
        {
            long now = DateTime.Now.Ticks;

            if (m_queueNextSave > 0 && m_queueNextSave < now)
            {
                m_queueNextSave = 0;
                //Save the terarin
                SaveTerrain();
                m_scene.PhysicsScene.SetTerrain(m_channel.GetSerialised(m_scene));
                
                if(m_queueNextSave == 0)
                    m_queueTimer.Stop();
            }
        }

        public void QueueTerrainUpdate()
        {
            m_queueNextSave = DateTime.Now.Ticks + Convert.ToInt64(m_savetime * 1000 * 10000);
            m_queueTimer.Start();
        }

        public void UpdateWaterHeight(double height)
        {
            short[] waterMap;
            if (m_waterChannel == null)
            {
                waterMap = new short[m_scene.RegionInfo.RegionSizeX * m_scene.RegionInfo.RegionSizeY];
                for (int x = 0; x < m_scene.RegionInfo.RegionSizeX; x++)
                {
                    for (int y = 0; y < m_scene.RegionInfo.RegionSizeY; y++)
                    {
                        waterMap[y * m_scene.RegionInfo.RegionSizeX + x] = (short)height;
                    }
                }
            }
            else
                waterMap = m_waterChannel.GetSerialised(m_scene);
            m_scene.PhysicsScene.SetWaterLevel(waterMap);
        }

        /// <summary>
        /// Installs terrain brush hook to IClientAPI
        /// </summary>
        /// <param name="client"></param>
        private void EventManager_OnNewClient(IClientAPI client)
        {
            client.OnModifyTerrain += client_OnModifyTerrain;
            client.OnBakeTerrain += client_OnBakeTerrain;
            client.OnLandUndo += client_OnLandUndo;
            client.OnGodlikeMessage += client_onGodlikeMessage;
            client.OnUnackedTerrain += client_OnUnackedTerrain;
            client.OnRegionHandShakeReply += SendLayerData;

            //Add them to the cache
            lock (m_terrainPatchesSent)
            {
                if (!m_terrainPatchesSent.ContainsKey(client.AgentId))
                {
                    IScenePresence agent = m_scene.GetScenePresence (client.AgentId);
                    if (agent != null && agent.IsChildAgent)
                    {
                        //If the avatar is a child agent, we need to send the terrain data initially
                        EventManager_OnSignificantClientMovement(client);
                    }
                }
            }
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnModifyTerrain -= client_OnModifyTerrain;
            client.OnBakeTerrain -= client_OnBakeTerrain;
            client.OnLandUndo -= client_OnLandUndo;
            client.OnGodlikeMessage -= client_onGodlikeMessage;
            client.OnUnackedTerrain -= client_OnUnackedTerrain;
            client.OnRegionHandShakeReply -= SendLayerData;

            //Remove them from the cache
            lock (m_terrainPatchesSent)
            {
                m_terrainPatchesSent.Remove(client.AgentId);
            }
        }

        /// <summary>
        /// Send the region heightmap to the client
        /// </summary>
        /// <param name="RemoteClient">Client to send to</param>
        public void SendLayerData(IClientAPI RemoteClient)
        {
            if (!m_sendTerrainUpdatesByViewDistance)
            {
                //Default way, send the full terrain at once
                RemoteClient.SendLayerData(m_channel.GetSerialised(RemoteClient.Scene));
            }
            else
            {
                //Send only what the client can see,
                //  but the client isn't loaded yet, wait until they get set up
                //  The first agent update they send will trigger the DrawDistanceChanged event and send the land
            }
        }

        object AuroraEventManager_OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "DrawDistanceChanged" || FunctionName == "SignficantCameraMovement")
            {
                SendTerrainUpdatesForClient ((IScenePresence)parameters);
            }
            return null;
        }

        void EventManager_OnSignificantClientMovement(IClientAPI remote_client)
        {
            IScenePresence presence = m_scene.GetScenePresence (remote_client.AgentId);
            SendTerrainUpdatesForClient(presence);
        }

        void OnNewPresence (IScenePresence presence)
        {
            SendTerrainUpdatesForClient(presence);
        }

        protected void SendTerrainUpdatesForClient (IScenePresence presence)
        {
            if (!m_sendTerrainUpdatesByViewDistance)
                return;

            if (presence == null)
                return;

            bool[,] terrainarray;
            lock (m_terrainPatchesSent)
            {
                m_terrainPatchesSent.TryGetValue(presence.UUID, out terrainarray);
            }
            bool fillLater = false;
            if (terrainarray == null)
            {
                int xSize = m_scene.RegionInfo.RegionSizeX != int.MaxValue ? m_scene.RegionInfo.RegionSizeX / Constants.TerrainPatchSize : Constants.RegionSize / Constants.TerrainPatchSize;
                int ySize = m_scene.RegionInfo.RegionSizeX != int.MaxValue ? m_scene.RegionInfo.RegionSizeY / Constants.TerrainPatchSize : Constants.RegionSize / Constants.TerrainPatchSize;
                terrainarray = new bool[xSize, ySize];
                fillLater = true;
            }

            List<int> xs = new List<int>();
            List<int> ys = new List<int> ();
            int startX = (((int)(presence.AbsolutePosition.X - presence.DrawDistance)) / Constants.TerrainPatchSize) - 1;
            startX = Math.Max (startX, 0);
            startX = Math.Min (startX, m_scene.RegionInfo.RegionSizeX / Constants.TerrainPatchSize);
            int startY = (((int)(presence.AbsolutePosition.Y - presence.DrawDistance)) / Constants.TerrainPatchSize) - 1;
            startY = Math.Max (startY, 0);
            startY = Math.Min (startY, m_scene.RegionInfo.RegionSizeY / Constants.TerrainPatchSize);
            int endX = (((int)(presence.AbsolutePosition.X + presence.DrawDistance)) / Constants.TerrainPatchSize) + 1;
            endX = Math.Max (endX, 0);
            endX = Math.Min (endX, m_scene.RegionInfo.RegionSizeX / Constants.TerrainPatchSize);
            int endY = (((int)(presence.AbsolutePosition.Y + presence.DrawDistance)) / Constants.TerrainPatchSize) + 1;
            endY = Math.Max (endY, 0);
            endY = Math.Min (endY, m_scene.RegionInfo.RegionSizeY / Constants.TerrainPatchSize);
            for (int x = startX; x <
                    endX; x++) 
            {
                for (int y = startY; y <
                    endY; y++) 
                {
                    if(x < 0 || y < 0 || x >= m_scene.RegionInfo.RegionSizeX / Constants.TerrainPatchSize ||
                        y >= m_scene.RegionInfo.RegionSizeY / Constants.TerrainPatchSize)
                        continue;
                    //Need to make sure we don't send the same ones over and over
                    if (!terrainarray[x, y])
                    {
                        //Check which has less distance, camera or avatar position, both have to be done
                        if (Util.DistanceLessThan(presence.AbsolutePosition,
                            new Vector3(x * Constants.TerrainPatchSize, y * Constants.TerrainPatchSize, 0), presence.DrawDistance + 35) ||
                        Util.DistanceLessThan(presence.CameraPosition,
                            new Vector3(x * Constants.TerrainPatchSize, y * Constants.TerrainPatchSize, 0), presence.DrawDistance + 35)) //Its not a radius, its a diameter and we add 35 so that it doesn't look like it cuts off
                        {
                            //They can see it, send it ot them
                            terrainarray[x, y] = true;
                            xs.Add(x);
                            ys.Add(y);
                            //Wait and send them all at once
                            //presence.ControllingClient.SendLayerData(x, y, serializedMap);
                        }
                    }
                }
            }
            if (xs.Count != 0)
            {
                //Send all the terrain patches at once
                presence.ControllingClient.SendLayerData(xs.ToArray(), ys.ToArray(), m_channel.GetSerialised(m_scene), TerrainPatch.LayerType.Land);
                if (m_use3DWater)
                {
                    //Send all the water patches at once
                    presence.ControllingClient.SendLayerData(xs.ToArray(), ys.ToArray(), m_waterChannel.GetSerialised(m_scene), TerrainPatch.LayerType.Water);
                }
            }
            if ((xs.Count != 0) || (fillLater))
            {
                if (m_terrainPatchesSent.ContainsKey(presence.UUID))
                {
                    lock (m_terrainPatchesSent)
                        m_terrainPatchesSent[presence.UUID] = terrainarray;
                }
                else
                {
                    lock (m_terrainPatchesSent)
                        m_terrainPatchesSent.Add(presence.UUID, terrainarray);
                }
            }
        }

        /// <summary>
        /// Store the terrain in the persistant data store
        /// </summary>
        public void SaveTerrain ()
        {
            m_scene.SimulationDataService.StoreTerrain(m_channel.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, false);
        }

        /// <summary>
        /// Store the terrain in the persistant data store
        /// </summary>
        public void SaveWater()
        {
            m_scene.SimulationDataService.StoreWater(m_waterChannel.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, false);
        }

        /// <summary>
        /// Reset the terrain of this region to the default
        /// </summary>
        public void ResetTerrain()
        {
            TerrainChannel channel = new TerrainChannel(m_scene);
            m_channel = channel;
            SaveRevertTerrain(channel);
            m_scene.RegisterModuleInterface<ITerrainChannel>(m_channel);
            CheckForTerrainUpdates(false, true, false);
        }

        /// <summary>
        /// Reset the terrain of this region to the default
        /// </summary>
        public void ResetWater()
        {
            TerrainChannel channel = new TerrainChannel(m_scene);
            m_waterChannel = channel;
            SaveRevertWater(m_waterChannel);
            CheckForTerrainUpdates(false, true, true);
        }

        /// <summary>
        /// Store the revert terrain in the persistant data store
        /// </summary>
        public void SaveRevertTerrain(ITerrainChannel channel)
        {
            m_scene.SimulationDataService.StoreTerrain(m_revert.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, true);
        }

        /// <summary>
        /// Store the revert terrain in the persistant data store
        /// </summary>
        public void SaveRevertWater(ITerrainChannel channel)
        {
            m_scene.SimulationDataService.StoreWater(m_waterRevert.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, true);
        }

        /// <summary>
        /// Loads the World Revert heightmap
        /// </summary>
        public void LoadRevertMap()
        {
            try
            {
                short[] map = m_scene.SimulationDataService.LoadTerrain(m_scene.RegionInfo.RegionID, true, m_scene.RegionInfo.RegionSizeX, m_scene.RegionInfo.RegionSizeY);
                if (map == null)
                {
                    m_revert = m_channel.MakeCopy();

                    m_scene.SimulationDataService.StoreTerrain(m_revert.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, true);
                }
                else
                {
                    m_revert = new TerrainChannel(map, m_scene);
                }
            }
            catch (IOException e)
            {
                m_log.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e.ToString() + " Regenerating");
                // Non standard region size.    If there's an old terrain in the database, it might read past the buffer
                if (m_scene.RegionInfo.RegionSizeX != Constants.RegionSize || m_scene.RegionInfo.RegionSizeY != Constants.RegionSize)
                {
                    m_revert = m_channel.MakeCopy();

                    m_scene.SimulationDataService.StoreTerrain(m_revert.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, true);
                }
            }
            catch (IndexOutOfRangeException e)
            {
                m_log.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e.ToString() + " Regenerating");
                m_revert = m_channel.MakeCopy();

                m_scene.SimulationDataService.StoreTerrain(m_revert.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, true);
            }
            catch (ArgumentOutOfRangeException e)
            {
                m_log.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e.ToString() + " Regenerating");
                m_revert = m_channel.MakeCopy();

                m_scene.SimulationDataService.StoreTerrain(m_revert.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, true);
            }
            catch (Exception e)
            {
                m_log.Warn("[TERRAIN]: Scene.cs: LoadWorldMap() - Failed with exception " + e.ToString());
            }
        }

        /// <summary>
        /// Loads the World Revert heightmap
        /// </summary>
        public void LoadRevertWaterMap()
        {
            try
            {
                short[] map = m_scene.SimulationDataService.LoadWater(m_scene.RegionInfo.RegionID, true, m_scene.RegionInfo.RegionSizeX, m_scene.RegionInfo.RegionSizeY);
                if (map == null)
                {
                    m_waterRevert = m_waterChannel.MakeCopy();

                    m_scene.SimulationDataService.StoreWater(m_waterRevert.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, true);
                }
                else
                {
                    m_waterRevert = new TerrainChannel(map, m_scene);
                }
            }
            catch (IOException e)
            {
                m_log.Warn("[TERRAIN]: LoadRevertWaterMap() - Failed with exception " + e.ToString() + " Regenerating");
                // Non standard region size.    If there's an old terrain in the database, it might read past the buffer
                if (m_scene.RegionInfo.RegionSizeX != Constants.RegionSize || m_scene.RegionInfo.RegionSizeY != Constants.RegionSize)
                {
                    m_waterRevert = m_waterChannel.MakeCopy();

                    m_scene.SimulationDataService.StoreWater(m_waterRevert.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, true);
                }
            }
            catch (IndexOutOfRangeException e)
            {
                m_log.Warn("[TERRAIN]: LoadRevertWaterMap() - Failed with exception " + e.ToString() + " Regenerating");
                m_waterRevert = m_waterChannel.MakeCopy();

                m_scene.SimulationDataService.StoreWater(m_waterRevert.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, true);
            }
            catch (ArgumentOutOfRangeException e)
            {
                m_log.Warn("[TERRAIN]: LoadRevertWaterMap() - Failed with exception " + e.ToString() + " Regenerating");
                m_waterRevert = m_waterChannel.MakeCopy();

                m_scene.SimulationDataService.StoreWater(m_waterRevert.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, true);
            }
            catch (Exception e)
            {
                m_log.Warn("[TERRAIN]: LoadRevertWaterMap() - Failed with exception " + e.ToString());
                m_waterRevert = m_waterChannel.MakeCopy();

                m_scene.SimulationDataService.StoreWater(m_waterRevert.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, true);
            }
        }

        /// <summary>
        /// Loads the World heightmap
        /// </summary>
        public void LoadWorldHeightmap()
        {
            try
            {
                short[] map = m_scene.SimulationDataService.LoadTerrain(m_scene.RegionInfo.RegionID, false, m_scene.RegionInfo.RegionSizeX, m_scene.RegionInfo.RegionSizeY);
                if (map == null)
                {
                    m_log.Info("[TERRAIN]: No default terrain. Generating a new terrain.");
                    m_channel = new TerrainChannel(m_scene);

                    m_scene.SimulationDataService.StoreTerrain(m_channel.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, false);
                }
                else
                {
                    m_channel = new TerrainChannel(map, m_scene);
                }
            }
            catch (IOException e)
            {
                m_log.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e.ToString() + " Regenerating");
                // Non standard region size.    If there's an old terrain in the database, it might read past the buffer
                if (m_scene.RegionInfo.RegionSizeX != Constants.RegionSize || m_scene.RegionInfo.RegionSizeY != Constants.RegionSize)
                {
                    m_channel = new TerrainChannel(m_scene);

                    m_scene.SimulationDataService.StoreTerrain(m_channel.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, false);
                }
            }
            catch (IndexOutOfRangeException e)
            {
                m_log.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e.ToString() + " Regenerating");
                m_channel = new TerrainChannel(m_scene);

                m_scene.SimulationDataService.StoreTerrain(m_channel.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, false);
            }
            catch (ArgumentOutOfRangeException e)
            {
                m_log.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e.ToString() + " Regenerating");
                m_channel = new TerrainChannel(m_scene);

                m_scene.SimulationDataService.StoreTerrain(m_channel.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, false);
            }
            catch (Exception e)
            {
                m_log.Warn("[TERRAIN]: Scene.cs: LoadWorldMap() - Failed with exception " + e.ToString());
                m_channel = new TerrainChannel(m_scene);

                m_scene.SimulationDataService.StoreTerrain(m_channel.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, false);
            }
            LoadRevertMap();
            m_scene.RegisterModuleInterface<ITerrainChannel>(m_channel);
        }

        /// <summary>
        /// Loads the World heightmap
        /// </summary>
        public void LoadWorldWaterMap()
        {
            if (!m_use3DWater)
                return;
            try
            {
                short[] map = m_scene.SimulationDataService.LoadWater(m_scene.RegionInfo.RegionID, false, m_scene.RegionInfo.RegionSizeX, m_scene.RegionInfo.RegionSizeY);
                if (map == null)
                {
                    m_log.Info("[TERRAIN]: No default water. Generating a new water.");
                    m_waterChannel = new TerrainChannel(m_scene);

                    m_scene.SimulationDataService.StoreWater(m_waterChannel.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, false);
                }
                else
                {
                    m_channel = new TerrainChannel(map, m_scene);
                }
            }
            catch (IOException e)
            {
                m_log.Warn("[TERRAIN]: LoadWorldWaterMap() - Failed with exception " + e.ToString() + " Regenerating");
                // Non standard region size.    If there's an old terrain in the database, it might read past the buffer
                if (m_scene.RegionInfo.RegionSizeX != Constants.RegionSize || m_scene.RegionInfo.RegionSizeY != Constants.RegionSize)
                {
                    m_waterChannel = new TerrainChannel(m_scene);

                    m_scene.SimulationDataService.StoreWater(m_waterChannel.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, false);
                }
            }
            catch (IndexOutOfRangeException e)
            {
                m_log.Warn("[TERRAIN]: LoadWorldWaterMap() - Failed with exception " + e.ToString() + " Regenerating");
                m_waterChannel = new TerrainChannel(m_scene);

                m_scene.SimulationDataService.StoreWater(m_waterChannel.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, false);
            }
            catch (ArgumentOutOfRangeException e)
            {
                m_log.Warn("[TERRAIN]: LoadWorldWaterMap() - Failed with exception " + e.ToString() + " Regenerating");
                m_waterChannel = new TerrainChannel(m_scene);

                m_scene.SimulationDataService.StoreWater(m_waterChannel.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, false);
            }
            catch (Exception e)
            {
                m_log.Warn("[TERRAIN]: Scene.cs: LoadWorldMap() - Failed with exception " + e.ToString());
                m_waterChannel = new TerrainChannel(m_scene);

                m_scene.SimulationDataService.StoreWater(m_waterChannel.GetSerialised(m_scene), m_scene.RegionInfo.RegionID, false);
            }
            LoadRevertWaterMap();
        }

        public void UndoTerrain(ITerrainChannel channel)
        {
            m_channel = channel;
        }

        /// <summary>
        /// Loads a terrain file from disk and installs it in the scene.
        /// </summary>
        /// <param name="filename">Filename to terrain file. Type is determined by extension.</param>
        public void LoadFromFile(string filename, int offsetX, int offsetY)
        {
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
            {
                if (filename.EndsWith(loader.Key))
                {
                    lock (m_scene)
                    {
                        try
                        {
                            ITerrainChannel channel = loader.Value.LoadFile (filename, m_scene);
                            channel.Scene = m_scene;
                            if (m_channel.Height == channel.Height &&
                                    m_channel.Width == channel.Width)
                            {
                                m_channel = channel;
                                m_scene.RegisterModuleInterface<ITerrainChannel>(m_channel);
                                m_log.DebugFormat("[TERRAIN]: Loaded terrain, wd/ht: {0}/{1}", channel.Width, channel.Height);
                            }
                            else
                            {
                                //Make sure it is in bounds
                                if ((offsetX + channel.Width) > m_channel.Width ||
                                        (offsetY + channel.Height) > m_channel.Height)
                                {
                                    m_log.Error("[TERRAIN]: Unable to load heightmap, the terrain you have given is larger than the current region.");
                                    return;
                                }
                                else
                                {
                                    //Merge the terrains together at the specified offset
                                    for (int x = offsetX; x < offsetX + channel.Width; x++)
                                    {
                                        for (int y = offsetY; y < offsetY + channel.Height; y++)
                                        {
                                            m_channel[x, y] = channel[x - offsetX, y - offsetY];
                                        }
                                    }
                                    m_log.DebugFormat("[TERRAIN]: Loaded terrain, wd/ht: {0}/{1}", channel.Width, channel.Height);
                                }
                            }
                            UpdateRevertMap();
                        }
                        catch (NotImplementedException)
                        {
                            m_log.Error("[TERRAIN]: Unable to load heightmap, the " + loader.Value +
                                        " parser does not support file loading. (May be save only)");
                            throw new TerrainException(String.Format("unable to load heightmap: parser {0} does not support loading", loader.Value));
                        }
                        catch (FileNotFoundException)
                        {
                            m_log.Error(
                                "[TERRAIN]: Unable to load heightmap, file not found. (A directory permissions error may also cause this)");
                            throw new TerrainException(
                                String.Format("unable to load heightmap: file {0} not found (or permissions do not allow access", filename));
                        }
                        catch (ArgumentException e)
                        {
                            m_log.ErrorFormat("[TERRAIN]: Unable to load heightmap: {0}", e.Message);
                            throw new TerrainException(
                                String.Format("Unable to load heightmap: {0}", e.Message));
                        }
                    }
                    CheckForTerrainUpdates();
                    m_log.Info("[TERRAIN]: File (" + filename + ") loaded successfully");
                    return;
                }
            }

            m_log.Error("[TERRAIN]: Unable to load heightmap, no file loader available for that format.");
            throw new TerrainException(String.Format("unable to load heightmap from file {0}: no loader available for that format", filename));
        }

        /// <summary>
        /// Saves the current heightmap to a specified file.
        /// </summary>
        /// <param name="filename">The destination filename</param>
        public void SaveToFile(string filename)
        {
            try
            {
                foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
                {
                    if (filename.EndsWith(loader.Key))
                    {
                        loader.Value.SaveFile(filename, m_channel);
                        return;
                    }
                }
            }
            catch (NotImplementedException)
            {
                m_log.Error("Unable to save to " + filename + ", saving of this file format has not been implemented.");
                throw new TerrainException(String.Format("Unable to save heightmap: saving of this file format not implemented"));
            }
            catch (IOException ioe)
            {
                m_log.Error(String.Format("[TERRAIN]: Unable to save to {0}, {1}", filename, ioe.Message));
                throw new TerrainException(String.Format("Unable to save heightmap: {0}", ioe.Message));
            }
        }

        /// <summary>
        /// Loads a terrain file from the specified URI
        /// </summary>
        /// <param name="filename">The name of the terrain to load</param>
        /// <param name="pathToTerrainHeightmap">The URI to the terrain height map</param>
        public void LoadFromStream(string filename, Uri pathToTerrainHeightmap)
        {
            LoadFromStream(filename, URIFetch(pathToTerrainHeightmap));
        }

        /// <summary>
        /// Loads a terrain file from a stream and installs it in the scene.
        /// </summary>
        /// <param name="filename">Filename to terrain file. Type is determined by extension.</param>
        /// <param name="stream"></param>
        public void LoadFromStream(string filename, Stream stream)
        {
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
            {
                if (filename.EndsWith(loader.Key))
                {
                    lock (m_scene)
                    {
                        try
                        {
                            ITerrainChannel channel = loader.Value.LoadStream (stream, m_scene);
                            if (channel != null)
                            {
                                channel.Scene = m_scene;
                                if (m_channel.Height == channel.Height &&
                                    m_channel.Width == channel.Width)
                                {
                                    m_channel = channel;
                                    m_scene.RegisterModuleInterface<ITerrainChannel>(m_channel);
                                }
                                else
                                {
                                    //Make sure it is in bounds
                                    if ((channel.Width) > m_channel.Width ||
                                            (channel.Height) > m_channel.Height)
                                    {
                                        m_log.Error("[TERRAIN]: Unable to load heightmap, the terrain you have given is larger than the current region.");
                                        return;
                                    }
                                    else
                                    {
                                        //Merge the terrains together at the specified offset
                                        for (int x = 0; x < channel.Width; x++)
                                        {
                                            for (int y = 0; y < channel.Height; y++)
                                            {
                                                m_channel[x, y] = channel[x, y];
                                            }
                                        }
                                        m_log.DebugFormat("[TERRAIN]: Loaded terrain, wd/ht: {0}/{1}", channel.Width, channel.Height);
                                    }
                                }
                                UpdateRevertMap();
                            }
                        }
                        catch (NotImplementedException)
                        {
                            m_log.Error("[TERRAIN]: Unable to load heightmap, the " + loader.Value +
                                        " parser does not support file loading. (May be save only)");
                            throw new TerrainException(String.Format("unable to load heightmap: parser {0} does not support loading", loader.Value));
                        }
                    }

                    CheckForTerrainUpdates();
                    m_log.Info("[TERRAIN]: File (" + filename + ") loaded successfully");
                    return;
                }
            }
            m_log.Error("[TERRAIN]: Unable to load heightmap, no file loader available for that format.");
            throw new TerrainException(String.Format("unable to load heightmap from file {0}: no loader available for that format", filename));
        }

        /// <summary>
        /// Loads a terrain file from a stream and installs it in the scene.
        /// </summary>
        /// <param name="filename">Filename to terrain file. Type is determined by extension.</param>
        /// <param name="stream"></param>
        public void LoadFromStream(string filename, Stream stream, int offsetX, int offsetY)
        {
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
            {
                if (filename.EndsWith(loader.Key))
                {
                    lock (m_scene)
                    {
                        try
                        {
                            ITerrainChannel channel = loader.Value.LoadStream (stream, m_scene);
                            if (channel != null)
                            {
                                channel.Scene = m_scene;
                                if (m_channel.Height == channel.Height &&
                                    m_channel.Width == channel.Width)
                                {
                                    m_channel = channel;
                                    m_scene.RegisterModuleInterface<ITerrainChannel>(m_channel);
                                }
                                else
                                {
                                    //Make sure it is in bounds
                                    if ((offsetX + channel.Width) > m_channel.Width ||
                                            (offsetY + channel.Height) > m_channel.Height)
                                    {
                                        m_log.Error("[TERRAIN]: Unable to load heightmap, the terrain you have given is larger than the current region.");
                                        return;
                                    }
                                    else
                                    {
                                        //Merge the terrains together at the specified offset
                                        for (int x = offsetX; x < offsetX + channel.Width; x++)
                                        {
                                            for (int y = offsetY; y < offsetY + channel.Height; y++)
                                            {
                                                m_channel[x, y] = channel[x - offsetX, y - offsetY];
                                            }
                                        }
                                    }
                                }
                                UpdateRevertMap();
                            }
                        }
                        catch (NotImplementedException)
                        {
                            m_log.Error("[TERRAIN]: Unable to load heightmap, the " + loader.Value +
                                        " parser does not support file loading. (May be save only)");
                            throw new TerrainException(String.Format("unable to load heightmap: parser {0} does not support loading", loader.Value));
                        }
                    }

                    CheckForTerrainUpdates();
                    m_log.Info("[TERRAIN]: File (" + filename + ") loaded successfully");
                    return;
                }
            }
            m_log.Error("[TERRAIN]: Unable to load heightmap, no file loader available for that format.");
            throw new TerrainException(String.Format("unable to load heightmap from file {0}: no loader available for that format", filename));
        }

        private static Stream URIFetch(Uri uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

            // request.Credentials = credentials;

            request.ContentLength = 0;
            request.KeepAlive = false;

            WebResponse response = request.GetResponse();
            Stream file = response.GetResponseStream();

            if (response.ContentLength == 0)
                throw new Exception(String.Format("{0} returned an empty file", uri.ToString()));

            // return new BufferedStream(file, (int) response.ContentLength);
            return new BufferedStream(file, 1000000);
        }

        /// <summary>
        /// Modify Land
        /// </summary>
        /// <param name="pos">Land-position (X,Y,0)</param>
        /// <param name="size">The size of the brush (0=small, 1=medium, 2=large)</param>
        /// <param name="action">0=LAND_LEVEL, 1=LAND_RAISE, 2=LAND_LOWER, 3=LAND_SMOOTH, 4=LAND_NOISE, 5=LAND_REVERT</param>
        /// <param name="agentId">UUID of script-owner</param>
        public void ModifyTerrain(UUID user, Vector3 pos, byte size, byte action, UUID agentId)
        {
            float duration = 0.25f;
            if (action == 0)
                duration = 4.0f;
            client_OnModifyTerrain(user, (float)pos.Z, duration, size, action, pos.Y, pos.X, pos.Y, pos.X, agentId, size);
        }

        /// <summary>
        /// Saves the current heightmap to a specified stream.
        /// </summary>
        /// <param name="filename">The destination filename.  Used here only to identify the image type</param>
        /// <param name="stream"></param>
        public void SaveToStream(string filename, Stream stream)
        {
            try
            {
                foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
                {
                    if (filename.EndsWith(loader.Key))
                    {
                        loader.Value.SaveStream(stream, m_channel);
                        return;
                    }
                }
            }
            catch (NotImplementedException)
            {
                m_log.Error("Unable to save to " + filename + ", saving of this file format has not been implemented.");
                throw new TerrainException(String.Format("Unable to save heightmap: saving of this file format not implemented"));
            }
        }

        public void TaintTerrain ()
        {
            CheckForTerrainUpdates();
        }

        #region Plugin Loading Methods

        private void LoadPlugins()
        {
            m_plugineffects = new Dictionary<string, ITerrainEffect>();
            string plugineffectsPath = "Terrain";
            
            // Load the files in the Terrain/ dir
            if (!Directory.Exists(plugineffectsPath))
                return;
            
            string[] files = Directory.GetFiles(plugineffectsPath);
            foreach (string file in files)
            {
                //m_log.Info("Loading effects in " + file);
                try
                {
                    Assembly library = Assembly.LoadFrom(file);
                    foreach (Type pluginType in library.GetTypes())
                    {
                        try
                        {
                            if (pluginType.IsAbstract || pluginType.IsNotPublic)
                                continue;

                            string typeName = pluginType.Name;

                            if (pluginType.GetInterface("ITerrainEffect", false) != null)
                            {
                                ITerrainEffect terEffect = (ITerrainEffect) Activator.CreateInstance(library.GetType(pluginType.ToString()));

                                InstallPlugin(typeName, terEffect);
                            }
                            else if (pluginType.GetInterface("ITerrainLoader", false) != null)
                            {
                                ITerrainLoader terLoader = (ITerrainLoader) Activator.CreateInstance(library.GetType(pluginType.ToString()));
                                m_loaders[terLoader.FileExtension] = terLoader;
                                //m_log.Info("L ... " + typeName);
                            }
                        }
                        catch (AmbiguousMatchException)
                        {
                        }
                    }
                }
                catch (BadImageFormatException)
                {
                }
            }
        }

        public void InstallPlugin(string pluginName, ITerrainEffect effect)
        {
            lock (m_plugineffects)
            {
                if (!m_plugineffects.ContainsKey(pluginName))
                {
                    m_plugineffects.Add(pluginName, effect);
                    //m_log.Info("E ... " + pluginName);
                }
                else
                {
                    m_plugineffects[pluginName] = effect;
                    //m_log.Warn("E ... " + pluginName + " (Replaced)");
                }
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// Installs into terrain module the standard suite of brushes
        /// </summary>
        private void InstallDefaultEffects()
        {
            // Draggable Paint Brush Effects
            m_painteffects[StandardTerrainEffects.Raise] = new RaiseSphere();
            m_painteffects[StandardTerrainEffects.Lower] = new LowerSphere();
            m_painteffects[StandardTerrainEffects.Smooth] = new SmoothSphere();
            m_painteffects[StandardTerrainEffects.Noise] = new NoiseSphere();
            m_painteffects[StandardTerrainEffects.Flatten] = new FlattenSphere();
            m_painteffects[StandardTerrainEffects.Revert] = new RevertSphere(this);
            m_painteffects[StandardTerrainEffects.Erode] = new ErodeSphere();
            m_painteffects[StandardTerrainEffects.Weather] = new WeatherSphere();
            m_painteffects[StandardTerrainEffects.Olsen] = new OlsenSphere();

            // Area of effect selection effects
            m_floodeffects[StandardTerrainEffects.Raise] = new RaiseArea();
            m_floodeffects[StandardTerrainEffects.Lower] = new LowerArea();
            m_floodeffects[StandardTerrainEffects.Smooth] = new SmoothArea();
            m_floodeffects[StandardTerrainEffects.Noise] = new NoiseArea();
            m_floodeffects[StandardTerrainEffects.Flatten] = new FlattenArea();
            m_floodeffects[StandardTerrainEffects.Revert] = new RevertArea(this);

            // Filesystem load/save loaders
            m_loaders[".r32"] = new RAW32();
            m_loaders[".f32"] = m_loaders[".r32"];
            m_loaders[".ter"] = new Terragen();
            m_loaders[".raw"] = new LLRAW();
            m_loaders[".jpg"] = new JPEG();
            m_loaders[".jpeg"] = m_loaders[".jpg"];
            m_loaders[".bmp"] = new BMP();
            m_loaders[".png"] = new PNG();
            m_loaders[".gif"] = new GIF();
            m_loaders[".tif"] = new TIFF();
            m_loaders[".tiff"] = m_loaders[".tif"];
        }

        /// <summary>
        /// Saves the current state of the region into the revert map buffer.
        /// </summary>
        public void UpdateRevertWaterMap()
        {
            m_waterRevert = m_waterChannel.MakeCopy();
            SaveRevertTerrain(m_waterRevert);
        }

        /// <summary>
        /// Saves the current state of the region into the revert map buffer.
        /// </summary>
        public void UpdateRevertMap()
        {
            m_revert = null;
            m_revert = m_channel.MakeCopy();
            SaveRevertTerrain(m_revert);
        }

        /// <summary>
        /// Loads a tile from a larger terrain file and installs it into the region.
        /// </summary>
        /// <param name="filename">The terrain file to load</param>
        /// <param name="fileWidth">The width of the file in units</param>
        /// <param name="fileHeight">The height of the file in units</param>
        /// <param name="fileStartX">Where to begin our slice</param>
        /// <param name="fileStartY">Where to begin our slice</param>
        public void LoadFromFile(string filename, int fileWidth, int fileHeight, int fileStartX, int fileStartY)
        {
            int offsetX = (int) (m_scene.RegionInfo.RegionLocX / Constants.RegionSize) - fileStartX;
            int offsetY = (int) (m_scene.RegionInfo.RegionLocY / Constants.RegionSize) - fileStartY;

            if (offsetX >= 0 && offsetX < fileWidth && offsetY >= 0 && offsetY < fileHeight)
            {
                // this region is included in the tile request
                foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
                {
                    if (filename.EndsWith(loader.Key))
                    {
                        lock (m_scene)
                        {
                            ITerrainChannel channel = loader.Value.LoadFile(filename, offsetX, offsetY,
                                                                            fileWidth, fileHeight,
                                                                            m_scene.RegionInfo.RegionSizeX,
                                                                            m_scene.RegionInfo.RegionSizeY);
                            channel.Scene = m_scene;
                            m_channel = channel;
                            m_scene.RegisterModuleInterface<ITerrainChannel>(m_channel);
                            UpdateRevertMap();
                        }
                        return;
                    }
                }
            }
        }

        void client_onGodlikeMessage(IClientAPI client, UUID requester, string Method, List<string> Parameters)
        {
            if (!m_scene.Permissions.IsGod(client.AgentId))
                return;
            if (((Scene)client.Scene).RegionInfo.RegionID != m_scene.RegionInfo.RegionID)
                return;
            string parameter1 = Parameters[0];
            if (Method == "terrain")
            {
                if (parameter1 == "bake")
                {
                    UpdateRevertMap();
                }
                if (parameter1 == "revert")
                {
                    InterfaceRevertTerrain("", null);
                }
                if (parameter1 == "swap")
                {
                    //This is so you can change terrain with other regions... not implemented yet
                }
            }
        }

        /// <summary>
        /// Checks to see if the terrain has been modified since last check
        /// but won't attempt to limit those changes to the limits specified in the estate settings
        /// currently invoked by the command line operations in the region server only
        /// </summary>
        private void CheckForTerrainUpdates()
        {
            CheckForTerrainUpdates(false, false, false);
        }

        /// <summary>
        /// Checks to see if the terrain has been modified since last check.
        /// If it has been modified, every all the terrain patches are sent to the client.
        /// If the call is asked to respect the estate settings for terrain_raise_limit and
        /// terrain_lower_limit, it will clamp terrain updates between these values
        /// currently invoked by client_OnModifyTerrain only and not the Commander interfaces
        /// <param name="respectEstateSettings">should height map deltas be limited to the estate settings limits</param>
        /// <param name="forceSendOfTerrainInfo">force send terrain</param>
        /// <param name="isWater">Check water or terrain</param>
        /// </summary>
        private void CheckForTerrainUpdates(bool respectEstateSettings, bool forceSendOfTerrainInfo, bool isWater)
        {
            ITerrainChannel channel = isWater ? m_waterChannel : m_channel;
            bool shouldTaint = false;

            // if we should respect the estate settings then
            // fixup and height deltas that don't respect them
            if(respectEstateSettings)
                LimitChannelChanges();
            else if (!forceSendOfTerrainInfo)
                LimitMaxTerrain();

            List<int> xs = new List<int>();
            List<int> ys = new List<int>();
            for (int x = 0; x < channel.Width; x += Constants.TerrainPatchSize)
            {
                for (int y = 0; y < channel.Height; y += Constants.TerrainPatchSize)
                {
                    if (channel.Tainted(x, y) || forceSendOfTerrainInfo)
                    {
                        xs.Add(x / Constants.TerrainPatchSize);
                        ys.Add(y / Constants.TerrainPatchSize);
                        shouldTaint = true;
                    }
                }
            }
            if (shouldTaint || forceSendOfTerrainInfo)
                QueueTerrainUpdate();

            foreach (IScenePresence presence in m_scene.GetScenePresences ())
            {
                if (!m_sendTerrainUpdatesByViewDistance)
                {
                    presence.ControllingClient.SendLayerData(xs.ToArray(), ys.ToArray(), channel.GetSerialised(m_scene), TerrainPatch.LayerType.Land);
                }
                else
                {
                    for(int i = 0; i < xs.Count; i++)
                    {
                         m_terrainPatchesSent[presence.UUID][xs[i], ys[i]] = false;
                    }
                    SendTerrainUpdatesForClient(presence);
                }
            }
        }

        private bool LimitMaxTerrain()
        {
            bool changesLimited = false;

            // loop through the height map for this patch and compare it against
            // the revert map
            for (int x = 0; x < m_scene.RegionInfo.RegionSizeX / Constants.TerrainPatchSize; x++)
            {
                for (int y = 0; y < m_scene.RegionInfo.RegionSizeY / +Constants.TerrainPatchSize; y++)
                {
                    float requestedHeight = m_channel[x, y];

                    if (requestedHeight > MAX_HEIGHT)
                    {
                        m_channel[x, y] = MAX_HEIGHT;
                        changesLimited = true;
                    }
                    else if (requestedHeight < MIN_HEIGHT)
                    {
                        m_channel[x, y] = MIN_HEIGHT; //as lower is a -ve delta
                        changesLimited = true;
                    }
                }
            }

            return changesLimited;
        }

        /// <summary>
        /// Checks to see height deltas in the tainted terrain patch at xStart ,yStart
        /// are all within the current estate limits
        /// <returns>true if changes were limited, false otherwise</returns>
        /// </summary>
        private bool LimitChannelChanges()
        {
            bool changesLimited = false;
            float minDelta = (float)m_scene.RegionInfo.RegionSettings.TerrainLowerLimit;
            float maxDelta = (float)m_scene.RegionInfo.RegionSettings.TerrainRaiseLimit;

            // loop through the height map for this patch and compare it against
            // the revert map
            for (int x = 0; x < m_scene.RegionInfo.RegionSizeX / Constants.TerrainPatchSize; x++)
            {
                for (int y = 0; y < m_scene.RegionInfo.RegionSizeY / Constants.TerrainPatchSize; y++)
                {

                    float requestedHeight = m_channel[x, y];
                    float bakedHeight = m_revert[x, y];
                    float requestedDelta = requestedHeight - bakedHeight;

                    if (requestedDelta > maxDelta)
                    {
                        m_channel[x, y] = bakedHeight + maxDelta;
                        changesLimited = true;
                    }
                    else if (requestedDelta < minDelta)
                    {
                        m_channel[x, y] = bakedHeight + minDelta; //as lower is a -ve delta
                        changesLimited = true;
                    }
                }
            }

            return changesLimited;
        }

        private void client_OnLandUndo(IClientAPI client)
        {
            lock (m_undo)
            {
                if (m_undo.Count > 0)
                {
                    LandUndoState goback = m_undo.Pop();
                    if (goback != null)
                        goback.PlaybackState();
                }
            }
        }

        /// <summary>
        /// Sends a copy of the current terrain to the scenes clients
        /// </summary>
        /// <param name="serialised">A copy of the terrain as a 1D float array of size w*h</param>
        /// <param name="x">The patch corner to send</param>
        /// <param name="y">The patch corner to send</param>
        private void SendToClients(short[] serialised, int x, int y)
        {
            m_scene.ForEachClient(
                delegate(IClientAPI controller)
                {
                    controller.SendLayerData(x / Constants.TerrainPatchSize, y / Constants.TerrainPatchSize, serialised);
                }
            );
        }

        private void client_OnModifyTerrain(UUID user, float height, float seconds, byte size, byte action,
                                            float north, float west, float south, float east, UUID agentId, float BrushSize)
        {
            bool god = m_scene.Permissions.IsGod(user);
            bool isWater = ((action & 512) == 512); //512 means its modifying water
            ITerrainChannel channel = isWater ? m_waterChannel : m_channel;
            if (north == south && east == west)
            {
                if (m_painteffects.ContainsKey((StandardTerrainEffects) action))
                {
                    StoreUndoState();
                        m_painteffects[(StandardTerrainEffects) action].PaintEffect(
                            channel, user, west, south, height, size, seconds, BrushSize, m_scenes);
                        
                        //revert changes outside estate limits
                        CheckForTerrainUpdates(!god, false, false);
                }
                else
                {
                    m_log.Warn("Unknown terrain brush type " + action);
                }
            }
            else
            {
                if (m_floodeffects.ContainsKey((StandardTerrainEffects)action))
                {
                    StoreUndoState();
                    m_floodeffects[(StandardTerrainEffects)action].FloodEffect(
                        channel, user, north, west, south, east, size);

                    //revert changes outside estate limits
                    CheckForTerrainUpdates(!god, false, false);
                }
                else
                {
                    m_log.Warn("Unknown terrain flood type " + action);
                }
            }
        }

        private void client_OnBakeTerrain(IClientAPI remoteClient)
        {
            // Not a good permissions check (see client_OnModifyTerrain above), need to check the entire area.
            // for now check a point in the centre of the region

            if (m_scene.Permissions.CanIssueEstateCommand(remoteClient.AgentId, true))
            {
                InterfaceBakeTerrain("", null); //bake terrain does not use the passed in parameter
            }
        }
        
        protected void client_OnUnackedTerrain(IClientAPI client, int patchX, int patchY)
        {
            //m_log.Debug("Terrain packet unacked, resending patch: " + patchX + " , " + patchY);
            client.SendLayerData(patchX, patchY, m_channel.GetSerialised(m_scene));
        }

        private void StoreUndoState()
        {
            lock (m_undo)
            {
                if (m_undo.Count > 0)
                {
                    LandUndoState last = m_undo.Peek();
                    if (last != null)
                    {
                        if (last.Compare(m_channel))
                            return;
                    }
                }

                LandUndoState nUndo = new LandUndoState(this, m_channel);
                m_undo.Push(nUndo);
            }
        }

        #region Console Commands

        private List<TerrainModule> FindModuleForScene(IScene scene)
        {
            List<TerrainModule> modules = new List<TerrainModule>();
            if (scene == null)
            {
                string line = MainConsole.Instance.CmdPrompt("Are you sure that you want to do this command on all scenes?", "yes");
                if (!line.Equals("yes", StringComparison.CurrentCultureIgnoreCase))
                    return modules;
                //Return them all
                return m_terrainModules;
            }
            foreach (TerrainModule module in m_terrainModules)
            {
                if (module.m_scene == scene)
                {
                    modules.Add(module);
                }
            }
            return modules;
        }

        private void InterfaceLoadFile(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);
            int offsetX = 0;
            int offsetY = 0;

            int i = 0;
            foreach (string param in cmd)
            {
                if (param.StartsWith("OffsetX"))
                {
                    string retVal = param.Remove(0, 8);
                    int.TryParse(retVal, out offsetX);
                }
                else if (param.StartsWith("OffsetY"))
                {
                    string retVal = param.Remove(0, 8);
                    int.TryParse(retVal, out offsetY);
                }
                i++;
            }

            foreach (TerrainModule tmodule in m)
            {
                tmodule.LoadFromFile(cmd[2], offsetX, offsetY);
                tmodule.CheckForTerrainUpdates();
            }
        }

        private void InterfaceLoadTileFile(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                tmodule.LoadFromFile((string)cmd[2],
                         int.Parse(cmd[3]),
                         int.Parse(cmd[4]),
                         int.Parse(cmd[5]),
                         int.Parse(cmd[6]));
                tmodule.CheckForTerrainUpdates();
            }
        }

        private void InterfaceSaveFile(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                tmodule.SaveToFile((string)cmd[2]);
            }
        }

        private void InterfaceSavePhysics(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                tmodule.m_scene.PhysicsScene.SetTerrain(tmodule.m_channel.GetSerialised(tmodule.m_scene));
            }
        }

        private void InterfaceBakeTerrain(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                tmodule.UpdateRevertMap();
            }
        }

        private void InterfaceRevertTerrain(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                int x, y;
                for (x = 0; x < m_channel.Width; x++)
                    for (y = 0; y < m_channel.Height; y++)
                        tmodule.m_channel[x, y] = m_revert[x, y];

                tmodule.CheckForTerrainUpdates();
            }
        }

        private void InterfaceFlipTerrain(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                String direction = cmd[2];

                if (direction.ToLower().StartsWith("y"))
                {
                    for (int x = 0; x < m_scene.RegionInfo.RegionSizeX; x++)
                    {
                        for (int y = 0; y < m_scene.RegionInfo.RegionSizeY / 2; y++)
                        {
                            float height = tmodule.m_channel[x, y];
                            float flippedHeight = tmodule.m_channel[x, m_scene.RegionInfo.RegionSizeY - 1 - y];
                            tmodule.m_channel[x, y] = flippedHeight;
                            tmodule.m_channel[x, m_scene.RegionInfo.RegionSizeY - 1 - y] = height;

                        }
                    }
                }
                else if (direction.ToLower().StartsWith("x"))
                {
                    for (int y = 0; y < m_scene.RegionInfo.RegionSizeY; y++)
                    {
                        for (int x = 0; x < m_scene.RegionInfo.RegionSizeX / 2; x++)
                        {
                            float height = tmodule.m_channel[x, y];
                            float flippedHeight = tmodule.m_channel[m_scene.RegionInfo.RegionSizeX - 1 - x, y];
                            tmodule.m_channel[x, y] = flippedHeight;
                            tmodule.m_channel[m_scene.RegionInfo.RegionSizeX - 1 - x, y] = height;

                        }
                    }
                }
                else
                {
                    m_log.Error("Unrecognised direction - need x or y");
                }


                tmodule.CheckForTerrainUpdates();
            }
        }

        private void InterfaceRescaleTerrain(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                float desiredMin = float.Parse(cmd[2]);
                float desiredMax = float.Parse(cmd[3]);

                // determine desired scaling factor
                float desiredRange = desiredMax - desiredMin;
                //m_log.InfoFormat("Desired {0}, {1} = {2}", new Object[] { desiredMin, desiredMax, desiredRange });

                if (desiredRange == 0d)
                {
                    // delta is zero so flatten at requested height
                    tmodule.InterfaceFillTerrain("", cmd);
                }
                else
                {
                    //work out current heightmap range
                    float currMin = float.MaxValue;
                    float currMax = float.MinValue;

                    int width = tmodule.m_channel.Width;
                    int height = tmodule.m_channel.Height;

                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float currHeight = tmodule.m_channel[x, y];
                            if (currHeight < currMin)
                            {
                                currMin = currHeight;
                            }
                            else if (currHeight > currMax)
                            {
                                currMax = currHeight;
                            }
                        }
                    }

                    float currRange = currMax - currMin;
                    float scale = desiredRange / currRange;

                    //m_log.InfoFormat("Current {0}, {1} = {2}", new Object[] { currMin, currMax, currRange });
                    //m_log.InfoFormat("Scale = {0}", scale);

                    // scale the heightmap accordingly
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float currHeight = tmodule.m_channel[x, y] - currMin;
                            tmodule.m_channel[x, y] = desiredMin + (currHeight * scale);
                        }
                    }

                    tmodule.CheckForTerrainUpdates();
                }
            }
        }

        private void InterfaceElevateTerrain(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                int x, y;
                for (x = 0; x < tmodule.m_channel.Width; x++)
                    for (y = 0; y < tmodule.m_channel.Height; y++)
                        tmodule.m_channel[x, y] += float.Parse(cmd[2]);
                tmodule.CheckForTerrainUpdates();
            }
        }

        private void InterfaceMultiplyTerrain(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                int x, y;
                for (x = 0; x < tmodule.m_channel.Width; x++)
                    for (y = 0; y < tmodule.m_channel.Height; y++)
                        tmodule.m_channel[x, y] *= float.Parse(cmd[2]);
                tmodule.CheckForTerrainUpdates();
            }
        }

        private void InterfaceLowerTerrain(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                int x, y;
                for (x = 0; x < tmodule.m_channel.Width; x++)
                    for (y = 0; y < tmodule.m_channel.Height; y++)
                        tmodule.m_channel[x, y] -= float.Parse(cmd[2]);
                tmodule.CheckForTerrainUpdates();
            }
        }

        private void InterfaceFillTerrain(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                int x, y;

                for (x = 0; x < tmodule.m_channel.Width; x++)
                    for (y = 0; y < tmodule.m_channel.Height; y++)
                        tmodule.m_channel[x, y] = float.Parse(cmd[2]);
                tmodule.CheckForTerrainUpdates();
            }
        }

        private void InterfaceShowDebugStats(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                float max = float.MinValue;
                float min = float.MaxValue;
                float sum = 0;

                int x;
                for (x = 0; x < tmodule.m_channel.Width; x++)
                {
                    int y;
                    for (y = 0; y < tmodule.m_channel.Height; y++)
                    {
                        sum += tmodule.m_channel[x, y];
                        if (max < tmodule.m_channel[x, y])
                            max = tmodule.m_channel[x, y];
                        if (min > tmodule.m_channel[x, y])
                            min = tmodule.m_channel[x, y];
                    }
                }

                double avg = sum / (tmodule.m_channel.Height * tmodule.m_channel.Width);

                m_log.Info("Channel " + tmodule.m_channel.Width + "x" + tmodule.m_channel.Height);
                m_log.Info("max/min/avg/sum: " + max + "/" + min + "/" + avg + "/" + sum);
            }
        }

        private void InterfaceEnableExperimentalBrushes(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                if (bool.Parse(cmd[2]))
                {
                    tmodule.m_painteffects[StandardTerrainEffects.Revert] = new WeatherSphere();
                    tmodule.m_painteffects[StandardTerrainEffects.Flatten] = new OlsenSphere();
                    tmodule.m_painteffects[StandardTerrainEffects.Smooth] = new ErodeSphere();
                }
                else
                {
                    tmodule.InstallDefaultEffects();
                }
            }
        }

        private void InterfaceRunPluginEffect(string module, string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                if (cmd[2] == "list")
                {
                    m_log.Info("List of loaded plugins");
                    foreach (KeyValuePair<string, ITerrainEffect> kvp in tmodule.m_plugineffects)
                    {
                        m_log.Info(kvp.Key);
                    }
                    return;
                }
                if (cmd[2] == "reload")
                {
                    tmodule.LoadPlugins();
                    return;
                }
                if (tmodule.m_plugineffects.ContainsKey(cmd[2]))
                {
                    tmodule.m_plugineffects[cmd[2]].RunEffect(tmodule.m_channel);
                    tmodule.CheckForTerrainUpdates();
                }
                else
                {
                    m_log.Warn("No such plugin effect loaded.");
                }
            }
        }

        private void InterfaceHelp(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            string supportedFileExtensions = "";
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
                supportedFileExtensions += " " + loader.Key + " (" + loader.Value + ")";

            m_log.Info("terrain load <FileName> - Loads a terrain from a specified file. FileName: The file you wish to load from, the file extension determines the loader to be used. Supported extensions include: " +
                                            supportedFileExtensions);
            m_log.Info("terrain save <FileName> - Saves the current heightmap to a specified file. FileName: The destination filename for your heightmap, the file extension determines the format to save in. Supported extensions include: " +
                                          supportedFileExtensions);
            m_log.Info("terrain load-tile <file width> <file height> <minimum X tile> <minimum Y tile> - Loads a terrain from a section of a larger file. " +
                    "\n file width: The width of the file in tiles" +
                    "\n file height: The height of the file in tiles" +
                    "\n minimum X tile: The X region coordinate of the first section on the file" +
                    "\n minimum Y tile: The Y region coordinate of the first section on the file");
            m_log.Info("terrain fill <value> - Fills the current heightmap with a specified value." +
                                            "\n value: The numeric value of the height you wish to set your region to.");
            m_log.Info("terrain elevate <value> - Raises the current heightmap by the specified amount." +
                                            "\n amount: The amount of height to remove from the terrain in meters.");
            m_log.Info("terrain lower <value> - Lowers the current heightmap by the specified amount." +
                                            "\n amount: The amount of height to remove from the terrain in meters.");
            m_log.Info("terrain multiply <value> - Multiplies the heightmap by the value specified." +
                                            "\n value: The value to multiply the heightmap by.");
            m_log.Info("terrain bake - Saves the current terrain into the regions revert map.");
            m_log.Info("terrain revert - Loads the revert map terrain into the regions heightmap.");
            m_log.Info("terrain stats - Shows some information about the regions heightmap for debugging purposes.");
            m_log.Info("terrain newbrushes <enabled> - Enables experimental brushes which replace the standard terrain brushes." +
                                            "\n enabled: true / false - Enable new brushes");
            m_log.Info("terrain effect <name> - Runs a specified plugin effect" +
                                            "\n name: The plugin effect you wish to run, or 'list' to see all plugins");
            m_log.Info("terrain flip <direction> - Flips the current terrain about the X or Y axis" +
                                            "\n direction: [x|y] the direction to flip the terrain in");
            m_log.Info("terrain rescale <min> <max> - Rescales the current terrain to fit between the given min and max heights" +
                                            "\n Min: min terrain height after rescaling" +
                                            "\n Max: max terrain height after rescaling");
        }

        private void AddConsoleCommands()
        {
            // Load / Save
            string supportedFileExtensions = "";
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
                supportedFileExtensions += " " + loader.Key + " (" + loader.Value + ")";

            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain save",
                "terrain save <FileName>", "Saves the current heightmap to a specified file. FileName: The destination filename for your heightmap, the file extension determines the format to save in. Supported extensions include: " +
                                          supportedFileExtensions, InterfaceSaveFile);

            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain physics update",
                "terrain physics update", "Update the physics map", InterfaceSavePhysics);

            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain load",
                "terrain load <FileName>", "Loads a terrain from a specified file. FileName: The file you wish to load from, the file extension determines the loader to be used. Supported extensions include: " +
                                            supportedFileExtensions, InterfaceLoadFile);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain load-tile",
                "terrain load-tile <file width> <file height> <minimum X tile> <minimum Y tile>",
                "Loads a terrain from a section of a larger file. " + 
            "\n file width: The width of the file in tiles" + 
            "\n file height: The height of the file in tiles" + 
            "\n minimum X tile: The X region coordinate of the first section on the file" + 
            "\n minimum Y tile: The Y region coordinate of the first section on the file", InterfaceLoadTileFile);

            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain fill",
                "terrain fill <value> ", "Fills the current heightmap with a specified value." +
                                            "\n value: The numeric value of the height you wish to set your region to.", InterfaceFillTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain elevate",
                "terrain elevate <amount> ", "Raises the current heightmap by the specified amount." +
                                            "\n amount: The amount of height to remove from the terrain in meters.", InterfaceElevateTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain lower",
                "terrain lower <amount> ", "Lowers the current heightmap by the specified amount." +
                                            "\n amount: The amount of height to remove from the terrain in meters.", InterfaceLowerTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain multiply",
                "terrain multiply <value> ", "Multiplies the heightmap by the value specified." +
                                            "\n value: The value to multiply the heightmap by.", InterfaceMultiplyTerrain);

            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain bake",
                "terrain bake", "Saves the current terrain into the regions revert map.", InterfaceBakeTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain revert",
                "terrain revert", "Loads the revert map terrain into the regions heightmap.", InterfaceRevertTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain stats",
                "terrain stats", "Shows some information about the regions heightmap for debugging purposes.", InterfaceShowDebugStats);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain newbrushes",
                "terrain newbrushes <enabled> ", "Enables experimental brushes which replace the standard terrain brushes." +
                                            "\n enabled: true / false - Enable new brushes", InterfaceEnableExperimentalBrushes);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain effect",
                "terrain effect <name> ", "Runs a specified plugin effect" +
                                            "\n name: The plugin effect you wish to run, or 'list' to see all plugins", InterfaceRunPluginEffect);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain flip",
                "terrain flip <direction> ", "Flips the current terrain about the X or Y axis" +
                                            "\n direction: [x|y] the direction to flip the terrain in", InterfaceFlipTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain rescale",
                "terrain rescale <min> <max>", "Rescales the current terrain to fit between the given min and max heights" +
                                            "\n Min: min terrain height after rescaling" +
                                            "\n Max: max terrain height after rescaling", InterfaceRescaleTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", true, "terrain help",
                "terrain help", "Gives help about the terrain module.", InterfaceHelp);
        }


        #endregion

    }
}
