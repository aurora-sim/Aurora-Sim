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
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Timers;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using Aurora.Modules.Terrain.FileLoaders;
using Aurora.Modules.Terrain.FloodBrushes;
using Aurora.Modules.Terrain.PaintBrushes;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.Modules.Terrain
{
    public class TerrainModule : INonSharedRegionModule, ITerrainModule
    {
        #region StandardTerrainEffects enum

        /// <summary>
        ///   A standard set of terrain brushes and effects recognised by viewers
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
            Olsen = 253,
            Paint = 252
        }

        #endregion

        private const int MAX_HEIGHT = 250;
        private const int MIN_HEIGHT = 0;

        private static readonly List<IScene> m_scenes = new List<IScene>();
        private static readonly List<TerrainModule> m_terrainModules = new List<TerrainModule>();

        private readonly Dictionary<StandardTerrainEffects, ITerrainFloodEffect> m_floodeffects =
            new Dictionary<StandardTerrainEffects, ITerrainFloodEffect>();

        private readonly Dictionary<string, ITerrainLoader> m_loaders = new Dictionary<string, ITerrainLoader>();

        private readonly Dictionary<StandardTerrainEffects, ITerrainPaintableEffect> m_painteffects =
            new Dictionary<StandardTerrainEffects, ITerrainPaintableEffect>();

        private readonly Timer m_queueTimer = new Timer();
        private readonly UndoStack<LandUndoState> m_undo = new UndoStack<LandUndoState>(5);

        private ITerrainChannel m_channel;
        protected bool m_noTerrain;
        private Vector3 m_previousCheckedPosition = Vector3.Zero;
        private long m_queueNextSave;
        private ITerrainChannel m_revert;
        private int m_savetime = 2; // seconds to wait before saving terrain
        private IScene m_scene;
        private bool m_sendTerrainUpdatesByViewDistance;
        protected Dictionary<UUID, bool[,]> m_terrainPatchesSent = new Dictionary<UUID, bool[,]>();
        protected bool m_use3DWater;
        private ITerrainChannel m_waterChannel;
        private ITerrainChannel m_waterRevert;

        #region INonSharedRegionModule Members

        /// <summary>
        ///   Creates and initialises a terrain module for a region
        /// </summary>
        /// <param name = "scene">Region initialising</param>
        /// <param name = "config">Config for the region</param>
        public void Initialise(IConfigSource config)
        {
            if (config.Configs["TerrainModule"] != null)
            {
                m_sendTerrainUpdatesByViewDistance =
                    config.Configs["TerrainModule"].GetBoolean("SendTerrainByViewDistance",
                                                               m_sendTerrainUpdatesByViewDistance);
                m_use3DWater = config.Configs["TerrainModule"].GetBoolean("Use3DWater", m_use3DWater);
                m_noTerrain = config.Configs["TerrainModule"].GetBoolean("NoTerrain", m_noTerrain);
            }
        }

        public void AddRegion(IScene scene)
        {
            bool firstScene = m_scenes.Count == 0;
            m_scene = scene;
            m_scenes.Add(scene);
            m_terrainModules.Add(this);

            m_scene.RegisterModuleInterface<ITerrainModule>(this);

            if (firstScene)
                AddConsoleCommands();

            InstallDefaultEffects();
            LoadPlugins();

            if (!m_noTerrain)
            {
                LoadWorldHeightmap();
                LoadWorldWaterMap();
                scene.PhysicsScene.SetTerrain(m_channel, m_channel.GetSerialised(scene));
                UpdateWaterHeight(scene.RegionInfo.RegionSettings.WaterHeight);
            }

            m_scene.EventManager.OnNewClient += EventManager_OnNewClient;
            m_scene.EventManager.OnClosingClient += OnClosingClient;
            m_scene.EventManager.OnSignificantClientMovement += EventManager_OnSignificantClientMovement;
            m_scene.AuroraEventManager.RegisterEventHandler("DrawDistanceChanged", AuroraEventManager_OnGenericEvent);
            m_scene.AuroraEventManager.RegisterEventHandler("SignficantCameraMovement",
                                                            AuroraEventManager_OnGenericEvent);
            m_scene.EventManager.OnNewPresence += OnNewPresence;

            m_queueTimer.Enabled = false;
            m_queueTimer.AutoReset = true;
            m_queueTimer.Interval = m_savetime*1000;
            m_queueTimer.Elapsed += TerrainUpdateTimer;
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public void RemoveRegion(IScene scene)
        {
            m_scenes.Remove(scene);
            lock (m_scene)
            {
                // remove the event-handlers
                m_scene.EventManager.OnNewClient -= EventManager_OnNewClient;
                m_scene.EventManager.OnClosingClient -= OnClosingClient;
                m_scene.EventManager.OnSignificantClientMovement -= EventManager_OnSignificantClientMovement;
                m_scene.AuroraEventManager.UnregisterEventHandler("DrawDistanceChanged",
                                                                  AuroraEventManager_OnGenericEvent);
                m_scene.AuroraEventManager.UnregisterEventHandler("SignficantCameraMovement",
                                                                  AuroraEventManager_OnGenericEvent);
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

        public ITerrainChannel TerrainMap
        {
            get { return m_channel; }
            set { m_channel = value; }
        }

        public ITerrainChannel TerrainRevertMap
        {
            get { return m_revert; }
            set { m_revert = value; }
        }

        public ITerrainChannel TerrainWaterMap
        {
            get { return m_waterChannel; }
            set { m_waterChannel = value; }
        }

        public ITerrainChannel TerrainWaterRevertMap
        {
            get { return m_waterRevert; }
            set { m_waterRevert = value; }
        }

        public void UpdateWaterHeight(double height)
        {
            short[] waterMap = null;
            if (m_waterChannel != null)
                waterMap = m_waterChannel.GetSerialised(m_scene);
            m_scene.PhysicsScene.SetWaterLevel(height, waterMap);
        }

        /// <summary>
        ///   Reset the terrain of this region to the default
        /// </summary>
        public void ResetTerrain()
        {
            if (!m_noTerrain)
            {
                TerrainChannel channel = new TerrainChannel(m_scene);
                m_channel = channel;
                m_scene.SimulationDataService.Tainted();
                m_scene.RegisterModuleInterface(m_channel);
                CheckForTerrainUpdates(false, true, false);
            }
        }

        /// <summary>
        ///   Loads the World Revert heightmap
        /// </summary>
        public void LoadRevertMap()
        {
            try
            {
                short[] map = m_scene.SimulationDataService.LoadTerrain(m_scene, true, m_scene.RegionInfo.RegionSizeX,
                                                                        m_scene.RegionInfo.RegionSizeY);
                if (map == null)
                {
                    if (m_revert == null)
                    {
                        m_revert = m_channel.MakeCopy();

                        m_scene.SimulationDataService.Tainted();
                    }
                }
                else
                {
                    m_revert = new TerrainChannel(map, m_scene);
                }
            }
            catch (IOException e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e + " Regenerating");
                // Non standard region size.    If there's an old terrain in the database, it might read past the buffer
                if (m_scene.RegionInfo.RegionSizeX != Constants.RegionSize ||
                    m_scene.RegionInfo.RegionSizeY != Constants.RegionSize)
                {
                    m_revert = m_channel.MakeCopy();

                    m_scene.SimulationDataService.Tainted();
                }
            }
            catch (IndexOutOfRangeException e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e + " Regenerating");
                m_revert = m_channel.MakeCopy();

                m_scene.SimulationDataService.Tainted();
            }
            catch (ArgumentOutOfRangeException e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e + " Regenerating");
                m_revert = m_channel.MakeCopy();

                m_scene.SimulationDataService.Tainted();
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: Scene.cs: LoadWorldMap() - Failed with exception " + e);
            }
        }

        /// <summary>
        ///   Loads the World heightmap
        /// </summary>
        public void LoadWorldHeightmap()
        {
            try
            {
                short[] map = m_scene.SimulationDataService.LoadTerrain(m_scene, false, m_scene.RegionInfo.RegionSizeX,
                                                                        m_scene.RegionInfo.RegionSizeY);
                if (map == null)
                {
                    if (m_channel == null)
                    {
                        MainConsole.Instance.Info("[TERRAIN]: No default terrain. Generating a new terrain.");
                        m_channel = new TerrainChannel(m_scene);

                        m_scene.SimulationDataService.Tainted();
                    }
                }
                else
                {
                    m_channel = new TerrainChannel(map, m_scene);
                }
            }
            catch (IOException e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e + " Regenerating");
                // Non standard region size.    If there's an old terrain in the database, it might read past the buffer
                if (m_scene.RegionInfo.RegionSizeX != Constants.RegionSize ||
                    m_scene.RegionInfo.RegionSizeY != Constants.RegionSize)
                {
                    m_channel = new TerrainChannel(m_scene);

                    m_scene.SimulationDataService.Tainted();
                }
            }
            catch (IndexOutOfRangeException e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e + " Regenerating");
                m_channel = new TerrainChannel(m_scene);

                m_scene.SimulationDataService.Tainted();
            }
            catch (ArgumentOutOfRangeException e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e + " Regenerating");
                m_channel = new TerrainChannel(m_scene);

                m_scene.SimulationDataService.Tainted();
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: Scene.cs: LoadWorldMap() - Failed with exception " + e);
                m_channel = new TerrainChannel(m_scene);

                m_scene.SimulationDataService.Tainted();
            }
            LoadRevertMap();
            m_scene.RegisterModuleInterface(m_channel);
        }

        public void UndoTerrain(ITerrainChannel channel)
        {
            m_channel = channel;
        }

        /// <summary>
        ///   Loads a terrain file from disk and installs it in the scene.
        /// </summary>
        /// <param name = "filename">Filename to terrain file. Type is determined by extension.</param>
        public void LoadFromFile(string filename, int offsetX, int offsetY)
        {
#if (!ISWIN)
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
            {
                if (filename.EndsWith(loader.Key))
                {
                    lock (m_scene)
                    {
                        try
                        {
                            ITerrainChannel channel = loader.Value.LoadFile(filename, m_scene);
                            channel.Scene = m_scene;
                            if (m_channel.Height == channel.Height && m_channel.Width == channel.Width)
                            {
                                m_channel = channel;
                                m_scene.RegisterModuleInterface(m_channel);
                                MainConsole.Instance.DebugFormat("[TERRAIN]: Loaded terrain, wd/ht: {0}/{1}", channel.Width, channel.Height);
                            }
                            else
                            {
                                //Make sure it is in bounds
                                if ((offsetX + channel.Width) > m_channel.Width || (offsetY + channel.Height) > m_channel.Height)
                                {
                                    MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, the terrain you have given is larger than the current region.");
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
                                    MainConsole.Instance.DebugFormat("[TERRAIN]: Loaded terrain, wd/ht: {0}/{1}", channel.Width, channel.Height);
                                }
                            }
                            UpdateRevertMap();
                        }
                        catch (NotImplementedException)
                        {
                            MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, the " + loader.Value + " parser does not support file loading. (May be save only)");
                            throw new TerrainException(String.Format("unable to load heightmap: parser {0} does not support loading", loader.Value));
                        }
                        catch (FileNotFoundException)
                        {
                            MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, file not found. (A directory permissions error may also cause this)");
                            throw new TerrainException(String.Format("unable to load heightmap: file {0} not found (or permissions do not allow access", filename));
                        }
                        catch (ArgumentException e)
                        {
                            MainConsole.Instance.ErrorFormat("[TERRAIN]: Unable to load heightmap: {0}", e.Message);
                            throw new TerrainException(String.Format("Unable to load heightmap: {0}", e.Message));
                        }
                    }
                    CheckForTerrainUpdates();
                    MainConsole.Instance.Info("[TERRAIN]: File (" + filename + ") loaded successfully");
                    return;
                }
            }
#else
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders.Where(loader => filename.EndsWith(loader.Key)))
            {
                lock (m_scene)
                {
                    try
                    {
                        ITerrainChannel channel = loader.Value.LoadFile(filename, m_scene);
                        channel.Scene = m_scene;
                        if (m_channel.Height == channel.Height &&
                            m_channel.Width == channel.Width)
                        {
                            m_channel = channel;
                            m_scene.RegisterModuleInterface(m_channel);
                            MainConsole.Instance.DebugFormat("[TERRAIN]: Loaded terrain, wd/ht: {0}/{1}", channel.Width,
                                              channel.Height);
                        }
                        else
                        {
                            //Make sure it is in bounds
                            if ((offsetX + channel.Width) > m_channel.Width ||
                                (offsetY + channel.Height) > m_channel.Height)
                            {
                                MainConsole.Instance.Error(
                                    "[TERRAIN]: Unable to load heightmap, the terrain you have given is larger than the current region.");
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
                                MainConsole.Instance.DebugFormat("[TERRAIN]: Loaded terrain, wd/ht: {0}/{1}", channel.Width,
                                                  channel.Height);
                            }
                        }
                        UpdateRevertMap();
                    }
                    catch (NotImplementedException)
                    {
                        MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, the " + loader.Value +
                                    " parser does not support file loading. (May be save only)");
                        throw new TerrainException(
                            String.Format("unable to load heightmap: parser {0} does not support loading",
                                          loader.Value));
                    }
                    catch (FileNotFoundException)
                    {
                        MainConsole.Instance.Error(
                            "[TERRAIN]: Unable to load heightmap, file not found. (A directory permissions error may also cause this)");
                        throw new TerrainException(
                            String.Format(
                                "unable to load heightmap: file {0} not found (or permissions do not allow access",
                                filename));
                    }
                    catch (ArgumentException e)
                    {
                        MainConsole.Instance.ErrorFormat("[TERRAIN]: Unable to load heightmap: {0}", e.Message);
                        throw new TerrainException(
                            String.Format("Unable to load heightmap: {0}", e.Message));
                    }
                }
                CheckForTerrainUpdates();
                MainConsole.Instance.Info("[TERRAIN]: File (" + filename + ") loaded successfully");
                return;
            }
#endif

            MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, no file loader available for that format.");
            throw new TerrainException(
                String.Format("unable to load heightmap from file {0}: no loader available for that format", filename));
        }

        /// <summary>
        ///   Saves the current heightmap to a specified file.
        /// </summary>
        /// <param name = "filename">The destination filename</param>
        public void SaveToFile(string filename)
        {
            try
            {
#if (!ISWIN)
                foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
                {
                    if (filename.EndsWith(loader.Key))
                    {
                        loader.Value.SaveFile(filename, m_channel);
                        return;
                    }
                }
#else
                foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders.Where(loader => filename.EndsWith(loader.Key)))
                {
                    loader.Value.SaveFile(filename, m_channel);
                    return;
                }
#endif
            }
            catch (NotImplementedException)
            {
                MainConsole.Instance.Error("Unable to save to " + filename + ", saving of this file format has not been implemented.");
                throw new TerrainException(
                    String.Format("Unable to save heightmap: saving of this file format not implemented"));
            }
            catch (IOException ioe)
            {
                MainConsole.Instance.Error(String.Format("[TERRAIN]: Unable to save to {0}, {1}", filename, ioe.Message));
                throw new TerrainException(String.Format("Unable to save heightmap: {0}", ioe.Message));
            }
        }

        /// <summary>
        ///   Loads a terrain file from the specified URI
        /// </summary>
        /// <param name = "filename">The name of the terrain to load</param>
        /// <param name = "pathToTerrainHeightmap">The URI to the terrain height map</param>
        public void LoadFromStream(string filename, Uri pathToTerrainHeightmap)
        {
            LoadFromStream(filename, URIFetch(pathToTerrainHeightmap));
        }

        /// <summary>
        ///   Loads a terrain file from a stream and installs it in the scene.
        /// </summary>
        /// <param name = "filename">Filename to terrain file. Type is determined by extension.</param>
        /// <param name = "stream"></param>
        public void LoadFromStream(string filename, Stream stream)
        {
#if (!ISWIN)
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
            {
                if (filename.EndsWith(loader.Key))
                {
                    lock (m_scene)
                    {
                        try
                        {
                            ITerrainChannel channel = loader.Value.LoadStream(stream, m_scene);
                            if (channel != null)
                            {
                                channel.Scene = m_scene;
                                if (m_channel.Height == channel.Height && m_channel.Width == channel.Width)
                                {
                                    m_channel = channel;
                                    m_scene.RegisterModuleInterface(m_channel);
                                }
                                else
                                {
                                    //Make sure it is in bounds
                                    if ((channel.Width) > m_channel.Width || (channel.Height) > m_channel.Height)
                                    {
                                        for (int x = 0; x < m_channel.Width; x++)
                                        {
                                            for (int y = 0; y < m_channel.Height; y++)
                                            {
                                                m_channel[x, y] = channel[x, y];
                                            }
                                        }
                                        //MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, the terrain you have given is larger than the current region.");
                                        //return;
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
                                        MainConsole.Instance.DebugFormat("[TERRAIN]: Loaded terrain, wd/ht: {0}/{1}", channel.Width, channel.Height);
                                    }
                                }
                                UpdateRevertMap();
                            }
                        }
                        catch (NotImplementedException)
                        {
                            MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, the " + loader.Value + " parser does not support file loading. (May be save only)");
                            throw new TerrainException(String.Format("unable to load heightmap: parser {0} does not support loading", loader.Value));
                        }
                    }

                    CheckForTerrainUpdates();
                    MainConsole.Instance.Info("[TERRAIN]: File (" + filename + ") loaded successfully");
                    return;
                }
            }
#else
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders.Where(loader => filename.EndsWith(loader.Key)))
            {
                lock (m_scene)
                {
                    try
                    {
                        ITerrainChannel channel = loader.Value.LoadStream(stream, m_scene);
                        if (channel != null)
                        {
                            channel.Scene = m_scene;
                            if (m_channel.Height == channel.Height &&
                                m_channel.Width == channel.Width)
                            {
                                m_channel = channel;
                                m_scene.RegisterModuleInterface(m_channel);
                            }
                            else
                            {
                                //Make sure it is in bounds
                                if ((channel.Width) > m_channel.Width ||
                                    (channel.Height) > m_channel.Height)
                                {
                                    for (int x = 0; x < m_channel.Width; x++)
                                    {
                                        for (int y = 0; y < m_channel.Height; y++)
                                        {
                                            m_channel[x, y] = channel[x, y];
                                        }
                                    }
                                    //MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, the terrain you have given is larger than the current region.");
                                    //return;
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
                                    MainConsole.Instance.DebugFormat("[TERRAIN]: Loaded terrain, wd/ht: {0}/{1}", channel.Width,
                                                      channel.Height);
                                }
                            }
                            UpdateRevertMap();
                        }
                    }
                    catch (NotImplementedException)
                    {
                        MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, the " + loader.Value +
                                    " parser does not support file loading. (May be save only)");
                        throw new TerrainException(
                            String.Format("unable to load heightmap: parser {0} does not support loading",
                                          loader.Value));
                    }
                }

                CheckForTerrainUpdates();
                MainConsole.Instance.Info("[TERRAIN]: File (" + filename + ") loaded successfully");
                return;
            }
#endif
            MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, no file loader available for that format.");
            throw new TerrainException(
                String.Format("unable to load heightmap from file {0}: no loader available for that format", filename));
        }

        /// <summary>
        ///   Loads a terrain file from a stream and installs it in the scene.
        /// </summary>
        /// <param name = "filename">Filename to terrain file. Type is determined by extension.</param>
        /// <param name = "stream"></param>
        public void LoadFromStream(string filename, Stream stream, int offsetX, int offsetY)
        {
            m_channel = InternalLoadFromStream(filename, stream, offsetX, offsetY, m_channel);
            if (m_channel != null)
            {
                CheckForTerrainUpdates();
                m_scene.RegisterModuleInterface(m_channel);
            }
        }

        /// <summary>
        ///   Loads a terrain file from a stream and installs it in the scene.
        /// </summary>
        /// <param name = "filename">Filename to terrain file. Type is determined by extension.</param>
        /// <param name = "stream"></param>
        public void LoadWaterFromStream(string filename, Stream stream, int offsetX, int offsetY)
        {
            m_waterChannel = InternalLoadFromStream(filename, stream, offsetX, offsetY, m_waterChannel);
            if (m_waterChannel != null)
            {
                CheckForTerrainUpdates(false, false, true);
            }
        }

        /// <summary>
        ///   Loads a terrain file from a stream and installs it in the scene.
        /// </summary>
        /// <param name = "filename">Filename to terrain file. Type is determined by extension.</param>
        /// <param name = "stream"></param>
        public void LoadRevertMapFromStream(string filename, Stream stream, int offsetX, int offsetY)
        {
            m_revert = InternalLoadFromStream(filename, stream, offsetX, offsetY, m_revert);
        }

        /// <summary>
        ///   Loads a terrain file from a stream and installs it in the scene.
        /// </summary>
        /// <param name = "filename">Filename to terrain file. Type is determined by extension.</param>
        /// <param name = "stream"></param>
        public void LoadWaterRevertMapFromStream(string filename, Stream stream, int offsetX, int offsetY)
        {
            m_waterRevert = InternalLoadFromStream(filename, stream, offsetX, offsetY, m_waterRevert);
        }

        /// <summary>
        ///   Modify Land
        /// </summary>
        /// <param name = "pos">Land-position (X,Y,0)</param>
        /// <param name = "size">The size of the brush (0=small, 1=medium, 2=large)</param>
        /// <param name = "action">0=LAND_LEVEL, 1=LAND_RAISE, 2=LAND_LOWER, 3=LAND_SMOOTH, 4=LAND_NOISE, 5=LAND_REVERT</param>
        /// <param name = "agentId">UUID of script-owner</param>
        public void ModifyTerrain(UUID user, Vector3 pos, byte size, byte action, UUID agentId)
        {
            float duration = 0.25f;
            if (action == 0)
                duration = 4.0f;
            client_OnModifyTerrain(user, pos.Z, duration, size, action, pos.Y, pos.X, pos.Y, pos.X, agentId, size);
        }

        /// <summary>
        ///   Saves the current heightmap to a specified stream.
        /// </summary>
        /// <param name = "filename">The destination filename.  Used here only to identify the image type</param>
        /// <param name = "stream"></param>
        public void SaveToStream(ITerrainChannel channel, string filename, Stream stream)
        {
            try
            {
#if (!ISWIN)
                foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
                {
                    if (filename.EndsWith(loader.Key))
                    {
                        loader.Value.SaveStream(stream, channel);
                        return;
                    }
                }
#else
                foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders.Where(loader => filename.EndsWith(loader.Key)))
                {
                    loader.Value.SaveStream(stream, channel);
                    return;
                }
#endif
            }
            catch (NotImplementedException)
            {
                MainConsole.Instance.Error("Unable to save to " + filename + ", saving of this file format has not been implemented.");
                throw new TerrainException(
                    String.Format("Unable to save heightmap: saving of this file format not implemented"));
            }
        }

        public void TaintTerrain()
        {
            CheckForTerrainUpdates();
        }

        #endregion

        #region Plugin Loading Methods

        private void LoadPlugins()
        {
            string plugineffectsPath = "Terrain";

            // Load the files in the Terrain/ dir
            if (!Directory.Exists(plugineffectsPath))
                return;

            ITerrainLoader[] loaders = AuroraModuleLoader.PickupModules<ITerrainLoader>().ToArray();
            foreach (ITerrainLoader terLoader in loaders)
            {
                m_loaders[terLoader.FileExtension] = terLoader;
            }
        }

        #endregion

        public void TerrainUpdateTimer(object sender, EventArgs ea)
        {
            long now = DateTime.Now.Ticks;

            if (m_queueNextSave > 0 && m_queueNextSave < now)
            {
                m_queueNextSave = 0;
                m_scene.PhysicsScene.SetTerrain(m_channel, m_channel.GetSerialised(m_scene));

                if (m_queueNextSave == 0)
                    m_queueTimer.Stop();
            }
        }

        public void QueueTerrainUpdate()
        {
            m_queueNextSave = DateTime.Now.Ticks + Convert.ToInt64(m_savetime*1000*10000);
            m_queueTimer.Start();
        }

        /// <summary>
        ///   Installs terrain brush hook to IClientAPI
        /// </summary>
        /// <param name = "client"></param>
        private void EventManager_OnNewClient(IClientAPI client)
        {
            client.OnModifyTerrain += client_OnModifyTerrain;
            client.OnBakeTerrain += client_OnBakeTerrain;
            client.OnLandUndo += client_OnLandUndo;
            client.OnGodlikeMessage += client_onGodlikeMessage;
            client.OnRegionHandShakeReply += SendLayerData;

            //Add them to the cache
            lock (m_terrainPatchesSent)
            {
                if (!m_terrainPatchesSent.ContainsKey(client.AgentId))
                {
                    IScenePresence agent = m_scene.GetScenePresence(client.AgentId);
                    if (agent != null && agent.IsChildAgent)
                    {
                        //If the avatar is a child agent, we need to send the terrain data initially
                        EventManager_OnSignificantClientMovement(agent);
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
            client.OnRegionHandShakeReply -= SendLayerData;

            //Remove them from the cache
            lock (m_terrainPatchesSent)
            {
                m_terrainPatchesSent.Remove(client.AgentId);
            }
        }

        /// <summary>
        ///   Send the region heightmap to the client
        /// </summary>
        /// <param name = "RemoteClient">Client to send to</param>
        public void SendLayerData(IClientAPI RemoteClient)
        {
            if (!m_sendTerrainUpdatesByViewDistance && !m_noTerrain)
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

        private object AuroraEventManager_OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "DrawDistanceChanged" || FunctionName == "SignficantCameraMovement")
            {
                SendTerrainUpdatesForClient((IScenePresence) parameters);
            }
            return null;
        }

        private void EventManager_OnSignificantClientMovement(IScenePresence presence)
        {
            if (Vector3.DistanceSquared(presence.AbsolutePosition, m_previousCheckedPosition) > 16*16)
            {
                m_previousCheckedPosition = presence.AbsolutePosition;
                SendTerrainUpdatesForClient(presence);
            }
        }

        private void OnNewPresence(IScenePresence presence)
        {
            SendTerrainUpdatesForClient(presence);
        }

        protected void SendTerrainUpdatesForClient(IScenePresence presence)
        {
            if (!m_sendTerrainUpdatesByViewDistance || m_noTerrain || presence.DrawDistance == 0)
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
                int xSize = m_scene.RegionInfo.RegionSizeX != int.MaxValue
                                ? m_scene.RegionInfo.RegionSizeX/Constants.TerrainPatchSize
                                : Constants.RegionSize/Constants.TerrainPatchSize;
                int ySize = m_scene.RegionInfo.RegionSizeX != int.MaxValue
                                ? m_scene.RegionInfo.RegionSizeY/Constants.TerrainPatchSize
                                : Constants.RegionSize/Constants.TerrainPatchSize;
                terrainarray = new bool[xSize,ySize];
                fillLater = true;
            }

            List<int> xs = new List<int>();
            List<int> ys = new List<int>();
            int startX = (((int) (presence.AbsolutePosition.X - presence.DrawDistance))/Constants.TerrainPatchSize) - 2;
            startX = Math.Max(startX, 0);
            startX = Math.Min(startX, m_scene.RegionInfo.RegionSizeX/Constants.TerrainPatchSize);
            int startY = (((int) (presence.AbsolutePosition.Y - presence.DrawDistance))/Constants.TerrainPatchSize) - 2;
            startY = Math.Max(startY, 0);
            startY = Math.Min(startY, m_scene.RegionInfo.RegionSizeY/Constants.TerrainPatchSize);
            int endX = (((int) (presence.AbsolutePosition.X + presence.DrawDistance))/Constants.TerrainPatchSize) + 2;
            endX = Math.Max(endX, 0);
            endX = Math.Min(endX, m_scene.RegionInfo.RegionSizeX/Constants.TerrainPatchSize);
            int endY = (((int) (presence.AbsolutePosition.Y + presence.DrawDistance))/Constants.TerrainPatchSize) + 2;
            endY = Math.Max(endY, 0);
            endY = Math.Min(endY, m_scene.RegionInfo.RegionSizeY/Constants.TerrainPatchSize);
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (x < 0 || y < 0 || x >= m_scene.RegionInfo.RegionSizeX/Constants.TerrainPatchSize ||
                        y >= m_scene.RegionInfo.RegionSizeY/Constants.TerrainPatchSize)
                        continue;
                    //Need to make sure we don't send the same ones over and over
                    if (!terrainarray[x, y])
                    {
                        Vector3 posToCheckFrom = new Vector3(presence.AbsolutePosition.X % m_scene.RegionInfo.RegionSizeX,
                            presence.AbsolutePosition.Y % m_scene.RegionInfo.RegionSizeY, presence.AbsolutePosition.Z);
                        int xx, yy;
                        Util.UlongToInts(presence.RootAgentHandle, out xx, out yy);
                        int xOffset = m_scene.RegionInfo.RegionLocX - xx;
                        int yOffset = m_scene.RegionInfo.RegionLocY - yy;
                        //Check which has less distance, camera or avatar position, both have to be done
                        if (Util.DistanceLessThan(posToCheckFrom,
                            new Vector3(x * Constants.TerrainPatchSize + (xOffset > 0 ? -xOffset : xOffset), y * Constants.TerrainPatchSize + (yOffset > 0 ? -yOffset : yOffset),
                                                              0), presence.DrawDistance + 50) ||
                            Util.DistanceLessThan(presence.CameraPosition,
                                                  new Vector3(x*Constants.TerrainPatchSize, y*Constants.TerrainPatchSize,
                                                              0), presence.DrawDistance + 50))
                            //Its not a radius, its a diameter and we add 35 so that it doesn't look like it cuts off
                        {
                            //They can see it, send it to them
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
                presence.ControllingClient.SendLayerData(xs.ToArray(), ys.ToArray(), m_channel.GetSerialised(m_scene),
                                                         TerrainPatch.LayerType.Land);
                if (m_use3DWater)
                {
                    //Send all the water patches at once
                    presence.ControllingClient.SendLayerData(xs.ToArray(), ys.ToArray(),
                                                             m_waterChannel.GetSerialised(m_scene),
                                                             TerrainPatch.LayerType.Water);
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
        ///   Reset the terrain of this region to the default
        /// </summary>
        public void ResetWater()
        {
            if (!m_noTerrain)
            {
                TerrainChannel channel = new TerrainChannel(m_scene);
                m_waterChannel = channel;
                m_scene.SimulationDataService.Tainted();
                CheckForTerrainUpdates(false, true, true);
            }
        }

        /// <summary>
        ///   Loads the World Revert heightmap
        /// </summary>
        public void LoadRevertWaterMap()
        {
            try
            {
                short[] map = m_scene.SimulationDataService.LoadWater(m_scene, true, m_scene.RegionInfo.RegionSizeX,
                                                                      m_scene.RegionInfo.RegionSizeY);
                if (map == null)
                {
                    if (m_waterRevert == null)
                    {
                        m_waterRevert = m_waterChannel.MakeCopy();

                        m_scene.SimulationDataService.Tainted();
                    }
                }
                else
                {
                    m_waterRevert = new TerrainChannel(map, m_scene);
                }
            }
            catch (IOException e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadRevertWaterMap() - Failed with exception " + e + " Regenerating");
                // Non standard region size.    If there's an old terrain in the database, it might read past the buffer
                if (m_scene.RegionInfo.RegionSizeX != Constants.RegionSize ||
                    m_scene.RegionInfo.RegionSizeY != Constants.RegionSize)
                {
                    m_waterRevert = m_waterChannel.MakeCopy();

                    m_scene.SimulationDataService.Tainted();
                }
            }
            catch (IndexOutOfRangeException e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadRevertWaterMap() - Failed with exception " + e + " Regenerating");
                m_waterRevert = m_waterChannel.MakeCopy();

                m_scene.SimulationDataService.Tainted();
            }
            catch (ArgumentOutOfRangeException e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadRevertWaterMap() - Failed with exception " + e + " Regenerating");
                m_waterRevert = m_waterChannel.MakeCopy();

                m_scene.SimulationDataService.Tainted();
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadRevertWaterMap() - Failed with exception " + e);
                m_waterRevert = m_waterChannel.MakeCopy();

                m_scene.SimulationDataService.Tainted();
            }
        }

        /// <summary>
        ///   Loads the World heightmap
        /// </summary>
        public void LoadWorldWaterMap()
        {
            if (!m_use3DWater)
                return;
            try
            {
                short[] map = m_scene.SimulationDataService.LoadWater(m_scene, false, m_scene.RegionInfo.RegionSizeX,
                                                                      m_scene.RegionInfo.RegionSizeY);
                if (map == null)
                {
                    if (m_waterChannel == null)
                    {
                        MainConsole.Instance.Info("[TERRAIN]: No default water. Generating a new water.");
                        m_waterChannel = new TerrainChannel(m_scene);
                        for (int x = 0; x < m_waterChannel.Height; x++)
                        {
                            for (int y = 0; y < m_waterChannel.Height; y++)
                            {
                                m_waterChannel[x, y] = (float) m_scene.RegionInfo.RegionSettings.WaterHeight;
                            }
                        }

                        m_scene.SimulationDataService.Tainted();
                    }
                }
                else
                {
                    m_waterChannel = new TerrainChannel(map, m_scene);
                }
            }
            catch (IOException e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadWorldWaterMap() - Failed with exception " + e + " Regenerating");
                // Non standard region size.    If there's an old terrain in the database, it might read past the buffer
                if (m_scene.RegionInfo.RegionSizeX != Constants.RegionSize ||
                    m_scene.RegionInfo.RegionSizeY != Constants.RegionSize)
                {
                    m_waterChannel = new TerrainChannel(m_scene);
                    for (int x = 0; x < m_waterChannel.Height; x++)
                    {
                        for (int y = 0; y < m_waterChannel.Height; y++)
                        {
                            m_waterChannel[x, y] = (float) m_scene.RegionInfo.RegionSettings.WaterHeight;
                        }
                    }

                    m_scene.SimulationDataService.Tainted();
                }
            }
            catch (IndexOutOfRangeException e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadWorldWaterMap() - Failed with exception " + e + " Regenerating");
                m_waterChannel = new TerrainChannel(m_scene);
                for (int x = 0; x < m_waterChannel.Height; x++)
                {
                    for (int y = 0; y < m_waterChannel.Height; y++)
                    {
                        m_waterChannel[x, y] = (float) m_scene.RegionInfo.RegionSettings.WaterHeight;
                    }
                }

                m_scene.SimulationDataService.Tainted();
            }
            catch (ArgumentOutOfRangeException e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: LoadWorldWaterMap() - Failed with exception " + e + " Regenerating");
                m_waterChannel = new TerrainChannel(m_scene);
                for (int x = 0; x < m_waterChannel.Height; x++)
                {
                    for (int y = 0; y < m_waterChannel.Height; y++)
                    {
                        m_waterChannel[x, y] = (float) m_scene.RegionInfo.RegionSettings.WaterHeight;
                    }
                }

                m_scene.SimulationDataService.Tainted();
            }
            catch (Exception e)
            {
                MainConsole.Instance.Warn("[TERRAIN]: Scene.cs: LoadWorldMap() - Failed with exception " + e);
                m_waterChannel = new TerrainChannel(m_scene);
                for (int x = 0; x < m_waterChannel.Height; x++)
                {
                    for (int y = 0; y < m_waterChannel.Height; y++)
                    {
                        m_waterChannel[x, y] = (float) m_scene.RegionInfo.RegionSettings.WaterHeight;
                    }
                }

                m_scene.SimulationDataService.Tainted();
            }
            LoadRevertWaterMap();
        }

        /// <summary>
        ///   Loads a terrain file from a stream and installs it in the scene.
        /// </summary>
        /// <param name = "filename">Filename to terrain file. Type is determined by extension.</param>
        /// <param name = "stream"></param>
        public ITerrainChannel InternalLoadFromStream(string filename, Stream stream, int offsetX, int offsetY,
                                                      ITerrainChannel update)
        {
            ITerrainChannel channel = null;
#if (!ISWIN)
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
            {
                if (filename.EndsWith(loader.Key))
                {
                    lock (m_scene)
                    {
                        try
                        {
                            channel = loader.Value.LoadStream(stream, m_scene);
                            if (channel != null)
                            {
                                channel.Scene = m_scene;
                                if (update == null || (update.Height == channel.Height && update.Width == channel.Width))
                                {
                                    if (m_scene.RegionInfo.RegionSizeX != channel.Width || m_scene.RegionInfo.RegionSizeY != channel.Height)
                                    {
                                        if ((channel.Width) > m_scene.RegionInfo.RegionSizeX || (channel.Height) > m_scene.RegionInfo.RegionSizeY)
                                        {
                                            TerrainChannel c = new TerrainChannel(true, m_scene);
                                            for (int x = 0; x < m_scene.RegionInfo.RegionSizeX; x++)
                                            {
                                                for (int y = 0; y < m_scene.RegionInfo.RegionSizeY; y++)
                                                {
                                                    c[x, y] = channel[x, y];
                                                }
                                            }
                                            return c;
                                        }
                                        return null;
                                    }
                                }
                                else
                                {
                                    //Make sure it is in bounds
                                    if ((offsetX + channel.Width) > update.Width || (offsetY + channel.Height) > update.Height)
                                    {
                                        MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, the terrain you have given is larger than the current region.");
                                        return null;
                                    }
                                    else
                                    {
                                        //Merge the terrains together at the specified offset
                                        for (int x = offsetX; x < offsetX + channel.Width; x++)
                                        {
                                            for (int y = offsetY; y < offsetY + channel.Height; y++)
                                            {
                                                update[x, y] = channel[x - offsetX, y - offsetY];
                                            }
                                        }
                                        return update;
                                    }
                                }
                            }
                        }
                        catch (NotImplementedException)
                        {
                            MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, the " + loader.Value + " parser does not support file loading. (May be save only)");
                            throw new TerrainException(String.Format("unable to load heightmap: parser {0} does not support loading", loader.Value));
                        }
                    }

                    MainConsole.Instance.Info("[TERRAIN]: File (" + filename + ") loaded successfully");
                    return channel;
                }
            }
#else
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders.Where(loader => filename.EndsWith(loader.Key)))
            {
                lock (m_scene)
                {
                    try
                    {
                        channel = loader.Value.LoadStream(stream, m_scene);
                        if (channel != null)
                        {
                            channel.Scene = m_scene;
                            if (update == null || (update.Height == channel.Height &&
                                                   update.Width == channel.Width))
                            {
                                if (m_scene.RegionInfo.RegionSizeX != channel.Width ||
                                    m_scene.RegionInfo.RegionSizeY != channel.Height)
                                {
                                    if ((channel.Width) > m_scene.RegionInfo.RegionSizeX ||
                                        (channel.Height) > m_scene.RegionInfo.RegionSizeY)
                                    {
                                        TerrainChannel c = new TerrainChannel(true, m_scene);
                                        for (int x = 0; x < m_scene.RegionInfo.RegionSizeX; x++)
                                        {
                                            for (int y = 0; y < m_scene.RegionInfo.RegionSizeY; y++)
                                            {
                                                c[x, y] = channel[x, y];
                                            }
                                        }
                                        return c;
                                    }
                                    return null;
                                }
                            }
                            else
                            {
                                //Make sure it is in bounds
                                if ((offsetX + channel.Width) > update.Width ||
                                    (offsetY + channel.Height) > update.Height)
                                {
                                    MainConsole.Instance.Error(
                                        "[TERRAIN]: Unable to load heightmap, the terrain you have given is larger than the current region.");
                                    return null;
                                }
                                else
                                {
                                    //Merge the terrains together at the specified offset
                                    for (int x = offsetX; x < offsetX + channel.Width; x++)
                                    {
                                        for (int y = offsetY; y < offsetY + channel.Height; y++)
                                        {
                                            update[x, y] = channel[x - offsetX, y - offsetY];
                                        }
                                    }
                                    return update;
                                }
                            }
                        }
                    }
                    catch (NotImplementedException)
                    {
                        MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, the " + loader.Value +
                                    " parser does not support file loading. (May be save only)");
                        throw new TerrainException(
                            String.Format("unable to load heightmap: parser {0} does not support loading",
                                          loader.Value));
                    }
                }

                MainConsole.Instance.Info("[TERRAIN]: File (" + filename + ") loaded successfully");
                return channel;
            }
#endif
            MainConsole.Instance.Error("[TERRAIN]: Unable to load heightmap, no file loader available for that format.");
            throw new TerrainException(
                String.Format("unable to load heightmap from file {0}: no loader available for that format", filename));
        }

        private static Stream URIFetch(Uri uri)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(uri);

            // request.Credentials = credentials;

            request.ContentLength = 0;
            request.KeepAlive = false;

            WebResponse response = request.GetResponse();
            Stream file = response.GetResponseStream();

            if (response.ContentLength == 0)
                throw new Exception(String.Format("{0} returned an empty file", uri));

            // return new BufferedStream(file, (int) response.ContentLength);
            return new BufferedStream(file, 1000000);
        }

        /// <summary>
        ///   Installs into terrain module the standard suite of brushes
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
            m_painteffects[StandardTerrainEffects.Paint] = new PaintSphere();
            if (m_scene.RegionInfo.RegionSettings.UsePaintableTerrain)
                m_painteffects[StandardTerrainEffects.Revert] = new PaintSphere();

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
        ///   Saves the current state of the region into the revert map buffer.
        /// </summary>
        public void UpdateRevertWaterMap()
        {
            m_waterRevert = m_waterChannel.MakeCopy();
            m_scene.SimulationDataService.Tainted();
        }

        /// <summary>
        ///   Saves the current state of the region into the revert map buffer.
        /// </summary>
        public void UpdateRevertMap()
        {
            m_revert = null;
            m_revert = m_channel.MakeCopy();
            m_scene.SimulationDataService.Tainted();
        }

        /// <summary>
        ///   Loads a tile from a larger terrain file and installs it into the region.
        /// </summary>
        /// <param name = "filename">The terrain file to load</param>
        /// <param name = "fileWidth">The width of the file in units</param>
        /// <param name = "fileHeight">The height of the file in units</param>
        /// <param name = "fileStartX">Where to begin our slice</param>
        /// <param name = "fileStartY">Where to begin our slice</param>
        public void LoadFromFile(string filename, int fileWidth, int fileHeight, int fileStartX, int fileStartY)
        {
            int offsetX = (m_scene.RegionInfo.RegionLocX/Constants.RegionSize) - fileStartX;
            int offsetY = (m_scene.RegionInfo.RegionLocY/Constants.RegionSize) - fileStartY;

            if (offsetX >= 0 && offsetX < fileWidth && offsetY >= 0 && offsetY < fileHeight)
            {
                // this region is included in the tile request
#if (!ISWIN)
                foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
                {
                    if (filename.EndsWith(loader.Key))
                    {
                        lock (m_scene)
                        {
                            ITerrainChannel channel = loader.Value.LoadFile(filename, offsetX, offsetY, fileWidth, fileHeight, m_scene.RegionInfo.RegionSizeX, m_scene.RegionInfo.RegionSizeY);
                            channel.Scene = m_scene;
                            m_channel = channel;
                            m_scene.RegisterModuleInterface(m_channel);
                            UpdateRevertMap();
                        }
                        return;
                    }
                }
#else
                foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders.Where(loader => filename.EndsWith(loader.Key)))
                {
                    lock (m_scene)
                    {
                        ITerrainChannel channel = loader.Value.LoadFile(filename, offsetX, offsetY,
                                                                        fileWidth, fileHeight,
                                                                        m_scene.RegionInfo.RegionSizeX,
                                                                        m_scene.RegionInfo.RegionSizeY);
                        channel.Scene = m_scene;
                        m_channel = channel;
                        m_scene.RegisterModuleInterface(m_channel);
                        UpdateRevertMap();
                    }
                    return;
                }
#endif
            }
        }

        private void client_onGodlikeMessage(IClientAPI client, UUID requester, string Method, List<string> Parameters)
        {
            if (!m_scene.Permissions.IsGod(client.AgentId))
                return;
            if (client.Scene.RegionInfo.RegionID != m_scene.RegionInfo.RegionID)
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
                    InterfaceRevertTerrain(null);
                }
                if (parameter1 == "swap")
                {
                    //This is so you can change terrain with other regions... not implemented yet
                }
            }
        }

        /// <summary>
        ///   Checks to see if the terrain has been modified since last check
        ///   but won't attempt to limit those changes to the limits specified in the estate settings
        ///   currently invoked by the command line operations in the region server only
        /// </summary>
        private void CheckForTerrainUpdates()
        {
            CheckForTerrainUpdates(false, false, false);
        }

        /// <summary>
        ///   Checks to see if the terrain has been modified since last check.
        ///   If it has been modified, every all the terrain patches are sent to the client.
        ///   If the call is asked to respect the estate settings for terrain_raise_limit and
        ///   terrain_lower_limit, it will clamp terrain updates between these values
        ///   currently invoked by client_OnModifyTerrain only and not the Commander interfaces
        ///   <param name = "respectEstateSettings">should height map deltas be limited to the estate settings limits</param>
        ///   <param name = "forceSendOfTerrainInfo">force send terrain</param>
        ///   <param name = "isWater">Check water or terrain</param>
        /// </summary>
        private void CheckForTerrainUpdates(bool respectEstateSettings, bool forceSendOfTerrainInfo, bool isWater)
        {
            ITerrainChannel channel = isWater ? m_waterChannel : m_channel;
            bool shouldTaint = false;

            // if we should respect the estate settings then
            // fixup and height deltas that don't respect them
            if (respectEstateSettings)
                LimitChannelChanges(channel, isWater ? m_waterRevert : m_revert);
            else if (!forceSendOfTerrainInfo)
                LimitMaxTerrain(channel);

            List<int> xs = new List<int>();
            List<int> ys = new List<int>();
            for (int x = 0; x < channel.Width; x += Constants.TerrainPatchSize)
            {
                for (int y = 0; y < channel.Height; y += Constants.TerrainPatchSize)
                {
                    if (channel.Tainted(x, y) || forceSendOfTerrainInfo)
                    {
                        xs.Add(x/Constants.TerrainPatchSize);
                        ys.Add(y/Constants.TerrainPatchSize);
                        shouldTaint = true;
                    }
                }
            }
            if (shouldTaint || forceSendOfTerrainInfo)
            {
                QueueTerrainUpdate();
                m_scene.SimulationDataService.Tainted();
            }

            foreach (IScenePresence presence in m_scene.GetScenePresences())
            {
                if (!m_sendTerrainUpdatesByViewDistance)
                {
                    presence.ControllingClient.SendLayerData(xs.ToArray(), ys.ToArray(), channel.GetSerialised(m_scene),
                                                             isWater
                                                                 ? TerrainPatch.LayerType.Land
                                                                 : TerrainPatch.LayerType.Water);
                }
                else
                {
                    for (int i = 0; i < xs.Count; i++)
                    {
                        m_terrainPatchesSent[presence.UUID][xs[i], ys[i]] = false;
                    }
                    SendTerrainUpdatesForClient(presence);
                }
            }
        }

        private bool LimitMaxTerrain(ITerrainChannel channel)
        {
            bool changesLimited = false;

            // loop through the height map for this patch and compare it against
            // the revert map
            for (int x = 0; x < m_scene.RegionInfo.RegionSizeX/Constants.TerrainPatchSize; x++)
            {
                for (int y = 0; y < m_scene.RegionInfo.RegionSizeY/+Constants.TerrainPatchSize; y++)
                {
                    float requestedHeight = channel[x, y];

                    if (requestedHeight > MAX_HEIGHT)
                    {
                        channel[x, y] = MAX_HEIGHT;
                        changesLimited = true;
                    }
                    else if (requestedHeight < MIN_HEIGHT)
                    {
                        channel[x, y] = MIN_HEIGHT; //as lower is a -ve delta
                        changesLimited = true;
                    }
                }
            }

            return changesLimited;
        }

        /// <summary>
        ///   Checks to see height deltas in the tainted terrain patch at xStart ,yStart
        ///   are all within the current estate limits
        ///   <returns>true if changes were limited, false otherwise</returns>
        /// </summary>
        private bool LimitChannelChanges(ITerrainChannel channel, ITerrainChannel revert)
        {
            bool changesLimited = false;
            float minDelta = (float) m_scene.RegionInfo.RegionSettings.TerrainLowerLimit;
            float maxDelta = (float) m_scene.RegionInfo.RegionSettings.TerrainRaiseLimit;

            // loop through the height map for this patch and compare it against
            // the revert map
            for (int x = 0; x < m_scene.RegionInfo.RegionSizeX/Constants.TerrainPatchSize; x++)
            {
                for (int y = 0; y < m_scene.RegionInfo.RegionSizeY/Constants.TerrainPatchSize; y++)
                {
                    float requestedHeight = channel[x, y];
                    float bakedHeight = revert[x, y];
                    float requestedDelta = requestedHeight - bakedHeight;

                    if (requestedDelta > maxDelta)
                    {
                        channel[x, y] = bakedHeight + maxDelta;
                        changesLimited = true;
                    }
                    else if (requestedDelta < minDelta)
                    {
                        channel[x, y] = bakedHeight + minDelta; //as lower is a -ve delta
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

        private void client_OnModifyTerrain(UUID user, float height, float seconds, byte size, byte action,
                                            float north, float west, float south, float east, UUID agentId,
                                            float BrushSize)
        {
            bool god = m_scene.Permissions.IsGod(user);
            const byte WATER_CONST = 128;
            if (north == south && east == west)
            {
                if (m_painteffects.ContainsKey((StandardTerrainEffects) action))
                {
                    StoreUndoState();
                    m_painteffects[(StandardTerrainEffects) action].PaintEffect(
                        m_channel, user, west, south, height, size, seconds, BrushSize, m_scenes);

                    //revert changes outside estate limits
                    CheckForTerrainUpdates(!god, false, false);
                }
                else
                {
                    if (m_painteffects.ContainsKey((StandardTerrainEffects) (action - WATER_CONST)))
                    {
                        StoreUndoState();
                        m_painteffects[(StandardTerrainEffects) action - WATER_CONST].PaintEffect(
                            m_waterChannel, user, west, south, height, size, seconds, BrushSize, m_scenes);

                        //revert changes outside estate limits
                        CheckForTerrainUpdates(!god, false, true);
                    }
                    else
                        MainConsole.Instance.Warn("Unknown terrain brush type " + action);
                }
            }
            else
            {
                if (m_floodeffects.ContainsKey((StandardTerrainEffects) action))
                {
                    StoreUndoState();
                    m_floodeffects[(StandardTerrainEffects) action].FloodEffect(
                        m_channel, user, north, west, south, east, size);

                    //revert changes outside estate limits
                    CheckForTerrainUpdates(!god, false, false);
                }
                else
                {
                    if (m_floodeffects.ContainsKey((StandardTerrainEffects) (action - WATER_CONST)))
                    {
                        StoreUndoState();
                        m_floodeffects[(StandardTerrainEffects) action - WATER_CONST].FloodEffect(
                            m_waterChannel, user, north, west, south, east, size);

                        //revert changes outside estate limits
                        CheckForTerrainUpdates(!god, false, true);
                    }
                    else
                        MainConsole.Instance.Warn("Unknown terrain flood type " + action);
                }
            }
        }

        private void client_OnBakeTerrain(IClientAPI remoteClient)
        {
            // Not a good permissions check (see client_OnModifyTerrain above), need to check the entire area.
            // for now check a point in the centre of the region

            if (m_scene.Permissions.CanIssueEstateCommand(remoteClient.AgentId, true))
            {
                InterfaceBakeTerrain(null); //bake terrain does not use the passed in parameter
            }
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
                string line =
                    MainConsole.Instance.Prompt("Are you sure that you want to do this command on all scenes?", "yes");
                if (!line.Equals("yes", StringComparison.CurrentCultureIgnoreCase))
                    return modules;
                //Return them all
                return m_terrainModules;
            }
#if (!ISWIN)
            foreach (TerrainModule module in m_terrainModules)
            {
                if (module.m_scene == scene)
                {
                    modules.Add(module);
                }
            }
#else
            modules.AddRange(m_terrainModules.Where(module => module.m_scene == scene));
#endif
            return modules;
        }

        private void InterfaceLoadFile(string[] cmd)
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
            }
        }

        private void InterfaceLoadTileFile(string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                tmodule.LoadFromFile(cmd[2],
                                     int.Parse(cmd[3]),
                                     int.Parse(cmd[4]),
                                     int.Parse(cmd[5]),
                                     int.Parse(cmd[6]));
            }
        }

        private void InterfaceSaveFile(string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                tmodule.SaveToFile(cmd[2]);
            }
        }

        private void InterfaceSavePhysics(string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                tmodule.m_scene.PhysicsScene.SetTerrain(tmodule.m_channel,
                                                        tmodule.m_channel.GetSerialised(tmodule.m_scene));
            }
        }

        private void InterfaceBakeTerrain(string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                tmodule.UpdateRevertMap();
            }
        }

        private void InterfaceRevertTerrain(string[] cmd)
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

        private void InterfaceFlipTerrain(string[] cmd)
        {
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                String direction = cmd[2];

                if (direction.ToLower().StartsWith("y"))
                {
                    for (int x = 0; x < m_scene.RegionInfo.RegionSizeX; x++)
                    {
                        for (int y = 0; y < m_scene.RegionInfo.RegionSizeY/2; y++)
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
                        for (int x = 0; x < m_scene.RegionInfo.RegionSizeX/2; x++)
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
                    MainConsole.Instance.Error("Unrecognised direction - need x or y");
                }


                tmodule.CheckForTerrainUpdates();
            }
        }

        private void InterfaceRescaleTerrain(string[] cmd)
        {
            if (cmd.Count() < 4)
            {
                MainConsole.Instance.Info("You do not have enough parameters. Please look at 'terrain help' for more info.");
                return;
            }
            List<TerrainModule> m = FindModuleForScene(MainConsole.Instance.ConsoleScene);

            foreach (TerrainModule tmodule in m)
            {
                float desiredMin = float.Parse(cmd[2]);
                float desiredMax = float.Parse(cmd[3]);

                // determine desired scaling factor
                float desiredRange = desiredMax - desiredMin;
                //MainConsole.Instance.InfoFormat("Desired {0}, {1} = {2}", new Object[] { desiredMin, desiredMax, desiredRange });

                if (desiredRange == 0d)
                {
                    // delta is zero so flatten at requested height
                    tmodule.InterfaceFillTerrain(cmd);
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
                    float scale = desiredRange/currRange;

                    //MainConsole.Instance.InfoFormat("Current {0}, {1} = {2}", new Object[] { currMin, currMax, currRange });
                    //MainConsole.Instance.InfoFormat("Scale = {0}", scale);

                    // scale the heightmap accordingly
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float currHeight = tmodule.m_channel[x, y] - currMin;
                            tmodule.m_channel[x, y] = desiredMin + (currHeight*scale);
                        }
                    }

                    tmodule.CheckForTerrainUpdates();
                }
            }
        }

        private void InterfaceElevateTerrain(string[] cmd)
        {
            if (cmd.Count() < 3)
            {
                MainConsole.Instance.Info("You do not have enough parameters. Please look at 'terrain help' for more info.");
                return;
            }
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

        private void InterfaceMultiplyTerrain(string[] cmd)
        {
            if (cmd.Count() < 3)
            {
                MainConsole.Instance.Info("You do not have enough parameters. Please look at 'terrain help' for more info.");
                return;
            }
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

        private void InterfaceLowerTerrain(string[] cmd)
        {
            if (cmd.Count() < 3)
            {
                MainConsole.Instance.Info("You do not have enough parameters. Please look at 'terrain help' for more info.");
                return;
            }
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

        private void InterfaceFillTerrain(string[] cmd)
        {
            if (cmd.Count() < 3)
            {
                MainConsole.Instance.Info("You do not have enough parameters. Please look at 'terrain help' for more info.");
                return;
            }
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

        private void InterfaceShowDebugStats(string[] cmd)
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

                double avg = sum/(tmodule.m_channel.Height*tmodule.m_channel.Width);

                MainConsole.Instance.Info("Channel " + tmodule.m_channel.Width + "x" + tmodule.m_channel.Height);
                MainConsole.Instance.Info("max/min/avg/sum: " + max + "/" + min + "/" + avg + "/" + sum);
            }
        }

        private void InterfaceEnableExperimentalBrushes(string[] cmd)
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

        private void InterfaceHelp(string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
            {
                if (MainConsole.Instance.ConsoleScene == null && !MainConsole.Instance.HasProcessedCurrentCommand)
                    MainConsole.Instance.HasProcessedCurrentCommand = true;
                else
                    return;
            }
#if (!ISWIN)
            string supportedFileExtensions = "";
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
                supportedFileExtensions = supportedFileExtensions + (" " + loader.Key + " (" + loader.Value + ")");
#else
            string supportedFileExtensions = m_loaders.Aggregate("", (current, loader) => current + (" " + loader.Key + " (" + loader.Value + ")"));
#endif

            MainConsole.Instance.Info(
                "terrain load <FileName> - Loads a terrain from a specified file. FileName: The file you wish to load from, the file extension determines the loader to be used. Supported extensions include: " +
                supportedFileExtensions);
            MainConsole.Instance.Info(
                "terrain save <FileName> - Saves the current heightmap to a specified file. FileName: The destination filename for your heightmap, the file extension determines the format to save in. Supported extensions include: " +
                supportedFileExtensions);
            MainConsole.Instance.Info(
                "terrain load-tile <file width> <file height> <minimum X tile> <minimum Y tile> - Loads a terrain from a section of a larger file. " +
                "\n file width: The width of the file in tiles" +
                "\n file height: The height of the file in tiles" +
                "\n minimum X tile: The X region coordinate of the first section on the file" +
                "\n minimum Y tile: The Y region coordinate of the first section on the file");
            MainConsole.Instance.Info("terrain fill <value> - Fills the current heightmap with a specified value." +
                       "\n value: The numeric value of the height you wish to set your region to.");
            MainConsole.Instance.Info("terrain elevate <value> - Raises the current heightmap by the specified amount." +
                       "\n amount: The amount of height to remove from the terrain in meters.");
            MainConsole.Instance.Info("terrain lower <value> - Lowers the current heightmap by the specified amount." +
                       "\n amount: The amount of height to remove from the terrain in meters.");
            MainConsole.Instance.Info("terrain multiply <value> - Multiplies the heightmap by the value specified." +
                       "\n value: The value to multiply the heightmap by.");
            MainConsole.Instance.Info("terrain bake - Saves the current terrain into the regions revert map.");
            MainConsole.Instance.Info("terrain revert - Loads the revert map terrain into the regions heightmap.");
            MainConsole.Instance.Info("terrain stats - Shows some information about the regions heightmap for debugging purposes.");
            MainConsole.Instance.Info(
                "terrain newbrushes <enabled> - Enables experimental brushes which replace the standard terrain brushes." +
                "\n enabled: true / false - Enable new brushes");
            MainConsole.Instance.Info("terrain flip <direction> - Flips the current terrain about the X or Y axis" +
                       "\n direction: [x|y] the direction to flip the terrain in");
            MainConsole.Instance.Info(
                "terrain rescale <min> <max> - Rescales the current terrain to fit between the given min and max heights" +
                "\n Min: min terrain height after rescaling" +
                "\n Max: max terrain height after rescaling");
        }

        private void AddConsoleCommands()
        {
            // Load / Save
#if (!ISWIN)
            string supportedFileExtensions = "";
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
                supportedFileExtensions = supportedFileExtensions + (" " + loader.Key + " (" + loader.Value + ")");
#else
            string supportedFileExtensions = m_loaders.Aggregate("", (current, loader) => current + (" " + loader.Key + " (" + loader.Value + ")"));
#endif

            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand("terrain save",
                                                         "terrain save <FileName>",
                                                         "Saves the current heightmap to a specified file. FileName: The destination filename for your heightmap, the file extension determines the format to save in. Supported extensions include: " +
                                                         supportedFileExtensions, InterfaceSaveFile);

                MainConsole.Instance.Commands.AddCommand("terrain physics update",
                                                         "terrain physics update", "Update the physics map",
                                                         InterfaceSavePhysics);

                MainConsole.Instance.Commands.AddCommand("terrain load",
                                                         "terrain load <FileName> <OffsetX=> <OffsetY=>",
                                                         "Loads a terrain from a specified file. FileName: The file you wish to load from, the file extension determines the loader to be used. Supported extensions include: " +
                                                         supportedFileExtensions, InterfaceLoadFile);
                MainConsole.Instance.Commands.AddCommand("terrain load-tile",
                                                         "terrain load-tile <file width> <file height> <minimum X tile> <minimum Y tile>",
                                                         "Loads a terrain from a section of a larger file. " +
                                                         "\n file width: The width of the file in tiles" +
                                                         "\n file height: The height of the file in tiles" +
                                                         "\n minimum X tile: The X region coordinate of the first section on the file" +
                                                         "\n minimum Y tile: The Y region coordinate of the first section on the file",
                                                         InterfaceLoadTileFile);

                MainConsole.Instance.Commands.AddCommand("terrain fill",
                                                         "terrain fill <value> ",
                                                         "Fills the current heightmap with a specified value." +
                                                         "\n value: The numeric value of the height you wish to set your region to.",
                                                         InterfaceFillTerrain);
                MainConsole.Instance.Commands.AddCommand("terrain elevate",
                                                         "terrain elevate <amount> ",
                                                         "Raises the current heightmap by the specified amount." +
                                                         "\n amount: The amount of height to remove from the terrain in meters.",
                                                         InterfaceElevateTerrain);
                MainConsole.Instance.Commands.AddCommand("terrain lower",
                                                         "terrain lower <amount> ",
                                                         "Lowers the current heightmap by the specified amount." +
                                                         "\n amount: The amount of height to remove from the terrain in meters.",
                                                         InterfaceLowerTerrain);
                MainConsole.Instance.Commands.AddCommand("terrain multiply",
                                                         "terrain multiply <value> ",
                                                         "Multiplies the heightmap by the value specified." +
                                                         "\n value: The value to multiply the heightmap by.",
                                                         InterfaceMultiplyTerrain);

                MainConsole.Instance.Commands.AddCommand("terrain bake",
                                                         "terrain bake",
                                                         "Saves the current terrain into the regions revert map.",
                                                         InterfaceBakeTerrain);
                MainConsole.Instance.Commands.AddCommand("terrain revert",
                                                         "terrain revert",
                                                         "Loads the revert map terrain into the regions heightmap.",
                                                         InterfaceRevertTerrain);
                MainConsole.Instance.Commands.AddCommand("terrain stats",
                                                         "terrain stats",
                                                         "Shows some information about the regions heightmap for debugging purposes.",
                                                         InterfaceShowDebugStats);
                MainConsole.Instance.Commands.AddCommand("terrain newbrushes",
                                                         "terrain newbrushes <enabled> ",
                                                         "Enables experimental brushes which replace the standard terrain brushes." +
                                                         "\n enabled: true / false - Enable new brushes",
                                                         InterfaceEnableExperimentalBrushes);
                MainConsole.Instance.Commands.AddCommand("terrain flip",
                                                         "terrain flip <direction> ",
                                                         "Flips the current terrain about the X or Y axis" +
                                                         "\n direction: [x|y] the direction to flip the terrain in",
                                                         InterfaceFlipTerrain);
                MainConsole.Instance.Commands.AddCommand("terrain rescale",
                                                         "terrain rescale <min> <max>",
                                                         "Rescales the current terrain to fit between the given min and max heights" +
                                                         "\n Min: min terrain height after rescaling" +
                                                         "\n Max: max terrain height after rescaling",
                                                         InterfaceRescaleTerrain);
                MainConsole.Instance.Commands.AddCommand("terrain help",
                                                         "terrain help", "Gives help about the terrain module.",
                                                         InterfaceHelp);
            }
        }

        #endregion
    }
}