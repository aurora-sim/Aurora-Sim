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

using Aurora.Framework;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Timer = System.Timers.Timer;

namespace Aurora.Region
{
    /// <summary>
    ///     Manager for adding, closing, reseting, and restarting scenes.
    /// </summary>
    public class SceneManager : ISceneManager, IApplicationPlugin
    {
        #region Static Constructor

        static SceneManager()
        {
            Aurora.Framework.Serialization.SceneEntitySerializer.SceneObjectSerializer =
                new Aurora.Region.Serialization.SceneObjectSerializer();
        }

        #endregion

        #region Declares

        public event NewScene OnCloseScene;
        public event NewScene OnAddedScene;

        protected ISimulationBase m_OpenSimBase;
        protected IScene m_scene;

        public IScene Scene
        {
            get { return m_scene; }
        }

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

            ISimulationDataStore[] stores = AuroraModuleLoader.PickupModules<ISimulationDataStore>().ToArray();
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
                MainConsole.Instance.ErrorFormat(
                    "[SceneManager]: FAILED TO LOAD THE SIMULATION SERVICE AT '{0}', ONLY OPTIONS ARE {1}, QUITING...",
                    name, string.Join(", ", storeNames.ToArray()));
                Console.Read(); //Wait till they see
                Environment.Exit(0);
            }
            m_simulationDataService.Initialise();

            AddConsoleCommands();

            //Load the startup modules for the region
            m_startupPlugins = AuroraModuleLoader.PickupModules<ISharedRegionStartupModule>();
        }

        public void ReloadConfiguration(IConfigSource config)
        {
            //Update this
            m_config = config;
            m_scene.Config = config;
            m_scene.PhysicsScene.PostInitialise(config);
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
        }

        public void PostStart()
        {
            if (m_simulationDataService == null)
                return;

            bool newRegion = false;
            StartRegion(out newRegion);
            MainConsole.Instance.DefaultPrompt = "Region ";
            if (newRegion) //Save the new info
                m_simulationDataService.ForceBackup();
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
            if (m_scene == null)
                return;
            CloseRegion(ShutdownType.Immediate, 0);
        }

        #endregion

        #region Startup complete

        public void HandleStartupComplete(List<string> data)
        {
            //Tell modules about it 
            StartupCompleteModules();
            m_OpenSimBase.RunStartupCommands();

            TimeSpan timeTaken = DateTime.Now - m_OpenSimBase.StartupTime;

            MainConsole.Instance.InfoFormat(
                "[SceneManager]: Startup Complete for region " + m_scene.RegionInfo.RegionName +
                ". This took {0}m {1}.{2}s",
                timeTaken.Minutes, timeTaken.Seconds, timeTaken.Milliseconds);

            AuroraModuleLoader.ClearCache();
            // In 99.9% of cases it is a bad idea to manually force garbage collection. However,
            // this is a rare case where we know we have just went through a long cycle of heap
            // allocations, and there is no more work to be done until someone logs in
            GC.Collect();
        }

        #endregion

        #region Add a region

        public void StartRegion(out bool newRegion)
        {
            RegionInfo regionInfo = m_simulationDataService.LoadRegionInfo(m_OpenSimBase, out newRegion);
            MainConsole.Instance.InfoFormat("[SceneManager]: Starting region \"{0}\" at @ {1},{2}",
                                            regionInfo.RegionName,
                                            regionInfo.RegionLocX/256, regionInfo.RegionLocY/256);
            ISceneLoader sceneLoader = m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<ISceneLoader>();
            if (sceneLoader == null)
                throw new Exception("No Scene Loader Interface!");

            //Get the new scene from the interface
            m_scene = sceneLoader.CreateScene(regionInfo);

            MainConsole.Instance.ConsoleScene = m_scene;
            m_simulationDataService.SetRegion(m_scene);

            if (OnAddedScene != null)
                OnAddedScene(m_scene);

            StartModules(m_scene);

            //Start the heartbeats
            m_scene.StartHeartbeat();
            //Tell the scene that the startup is complete 
            // Note: this event is added in the scene constructor
            m_scene.FinishedStartup("Startup", new List<string>());
        }

        /// <summary>
        ///     Gets a new copy of the simulation data store, keep one per region
        /// </summary>
        /// <returns></returns>
        public ISimulationDataStore GetSimulationDataStore()
        {
            return m_simulationDataService;
        }

        #endregion

        #region Reset a region

        public void ResetRegion()
        {
            IBackupModule backup = m_scene.RequestModuleInterface<IBackupModule>();
            if (backup != null)
                backup.DeleteAllSceneObjects(); //Remove all the objects from the region
            ITerrainModule module = m_scene.RequestModuleInterface<ITerrainModule>();
            if (module != null)
                module.ResetTerrain(); //Then remove the terrain
            //Then reset the textures
            m_scene.RegionInfo.RegionSettings.TerrainTexture1 = RegionSettings.DEFAULT_TERRAIN_TEXTURE_1;
            m_scene.RegionInfo.RegionSettings.TerrainTexture2 = RegionSettings.DEFAULT_TERRAIN_TEXTURE_2;
            m_scene.RegionInfo.RegionSettings.TerrainTexture3 = RegionSettings.DEFAULT_TERRAIN_TEXTURE_3;
            m_scene.RegionInfo.RegionSettings.TerrainTexture4 = RegionSettings.DEFAULT_TERRAIN_TEXTURE_4;
            MainConsole.Instance.Warn("Region " + m_scene.RegionInfo.RegionName + " was reset");
        }

        #endregion

        #region Restart a region

        public void RestartRegion()
        {
            CloseRegion(ShutdownType.Immediate, 0);
            bool newRegion;
            StartRegion(out newRegion);
        }

        #endregion

        #region Shutdown regions

        /// <summary>
        ///     Shuts down and permanently removes all info associated with the region
        /// </summary>
        /// <param name="cleanup"></param>
        public void RemoveRegion(bool cleanup)
        {
            IBackupModule backup = m_scene.RequestModuleInterface<IBackupModule>();
            if (backup != null)
                backup.DeleteAllSceneObjects();

            m_scene.RegionInfo.HasBeenDeleted = true;
            CloseRegion(ShutdownType.Immediate, 0);

            if (!cleanup)
                return;

            IRegionLoader[] loaders = m_OpenSimBase.ApplicationRegistry.RequestModuleInterfaces<IRegionLoader>();
            foreach (IRegionLoader loader in loaders)
            {
                loader.DeleteRegion(m_scene.RegionInfo);
            }
        }

        /// <summary>
        ///     Shuts down a region and removes it from all running modules
        /// </summary>
        /// <param name="type"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public void CloseRegion(ShutdownType type, int seconds)
        {
            if (type == ShutdownType.Immediate)
            {
                m_scene.Close();
                if (OnCloseScene != null)
                    OnCloseScene(m_scene);
                CloseModules();
            }
            else
            {
                Timer t = new Timer(seconds*1000); //Millisecond conversion
                t.Elapsed += (sender, e) => CloseRegion(ShutdownType.Immediate, 0);
                t.AutoReset = false;
                t.Start();
            }
        }

        #endregion

        #region ISharedRegionStartupModule plugins

        protected List<ISharedRegionStartupModule> m_startupPlugins = new List<ISharedRegionStartupModule>();

        protected void StartModules(IScene scene)
        {
            //Run all the initialization
            //First, Initialize the SharedRegionStartupModule
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.Initialise(scene, m_config, m_OpenSimBase);
            }
            //Then do the ISharedRegionModule and INonSharedRegionModules
            MainConsole.Instance.Debug("[Modules]: Loading region modules");
            IRegionModulesController controller;
            if (m_OpenSimBase.ApplicationRegistry.TryRequestModuleInterface(out controller))
            {
                controller.AddRegionToModules(scene);
            }
            else
                MainConsole.Instance.Error("[Modules]: The new RegionModulesController is missing...");
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
        }

        protected void StartupCompleteModules()
        {
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                try
                {
                    module.StartupComplete();
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.Warn("[SceneManager]: Exception running StartupComplete, " + ex);
                }
            }
        }

        protected void CloseModules()
        {
            IRegionModulesController controller;
            if (m_OpenSimBase.ApplicationRegistry.TryRequestModuleInterface(out controller))
                controller.RemoveRegionFromModules(m_scene);

            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.Close(m_scene);
            }
        }

        public void DeleteRegion(UUID regionID)
        {
            foreach (ISharedRegionStartupModule module in m_startupPlugins)
            {
                module.DeleteRegion(m_scene);
            }
        }

        #endregion

        #region Console Commands

        private void AddConsoleCommands()
        {
            if (MainConsole.Instance == null)
                return;
            MainConsole.Instance.Commands.AddCommand("show users", "show users [full]",
                                                     "Shows users in the given region (if full is added, child agents are shown as well)",
                                                     HandleShowUsers);
            MainConsole.Instance.Commands.AddCommand("show regions", "show regions",
                                                     "Show information about all regions in this instance",
                                                     HandleShowRegions);
            MainConsole.Instance.Commands.AddCommand("show maturity", "show maturity",
                                                     "Show all region's maturity levels", HandleShowMaturity);

            MainConsole.Instance.Commands.AddCommand("force update", "force update",
                                                     "Force the update of all objects on clients", HandleForceUpdate);

            MainConsole.Instance.Commands.AddCommand("debug packet", "debug packet [level]", "Turn on packet debugging",
                                                     Debug);
            MainConsole.Instance.Commands.AddCommand("debug scene", "debug scene [scripting] [collisions] [physics]",
                                                     "Turn on scene debugging", Debug);

            MainConsole.Instance.Commands.AddCommand("load oar",
                                                     "load oar [oar name] [--merge] [--skip-assets] [--OffsetX=#] [--OffsetY=#] [--OffsetZ=#] [--FlipX] [--FlipY] [--UseParcelOwnership] [--CheckOwnership]",
                                                     "Load a region's data from OAR archive.  \n" +
                                                     "--merge will merge the oar with the existing scene (including parcels).  \n" +
                                                     "--skip-assets will load the oar but ignore the assets it contains. \n" +
                                                     "--OffsetX will change where the X location of the oar is loaded, and the same for Y and Z.  \n" +
                                                     "--FlipX flips the region on the X axis.  \n" +
                                                     "--FlipY flips the region on the Y axis.  \n" +
                                                     "--UseParcelOwnership changes who the default owner of objects whose owner cannot be found from the Estate Owner to the parcel owner on which the object is found.  \n" +
                                                     "--CheckOwnership asks for each UUID that is not found on the grid what user it should be changed to (useful for changing UUIDs from other grids, but very long with many users).  ",
                                                     LoadOar);

            MainConsole.Instance.Commands.AddCommand("save oar", "save oar [<OAR path>] [--perm=<permissions>] ",
                                                     "Save a region's data to an OAR archive" + Environment.NewLine
                                                     + "<OAR path> The OAR path must be a filesystem path."
                                                     +
                                                     "  If this is not given then the oar is saved to region.oar in the current directory." +
                                                     Environment.NewLine
                                                     +
                                                     "--perm stops objects with insufficient permissions from being saved to the OAR." +
                                                     Environment.NewLine
                                                     +
                                                     "  <permissions> can contain one or more of these characters: \"C\" = Copy, \"T\" = Transfer" +
                                                     Environment.NewLine, SaveOar);

            MainConsole.Instance.Commands.AddCommand("kick user", "kick user [first] [last] [message]",
                                                     "Kick a user off the simulator", KickUserCommand);

            MainConsole.Instance.Commands.AddCommand("reset region", "reset region",
                                                     "Reset region to the default terrain, wipe all prims, etc.",
                                                     RunCommand);

            MainConsole.Instance.Commands.AddCommand("restart-instance", "restart-instance",
                                                     "Restarts the instance (as if you closed and re-opened Aurora)",
                                                     RunCommand);

            MainConsole.Instance.Commands.AddCommand("command-script", "command-script [script]",
                                                     "Run a command script from file", RunCommand);

            MainConsole.Instance.Commands.AddCommand("modules list", "modules list", "Lists all simulator modules",
                                                     HandleModulesList);

            MainConsole.Instance.Commands.AddCommand("modules unload", "modules unload [module]",
                                                     "Unload the given simulator module", HandleModulesUnload);
        }

        /// <summary>
        ///     Kicks users off the region
        /// </summary>
        /// <param name="cmdparams">name of avatar to kick</param>
        private void KickUserCommand(string[] cmdparams)
        {
            string alert = null;
            IList agents = new List<IScenePresence>(m_scene.GetScenePresences());

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

                        MainConsole.Instance.Info(String.Format("Kicking user: {0,-16}{1,-37} in region: {2,-16}",
                                                                presence.Name, presence.UUID, regionInfo.RegionName));

                        // kick client...
                        presence.ControllingClient.Kick(alert ?? "\nThe Aurora manager kicked you out.\n");

                        // ...and close on our side
                        IEntityTransferModule transferModule =
                            presence.Scene.RequestModuleInterface<IEntityTransferModule>();
                        if (transferModule != null)
                            transferModule.IncomingCloseAgent(presence.Scene, presence.UUID);
                    }
                }
                else if (UUID.TryParse(cmdparams[2], out avID))
                {
                    foreach (IScenePresence presence in agents)
                    {
                        if (presence.UUID == avID)
                        {
                            RegionInfo regionInfo = presence.Scene.RegionInfo;

                            MainConsole.Instance.Info(String.Format("Kicking user: {0,-16}{1,-37} in region: {2,-16}",
                                                                    presence.Name, presence.UUID, regionInfo.RegionName));

                            // kick client...
                            presence.ControllingClient.Kick(alert ?? "\nThe Aurora manager kicked you out.\n");

                            // ...and close on our side
                            IEntityTransferModule transferModule =
                                presence.Scene.RequestModuleInterface<IEntityTransferModule>();
                            if (transferModule != null)
                                transferModule.IncomingCloseAgent(presence.Scene, presence.UUID);
                        }
                    }
                }
            }

            if (cmdparams.Length > 4)
                alert = String.Format("\n{0}\n", String.Join(" ", cmdparams, 4, cmdparams.Length - 4));

            foreach (IScenePresence presence in agents)
            {
                RegionInfo regionInfo = presence.Scene.RegionInfo;
                string param = Util.CombineParams(cmdparams, 2);
                if (presence.Name.ToLower().Contains(param.ToLower()) ||
                    (presence.Firstname.ToLower().Contains(cmdparams[2].ToLower()) &&
                     presence.Lastname.ToLower().Contains(cmdparams[3].ToLower())))
                {
                    MainConsole.Instance.Info(String.Format("Kicking user: {0,-16}{1,-37} in region: {2,-16}",
                                                            presence.Name, presence.UUID, regionInfo.RegionName));

                    // kick client...
                    presence.ControllingClient.Kick(alert ?? "\nThe Aurora manager kicked you out.\n");

                    // ...and close on our side
                    IEntityTransferModule transferModule =
                        presence.Scene.RequestModuleInterface<IEntityTransferModule>();
                    if (transferModule != null)
                        transferModule.IncomingCloseAgent(presence.Scene, presence.UUID);
                }
            }
            MainConsole.Instance.Info("");
        }

        /// <summary>
        ///     Force resending of all updates to all clients in active region(s)
        /// </summary>
        /// <param name="args"></param>
        private void HandleForceUpdate(string[] args)
        {
            MainConsole.Instance.Info("Updating all clients");
            ISceneEntity[] EntityList = m_scene.Entities.GetEntities();

            foreach (SceneObjectGroup ent in EntityList.OfType<SceneObjectGroup>())
            {
                (ent).ScheduleGroupUpdate(
                    PrimUpdateFlags.ForcedFullUpdate);
            }
            List<IScenePresence> presences = m_scene.Entities.GetPresences();

            foreach (IScenePresence presence in presences.Where(presence => !presence.IsChildAgent))
            {
                IScenePresence presence1 = presence;
                m_scene.ForEachClient(
                    client => client.SendAvatarDataImmediate(presence1));
            }
        }

        /// <summary>
        ///     Load, Unload, and list Region modules in use
        /// </summary>
        /// <param name="cmd"></param>
        private void HandleModulesUnload(string[] cmd)
        {
            List<string> args = new List<string>(cmd);
            args.RemoveAt(0);
            string[] cmdparams = args.ToArray();

            IRegionModulesController controller =
                m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<IRegionModulesController>();
            if (cmdparams.Length > 1)
            {
                foreach (IRegionModuleBase irm in controller.AllModules)
                {
                    if (irm.Name.ToLower() == cmdparams[1].ToLower())
                    {
                        MainConsole.Instance.Info(String.Format("Unloading module: {0}", irm.Name));
                        irm.RemoveRegion(m_scene);
                        irm.Close();
                    }
                }
            }
        }

        /// <summary>
        ///     Load, Unload, and list Region modules in use
        /// </summary>
        /// <param name="cmd"></param>
        private void HandleModulesList(string[] cmd)
        {
            List<string> args = new List<string>(cmd);
            args.RemoveAt(0);

            IRegionModulesController controller =
                m_OpenSimBase.ApplicationRegistry.RequestModuleInterface<IRegionModulesController>();
            foreach (IRegionModuleBase irm in controller.AllModules)
            {
                if (irm is INonSharedRegionModule)
                    MainConsole.Instance.Info(String.Format("Nonshared region module: {0}", irm.Name));
                else
                    MainConsole.Instance.Info(String.Format("Unknown type " + irm.GetType() + " region module: {0}",
                                                            irm.Name));
            }
        }

        /// <summary>
        ///     Runs commands issued by the server console from the operator
        /// </summary>
        /// <param name="cmdparams">Additional arguments passed to the command</param>
        private void RunCommand(string[] cmdparams)
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
                            if (MainConsole.Instance.Prompt("Are you sure you want to reset the region?", "yes") !=
                                "yes")
                                return;
                            ResetRegion();
                        }
                    break;
                case "command-script":
                    if (cmdparams.Length > 0)
                    {
                        m_OpenSimBase.RunCommandScript(cmdparams[0]);
                    }
                    break;

                case "restart-instance":
                    //This kills the instance and restarts it
                    IRestartModule restartModule = m_scene.RequestModuleInterface<IRestartModule>();
                    if (restartModule != null)
                        restartModule.RestartScene();
                    break;
            }
        }

        /// <summary>
        ///     Turn on some debugging values for Aurora.
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
                            MainConsole.Instance.Info("packet debug should be 0..255");
                        }
                        MainConsole.Instance.Info(String.Format("New packet debug: {0}", newDebug));
                    }

                    break;
                default:

                    MainConsole.Instance.Info("Unknown debug");
                    break;
            }
        }

        /// <summary>
        ///     Set the debug packet level on the current scene.  This level governs which packets are printed out to the
        ///     console.
        /// </summary>
        /// <param name="newDebug"></param>
        private void SetDebugPacketLevelOnCurrentScene(int newDebug)
        {
            m_scene.ForEachScenePresence(scenePresence =>
                                             {
                                                 if (scenePresence.IsChildAgent) return;
                                                 MainConsole.Instance.DebugFormat(
                                                     "Packet debug for {0} set to {1}",
                                                     scenePresence.Name,
                                                     newDebug);

                                                 scenePresence.ControllingClient.SetDebugPacketLevel(
                                                     newDebug);
                                             });
        }

        private void HandleShowUsers(string[] cmd)
        {
            List<string> args = new List<string>(cmd);
            args.RemoveAt(0);
            string[] showParams = args.ToArray();

            List<IScenePresence> agents = new List<IScenePresence>();
            agents.AddRange(m_scene.GetScenePresences());
            if (showParams.Length == 1 || showParams[1] != "full")
                agents.RemoveAll(sp => sp.IsChildAgent);

            MainConsole.Instance.Info(String.Format("\nAgents connected: {0}\n", agents.Count));

            MainConsole.Instance.Info(String.Format("{0,-16}{1,-16}{2,-37}{3,-11}{4,-16}{5,-30}", "Firstname",
                                                    "Lastname", "Agent ID", "Root/Child", "Region", "Position"));

            foreach (IScenePresence presence in agents)
            {
                RegionInfo regionInfo = presence.Scene.RegionInfo;

                string regionName = regionInfo == null ? "Unresolvable" : regionInfo.RegionName;

                MainConsole.Instance.Info(String.Format("{0,-16}{1,-37}{2,-11}{3,-16}{4,-30}", presence.Name,
                                                        presence.UUID, presence.IsChildAgent ? "Child" : "Root",
                                                        regionName, presence.AbsolutePosition.ToString()));
            }

            MainConsole.Instance.Info(String.Empty);
            MainConsole.Instance.Info(String.Empty);
        }

        private void HandleShowRegions(string[] cmd)
        {
            MainConsole.Instance.Info(m_scene.ToString());
        }

        private void HandleShowMaturity(string[] cmd)
        {
            string rating = "";
            if (m_scene.RegionInfo.RegionSettings.Maturity == 1)
            {
                rating = "Mature";
            }
            else if (m_scene.RegionInfo.RegionSettings.Maturity == 2)
            {
                rating = "Adult";
            }
            else
            {
                rating = "PG";
            }
            MainConsole.Instance.Info(String.Format("Region Name: {0}, Region Rating {1}", m_scene.RegionInfo.RegionName,
                                                    rating));
        }

        /// <summary>
        ///     Load a whole region from an opensimulator archive.
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void LoadOar(string[] cmdparams)
        {
            try
            {
                IRegionArchiverModule archiver = m_scene.RequestModuleInterface<IRegionArchiverModule>();
                if (archiver != null)
                    archiver.HandleLoadOarConsoleCommand(cmdparams);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error(e.ToString());
            }
        }

        /// <summary>
        ///     Save a region to a file, including all the assets needed to restore it.
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void SaveOar(string[] cmdparams)
        {
            IRegionArchiverModule archiver = m_scene.RequestModuleInterface<IRegionArchiverModule>();
            if (archiver != null)
                archiver.HandleSaveOarConsoleCommand(cmdparams);
        }

        #endregion
    }
}