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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using Nini.Config;
using Aurora.Framework;
using System.Timers;
using Timer = System.Timers.Timer;

namespace OpenSim.Region.Framework.Scenes
{
    /// <summary>
    /// Manager for adding, closing, reseting, and restarting scenes.
    /// </summary>
    public class SceneManager : ISceneManager, IApplicationPlugin
    {
        #region Declares

        #region Events

        public event NewScene OnAddedScene;
        public event NewScene OnCloseScene;

        #endregion

        private ISimulationBase m_OpenSimBase;
        private int RegionsFinishedStarting = 0;
        public int AllRegions { get; set; }

        protected ISimulationDataStore m_simulationDataService;
        public ISimulationDataStore SimulationDataService
        {
            get { return m_simulationDataService; }
            set { m_simulationDataService = value; }
        }

        private IConfigSource m_config = null;
        public IConfigSource ConfigSource
        {
            get { return m_config; }
        }

        private readonly List<IScene> m_localScenes = new List<IScene> ();

        public List<IScene> GetAllScenes() { return new List<IScene>(m_localScenes); }

        public IScene GetCurrentOrFirstScene()
        {
            if (MainConsole.Instance.ConsoleScene == null)
            {
                if (m_localScenes.Count > 0)
                {
                    return m_localScenes[0];
                }
                return null;
            }
            return MainConsole.Instance.ConsoleScene;
        }

        #endregion

        #region IApplicationPlugin members

        public void PreStartup(ISimulationBase simBase)
        {
            m_OpenSimBase = simBase;

            IConfig handlerConfig = simBase.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("SceneManager", "") != Name)
                return;

            m_config = simBase.ConfigSource;
            //Register us!
            m_OpenSimBase.ApplicationRegistry.RegisterModuleInterface<ISceneManager>(this);
        }

        public void Initialize(ISimulationBase simBase)
        {
            IConfig handlerConfig = simBase.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("SceneManager", "") != Name)
                return;

            string name = "FileBasedDatabase";
            // Try reading the [SimulationDataStore] section
            IConfig simConfig = simBase.ConfigSource.Configs["SimulationDataStore"];
            if (simConfig != null)
            {
                name = simConfig.GetString("DatabaseLoaderName", "FileBasedDatabase");
            }

            IConfig gridConfig = m_config.Configs["Configuration"];
            m_RegisterRegionPassword = Util.Md5Hash(gridConfig.GetString("RegisterRegionPassword", m_RegisterRegionPassword));

            ISimulationDataStore[] stores = AuroraModuleLoader.PickupModules<ISimulationDataStore> ().ToArray ();
            List<string> storeNames = new List<string>();
            foreach (ISimulationDataStore store in stores)
            {
                if (store.Name.ToLower() == name.ToLower())
                {
                    m_simulationDataService = store;
                    break;
                }
                storeNames.Add(store.Name);
            }

            if (m_simulationDataService == null)
            {
                MainConsole.Instance.ErrorFormat("[SceneManager]: FAILED TO LOAD THE SIMULATION SERVICE AT '{0}', ONLY OPTIONS ARE {1}, QUITING...", name, string.Join(", ", storeNames.ToArray()));
                Console.Read ();//Wait till they see
                Environment.Exit(0);
            }
            m_simulationDataService.Initialise();

            AddConsoleCommands();

            //Load the startup modules for the region
            m_startupPlugins = AuroraModuleLoader.PickupModules<ISharedRegionStartupModule>();

            m_OpenSimBase.EventManager.RegisterEventHandler("RegionInfoChanged", RegionInfoChanged);
        }

        public void ReloadConfiguration(IConfigSource config)
        {
            //Update this
            m_config = config;
            if (m_localScenes == null)
                return;
            foreach (IScene scene in m_localScenes)
            {
                scene.Config = config;
                scene.PhysicsScene.PostInitialise(config);
            }
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
        }

        public void PostStart()
        {
        }

        public string Name
        {
            get { return "SceneManager"; }
        }

        public void Dispose()
        {
        }

        public void Close()
        {
            if (m_localScenes == null)
                return;
            IScene[] scenes = new IScene[m_localScenes.Count];
            m_localScenes.CopyTo(scenes, 0);
            // collect known shared modules in sharedModules
            foreach (IScene t in scenes)
            {
                // close scene/region
                CloseRegion (t, ShutdownType.Immediate, 0);
            }
        }

        #endregion

        #region Startup complete

        public void HandleStartupComplete(IScene scene, List<string> data)
        {
            MainConsole.Instance.Info("[SceneManager]: Startup Complete in region " + scene.RegionInfo.RegionName);
            RegionsFinishedStarting++;
            if (RegionsFinishedStarting >= AllRegions)
                FinishStartUp();
        }

        private void FinishStartUp()
        {
            //Tell modules about it 
            StartupCompleteModules();

            m_OpenSimBase.RunStartupCommands();

            TimeSpan timeTaken = DateTime.Now - m_OpenSimBase.StartupTime;

            MainConsole.Instance.InfoFormat ("[SceneManager]: All regions are started. This took {0}m {1}.{2}s", timeTaken.Minutes, timeTaken.Seconds, timeTaken.Milliseconds);
            AuroraModuleLoader.ClearCache ();
            // In 99.9% of cases it is a bad idea to manually force garbage collection. However,
            // this is a rare case where we know we have just went through a long cycle of heap
            // allocations, and there is no more work to be done until someone logs in
            GC.Collect ();
        }

        #endregion

        #region ForEach functions

        public void ForEachCurrentScene(Action<IScene> func)
        {
            if (MainConsole.Instance.ConsoleScene == null)
            {
                m_localScenes.ForEach(func);
            }
            else
            {
                func (MainConsole.Instance.ConsoleScene);
            }
        }

        public void ForEachScene(Action<IScene> action)
        {
            m_localScenes.ForEach(action);
        }

        #endregion

        #region TrySetScene functions

        /// <summary>
        /// Checks to see whether a region with the given name exists, and then sets the MainConsole.Instance.ConsoleScene ref
        /// </summary>
        /// <param name="regionName"></param>
        /// <returns></returns>
        private bool TrySetConsoleScene(string regionName)
        {
            if ((String.Compare(regionName, "root") == 0))
            {
                MainConsole.Instance.ConsoleScene = null;
                return true;
            }
#if (!ISWIN)
            foreach (IScene scene in m_localScenes)
            {
                if (String.Compare(scene.RegionInfo.RegionName, regionName, true) == 0)
                {
                    MainConsole.Instance.ConsoleScene = scene;
                    return true;
                }
            }
#else
            foreach (IScene scene in m_localScenes.Where(scene => String.Compare(scene.RegionInfo.RegionName, regionName, true) == 0))
            {
                MainConsole.Instance.ConsoleScene = scene;
                return true;
            }
#endif

            return false;
        }

        /// <summary>
        /// Changes the console scene to a new region, also can pass 'root' down to set it to no console scene
        /// </summary>
        /// <param name="newRegionName"></param>
        /// <returns></returns>
        public bool ChangeConsoleRegion(string newRegionName)
        {
            if (!TrySetConsoleScene(newRegionName))
            {
                MainConsole.Instance.Info (String.Format ("Couldn't select region {0}", newRegionName));
                return false;
            }

            string regionName = (MainConsole.Instance.ConsoleScene == null ?
                "root" : MainConsole.Instance.ConsoleScene.RegionInfo.RegionName);
            if (MainConsole.Instance != null)
                MainConsole.Instance.DefaultPrompt = String.Format ("Region ({0}) ", regionName);
            return true;
        }

        #endregion

        #region TryGet functions

        /// <summary>
        /// Gets a region by its region name
        /// </summary>
        /// <param name="regionName"></param>
        /// <param name="scene"></param>
        /// <returns></returns>
        public bool TryGetScene (string regionName, out IScene scene)
        {
            foreach (IScene mscene in m_localScenes)
            {
                if (String.Compare(mscene.RegionInfo.RegionName, regionName, true) == 0)
                {
                    scene = mscene;
                    return true;
                }
            }
            scene = null;
            return false;
        }

        /// <summary>
        /// Gets a region by its region UUID
        /// </summary>
        /// <param name="regionID"></param>
        /// <param name="scene"></param>
        /// <returns></returns>
        public bool TryGetScene(UUID regionID, out IScene scene)
        {
            foreach (IScene mscene in m_localScenes)
            {
                if (mscene.RegionInfo.RegionID == regionID)
                {
                    scene = mscene;
                    return true;
                }
            }

            scene = null;
            return false;
        }

        /// <summary>
        /// Gets a region at the given location
        /// </summary>
        /// <param name="locX">In meters</param>
        /// <param name="locY">In meters</param>
        /// <param name="scene"></param>
        /// <returns></returns>
        public bool TryGetScene(int locX, int locY, out IScene scene)
        {
            foreach (IScene mscene in m_localScenes)
            {
                if (mscene.RegionInfo.RegionLocX == locX &&
                    mscene.RegionInfo.RegionLocY == locY)
                {
                    scene = mscene;
                    return true;
                }
            }

            scene = null;
            return false;
        }

        /// <summary>
        /// Gets a region at the given location
        /// </summary>
        /// <param name="RegionHandle"></param>
        /// <param name="scene"></param>
        /// <returns></returns>
        public bool TryGetScene(ulong RegionHandle, out IScene scene)
        {
            int X, Y;
            Util.UlongToInts (RegionHandle, out X, out Y);
            return TryGetScene (X, Y, out scene);
        }

        #endregion

        #region Add a region

        public IScene StartNewRegion (RegionInfo regionInfo)
        {
            MainConsole.Instance.InfoFormat("[SceneManager]: Starting region \"{0}\" at @ {1},{2}", regionInfo.RegionName,
                regionInfo.RegionLocX / 256, regionInfo.RegionLocY / 256);
            ISceneLoader sceneLoader = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<ISceneLoader> ();
            if (sceneLoader == null)
                throw new Exception ("No Scene Loader Interface!");

            //Get the new scene from the interface
            IScene scene = sceneLoader.CreateScene (regionInfo);
#if (!ISWIN)
            foreach (IScene loadedScene in m_localScenes)
            {
                if (loadedScene.RegionInfo.RegionName == regionInfo.RegionName && loadedScene.RegionInfo.RegionHandle == regionInfo.RegionHandle)
                {
                    throw new Exception("Duplicate region!");
                }
            }
#else
            if (m_localScenes.Any(loadedScene => loadedScene.RegionInfo.RegionName == regionInfo.RegionName &&
                                                 loadedScene.RegionInfo.RegionHandle == regionInfo.RegionHandle))
            {
                throw new Exception("Duplicate region!");
            }
#endif
            StartNewRegion (scene);
            return scene;
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public void StartNewRegion(IScene scene)
        {
            //Do this here so that we don't have issues later when startup complete messages start coming in
            m_localScenes.Add (scene);

            StartModules (scene);

            //Start the heartbeats
            scene.StartHeartbeat();
            //Tell the scene that the startup is complete 
            // Note: this event is added in the scene constructor
            scene.FinishedStartup("Startup", new List<string>());
        }

        /// <summary>
        /// Gets a new copy of the simulation data store, keep one per region
        /// </summary>
        /// <returns></returns>
        public ISimulationDataStore GetNewSimulationDataStore()
        {
            return m_simulationDataService.Copy();
        }

        #endregion

        #region Reset a region

        public void ResetRegion (IScene scene)
        {
            if (scene == null)
            {
                MainConsole.Instance.Warn("You must use this command on a region. Use 'change region' to change to the region you would like to change");
                return;
            }

            IBackupModule backup = scene.RequestModuleInterface<IBackupModule> ();
            if(backup != null)
                backup.DeleteAllSceneObjects();//Remove all the objects from the region
            ITerrainModule module = scene.RequestModuleInterface<ITerrainModule> ();
            if (module != null)
                module.ResetTerrain();//Then remove the terrain
            //Then reset the textures
            scene.RegionInfo.RegionSettings.TerrainTexture1 = RegionSettings.DEFAULT_TERRAIN_TEXTURE_1;
            scene.RegionInfo.RegionSettings.TerrainTexture2 = RegionSettings.DEFAULT_TERRAIN_TEXTURE_2;
            scene.RegionInfo.RegionSettings.TerrainTexture3 = RegionSettings.DEFAULT_TERRAIN_TEXTURE_3;
            scene.RegionInfo.RegionSettings.TerrainTexture4 = RegionSettings.DEFAULT_TERRAIN_TEXTURE_4;
            scene.RegionInfo.RegionSettings.Save ();
            MainConsole.Instance.Warn ("Region " + scene.RegionInfo.RegionName + " was reset");
        }

        #endregion

        #region Restart a region

        public void RestartRegion (IScene scene)
        {
            CloseRegion (scene, ShutdownType.Immediate, 0);
            StartNewRegion (scene.RegionInfo);
        }

        #endregion

        #region Shutdown regions

        /// <summary>
        /// Shuts down and permanently removes all info associated with the region
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="cleanup"></param>
        public void RemoveRegion (IScene scene, bool cleanup)
        {
            IBackupModule backup = scene.RequestModuleInterface<IBackupModule>();
            if (backup != null)
                backup.DeleteAllSceneObjects();

            scene.RegionInfo.HasBeenDeleted = true;
            CloseRegion (scene, ShutdownType.Immediate, 0);

            if (!cleanup)
                return;

            IRegionLoader[] loaders = m_OpenSimBase.ApplicationRegistry.RequestModuleInterfaces<IRegionLoader>();
            foreach (IRegionLoader loader in loaders)
            {
                loader.DeleteRegion(scene.RegionInfo);
            }
        }

        /// <summary>
        /// Shuts down a region and removes it from all running modules
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="type"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public void CloseRegion (IScene scene, ShutdownType type, int seconds)
        {
            if (type == ShutdownType.Immediate)
            {
                InnerCloseRegion (scene);
            }
            else
            {
                Timer t = new Timer (seconds * 1000);//Millisecond conversion
#if (!ISWIN)
                t.Elapsed +=
                    delegate(object sender, ElapsedEventArgs e)
                    {
                        CloseRegion(scene, ShutdownType.Immediate, 0);
                    };
#else
                t.Elapsed += (sender, e) => CloseRegion(scene, ShutdownType.Immediate, 0);
#endif
                t.AutoReset = false;
                t.Start ();
            }
        }

        private void InnerCloseRegion (IScene scene)
        {
            //Make sure that if we are set on the console, that we are removed from it
            if ((MainConsole.Instance.ConsoleScene != null) &&
                (MainConsole.Instance.ConsoleScene.RegionInfo.RegionID == scene.RegionInfo.RegionID))
                ChangeConsoleRegion ("root");

            m_localScenes.Remove (scene);
            scene.Close ();

            CloseModules (scene);
        }

        #endregion

        #region Update region info

        public object RegionInfoChanged(string funcName, object param)
        {
            UpdateRegionInfo((RegionInfo)((object[])param)[0], (RegionInfo)((object[])param)[1]);
            return null;
        }

        public void UpdateRegionInfo (RegionInfo oldRegion, RegionInfo region)
        {
            foreach(IScene scene in m_localScenes)
            {
                if(scene.RegionInfo.RegionID == region.RegionID)
                {
                    bool needsGridUpdate = 
                        scene.RegionInfo.RegionName != region.RegionName ||
                        scene.RegionInfo.RegionLocX != region.RegionLocX ||
                        scene.RegionInfo.RegionLocY != region.RegionLocY ||
                        scene.RegionInfo.RegionLocZ != region.RegionLocZ ||
                        scene.RegionInfo.AccessLevel != region.AccessLevel ||
                        scene.RegionInfo.RegionType != region.RegionType// ||
                        //scene.RegionInfo.RegionSizeX != region.RegionSizeX //Don't allow for size updates on the fly, that needs a restart
                        //scene.RegionInfo.RegionSizeY != region.RegionSizeY
                        //scene.RegionInfo.RegionSizeZ != region.RegionSizeZ
                    ;
                    bool needsRegistration = 
                        scene.RegionInfo.RegionName != region.RegionName || 
                        scene.RegionInfo.RegionLocX != region.RegionLocX ||
                        scene.RegionInfo.RegionLocY != region.RegionLocY;

                    region.RegionSettings = scene.RegionInfo.RegionSettings;
                    region.EstateSettings = scene.RegionInfo.EstateSettings;
                    region.GridSecureSessionID = scene.RegionInfo.GridSecureSessionID;
                    scene.RegionInfo = region;
                    if(needsRegistration)
                        scene.RequestModuleInterface<IGridRegisterModule>().RegisterRegionWithGrid(scene, false, false, m_RegisterRegionPassword);
                    else if(needsGridUpdate)
                        scene.RequestModuleInterface<IGridRegisterModule>().UpdateGridRegion(scene);
                    //Tell clients about the changes
                    IEstateModule es = scene.RequestModuleInterface<IEstateModule>();
                    if(es != null)
                        es.sendRegionHandshakeToAll();
                }
            }
        }

        #endregion

        #region ISharedRegionStartupModule plugins

        protected List<ISharedRegionStartupModule> m_startupPlugins = new List<ISharedRegionStartupModule> ();
        private string m_RegisterRegionPassword = "";

        protected void StartModules(IScene scene)
        {
            //Run all the initialization
            //First, Initialize the SharedRegionStartupModule
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.Initialise(scene, m_config, m_OpenSimBase);
            }
            //Then do the ISharedRegionModule and INonSharedRegionModules
            MainConsole.Instance.Debug ("[Modules]: Loading region modules");
            IRegionModulesController controller;
            if (m_OpenSimBase.ApplicationRegistry.TryRequestModuleInterface (out controller))
            {
                controller.AddRegionToModules (scene);
            }
            else
                MainConsole.Instance.Error ("[Modules]: The new RegionModulesController is missing...");
            //Then finish the rest of the SharedRegionStartupModules
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.PostInitialise(scene, m_config, m_OpenSimBase);
            }
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.FinishStartup(scene, m_config, m_OpenSimBase);
            }
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.PostFinishStartup(scene, m_config, m_OpenSimBase);
            }
            if (OnAddedScene != null)
                OnAddedScene (scene);
        }

        protected void StartupCompleteModules()
        {
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                try
                {
                    module.StartupComplete();
                }
                catch (Exception ex) { MainConsole.Instance.Warn("[SceneManager]: Exception running StartupComplete, " + ex); }
            }
        }

        protected void CloseModules(IScene scene)
        {
            IRegionModulesController controller;
            if (m_OpenSimBase.ApplicationRegistry.TryRequestModuleInterface(out controller))
                controller.RemoveRegionFromModules(scene);

            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.Close(scene);
            }
            if (OnCloseScene != null)
                OnCloseScene(scene);
        }

        public void DeleteRegion(UUID regionID)
        {
            IScene scene;
            if (TryGetScene(regionID, out scene))
                DeleteSceneFromModules(scene);
        }

        protected void DeleteSceneFromModules(IScene scene)
        {
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.DeleteRegion(scene);
            }
        }

        #endregion

        #region Console Commands

        private void AddConsoleCommands()
        {
            if (MainConsole.Instance == null)
                return;
            MainConsole.Instance.Commands.AddCommand ("show users", "show users [full]", "Shows users in the given region (if full is added, child agents are shown as well)", HandleShowUsers);
            MainConsole.Instance.Commands.AddCommand ("show regions", "show regions", "Show information about all regions in this instance", HandleShowRegions);
            MainConsole.Instance.Commands.AddCommand ("show maturity", "show maturity", "Show all region's maturity levels", HandleShowMaturity);

            MainConsole.Instance.Commands.AddCommand ("force update", "force update", "Force the update of all objects on clients", HandleForceUpdate);

            MainConsole.Instance.Commands.AddCommand("debug packet", "debug packet [level]", "Turn on packet debugging", Debug);
            MainConsole.Instance.Commands.AddCommand("debug scene", "debug scene [scripting] [collisions] [physics]", "Turn on scene debugging", Debug);

            MainConsole.Instance.Commands.AddCommand("change region", "change region [region name]", "Change current console region", ChangeSelectedRegion);

            MainConsole.Instance.Commands.AddCommand("load xml2", "load xml2", "Load a region's data from XML2 format", LoadXml2);

            MainConsole.Instance.Commands.AddCommand("save xml2", "save xml2", "Save a region's data in XML2 format", SaveXml2);

            MainConsole.Instance.Commands.AddCommand("load oar", "load oar [oar name] [--merge] [--skip-assets] [--OffsetX=#] [--OffsetY=#] [--OffsetZ=#] [--FlipX] [--FlipY] [--UseParcelOwnership] [--CheckOwnership]",
                "Load a region's data from OAR archive.  \n" +
                "--merge will merge the oar with the existing scene (including parcels).  \n" +
                "--skip-assets will load the oar but ignore the assets it contains. \n" +
                "--OffsetX will change where the X location of the oar is loaded, and the same for Y and Z.  \n" +
                "--FlipX flips the region on the X axis.  \n" +
                "--FlipY flips the region on the Y axis.  \n" +
                "--UseParcelOwnership changes who the default owner of objects whose owner cannot be found from the Estate Owner to the parcel owner on which the object is found.  \n" +
                "--CheckOwnership asks for each UUID that is not found on the grid what user it should be changed to (useful for changing UUIDs from other grids, but very long with many users).  ", LoadOar);

            MainConsole.Instance.Commands.AddCommand("save oar", "save oar [<OAR path>] [--perm=<permissions>] ", "Save a region's data to an OAR archive" + Environment.NewLine
                                           + "<OAR path> The OAR path must be a filesystem path."
                                           + "  If this is not given then the oar is saved to region.oar in the current directory." + Environment.NewLine
                                           + "--perm stops objects with insufficient permissions from being saved to the OAR." + Environment.NewLine
                                           + "  <permissions> can contain one or more of these characters: \"C\" = Copy, \"T\" = Transfer" + Environment.NewLine, SaveOar);

            MainConsole.Instance.Commands.AddCommand("kick user", "kick user [first] [last] [message]", "Kick a user off the simulator", KickUserCommand);

            MainConsole.Instance.Commands.AddCommand("reset region", "reset region", "Reset region to the default terrain, wipe all prims, etc.", RunCommand);

            MainConsole.Instance.Commands.AddCommand("restart-instance", "restart-instance", "Restarts the instance (as if you closed and re-opened Aurora)", RunCommand);

            MainConsole.Instance.Commands.AddCommand("command-script", "command-script [script]", "Run a command script from file", RunCommand);

            MainConsole.Instance.Commands.AddCommand("remove-region", "remove-region [name]", "Remove a region from this simulator", RunCommand);

            MainConsole.Instance.Commands.AddCommand("delete-region", "delete-region [name]", "Delete a region from disk", RunCommand);

            MainConsole.Instance.Commands.AddCommand ("modules list", "modules list", "Lists all simulator modules", HandleModulesList);

            MainConsole.Instance.Commands.AddCommand ("modules unload", "modules unload [module]", "Unload the given simulator module", HandleModulesUnload);
        }

        /// <summary>
        /// Kicks users off the region
        /// </summary>
        /// <param name="cmdparams">name of avatar to kick</param>
        private void KickUserCommand(string[] cmdparams)
        {
            string alert = null;
            IList agents = new List<IScenePresence>(GetCurrentOrFirstScene().GetScenePresences());

            if (cmdparams.Length < 4)
            {
                if (cmdparams.Length < 3)
                    return;
                UUID avID = UUID.Zero;
                if (cmdparams[2] == "all")
                {
                    foreach (IScenePresence presence in agents)
                    {
                        RegionInfo regionInfo = presence.Scene.RegionInfo;

                        MainConsole.Instance.Info (String.Format ("Kicking user: {0,-16}{1,-37} in region: {2,-16}", presence.Name, presence.UUID, regionInfo.RegionName));

                        // kick client...
                        presence.ControllingClient.Kick(alert ?? "\nThe Aurora manager kicked you out.\n");

                        // ...and close on our side
                        IEntityTransferModule transferModule = presence.Scene.RequestModuleInterface<IEntityTransferModule> ();
                        if(transferModule != null)
                            transferModule.IncomingCloseAgent (presence.Scene, presence.UUID);
                    }
                }
                else if(UUID.TryParse(cmdparams[2], out avID))
                {
                    foreach (IScenePresence presence in agents)
                    {
                        if (presence.UUID == avID)
                        {
                            RegionInfo regionInfo = presence.Scene.RegionInfo;

                            MainConsole.Instance.Info (String.Format ("Kicking user: {0,-16}{1,-37} in region: {2,-16}", presence.Name, presence.UUID, regionInfo.RegionName));

                            // kick client...
                            presence.ControllingClient.Kick(alert ?? "\nThe Aurora manager kicked you out.\n");

                            // ...and close on our side
                            IEntityTransferModule transferModule = presence.Scene.RequestModuleInterface<IEntityTransferModule> ();
                            if (transferModule != null)
                                transferModule.IncomingCloseAgent (presence.Scene, presence.UUID);
                        }
                    }
                }
            }

            if (cmdparams.Length > 4)
                alert = String.Format("\n{0}\n", String.Join(" ", cmdparams, 4, cmdparams.Length - 4));

            foreach (IScenePresence presence in agents)
            {
                RegionInfo regionInfo = presence.Scene.RegionInfo;
                string param = Util.CombineParams (cmdparams, 2);
                if (presence.Name.ToLower().Contains (param.ToLower ()) ||
                    (presence.Firstname.ToLower ().Contains (cmdparams[2].ToLower ()) && presence.Lastname.ToLower ().Contains (cmdparams[3].ToLower ())))
                {
                    MainConsole.Instance.Info (String.Format ("Kicking user: {0,-16}{1,-37} in region: {2,-16}", presence.Name, presence.UUID, regionInfo.RegionName));

                    // kick client...
                    presence.ControllingClient.Kick(alert ?? "\nThe Aurora manager kicked you out.\n");

                    // ...and close on our side
                    IEntityTransferModule transferModule = presence.Scene.RequestModuleInterface<IEntityTransferModule> ();
                    if (transferModule != null)
                        transferModule.IncomingCloseAgent (presence.Scene, presence.UUID);
                }
            }
            MainConsole.Instance.Info ("");
        }

        /// <summary>
        /// Force resending of all updates to all clients in active region(s)
        /// </summary>
        /// <param name="args"></param>
        private void HandleForceUpdate(string[] args)
        {
            MainConsole.Instance.Info ("Updating all clients");
#if (!ISWIN)
            ForEachCurrentScene(delegate(IScene scene)
            {
                ISceneEntity[] EntityList = scene.Entities.GetEntities ();

                foreach (ISceneEntity ent in EntityList)
                {
                    if (ent is SceneObjectGroup)
                    {
                        ((SceneObjectGroup)ent).ScheduleGroupUpdate (PrimUpdateFlags.ForcedFullUpdate);
                    }
                }
                List<IScenePresence> presences = scene.Entities.GetPresences ();

                foreach(IScenePresence presence in presences)
                {
                    if(!presence.IsChildAgent)
                        scene.ForEachClient(delegate(IClientAPI client)
                        {
                            client.SendAvatarDataImmediate(presence);
                        });
                }
            });
#else
            ForEachCurrentScene(scene =>
                                    {
                                        ISceneEntity[] EntityList = scene.Entities.GetEntities();

                                        foreach (SceneObjectGroup ent in EntityList.OfType<SceneObjectGroup>())
                                        {
                                            (ent).ScheduleGroupUpdate(
                                                PrimUpdateFlags.ForcedFullUpdate);
                                        }
                                        List<IScenePresence> presences = scene.Entities.GetPresences();

                                        foreach (IScenePresence presence in presences.Where(presence => !presence.IsChildAgent))
                                        {
                                            IScenePresence presence1 = presence;
                                            scene.ForEachClient(
                                                client => client.SendAvatarDataImmediate(presence1));
                                        }
                                    });
#endif
        }

        /// <summary>
        /// Load, Unload, and list Region modules in use
        /// </summary>
        /// <param name="cmd"></param>
        private void HandleModulesUnload(string[] cmd)
        {
            List<string> args = new List<string> (cmd);
            args.RemoveAt (0);
            string[] cmdparams = args.ToArray ();

            IRegionModulesController controller = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<IRegionModulesController> ();
            if (cmdparams.Length > 1)
            {
                foreach (IRegionModuleBase irm in controller.AllModules)
                {
                    if (irm.Name.ToLower () == cmdparams[1].ToLower ())
                    {
                        MainConsole.Instance.Info (String.Format ("Unloading module: {0}", irm.Name));
                        foreach (IScene scene in m_localScenes)
                            irm.RemoveRegion (scene);
                        irm.Close ();
                    }
                }
            }
        }

        /// <summary>
        /// Load, Unload, and list Region modules in use
        /// </summary>
        /// <param name="cmd"></param>
        private void HandleModulesList (string[] cmd)
        {
            List<string> args = new List<string> (cmd);
            args.RemoveAt (0);
            
            IRegionModulesController controller = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<IRegionModulesController> ();
            foreach (IRegionModuleBase irm in controller.AllModules)
            {
                if (irm is ISharedRegionModule)
                    MainConsole.Instance.Info (String.Format ("Shared region module: {0}", irm.Name));
                else if (irm is INonSharedRegionModule)
                    MainConsole.Instance.Info (String.Format ("Nonshared region module: {0}", irm.Name));
                else
                    MainConsole.Instance.Info (String.Format ("Unknown type " + irm.GetType () + " region module: {0}", irm.Name));
            }
        }

        /// <summary>
        /// Serialize region data to XML2Format
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void SaveXml2(string[] cmdparams)
        {
            if (cmdparams.Length > 2)
            {
                IRegionSerialiserModule serialiser = GetCurrentOrFirstScene().RequestModuleInterface<IRegionSerialiserModule>();
                if (serialiser != null)
                    serialiser.SavePrimsToXml2(GetCurrentOrFirstScene(), cmdparams[2]);
            }
            else
            {
                MainConsole.Instance.Warn("Wrong number of parameters!");
            }
        }

        /// <summary>
        /// Runs commands issued by the server console from the operator
        /// </summary>
        /// <param name="cmdparams">Additional arguments passed to the command</param>
        private void RunCommand (string[] cmdparams)
        {
            List<string> args = new List<string>(cmdparams);
            if (args.Count < 1)
                return;

            string command = args[0];
            args.RemoveAt(0);

            cmdparams = args.ToArray();

            switch (command)
            {
                case "reset":
                    if (cmdparams.Length > 0)
                        if (cmdparams[0] == "region")
                        {
                            if (MainConsole.Instance.Prompt ("Are you sure you want to reset the region?", "yes") != "yes")
                                return;
                            ResetRegion (MainConsole.Instance.ConsoleScene);
                        }
                    break;
                case "command-script":
                    if (cmdparams.Length > 0)
                    {
                        m_OpenSimBase.RunCommandScript(cmdparams[0]);
                    }
                    break;

                case "remove-region":
                    string regRemoveName = Util.CombineParams(cmdparams, 0);

                    IScene removeScene;
                    if (TryGetScene(regRemoveName, out removeScene))
                        RemoveRegion(removeScene, false);
                    else
                        MainConsole.Instance.Info ("no region with that name");
                    break;

                case "delete-region":
                    string regDeleteName = Util.CombineParams(cmdparams, 0);

                    IScene killScene;
                    if (TryGetScene(regDeleteName, out killScene))
                        RemoveRegion(killScene, true);
                    else
                        MainConsole.Instance.Info ("no region with that name");
                    break;
                case "restart-instance":
                    //This kills the instance and restarts it
                    MainConsole.Instance.EndConsoleProcessing();
                    break;
            }
        }

        /// <summary>
        /// Change the currently selected region.  The selected region is that operated upon by single region commands.
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void ChangeSelectedRegion(string[] cmdparams)
        {
            if (cmdparams.Length > 2)
            {
                string newRegionName = Util.CombineParams(cmdparams, 2);
                ChangeConsoleRegion(newRegionName);
            }
            else
            {
                MainConsole.Instance.Info ("Usage: change region <region name>");
            }
        }

        /// <summary>
        /// Turn on some debugging values for OpenSim.
        /// </summary>
        /// <param name="args"></param>
        protected void Debug(string[] args)
        {
            if (args.Length == 1)
                return;

            switch (args[1])
            {
                case "packet":
                    if (args.Length > 2)
                    {
                        int newDebug;
                        if (int.TryParse(args[2], out newDebug))
                        {
                            SetDebugPacketLevelOnCurrentScene(newDebug);
                        }
                        else
                        {
                            MainConsole.Instance.Info ("packet debug should be 0..255");
                        }
                        MainConsole.Instance.Info (String.Format ("New packet debug: {0}", newDebug));
                    }

                    break;
                default:

                    MainConsole.Instance.Info ("Unknown debug");
                    break;
            }
        }

        /// <summary>
        /// Set the debug packet level on the current scene.  This level governs which packets are printed out to the
        /// console.
        /// </summary>
        /// <param name="newDebug"></param>
        private void SetDebugPacketLevelOnCurrentScene(int newDebug)
        {
#if (!ISWIN)
            ForEachCurrentScene(
                delegate(IScene scene)
                {
                    scene.ForEachScenePresence (delegate (IScenePresence scenePresence)
                    {
                        if (!scenePresence.IsChildAgent)
                        {
                            MainConsole.Instance.DebugFormat("Packet debug for {0} set to {1}",
                                              scenePresence.Name,
                                              newDebug);

                            scenePresence.ControllingClient.SetDebugPacketLevel(newDebug);
                        }
                    });
                }
            );
#else
            ForEachCurrentScene(
                scene => scene.ForEachScenePresence(scenePresence =>
                                                        {
                                                            if (scenePresence.IsChildAgent) return;
                                                            MainConsole.Instance.DebugFormat(
                                                                "Packet debug for {0} set to {1}",
                                                                scenePresence.Name,
                                                                newDebug);

                                                            scenePresence.ControllingClient.SetDebugPacketLevel(
                                                                newDebug);
                                                        })
                );
#endif

        }

        private void HandleShowUsers (string[] cmd)
        {
            List<string> args = new List<string> (cmd);
            args.RemoveAt (0);
            string[] showParams = args.ToArray ();

            List<IScenePresence> agents = new List<IScenePresence>();
            if(showParams.Length > 1 && showParams[1] == "full")
            {
                if(MainConsole.Instance.ConsoleScene == null)
                {
                    foreach(IScene scene in m_localScenes)
                    {
                        agents.AddRange(scene.GetScenePresences());
                    }
                }
                else
                    agents = GetCurrentOrFirstScene().GetScenePresences();
            }
            else
            {
                if(MainConsole.Instance.ConsoleScene == null)
                {
                    foreach(IScene scene in m_localScenes)
                    {
                        agents.AddRange(scene.GetScenePresences());
                    }
                }
                else
                    agents = GetCurrentOrFirstScene().GetScenePresences();
#if (!ISWIN)
                agents.RemoveAll(delegate(IScenePresence sp)
                {
                    return sp.IsChildAgent;
                });
#else
                agents.RemoveAll(sp => sp.IsChildAgent);
#endif
            }

            MainConsole.Instance.Info (String.Format ("\nAgents connected: {0}\n", agents.Count));

            MainConsole.Instance.Info (String.Format ("{0,-16}{1,-16}{2,-37}{3,-11}{4,-16}{5,-30}", "Firstname", "Lastname", "Agent ID", "Root/Child", "Region", "Position"));

            foreach (IScenePresence presence in agents)
            {
                RegionInfo regionInfo = presence.Scene.RegionInfo;

                string regionName = regionInfo == null ? "Unresolvable" : regionInfo.RegionName;

                MainConsole.Instance.Info (String.Format ("{0,-16}{1,-37}{2,-11}{3,-16}{4,-30}", presence.Name, presence.UUID, presence.IsChildAgent ? "Child" : "Root", regionName, presence.AbsolutePosition.ToString ()));
            }

            MainConsole.Instance.Info (String.Empty);
            MainConsole.Instance.Info (String.Empty);
        }

        private void HandleShowRegions (string[] cmd)
        {
#if (!ISWIN)
            ForEachScene (delegate (IScene scene)
            {
                MainConsole.Instance.Info (scene.ToString ());
            });
#else
            ForEachScene(scene => MainConsole.Instance.Info(scene.ToString()));
#endif
        }

        private void HandleShowMaturity (string[] cmd)
        {
            ForEachCurrentScene (delegate (IScene scene)
            {
                string rating = "";
                if (scene.RegionInfo.RegionSettings.Maturity == 1)
                {
                    rating = "Mature";
                }
                else if (scene.RegionInfo.RegionSettings.Maturity == 2)
                {
                    rating = "Adult";
                }
                else
                {
                    rating = "PG";
                }
                MainConsole.Instance.Info (String.Format ("Region Name: {0}, Region Rating {1}", scene.RegionInfo.RegionName, rating));
            });
        }

        /// <summary>
        /// Load region data from Xml2Format
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void LoadXml2(string[] cmdparams)
        {
            if (cmdparams.Length > 2)
            {
                try
                {
                    IRegionSerialiserModule serialiser = GetCurrentOrFirstScene().RequestModuleInterface<IRegionSerialiserModule>();
                    if (serialiser != null)
                        serialiser.LoadPrimsFromXml2(GetCurrentOrFirstScene(), cmdparams[2]);
                }
                catch (FileNotFoundException)
                {
                    MainConsole.Instance.Info ("Specified xml not found. Usage: load xml2 <filename>");
                }
            }
            else
            {
                MainConsole.Instance.Warn("Not enough parameters!");
            }
        }

        /// <summary>
        /// Load a whole region from an opensimulator archive.
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void LoadOar(string[] cmdparams)
        {
            try
            {
                IRegionArchiverModule archiver = GetCurrentOrFirstScene().RequestModuleInterface<IRegionArchiverModule>();
                if (archiver != null)
                    archiver.HandleLoadOarConsoleCommand(cmdparams);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error (e.ToString ());
            }
        }

        /// <summary>
        /// Save a region to a file, including all the assets needed to restore it.
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void SaveOar(string[] cmdparams)
        {
            IRegionArchiverModule archiver = GetCurrentOrFirstScene().RequestModuleInterface<IRegionArchiverModule>();
            if (archiver != null)
                archiver.HandleSaveOarConsoleCommand(cmdparams);
        }

        #endregion
    }
}
