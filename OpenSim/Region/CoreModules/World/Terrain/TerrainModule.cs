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
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
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

            // Extended brushes
            Erode = 255,
            Weather = 254,
            Olsen = 253
        }

        #endregion

        private static List<Scene> m_scenes = new List<Scene>();

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private readonly Dictionary<StandardTerrainEffects, ITerrainFloodEffect> m_floodeffects =
            new Dictionary<StandardTerrainEffects, ITerrainFloodEffect>();

        private readonly Dictionary<string, ITerrainLoader> m_loaders = new Dictionary<string, ITerrainLoader>();

        private readonly Dictionary<StandardTerrainEffects, ITerrainPaintableEffect> m_painteffects =
            new Dictionary<StandardTerrainEffects, ITerrainPaintableEffect>();

        private ITerrainChannel m_channel;
        private Dictionary<string, ITerrainEffect> m_plugineffects;
        private ITerrainChannel m_revert;
        private Scene m_scene;
        private volatile bool m_tainted;
        private const double MAX_HEIGHT = 250;
        private const double MIN_HEIGHT = -100;
        private readonly UndoStack<LandUndoState> m_undo = new UndoStack<LandUndoState>(5);
        private int m_update_terrain = 50; //Trigger the updating of the terrain mesh in the physics engine
        
        #region INonSharedRegionModule Members

        /// <summary>
        /// Creates and initialises a terrain module for a region
        /// </summary>
        /// <param name="scene">Region initialising</param>
        /// <param name="config">Config for the region</param>
        public void Initialise(IConfigSource config)
        {
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;
            m_scenes.Add(scene);

            LoadWorldHeightmap();
            scene.PhysicsScene.SetTerrain(m_channel.GetFloatsSerialised(scene), m_channel.GetDoubles(scene));
            scene.PhysicsScene.SetWaterLevel((float)scene.RegionInfo.RegionSettings.WaterHeight);

            m_scene.RegisterModuleInterface<ITerrainModule>(this);
            m_scene.EventManager.OnNewClient += EventManager_OnNewClient;
            m_scene.EventManager.OnFrame += EventManager_OnFrame;
            m_scene.EventManager.OnClosingClient += OnClosingClient;
            InstallInterfaces();

            InstallDefaultEffects();
            LoadPlugins();
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
            lock (m_scene)
            {
                // remove the event-handlers
                m_scene.EventManager.OnFrame -= EventManager_OnFrame;
                m_scene.EventManager.OnNewClient -= EventManager_OnNewClient;
                m_scene.EventManager.OnClosingClient -= OnClosingClient;
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

        /// <summary>
        /// Store the terrain in the persistant data store
        /// </summary>
        public void SaveTerrain()
        {
            m_scene.SimulationDataService.StoreTerrain(m_channel.GetDoubles(m_scene), m_scene.RegionInfo.RegionID, false);
        }

        /// <summary>
        /// Reset the terrain of this region to the default
        /// </summary>
        public void ResetTerrain()
        {
            TerrainChannel channel = new TerrainChannel(m_scene);
            SaveRevertTerrain(channel);
            m_channel = channel;
            m_scene.RegisterModuleInterface<ITerrainChannel>(m_channel);
            CheckForTerrainUpdates(false, true);
        }

        /// <summary>
        /// Store the revert terrain in the persistant data store
        /// </summary>
        public void SaveRevertTerrain(ITerrainChannel channel)
        {
            m_scene.SimulationDataService.StoreTerrain(m_channel.GetDoubles(m_scene), m_scene.RegionInfo.RegionID, true);
        }

        /// <summary>
        /// Loads the World Revert heightmap
        /// </summary>
        public ITerrainChannel LoadRevertMap()
        {
            try
            {
                double[,] map = m_scene.SimulationDataService.LoadTerrain(m_scene.RegionInfo.RegionID, true);
                if (map == null)
                {
                    map = m_channel.GetDoubles(m_scene);
                    TerrainChannel channel = new TerrainChannel(map, m_scene);
                    SaveRevertTerrain(channel);
                    return channel;
                }
                return new TerrainChannel(map, m_scene);
            }
            catch (Exception e)
            {
                m_log.Warn("[TERRAIN]: Scene.cs: LoadRevertMap() - Failed with exception " + e.ToString());
            }
            return m_channel;
        }

        /// <summary>
        /// Loads the World heightmap
        /// </summary>
        public void LoadWorldHeightmap()
        {
            try
            {
                double[,] map = m_scene.SimulationDataService.LoadTerrain(m_scene.RegionInfo.RegionID, false);
                if (map == null)
                {
                    m_log.Info("[TERRAIN]: No default terrain. Generating a new terrain.");
                    m_channel = new TerrainChannel(m_scene);

                    m_scene.SimulationDataService.StoreTerrain(m_channel.GetDoubles(m_scene), m_scene.RegionInfo.RegionID, false);
                }
                else
                {
                    m_channel = new TerrainChannel(map, m_scene);
                }
            }
            catch (IOException e)
            {
                m_log.Warn("[TERRAIN]: LoadWorldMap() - Failed with exception " + e.ToString() + " Regenerating");
#pragma warning disable 0162
                // Non standard region size.    If there's an old terrain in the database, it might read past the buffer
                if ((int)Constants.RegionSize != 256)
                {
                    m_channel = new TerrainChannel(m_scene);

                    m_scene.SimulationDataService.StoreTerrain(m_channel.GetDoubles(m_scene), m_scene.RegionInfo.RegionID, false);
                }
#pragma warning restore 0162
            }
            catch (Exception e)
            {
                m_log.Warn("[TERRAIN]: Scene.cs: LoadWorldMap() - Failed with exception " + e.ToString());
            }
            m_scene.RegisterModuleInterface<ITerrainChannel>(m_channel);
        }

        public void UndoTerrain(ITerrainChannel channel)
        {
            m_channel = channel;
        }

        /// <summary>
        /// Loads a terrain file from disk and installs it in the scene.
        /// </summary>
        /// <param name="filename">Filename to terrain file. Type is determined by extension.</param>
        public void LoadFromFile(string filename)
        {
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
            {
                if (filename.EndsWith(loader.Key))
                {
                    lock (m_scene)
                    {
                        try
                        {
                            ITerrainChannel channel = loader.Value.LoadFile(filename);
                            channel.Scene = m_scene;
                            if (channel.Width != Constants.RegionSize || channel.Height != Constants.RegionSize)
                            {
                                // TerrainChannel expects a RegionSize x RegionSize map, currently
                                throw new ArgumentException(String.Format("wrong size, use a file with size {0} x {1}",
                                                                          Constants.RegionSize, Constants.RegionSize));
                            }
                            m_log.DebugFormat("[TERRAIN]: Loaded terrain, wd/ht: {0}/{1}", channel.Width, channel.Height);
                            m_channel = channel;
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
                            ITerrainChannel channel = loader.Value.LoadStream(stream);
                            if (channel != null)
                            {
                                channel.Scene = m_scene;
                                m_channel = channel;
                                m_scene.RegisterModuleInterface<ITerrainChannel>(m_channel);
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
            m_painteffects[StandardTerrainEffects.Revert] = new RevertSphere(m_revert);
            m_painteffects[StandardTerrainEffects.Erode] = new ErodeSphere();
            m_painteffects[StandardTerrainEffects.Weather] = new WeatherSphere();
            m_painteffects[StandardTerrainEffects.Olsen] = new OlsenSphere();

            // Area of effect selection effects
            m_floodeffects[StandardTerrainEffects.Raise] = new RaiseArea();
            m_floodeffects[StandardTerrainEffects.Lower] = new LowerArea();
            m_floodeffects[StandardTerrainEffects.Smooth] = new SmoothArea();
            m_floodeffects[StandardTerrainEffects.Noise] = new NoiseArea();
            m_floodeffects[StandardTerrainEffects.Flatten] = new FlattenArea();
            m_floodeffects[StandardTerrainEffects.Revert] = new RevertArea(m_revert);

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
        /// Finds and updates the revert map from the database.
        /// </summary>
        public void FindRevertMap()
        {
            m_revert = LoadRevertMap();
        }

        /// <summary>
        /// Saves the current state of the region into the revert map buffer.
        /// </summary>
        public void UpdateRevertMap()
        {
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
                                                                            (int) Constants.RegionSize,
                                                                            (int) Constants.RegionSize);
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

        /// <summary>
        /// Performs updates to the region periodically, synchronising physics and other heightmap aware sections
        /// </summary>
        private void EventManager_OnFrame()
        {
            if ((m_scene.Frame & m_update_terrain) == 0)
            {
                //It's time
                if (m_tainted)
                {
                    m_tainted = false;
                    m_scene.PhysicsScene.SetTerrain(m_channel.GetFloatsSerialised(m_scene), m_channel.GetDoubles(m_scene));
                    SaveTerrain();
                }
            }
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
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnModifyTerrain -= client_OnModifyTerrain;
            client.OnBakeTerrain -= client_OnBakeTerrain;
            client.OnLandUndo -= client_OnLandUndo;
            client.OnGodlikeMessage -= client_onGodlikeMessage;
            client.OnUnackedTerrain -= client_OnUnackedTerrain;
            client.OnRegionHandShakeReply -= SendLayerData;
        }

        /// <summary>
        /// Send the region heightmap to the client
        /// </summary>
        /// <param name="RemoteClient">Client to send to</param>
        public void SendLayerData(IClientAPI RemoteClient)
        {
            Scene scene = (Scene)RemoteClient.Scene;
            RemoteClient.SendLayerData(m_channel.GetFloatsSerialised(scene));
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
            CheckForTerrainUpdates(false, false);
        }

        /// <summary>
        /// Checks to see if the terrain has been modified since last check.
        /// If it has been modified, every all the terrain patches are sent to the client.
        /// If the call is asked to respect the estate settings for terrain_raise_limit and
        /// terrain_lower_limit, it will clamp terrain updates between these values
        /// currently invoked by client_OnModifyTerrain only and not the Commander interfaces
        /// <param name="respectEstateSettings">should height map deltas be limited to the estate settings limits</param>
        /// </summary>
        private void CheckForTerrainUpdates(bool respectEstateSettings, bool forceSendOfTerrainInfo)
        {
            bool shouldTaint = false;
            float[] serialised = m_channel.GetFloatsSerialised(m_scene);
            int x;
            for (x = 0; x < m_channel.Width; x += Constants.TerrainPatchSize)
            {
                int y;
                for (y = 0; y < m_channel.Height; y += Constants.TerrainPatchSize)
                {
                    if (m_channel.Tainted(x, y) || forceSendOfTerrainInfo)
                    {
                        // if we should respect the estate settings then
                        // fixup and height deltas that don't respect them
                        if ((respectEstateSettings && LimitChannelChanges(x, y)) ||
                            LimitMaxTerrain(x, y) && !forceSendOfTerrainInfo)
                        {
                            // this has been vetoed, so update
                            // what we are going to send to the client
                            serialised = m_channel.GetFloatsSerialised(m_scene);
                        }

                        SendToClients(serialised, x, y);
                        shouldTaint = true;
                    }
                }
            }
            if (shouldTaint || forceSendOfTerrainInfo)
            {
                m_tainted = true;
            }
        }

        private bool LimitMaxTerrain(int xStart, int yStart)
        {
            bool changesLimited = false;

            // loop through the height map for this patch and compare it against
            // the revert map
            for (int x = xStart; x < xStart + Constants.TerrainPatchSize; x++)
            {
                for (int y = yStart; y < yStart + Constants.TerrainPatchSize; y++)
                {
                    double requestedHeight = m_channel[x, y];

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
        private bool LimitChannelChanges(int xStart, int yStart)
        {
            bool changesLimited = false;
            double minDelta = m_scene.RegionInfo.RegionSettings.TerrainLowerLimit;
            double maxDelta = m_scene.RegionInfo.RegionSettings.TerrainRaiseLimit;

            // loop through the height map for this patch and compare it against
            // the revert map
            for (int x = xStart; x < xStart + Constants.TerrainPatchSize; x++)
            {
                for (int y = yStart; y < yStart + Constants.TerrainPatchSize; y++)
                {

                    double requestedHeight = m_channel[x, y];
                    double bakedHeight = m_revert[x, y];
                    double requestedDelta = requestedHeight - bakedHeight;

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
        private void SendToClients(float[] serialised, int x, int y)
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
            bool allowed = false;
            if (north == south && east == west)
            {
                if (m_painteffects.ContainsKey((StandardTerrainEffects) action))
                {
                    bool[,] allowMask = new bool[m_channel.Width,m_channel.Height];
                    allowMask.Initialize();
                    int n = (int)BrushSize;

                    int zx = (int) (west + 0.5);
                    int zy = (int) (north + 0.5);

                    int dx;
                    for (dx=-n; dx<=n; dx++)
                    {
                        int dy;
                        for (dy=-n; dy<=n; dy++)
                        {
                            int x = zx + dx;
                            int y = zy + dy;
                            if (x>=0 && y>=0 && x<m_channel.Width && y<m_channel.Height)
                            {
                                if (m_scene.Permissions.CanTerraformLand(agentId, new Vector3(x,y,0)))
                                {
                                    allowMask[x, y] = true;
                                    allowed = true;
                                }
                            }
                        }
                    }
                    if (allowed)
                    {
                        StoreUndoState();
                        m_painteffects[(StandardTerrainEffects) action].PaintEffect(
                            m_channel, allowMask, west, south, height, size, seconds, BrushSize, m_scenes);

                        CheckForTerrainUpdates(!god, false); //revert changes outside estate limits
                    }
                }
                else
                {
                    m_log.Debug("Unknown terrain brush type " + action);
                }
            }
            else
            {
                if (m_floodeffects.ContainsKey((StandardTerrainEffects) action))
                {
                    bool[,] fillArea = new bool[m_channel.Width,m_channel.Height];
                    fillArea.Initialize();

                    int x;
                    for (x = 0; x < m_channel.Width; x++)
                    {
                        int y;
                        for (y = 0; y < m_channel.Height; y++)
                        {
                            if (x < east && x > west)
                            {
                                if (y < north && y > south)
                                {
                                    if (m_scene.Permissions.CanTerraformLand(agentId, new Vector3(x,y,0)))
                                    {
                                        fillArea[x, y] = true;
                                        allowed = true;
                                    }
                                }
                            }
                        }
                    }

                    if (allowed)
                    {
                        StoreUndoState();
                        m_floodeffects[(StandardTerrainEffects) action].FloodEffect(
                            m_channel, fillArea, size);

                        CheckForTerrainUpdates(!god, false); //revert changes outside estate limits
                    }
                }
                else
                {
                    m_log.Debug("Unknown terrain flood type " + action);
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
            client.SendLayerData(patchX, patchY, m_channel.GetFloatsSerialised(m_scene));
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

        private void InterfaceLoadFile(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            LoadFromFile(cmd[2]);
            CheckForTerrainUpdates();
        }

        private void InterfaceLoadTileFile(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            LoadFromFile((string)cmd[2],
                         int.Parse(cmd[3]),
                         int.Parse(cmd[4]),
                         int.Parse(cmd[5]),
                         int.Parse(cmd[6]));
            CheckForTerrainUpdates();
        }

        private void InterfaceSaveFile(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            SaveToFile((string)cmd[2]);
        }

        private void InterfaceBakeTerrain(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            UpdateRevertMap();
        }

        private void InterfaceRevertTerrain(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            int x, y;
            for (x = 0; x < m_channel.Width; x++)
                for (y = 0; y < m_channel.Height; y++)
                    m_channel[x, y] = m_revert[x, y];

            CheckForTerrainUpdates();
        }

        private void InterfaceFlipTerrain(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            String direction = cmd[2];

            if (direction.ToLower().StartsWith("y"))
            {
                for (int x = 0; x < Constants.RegionSize; x++)
                {
                    for (int y = 0; y < Constants.RegionSize / 2; y++)
                    {
                        double height = m_channel[x, y];
                        double flippedHeight = m_channel[x, (int)Constants.RegionSize - 1 - y];
                        m_channel[x, y] = flippedHeight;
                        m_channel[x, (int)Constants.RegionSize - 1 - y] = height;

                    }
                }
            }
            else if (direction.ToLower().StartsWith("x"))
            {
                for (int y = 0; y < Constants.RegionSize; y++)
                {
                    for (int x = 0; x < Constants.RegionSize / 2; x++)
                    {
                        double height = m_channel[x, y];
                        double flippedHeight = m_channel[(int)Constants.RegionSize - 1 - x, y];
                        m_channel[x, y] = flippedHeight;
                        m_channel[(int)Constants.RegionSize - 1 - x, y] = height;

                    }
                }
            }
            else
            {
                m_log.Error("Unrecognised direction - need x or y");
            }


            CheckForTerrainUpdates();
        }

        private void InterfaceRescaleTerrain(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            double desiredMin = double.Parse(cmd[2]);
            double desiredMax = double.Parse(cmd[3]);

            // determine desired scaling factor
            double desiredRange = desiredMax - desiredMin;
            //m_log.InfoFormat("Desired {0}, {1} = {2}", new Object[] { desiredMin, desiredMax, desiredRange });

            if (desiredRange == 0d)
            {
                // delta is zero so flatten at requested height
                InterfaceFillTerrain("", cmd);
            }
            else
            {
                //work out current heightmap range
                double currMin = double.MaxValue;
                double currMax = double.MinValue;

                int width = m_channel.Width;
                int height = m_channel.Height;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        double currHeight = m_channel[x, y];
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

                double currRange = currMax - currMin;
                double scale = desiredRange / currRange;

                //m_log.InfoFormat("Current {0}, {1} = {2}", new Object[] { currMin, currMax, currRange });
                //m_log.InfoFormat("Scale = {0}", scale);

                // scale the heightmap accordingly
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                            double currHeight = m_channel[x, y] - currMin;
                            m_channel[x, y] = desiredMin + (currHeight * scale);
                    }
                }

                CheckForTerrainUpdates();
            }

        }

        private void InterfaceElevateTerrain(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            int x, y;
            for (x = 0; x < m_channel.Width; x++)
                for (y = 0; y < m_channel.Height; y++)
                    m_channel[x, y] += double.Parse(cmd[2]);
            CheckForTerrainUpdates();
        }

        private void InterfaceMultiplyTerrain(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            int x, y;
            for (x = 0; x < m_channel.Width; x++)
                for (y = 0; y < m_channel.Height; y++)
                    m_channel[x, y] *= double.Parse(cmd[0]);
            CheckForTerrainUpdates();
        }

        private void InterfaceLowerTerrain(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            int x, y;
            for (x = 0; x < m_channel.Width; x++)
                for (y = 0; y < m_channel.Height; y++)
                    m_channel[x, y] -= double.Parse(cmd[2]);
            CheckForTerrainUpdates();
        }

        private void InterfaceFillTerrain(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            int x, y;

            for (x = 0; x < m_channel.Width; x++)
                for (y = 0; y < m_channel.Height; y++)
                    m_channel[x, y] = double.Parse(cmd[2]);
            CheckForTerrainUpdates();
        }

        private void InterfaceShowDebugStats(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            double max = Double.MinValue;
            double min = double.MaxValue;
            double sum = 0;

            int x;
            for (x = 0; x < m_channel.Width; x++)
            {
                int y;
                for (y = 0; y < m_channel.Height; y++)
                {
                    sum += m_channel[x, y];
                    if (max < m_channel[x, y])
                        max = m_channel[x, y];
                    if (min > m_channel[x, y])
                        min = m_channel[x, y];
                }
            }

            double avg = sum / (m_channel.Height * m_channel.Width);

            m_log.Info("Channel " + m_channel.Width + "x" + m_channel.Height);
            m_log.Info("max/min/avg/sum: " + max + "/" + min + "/" + avg + "/" + sum);
        }

        private void InterfaceEnableExperimentalBrushes(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            if (bool.Parse(cmd[2]))
            {
                m_painteffects[StandardTerrainEffects.Revert] = new WeatherSphere();
                m_painteffects[StandardTerrainEffects.Flatten] = new OlsenSphere();
                m_painteffects[StandardTerrainEffects.Smooth] = new ErodeSphere();
            }
            else
            {
                InstallDefaultEffects();
            }
        }

        private void InterfaceRunPluginEffect(string module, string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;
            if (cmd[2] == "list")
            {
                m_log.Info("List of loaded plugins");
                foreach (KeyValuePair<string, ITerrainEffect> kvp in m_plugineffects)
                {
                    m_log.Info(kvp.Key);
                }
                return;
            }
            if (cmd[2] == "reload")
            {
                LoadPlugins();
                return;
            }
            if (m_plugineffects.ContainsKey(cmd[2]))
            {
                m_plugineffects[cmd[2]].RunEffect(m_channel);
                CheckForTerrainUpdates();
            }
            else
            {
                m_log.Warn("No such plugin effect loaded.");
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

        private void InstallInterfaces()
        {
            // Load / Save
            string supportedFileExtensions = "";
            foreach (KeyValuePair<string, ITerrainLoader> loader in m_loaders)
                supportedFileExtensions += " " + loader.Key + " (" + loader.Value + ")";

            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain save",
                "terrain save <FileName>", "Saves the current heightmap to a specified file. FileName: The destination filename for your heightmap, the file extension determines the format to save in. Supported extensions include: " +
                                          supportedFileExtensions, InterfaceSaveFile);
            
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain load",
                "terrain load <FileName>", "Loads a terrain from a specified file. FileName: The file you wish to load from, the file extension determines the loader to be used. Supported extensions include: " +
                                            supportedFileExtensions, InterfaceLoadFile);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain load-tile",
                "terrain load-tile <file width> <file height> <minimum X tile> <minimum Y tile>",
                "Loads a terrain from a section of a larger file. " + 
            "\n file width: The width of the file in tiles" + 
            "\n file height: The height of the file in tiles" + 
            "\n minimum X tile: The X region coordinate of the first section on the file" + 
            "\n minimum Y tile: The Y region coordinate of the first section on the file", InterfaceLoadTileFile);

            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain fill",
                "terrain fill <value> ", "Fills the current heightmap with a specified value." +
                                            "\n value: The numeric value of the height you wish to set your region to.", InterfaceFillTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain elevate",
                "terrain elevate <amount> ", "Raises the current heightmap by the specified amount." +
                                            "\n amount: The amount of height to remove from the terrain in meters.", InterfaceElevateTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain lower",
                "terrain lower <amount> ", "Lowers the current heightmap by the specified amount." +
                                            "\n amount: The amount of height to remove from the terrain in meters.", InterfaceLowerTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain multiply",
                "terrain multiply <value> ", "Multiplies the heightmap by the value specified." +
                                            "\n value: The value to multiply the heightmap by.", InterfaceMultiplyTerrain);
            
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain bake",
                "terrain bake", "Saves the current terrain into the regions revert map.", InterfaceBakeTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain revert",
                "terrain revert", "Loads the revert map terrain into the regions heightmap.", InterfaceRevertTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain stats",
                "terrain stats", "Shows some information about the regions heightmap for debugging purposes.", InterfaceShowDebugStats);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain newbrushes",
                "terrain newbrushes <enabled> ", "Enables experimental brushes which replace the standard terrain brushes." +
                                            "\n enabled: true / false - Enable new brushes", InterfaceEnableExperimentalBrushes);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain effect",
                "terrain effect <name> ", "Runs a specified plugin effect" +
                                            "\n name: The plugin effect you wish to run, or 'list' to see all plugins", InterfaceRunPluginEffect);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain flip",
                "terrain flip <direction> ", "Flips the current terrain about the X or Y axis" +
                                            "\n direction: [x|y] the direction to flip the terrain in", InterfaceFlipTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain rescale",
                "terrain rescale <min> <max>", "Rescales the current terrain to fit between the given min and max heights" +
                                            "\n Min: min terrain height after rescaling" +
                                            "\n Max: max terrain height after rescaling", InterfaceRescaleTerrain);
            MainConsole.Instance.Commands.AddCommand("TerrainModule", false, "terrain help",
                "terrain help", "Gives help about the terrain module.", InterfaceHelp);
        }


        #endregion

    }
}
